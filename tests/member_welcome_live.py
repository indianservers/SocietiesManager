"""Read-only live verification of welcome page; never sends member emails."""
import json
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
ROOT=Path(__file__).resolve().parents[1];URL='https://members.aimersociety.com';checks=[];errors=[]
def check(n,v=True):
 assert v,n
 checks.append(n);print('PASS',n,flush=True)
with sync_playwright() as pw:
 browser=pw.chromium.launch();ctx=browser.new_context(viewport={'width':1440,'height':1000});branding=ctx.request.get(URL+'/api/v1/branding/resolve').json();tenant=branding['tenantId'];base='/api/v1/societies/'+tenant+'/member-welcome';check('Live anonymous access denied',ctx.request.get(URL+base+'/templates').status==401)
 cfg=json.loads((ROOT/'.local/production-owner-login.json').read_text());token=ctx.request.get(URL+'/api/v1/auth/csrf').json()['token'];check('Live platform login',ctx.request.post(URL+'/api/v1/auth/login',data={'email':cfg['email'],'password':cfg['initialPassword']},headers={'X-CSRF-TOKEN':token}).ok)
 r=ctx.request.get(URL+base+'/templates');check('Six professional templates available on live server',r.ok and len(r.json())==6);check('Email data responses are private',r.headers.get('cache-control')=='no-store');defaults=ctx.request.get(URL+base+'/defaults');check('Live per-type defaults available',defaults.ok and set(defaults.json())=={'Individual','Institutional'})
 page=ctx.new_page();page.on('pageerror',lambda e:errors.append(str(e)));page.goto(URL+'/app/communications/welcome');expect(page.get_by_role('heading',name='Welcome new members')).to_be_visible();check('Live welcome page renders');expect(page.get_by_role('button',name='Set selected template as default')).to_be_visible();check('Live default-template control available');expect(page.get_by_role('link',name='Welcome members').first).to_be_visible();check('Platform admin navigation includes welcome page')
 portfolio=ctx.request.get(URL+'/api/v1/membership/portfolio').json();owned={m['id'] for m in portfolio}
 for kind in ['Individual','Institutional']:
  page.get_by_label('Membership type',exact=True).select_option(kind);rows=ctx.request.get(URL+base+'/members?kind='+kind).json();check(kind+' live register accessible',isinstance(rows,list))
  if rows:
   selfstatus=ctx.request.get(URL+base+'/self/'+rows[0]['id']);check(kind+' self email request is restricted to owned memberships',selfstatus.ok if rows[0]['id'] in owned else selfstatus.status==403)
   page.locator('.welcome-members button').first.click();expect(page.locator('iframe')).to_be_visible();expect(page.frame_locator('iframe').get_by_text('YOUR MEMBERSHIP ID',exact=True)).to_be_visible();check(kind+' live ID email preview renders under production security policy');expect(page.frame_locator('iframe').get_by_text('Valid till',exact=False)).to_be_visible();check(kind+' email includes explicit Valid till');expect(page.get_by_role('button',name='Send welcome email',exact=True)).to_be_disabled()
 page.screenshot(path=str(ROOT/'.local/member-welcome-live-desktop.png'),full_page=True);page.set_viewport_size({'width':390,'height':844});check('Live mobile page has no horizontal overflow',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
 if page.locator('iframe').count():
  page.locator('iframe').scroll_into_view_if_needed();expect(page.frame_locator('iframe').get_by_text('YOUR MEMBERSHIP ID',exact=True)).to_be_visible()
 page.screenshot(path=str(ROOT/'.local/member-welcome-live-mobile.png'),full_page=True);check('No live browser errors',not errors);browser.close()
(ROOT/'tests/member-welcome-live-results.json').write_text(json.dumps({'url':URL,'checks':checks,'errors':errors,'memberEmailsSent':0},indent=2))
