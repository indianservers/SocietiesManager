"""Browser auth flows using the real frontend and test-only upstream provider stub."""
import shutil
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
# Shared isolated Identity host and test DB helpers; no production token bypass.
exec((Path(__file__).with_name('firebase_auth_integration.py')).read_text(encoding='utf-8').split('try:\n for _ in range(100):')[0])
shutil.copytree(ROOT/'.local/sso-source/frontend/dist',dll.parent/'wwwroot',dirs_exist_ok=True)
try:
 for _ in range(100):
  try:Client().call('/auth/csrf');break
  except Exception:time.sleep(.2)
 suffix=uuid.uuid4().hex[:12];email='browser-sso-'+suffix+'@example.test';username='browser_'+suffix;password=secrets.token_urlsafe(20)+'!Aa1'
 seed=Client();seed.call('/auth/signup','POST',{'email':email,'username':username,'password':password,'fullName':'Synthetic Browser Member'});confirm(email)
 user=str(sql('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(email,))[0][0])
 with sync_playwright() as p:
  browser=p.chromium.launch();ctx=browser.new_context();page=ctx.new_page();errors=[];page.on('pageerror',lambda e:errors.append(str(e)))
  ctx.route('**/assets/firebase-login-*.js',lambda route:route.fulfill(content_type='application/javascript',body="export async function prepare(){};export async function popup(provider){if(window.__cancelProvider)throw new Error('Sign-in was cancelled.');return JSON.stringify({...window.__testIdentity,Provider:provider});}"))
  def identity():page.evaluate('(v)=>window.__testIdentity=v',{'Uid':'browser-'+suffix,'Email':email,'Verified':True})
  def me():return ctx.request.get(url+'/api/v1/users/me')
  for width in [360,390,768,1586]:
   page.set_viewport_size({'width':width,'height':900});page.goto(url+'/login');expect(page.get_by_role('button',name='Google',exact=True)).to_be_enabled();expect(page.get_by_role('button',name='Microsoft',exact=True)).to_be_enabled()
   check('provider and password controls fit viewport '+str(width),page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
  identity();page.get_by_role('button',name='Google',exact=True).click();expect(page.get_by_role('heading',name='Confirm your existing account')).to_be_visible();check('Google matching email requires confirmation',me().status==401)
  page.get_by_label('Username or email address',exact=True).fill(username);page.get_by_label('Password',exact=True).fill(password);page.get_by_role('button',name='Confirm account',exact=True).click()
  expect(page.locator('.community-auth')).to_have_count(0);check('browser username confirmation links original account',me().json()['id']==user)
  for provider in ['Google','Microsoft']:
   csrf=ctx.request.get(url+'/api/v1/auth/csrf').json()['token'];ctx.request.post(url+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':csrf});page.goto(url+'/login');expect(page.get_by_role('button',name=provider,exact=True)).to_be_enabled();identity();page.get_by_role('button',name=provider,exact=True).click();expect(page.locator('.community-auth')).to_have_count(0);check(provider+' browser sign-in preserves linked account',me().json()['id']==user)
  csrf=ctx.request.get(url+'/api/v1/auth/csrf').json()['token'];ctx.request.post(url+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':csrf});page.goto(url+'/login');expect(page.get_by_role('button',name='Google',exact=True)).to_be_enabled();page.evaluate('window.__cancelProvider=true');page.get_by_role('button',name='Google',exact=True).click();expect(page.get_by_role('button',name='Login',exact=True)).to_be_enabled();page.get_by_label('Username or email address',exact=True).fill(email);page.get_by_label('Password',exact=True).fill(password);page.get_by_role('button',name='Login',exact=True).click();expect(page.locator('.community-auth')).to_have_count(0);check('cancelled popup leaves email/password login working',me().json()['id']==user)
  check('browser has no uncaught errors',not errors);browser.close()
 (ROOT/'tests/firebase-auth-browser-results.json').write_text(json.dumps({'passed':len(checks),'checks':checks},indent=2),encoding='utf-8')
finally:
 server.terminate();server.wait(timeout=15);log.close()
