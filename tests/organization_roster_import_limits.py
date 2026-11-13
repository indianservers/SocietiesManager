"""Exercise the documented bulk-import boundary on the isolated test database."""
import importlib, json, os, subprocess, sys, time, urllib.error
from pathlib import Path
from api_runtime import API_DLL

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'scripts'))
Client=importlib.import_module('seed-society-example').Client
config=json.loads((ROOT/'.local/dev.json').read_text())
fixture=json.loads((ROOT/'.local/roster-browser-fixture.json').read_text())
env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=assemblia_test;Username=postgres;Password={config['password']}",'PublicUrl':'http://127.0.0.1:5094','Logging__LogLevel__Default':'Warning'}
log=(ROOT/'.local/roster-import-limit-api.log').open('w',encoding='utf-8')
server=subprocess.Popen(['dotnet',str(API_DLL),'--urls','http://127.0.0.1:5094'],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
client=Client('http://127.0.0.1:5094/api/v1')
checks=[]
try:
 for _ in range(100):
  try:client.call('/auth/csrf');break
  except Exception:time.sleep(.2)
 client.call('/auth/login','POST',fixture['controller'])
 path='/societies/'+fixture['tenant']+'/membership/members/'+fixture['institution']+'/roster/imports/preview'
 content='Full name,Member type,Email\n'+''.join('Limit member '+str(i)+',Student,limit-repeated@example.test\n' for i in range(10000))
 started=time.monotonic()
 result=client.call(path,'POST',{'format':'csv','content':content})
 assert result['total']==10000 and result['invalid']==10000 and result['valid']==0
 assert all(len(error)<250 for row in result['rows'] for error in row['errors'])
 checks.append('10,000 duplicate rows produce bounded row errors without importing people')
 try:
  client.call(path,'POST',{'format':'csv','content':content+'Extra,Faculty,extra@example.test\n'})
  raise AssertionError('10,001 rows unexpectedly accepted')
 except urllib.error.HTTPError as error:
  detail=json.loads(error.read())['title']
  assert error.code==400 and ('Maximum 10,000' in detail or '1–10,000' in detail),detail
 checks.append('10,001-row upload is rejected with the documented limit')
 for name in checks:print('PASS LIMIT',name,flush=True)
 (ROOT/'tests/organization-roster-import-limits.json').write_text(json.dumps({'passed':len(checks),'checks':checks,'seconds':round(time.monotonic()-started,2)},indent=2),encoding='utf-8')
finally:
 server.terminate();server.wait(timeout=20);log.close()
