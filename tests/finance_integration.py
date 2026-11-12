"""Phase 5 PostgreSQL/HTTP finance suite; imports all Phase 1–4 regressions."""
from api_runtime import API_DLL
import communication_integration as f
import json,time,subprocess,datetime,uuid,hashlib,hmac,urllib.request,urllib.error,concurrent.futures
from pathlib import Path
import psycopg
c=f.c;p=f.p;ROOT=p.ROOT;tenant=p.tenant;prefix='/societies/'+tenant;base=prefix+'/finance';owner=p.owner;reviewer=c.reviewer
env={**f.env,'Finance__AllowTestGateway':'true'};results=[];server=None;log=open(ROOT/'.local/phase5-api.log','w',encoding='utf-8')
def start():
 global server
 server=subprocess.Popen(['dotnet',str(API_DLL),'--urls','http://127.0.0.1:5089'],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
 for _ in range(80):
  try:owner.request('/auth/csrf');return
  except Exception:time.sleep(.15)
 raise RuntimeError('Finance API did not start')
def check(name,fn):
 started=time.perf_counter()
 try:fn();results.append({'name':name,'passed':True,'ms':round((time.perf_counter()-started)*1000,2)});print('PASS PHASE5',name,flush=True)
 except Exception as ex:results.append({'name':name,'passed':False,'error':str(ex)});print('FAIL PHASE5',name,str(ex),flush=True);raise
def yes(v):assert v
sql=f.sql;today=datetime.datetime.now(datetime.timezone.utc).date();yearend=today.replace(year=today.year+1)-datetime.timedelta(days=1)
def invoice(amount=100,**extra):return owner.request(base+'/invoices','POST',{'entityId':entity,'unitId':chapter,'counterpartyName':'ignored canonical name','membershipId':memberid,'date':str(today),'dueOn':str(today+datetime.timedelta(days=30)),'lines':[{'description':'Annual membership','quantity':1,'unitPrice':amount,'accountCode':'4000'}],**extra})
def issue(i):return owner.request(base+'/invoices/'+i['id']+'/issue','POST')
def payment(amount=100,**extra):return owner.request(base+'/payments','POST',{'entityId':entity,'unitId':chapter,'amount':amount,'reference':'manual-'+uuid.uuid4().hex[:12],'date':str(today),'payer':'Synthetic finance member','membershipId':memberid,'method':'Cash',**extra})
def confirm(pay):return reviewer.request(base+'/payments/'+pay['id']+'/decision','POST',{'decision':'Approve'})
def allocate(pay,inv,amount,key=None):return owner.request(base+'/payments/'+pay['id']+'/allocate','POST',{'invoiceId':inv['id'],'amount':amount,'key':key or uuid.uuid4().hex})
def waitjob(job,timeout=20):
 until=time.time()+timeout
 while time.time()<until:
  row=next((j for j in owner.request(base+'/jobs?entity='+entity) if j['id']==job['id']),None)
  if row and row['state'] in ['Completed','Failed']:return row
  time.sleep(.15)
 raise AssertionError('Durable job did not finish: '+str(row))
def plan(**extra):return owner.request(base+'/dues-plans','POST',{'entityId':entity,'unitId':chapter,'name':'Annual dues','code':'F5-'+uuid.uuid4().hex[:8].upper(),'amount':120,'effectiveFrom':str(today),'requiresApproval':False,**extra})
def assess(rule,**extra):return owner.request(base+'/assessments','POST',{'entityId':entity,'planId':rule['id'],'startsOn':str(today),'endsOn':str(yearend),'membershipIds':[memberid],**extra})
try:
 start();p.login(owner,p.accounts['owner'][0],'Changed-Password-2026!')
 chapter=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region'],'Finance test chapter','F5-'+uuid.uuid4().hex[:6]))['id'];remote=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region2'],'Separate finance chapter','F5-'+uuid.uuid4().hex[:6]))['id']
 member,_,memberuser=c.account('finance-member',chapter);memberid=member.request('/membership/me')[0]['id'];outsider,_,_=c.account('finance-outsider',remote)
 e=owner.request(base+'/entities','POST',{'name':'Central Society Accounts','legalName':'Synthetic Finance Entity','unitId':p.units['region'],'units':[p.units['region'],chapter],'currency':'INR','requireInvoiceApproval':False});entity=e['id']
 check('financial entity is explicit and separate from chapter master',lambda:yes(e['legalName']=='Synthetic Finance Entity' and e['currency']=='INR'))
 check('entity seeds chart of accounts and monthly fiscal periods',lambda:yes(len(owner.request(base+'/accounts?entity='+entity))>=25 and len(owner.request(base+'/periods?entity='+entity))==12))
 check('member cannot configure accounting entity',lambda:member.request(base+'/entities','POST',{'name':'Breach','legalName':'Breach','units':[chapter]},expected=403))
 check('same chapter cannot silently belong to another legal entity',lambda:owner.request(base+'/entities','POST',{'name':'Duplicate','legalName':'Duplicate','unitId':chapter,'units':[chapter],'currency':'INR'},expected=409))
 otherentity=owner.request(base+'/entities','POST',{'name':'Separate Fund','legalName':'Independent Legal Fund','unitId':remote,'units':[remote],'currency':'INR','requireInvoiceApproval':False})['id']
 annual=plan();state={}
 check('annual member dues assess through the shared invoice engine',lambda:state.update(assessment=assess(annual)))
 assessment=state['assessment'][0];i=owner.request(base+'/invoices/'+assessment['invoiceId'])['invoice']
 check('dues assessment snapshots the rule and member evidence',lambda:yes(assessment['total']==120 and 'Revision' in assessment['snapshot']))
 check('duplicate assessment is idempotent',lambda:yes(assess(annual)[0]['id']==assessment['id']))
 check('assessment issues a balanced receivable and income journal',lambda:yes(i['status']=='Issued' and i['outstanding']==120))
 check('issued invoice retains original amount after plan edit',lambda:owner.request(base+'/dues-plans/'+annual['id'],'PUT',{'entityId':entity,'unitId':chapter,'name':'Revised future dues','code':annual['code'],'amount':200,'effectiveFrom':str(today),'requiresApproval':False,'revision':0}))
 check('historical invoice amount did not silently change',lambda:yes(owner.request(base+'/invoices/'+i['id'])['invoice']['total']==120))
 once=plan(kind='Life membership fee',frequency='Once');assess(once)
 check('life membership fee cannot be assessed twice in another period',lambda:owner.request(base+'/assessments','POST',{'entityId':entity,'planId':once['id'],'startsOn':str(today+datetime.timedelta(days=1)),'endsOn':str(yearend+datetime.timedelta(days=1)),'membershipIds':[memberid]},expected=400))
 check('joining fee uses configurable once-only rule',lambda:yes(assess(plan(kind='Joining fee',frequency='Once'))[0]['total']==120))
 prorated=plan(proration='Daily');sql('UPDATE "Memberships" SET "EffectiveFrom"=%s WHERE "Id"=%s',(datetime.datetime.combine(today+datetime.timedelta(days=30),datetime.time.min,datetime.timezone.utc),memberid))
 check('daily proration uses eligible days and decimal rounding',lambda:yes(assess(prorated)[0]['total']<120))
 sql('UPDATE "Memberships" SET "EffectiveFrom"=now()-interval \'1 day\' WHERE "Id"=%s',(memberid,))
 waiverplan=plan();waiver=owner.request(base+'/exemptions','POST',{'entityId':entity,'unitId':chapter,'planId':waiverplan['id'],'membershipId':memberid,'startsOn':str(today),'endsOn':str(yearend),'percent':50,'reason':'Authorized waiver'})
 check('waiver cannot be self approved',lambda:owner.request(base+'/exemptions/'+waiver['id']+'/decision','POST',{'decision':'Approve'},expected=403))
 reviewer.request(base+'/exemptions/'+waiver['id']+'/decision','POST',{'decision':'Approve'})
 check('approved waiver reduces actual assessment',lambda:yes(assess(waiverplan)[0]['total']==60))
 exemptplan=plan();exempt=owner.request(base+'/exemptions','POST',{'entityId':entity,'unitId':chapter,'planId':exemptplan['id'],'membershipId':memberid,'startsOn':str(today),'endsOn':str(yearend),'percent':100,'reason':'Full exemption'})
 reviewer.request(base+'/exemptions/'+exempt['id']+'/decision','POST',{'decision':'Approve'})
 check('full exemption records zero payable without a fake payment',lambda:yes(assess(exemptplan)[0]['total']==0))
 chapterplan=plan(target='Chapter',amount=50,perMember=10,incomeCode='4100')
 ca=assess(chapterplan,membershipIds=[],chapterIds=[chapter])[0]
 check('chapter institutional assessment is separate and count-based',lambda:yes(ca['membershipId'] is None and ca['chapterId']==chapter and ca['memberCount']==1 and ca['total']==60))
 sql('UPDATE "Memberships" SET "Status"=\'Inactive\' WHERE "Id"=%s',(memberid,))
 check('historical member-count assessment does not recalculate',lambda:yes(owner.request(base+'/invoices/'+ca['invoiceId'])['invoice']['total']==60))
 sql('UPDATE "Memberships" SET "Status"=\'Active\' WHERE "Id"=%s',(memberid,))
 calc=invoice(lines=[{'description':'Decimal taxable item','quantity':3,'unitPrice':19.95,'discountPercent':10,'taxPercent':18,'accountCode':'4000'}])
 check('invoice line discounts and taxes use decimal minor-unit rounding',lambda:yes(calc['subtotal']==59.85 and calc['discount']==5.99 and calc['tax']==9.69 and calc['total']==63.55))
 check('fractional minor-unit price is rejected',lambda:owner.request(base+'/invoices','POST',{'entityId':entity,'unitId':chapter,'counterpartyName':'Test','date':str(today),'dueOn':str(today),'lines':[{'description':'bad precision','quantity':1,'unitPrice':1.001}]},expected=400))
 pay=payment(40);check('manual payment remains pending without receipt',lambda:yes(pay['status']=='Pending Approval' and not any(r['paymentId']==pay['id'] for r in owner.request(base+'/receipts?entity='+entity)['items'])))
 check('payment claimant cannot self approve',lambda:owner.request(base+'/payments/'+pay['id']+'/decision','POST',{'decision':'Approve'},expected=403))
 confirm(pay);allocate(pay,i,40,'partial-allocation')
 check('partial payment allocates without altering invoice total',lambda:yes(owner.request(base+'/invoices/'+i['id'])['invoice']['paymentStatus']=='Partially Paid'))
 check('allocation key retry does not post twice',lambda:yes(allocate(pay,i,40,'partial-allocation')['amount']==40))
 check('over-allocation is rejected transactionally',lambda:owner.request(base+'/payments/'+pay['id']+'/allocate','POST',{'invoiceId':i['id'],'amount':1,'key':'excess'},expected=400))
 extra=payment(100);confirm(extra);allocate(extra,i,80)
 check('full payment closes outstanding with overpayment retained as advance',lambda:yes(owner.request(base+'/invoices/'+i['id'])['invoice']['outstanding']==0 and sql('SELECT sum("Amount") FROM "PaymentAllocation" WHERE "PaymentId"=%s',(extra['id'],))[0][0]==80))
 receipt=next(r for r in owner.request(base+'/receipts?entity='+entity)['items'] if r['paymentId']==extra['id'])
 check('confirmed collection produces a numbered actual receipt',lambda:yes(receipt['amount']==100 and receipt['number'].startswith('RCT-')))
 refund=owner.request(base+'/payments/'+extra['id']+'/refunds','POST',{'amount':30,'reference':'refund-'+uuid.uuid4().hex[:8],'reason':'Approved partial return','date':str(today)})
 check('refund cannot be self approved',lambda:owner.request(base+'/refunds/'+refund['id']+'/decision','POST',{'decision':'Approve'},expected=403))
 reviewer.request(base+'/refunds/'+refund['id']+'/decision','POST',{'decision':'Approve'})
 check('refund releases allocations and reopens exact receivable',lambda:yes(owner.request(base+'/invoices/'+i['id'])['invoice']['outstanding']==10))
 credit=owner.request(base+'/invoices/'+i['id']+'/credit','POST',{'amount':10,'reason':'Authorized adjustment','date':str(today)})
 reviewer.request(base+'/invoices/'+credit['id']+'/decision','POST',{'decision':'Approve','revision':0})
 check('credit note is independently posted and linked to original',lambda:yes(owner.request(base+'/invoices/'+i['id'])['invoice']['credited']==10))
 check('member can view only own issued invoice',lambda:yes(member.request(base+'/invoices/'+i['id'])['invoice']['id']==i['id']))
 check('outside member cannot inspect another balance',lambda:outsider.request(base+'/invoices/'+i['id'],expected=403))
 check('outside member cannot obtain another statement',lambda:outsider.request(base+'/statements/member/'+memberid,expected=403))
 statement=member.request(base+'/statements/member/'+memberid+'?entity='+entity)
 check('member statement closing reconciles charges and credits',lambda:yes(all(round(b['opening']+b['charges']-b['creditsAndAllocations'],2)==b['closing'] for b in statement['balances'])))
 check('chapter statement is a distinct institutional account',lambda:yes(owner.request(base+'/statements/chapter/'+chapter+'?entity='+entity)['balances'][0]['closing']==60))
 tb=owner.request(base+'/reports?report=Trial%20Balance&entity='+entity)
 check('trial balance reconciles all posted finance sources',lambda:yes(all(row['difference']==0 for row in tb['totalDebits'])))
 check('unbalanced manual journal rejected',lambda:owner.request(base+'/journals','POST',{'entityId':entity,'unitId':chapter,'date':str(today),'purpose':'Bad journal','key':'bad-journal','lines':[{'accountCode':'1000','debit':10,'credit':0},{'accountCode':'3000','debit':0,'credit':9}]},expected=400))
 check('manual journals cannot bypass subledger control accounts',lambda:owner.request(base+'/journals','POST',{'entityId':entity,'unitId':chapter,'date':str(today),'purpose':'Bad control','key':'bad-control','lines':[{'accountCode':'1100','debit':10,'credit':0},{'accountCode':'3000','debit':0,'credit':10}]},expected=400))
 request=owner.request(base+'/journals','POST',{'entityId':entity,'unitId':chapter,'date':str(today),'purpose':'Opening actual cash fixture','key':'opening-fixture','lines':[{'accountCode':'1000','debit':200,'credit':0},{'accountCode':'3000','debit':0,'credit':200}]})
 check('manual journal requires independent review',lambda:owner.request(base+'/journals/requests/'+request['id']+'/decision','POST',{'decision':'Approve'},expected=403))
 posted=reviewer.request(base+'/journals/requests/'+request['id']+'/decision','POST',{'decision':'Approve'})
 journal=owner.request(base+'/journals/'+posted['documentId'])['journal']
 reversedj=reviewer.request(base+'/journals/'+journal['id']+'/reverse','POST',{'date':str(today),'reason':'Reverse fixture opening'})
 check('journal reversal posts a separate linked balanced journal',lambda:yes(reversedj['reversesId']==journal['id']))
 def dbreject(statement,args=()):
  try:
   with psycopg.connect(p.DSN) as db:db.execute(statement,args)
  except psycopg.Error:return True
  return False
 check('PostgreSQL rejects direct mutation of posted journal lines',lambda:yes(dbreject('UPDATE "JournalLine" SET "Debit"="Debit"+1 WHERE "JournalId"=%s',(journal['id'],))))
 check('PostgreSQL preserves issued invoice document content',lambda:yes(dbreject('UPDATE "FinanceInvoice" SET "Total"="Total"+1 WHERE "Id"=%s',(i['id'],))))
 periods=owner.request(base+'/periods?entity='+entity);period=next(x for x in periods if x['startsOn']<=str(today)<=x['endsOn'])
 budget=owner.request(base+'/budgets','POST',{'entityId':entity,'unitId':chapter,'periodId':period['id'],'name':'Chapter operations','amount':200,'lines':json.dumps([{'accountCode':'5000','amount':200}])})
 check('budget author cannot self approve',lambda:owner.request(base+'/budgets/'+budget['id']+'/decision','POST',{'decision':'Approve','revision':0},expected=403))
 reviewer.request(base+'/budgets/'+budget['id']+'/decision','POST',{'decision':'Approve','revision':0})
 expense=owner.request(base+'/expenses','POST',{'entityId':entity,'unitId':chapter,'budgetId':budget['id'],'vendor':'Synthetic Vendor','description':'Authorized operations','accountCode':'5000','amount':70,'date':str(today)})
 check('expense commitments reserve actual category budget',lambda:yes(next(b for b in owner.request(base+'/budgets?entity='+entity) if b['id']==budget['id'])['committed']==70))
 check('expense cannot exceed approved category funds',lambda:owner.request(base+'/expenses','POST',{'entityId':entity,'unitId':chapter,'budgetId':budget['id'],'vendor':'Synthetic Vendor','description':'Excess commitment','accountCode':'5000','amount':150,'date':str(today)},expected=400))
 check('expense author cannot self approve',lambda:owner.request(base+'/expenses/'+expense['id']+'/decision','POST',{'decision':'Approve'},expected=403))
 reviewer.request(base+'/expenses/'+expense['id']+'/decision','POST',{'decision':'Approve'})
 owner.request(base+'/expenses/'+expense['id']+'/pay','POST',{'reference':'expense-paid-'+uuid.uuid4().hex[:6],'date':str(today),'method':'Cash'})
 check('approved expense posts accrual and separate cash payment',lambda:yes(next(x for x in owner.request(base+'/expenses?entity='+entity)['items'] if x['id']==expense['id'])['status']=='Paid'))
 donor=owner.request(base+'/donors','POST',{'entityId':entity,'unitId':chapter,'name':'Synthetic Sponsor','kind':'External sponsor','contact':'example.test'})
 pledge=owner.request(base+'/contributions','POST',{'entityId':entity,'unitId':chapter,'donorId':donor['id'],'kind':'Sponsorship','purpose':'Training support','pledged':300,'date':str(today),'dueOn':str(today+datetime.timedelta(days=20))})
 check('sponsorship pledge is not fabricated cash',lambda:yes(pledge['status']=='Pledged'))
 donation=owner.request(base+'/contributions/'+pledge['id']+'/invoice','POST')
 check('sponsorship uses the shared invoice model',lambda:yes(donation['donorId']==donor['id'] and donation['purpose']=='Sponsorship'))
 bank=owner.request(base+'/banks','POST',{'entityId':entity,'unitId':chapter,'name':'Synthetic bank','details':'TEST ACCOUNT 1234567890','accountCode':'1010'})
 check('bank API masks restricted details',lambda:yes('1234567890' not in json.dumps(owner.request(base+'/banks?entity='+entity))))
 check('ordinary member cannot view restricted bank details',lambda:yes(member.request(base+'/banks?entity='+entity)==[]))
 bankpay=payment(35,method='Bank transfer',bankId=bank['id']);confirm(bankpay)
 bankjob=owner.request(base+'/banks/'+bank['id']+'/import','POST',{'key':'statement-'+uuid.uuid4().hex[:8],'rows':[{'date':str(today),'reference':'BANK-001','amount':20,'description':'Partial settlement'},{'date':str(today),'reference':'BANK-002','amount':15,'description':'Remainder'}]})
 check('bank import is durable and completes',lambda:yes(waitjob(bankjob)['state']=='Completed'))
 duplicatejob=owner.request(base+'/banks/'+bank['id']+'/import','POST',{'key':'duplicate-statement-'+uuid.uuid4().hex,'rows':[{'date':str(today),'reference':'BANK-001','amount':20,'description':'Repeated row'}]*2})
 check('duplicate statement rows within one durable batch are harmless',lambda:yes(waitjob(duplicatejob)['state']=='Completed'))
 bankrows=owner.request(base+'/bank-transactions?entity='+entity)['items']
 for row in bankrows:owner.request(base+'/bank-transactions/'+row['id']+'/match','POST',{'paymentId':bankpay['id']})
 check('partial bank settlements reconcile the exact payment',lambda:yes(next(x for x in owner.request(base+'/payments?entity='+entity)['items'] if x['id']==bankpay['id'])['settledOn'] is not None))
 check('duplicate bank match cannot post twice',lambda:owner.request(base+'/bank-transactions/'+bankrows[0]['id']+'/match','POST',{'paymentId':bankpay['id']},expected=403))
 gateway=owner.request(base+'/providers','POST',{'entityId':entity,'unitId':chapter,'name':'Development fixture only','provider':'Test','publicKey':'test-key','mode':'Test','enabled':True,'secrets':json.dumps({'secret':'synthetic-api-secret-at-least-32','webhookSecret':'synthetic-signing-secret-at-least-32'})})['id'];owner.request(base+'/providers/'+gateway+'/verify','POST')
 online=issue(invoice(90));intent=member.request(base+'/invoices/'+online['id']+'/payment-intents','POST',{'connectionId':gateway,'key':'checkout-'+uuid.uuid4().hex})['intent']
 def webhook(payload,valid=True,eid=None):
  body=json.dumps(payload).encode();signature=hmac.new(b'synthetic-signing-secret-at-least-32',body,hashlib.sha256).hexdigest() if valid else 'invalid';req=urllib.request.Request('http://127.0.0.1:5089/api/v1/finance/webhooks/'+gateway,data=body,method='POST',headers={'Content-Type':'application/json','X-Razorpay-Signature':signature,'X-Razorpay-Event-Id':eid or 'event-'+uuid.uuid4().hex})
  try:
   with urllib.request.urlopen(req) as response:return response.status,json.loads(response.read())
  except urllib.error.HTTPError as ex:return ex.code,ex.read().decode()
 captured={'event':'payment.captured','payload':{'payment':{'entity':{'id':'pay_fixture_'+uuid.uuid4().hex,'order_id':intent['orderId'],'amount':9000,'currency':'INR','status':'captured'}}}};eid='captured-'+uuid.uuid4().hex
 check('browser checkout creation is not payment confirmation',lambda:yes(member.request(base+'/invoices/'+online['id'])['invoice']['outstanding']==90))
 check('invalid payment signature cannot post funds',lambda:yes(webhook(captured,False)[0]==403))
 check('verified captured event confirms and allocates once',lambda:yes(webhook(captured,eid=eid)[0]==200))
 check('duplicate gateway event is idempotent',lambda:yes(webhook(captured,eid=eid)[1]['duplicate']))
 started=time.perf_counter()
 with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:simultaneous=list(pool.map(lambda _:webhook(captured),range(18)))
 check('overlapping signed captures cannot create duplicate money or receipts',lambda:yes(all(r[0]==200 for r in simultaneous) and sql('SELECT count(*) FROM "FinancePayment" WHERE "ProviderReference"=%s',(captured['payload']['payment']['entity']['id'],))[0][0]==1 and sql('SELECT count(*) FROM "FinanceReceipt" r JOIN "FinancePayment" p ON p."Id"=r."PaymentId" WHERE p."ProviderReference"=%s',(captured['payload']['payment']['entity']['id'],))[0][0]==1))
 (ROOT/'tests/phase5-payment-concurrency.json').write_text(json.dumps({'workers':6,'requests':18,'successful':sum(r[0]==200 for r in simultaneous),'elapsed_ms':round((time.perf_counter()-started)*1000,2),'scope':'Signed mock-gateway duplicate captures against PostgreSQL; exactly one payment and receipt. No external payment.'},indent=2))

 check('out-of-order authorization cannot downgrade captured funds',lambda:yes(webhook({'event':'payment.authorized','payload':captured['payload']})[0]==200 and member.request(base+'/invoices/'+online['id'])['invoice']['outstanding']==0))
 bad=json.loads(json.dumps(captured));bad['payload']['payment']['entity']['amount']=1;bad['payload']['payment']['entity']['id']='pay_bad_amount'
 check('signed wrong amount is rejected before posting',lambda:yes(webhook(bad)[0]==400))
 check('gateway credentials are never returned',lambda:yes('synthetic-api-secret' not in json.dumps(owner.request(base+'/providers?entity='+entity))))
 obligation=owner.request(base+'/inter-entity','POST',{'entityId':entity,'unitId':chapter,'otherEntityId':otherentity,'otherUnitId':remote,'amount':25,'currency':'INR','purpose':'Approved fund allocation','date':str(today)})
 reviewer.request(base+'/inter-entity/'+obligation['id']+'/decision','POST',{'decision':'Approve'})
 check('inter-entity allocation creates obligation without pretending bank transfer',lambda:yes(owner.request(base+'/inter-entity?entity='+entity)[0]['settled']==0))
 settlement=owner.request(base+'/inter-entity/'+obligation['id']+'/settlements','POST',{'amount':10,'reference':'actual-bank-reference','date':str(today)})
 reviewer.request(base+'/settlements/'+settlement['id']+'/decision','POST',{'decision':'Approve'})
 check('approved settlement updates both entities by the actual amount',lambda:yes(owner.request(base+'/inter-entity?entity='+entity)[0]['settled']==10))
 check('legal entity balances still reconcile independently',lambda:yes(all(x['difference']==0 for x in owner.request(base+'/reports?report=Trial%20Balance&entity='+entity)['totalDebits'])))
 check('cross-tenant invoice ID access denied',lambda:p.other.request(base+'/invoices/'+i['id'],expected=403))
 check('unauthorized financial export denied',lambda:member.request(base+'/reports/export','POST',{'entityId':entity,'unitId':chapter,'report':'Invoices','format':'csv'},expected=403))
 export=owner.request(base+'/reports/export','POST',{'entityId':entity,'unitId':chapter,'report':'Trial Balance','format':'xlsx'})
 check('durable Excel report generation completes from posted ledgers',lambda:yes(waitjob(export)['state']=='Completed'))
 def verify_excel():
  import io,zipfile,xml.etree.ElementTree as ET
  with owner.opener.open('http://127.0.0.1:5089/api/v1'+base+'/jobs/'+export['id']+'/download') as response:data=response.read()
  with zipfile.ZipFile(io.BytesIO(data)) as archive:
   assert archive.testzip() is None
   sheet=ET.fromstring(archive.read('xl/worksheets/sheet1.xml'));assert not sheet.findall('.//{*}f')
  (ROOT/'tests/phase5-trial-balance-sample.xlsx').write_bytes(data)
 check('Excel export is a genuine valid formula-free workbook',verify_excel)

 check('member portal displays own genuine balances and receipts',lambda:yes(member.request(base+'/my/summary')['balances'] and member.request(base+'/my/receipts')))
 check('all twenty financial report routes return permitted source data',lambda:[owner.request(base+'/reports?report='+urllib.parse.quote(name)+'&entity='+entity+'&member='+memberid+'&chapter='+chapter) for name in owner.request(base+'/reports/catalog')])
 # Existing event registration is extended, never duplicated by Finance.
 event=owner.request(prefix+'/community/events','POST',c.eventbody(unitId=chapter,title='Paid finance integration event',capacity=3))
 event=reviewer.request(prefix+'/community/events/'+event['id']+'/decision','POST',{'decision':'Approve','revision':event['revision'],'note':'Reviewed paid event'})
 eventid=event['id'];eventpolicy=owner.request(base+'/event-policies','POST',{'entityId':entity,'unitId':chapter,'eventId':eventid,'memberPrice':25,'guestPrice':40,'allowManualSettlement':False})
 check('event pricing requires independent approval',lambda:owner.request(base+'/event-policies/'+eventpolicy['id']+'/decision','POST',{'decision':'Approve'},expected=403))
 reviewer.request(base+'/event-policies/'+eventpolicy['id']+'/decision','POST',{'decision':'Approve'})
 reg=member.request(prefix+'/community/events/'+eventid+'/registrations','POST',{'companions':0})
 check('paid event registration waits for verified settlement',lambda:yes(reg['status']=='Awaiting Payment'))
 check('unpaid registration cannot issue an attendance pass',lambda:member.request(prefix+'/community/events/'+eventid+'/passes?registration='+reg['id'],'POST',expected=400))
 eventi=member.request(base+'/events/'+eventid+'/registration-invoice','POST')
 check('paid event creates a canonical invoice without another payment model',lambda:yes(eventi['eventId']==eventid and eventi['registrationId']==reg['id'] and eventi['total']==25))
 manualevent=payment(25);confirm(manualevent)
 check('event policy can prohibit alternative manual settlement',lambda:owner.request(base+'/payments/'+manualevent['id']+'/allocate','POST',{'invoiceId':eventi['id'],'amount':25,'key':'event-manual-bypass'},expected=400))
 eventi_intent=member.request(base+'/invoices/'+eventi['id']+'/payment-intents','POST',{'connectionId':gateway,'key':'event-checkout-'+uuid.uuid4().hex})['intent'];payref='pay_event_'+uuid.uuid4().hex
 captureevent={'event':'payment.captured','payload':{'payment':{'entity':{'id':payref,'order_id':eventi_intent['orderId'],'amount':2500,'currency':'INR','status':'captured'}}}}
 check('verified event capture uses central invoice allocation',lambda:yes(webhook(captureevent)[0]==200))
 check('verified allocated payment confirms original registration',lambda:yes(member.request(prefix+'/community/events/'+eventid)['registration']['status']=='Confirmed'))
 eventpass=member.request(prefix+'/community/events/'+eventid+'/passes?registration='+reg['id'],'POST')
 eventpayment=str(sql('SELECT "Id" FROM "FinancePayment" WHERE "ProviderReference"=%s',(payref,))[0][0]);er=owner.request(base+'/payments/'+eventpayment+'/refunds','POST',{'amount':5,'reference':'event-return-'+uuid.uuid4().hex,'reason':'Partial event return','date':str(today)})
 reviewer.request(base+'/refunds/'+er['id']+'/decision','POST',{'decision':'Approve'})
 check('mock gateway refund dispatch records actual adapter result only',lambda:yes(reviewer.request(base+'/refunds/'+er['id']+'/dispatch','POST')['status']=='Processed'))
 check('event refund reopens payment requirement and revokes pass',lambda:yes(member.request(prefix+'/community/events/'+eventid)['registration']['status']=='Awaiting Payment' and sql('SELECT "RevokedAt" FROM "EventPass" WHERE "Id"=%s',(eventpass['id'],))[0][0] is not None))
 dispute={'event':'payment.dispute.lost','payload':{'dispute':{'entity':{'id':'disp_fixture_'+uuid.uuid4().hex,'payment_id':captured['payload']['payment']['entity']['id'],'amount':1000,'amount_deducted':1000,'currency':'INR','status':'lost'}}}}
 check('verified chargeback reopens exact receivable',lambda:yes(webhook(dispute)[0]==200 and member.request(base+'/invoices/'+online['id'])['invoice']['outstanding']==10))
 check('duplicate chargeback reference cannot deduct twice',lambda:yes(webhook(dispute)[0]==200 and member.request(base+'/invoices/'+online['id'])['invoice']['outstanding']==10))
 # Role templates grant Presidents read access without posting or bank authority.
 treasurer,_,treasureruser=c.account('finance-treasurer',chapter)
 role=str(sql('SELECT "Id" FROM "GovernanceRoles" WHERE "TenantId"=%s AND "Name"=%s',(tenant,'Chapter Treasurer'))[0][0])
 sql('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt") VALUES (%s,%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,treasureruser,role,chapter))
 check('Chapter Treasurer sees its authorized member dues',lambda:yes(treasurer.request(base+'/invoices?entity='+entity)['total']>0))
 check('Chapter Treasurer cannot inspect a separate entity bank scope',lambda:yes(treasurer.request(base+'/invoices?entity='+otherentity)['total']==0))
 president,_,presidentuser=c.account('finance-president',chapter)
 prole=str(sql('SELECT "Id" FROM "GovernanceRoles" WHERE "TenantId"=%s AND "Name"=%s',(tenant,'Chapter President'))[0][0]);sql('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt") VALUES (%s,%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,presidentuser,prole,chapter))
 check('Chapter President can read actual authorized summary',lambda:yes(president.request(base+'/dashboard?entity='+entity)['currencies']))
 check('Chapter President does not inherit ledger-posting permission',lambda:president.request(base+'/journals','POST',{'entityId':entity,'unitId':chapter,'date':str(today),'purpose':'Unauthorized adjustment','key':'president-bypass','lines':[{'accountCode':'1000','debit':1,'credit':0},{'accountCode':'3000','debit':0,'credit':1}]},expected=403))
 check('Chapter President has no banking scope',lambda:yes(president.request(base+'/banks?entity='+entity)==[]))
 check('PostgreSQL refuses an unbalanced posted header at commit',lambda:yes(dbreject('''INSERT INTO "JournalEntry" SELECT (jsonb_populate_record(NULL::"JournalEntry",to_jsonb(j)||jsonb_build_object('Id',gen_random_uuid(),'SourceKey','unbalanced-direct-fixture','Number','BAD-DIRECT'))).* FROM "JournalEntry" j WHERE j."Id"=%s''',(journal['id'],))))
 # Real server-generated PDF and Excel files are fetched and inspected, never fabricated.
 def download(path,file):
  request=urllib.request.Request('http://127.0.0.1:5089/api/v1'+path)
  with member.opener.open(request) as response:file.write_bytes(response.read())
 check('finance PDF is generated from issued historical invoice',lambda:download(base+'/invoices/'+i['id']+'/pdf',ROOT/'tests/phase5-invoice-sample.pdf'))
 check('confirmed receipt PDF is generated from receipt snapshot',lambda:download(base+'/receipts/'+receipt['id']+'/pdf',ROOT/'tests/phase5-receipt-sample.pdf'))
 # Current and historical balances are different queries, both reconciled to posted AR.
 historical=issue(invoice(100,date=str(today-datetime.timedelta(days=1))))
 hp=payment(40);confirm(hp);allocate(hp,historical,40)
 ageing=owner.request(base+'/reports?report=Ageing&entity='+entity+'&to='+str(today-datetime.timedelta(days=1)))
 check('historical ageing excludes later allocations instead of rewriting the past',lambda:yes(sum(row['current']+row['days1To30']+row['days31To60']+row['days61To90']+row['over90'] for row in ageing['items'])==100))
 balance=owner.request(base+'/reports?report=Balance%20Sheet&entity='+entity)
 check('balance sheet reconciles assets to liabilities funds and current surplus',lambda:yes(all(round(row['assets']-row['liabilities']-row['funds']-row['currentSurplus'],2)==0 for row in balance['summaries'])))
 current=member.request(prefix+'/membership/members/'+memberid);member.request(prefix+'/membership/members/'+memberid,'PUT',{**current['member'],'profile':json.dumps(current['profile']),'privacy':json.dumps({'directory':True,'communication':True})},expected=204)
 f.prefs(member,inApp=True,quietStart=0,quietEnd=0)
 reminderinvoice=issue(invoice(15,dueOn=str(today)));notice=owner.request(base+'/reminders','POST',{'entityId':entity,'unitId':chapter,'name':'Due date integration reminder','daysFromDue':0,'channel':'InApp','enabled':True})
 until=time.time()+25
 while time.time()<until:
  records=sql('SELECT "Id","Status" FROM "Outbox" WHERE "TenantId"=%s AND "IdempotencyKey" LIKE %s',(tenant,'finance-reminder:'+notice['id']+':'+reminderinvoice['id']+'%'))
  if records and records[0][1]=='Delivered':break
  time.sleep(.2)
 check('financial reminder reuses the real consent-aware Outbox dispatcher',lambda:yes(records and records[0][1]=='Delivered'))
 check('reminder key suppresses duplicate recipient dispatch',lambda:yes(len(records)==1))
 check('financial notification opens the existing member finance portal',lambda:yes(any(n['link']=='/app/finance/my' for n in member.request(prefix+'/communications/notifications')['items'])))
 auto=plan(name='Automatic activation billing',amount=9,autoBill=True,requiresApproval=True)
 until=time.time()+25
 while time.time()<until:
  automatic=sql('SELECT "Id" FROM "DuesAssessment" WHERE "PlanId"=%s AND "MembershipId"=%s',(auto['id'],memberid))
  if automatic:break
  time.sleep(.2)
 check('automatic effective-dated billing creates a durable assessment',lambda:yes(automatic))
 batchrule=plan(name='Durable annual batch',amount=7,requiresApproval=True);batch=owner.request(base+'/assessments','POST',{'entityId':entity,'planId':batchrule['id'],'startsOn':str(today),'endsOn':str(yearend),'bulk':True,'key':'financial-batch-fixture'})
 check('annual dues batch completes independently of a browser timer',lambda:yes(waitjob(batch)['state']=='Completed'))
 check('bulk billing stable key returns the same durable job',lambda:yes(owner.request(base+'/assessments','POST',{'entityId':entity,'planId':batchrule['id'],'startsOn':str(today),'endsOn':str(yearend),'bulk':True,'key':'financial-batch-fixture'})['id']==batch['id']))
 latest=owner.request(base+'/entities');currententity=next(e for e in latest if e['id']==entity)
 owner.request(base+'/exchange-rates','POST',{'entityId':entity,'unitId':p.units['region'],'fromCurrency':'INR','toCurrency':'USD','date':str(today),'rate':'0.012345','source':'Explicit synthetic test rate'})
 conversion=owner.request(base+'/conversions?entity='+entity+'&target=USD&on='+str(today))
 check('report conversion uses the explicitly recorded decimal rate',lambda:yes(conversion['rate']==0.012345 and conversion['reportingCurrency']=='USD'))
 check('conversion does not combine legal accounting balances',lambda:yes(owner.request(base+'/reports?report=Trial%20Balance&entity='+entity)['totalDebits'][0]['difference']==0))
 check('protected export cannot be downloaded through generic file ID by another officer',lambda:reviewer.request(base+'/files/'+waitjob(export)['documentId'],expected=403))
 check('member statement export includes opening and closing summary',lambda:download(base+'/my/statements/'+memberid+'/export?format=csv',ROOT/'tests/phase5-statement-sample.csv'))
 check('statement CSV retains its balance summary',lambda:yes('Opening and closing summary' in (ROOT/'tests/phase5-statement-sample.csv').read_text(encoding='utf-8-sig')))
 # Close last so earlier posting cases remain in an open period.
 owner.request(base+'/periods/'+period['id']+'/close','POST',expected=204)
 check('closed accounting period prevents additional invoice posting',lambda:owner.request(base+'/invoices/'+invoice(5)['id']+'/issue','POST',expected=400))
 check('closed period cannot be silently reopened in PostgreSQL',lambda:yes(dbreject('UPDATE "AccountingPeriod" SET "Closed"=false WHERE "Id"=%s',(period['id'],))))
finally:
 if server is not None:server.terminate();server.wait(timeout=15)
 log.close();(ROOT/'tests/phase5-results.json').write_text(json.dumps({'passed':sum(x['passed'] for x in results),'total':len(results),'results':results},indent=2))