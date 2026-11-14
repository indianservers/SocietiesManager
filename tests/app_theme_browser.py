"""Purple/cyan theme, mobile role coverage and real local form workflows.

Uses the opt-in synthetic development society. Temporary role grants are removed.
No live providers, payments, production members or email relays are touched.
"""
import importlib,json,os,shutil,socket,subprocess,sys,time,uuid
from pathlib import Path
import psycopg
from playwright.sync_api import sync_playwright,expect
from api_runtime import API_DLL

ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'scripts'))
Client=importlib.import_module('seed-society-example').Client
account=json.loads((ROOT/'.local/society-example-account.json').read_text());dev=json.loads((ROOT/'.local/dev.json').read_text())
probe=json.loads((ROOT/'.local/mobile-role-account.json').read_text());preview=json.loads((ROOT/'.local/preview-account.json').read_text())
dsn=dict(host='127.0.0.1',port=55439,dbname='assemblia',user='postgres',password=dev['password'])
with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
url='http://127.0.0.1:'+str(port);prefix='/societies/'+account['tenant']
dist=Path(os.environ['APP_THEME_DIST']);shutil.copytree(dist,API_DLL.parent/'wwwroot',dirs_exist_ok=True)
env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','PublicUrl':url,'Society__DefaultTenantId':account['tenant'],'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=assemblia;Username=postgres;Password={dev['password']}",'Logging__LogLevel__Default':'Warning'}
log=(ROOT/'.local/app-theme-api.log').open('w',encoding='utf-8')
subprocess.run(['dotnet',str(API_DLL),'--migrate'],cwd=API_DLL.parent,env=env,stdout=log,stderr=log,check=True)
server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',url],cwd=API_DLL.parent,env=env,stdout=log,stderr=log)
owner=Client(url+'/api/v1');checks=[];screens=[];errors=[];active_grant=None
routes=['/app','/app/membership','/app/members','/app/members/governance','/app/organization','/app/members/operations','/app/community','/app/community/events','/app/service/projects','/app/communications','/app/finance','/app/account','/app/studio','/app/settings']

def sql(query,args=()):
 with psycopg.connect(**dsn) as db:return db.execute(query,args).fetchall() if query.lstrip().startswith('SELECT') else db.execute(query,args)
def check(name,condition=True):
 assert condition,name
 checks.append(name);print('PASS THEME',name,flush=True)
def login(context,credentials):
 token=context.request.get(url+'/api/v1/auth/csrf').json()['token']
 response=context.request.post(url+'/api/v1/auth/login',data={'email':credentials['email'],'password':credentials['password']},headers={'X-CSRF-TOKEN':token});assert response.ok,response.text()
def navigate(page,path):
 if path=='/app' or page.url=='about:blank':page.goto(url+path)
 else:page.evaluate('path=>{history.pushState(null,"",path);dispatchEvent(new PopStateEvent("popstate"));}',path)
 page.wait_for_load_state('networkidle');expect(page.locator('.app-shell')).to_be_visible()
 if page.locator('.skeleton').count():page.locator('.skeleton').first.wait_for(state='hidden',timeout=15000)
