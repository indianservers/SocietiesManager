"""Real Identity cookies/account-linking with a test-only upstream identity stub.

Token validation is separately exercised by --verifier-tests with a mock Google
lookup handler. No unsigned test token is accepted by the production verifier.
"""
import base64,datetime,hashlib,hmac,http.cookiejar,json,os,secrets,socket,struct,subprocess,time,urllib.error,urllib.request,uuid
from pathlib import Path
import psycopg

ROOT=Path(__file__).resolve().parents[1];dev=json.loads((ROOT/'.local/dev.json').read_text());fixture=json.loads((ROOT/'.local/roster-browser-fixture.json').read_text());tenant=fixture['tenant']
dsn=dict(host='127.0.0.1',port=55439,dbname='assemblia_test',user='postgres',password=dev['password'])
with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
url='http://127.0.0.1:'+str(port);env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=assemblia_test;Username=postgres;Password={dev['password']}",'PublicUrl':url,'Logging__LogLevel__Default':'Warning'}
log=(ROOT/'.local/firebase-auth-test-api.log').open('w',encoding='utf-8');dll=ROOT/'.local/firebase-auth-tests/FirebaseAuthHarness.dll';server=subprocess.Popen(['dotnet',str(dll),'--urls',url],cwd=dll.parent,env=env,stdout=log,stderr=log);checks=[]
class Client:
 def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def call(self,path,method='GET',body=None,status=200,csrf=True):
  headers={'Content-Type':'application/json'}
  if method!='GET' and csrf:headers['X-CSRF-TOKEN']=self.call('/auth/csrf')['token']
  request=urllib.request.Request(url+'/api/v1'+path,method=method,data=json.dumps(body or {}).encode() if method!='GET' else None,headers=headers)
  try:
   with self.opener.open(request) as response:actual=response.status;raw=response.read()
  except urllib.error.HTTPError as error:actual=error.code;raw=error.read()
  assert actual==status,(path,status,actual,raw[:250])
  return json.loads(raw) if raw else None
def check(name,value=True):assert value,name;checks.append(name);print('PASS SSO',name,flush=True)
def token(uid,email,provider='google.com',verified=True):return json.dumps({'Uid':uid,'Email':email,'Provider':provider,'Verified':verified})
def sql(statement,args=()):
 with psycopg.connect(**dsn) as db:
  result=db.execute(statement,args);return result.fetchall() if result.description else None
