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
 check('platform overview rejects unauthenticated requests',lambda:p.Client().request('/platform/overview',expected=401))
 check('platform overview rejects society administrators without platform authority',lambda:owner.request('/platform/overview',expected=403))
 check('platform overview returns live aggregate counts for platform administrators',lambda:yes(platform.request('/platform/overview?tenant='+tenant)['units']>=1))
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
 check('individual directory excludes organization memberships',lambda:yes(all(m['kind']=='Individual' for m in owner.request(base+'/members?kind=Individual&pageSize=100')['items'])))
 check('institutional directory includes the organization and excludes people',lambda:yes(institution in [m['id'] for m in owner.request(base+'/members?kind=Institutional&pageSize=100')['items']] and all(m['kind']=='Institutional' for m in owner.request(base+'/members?kind=Institutional&pageSize=100')['items'])))
 check('organization cannot be appointed to a human officer role',lambda:owner.request(base+'/appointments','POST',{'membershipId':institution,'unitId':chapter,'positionId':str(uuid.uuid4()),'startsAt':'2026-01-01T00:00:00Z','endsAt':'2027-01-01T00:00:00Z','method':'Appointment','notes':'Synthetic invalid holder','documentIds':[]},expected=400))
 check('service volunteer picker contains individual people only',lambda:yes(all(m['id']!=institution for m in owner.request(prefix+'/service/members'))))
 check('studio individual picker excludes organization',lambda:yes(all(m['id']!=institution for m in owner.request(prefix+'/studio/members?kind=Individual'))))
 check('membership intelligence reports people and organizations separately',lambda:yes('institutionalMembers' in owner.request(base+'/intelligence')))

 corporate=apply(person,categories['Corporate'],'Synthetic Corporation')
 check('one human can hold individual membership and control separate institution and corporate memberships in the same chapter',lambda:yes({m['kind'] for m in person.request('/membership/portfolio')}=={'Individual','Institutional','Corporate'}))
 check('organization approval does not merge personal and institution identities',lambda:yes(len({str(row[0]) for row in sql('SELECT "PersonId" FROM "Memberships" WHERE "Id"=ANY(%s::uuid[])',([individual,institution,corporate],))})==3))
 identity_detail=owner.request(base+'/members/'+individual)
 check('category edits cannot merge a personal identity into an institutional membership',lambda:owner.request(base+'/members/'+individual,'PUT',{**identity_detail['member'],'categoryId':categories['Institutional'],'profile':json.dumps(identity_detail['profile']),'privacy':json.dumps(identity_detail['privacy'])},expected=400))
 check('holder can list their own ID cards without studio administrator permissions',lambda:yes(person.request(base+'/members/'+individual+'/id-cards')['downloadsEnabled']))
 check('ID card lookup rejects memberships outside the holder account',lambda:person.request(base+'/members/'+str(uuid.uuid4())+'/id-cards',expected=403))
 check('organization controller is not automatically a society administrator',lambda:person.request(prefix+'/roles',expected=403))
 inst_renewal=person.request(base+'/members/'+institution+'/renew','POST');capture(person,inst_renewal,1000000)
 resource=owner.request(base+'/resource-library','POST',{'title':'Institution teaching toolkit','description':'Private institution-only fixture','kind':'Guide','audiences':['Institutional'],'content':'Restricted institute resource content','url':'','published':True})['id']
 # Organization roster IDs are independent of both the organization and personal IDs.
 roster=base+'/members/'+institution+'/roster'
 student=person.request(roster,'POST',{'fullName':'Dual Student','email':email,'role':'Student','reference':'ROLL-001'})
 faculty_email='faculty-later-'+p.suffix+'@example.test'
 faculty=person.request(roster,'POST',{'fullName':'Faculty Later','email':faculty_email,'role':'Faculty','reference':'FAC-1'})
 offline=person.request(roster,'POST',{'fullName':'Offline Student','email':None,'role':'Student','reference':'ROLL-002'})
 check('multiple students and faculty receive different durable IDs',lambda:yes(len({student['number'],faculty['number'],offline['number']})==3 and len(person.request(roster))==3))
 check('Individual and institutional member IDs remain separate under one login',lambda:yes(person.request('/membership/affiliations')[0]['number']==student['number'] and student['number']!=next(m for m in person.request('/membership/portfolio') if m['id']==individual)['number']))
 check('roster enrollment never grants administrative representative authority',lambda:yes(not any(r['email']==faculty_email for r in person.request(base+'/members/'+institution+'/representatives'))))
 check('duplicate institutional email is rejected case insensitively',lambda:person.request(roster,'POST',{'fullName':'Duplicate','email':email.upper(),'role':'Faculty'},expected=400))
 check('roster rejects malformed email',lambda:person.request(roster,'POST',{'fullName':'Invalid','email':'invalid','role':'Student'},expected=400))
 check('roster rejects empty names',lambda:person.request(roster,'POST',{'fullName':'   ','email':None,'role':'Student'},expected=400))
 check('personal memberships cannot hold an organization roster',lambda:person.request(base+'/members/'+individual+'/roster','POST',{'fullName':'Invalid','email':None,'role':'Student'},expected=400))
 check('unpaid organization cannot enroll members',lambda:person.request(base+'/members/'+corporate+'/roster','POST',{'fullName':'Invalid','email':None,'role':'Employee'},expected=400))
 faculty_client=p.Client()
 faculty_client.request('/auth/signup','POST',{'email':faculty_email,'password':'Test-Password-2026!','fullName':'Faculty Later'})
 faculty_user=str(sql('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s',(faculty_email,))[0][0])
 sql('UPDATE "AspNetUsers" SET "EmailConfirmed"=true WHERE "Id"=%s',(faculty_user,));p.login(faculty_client,faculty_email)
 check('roster-only verified login receives its own society workspace without administrative scopes',lambda:yes(any(w['id']==tenant and not w['scopes'] for w in faculty_client.request('/workspaces'))))
 check('roster-only login does not claim ownership of the institution',lambda:yes(faculty_client.request('/membership/portfolio')==[]))
 check('faculty registering after enrollment discovers their existing institutional ID',lambda:yes(faculty_client.request('/membership/affiliations')[0]['number']==faculty['number']))
 check('roster-only member can read institutional resources without an admin role',lambda:yes(faculty_client.request(base+'/resources/'+resource)['id']==resource and any(r['id']==resource for r in faculty_client.request(base+'/resources'))))
 check('roster-only member cannot list private organization roster',lambda:faculty_client.request(roster,expected=403))
 check('roster-only member cannot add other people',lambda:faculty_client.request(roster,'POST',{'fullName':'Unauthorized','email':None,'role':'Student'},expected=403))
 check('roster-only member cannot access institution billing',lambda:faculty_client.request(finance+'/invoices/'+inst_renewal['invoiceId'],expected=403))
 check('cross-society organization roster cannot be accessed',lambda:person.request('/societies/'+p.otherTenant+'/membership/members/'+institution+'/roster',expected=403))
 sql('UPDATE "Memberships" SET "EffectiveFrom"=now()-interval \'1 year\', "EndsAt"=now()-interval \'1 day\' WHERE "Id"=%s',(institution,))
 check('institution expiry blocks roster resource entitlement',lambda:faculty_client.request(base+'/resources/'+resource,expected=403))
 sql('UPDATE "Memberships" SET "EndsAt"=now()+interval \'1 year\' WHERE "Id"=%s',(institution,))
 check('institution renewal restores roster entitlement without changing member IDs',lambda:yes(faculty_client.request(base+'/resources/'+resource)['id']==resource and faculty_client.request('/membership/affiliations')[0]['number']==faculty['number']))
 person.request(roster+'/'+faculty['id'],'DELETE',expected=204)
 check('roster removal immediately removes institutional resource access',lambda:faculty_client.request(base+'/resources/'+resource,expected=403))
 check('removed ID is retained in organization history',lambda:yes(any(r['number']==faculty['number'] and r['revokedAt'] for r in person.request(roster)) and faculty_client.request('/membership/affiliations')==[]))
 replacement=person.request(roster,'POST',{'fullName':'Faculty Later','email':faculty_email,'role':'Faculty','memberId':'REJOIN-'+uuid.uuid4().hex})
 check('new enrollment never reuses a removed membership ID',lambda:yes(replacement['number']!=faculty['number']))
 (ROOT/'.local/roster-browser-fixture.json').write_text(json.dumps({'tenant':tenant,'institution':institution,'controller':{'email':email,'password':'Test-Password-2026!'},'faculty':{'email':faculty_email,'password':'Test-Password-2026!'},'studentNumber':student['number'],'facultyNumber':replacement['number']}),encoding='utf-8')
 check('active institution controller can read the institutional resource',lambda:yes(person.request(base+'/resources/'+resource)['content']=='Restricted institute resource content'))
 check('resource publication rejects unsafe external URLs',lambda:owner.request(base+'/resource-library','POST',{'title':'Unsafe','description':'','kind':'Link','audiences':['Individual'],'content':'','url':'javascript:alert(1)','published':True},expected=400))
 check('cross-society resource reference cannot be read',lambda:person.request('/societies/'+p.otherTenant+'/membership/resources/'+resource,expected=403))
 representative=p.Client();rep_email,rep_user=p.signup(representative,'representative');p.login(representative,rep_email)
 owner.request(base+'/members/'+institution+'/representatives','POST',{'email':rep_email,'role':'Admin'})
 check('verified organization admin can access delegated institution membership',lambda:yes(representative.request('/membership/portfolio')[0]['id']==institution))
 check('organization admin can list institution ID cards',lambda:yes('cards' in representative.request(base+'/members/'+institution+'/id-cards')))
 check('verified organization admin can read its institutional resource',lambda:yes(representative.request(base+'/resources/'+resource)['id']==resource))
 check('organization admin cannot grant controller or society authority',lambda:representative.request(base+'/members/'+institution+'/representatives','POST',{'email':email,'role':'Controller'},expected=403))
 reps=person.request(base+'/members/'+institution+'/representatives');rep=next(r for r in reps if r['userId']==rep_user)
 person.request(base+'/members/'+institution+'/representatives/'+rep['id'],'DELETE',expected=204)
 check('revocation removes institution ID card access immediately',lambda:representative.request(base+'/members/'+institution+'/id-cards',expected=403))
 check('revocation immediately removes institution resource access',lambda:representative.request(base+'/resources/'+resource,expected=403))
 check('revocation never changes the controller individual membership',lambda:yes(next(m for m in person.request('/membership/portfolio') if m['id']==individual)['endsAt']==extended))
 person.request(base+'/members/'+institution+'/representatives','POST',{'email':rep_email,'role':'Coordinator'})
 check('organization coordinator may read member resources without acquiring billing authority',lambda:yes(representative.request(base+'/resources/'+resource)['id']==resource))
 check('organization coordinator cannot add organization roster members',lambda:representative.request(roster,'POST',{'fullName':'Unauthorized','email':None,'role':'Student'},expected=403))
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
 # Imported records use immutable approved claim addresses, with no password sharing or unsolicited email.
 imported_person,imported_member=str(uuid.uuid4()),str(uuid.uuid4())
 sql('INSERT INTO "Persons" ("Id","FullName","SearchName","CreatedAt","Revision") VALUES (%s,%s,%s,now(),0)',(imported_person,'Imported Member','imported member'))
 sql("""INSERT INTO "Memberships" ("Id","TenantId","PersonId","ChapterId","CategoryId","Number","Status","JoinedAt","EffectiveFrom","EndsAt","Profile","Privacy","Revision","PendingPayment","RenewalReminders") VALUES (%s,%s,%s,%s,%s,%s,'Active',now()-interval '1 year',now()-interval '1 year',now()+interval '1 year','{}','{}',0,false,false)""",(imported_member,tenant,imported_person,chapter,categories['Individual'],'IMPORTED-'+p.suffix))
 sql('INSERT INTO "MembershipHistories" ("Id","TenantId","MembershipId","ActorId","Action","Details","At") VALUES (%s,%s,%s,%s,%s,%s::jsonb,now())',(str(uuid.uuid4()),tenant,imported_member,p.accounts['owner'][1],'membership.import.approved',json.dumps({'claimEmails':[email.upper()]})))
 check('verified source email can access imported membership without modifying canonical person identity',lambda:yes(any(m['id']==imported_member for m in person.request('/membership/portfolio'))))
 check('imported membership holder can list their card downloads',lambda:yes(person.request(base+'/members/'+imported_member+'/id-cards')['downloadsEnabled']))
 with person.opener.open('http://127.0.0.1:5089/api/v1'+base+'/members/'+imported_member+'/id-card') as response:
  check('holder can download a physical-size PDF membership ID directly',lambda:yes(response.headers['Content-Type']=='application/pdf' and response.read().startswith(b'%PDF-')))
 check('other verified accounts cannot download the imported membership ID',lambda:representative.request(base+'/members/'+imported_member+'/id-card',expected=403))
 sql('UPDATE "AspNetUsers" SET "EmailConfirmed"=false WHERE "Id"=%s',(user,))
 check('unverified source email cannot claim imported membership',lambda:yes(not any(m['id']==imported_member for m in person.request('/membership/portfolio'))))
 sql('UPDATE "AspNetUsers" SET "EmailConfirmed"=true WHERE "Id"=%s',(user,))
 check('tampered membership QR token fails closed',lambda:p.Client().request('/credentials/verify/m-invalid',expected=404))
 # The signup choice records intent only, never authority or paid entitlement.
 check('signup rejects invented membership types',lambda:p.Client().request('/auth/signup','POST',{'email':'bad-'+p.suffix+'@example.test','password':'Test-Password-2026!','fullName':'Bad kind','membershipKind':'SuperAdmin'},expected=400))
 signup_email='corporate-choice-'+p.suffix+'@example.test'
 p.Client().request('/auth/signup','POST',{'email':signup_email,'password':'Test-Password-2026!','fullName':'Corporate applicant','membershipKind':'Corporate','societyId':tenant})
 check('signup records the chosen membership type without granting platform privileges',lambda:yes(sql('SELECT "PreferredMembershipKind","PlatformAdmin","EmailConfirmed" FROM "AspNetUsers" WHERE "Email"=%s',(signup_email,))[0]==('Corporate',False,False)))
 check('platform pricing preserves the three configured annual fees',lambda:yes({c['kind']:c['offer']['price'] for c in platform.request('/platform/membership-pricing/'+tenant)['categories'] if c['id'] in categories.values()}=={'Individual':500,'Institutional':10000,'Corporate':10000}))
 check('active detail filter excludes expired memberships',lambda:yes(all(m['membershipState'] in ['Active','Expiring soon'] for m in owner.request(base+'/members?validity=Active&pageSize=100')['items'])))
 check('joining-date detail filter excludes older records',lambda:yes(owner.request(base+'/members?joinedFrom=2099-01-01')['total']==0))
 # UX writes stay inside synthetic fixtures and retain access boundaries.
 ux_chapter=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region'],'UX contact chapter','UX-'+p.suffix))['id']
 owner.request(base+'/members/'+institution+'/chapters/'+ux_chapter,'PUT',expected=204)
 contact_path=base+'/members/'+institution+'/contact-people'
 current=owner.request(contact_path);people=[{'name':'UX Primary '+p.suffix,'email':'ux-primary-'+p.suffix+'@example.test','mobile':'9876543210','designation':'Program head','department':'Engineering'},{'name':'UX Additional','email':'ux-additional-'+p.suffix+'@example.test','mobile':'9876543211','designation':'Advisor','department':'Research'}]
 check('UX invalid organization contact email is rejected',lambda:owner.request(contact_path,'PUT',{'revision':current['revision'],'contacts':[{**people[0],'email':'invalid'}]},expected=400))
 check('UX authorized contact update succeeds',lambda:yes(owner.request(contact_path,'PUT',{'revision':current['revision'],'contacts':people})['revision']==current['revision']+1))
 check('UX stale contact update is rejected',lambda:owner.request(contact_path,'PUT',{'revision':current['revision'],'contacts':people},expected=409))
 check('UX institution and linked chapter share the updated primary contact',lambda:yes(json.loads(owner.request(prefix+'/organizations/units/'+ux_chapter)['details'])['contactName']==people[0]['name']))
 check('UX contact email can find the organization',lambda:yes(institution in [m['id'] for m in owner.request(base+'/members?kind=Institutional&search='+people[1]['email'])['items']]))
 check('UX contact mobile can find the organization',lambda:yes(institution in [m['id'] for m in owner.request(base+'/members?kind=Institutional&search=9876543211')['items']]))
 check('UX changes identify the actor and preserve old and new contacts',lambda:yes(any(h['action']=='organization.contacts.updated' and h['actor'] and 'before' in json.loads(h['details']) and 'after' in json.loads(h['details']) for h in owner.request(base+'/members/'+institution+'/changes'))))
 check('UX organization coordinator cannot edit private organization contacts',lambda:representative.request(contact_path,'PUT',{'revision':current['revision']+1,'contacts':people},expected=403))
 check('UX anonymous contact access is blocked',lambda:p.Client().request(contact_path,expected=401))
 check('UX individual cannot be edited through organization contacts',lambda:owner.request(base+'/members/'+individual+'/contact-people','PUT',{'revision':0,'contacts':people},expected=400))
 check('UX duplicate organization names are flagged',lambda:yes(any(m['id']==institution for m in owner.request(base+'/duplicates?kind=Institutional&name=Synthetic%20Institute'))))
 check('UX member reminder timing accepts supported values',lambda:person.request(base+'/members/'+individual+'/renewal-reminders','PUT',{'enabled':False,'days':14},expected=204))
 check('UX reminder timing rejects unsupported values',lambda:person.request(base+'/members/'+individual+'/renewal-reminders','PUT',{'enabled':False,'days':365},expected=400))
 inspect_request={'chapterId':chapter,'categoryId':categories['Individual'],'format':'csv','content':'FullName,Email,Phone,City,Number\nUX New,ux-new-'+p.suffix+'@example.test,9876543210,City,UX-'+p.suffix,'mapping':{'fullName':'FullName','email':'Email','phone':'Phone','city':'City','number':'Number'}}
 check('UX import inspection exposes headers and sample values before validation',lambda:yes(owner.request(base+'/imports/inspect','POST',inspect_request)['headers']==['FullName','Email','Phone','City','Number']))
 old=owner.request(base+'/members/'+individual)['member'];dup={**inspect_request,'content':'FullName,Email,Phone,City,Number\n'+old['fullName']+',,9876543210,City,'+old['number']}
 job=owner.request(base+'/imports/preview','POST',dup);row=owner.request(base+'/imports/'+job['id'])['rows'][0]
 check('UX import preview compares existing values without overwriting',lambda:yes(row['status']=='Invalid' and json.loads(row['data'])['existing']['number']==old['number']))
 with owner.opener.open('http://127.0.0.1:5089/api/v1'+base+'/imports/'+job['id']+'/errors.csv') as response:
  check('UX downloadable row errors provide correction instructions',lambda:yes(response.headers['Content-Type'].startswith('text/csv') and b'Existing membership matches' in response.read()))
 # Chapter CRUD uses disposable synthetic records; delete is reversible archival.
 crud_body=p.unit(p.chapter,p.units['region'],'CRUD Chapter '+p.suffix,'CRUD-'+p.suffix)
 crud=owner.request(prefix+'/organizations/units','POST',crud_body)['id'];path=prefix+'/organizations/units/'+crud
 record=owner.request(path)
 check('chapter CRUD exposes scoped operation permissions',lambda:yes(record['canEdit'] and record['canCreate'] and record['canArchive'] and record['kind']=='Chapter'))
 check('chapter CRUD rejects unauthorized edit',lambda:representative.request(path,'PUT',{**crud_body,'revision':record['revision']},expected=403))
 check('chapter CRUD edits names and metadata',lambda:owner.request(path,'PUT',{**crud_body,'name':'Updated CRUD Chapter '+p.suffix,'details':json.dumps({'city':'Updated City','custom':'preserved'}),'revision':record['revision']},expected=204))
 updated=owner.request(path)
 check('chapter CRUD reads edited values',lambda:yes(updated['name'].startswith('Updated CRUD') and json.loads(updated['details'])['city']=='Updated City'))
 check('chapter CRUD rejects stale deletion',lambda:owner.request(path+'?revision='+str(record['revision']),'DELETE',expected=409))
 check('chapter CRUD rejects unauthorized deletion',lambda:representative.request(path+'?revision='+str(updated['revision']),'DELETE',expected=403))
 check('chapter CRUD delete archives the record',lambda:owner.request(path+'?revision='+str(updated['revision']),'DELETE',expected=204))
 archived=owner.request(path)
 check('chapter CRUD archival preserves metadata and identity',lambda:yes(archived['id']==crud and archived['status']=='Archived' and json.loads(archived['details'])['custom']=='preserved'))
 check('chapter CRUD archived records are hidden from the normal directory',lambda:yes(owner.request(prefix+'/organizations/units?search=CRUD-'+p.suffix)['total']==0))
 check('chapter CRUD archived records can be browsed explicitly',lambda:yes(owner.request(prefix+'/organizations/units?status=Archived&search=CRUD-'+p.suffix)['items'][0]['id']==crud))
 check('chapter CRUD prevents editing before restore',lambda:owner.request(path,'PUT',{**crud_body,'revision':archived['revision']},expected=400))
 check('chapter CRUD rejects stale restore',lambda:owner.request(path+'/restore?revision=0','POST',expected=409))
 check('chapter CRUD restore recovers previous status',lambda:owner.request(path+'/restore?revision='+str(archived['revision']),'POST',expected=204))
 restored=owner.request(path)
 check('chapter CRUD restored record returns to directory',lambda:yes(restored['status']=='Active' and owner.request(prefix+'/organizations/units?search=CRUD-'+p.suffix)['items'][0]['id']==crud))
 check('chapter CRUD duplicate code conflicts',lambda:owner.request(prefix+'/organizations/units','POST',crud_body,expected=409))
 check('chapter CRUD rejects invalid names',lambda:owner.request(prefix+'/organizations/units','POST',{**crud_body,'name':'','code':'INVALID-'+p.suffix},expected=400))
 region_record=owner.request(prefix+'/organizations/units/'+p.units['region'])
 check('chapter CRUD protects parents with live subordinate units',lambda:owner.request(prefix+'/organizations/units/'+p.units['region']+'?revision='+str(region_record['revision']),'DELETE',expected=400))
 check('chapter CRUD archive and restore are audited',lambda:yes(sql('SELECT count(*) FROM "Audits" WHERE "ResourceId"=%s AND "Action" IN (%s,%s)',(crud,'unit.archived','unit.restored'))[0][0]==2))
 # Platform-only manual administration, isolated from society-role membership.
 manual='/platform/membership/'+tenant
 check('platform can open manual setup without a society-role assignment',lambda:yes(len(platform.request(manual+'/setup')['categories'])>=3))
 check('society administrators cannot use platform manual override',lambda:owner.request(manual+'/setup',expected=403))
 check('platform directory reads all memberships without inheriting society roles',lambda:yes(platform.request(base+'/members?kind=Individual')['total']>=1))
 check('platform can read private membership details without society roles',lambda:yes(platform.request(base+'/members/'+individual)['platformAdmin'] and platform.request(base+'/members/'+individual)['profile'] is not None))
 manual_body={'fullName':'Manual Individual '+p.suffix,'number':'MANUAL-'+p.suffix,'chapterId':chapter,'categoryId':categories['Individual'],'status':'Active','joinedOn':'2023-08-01','validFrom':'2023-08-01','validThrough':'2027-01-31','profile':json.dumps({'email':'manual-'+p.suffix+'@example.test','phone':'9876543210'}),'revision':0,'reason':'Owner-approved offline membership entry'}
 check('manual override requires an audit reason',lambda:platform.request(manual+'/members','POST',{**manual_body,'reason':''},expected=400))
 created=platform.request(manual+'/members','POST',manual_body);manual_id=created['id'];detail=platform.request(manual+'/members/'+manual_id)
 check('platform manual form can warn about duplicate IDs',lambda:yes(any(m['id']==manual_id for m in platform.request(manual+'/duplicates?number='+manual_body['number']) )))
 check('platform without society roles can search private contact emails',lambda:yes(any(m['id']==manual_id for m in platform.request(base+'/members?search=manual-'+p.suffix+'@example.test')['items'])))
 check('platform manually creates active membership without fabricating payment',lambda:yes(detail['member']['status']=='Active' and not detail['member']['pendingPayment'] and sql('SELECT count(*) FROM "MembershipRenewal" WHERE "MembershipId"=%s',(manual_id,))[0][0]==0))
 check('manual expiry is inclusive through the chosen India date',lambda:yes(detail['validThrough']=='2027-01-31' and datetime.datetime.fromisoformat(detail['member']['endsAt'])==datetime.datetime(2027,1,31,18,30,tzinfo=datetime.timezone.utc)))
 check('manual duplicate IDs are rejected',lambda:platform.request(manual+'/members','POST',manual_body,expected=409))
 check('manual edits reject stale revisions',lambda:platform.request(manual+'/members/'+manual_id,'PUT',{**manual_body,'revision':0},expected=409))
 alternative=owner.request(base+'/categories','POST',{'name':'Individual alternate '+p.suffix,'description':'Synthetic alternate type','kind':'Individual','active':True})['id']
 changed={**manual_body,'fullName':'Manually corrected '+p.suffix,'categoryId':alternative,'validThrough':'2028-02-29','revision':detail['revision'],'reason':'Corrected member name, type and expiry'}
 platform.request(manual+'/members/'+manual_id,'PUT',changed);detail=platform.request(manual+'/members/'+manual_id)
 check('platform edits name category and leap-day expiry',lambda:yes(detail['member']['fullName']==changed['fullName'] and detail['member']['categoryId']==alternative and detail['validThrough']=='2028-02-29'))
 check('platform edits preserve a term when only contact fields change',lambda:yes(platform.request(manual+'/members/'+manual_id,'PUT',{**changed,'revision':detail['revision'],'reason':'Correct contact detail with unchanged term'})['id']==manual_id and platform.request(manual+'/members/'+manual_id)['member']['endsAt']==detail['member']['endsAt']))
 detail=platform.request(manual+'/members/'+manual_id)
 platform.request(manual+'/members/'+manual_id,'PUT',{**changed,'validThrough':None,'revision':detail['revision'],'reason':'Approved lifetime membership without expiry'});detail=platform.request(manual+'/members/'+manual_id)
 check('platform can explicitly remove membership expiry',lambda:yes(detail['member']['endsAt'] is None))
 check('manual dates reject end-before-start',lambda:platform.request(manual+'/members/'+manual_id,'PUT',{**changed,'validThrough':'2020-01-01','revision':detail['revision']},expected=400))
 source=platform.request(manual+'/members/'+institution)
 check('linked organization cannot silently turn into a human',lambda:platform.request(manual+'/members/'+institution,'PUT',{**manual_body,'fullName':'Wrong conversion','number':source['member']['number'],'revision':source['revision'],'categoryId':categories['Individual'],'reclassifyConfirmed':True},expected=400))
 expired={**manual_body,'number':'MANUAL-EXP-'+p.suffix,'fullName':'Expired manual member','validFrom':'2020-01-01','validThrough':'2020-12-31'}
 expired_id=platform.request(manual+'/members','POST',expired)['id']
 with platform.opener.open('http://127.0.0.1:5089/api/v1'+base+'/members/'+expired_id+'/id-card') as response:
  check('platform downloads another individual ID even when expired',lambda:yes(response.headers['Content-Type']=='application/pdf' and response.read().startswith(b'%PDF-')))
 import io,zipfile
 with platform.opener.open('http://127.0.0.1:5089/api/v1'+manual+'/id-cards.zip?ids='+manual_id+','+expired_id) as response:
  contents=response.read();archive=zipfile.ZipFile(io.BytesIO(contents));check('platform downloads selected individual ID PDFs in one ZIP',lambda:yes(len(archive.namelist())==2 and all(archive.read(n).startswith(b'%PDF-') for n in archive.namelist())))
 check('bulk individual cards reject organization selections',lambda:platform.request(manual+'/id-cards.zip?ids='+institution,expected=400))
 check('non-platform administrators cannot download bulk member cards',lambda:owner.request(manual+'/id-cards.zip?ids='+manual_id,expected=403))
 check('other members cannot download someone else individual ID',lambda:representative.request(base+'/members/'+manual_id+'/id-card',expected=403))
 detail=platform.request(manual+'/members/'+manual_id)
 check('platform archives any manually created membership',lambda:platform.request(manual+'/members/'+manual_id,'DELETE',{'revision':detail['revision'],'reason':'Owner-approved membership removal'},expected=204))
 detail=platform.request(manual+'/members/'+manual_id)
 check('archived membership is hidden from normal register',lambda:yes(manual_id not in [m['id'] for m in platform.request(base+'/members?search=MANUAL-'+p.suffix)['items']] and detail['archived']))
 check('platform can browse archived memberships',lambda:yes(any(m['id']==manual_id for m in platform.request(base+'/members?archived=true&search=MANUAL-'+p.suffix)['items'])))
 check('ordinary member cannot browse platform archive',lambda:owner.request(base+'/members?archived=true',expected=403))
 platform.request(manual+'/members/'+manual_id+'/restore','POST',{'revision':detail['revision'],'reason':'Owner approved reinstatement'},expected=204)
 check('platform restores the same ID and lifetime validity',lambda:yes(platform.request(manual+'/members/'+manual_id)['member']['number']==manual_body['number'].upper() and platform.request(manual+'/members/'+manual_id)['member']['endsAt'] is None))
 check('manual operations record actor reason and before-after history',lambda:yes(any(h['action']=='platform.membership.updated' and json.loads(h['details']).get('reason') and 'after' in json.loads(h['details']) for h in platform.request(base+'/members/'+manual_id+'/changes'))))
 claim_body={**manual_body,'number':'MANUAL-CLAIM-'+p.suffix,'profile':json.dumps({'email':email}),'allowEmailClaim':True,'reason':'Explicitly approve verified email access'}
 claim_id=platform.request(manual+'/members','POST',claim_body)['id']
 check('manual member can discover ID using explicitly approved verified account email',lambda:yes(any(m['id']==claim_id for m in person.request('/membership/portfolio'))))
 with person.opener.open('http://127.0.0.1:5089/api/v1'+base+'/members/'+claim_id+'/id-card') as response:
  check('manually registered individual can download their own ID',lambda:yes(response.read().startswith(b'%PDF-')))
 claim_detail=platform.request(manual+'/members/'+claim_id)
 check('approved account claim prevents person-to-organization identity merging',lambda:platform.request(manual+'/members/'+claim_id,'PUT',{**claim_body,'categoryId':categories['Corporate'],'reclassifyConfirmed':True,'revision':claim_detail['revision']},expected=400))
 sql('UPDATE "StudioPolicy" SET "MemberDownloads"=false WHERE "TenantId"=%s',(tenant,))
 check('individual self ID download is available when issued-card downloads are disabled',lambda:yes(person.request(base+'/members/'+claim_id+'/id-cards')['downloadsEnabled']))
 with person.opener.open('http://127.0.0.1:5089/api/v1'+base+'/members/'+claim_id+'/id-card') as response:
  check('individual downloads actual PDF independently of issued-card policy',lambda:yes(response.read().startswith(b'%PDF-')))
 check('issued-card policy still blocks organization membership documents',lambda:person.request(base+'/members/'+institution+'/id-card',expected=403))
 sql('UPDATE "StudioPolicy" SET "MemberDownloads"=true WHERE "TenantId"=%s',(tenant,))
 # Install the branded template in this isolated test society and test the actual member PDF.
 sql('UPDATE "Tenants" SET "Name"=%s WHERE "Id"=%s',('AIMER synthetic template verification',tenant))
 subprocess.run(['dotnet',str(API_DLL),'--install-aimer-card'],cwd=ROOT/'backend',env={**env,'AIMER_CARD_TENANT':tenant},stdout=log,stderr=log,check=True)
 check('professional individual template is published with an immutable version',lambda:yes(sql('SELECT "PublishedVersion","Status" FROM "DesignTemplate" WHERE "Id"=%s',('8685a442-ed2f-49c1-9d04-fcaa8472a837',))[0]==(1,'Published')))
 with person.opener.open('http://127.0.0.1:5089/api/v1'+base+'/members/'+claim_id+'/id-card') as response:
  branded_pdf=response.read()
 import fitz,zxingcpp
 doc=fitz.open(stream=branded_pdf,filetype='pdf');front=doc[0].get_pixmap(matrix=fitz.Matrix(4,4));decoded=zxingcpp.read_barcodes(__import__('PIL.Image',fromlist=['Image']).frombytes('RGB',[front.width,front.height],front.samples))
 check('branded ID PDF has front and back with a scannable live verification QR',lambda:yes(len(doc)==2 and any('/verify/credential/m-' in r.text for r in decoded)))
 subprocess.run(['dotnet',str(API_DLL),'--install-aimer-card'],cwd=ROOT/'backend',env={**env,'AIMER_CARD_TENANT':tenant},stdout=log,stderr=log,check=True)
 check('template installation is idempotent and preserves saved designs',lambda:yes(sql('SELECT count(*) FROM "DesignVersion" WHERE "TemplateId"=%s',('8685a442-ed2f-49c1-9d04-fcaa8472a837',))[0][0]==1))
 self_path='/membership/my/'+claim_id+'/profile';self_before=person.request(self_path)
 person.request(self_path,'PUT',{'revision':self_before['revision'],'profile':json.dumps({'phone':'+91 90000 00123','city':'Self profile city','addresses':['New address']})},expected=204)
 self_after=person.request(self_path)
 check('individual creates and reads personal profile fields',lambda:yes(self_after['profile']['city']=='Self profile city' and self_after['fullName']==self_before['fullName'] and self_after['number']==self_before['number']))
 check('individual stale profile edits are rejected',lambda:person.request(self_path,'PUT',{'revision':self_before['revision'],'profile':'{}'},expected=409))
 for field in ['fullName','number','categoryId','endsAt','adminArchived','photoUrl']:
  check('self profile rejects protected '+field,lambda field=field:person.request(self_path,'PUT',{'revision':self_after['revision'],'profile':json.dumps({field:'tamper'})},expected=400))
 check('another member cannot read this personal editor',lambda:representative.request(self_path,expected=403))
 check('self profile rejects malformed email',lambda:person.request(self_path,'PUT',{'revision':self_after['revision'],'profile':json.dumps({'email':'invalid'})},expected=400))
 person.request(self_path,'DELETE',{'revision':self_after['revision']},expected=204)
 check('individual clears optional details while keeping identity',lambda:yes(not person.request(self_path)['profile'] and person.request(self_path)['number']==self_before['number']))
 check('normal member creation cannot set platform archive flags',lambda:owner.request(base+'/members','POST',{'fullName':'Invalid flags','chapterId':chapter,'categoryId':categories['Individual'],'status':'Inactive','profile':json.dumps({'adminArchived':True})},expected=400))
finally:
 if server:server.terminate();server.wait(timeout=20)
 log.close();(ROOT/'tests/membership-experience-results.json').write_text(json.dumps({'passed':sum(r['passed'] for r in results),'total':len(results),'results':results},indent=2),encoding='utf-8')
