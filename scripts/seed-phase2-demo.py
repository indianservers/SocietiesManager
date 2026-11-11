"""Optional fictional Phase 2 fixture, targeting the local development instance only."""
from pathlib import Path
import json,urllib.request,http.cookiejar,datetime
root=Path(__file__).resolve().parents[1];c=json.loads((root/'.local/preview-account.json').read_text())
op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));base='http://127.0.0.1:5092/api/v1'
def call(path,method='GET',body=None):
 h={'Content-Type':'application/json'}
 if method!='GET':h['X-CSRF-TOKEN']=call('/auth/csrf')['token']
 req=urllib.request.Request(base+path,method=method,headers=h,data=json.dumps(body).encode() if body is not None else (b'' if method!='GET' else None))
 with op.open(req) as r:return json.loads(r.read() or 'null')
call('/auth/login','POST',{'email':c['email'],'password':c['password']});prefix='/societies/'+c['tenant'];m=prefix+'/membership';chapters=[u for u in call(prefix+'/organizations/units?kind=Chapter&pageSize=100')['items'] if u['status']=='Active'];cats=call(m+'/categories');category=next(x['id'] for x in cats if x['name']=='Active Member');associate=next(x['id'] for x in cats if x['name']=='Associate Member')
for i,name in enumerate(['Asha Rao','Vikram Menon','Meera Shah','Arjun Reddy','Nisha Patel','Rohan Kumar','Priya Nair','Dev Singh']):
 number='DEMO-'+str(i+1).zfill(3)
 if call(m+'/members?search='+number)['total']:continue
 ch=chapters[i%len(chapters)]
 call(m+'/members','POST',{'fullName':name+' · Demo','chapterId':ch['id'],'categoryId':associate if i%3==0 else category,'personId':None,'number':number,'status':'Inactive' if i in [5,7] else 'Active','profile':json.dumps({'email':'demo'+str(i+1)+'@assemblia.example','city':json.loads(ch['details']).get('city','Vijayawada'),'country':'India','professional':{'organization':'Fictional Community Foundation','profession':'Community volunteer','industry':'Nonprofit'}}),'privacy':json.dumps({'directory':True,'contacts':False,'photo':False,'professional':True})})
print('Local fictional membership fixture is ready. All dashboard counts come from database records.')
