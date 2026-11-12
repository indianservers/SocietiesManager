"""Authenticated communication benchmark on a disposable isolated PostgreSQL database."""
from api_runtime import API_DLL
from pathlib import Path
import os,json,uuid,subprocess,time,statistics,urllib.request,urllib.error,urllib.parse,http.cookiejar,ast,socket
import psycopg
from psycopg import sql
ROOT=Path(__file__).resolve().parents[1];c=json.loads((ROOT/'.local/dev.json').read_text());database='assemblia_p4_bench_'+uuid.uuid4().hex[:10]
created=False;server=None;db=None;log=None
admin=psycopg.connect(host='127.0.0.1',port=55439,dbname='postgres',user='postgres',password=c['password'],autocommit=True)
try:
 admin.execute(sql.SQL('CREATE DATABASE {}').format(sql.Identifier(database)));created=True
 env={**os.environ,'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database={database};Username=postgres;Password={c['password']}",'ASPNETCORE_ENVIRONMENT':'Development','Logging__LogLevel__Default':'Error','Communications__AllowTestProvider':'true'}
 subprocess.run(['dotnet',str(API_DLL),'--migrate'],cwd=ROOT/'backend',env=env,check=True,stdout=subprocess.DEVNULL)
 db=psycopg.connect(host='127.0.0.1',port=55439,dbname=database,user='postgres',password=c['password'])
 tree=ast.parse((ROOT/'tests/membership_benchmark.py').read_text(encoding='utf-8-sig'))
 seed=next(n.args[0].value for n in ast.walk(tree) if isinstance(n,ast.Call) and isinstance(n.func,ast.Attribute) and n.func.attr=='execute' and n.args and isinstance(n.args[0],ast.Constant) and isinstance(n.args[0].value,str) and 'CREATE TEMP TABLE bench_tenant' in n.args[0].value)
 start=time.perf_counter();db.execute(seed);tenant,chapter=db.execute('SELECT tenant,id FROM bench_chapters ORDER BY i LIMIT 1').fetchone();db.commit();insert_ms=round((time.perf_counter()-start)*1000,2)
 with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
 env['PublicUrl']=f'http://127.0.0.1:{port}'
 log=open(ROOT/'.local/communication-api-benchmark.log','w',encoding='utf-8');server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',env['PublicUrl']],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
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
 db.execute('UPDATE "GovernanceRoles" SET "Permissions"=%s WHERE "Id"=%s',(['units.read','members.read','communications.inbox','communications.send','communications.analytics','communications.highvolume','communications.accounts'],role));db.commit()
 # Construct deterministic SQL-only load fixtures. No external dispatch or fabricated real messages.
 def fill(table,overrides,source):
  columns=db.execute("SELECT column_name,data_type,is_nullable,udt_name FROM information_schema.columns WHERE table_schema='public' AND table_name=%s ORDER BY ordinal_position",(table,)).fetchall()
  names=[];values=[]
  for name,typ,nullable,udt in columns:
   names.append('"'+name+'"')
   defaults={'uuid':'gen_random_uuid()','text':"''",'integer':'0','bigint':'0','boolean':'false','timestamp with time zone':'now()','jsonb':"'{}'::jsonb",'ARRAY':"ARRAY[]::text[]"}
   values.append(overrides.get(name,'NULL' if nullable=='YES' else defaults.get(typ,'0')))
  db.execute('INSERT INTO "'+table+'" ('+','.join(names)+') SELECT '+','.join(values)+' '+source)
 tenant_sql="'"+str(tenant)+"'::uuid";user_sql="'"+str(user)+"'::uuid";chapter_sql="'"+str(chapter)+"'::uuid"
 fill('Conversation',{'TenantId':tenant_sql,'OwnerId':user_sql,'UnitId':chapter_sql,'Kind':"'Team'",'Channel':"'InApp'",'Subject':"'Synthetic load inquiry '||i",'Posting':"'Members'",'Status':"'Open'",'Priority':"'Normal'",'LastAt':"now()-i*interval '1 second'"},'FROM generate_series(1,1000) i')
 db.execute('CREATE TEMP TABLE comm_fixture AS SELECT "Id",row_number() over() i FROM "Conversation" WHERE "TenantId"=%s',(tenant,))
 fill('CommunicationMessage',{'TenantId':tenant_sql,'ConversationId':'c."Id"','SenderId':user_sql,'Direction':"'Outgoing'",'Body':"'Synthetic benchmark history '||g.i",'Status':"'Sent'",'ClientKey':'gen_random_uuid()::text','At':"now()-g.i*interval '1 second'"},'FROM generate_series(1,100000) g(i) JOIN comm_fixture c ON c.i=(g.i%1000)+1')
 campaign_id=str(uuid.uuid4())
 fill('CommunicationCampaign',{'Id':"'"+campaign_id+"'::uuid",'TenantId':tenant_sql,'AuthorId':user_sql,'Kind':"'Campaign'",'Name':"'Synthetic 50k recipient fixture'",'Channel':"'InApp'",'Status':"'Draft'",'Stages':"'[]'",'Priority':"'Normal'",'SnapshotCount':'50000'},'')
 fill('CampaignRecipient',{'TenantId':tenant_sql,'CampaignId':"'"+campaign_id+"'::uuid",'MembershipId':'m."Id"','PersonId':'m."PersonId"','Status':"'Pending'",'Variables':"'{}'"},'FROM (SELECT * FROM "Memberships" WHERE "TenantId"='+tenant_sql+' LIMIT 50000) m')
 fill('CommunicationNotification',{'TenantId':tenant_sql,'UserId':user_sql,'Title':"'Synthetic load notification '||i",'Kind':"'Message'",'DedupKey':'gen_random_uuid()::text','At':"now()-i*interval '1 second'"},'FROM generate_series(1,25000) i')
 db.commit();db.execute('ANALYZE');db.commit()
 prefix='/societies/'+str(tenant)+'/communications';conv=db.execute('SELECT "Id" FROM "Conversation" WHERE "TenantId"=%s LIMIT 1',(tenant,)).fetchone()[0]
 scenarios={'large_team_inbox':prefix+'/inbox','conversation_history':prefix+'/conversations/'+str(conv)+'/messages','notification_center':prefix+'/notifications','campaign_list':prefix+'/campaigns','recipient_snapshot_page':prefix+'/campaigns/'+campaign_id,'analytics':prefix+'/analytics'}
 plans={}
 for name,statement,args in [('team_inbox_cursor','SELECT "Id" FROM "Conversation" WHERE "TenantId"=%s AND "Kind"=%s ORDER BY "LastAt" DESC,"Id" DESC LIMIT 30',(tenant,'Team')),('message_history_cursor','SELECT "Id" FROM "CommunicationMessage" WHERE "TenantId"=%s AND "ConversationId"=%s ORDER BY "At" DESC,"Id" DESC LIMIT 50',(tenant,conv))]:
  plans[name]=db.execute('EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON) '+statement,args).fetchone()[0]
 db.commit()
 timings={}
 for name,path in scenarios.items():
  for _ in range(3):response=call(path)
  samples=[]
  for _ in range(20):
   t=time.perf_counter();response=call(path);samples.append((time.perf_counter()-t)*1000)
  timings[name]={'median_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[18],3),'iterations':20,'local_target_p95_ms':1000,'target_met':sorted(samples)[18]<1000,'records':response.get('total') if isinstance(response,dict) else len(response)}
 # Eight overlapping authenticated readers; cookie jar is read-only after login.
 from concurrent.futures import ThreadPoolExecutor
 import hmac,hashlib
 def reader(_):
  started=time.perf_counter()
  try:call(prefix+'/inbox');return ((time.perf_counter()-started)*1000,True)
  except Exception:return ((time.perf_counter()-started)*1000,False)
 started=time.perf_counter()
 with ThreadPoolExecutor(max_workers=8) as pool:concurrent=list(pool.map(reader,range(40)))
 concurrency={'concurrent_clients':8,'requests':40,'failures':sum(not ok for _,ok in concurrent),'p95_ms':round(sorted(ms for ms,_ in concurrent)[37],3),'requests_per_second':round(40/(time.perf_counter()-started),2)}
 account=call(prefix+'/accounts','POST',{'name':'Synthetic benchmark adapter','channel':'SMS','provider':'Test','sender':'+15551234567','providerReference':'benchmark','secrets':json.dumps({'mode':'Success','webhookSecret':'benchmark-signing-secret-at-least-32'}),'enabled':True,'inboundEnabled':False,'perMinute':1000})['id'];call(prefix+'/accounts/'+account+'/verify','POST')
 # A bounded 200-job worker throughput sample with no network provider calls.
 fill('Outbox',{'TenantId':tenant_sql,'AccountId':"'"+account+"'::uuid",'Channel':"'SMS'",'Recipient':"'+15551234567'",'Subject':"'Synthetic benchmark dispatch'",'Body':"'Mock only'",'Status':"'Queued'",'IdempotencyKey':'gen_random_uuid()::text'},'FROM generate_series(1,200) i');db.commit();started=time.perf_counter()
 while time.perf_counter()-started<30:
  accepted=db.execute('SELECT count(*) FROM "Outbox" WHERE "AccountId"=%s AND "Status"=%s',(account,'Accepted')).fetchone()[0];db.commit()
  if accepted==200:break
  time.sleep(.1)
 dispatch={'jobs':200,'accepted':accepted,'elapsed_ms':round((time.perf_counter()-started)*1000,2),'jobs_per_second':round(accepted/(time.perf_counter()-started),2),'adapter':'Development-only Test provider; acceptance is not final delivery'}
 def callback(i):
  payload=json.dumps({'id':'bench-event-'+str(i),'providerId':'unmatched-benchmark-reference','status':'Delivered'}).encode();stamp=str(int(time.time()));signature=hmac.new(b'benchmark-signing-secret-at-least-32',stamp.encode()+b'.'+payload,hashlib.sha256).hexdigest();req=urllib.request.Request(base+'/communications/webhooks/'+account,data=payload,method='POST',headers={'Content-Type':'application/json','X-Communication-Timestamp':stamp,'X-Communication-Signature':signature});started=time.perf_counter()
  try:
   with urllib.request.urlopen(req) as response:response.read();return ((time.perf_counter()-started)*1000,response.status==200)
  except Exception:return ((time.perf_counter()-started)*1000,False)
 with ThreadPoolExecutor(max_workers=8) as pool:callbacks=list(pool.map(callback,range(30)))
 webhook_burst={'concurrent_clients':8,'signed_events':30,'failures':sum(not ok for _,ok in callbacks),'p95_ms':round(sorted(ms for ms,_ in callbacks)[28],3),'stored_unique':db.execute('SELECT count(*) FROM "ProviderEvent" WHERE "AccountId"=%s',(account,)).fetchone()[0]};db.commit()
 result={'concurrency':concurrency,'dispatch':dispatch,'webhook_burst':webhook_burst,'scenario':{'persons':100000,'memberships':100000,'chapters':10000,'conversations':1000,'messages':100000,'campaign_recipients':50000,'notifications':25000},'scope':'Warm authenticated API requests against SQL-seeded synthetic fixtures. Twenty single-client samples per route plus the separately reported overlapping-reader, 200-job mock dispatch and signed callback samples. Does not establish full 50k dispatch throughput or a production SLA. Dedicated benchmark database removed afterward.','fixture_insert_ms':insert_ms,'timings':timings,'explain_analyze_buffers':plans}
 (ROOT/'tests/communication-api-performance.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(timings,indent=2))
 assert concurrency['failures']==0 and webhook_burst['failures']==0 and accepted==200
 assert all(m['target_met'] for m in timings.values()),'An authenticated API benchmark target was not met'
finally:
 if server:server.terminate();server.wait(timeout=20)
 if log:log.close()
 if db:db.close()
 if created:
  assert database.startswith('assemblia_p4_bench_') and len(database)<40
  admin.execute(sql.SQL('DROP DATABASE {} WITH (FORCE)').format(sql.Identifier(database)))
 admin.close()