"""End-to-end individual self-service using isolated synthetic records only.
Verification uses the real queued link; SMTP transport is intentionally not exercised.
"""
import json,os,socket,subprocess,sys,time,uuid,shutil
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
import psycopg
ROOT=Path(__file__).resolve().parents[1];live='--production' in sys.argv
suffix=uuid.uuid4().hex[:12];email='selftest-'+suffix+'@example.invalid';password='SelfTest!'+uuid.uuid4().hex+'A9';name='Individual self-service verification '+suffix;checks=[];errors=[];server=None;created=[];user_id=None
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
  manual='/platform/membership/'+tenant;setup=admin.request.get(url+'/api/v1'+manual+'/setup').json();category=next(c['id'] for c in setup['categories'] if c['kind']=='Individual' and c['active']);chapter=next(c['id'] for c in setup['chapters'] if c['status']!='Archived')
  for label,address in [('SELF',email),('OTHER','other-'+suffix+'@example.invalid')]:
   r=mutate(admin,manual+'/members',{'fullName':name if label=='SELF' else 'Unrelated synthetic individual '+suffix,'number':'SELFTEST-'+label+'-'+suffix,'chapterId':chapter,'categoryId':category,'status':'Active','joinedOn':'2020-01-01','validFrom':'2020-01-01','validThrough':'2027-01-31','profile':json.dumps({'email':address}),'revision':0,'reason':'Temporary isolated self-service end-to-end verification','allowEmailClaim':True});assert r.ok,(r.status,r.text());created.append(r.json()['id'])
  member=created[0];other=created[1];base='/societies/'+tenant+'/membership/members/';before=admin.request.get(url+'/api/v1'+manual+'/members/'+member).json()
  if not live:query('UPDATE "StudioPolicy" SET "MemberDownloads"=false WHERE "TenantId"=%s RETURNING "Id"',(tenant,))
  ctx=browser.new_context(viewport={'width':1440,'height':1000});p=ctx.new_page();p.on('pageerror',lambda e:errors.append(str(e)));p.goto(url+'/signup');p.get_by_label('Full name',exact=True).fill(name);p.get_by_label('Email address',exact=True).fill(email);p.get_by_label('Password',exact=True).fill(password);p.get_by_role('combobox',name='I am a',exact=True).select_option('Individual');p.locator('input[name=terms]').check();p.get_by_role('button',name='Create account',exact=True).click();expect(p.get_by_role('link',name='Continue to sign in',exact=True)).to_be_visible();check('individual creates a login account without a new membership application')
  r=mutate(ctx,'/auth/login',{'email':email,'password':password});assert r.status==401;check('unverified account cannot sign in')
  if live:
   record=json.loads(remote_sql('SELECT json_build_object(\'id\',u."Id",\'body\',(SELECT o."Body" FROM "Outbox" o WHERE o."Recipient"=u."Email" AND o."Subject"=\'Verify your email\' ORDER BY o."CreatedAt" DESC LIMIT 1)) FROM "AspNetUsers" u WHERE u."NormalizedEmail"=\''+email.upper()+'\';'))
  else:
   row=query('SELECT u."Id",(SELECT o."Body" FROM "Outbox" o WHERE o."Recipient"=u."Email" AND o."Subject"=\'Verify your email\' ORDER BY o."CreatedAt" DESC LIMIT 1) FROM "AspNetUsers" u WHERE u."NormalizedEmail"=%s',(email.upper(),))[0];record={'id':str(row[0]),'body':row[1]}
  user_id=record['id'];p.goto(record['body']);p.get_by_role('button',name='Verify email',exact=True).click();expect(p.get_by_text('Email verified. You can sign in.',exact=True)).to_be_visible();check('queued verification link confirms the individual account')
  p.goto(url+'/');p.get_by_label('Email or username',exact=True).fill(email);p.get_by_label('Password',exact=True).fill(password);p.get_by_role('button',name='Sign in',exact=True).click();p.wait_for_url('**/app/membership',timeout=60000);expect(p.get_by_role('heading',name='Your membership. Your opportunities.',exact=True)).to_be_visible();expect(p.get_by_role('heading',name='My individual ID cards',exact=True)).to_be_visible();check('verified member login opens their existing individual membership and ID card section')
  portfolio=ctx.request.get(url+'/api/v1/membership/portfolio').json();assert [m['id'] for m in portfolio]==[member];assert not ctx.request.get(url+'/api/v1/users/me').json()['platformAdmin'];check('ordinary member sees only their own membership without administrator privileges')
  with p.expect_download(timeout=60000) as item:p.get_by_role('link',name='Download my individual membership ID (PDF)',exact=True).click()
  target=ROOT/('.local/member-self-'+('live' if live else 'local')+'.pdf');item.value.save_as(str(target));assert target.read_bytes().startswith(b'%PDF-');check('individual downloads their own ID PDF through the member portal')
  assert ctx.request.get(url+'/api/v1'+base+other+'/id-card').status==403;assert ctx.request.get(url+'/api/v1'+manual+'/id-cards.zip').status==403;check('member cannot download another individual ID or the administrator bulk ZIP')
  p.get_by_role('button',name='Edit my personal details',exact=True).click();expect(p.get_by_label('Registered name',exact=True)).to_have_attribute('readonly','');expect(p.get_by_label('Membership ID',exact=True)).to_have_attribute('readonly','');check('registered name and membership ID are read-only in the individual editor')
  p.get_by_label('Mobile number',exact=True).fill('+91 90000 00123');p.get_by_label('City',exact=True).fill('Member self-service city');p.get_by_label('Addresses (one per line)',exact=True).fill('Synthetic test address');p.get_by_role('button',name='Save my personal details',exact=True).click();expect(p.get_by_role('dialog',name='Edit my personal details',exact=True)).not_to_be_visible();expect(p.get_by_text('No contact email',exact=False)).not_to_be_visible();check('member creates and updates their own contact and address details')
  ownpath='/membership/my/'+member+'/profile';own=ctx.request.get(url+'/api/v1'+ownpath).json();assert own['profile']['phone']=='+91 90000 00123' and own['profile']['city']=='Member self-service city';assert own['fullName']==name and own['number']==before['member']['number'];check('saved profile reads back while registered identity stays unchanged')
  assert mutate(ctx,ownpath,{'revision':own['revision'],'profile':json.dumps({'fullName':'Forbidden rename'})},'PUT').status==400
  assert mutate(ctx,ownpath,{'revision':own['revision'],'profile':json.dumps({'number':'FORBIDDEN-ID'})},'PUT').status==400
  assert mutate(ctx,ownpath,{'revision':own['revision'],'profile':json.dumps({'adminArchived':True})},'PUT').status==400
  assert ctx.request.get(url+'/api/v1/membership/my/'+other+'/profile').status==403;check('server rejects name ID administration fields and cross-member profile access')
  legacy={**before['member'],'fullName':'Forbidden legacy rename','profile':json.dumps(own['profile']),'revision':own['revision']};assert mutate(ctx,base+member,legacy,'PUT').status==400;check('legacy profile edit endpoint also rejects registered-name changes')
  p.get_by_role('button',name='Clear optional personal details',exact=True).click();p.get_by_role('button',name='Confirm clear optional details',exact=True).click();expect(p.get_by_role('dialog',name='Clear optional personal details',exact=True)).not_to_be_visible();cleared=ctx.request.get(url+'/api/v1'+ownpath).json();assert not cleared['profile'] and cleared['fullName']==name and cleared['number']==before['member']['number'];check('member deletes optional personal details without deleting membership or identity')
  nav=p.get_by_role('navigation',name='Main workspace navigation',exact=True)
  expect(nav.get_by_role('link',name='My individual membership',exact=True)).to_be_visible();expect(nav.get_by_role('link',name='Individual members',exact=True)).not_to_be_visible();expect(nav.get_by_role('link',name='Institutional members',exact=True)).not_to_be_visible();expect(nav.get_by_role('link',name='Home',exact=True)).not_to_be_visible();check('individual navigation contains only private member pages')
  for forbidden in ['/app/members','/app/members/governance','/app/organization','/app/platform','/app/settings','/app/intelligence','/app/community','/app/studio/templates']:
   p.goto(url+forbidden);p.wait_for_url('**/app/membership');expect(p.get_by_role('heading',name='My individual ID cards',exact=True)).to_be_visible()
  check('direct links to administration pages redirect to the individual portal')
  p.goto(url+'/app/finance/my');expect(p.get_by_role('heading',name='Your contributions, in one place.',exact=True)).to_be_visible();expect(p.get_by_role('link',name='Reports & settings',exact=True)).not_to_be_visible();check('member finance page shows only own invoices and receipts')
  p.goto(url+'/app/membership')
  p.set_viewport_size({'width':390,'height':900});p.reload();expect(p.get_by_role('link',name='Download my individual membership ID (PDF)',exact=True)).to_be_visible();assert p.eval_on_selector('html','el=>el.scrollWidth')<=390;p.screenshot(path=str(ROOT/('.local/member-self-'+('live' if live else 'local')+'-mobile.png')),full_page=True);check('member login destination and download control fit mobile')
  after=admin.request.get(url+'/api/v1'+manual+'/members/'+member).json();assert all(before['member'][k]==after['member'][k] for k in ['fullName','number','status','categoryId','chapterId','effectiveFrom','endsAt']);check('member profile CRUD preserves registered identity and membership terms')
  assert mutate(ctx,'/auth/logout',{}).ok;assert ctx.request.get(url+'/api/v1'+base+member+'/id-card').status==401;p.goto(url+'/app/membership');expect(p.get_by_role('heading',name='Welcome back',exact=True)).to_be_visible();check('logout ends member access and requires sign-in again')
  assert not errors,errors;browser.close()
