"""Phase 4 real PostgreSQL/HTTP tests; imports and runs all earlier phase suites."""
from api_runtime import API_DLL
import community_integration as c
import json,time,subprocess,datetime,uuid,hmac,hashlib,urllib.request,urllib.error,statistics
import psycopg
p=c.p;ROOT=p.ROOT;tenant=p.tenant;prefix='/societies/'+tenant;base=prefix+'/communications';owner=p.owner;reviewer=c.reviewer
env={**p.env,'Communications__AllowTestProvider':'true'};log=open(ROOT/'.local/phase4-api.log','w',encoding='utf-8');server=None;results=[]
def start():
 global server
 server=subprocess.Popen(['dotnet',str(API_DLL),'--urls','http://127.0.0.1:5089'],cwd=ROOT/'backend',env=env,stdout=log,stderr=log)
 for _ in range(80):
  try:owner.request('/auth/csrf');return
  except Exception:time.sleep(.15)
 raise RuntimeError('Test API did not start')
def check(name,fn):
 t=time.perf_counter()
 try:fn();results.append({'name':name,'passed':True,'ms':round((time.perf_counter()-t)*1000,2)});print('PASS PHASE4',name,flush=True)
 except Exception as e:results.append({'name':name,'passed':False,'error':str(e)});print('FAIL PHASE4',name,e,flush=True);raise
def yes(value):assert value
def sql(statement,args=()):
 with psycopg.connect(p.DSN) as db:
  result=db.execute(statement,args)
  return result.fetchall() if result.description else None
def prefs(client,**extra):
 v=client.request(base+'/preferences');client.request(base+'/preferences','PUT',{**v,'quietStart':0,'quietEnd':0,**extra},expected=204)
def waitjob(campaign,statuses=('Accepted','Delivered'),timeout=15):
 stop=time.time()+timeout
 while time.time()<stop:
  rows=owner.request(base+'/delivery?campaign='+campaign)['items']
  if rows and all(r['status'] in statuses for r in rows):return rows
  time.sleep(.15)
 raise AssertionError('Delivery did not reach '+str(statuses)+': '+str(rows))
def campaign(**extra):
 return owner.request(base+'/campaigns','POST',{'unitId':chapter,'name':'Integration campaign','channel':'InApp','body':'Hello {{member_name}} from {{chapter_name}}','subject':'Chapter notice','audience':{'match':'AND','filters':[{'key':'Chapter','values':[chapter]}]},'submit':True,**extra})
def account(channel='Email',mode='Success',**extra):
 v=owner.request(base+'/accounts','POST',{'unitId':chapter,'name':'Mock '+channel,'channel':channel,'provider':'Test','sender':'test@example.test' if channel=='Email' else '+15551234567','providerReference':str(uuid.uuid4()),'secrets':json.dumps({'mode':mode,'token':'test-token-never-live','webhookSecret':'test-webhook-secret-at-least-32bytes','templates':[{'name':'chapter_notice','language':'en','status':'APPROVED','components':[{'type':'BODY','text':'Hello {{1}}'}]}]}),'enabled':True,'inboundEnabled':True,'perMinute':100,**extra})
 owner.request(base+'/accounts/'+v['id']+'/verify','POST');return v['id']
def webhook(account,payload,valid=True):
 body=json.dumps(payload).encode();stamp=str(int(time.time()));signature=hmac.new(b'test-webhook-secret-at-least-32bytes',stamp.encode()+b'.'+body,hashlib.sha256).hexdigest() if valid else 'invalid'
 req=urllib.request.Request('http://127.0.0.1:5089/api/v1/communications/webhooks/'+account,method='POST',data=body,headers={'Content-Type':'application/json','X-Communication-Timestamp':stamp,'X-Communication-Signature':signature})
 try:
  with urllib.request.urlopen(req) as r:return r.status,json.loads(r.read())
 except urllib.error.HTTPError as e:return e.code,e.read().decode()
