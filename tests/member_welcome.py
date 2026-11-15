"""Welcome email integration checks; uses synthetic local memberships and development pickup only."""
import json,time,uuid,os
from pathlib import Path
import psycopg
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parents[1];URL=os.environ.get('WELCOME_URL','http://127.0.0.1:5201');dev=json.loads((ROOT/'.local/dev.json').read_text());dsn=dict(host='127.0.0.1',port=55439,user='postgres',password=dev['password'],dbname='assemblia_welcome_test');checks=[];ids=[];people=[]
def check(n,v):
 assert v,n
 checks.append(n);print('PASS',n,flush=True)
with psycopg.connect(**dsn) as db:
 tenant=str(db.execute('SELECT m."TenantId" FROM "Memberships" m JOIN "MembershipCategories" c ON c."Id"=m."CategoryId" WHERE c."Kind"=\'Institutional\' LIMIT 1').fetchone()[0])
 for kind in ['Individual','Institutional']:
  row=db.execute('SELECT m."ChapterId",c."Id" FROM "Memberships" m JOIN "MembershipCategories" c ON c."Id"=m."CategoryId" WHERE m."TenantId"=%s AND c."Kind"=%s LIMIT 1',(tenant,kind)).fetchone();mid=uuid.uuid4();pid=uuid.uuid4();ids.append(str(mid));people.append(str(pid))
  db.execute('INSERT INTO "Persons" ("Id","FullName","SearchName","CreatedAt","Revision") VALUES (%s,%s,%s,now(),0)',(pid,'Welcome <QA> '+kind,'welcome qa '+kind.lower()))
  db.execute('INSERT INTO "Memberships" ("Id","TenantId","PersonId","ChapterId","CategoryId","Number","Status","JoinedAt","EffectiveFrom","EndsAt","Profile","Privacy","Revision","PendingPayment","RenewalReminders") VALUES (%s,%s,%s,%s,%s,%s,\'Active\',now(),now()-interval \'1 day\',now()+interval \'1 year\',%s::jsonb,%s::jsonb,0,false,false)',(mid,tenant,pid,*row,'WELCOME-QA-'+kind+'-'+uuid.uuid4().hex[:6],json.dumps({'email':'welcome-qa@example.test'}),json.dumps({'communication':True})))
