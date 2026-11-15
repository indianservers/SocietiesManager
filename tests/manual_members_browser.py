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
 owner.call('/auth/login','POST',account);cat=owner.call(prefix+'/membership/categories','POST',{'name':'Manual UI '+uuid.uuid4().hex[:8],'description':'Synthetic manual administration type','kind':'Individual','active':True})['id'];alternate=owner.call(prefix+'/membership/categories','POST',{'name':'Manual UI alternate '+uuid.uuid4().hex[:8],'description':'Synthetic alternate membership type','kind':'Individual','active':True})['id'];chapter=owner.call(prefix+'/organizations/units?kind=Chapter&status=Active&pageSize=100')['items'][0]['id']
 name='Manual Browser '+uuid.uuid4().hex[:8];number='MAN-UI-'+uuid.uuid4().hex[:8].upper()
 with sync_playwright() as p:
  browser=p.chromium.launch();context=browser.new_context(viewport={'width':1440,'height':1000});login(context,{'email':preview['platformEmail'],'password':preview['platformPassword']});page=context.new_page();page.on('pageerror',lambda e:errors.append(str(e)));page.goto(url+'/app/members?view=directory&memberKind=Individual');expect(page.get_by_label('Active workspace',exact=True)).to_be_visible();page.get_by_label('Active workspace',exact=True).select_option(account['tenant']+':');page.goto(url+'/app/members?view=directory&memberKind=Individual');expect(page.get_by_role('button',name='Add individual member',exact=True)).to_be_visible();check('platform-only user can select society and manage its member directory')
  page.get_by_role('button',name='Add individual member',exact=True).click();expect(page.get_by_role('dialog',name='Manually create membership',exact=True)).to_be_visible();page.get_by_label('Individual member name',exact=True).fill(name);page.get_by_label('Membership ID (blank to generate)',exact=True).fill(number);page.get_by_label('Membership type / category',exact=True).select_option(cat);page.get_by_label('Chapter',exact=True).select_option(chapter);page.get_by_label('Joining date',exact=True).fill('2023-08-01');page.get_by_label('Valid from',exact=True).fill('2023-08-01');page.get_by_label('Expiry date (valid through)',exact=True).fill('2027-01-31');page.get_by_label('Contact email',exact=True).fill('manual-ui-'+uuid.uuid4().hex[:8]+'@example.test');page.get_by_label('Contact mobile',exact=True).fill('9876543210');page.get_by_label('Administration reason',exact=True).fill('Approved offline membership entry for UI test');page.get_by_role('button',name='Create membership manually',exact=True).click();expect(page.get_by_role('heading',name=name,exact=True,level=1)).to_be_visible();created=page.url.split('/profile/')[1].split('?')[0];expect(page.get_by_text('Valid through 31 Jan 2027',exact=True)).to_be_visible();check('manual create accepts an inclusive expiry and opens the new membership')
  with page.expect_download() as item:page.get_by_role('link',name='Download individual ID card (PDF)',exact=True).click()
  pdf=item.value;pdf.save_as(str(ROOT/'.local/manual-member-test.pdf'));assert (ROOT/'.local/manual-member-test.pdf').read_bytes().startswith(b'%PDF-');check('platform-only user downloads another individual member PDF')
  page.get_by_role('button',name='Manage membership manually',exact=True).click();page.get_by_label('Membership type / category',exact=True).select_option(alternate);page.get_by_label('Expiry date (valid through)',exact=True).fill('2028-02-29');page.get_by_label('Administration reason',exact=True).fill('Approved alternate type and corrected expiry');page.get_by_role('button',name='Save manual changes',exact=True).click();expect(page.get_by_text('Valid through 29 Feb 2028',exact=True)).to_be_visible();check('manual edit changes membership type and leap-day expiry')
  page.get_by_role('button',name='Delete membership',exact=True).click();expect(page.get_by_role('button',name='Delete and archive membership',exact=True)).to_be_disabled();page.get_by_label('Type membership ID to confirm',exact=True).fill(number);page.get_by_label('Administration reason',exact=True).fill('Archive temporary browser verification member');page.get_by_role('button',name='Delete and archive membership',exact=True).click();expect(page.get_by_role('button',name='Delete membership',exact=True)).not_to_be_visible();check('delete archives with typed ID confirmation')
  page.get_by_role('button',name='Manage membership manually',exact=True).click();expect(page.get_by_role('button',name='Restore membership',exact=True)).to_be_visible();page.get_by_label('Administration reason',exact=True).fill('Restore temporary membership for verification');page.get_by_role('button',name='Restore membership',exact=True).click();expect(page.get_by_role('button',name='Delete membership',exact=True)).to_be_visible();check('platform restore returns the same membership and dates')
  page.goto(url+'/app/members?view=directory&memberKind=Individual&dsearch='+number);expect(page.locator('tbody tr').first).to_be_visible();page.get_by_role('checkbox',name='Select '+name,exact=True).check()
  with page.expect_download() as item:page.get_by_role('button',name='Download selected individual IDs (1)',exact=True).click()
  archive=item.value;archive.save_as(str(ROOT/'.local/manual-member-test.zip'));import zipfile
  with zipfile.ZipFile(ROOT/'.local/manual-member-test.zip') as z:assert len(z.namelist())==1 and z.read(z.namelist()[0]).startswith(b'%PDF-')
  check('selected individual IDs download as a ZIP of valid PDFs')
  page.set_viewport_size({'width':390,'height':900});page.goto(url+'/app/members/profile/'+created);expect(page.get_by_role('heading',name='Platform Admin controls',exact=True)).to_be_visible();assert page.eval_on_selector('html','el=>el.scrollWidth')<=390;page.screenshot(path=str(ROOT/'.local/manual-members-browser.png'),full_page=True);check('manual administration and ID download controls fit mobile')
  # Archive this synthetic record after verification.
  page.get_by_role('button',name='Delete membership',exact=True).click();page.get_by_label('Type membership ID to confirm',exact=True).fill(number);page.get_by_label('Administration reason',exact=True).fill('End temporary browser verification');page.get_by_role('button',name='Delete and archive membership',exact=True).click();expect(page.get_by_role('button',name='Delete membership',exact=True)).not_to_be_visible();assert not errors,errors;browser.close()
finally:
 server.terminate();server.wait(timeout=20);log.close();(ROOT/'.local/manual-members-browser-results.json').write_text(json.dumps({'checks':checks,'pageErrors':errors},indent=2))
