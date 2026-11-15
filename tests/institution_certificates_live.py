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
import pymupdf,datetime,hashlib
out=ROOT/'output/certificates';out.mkdir(parents=True,exist_ok=True)
chosen=[next(a for a in institutions if a['email']=='hodit@klu.ac.in')];chosen.append(next((a for a in institutions if 'BVC' in str(a['institutions']).upper()),next(a for a in institutions if a['email']!=chosen[0]['email'])))
expect.set_options(timeout=30000)
with sync_playwright() as pw:
 browser=pw.chromium.launch();target=None
 for index,account in enumerate(chosen,1):
  ctx=browser.new_context(viewport={'width':1366,'height':1000});token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token}).ok
  portfolio=ctx.request.get(LIVE+'/api/v1/membership/portfolio').json();rows=portfolio if isinstance(portfolio,list) else portfolio['memberships'];member=next((m for m in rows if m['number']=='AIMERS-INS117'),next(m for m in rows if m['kind']=='Institutional'));base=LIVE+'/api/v1/societies/'+member['tenantId']+'/membership/members/'+member['id'];target=base
  response=ctx.request.get(base+'/membership-certificate');assert response.ok,(response.status,response.text()[:200]);assert 'Membership-Certificate-' in response.headers['content-disposition'];doc=pymupdf.open(stream=response.body(),filetype='pdf');assert len(doc)==1;assert abs(doc[0].rect.width-210*72/25.4)<1 and abs(doc[0].rect.height-297*72/25.4)<1
  stem='institution-'+str(index);(out/(stem+'.pdf')).write_bytes(response.body());doc[0].get_pixmap(dpi=120).save(str(out/(stem+'.png')));(out/(stem+'-record.json')).write_text(json.dumps({k:member.get(k) for k in ['holder','number','effectiveFrom','endsAt','state']},indent=2));assert ctx.request.get(LIVE+'/api/v1/membership/portfolio').json()==portfolio;assert ctx.request.get(base+'/id-card/front.png').status in [400,403]
  page=ctx.new_page();page.goto(LIVE+'/app/membership',wait_until='domcontentloaded',timeout=60000);expect(page.locator('a[href$="/membership-certificate"]').first).to_be_visible();page.screenshot(path=str(out/(stem+'-portal.png')),full_page=True);ctx.close();print('PASS Institutional-only letterhead A4 certificate and unchanged record: '+member['number'],flush=True)
 account=individuals[0];ctx=browser.new_context();token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token}).ok;portfolio=ctx.request.get(LIVE+'/api/v1/membership/portfolio').json();rows=portfolio if isinstance(portfolio,list) else portfolio['memberships'];member=next(m for m in rows if m['kind']=='Individual');base=LIVE+'/api/v1/societies/'+member['tenantId']+'/membership/members/'+member['id'];assert ctx.request.get(base+'/membership-certificate').status==400;assert ctx.request.get(target+'/membership-certificate').status==403;front=ctx.request.get(base+'/id-card/front.png');assert front.ok and front.body()==(ROOT/'output/id-cards/individual-1-front.png').read_bytes();assert len(pymupdf.open(stream=ctx.request.get(base+'/id-card').body(),filetype='pdf'))==2;ctx.close();browser.close();print('PASS Individual rejected from institutional certificate, cross-account access denied, approved ID front unchanged and two-sided card retained')
