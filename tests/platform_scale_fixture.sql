-- Synthetic capacity records only, in the benchmark's disposable database.
-- Main tenant already has one region and 10,000 chapters. Other tenants add
-- 9,999 regions and 90,000 chapters, for asserted global totals of 10k / 100k.
CREATE TEMP TABLE platform_tenants AS SELECT i,gen_random_uuid() id,gen_random_uuid() region_level,gen_random_uuid() chapter_level FROM generate_series(1,999) i;
INSERT INTO "Tenants" ("Id","Name","Status","Profile","CreatedAt") SELECT id,'Synthetic capacity society '||i,'Active','{}',now() FROM platform_tenants;
INSERT INTO "Levels" ("Id","TenantId","Name","Kind","AllowedParents","AllowRoot") SELECT region_level,id,'Region','Region',ARRAY[]::uuid[],true FROM platform_tenants UNION ALL SELECT chapter_level,id,'Chapter','Chapter',ARRAY[region_level],false FROM platform_tenants;
CREATE TEMP TABLE platform_regions AS SELECT t.id tenant,t.i tenant_i,t.region_level,t.chapter_level,n i,gen_random_uuid() id FROM platform_tenants t CROSS JOIN generate_series(1,10) n UNION ALL SELECT t.id,t.i,t.region_level,t.chapter_level,11,gen_random_uuid() FROM platform_tenants t WHERE t.i<=9;
INSERT INTO "Units" ("Id","TenantId","ParentId","LevelId","Name","Code","Status","Details","CreatedAt","UpdatedAt","Revision") SELECT id,tenant,NULL,region_level,'Synthetic Region '||i,'R-'||i,'Active','{}',now(),now(),0 FROM platform_regions;
CREATE TEMP TABLE platform_chapters AS SELECT t.id tenant,t.chapter_level,n i,gen_random_uuid() id,(SELECT r.id FROM platform_regions r WHERE r.tenant=t.id AND r.i=((n-1)%10)+1) region FROM platform_tenants t CROSS JOIN LATERAL generate_series(1,CASE WHEN t.i<=90 THEN 91 ELSE 90 END) n;
INSERT INTO "Units" ("Id","TenantId","ParentId","LevelId","Name","Code","Status","Details","CreatedAt","UpdatedAt","Revision") SELECT id,tenant,region,chapter_level,'Synthetic Chapter '||i,'C-'||i,'Active','{}',now(),now(),0 FROM platform_chapters;
