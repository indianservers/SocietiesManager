import json
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
r=Path.cwd();url='http://127.0.0.1:5202';cfg=json.loads((r/'.local/individual-test-account.json').read_text());errors=[];checks=[]
with sync_playwright() as p:
 b=p.chromium.launch();ctx=b.new_context(viewport={'width':1586,'height':992},device_scale_factor=1);ctx.route('**/realtime/**',lambda route:route.fulfill(status=503,body='Local proxy does not support realtime'));token=ctx.request.get(url+'/api/v1/auth/csrf').json()['token'];response=ctx.request.post(url+'/api/v1/auth/login',data={'email':cfg['email'],'password':cfg['password']},headers={'X-CSRF-TOKEN':token});assert response.ok,response.text();page=ctx.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
 for path,title,name in [('/app/membership','My membership','membership'),('/app/clubs','My clubs & chapters','clubs'),('/app/resources','Your resource library','resources'),('/app/finance/my','Invoices & receipts','finance'),('/app/account','Your account','account')]:
  page.goto(url+path);expect(page.get_by_role('heading',name=title,exact=True)).to_be_visible();expect(page.locator('.ip-shell')).to_be_visible();expect(page.locator('.ip-sidebar')).to_be_visible();page.locator('.content-skeleton').wait_for(state='hidden',timeout=15000);page.evaluate('document.fonts.ready');page.screenshot(path=str(r/'tests'/('individual-reference-'+name+'.png')),full_page=True);assert page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'),name+' overflow';checks.append(name+' desktop');print('PASS',name,'desktop',flush=True)
  page.set_viewport_size({'width':390,'height':844});assert page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'),name+' mobile overflow';page.screenshot(path=str(r/'tests'/('individual-reference-'+name+'-mobile.png')),full_page=True);page.set_viewport_size({'width':1586,'height':992});checks.append(name+' mobile')
 assert not errors,errors;print('PASS all five pages and mobile layouts with no browser errors');b.close()
(r/'tests/individual-reference-results.json').write_text(json.dumps({'checks':checks,'errors':errors},indent=2))
