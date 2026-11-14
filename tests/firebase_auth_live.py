"""Read-only live provider routing; never enters external account credentials."""
import json,time
from pathlib import Path
from urllib.parse import urlparse
from playwright.sync_api import sync_playwright,expect

ROOT=Path(__file__).resolve().parents[1];url='https://members.aimersociety.com';checks=[]
def check(name,value=True):
 assert value,name
 checks.append(name);print('PASS LIVE SSO',name,flush=True)
with sync_playwright() as p:
 browser=p.chromium.launch();ctx=browser.new_context(viewport={'width':390,'height':844});page=ctx.new_page();errors=[];violations=[]
 page.on('pageerror',lambda e:errors.append(str(e)))
 page.on('console',lambda m:violations.append(m.text) if 'Content Security Policy' in m.text and 'violates' in m.text else None)
 try:
  config=ctx.request.get(url+'/api/v1/auth/providers');check('live Firebase configuration enables both providers',config.ok and set(config.json()['providers'])=={'google.com','microsoft.com'} and config.json()['enabled'])
  page.goto(url+'/login');expect(page.get_by_label('Username or email address',exact=True)).to_be_visible()
  for provider,host in [('Google','accounts.google.com'),('Microsoft','login.microsoftonline.com')]:
   button=page.get_by_role('button',name=provider,exact=True);expect(button).to_be_enabled(timeout=30000)
   with page.expect_popup(timeout=30000) as popup_info:button.click()
   popup=popup_info.value
   popup.wait_for_url(lambda u: urlparse(u).hostname==host,timeout=45000)
   check(provider+' opens the real provider sign-in page',urlparse(popup.url).hostname==host)
   popup.close();expect(button).to_be_enabled(timeout=30000)
  for width in [360,390,768,1586]:
   page.set_viewport_size({'width':width,'height':900});check('live login fits '+str(width)+'px',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
  check('auth provider initialization produces no page errors',not errors)
  check('portal CSP permits Firebase sign-in initialization',not any(any(host in text.split('violates',1)[0] for host in ['apis.google.com','firebaseapp.com','identitytoolkit.googleapis.com','securetoken.googleapis.com']) for text in violations))
  page.screenshot(path=str(ROOT/'tests/firebase-login-live-mobile.png'),full_page=True)
 finally:
  browser.close();(ROOT/'tests/firebase-auth-live-results.json').write_text(json.dumps({'passed':len(checks),'checks':checks,'errors':errors,'cspViolations':violations,'externalConsent':'Provider login pages opened; no real user consent submitted'},indent=2),encoding='utf-8')
