"""Authenticated finance benchmark on a disposable isolated PostgreSQL database."""
from api_runtime import API_DLL
from pathlib import Path
import os,json,uuid,subprocess,time,statistics,urllib.request,urllib.error,urllib.parse,http.cookiejar,ast,socket
import psycopg
from psycopg import sql
ROOT=Path(__file__).resolve().parents[1];c=json.loads((ROOT/'.local/dev.json').read_text());database='assemblia_p6_bench_'+uuid.uuid4().hex[:10]
created=False;server=None;db=None;log=None
admin=psycopg.connect(host='127.0.0.1',port=55439,dbname='postgres',user='postgres',password=c['password'],autocommit=True)
try:
 admin.execute(sql.SQL('CREATE DATABASE {}').format(sql.Identifier(database)));created=True
 env={**os.environ,'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database={database};Username=postgres;Password={c['password']}",'ASPNETCORE_ENVIRONMENT':'Development','Logging__LogLevel__Default':'Error','Finance__AllowTestGateway':'true'}
 subprocess.run(['dotnet',str(API_DLL),'--migrate'],cwd=ROOT/'backend',env=env,check=True,stdout=subprocess.DEVNULL)
 db=psycopg.connect(host='127.0.0.1',port=55439,dbname=database,user='postgres',password=c['password'])
 tree=ast.parse((ROOT/'tests/membership_benchmark.py').read_text(encoding='utf-8-sig'))
 seed=next(n.args[0].value for n in ast.walk(tree) if isinstance(n,ast.Call) and isinstance(n.func,ast.Attribute) and n.func.attr=='execute' and n.args and isinstance(n.args[0],ast.Constant) and isinstance(n.args[0].value,str) and 'CREATE TEMP TABLE bench_tenant' in n.args[0].value)
 start=time.perf_counter();db.execute(seed.replace('generate_series(1,10)','generate_series(1,10000)').replace('(p.i%10)+1','(p.i%10000)+1'));tenant,chapter=db.execute('SELECT tenant,id FROM bench_chapters ORDER BY i LIMIT 1').fetchone();db.execute((ROOT/'tests/platform_scale_fixture.sql').read_text());db.commit();insert_ms=round((time.perf_counter()-start)*1000,2)
 with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
 env['PublicUrl']=f'http://127.0.0.1:{port}'
 log=open(ROOT/'.local/phase6-api-benchmark.log','w',encoding='utf-8');server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',env['PublicUrl']],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
 base=env['PublicUrl']+'/api/v1';op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def call(path,method='GET',body=None,allow_refresh=True):
  headers={'Content-Type':'application/json'}
  if method!='GET':headers['X-CSRF-TOKEN']=call('/auth/csrf')['token']
  req=urllib.request.Request(base+path,method=method,headers=headers,data=json.dumps(body).encode() if body is not None else (b'' if method!='GET' else None))
  try:
   with op.open(req,timeout=120) as response:return json.loads(response.read() or 'null')
  except urllib.error.HTTPError as error:
   if error.code==401 and path!='/auth/login' and allow_refresh:
    call('/auth/login','POST',{'email':email,'password':password},False)
    return call(path,method,body,False)
   raise
 for _ in range(60):
  try:call('/auth/csrf');break
  except Exception:time.sleep(.25)
 email='benchmark-'+uuid.uuid4().hex[:8]+'@example.test';password=uuid.uuid4().hex+'!aA1'
 call('/auth/signup','POST',{'email':email,'password':password,'fullName':'Synthetic Benchmark Reader'})
 user=db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(email,)).fetchone()[0];body=db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s',(email,)).fetchone()[0];token=urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
 call('/auth/verify?userId='+str(user)+'&token='+urllib.parse.quote(token,safe=''),'POST');call('/auth/login','POST',{'email':email,'password':password})
 role=uuid.uuid4();assignment=uuid.uuid4()
 db.execute('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s)',(tenant,user));db.execute('INSERT INTO "GovernanceRoles" ("Id","TenantId","Name","Permissions","Responsibilities") VALUES (%s,%s,%s,%s,%s)',(role,tenant,'Benchmark Reader',['units.read','members.read'],'Synthetic benchmark fixture'));db.execute('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt","EndsAt","MembershipId","RevokedAt") VALUES (%s,%s,%s,%s,NULL,now()-interval \'1 day\',NULL,NULL,NULL)',(assignment,tenant,user,role));db.commit()
 db.execute('UPDATE "GovernanceRoles" SET "Permissions"=%s WHERE "Id"=%s',(['units.read','members.read','finance.read','finance.configure','finance.invoice','finance.collect','finance.dues','finance.highvalue','finance.export','finance.providers','finance.approve','finance.reconcile'],role));db.commit()

 # Add all existing declared permissions to this synthetic society role.
 permissions=sorted(set(__import__('re').findall(r'"([a-z]+\.[a-z]+)"',(ROOT/'backend/Security.cs').read_text())+__import__('re').findall(r'"([a-z]+\.[a-z]+)"',(ROOT/'backend/StudioService.cs').read_text())))
 db.execute('UPDATE "GovernanceRoles" SET "Permissions"=%s WHERE "Id"=%s',(permissions,role));db.execute('UPDATE "AspNetUsers" SET "PlatformAdmin"=true WHERE "Id"=%s',(user,))
 reviewer=uuid.uuid4();reviewemail='review-'+uuid.uuid4().hex+'@example.test'
 db.execute("""INSERT INTO "AspNetUsers" SELECT (jsonb_populate_record(NULL::"AspNetUsers",to_jsonb(u)||jsonb_build_object('Id',%s::uuid,'UserName',%s::text,'NormalizedUserName',upper(%s),'Email',%s::text,'NormalizedEmail',upper(%s),'SecurityStamp',gen_random_uuid()::text,'ConcurrencyStamp',gen_random_uuid()::text))).* FROM "AspNetUsers" u WHERE u."Id"=%s""",(reviewer,reviewemail,reviewemail,reviewemail,reviewemail,user))
 db.execute('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s)',(tenant,reviewer));db.execute('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","StartsAt") VALUES (%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,reviewer,role));db.commit()
 rop=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def review_call(path,method='GET',body=None,allow_refresh=True):
  headers={'Content-Type':'application/json'}
  if method!='GET':headers['X-CSRF-TOKEN']=review_call('/auth/csrf')['token']
  request=urllib.request.Request(base+path,method=method,headers=headers,data=json.dumps(body).encode() if body is not None else (b'' if method!='GET' else None))
  try:
   with rop.open(request,timeout=120) as response:return json.loads(response.read() or 'null')
  except urllib.error.HTTPError as error:
   if error.code==401 and path!='/auth/login' and allow_refresh:
    review_call('/auth/login','POST',{'email':reviewemail,'password':password},False)
    return review_call(path,method,body,False)
   raise
 review_call('/auth/login','POST',{'email':reviewemail,'password':password})
 import psutil,platform,threading,math
 metrics={'peak_api_rss_bytes':0,'api_cpu_seconds':0};monitor_stop=threading.Event();process=psutil.Process(server.pid)
 def monitor():
  while not monitor_stop.wait(.25):
   try:
    metrics['peak_api_rss_bytes']=max(metrics['peak_api_rss_bytes'],process.memory_info().rss);cpu=process.cpu_times();metrics['api_cpu_seconds']=round(cpu.user+cpu.system,3)
   except psutil.Error:break
 thread=threading.Thread(target=monitor,daemon=True);thread.start()
 prefix='/societies/'+str(tenant)+'/finance'
 region=db.execute('SELECT region FROM bench_tenant').fetchone()[0]
 # Configure through the real API; financial entities are never assumed from chapters.
 chapterids=[str(r[0]) for r in db.execute('SELECT id FROM bench_chapters ORDER BY i').fetchall()];db.commit()
 entity=call(prefix+'/entities','POST',{'name':'Synthetic scale entity','legalName':'Synthetic Benchmark Accounting Entity','unitId':str(region),'units':[str(region),*chapterids],'currency':'INR','requireInvoiceApproval':False})['id']
 ar,inc=[db.execute('SELECT "Id" FROM "LedgerAccount" WHERE "EntityId"=%s AND "Code"=%s',(entity,code)).fetchone()[0] for code in ['1100','4000']]
 period=db.execute('SELECT "Id" FROM "AccountingPeriod" WHERE "EntityId"=%s AND "StartsOn"<=current_date AND "EndsOn">=current_date',(entity,)).fetchone()[0]
 def fill(table,overrides,source):
  columns=db.execute("SELECT column_name,data_type,is_nullable FROM information_schema.columns WHERE table_schema='public' AND table_name=%s ORDER BY ordinal_position",(table,)).fetchall();names=[];values=[]
  defaults={'uuid':'gen_random_uuid()','text':"''",'integer':'0','bigint':'0','numeric':'0','boolean':'false','timestamp with time zone':'now()','date':'current_date','jsonb':"'{}'::jsonb",'ARRAY':"ARRAY[]::text[]"}
  for name,typ,nullable in columns:names.append('"'+name+'"');values.append(overrides.get(name,'NULL' if nullable=='YES' else defaults.get(typ,'0')))
  db.execute('INSERT INTO "'+table+'" ('+','.join(names)+') SELECT '+','.join(values)+' '+source)
 literal=lambda value:"'"+str(value)+"'::uuid"
 common={'TenantId':literal(tenant),'EntityId':literal(entity),'UnitId':'f.chapter'}
 started=time.perf_counter();db.execute('''CREATE TEMP TABLE finance_fixture AS SELECT gen_random_uuid() id,gen_random_uuid() journal,m."Id" membership,m."ChapterId" chapter,p."FullName" name,row_number() over(order by m."Id") i FROM "Memberships" m JOIN "Persons" p ON p."Id"=m."PersonId" CROSS JOIN generate_series(1,3) cycle WHERE m."TenantId"=%s''',(tenant,))
 fill('JournalEntry',{**common,'Id':'f.journal','PeriodId':literal(period),'Number':"'BENCH-J-'||f.i",'Date':'current_date','Currency':"'INR'",'Purpose':"'Synthetic historical membership billing'",'SourceKind':"'Invoice'",'SourceId':'f.id','SourceKey':"'invoice:'||f.id",'AuthorId':literal(user),'Posted':'false'},'FROM finance_fixture f')
 fill('FinanceInvoice',{**common,'Id':'f.id','AuthorId':literal(user),'Number':"'BENCH-INV-'||f.i",'Kind':"'Invoice'",'MembershipId':'f.membership','Purpose':"'Synthetic annual membership'",'CounterpartyName':'f.name','Date':'current_date','DueOn':"current_date-((f.i%120)::int)",'Currency':"'INR'",'Lines':"'[{}]'",'Subtotal':'100','Total':'100','Paid':'0','Credited':'0','Status':"'Issued'",'JournalId':'f.journal','IssuerSnapshot':"'{\"LegalName\":\"Synthetic Benchmark Accounting Entity\",\"Address\":\"\",\"TaxRegistration\":\"\",\"Currency\":\"INR\",\"Country\":\"IN\"}'"},'FROM finance_fixture f')
 db.execute('ANALYZE \"JournalEntry\";ANALYZE \"FinanceInvoice\"')
 fill('JournalLine',{**common,'InvoiceId':'f.id','JournalId':'f.journal','AccountId':literal(ar),'Debit':'100','Credit':'0','MembershipId':'f.membership'},'FROM finance_fixture f')
 fill('JournalLine',{**common,'InvoiceId':'f.id','JournalId':'f.journal','AccountId':literal(inc),'Debit':'0','Credit':'100','MembershipId':'f.membership'},'FROM finance_fixture f')
 db.execute('ANALYZE \"JournalLine\"')
 db.execute('UPDATE "JournalEntry" SET "Posted"=true,"PostedAt"=now() WHERE "TenantId"=%s AND "EntityId"=%s',(tenant,entity));db.execute('ANALYZE "JournalEntry"');db.commit();accounting_fixture_ms=round((time.perf_counter()-started)*1000,2)
 print('Balanced accounting fixture committed',flush=True)
 # Assert actual counts, rather than trusting descriptive labels.
 counts={table:db.execute('SELECT count(*) FROM "'+table+'" WHERE "TenantId"=%s',(tenant,)).fetchone()[0] for table in ['Persons','Memberships','Units','FinanceInvoice','JournalEntry','JournalLine'] if table!='Persons'}
 counts['chapters']=db.execute('SELECT count(*) FROM bench_chapters').fetchone()[0];counts['persons']=db.execute('SELECT count(*) FROM bench_persons').fetchone()[0]
 assert counts['chapters']==10000 and counts['FinanceInvoice']==300000 and counts['JournalLine']==600000
 db.execute('ANALYZE');db.commit()

 global_counts={'societies':db.execute('SELECT count(*) FROM "Tenants"').fetchone()[0],'regions':db.execute('SELECT count(*) FROM "Units" u JOIN "Levels" l ON l."Id"=u."LevelId" WHERE l."Kind"=%s',('Region',)).fetchone()[0],'chapters':db.execute('SELECT count(*) FROM "Units" u JOIN "Levels" l ON l."Id"=u."LevelId" WHERE l."Kind"=%s',('Chapter',)).fetchone()[0]}
 assert global_counts=={'societies':1000,'regions':10000,'chapters':100000},global_counts
 community_sql='\n'.join(line for line in (ROOT/'tests/community_fixture.sql').read_text().splitlines() if not line.startswith('INSERT INTO "Units"')).replace('%10)+1','%10000)+1')
 db.execute(community_sql);db.commit()
 tenant_sql=literal(tenant);user_sql=literal(user)
 fill('Conversation',{'TenantId':tenant_sql,'OwnerId':user_sql,'UnitId':literal(chapter),'Kind':"'Team'",'Channel':"'InApp'",'Subject':"'Synthetic scale inquiry '||i",'Posting':"'Members'",'Status':"'Open'",'Priority':"'Normal'",'LastAt':"now()-i*interval '1 second'"},'FROM generate_series(1,1000) i')
 db.execute('CREATE TEMP TABLE comm_fixture AS SELECT "Id",row_number() over() i FROM "Conversation" WHERE "TenantId"=%s',(tenant,))
 fill('CommunicationMessage',{'TenantId':tenant_sql,'ConversationId':'c."Id"','SenderId':user_sql,'Direction':"'Outgoing'",'Body':"'Synthetic scale history '||g.i",'Status':"'Sent'",'ClientKey':'gen_random_uuid()::text','At':"now()-g.i*interval '1 second'"},'FROM generate_series(1,100000) g(i) JOIN comm_fixture c ON c.i=(g.i%1000)+1')
 db.execute("""CREATE TEMP TABLE project_fixture AS SELECT gen_random_uuid() id,c.id chapter,c.tenant,c.i,(SELECT m."Id" FROM "Memberships" m WHERE m."TenantId"=c.tenant AND m."ChapterId"=c.id ORDER BY m."Id" LIMIT 1) member FROM bench_chapters c""")
 fill('ServiceProject',{'TenantId':tenant_sql,'Id':'f.id','UnitId':'f.chapter','Name':"'Synthetic joint project '||f.i",'Category':"'Community Service'",'Description':"'Synthetic scale fixture'",'Objectives':"'Measured verified impact'",'ManagerId':'f.member','StartsOn':'current_date-30','EndsOn':'current_date+30','Status':"'Approved'",'AuthorId':user_sql,'ApproverId':literal(reviewer)},'FROM project_fixture f')
 fill('ProjectChapter',{'TenantId':tenant_sql,'ProjectId':'f.id','ChapterId':'f.chapter'},'FROM project_fixture f')
 metric=uuid.uuid4();fill('ImpactMetric',{'Id':literal(metric),'TenantId':tenant_sql,'Name':"'Synthetic people served'",'Unit':"'People'",'Category':"'Service'",'Aggregation':"'Sum'",'EvidenceRequired':'true','Active':'true'},'')
 fill('ImpactResult',{'TenantId':tenant_sql,'ProjectId':'f.id','MetricId':literal(metric),'OutcomeKey':"'scale-project-'||f.i",'Value':'10','Evidence':"'Synthetic verified evidence'",'Status':"'Verified'",'AuthorId':user_sql,'VerifierId':literal(reviewer)},'FROM project_fixture f')
 db.commit();db.execute('ANALYZE');db.commit()
 global_counts.update({'memberships':db.execute('SELECT count(*) FROM "Memberships"').fetchone()[0],'invoices':counts['FinanceInvoice'],'journal_lines':counts['JournalLine'],'activities':db.execute('SELECT count(*) FROM "ActivityPost"').fetchone()[0],'events':db.execute('SELECT count(*) FROM "CommunityEvent"').fetchone()[0],'messages':db.execute('SELECT count(*) FROM "CommunicationMessage"').fetchone()[0],'service_projects':db.execute('SELECT count(*) FROM "ServiceProject"').fetchone()[0]})
 print('Phase 6 capacity fixture committed',json.dumps(global_counts),flush=True)
 timings={};plans={}
 for name,statement,args in [('invoice_due_index','SELECT "Id" FROM "FinanceInvoice" WHERE "TenantId"=%s AND "EntityId"=%s AND "Status"=%s ORDER BY "DueOn" LIMIT 50',(tenant,entity,'Issued')),('member_statement','SELECT "Id","Debit","Credit" FROM "JournalLine" WHERE "TenantId"=%s AND "MembershipId"=(SELECT membership FROM finance_fixture LIMIT 1)',(tenant,))]:plans[name]=db.execute('EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON) '+statement,args).fetchone()[0]
 db.commit()
 scenarios={'invoice_register':prefix+'/invoices?entity='+entity,'chapter_filter':prefix+'/invoices?entity='+entity+'&workspace='+str(chapter),'dashboard':prefix+'/dashboard?entity='+entity,'ledger_cursor':prefix+'/ledger?entity='+entity,'trial_balance':prefix+'/reports?report=Trial%20Balance&entity='+entity,'ageing':prefix+'/reports?report=Ageing&entity='+entity}
 for name,path in scenarios.items():
  for _ in range(2):call(path)
  samples=[]
  for _ in range(30):
   started=time.perf_counter();response=call(path);samples.append((time.perf_counter()-started)*1000)
  timings[name]={'median_ms':round(statistics.median(samples),3),'p50_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[28],3),'p99_ms':round(sorted(samples)[29],3),'samples':30,'local_target_p95_ms':2000,'target_met':sorted(samples)[28]<2000}
 # A real, completed 1,000-member billing batch on the 100,000-member database.
 category=uuid.uuid4();db.execute('INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active") VALUES (%s,%s,%s,%s,true)',(category,tenant,'Bounded annual performance cohort','Synthetic 1000-member billing sample'));db.execute('UPDATE "Memberships" SET "CategoryId"=%s,"Status"=%s WHERE "Id" IN(SELECT DISTINCT membership FROM finance_fixture ORDER BY membership LIMIT 1000)',(category,'Active'));db.commit()
 rule=call(prefix+'/dues-plans','POST',{'entityId':entity,'unitId':str(region),'name':'Actual annual billing throughput','code':'BENCH-ANNUAL','amount':'120.00','categoryId':str(category),'requiresApproval':True,'effectiveFrom':time.strftime('%Y-%m-%d'),'frequency':'Annual'})
 import datetime
 start=datetime.datetime.now(datetime.timezone.utc).date();end=start.replace(year=start.year+1)-datetime.timedelta(days=1)
 started=time.perf_counter();job=call(prefix+'/assessments','POST',{'entityId':entity,'planId':rule['id'],'startsOn':str(start),'endsOn':str(end),'bulk':True,'key':'benchmark-annual-batch'})
 while time.perf_counter()-started<180:
  state=db.execute('SELECT "State","Processed","Error" FROM "FinanceJob" WHERE "Id"=%s',(job['id'],)).fetchone();db.commit()
  if state[0] in ['Completed','Failed']:break
  time.sleep(.2)
 billing={'cohort_members':1000,'state':state[0],'processed':state[1],'error':state[2],'elapsed_ms':round((time.perf_counter()-started)*1000,2),'invoices_per_second':round(state[1]/(time.perf_counter()-started),2),'scope':'Actual assessment and pending-review invoice creation; not issued-journal/PDF/payment throughput for 100000 recipients'}
 from concurrent.futures import ThreadPoolExecutor
 def reader(_):
  started=time.perf_counter()
  try:call(prefix+'/invoices?entity='+entity);return((time.perf_counter()-started)*1000,True)
  except Exception:return((time.perf_counter()-started)*1000,False)
 with ThreadPoolExecutor(max_workers=6) as pool:reads=list(pool.map(reader,range(24)))
 concurrent={'overlapping_readers':6,'requests':24,'failures':sum(not ok for _,ok in reads),'p95_ms':round(sorted(ms for ms,_ in reads)[22],3)}

 studio='/societies/'+str(tenant)+'/studio';service='/societies/'+str(tenant)+'/service';intel='/societies/'+str(tenant)+'/intelligence'
 review_call('/auth/login','POST',{'email':reviewemail,'password':password})
 extra_timings={}
 for name,path,method,body in [('integrated_intelligence',intel+'/summary','GET',None),('bulk_selection_100k',studio+'/selection','POST',{}),('project_directory',service+'/projects','GET',None),('verified_impact',service+'/impact','GET',None),('unified_members_report',intel+'/reports/query','POST',{'report':'Members'}),('platform_tenant_page','/platform/tenants','GET',None)]:
  # The reviewer is a separate authenticated identity, preserving the production request limits.
  if name=='platform_tenant_page':db.execute('UPDATE "AspNetUsers" SET "PlatformAdmin"=true WHERE "Id"=%s',(reviewer,));db.commit()
  review_call(path,method,body);samples=[];errors=[]
  for _ in range(30):
   started=time.perf_counter()
   try:review_call(path,method,body)
   except Exception as error:errors.append(type(error).__name__)
   samples.append((time.perf_counter()-started)*1000)
  extra_timings[name]={'p50_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[28],3),'p99_ms':round(sorted(samples)[29],3),'samples':30,'failures':len(errors),'error_rate':len(errors)/30,'local_target_p95_ms':5000,'target_met':not errors and sorted(samples)[28]<5000}
 (ROOT/'.local/phase6-read-checkpoint.json').write_text(json.dumps({'scenario':global_counts,'timings':timings,'phase6_timings':extra_timings,'annual_billing':billing,'concurrent_reads':concurrent},indent=2));print('Phase 6 read measurements completed',flush=True)
 def completed_job(id,deadline=900):
  started=time.perf_counter()
  while time.perf_counter()-started<deadline:
   row=db.execute('SELECT "State","Processed","Failed","Error" FROM "StudioJob" WHERE "Id"=%s',(id,)).fetchone();db.commit()
   if row[0] in ['Completed','Completed with errors','Failed']:return row,(time.perf_counter()-started)*1000
   time.sleep(.25)
  raise AssertionError('PDF queue did not complete: '+str(row))
 memberids=[str(row[0]) for row in db.execute('SELECT "Id" FROM "Memberships" WHERE "TenantId"=%s AND "CategoryId"=%s ORDER BY "Id" LIMIT 1000',(tenant,category)).fetchall()];db.commit();assert len(memberids)==1000
 call('/auth/login','POST',{'email':email,'password':password})
 generation={}
 for kind,selected in [('Card',memberids),('Certificate',memberids[:100])]:
  design=call(studio+'/starter?kind='+kind);template=call(studio+'/templates','POST',{'name':'Synthetic performance '+kind,'kind':kind,'category':'Ordinary Member' if kind=='Card' else 'Appreciation certificate','design':json.dumps(design)})
  call(studio+'/templates/'+template['id']+'/publish','POST');version=call(studio+'/templates/'+template['id'])['versions'][0]['id']
  job=call(studio+'/jobs','POST',{'templateVersionId':version,'selection':{'membershipIds':selected},'expiryDate':str(start.replace(year=start.year+1)),'key':'pdf-throughput-'+kind,'reason':'Synthetic scale generation'})
  review_call(studio+'/jobs/'+job['id']+'/decision','POST',{'decision':'Approve','note':'Independent synthetic performance approval'})
  state,elapsed=completed_job(job['id']);assert state[0]=='Completed' and state[1]==len(selected) and state[2]==0,(kind,state)
  generation[kind]={'recipients':len(selected),'state':state[0],'issued':state[1],'failed':state[2],'elapsed_ms':round(elapsed,3),'documents_per_second':round(state[1]/(elapsed/1000),3),'rendering':'Actual server-side 300 DPI configured-side image and physical-size PDF generation, not SQL fixtures'}
  if kind=='Card':card_job=job['id']
 ids=[str(row[0]) for row in db.execute('SELECT "Id" FROM "Credential" WHERE "JobId"=%s ORDER BY "Id" LIMIT 1000',(card_job,)).fetchall()];db.commit()
 export=call(studio+'/exports','POST',{'ids':ids,'key':'scale-zip-1000','layout':{'format':'zip-pdf','sides':'Both'}});state,elapsed=completed_job(export['id']);assert state[0]=='Completed'
 generation['ZIP']={'credentials':len(ids),'state':state[0],'elapsed_ms':round(elapsed,3),'scope':'Actual durable ZIP export generation; download authorization tested in integration suite'}
 for name,statement,args in [('credential_worker','SELECT "Id" FROM "Credential" WHERE "Status"=%s ORDER BY "At" LIMIT 1',('Approved',)),('project_verified_impact','SELECT "MetricId",sum("Value") FROM "ImpactResult" WHERE "TenantId"=%s AND "Status"=%s GROUP BY "MetricId"',(tenant,'Verified'))]:plans[name]=db.execute('EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON) '+statement,args).fetchone()[0]
 db.commit();monitor_stop.set();thread.join(timeout=2)
 result={'scenario':global_counts,'main_tenant_distribution':counts,'phase6_timings':extra_timings,'document_generation':generation,'api_process_metrics':metrics,'hardware':{'os':platform.platform(),'logical_cpu_count':os.cpu_count(),'processor':platform.processor(),'memory_bytes':psutil.virtual_memory().total},'fixture_insert_ms':insert_ms,'accounting_fixture_ms':accounting_fixture_ms,'timings':timings,'annual_billing':billing,'concurrent_reads':concurrent,'explain_analyze_buffers':plans,'scope':'Freshly migrated disposable PostgreSQL database. Global totals: 1000 societies, 10000 regions, 100000 chapters. Main tenant holds the 100000 memberships, 300000 synthetic source-linked invoices and large activity/message fixtures. Real API billing and actual 1000-card/100-certificate generation are measured separately. Percentiles use 30 warm local samples, not production tail guarantees. No live provider or real recipient.'}
 (ROOT/'tests/phase6-performance.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps({k:result[k] for k in ['timings','annual_billing','concurrent_reads']},indent=2),flush=True)
 assert billing['state']=='Completed' and billing['processed']==1000 and concurrent['failures']==0
 assert all(v['target_met'] for v in timings.values()) and all(v['target_met'] for v in extra_timings.values()),'A documented local API target was exceeded'
finally:
 if server:server.terminate();server.wait(timeout=20)
 if log:log.close()
 if db:db.close()
 if created:
  assert database.startswith('assemblia_p6_bench_') and len(database)<40
  admin.execute(sql.SQL('DROP DATABASE {} WITH (FORCE)').format(sql.Identifier(database)))
 admin.close()
