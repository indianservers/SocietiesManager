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
 req=pw.request.new_context();assert req.get(LIVE+'/health/ready').ok
 for role,account in [('Institutional',next(a for a in institutions if a['email']=='hodit@klu.ac.in')),('Individual',individuals[0])]:
  ctx=pw.request.new_context();token=ctx.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token}).ok;rows=ctx.get(LIVE+'/api/v1/membership/portfolio').json();rows=rows if isinstance(rows,list) else rows['memberships'];m=next(m for m in rows if m['kind']==role);path=LIVE+'/api/v1/societies/'+m['tenantId']+'/membership/members/'+m['id']+'/recognitions';assert req.get(path).status==401;response=ctx.get(path);assert response.ok,(role,response.status,response.text()[:150]);data=response.json();assert data['options']==['Fellowship','Board Member'] and not data['canManage'];print('PASS live '+role+' recognition visibility is read-only; anonymous access denied',flush=True);ctx.dispose()
