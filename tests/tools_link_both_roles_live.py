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
expect.set_options(timeout=30000)
with sync_playwright() as pw:
 browser=pw.chromium.launch()
 for role,account in [('Institutional',next(a for a in institutions if a['email']=='hodit@klu.ac.in')),('Individual',individuals[0])]:
  ctx=browser.new_context(viewport={'width':1366,'height':1000});token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token}).ok;page=ctx.new_page();page.goto(LIVE+'/app/resources',wait_until='domcontentloaded',timeout=60000);section=page.get_by_role('region',name='AIMER Tools',exact=True);expect(section).to_be_visible();link=section.get_by_role('link',name='Open AIMER Tools');expect(link).to_have_attribute('href','https://tools.aimersociety.com/');expect(link).to_have_attribute('target','_blank');expect(link).to_have_attribute('rel','noopener noreferrer');page.screenshot(path=str(ROOT/'tests'/('tools-link-'+role.lower()+'-live.png')),full_page=True);page.set_viewport_size({'width':390,'height':844});assert page.evaluate('document.documentElement.scrollWidth<=innerWidth');assert ctx.request.get(LIVE+'/api/v1/platform/communications/setup').status==403;ctx.close();print('PASS '+role+' Resources tools link, safe new-tab navigation, mobile layout and platform-access restriction',flush=True)
 browser.close()
