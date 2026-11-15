"""A valid session survives a change of release content root with the same protected key ring."""
import http.cookiejar,json,os,socket,subprocess,time,urllib.request,urllib.error,uuid
from pathlib import Path
import psycopg
ROOT=Path.cwd();cfg=json.loads((ROOT/'.local/dev.json').read_text());suffix=uuid.uuid4().hex;folder=ROOT/'.local'/('auth-keyring-'+suffix);folder.mkdir();first=folder/'release-one';second=folder/'release-two';first.mkdir();second.mkdir();keys=folder/'keys';keys.mkdir()
with socket.socket() as s:s.bind(('127.0.0.1',0));port=s.getsockname()[1]
url='http://localhost:'+str(port);env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=auth_security_test;Username=postgres;Password={cfg['password']}",'PublicUrl':url,'DataProtection__KeyPath':str(keys),'Logging__LogLevel__Default':'Warning'}
client=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));log=(folder/'api.log').open('w');server=None
def call(path,body=None):
 headers={'Content-Type':'application/json'}
 if body is not None:headers['X-CSRF-TOKEN']=call('/auth/csrf')['token']
 req=urllib.request.Request(url+'/api/v1'+path,headers=headers,data=json.dumps(body).encode() if body is not None else None)
 with client.open(req) as r:return json.loads(r.read() or 'null')
def start(root):
 global server
 server=subprocess.Popen(['dotnet',str(ROOT/'.local/renewal-session-local/Society.Api.dll'),'--urls',url,'--contentRoot',str(root)],cwd=root,env=env,stdout=log,stderr=log,creationflags=subprocess.CREATE_NO_WINDOW)
 for _ in range(100):
  try:urllib.request.urlopen(url+'/health');return
  except Exception:time.sleep(.2)
 raise RuntimeError('Session-protection test API did not start')
try:
 start(first);email='keyring-'+suffix+'@example.invalid';password='Keyring!'+suffix+'Aa9';call('/auth/signup',{'email':email,'password':password,'fullName':'Key ring test'})
 with psycopg.connect(host='127.0.0.1',port=55439,dbname='auth_security_test',user='postgres',password=cfg['password']) as db:db.execute('UPDATE "AspNetUsers" SET "EmailConfirmed"=true WHERE "NormalizedEmail"=%s',(email.upper(),))
 call('/auth/login',{'email':email,'password':password});uid=call('/users/me')['id']; status=call('/security/session'); assert status['persistent']; assert __import__('datetime').datetime.fromisoformat(status['expiresAt'].replace('Z','+00:00')).timestamp()>time.time()+360*86400; server.terminate();server.wait(timeout=15);server=None;start(second);assert call('/users/me')['id']==uid;call('/auth/logout',{});
 try: call('/users/me'); raise AssertionError('Logout retained authentication')
 except urllib.error.HTTPError as e: assert e.code==401
 print('PASS persistent 365-day session, release restart continuity, and logout revocation')
finally:
 if server:server.terminate();server.wait(timeout=15)
 log.close()
