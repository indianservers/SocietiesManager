"""End-to-end individual self-service using isolated synthetic records only.
Verification uses the real queued link; SMTP transport is intentionally not exercised.
"""
import json,os,socket,subprocess,sys,time,uuid,shutil
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
import psycopg
ROOT=Path(__file__).resolve().parents[1];live='--production' in sys.argv;kind='Corporate' if '--corporate' in sys.argv else 'Institutional';invitation_id=None
suffix=uuid.uuid4().hex[:12];email='selftest-'+suffix+'@example.invalid';password='SelfTest!'+uuid.uuid4().hex+'A9';name=kind+' self-service verification '+suffix;checks=[];errors=[];server=None;created=[];user_id=None
if live:
 cfg=json.loads((ROOT/'.local/production-owner-login.json').read_text());url='https://members.aimersociety.com';tenant='33ea8a45-ccaa-4810-9fac-06c5b72c2f10';credentials={'email':cfg['email'],'password':cfg['initialPassword']}
else:
 from api_runtime import API_DLL
 cfg=json.loads((ROOT/'.local/preview-account.json').read_text());account=json.loads((ROOT/'.local/society-example-account.json').read_text());dev=json.loads((ROOT/'.local/dev.json').read_text());tenant=account['tenant'];credentials={'email':cfg['platformEmail'],'password':cfg['platformPassword']};dsn=dict(host='127.0.0.1',port=55439,dbname='assemblia',user='postgres',password=dev['password'])
 with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
 url='http://127.0.0.1:'+str(port);shutil.copytree(ROOT/'frontend/dist',API_DLL.parent/'wwwroot',dirs_exist_ok=True)
 env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','PublicUrl':url,'Society__DefaultTenantId':tenant,'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=assemblia;Username=postgres;Password={dev['password']}",'Logging__LogLevel__Default':'Warning'}
 log=(ROOT/'.local/member-self-browser-api.log').open('w');server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',url],cwd=API_DLL.parent,env=env,stdout=log,stderr=log)
def query(sql,args=()):
 with psycopg.connect(**dsn) as db:return db.execute(sql,args).fetchall()
def remote_sql(sql):
 header=(ROOT/'.local/institution-fields-audit.sh').read_text().split('data=json.loads')[0]
 script=header+'q('+repr(sql)+')\nPY\n';path=ROOT/('.local/member-self-test-db-'+suffix+'.sh');path.write_text(script,encoding='utf-8',newline='\n');return subprocess.run([sys.executable,str(ROOT/'.local/remote_deploy.py'),'exec',str(path)],capture_output=True,text=True,check=True).stdout.strip()
def check(label):checks.append(label);print('PASS',label,flush=True)
def mutate(context,path,data,method='POST'):
 token=context.request.get(url+'/api/v1/auth/csrf').json()['token'];return context.request.fetch(url+'/api/v1'+path,method=method,data=data,headers={'X-CSRF-TOKEN':token})
