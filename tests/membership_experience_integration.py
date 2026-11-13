"""Real PostgreSQL/HTTP renewal, dual identity, resources and pricing tests.

Imports the foundation regression suite; all fixtures stay in assemblia_test.
Payment provider events are signed synthetic test events, never live charges.
"""
import integration as p
from api_runtime import API_DLL
import concurrent.futures
import datetime
import hashlib
import hmac
import json
import os
import subprocess
import time
import urllib.request
import urllib.error
import uuid
from pathlib import Path
import psycopg

ROOT=p.ROOT;tenant=p.tenant;prefix='/societies/'+tenant;base=prefix+'/membership';finance=prefix+'/finance'
owner=p.owner;platform=p.admin;results=[];server=None
env={**p.env,'Finance__AllowTestGateway':'true','Society__DefaultTenantId':tenant}
log=open(ROOT/'.local/membership-experience-api.log','w',encoding='utf-8')

def sql(statement,args=()):
 with psycopg.connect(p.DSN) as db:
  cur=db.execute(statement,args)
  return cur.fetchall() if cur.description else None

def check(name,fn):
 try:fn();results.append({'name':name,'passed':True});print('PASS EXPERIENCE',name,flush=True)
 except Exception as e:results.append({'name':name,'passed':False,'error':str(e)});raise

def yes(value):assert value

def capture(client,renewal,amount,currency='INR',valid=True,event_id=None,payment_ref=None):
 intent=client.request(finance+'/invoices/'+renewal['invoiceId']+'/payment-intents','POST',{'connectionId':gateway,'key':'renewal-'+uuid.uuid4().hex})['intent']
 payload={'event':'payment.captured','payload':{'payment':{'entity':{'id':payment_ref or 'pay_renewal_'+uuid.uuid4().hex,'order_id':intent['orderId'],'amount':amount,'currency':currency,'status':'captured'}}}}
 return webhook(payload,valid,event_id)

def webhook(payload,valid=True,event_id=None):
 body=json.dumps(payload).encode();signature=hmac.new(b'renewal-synthetic-webhook-secret-32',body,hashlib.sha256).hexdigest() if valid else 'invalid'
 req=urllib.request.Request('http://127.0.0.1:5089/api/v1/finance/webhooks/'+gateway,data=body,method='POST',headers={'Content-Type':'application/json','X-Razorpay-Signature':signature,'X-Razorpay-Event-Id':event_id or 'event-'+uuid.uuid4().hex})
 try:
  with urllib.request.urlopen(req) as response:return response.status,json.loads(response.read())
 except urllib.error.HTTPError as error:return error.code,error.read().decode()

def offer(category,amount=500,**extra):
 return platform.request('/platform/membership-pricing/'+tenant+'/'+category,'PUT',{'enabled':True,'termMonths':12,'price':amount,'entityId':entity,'currency':'INR','incomeCode':'4000','paymentRequired':True,'reminderDays':30,'revision':0,**extra})

def apply(client,category,organization=''):
 app=client.request('/membership/applications','POST',{'tenantId':tenant,'chapterId':chapter,'categoryId':category,'profile':json.dumps({'organizationName':organization}),'submit':True})
 return owner.request(base+'/applications/'+app['id']+'/decision','POST',{'decision':'Approve','comment':'Independent synthetic membership approval','revision':0})['membershipId']

