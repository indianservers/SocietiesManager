"""Fetch historical PDFs from the isolated synthetic finance fixture without resetting it."""
from api_runtime import API_DLL
from pathlib import Path
import json,os,subprocess,socket,time,urllib.request,http.cookiejar
import psycopg
ROOT=Path(__file__).resolve().parents[1];cfg=json.loads((ROOT/'.local/dev.json').read_text())
with psycopg.connect(host='127.0.0.1',port=55439,dbname='assemblia_test',user='postgres',password=cfg['password']) as db:
 row=db.execute('''SELECT i."TenantId",i."Id",u."Email",r."Id" FROM "FinanceInvoice" i JOIN "FinancialEntity" e ON e."Id"=i."EntityId" JOIN "Memberships" m ON m."Id"=i."MembershipId" JOIN "Persons" p ON p."Id"=m."PersonId" JOIN "AspNetUsers" u ON u."Id"=p."UserId" JOIN "FinancePayment" pay ON pay."MembershipId"=m."Id" AND pay."Status"='Confirmed' JOIN "FinanceReceipt" r ON r."PaymentId"=pay."Id" WHERE e."LegalName"='Synthetic Finance Entity' AND i."Status"='Issued' AND i."Kind"='Invoice' ORDER BY i."At" LIMIT 1''').fetchone()
assert row and row[2].endswith('@example.test'),'Expected isolated synthetic fixture'
tenant,invoice,email,receipt=map(str,row)
with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
env={**os.environ,'ConnectionStrings__Database':f"Host=127.0.0.1;Port=55439;Database=assemblia_test;Username=postgres;Password={cfg['password']}",'ASPNETCORE_ENVIRONMENT':'Development','Logging__LogLevel__Default':'Error'}
log=open(ROOT/'.local/phase5-pdf-api.log','w');server=subprocess.Popen(['dotnet',str(API_DLL),'--urls',f'http://127.0.0.1:{port}'],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));base=f'http://127.0.0.1:{port}/api/v1'
try:
 for _ in range(80):
  try:
   with op.open(base+'/auth/csrf') as response:csrf=json.loads(response.read())['token']
   break
  except Exception:time.sleep(.15)
 req=urllib.request.Request(base+'/auth/login',data=json.dumps({'email':email,'password':'Changed-Password-2026!'}).encode(),headers={'Content-Type':'application/json','X-CSRF-TOKEN':csrf},method='POST')
 with op.open(req) as response:response.read()
 for kind,resource in [('invoice',invoice),('receipt',receipt)]:
  with op.open(base+'/societies/'+tenant+'/finance/'+kind+'s/'+resource+'/pdf') as response:data=response.read()
  assert data.startswith(b'%PDF-')
  target=ROOT/'tests'/('phase5-'+kind+'-sample.pdf');target.write_bytes(data);print(kind,len(data),'bytes',flush=True)
finally:server.terminate();server.wait(timeout=20);log.close()
