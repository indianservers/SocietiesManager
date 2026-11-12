"""Execute shipped grants under an ephemeral restricted role on the test database."""
from pathlib import Path
import json,uuid,psycopg
from psycopg import sql
ROOT=Path(__file__).resolve().parents[1];cfg=json.loads((ROOT/'.local/dev.json').read_text());role='assemblia_runtime_test_'+uuid.uuid4().hex[:12];aid=uuid.uuid4();results=[]
db=psycopg.connect(host='127.0.0.1',port=55439,dbname='assemblia_test',user='postgres',password=cfg['password'],autocommit=True)
def run(name,statement,denied=False):
 db.execute(sql.SQL('SET ROLE {}').format(sql.Identifier(role)))
 try:
  try:
   db.execute(statement)
   assert not denied,'Expected permission denial'
  except psycopg.errors.InsufficientPrivilege:
   assert denied,'Unexpected permission denial'
  results.append({'name':name,'passed':True})
 finally:db.execute('RESET ROLE')
try:
 db.execute(sql.SQL('CREATE ROLE {} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT').format(sql.Identifier(role)))
 grants=(ROOT/'scripts/runtime-grants.sql').read_text().replace('ON DATABASE assemblia TO','ON DATABASE assemblia_test TO').replace('assemblia_runtime',role)
 db.execute(grants)
 run('runtime can read migrated schema history','SELECT * FROM "__EFMigrationsHistory" LIMIT 1')
 run('runtime can read canonical membership','SELECT * FROM "Memberships" LIMIT 0')
 run('runtime can read credential queue','SELECT * FROM "StudioJob" LIMIT 0')
 run('runtime can execute canonical scope function',"SELECT unit_within('00000000-0000-0000-0000-000000000000','00000000-0000-0000-0000-000000000000','00000000-0000-0000-0000-000000000000')")
 run('runtime can update ordinary membership metadata','UPDATE "Memberships" SET "Number"="Number" WHERE false')
 run('runtime cannot rewrite audit rows','UPDATE "Audits" SET "Action"="Action" WHERE false',True)
 run('runtime cannot delete audit rows','DELETE FROM "Audits" WHERE false',True)
 run('runtime cannot rewrite published designs','UPDATE "DesignVersion" SET "Version"="Version" WHERE false',True)
 run('runtime cannot delete issued financial journal lines','DELETE FROM "JournalLine" WHERE false',True)
 run('runtime cannot rewrite migration history','UPDATE "__EFMigrationsHistory" SET "ProductVersion"="ProductVersion" WHERE false',True)
 run('runtime cannot create schema objects','CREATE TABLE public.phase6_forbidden_runtime_table(id int)',True)
 (ROOT/'tests/phase6-runtime-role.json').write_text(json.dumps({'passed':len(results),'total':len(results),'results':results,'scope':'Shipped grants exercised using SET ROLE on the real test database. No production runtime identity or deployed API was used.'},indent=2))
 print('Restricted runtime grant checks:',len(results),'passed')
finally:
 db.execute('RESET ROLE')
 assert role.startswith('assemblia_runtime_test_')
 db.execute(sql.SQL('DROP OWNED BY {}').format(sql.Identifier(role)))
 db.execute(sql.SQL('DROP ROLE {}').format(sql.Identifier(role)));db.close()
