"""Read-only checks of the deployed reference layout and PNG branding."""
import json
from pathlib import Path
from playwright.sync_api import sync_playwright,expect

ROOT=Path(__file__).resolve().parents[1];url='https://members.aimersociety.com';checks=[];errors=[]
def check(name,condition=True):
 assert condition,name
 checks.append(name);print('PASS LIVE AUTH',name,flush=True)
with sync_playwright() as p:
 browser=p.chromium.launch()
 try:
  context=browser.new_context(viewport={'width':1586,'height':992});page=context.new_page();page.on('pageerror',lambda error:errors.append(str(error)))
  page.goto(url+'/signup');page.wait_for_load_state('networkidle');expect(page.get_by_role('heading',name='Join the AIMER community',exact=True)).to_be_visible()
  check('deployed signup uses the society-specific reference copy')
  image=page.locator('.community-brand img');source=image.get_attribute('src');response=context.request.get(url+source if source.startswith('/') else source)
  check('live logo is served as an actual PNG',response.ok and response.headers.get('content-type','').startswith('image/png') and response.body().startswith(b'\x89PNG\r\n\x1a\n'))
  check('live PNG logo and hero artwork finish loading',image.evaluate('e=>e.complete&&e.naturalWidth>0') and page.locator('.community-artwork img').evaluate('e=>e.complete&&e.naturalWidth>0'))
  check('live signup retains Individual, Institutional and Corporate options',page.locator('select[name=membershipKind] option').count()==3)
  check('configured country calling code is used',page.locator('select[name=countryCode]').input_value()=='+91')
  check('reference banner and feature strip are present',page.locator('.community-features section').count()==4 and 'Artificial Intelligence Medical and Engineering Researchers Society' in page.locator('.community-society-band').inner_text())
  page.screenshot(path=str(ROOT/'tests/auth-live-signup-desktop.png'),full_page=True)
  page.get_by_role('link',name='Login',exact=True).click();expect(page.get_by_role('button',name='Login',exact=True)).to_be_visible()
  check('live login tab opens the real sign-in form',page.locator('input[name=email]').count()==1 and page.locator('input[name=password]').count()==1)
  page.screenshot(path=str(ROOT/'tests/auth-live-login-desktop.png'),full_page=True)
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   check('live login fits '+str(width)+'px',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
  check('no uncaught runtime errors on the live auth pages',not errors)
 finally:
  browser.close();(ROOT/'tests/auth-design-live-results.json').write_text(json.dumps({'url':url,'passed':len(checks),'checks':checks,'errors':errors},indent=2),encoding='utf-8')
