import json
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
r=Path(__file__).resolve().parents[1];url='http://127.0.0.1:5202'
with sync_playwright() as p:
 b=p.chromium.launch();c=b.new_context(viewport={'width':390,'height':844});c.route('**/realtime/**',lambda route:route.fulfill(status=503,body='Local proxy'));cfg=json.loads((r/'.local/society-example-account.json').read_text());token=c.request.get(url+'/api/v1/auth/csrf').json()['token'];assert c.request.post(url+'/api/v1/auth/login',data={'email':cfg['email'],'password':cfg['password']},headers={'X-CSRF-TOKEN':token}).ok
 portfolio=c.request.get(url+'/api/v1/membership/portfolio').json();print('Local account linked memberships:',len(portfolio));page=c.new_page();page.goto(url+'/app/membership');expect(page.get_by_role('heading',name='Email my membership details')).to_be_visible();expect(page.get_by_text('Valid till',exact=True)).to_be_visible();page.get_by_role('heading',name='Email my membership details').scroll_into_view_if_needed();page.screenshot(path=str(r/'tests/member-self-email-mobile.png'),full_page=True);assert page.evaluate('document.documentElement.scrollWidth<=window.innerWidth');print('PASS mobile membership email request section with Valid till and no overflow');b.close()
