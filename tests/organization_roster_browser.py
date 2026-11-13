"""Mobile end-to-end roster workflows against a dedicated test database."""
import json, os, shutil, subprocess, time, urllib.request, uuid, io, zipfile
from pathlib import Path
import psycopg
from playwright.sync_api import sync_playwright, expect
from api_runtime import API_DLL
ROOT=Path(__file__).resolve().parents[1]
f=json.loads((ROOT/'.local/roster-browser-fixture.json').read_text())
# Fresh synthetic identifiers allow repeating the browser checks without recycling IDs.
new_number='BROWSER-'+uuid.uuid4().hex[:10]
fixture=ROOT/'.local/roster-upload-fixture.xlsx'
old=fixture.read_bytes();out=io.BytesIO()
with zipfile.ZipFile(io.BytesIO(old)) as source,zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED) as target:
 for entry in source.infolist():
  data=source.read(entry)
  if entry.filename=='xl/worksheets/sheet1.xml':data=data.replace(f['uploadNumber'].encode(),new_number.encode())
  target.writestr(entry,data)
fixture.write_bytes(out.getvalue());f['uploadNumber']=new_number
(ROOT/'.local/roster-browser-fixture.json').write_text(json.dumps(f),encoding='utf-8')
config=json.loads((ROOT/'.local/dev.json').read_text())
url='http://127.0.0.1:5093'
env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','PublicUrl':url,'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=assemblia_test;Username=postgres;Password={config['password']}",'Logging__LogLevel__Default':'Warning'}
with psycopg.connect(host='127.0.0.1',port=55439,dbname='assemblia_test',user='postgres',password=config['password']) as db:
 db.execute('UPDATE "Memberships" SET "EffectiveFrom"=now()-interval \'1 year\', "EndsAt"=now()+interval \'1 year\' WHERE "Id"=%s',(f['institution'],))
shutil.copytree(ROOT/'frontend/dist',API_DLL.parent/'wwwroot',dirs_exist_ok=True)
checks=[];errors=[]
def check(name,condition=True):
 assert condition,name
 checks.append(name);print('PASS MOBILE',name,flush=True)
def login(context,key):
 token=context.request.get(url+'/api/v1/auth/csrf').json()['token']
 response=context.request.post(url+'/api/v1/auth/login',data=f[key],headers={'X-CSRF-TOKEN':token})
 assert response.ok,response.text()
