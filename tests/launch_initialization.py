"""Production-style first-run initialization against a dedicated empty database."""
from api_runtime import API_DLL
import json,os,secrets,subprocess,uuid,time,urllib.request,http.cookiejar
from pathlib import Path
import psycopg

ROOT=Path(__file__).resolve().parents[1]
dev=json.loads((ROOT/'.local/dev.json').read_text())
suffix=uuid.uuid4().hex[:10]
database='assemblia_launch_bootstrap_'+suffix+'_test'
assert database.endswith('_test')
dsn=dict(host='127.0.0.1',port=55439,user='postgres',password=dev['password'])
with psycopg.connect(dbname='postgres',autocommit=True,**dsn) as db:
 if not db.execute('SELECT 1 FROM pg_database WHERE datname=%s',(database,)).fetchone():db.execute('CREATE DATABASE '+database)
tenant=str(uuid.uuid4())
email='launch-owner-'+suffix+'@example.test'
configuration={'name':'Synthetic launch '+suffix,'platformOwnerEmail':email,'launchDomain':'launch-'+suffix+'.example.test','country':'India','chapters':['Chapter One','Chapter Two'],
 'memberships':[{'category':kind+' Membership','kind':kind,'annualPrice':500 if kind=='Individual' else 10000} for kind in ['Individual','Institutional','Corporate']]}
config_file=ROOT/'.local/launch-bootstrap-fixture.json';config_file.write_text(json.dumps(configuration))
output=ROOT/'.local/launch-bootstrap-owner-output.json'
env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Production','ConnectionStrings__Database':f'Host=127.0.0.1;Port=55439;Database={database};Username=postgres;Password={dev["password"]}',
 'BOOTSTRAP_EMAIL':email,'BOOTSTRAP_PASSWORD':secrets.token_urlsafe(32)+'!aA1','SOCIETY_LAUNCH_CONFIG':str(config_file),'SOCIETY_LAUNCH_TENANT':tenant,'PublicUrl':'https://'+configuration['launchDomain'],'Society__LaunchOutput':str(output)}