finally:
 if not live:
  if server:server.terminate();server.wait(timeout=20);log.close()
 # Retain only local synthetic fixtures; production cleanup is scoped to exact generated IDs.
 if live and created:
  ids=','.join("'"+str(uuid.UUID(i))+"'" for i in created);uid="'"+str(uuid.UUID(user_id))+"'" if user_id else '(SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail"=\''+email.upper()+'\')'
  cleanup='BEGIN; SET LOCAL session_replication_role = replica; DELETE FROM "MembershipHistories" WHERE "MembershipId" IN ('+ids+'); DELETE FROM "Audits" WHERE "ResourceId" IN ('+ids+') OR "ActorId"='+uid+'; DELETE FROM "Memberships" WHERE "Id" IN ('+ids+') AND "Number" ILIKE \'SELFTEST-%-'+suffix+'\'; DELETE FROM "Persons" WHERE "FullName" IN (\''+name+'\',\'Unrelated synthetic individual '+suffix+'\') AND "UserId" IS NULL; DELETE FROM "Links" WHERE "UserId"='+uid+'; DELETE FROM "Outbox" WHERE "Recipient"=\''+email+'\'; DELETE FROM "AspNetUsers" WHERE "Id"='+uid+' AND "NormalizedEmail"=\''+email.upper()+'\'; COMMIT;'
  remote_sql(cleanup);check('temporary production membership and account fixtures removed')
 (ROOT/('.local/member-self-'+('live' if live else 'local')+'-results.json')).write_text(json.dumps({'checks':checks,'pageErrors':errors,'smtpTransportTested':False,'production':live},indent=2),encoding='utf-8')

