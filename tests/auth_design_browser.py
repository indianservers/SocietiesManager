"""Reference-layout and real signup/recovery/MFA browser checks on assemblia_test."""
import base64,hashlib,hmac,json,os,secrets,shutil,struct,subprocess,time,urllib.parse,socket
from pathlib import Path
import psycopg
from psycopg.types.json import Jsonb
from playwright.sync_api import sync_playwright,expect
from api_runtime import API_DLL

ROOT=Path(__file__).resolve().parents[1]
with socket.socket() as probe:
 probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
url='http://127.0.0.1:'+str(port)
dev=json.loads((ROOT/'.local/dev.json').read_text());fixture=json.loads((ROOT/'.local/roster-browser-fixture.json').read_text());tenant=fixture['tenant']
dsn=dict(host='127.0.0.1',port=55439,dbname='assemblia_test',user='postgres',password=dev['password'])
landing=json.loads((ROOT/'scripts/aimer-auth-landing.json').read_text(encoding='utf-8'))
with psycopg.connect(**dsn) as db:
 profile=db.execute('SELECT "Profile" FROM "Tenants" WHERE "Id"=%s',(tenant,)).fetchone()[0]
 if isinstance(profile,str):profile=json.loads(profile)
 profile['authLanding']=landing
 db.execute('UPDATE "Tenants" SET "Profile"=%s WHERE "Id"=%s',(Jsonb(profile),tenant))
 db.execute('UPDATE "TenantBrand" SET "DisplayName"=\'AIMER Society\' WHERE "TenantId"=%s',(tenant,))
shutil.copytree(Path(os.environ.get('AUTH_DESIGN_FRONTEND_DIST',str(ROOT/'frontend/dist'))),API_DLL.parent/'wwwroot',dirs_exist_ok=True)
env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=assemblia_test;Username=postgres;Password={dev['password']}",'PublicUrl':url,'Society__DefaultTenantId':tenant,'Logging__LogLevel__Default':'Warning'}
log=(ROOT/'.local/auth-design-api.log').open('w',encoding='utf-8');server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',url],cwd=API_DLL.parent,env=env,stdout=log,stderr=log)
checks=[];errors=[]
def check(name,value=True):
 assert value,name
 checks.append(name);print('PASS AUTH',name,flush=True)
def call(ctx,path,body=None):
 token=ctx.request.get(url+'/api/v1/auth/csrf').json()['token']
 response=ctx.request.post(url+'/api/v1'+path,data=body or {},headers={'X-CSRF-TOKEN':token})
 assert response.ok,response.text()
 return response.json() if response.status!=204 else None
