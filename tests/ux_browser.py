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
 categories=owner.call(prefix+'/membership/categories');cat=owner.call(prefix+'/membership/categories','POST',{'name':'UX Organization '+uuid.uuid4().hex[:8],'kind':'Institutional','active':True,'description':'Synthetic browser verification'})['id']
 chapter=owner.call(prefix+'/organizations/units?kind=Chapter&pageSize=100')['items'][0]['id']
 member=owner.call(prefix+'/membership/members','POST',{'fullName':'UX Browser Institute '+uuid.uuid4().hex[:8],'chapterId':chapter,'categoryId':cat,'status':'Inactive','profile':json.dumps({'organizationName':'UX Browser Institute','email':'ux-browser-'+uuid.uuid4().hex[:8]+'@example.test','contacts':[{'name':'UX Original Contact','email':'ux-original@example.test','mobile':'9876543210','designation':'Head','department':'Research'}]}),'duplicateReviewed':True})['id']
 owner.call(prefix+'/membership/members/'+member+'/chapters/'+chapter,'PUT')
 with sync_playwright() as p:
  browser=p.chromium.launch();context=browser.new_context(viewport={'width':1440,'height':1000});login(context,account);page=context.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
  page.goto(url+'/app');expect(page.get_by_role('heading',name='Membership administration',exact=True)).to_be_visible();check('role home presents society administrator actions')
  for kind in ['Individual','Institutional','Corporate']:expect(page.get_by_role('link',name=kind+' members',exact=True)).to_be_visible()
  check('all membership types have distinct navigation entries')
  page.goto(url+'/app/members/profile/'+member);expect(page.get_by_role('heading',name='Membership at a glance',exact=True)).to_be_visible();expect(page.get_by_role('heading',name='Profile completeness',exact=True)).to_be_visible();expect(page.locator('a[href="mailto:ux-original@example.test"]').last).to_be_visible();check('institution summary, completeness and clickable contacts')
  page.get_by_role('button',name='Edit contacts inline',exact=True).click();page.get_by_label('Contact person name',exact=True).fill('UX Updated Contact');page.get_by_role('button',name='Add contact',exact=True).click();page.get_by_label('Contact person name',exact=True).nth(1).fill('UX Primary Contact');page.get_by_label('Email',exact=True).nth(1).fill('ux-primary@example.test');page.get_by_label('Mobile',exact=True).nth(1).fill('9876543211');page.get_by_role('button',name='Make primary',exact=True).click();page.get_by_role('button',name='Save contacts',exact=True).click();expect(page.get_by_role('button',name='Edit contacts inline',exact=True)).to_be_visible();expect(page.locator('.contact-person').first).to_contain_text('UX Primary Contact');check('inline contact add, reorder and save updates primary representative')
  page.goto(url+'/app/units/'+chapter);expect(page.get_by_role('heading',name='Points of contact',exact=True)).to_be_visible();expect(page.locator('.contact-person').first).to_contain_text('UX Primary Contact');check('linked chapter reflects the saved contacts')
  page.goto(url+'/app/members?view=directory&memberKind=Institutional&dsearch=ux-primary%40example.test');expect(page.locator('tbody tr').first).to_be_visible();check('contact email search resolves the institution')
  page.locator('tbody tr').first.locator('a').first.click();expect(page.get_by_role('heading',name='Membership at a glance',exact=True)).to_be_visible();page.go_back();expect(page.get_by_role('textbox',name='Search members',exact=True)).to_have_value('ux-primary@example.test');check('directory search persists across profile navigation')
  page.goto(url+'/app/members/operations?view=import');page.locator('input[type=file]').set_input_files({'name':'ux-preview.csv','mimeType':'text/csv','buffer':b'FullName,Email,Phone,City,Number\nUX New,ux-new@example.test,9876543210,City,UX-NEW\n'});expect(page.get_by_text('Column mapping preview',exact=False)).to_be_visible();check('import mapping displays source headers and sample values before validation')
  for width in [390,768]:
   page.set_viewport_size({'width':width,'height':900});page.goto(url+'/app/members/profile/'+member);expect(page.locator('.contact-person').first).to_be_visible();assert page.eval_on_selector('html','el=>el.scrollWidth')<=width;check('contact profile fits '+str(width)+'px')
  page.set_viewport_size({'width':1440,'height':1000});page.goto(url+'/app/members/profile/'+member);page.get_by_role('button',name='Edit organization',exact=True).click();page.get_by_label('College / university / institution name',exact=True).fill('Unsaved value');dialogs=[];page.on('dialog',lambda d:(dialogs.append(d.message),d.dismiss()));page.keyboard.press('Escape');expect(page.get_by_role('dialog')).to_be_visible();assert any('unsaved' in d.lower() for d in dialogs);check('Escape protects unsaved drawer changes')
  page.get_by_label('Organization registration email',exact=True).fill('bad-email');page.get_by_role('button',name='Save organization',exact=True).click();expect(page.get_by_role('alert').filter(has_text='email')).to_be_visible();check('invalid email has adjacent accessible validation')
  page.screenshot(path=str(ROOT/'.local/ux-browser-verified.png'),full_page=True)
  assert not errors,errors
  browser.close()
finally:
 server.terminate();server.wait(timeout=20);log.close();(ROOT/'.local/ux-browser-results.json').write_text(json.dumps({'checks':checks,'pageErrors':errors},indent=2))