try:
 with sync_playwright() as pw:
  req=pw.request.new_context(base_url=URL);base='/api/v1/societies/'+tenant+'/member-welcome'
  check('Anonymous request rejected',req.get(base+'/templates').status==401)
  c=json.loads((ROOT/'.local/preview-account.json').read_text());token=req.get('/api/v1/auth/csrf').json()['token'];check('Platform login',req.post('/api/v1/auth/login',data={'email':c['platformEmail'],'password':c['platformPassword']},headers={'X-CSRF-TOKEN':token}).ok)
  templates=req.get(base+'/templates').json();check('Six templates, three per membership type',len(templates)==6 and all(sum(t['kind']==k for t in templates)==3 for k in ['Individual','Institutional']))
  token=req.get('/api/v1/auth/csrf').json()['token'];headers={'X-CSRF-TOKEN':token}
  for i,kind in enumerate(['Individual','Institutional']):
   for t in [t for t in templates if t['kind']==kind]:
    r=req.get(base+'/preview',params={'membershipId':ids[i],'templateId':t['id']});check(kind+' preview '+t['id'],r.ok and r.json()['membershipNumber'] in r.json()['html'] and '&lt;QA&gt;' in r.json()['html'] and r.json()['eligible'])
   wrong='institutional-classic' if i==0 else 'individual-classic';check(kind+' rejects mismatched template',not req.post(base+'/send',data={'membershipId':ids[i],'templateId':wrong},headers=headers).ok)
  with psycopg.connect(**dsn) as db:db.execute('UPDATE "Memberships" SET "PendingPayment"=true WHERE "Id"=%s',(ids[0],))
  check('Unpaid membership cannot receive welcome',not req.post(base+'/send',data={'membershipId':ids[0],'templateId':'individual-classic'},headers=headers).ok)
  with psycopg.connect(**dsn) as db:db.execute('UPDATE "Memberships" SET "PendingPayment"=false WHERE "Id"=%s',(ids[0],))
  r=req.post(base+'/send',data={'membershipId':ids[0],'templateId':'individual-classic'},headers=headers);check('Eligible welcome durably queued',r.ok);job=r.json()['id'];check('Duplicate welcome prevented',not req.post(base+'/send',data={'membershipId':ids[0],'templateId':'individual-connect'},headers=headers).ok)
  for _ in range(40):
   with psycopg.connect(**dsn) as db:status=db.execute('SELECT "Status" FROM "Outbox" WHERE "Id"=%s',(job,)).fetchone()[0]
   if status=='Pickup':break
   time.sleep(.25)
  check('SMTP worker processes welcome using development pickup',status=='Pickup');mail=json.loads((ROOT/'.local/mail'/ (job+'.json')).read_text());check('Queued email includes real ID and HTML',mail['Body'].startswith('<!doctype html>') and mail['Subject'].startswith('Welcome to'))
  foreign=str(uuid.uuid4());check('Cross-tenant member preview denied',req.get('/api/v1/societies/'+foreign+'/member-welcome/preview',params={'membershipId':ids[0],'templateId':'individual-classic'}).status in [403,404])
  admin=pw.request.new_context(base_url=URL);cfg=json.loads((ROOT/'.local/society-example-account.json').read_text());token=admin.get('/api/v1/auth/csrf').json()['token'];check('Administrator login',admin.post('/api/v1/auth/login',data={'email':cfg['email'],'password':cfg['password']},headers={'X-CSRF-TOKEN':token}).ok)
  adminbase='/api/v1/societies/'+cfg['tenant']+'/member-welcome';check('Administrator can open welcome templates',admin.get(adminbase+'/templates').ok)
  with psycopg.connect(**dsn) as db:
   uid=db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(cfg['email'],)).fetchone()[0];assignments=db.execute('SELECT "Id","RevokedAt" FROM "Assignments" WHERE "UserId"=%s AND "TenantId"=%s',(uid,cfg['tenant'])).fetchall();db.execute('UPDATE "Assignments" SET "RevokedAt"=now() WHERE "UserId"=%s AND "TenantId"=%s',(uid,cfg['tenant']))
  try:
   check('Revoked administrator cannot open templates',admin.get(adminbase+'/templates').status==403);check('Revoked administrator cannot list private members',admin.get(adminbase+'/members?kind=Individual').json()==[])
  finally:
   with psycopg.connect(**dsn) as db:
    for aid,revoked in assignments:db.execute('UPDATE "Assignments" SET "RevokedAt"=%s WHERE "Id"=%s',(revoked,aid))
  with psycopg.connect(**dsn) as db:
   oldprofile=db.execute('SELECT "Profile"::text FROM "Tenants" WHERE "Id"=%s',(tenant,)).fetchone()[0]
   db.execute('UPDATE "Persons" SET "UserId"=%s WHERE "Id"=%s',(uid,people[0]))
   db.execute('INSERT INTO "MembershipRepresentative" ("Id","TenantId","MembershipId","UserId","Role","StartsAt") VALUES (%s,%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,ids[1],uid,'Admin'))
   for mid in ids:db.execute('UPDATE "Memberships" SET "Privacy"=%s::jsonb WHERE "Id"=%s',('{}',mid,))
  try:
   check('Admin sets default Individual email',req.put(base+'/defaults',data={'kind':'Individual','templateId':'individual-connect'},headers=headers).ok)
   check('Default cannot use wrong membership kind',not req.put(base+'/defaults',data={'kind':'Individual','templateId':'institutional-classic'},headers=headers).ok)
   atoken=admin.get('/api/v1/auth/csrf').json()['token'];ah={'X-CSRF-TOKEN':atoken}
   for i,kind in enumerate(['Individual','Institutional']):
    selfpath=base+'/self/'+ids[i];info=admin.get(selfpath).json();check(kind+' self email uses verified account and includes Valid till',info['recipient']==cfg['email'] and info['canRequest'] and info['validTill']!='No expiry recorded')
    if i==0:check('Self request resolves configured default',info['templateId']=='individual-connect')
    sent=admin.post(selfpath+'/request',data={'email':'other@example.test','templateId':'institutional-partnership'},headers=ah);check(kind+' self email queued with no arbitrary recipient',sent.ok);selfjob=sent.json()['id']
    check(kind+' repeat requests throttled',not admin.post(selfpath+'/request',headers=ah).ok)
    for _ in range(60):
     with psycopg.connect(**dsn) as db:row=db.execute('SELECT "Status","Recipient","Body" FROM "Outbox" WHERE "Id"=%s',(selfjob,)).fetchone()
     if row[0]=='Pickup':break
     time.sleep(.25)
    check(kind+' requested copy dispatched only to requester with validity',row[0]=='Pickup' and row[1]==cfg['email'] and 'Valid till '+info['validTill'] in row[2])
   with psycopg.connect(**dsn) as db:db.execute('UPDATE "AspNetUsers" SET "EmailConfirmed"=false WHERE "Id"=%s',(uid,))
   try:check('Unverified email cannot request a membership copy',not admin.get(base+'/self/'+ids[0]).json()['canRequest'])
   finally:
    with psycopg.connect(**dsn) as db:db.execute('UPDATE "AspNetUsers" SET "EmailConfirmed"=true WHERE "Id"=%s',(uid,))
   check('Foreign membership request denied',admin.post('/api/v1/societies/'+str(uuid.uuid4())+'/member-welcome/self/'+ids[0]+'/request',headers=ah).status in [403,404])
   if os.environ.get('WELCOME_BROWSER')=='1':
    import subprocess
    subprocess.run(['python',str(ROOT/'tests/member_self_email_browser.py')],check=True)
  finally:
   with psycopg.connect(**dsn) as db:db.execute('UPDATE "Tenants" SET "Profile"=%s::jsonb WHERE "Id"=%s',(oldprofile,tenant))
  admin.dispose()
  req.dispose()
finally:
 with psycopg.connect(**dsn) as db:
  db.execute('SET LOCAL session_replication_role=replica')
  for mid in ids:
   db.execute('DELETE FROM "MembershipRepresentative" WHERE "MembershipId"=%s',(mid,));db.execute('DELETE FROM "DeliveryAttempt" WHERE "OutboxId" IN (SELECT "Id" FROM "Outbox" WHERE "MembershipId"=%s)',(mid,));db.execute('DELETE FROM "Outbox" WHERE "MembershipId"=%s',(mid,));db.execute('DELETE FROM "Audits" WHERE "Action" IN (\'membership.welcome.queued\',\'membership.email.self.requested\') AND "ResourceId"=%s',(mid,));db.execute('DELETE FROM "Memberships" WHERE "Id"=%s',(mid,))
  for pid in people:db.execute('DELETE FROM "Persons" WHERE "Id"=%s',(pid,))
(ROOT/'tests/member-welcome-results.json').write_text(json.dumps({'checks':checks},indent=2))