log=(ROOT/'.local/roster-browser-api.log').open('w',encoding='utf-8')
server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',url],cwd=API_DLL.parent,env=env,stdout=log,stderr=log)
try:
 for _ in range(100):
  try:
   urllib.request.urlopen(url+'/health/ready');break
  except Exception:time.sleep(.2)
 with sync_playwright() as p:
  browser=p.chromium.launch()
  controller=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True)
  login(controller,'controller');page=controller.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
  page.goto(url+'/app/membership');page.wait_for_load_state('networkidle')
  expect(page.get_by_text(f['studentNumber'],exact=False)).to_be_visible()
  check('same account displays institutional member ID alongside Individual membership')
  page.get_by_role('button',name='Synthetic Institute Institutional',exact=False).click()
  page.get_by_role('button',name='Organization team & chapters').click()
  expect(page.get_by_text('Students, faculty & organization members',exact=True)).to_be_visible()
  check('institution controller can open member roster on mobile')
  label='Mobile Student '+uuid.uuid4().hex[:6]
  page.get_by_role('button',name='Add member',exact=True).click()
  page.get_by_label('Full name',exact=True).fill(label)
  mobile_reference='MOBILE-'+uuid.uuid4().hex[:10]
  page.get_by_label('Roll number / employee reference (optional)').fill(mobile_reference)
  page.get_by_role('button',name='Add member & generate ID').click()
  expect(page.get_by_text(label+' · Student',exact=True)).to_be_visible()
  row=page.locator('.renewal-history > div').filter(has=page.get_by_text(label+' · Student',exact=True))
  new_id=row.locator('.roster-number').inner_text()
  check('adding student generates a distinct membership ID',new_id==mobile_reference and new_id!=f['studentNumber'])
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   check('roster and IDs fit mobile width '+str(width),page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
  page.screenshot(path=str(ROOT/'tests/organization-roster-mobile.png'),full_page=True)
  row.get_by_role('button',name='Remove',exact=True).click()
  expect(row.get_by_text('Removed',exact=True)).to_be_visible()
  check('removing student retains their ID in history',row.locator('.roster-number').inner_text()==new_id)
  page.get_by_role('button',name='Bulk upload Excel',exact=True).click()
  check('institution admin can download an Excel upload template',page.get_by_role('link',name='Download Excel template').count()==1)
  page.get_by_label('Excel or CSV file',exact=True).set_input_files(ROOT/'.local/roster-upload-fixture.xlsx')
  page.get_by_role('button',name='Preview & check duplicates').click()
  expect(page.get_by_text('2 rows · 1 valid · 1 need attention',exact=True)).to_be_visible()
  check('Excel upload previews valid and duplicate rows before import')
  import_button=page.get_by_role('button',name='Import 1 valid members',exact=True)
  expect(import_button).to_be_disabled()
  check('skipping invalid rows requires explicit acknowledgment')
  page.get_by_label('Import only valid rows and skip the highlighted rows').check()
  expect(import_button).to_be_enabled()
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   check('bulk import preview fits mobile width '+str(width),page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
  page.set_viewport_size({'width':390,'height':844})
  page.screenshot(path=str(ROOT/'tests/organization-roster-import-mobile.png'),full_page=True)
  import_button.click()
  expect(page.get_by_text('Imported 1 members. 1 rows skipped. Their IDs are shown below.',exact=True)).to_be_visible()
  check('confirming upload adds the valid student and retains their previewed ID',page.get_by_text('Member ID: '+f['uploadNumber'],exact=True).count()==1)
  page.get_by_role('button',name='Close drawer',exact=True).click()
  expect(page.locator('.renewal-history > div').filter(has=page.get_by_text(f['uploadNumber'],exact=True)).get_by_text('Browser Excel Student · Student',exact=True)).to_be_visible()
  page.get_by_role('button',name='Bulk upload Excel',exact=True).click()
  page.get_by_label('Excel or CSV file',exact=True).set_input_files(ROOT/'.local/roster-upload-fixture.xlsx')
  page.get_by_role('button',name='Preview & check duplicates').click()
  expect(page.get_by_text('2 rows · 0 valid · 2 need attention',exact=True)).to_be_visible()
  expect(page.get_by_role('button',name='Import 0 valid members',exact=True)).to_be_disabled()
  check('re-uploading the same Excel file cannot silently add duplicate people')
  page.get_by_role('button',name='Close drawer',exact=True).click()
  faculty=browser.new_context(viewport={'width':360,'height':800},is_mobile=True,has_touch=True)
  login(faculty,'faculty');fp=faculty.new_page();fp.on('pageerror',lambda e:errors.append(str(e)))
  fp.goto(url+'/app/membership');fp.wait_for_load_state('networkidle')
  expect(fp.get_by_text(f['facultyNumber'],exact=False)).to_be_visible()
  check('faculty sees their own institutional ID without administrator access')
  check('faculty cannot see private organization roster',fp.get_by_text('Students, faculty & organization members',exact=True).count()==0)
  fp.get_by_role('button',name='Resources',exact=True).click()
  fp.get_by_role('button',name='Institution teaching toolkit',exact=True).click()
  expect(fp.get_by_text('Restricted institute resource content',exact=True)).to_be_visible()
  check('faculty can open eligible institutional resources end to end')
  check('no uncaught browser errors',not errors)
  browser.close()
finally:
 server.terminate();server.wait(timeout=20);log.close()
 (ROOT/'tests/organization-roster-browser-results.json').write_text(json.dumps({'passed':len(checks),'checks':checks,'errors':errors},indent=2),encoding='utf-8')