try:
 with sync_playwright() as pw:
  browser=pw.chromium.launch();admin=browser.new_context();page=admin.new_page();expect.set_options(timeout=30000)
  for _ in range(100):
   try:
    if admin.request.get(url+'/health/ready').ok:break
   except Exception:pass
   time.sleep(.3)
  r=mutate(admin,'/auth/login',credentials);assert r.ok,r.status
  manual='/platform/membership/'+tenant;setup=admin.request.get(url+'/api/v1'+manual+'/setup').json();category=next(c['id'] for c in setup['categories'] if c['kind']==kind and c['active']);chapter=next(c['id'] for c in setup['chapters'] if c['status']!='Archived')
  for label,address in [('SELF',email),('OTHER','other-'+suffix+'@example.invalid')]:
   r=mutate(admin,manual+'/members',{'fullName':name if label=='SELF' else 'Unrelated synthetic individual '+suffix,'number':'SELFTEST-'+label+'-'+suffix,'chapterId':chapter,'categoryId':category,'status':'Active','joinedOn':'2020-01-01','validFrom':'2020-01-01','validThrough':'2027-01-31','profile':json.dumps({'email':address}),'revision':0,'reason':'Temporary isolated self-service end-to-end verification','allowEmailClaim':False});assert r.ok,(r.status,r.text());created.append(r.json()['id'])
  member=created[0];other=created[1];base='/societies/'+tenant+'/membership/members/';before=admin.request.get(url+'/api/v1'+manual+'/members/'+member).json()
  if not live:query('UPDATE "StudioPolicy" SET "MemberDownloads"=true WHERE "TenantId"=%s RETURNING "Id"',(tenant,))
  claim_admin=admin
  if not live:
   claim_admin=browser.new_context();assert mutate(claim_admin,'/auth/login',{'email':account['email'],'password':account['password']}).ok
  raw=admin.request.get(url+'/api/v1/societies/'+tenant+'/membership/members/'+member).json();person_id=raw['member']['personId']
  invitation=mutate(claim_admin,'/societies/'+tenant+'/membership/invitations',{'chapterId':chapter,'categoryId':category,'email':email,'personId':person_id,'days':1,'usageLimit':1});assert invitation.ok,(invitation.status,invitation.text());invitation=invitation.json();invitation_id=invitation['id'];invitation_token=invitation['url'].split('invitation=')[1]
  ctx=browser.new_context(viewport={'width':1440,'height':1000});p=ctx.new_page();p.on('pageerror',lambda e:errors.append(str(e)));p.goto(url+'/signup');p.get_by_label('Full name',exact=True).fill(name);p.get_by_label('Email address',exact=True).fill(email);p.get_by_label('Password',exact=True).fill(password);p.get_by_role('combobox',name='I am a',exact=True).select_option(kind);p.locator('input[name=terms]').check();p.get_by_role('button',name='Create account',exact=True).click();expect(p.get_by_role('link',name='Continue to sign in',exact=True)).to_be_visible();check(kind+' representative creates a login account')
  r=mutate(ctx,'/auth/login',{'email':email,'password':password});assert r.status==401;check('unverified account cannot sign in')
  if live:
   record=json.loads(remote_sql('SELECT json_build_object(\'id\',u."Id",\'body\',(SELECT o."Body" FROM "Outbox" o WHERE o."Recipient"=u."Email" AND o."Subject"=\'Verify your email\' ORDER BY o."CreatedAt" DESC LIMIT 1)) FROM "AspNetUsers" u WHERE u."NormalizedEmail"=\''+email.upper()+'\';'))
  else:
   row=query('SELECT u."Id",(SELECT o."Body" FROM "Outbox" o WHERE o."Recipient"=u."Email" AND o."Subject"=\'Verify your email\' ORDER BY o."CreatedAt" DESC LIMIT 1) FROM "AspNetUsers" u WHERE u."NormalizedEmail"=%s',(email.upper(),))[0];record={'id':str(row[0]),'body':row[1]}
  user_id=record['id'];p.goto(record['body']);p.get_by_role('button',name='Verify email',exact=True).click();expect(p.get_by_text('Email verified. You can sign in.',exact=True)).to_be_visible();check('queued verification link confirms the individual account')
  p.goto(url+'/login');p.get_by_label('Email address',exact=True).fill(email);p.get_by_label('Password',exact=True).fill(password);p.get_by_role('button',name='Sign in',exact=True).click();p.wait_for_url('**/app/membership',timeout=60000);accepted=mutate(ctx,'/membership/invitations/accept',{'token':invitation_token});assert accepted.ok,(accepted.status,accepted.text());p.reload();expect(p.get_by_role('heading',name='Your organization. Your network.',exact=True)).to_be_visible();expect(p.get_by_role('heading',name='Organization membership documents',exact=True)).to_be_visible();check('verified organization representative claims and opens only their organization')
  portfolio=ctx.request.get(url+'/api/v1/membership/portfolio').json();assert [m['id'] for m in portfolio]==[member];assert not ctx.request.get(url+'/api/v1/users/me').json()['platformAdmin'];check('ordinary member sees only their own membership without administrator privileges')
  with p.expect_download(timeout=60000) as item:p.get_by_role('link',name='Download organization membership document (PDF)',exact=True).click()
  target=ROOT/('.local/organization-'+kind.lower()+'-'+('live' if live else 'local')+'.pdf');item.value.save_as(str(target));assert target.read_bytes().startswith(b'%PDF-');check(kind+' representative downloads only their organization document')
  assert ctx.request.get(url+'/api/v1'+base+other+'/id-card').status==403;assert ctx.request.get(url+'/api/v1'+manual+'/id-cards.zip').status==403;check('member cannot download another individual ID or the administrator bulk ZIP')
  expect(p.get_by_role('heading',name='My personal details',exact=True)).not_to_be_visible();nav=p.get_by_role('navigation',name='Main workspace navigation',exact=True);expect(nav.get_by_role('link',name='My '+kind.lower()+' memberships',exact=True)).to_be_visible();expect(nav.get_by_role('link',name='My individual membership',exact=True)).not_to_be_visible();check('organization-only account sees organization pages without individual or admin controls')
  for forbidden in ['/app/members','/app/organization','/app/platform','/app/settings','/app/studio/templates']:
   p.goto(url+forbidden);p.wait_for_url('**/app/membership');expect(p.get_by_role('heading',name='Organization membership documents',exact=True)).to_be_visible()
  check('organization account cannot open administrator or society-wide directory pages')
  assert ctx.request.get(url+'/api/v1/membership/my/'+member+'/profile').status==403;check('organization cannot use the individual personal profile editor')
  p.set_viewport_size({'width':390,'height':900});p.reload();expect(p.get_by_role('link',name='Download organization membership document (PDF)',exact=True)).to_be_visible();assert p.eval_on_selector('html','el=>el.scrollWidth')<=390;p.screenshot(path=str(ROOT/('.local/organization-'+kind.lower()+'-'+('live' if live else 'local')+'-mobile.png')),full_page=True);check('member login destination and download control fit mobile')
  after=admin.request.get(url+'/api/v1'+manual+'/members/'+member).json();assert all(before['member'][k]==after['member'][k] for k in ['fullName','number','status','categoryId','chapterId','effectiveFrom','endsAt']);check('member profile CRUD preserves registered identity and membership terms')
  assert mutate(ctx,'/auth/logout',{}).ok;assert ctx.request.get(url+'/api/v1'+base+member+'/id-card').status==401;p.goto(url+'/app/membership');expect(p.get_by_role('heading',name='Welcome back',exact=True)).to_be_visible();check('logout ends member access and requires sign-in again')
  assert not errors,errors;browser.close()
