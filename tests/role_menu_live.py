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
  ctx=browser.new_context(viewport={'width':1586,'height':992},user_agent='Mozilla/5.0');token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token}).ok
  page=ctx.new_page();page.on('pageerror',lambda e:errors.append(str(e)));baseline=None
  for path in ['/app/membership','/app/clubs','/app/resources','/app/finance/my','/app/account']:
   page.goto(LIVE+path,wait_until='domcontentloaded',timeout=60000);expect(page.locator('.ip-sidebar')).to_be_visible();expect(page.locator('.ip-brand img')).to_have_attribute('src','/images/aimer-society-logo.png')
   styles=page.locator('.ip-sidebar').evaluate("e=>{const s=getComputedStyle(e),b=getComputedStyle(e.querySelector('.ip-brand')),i=getComputedStyle(e.querySelector('.ip-brand img'));return {background:s.background,color:s.color,width:s.width,padding:s.padding,topbarHeight:getComputedStyle(document.querySelector('.ip-topbar')).height,brandDirection:b.flexDirection,logoWidth:i.width,logoHeight:i.height,links:Array.from(e.querySelectorAll('nav a')).map(a=>a.textContent)}}")
   if baseline is None:baseline=styles
   assert styles==baseline,(role,path,baseline,styles)
   assert page.locator('.ip-institutional').count()==(1 if role=='Institutional' else 0)
   assert page.locator('.in-light').count()==0
   if path=='/app/resources':
    for width in [1586,390]:
     page.set_viewport_size({'width':width,'height':992});assert page.evaluate('document.documentElement.scrollWidth<=innerWidth');page.screenshot(path=str(ROOT/'tests'/('role-menu-'+role.lower()+'-'+str(width)+'.png')),full_page=True)
    page.set_viewport_size({'width':1586,'height':992})
   checks.append(role+' consistent menu: '+path);print('PASS',checks[-1],flush=True)
  token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];assert ctx.request.post(LIVE+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':token}).ok;ctx.close()
 assert not errors,errors;browser.close()
(ROOT/'tests/role-menu-live-results.json').write_text(json.dumps({'checks':checks,'errors':errors},indent=2))