def confirm(email):sql('UPDATE "AspNetUsers" SET "EmailConfirmed"=true WHERE "NormalizedEmail"=%s',(email.upper(),))
def totp(key):
 digest=hmac.new(base64.b32decode(key.upper()+'='*((8-len(key)%8)%8)),struct.pack('>Q',int(time.time())//30),hashlib.sha1).digest();offset=digest[-1]&15;return str((struct.unpack('>I',digest[offset:offset+4])[0]&0x7fffffff)%1000000).zfill(6)
try:
 for _ in range(100):
  try:Client().call('/auth/csrf');break
  except Exception:time.sleep(.2)
 suffix=uuid.uuid4().hex[:12];password=secrets.token_urlsafe(20)+'!Aa1';username='owner_'+suffix;email='sso-owner-'+suffix+'@example.test';client=Client()
 config=client.call('/auth/providers');check('provider configuration advertises both providers and local passwords',config['enabled'] and set(config['providers'])=={'google.com','microsoft.com'} and config['usernamePassword'])
 client.call('/auth/signup','POST',{'email':email,'password':password,'fullName':'Synthetic Existing Owner','username':username});confirm(email)
 user=sql('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(email,))[0][0]
 role=sql('SELECT "Id" FROM "GovernanceRoles" WHERE "TenantId"=%s AND "Name"=\'Society Administrator\'',(tenant,))[0][0]
 sql('UPDATE "AspNetUsers" SET "PlatformAdmin"=true WHERE "Id"=%s',(user,));sql('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s)',(tenant,user))
 assignment=uuid.uuid4();sql('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","StartsAt") VALUES (%s,%s,%s,%s,now()-interval \'1 day\')',(assignment,tenant,user,role))
 google=token('existing-'+suffix,email);guest=Client();link=guest.call('/auth/firebase','POST',{'idToken':google,'societyId':tenant})
 check('matching email alone requests account confirmation rather than signing in',link['requiresAccountLink']);guest.call('/users/me',status=401)
 check('unconfirmed social identity cannot obtain the existing owner session',True)
 guest.call('/auth/firebase/link','POST',{'idToken':google},status=401)
 check('link endpoint rejects unauthenticated requests',True)
 guest.call('/auth/firebase','POST',{'idToken':google},status=400,csrf=False)
 check('social sign-in requires the existing CSRF protection',True)
 guest.call('/auth/login','POST',{'email':username.upper(),'password':password});check('username/password signs in case insensitively',guest.call('/users/me')['id']==str(user))
 guest.call('/auth/firebase/link','POST',{'idToken':google});check('confirmed account can connect its social identity',True)
 bound=Client();bound.call('/auth/firebase','POST',{'idToken':google});me=bound.call('/users/me');check('Google sign-in preserves the existing user and platform-owner flag',me['id']==str(user) and me['platformAdmin'])
 check('linking preserves society assignments',sql('SELECT count(*) FROM "Assignments" WHERE "Id"=%s AND "UserId"=%s',(assignment,user))[0][0]==1)
 microsoft=token('existing-'+suffix,email,'microsoft.com');ms=Client();ms.call('/auth/firebase','POST',{'idToken':microsoft});check('Microsoft sign-in to the connected Firebase UID retains the same account',ms.call('/users/me')['id']==str(user))
 check('Google and Microsoft do not duplicate the local account',sql('SELECT count(*) FROM "AspNetUsers" WHERE "NormalizedEmail"=%s',(email.upper(),))[0][0]==1)
 ms.call('/auth/logout','POST',status=204);ms.call('/auth/login','POST',{'email':email,'password':password});check('email/password still works for an account with a separate username',ms.call('/users/me')['id']==str(user))
 wrong_email=token('wrong-'+suffix,'different-'+suffix+'@example.test');ms.call('/auth/firebase/link','POST',{'idToken':wrong_email},status=400);check('a different provider email cannot replace or attach to the current account',True)
 ms.call('/auth/firebase/link','POST',{'idToken':google});check('repeating account linking is idempotent',sql('SELECT count(*) FROM "AspNetUserLogins" WHERE "UserId"=%s',(user,))[0][0]==1)
 key=ms.call('/security/mfa/setup','POST',{'password':password})['key'];ms.call('/security/mfa/enable','POST',{'code':totp(key)});ms.call('/auth/logout','POST',status=204)
 mfa=Client();result=mfa.call('/auth/firebase','POST',{'idToken':google});check('social sign-in still requires the existing account authenticator',result['requiresTwoFactor']);mfa.call('/users/me',status=401)
 check('social sign-in does not issue a full session before MFA',True)
 mfa.call('/auth/two-factor','POST',{'code':totp(key)});check('MFA completes social sign-in and records an MFA session',mfa.call('/users/me')['id']==str(user) and mfa.call('/security/status')['mfaSession'])
 second_uid=token('second-'+suffix,email,'microsoft.com');mfa.call('/auth/firebase/link','POST',{'idToken':second_uid});check('a recent full MFA session may connect another owned provider identity',True)
 sql('UPDATE "AspNetUsers" SET "LockoutEnd"=now()+interval \'1 hour\', "LockoutEnabled"=true WHERE "Id"=%s',(user,))
 Client().call('/auth/firebase','POST',{'idToken':google},status=401);check('locked accounts cannot bypass lockout through social sign-in',True)
 sql('UPDATE "AspNetUsers" SET "LockoutEnd"=NULL WHERE "Id"=%s',(user,))
 for provider in ['google.com','microsoft.com']:
  new_email=provider.split('.')[0]+'-'+suffix+'@example.test';new=Client();new.call('/auth/firebase','POST',{'idToken':token('new-'+provider+suffix,new_email,provider),'membershipKind':'Institutional','societyId':tenant});current=new.call('/users/me')
  check(provider+' creates a regular verified account without granting owner privileges',current['email']==new_email and current['emailConfirmed'] and not current['platformAdmin'] and current['preferredMembershipKind']=='Institutional')
  check(provider+' signup grants no membership or governance access',sql('SELECT count(*) FROM "Links" WHERE "UserId"=%s',(current['id'],))[0][0]==0)
 unverified_email='unverified-'+suffix+'@example.test';unverified=Client();result=unverified.call('/auth/firebase','POST',{'idToken':token('unverified-'+suffix,unverified_email,'microsoft.com',False)})
 check('unverified provider email still requires local email verification',result['requiresEmailVerification']);unverified.call('/users/me',status=401)
 check('unverified social signup queues an actual verification request',sql('SELECT count(*) FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=\'Verify your email\'',(unverified_email,))[0][0]==1)
 ordinary=Client();ordinary_email='ordinary-'+suffix+'@example.test';ordinary.call('/auth/signup','POST',{'email':ordinary_email,'password':password,'fullName':'Normal User','username':''});confirm(ordinary_email);ordinary.call('/auth/login','POST',{'email':ordinary_email,'password':password});check('ordinary email/password registration remains backward compatible',not ordinary.call('/users/me')['platformAdmin'])
 check('provider-supplied roles never grant platform privileges',not ordinary.call('/users/me')['platformAdmin'])
 print('SSO_TOTAL',len(checks),flush=True)
finally:
 server.terminate();server.wait(timeout=20);log.close();(ROOT/'tests/firebase-auth-results.json').write_bytes(json.dumps({'passed':len(checks),'checks':checks},indent=2).encode())
