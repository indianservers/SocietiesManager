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
 API_DLL=ROOT/'.local/institutional-design-local/Society.Api.dll'
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
  p.goto(url+'/');p.get_by_label('Email or username',exact=True).fill(email);p.get_by_label('Password',exact=True).fill(password);p.get_by_role('button',name='Sign in',exact=True).click();p.wait_for_url('**/app/membership',timeout=60000);accepted=mutate(ctx,'/membership/invitations/accept',{'token':invitation_token});assert accepted.ok,(accepted.status,accepted.text());p.reload();expect(p.locator('.ip-shell')).to_be_visible()
  p.goto(url+'/app/account');expect(p.get_by_role('heading',name='Personal profile',exact=True)).to_be_visible();expect(p.get_by_role('heading',name='Institutional profile',exact=True)).to_be_visible()
  personal=ctx.request.get(url+'/api/v1/users/me/personal-profile').json()
  payload={'fullName':'Dr. Personal Test','designation':'Faculty','phone':'+919999999999','revision':personal['revision']}
  r=mutate(ctx,'/users/me/personal-profile',payload,'PUT');assert r.ok,r.text()
  assert mutate(ctx,'/users/me/personal-profile',payload,'PUT').status==409
  latest=ctx.request.get(url+'/api/v1/users/me/personal-profile').json();assert latest['fullName']=='Dr. Personal Test' and latest['designation']=='Faculty'
  assert mutate(ctx,'/users/me/personal-profile',{**payload,'revision':latest['revision'],'registeredName':'Forged institution'},'PUT').status==400
  assert admin.request.get(url+'/api/v1'+manual+'/members/'+member).json()['member']['fullName']==name
  raw=admin.request.get(url+'/api/v1/societies/'+tenant+'/membership/members/'+member).json();m=raw['member']
  forged={'fullName':'Forged institution','number':m['number'],'chapterId':m['chapterId'],'categoryId':m['categoryId'],'status':m['status'],'profile':json.dumps(raw['profile']),'revision':m['revision']}
  assert mutate(ctx,base+member,forged,'PUT').status==400
  admin_record=admin.request.get(url+'/api/v1'+manual+'/members/'+member).json()['member']
  correction={'fullName':name+' corrected','number':admin_record['number'],'chapterId':chapter,'categoryId':category,'status':'Active','joinedOn':'2020-01-01','validFrom':'2020-01-01','validThrough':'2024-08-31','profile':json.dumps({'email':email}),'revision':admin.request.get(url+'/api/v1'+manual+'/members/'+member).json()['revision'],'reason':'Verify authorized institution name correction','allowEmailClaim':False}
  r=mutate(admin,manual+'/members/'+member,correction,'PUT');assert r.ok,r.text()
  corrected=admin.request.get(url+'/api/v1'+manual+'/members/'+member).json()['member'];assert corrected['fullName']==name+' corrected'
  correction.update(fullName=name,revision=admin.request.get(url+'/api/v1'+manual+'/members/'+member).json()['revision']);assert mutate(admin,manual+'/members/'+member,correction,'PUT').ok
  assert ctx.request.get(url+'/api/v1/users/me/personal-profile').json()['fullName']=='Dr. Personal Test'
  p.reload();expect(p.get_by_label('Full name',exact=True)).to_have_value('Dr. Personal Test');expect(p.locator('.pa-locked strong')).to_have_text(name)
  catalog=ctx.request.get(url+'/api/v1/membership/institutions/'+member+'/chapters').json();assert catalog
  assert ctx.request.get(url+'/api/v1/membership/institutions/'+other+'/chapters').status==403
  affiliation={'chapterId':catalog[0]['id'],'note':'Please review this institution chapter affiliation.'}
  assert mutate(ctx,'/membership/institutions/'+member+'/requests/affiliation',affiliation).ok
  assert mutate(ctx,'/membership/institutions/'+member+'/requests/affiliation',affiliation).status==400
  assert mutate(ctx,'/membership/institutions/'+member+'/requests/name-correction',{'proposedName':'Requested correction','note':'Administrator approval required.'}).ok
  assert admin.request.get(url+'/api/v1'+manual+'/members/'+member).json()['member']['fullName']==name
  for path,title,slug in [('/app/membership','Institutional membership','membership'),('/app/account','My account','account'),('/app/clubs','Clubs & chapters','clubs'),('/app/resources','Resources','resources')]:
   p.goto(url+path,wait_until='domcontentloaded');expect(p.get_by_role('heading',name=title,exact=True)).to_be_visible();p.locator('.content-skeleton').wait_for(state='hidden',timeout=15000);p.evaluate('document.fonts.ready')
   for width in [1586,390]:
    p.set_viewport_size({'width':width,'height':992});assert p.evaluate('document.documentElement.scrollWidth<=innerWidth'),slug+' overflow';p.screenshot(path=str(ROOT/'tests'/('institutional-design-'+kind.lower()+'-'+slug+'-'+str(width)+'.png')),full_page=True)
   p.set_viewport_size({'width':1586,'height':992})
   if slug=='membership':
    for tab in ['Institution details','Documents','Renewal history','Overview']:p.get_by_role('button',name=tab,exact=True).click()
    expect(p.get_by_role('heading',name='Document download',exact=True)).to_be_visible()
    with p.expect_download(timeout=60000) as download:p.get_by_role('link',name='Membership certificate (PDF)',exact=True).click()
    download.value.save_as(str(ROOT/'.local/institutional-design-test-certificate.pdf'))
   elif slug=='clubs':
    p.get_by_role('button',name='Explore chapters',exact=True).click();expect(p.get_by_role('heading',name='Explore institution chapters',exact=True)).to_be_visible();p.get_by_role('button',name='Close drawer',exact=True).click()
   elif slug=='resources':
    for tab in ['Guides','Documents','Videos','Saved','All']:p.get_by_role('button',name=tab,exact=True).click()
    expect(p.get_by_role('heading',name='Renew membership to access eligible resources.',exact=True)).to_be_visible()
  rid=str(uuid.uuid4());query('INSERT INTO "MemberResource" ("Id","TenantId","Title","Description","Kind","Audiences","Content","Url","Published","Revision") VALUES (%s,%s,%s,%s,%s,%s,%s,%s,true,0) RETURNING "Id"',(rid,tenant,'Institutional test tool '+suffix,'Isolated resource interaction check','Tool',[kind],'Verified institutional resource content.','https://aimersociety.com'))
  # Expired institutions cannot read published institutional resources.
  assert ctx.request.get(url+'/api/v1/societies/'+tenant+'/membership/resources/'+rid).status==403
  current=admin.request.get(url+'/api/v1'+manual+'/members/'+member).json();correction.update(revision=current['revision'],validThrough='2027-01-31');assert mutate(admin,manual+'/members/'+member,correction,'PUT').ok
  p.goto(url+'/app/resources?resource='+rid);expect(p.get_by_role('heading',name='Institutional test tool '+suffix,exact=True)).to_be_visible();expect(p.get_by_text('Verified institutional resource content.',exact=True)).to_be_visible();p.get_by_role('button',name='Save resource',exact=True).click();p.get_by_role('button',name='Close drawer',exact=True).click();p.get_by_role('button',name='Saved',exact=True).click();expect(p.get_by_text('Institutional test tool '+suffix,exact=True)).to_be_visible();p.get_by_role('textbox',name='Search library',exact=True).fill('no matching resource');expect(p.get_by_role('heading',name='No resources found',exact=True)).to_be_visible()
  p.goto(url+'/app/account');p.get_by_role('button',name='Request institution name correction',exact=False).click();expect(p.get_by_role('heading',name='Request institution name correction',exact=True)).to_be_visible();p.get_by_role('button',name='Close drawer',exact=True).click()
  p.get_by_role('link',name='Apply for Individual membership',exact=True).click();expect(p.get_by_role('radio',name='Individual',exact=False)).to_be_checked()
  assert not errors,errors
  check('Four institutional pages at desktop/mobile; name permissions, PDF download, reviewed chapter requests, resource lock/open/save/search and Individual application verified')
  browser.close()
finally:
 if server:server.terminate();server.wait(timeout=20);log.close()
(ROOT/'tests'/('institutional-design-'+kind.lower()+'-results.json')).write_text(json.dumps({'checks':checks,'errors':errors},indent=2))
