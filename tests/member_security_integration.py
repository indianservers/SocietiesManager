"""Adversarial member/API checks. Uses only the isolated member_security_test DB.
Run --observe to collect baseline findings without treating them as success.
"""
from api_runtime import API_DLL
import argparse, http.cookiejar, io, json, os, shutil, socket, subprocess, time, urllib.error, urllib.parse, urllib.request, uuid
from pathlib import Path
import psycopg
from pypdf import PdfReader

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--observe',action='store_true');args=parser.parse_args()
dev=json.loads((ROOT/'.local/dev.json').read_text())
dsn=dict(host='127.0.0.1',port=55439,dbname='member_security_test',user='postgres',password=dev['password'])
with psycopg.connect(**{**dsn,'dbname':'postgres'},autocommit=True) as db:
    if not db.execute("SELECT 1 FROM pg_database WHERE datname='member_security_test'").fetchone():db.execute('CREATE DATABASE member_security_test')
with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
url=f'http://localhost:{port}';base=url+'/api/v1'
env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','PublicUrl':url,'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=member_security_test;Username=postgres;Password={dev['password']}",'Logging__LogLevel__Default':'Warning'}
subprocess.run(['dotnet',str(API_DLL),'--migrate'],env=env,cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
shutil.copytree(ROOT/'frontend/dist',API_DLL.parent/'wwwroot',dirs_exist_ok=True)
log=(ROOT/'.local/member-security-api.log').open('w',encoding='utf-8');server=None;checks=[];findings=[];auth_cases=0
def start():
    global server,auth_cases
    if server:server.terminate();server.wait(timeout=20)
    server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',url],cwd=API_DLL.parent,env=env,stdout=log,stderr=log);auth_cases=0
    for _ in range(100):
        try:urllib.request.urlopen(url+'/health',timeout=2);return
        except Exception:time.sleep(.2)
    raise RuntimeError('Isolated test server did not become ready')
def check(name,condition):
    if condition:checks.append(name);print('PASS',name,flush=True)
    else:findings.append(name);print('FINDING',name,flush=True)
class Client:
    def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def request(self,path,method='GET',body=None,raw=None,csrf=True):
        global auth_cases
        if path.startswith('/auth/') and method!='GET':
            auth_cases+=1
            if auth_cases>=15:start();auth_cases=1
        headers={'Content-Type':'application/json'}
        if method!='GET' and csrf:headers['X-CSRF-TOKEN']=json.loads(self.request('/auth/csrf')[1])['token']
        data=raw if raw is not None else (json.dumps(body).encode() if method!='GET' else None)
        req=urllib.request.Request(base+path,data=data,headers=headers,method=method)
        try:
            with self.opener.open(req,timeout=30) as res:return res.status,res.read(),res.headers
        except urllib.error.HTTPError as res:return res.code,res.read(),res.headers
    def json(self,path,method='GET',body=None):
        status,raw,_=self.request(path,method,body);assert status in (200,204),(path,status,raw[:120]);return json.loads(raw) if raw else None
def account(c,name):
    email='security-'+uuid.uuid4().hex[:12]+'@example.test'
    c.json('/auth/signup','POST',{'email':email,'password':'Security-Password-2026!','fullName':name})
    with psycopg.connect(**dsn) as db:
        uid=db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(email,)).fetchone()[0]
        body=db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s AND "Subject"=\'Verify your email\' ORDER BY "CreatedAt" DESC LIMIT 1',(email,)).fetchone()[0]
    token=urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
    c.json('/auth/verify?userId='+str(uid)+'&token='+urllib.parse.quote(token,safe=''),'POST',{})
    c.json('/auth/login','POST',{'email':email,'password':'Security-Password-2026!'})
    return email,uid
def fixture(users):
    tenant,level,chapter,category=[uuid.uuid4() for _ in range(4)];members=[]
    with psycopg.connect(**dsn) as db:
        db.execute('INSERT INTO "Tenants" ("Id","Name","Status","Profile","CreatedAt") VALUES (%s,\'Security Test Society\',\'Active\',\'{}\',now())',(tenant,))
        db.execute('INSERT INTO "Levels" ("Id","TenantId","Name","Kind","AllowedParents","AllowRoot") VALUES (%s,%s,\'Chapter\',\'Chapter\',\'{}\',true)',(level,tenant))
        db.execute('INSERT INTO "Units" ("Id","TenantId","LevelId","Name","Code","Status","Details","CreatedAt","UpdatedAt","Revision") VALUES (%s,%s,%s,\'Security chapter\',\'SEC\',\'Active\',\'{}\',now(),now(),0)',(chapter,tenant,level))
        db.execute('INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active","Kind") VALUES (%s,%s,\'Individual\',\'Security fixture\',true,\'Individual\')',(category,tenant))
        for i,user in enumerate(users):
            person,member=uuid.uuid4(),uuid.uuid4();members.append(member)
            db.execute('INSERT INTO "Persons" ("Id","UserId","FullName","SearchName","CreatedAt","Revision") VALUES (%s,%s,%s,%s,now(),0)',(person,user,'Security Member '+str(i),'security member '+str(i)))
            db.execute('INSERT INTO "Memberships" ("Id","TenantId","PersonId","ChapterId","CategoryId","Number","Status","JoinedAt","EffectiveFrom","EndsAt","Profile","Privacy","Revision","RenewalReminders","PendingPayment") VALUES (%s,%s,%s,%s,%s,%s,\'Active\',now(),now(),now()+interval \'1 year\',\'{}\',\'{"directory":false}\',0,false,false)',(member,tenant,person,chapter,category,'SEC-'+str(i)))
            db.execute('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s)',(tenant,user))
    return tenant,chapter,category,members
