"""Real local API/browser checks for the 2027 experience; no provider emails sent."""
import json,os,sys
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
ROOT=Path(__file__).resolve().parents[1]
URL=os.environ.get('EXPERIENCE_URL','http://127.0.0.1:5176')
checks=[];errors=[]
def check(name,value=True):
 assert value,name
 checks.append(name);print('PASS',name,flush=True)
def login(context,email,password):
 token=context.request.get(URL+'/api/v1/auth/csrf').json()['token']
 r=context.request.post(URL+'/api/v1/auth/login',data={'email':email,'password':password},headers={'X-CSRF-TOKEN':token})
 assert r.ok,(r.status,r.text())
with sync_playwright() as pw:
 browser=pw.chromium.launch();context=browser.new_context(viewport={'width':1440,'height':1050});page=context.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
 page.goto(URL+'/');expect(page.get_by_role('button',name='Sign in',exact=True)).to_be_visible();check('Root URL renders sign-in directly')
 check('No separate landing page',page.locator('.landing').count()==0)
 check('One create-account invitation',page.get_by_role('link',name='Create an account',exact=True).count()==1)
 page.screenshot(path=str(ROOT/'tests/experience-login-desktop.png'),full_page=True)
 page.goto(URL+'/login?returnTo=%2Fapp%2Faccount');expect(page).to_have_url(URL+'/?returnTo=%2Fapp%2Faccount');check('Legacy login redirects to root and retains return destination')
 for width in [360,390,768,1280]:
  page.set_viewport_size({'width':width,'height':900});page.goto(URL+'/');expect(page.get_by_role('button',name='Sign in',exact=True)).to_be_visible()
  check('Login fits '+str(width)+'px',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'))
  if width==390:page.screenshot(path=str(ROOT/'tests/experience-login-mobile.png'),full_page=True)
 page.get_by_label('Language',exact=True).select_option('hi');expect(page.locator('html')).to_have_attribute('lang','hi');check('Hindi labels available')
 page.get_by_label('Language',exact=True).select_option('en')
 page.goto(URL+'/signup');expect(page.get_by_role('button',name='Create account',exact=True)).to_be_visible();check('Signup preserved')
 page.goto(URL+'/forgot');expect(page.get_by_role('button',name='Send recovery email')).to_be_visible();check('Recovery preserved')
 if URL.startswith('https:'):
  cfg=json.loads((ROOT/'.local/production-owner-login.json').read_text());email=cfg['email'];password=cfg['initialPassword']
 else:
  cfg=json.loads((ROOT/'.local/preview-account.json').read_text());email=cfg['platformEmail'];password=cfg['platformPassword']
 login(context,email,password);page.set_viewport_size({'width':1440,'height':1050});page.goto(URL+'/app');expect(page.locator('.app-shell')).to_be_visible();expect(page.locator('.workspace-tools')).to_be_visible();check('Authenticated platform shell and workspace tools')
 page.screenshot(path=str(ROOT/'tests/experience-dashboard-desktop.png'),full_page=True)
 page.get_by_role('button',name='Save page to favorites').click();expect(page.get_by_role('button',name='Remove page from favorites')).to_be_visible();check('Favorite page can be saved')
 page.get_by_label('Display density').select_option('compact');expect(page.locator('html')).to_have_attribute('data-density','compact');check('Compact density persists')
 page.keyboard.press('Control+k');expect(page.get_by_role('dialog',name='Search your workspace')).to_be_visible();page.get_by_label('Search pages, records and actions').fill('security');expect(page.get_by_role('link',name='Account Account security & devices')).to_be_visible();check('Command palette finds authorized destinations')
 page.keyboard.press('Escape');expect(page.get_by_role('dialog',name='Search your workspace')).not_to_be_visible();check('Command palette closes with Escape')
 page.get_by_role('button',name='Open notification center').click();expect(page.get_by_role('dialog',name='Notification center')).to_be_visible();page.keyboard.press('Escape');check('Notification center accessible')
 page.set_viewport_size({'width':390,'height':844});expect(page.get_by_role('navigation',name='Mobile workspace navigation')).to_be_visible();check('Mobile bottom navigation visible')
 check('Dashboard fits mobile',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'));page.screenshot(path=str(ROOT/'tests/experience-dashboard-mobile.png'),full_page=True)
 page.get_by_role('navigation',name='Mobile workspace navigation').get_by_role('button',name='More').click();expect(page.get_by_role('dialog',name='Workspace pages')).to_be_visible();page.keyboard.press('Escape');check('Mobile More opens accessible page navigation')
 page.emulate_media(reduced_motion='reduce');check('Reduced-motion animations disabled',page.locator('.workspace-search-trigger').evaluate('(el)=>getComputedStyle(el).transitionDuration')=='0s')
 check('No uncaught browser errors',not errors)
 context.close();browser.close()
(ROOT/'tests/experience-browser-results.json').write_text(json.dumps({'checks':checks,'errors':errors},indent=2))
