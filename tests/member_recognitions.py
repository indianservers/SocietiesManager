"""Recognition permissions, history, existing-member invariants and admin UI; isolated local database."""
import json,uuid,os,socket,subprocess,shutil,time
from pathlib import Path
import psycopg
from psycopg import sql
from playwright.sync_api import sync_playwright,expect
ROOT=Path.cwd();cfg=json.loads((ROOT/'.local/dev.json').read_text());suffix=uuid.uuid4().hex[:10];name='recognitions_'+suffix;dsn=dict(host='127.0.0.1',port=55439,user='postgres',password=cfg['password'],dbname=name)
with psycopg.connect(**{**dsn,'dbname':'postgres'},autocommit=True) as db:db.execute(sql.SQL('CREATE DATABASE {} TEMPLATE assemblia_welcome_test').format(sql.Identifier(name)))
def query(text,args=()):
 with psycopg.connect(**dsn) as db:
  r=db.execute(text,args);return r.fetchall() if r.description else None
query('UPDATE "Outbox" SET "Status"=\'Cancelled\' WHERE "SentAt" IS NULL');query('UPDATE "CommunicationCampaign" SET "Status"=\'Cancelled\'');query('UPDATE "CommunicationAutomation" SET "Enabled"=false')
with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
URL='http://localhost:'+str(port);folder=ROOT/'.local'/('recognitions-run-'+suffix);folder.mkdir();shutil.copytree(ROOT/'frontend/dist',folder/'wwwroot');env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database={name};Username=postgres;Password={cfg['password']}",'PublicUrl':URL,'Logging__LogLevel__Default':'Warning'};subprocess.run(['dotnet',str(ROOT/'.local/recognitions-local/Society.Api.dll'),'--migrate'],cwd=folder,env=env,check=True,stdout=subprocess.DEVNULL);log=(folder/'api.log').open('w');server=subprocess.Popen(['dotnet',str(ROOT/'.local/recognitions-local/Society.Api.dll'),'--urls',URL,'--contentRoot',str(folder)],cwd=folder,env=env,stdout=log,stderr=log,creationflags=subprocess.CREATE_NO_WINDOW);password='Recognition!'+uuid.uuid4().hex+'Aa9';checks=[]
def check(label,value=True):assert value,label;checks.append(label);print('PASS',label,flush=True)
try:
 with sync_playwright() as pw:
  browser=pw.chromium.launch();ctx=browser.new_context(viewport={'width':1440,'height':1000});req=ctx.request
  def call(path,body=None,method=None,status=200,client=req):
   headers={}
   if body is not None:headers['X-CSRF-TOKEN']=client.get(URL+'/api/v1/auth/csrf').json()['token']
   r=client.fetch(URL+'/api/v1'+path,method=method or ('POST' if body is not None else 'GET'),data=body,headers=headers);assert r.status==status,(path,r.status,r.text()[:200]);return r.json() if r.body() else None
  for _ in range(100):
   try:req.get(URL+'/health');break
   except Exception:time.sleep(.2)
  def account(email,platform):
   existing=query('SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail"=%s',(email.upper(),))
   if existing:
    shadow='shadow-'+suffix+'@example.invalid';query('UPDATE "AspNetUsers" SET "Email"=%s,"NormalizedEmail"=%s,"UserName"=%s,"NormalizedUserName"=%s WHERE "Id"=%s',(shadow,shadow.upper(),shadow,shadow.upper(),existing[0][0]))
   call('/auth/signup',{'email':email,'password':password,'fullName':'Recognition QA'});query('UPDATE "AspNetUsers" SET "EmailConfirmed"=true,"PlatformAdmin"=%s WHERE "NormalizedEmail"=%s',(platform,email.upper()));return str(query('SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail"=%s',(email.upper(),))[0][0])
  manager='saisatish@indianservers.com';managerid=account(manager,True);otheremail='other-admin-'+suffix+'@example.invalid';otherid=account(otheremail,True);memberemail='member-'+suffix+'@example.invalid';memberid=account(memberemail,False)
  tenant,mid,chapter=query('SELECT m."TenantId",m."Id",m."ChapterId" FROM "Memberships" m JOIN "MembershipCategories" c ON c."Id"=m."CategoryId" WHERE c."Kind"=\'Individual\' AND m."Status"=\'Active\' AND m."EndsAt">now() LIMIT 1')[0];tenant=str(tenant);mid=str(mid);person=query('SELECT "PersonId" FROM "Memberships" WHERE "Id"=%s',(mid,))[0][0];query('UPDATE "Persons" SET "UserId"=%s WHERE "Id"=%s',(memberid,person));query('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s) ON CONFLICT DO NOTHING',(tenant,memberid));path='/societies/'+tenant+'/membership/members/'+mid+'/recognitions';write='/platform/membership/'+tenant+'/members/'+mid+'/recognitions'
  call(path,status=401);check('Anonymous recognition access denied');call('/auth/login',{'email':manager,'password':password});data=call(path);check('Named verified Platform Admin can manage recognition options',data['canManage'] and data['options']==['Fellowship','Board Member']);before=query('SELECT "Number","CategoryId","Status","EffectiveFrom","EndsAt" FROM "Memberships" WHERE "Id"=%s',(mid,));counts=query('SELECT (SELECT count(*) FROM "Memberships"),(SELECT count(*) FROM "Assignments"),(SELECT count(*) FROM "OfficerAppointments")')
  query('UPDATE "Memberships" SET "Profile"=jsonb_set("Profile",\'{recognitions}\',\'["Fellowship"]\') WHERE "Id"=%s',(mid,));check('Editable profile JSON cannot grant recognition',call(path)['recognitions']==[])
  r=call(write,{'recognitions':['Fellowship','Board Member'],'revision':data['revision'],'reason':'Approved recognition test'},method='PUT');check('Both additional recognitions persist',call(path)['recognitions']==['Fellowship','Board Member']);call(write,{'recognitions':[],'revision':data['revision'],'reason':'Stale update test'},method='PUT',status=409);check('Stale recognition update rejected');call(write,{'recognitions':['Administrator'],'revision':r['revision'],'reason':'Invalid recognition'},method='PUT',status=400);check('Only supported recognition labels allowed');check('Membership identity, category, validity and access permissions unchanged',before==query('SELECT "Number","CategoryId","Status","EffectiveFrom","EndsAt" FROM "Memberships" WHERE "Id"=%s',(mid,)) and counts==query('SELECT (SELECT count(*) FROM "Memberships"),(SELECT count(*) FROM "Assignments"),(SELECT count(*) FROM "OfficerAppointments")'))
  for email,can in [(otheremail,False),(memberemail,False)]:
   x=browser.new_context();call('/auth/login',{'email':email,'password':password},client=x.request);read=call(path,client=x.request);check('Recognition display read-only for '+('other admin' if email==otheremail else 'member'),read['recognitions']==['Fellowship','Board Member'] and not read['canManage']);call(write,{'recognitions':[],'revision':r['revision'],'reason':'Unauthorized change'},method='PUT',status=403,client=x.request);x.close()
  r=call(write,{'recognitions':['Board Member'],'revision':r['revision'],'reason':'Remove Fellowship recognition'},method='PUT');check('Recognition can be removed independently',call(path)['recognitions']==['Board Member']);r=call(write,{'recognitions':[],'revision':r['revision'],'reason':'Remove remaining recognition'},method='PUT');check('Both recognitions can be cleared',call(path)['recognitions']==[]);check('Recognition grants and removals are audited',query('SELECT count(*) FROM "MembershipHistories" WHERE "MembershipId"=%s AND "Action"=\'membership.recognitions.updated\'',(mid,))[0][0]==3)
  page=ctx.new_page();page.goto(URL+'/app/members/profile/'+mid+'?memberKind=Individual',wait_until='domcontentloaded');button=page.get_by_role('button',name='Set member recognitions',exact=True);expect(button).to_be_visible();button.click();page.get_by_label('Fellowship',exact=True).check();page.get_by_label('Board Member',exact=True).check();page.get_by_label('Reason for this change',exact=True).fill('Recognition approved from admin controls');page.get_by_role('button',name='Save recognitions',exact=True).click();expect(button).to_be_visible();section=page.get_by_role('region',name='Additional member recognitions');expect(section).to_contain_text('Fellowship');expect(section).to_contain_text('Board Member');section.screenshot(path=str(ROOT/'tests/member-recognitions-admin.png'));page.set_viewport_size({'width':390,'height':844});assert page.evaluate('document.documentElement.scrollWidth<=innerWidth');section.screenshot(path=str(ROOT/'tests/member-recognitions-mobile.png'));check('Named administrator can assign recognition from member profile UI');browser.close()
 (ROOT/'tests/member-recognitions-results.json').write_text(json.dumps({'checks':checks},indent=2))
finally:
 server.terminate();server.wait(timeout=15);log.close()
 with psycopg.connect(**{**dsn,'dbname':'postgres'},autocommit=True) as db:db.execute(sql.SQL('DROP DATABASE {}').format(sql.Identifier(name)))
