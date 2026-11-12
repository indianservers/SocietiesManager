"""Real PostgreSQL + HTTP integration suite. Uses a dedicated database ending _test."""
from api_runtime import API_DLL
import os,sys,json,time,uuid,subprocess,urllib.request,urllib.error,http.cookiejar,urllib.parse,traceback
from pathlib import Path
import psycopg,struct,zlib,statistics
ROOT=Path(__file__).resolve().parents[1]
config=json.loads((ROOT/'.local/dev.json').read_text())
password=config['password']
DSN=f'host=127.0.0.1 port=55439 dbname=assemblia_test user=postgres password={password}'
with psycopg.connect(f'host=127.0.0.1 port=55439 dbname=postgres user=postgres password={password}',autocommit=True) as conn:
 if not conn.execute("SELECT 1 FROM pg_database WHERE datname='assemblia_test'").fetchone():conn.execute('CREATE DATABASE assemblia_test')
env={**os.environ,'ConnectionStrings__Database':f'Host=127.0.0.1;Port=55439;Database=assemblia_test;Username=postgres;Password={password}','ASPNETCORE_ENVIRONMENT':'Development','PublicUrl':'http://127.0.0.1:5089','Logging__LogLevel__Default':'Warning'}
subprocess.run(['dotnet',str(API_DLL),'--migrate'],cwd=ROOT,env=env,check=True,stdout=subprocess.DEVNULL)
log=open(ROOT/'.local/test-api.log','w',encoding='utf-8')
server=subprocess.Popen(['dotnet',str(API_DLL),'--urls','http://127.0.0.1:5089'],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
BASE='http://127.0.0.1:5089/api/v1'
class Client:
 def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def upload(self,path,data,expected=200):
  csrf=self.request('/auth/csrf')['token']
  req=urllib.request.Request(BASE+path,method='POST',headers={'Content-Type':'image/png','X-CSRF-TOKEN':csrf},data=data)
  try:
   with self.opener.open(req) as r:status=r.status;body=r.read()
  except urllib.error.HTTPError as e:status=e.code;body=e.read()
  assert status==expected,f'Upload expected {expected}, got {status}'
  return json.loads(body)
 def request(self,path,method='GET',data=None,expected=200,csrf=True):
  headers={'Content-Type':'application/json'}
  if method!='GET' and csrf:headers['X-CSRF-TOKEN']=self.request('/auth/csrf')['token']
  req=urllib.request.Request(BASE+path,data=json.dumps(data).encode() if data is not None else (b'' if method!='GET' else None),headers=headers,method=method)
  try:
   with self.opener.open(req) as r:status=r.status;raw=r.read()
  except urllib.error.HTTPError as e:status=e.code;raw=e.read()
  assert status==expected,f'{method} {path.split("?")[0]}: expected {expected}, got {status}: {raw[:300]!r}'
  return json.loads(raw) if raw else None
def exec_assert(condition):
 assert condition
results=[]
def test(name,fn):
 start=time.perf_counter()
 try:fn();results.append({'name':name,'passed':True,'ms':round((time.perf_counter()-start)*1000,2)});print('PASS',name,flush=True)
 except Exception as e:results.append({'name':name,'passed':False,'error':str(e)});print('FAIL',name,str(e),flush=True);raise
suffix=uuid.uuid4().hex[:10]
def signup(client,name):
 email=f'{name}-{suffix}@example.test'
 client.request('/auth/signup','POST',{'email':email,'password':'Test-Password-2026!','fullName':name})
 with psycopg.connect(DSN) as db:
  user=db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(email,)).fetchone()[0]
  body=db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s ORDER BY "CreatedAt" DESC LIMIT 1',(email,)).fetchone()[0]
 token=urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
 client.request('/auth/verify?userId='+str(user)+'&token='+urllib.parse.quote(token,safe=''),'POST')
 return email,str(user)
