"""Isolated real Identity/PostgreSQL checks. Upstream provider identities are stubbed only in the dedicated harness."""
import base64,hashlib,hmac,http.cookiejar,json,os,re,shutil,socket,struct,subprocess,time,urllib.error,urllib.request,uuid
from pathlib import Path
from urllib.parse import parse_qs,urlparse
from concurrent.futures import ThreadPoolExecutor
import psycopg
from playwright.sync_api import sync_playwright,expect
ROOT=Path(__file__).resolve().parents[1];dev=json.loads((ROOT/'.local/dev.json').read_text());suffix=uuid.uuid4().hex[:10];checks=[]
dsn=dict(host='127.0.0.1',port=55439,dbname='auth_security_test',user='postgres',password=dev['password'])
with psycopg.connect(**{**dsn,'dbname':'postgres'},autocommit=True) as db:
 if not db.execute("select 1 from pg_database where datname='auth_security_test'").fetchone():db.execute('CREATE DATABASE auth_security_test')
with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
URL='http://localhost:'+str(port);env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=auth_security_test;Username=postgres;Password={dev['password']}",'PublicUrl':URL,'Logging__LogLevel__Default':'Warning'}
subprocess.run(['dotnet',str(ROOT/'.local/auth-security-local/Society.Api.dll'),'--migrate'],env=env,cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
shutil.copytree(ROOT/'frontend/dist',ROOT/'.local/auth-security-harness/wwwroot',dirs_exist_ok=True)
log=(ROOT/'.local/auth-security-tests.log').open('w',encoding='utf-8');server=subprocess.Popen(['dotnet',str(ROOT/'.local/auth-security-harness/FirebaseAuthHarness.dll'),'--urls',URL],cwd=ROOT/'.local/auth-security-harness',env=env,stdout=log,stderr=log,creationflags=subprocess.CREATE_NO_WINDOW)
def sql(q,args=()):
 with psycopg.connect(**dsn) as db:
  r=db.execute(q,args);return r.fetchall() if r.description else None
class Client:
 def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def call(self,path,method='GET',body=None,status=200,csrf=True):
  headers={'Content-Type':'application/json'}
  if method!='GET' and csrf:headers['X-CSRF-TOKEN']=self.call('/auth/csrf')['token']
  request=urllib.request.Request(URL+'/api/v1'+path,method=method,data=json.dumps(body or {}).encode() if method!='GET' else None,headers=headers)
  try:
   with self.opener.open(request) as r:actual=r.status;raw=r.read()
  except urllib.error.HTTPError as e:actual=e.code;raw=e.read()
  assert actual==status,(path,status,actual,raw[:250]);return json.loads(raw) if raw else None
PASSWORD='AuthCheck!'+uuid.uuid4().hex+'A9'
def create(label,mobile=None):
 email=label+'-'+suffix+'@example.invalid';c=Client();c.call('/auth/signup','POST',{'email':email,'password':PASSWORD,'fullName':'Synthetic '+label,'phoneNumber':mobile});sql('UPDATE "AspNetUsers" SET "EmailConfirmed"=true WHERE "NormalizedEmail"=%s',(email.upper(),));uid=str(sql('SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail"=%s',(email.upper(),))[0][0]);return email,uid
def login(c,email,password=PASSWORD):return c.call('/auth/login','POST',{'email':email,'password':password})
def otp(c,email):
 r=c.call('/auth/email-otp/request','POST',{'email':email});row=sql('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=%s ORDER BY "CreatedAt" DESC LIMIT 1',(email,'Your AIMER sign-in code'))
 return r,re.search(r'code is (\d{6})',row[0][0]).group(1) if row else None
def age(email):sql('UPDATE "EmailLoginChallenge" SET "CreatedAt"="CreatedAt"-interval \'65 seconds\' WHERE "IdentifierHash"=%s',(hashlib.sha256(email.upper().encode()).hexdigest().upper(),))
def token(uid,email='',provider='google.com',phone=None,verified=True):return json.dumps({'Uid':uid,'Email':email,'Verified':verified,'Provider':provider,'PhoneNumber':phone})
def totp(key):
 d=hmac.new(base64.b32decode(key.upper()+'='*((8-len(key)%8)%8)),struct.pack('>Q',int(time.time())//30),hashlib.sha1).digest();o=d[-1]&15;return str((struct.unpack('>I',d[o:o+4])[0]&0x7fffffff)%1000000).zfill(6)
def check(s):checks.append(s);print('PASS',s,flush=True)
try:
 for _ in range(100):
  try:Client().call('/auth/csrf');break
  except Exception:time.sleep(.2)
 email,uid=create('otp');other,otherid=create('other');unknown='missing-'+suffix+'@example.invalid';c=Client();baseline=sql('SELECT count(*) FROM "AspNetUsers"')[0][0]
 r,code=otp(c,email);missing=c.call('/auth/email-otp/request','POST',{'email':unknown});assert r['message']==missing['message'] and set(r)==set(missing);assert sql('SELECT count(*) FROM "AspNetUsers"')[0][0]==baseline
 assert otp(c,email)[0]['challengeId']==r['challengeId'];assert sql('SELECT count(*) FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=%s',(email,'Your AIMER sign-in code'))[0][0]==1
 assert sql('SELECT "ProtectedCode" FROM "EmailLoginChallenge" WHERE "Id"=%s',(r['challengeId'],))[0][0]!=code
 c.call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':code});assert c.call('/users/me')['id']==uid;c.call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':code},401);check('OTP opens the existing account, conceals unknown accounts, throttles resend and rejects replay')
 age(email);r,code=otp(c,email);wrong='000000' if code!='000000' else '111111'
 for _ in range(5):c.call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':wrong},401)
 c.call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':code},401);assert not sql('SELECT "LockoutEnd" FROM "AspNetUsers" WHERE "Id"=%s',(uid,))[0][0];check('Five incorrect OTP attempts invalidate the challenge without locking the account')
 age(email);r,code=otp(c,email);sql('UPDATE "EmailLoginChallenge" SET "ExpiresAt"=now()-interval \'1 second\' WHERE "Id"=%s',(r['challengeId'],));c.call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':code},401)
 age(email);r,code=otp(c,email)
 def race(_):
  try:Client().call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':code});return 200
  except AssertionError as e:return e.args[0][2]
 with ThreadPoolExecutor(max_workers=2) as pool:assert sorted(pool.map(race,range(2)))==[200,401]
 check('Expired OTP and concurrent reuse are rejected')
 recovery=Client();login(recovery,email);old=Client();login(old,email);age(email);r,code=otp(c,email);c.call('/auth/magic/request','POST',{'email':email});c.call('/auth/forgot','POST',{'email':email});body=sql('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=%s ORDER BY "CreatedAt" DESC LIMIT 1',(email,'Reset your password'))[0][0];link=next(line for line in body.splitlines() if '/reset#' in line);params=parse_qs(urlparse(link).fragment);assert not urlparse(link).query
 newpass='Changed!'+uuid.uuid4().hex+'Aa9';request={'email':params['email'][0],'token':params['token'][0],'password':newpass};recovery.call('/auth/reset','POST',request);old.call('/users/me',status=401);c.call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':code},401);recovery.call('/auth/reset','POST',request,400);Client().call('/auth/login','POST',{'email':email,'password':PASSWORD},401);login(recovery,email,newpass);check('Password reset uses a fragment link, invalidates old passwords, sessions and OTPs, and rejects token replay')
 socialEmail,socialId=create('linked');owner=Client();login(owner,socialEmail);before=sql('SELECT count(*) FROM "AspNetUsers"')[0][0];unconnected=token('unconnected-'+suffix,'new-provider-'+suffix+'@example.invalid');assert Client().call('/auth/firebase','POST',{'idToken':unconnected})['requiresAccountLink'];assert sql('SELECT count(*) FROM "AspNetUsers"')[0][0]==before;knownToken=token('known-'+suffix,socialEmail);google=Client();google.call('/auth/firebase','POST',{'idToken':knownToken});assert google.call('/users/me')['id']==socialId;assert sql('SELECT count(*) FROM "AspNetUsers"')[0][0]==before
 owner.call('/auth/firebase/link','POST',{'idToken':knownToken});external=Client();external.call('/auth/firebase','POST',{'idToken':knownToken});assert external.call('/users/me')['id']==socialId
 microsoft=token('ms-'+suffix,'different-'+suffix+'@example.invalid','microsoft.com');owner.call('/auth/firebase/link','POST',{'idToken':microsoft});ms=Client();ms.call('/auth/firebase','POST',{'idToken':microsoft});assert ms.call('/users/me')['id']==socialId
 phone='+1999'+str(int(suffix[:6],16)).zfill(7);phoneToken=token('phone-'+suffix,provider='phone',phone=phone);assert Client().call('/auth/firebase','POST',{'idToken':phoneToken})['requiresAccountLink'];owner.call('/auth/firebase/link','POST',{'idToken':phoneToken});sms=Client();sms.call('/auth/firebase','POST',{'idToken':phoneToken});assert sms.call('/users/me')['id']==socialId;mobilePassword=Client();login(mobilePassword,phone);assert mobilePassword.call('/users/me')['id']==socialId
 attacker=Client();login(attacker,other);attacker.call('/auth/firebase/link','POST',{'idToken':phoneToken},400);Client().call('/auth/firebase/link','POST',{'idToken':microsoft},401)
 profile=owner.call('/users/me/personal-profile');owner.call('/users/me/personal-profile','PUT',{'fullName':profile['fullName'],'designation':'Faculty','phone':'+18885550100','revision':profile['revision']},204);methods=owner.call('/security/methods');assert methods['phoneVerified'] and methods['phoneNumber']==phone;Client().call('/auth/login','POST',{'email':'+18885550100','password':PASSWORD},401)
 assert sql('SELECT count(*) FROM "AspNetUsers"')[0][0]==before;check('Google, Microsoft, verified SMS and verified-mobile password login resolve to one existing account; cross-account linking and unverified phone login are blocked')
 owner.call('/auth/firebase/link','POST',{'idToken':token('unverified-'+suffix,'unverified-'+suffix+'@example.invalid',verified=False)},400)
 signup=Client();newIdentity=token('signup-'+suffix,'signup-provider-'+suffix+'@example.invalid');signup.call('/auth/firebase','POST',{'idToken':newIdentity,'createAccount':True});newid=signup.call('/users/me')['id'];repeat=Client();repeat.call('/auth/firebase','POST',{'idToken':newIdentity});assert repeat.call('/users/me')['id']==newid;check('Explicit provider signup works; unconnected provider login does not silently create an account')
 mfaEmail,mfaId=create('mfa');mfa=Client();login(mfa,mfaEmail);key=mfa.call('/security/mfa/setup','POST',{'password':PASSWORD})['key'];mfa.call('/security/mfa/enable','POST',{'code':totp(key)});mfaOtp=Client();r,code=otp(mfaOtp,mfaEmail);assert mfaOtp.call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':code})['requiresTwoFactor'];mfaOtp.call('/users/me',status=401);mfaOtp.call('/auth/two-factor','POST',{'code':totp(key)});assert mfaOtp.call('/users/me')['id']==mfaId;check('Email OTP cannot bypass MFA')
 # Approved manual memberships can activate through OTP without a pre-created login.
 tenant,level,chapter,category,person,member=[str(uuid.uuid4()) for _ in range(6)];approvedEmail='approved-'+suffix+'@example.invalid'
 sql("INSERT INTO \"Tenants\" (\"Id\",\"Name\",\"Status\",\"Profile\",\"CreatedAt\") VALUES (%s,'OTP fixture','Active','{}',now())",(tenant,))
 sql("INSERT INTO \"Levels\" (\"Id\",\"TenantId\",\"Name\",\"Kind\",\"AllowedParents\",\"AllowRoot\") VALUES (%s,%s,'Chapter','Chapter','{}',true)",(level,tenant))
 sql("INSERT INTO \"Units\" (\"Id\",\"TenantId\",\"LevelId\",\"Name\",\"Code\",\"Status\",\"Details\",\"CreatedAt\",\"UpdatedAt\",\"Revision\") VALUES (%s,%s,%s,'Chapter',%s,'Active','{}',now(),now(),1)",(chapter,tenant,level,"T"+suffix[:7].upper()))
 sql("INSERT INTO \"MembershipCategories\" (\"Id\",\"TenantId\",\"Name\",\"Description\",\"Active\",\"Kind\") VALUES (%s,%s,'Individual','',true,'Individual')",(category,tenant))
 sql("INSERT INTO \"Persons\" (\"Id\",\"FullName\",\"SearchName\",\"CreatedAt\",\"Revision\") VALUES (%s,'Approved member','approved member',now(),1)",(person,))
 sql("INSERT INTO \"Memberships\" (\"Id\",\"TenantId\",\"PersonId\",\"ChapterId\",\"CategoryId\",\"Number\",\"Status\",\"JoinedAt\",\"EffectiveFrom\",\"Profile\",\"Privacy\",\"Revision\") VALUES (%s,%s,%s,%s,%s,%s,'Active',now(),now()-interval '1 day','{}','{}',1)",(member,tenant,person,chapter,category,suffix))
 sql("INSERT INTO \"MembershipHistories\" (\"Id\",\"TenantId\",\"MembershipId\",\"ActorId\",\"Action\",\"Details\",\"At\") VALUES (%s,%s,%s,%s,'membership.manual.claim.approved',%s::jsonb,now())",(str(uuid.uuid4()),tenant,member,uid,json.dumps({'claimEmails':[approvedEmail.upper(),('approved-google-'+suffix+'@example.invalid').upper()]})))
 approvedClient=Client();r,code=otp(approvedClient,approvedEmail);assert code;newUser=sql('SELECT "Id","EmailConfirmed" FROM "AspNetUsers" WHERE "NormalizedEmail"=%s',(approvedEmail.upper(),))[0];assert not newUser[1] and str(newUser[0])!='00000000-0000-0000-0000-000000000000'
 approvedClient.call('/users/me',status=401);approvedClient.call('/auth/email-otp/verify','POST',{'challengeId':r['challengeId'],'code':code});assert approvedClient.call('/users/me')['id']==str(newUser[0]);assert sql('SELECT "EmailConfirmed" FROM "AspNetUsers" WHERE "Id"=%s',(newUser[0],))[0][0];assert sql('SELECT count(*) FROM "Links" WHERE "UserId"=%s AND "TenantId"=%s',(newUser[0],tenant))[0][0]==1
 approvedGoogle=Client();approvedGoogle.call('/auth/firebase','POST',{'idToken':token('approved-google-'+suffix,'approved-google-'+suffix+'@example.invalid')});assert approvedGoogle.call('/users/me')['email']=='approved-google-'+suffix+'@example.invalid'
 unverifiedGoogle=Client();assert unverifiedGoogle.call('/auth/firebase','POST',{'idToken':token('bad-google-'+suffix,other,verified=False)})['requiresAccountLink'];unverifiedGoogle.call('/users/me',status=401)
 googleMfa=Client();assert googleMfa.call('/auth/firebase','POST',{'idToken':token('mfa-google-'+suffix,mfaEmail)})['requiresTwoFactor'];googleMfa.call('/users/me',status=401)
 check('Approved membership first OTP creates an unverified account, verifies only after code proof and links membership; first Google login retains MFA')
 fixture=owner.call('/test/provisioning','POST');prepared=sql('SELECT count(*) FROM "AspNetUsers"')[0][0]
 for account in fixture['accounts']:
  em=account['email'];row=sql('SELECT "Id","EmailConfirmed","PasswordHash","PlatformAdmin" FROM "AspNetUsers" WHERE "NormalizedEmail"=%s',(em.upper(),))[0];assert not row[1] and row[2] is None and not row[3]
  memberClient=Client();challenge,proof=otp(memberClient,em);assert proof;memberClient.call('/auth/email-otp/verify','POST',{'challengeId':challenge['challengeId'],'code':proof});assert memberClient.call('/users/me')['id']==str(row[0]);assert sql('SELECT count(*) FROM "Links" WHERE "UserId"=%s AND "TenantId"=%s',(row[0],fixture['tenantId']))[0][0]==1
  if account['kind']=='Roster':assert not sql("SELECT 1 FROM \"MembershipHistories\" WHERE \"MembershipId\"=%s AND \"Action\"='membership.manual.claim.approved' AND (\"Details\"->'claimEmails') ? %s",(account['memberId'],em.upper()))
 legacy=owner.call('/test/provisioning','POST');legacyEmail=legacy['accounts'][0]['email'];sql('DELETE FROM "AspNetUsers" WHERE "NormalizedEmail"=%s',(legacyEmail.upper(),));beforeBackfill=sql('SELECT count(*) FROM "AspNetUsers"')[0][0]
 first=owner.call('/test/provisioning/backfill','POST');second=owner.call('/test/provisioning/backfill','POST');assert first['accountsCreated']==1 and second['accountsCreated']==0;assert sql('SELECT count(*) FROM "AspNetUsers"')[0][0]==beforeBackfill+1
 check('Saving Individual, Institutional, Corporate and roster members prepares passwordless accounts immediately; first OTP succeeds and backfill is idempotent without roster privilege escalation')
 with sync_playwright() as pw:
  browser=pw.chromium.launch();ctx=browser.new_context(viewport={'width':1366,'height':900});p=ctx.new_page();expect.set_options(timeout=30000);age(socialEmail);p.goto(URL+'/');p.get_by_role('button',name='Sign in with email OTP',exact=True).click();p.get_by_label('Email address',exact=True).fill(socialEmail);p.get_by_role('button',name='Send email code',exact=True).click();expect(p.get_by_label('Email sign-in code',exact=True)).to_be_visible();livecode=re.search(r'code is (\d{6})',sql('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=%s ORDER BY "CreatedAt" DESC LIMIT 1',(socialEmail,'Your AIMER sign-in code'))[0][0]).group(1);p.screenshot(path=str(ROOT/'tests/auth-email-otp-desktop.png'),full_page=True);p.get_by_label('Email sign-in code',exact=True).fill(livecode);p.get_by_role('button',name='Verify code & sign in',exact=True).click();p.wait_for_url('**/app/**');assert ctx.request.get(URL+'/api/v1/users/me').json()['id']==socialId
  p.goto(URL+'/app/account');expect(p.get_by_label('Full name',exact=True)).to_be_visible();p.get_by_label('Full name',exact=True).fill('Unsent draft');p.evaluate("window.dispatchEvent(new Event('member:session-expired'))");expect(p.get_by_role('heading',name='Sign in to continue',exact=True)).to_be_visible();dialog=p.get_by_role('dialog',name='Sign in to continue',exact=True);dialog.get_by_label('Email or username',exact=True).fill(other);dialog.get_by_label('Password',exact=True).fill(PASSWORD);dialog.get_by_role('button',name='Sign in',exact=True).click();expect(p.get_by_role('button',name='Close open work & switch account',exact=True)).to_be_visible();assert ctx.request.get(URL+'/api/v1/users/me').status==401
  p.on('dialog',lambda d:d.accept());p.get_by_role('button',name='Close open work & switch account',exact=True).click();p.wait_for_url(URL+'/');expect(p.get_by_role('heading',name='Welcome back',exact=True)).to_be_visible();check('Browser OTP sign-in works and switching away from open work requires an explicit safe action')
  p.set_viewport_size({'width':390,'height':844});p.get_by_role('button',name='Sign in with mobile SMS',exact=True).click();expect(p.get_by_label('Verified mobile number',exact=True)).to_be_visible();assert p.evaluate('document.documentElement.scrollWidth<=innerWidth');p.screenshot(path=str(ROOT/'tests/auth-mobile-sms.png'),full_page=True);browser.close()
 (ROOT/'tests/auth-security-results.json').write_text(json.dumps({'checks':checks,'externalProviderMode':'Test-only verifier stub; production verifier is checked separately'},indent=2))
finally:server.terminate();server.wait(timeout=20);log.close()