def totp(key):
 secret=base64.b32decode(key.upper()+'='*((8-len(key)%8)%8));digest=hmac.new(secret,struct.pack('>Q',int(time.time())//30),hashlib.sha1).digest();offset=digest[-1]&15
 return str((struct.unpack('>I',digest[offset:offset+4])[0]&0x7fffffff)%1000000).zfill(6)
def outbox(email,subject):
 with psycopg.connect(**dsn) as db:return db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=%s ORDER BY "CreatedAt" DESC LIMIT 1',(email,subject)).fetchone()[0]
try:
 with sync_playwright() as p:
  browser=p.chromium.launch();ctx=browser.new_context(viewport={'width':1586,'height':992});page=ctx.new_page();page.on('pageerror',lambda error:errors.append(str(error)))
  for _ in range(100):
   try:
    if ctx.request.get(url+'/health/ready').ok:break
   except Exception:pass
   time.sleep(.2)
  page.goto(url+'/signup');page.wait_for_load_state('networkidle')
  expect(page.get_by_role('heading',name='Join the AIMER community',exact=True)).to_be_visible()
  logo=page.locator('.community-brand img')
  check('header uses a loaded PNG logo',logo.get_attribute('src').endswith('.png') and logo.evaluate('img=>img.complete&&img.naturalWidth>0'))
  check('hero artwork is a loaded PNG',page.locator('.community-artwork img').evaluate('img=>img.complete&&img.naturalWidth>0'))
  check('reference heading, navigation, banner and four benefits are present',page.locator('.community-header nav a').count()==4 and page.locator('.community-features section').count()==4 and 'Artificial Intelligence Medical and Engineering Researchers Society' in page.locator('.community-society-band').inner_text())
  check('desktop has two columns matching the supplied reference',page.locator('.community-story').bounding_box()['x']<page.locator('.community-auth-card').bounding_box()['x'])
  check('all three membership options and owner-configured prices remain available',page.locator('select[name=membershipKind] option').count()==3 and '₹500 / year' in page.locator('#signup-fees').inner_text() and '₹10,000 / year' in page.locator('#signup-fees').inner_text())
  for kind in ['Individual','Institutional','Corporate']:
   page.locator('select[name=membershipKind]').select_option(kind);expect(page.locator('select[name=membershipKind]')).to_have_value(kind)
  check('Individual, Institutional and Corporate are selectable')
  page.screenshot(path=str(ROOT/'tests/auth-reference-signup-desktop.png'),full_page=True)
  page.get_by_role('link',name='Login',exact=True).click();expect(page.get_by_role('heading',name='Welcome to the AIMER community',exact=True)).to_be_visible()
  page.screenshot(path=str(ROOT/'tests/auth-reference-login-desktop.png'),full_page=True)
  check('Login and Sign Up tabs switch functional forms')
  page.evaluate("document.documentElement.dataset.theme='dark'")
  check('saved dark-theme preference does not obscure the reference login controls',page.locator('.community-password button').evaluate("element=>getComputedStyle(element).backgroundColor==='rgba(0, 0, 0, 0)'"))
  page.evaluate("document.documentElement.dataset.theme='light'")
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   for path in ['/login','/signup']:
    page.goto(url+path);page.wait_for_load_state('networkidle')
    if page.evaluate('document.documentElement.scrollWidth>innerWidth'):
     page.screenshot(path=str(ROOT/'tests/auth-overflow-debug.png'),full_page=True)
     print(page.evaluate("[...document.querySelectorAll('body *')].filter(e=>e.getBoundingClientRect().right>innerWidth+1).map(e=>({tag:e.tagName,cls:e.className,w:e.getBoundingClientRect().width,right:e.getBoundingClientRect().right})).slice(0,16)"),flush=True)
    check(path+' fits '+str(width)+'px without horizontal overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
  page.set_viewport_size({'width':390,'height':844});page.screenshot(path=str(ROOT/'tests/auth-reference-signup-mobile.png'),full_page=True)
  page.get_by_role('button',name='Terms of Use',exact=True).click();expect(page.get_by_role('dialog')).to_be_visible();page.get_by_role('button',name='Close drawer',exact=True).click()
  page.get_by_role('button',name='Privacy Policy',exact=True).click();expect(page.get_by_role('dialog')).to_be_visible();page.get_by_role('button',name='Close drawer',exact=True).click()
  check('terms and privacy controls open accessible information dialogs')
  email='design-'+secrets.token_hex(5)+'@example.test';password=secrets.token_urlsafe(20)+'!Aa1'
  page.get_by_label('Full name',exact=True).fill('Design Verification Member');page.get_by_label('Mobile number',exact=True).fill('9876543210');page.get_by_label('Email address',exact=True).fill(email);page.locator('select[name=membershipKind]').select_option('Institutional');page.get_by_label('Password',exact=True).fill(password)
  page.get_by_role('button',name='Show password',exact=True).click();expect(page.get_by_label('Password',exact=True)).to_have_attribute('type','text');page.get_by_role('button',name='Hide password',exact=True).click()
  check('password visibility toggle works without changing the password')
  page.locator('input[name=terms]').check()
  with page.expect_response(lambda r:r.request.method=='POST' and r.url.endswith('/auth/signup')) as response:page.get_by_role('button',name='Create account',exact=True).click()
  check('mobile signup submits successfully',response.value.ok)
  with psycopg.connect(**dsn) as db:
   row=db.execute('SELECT "PhoneNumber","PreferredMembershipKind","PlatformAdmin","PhoneNumberConfirmed" FROM "AspNetUsers" WHERE "Email"=%s',(email,)).fetchone()
  check('signup preserves membership choice and optional phone without granting authority or claiming phone verification',row==('+919876543210','Institutional',False,False))
  verification=urllib.parse.urlsplit(outbox(email,'Verify your email'));page.goto(url+verification.path+'?'+verification.query);page.get_by_role('button',name='Verify email',exact=True).click();expect(page.get_by_role('status')).to_contain_text('Email verified')
  check('email verification remains functional')
  page.goto(url+'/login');page.get_by_label('Email address',exact=True).fill(email);page.get_by_label('Password',exact=True).fill(password);page.get_by_role('button',name='Login',exact=True).click();page.wait_for_url('**/app/membership')
  check('email/password login opens the member workspace')
  call(ctx,'/auth/logout');page.goto(url+'/forgot');page.get_by_label('Email address',exact=True).fill(email);page.get_by_role('button',name='Send recovery email',exact=True).click();expect(page.get_by_role('status')).to_be_visible()
  reset=urllib.parse.urlsplit(outbox(email,'Reset your password'));new_password=secrets.token_urlsafe(20)+'!Aa1';page.goto(url+reset.path+'?'+reset.query);page.get_by_label('New password',exact=True).fill(new_password);page.get_by_role('button',name='Reset password',exact=True).click();expect(page.get_by_role('status')).to_contain_text('Password reset')
  check('forgot-password and reset-password workflows remain functional')
  call(ctx,'/auth/login',{'email':email,'password':new_password});key=call(ctx,'/security/mfa/setup',{'password':new_password})['key'];recovery=call(ctx,'/security/mfa/enable',{'code':totp(key)})['recoveryCodes'];call(ctx,'/auth/logout')
  page.goto(url+'/login');page.get_by_label('Email address',exact=True).fill(email);page.get_by_label('Password',exact=True).fill(new_password);page.get_by_role('button',name='Login',exact=True).click();expect(page.get_by_label('Authenticator or recovery code',exact=True)).to_be_visible();page.get_by_label('Authenticator or recovery code',exact=True).fill(totp(key));page.get_by_role('button',name='Verify & sign in',exact=True).click();page.wait_for_url('**/app/membership')
  check('authenticator login still completes end to end')
  call(ctx,'/auth/logout');page.goto(url+'/login');page.get_by_label('Email address',exact=True).fill(email);page.get_by_label('Password',exact=True).fill(new_password);page.get_by_role('button',name='Login',exact=True).click();page.get_by_label('Authenticator or recovery code',exact=True).fill(recovery[0]);page.get_by_label('Use a single-use recovery code').check();page.get_by_role('button',name='Verify & sign in',exact=True).click();page.wait_for_url('**/app/membership')
  check('single-use recovery-code login still completes end to end')
  # A second society changes content through data, without a source change.
  ctx2=browser.new_context();other=ctx2.new_page();other.route('**/api/v1/branding/resolve*',lambda route:route.fulfill(json={'displayName':'Example Association','authLanding':{'communityName':'Example','fullName':'Example Association of Researchers'},'loginTitle':'Example login','chapterTerm':'Chapter'}));other.goto(url+'/signup');expect(other.get_by_role('heading',name='Join the Example community',exact=True)).to_be_visible()
  check('same page serves another organization through configurable branding',other.locator('.community-brand').inner_text().startswith('Example Association'))
  check('no uncaught browser errors',not errors)
  browser.close()
finally:
 server.terminate();server.wait(timeout=20);log.close();(ROOT/'tests/auth-design-browser-results.json').write_text(json.dumps({'passed':len(checks),'checks':checks,'errors':errors},indent=2),encoding='utf-8')