def inspect(page,role,path,width,mode='light'):
 navigate(page,path)
 details=page.evaluate('''()=>({width:innerWidth,scroll:document.documentElement.scrollWidth,primary:getComputedStyle(document.querySelector('.app-shell')).getPropertyValue('--society-primary').trim(),surface:getComputedStyle(document.querySelector('.sidebar')).backgroundImage})''')
 alerts=page.locator('main [role=alert]').all_text_contents()
 for retry in range(4):
  if not any('429' in alert for alert in alerts):break
  print('RATE LIMIT cooldown',role,path,flush=True)
  page.wait_for_timeout(15000);page.reload();page.wait_for_load_state('networkidle')
  alerts=page.locator('main [role=alert]').all_text_contents()

 screens.append({'role':role,'path':path,'width':width,'mode':mode,**details,'alerts':alerts})
 if details['scroll']>width+2:
  page.screenshot(path=str(ROOT/'tests/app-theme-overflow-debug.png'),full_page=True)
  print('OVERFLOW',role,path,width,page.evaluate('''[...document.querySelectorAll('main *')].filter(e=>{const b=e.getBoundingClientRect();return b.right>innerWidth+2&&b.width>0&&!e.closest('.table-wrap,.table-scroll,.finance-table-scroll,.finance-table-wrap,.studio-table-wrap')}).slice(0,12).map(e=>({cls:e.className,w:e.getBoundingClientRect().width,text:e.textContent.slice(0,70)}))'''),flush=True)
 assert details['scroll']<=width+2,(role,path,width,details['scroll'])
 assert not alerts,(role,path,alerts)
 assert details['primary']==('#c88afa' if mode=='dark' else '#7e12bf'),(role,path,details)

