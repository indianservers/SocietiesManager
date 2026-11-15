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
  page=ctx.new_page();page.on('pageerror',lambda e:errors.append(str(e)));page.goto(LIVE+'/app/account',wait_until='domcontentloaded',timeout=60000)
  if kind=='Institutional':
   expect(page.get_by_role('heading',name='Institutional profile',exact=True)).to_be_visible();expect(page.get_by_role('heading',name='Personal profile',exact=True)).to_be_visible();expect(page.locator('.pa-locked strong')).to_have_text('Kalasalingam Academy of Research and Education');assert page.get_by_label('Full name',exact=True).input_value()!='Kalasalingam Academy of Research and Education'
   personal=ctx.request.get(LIVE+'/api/v1/users/me/personal-profile').json();expect(page.locator('.ip-side-person strong')).to_have_text(personal['fullName'])
   for width in [1586,390]:
    page.set_viewport_size({'width':width,'height':992});page.evaluate('document.fonts.ready');assert page.evaluate('document.documentElement.scrollWidth<=innerWidth');page.screenshot(path=str(ROOT/'tests'/('personal-institutional-live-'+str(width)+'.png')),full_page=True)
   page.get_by_role('link',name='Apply for Individual membership',exact=True).click();expect(page.get_by_role('radio',name='Individual',exact=False)).to_be_checked()
  else:
   expect(page.get_by_role('heading',name='Personal information',exact=True)).to_be_visible();assert page.get_by_label('Full name',exact=True).is_editable()
  checks.append(kind+' existing login and account UI verified');token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':token}).ok;ctx.close()
 assert not errors,errors;browser.close()
(ROOT/'tests/personal-institutional-live-results.json').write_text(json.dumps({'checks':checks,'errors':errors},indent=2));print('\n'.join('PASS '+c for c in checks))
