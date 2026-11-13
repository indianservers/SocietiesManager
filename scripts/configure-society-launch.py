"""Apply the opt-in local society launch configuration; never deploys publicly."""
import argparse
import json
import os
import subprocess
from pathlib import Path
import psycopg

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--config',type=Path,default=ROOT/'scripts/society-example.json')
parser.add_argument('--account',type=Path,default=ROOT/'.local/society-example-account.json')
parser.add_argument('--api-dll',type=Path,default=ROOT/'.local/membership-launch-build/Society.Api.dll')
args=parser.parse_args()
dev=json.loads((ROOT/'.local/dev.json').read_text())
account=json.loads(args.account.read_text())
launch=json.loads(args.config.read_text(encoding='utf-8'))
env={**os.environ,'ConnectionStrings__Database':dev['connectionString'],'ASPNETCORE_ENVIRONMENT':'Development',
     'PublicUrl':'http://127.0.0.1:5176','Society__DefaultTenantId':account['tenant'],
     'SOCIETY_LAUNCH_CONFIG':str(args.config.resolve()),'SOCIETY_LAUNCH_TENANT':account['tenant'],
     'Society__LaunchOutput':str(ROOT/'.local/launch-owner.json')}
for command in ['--migrate','--configure-society-launch']:
 subprocess.run(['dotnet',str(args.api_dll.resolve()),command],cwd=ROOT/'backend',env=env,check=True)
# Give only the explicitly fictional legacy sample records a visible annual term.
# No real membership or paid transaction is altered.
with psycopg.connect(host='127.0.0.1',port=55439,dbname='assemblia',user='postgres',password=dev['password']) as db:
 for group in launch['memberships']:
  db.execute('UPDATE "Memberships" m SET "EndsAt"=m."EffectiveFrom"+interval \'1 year\', "Revision"=m."Revision"+1 WHERE m."TenantId"=%s AND m."Number" LIKE %s AND m."EndsAt" IS NULL AND EXISTS (SELECT 1 FROM "Persons" p WHERE p."Id"=m."PersonId" AND p."FullName" LIKE %s)',(account['tenant'],group['prefix']+'-%','Example %'))
print('Local launch configuration ready. Private owner setup and DNS proof: .local/launch-owner.json')
