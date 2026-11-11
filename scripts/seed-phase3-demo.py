"""Optional, explicitly fictional preview fixtures. Never called by application startup."""
from pathlib import Path
import json,urllib.request,urllib.error,http.cookiejar,datetime,psycopg,uuid
root=Path(__file__).resolve().parents[1]
account=json.loads((root/'.local/preview-account.json').read_text());config=json.loads((root/'.local/dev.json').read_text())
parts=dict(x.split('=',1) for x in config['connectionString'].split(';') if '=' in x)
db=psycopg.connect(host=parts['Host'],port=parts['Port'],dbname=parts['Database'],user=parts['Username'],password=parts['Password'])
tenant=account['tenant']
assert db.execute('SELECT "Name" FROM "Tenants" WHERE "Id"=%s',(tenant,)).fetchone()[0]=='Demo · Community Foundation','This fixture only targets the named local fictional society'
op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));url='http://127.0.0.1:5092/api/v1'
def call(path,method='GET',body=None):
 h={'Content-Type':'application/json'}
 if method!='GET':h['X-CSRF-TOKEN']=call('/auth/csrf')['token']
 req=urllib.request.Request(url+path,method=method,headers=h,data=json.dumps(body).encode() if body is not None else (b'' if method!='GET' else None))
 try:
  with op.open(req) as r:return json.loads(r.read() or 'null')
 except urllib.error.HTTPError as error:raise RuntimeError(error.read().decode()[:500]) from None
call('/auth/login','POST',{'email':account['email'],'password':account['password']});base='/societies/'+tenant+'/community';setup=call(base+'/setup');chapters=[u for u in call('/societies/'+tenant+'/organizations/units?kind=Chapter&pageSize=100')['items'] if u['status']=='Active'];activity=next(c['id'] for c in setup['categories'] if c['name']=='Community Service' and c['kind']=='Activity');eventtype=next(c['id'] for c in setup['categories'] if c['kind']=='Event');actor=db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(account['email'],)).fetchone()[0]
for index,(title,body) in enumerate([
 ('A little time. A lasting difference. · Demo','<p>Fictional preview story: neighbors came together to prepare a community garden, exchange ideas and plan their next service morning.</p><p>Here is to the quiet work that brings a chapter closer.</p>'),
 ('Learning is better when we share it. · Demo','<p>Fictional preview story: our chapter workshop paired experienced volunteers with new members for an afternoon of practical learning.</p>'),
 ('Recognizing the people behind the progress. · Demo','<p>Fictional recognition: a moment of appreciation for the volunteers who make community projects possible. This is sample content, not a record of a real award.</p>')]):
 if db.execute('SELECT 1 FROM "ActivityPost" WHERE "TenantId"=%s AND "Title"=%s',(tenant,title)).fetchone():continue
 post=call(base+'/posts','POST',{'unitId':chapters[index%len(chapters)]['id'],'categoryId':activity,'title':title,'body':body,'official':True,'visibility':'Chapter','submit':False,'achievement':index==2,'tags':['FictionalPreview','Community']})
 db.execute('UPDATE "ActivityPost" SET "Status"=\'Published\',"Stages"=\'[]\',"PublishedAt"=now()-(%s*interval \'2 hours\') WHERE "TenantId"=%s AND "Id"=%s',(index,tenant,post['id']))
 db.execute('INSERT INTO "Audits" ("Id","TenantId","ActorId","Action","ResourceId","Details","At") VALUES (%s,%s,%s,\'demo.fixture.created\',%s,\'{"fictional":true}\',now())',(uuid.uuid4(),tenant,actor,post['id']));db.commit()
now=datetime.datetime.now(datetime.timezone.utc)
for index,title in enumerate(['Community service morning · Demo','Chapter learning circle · Demo']):
 if db.execute('SELECT 1 FROM "CommunityEvent" WHERE "TenantId"=%s AND "Title"=%s',(tenant,title)).fetchone():continue
 start=now+datetime.timedelta(days=3+index*4);end=start+datetime.timedelta(hours=2)
 event=call(base+'/events','POST',{'unitId':chapters[index%len(chapters)]['id'],'categoryId':eventtype,'title':title,'body':'<p>Fictional preview event. Explore RSVP, registration and the event desk using development test accounts.</p>','startsAt':start.isoformat(),'endsAt':end.isoformat(),'registrationOpens':(now-datetime.timedelta(days=1)).isoformat(),'registrationCloses':start.isoformat(),'venue':'Fictional Community Hall','capacity':80,'submit':False})
 db.execute('UPDATE "CommunityEvent" SET "Status"=\'Published\',"Stages"=\'[]\',"PublishedAt"=now() WHERE "TenantId"=%s AND "Id"=%s',(tenant,event['id']));db.execute('INSERT INTO "Audits" ("Id","TenantId","ActorId","Action","ResourceId","Details","At") VALUES (%s,%s,%s,\'demo.fixture.created\',%s,\'{"fictional":true}\',now())',(uuid.uuid4(),tenant,actor,event['id']));db.commit()
db.close();print('Explicitly fictional local community fixtures are ready; metrics query their stored records.')
