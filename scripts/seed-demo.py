"""Optional local-only demo fixture. Never runs at application startup."""
import json,secrets,subprocess,os,urllib.request,urllib.error,http.cookiejar,urllib.parse
from pathlib import Path
import psycopg
ROOT=Path(__file__).resolve().parents[1]
c=json.loads((ROOT/'.local/dev.json').read_text())
if (ROOT/'.local/preview-account.json').exists():raise SystemExit('Local preview fixture already exists.')
adminPassword=secrets.token_urlsafe(22)+'!aA1';password=secrets.token_urlsafe(22)+'!aA1'
env={**os.environ,'ConnectionStrings__Database':c['connectionString'],'ASPNETCORE_ENVIRONMENT':'Development','BOOTSTRAP_EMAIL':'platform@assemblia.example','BOOTSTRAP_PASSWORD':adminPassword}
subprocess.run(['dotnet',str(ROOT/'backend/bin/Debug/net10.0/Society.Api.dll'),'--bootstrap-admin'],cwd=ROOT/'backend',env=env,check=True,stdout=subprocess.DEVNULL)
class Client:
 def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def call(self,path,method='GET',body=None):
  h={'Content-Type':'application/json'}
  if method!='GET':h['X-CSRF-TOKEN']=self.call('/auth/csrf')['token']
  req=urllib.request.Request('http://127.0.0.1:5088/api/v1'+path,method=method,headers=h,data=json.dumps(body).encode() if body is not None else (b'' if method!='GET' else None))
  with self.opener.open(req) as r:return json.loads(r.read() or 'null')
admin=Client();owner=Client();email='preview@assemblia.example'
owner.call('/auth/signup','POST',{'fullName':'Demo Administrator','email':email,'password':password})
with psycopg.connect(host='127.0.0.1',port=55439,dbname='assemblia',user='postgres',password=c['password']) as db:
 user=db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(email,)).fetchone()[0]
 body=db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s',(email,)).fetchone()[0]
token=urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
owner.call('/auth/verify?userId='+str(user)+'&token='+urllib.parse.quote(token,safe=''),'POST')
owner.call('/auth/login','POST',{'email':email,'password':password});admin.call('/auth/login','POST',{'email':'platform@assemblia.example','password':adminPassword})
app=owner.call('/societies/applications','POST',{'name':'Demo · Community Foundation','category':'Community association','country':'India','state':'Andhra Pradesh','city':'Vijayawada','officialEmail':'office@assemblia.example','contactNumber':'0000000000','address':'Demonstration workspace — fictional organization','applicantMobile':'0000000000'})
tenant=admin.call('/societies/applications/'+app['id']+'/review','POST',{'approve':True,'note':'Local demonstration fixture only'})['tenantId'];prefix='/societies/'+tenant
levels=owner.call(prefix+'/organizations/levels');r=next(l['id'] for l in levels if l['kind']=='Region');ch=next(l['id'] for l in levels if l['kind']=='Chapter')
for name,code,cities in [('Andhra Pradesh','AP',['Vijayawada','Guntur','Visakhapatnam']),('Telangana','TS',['Hyderabad','Warangal'])]:
 region=owner.call(prefix+'/organizations/units','POST',{'name':name,'code':code,'levelId':r,'parentId':None,'status':'Active','details':json.dumps({'country':'India','state':name,'description':'Fictional demonstration region'})})['id']
 for i,city in enumerate(cities):owner.call(prefix+'/organizations/units','POST',{'name':city+' Chapter','code':code+'-'+str(i+1).zfill(2),'levelId':ch,'parentId':region,'status':'Proposed' if i==2 else 'Active','details':json.dumps({'country':'India','state':name,'city':city,'description':'Fictional demonstration chapter'})})
owner.call(prefix+'/onboarding','PUT',{'step':7,'data':json.dumps({'displayName':'Demo · Community Foundation','purpose':'A fictional organization for local UI review','directoryVisibility':'chapter'}),'activate':True})
(ROOT/'.local/preview-account.json').write_text(json.dumps({'email':email,'password':password,'platformEmail':'platform@assemblia.example','platformPassword':adminPassword,'tenant':tenant}),encoding='utf-8')
print('Created isolated local demonstration workspace. Credentials are in .local/preview-account.json.')
