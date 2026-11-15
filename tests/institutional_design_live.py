"""Read-only live UI checks using existing accounts; creates no memberships."""
import json
from pathlib import Path
import paramiko
from playwright.sync_api import sync_playwright,expect
ROOT=Path(__file__).resolve().parents[1];host=json.loads((ROOT/'.local/deploy-host.json').read_text());ssh=paramiko.SSHClient();ssh.load_host_keys(str(ROOT/'.local/deployment-known-hosts'));ssh.connect(host['host'],username=host['username'],password=host['password'],timeout=20)
with ssh.open_sftp() as sftp:
 with sftp.open('/mnt/volume_sfo3_1785477034157/aimer-members/secrets/institutional-test-logins-20261115.json') as f: institutions=json.load(f)
 with sftp.open('/mnt/volume_sfo3_1785477034157/aimer-members/secrets/individual-test-logins-20261115.json') as f: individuals=json.load(f)
ssh.close();LIVE='https://members.aimersociety.com';errors=[];checks=[]
with sync_playwright() as pw:
 browser=pw.chromium.launch()
 for kind,account in [('Institutional',next(a for a in institutions if a['email']=='hodit@klu.ac.in')),('Individual',individuals[0])]:
  ctx=browser.new_context(viewport={'width':1586,'height':992},user_agent='Mozilla/5.0')
  token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];r=ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token});assert r.ok,r.text()
  page=ctx.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
  if kind=='Institutional':
   portfolio=ctx.request.get(LIVE+'/api/v1/membership/portfolio').json();institution=next(m for m in portfolio if m['kind']=='Institutional');requests_before=ctx.request.get(LIVE+'/api/v1/membership/institutions/'+institution['id']+'/requests').json()
   for path,title,slug in [('/app/membership','Institutional membership','membership'),('/app/account','My account','account'),('/app/clubs','Clubs & chapters','clubs'),('/app/resources','Resources','resources')]:
    page.goto(LIVE+path,wait_until='domcontentloaded',timeout=60000);expect(page.get_by_role('heading',name=title,exact=True)).to_be_visible();expect(page.locator('.ip-side-person strong')).to_have_text(ctx.request.get(LIVE+'/api/v1/users/me/personal-profile').json()['fullName']);page.locator('.content-skeleton').wait_for(state='hidden',timeout=30000);page.evaluate('document.fonts.ready');page.locator('img[src*="/images/institutional/"]').evaluate_all("images=>Promise.all(images.map(image=>image.decode()))")
    for width in [1586,390]:
     page.set_viewport_size({'width':width,'height':992});assert page.evaluate('document.documentElement.scrollWidth<=innerWidth'),slug+' overflow';page.screenshot(path=str(ROOT/'tests'/('institutional-design-live-'+slug+'-'+str(width)+'.png')),full_page=True)
    page.set_viewport_size({'width':1586,'height':992});checks.append(slug+' live desktop/mobile');print('PASS',checks[-1],flush=True)
    if slug=='membership':
     for tab in ['Institution details','Documents','Renewal history','Overview']:page.get_by_role('button',name=tab,exact=True).click()
     expect(page.get_by_role('heading',name='Document download',exact=True)).to_be_visible()
    elif slug=='account':
     expect(page.locator('.pa-locked strong')).to_have_text(institution['holder']);assert page.get_by_label('Full name',exact=True).is_editable()
    elif slug=='resources':
     for tab in ['Guides','Documents','Videos','Saved','All']:page.get_by_role('button',name=tab,exact=True).click()
   assert ctx.request.get(LIVE+'/api/v1/membership/institutions/'+institution['id']+'/requests').json()==requests_before
  else:
   page.goto(LIVE+'/app/account',wait_until='domcontentloaded',timeout=60000);expect(page.get_by_role('heading',name='Personal information',exact=True)).to_be_visible();assert page.get_by_label('Full name',exact=True).is_editable();checks.append('Individual account unchanged');print('PASS',checks[-1],flush=True)
  token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':token}).ok;ctx.close()
 assert not errors,errors;browser.close()
(ROOT/'tests/institutional-design-live-results.json').write_text(json.dumps({'checks':checks,'errors':errors,'mode':'Existing live accounts; read-only verification apart from login/logout'},indent=2))
