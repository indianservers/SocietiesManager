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
 browser=pw.chromium.launch();ctx=browser.new_context(viewport={'width':1366,'height':900},user_agent='Mozilla/5.0');p=ctx.new_page();expect.set_options(timeout=30000);account=next(a for a in institutions if a['email']=='hodit@klu.ac.in');token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token}).ok;original=ctx.request.get(LIVE+'/api/v1/users/me').json();portfolio=ctx.request.get(LIVE+'/api/v1/membership/portfolio').json();token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':token}).ok
 p.goto(LIVE+'/',wait_until='domcontentloaded',timeout=60000);p.get_by_label('Email or username',exact=True).fill(account['email']);p.get_by_label('Password',exact=True).fill(account['password']);p.get_by_role('button',name='Sign in',exact=True).click();p.wait_for_url('**/app/membership',timeout=60000);expect(p.get_by_role('heading',name='Institutional membership',exact=True)).to_be_visible();current=ctx.request.get(LIVE+'/api/v1/users/me').json();assert current['id']==original['id'] and current['platformAdmin']==original['platformAdmin'];assert ctx.request.get(LIVE+'/api/v1/membership/portfolio').json()==portfolio;token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':token}).ok;browser.close();print('PASS live existing institutional account retains its account ID, role, memberships and portal after sign-in')
