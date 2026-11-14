"""Read-only HTTPS/browser acceptance checks; creates no production member data."""
import argparse,json,re,socket,ssl
from pathlib import Path
from playwright.sync_api import sync_playwright,expect

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--url',default='https://members.aimersociety.com')
parser.add_argument('--owner-file',type=Path,default=ROOT/'.local/production-owner-login.json')
parser.add_argument('--origin',default='164.92.119.35')
args=parser.parse_args();url=args.url.rstrip('/');credentials=json.loads(args.owner_file.read_text())
checks=[];errors=[];failures=[]
def check(name,value=True):assert value,name;checks.append(name);print('PASS PRODUCTION',name,flush=True)

# Validate the origin's certificate with public trust and SNI, without disabling
# certificate checks or changing DNS/firewall settings.
hostname=url.split('://',1)[1].split('/')[0]
with socket.create_connection((args.origin,443),timeout=20) as raw:
 with ssl.create_default_context().wrap_socket(raw,server_hostname=hostname) as tls:
  certificate=tls.getpeercert()
  check('origin certificate is publicly trusted and covers the launch hostname',('DNS',hostname) in certificate['subjectAltName'])
with sync_playwright() as p:
 browser=p.chromium.launch()
 try:
  context=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True)
  page=context.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
  page.on('response',lambda r:failures.append({'status':r.status,'url':r.url.split('?')[0]}) if r.status>=500 and r.url.startswith(url) else None)
  readiness=context.request.get(url+'/health/ready')
  check('public HTTPS readiness reports a migrated connected database',readiness.ok and readiness.json()['ready'])
  brand=context.request.get(url+'/api/v1/branding/resolve').json();tenant=brand['tenantId']
  check('public hostname resolves to the configured society',brand['displayName']=='AIMER Society')
  catalog=context.request.get(url+'/api/v1/membership/catalog?tenant='+tenant).json()
  check('production publishes the current chapter catalog',len(catalog['chapters'])>0 and len({c['id'] for c in catalog['chapters']})==len(catalog['chapters']))
  check('production exposes only the three configured launch membership plans',len(catalog['categories'])==3)
  check('live annual fees are INR 500 and INR 10000', {c['kind']:c['offer']['price'] for c in catalog['categories']}=={'Individual':500,'Institutional':10000,'Corporate':10000})
  check('anonymous visitors cannot inspect private account data',context.request.get(url+'/api/v1/users/me').status==401)
  check('anonymous visitors cannot read the member resource library',context.request.get(url+'/api/v1/societies/'+tenant+'/membership/resources').status==401)
  check('anonymous visitors cannot read institutional member IDs',context.request.get(url+'/api/v1/membership/affiliations').status==401)
  page.goto(url+'/signup');page.wait_for_load_state('networkidle')
  expect(page.locator('select[name=membershipKind] option')).to_have_count(3)
  check('browser tab title uses the configured society name','AIMER Society' in page.title())
  check('live mobile signup shows all three membership choices')
  expect(page.locator('body')).to_contain_text('₹500 / year')
  check('live signup displays configured annual pricing')
  for kind in ['Individual','Institutional','Corporate']:
   page.locator('select[name=membershipKind]').select_option(kind)
   expect(page.locator('select[name=membershipKind]')).to_have_value(kind)
  check('all live signup options are selectable without submitting fake production accounts')
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   check(str(width)+'px public signup fits the viewport',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
  page.set_viewport_size({'width':390,'height':844})
  page.screenshot(path=str(ROOT/'tests/production-members-signup-mobile.png'),full_page=True)
  csrf=context.request.get(url+'/api/v1/auth/csrf').json()['token']
  signed=context.request.post(url+'/api/v1/auth/login',data={'email':credentials['email'],'password':credentials['initialPassword']},headers={'X-CSRF-TOKEN':csrf})
  check('designated platform owner can sign in over production HTTPS',signed.ok)
  me=context.request.get(url+'/api/v1/users/me').json()
  check('designated email owns the platform account',me['email']==credentials['email'] and me['platformAdmin'])
  cookies=context.cookies();session=next(c for c in cookies if c['name']=='society.session')
  check('production session cookie is Secure HttpOnly and SameSite Strict',session['secure'] and session['httpOnly'] and session['sameSite']=='Strict')
  workspaces=context.request.get(url+'/api/v1/workspaces').json()
  check('owner has an explicit society administrator workspace',any(w['id']==tenant and any(s['name']=='Society Administrator' for s in w['scopes']) for w in workspaces))
  portfolio=context.request.get(url+'/api/v1/membership/portfolio')
  check('owner can read the current membership portfolio',portfolio.ok and isinstance(portfolio.json(),list))
  affiliations=context.request.get(url+'/api/v1/membership/affiliations')
  check('live institution affiliations remain available',affiliations.ok and isinstance(affiliations.json(),list))
  security=context.request.get(url+'/api/v1/security/status')
  check('production enforces recent MFA for protected owner operations',security.ok and security.json()['highRiskMfaRequired'])
  check('pricing changes remain inaccessible until owner MFA is enrolled',context.request.get(url+'/api/v1/platform/membership-pricing/'+tenant).status==403)
  page.goto(url+'/app/members/settings');page.wait_for_load_state('networkidle')
  page.get_by_role('button',name='Create category',exact=True).click()
  check('membership administrators can configure membership kinds without changing application code',page.get_by_role('dialog').locator('select[name=kind] option').count()==3)
  page.get_by_role('button',name='Close drawer',exact=True).click()
  page.goto(url+'/app/account?view=security');page.wait_for_load_state('networkidle')
  check('owner can open the authenticator setup workspace','Authenticator' in page.locator('main').inner_text())
  page.screenshot(path=str(ROOT/'tests/production-owner-security-mobile.png'),full_page=True)
  check('live browser has no uncaught runtime errors or failed server responses',not errors and not failures)
  csrf=context.request.get(url+'/api/v1/auth/csrf').json()['token']
  context.request.post(url+'/api/v1/auth/logout',headers={'X-CSRF-TOKEN':csrf})
  check('owner session is revoked after logout',context.request.get(url+'/api/v1/users/me').status==401)
 finally:
  browser.close()
  (ROOT/'tests/production-launch-results.json').write_text(json.dumps({'url':url,'passed':len(checks),'checks':checks,'pageErrors':errors,'serverFailures':failures},indent=2),encoding='utf-8')
