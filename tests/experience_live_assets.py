"""Verify that production serves the exact built homepage and application bundle."""
import hashlib,json,re
from pathlib import Path
from playwright.sync_api import sync_playwright
root=Path(__file__).resolve().parents[1];url='https://members.aimersociety.com';checks=[]
def check(name,value):
 assert value,name
 checks.append(name);print('PASS',name,flush=True)
with sync_playwright() as p:
 request=p.request.new_context()
 r=request.get(url+'/health/ready');check('Production HTTPS readiness',r.ok and r.json()['ready'])
 html=request.get(url+'/').text();local=(root/'frontend/dist/index.html').read_text(encoding='utf-8')
 entry=re.search(r'src="(/assets/[^\"]+\.js)"',local).group(1);check('Homepage references current built entry',entry in html)
 for path in [root/'frontend/dist'/entry.lstrip('/'),*list((root/'frontend/dist/assets').glob('App-*.js')),*list((root/'frontend/dist/assets').glob('App-*.css'))]:
  remote=request.get(url+'/'+path.relative_to(root/'frontend/dist').as_posix());check('Exact deployed checksum: '+path.name,remote.ok and hashlib.sha256(remote.body()).hexdigest()==hashlib.sha256(path.read_bytes()).hexdigest())
 check('Private account endpoint rejects anonymous visitors',request.get(url+'/api/v1/users/me').status==401)
 request.dispose()
(root/'tests/experience-live-assets.json').write_text(json.dumps({'checks':checks},indent=2))
