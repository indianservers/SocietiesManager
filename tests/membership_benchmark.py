"""Single-client warm PostgreSQL membership benchmark. All fixtures roll back."""
from pathlib import Path
import json,time,statistics,psycopg
ROOT=Path(__file__).resolve().parents[1];c=json.loads((ROOT/'.local/dev.json').read_text())
db=psycopg.connect(host='127.0.0.1',port=55439,dbname='assemblia_test',user='postgres',password=c['password'])
try:
 start=time.perf_counter()
 db.execute('''CREATE TEMP TABLE bench_tenant AS SELECT gen_random_uuid() id,gen_random_uuid() region_level,gen_random_uuid() chapter_level,gen_random_uuid() region,gen_random_uuid() category;
 INSERT INTO "Tenants" ("Id","Name","Status","Profile","CreatedAt") SELECT id,'Membership benchmark','Active','{}',now() FROM bench_tenant;
 INSERT INTO "Levels" ("Id","TenantId","Name","Kind","AllowedParents","AllowRoot") SELECT region_level,id,'Region','Region',ARRAY[]::uuid[],true FROM bench_tenant;
 INSERT INTO "Levels" ("Id","TenantId","Name","Kind","AllowedParents","AllowRoot") SELECT chapter_level,id,'Chapter','Chapter',ARRAY[region_level],false FROM bench_tenant;
 INSERT INTO "Units" ("Id","TenantId","ParentId","LevelId","Name","Code","Status","Details","CreatedAt","UpdatedAt","Revision") SELECT region,id,NULL,region_level,'Example Region','BENCH-REG','Active','{}',now(),now(),1 FROM bench_tenant;
 CREATE TEMP TABLE bench_chapters AS SELECT gen_random_uuid() id,t.id tenant,t.region,t.chapter_level,i FROM bench_tenant t CROSS JOIN generate_series(1,10) i;
 INSERT INTO "Units" ("Id","TenantId","ParentId","LevelId","Name","Code","Status","Details","CreatedAt","UpdatedAt","Revision") SELECT id,tenant,region,chapter_level,'Example Chapter '||i,'BENCH-'||i,'Active','{}',now(),now(),1 FROM bench_chapters;
 INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active") SELECT category,id,'Regular','Benchmark category',true FROM bench_tenant;
 CREATE TEMP TABLE bench_persons AS SELECT gen_random_uuid() id,i FROM generate_series(1,100000) i;
 INSERT INTO "Persons" ("Id","UserId","FullName","SearchName","CreatedAt","Revision") SELECT id,NULL,'Member '||lpad(i::text,6,'0'),'member '||lpad(i::text,6,'0'),now(),0 FROM bench_persons;
 INSERT INTO "Memberships" ("Id","TenantId","PersonId","ChapterId","CategoryId","Number","Status","JoinedAt","EffectiveFrom","EndsAt","Profile","Privacy","Revision") SELECT gen_random_uuid(),t.id,p.id,c.id,t.category,'BENCH-M-'||p.i,CASE WHEN p.i%17=0 THEN 'Inactive' ELSE 'Active' END,now()-(p.i%365)*interval '1 day',now()-(p.i%365)*interval '1 day',NULL,jsonb_build_object('city','Example City','email','member'||p.i||'@example.test'),'{"directory":true,"photo":false,"contacts":false,"professional":false}',0 FROM bench_persons p CROSS JOIN bench_tenant t JOIN bench_chapters c ON c.i=(p.i%10)+1;
 ANALYZE "Persons";ANALYZE "Memberships";''')
 insert_ms=round((time.perf_counter()-start)*1000,2)
 tenant,chapter=db.execute('SELECT tenant,id FROM bench_chapters ORDER BY i LIMIT 1').fetchone()
 statements={
 'chapter_directory_page':('SELECT m."Id",m."Number",p."FullName",m."Status" FROM "Memberships" m JOIN "Persons" p ON p."Id"=m."PersonId" WHERE m."TenantId"=%s AND m."ChapterId"=%s ORDER BY m."Number",m."Id" LIMIT 20',(tenant,chapter)),
 'large_name_search':('SELECT m."Id",m."Number",p."FullName" FROM "Memberships" m JOIN "Persons" p ON p."Id"=m."PersonId" WHERE m."TenantId"=%s AND p."SearchName" LIKE %s ORDER BY p."FullName",m."Id" LIMIT 20',(tenant,'%member 00012%')),
 'chapter_active_counts':('SELECT "ChapterId",count(*),count(*) FILTER(WHERE "Status"=\'Active\') FROM "Memberships" WHERE "TenantId"=%s GROUP BY "ChapterId"',(tenant,)),
 'privacy_filtered_directory':('SELECT m."Id",m."Number",p."FullName",m."Profile"->>\'city\' AS city FROM "Memberships" m JOIN "Persons" p ON p."Id"=m."PersonId" WHERE m."TenantId"=%s AND m."ChapterId"=%s AND m."Status"=\'Active\' AND m."Privacy" @> \'{"directory":true}\' AND m."EffectiveFrom"<=now() AND (m."EndsAt" IS NULL OR m."EndsAt">now()) ORDER BY m."Number",m."Id" LIMIT 20',(tenant,chapter))}
 timings={};plans={}
 for name,(sql,args) in statements.items():
  samples=[]
  for _ in range(30):
   t=time.perf_counter();db.execute(sql,args).fetchall();samples.append((time.perf_counter()-t)*1000)
  timings[name]={'median_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[28],3),'iterations':30,'local_target_p95_ms':100,'target_met':sorted(samples)[28]<100}
  plans[name]=db.execute('EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) '+sql,args).fetchone()[0]
 result={'scenario':{'persons':100000,'memberships':100000,'chapters':10,'memberships_per_chapter':10000},'scope':'Real PostgreSQL, warm SQL-only single-client queries. Does not establish API concurrency or a production SLA. Fixtures rolled back.','fixture_insert_ms':insert_ms,'timings':timings,'plans':plans,'postgresql':db.execute('SELECT version()').fetchone()[0]}
 (ROOT/'tests/membership-performance.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps({'fixture_insert_ms':insert_ms,'timings':timings},indent=2))
 assert all(v['target_met'] for v in timings.values()),'A documented local query benchmark target was not met'
finally:db.rollback();db.close()
