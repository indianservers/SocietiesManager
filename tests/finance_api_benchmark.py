"""Authenticated finance benchmark on a disposable isolated PostgreSQL database."""
from api_runtime import API_DLL
from pathlib import Path
import os,json,uuid,subprocess,time,statistics,urllib.request,urllib.error,urllib.parse,http.cookiejar,ast,socket
import psycopg
from psycopg import sql
ROOT=Path(__file__).resolve().parents[1];c=json.loads((ROOT/'.local/dev.json').read_text());database='assemblia_p5_bench_'+uuid.uuid4().hex[:10]
created=False;server=None;db=None;log=None
admin=psycopg.connect(host='127.0.0.1',port=55439,dbname='postgres',user='postgres',password=c['password'],autocommit=True)
try:
 admin.execute(sql.SQL('CREATE DATABASE {}').format(sql.Identifier(database)));created=True
 env={**os.environ,'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database={database};Username=postgres;Password={c['password']}",'ASPNETCORE_ENVIRONMENT':'Development','Logging__LogLevel__Default':'Error','Finance__AllowTestGateway':'true'}
 subprocess.run(['dotnet',str(API_DLL),'--migrate'],cwd=ROOT/'backend',env=env,check=True,stdout=subprocess.DEVNULL)
 db=psycopg.connect(host='127.0.0.1',port=55439,dbname=database,user='postgres',password=c['password'])
 tree=ast.parse((ROOT/'tests/membership_benchmark.py').read_text(encoding='utf-8-sig'))
 seed=next(n.args[0].value for n in ast.walk(tree) if isinstance(n,ast.Call) and isinstance(n.func,ast.Attribute) and n.func.attr=='execute' and n.args and isinstance(n.args[0],ast.Constant) and isinstance(n.args[0].value,str) and 'CREATE TEMP TABLE bench_tenant' in n.args[0].value)
 start=time.perf_counter();db.execute(seed.replace('generate_series(1,10)','generate_series(1,10000)').replace('(p.i%10)+1','(p.i%10000)+1'));tenant,chapter=db.execute('SELECT tenant,id FROM bench_chapters ORDER BY i LIMIT 1').fetchone();db.commit();insert_ms=round((time.perf_counter()-start)*1000,2)
 with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
 env['PublicUrl']=f'http://127.0.0.1:{port}'
 log=open(ROOT/'.local/finance-api-benchmark.log','w',encoding='utf-8');server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',env['PublicUrl']],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
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
 db.execute('UPDATE "GovernanceRoles" SET "Permissions"=%s WHERE "Id"=%s',(['units.read','members.read','finance.read','finance.configure','finance.invoice','finance.collect','finance.dues','finance.highvalue','finance.export','finance.providers','finance.approve','finance.reconcile'],role));db.commit()
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
 started=time.perf_counter();db.execute('''CREATE TEMP TABLE finance_fixture AS SELECT gen_random_uuid() id,gen_random_uuid() journal,m."Id" membership,m."ChapterId" chapter,p."FullName" name,row_number() over(order by m."Id") i FROM "Memberships" m JOIN "Persons" p ON p."Id"=m."PersonId" WHERE m."TenantId"=%s''',(tenant,))
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
 assert counts['chapters']==10000 and counts['FinanceInvoice']==100000 and counts['JournalLine']==200000
 db.execute('ANALYZE');db.commit()
 timings={};plans={}
 for name,statement,args in [('invoice_due_index','SELECT "Id" FROM "FinanceInvoice" WHERE "TenantId"=%s AND "EntityId"=%s AND "Status"=%s ORDER BY "DueOn" LIMIT 50',(tenant,entity,'Issued')),('member_statement','SELECT "Id","Debit","Credit" FROM "JournalLine" WHERE "TenantId"=%s AND "MembershipId"=(SELECT membership FROM finance_fixture LIMIT 1)',(tenant,))]:plans[name]=db.execute('EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON) '+statement,args).fetchone()[0]
 db.commit()
 scenarios={'invoice_register':prefix+'/invoices?entity='+entity,'chapter_filter':prefix+'/invoices?entity='+entity+'&workspace='+str(chapter),'dashboard':prefix+'/dashboard?entity='+entity,'ledger_cursor':prefix+'/ledger?entity='+entity,'trial_balance':prefix+'/reports?report=Trial%20Balance&entity='+entity,'ageing':prefix+'/reports?report=Ageing&entity='+entity}
 for name,path in scenarios.items():
  for _ in range(2):call(path)
  samples=[]
  for _ in range(12):
   started=time.perf_counter();response=call(path);samples.append((time.perf_counter()-started)*1000)
  timings[name]={'median_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[-1],3),'samples':12,'local_target_p95_ms':2000,'target_met':max(samples)<2000}
 # A real, completed 1,000-member billing batch on the 100,000-member database.
 category=uuid.uuid4();db.execute('INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active") VALUES (%s,%s,%s,%s,true)',(category,tenant,'Bounded annual performance cohort','Synthetic 1000-member billing sample'));db.execute('UPDATE "Memberships" SET "CategoryId"=%s,"Status"=%s WHERE "Id" IN(SELECT membership FROM finance_fixture ORDER BY i LIMIT 1000)',(category,'Active'));db.commit()
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
 result={'scenario':counts,'fixture_insert_ms':insert_ms,'accounting_fixture_ms':accounting_fixture_ms,'timings':timings,'annual_billing':billing,'concurrent_reads':concurrent,'explain_analyze_buffers':plans,'scope':'Freshly migrated disposable PostgreSQL database; source-linked 100000 issued invoices, 100000 balanced posted journals and 200000 immutable lines. Synthetic SQL load fixtures remain distinct from measured actual API billing. No network payment provider or real recipient.'}
 (ROOT/'tests/finance-api-performance.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps({k:result[k] for k in ['timings','annual_billing','concurrent_reads']},indent=2),flush=True)
 assert billing['state']=='Completed' and billing['processed']==1000 and concurrent['failures']==0
 assert all(v['target_met'] for v in timings.values()),'A local finance API target was exceeded'
finally:
 if server:server.terminate();server.wait(timeout=20)
 if log:log.close()
 if db:db.close()
 if created:
  assert database.startswith('assemblia_p5_bench_') and len(database)<40
  admin.execute(sql.SQL('DROP DATABASE {} WITH (FORCE)').format(sql.Identifier(database)))
 admin.close()
