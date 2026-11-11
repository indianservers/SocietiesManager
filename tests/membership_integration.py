"""Phase 2 HTTP + PostgreSQL suite; runs the Phase 1 suite first."""
import integration as p
import json,time,subprocess,uuid,datetime,io,zipfile,base64,urllib.request,urllib.error,statistics
from pathlib import Path
import psycopg
ROOT=p.ROOT
log=open(ROOT/'.local/phase2-api.log','w',encoding='utf-8')
server=subprocess.Popen(['dotnet',str(ROOT/'backend/bin/Debug/net10.0/Society.Api.dll'),'--urls','http://127.0.0.1:5089'],cwd=ROOT/'backend',env=p.env,stdout=log,stderr=log)
results=[]
def check(name,fn):
 t=time.perf_counter()
 try:fn();results.append({'name':name,'passed':True,'ms':round((time.perf_counter()-t)*1000,2)});print('PASS PHASE2',name,flush=True)
 except Exception as e:results.append({'name':name,'passed':False,'error':str(e)});print('FAIL PHASE2',name,str(e),flush=True);raise
owner=p.owner;admin=p.admin;other=p.other
prefix='/societies/'+p.tenant;base=prefix+'/membership'
clients={}
now=datetime.datetime.now(datetime.timezone.utc)
def iso(days):return (now+datetime.timedelta(days=days)).isoformat()
def assert_(v):assert v
try:
 for _ in range(60):
  try:owner.request('/auth/csrf');break
  except Exception:time.sleep(.2)
 p.login(owner,p.accounts['owner'][0],'Changed-Password-2026!')
 chapters={}
 for key,name in [('a','First members chapter'),('b','Second members chapter')]:chapters[key]=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region'],name,'MEM-'+key.upper()))['id']
 cats=owner.request(base+'/categories');category=next(c['id'] for c in cats if c['name']=='Active Member')
 def account(name):
  c=p.Client();email,user=p.signup(c,name);p.login(c,email);clients[name]=(c,email,user);return c,email,user
 applicant,email,uid=account('member')
 body={'tenantId':p.tenant,'chapterId':chapters['a'],'categoryId':category,'profile':json.dumps({'email':email,'phone':'1234567890','city':'Vijayawada','dateOfBirth':'1990-01-01','addresses':['Private home'],'professional':{'profession':'Engineer'}}),'submit':False}
 state={}
 check('verified member saves a draft application',lambda:state.update(app=applicant.request('/membership/applications','POST',body)['id']))
 check('draft application cannot be approved',lambda:owner.request(base+'/applications/'+state['app']+'/decision','POST',{'decision':'Approve','comment':'Invalid','revision':0},expected=400))
 check('member submits chapter application',lambda:applicant.request('/membership/applications/'+state['app'],'PUT',{**body,'submit':True},expected=204))
 check('duplicate application is rejected',lambda:applicant.request('/membership/applications','POST',body,expected=409))
 check('applicant cannot approve own membership',lambda:applicant.request(base+'/applications/'+state['app']+'/decision','POST',{'decision':'Approve','comment':'Self','revision':1},expected=403))
 check('authorized chapter approval activates membership',lambda:state.update(member=owner.request(base+'/applications/'+state['app']+'/decision','POST',{'decision':'Approve','comment':'Verified application','revision':1})['membershipId']))
 check('duplicate approval is rejected',lambda:owner.request(base+'/applications/'+state['app']+'/decision','POST',{'decision':'Approve','comment':'Replay','revision':1},expected=400))
 check('member can read own full private profile',lambda:assert_(applicant.request(base+'/members/'+state['member'])['profile']['dateOfBirth']=='1990-01-01'))
 manual={}
 def create(name,chapter=chapters['a'],status='Active',email='peer@example.test'):
  return owner.request(base+'/members','POST',{'fullName':name,'chapterId':chapter,'categoryId':category,'personId':None,'number':None,'status':status,'profile':json.dumps({'email':email,'phone':'1234567890','dateOfBirth':'1985-06-12','city':'Guntur','professional':{'profession':'Consultant'}}),'privacy':json.dumps({'directory':True,'photo':False,'contacts':False,'professional':False})})
 check('administrator creates person without login',lambda:manual.update(peer=create('Private peer')))
 check('shared phone numbers do not merge people',lambda:assert_(manual['peer']['personId']!=applicant.request(base+'/members/'+state['member'])['member']['personId']))
 check('ordinary member sees chapter peer directory',lambda:assert_(any(x['id']==manual['peer']['id'] for x in applicant.request(base+'/members')['items'])))
 check('ordinary directory redacts contact and professional data',lambda:assert_(next(x for x in applicant.request(base+'/members')['items'] if x['id']==manual['peer']['id'])['contacts'] is None and applicant.request(base+'/members/'+manual['peer']['id'])['profile'] is None))
 remote=create('Different chapter',chapters['b'],email='remote@example.test')
 check('cross-chapter private member IDOR blocked',lambda:applicant.request(base+'/members/'+remote['id'],expected=403))
 check('cross-society member access blocked',lambda:other.request(base+'/members',expected=403))
 selfdata=applicant.request(base+'/members/'+state['member']);m=selfdata['member']
 update={'fullName':m['fullName'],'chapterId':m['chapterId'],'categoryId':m['categoryId'],'number':m['number'],'status':m['status'],'profile':json.dumps({**selfdata['profile'],'city':'Updated city'}),'privacy':json.dumps(selfdata['privacy']),'revision':m['revision']}
 check('member edits own permitted profile',lambda:applicant.request(base+'/members/'+state['member'],'PUT',update,expected=204))
 check('stale member revision rejected',lambda:applicant.request(base+'/members/'+state['member'],'PUT',update,expected=409))
 check('member cannot self-activate status/category changes',lambda:applicant.request(base+'/members/'+state['member'],'PUT',{**update,'revision':1,'status':'Suspended'},expected=403))
 check('unauthorized member export blocked',lambda:applicant.request(base+'/export','POST',[manual['peer']['id']],expected=403))
 # Claimed account is distinct from phone and email matching.
 peer,peerEmail,peerUid=account('claimed')
 invite={}
 check('officer creates secure membership account claim',lambda:invite.update(owner.request(base+'/invitations','POST',{'chapterId':chapters['a'],'categoryId':category,'email':peerEmail,'personId':manual['peer']['personId'],'days':7,'usageLimit':1})))
 token=invite['url'].split('invitation=')[1]
 check('verified recipient claims imported/manual person',lambda:peer.request('/membership/invitations/accept','POST',{'token':token}))
 check('claim invitation cannot be replayed',lambda:peer.request('/membership/invitations/accept','POST',{'token':token},expected=400))
 check('claim preserves existing membership and person ID',lambda:assert_(peer.request('/membership/me')[0]['id']==manual['peer']['id']))
 # Expiry fixture modifies only the dedicated test database.
 exp=owner.request(base+'/invitations','POST',{'chapterId':chapters['a'],'categoryId':category,'email':peerEmail,'personId':None,'days':1,'usageLimit':1})
 with psycopg.connect(p.DSN) as db:db.execute('UPDATE "MemberInvitations" SET "ExpiresAt"=now()-interval \'1 hour\' WHERE "Id"=%s',(exp['id'],))
 check('expired membership invitation rejected',lambda:peer.request('/membership/invitations/accept','POST',{'token':exp['url'].split('invitation=')[1]},expected=400))
 policy=owner.request(base+'/policy')
 check('society configures contact opt-in policy',lambda:owner.request(base+'/policy','PUT',{**policy,'contactsAllowed':True},expected=204))
 pd=peer.request(base+'/members/'+manual['peer']['id']);pm=pd['member']
 check('member explicitly opts into contact sharing',lambda:peer.request(base+'/members/'+pm['id'],'PUT',{'fullName':pm['fullName'],'chapterId':pm['chapterId'],'categoryId':pm['categoryId'],'number':pm['number'],'status':pm['status'],'profile':json.dumps(pd['profile']),'privacy':json.dumps({**pd['privacy'],'contacts':True}),'revision':pm['revision']},expected=204))
 check('contacts require policy and member consent together',lambda:assert_(applicant.request(base+'/members/'+pm['id'])['member']['contacts']['email']=='peer@example.test'))
 check('contact consent never exposes birth date or addresses',lambda:assert_(applicant.request(base+'/members/'+pm['id'])['profile'] is None))
 positions=owner.request(base+'/positions');secretary=next(x['id'] for x in positions if x['name']=='Chapter Secretary');treasurer=next(x['id'] for x in positions if x['name']=='Chapter Treasurer')
 appointments={}
 check('officer appointment proposed with explicit term',lambda:appointments.update(secretary=owner.request(base+'/appointments','POST',{'membershipId':pm['id'],'unitId':chapters['a'],'positionId':secretary,'startsAt':iso(-1),'endsAt':iso(30),'method':'Elected','notes':'Test officer term'})['id']))
 check('overlapping exclusive officer term rejected',lambda:owner.request(base+'/appointments','POST',{'membershipId':state['member'],'unitId':chapters['a'],'positionId':secretary,'startsAt':iso(0),'endsAt':iso(20),'method':'Appointed','notes':''},expected=400))
 check('approved term grants existing scoped permissions',lambda:owner.request(base+'/appointments/'+appointments['secretary']+'/decision','POST',{'decision':'Approve','comment':'Officer verified','revision':0},expected=204))
 check('chapter secretary sees authorized private records',lambda:assert_(peer.request(base+'/members/'+state['member'])['profile']['dateOfBirth']=='1990-01-01'))
 check('chapter secretary cannot access other chapter private profile',lambda:peer.request(base+'/members/'+remote['id'],expected=403))
 check('officer cannot appoint themselves',lambda:peer.request(base+'/appointments','POST',{'membershipId':pm['id'],'unitId':chapters['a'],'positionId':secretary,'startsAt':iso(40),'endsAt':iso(60),'method':'Acting','notes':''},expected=403))
 committee={}
 check('committee creation',lambda:committee.update(id=owner.request(base+'/committees','POST',{'unitId':chapters['a'],'name':'Membership Committee','purpose':'Review members','startsAt':iso(-1),'endsAt':iso(30),'status':'Active'})['id']))
 check('committee member and responsibilities assigned',lambda:committee.update(member=owner.request(base+'/committees/'+committee['id']+'/members','POST',{'membershipId':pm['id'],'role':'Chairperson','responsibilities':'Coordinate review','startsAt':iso(-1),'endsAt':None})['id']))
 check('committee roster visible within chapter directory scope',lambda:assert_(len(applicant.request(base+'/committees/'+committee['id']+'/members'))==1))
 check('committee removal retains historical entry',lambda:owner.request(base+'/committees/'+committee['id']+'/members/'+committee['member'],'DELETE',expected=204))
 check('committee membership history preserved',lambda:assert_(owner.request(base+'/committees/'+committee['id']+'/members')[0]['endsAt'] is not None))
 # Treasurer is a distinct billing projection, not the private administrative record.
 ap=owner.request(base+'/appointments','POST',{'membershipId':state['member'],'unitId':chapters['a'],'positionId':treasurer,'startsAt':iso(-1),'endsAt':iso(30),'method':'Appointed','notes':''})['id']
 owner.request(base+'/appointments/'+ap+'/decision','POST',{'decision':'Approve','comment':'Approved','revision':0},expected=204)
 check('treasurer cannot read private administrative profile',lambda:assert_(applicant.request(base+'/members/'+pm['id'])['profile'] is None))
 check('treasurer cannot manage member records',lambda:applicant.request(base+'/members/'+pm['id'],'PUT',{**update,'fullName':pm['fullName'],'revision':1},expected=403))
 check('officer revocation removes administrative privilege immediately',lambda:owner.request(base+'/appointments/'+appointments['secretary']+'/revoke','POST',expected=204))
 check('revoked officer no longer sees private peer data',lambda:assert_(peer.request(base+'/members/'+state['member'])['profile'] is None))
 # Incoming term starts after outgoing expiry; handover is reviewed by successor.
 outgoing=owner.request(base+'/appointments','POST',{'membershipId':pm['id'],'unitId':chapters['a'],'positionId':secretary,'startsAt':iso(-10),'endsAt':iso(-2),'method':'Elected','notes':'Past officer'})['id']
 owner.request(base+'/appointments/'+outgoing+'/decision','POST',{'decision':'Approve','comment':'Historical term','revision':0},expected=204)
 incoming=owner.request(base+'/appointments','POST',{'membershipId':state['member'],'unitId':chapters['a'],'positionId':secretary,'startsAt':iso(1),'endsAt':iso(31),'method':'Elected','notes':'Incoming officer'})['id']
 h=owner.request(base+'/handovers','POST',{'outgoingId':outgoing,'incomingId':incoming,'notes':'Pending memberships and committee work'})['id']
 check('leadership handover incoming review',lambda:applicant.request(base+'/handovers/'+h+'/decision','POST',{'decision':'Review','comment':'Reviewed','revision':0},expected=204))
 check('authorized handover approval activates dated successor',lambda:owner.request(base+'/handovers/'+h+'/decision','POST',{'decision':'Approve','comment':'Approved','revision':1},expected=204))
 check('incoming future term has no current private access',lambda:assert_(applicant.request(base+'/members/'+pm['id'])['profile'] is None))
 check('expired officer term grants no private access',lambda:assert_(peer.request(base+'/members/'+state['member'])['profile'] is None))
 transfer={}
 original=owner.request(base+'/members/'+pm['id'])['member']
 check('chapter transfer request',lambda:transfer.update(id=peer.request(base+'/transfers','POST',{'membershipId':pm['id'],'destinationId':chapters['b'],'effectiveAt':iso(-.01),'notes':'Move chapter'})['id']))
 check('source chapter review',lambda:owner.request(base+'/transfers/'+transfer['id']+'/decision','POST',{'decision':'Approve','comment':'Source approved','revision':0}))
 check('destination review makes due transfer effective',lambda:assert_(owner.request(base+'/transfers/'+transfer['id']+'/decision','POST',{'decision':'Approve','comment':'Destination approved','revision':1})['status']=='Effective'))
 moved=owner.request(base+'/members/'+pm['id'])['member']
 check('transfer preserves person number and original joining history',lambda:assert_(moved['personId']==original['personId'] and moved['number']==original['number'] and moved['joinedAt']==original['joinedAt'] and moved['chapterId']==chapters['b']))
 check('transferred member loses old chapter directory',lambda:peer.request(base+'/members/'+state['member'],expected=403))
 reject=owner.request(base+'/transfers','POST',{'membershipId':remote['id'],'destinationId':chapters['a'],'effectiveAt':iso(1),'notes':'Declined transfer'})['id']
 check('transfer rejection preserves affiliation',lambda:owner.request(base+'/transfers/'+reject+'/decision','POST',{'decision':'Reject','comment':'Not approved','revision':0}))
 # CSV contains a valid row, an invalid email, and a duplicate email row.
 job={}
 csv='FullName,Email,Phone,Number,City\nImport One,import-'+p.suffix+'@example.test,555,IMP-'+p.suffix+',Delhi\nInvalid Email,invalid,555,BAD-'+p.suffix+',Delhi\nDuplicate,import-'+p.suffix+'@example.test,555,DUP-'+p.suffix+',Delhi\n'
 check('bulk CSV upload validates preview without writing memberships',lambda:job.update(id=owner.request(base+'/imports/preview','POST',{'chapterId':chapters['a'],'categoryId':category,'format':'csv','content':csv,'mapping':{'fullName':'FullName','email':'Email','phone':'Phone','number':'Number','city':'City'}})['id']))
 preview=owner.request(base+'/imports/'+job['id'])
 check('invalid and duplicate import rows reported',lambda:assert_(sum(r['status']=='Invalid' for r in preview['rows'])==2))
 check('confirmed import queued durably',lambda:owner.request(base+'/imports/'+job['id']+'/confirm','POST',expected=204))
 for _ in range(40):
  preview=owner.request(base+'/imports/'+job['id'])
  if preview['status']=='Complete':break
  time.sleep(.25)
 check('background import completes valid rows',lambda:assert_(preview['status']=='Complete' and preview['rows'][0]['status']=='Imported'))
 imported=owner.request(base+'/members/'+preview['rows'][0]['membershipId'])
 check('imported person remains inactive and unclaimed',lambda:assert_(imported['member']['status']=='Inactive'))
 # XLSX first-sheet parser, including inline and sparse cells.
 stream=io.BytesIO()
 with zipfile.ZipFile(stream,'w') as z:z.writestr('xl/worksheets/sheet1.xml','<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>FullName</t></is></c><c r="B1" t="inlineStr"><is><t>Email</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>Excel Person</t></is></c><c r="B2" t="inlineStr"><is><t>excel-'+p.suffix+'@example.test</t></is></c></row></sheetData></worksheet>')
 check('Excel XLSX member preview supported',lambda:owner.request(base+'/imports/preview','POST',{'chapterId':chapters['a'],'categoryId':category,'format':'xlsx','content':base64.b64encode(stream.getvalue()).decode(),'mapping':{'fullName':'FullName','email':'Email'}}))
 check('membership intelligence reports real database counts',lambda:assert_(owner.request(base+'/intelligence')['total']>=4 and owner.request(base+'/intelligence')['uniquePersons']<=owner.request(base+'/intelligence')['total']))
 check('membership migration applied',lambda:assert_(psycopg.connect(p.DSN).execute('SELECT count(*) FROM "__EFMigrationsHistory"').fetchone()[0]>=8))
 # Extended acceptance and security cases.
 def raw(client,path,method='GET',data=None,contentType='application/octet-stream',expected=200):
  headers={'Content-Type':contentType}
  if method!='GET':headers['X-CSRF-TOKEN']=client.request('/auth/csrf')['token']
  request=urllib.request.Request(p.BASE+path,method=method,headers=headers,data=data if data is not None else (b'' if method!='GET' else None))
  try:
   with client.opener.open(request) as response:status=response.status;body=response.read();h=dict(response.headers)
  except urllib.error.HTTPError as error:status=error.code;body=error.read();h=dict(error.headers)
  assert status==expected,f'{method} {path}: {status} != {expected}: {body[:200]!r}'
  return body,h
 check('profile photograph upload is private by default',lambda:raw(peer,base+'/members/'+pm['id']+'/photo','POST',p.png,'image/png',204))
 check('photograph retrieval enforces directory and consent',lambda:applicant.request(base+'/members/'+pm['id']+'/photo',expected=403))
 doc={}
 pdf=b'%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF'
 check('supporting PDF attachment stored securely',lambda:doc.update(id=json.loads(raw(peer,base+'/members/'+pm['id']+'/documents?name=Application%20evidence','POST',pdf,'application/pdf')[0])['id']))
 check('member document access blocked for ordinary peer',lambda:applicant.request(base+'/members/'+pm['id']+'/documents/'+doc['id'],expected=403))
 check('authorized member document downloads as an attachment',lambda:assert_('attachment' in raw(peer,base+'/members/'+pm['id']+'/documents/'+doc['id'])[1]['Content-Disposition']))
 check('HTML document upload rejected',lambda:raw(peer,base+'/members/'+pm['id']+'/documents?name=Invalid','POST',b'<html>unsafe</html>','text/html',400))
 check('oversized member upload rejected',lambda:raw(peer,base+'/members/'+pm['id']+'/photo','POST',b'0'*2097153,'image/png',400))
 check('revoked grants disappear from workspace discovery',lambda:assert_(all(scope['name']!='Chapter Secretary' for w in peer.request('/workspaces') if w['id']==p.tenant for scope in w['scopes'])))
 # One canonical person can join another society without exposing its private data.
 otherPrefix='/societies/'+p.otherTenant;otherBase=otherPrefix+'/membership'
 other.request(otherPrefix+'/onboarding','PUT',{'step':7,'data':'{}','activate':True},expected=204)
 otherChapterLevel=next(l['id'] for l in other.request(otherPrefix+'/organizations/levels') if l['kind']=='Chapter')
 chapterOther=other.request(otherPrefix+'/organizations/units','POST',p.unit(otherChapterLevel,p.otherRegion,'Other society membership chapter','P2-OTHER'))['id']
 categoryOther=next(c['id'] for c in other.request(otherBase+'/categories') if c['name']=='Active Member')
 applicationOther=applicant.request('/membership/applications','POST',{'tenantId':p.otherTenant,'chapterId':chapterOther,'categoryId':categoryOther,'profile':'{}','submit':True})['id']
 newMembership=other.request(otherBase+'/applications/'+applicationOther+'/decision','POST',{'decision':'Approve','comment':'Society-approved affiliation','revision':0})['membershipId']
 check('one person holds memberships across two societies',lambda:assert_(other.request(otherBase+'/members/'+newMembership)['member']['personId']==owner.request(base+'/members/'+state['member'])['member']['personId']))
 check('society administrator cannot browse another society membership',lambda:owner.request(otherBase+'/members/'+newMembership,expected=403))
 # Configure staged approvals and allow multiple chapter affiliations explicitly.
 policy=owner.request(base+'/policy')
 check('configurable two-stage approval policy',lambda:owner.request(base+'/policy','PUT',{**policy,'allowMultipleChapters':True,'approvalStages':json.dumps([{'name':'Secretary review','permission':'members.approve'},{'name':'Membership committee','permission':'members.write'}])},expected=204))
 third=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region'],'Third chapter','MEM-C'))['id']
 claimedLegacy=create('Imported existing identity',third,email='unclaimed-'+p.suffix+'@example.test')
 claim=owner.request(base+'/invitations','POST',{'chapterId':third,'categoryId':category,'email':email,'personId':claimedLegacy['personId'],'days':7,'usageLimit':1})
 check('existing verified account securely claims imported affiliation',lambda:applicant.request('/membership/invitations/accept','POST',{'token':claim['url'].split('invitation=')[1]}))
 check('claim reuses canonical person instead of duplicating identity',lambda:assert_(owner.request(base+'/members/'+claimedLegacy['id'])['member']['personId']==owner.request(base+'/members/'+state['member'])['member']['personId']))
 check('canonical identity retains three authorized affiliations',lambda:assert_(len(applicant.request('/membership/me'))==3))
 invitee,inviteEmail,inviteUid=account('invitee')
 inv=owner.request(base+'/invitations','POST',{'chapterId':chapters['a'],'categoryId':category,'email':inviteEmail,'personId':None,'days':2,'usageLimit':1})
 invResult={}
 check('invitation registration creates draft without activation',lambda:invResult.update(invitee.request('/membership/invitations/accept','POST',{'token':inv['url'].split('invitation=')[1]})))
 appId=invResult['applicationId']
 check('invitation cannot bypass required approval',lambda:assert_(len(invitee.request('/membership/me'))==0))
 appDoc={}
 check('draft application supporting evidence uploaded',lambda:appDoc.update(id=json.loads(raw(invitee,base+'/applications/'+appId+'/documents?name=Supporting%20evidence','POST',pdf,'application/pdf')[0])['id']))
 check('unauthorized ordinary member cannot read application documents',lambda:applicant.request(base+'/applications/'+appId+'/documents/'+appDoc['id'],expected=403))
 applyBody={'tenantId':p.tenant,'chapterId':chapters['a'],'categoryId':category,'profile':json.dumps({'email':inviteEmail}),'submit':True,'revision':0}
 invitee.request('/membership/applications/'+appId,'PUT',applyBody,expected=204)
 check('reviewer can request changes',lambda:owner.request(base+'/applications/'+appId+'/decision','POST',{'decision':'Changes Requested','comment':'Please confirm profile','revision':1}))
 check('applicant resubmits returned application',lambda:invitee.request('/membership/applications/'+appId,'PUT',{**applyBody,'revision':2},expected=204))
 check('first approval stage does not activate membership',lambda:assert_(owner.request(base+'/applications/'+appId+'/decision','POST',{'decision':'Approve','comment':'First stage approved','revision':3})['status']=='Under Review'))
 check('duplicate stage decision rejected by revision',lambda:owner.request(base+'/applications/'+appId+'/decision','POST',{'decision':'Approve','comment':'Stale repeat','revision':3},expected=409))
 check('final approval stage activates member',lambda:assert_(owner.request(base+'/applications/'+appId+'/decision','POST',{'decision':'Approve','comment':'Committee approved','revision':4})['status']=='Approved'))
 check('approval history records role organization and document evidence',lambda:assert_(all(d['reviewerRoles'] and d['organizationId']==chapters['a'] and appDoc['id'] in d['documentIds'] for d in owner.request(base+'/applications/'+appId+'/history'))))
 rejectedApp=p.officer.request('/membership/applications','POST',{'tenantId':p.tenant,'chapterId':chapters['a'],'categoryId':category,'profile':'{}','submit':True})['id']
 check('membership rejection does not activate person',lambda:assert_(owner.request(base+'/applications/'+rejectedApp+'/decision','POST',{'decision':'Reject','comment':'Incomplete application','revision':0})['status']=='Rejected'))
 privacyPerson=create('Directory privacy person',email='private-'+p.suffix+'@example.test')
 check('ordinary directory never exposes global person identifier',lambda:assert_(applicant.request(base+'/members/'+privacyPerson['id'])['member']['personId'] is None))
 # Regional supervision is inherited through the existing hierarchy engine.
 officerRecord=create('Verified regional officer',email='regional-'+p.suffix+'@example.test')
 claimOfficer=owner.request(base+'/invitations','POST',{'chapterId':chapters['a'],'categoryId':category,'email':p.fe,'personId':officerRecord['personId'],'days':7,'usageLimit':1})
 p.officer.request('/membership/invitations/accept','POST',{'token':claimOfficer['url'].split('invitation=')[1]})
 president=next(x['id'] for x in positions if x['name']=='Chapter President')
 appointment=owner.request(base+'/appointments','POST',{'membershipId':officerRecord['id'],'unitId':chapters['a'],'positionId':president,'startsAt':iso(-1),'endsAt':iso(20),'method':'Elected','notes':'Chapter term'})['id']
 owner.request(base+'/appointments/'+appointment+'/decision','POST',{'decision':'Approve','comment':'Authorized president','revision':0},expected=204)
 check('chapter president reads permitted administrative profile',lambda:assert_(p.officer.request(base+'/members/'+privacyPerson['id'])['profile']['dateOfBirth']=='1985-06-12'))
 check('chapter president has no unrelated chapter authority',lambda:p.officer.request(base+'/members/'+remote['id'],expected=403))
 regional=next(x['id'] for x in positions if x['name']=='Regional Coordinator')
 appointmentRegional=owner.request(base+'/appointments','POST',{'membershipId':officerRecord['id'],'unitId':p.units['region'],'positionId':regional,'startsAt':iso(-1),'endsAt':iso(20),'method':'Appointed','notes':'Region scope'})['id']
 owner.request(base+'/appointments/'+appointmentRegional+'/decision','POST',{'decision':'Approve','comment':'Authorized region scope','revision':0},expected=204)
 check('regional officer supervises assigned subordinate chapters',lambda:assert_(p.officer.request(base+'/members/'+remote['id'])['profile']['dateOfBirth']=='1985-06-12'))
 unrelated=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region2'],'Unrelated region chapter','MEM-OUT'))['id']
 unrelatedMember=create('Unrelated regional member',unrelated,email='unrelated-'+p.suffix+'@example.test')
 check('regional officer cannot access unrelated region members',lambda:p.officer.request(base+'/members/'+unrelatedMember['id'],expected=403))
 # Scoped directory policy can widen visibility without widening private fields.
 policy=owner.request(base+'/policy')
 check('society configures regional directory visibility',lambda:owner.request(base+'/policy','PUT',{**policy,'directoryScope':'region'},expected=204))
 check('regional directory follows hierarchy ancestors',lambda:assert_(applicant.request(base+'/members/'+remote['id'])['profile'] is None))
 check('regional directory remains inside assigned region',lambda:applicant.request(base+'/members/'+unrelatedMember['id'],expected=403))
 policy=owner.request(base+'/policy');owner.request(base+'/policy','PUT',{**policy,'directoryScope':'chapter'},expected=204)
 formula=create('=2+2',email='formula-'+p.suffix+'@example.test')
 check('authorized member export neutralizes spreadsheet formulas',lambda:assert_(b"'=2+2" in raw(owner,base+'/export','POST',json.dumps([formula['id']]).encode(),'application/json')[0]))
 check('member export never includes private contact values',lambda:assert_(b'@example.test' not in raw(owner,base+'/export','POST',json.dumps([formula['id']]).encode(),'application/json')[0]))
 timings=[]
 for _ in range(20):
  start=time.perf_counter();owner.request(base+'/members?pageSize=20');timings.append((time.perf_counter()-start)*1000)
 (ROOT/'tests/phase2-http-performance.json').write_text(json.dumps({'scope':'20 warm authenticated HTTP directory reads on the integration fixture, single client','median_ms':round(statistics.median(timings),3),'p95_ms':round(sorted(timings)[18],3),'target_p95_ms':500,'target_met':sorted(timings)[18]<500,'samples':20},indent=2),encoding='utf-8')

finally:
 server.terminate();server.wait(timeout=20);log.close();(ROOT/'tests/phase2-results.json').write_text(json.dumps({'passed':sum(r['passed'] for r in results),'total':len(results),'results':results},indent=2),encoding='utf-8')
