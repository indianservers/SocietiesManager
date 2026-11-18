"""Read-only live UI checks using existing accounts; creates no memberships."""
import json
from pathlib import Path
import paramiko
from playwright.sync_api import sync_playwright,expect
ROOT=Path(__file__).resolve().parents[1];host=json.loads((ROOT/'.local/deploy-host.json').read_text());ssh=paramiko.SSHClient();ssh.load_host_keys(str(ROOT/'.local/deployment-known-hosts'));ssh.connect(host['host'],username=host['username'],password=host['password'],timeout=20)
with ssh.open_sftp() as sftp:
 with sftp.open('/mnt/volume_sfo3_1785477034157/aimer-members/secrets/institutional-test-logins-20261115.json') as f: institutions=json.load(f)
 with sftp.open('/mnt/volume_sfo3_1785477034157/aimer-members/secrets/individual-test-logins-20261115.json') as f: individuals=json.load(f)
# A local TLS tunnel avoids intermittent public-network timeouts while testing the live nginx host.
import socketserver,select,threading
class Forward(socketserver.BaseRequestHandler):
 def handle(self):
  channel=ssh.get_transport().open_channel('direct-tcpip',('127.0.0.1',443),self.request.getpeername())
  try:
   while True:
    ready,_,_=select.select([self.request,channel],[],[],30)
    if self.request in ready:
     data=self.request.recv(65536)
     if not data:break
     channel.sendall(data)
    if channel in ready:
     data=channel.recv(65536)
     if not data:break
     self.request.sendall(data)
  finally:channel.close()
class Tunnel(socketserver.ThreadingTCPServer):daemon_threads=True
server=Tunnel(('127.0.0.1',0),Forward);threading.Thread(target=server.serve_forever,daemon=True).start()
LIVE='https://localhost:'+str(server.server_address[1]);checks=[]
import os,mimetypes,requests,urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
class SniAdapter(requests.adapters.HTTPAdapter):
 def init_poolmanager(self,*args,**kwargs):
  kwargs.update(server_hostname='members.aimersociety.com',assert_hostname=False)
  return super().init_poolmanager(*args,**kwargs)
from urllib.parse import urlsplit
out=ROOT/'.local/member-workflow-ux';out.mkdir(exist_ok=True)
dist=Path(os.environ['FRONTEND_DIST']).resolve() if os.environ.get('FRONTEND_DIST') else None
with sync_playwright() as pw:
 browser=pw.chromium.launch(args=['--disable-features=LocalNetworkAccessChecks'])
 institutional_path=None
 for role,account in [('Institutional',next(a for a in institutions if a['email']=='hodit@klu.ac.in')),('Individual',individuals[0])]:
  if os.environ.get('CHECK_ROLE') and os.environ['CHECK_ROLE']!=role:continue
  ctx=browser.new_context(viewport={'width':1440,'height':1000},ignore_https_errors=True);token=ctx.request.get(LIVE+'/api/v1/auth/csrf',headers={'Host':'members.aimersociety.com'}).json()['token'];response=ctx.request.post(LIVE+'/api/v1/auth/login',data={'email':account['email'],'password':account['password']},headers={'X-CSRF-TOKEN':token,'Host':'members.aimersociety.com'});assert response.ok;page=ctx.new_page()
  proxy=requests.Session();proxy.mount('https://',SniAdapter());proxy.verify=False;proxy.headers.update({'Host':'members.aimersociety.com','User-Agent':page.evaluate('navigator.userAgent'),'Cookie':'; '.join(c['name']+'='+c['value'] for c in ctx.cookies())})
  def route_local(route):
   path=urlsplit(route.request.url).path
   if path.startswith('/realtime/'):return route.fulfill(status=404,body='Realtime disabled for layout check')
   if urlsplit(route.request.url).netloc!=urlsplit(LIVE).netloc:return route.continue_()
   f=dist/('index.html' if path.startswith('/app/') else path.lstrip('/')) if dist else None
   if f and not path.startswith('/api/') and f.is_file():return route.fulfill(body=f.read_bytes(),content_type=mimetypes.guess_type(f.name)[0] or 'application/octet-stream')
   for attempt in range(3):
    try:
     response=proxy.request(route.request.method,route.request.url,data=route.request.post_data_buffer,timeout=20)
     return route.fulfill(status=response.status_code,body=response.content,content_type=response.headers.get('Content-Type','application/octet-stream'))
    except requests.RequestException:
     if attempt==2:return route.fulfill(status=502,body='Production proxy failed')
  page.route('**/*',route_local)
  for path in (['membership','account','resources'] if role=='Institutional' else ['resources']):
   page.goto(LIVE+'/app/'+path,wait_until='domcontentloaded',timeout=60000);
   try:page.locator('.ip-sidebar').wait_for(timeout=15000)
   except:
    print('UI diagnostic',page.url,ascii(page.locator('body').inner_text()[:600]),flush=True);raise
   page.wait_for_timeout(500)
   for width in [1440,1366,1024,390]:
    page.set_viewport_size({'width':width,'height':1000 if width>500 else 844});page.wait_for_timeout(150)
    if page.evaluate('document.documentElement.scrollWidth>innerWidth'):print('Overflow elements',page.evaluate("Array.from(document.querySelectorAll('body *')).filter(e=>e.getBoundingClientRect().right>innerWidth+1).slice(0,30).map(e=>({tag:e.tagName,cls:e.className,right:e.getBoundingClientRect().right,width:e.getBoundingClientRect().width}))"),flush=True)
    assert page.evaluate('document.documentElement.scrollWidth<=innerWidth'),(role,path,width,page.evaluate('document.documentElement.scrollWidth'))
    if role=='Institutional' and path=='membership':
     assert page.get_by_role('button',name='Renew membership',exact=True).count()==1
     expect(page.get_by_text('Institutional membership summary',exact=True)).to_be_visible()
    if role=='Institutional' and path=='account' and width==390:
     name=page.locator('.pa-locked strong').bounding_box();badge=page.locator('.pa-locked small').bounding_box();assert badge['y']>=name['y']+name['height']-1
    checks.append(f'{role} {path} {width}px fits viewport')
   page.screenshot(path=str(out/(role.lower()+'-'+path+'-mobile.png')),full_page=True)
   if role=='Institutional' and path=='membership' and not dist:
    page.set_viewport_size({'width':1440,'height':1000});page.get_by_role('button',name='Preview certificate',exact=True).first.click();img=page.get_by_alt_text(__import__('re').compile('^Official membership certificate for '));expect(img).to_be_visible();page.wait_for_function("Array.from(document.images).some(i=>i.alt.startsWith('Official membership certificate for ')&&i.complete&&i.naturalWidth>0)")
    institutional_path=img.get_attribute('src');res=ctx.request.get(LIVE+institutional_path,headers={'Host':'members.aimersociety.com'});assert res.ok and res.headers['content-type'].startswith('image/png') and res.body().startswith(b'\x89PNG');page.get_by_label('Certificate zoom').select_option('125');assert img.evaluate("e=>e.style.width")=='125%';page.screenshot(path=str(out/'certificate-preview.png'),full_page=True);checks.append('Actual protected letterhead PNG preview and zoom work')
  if role=='Individual' and institutional_path:
   assert ctx.request.get(LIVE+institutional_path,headers={'Host':'members.aimersociety.com'}).status in [403,404]
   checks.append('Individual account cannot access another institution certificate preview')
  ctx.close()
 browser.close()
print('PASS '+str(len(checks))+' responsive and certificate checks')
(out/'results.json').write_text(json.dumps(checks,indent=2))

server.shutdown();server.server_close();ssh.close()
