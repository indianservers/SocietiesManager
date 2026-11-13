"""Mobile end-to-end roster workflows against a dedicated test database."""
import json, os, shutil, subprocess, time, urllib.request, uuid
from pathlib import Path
import psycopg
from playwright.sync_api import sync_playwright, expect
from api_runtime import API_DLL
ROOT=Path(__file__).resolve().parents[1]
f=json.loads((ROOT/'.local/roster-browser-fixture.json').read_text())
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
  page.get_by_label('Roll number / employee reference (optional)').fill('MOBILE-001')
  page.get_by_role('button',name='Add member & generate ID').click()
  expect(page.get_by_text(label+' · Student',exact=True)).to_be_visible()
  row=page.locator('.renewal-history > div').filter(has=page.get_by_text(label+' · Student',exact=True))
  new_id=row.locator('.roster-number').inner_text()
  check('adding student generates a distinct membership ID',new_id.startswith('ORG-') and new_id!=f['studentNumber'])
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   check('roster and IDs fit mobile width '+str(width),page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
  page.screenshot(path=str(ROOT/'tests/organization-roster-mobile.png'),full_page=True)
  row.get_by_role('button',name='Remove',exact=True).click()
  expect(row.get_by_text('Removed',exact=True)).to_be_visible()
  check('removing student retains their ID in history',row.locator('.roster-number').inner_text()==new_id)
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