finally:
 if not live:
  if server:server.terminate();server.wait(timeout=20);log.close()
 # Retain only local synthetic fixtures; production cleanup is scoped to exact generated IDs.
 if live and created:
  ids=','.join("'"+str(uuid.UUID(i))+"'" for i in created);uid="'"+str(uuid.UUID(user_id))+"'" if user_id else '(SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail"=\''+email.upper()+'\')'
  cleanup='BEGIN; SET LOCAL session_replication_role = replica; DELETE FROM "MemberInvitations" WHERE "PersonId" IN (SELECT "PersonId" FROM "Memberships" WHERE "Id" IN ('+ids+')); DELETE FROM "MembershipRepresentative" WHERE "MembershipId" IN ('+ids+'); DELETE FROM "MembershipHistories" WHERE "MembershipId" IN ('+ids+'); DELETE FROM "Audits" WHERE "ResourceId" IN ('+ids+') OR "ActorId"='+uid+'; DELETE FROM "Memberships" WHERE "Id" IN ('+ids+') AND "Number" ILIKE \'SELFTEST-%-'+suffix+'\'; DELETE FROM "Persons" WHERE "FullName" IN (\''+name+'\',\'Unrelated synthetic individual '+suffix+'\') AND "UserId" IS NULL; DELETE FROM "Links" WHERE "UserId"='+uid+'; DELETE FROM "Outbox" WHERE "Recipient"=\''+email+'\'; DELETE FROM "AspNetUsers" WHERE "Id"='+uid+' AND "NormalizedEmail"=\''+email.upper()+'\'; COMMIT;'
  remote_sql(cleanup);check('temporary production membership and account fixtures removed')
 (ROOT/('.local/organization-'+kind.lower()+'-'+('live' if live else 'local')+'-results.json')).write_text(json.dumps({'checks':checks,'pageErrors':errors,'smtpTransportTested':False,'production':live},indent=2),encoding='utf-8')

