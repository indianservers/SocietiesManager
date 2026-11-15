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
from pypdf import PdfReader
from io import BytesIO
out=ROOT/'output/id-cards';out.mkdir(parents=True,exist_ok=True)
with sync_playwright() as pw:
 browser=pw.chromium.launch()
 for index,account in enumerate(individuals[:2],1):
  ctx=browser.new_context(viewport={'width':1366,'height':1000});token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token}).ok
  portfolio=ctx.request.get(LIVE+'/api/v1/membership/portfolio').json(); rows=portfolio if isinstance(portfolio,list) else portfolio.get('memberships',[]);member=next(m for m in rows if m['kind']=='Individual');base=LIVE+'/api/v1/societies/'+member['tenantId']+'/membership/members/'+member['id']+'/id-card'
  front=ctx.request.get(base+'/front.png');assert front.ok,(front.status,front.text()[:200]);old=out/('individual-'+str(index)+'-front.png');assert not old.exists() or old.read_bytes()==front.body(),'Approved front changed';old.write_bytes(front.body());back=ctx.request.get(base+'/back.png');assert back.ok;(out/('individual-'+str(index)+'-back.png')).write_bytes(back.body());pdf=ctx.request.get(base);assert pdf.ok;(out/('individual-'+str(index)+'-card.pdf')).write_bytes(pdf.body());doc=PdfReader(BytesIO(pdf.body()));assert len(doc.pages)==2;assert abs(float(doc.pages[0].mediabox.width)-85.6*72/25.4)<1;assert ctx.request.get(LIVE+'/api/v1/membership/portfolio').json()==portfolio
  page=ctx.new_page();page.goto(LIVE+'/app/membership',wait_until='domcontentloaded',timeout=60000);image=page.locator('.ip-printed-card>img');expect(image).to_be_visible();page.wait_for_function("()=>{const e=document.querySelector('.ip-printed-card>img');return e&&e.complete&&e.naturalWidth>0}",timeout=30000);page.goto(LIVE+'/app/membership?tab=cards',wait_until='domcontentloaded',timeout=60000);expect(page.locator('.ip-card-sides img')).to_have_count(2);page.wait_for_function("()=>Array.from(document.querySelectorAll('.ip-card-sides img')).every(e=>e.complete&&e.naturalWidth>0)",timeout=30000);page.screenshot(path=str(out/('individual-'+str(index)+'-portal.png')),full_page=True)
  print('PASS real individual two-sided PDF/PNG, portal preview and unchanged membership: '+member['number'],flush=True);ctx.close()
 account=next(a for a in institutions if a['email']=='hodit@klu.ac.in');ctx=browser.new_context();token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token}).ok
 portfolio=ctx.request.get(LIVE+'/api/v1/membership/portfolio').json();rows=portfolio if isinstance(portfolio,list) else portfolio.get('memberships',[]);member=next(m for m in rows if m['kind']=='Institutional');base=LIVE+'/api/v1/societies/'+member['tenantId']+'/membership/members/'+member['id']+'/id-card';assert ctx.request.get(base+'/front.png').status in [400,403];pdf=ctx.request.get(base);assert pdf.ok;assert abs(float(PdfReader(BytesIO(pdf.body())).pages[0].mediabox.width)-210*72/25.4)<1;ctx.close();browser.close();print('PASS institutional member receives certificate format and cannot obtain an individual card front')
