import json,time,os
from pathlib import Path
from playwright.sync_api import sync_playwright
r=Path.cwd();dist=r/os.environ.get('FOCUS_DIST','frontend/dist');url='https://members.aimersociety.com';checks=[]
with sync_playwright() as p:
 b=p.chromium.launch();c=b.new_context();page=c.new_page();errors=[];page.on('pageerror',lambda e:errors.append(str(e)))
 calls=[]
 def serve(route):
  path=route.request.url.split(url,1)[-1].split('?',1)[0]
  if path=='/api/v1/users/me':
   calls.append(time.time());time.sleep(.5);route.fulfill(status=401,content_type='application/json',body='{"message":"Not signed in"}');return
  f=dist/path.lstrip('/')
  if path in ['/','/signup','/forgot']:f=dist/'index.html'
  if f.is_file():route.fulfill(path=str(f))
  else:route.continue_()
 c.route(url+'/**',serve)
 for path in ['/','/signup','/forgot']:
  page.goto(url+path);page.locator('input[name=email]').wait_for();page.locator('input[name=email]').fill('focus-check@example.com')
  if path!='/forgot':page.locator('input[name=password]').fill('Unsubmitted-test-only-849!')
  page.evaluate('''()=>{window.authForm=document.querySelector('.community-account-form');window.authDocument=document;window.remounts=0;window.formObserver=new MutationObserver(()=>{if(!window.authForm.isConnected)window.remounts++});window.formObserver.observe(document.body,{childList:true,subtree:true})}''')
  page.wait_for_timeout(1500)
  page.evaluate('''()=>{const previousNow=Date.now;Date.now=()=>previousNow()+31000}''')
  before=len(calls)
  for n in range(3):
   page.evaluate('''()=>{Object.defineProperty(document,'visibilityState',{configurable:true,value:'hidden'});window.dispatchEvent(new Event('visibilitychange'))}''');page.wait_for_timeout(100)
   page.evaluate('''()=>{Object.defineProperty(document,'visibilityState',{configurable:true,value:'visible'});window.dispatchEvent(new Event('visibilitychange'));window.dispatchEvent(new Event('focus'))}''');page.wait_for_timeout(900)
   assert page.locator('input[name=email]').input_value()=='focus-check@example.com'
   if path!='/forgot':assert page.locator('input[name=password]').input_value()=='Unsubmitted-test-only-849!'
   assert page.evaluate('authForm===document.querySelector(".community-account-form")&&authDocument===document&&remounts===0')
  assert len(calls)>before,'Session check must still run on focus'
  checks.append({'path':path,'switches':3,'sessionChecks':len(calls)-before,'preservedForm':True})
 assert not errors,errors;b.close()
(r/'tests/login-focus-results.json').write_text(json.dumps({'checks':checks,'errors':errors},indent=2));print(json.dumps(checks))
