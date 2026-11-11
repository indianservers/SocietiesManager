"""Authenticated community/event benchmark on a disposable isolated PostgreSQL database."""
from pathlib import Path
import os,json,uuid,subprocess,time,statistics,urllib.request,urllib.error,urllib.parse,http.cookiejar,ast,socket
import psycopg
from psycopg import sql
ROOT=Path(__file__).resolve().parents[1];c=json.loads((ROOT/'.local/dev.json').read_text());database='assemblia_p3_bench_'+uuid.uuid4().hex[:10]
created=False;server=None;db=None;log=None
admin=psycopg.connect(host='127.0.0.1',port=55439,dbname='postgres',user='postgres',password=c['password'],autocommit=True)
try:
 admin.execute(sql.SQL('CREATE DATABASE {}').format(sql.Identifier(database)));created=True
 env={**os.environ,'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database={database};Username=postgres;Password={c['password']}",'ASPNETCORE_ENVIRONMENT':'Development','Logging__LogLevel__Default':'Error'}
 subprocess.run(['dotnet',str(ROOT/'backend/bin/Debug/net10.0/Society.Api.dll'),'--migrate'],cwd=ROOT/'backend',env=env,check=True,stdout=subprocess.DEVNULL)
 db=psycopg.connect(host='127.0.0.1',port=55439,dbname=database,user='postgres',password=c['password'])
 tree=ast.parse((ROOT/'tests/membership_benchmark.py').read_text(encoding='utf-8-sig'))
 seed=next(n.args[0].value for n in ast.walk(tree) if isinstance(n,ast.Call) and isinstance(n.func,ast.Attribute) and n.func.attr=='execute' and n.args and isinstance(n.args[0],ast.Constant) and isinstance(n.args[0].value,str) and 'CREATE TEMP TABLE bench_tenant' in n.args[0].value)
 start=time.perf_counter();db.execute(seed);tenant,chapter=db.execute('SELECT tenant,id FROM bench_chapters ORDER BY i LIMIT 1').fetchone();db.commit();insert_ms=round((time.perf_counter()-start)*1000,2)
 with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
 env['PublicUrl']=f'http://127.0.0.1:{port}'
 log=open(ROOT/'.local/community-api-benchmark.log','w',encoding='utf-8');server=subprocess.Popen(['dotnet',str(ROOT/'backend/bin/Debug/net10.0/Society.Api.dll'),'--urls',env['PublicUrl']],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
 base=env['PublicUrl']+'/api/v1';op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def call(path,method='GET',body=None):
  headers={'Content-Type':'application/json'}
  if method!='GET':headers['X-CSRF-TOKEN']=call('/auth/csrf')['token']
  req=urllib.request.Request(base+path,method=method,headers=headers,data=json.dumps(body).encode() if body is not None else (b'' if method!='GET' else None))
  with op.open(req) as response:return json.loads(response.read() or 'null')
 for _ in range(60):
  try:call('/auth/csrf');break
  except Exception:time.sleep(.25)
 email='benchmark-'+uuid.uuid4().hex[:8]+'@example.test';password=uuid.uuid4().hex+'!aA1'
 call('/auth/signup','POST',{'email':email,'password':password,'fullName':'Synthetic Benchmark Reader'})
 user=db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(email,)).fetchone()[0];body=db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s',(email,)).fetchone()[0];token=urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
 call('/auth/verify?userId='+str(user)+'&token='+urllib.parse.quote(token,safe=''),'POST');call('/auth/login','POST',{'email':email,'password':password})
 role=uuid.uuid4();assignment=uuid.uuid4()
 db.execute('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s)',(tenant,user));db.execute('INSERT INTO "GovernanceRoles" ("Id","TenantId","Name","Permissions","Responsibilities") VALUES (%s,%s,%s,%s,%s)',(role,tenant,'Benchmark Reader',['units.read','members.read'],'Synthetic benchmark fixture'));db.execute('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt","EndsAt","MembershipId","RevokedAt") VALUES (%s,%s,%s,%s,NULL,now()-interval \'1 day\',NULL,NULL,NULL)',(assignment,tenant,user,role));db.commit()
 db.execute((ROOT/'tests/community_fixture.sql').read_text(encoding='utf-8'));db.execute('UPDATE "GovernanceRoles" SET "Permissions"=%s WHERE "Id"=%s',(['units.read','members.read','activities.read','activities.analytics','events.analytics','events.manage'],role));db.commit()
 prefix='/societies/'+str(tenant)+'/community' 
 scenarios={'large_feed':prefix+'/feed','chapter_feed':prefix+'/feed?chapter='+str(chapter),'calendar':prefix+'/events','intelligence':prefix+'/intelligence'}
 event=db.execute('SELECT id FROM community_events ORDER BY i LIMIT 1').fetchone()[0];scenarios['attendance_analytics']=prefix+'/events/'+str(event)+'/analytics'
 plans={}
 for name,statement in {'feed_cursor':'SELECT "Id","Title" FROM "ActivityPost" WHERE "TenantId"=%s AND "Status"=\'Published\' ORDER BY "PublishedAt" DESC,"Id" DESC LIMIT 20','event_calendar':'SELECT "Id" FROM "CommunityEvent" WHERE "TenantId"=%s AND "Status"=\'Published\' AND "StartsAt">=now() AND "StartsAt"<now()+interval \'31 days\' ORDER BY "StartsAt" LIMIT 50'}.items():
  plans[name]=db.execute('EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON) '+statement,(tenant,)).fetchone()[0]

 timings={}
 for name,path in scenarios.items():
  for _ in range(3):response=call(path)
  samples=[]
  for _ in range(20):
   t=time.perf_counter();response=call(path);samples.append((time.perf_counter()-t)*1000)
  timings[name]={'median_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[18],3),'iterations':20,'local_target_p95_ms':1000,'target_met':sorted(samples)[18]<1000,'records':response.get('total')}
 result={'scenario':{'persons':100000,'memberships':100000,'chapters':10000,'posts':100000,'events':10000,'invitations':50000,'registrations':50000,'attendance':25000},'scope':'Warm, authenticated, single-client local HTTP API requests including cookie validation, authorization, privacy projection, PostgreSQL and serialization. Not a concurrency or production SLA claim. Dedicated benchmark database removed afterward.','fixture_insert_ms':insert_ms,'timings':timings,'explain_analyze_buffers':plans}
 (ROOT/'tests/community-api-performance.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(timings,indent=2))
 assert all(m['target_met'] for m in timings.values()),'An authenticated API benchmark target was not met'
finally:
 if server:server.terminate();server.wait(timeout=20)
 if log:log.close()
 if db:db.close()
 if created:
  assert database.startswith('assemblia_p3_bench_') and len(database)<40
  admin.execute(sql.SQL('DROP DATABASE {} WITH (FORCE)').format(sql.Identifier(database)))
 admin.close()
