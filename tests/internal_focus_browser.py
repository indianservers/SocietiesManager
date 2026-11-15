"""Read-only browser checks: unsaved internal forms survive tab changes and refresh failures."""
import json,os,time
from pathlib import Path
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parents[1];URL='https://members.aimersociety.com';LIVE=os.getenv('INTERNAL_FOCUS_LIVE')=='1';DIST=ROOT/os.getenv('INTERNAL_FOCUS_DIST','frontend/dist');checks=[];errors=[]
cases=[
 ('individual member','/app/members?view=directory&memberKind=Individual&create=member',None),
 ('institutional member','/app/members?view=directory&memberKind=Institutional','Add organization'),
 ('corporate member','/app/members?view=directory&memberKind=Corporate','Add organization'),
 ('chapter','/app/chapters','Create chapter'),('activity','/app/community','Share an activity'),
 ('invoice','/app/finance','Create invoice'),('template studio','/app/studio','Create a template'),
 ('message template','/app/communications/content','New template'),('account profile','/app/account',None),
 ('settings','/app/settings',None),('service','/app/service',None),('intelligence','/app/intelligence',None),('resources','/app/resources',None),('clubs','/app/clubs',None),('membership portal','/app/membership',None),('organization','/app/organization',None),('regions','/app/regions',None),('dashboard','/app',None),('welcome emails','/app/communications/welcome',None),('platform','/app/platform',None)]