try:
 for _ in range(100):
  try:owner.call('/auth/csrf');break
  except Exception:time.sleep(.2)
 owner.call('/auth/login','POST',account)
 roles=owner.call(prefix+'/roles');chapters=owner.call(prefix+'/organizations/units?kind=Chapter&pageSize=100')['items'];region=owner.call(prefix+'/organizations/units?kind=Region&pageSize=100')['items'][0]['id']
 with sync_playwright() as p:
  browser=p.chromium.launch();context=browser.new_context(viewport={'width':1440,'height':1000});login(context,account)
  page=context.new_page();page.on('pageerror',lambda error:errors.append(str(error)))
  page.goto(url+'/');page.wait_for_load_state('networkidle');check('public landing page shares the purple palette',page.locator('.landing .primary').evaluate("e=>getComputedStyle(e).backgroundImage.includes('140, 27, 197')"))
  for route in routes:inspect(page,'Society Administrator',route,1440)
  page.goto(url+'/app');page.wait_for_load_state('networkidle');page.screenshot(path=str(ROOT/'tests/app-theme-dashboard-desktop.png'),full_page=True)
  check('shared theme loads on every administrative workspace, including lazy modules')
  page.get_by_role('button',name='Toggle light or dark appearance').click()
  for route in ['/app','/app/finance','/app/studio','/app/communications','/app/membership','/app/account']:inspect(page,'Society Administrator',route,1440,'dark')
  page.screenshot(path=str(ROOT/'tests/app-theme-dark-desktop.png'),full_page=True)
  check('dark appearance uses the matching purple palette across workspaces')
  page.emulate_media(media='print');check('print output keeps a white document surface even in dark mode',page.locator('.app-shell').evaluate("e=>getComputedStyle(e).getPropertyValue('--society-surface').trim()==='#fff'"));page.emulate_media(media='screen')
  page.get_by_role('button',name='Toggle light or dark appearance').click()
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   for route in routes:inspect(page,'Society Administrator',route,width)
  check('all administrative workspaces fit 360px, 390px and 768px')
  page.set_viewport_size({'width':390,'height':844});navigate(page,'/app/members');page.get_by_role('button',name='Toggle navigation',exact=True).click();expect(page.locator('.sidebar')).to_have_class('sidebar open');page.get_by_role('link',name='Home',exact=True).click();expect(page.locator('.sidebar')).to_have_class('sidebar')
  check('mobile navigation opens and closes through real navigation')
  page.screenshot(path=str(ROOT/'tests/app-theme-dashboard-mobile.png'),full_page=True)
  member=owner.call(prefix+'/membership/members?search=IND-0001')['items'][0];before=owner.call(prefix+'/membership/members/'+member['id'])
  page.goto(url+'/app/members/profile/'+member['id']);page.wait_for_load_state('networkidle');page.get_by_role('button',name='Edit profile',exact=True).click();dialog=page.get_by_role('dialog');expect(dialog).to_be_visible()
  page.get_by_text('Personal & professional details',exact=True).click();field=dialog.get_by_label('Authorized representative name',exact=True);original=field.input_value();field.fill(original+' Theme QA')
  dialog.get_by_role('button',name='Save member',exact=True).click();expect(dialog).not_to_be_visible()
  after=owner.call(prefix+'/membership/members/'+member['id']);check('mobile themed form saves contact metadata without changing category or status',after['profile']['professional']['representativeName']==original+' Theme QA' and after['member']['categoryId']==before['member']['categoryId'] and after['member']['status']==before['member']['status'])
  page.reload();page.wait_for_load_state('networkidle');page.get_by_role('button',name='Edit profile',exact=True).click();page.get_by_text('Personal & professional details',exact=True).click();dialog=page.get_by_role('dialog');expect(dialog.get_by_label('Authorized representative name',exact=True)).to_have_value(original+' Theme QA');dialog.get_by_label('Authorized representative name',exact=True).fill(original);dialog.get_by_role('button',name='Save member',exact=True).click();expect(dialog).not_to_be_visible()
  check('mobile form tabs preserve data across save/reload and restore the test edit')
  page.goto(url+'/app/members/settings');page.wait_for_load_state('networkidle');page.get_by_role('button',name='Create category',exact=True).click();expect(page.get_by_role('dialog')).to_be_visible()
  check('drawers keep form controls inside the mobile viewport',page.get_by_role('dialog').evaluate('e=>e.getBoundingClientRect().right<=innerWidth+1'))
  page.get_by_role('button',name='Close drawer',exact=True).click()
  context.close()
  context=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True);login(context,probe);page=context.new_page();page.on('pageerror',lambda error:errors.append(str(error)))
  for role in roles:
   if role['name']=='Society Administrator':continue
   active_grant=str(uuid.uuid4());unit=chapters[0]['id'] if role['name'].startswith('Chapter') or role['name']=='Member Directory' else region if role['name'].startswith(('Regional','District','Zone')) else None
   sql('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt") VALUES (%s,%s,%s,%s,%s,now()-interval \'1 day\')',(active_grant,account['tenant'],probe['user'],role['id'],unit))
   try:
    for route in routes:inspect(page,role['name'],route,390)
    print('PASS ROLE',role['name'],flush=True)
   finally:sql('DELETE FROM "Assignments" WHERE "Id"=%s AND "UserId"=%s',(active_grant,probe['user']));active_grant=None
  context.close()
  context=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True);login(context,{'email':preview['platformEmail'],'password':preview['platformPassword']});page=context.new_page();page.on('pageerror',lambda error:errors.append(str(error)))
  inspect(page,'Platform Owner','/app/platform',390);page.screenshot(path=str(ROOT/'tests/app-theme-platform-mobile.png'),full_page=True)
  check('platform-owner dashboard shares the same mobile theme')
  member_credentials=json.loads((ROOT/'.local/mobile-participant.json').read_text());context.close();context=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True);login(context,member_credentials);page=context.new_page();page.on('pageerror',lambda error:errors.append(str(error)))
  for route in ['/app/membership','/app/finance/my','/app/account']:inspect(page,'Individual & Institution Controller',route,390)
  page.screenshot(path=str(ROOT/'tests/app-theme-member-mobile.png'),full_page=True)
  check('personal and institutional member access retains the shared palette')
  check('every configured society role was exercised',{r['name'] for r in roles}.issubset({r['role'] for r in screens}))
  check('no uncaught browser errors',not errors)
  browser.close()
finally:
 if active_grant:sql('DELETE FROM "Assignments" WHERE "Id"=%s AND "UserId"=%s',(active_grant,probe['user']))
 server.terminate();server.wait(timeout=20);log.close()
 (ROOT/'tests/app-theme-browser-results.json').write_bytes(json.dumps({'checks':checks,'screenCount':len(screens),'roles':sorted({r['role'] for r in screens}),'pageErrors':errors,'screens':screens},indent=2).encode('utf-8'))
