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
import time
expect.set_options(timeout=30000)
with sync_playwright() as pw:
 browser=pw.chromium.launch()
 for role,account in [('Institutional',next(a for a in institutions if a['email']=='hodit@klu.ac.in')),('Individual',individuals[0])]:
  ctx=browser.new_context(viewport={'width':1366,'height':900});token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password'],'rememberMe':False},headers={'X-CSRF-TOKEN':token}).ok
  original=ctx.request.get(LIVE+'/api/v1/users/me').json()['id'];portfolio=ctx.request.get(LIVE+'/api/v1/membership/portfolio').json();session=ctx.request.get(LIVE+'/api/v1/security/session').json();assert session['persistent'];assert any(c['expires']>time.time()+360*86400 for c in ctx.cookies() if c['name']=='society.session')
  page=ctx.new_page();page.goto(LIVE+'/app/membership',wait_until='domcontentloaded',timeout=60000);section=page.get_by_role('region',name='Membership signup and renewal payments');expect(section).to_be_visible();url='https://rzp.io/l/'+('AIMERSIndividualFee' if role=='Individual' else 'AIMERSEduMembershipFee');expect(section.locator('a')).to_have_attribute('href',url);assert section.locator('a').count()==1;page.screenshot(path=str(ROOT/'tests'/('membership-payment-'+role.lower()+'.png')),full_page=True)
  state=ctx.storage_state();ctx.close();ctx=browser.new_context(storage_state=state);assert ctx.request.get(LIVE+'/api/v1/users/me').json()['id']==original;assert ctx.request.get(LIVE+'/api/v1/membership/portfolio').json()==portfolio;token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':token}).ok;assert ctx.request.get(LIVE+'/api/v1/users/me').status==401;ctx.close();print('PASS live '+role+' payment link, persistent cookie/browser restart, unchanged memberships and logout',flush=True)
 ctx=browser.new_context();page=ctx.new_page();page.goto(LIVE+'/',wait_until='domcontentloaded',timeout=60000);section=page.get_by_role('region',name='Membership signup and renewal payments');expect(section).to_be_visible();assert section.locator('a').count()==2;browser.close()