progress=ROOT/('tests/internal-focus-live-progress.json' if LIVE else 'tests/internal-focus-progress.json')
if os.getenv('FOCUS_RESUME')=='1' and progress.exists():checks=json.loads(progress.read_text())
with sync_playwright() as p:
 browser=p.chromium.launch();ctx=browser.new_context(viewport={'width':1440,'height':1000});page=ctx.new_page();page.on('pageerror',lambda e:errors.append(str(e)));page.on('dialog',lambda d:d.accept());sessionRequests=[];page.on('request',lambda req:sessionRequests.append(req.url) if req.url==URL+'/api/v1/users/me' else None);fail_setup=False;failed_setup=[]
 def visit(address):
  if page.get_by_role('button',name='Close drawer',exact=True).count():
   page.get_by_role('button',name='Close drawer',exact=True).last.click()
  if address.startswith(URL+'/app') and page.locator('.app-shell').count():
   page.evaluate("(url)=>{history.pushState(null,'',url);window.dispatchEvent(new PopStateEvent('popstate'))}",address)
   return
  for attempt in range(3):
   try:return page.goto(address,wait_until='domcontentloaded')
   except Exception as exc:
    if attempt==2 or not any(e in str(exc) for e in ['ERR_NETWORK_CHANGED','ERR_CONNECTION_RESET','ERR_ABORTED']):raise
    page.wait_for_timeout(600)
 def serve(route):
  path=route.request.url.split(URL,1)[-1].split('?',1)[0]
  if fail_setup and '/platform/membership/' in path and path.endswith('/setup'):
   failed_setup.append(path);route.fulfill(status=503,content_type='application/json',body='{"title":"Temporary test outage"}');return
  f=DIST/path.lstrip('/')
  if path=='/' or path.startswith('/app'):f=DIST/'index.html'
  if not LIVE and f.is_file():route.fulfill(path=str(f))
  else:route.continue_()
 ctx.route(URL+'/**',serve);visit(URL);page.locator('input[name=email]').wait_for();cfg=json.loads((ROOT/'.local/production-owner-login.json').read_text())
 status=page.evaluate('''async(v)=>{const t=await fetch('/api/v1/auth/csrf').then(r=>r.json());return (await fetch('/api/v1/auth/login',{method:'POST',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':t.token},body:JSON.stringify(v)})).status}''',{'email':cfg['email'],'password':cfg['initialPassword']});assert status==200,status
 for name,path,button in ([] if os.getenv('FOCUS_ONLY_BACKGROUND')=='1' else cases):
  if any(c.get('page')==name for c in checks):continue
  visit(URL+path);page.locator('.app-shell main').wait_for();page.wait_for_timeout(900)
  if button:
   target=page.get_by_role('button',name=button,exact=True)
   target.wait_for(state='visible',timeout=30000)
   target.click();page.locator('[role=dialog]').wait_for();page.wait_for_timeout(1000)
  if page.locator('[role=dialog]').count():page.locator('[role=dialog] input:not([type=hidden]), [role=dialog] textarea').first.wait_for(timeout=20000)
  if name=='account profile':page.get_by_role('button',name='Save profile',exact=True).wait_for(timeout=30000)
  scope=page.locator('[role=dialog]').last if page.locator('[role=dialog]').count() else page.locator('.app-shell main')
  fields=scope.locator('input:not([readonly]):not([disabled]):is([type=text],[type=email],[type=tel],[type=url],:not([type])),textarea:not([readonly]):not([disabled])')
  filled=0
  for i in range(fields.count()):
   field=fields.nth(i)
   if not field.is_visible():continue
   value='unsaved-focus-check@example.com' if field.get_attribute('type')=='email' else 'Unsaved tab-switch check'
   field.fill(value);field.evaluate("e=>e.dataset.focusTest='true'");filled+=1
   if filled>=2:break
  if (button or name=='individual member' or name=='account profile') and not filled:
   fields.first.wait_for(timeout=15000)
   fields.first.fill('Unsaved tab-switch check');fields.first.evaluate("e=>e.dataset.focusTest='true'");filled=1
  for editor in scope.locator('[contenteditable=true]').all():
   if editor.is_visible():editor.fill('Unsaved rich-text draft');filled+=1
  page.wait_for_timeout(1200)
  sessionBefore=len(sessionRequests)
  page.evaluate('''()=>{window.beforeDocument=document;window.beforeMain=document.querySelector('main');window.beforeForm=document.querySelector('[role=dialog] form')||document.querySelector('main form');window.beforeRich=[...document.querySelectorAll('[contenteditable=true]')].map(e=>({node:e,text:e.innerHTML}));window.beforeFields=[...document.querySelectorAll('[data-focus-test=true]')].filter(e=>e.getClientRects().length).map(e=>({node:e,value:e.value,checked:e.checked}));const oldNow=Date.now;Date.now=()=>oldNow()+31000}''')
  for repeat in range(3):
   page.evaluate('''()=>{Object.defineProperty(document,'visibilityState',{configurable:true,value:'hidden'});window.dispatchEvent(new Event('visibilitychange'));window.dispatchEvent(new Event('blur'))}''');page.wait_for_timeout(80)
   page.evaluate('''()=>{Object.defineProperty(document,'visibilityState',{configurable:true,value:'visible'});window.dispatchEvent(new Event('visibilitychange'));window.dispatchEvent(new Event('focus'));window.dispatchEvent(new Event('online'))}''');page.wait_for_timeout(700)
   state=page.evaluate('''()=>({document:document===beforeDocument,main:document.querySelector('main')===beforeMain,form:!beforeForm||beforeForm.isConnected,rich:beforeRich.every(e=>e.node.isConnected&&e.node.innerHTML===e.text),fields:beforeFields.every(f=>f.node.isConnected&&f.node.value===f.value&&f.node.checked===f.checked)})''')
   assert all(state.values()),(name,state)
  assert len(sessionRequests)==sessionBefore,(name,'Unexpected focus-triggered session request')
  checks.append({'page':name,'url':path,'tabReturns':3,'editedFields':filled,'preserved':True});print('PASS',name,flush=True)
  (ROOT/('tests/internal-focus-live-progress.json' if LIVE else 'tests/internal-focus-progress.json')).write_text(json.dumps(checks,indent=2))
 # Force the member editor's normal 30-second poll to fail after typing.
 page.clock.install();visit(URL+cases[0][1]);page.get_by_role('dialog',name='Manually create membership').wait_for();field=page.locator('[role=dialog] input[name=fullName]');field.wait_for();field.fill('Unsaved through temporary outage');page.wait_for_timeout(600)
 page.evaluate('window.editorBeforeFailure=document.querySelector("[role=dialog] form")');fail_setup=True;page.clock.fast_forward(31000);page.wait_for_timeout(1500)
 assert failed_setup,'Expected the setup polling request to fail'
 assert field.input_value()=='Unsaved through temporary outage';assert page.evaluate('editorBeforeFailure===document.querySelector("[role=dialog] form")');assert page.get_by_role('button',name='Retry updates',exact=True).count()>0
 checks.append({'background503RetainsEditor':True});print('PASS temporary background outage preserves editor',flush=True)
 fail_setup=False;page.clock.resume()
 # Mobile shell and unsaved account fields.
 visit(URL+'/app/account');page.set_viewport_size({'width':390,'height':844});page.locator('.app-shell main form').first.wait_for();field=page.locator('main form input:not([readonly])').first;field.fill('Mobile unsaved input');page.evaluate("Object.defineProperty(document,'visibilityState',{configurable:true,value:'hidden'});window.dispatchEvent(new Event('visibilitychange'))");page.evaluate("Object.defineProperty(document,'visibilityState',{configurable:true,value:'visible'});window.dispatchEvent(new Event('visibilitychange'))");page.wait_for_timeout(800);assert field.input_value()=='Mobile unsaved input';checks.append({'mobileAccountPreserved':True})
 page.evaluate('''async()=>{const t=await fetch('/api/v1/auth/csrf').then(r=>r.json());await fetch('/api/v1/auth/logout',{method:'POST',headers:{'X-CSRF-TOKEN':t.token}})}''');assert not errors,errors;browser.close()
out=ROOT/('tests/internal-focus-live-results.json' if LIVE else 'tests/internal-focus-results.json');out.write_text(json.dumps({'checks':checks,'errors':errors,'recordsSaved':0},indent=2));print('PASS',len(checks),'checks, no records saved')
