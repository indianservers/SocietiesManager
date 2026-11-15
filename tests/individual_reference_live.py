"""Live frontend checks with browser-only local member fixture; no production mutations."""
import json,re
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
ROOT=Path(__file__).resolve().parents[1];LIVE='https://members.aimersociety.com';LOCAL='http://127.0.0.1:5201';cfg=json.loads((ROOT/'.local/individual-test-account.json').read_text());checks=[];errors=[]
with sync_playwright() as pw:
 b=pw.chromium.launch();local=pw.request.new_context(base_url=LOCAL);token=local.get('/api/v1/auth/csrf').json()['token'];assert local.post('/api/v1/auth/login',data={'email':cfg['email'],'password':cfg['password']},headers={'X-CSRF-TOKEN':token}).ok
 ctx=b.new_context(viewport={'width':1586,'height':992});ctx.route('**/realtime/**',lambda route:route.fulfill(status=503,body='Read-only preview'))
 def api(route):
  request=route.request
  if request.method!='GET':return route.fulfill(status=403,content_type='application/json',body='{"title":"Read-only live visual verification"}')
  path=request.url.split('/api/v1',1)[1];r=local.get('/api/v1'+path);route.fulfill(status=r.status,headers={k:v for k,v in r.headers.items() if k.lower() not in ['content-length','content-encoding','transfer-encoding','connection','set-cookie']},body=r.body())
 ctx.route('**/api/v1/**',api);page=ctx.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
 for path,title,name in [('/app/membership','My membership','membership'),('/app/clubs','My clubs & chapters','clubs'),('/app/resources','Your resource library','resources'),('/app/finance/my','Invoices & receipts','finance'),('/app/account','Your account','account')]:
  page.goto(LIVE+path);expect(page.get_by_role('heading',name=title,exact=True)).to_be_visible();page.evaluate('document.fonts.ready');assert page.evaluate('document.documentElement.scrollWidth<=innerWidth');page.screenshot(path=str(ROOT/'.local'/('individual-live-'+name+'.png')),full_page=True);page.set_viewport_size({'width':390,'height':844});assert page.evaluate('document.documentElement.scrollWidth<=innerWidth');page.set_viewport_size({'width':1586,'height':992});checks.append(name+' deployed frontend desktop and mobile');print('PASS',checks[-1],flush=True)
 assert not errors,errors;b.close()
(ROOT/'tests/individual-reference-live-results.json').write_text(json.dumps({'url':LIVE,'mode':'Production assets with isolated local member fixture supplied only in browser; no production records or emails created','checks':checks,'errors':errors},indent=2))