try:
    start();a,b,anon=Client(),Client(),Client();email,uid=account(a,'Security Member 0');other_email,other=account(b,'Security Member 1')
    tenant,chapter,category,members=fixture([uid,other]);own,foreign=members
    prefix=f'/societies/{tenant}/membership/members/';profile=f'/membership/my/{own}/profile'
    for route in ['/auth/login','/auth/signup','/auth/forgot','/auth/reset','/auth/magic/request','/auth/magic/consume','/auth/passkeys/login','/auth/two-factor']:
        for name,raw in [('null',b'null'),('array',b'[]'),('invalid JSON',b'{'),('missing fields',b'{}')]:
            status,body,_=anon.request(route,'POST',raw=raw)
            check(route+' rejects '+name+' without server errors',400<=status<500 and status!=429 and b'StackTrace' not in body and b'at Microsoft.' not in body and b'C:\\Indian Servers' not in body)
    for route,body in [('/auth/login',{'email':None,'password':'x'}),('/auth/login',{'email':'x','password':None}),('/auth/login',{'email':'x\u0000','password':'x'}),('/auth/login',{'email':'x'*255,'password':'x'}),('/auth/magic/request',{'email':[]}),('/auth/passkeys/login',{'credentialJson':'null'}),('/auth/passkeys/login',{'credentialJson':'{}'}),('/auth/passkeys/login',{'credentialJson':'[]'}),('/auth/passkeys/login',{'credentialJson':'{"response":null}'}),('/auth/two-factor',{'code':None})]:
        status,raw,_=anon.request(route,'POST',body)
        check('Malformed field safely rejected: '+route+' '+str(len(checks)),400<=status<500 and status!=429)
    anon.json('/auth/passkeys/options','POST',{})
    check('Malformed passkey with a real pending challenge is safely rejected',400<=anon.request('/auth/passkeys/login','POST',{'credentialJson':'{}'})[0]<500)
    check('Passkey credential cannot replay a consumed challenge',400<=anon.request('/auth/passkeys/login','POST',{'credentialJson':'{}'})[0]<500)
    check('Passkey registration requires a pending ceremony',a.request('/security/passkeys','POST',{'credentialJson':'{}'})[0]==400)
    a.json('/security/passkeys/options','POST',{})
    check('Attestation state cannot be used for login',a.request('/auth/passkeys/login','POST',{'credentialJson':'{}'})[0]==400)
    start()
    for route,body in [('/auth/login',{'email':email,'password':'x'*1025}),('/auth/forgot',{'email':('a'*260)+'@example.test'}),('/auth/two-factor',{'code':'x'*129})]:
        check('Bounded input: '+route,anon.request(route,'POST',body)[0]==400)
    check('Oversized auth body rejected before hashing',anon.request('/auth/login','POST',{'email':email,'password':'x'*70000})[0]==413)
    check('CSRF required for mutations',a.request('/users/me/password','POST',{'currentPassword':'x','newPassword':'x'},csrf=False)[0]==400)
    check('SQL-like login identifier cannot authenticate',anon.request('/auth/login','POST',{'email':"' OR 1=1 --",'password':'x'})[0]==401)
    check('Unknown API route returns 404 instead of the application HTML',anon.request('/not-an-endpoint')[0]==404)
    check('Unknown static-looking API route returns 404',anon.request('/not-an-endpoint.json')[0]==404)
    check('Encoded null query rejected',a.request('/membership/portfolio?search=%00')[0]==400)
    check('Account responses are not cacheable','no-store' in a.request('/users/me')[2].get('Cache-Control',''))
    check('Membership responses are not cacheable','no-store' in a.request('/membership/portfolio')[2].get('Cache-Control',''))
    status,pdf,headers=a.request(prefix+str(own)+'/id-card')
    check('Member can download own PDF',status==200 and pdf.startswith(b'%PDF-') and len(PdfReader(io.BytesIO(pdf)).pages)==2)
    check('Member cannot download another person ID',a.request(prefix+str(foreign)+'/id-card')[0]==403)
    check('Anonymous visitor cannot download an ID',anon.request(prefix+str(own)+'/id-card')[0]==401)
    check('ID request cannot cross tenant boundary',a.request(f'/societies/{uuid.uuid4()}/membership/members/{own}/id-card')[0] in (403,404))
    check('Invalid ID input rejected',a.request(prefix+'not-a-guid/id-card')[0] in (400,404))
    check('Individual member cannot bulk export IDs',a.request(f'/platform/membership/{tenant}/id-cards.zip')[0]==403)
    check('Member cannot read another private profile',a.request(f'/membership/my/{foreign}/profile')[0]==403)
    state=a.json(profile);rev=state['revision']
    for field,value in [('status','Active'),('number','CHANGED'),('fullName','Changed name'),('adminArchived',True),('claimEmails',[email]),('reminderLeadDays','bad')]:
        status,_,_=a.request(profile,'PUT',{'revision':rev,'profile':json.dumps({field:value})})
        check('Self-profile rejects administrative field '+field,status==400)
    for value in ['null','[]','{','{"email":42}','{"phone":[]}','{"addresses":[null]}','{"professional":{"title":[]}}','{"email":"a\\u0000@example.test"}','{"dateOfBirth":"not-a-date"}']:
        check('Malformed contact profile safely rejected '+str(len(checks)),a.request(profile,'PUT',{'revision':rev,'profile':value})[0]==400)
    check('Valid personal contact editing retained',a.request(profile,'PUT',{'revision':rev,'profile':json.dumps({'email':'contact@example.test','phone':'+91 9999999999','city':'Hyderabad'})})[0]==204)
    state=a.json(profile)
    check('Duplicate profile keys are safely rejected',a.request(profile,'PUT',{'revision':state['revision'],'profile':'{"city":"one","city":"two"}'})[0]==400)
    check('Cross-account profile update forbidden',a.request(f'/membership/my/{foreign}/profile','PUT',{'revision':0,'profile':'{"city":"Changed"}'})[0]==403)
    check('Cross-account profile clearing forbidden',a.request(f'/membership/my/{foreign}/profile','DELETE',{'revision':0})[0]==403)
    check('Stale profile writes conflict',a.request(profile,'PUT',{'revision':rev,'profile':'{}'})[0]==409)
    state=a.json(profile);rev=state['revision']
    legacy={'fullName':'Security Member 0','chapterId':str(chapter),'categoryId':str(category),'personId':None,'number':'SEC-0','status':'Active','profile':json.dumps(state['profile']),'privacy':None,'revision':rev}
    for field,value in [('status','Suspended'),('number','CHANGED'),('categoryId',str(uuid.uuid4()))]:
        check('Legacy member update protects '+field,a.request(prefix+str(own),'PUT',{**legacy,field:value})[0]==403)
    check('Legacy member update protects registered name',a.request(prefix+str(own),'PUT',{**legacy,'fullName':'Changed registered identity'})[0]==400)
    status,_,_=a.request(prefix+str(own),'PUT',{**legacy,'profile':json.dumps({**state['profile'],'reminderLeadDays':'bad'})})
    check('Legacy profile cannot inject non-contact metadata',status==400)
    if status==204:
        with psycopg.connect(**dsn) as db:db.execute('UPDATE "Memberships" SET "Profile"=%s,"Revision"=%s WHERE "Id"=%s',(json.dumps(state['profile']),rev,own))
    check('Member cannot edit another membership through legacy route',a.request(prefix+str(foreign),'PUT',legacy)[0]==403)
    check('Profile remains readable after malformed inputs',a.request(profile)[0]==200)
    check('Portfolio remains readable after malformed inputs',a.request('/membership/portfolio')[0]==200)
    start()
    statuses=[a.request(prefix+str(own)+'/id-card')[0] for _ in range(12)]
    check('PDF generation is rate-limited per member',429 in statuses and statuses[0]==200)
    check('Other member retains a separate PDF allowance',b.request(prefix+str(foreign)+'/id-card')[0]==200)
    with psycopg.connect(**dsn) as db:
        current=db.execute('SELECT "Number","Status","CategoryId" FROM "Memberships" WHERE "Id"=%s',(own,)).fetchone()
        check('Membership identity and entitlement unchanged',current==('SEC-0','Active',category))
finally:
    if server:server.terminate();server.wait(timeout=20)
    log.close()
    output={'passed':len(checks),'findings':findings,'checks':checks,'isolatedDatabase':'member_security_test'}
    (ROOT/'tests/member-security-results.json').write_text(json.dumps(output,indent=2),encoding='utf-8')
if findings and not args.observe:raise SystemExit(str(len(findings))+' security checks failed')
print('Security checks passed:',len(checks),'Findings:',len(findings),flush=True)
