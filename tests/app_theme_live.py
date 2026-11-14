"""Read-only HTTPS checks of shared theme and existing owner/member access."""
import json
from pathlib import Path
from playwright.sync_api import sync_playwright,expect

ROOT=Path(__file__).resolve().parents[1];url='https://members.aimersociety.com';checks=[];errors=[];failures=[]
credentials=json.loads((ROOT/'.local/production-owner-login.json').read_text())
def check(name,value=True):
 assert value,name
 checks.append(name);print('PASS LIVE THEME',name,flush=True)
with sync_playwright() as p:
 browser=p.chromium.launch()
 try:
  context=browser.new_context(viewport={'width':1440,'height':1000});page=context.new_page();page.on('pageerror',lambda error:errors.append(str(error)));page.on('response',lambda response:failures.append(response.url.split('?')[0]) if response.status>=500 and response.url.startswith(url) else None)
  ready=context.request.get(url+'/health/ready');check('production remains ready over HTTPS',ready.ok and ready.json()['ready'])
  theme=context.request.get(url+'/society-theme.css');check('shared stylesheet is publicly available',theme.ok and '--society-primary: #7e12bf' in theme.text())
  page.goto(url+'/');page.wait_for_load_state('networkidle');check('public landing shares the new purple theme',page.locator('.landing .primary').evaluate("e=>getComputedStyle(e).backgroundImage.includes('140, 27, 197')"))
  token=context.request.get(url+'/api/v1/auth/csrf').json()['token']
  response=context.request.post(url+'/api/v1/auth/login',data={'email':credentials['email'],'password':credentials['initialPassword']},headers={'X-CSRF-TOKEN':token});assert response.ok,response.text()
  check('designated platform-owner login still works',not response.json().get('requiresTwoFactor'))
  page.goto(url+'/app');page.wait_for_load_state('networkidle');expect(page.locator('.app-shell')).to_be_visible()
  check('live dashboard uses the purple design tokens',page.locator('.app-shell').evaluate("e=>getComputedStyle(e).getPropertyValue('--society-primary').trim()==='#7e12bf'"))
  check('sidebar uses the existing PNG logo',page.locator('.sidebar .brand-mark').evaluate("e=>getComputedStyle(e).backgroundImage.includes('community-brain-chip.png')"))
  page.screenshot(path=str(ROOT/'tests/app-theme-live-dashboard.png'),full_page=True)
  routes=['/app','/app/membership','/app/members','/app/community','/app/communications','/app/finance','/app/studio','/app/account','/app/settings']
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   for route in routes:
    page.evaluate('path=>{history.pushState(null,"",path);dispatchEvent(new PopStateEvent("popstate"));}',route);page.wait_for_load_state('networkidle')
    if page.locator('.skeleton').count():page.locator('.skeleton').first.wait_for(state='hidden',timeout=15000)
    check(route+' fits live '+str(width)+'px',page.evaluate('document.documentElement.scrollWidth<=innerWidth+2'))
  page.set_viewport_size({'width':390,'height':844});page.goto(url+'/app');page.wait_for_load_state('networkidle');page.screenshot(path=str(ROOT/'tests/app-theme-live-mobile.png'),full_page=True)
  page.get_by_role('button',name='Toggle light or dark appearance').click();check('live dark mode uses the purple palette',page.locator('.app-shell').evaluate("e=>getComputedStyle(e).getPropertyValue('--society-primary').trim()==='#c88afa'"))
  page.screenshot(path=str(ROOT/'tests/app-theme-live-dark-mobile.png'),full_page=True)
  page.get_by_role('button',name='Toggle light or dark appearance').click()
  status=context.request.get(url+'/api/v1/security/status');check('MFA protection for owner operations remains enabled',status.ok and status.json()['highRiskMfaRequired'])
  check('no live runtime errors or failed server responses',not errors and not failures)
  token=context.request.get(url+'/api/v1/auth/csrf').json()['token'];context.request.post(url+'/api/v1/auth/logout',headers={'X-CSRF-TOKEN':token});check('logout still revokes the owner session',context.request.get(url+'/api/v1/users/me').status==401)
 finally:
  browser.close();(ROOT/'tests/app-theme-live-results.json').write_bytes(json.dumps({'url':url,'passed':len(checks),'checks':checks,'pageErrors':errors,'serverFailures':failures},indent=2).encode('utf-8'))
