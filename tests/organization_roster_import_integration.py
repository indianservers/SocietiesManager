"""Real Excel/HTTP/PostgreSQL import checks, including concurrent confirmation.

Runs membership regression fixtures first; only assemblia_test is touched.
"""
import membership_experience_integration as m
import base64, concurrent.futures, datetime, io, json, subprocess, time, uuid, zipfile
import xml.etree.ElementTree as ET
from api_runtime import API_DLL

ROOT=m.ROOT;owner=m.person;base=m.base;tenant=m.tenant;institution=m.institution
path=base+'/members/'+institution+'/roster';results=[]
log=(ROOT/'.local/roster-excel-api.log').open('w',encoding='utf-8')
server=subprocess.Popen(['dotnet',str(API_DLL),'--urls','http://127.0.0.1:5089'],cwd=ROOT/'backend',env=m.env,stdout=log,stderr=log)
NS='http://schemas.openxmlformats.org/spreadsheetml/2006/main'

def check(name,fn):
 fn();results.append(name);print('PASS EXCEL',name,flush=True)
def yes(value):assert value
def request(rows,client=owner):return client.request(path+'/imports/preview','POST',{'format':'xlsx','content':workbook(rows)})
def commit(preview,client=owner,expected=200):return client.request(path+'/imports/'+preview['id']+'/commit','POST',expected=expected)
def workbook(rows,formula=False):
 root=ET.Element('{'+NS+'}worksheet');data=ET.SubElement(root,'{'+NS+'}sheetData')
 headers=['Full name','Member type','Roll number','Mobile','Email','Member ID']
 for i,values in enumerate([headers,*rows],1):
  row=ET.SubElement(data,'{'+NS+'}row',r=str(i))
  for j,value in enumerate(values):
   cell=ET.SubElement(row,'{'+NS+'}c',r=chr(65+j)+str(i),t='inlineStr')
   ET.SubElement(ET.SubElement(cell,'{'+NS+'}is'),'{'+NS+'}t').text=str(value)
   if formula and i==2 and j==0:ET.SubElement(cell,'{'+NS+'}f').text='1+1'
 out=io.BytesIO()
 with zipfile.ZipFile(io.BytesIO(template)) as original,zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED) as archive:
  for entry in original.infolist():archive.writestr(entry,ET.tostring(root,encoding='utf-8',xml_declaration=True) if entry.filename=='xl/worksheets/sheet1.xml' else original.read(entry))
 return base64.b64encode(out.getvalue()).decode()

