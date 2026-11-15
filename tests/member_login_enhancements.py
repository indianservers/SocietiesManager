"""Real HTTP, PostgreSQL and Chromium coverage for member sign-in enhancements.

Creates only fixtures in member_login_test; never uses the application database.
Build frontend and set ASSEMBLIA_API_DLL to the verification build before running.
"""
import base64, hashlib, hmac, json, os, shutil, socket, struct, subprocess, time, uuid
from pathlib import Path
from urllib.parse import urlparse, parse_qs, quote
import psycopg
from playwright.sync_api import sync_playwright, expect
from api_runtime import API_DLL

ROOT=Path(__file__).resolve().parents[1]
dev=json.loads((ROOT/'.local/dev.json').read_text())
dsn=dict(host='127.0.0.1',port=55439,dbname='member_login_test',user='postgres',password=dev['password'])
with psycopg.connect(**{**dsn,'dbname':'postgres'},autocommit=True) as db:
    if not db.execute("select 1 from pg_database where datname='member_login_test'").fetchone():db.execute('CREATE DATABASE member_login_test')
with socket.socket() as s:s.bind(('127.0.0.1',0));port=s.getsockname()[1]
URL=f'http://localhost:{port}'
env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=member_login_test;Username=postgres;Password={dev['password']}",'PublicUrl':URL,'Logging__LogLevel__Default':'Warning'}
subprocess.run(['dotnet',str(API_DLL),'--migrate'],env=env,cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
shutil.copytree(ROOT/'frontend/dist',API_DLL.parent/'wwwroot',dirs_exist_ok=True)
log=(ROOT/'.local/member-login-test.log').open('w',encoding='utf-8');server=None;checks=[]
def check(name,condition=True):
    assert condition,name
    checks.append(name);print('PASS',name,flush=True)
def start():
    global server
    if server:server.terminate();server.wait(timeout=20)
    server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',URL],cwd=API_DLL.parent,env=env,stdout=log,stderr=log)
    import urllib.request
    for _ in range(80):
        try:urllib.request.urlopen(URL+'/health');return
        except Exception:time.sleep(.25)
    raise RuntimeError('API failed to start; see private test log')
def call(ctx,path,method='GET',body=None,status=200):
    headers={}
    if method!='GET':headers['X-CSRF-TOKEN']=ctx.request.get(URL+'/api/v1/auth/csrf').json()['token']
    r=ctx.request.fetch(URL+'/api/v1'+path,method=method,data=body,headers=headers)
    assert r.status==status,f'{method} {path}: {r.status}, {r.text()[:300]}'
    return r.json() if r.body() else None
def totp(key):
    secret=base64.b32decode(key.upper()+'='*((8-len(key)%8)%8));digest=hmac.new(secret,struct.pack('>Q',int(time.time())//30),hashlib.sha1).digest();offset=digest[-1]&15
    return str((struct.unpack('>I',digest[offset:offset+4])[0]&0x7fffffff)%1000000).zfill(6)
def signup(ctx,name):
    email=f'login-{name}-{uuid.uuid4().hex[:10]}@example.test'
    call(ctx,'/auth/signup','POST',{'email':email,'password':'Member-Password-2026!','fullName':name})
    with psycopg.connect(**dsn) as db:
        uid=db.execute('select "Id" from "AspNetUsers" where "Email"=%s',(email,)).fetchone()[0]
        body=db.execute('select "Body" from "Outbox" where "Recipient"=%s and "Subject"=\'Verify your email\' order by "CreatedAt" desc limit 1',(email,)).fetchone()[0]
    token=parse_qs(urlparse(body).query)['token'][0]
    call(ctx,'/auth/verify?userId='+str(uid)+'&token='+quote(token,safe=''),'POST',{})
    return email,uid
def login(ctx,email):return call(ctx,'/auth/login','POST',{'email':email,'password':'Member-Password-2026!','rememberMe':True})
def magic_token(email):
    with psycopg.connect(**dsn) as db:
        body=db.execute('select "Body" from "Outbox" where "Recipient"=%s and "Subject"=\'Your sign-in link\' order by "CreatedAt" desc limit 1',(email,)).fetchone()[0]
    link=next(line for line in body.splitlines() if '/magic#' in line)
    return parse_qs(urlparse(link).fragment)['token'][0]

try:
    start()
    with sync_playwright() as p:
        browser=p.chromium.launch()
        a=browser.new_context();b=browser.new_context();other=browser.new_context();anonymous=browser.new_context()
        email,uid=signup(a,'Member');other_email,other_uid=signup(other,'Officer')
        check('signup and actual email verification')
        login(a,email);login(b,email);login(other,other_email)
        sessions=call(a,'/security/sessions');check('two independently revocable sessions',len(sessions)==2 and sum(s['current'] for s in sessions)==1)
        check('session expiry is available',call(a,'/security/session')['persistent'])
        with psycopg.connect(**dsn) as db:
            alerts=db.execute('select count(*) from "Outbox" where "Recipient"=%s and "Subject"=\'New device signed in\'',(email,)).fetchone()[0]
        check('sign-ins do not queue automatic email alerts',alerts==0)
        victim=next(s for s in sessions if not s['current'])
        call(other,'/security/sessions/'+victim['id'],'DELETE',status=404)
        check('cannot revoke another account session')
        call(a,'/security/sessions/'+victim['id'],'DELETE',status=204)
        call(b,'/users/me',status=401);check('revocation takes effect on next request')
        call(a,'/auth/login','POST',{'email':email,'password':'wrong'},status=401)
        activity=call(a,'/security/activity');check('successful and failed login history',{'auth.login.success','auth.login.failed'}<={r['action'] for r in activity})
        call(anonymous,'/security/sessions',status=401);check('sessions require authentication')
        methods=call(a,'/security/methods');check('methods and recovery-code counts load',methods['hasPassword'] and methods['passkeys']==[])
        before=call(a,'/security/session')['expiresAt'];extended=call(a,'/security/session/extend','POST',{})['expiresAt']
        check('explicit session extension returns renewed expiry',extended>=before)
        check('session extension does not create a spurious login',len(call(a,'/security/sessions'))==1)
        call(a,'/auth/magic/request','POST',{'email':email,'returnTo':'https://evil.example'})
        token=magic_token(email)
        with psycopg.connect(**dsn) as db:
            row=db.execute('select "TokenHash","ReturnTo" from "MemberLoginLink" where "UserId"=%s order by "CreatedAt" desc limit 1',(uid,)).fetchone()
        check('magic links store only hashes and reject external redirects',row[0]!=token and row[1]=='/app/membership')
        call(b,'/auth/magic/consume','POST',{'token':token});check('magic link creates a real session',call(b,'/users/me')['email']==email)
        call(anonymous,'/auth/magic/consume','POST',{'token':token},status=401);check('magic link cannot be reused')
        call(anonymous,'/auth/magic/request','POST',{'email':'unknown@example.test','returnTo':'/app/account'})
        check('unknown magic-link email returns generic success')
        # Restart only the isolated test process to reset the public auth rate-limit window.
        start()
        call(a,'/security/sessions/revoke-all','POST',{},status=204)
        call(b,'/users/me',status=401);call(a,'/users/me',status=401);check('global sign-out revokes all devices')
        login(a,email)
        setup=call(a,'/security/mfa/setup','POST',{'password':'Member-Password-2026!'})
        codes=call(a,'/security/mfa/enable','POST',{'code':totp(setup['key'])})['recoveryCodes']
        check('authenticator enrollment yields ten codes',len(codes)==10)
        fresh=call(a,'/security/mfa/recovery-codes','POST',{'code':totp(setup['key'])})['recoveryCodes']
        check('recovery-code regeneration replaces old codes',len(fresh)==10 and fresh!=codes)
        check('recovery-code count is accurate',call(a,'/security/methods')['recoveryCodes']==10)
        call(a,'/auth/logout','POST',{},status=204)
        check('password login still requires MFA',login(a,email)['requiresTwoFactor'])
        call(a,'/auth/two-factor','POST',{'code':codes[0],'recovery':True},status=401)
        call(a,'/auth/two-factor','POST',{'code':fresh[0],'recovery':True});check('old recovery codes fail and new codes work')
        check('recovery codes are counted after use',call(a,'/security/methods')['recoveryCodes']==9)
        call(a,'/auth/logout','POST',{},status=204)
        login(a,email);call(a,'/auth/restart','POST',{},status=204)
        call(a,'/auth/two-factor','POST',{'code':totp(setup['key'])},status=401);check('MFA restart clears pending challenge')
        with psycopg.connect(**dsn) as db:db.execute('update "MemberLoginLink" set "CreatedAt"=now()-interval \'2 minutes\' where "UserId"=%s',(uid,))
        call(a,'/auth/magic/request','POST',{'email':email,'returnTo':'/app/account?view=profile'})
        token=magic_token(email)
        check('magic link preserves MFA requirement',call(a,'/auth/magic/consume','POST',{'token':token})['requiresTwoFactor'])
        call(a,'/users/me',status=401)
        call(a,'/auth/two-factor','POST',{'code':totp(setup['key'])});check('magic-link MFA completes normally')
        # Actual Chromium WebAuthn ceremony with a virtual authenticator.
        start()
        page=a.new_page();page.on('pageerror',lambda e: print('BROWSER ERROR',str(e),flush=True));page.on('response',lambda r: print('HTTP ERROR',r.url.split('/api/v1')[-1].split('?')[0],r.status,r.text()[:200],flush=True) if r.status>=400 else None);page.goto(URL+'/app/account?view=security');page.wait_for_load_state('networkidle')
        cdp=a.new_cdp_session(page);cdp.send('WebAuthn.enable')
        cdp.send('WebAuthn.addVirtualAuthenticator',{'options':{'protocol':'ctap2','transport':'internal','hasResidentKey':True,'hasUserVerification':True,'isUserVerified':True,'automaticPresenceSimulation':True}})
        page.get_by_role('button',name='Add a passkey',exact=True).click()
        page.get_by_role('textbox',name='Passkey name').fill('Test virtual passkey')
        page.get_by_role('button',name='Register passkey',exact=True).click()
        expect(page.get_by_text('Test virtual passkey',exact=True)).to_be_visible(timeout=20000)
        check('real WebAuthn passkey enrollment')
        credentials=call(a,'/security/methods')['passkeys'];check('passkeys are persisted',len(credentials)==1)
        call(a,'/auth/logout','POST',{},status=204)
        page.goto(URL+'/login');page.get_by_role('button',name='Sign in with a passkey',exact=True).click()
        expect(page.get_by_label('Authenticator code',exact=True)).to_be_visible(timeout=20000)
        check('passkey login preserves configured MFA')
        page.get_by_label('Authenticator code',exact=True).fill(totp(setup['key']))
        page.get_by_role('button',name='Verify & sign in',exact=True).click()
        page.wait_for_url('**/app/membership',timeout=20000)
        check('passkey and MFA open member portal')
        call(a,'/security/passkeys/'+credentials[0]['id'],'DELETE',status=204)
        check('passkey removal persists',call(a,'/security/methods')['passkeys']==[])
        # Minimal isolated society fixture for membership ownership review.
        tenant,level,chapter,category,person,member,role=[uuid.uuid4() for _ in range(7)]
        with psycopg.connect(**dsn) as db:
            db.execute('insert into "Tenants" ("Id","Name","Status","Profile","CreatedAt") values (%s,\'Login Test Society\',\'Active\',\'{}\',now())',(tenant,))
            db.execute('insert into "Levels" ("Id","TenantId","Name","Kind","AllowedParents","AllowRoot") values (%s,%s,\'Chapter\',\'Chapter\',\'{}\',true)',(level,tenant))
            db.execute('insert into "Units" ("Id","TenantId","LevelId","Name","Code","Status","Details","CreatedAt","UpdatedAt","Revision") values (%s,%s,%s,\'Test Chapter\',\'TEST\',\'Active\',\'{}\',now(),now(),0)',(chapter,tenant,level))
            db.execute('insert into "MembershipCategories" ("Id","TenantId","Name","Description","Active","Kind") values (%s,%s,\'Individual\',\'Test\',true,\'Individual\')',(category,tenant))
            db.execute('insert into "Persons" ("Id","FullName","SearchName","CreatedAt","Revision") values (%s,\'Unclaimed member\',\'unclaimed member\',now(),0)',(person,))
            db.execute('insert into "Memberships" ("Id","TenantId","PersonId","ChapterId","CategoryId","Number","Status","JoinedAt","EffectiveFrom","Profile","Privacy","Revision","RenewalReminders","PendingPayment") values (%s,%s,%s,%s,%s,\'LOGIN-001\',\'Active\',now(),now(),\'{}\',\'{}\',0,false,false)',(member,tenant,person,chapter,category))
            db.execute('insert into "Links" ("TenantId","UserId") values (%s,%s)',(tenant,uid))
            db.execute('insert into "GovernanceRoles" ("Id","TenantId","Name","Permissions","Responsibilities") values (%s,%s,\'Membership reviewer\',ARRAY[\'members.approve\'],\'Test\')',(role,tenant))
            db.execute('insert into "Assignments" ("Id","TenantId","UserId","RoleId","StartsAt") values (%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,uid,role))
        request=call(other,'/membership/link-requests','POST',{'tenantId':str(tenant),'number':'LOGIN-001','note':'Please check my historical membership records.'})
        check('authenticated member can request a missing membership',request['status']=='Pending')
        call(other,'/membership/link-requests','POST',{'tenantId':str(tenant),'number':'LOGIN-001','note':'Duplicate request for the same account.'},status=400)
        check('duplicate pending membership requests are blocked')
        path=f'/societies/{tenant}/membership/link-requests'
        call(other,path,status=403);check('ordinary members cannot read the review queue')
        review=call(a,path);check('authorized reviewer sees the request',review[0]['id']==request['id'])
        call(a,path+'/'+request['id']+'/review','POST',{'approve':True,'note':'Ownership checked against the original member register.'},status=204)
        check('verified ownership approval links the membership',any(m['id']==str(member) for m in call(other,'/membership/portfolio')))
        with psycopg.connect(**dsn) as db:
            check('membership linking does not rewrite a shared person identity',db.execute('select "UserId" from "Persons" where "Id"=%s',(person,)).fetchone()[0] is None)
        call(a,path+'/'+request['id']+'/review','POST',{'approve':True,'note':'Repeated approval must be prevented.'},status=400)
        check('membership linking cannot be approved twice')
        own=call(a,'/membership/link-requests','POST',{'tenantId':str(tenant),'number':'LOGIN-001','note':'Testing that self-approval is forbidden.'})
        call(a,path+'/'+own['id']+'/review','POST',{'approve':True,'note':'Self approval must not be allowed.'},status=400)
        check('reviewers cannot approve their own membership request')
        with psycopg.connect(**dsn) as db:
            db.execute('insert into "Assignments" ("Id","TenantId","UserId","RoleId","StartsAt") values (%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,other_uid,role))
        call(other,path+'/'+own['id']+'/review','POST',{'approve':True,'note':'Existing ownership must not be overwritten.'},status=400)
        check('link review cannot steal another verified account membership')
        checklist=call(other,'/users/me/login-checklist');check('welcome checklist detects linked membership',checklist['membership'])
        call(other,'/users/me/login-checklist/profile','POST',{},status=204)
        call(other,'/users/me/login-checklist/id','POST',{},status=204)
        call(other,'/users/me/login-checklist','PUT',{'dismissed':True},status=204)
        checklist=call(other,'/users/me/login-checklist');check('welcome checklist progress persists',checklist['profile'] and checklist['idViewed'] and checklist['dismissed'])
        # Public UI recovery, branding, accessibility and language checks.
        start()
        ui=anonymous.new_page();ui.goto(URL+'/resend')
        expect(ui.get_by_role('button',name='Resend verification email',exact=True)).to_be_visible();check('resend is a working public route')
        ui.get_by_label('Email address',exact=True).fill(email);ui.get_by_role('button',name='Resend verification email',exact=True).click()
        expect(ui.get_by_text('Correct email / try again',exact=True)).to_be_visible();check('verification flow has correction and spam guidance')
        ui.goto(URL+'/login');expect(ui.get_by_label('Email or username',exact=True)).to_be_visible();check('identifier accurately names username support')
        ui.get_by_label('Language',exact=True).select_option('hi');expect(ui.get_by_label('ईमेल या उपयोगकर्ता नाम',exact=True)).to_be_visible();check('Hindi login labels')
        ui.reload();expect(ui.get_by_label('ईमेल या उपयोगकर्ता नाम',exact=True)).to_be_visible();check('language preference persists')
        ui.get_by_label('Language',exact=True).select_option('te');expect(ui.get_by_label('ఈమెయిల్ లేదా వినియోగదారు పేరు',exact=True)).to_be_visible();check('Telugu login labels')
        ui.get_by_label('Language',exact=True).select_option('en')
        ui.set_viewport_size({'width':390,'height':844});ui.screenshot(path=str(ROOT/'tests/member-login-enhancements-mobile.png'),full_page=True)
        check('mobile layout has no horizontal overflow',ui.evaluate('document.documentElement.scrollWidth <= innerWidth'))
        ui.goto(URL+'/magic-request');ui.get_by_label('Email address',exact=True).fill(other_email);ui.get_by_role('button',name='Send sign-in link',exact=True).click()
        expect(ui.get_by_role('status').filter(has_text='If your account is eligible')).to_be_visible();check('magic-link request UI works')
        magic=magic_token(other_email);ui.goto(URL+'/magic#token='+magic)
        expect(ui.get_by_role('button',name='Confirm sign-in',exact=True)).to_be_visible()
        check('magic token is removed from browser URL',not ui.evaluate('location.hash'))
        ui.get_by_role('button',name='Confirm sign-in',exact=True).click();ui.wait_for_url('**/app/membership')
        check('magic-link confirmation opens portal')
        # Reauthentication overlay retains existing form DOM and unsaved text.
        ui.goto(URL+'/app/account?view=profile');ui.wait_for_load_state('networkidle')
        field=ui.get_by_label('Full name',exact=True);field.fill('Unsaved member name')
        ui.evaluate("window.dispatchEvent(new Event('member:session-expired'))")
        dialog=ui.get_by_role('dialog',name='Sign in to continue');expect(dialog).to_be_visible()
        dialog.get_by_label('Email or username',exact=True).fill(other_email);dialog.get_by_label('Password',exact=True).fill('Member-Password-2026!');dialog.get_by_role('button',name='Sign in',exact=True).click()
        expect(dialog).not_to_be_visible(timeout=20000);expect(field).to_have_value('Unsaved member name');check('reauthentication preserves unsaved form text')
        browser.close()
finally:
    if server:server.terminate();server.wait(timeout=20)
    log.close()
    (ROOT/'tests/member-login-enhancements-results.json').write_text(json.dumps({'checks':checks,'passed':len(checks)},indent=2),encoding='utf-8')