try:
 start();p.login(owner,p.accounts['owner'][0],'Changed-Password-2026!')
 chapter=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region'],'Communication chapter','C4-'+uuid.uuid4().hex[:6]))['id'];remote=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region2'],'Remote communication chapter','C4-'+uuid.uuid4().hex[:6]))['id']
 sender,email,uid=c.account('c4sender',chapter);peer,peeremail,peeruid=c.account('c4peer',chapter);outside,_,_=c.account('c4outside',remote)
 sid=sender.request('/membership/me')[0]['id'];pid=peer.request('/membership/me')[0]['id'];outsideid=outside.request('/membership/me')[0]['id'];prefs(sender,email=True,sms=True,whatsApp=True);prefs(peer,email=True,sms=True,whatsApp=True)
 # Phone is a contact field, never an identity key.
 for memberid in [sid,pid]:sql('UPDATE "Memberships" SET "Profile"="Profile" || %s::jsonb WHERE "Id"=%s',(json.dumps({'phone':'+1555'+str(uuid.uuid4().int%10000000).zfill(7)}),memberid))
 state={}
 check('same-chapter member starts a private conversation',lambda:state.update(dm=sender.request(base+'/conversations','POST',{'kind':'Direct','subject':'Private member conversation','membershipId':pid})))
 dm=state['dm']['id'];dp=base+'/conversations/'+dm
 check('direct participants can read conversation',lambda:yes(peer.request(dp)['conversation']['kind']=='Direct'))
 check('society administrator cannot read private member conversation',lambda:owner.request(dp,expected=403))
 check('regional scope cannot reveal private message history',lambda:c.regional.request(dp+'/messages',expected=403))
 check('unrelated chapter cannot create unrestricted direct message',lambda:outside.request(base+'/conversations','POST',{'kind':'Direct','subject':'Unauthorized','membershipId':pid},expected=403))
 msg=sender.request(dp+'/messages','POST',{'body':'A real private in-app message','clientKey':'one'})
 check('peer receives in-app message history',lambda:yes(peer.request(dp+'/messages')['items'][0]['id']==msg['id']))
 check('idempotent client key prevents duplicate message',lambda:yes(sender.request(dp+'/messages','POST',{'body':'same retry','clientKey':'one'})['id']==msg['id']))
 check('unread count reflects new peer message',lambda:yes(next(v for v in peer.request(base+'/inbox')['items'] if v['id']==dm)['unread']==1))
 check('read action clears unread count',lambda:peer.request(dp+'/read','POST',expected=204))
 check('unread count is cleared',lambda:yes(next(v for v in peer.request(base+'/inbox')['items'] if v['id']==dm)['unread']==0))
 check('authenticated reader produces a truthful message read receipt',lambda:yes(sender.request(dp+'/messages')['items'][0]['status']=='Read'))
 check('message search is confined to permitted conversation',lambda:yes(peer.request(dp+'/messages?search=private')['items'][0]['id']==msg['id']))
 check('conversation mute is stored per participant',lambda:peer.request(dp+'/mute','POST',expected=204))
 check('internal notes are forbidden in personal conversations',lambda:sender.request(dp+'/messages','POST',{'body':'secret note','clientKey':'internal','internal':True},expected=403))
 attachment=sender.upload(base+'/messages/'+msg['id']+'/attachments',p.png)
 check('message attachment is stored privately',lambda:yes(bool(attachment['id'])))
 check('unrelated member cannot retrieve private attachment',lambda:outside.request(base+'/attachments/'+attachment['id'],expected=403))
 check('invalid message upload rejected',lambda:sender.upload(base+'/messages/'+msg['id']+'/attachments',b'<html>',expected=400))
 check('message reporting persists an audit entry',lambda:peer.request(base+'/messages/'+msg['id']+'/report','POST',{'reason':'Test report'},expected=204))
 check('member block uses existing community policy',lambda:peer.request(prefix+'/community/blocks/'+uid,'PUT',expected=204))
 check('blocked sender cannot use direct history endpoint',lambda:sender.request(dp+'/messages',expected=403))
 check('blocked sender cannot bypass via message endpoint',lambda:sender.request(dp+'/messages','POST',{'body':'bypass','clientKey':'blocked'},expected=403))
 check('blocked sender cannot bypass via duplicate thread creation',lambda:sender.request(base+'/conversations','POST',{'kind':'Direct','subject':'bypass','membershipId':pid},expected=403))
 peer.request(prefix+'/community/blocks/'+uid,'DELETE',expected=204)
 prefs(peer,directMessages=False)
 check('direct-message preference prevents new sends',lambda:sender.request(dp+'/messages','POST',{'body':'optout','clientKey':'pref-block'},expected=403))
 prefs(peer,directMessages=True)
 group=owner.request(base+'/conversations','POST',{'kind':'Group','subject':'Chapter officers group','unitId':chapter,'memberIds':[sid,pid],'posting':'Admins'})['id'];gp=base+'/conversations/'+group
 check('authorized officer creates chapter group',lambda:yes(sender.request(gp)['conversation']['kind']=='Group'))
 check('group posting policy blocks ordinary members',lambda:sender.request(gp+'/messages','POST',{'body':'not allowed','clientKey':'group-no'},expected=403))
 check('group administrator can post',lambda:owner.request(gp+'/messages','POST',{'body':'Official group update','clientKey':'group-yes'}))
 check('unrelated chapter cannot read chapter group',lambda:outside.request(gp,expected=403))
 check('group removal retains join/leave history',lambda:owner.request(base+'/groups/'+group+'/members/'+peeruid,'DELETE',expected=204))
 check('removed participant loses group access',lambda:peer.request(gp,expected=403))
 check('authorized group member rejoin',lambda:owner.request(base+'/groups/'+group+'/members','POST',{'membershipId':pid,'role':'Member','responsibilities':'Participant','startsAt':datetime.datetime.now(datetime.timezone.utc).isoformat(),'endsAt':None},expected=204))
 support=sender.request(base+'/conversations','POST',{'kind':'Team','subject':'Membership assistance','unitId':chapter})['id'];sp=base+'/conversations/'+support
 check('member explicitly routes inquiry to shared team',lambda:yes(owner.request(sp)['canInternal']))
 note=owner.request(sp+'/messages','POST',{'body':'Private staff assessment','clientKey':'team-note','internal':True})
 check('internal note remains hidden from requesting member',lambda:yes(not any(m['id']==note['id'] for m in sender.request(sp+'/messages')['items'])))
 check('outside member cannot read team inquiry',lambda:outside.request(sp,expected=403))
 supportdata=owner.request(sp)['conversation'];owner.request(sp,'PUT',{'status':'Pending','priority':'Important','assignedTo':c.revieweruser,'followUpAt':None,'tags':['Membership'],'resolution':'','revision':supportdata['revision']},expected=204)
 check('team assignment is visible to authorized assignee',lambda:yes(any(v['id']==support for v in reviewer.request(base+'/inbox?filter=Assigned')['items'])))
 check('unauthorized conversation assignment denied',lambda:sender.request(sp,'PUT',{'status':'Resolved','priority':'Normal','assignedTo':uid,'followUpAt':None,'tags':[],'resolution':'bypass','revision':1},expected=403))
 q=owner.request(base+'/setup')['policy'];owner.request(base+'/policy','PUT',{**q,'approvalStages':'[]'},expected=204)
 preview=owner.request(base+'/audiences/preview?channel=InApp&unit='+chapter,'POST',{'match':'AND','filters':[{'key':'Chapter','values':[chapter]}]})
 check('audience resolves real eligible recipients',lambda:yes(preview['eligible']==2 and preview['duplicates']==0))
 regioncomm=owner.request(prefix+'/roles','POST',{'name':'Scoped regional communicator '+uuid.uuid4().hex[:5],'permissions':['communications.send','communications.inbox','communications.analytics'],'responsibilities':'Regional communication test'})
 sql('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt") VALUES (%s,%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,c.regionaluser,regioncomm['id'],p.units['region']))
 check('regional sender resolves assigned chapter audience',lambda:yes(c.regional.request(base+'/audiences/preview?channel=InApp&unit='+chapter,'POST',{'match':'AND','filters':[{'key':'Chapter','values':[chapter]}]})['eligible']==2))
 check('regional sender lists shared team inbox in its branch',lambda:yes(any(v['id']==support for v in c.regional.request(base+'/inbox')['items'])))
 check('regional communication permission still cannot reveal private DM',lambda:c.regional.request(dp+'/messages',expected=403))
 check('unauthorized audience unit is denied',lambda:c.regional.request(base+'/audiences/preview?channel=InApp&unit='+chapter,'POST',{'match':'AND','filters':[{'key':'Chapter','values':[remote]}]},expected=403))
 check('cross-society audience lookup is denied',lambda:p.other.request(base+'/audiences/preview?channel=InApp','POST',{'match':'AND','filters':[]},expected=403))
 template=owner.request(base+'/templates','POST',{'unitId':chapter,'name':'Personal notice '+uuid.uuid4().hex[:5],'channel':'InApp','body':'Hello {{member_name}} in {{chapter_name}}','subject':'Welcome'})
 check('typed template variables render authorized sample',lambda:yes('Synthetic c4peer' in owner.request(base+'/templates/'+template['id']+'/preview?membershipId='+pid,'POST')['body']))
 check('unknown template variables are rejected',lambda:owner.request(base+'/templates','POST',{'unitId':chapter,'name':'Unsafe','channel':'InApp','body':'{{arbitrary_sql}}'},expected=400))
 check('script content is rejected from templates',lambda:owner.request(base+'/templates','POST',{'unitId':chapter,'name':'Unsafe script','channel':'Email','body':'<script>alert(1)</script>'},expected=400))
 notice=campaign(kind='Announcement',acknowledge=True,pinned=True)
 jobs=waitjob(notice['id'],('Delivered',))
 check('announcement snapshots and queues two distinct recipients',lambda:yes(len(jobs)==2))
 check('in-app announcement creates real notification',lambda:yes(any(n['title']=='Chapter notice' for n in peer.request(base+'/notifications')['items'])))
 check('targeted announcement is visible to recipient',lambda:yes(any(a['id']==notice['id'] for a in peer.request(base+'/announcements'))))
 check('announcement excluded from unrelated chapter',lambda:yes(not any(a['id']==notice['id'] for a in outside.request(base+'/announcements'))))
 check('notification detail renders personalized content for recipient',lambda:yes('Synthetic c4peer' in peer.request(base+'/received/'+notice['id'])['body']))
 check('notification detail cannot be opened by unrelated member',lambda:outside.request(base+'/received/'+notice['id'],expected=403))
 check('in-app read receipt updates durable delivery row',lambda:yes(any(j['readAt'] for j in owner.request(base+'/delivery?campaign='+notice['id'])['items'])))
 check('announcement acknowledgment is audited',lambda:peer.request(base+'/announcements/'+notice['id']+'/acknowledge','POST',expected=204))
 check('notification read state can be changed',lambda:peer.request(base+'/notifications/read','POST',expected=204))
 check('notification counter is genuine and clears',lambda:yes(peer.request(base+'/notifications')['unread']==0))
 q=owner.request(base+'/setup')['policy'];owner.request(base+'/policy','PUT',{**q,'approvalStages':'["communications.approve"]'},expected=204)
 draft=campaign(name='Needs approval')
 check('campaign requires configured approval',lambda:yes(draft['status']=='Pending Approval'))
 check('campaign author cannot self approve',lambda:owner.request(base+'/campaigns/'+draft['id']+'/decision','POST',{'decision':'Approve','note':'self','revision':0},expected=403))
 approved=reviewer.request(base+'/campaigns/'+draft['id']+'/decision','POST',{'decision':'Approve','note':'Reviewed','revision':0})
 check('independent review schedules approved campaign',lambda:yes(approved['status']=='Scheduled'))
 waitjob(draft['id'])
 future=campaign(name='Future edited campaign',scheduledAt=(datetime.datetime.now(datetime.timezone.utc)+datetime.timedelta(days=1)).isoformat())
 future=reviewer.request(base+'/campaigns/'+future['id']+'/decision','POST',{'decision':'Approve','note':'Approved future','revision':0})
 update={'unitId':chapter,'name':'Material edit','channel':'InApp','body':'Changed content','subject':'Changed notice','audience':{'match':'AND','filters':[{'key':'Chapter','values':[chapter]}]},'scheduledAt':future['scheduledAt'],'submit':True,'revision':future['revision']}
 edited=owner.request(base+'/campaigns/'+future['id'],'PUT',update)
 check('material edits invalidate prior approval and preserve snapshot versions',lambda:yes(edited['status']=='Pending Approval' and edited['snapshotVersion']==1))
 check('campaign cancellation prevents unsent jobs',lambda:owner.request(base+'/campaigns/'+future['id']+'/cancel','POST',expected=204))
 check('campaign duplication creates an unsent draft',lambda:yes(owner.request(base+'/campaigns/'+future['id']+'/duplicate','POST')['status']=='Draft'))
 q=owner.request(base+'/setup')['policy'];owner.request(base+'/policy','PUT',{**q,'approvalStages':'[]'},expected=204)
 emailaccount=account();ec=campaign(channel='Email',accountId=emailaccount,name='Mock email campaign')
 ej=waitjob(ec['id'])
 check('email adapter records provider acceptance without claiming delivery',lambda:yes(all(j['status']=='Accepted' and j['deliveredAt'] is None for j in ej)))
 check('account API never returns provider credentials',lambda:yes('test-token-never-live' not in json.dumps(owner.request(base+'/accounts')) and 'protectedSecrets' not in json.dumps(owner.request(base+'/accounts'))))
 check('ordinary members cannot administer sender accounts',lambda:peer.request(base+'/accounts/'+emailaccount+'/verify','POST',expected=403))
 event={'id':'event-'+uuid.uuid4().hex,'providerId':ej[0]['providerId'],'status':'Delivered'}
 check('unauthenticated webhook signature is rejected',lambda:yes(webhook(emailaccount,event,False)[0]==403))
 check('verified webhook records actual delivery',lambda:yes(webhook(emailaccount,event)[1]['processed']==1))
 check('webhook duplicates are suppressed',lambda:yes(webhook(emailaccount,event)[1]['processed']==0))
 check('verified provider delivery updates stored status',lambda:yes(any(j['status']=='Delivered' for j in owner.request(base+'/delivery?campaign='+ec['id'])['items'])))
 bounce={'id':'bounce-'+uuid.uuid4().hex,'providerId':ej[0]['providerId'],'status':'Bounced'};webhook(emailaccount,bounce)
 check('email bounce creates a channel suppression',lambda:yes(sql('SELECT count(*) FROM "CommunicationSuppression" WHERE "TenantId"=%s AND "Reason"=\'Bounced\'',(tenant,))[0][0]>=1))
 target=pid if str(sql('SELECT "MembershipId" FROM "Outbox" WHERE "Id"=%s',(ej[0]['id'],))[0][0])!=pid else sid
 ext=owner.request(base+'/conversations','POST',{'kind':'Team','subject':'Held attachment integration','unitId':chapter,'channel':'Email','accountId':emailaccount,'membershipId':target})['id']
 held=owner.request(base+'/conversations/'+ext+'/messages','POST',{'body':'Attachment transport test','clientKey':'held-file','hold':True})
 check('external attachment draft is held before dispatch',lambda:yes(sql('SELECT "Status" FROM "Outbox" WHERE "MessageId"=%s',(held['id'],))[0][0]=='Held'))
 check('held external draft accepts a private sanitized attachment',lambda:yes(bool(owner.upload(base+'/messages/'+held['id']+'/attachments',p.png)['id'])))
 check('author finalizes held delivery after attachment upload',lambda:yes(owner.request(base+'/messages/'+held['id']+'/send','POST')['status']=='Queued'))
 check('finalize retry does not duplicate delivery job',lambda:yes(owner.request(base+'/messages/'+held['id']+'/send','POST')['id']==held['id'] and sql('SELECT count(*) FROM "Outbox" WHERE "MessageId"=%s',(held['id'],))[0][0]==1))
 failed=account('SMS','Failure');fc=campaign(channel='SMS',accountId=failed,name='Mock SMS failure')
 fj=waitjob(fc['id'],('Failed',))
 check('SMS provider failure is recorded accurately',lambda:yes(all(j['status']=='Failed' for j in fj)))
 check('failed dispatch exposes safe operational error only',lambda:yes(all('test-token' not in (j['error'] or '') for j in fj)))
 wa=account('WhatsApp');check('WhatsApp approved templates synchronize from provider adapter',lambda:yes(owner.request(base+'/accounts/'+wa+'/templates/sync','POST')['count']==1))
 wt=next(t for t in owner.request(base+'/templates') if t['providerName']=='chapter_notice');wc=campaign(channel='WhatsApp',accountId=wa,templateId=wt['id'],name='WhatsApp approved template')
 check('WhatsApp template dispatch uses verified approved binding',lambda:yes(all(j['status']=='Accepted' for j in waitjob(wc['id']))))
 prefs(peer,email=False)
 # Exercise each production signature verifier using synthetic signed status events, without provider calls.
 import base64,urllib.parse
 def rawcallback(account,payload,headers):
  req=urllib.request.Request('http://127.0.0.1:5089/api/v1/communications/webhooks/'+account,method='POST',data=payload,headers=headers)
  try:
   with urllib.request.urlopen(req) as response:return response.status,json.loads(response.read())
  except urllib.error.HTTPError as error:return error.code,None
 def signedaccount(provider,channel,secrets):
  return owner.request(base+'/accounts','POST',{'unitId':chapter,'name':'Verifier fixture '+provider,'channel':channel,'provider':provider,'sender':'test@example.test' if channel=='Email' else '+15551234567','providerReference':'fixture-reference','secrets':json.dumps(secrets),'enabled':False,'inboundEnabled':False,'perMinute':10})['id']
 secret='fixture-provider-signing-secret-32bytes'
 ma=signedaccount('Meta','WhatsApp',{'appSecret':secret,'verifyToken':secret,'apiVersion':'v99.0','token':'never-live'})
 payload=json.dumps({'entry':[{'changes':[{'value':{'metadata':{'phone_number_id':'fixture-reference'},'statuses':[{'id':'meta-fixture','status':'delivered','timestamp':'1'}]}}]}]}).encode()
 check('Meta rejects an invalid HMAC signature',lambda:yes(rawcallback(ma,payload,{'Content-Type':'application/json','X-Hub-Signature-256':'sha256=invalid'})[0]==403))
 check('Meta accepts a valid account-bound HMAC signature',lambda:yes(rawcallback(ma,payload,{'Content-Type':'application/json','X-Hub-Signature-256':'sha256='+hmac.new(secret.encode(),payload,hashlib.sha256).hexdigest()})[1]['processed']==1))
 ta=signedaccount('Twilio','SMS',{'token':secret});fields={'AccountSid':'fixture-reference','MessageSid':'twilio-fixture','MessageStatus':'delivered'};payload=urllib.parse.urlencode(fields).encode();url='http://127.0.0.1:5089/api/v1/communications/webhooks/'+ta;signature=base64.b64encode(hmac.new(secret.encode(),(url+''.join(k+v for k,v in sorted(fields.items()))).encode(),hashlib.sha1).digest()).decode()
 check('Twilio rejects unsigned callbacks',lambda:yes(rawcallback(ta,payload,{'Content-Type':'application/x-www-form-urlencoded','X-Twilio-Signature':'invalid'})[0]==403))
 check('Twilio accepts canonical URL and parameter signature',lambda:yes(rawcallback(ta,payload,{'Content-Type':'application/x-www-form-urlencoded','X-Twilio-Signature':signature})[1]['processed']==1))
 ra=signedaccount('Resend','Email',{'webhookSecret':'whsec_'+base64.b64encode(secret.encode()).decode(),'token':'never-live'});payload=json.dumps({'type':'email.delivered','data':{'email_id':'resend-fixture'}}).encode();ts=str(int(time.time()));eid='svix-fixture';signature=base64.b64encode(hmac.new(secret.encode(),eid.encode()+b'.'+ts.encode()+b'.'+payload,hashlib.sha256).digest()).decode()
 check('Resend rejects an invalid Svix signature',lambda:yes(rawcallback(ra,payload,{'Content-Type':'application/json','svix-id':eid,'svix-timestamp':ts,'svix-signature':'v1,invalid'})[0]==403))
 check('Resend accepts a timestamped Svix signature',lambda:yes(rawcallback(ra,payload,{'Content-Type':'application/json','svix-id':eid,'svix-timestamp':ts,'svix-signature':'v1,'+signature})[1]['processed']==1))
 check('channel opt-out is excluded by audience builder',lambda:yes(owner.request(base+'/audiences/preview?channel=Email&unit='+chapter,'POST',{'match':'AND','filters':[]})['optedOut']>=1))
 prefs(peer,email=True)
 # Retry / quiet / lease recovery are manipulated only in the isolated test database.
 retryaccount=account('SMS','Transient');retrycampaign=campaign(channel='SMS',accountId=retryaccount,name='Retry behavior')
 retryjobs=waitjob(retrycampaign['id'],('Retry',))
 check('transient provider failure schedules a durable retry',lambda:yes(all(j['attempts']>=1 and j['nextAttemptAt'] for j in retryjobs)))
 sql('UPDATE "Outbox" SET "Attempts"=4,"NextAttemptAt"=now() WHERE "CampaignId"=%s',(retrycampaign['id'],))
 check('bounded retries become a visible dead letter',lambda:yes(all(j['status']=='DeadLetter' for j in waitjob(retrycampaign['id'],('DeadLetter',)))))
 now=datetime.datetime.now(datetime.timezone.utc);prefs(peer,timeZone='UTC',quietStart=now.hour,quietEnd=(now.hour+1)%24)
 quietcampaign=campaign(name='Quiet hour policy')
 stop=time.time()+10
 while time.time()<stop:
  quietjobs=owner.request(base+'/delivery?campaign='+quietcampaign['id'])['items']
  if len(quietjobs)==2 and any(j['nextAttemptAt']>now.isoformat() for j in quietjobs):break
  time.sleep(.1)
 check('quiet hours defer recipient without claiming delivery',lambda:yes(any(j['status']=='Queued' and j['deliveredAt'] is None for j in quietjobs)))
 prefs(peer,timeZone='Asia/Kolkata',quietStart=0,quietEnd=0)
 sql('UPDATE "Outbox" SET "NextAttemptAt"=now() WHERE "CampaignId"=%s',(quietcampaign['id'],));waitjob(quietcampaign['id'],('Delivered',))
 # An expired external processing lease has an uncertain transport outcome, never an automatic re-send.
 leasecampaign=campaign(channel='SMS',accountId=failed,name='Uncertain transport',scheduledAt=(now+datetime.timedelta(days=1)).isoformat())
 sql('UPDATE "CommunicationCampaign" SET "ScheduledAt"=now() WHERE "Id"=%s',(leasecampaign['id'],));leasejobs=waitjob(leasecampaign['id'],('Failed',))
 sql('''UPDATE "Outbox" SET "Status"='Processing',"SentAt"=NULL,"LeaseUntil"=now()-interval '1 minute' WHERE "Id"=%s''',(leasejobs[0]['id'],));time.sleep(.6)
 check('expired external lease requires reconciliation',lambda:yes(owner.request(base+'/delivery?campaign='+leasecampaign['id'])['items'][0]['status'] in ['Unknown','Failed'] and sql('SELECT "Status" FROM "Outbox" WHERE "Id"=%s',(leasejobs[0]['id'],))[0][0]=='Unknown'))
 later=campaign(name='Durable future in-app',scheduledAt=(datetime.datetime.now(datetime.timezone.utc)+datetime.timedelta(hours=1)).isoformat())
 check('scheduled campaign persists without browser timer',lambda:yes(sql('SELECT "Status" FROM "CommunicationCampaign" WHERE "Id"=%s',(later['id'],))[0][0]=='Scheduled'))
 server.terminate();server.wait(timeout=15);server=None
 sql('UPDATE "CommunicationCampaign" SET "ScheduledAt"=now()-interval \'1 minute\' WHERE "Id"=%s',(later['id'],));start()
 check('worker restart recovers durable scheduled campaign',lambda:yes(len(waitjob(later['id'],('Delivered',)))==2))
 automation=owner.request(base+'/automations','POST',{'unitId':chapter,'templateId':template['id'],'name':'Test recurring rule','trigger':'Recurring','channel':'InApp','audience':{'match':'AND','filters':[]},'repeatMinutes':60,'enabled':True,'testMode':True})
 stop=time.time()+12
 while time.time()<stop:
  executions=owner.request(base+'/automations/'+automation['id']+'/executions')
  if executions:break
  time.sleep(.2)
 check('durable automation executes in safe test mode',lambda:yes(executions[0]['status']=='Tested'))
 sql('UPDATE "CommunicationAutomation" SET "LastRun"=NULL WHERE "Id"=%s',(automation['id'],));time.sleep(.7)
 check('automation source key suppresses duplicate execution',lambda:yes(len(owner.request(base+'/automations/'+automation['id']+'/executions'))==1))
 check('automation loops and overly short recurrence rejected',lambda:owner.request(base+'/automations','POST',{'unitId':chapter,'templateId':template['id'],'name':'Too fast','trigger':'Recurring','channel':'InApp','audience':{'match':'AND','filters':[]},'repeatMinutes':1,'enabled':True},expected=400))
 check('communication analytics count real deliveries and failures',lambda:yes(owner.request(base+'/analytics')['attempted']>0 and owner.request(base+'/analytics')['failed']>=2))
 check('unsupported provider cost metric remains unavailable',lambda:yes(owner.request(base+'/analytics')['cost'] is None))
 check('cross-society inbox access denied',lambda:p.other.request(base+'/inbox',expected=403))
 check('cross-chapter analytics denied',lambda:outside.request(base+'/analytics',expected=403))
 check('delivery queue cannot be inspected by ordinary member',lambda:yes(outside.request(base+'/delivery')['total']==0))
 samples=[]
 for _ in range(20):
  t=time.perf_counter();peer.request(base+'/inbox');samples.append((time.perf_counter()-t)*1000)
 perf={'scope':'20 authenticated warm inbox reads on integration fixtures','median_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[18],3),'target_p95_ms':500,'target_met':sorted(samples)[18]<500};(ROOT/'tests/phase4-http-performance.json').write_text(json.dumps(perf,indent=2))
 check('authenticated inbox meets documented local target',lambda:yes(perf['target_met']))
finally:
 if server is not None:server.terminate();server.wait(timeout=15)
 log.close();(ROOT/'tests/phase4-results.json').write_text(json.dumps({'passed':sum(r['passed'] for r in results),'total':len(results),'results':results},indent=2))