try:
 for _ in range(100):
  try:owner.request('/auth/csrf');break
  except Exception:time.sleep(.2)
 m.sql('UPDATE "Memberships" SET "EffectiveFrom"=now()-interval \'1 year\', "EndsAt"=now()+interval \'1 year\' WHERE "Id"=%s',(institution,))
 with owner.opener.open('http://127.0.0.1:5089/api/v1'+path+'/template') as response:template=response.read()
 with zipfile.ZipFile(io.BytesIO(template)) as archive:
  check('Excel template has member sheet, instructions and Text identifier formatting',lambda:yes('xl/worksheets/sheet2.xml' in archive.namelist() and b'numFmtId="49"' in archive.read('xl/styles.xml')))
 token=uuid.uuid4().hex[:8]
 rows=[['Roll Student','Student','000012345','','',''],['Mobile Faculty','Faculty','','+91 (98765) 43210','',''],['Email Staff','Staff','','','staff-'+token+'@example.test',''],['Custom Faculty','Faculty','FAC-'+token,'','','CUSTOM-'+token],['Auto Student','Student','','','','']]
 before=len(owner.request(path));preview=request(rows)
 check('XLSX previews students and faculty without creating members',lambda:yes(preview['valid']==5 and len(owner.request(path))==before))
 numbers=[r['number'] for r in preview['rows']]
 check('roll number leading zeros are preserved as member ID',lambda:yes(numbers[0]=='000012345'))
 check('mobile is normalized and used as member ID',lambda:yes(numbers[1]=='919876543210'))
 check('email can be the member ID',lambda:yes(numbers[2]=='STAFF-'+token.upper()+'@EXAMPLE.TEST'))
 check('explicit ID takes precedence over roll number',lambda:yes(numbers[3]=='CUSTOM-'+token))
 check('ID is automatically generated when identifiers are absent',lambda:yes(numbers[4].startswith('ORG-')))
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
  # Use independent authenticated clients; cookie jars are not shared across threads.
  clients=[m.p.Client(),m.p.Client()]
  for client in clients:m.p.login(client,m.email)
  responses=list(pool.map(lambda client:commit(preview,client),clients))
 check('concurrent confirmations import every valid row exactly once',lambda:yes(all(r['committedAt'] for r in responses) and len(owner.request(path))==before+5))
 check('confirmed IDs are identical to previewed IDs',lambda:yes(set(numbers).issubset({r['number'] for r in owner.request(path)})))
 repeated=request(rows)
 check('re-uploading existing identified members flags duplicates instead of duplicating them',lambda:yes(repeated['invalid']==5 and repeated['valid']==0))
 duplicate=request([['One','Student','DUP-'+token,'','',''],['Two','Faculty','dup-'+token,'','',''],['Three','Student','','+91 91234 56789','','MOB-A-'+token],['Four','Faculty','','0091(91234)56789','','MOB-B-'+token],['Five','Staff','','','same-'+token+'@example.test','EMAIL-A-'+token],['Six','Staff','','','SAME-'+token+'@EXAMPLE.TEST','EMAIL-B-'+token]])
 check('every occurrence of case-insensitive roll, normalized mobile and email duplicates is flagged',lambda:yes(duplicate['invalid']==6 and all(r['errors'] for r in duplicate['rows'])))
 check('same names without identifying details are flagged rather than duplicated',lambda:yes(request([['Same Name','Student','','','',''],['Same Name','Faculty','','','','']])['invalid']==2))
 check('scientific-notation identifiers require correction to Text',lambda:yes(request([['Rounded ID','Student','1.234567E+16','','','']])['invalid']==1))
 check('duplicate mapped header aliases are rejected',lambda:owner.request(path+'/imports/preview','POST',{'format':'csv','content':'Full name,Role,Email,Email ID\nTest,Student,a@example.test,b@example.test'},expected=400))
 check('legacy XLS workbooks require conversion rather than silent misreading',lambda:owner.request(path+'/imports/preview','POST',{'format':'xls','content':'invalid'},expected=400))
 invalid=request([['Bad email','Student','','','invalid',''],['Bad phone','Faculty','','phone','',''],['','Faculty','MISSING-'+token,'','',''],['Good','Student','VALID-'+token,'','','']])
 check('row validation identifies missing names, malformed emails and invalid mobiles',lambda:yes(invalid['invalid']==3 and invalid['valid']==1))
 committed=commit(invalid)
 check('explicit import adds valid rows and skips highlighted invalid rows',lambda:yes(committed['valid']==1 and len(owner.request(path))==before+6))
 owner.request(path+'/imports/preview','POST',{'format':'xlsx','content':workbook([['Formula','Student','','','','']],formula=True)},expected=400)
 check('Excel formulas are rejected instead of evaluated',lambda:yes(True))
 owner.request(path+'/imports/preview','POST',{'format':'xlsx','content':'invalid-base64'},expected=400)
 check('malformed workbooks fail with a readable validation response',lambda:yes(True))
 csv=owner.request(path+'/imports/preview','POST',{'format':'csv','content':'Name,Type,Roll No,Mobile,Email,Member ID\nCSV Faculty,Faculty,CSV-'+token+',,,\n'})
 check('CSV and common Excel header aliases work',lambda:yes(csv['valid']==1 and csv['rows'][0]['number']=='CSV-'+token))
 stale=request([['Stale','Student','STALE-'+token,'','','']])
 owner.request(path,'POST',{'fullName':'Concurrent add','role':'Student','email':None,'reference':'STALE-'+token})
 check('commit rechecks concurrent roster changes and refuses the stale preview',lambda:commit(stale,expected=409))
 expired=request([['Expired','Student','EXPIRED-'+token,'','','']])
 m.sql('UPDATE "OrganizationRosterImport" SET "ExpiresAt"=now()-interval \'1 day\' WHERE "Id"=%s',(expired['id'],))
 check('expired previews cannot be committed',lambda:commit(expired,expected=400))
 pending=request([['Held','Student','HELD-'+token,'','','']])
 m.sql('UPDATE "Memberships" SET "Status"=\'Suspended\' WHERE "Id"=%s',(institution,))
 check('membership hold prevents import confirmation even after a valid preview',lambda:commit(pending,expected=400))
 m.sql('UPDATE "Memberships" SET "Status"=\'Active\' WHERE "Id"=%s',(institution,))
 faculty=m.p.Client();m.p.login(faculty,m.faculty_email)
 check('ordinary faculty cannot bulk upload an institution roster',lambda:faculty.request(path+'/imports/preview','POST',{'format':'csv','content':'Full name,Role\nUnauthorized,Faculty'},expected=403))
 owner.request(base+'/members/'+institution+'/representatives','POST',{'email':m.rep_email,'role':'Admin'})
 admin=m.p.Client();m.p.login(admin,m.rep_email)
 check('delegated institutional admins can preview Excel uploads',lambda:yes(request([['Admin student','Student','ADMIN-'+token,'','','']],admin)['valid']==1))
 check('another admin cannot commit a different uploader preview',lambda:commit(pending,admin,expected=403))
 owner.request(base+'/members/'+institution+'/representatives/'+next(r['id'] for r in owner.request(base+'/members/'+institution+'/representatives') if r['userId']==m.rep_user),'DELETE',expected=204)
 owner.request(base+'/members/'+institution+'/representatives','POST',{'email':m.rep_email,'role':'Coordinator'})
 check('chapter coordinators do not gain bulk roster authority',lambda:admin.request(path+'/imports/preview','POST',{'format':'csv','content':'Full name,Role\nUnauthorized,Student'},expected=403))
 owner.request(base+'/members/'+institution+'/representatives/'+next(r['id'] for r in owner.request(base+'/members/'+institution+'/representatives') if r['userId']==m.rep_user),'DELETE',expected=204)
 check('cross-society import identifiers do not bypass tenant boundaries',lambda:owner.request('/societies/'+m.p.otherTenant+'/membership/members/'+institution+'/roster/imports/'+pending['id']+'/commit','POST',expected=403))
 target=next(r for r in owner.request(path) if r['number']=='000012345')
 owner.request(path+'/'+target['id'],'DELETE',expected=204)
 check('removed member IDs are reserved and cannot be silently recycled',lambda:yes(request([rows[0]])['invalid']==1))
 other=base+'/members/'+m.corporate+'/roster'
 check('different organizations may use the same local roll number',lambda:yes(owner.request(other,'POST',{'fullName':'Corporate Student','role':'Employee','email':None,'reference':'000012345'})['number']=='000012345'))
 # Fixture for browser upload checks. It contains synthetic test data only.
 (ROOT/'.local/roster-upload-fixture.xlsx').write_bytes(base64.b64decode(workbook([['Browser Excel Student','Student','BROWSER-'+token,'','',''],['Existing Student','Student','VALID-'+token,'','','']])))
 fixture=json.loads((ROOT/'.local/roster-browser-fixture.json').read_text());fixture['uploadNumber']='BROWSER-'+token;(ROOT/'.local/roster-browser-fixture.json').write_text(json.dumps(fixture),encoding='utf-8')
finally:
 server.terminate();server.wait(timeout=20);log.close()
 (ROOT/'tests/organization-roster-import-results.json').write_text(json.dumps({'passed':len(results),'checks':results},indent=2),encoding='utf-8')
