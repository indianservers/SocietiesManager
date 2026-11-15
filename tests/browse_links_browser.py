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
  except:time.sleep(.2)
 owner.call('/auth/login','POST',account)
 data=owner.call(prefix+'/membership/intelligence');chapter=next(c for c in data['byChapter'] if c['institutions']>0)
 with sync_playwright() as p:
  browser=p.chromium.launch();context=browser.new_context(viewport={'width':1440,'height':1000});login(context,account);page=context.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
  page.goto(url+'/app/members');expect(page.get_by_role('link',name='Browse Institutional organizations',exact=True)).to_be_visible();page.get_by_role('link',name='Browse Institutional organizations',exact=True).click();expect(page.get_by_role('heading',name='Institutional organizations',exact=True)).to_be_visible();check('overview institutional total opens its separate register')
  page.goto(url+'/app/members');page.get_by_role('link',name='Browse Institutional memberships in '+chapter['name'],exact=True).click();expect(page.get_by_role('heading',name='Institutional organizations',exact=True)).to_be_visible();assert 'chapter='+chapter['id'] in page.url;expect(page.locator('tbody tr').first).to_be_visible();check('chapter membership count opens the chapter-filtered register')
  row=page.locator('tbody tr').first;expect(row.locator('td').nth(3).locator('a')).to_have_attribute('href','/app/units/'+chapter['id']);row.locator('td').nth(2).locator('a').click();expect(page.get_by_role('heading',name='Membership at a glance',exact=True)).to_be_visible();check('membership number opens the full profile')
  page.goto(url+'/app/members');chapter_link=page.get_by_role('link',name=chapter['name'],exact=True).first;expect(chapter_link).to_have_attribute('href','/app/units/'+chapter['id']);chapter_link.click();expect(page.get_by_role('heading',name=chapter['name'],exact=True)).to_be_visible();check('chapter names open chapter details')
  assert not errors,errors;browser.close()
finally:
 server.terminate();server.wait(timeout=20);log.close();(ROOT/'.local/browse-links-browser-results.json').write_text(json.dumps({'checks':checks,'pageErrors':errors},indent=2))
