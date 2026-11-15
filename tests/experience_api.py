"""Read-only attention/expiry integration checks against the current local preview."""
import json,os,uuid
from pathlib import Path
from urllib.parse import urlparse,parse_qs
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parents[1];URL=os.environ.get('EXPERIENCE_URL','http://127.0.0.1:5197');checks=[]
def check(name,value):
 assert value,name
 checks.append(name);print('PASS',name,flush=True)
with sync_playwright() as pw:
 browser=pw.chromium.launch();ctx=browser.new_context();account=json.loads((ROOT/'.local/society-example-account.json').read_text());tenant=ctx.request.get(URL+'/api/v1/branding/resolve').json()['tenantId'] if URL.startswith('https:') else account['tenant'];path='/api/v1/societies/'+tenant
 check('Anonymous attention denied',ctx.request.get(URL+path+'/workspace/attention').status==401)
 cfg=json.loads((ROOT/('.local/production-owner-login.json' if URL.startswith('https:') else '.local/preview-account.json')).read_text());credentials={'email':cfg['email'],'password':cfg['initialPassword']} if URL.startswith('https:') else {'email':cfg['platformEmail'],'password':cfg['platformPassword']};token=ctx.request.get(URL+'/api/v1/auth/csrf').json()['token'];r=ctx.request.post(URL+'/api/v1/auth/login',data=credentials,headers={'X-CSRF-TOKEN':token});check('Platform login',r.ok)
 r=ctx.request.get(URL+path+'/workspace/attention');check('Platform attention uses real records',r.ok and isinstance(r.json()['items'],list));check('Attention response is private',r.headers.get('cache-control')=='no-store')
 for item in r.json()['items']:
  q=parse_qs(urlparse(item['to']).query)
  if 'validity' not in q:continue
  data=ctx.request.get(URL+path+'/membership/members?kind='+q['memberKind'][0]+'&validity='+q['validity'][0]).json()
  check(item['label']+' matches filtered register',int(item['label'].split()[0])==data['total'])
 check('Unknown tenant denied',ctx.request.get(URL+'/api/v1/societies/'+str(uuid.uuid4())+'/workspace/attention').status==403)
 r=ctx.request.get(URL+path+'/workspace/attention?workspace='+str(uuid.uuid4()));check('Empty workspace cannot return society-wide counts',r.ok and r.json()['items']==[])
 check('Unsupported validity rejected',ctx.request.get(URL+path+'/membership/members?validity=Invalid').status==400)
 for state in ['Active','Expired','Expiring']:
  check(state+' register filter available',ctx.request.get(URL+path+'/membership/members?validity='+state).ok)
 ctx.close();browser.close()
(ROOT/'tests/experience-api-results.json').write_text(json.dumps({'checks':checks},indent=2))