log=open(ROOT/'.local/launch-bootstrap-test.log','w',encoding='utf-8')
checks=[]
def command(flag):subprocess.run(['dotnet',str(API_DLL),flag],cwd=ROOT/'backend',env=env,stdout=log,stderr=log,check=True,timeout=120)
def check(name,value):assert value,name;checks.append(name);print('PASS INITIALIZATION',name,flush=True)
try:
 command('--migrate');command('--bootstrap-admin');command('--initialize-society-launch')
 with psycopg.connect(dbname=database,**dsn) as db:
  check('production initialization creates the approved society',db.execute('SELECT "Status" FROM "Tenants" WHERE "Id"=%s',(tenant,)).fetchone()[0]=='Active')
  check('production initialization creates only configured chapters',db.execute('SELECT count(*) FROM "Units" u JOIN "Levels" l ON l."Id"=u."LevelId" WHERE u."TenantId"=%s AND l."Kind"=\'Chapter\'',(tenant,)).fetchone()[0]==2)
  check('production initialization never imports synthetic members',db.execute('SELECT count(*) FROM "Memberships" WHERE "TenantId"=%s',(tenant,)).fetchone()[0]==0)
  check('production initialization never invents payments or receipts',db.execute('SELECT count(*) FROM "FinancePayment" WHERE "TenantId"=%s',(tenant,)).fetchone()[0]==0)
  check('receiving entity is explicitly configured',db.execute('SELECT count(*) FROM "FinancialEntity" WHERE "TenantId"=%s',(tenant,)).fetchone()[0]==1)
  check('all membership offers have a receiving entity and annual term',db.execute('SELECT count(*) FROM "MembershipOffer" WHERE "TenantId"=%s AND "EntityId" IS NOT NULL AND "TermMonths"=12',(tenant,)).fetchone()[0]==3)
  check('fee policy is INR 500 individual and INR 10000 institutional and corporate',dict(db.execute('SELECT c."Kind",o."Price" FROM "MembershipOffer" o JOIN "MembershipCategories" c ON c."Id"=o."CategoryId" WHERE o."TenantId"=%s',(tenant,)).fetchall())=={'Individual':500,'Institutional':10000,'Corporate':10000})
  check('owner identity is linked to the society with explicit administration',db.execute('SELECT count(*) FROM "Links" l JOIN "AspNetUsers" u ON u."Id"=l."UserId" WHERE l."TenantId"=%s AND u."Email"=%s AND u."PlatformAdmin"',(tenant,email)).fetchone()[0]==1)
  check('member welcome resources cover all membership kinds',db.execute('SELECT count(*) FROM "MemberResource" WHERE "TenantId"=%s AND "Published"',(tenant,)).fetchone()[0]==3)
  check('domain verification is never fabricated',db.execute('SELECT "Status" FROM "TenantDomain" WHERE "TenantId"=%s',(tenant,)).fetchone()[0]=='Pending Verification')
 command('--initialize-society-launch')
 with psycopg.connect(dbname=database,**dsn) as db:
  check('reinitialization preserves one receiving entity and three offers',db.execute('SELECT count(*) FROM "FinancialEntity" WHERE "TenantId"=%s',(tenant,)).fetchone()[0]==1 and db.execute('SELECT count(*) FROM "MembershipOffer" WHERE "TenantId"=%s',(tenant,)).fetchone()[0]==3)
 # A deployment without SMTP remains honest and preserves security emails for
 # later dispatch; signup never pretends that a provider has delivered them.
 server=subprocess.Popen(['dotnet',str(API_DLL),'--urls','http://127.0.0.1:5091'],cwd=ROOT/'backend',env={**env,'Society__DefaultTenantId':tenant,'ReverseProxy__KnownProxies__0':'127.0.0.1'},stdout=log,stderr=log)
 try:
  endpoint='http://127.0.0.1:5091/api/v1'
  for _ in range(100):
   try:urllib.request.urlopen('http://127.0.0.1:5091/health/live');break
   except Exception:time.sleep(.1)
  jar=http.cookiejar.CookieJar()
  opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
  def post(path,body):
   with opener.open(urllib.request.Request(endpoint+'/auth/csrf',headers={'X-Forwarded-Proto':'https'})) as response:token=json.loads(response.read())['token']
   # This isolated loopback harness stands in for the trusted TLS proxy. The
   # application still emits Secure cookies and sees the forwarded HTTPS scheme.
   for cookie in jar:cookie.secure=False
   with opener.open(urllib.request.Request(endpoint+path,method='POST',headers={'Content-Type':'application/json','X-CSRF-TOKEN':token,'X-Forwarded-Proto':'https'},data=json.dumps(body).encode())) as response:return json.loads(response.read())
  applicant='pending-email-'+suffix+'@example.test'
  result=post('/auth/signup',{'email':applicant,'password':secrets.token_urlsafe(24)+'!Aa1','fullName':'Pending Email Applicant','membershipKind':'Individual','societyId':tenant})
  check('production signup clearly states that email delivery is awaiting setup','awaiting email delivery setup' in result['message'])
  known=post('/auth/resend-verification',{'email':applicant});unknown=post('/auth/resend-verification',{'email':'unknown-'+suffix+'@example.test'})
  check('verification resend does not disclose whether an account exists',known==unknown)
  time.sleep(1)
  with psycopg.connect(dbname=database,**dsn) as db:
   rows=db.execute('SELECT "Status","Attempts","SentAt" FROM "Outbox" WHERE "Recipient"=%s',(applicant,)).fetchall()
   check('missing SMTP keeps verification email queued without consuming attempts or marking uncertain delivery',len(rows)==2 and all(row==('Queued',0,None) for row in rows))
 finally:server.terminate();server.wait(timeout=20)
finally:
 log.close();(ROOT/'tests/launch-initialization-results.json').write_text(json.dumps({'passed':len(checks),'checks':checks},indent=2),encoding='utf-8')