def login(c,email,password='Test-Password-2026!'):return c.request('/auth/login','POST',{'email':email,'password':password})
def application(c,name):return c.request('/societies/applications','POST',{'name':name+' '+suffix,'category':'Association','country':'India','state':'AP','city':'Vijayawada','officialEmail':'office@example.test','contactNumber':'1234567890','website':None,'address':'Test address','reference':suffix,'applicantMobile':'1234567890'})['id']
try:
 for _ in range(60):
  try:urllib.request.urlopen('http://127.0.0.1:5089/health');break
  except Exception:time.sleep(.5)
 admin,owner,other,officer=Client(),Client(),Client(),Client()
 accounts={}
 test('user signup and email verification',lambda:accounts.update(owner=signup(owner,'owner')))
 email,user=accounts['owner']
 test('login',lambda:login(owner,email))
 test('CSRF rejection',lambda:owner.request('/auth/logout','POST',expected=400,csrf=False))
 test('password recovery request',lambda:owner.request('/auth/forgot','POST',{'email':email,'password':''}))
 with psycopg.connect(DSN) as db:resetbody=db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=%s ORDER BY "CreatedAt" DESC LIMIT 1',(email,'Reset your password')).fetchone()[0]
 reset=urllib.parse.parse_qs(urllib.parse.urlparse(resetbody).query)['token'][0]
 test('password recovery completion',lambda:owner.request('/auth/reset','POST',{'email':email,'token':reset,'password':'Changed-Password-2026!'}))
 test('login after password reset',lambda:login(owner,email,'Changed-Password-2026!'))
 app={}
 test('society application',lambda:app.update(a=application(owner,'Primary society')))
 test('duplicate society rejected',lambda:owner.request('/societies/applications','POST',{'name':'Primary society '+suffix,'category':'Association','country':'India','state':'AP','city':'Vijayawada','officialEmail':'office@example.test','contactNumber':'123','address':'Address','applicantMobile':'123'},expected=409))
 test('no administrator workspace before approval',lambda:(_ for _ in ()).throw(AssertionError()) if owner.request('/workspaces') else None)
 test('unauthorized society approval blocked',lambda:owner.request('/societies/applications/'+app['a']+'/review','POST',{'approve':True,'note':'Invalid'},expected=403))
 ae,aid=signup(admin,'platform')
 with psycopg.connect(DSN) as db:db.execute('UPDATE "AspNetUsers" SET "PlatformAdmin"=true WHERE "Id"=%s',(aid,))
 login(admin,ae)
 test('authorized society approval',lambda:app.update(approval=admin.request('/societies/applications/'+app['a']+'/review','POST',{'approve':True,'note':'Verified'})))
 tenant=app['approval']['tenantId'];prefix='/societies/'+tenant
 test('workspace discovery',lambda:exec_assert(any(w['id']==tenant for w in owner.request('/workspaces'))))
 test('platform admin cannot browse private tenant',lambda:admin.request(prefix+'/dashboard',expected=403))
 test('approval cannot be repeated',lambda:admin.request('/societies/applications/'+app['a']+'/review','POST',{'approve':True,'note':'Again'},expected=400))
 rejected=application(owner,'Rejected society')
 test('society rejection',lambda:admin.request('/societies/applications/'+rejected+'/review','POST',{'approve':False,'note':'Missing documents'}))
 test('society onboarding save',lambda:owner.request(prefix+'/onboarding','PUT',{'step':2,'data':'{"purpose":"Community"}','activate':False},expected=204))
 test('society activation',lambda:owner.request(prefix+'/onboarding','PUT',{'step':7,'data':'{"directoryVisibility":"chapter"}','activate':True},expected=204))
 levels=owner.request(prefix+'/organizations/levels');region=next(l['id'] for l in levels if l['kind']=='Region');chapter=next(l['id'] for l in levels if l['kind']=='Chapter')
 def unit(level,parent,name,code,status='Active'):return {'levelId':level,'parentId':parent,'name':name,'code':code,'status':status,'details':'{"city":"Vijayawada"}'}
 units={}
 test('region creation',lambda:units.update(region=owner.request(prefix+'/organizations/units','POST',unit(region,None,'Andhra Pradesh','AP'))['id']))
 test('second region creation',lambda:units.update(region2=owner.request(prefix+'/organizations/units','POST',unit(region,None,'Telangana','TS'))['id']))
 test('chapter creation',lambda:units.update(chapter=owner.request(prefix+'/organizations/units','POST',unit(chapter,units['region'],'Vijayawada','VJA'))['id']))
 test('invalid parent type rejected',lambda:owner.request(prefix+'/organizations/units','POST',unit(chapter,units['chapter'],'Invalid chapter','INVALID'),expected=400))
 test('duplicate chapter code rejected',lambda:owner.request(prefix+'/organizations/units','POST',unit(chapter,units['region'],'Duplicate','VJA'),expected=409))
 def edit_unit(id,**changes):
  u=owner.request(prefix+'/organizations/units/'+id);u.update(changes);return owner.request(prefix+'/organizations/units/'+id,'PUT',u,expected=204)
 test('chapter suspension',lambda:edit_unit(units['chapter'],status='Suspended'))
 test('chapter reassignment',lambda:edit_unit(units['chapter'],parentId=units['region2']))
 test('stale edit conflict',lambda:owner.request(prefix+'/organizations/units/'+units['chapter'],'PUT',{**unit(chapter,units['region'],'Stale','VJA'),'revision':0},expected=409))
 alt={}
 test('alternate hierarchy definition',lambda:alt.update(level=owner.request(prefix+'/organizations/levels','POST',{'name':'Flexible tier','kind':'Other','allowedParents':[],'allowRoot':True})['id']))
 # An existing level can be self-referential in fixtures to test cycle safety independently of parent types.
 test('hierarchy level editing',lambda:owner.request(prefix+'/organizations/levels/'+alt['level'],'PUT',{'name':'Flexible tier','kind':'Other','allowedParents':[alt['level']],'allowRoot':True},expected=204))
 alt['root']=owner.request(prefix+'/organizations/units','POST',unit(alt['level'],None,'Root tier','ROOT'))['id']
 alt['child']=owner.request(prefix+'/organizations/units','POST',unit(alt['level'],alt['root'],'Child tier','CHILD'))['id']
 test('hierarchy cycle prevention',lambda:owner.request(prefix+'/organizations/units/'+alt['root'],'PUT',{**unit(alt['level'],alt['child'],'Root tier','ROOT'),'revision':1},expected=400))
 oe,oid=signup(other,'other');login(other,oe);otherapp=application(other,'Other society');otherTenant=admin.request('/societies/applications/'+otherapp+'/review','POST',{'approve':True,'note':'Verified'})['tenantId']
 test('cross-tenant dashboard isolation',lambda:other.request(prefix+'/dashboard',expected=403))
 test('unauthorized chapter editing blocked',lambda:other.request(prefix+'/organizations/units/'+units['chapter'],'PUT',{**unit(chapter,units['region'],'Attack','VJA'),'revision':3},expected=403))
 otherLevel=next(l['id'] for l in other.request('/societies/'+otherTenant+'/organizations/levels') if l['kind']=='Region')
 otherRegion=other.request('/societies/'+otherTenant+'/organizations/units','POST',unit(otherLevel,None,'Other region','OTHER'))['id']
 test('cross-society parent rejected',lambda:owner.request(prefix+'/organizations/units','POST',unit(chapter,otherRegion,'Cross tenant','CROSS'),expected=400))
 test('unauthorized society settings blocked',lambda:other.request(prefix+'/settings',expected=403))
 test('tree pagination bounded',lambda:exec_assert(len(owner.request(prefix+'/organizations/units?pageSize=1')['items'])==1))
 test('lazy root loading',lambda:exec_assert(all(u['parentId'] is None for u in owner.request(prefix+'/organizations/units?kind=roots')['items'])))
 test('region search',lambda:exec_assert(owner.request(prefix+'/organizations/units?kind=Region&search=Andhra')['total']==1))
 test('chapter status filtering',lambda:exec_assert(owner.request(prefix+'/organizations/units?kind=Chapter&status=Suspended')['total']==1))
 roles={}
 test('role creation',lambda:roles.update(chapter=owner.request(prefix+'/roles','POST',{'name':'Custom Chapter Secretary','permissions':['units.read','units.write'],'responsibilities':'Manage assigned chapter'})['id']))
 test('role editing',lambda:owner.request(prefix+'/roles/'+roles['chapter'],'PUT',{'name':'Custom Chapter Secretary','permissions':['units.read','units.write'],'responsibilities':'Manage one authorized chapter'},expected=204))
 fe,fid=signup(officer,'officer');login(officer,fe)
 test('officer invitation',lambda:owner.request(prefix+'/invitations','POST',{'email':fe}))
 with psycopg.connect(DSN) as db:body=db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=%s ORDER BY "CreatedAt" DESC LIMIT 1',(fe,'Society workspace invitation')).fetchone()[0]
 inviteToken=urllib.parse.parse_qs(urllib.parse.urlparse(body.split('open ')[1]).query)['token'][0]
 test('verified invitation acceptance',lambda:officer.request('/invitations/accept','POST',{'token':inviteToken}))
 test('invitation replay blocked',lambda:officer.request('/invitations/accept','POST',{'token':inviteToken},expected=400))
 test('role assignment',lambda:owner.request(prefix+'/role-assignments','POST',{'userId':fid,'roleId':roles['chapter'],'unitId':units['chapter'],'startsAt':'2026-01-01T00:00:00Z','endsAt':'2027-01-01T00:00:00Z'}))
 test('scoped workspace dashboard narrows counts',lambda:exec_assert(owner.request(prefix+'/dashboard?workspace='+units['chapter'])['units']==1))
 test('unrelated workspace parameter grants no authority',lambda:exec_assert(officer.request(prefix+'/organizations/units?workspace='+units['region'])['total']==0))
 test('scoped hierarchy exposes its authorized root',lambda:exec_assert(officer.request(prefix+'/organizations/units?kind=roots')['items'][0]['id']==units['chapter']))
 test('scoped chapter read allowed',lambda:officer.request(prefix+'/organizations/units/'+units['chapter']))
 test('permission scope enforcement',lambda:officer.request(prefix+'/organizations/units/'+units['region'],expected=403))
 test('scoped officer cannot assign roles',lambda:officer.request(prefix+'/roles','POST',{'name':'Escalation','permissions':['roles.manage'],'responsibilities':''},expected=403))
 test('future leadership not yet effective',lambda:owner.request(prefix+'/role-assignments','POST',{'userId':fid,'roleId':roles['chapter'],'unitId':units['region'],'startsAt':'2099-01-01T00:00:00Z','endsAt':None}))
 test('expired leadership assignment accepted as history',lambda:owner.request(prefix+'/role-assignments','POST',{'userId':fid,'roleId':roles['chapter'],'unitId':units['region'],'startsAt':'2020-01-01T00:00:00Z','endsAt':'2021-01-01T00:00:00Z'}))
 test('expired leadership grants no access',lambda:officer.request(prefix+'/organizations/units/'+units['region'],expected=403))
 test('future leadership grants no access',lambda:officer.request(prefix+'/organizations/units/'+units['region'],expected=403))
 test('invalid leadership effective dates',lambda:owner.request(prefix+'/role-assignments','POST',{'userId':fid,'roleId':roles['chapter'],'unitId':units['chapter'],'startsAt':'2027-01-01T00:00:00Z','endsAt':'2026-01-01T00:00:00Z'},expected=400))
 test('workspace metadata excludes application personal data',lambda:exec_assert('ApplicantMobile' not in officer.request('/workspaces')[0]['profile']))
 test('workspace switching remains isolated',lambda:exec_assert(len(officer.request('/workspaces'))==1 and other.request('/workspaces')[0]['id']!=tenant))
 test('recursive region chapter counts',lambda:exec_assert(next(u['chapters'] for u in owner.request(prefix+'/organizations/units?kind=Region')['items'] if u['id']==units['region2'])==1))
 def chunk(t,d):return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
 png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',1,1,8,6,0,0,0))+chunk(b'tEXt',b'private metadata')+chunk(b'IDAT',zlib.compress(b'\x00\xff\x00\x00\xff'))+chunk(b'IEND',b'')
 assets={}
 test('validated PNG upload',lambda:assets.update(image=owner.upload(prefix+'/assets',png)['url']))
 test('invalid image upload rejected',lambda:owner.upload(prefix+'/assets',b'<svg><script>alert(1)</script></svg>',expected=400))
 test('unauthorized image upload rejected',lambda:other.upload(prefix+'/assets',png,expected=403))
 def assert_asset():
  with owner.opener.open('http://127.0.0.1:5089'+assets['image']) as r:
   image=r.read();assert r.headers['Content-Type']=='image/png' and b'private metadata' not in image
 test('uploaded image strips metadata',assert_asset)
 test('cross-tenant image retrieval blocked',lambda:other.request(assets['image'].removeprefix('/api/v1'),expected=403))
 response_times=[]
 for _ in range(20):
  start=time.perf_counter();owner.request(prefix+'/organizations/units?pageSize=20');response_times.append((time.perf_counter()-start)*1000)
 (ROOT/'tests/http-performance.json').write_text(json.dumps({'scope':'20 warm authenticated HTTP directory reads, small integration fixture, single client','median_ms':round(statistics.median(response_times),3),'p95_ms':round(sorted(response_times)[18],3),'samples':20},indent=2),encoding='utf-8')
 test('audit trail generation',lambda:exec_assert(any(x['action']=='unit.updated' for x in owner.request(prefix+'/audit-logs'))))
 def database_guards():
  with psycopg.connect(DSN) as db:
   try:db.execute('UPDATE "Units" SET "ParentId"=%s WHERE "Id"=%s',(alt['child'],alt['root']));raise AssertionError('Database allowed a cycle')
   except psycopg.errors.RaiseException:db.rollback()
   try:db.execute('DELETE FROM "Units" WHERE "Id"=%s',(units['chapter'],));raise AssertionError('Database allowed history deletion')
   except psycopg.errors.RaiseException:db.rollback()
   try:db.execute('DELETE FROM "Audits" WHERE "TenantId"=%s',(tenant,));raise AssertionError('Database allowed audit deletion')
   except psycopg.errors.RaiseException:db.rollback()
 test('PostgreSQL cycle and immutable-history guards',database_guards)
 test('PostgreSQL migrations applied',lambda:exec_assert(len(psycopg.connect(DSN).execute('SELECT * FROM "__EFMigrationsHistory"').fetchall())>=5))
 def multiple_societies():
  app2=application(owner,'Second owner society')
  second=admin.request('/societies/applications/'+app2+'/review','POST',{'approve':True,'note':'Verified second affiliation'})['tenantId']
  assert len(owner.request('/workspaces'))==2
  assert owner.request('/societies/'+second+'/dashboard')['units']==0
  assert owner.request(prefix+'/dashboard')['units']>0
 test('one verified identity switches between two societies',multiple_societies)
 test('logout',lambda:owner.request('/auth/logout','POST',expected=204))
 test('session revoked on logout',lambda:owner.request('/users/me',expected=401))
finally:
 server.terminate();server.wait(timeout=20);log.close()
 (ROOT/'tests/results.json').write_text(json.dumps({'database':'PostgreSQL 17','results':results,'passed':sum(x['passed'] for x in results),'total':len(results)},indent=2),encoding='utf-8')

def unused():pass
