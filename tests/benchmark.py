"""Capacity scenario on real PostgreSQL; synthetic fixtures are transactionally rolled back."""
import json,time,statistics,uuid
from pathlib import Path
import psycopg
ROOT=Path(__file__).resolve().parents[1]
c=json.loads((ROOT/'.local/dev.json').read_text())
db=psycopg.connect(host='127.0.0.1',port=55439,dbname='assemblia_test',user='postgres',password=c['password'])
timings={}
try:
 start=time.perf_counter()
 db.execute("""CREATE TEMP TABLE fixture_tenants AS SELECT gen_random_uuid() id,i FROM generate_series(1,1000) i;
 INSERT INTO "Tenants" ("Id","Name","Status","Profile","CreatedAt") SELECT id,'Benchmark society '||i,'Active','{}',now() FROM fixture_tenants;
 CREATE TEMP TABLE fixture_levels AS SELECT id tenant,gen_random_uuid() region,gen_random_uuid() chapter FROM fixture_tenants;
 INSERT INTO "Levels" ("Id","TenantId","Name","Kind","AllowedParents","AllowRoot") SELECT region,tenant,'Region','Region',ARRAY[]::uuid[],true FROM fixture_levels;
 INSERT INTO "Levels" ("Id","TenantId","Name","Kind","AllowedParents","AllowRoot") SELECT chapter,tenant,'Chapter','Chapter',ARRAY[region],false FROM fixture_levels;
 CREATE TEMP TABLE fixture_regions AS SELECT gen_random_uuid() id,tenant,region level,chapter,i FROM fixture_levels CROSS JOIN generate_series(1,10) i;
 INSERT INTO "Units" ("Id","TenantId","ParentId","LevelId","Name","Code","Status","Details","CreatedAt","UpdatedAt","Revision") SELECT id,tenant,NULL,level,'Region '||i,'REG-'||i,'Active','{}',now(),now(),1 FROM fixture_regions;
 INSERT INTO "Units" ("Id","TenantId","ParentId","LevelId","Name","Code","Status","Details","CreatedAt","UpdatedAt","Revision") SELECT gen_random_uuid(),tenant,id,chapter,'Chapter '||i||'-'||j,'CH-'||i||'-'||j,'Active','{}',now(),now(),1 FROM fixture_regions CROSS JOIN generate_series(1,10) j;
 INSERT INTO "Links" ("TenantId","UserId") SELECT t.id,u."Id" FROM fixture_tenants t CROSS JOIN LATERAL (SELECT "Id" FROM "AspNetUsers" LIMIT 1) u;
 INSERT INTO "GovernanceRoles" ("Id","TenantId","Name","Permissions","Responsibilities") SELECT gen_random_uuid(),id,'Benchmark Reader',ARRAY['units.read'],'Capacity fixture' FROM fixture_tenants;
 INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt","EndsAt") SELECT gen_random_uuid(),l."TenantId",l."UserId",r."Id",NULL,now()-interval '1 day',NULL FROM "Links" l JOIN fixture_tenants t ON t.id=l."TenantId" JOIN "GovernanceRoles" r ON r."TenantId"=t.id CROSS JOIN generate_series(1,5) i;
 ANALYZE "Assignments"; ANALYZE "Units";
 """)
 timings['fixture_insert_ms']=round((time.perf_counter()-start)*1000,2)
 tenant,region=db.execute('SELECT tenant,id FROM fixture_regions LIMIT 1').fetchone()
 sqls={
 'tenant_paginated_directory':('SELECT "Id","Name","Code","Status" FROM "Units" WHERE "TenantId"=%s ORDER BY "Name","Id" LIMIT 20',(tenant,)),
 'lazy_children':('SELECT "Id","Name","Code" FROM "Units" WHERE "TenantId"=%s AND "ParentId"=%s ORDER BY "Name" LIMIT 20',(tenant,region)),
 'subtree_count':('WITH RECURSIVE tree AS (SELECT "Id" FROM "Units" WHERE "TenantId"=%s AND "Id"=%s UNION ALL SELECT u."Id" FROM "Units" u JOIN tree t ON u."ParentId"=t."Id" WHERE u."TenantId"=%s) SELECT count(*) FROM tree',(tenant,region,tenant)),
  'permission_evaluation':('SELECT EXISTS(SELECT 1 FROM "Assignments" a JOIN "GovernanceRoles" r ON r."Id"=a."RoleId" AND r."TenantId"=a."TenantId" WHERE a."TenantId"=%s AND a."StartsAt"<=now() AND (a."EndsAt" IS NULL OR a."EndsAt">now()) AND %s=ANY(r."Permissions"))',(tenant,'units.read')),
 'indexed_search':('SELECT "Id" FROM "Units" WHERE "TenantId"=%s AND lower("Name") LIKE %s LIMIT 20',(tenant,'%chapter 1%'))}
 plans={}
 for name,(sql,args) in sqls.items():
  samples=[]
  for _ in range(30):
   t=time.perf_counter();db.execute(sql,args).fetchall();samples.append((time.perf_counter()-t)*1000)
  timings[name]={'median_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[28],3),'iterations':30}
  plans[name]=db.execute('EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) '+sql,args).fetchone()[0]
 result={'scenario':{'societies':1000,'regions':10000,'chapters':100000,'role_assignments':5000},'scope':'Local single-client warm database queries; not a load or production SLA test','timings':timings,'plans':plans,'postgresql':db.execute('SELECT version()').fetchone()[0]}
 (ROOT/'tests/performance.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
 print(json.dumps(timings,indent=2))
finally:db.rollback();db.close()