try:
 server=subprocess.Popen(['dotnet',str(API_DLL),'--urls','http://127.0.0.1:5089'],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
 for _ in range(100):
  try:owner.request('/auth/csrf');break
  except Exception:time.sleep(.2)
 p.login(owner,p.accounts['owner'][0],'Changed-Password-2026!')
 p.login(platform,p.ae)
 check('configured launch society resolves before login without hardcoded application branding',lambda:yes(p.Client().request('/branding/resolve')['tenantId']==tenant))
 chapter=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region'],'Renewal chapter','RENEW-'+p.suffix))['id']
 entity=owner.request(finance+'/entities','POST',{'name':'Membership receiving entity','legalName':'Synthetic membership entity','unitId':p.units['region'],'units':[chapter],'currency':'INR','requireInvoiceApproval':False})['id']
 categories={}
 for kind in ['Individual','Institutional','Corporate']:
  categories[kind]=owner.request(base+'/categories','POST',{'name':kind+' launch '+p.suffix,'description':'Synthetic launch category','kind':kind,'active':True})['id']
  offer(categories[kind],500 if kind=='Individual' else 10000)
 check('only platform owners can change membership prices',lambda:owner.request('/platform/membership-pricing/'+tenant+'/'+categories['Individual'],'PUT',{'price':1,'enabled':True},expected=403))
 check('public signup catalog includes three kinds and configured annual prices',lambda:yes({c['kind']:c['offer']['price'] for c in p.Client().request('/membership/catalog?tenant='+tenant)['categories'] if c['id'] in categories.values()}=={'Individual':500,'Institutional':10000,'Corporate':10000}))
 check('public signup does not advertise legacy unpriced categories when launch offers are configured',lambda:yes(len(p.Client().request('/membership/catalog?tenant='+tenant)['categories'])==3))
 person=p.Client();email,user=p.signup(person,'dual-identity');p.login(person,email)
 legacy=next(c['id'] for c in owner.request(base+'/categories') if c['name']=='Active Member')
 check('direct application cannot bypass launch pricing through a legacy category',lambda:person.request('/membership/applications','POST',{'tenantId':tenant,'chapterId':chapter,'categoryId':legacy,'profile':'{}','submit':True},expected=400))
 individual=apply(person,categories['Individual'])
 check('approval does not activate an unpaid paid-membership offer',lambda:yes(person.request('/membership/portfolio')[0]['state']=='Payment required'))
 check('unpaid membership has no resource entitlement',lambda:yes(person.request(base+'/resources')==[]))
 check('manual membership edit cannot bypass payment activation',lambda:owner.request(base+'/members/'+individual,'PUT',{'fullName':'Synthetic','chapterId':chapter,'categoryId':categories['Individual'],'status':'Active','profile':'{}','revision':0},expected=400))
 renewal=person.request(base+'/members/'+individual+'/renew','POST')
 check('renewal fee comes from owner pricing rather than client input',lambda:yes(renewal['amount']==500 and renewal['termMonths']==12))
 check('repeated renewal requests reuse a single pending invoice',lambda:yes(person.request(base+'/members/'+individual+'/renew','POST')['invoiceId']==renewal['invoiceId']))
 gateway=owner.request(finance+'/providers','POST',{'entityId':entity,'unitId':chapter,'name':'Synthetic renewal gateway','provider':'Test','publicKey':'test-membership','mode':'Test','enabled':True,'secrets':json.dumps({'secret':'renewal-synthetic-api-secret-at-least32','webhookSecret':'renewal-synthetic-webhook-secret-32'})})['id'];owner.request(finance+'/providers/'+gateway+'/verify','POST')
 check('forged payment webhook cannot activate membership',lambda:yes(capture(person,renewal,50000,valid=False)[0]==403 and not person.request('/membership/portfolio')[0]['active']))
 check('wrong captured amount cannot activate membership',lambda:yes(capture(person,renewal,1)[0]==400 and not person.request('/membership/portfolio')[0]['active']))
 check('verified full capture activates annual membership',lambda:yes(capture(person,renewal,50000)[0]==200 and person.request('/membership/portfolio')[0]['active']))
 first=person.request('/membership/portfolio')[0];first_end=first['endsAt']
 check('renewed validity is approximately one year from confirmed initial payment',lambda:yes(364<=(datetime.datetime.fromisoformat(first_end)-datetime.datetime.now(datetime.timezone.utc)).days<=366))
 next_renewal=person.request(base+'/members/'+individual+'/renew','POST')
 intent=person.request(finance+'/invoices/'+next_renewal['invoiceId']+'/payment-intents','POST',{'connectionId':gateway,'key':'repeat-'+uuid.uuid4().hex})['intent']
 payload={'event':'payment.captured','payload':{'payment':{'entity':{'id':'pay_duplicate_'+uuid.uuid4().hex,'order_id':intent['orderId'],'amount':50000,'currency':'INR','status':'captured'}}}}
 event_key='duplicate-'+uuid.uuid4().hex
 check('early renewal carries remaining paid time forward',lambda:yes(webhook(payload,event_id=event_key)[0]==200 and datetime.datetime.fromisoformat(person.request('/membership/portfolio')[0]['endsAt']).year==datetime.datetime.fromisoformat(first_end).year+1))
 extended=person.request('/membership/portfolio')[0]['endsAt']
 with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:duplicates=list(pool.map(lambda _:webhook(payload,event_id=event_key),range(8)))
 check('concurrent duplicate capture never grants extra membership time',lambda:yes(all(r[0]==200 for r in duplicates) and person.request('/membership/portfolio')[0]['endsAt']==extended))
 institution=apply(person,categories['Institutional'],'Synthetic Institute')
 corporate=apply(person,categories['Corporate'],'Synthetic Corporation')
 check('one human can hold individual membership and control separate institution and corporate memberships in the same chapter',lambda:yes({m['kind'] for m in person.request('/membership/portfolio')}=={'Individual','Institutional','Corporate'}))
 check('organization approval does not merge personal and institution identities',lambda:yes(len({str(row[0]) for row in sql('SELECT "PersonId" FROM "Memberships" WHERE "Id"=ANY(%s::uuid[])',([individual,institution,corporate],))})==3))
 identity_detail=owner.request(base+'/members/'+individual)
 check('category edits cannot merge a personal identity into an institutional membership',lambda:owner.request(base+'/members/'+individual,'PUT',{**identity_detail['member'],'categoryId':categories['Institutional'],'profile':json.dumps(identity_detail['profile']),'privacy':json.dumps(identity_detail['privacy'])},expected=400))
 check('organization controller is not automatically a society administrator',lambda:person.request(prefix+'/roles',expected=403))
 inst_renewal=person.request(base+'/members/'+institution+'/renew','POST');capture(person,inst_renewal,1000000)
 resource=owner.request(base+'/resource-library','POST',{'title':'Institution teaching toolkit','description':'Private institution-only fixture','kind':'Guide','audiences':['Institutional'],'content':'Restricted institute resource content','url':'','published':True})['id']
 check('active institution controller can read the institutional resource',lambda:yes(person.request(base+'/resources/'+resource)['content']=='Restricted institute resource content'))
 check('resource publication rejects unsafe external URLs',lambda:owner.request(base+'/resource-library','POST',{'title':'Unsafe','description':'','kind':'Link','audiences':['Individual'],'content':'','url':'javascript:alert(1)','published':True},expected=400))
 check('cross-society resource reference cannot be read',lambda:person.request('/societies/'+p.otherTenant+'/membership/resources/'+resource,expected=403))
 representative=p.Client();rep_email,rep_user=p.signup(representative,'representative');p.login(representative,rep_email)
 owner.request(base+'/members/'+institution+'/representatives','POST',{'email':rep_email,'role':'Admin'})
 check('verified organization admin can access delegated institution membership',lambda:yes(representative.request('/membership/portfolio')[0]['id']==institution))
 check('verified organization admin can read its institutional resource',lambda:yes(representative.request(base+'/resources/'+resource)['id']==resource))
 check('organization admin cannot grant controller or society authority',lambda:representative.request(base+'/members/'+institution+'/representatives','POST',{'email':email,'role':'Controller'},expected=403))
 reps=person.request(base+'/members/'+institution+'/representatives');rep=next(r for r in reps if r['userId']==rep_user)
 person.request(base+'/members/'+institution+'/representatives/'+rep['id'],'DELETE',expected=204)
 check('revocation immediately removes institution resource access',lambda:representative.request(base+'/resources/'+resource,expected=403))
 check('revocation never changes the controller individual membership',lambda:yes(next(m for m in person.request('/membership/portfolio') if m['id']==individual)['endsAt']==extended))
 person.request(base+'/members/'+institution+'/representatives','POST',{'email':rep_email,'role':'Coordinator'})
 check('organization coordinator may read member resources without acquiring billing authority',lambda:yes(representative.request(base+'/resources/'+resource)['id']==resource))
 check('organization coordinator cannot read private billing records',lambda:representative.request(finance+'/invoices/'+inst_renewal['invoiceId'],expected=403))
 check('organization coordinator cannot renew or edit the organization profile as an administrator',lambda:representative.request(base+'/members/'+institution+'/renew','POST',expected=403))
 check('organization coordinator sees no private editable profile',lambda:yes(not representative.request(base+'/members/'+institution)['canEdit']))
 coordinator=next(r for r in person.request(base+'/members/'+institution+'/representatives') if r['userId']==rep_user)
 person.request(base+'/members/'+institution+'/representatives/'+coordinator['id'],'DELETE',expected=204)
 owner.request(base+'/members/'+institution+'/chapters/'+chapter,'PUT',expected=204)
 check('institution chapter relationship is explicit and independent of personal membership',lambda:yes(person.request(base+'/members/'+institution+'/chapters')[0]['id']==chapter))
 chapter_manager=next(position['id'] for position in owner.request(base+'/positions') if position['name']=='Chapter Manager')
 appointment=owner.request(base+'/appointments','POST',{'membershipId':individual,'unitId':chapter,'positionId':chapter_manager,'startsAt':(datetime.datetime.now(datetime.timezone.utc)-datetime.timedelta(minutes=1)).isoformat(),'endsAt':(datetime.datetime.now(datetime.timezone.utc)+datetime.timedelta(days=180)).isoformat(),'method':'Appointed','notes':'Explicit coordinator of institution-started chapter'})['id']
 owner.request(base+'/appointments/'+appointment+'/decision','POST',{'decision':'Approve','comment':'Independent approved chapter responsibility','revision':0},expected=204)
 check('an Individual member can explicitly coordinate the chapter linked to their institution without merging memberships',lambda:yes(any(scope['name']=='Chapter Manager' and scope['unitId']==chapter for workspace in person.request('/workspaces') for scope in workspace['scopes']) and len(person.request('/membership/portfolio'))==3))
 check('institution controller cannot assign chapter governance roles by itself',lambda:person.request(prefix+'/role-assignments','POST',{'userId':user,'roleId':str(uuid.uuid4()),'unitId':chapter,'startsAt':datetime.datetime.now(datetime.timezone.utc).isoformat()},expected=403))
 sql('UPDATE "Memberships" SET "EffectiveFrom"=now()-interval \'1 year\', "EndsAt"=now()-interval \'1 day\' WHERE "Id"=%s',(institution,))
 check('expired institutional membership loses resources even with an active individual membership',lambda:person.request(base+'/resources/'+resource,expected=403))
 check('expired membership can still request a renewal invoice',lambda:yes(person.request(base+'/members/'+institution+'/renew','POST')['amount']==10000))
 # Reminder sweeps are repeatable operator commands, scoped to opt-in verified accounts.
 person.request(base+'/members/'+individual+'/renewal-reminders','PUT',{'enabled':True},expected=204)
 sql('UPDATE "Memberships" SET "EndsAt"=now()+interval \'15 days\' WHERE "Id"=%s',(individual,))
 for _ in range(2):subprocess.run(['dotnet',str(API_DLL),'--membership-renewal-reminders-once'],cwd=ROOT/'backend',env=env,check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 check('renewal reminders persist and are sent once per term, not once per worker restart',lambda:yes(sql('SELECT count(*) FROM "MembershipRenewalNotice" WHERE "MembershipId"=%s',(individual,))[0][0]==1))
 check('reminder is delivered through the durable private outbox',lambda:yes(sql('SELECT count(*) FROM "Outbox" WHERE "TenantId"=%s AND "Recipient"=%s AND "Subject" LIKE %s',(tenant,email,'%membership renewal reminder'))[0][0]==1))
 for _ in range(30):
  if sql('SELECT "SentAt" FROM "Outbox" WHERE "TenantId"=%s AND "Recipient"=%s AND "Subject" LIKE %s',(tenant,email,'%membership renewal reminder'))[0][0] is not None:break
  time.sleep(.1)
 check('opted-in reminder actually dispatches through the email adapter rather than failing as an unconfigured campaign',lambda:yes(sql('SELECT "Status" FROM "Outbox" WHERE "TenantId"=%s AND "Recipient"=%s AND "Subject" LIKE %s',(tenant,email,'%membership renewal reminder'))[0][0]=='Pickup'))
 # Signed chargeback must retract paid access and preserve historical validity evidence.
 payment_ref=payload['payload']['payment']['entity']['id']
 dispute={'event':'payment.dispute.lost','payload':{'dispute':{'entity':{'id':'dp_'+uuid.uuid4().hex,'payment_id':payment_ref,'status':'lost','currency':'INR','amount_deducted':50000}}}}
 check('verified chargeback blocks paid membership benefits without deleting history',lambda:yes(webhook(dispute)[0]==200 and next(m for m in person.request('/membership/portfolio') if m['id']==individual)['state']=='Payment required'))
 check('personal membership payment hold also removes its dated chapter governance authority',lambda:yes(not any(scope['name']=='Chapter Manager' for workspace in person.request('/workspaces') for scope in workspace['scopes'])))
 check('renewal snapshots remain available after payment reversal',lambda:yes(any(r['state']=='Payment Reversed' and r['appliedAt'] for r in person.request(base+'/members/'+individual+'/renewals'))))
 # Independently approved manual collections use the same full-payment gate.
 reviewer_role=owner.request(prefix+'/roles','POST',{'name':'Renewal reviewer '+p.suffix,'permissions':['finance.read','finance.approve'],'responsibilities':'Synthetic independent finance review'})['id']
 owner.request(prefix+'/role-assignments','POST',{'userId':rep_user,'roleId':reviewer_role,'unitId':chapter,'startsAt':(datetime.datetime.now(datetime.timezone.utc)-datetime.timedelta(days=1)).isoformat()})
 corp_renewal=person.request(base+'/members/'+corporate+'/renew','POST')
 def collect(amount):
  payment=owner.request(finance+'/payments','POST',{'entityId':entity,'unitId':chapter,'amount':amount,'reference':'renewal-cash-'+uuid.uuid4().hex,'date':str(datetime.date.today()),'payer':'Synthetic Corporation','membershipId':corporate,'method':'Cash'})
  representative.request(finance+'/payments/'+payment['id']+'/decision','POST',{'decision':'Approve'})
  owner.request(finance+'/payments/'+payment['id']+'/allocate','POST',{'invoiceId':corp_renewal['invoiceId'],'amount':amount,'key':'allocate-'+uuid.uuid4().hex})
 collect(2500)
 check('confirmed partial payment does not activate a corporate membership',lambda:yes(next(m for m in person.request('/membership/portfolio') if m['id']==corporate)['state']=='Payment required'))
 collect(7500)
 check('membership activates automatically when independently confirmed allocations reach the full fee',lambda:yes(next(m for m in person.request('/membership/portfolio') if m['id']==corporate)['active']))
 corporate_before=next(m for m in person.request('/membership/portfolio') if m['id']==corporate)['endsAt']
 held_renewal=person.request(base+'/members/'+corporate+'/renew','POST')
 sql('UPDATE "Memberships" SET "Status"=\'Suspended\' WHERE "Id"=%s',(corporate,))
 check('capture on a suspended membership records funds without silently lifting the officer hold',lambda:yes(capture(person,held_renewal,1000000)[0]==200 and person.request(finance+'/invoices/'+held_renewal['invoiceId'])['invoice']['outstanding']==0 and next(m for m in person.request('/membership/portfolio') if m['id']==corporate)['state']=='Suspended'))
 check('suspension prevents automatic benefit extension while retaining the captured payment',lambda:yes(next(m for m in person.request('/membership/portfolio') if m['id']==corporate)['endsAt']==corporate_before))
 corporate_detail=owner.request(base+'/members/'+corporate)
 owner.request(base+'/members/'+corporate,'PUT',{**corporate_detail['member'],'status':'Active','profile':json.dumps(corporate_detail['profile']),'privacy':json.dumps(corporate_detail['privacy'])},expected=204)
 check('explicit officer reinstatement applies the already verified paid term once',lambda:yes(next(m for m in person.request('/membership/portfolio') if m['id']==corporate)['active'] and next(m for m in person.request('/membership/portfolio') if m['id']==corporate)['endsAt']!=corporate_before))
 # The signup choice records intent only, never authority or paid entitlement.
 check('signup rejects invented membership types',lambda:p.Client().request('/auth/signup','POST',{'email':'bad-'+p.suffix+'@example.test','password':'Test-Password-2026!','fullName':'Bad kind','membershipKind':'SuperAdmin'},expected=400))
 signup_email='corporate-choice-'+p.suffix+'@example.test'
 p.Client().request('/auth/signup','POST',{'email':signup_email,'password':'Test-Password-2026!','fullName':'Corporate applicant','membershipKind':'Corporate','societyId':tenant})
 check('signup records the chosen membership type without granting platform privileges',lambda:yes(sql('SELECT "PreferredMembershipKind","PlatformAdmin","EmailConfirmed" FROM "AspNetUsers" WHERE "Email"=%s',(signup_email,))[0]==('Corporate',False,False)))
 check('platform pricing preserves the three configured annual fees',lambda:yes({c['kind']:c['offer']['price'] for c in platform.request('/platform/membership-pricing/'+tenant)['categories'] if c['id'] in categories.values()}=={'Individual':500,'Institutional':10000,'Corporate':10000}))
finally:
 if server:server.terminate();server.wait(timeout=20)
 log.close();(ROOT/'tests/membership-experience-results.json').write_text(json.dumps({'passed':sum(r['passed'] for r in results),'total':len(results),'results':results},indent=2),encoding='utf-8')
