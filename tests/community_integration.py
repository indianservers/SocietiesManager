"""Phase 3 real PostgreSQL/HTTP suite, including Phases 1–2 regression."""
import membership_integration as m
import json,time,subprocess,datetime,uuid,concurrent.futures,urllib.request,urllib.error,statistics
import psycopg
p=m.p;ROOT=p.ROOT
log=open(ROOT/'.local/phase3-api.log','w',encoding='utf-8')
server=subprocess.Popen(['dotnet',str(ROOT/'backend/bin/Debug/net10.0/Society.Api.dll'),'--urls','http://127.0.0.1:5089'],cwd=ROOT/'backend',env=p.env,stdout=log,stderr=log)
results=[];owner=p.owner;other=p.other;tenant=p.tenant;prefix='/societies/'+tenant;base=prefix+'/community'
now=datetime.datetime.now(datetime.timezone.utc)
def iso(hours):return (now+datetime.timedelta(hours=hours)).isoformat()
def check(name,fn):
 t=time.perf_counter()
 try:fn();results.append({'name':name,'passed':True,'ms':round((time.perf_counter()-t)*1000,2)});print('PASS PHASE3',name,flush=True)
 except Exception as e:results.append({'name':name,'passed':False,'error':str(e)});print('FAIL PHASE3',name,e,flush=True);raise
def yes(v):assert v
def account(name,chapter=None,admin=False):
 c=p.Client();user=str(uuid.uuid4());email='p3-'+name+'-'+uuid.uuid4().hex[:8]+'@example.test'
 with psycopg.connect(p.DSN) as db:
  ownerhash=db.execute('SELECT "PasswordHash" FROM "AspNetUsers" WHERE "Id"=%s',(p.accounts['owner'][1],)).fetchone()[0]
  db.execute('INSERT INTO "AspNetUsers" ("Id","FullName","PlatformAdmin","UserName","NormalizedUserName","Email","NormalizedEmail","EmailConfirmed","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount","PasswordHash","SecurityStamp","ConcurrencyStamp") VALUES (%s,%s,false,%s,%s,%s,%s,true,false,false,true,0,%s,%s,%s)',(user,'Synthetic '+name,email,email.upper(),email,email.upper(),ownerhash,uuid.uuid4().hex,uuid.uuid4().hex))
 p.login(c,email,'Changed-Password-2026!')
 if chapter:
  app=c.request('/membership/applications','POST',{'tenantId':tenant,'chapterId':chapter,'categoryId':category,'profile':'{}','submit':True})
  owner.request(prefix+'/membership/applications/'+app['id']+'/decision','POST',{'decision':'Approve','comment':'Phase 3 fixture','revision':0})
 if chapter:
  own=c.request('/membership/me')[0];full=c.request(prefix+'/membership/members/'+own['id']);mbr=full['member']
  c.request(prefix+'/membership/members/'+mbr['id'],'PUT',{'fullName':mbr['fullName'],'chapterId':mbr['chapterId'],'categoryId':mbr['categoryId'],'number':mbr['number'],'status':mbr['status'],'profile':json.dumps(full['profile']),'privacy':json.dumps({**full['privacy'],'communication':True}),'revision':mbr['revision']},expected=204)
 if admin:
  with psycopg.connect(p.DSN) as db:
   db.execute('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s) ON CONFLICT DO NOTHING',(tenant,user))
   role=db.execute('SELECT "Id" FROM "GovernanceRoles" WHERE "TenantId"=%s AND "Name"=\'Society Administrator\'',(tenant,)).fetchone()[0]
   db.execute('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","StartsAt") VALUES (%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,user,role))
 return c,email,user
def postbody(**kwargs):return {'unitId':chapter,'categoryId':activity,'body':'<p>Neighbors making a difference.</p>','title':'Service chapter story','submit':True,**kwargs}
def eventbody(**kwargs):return {'unitId':chapter,'categoryId':eventtype,'title':'Chapter service gathering','body':'A real program','startsAt':iso(1),'endsAt':iso(3),'registrationOpens':iso(-1),'registrationCloses':iso(2),'capacity':2,'submit':True,**kwargs}
def draft(client,**kw):return client.request(base+'/posts','POST',postbody(**kw))
def create_event(**kw):return owner.request(base+'/events','POST',eventbody(**kw))
def decision(client,kind,resource,decision='Approve',**kw):return client.request(base+'/'+kind+'/'+resource['id']+'/decision','POST',{'decision':decision,'note':'Reviewed test evidence','revision':resource['revision'],**kw})
try:
 for _ in range(60):
  try:owner.request('/auth/csrf');break
  except Exception:time.sleep(.2)
 p.login(owner,p.accounts['owner'][0],'Changed-Password-2026!')
 chapter=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region'],'Community Chapter','P3-'+uuid.uuid4().hex[:6]))['id']
 remote=owner.request(prefix+'/organizations/units','POST',p.unit(p.chapter,p.units['region2'],'Remote Community Chapter','P3-'+uuid.uuid4().hex[:6]))['id']
 category=next(c['id'] for c in owner.request(prefix+'/membership/categories') if c['name']=='Active Member')
 membershippolicy=owner.request(prefix+'/membership/policy');owner.request(prefix+'/membership/policy','PUT',{**membershippolicy,'approvalStages':json.dumps([{'name':'Chapter review','permission':'members.approve'}])},expected=204)
 member,email,user=account('member',chapter);peer,peeremail,peeruser=account('peer',chapter);remoteclient,_,_=account('remote',remote);reviewer,_,revieweruser=account('reviewer',admin=True)
 setup=owner.request(base+'/setup');activity=next(c['id'] for c in setup['categories'] if c['kind']=='Activity');eventtype=next(c['id'] for c in setup['categories'] if c['kind']=='Event')
 state={}
 check('member creates permitted chapter draft',lambda:state.update(draft=draft(member,submit=False)))
 check('draft is excluded from peer feed',lambda:yes(all(x['id']!=state['draft']['id'] for x in peer.request(base+'/feed')['items'])))
 check('draft direct URL remains private',lambda:peer.request(base+'/posts/'+state['draft']['id'],expected=403))
 check('member cannot impersonate chapter organization',lambda:member.request(base+'/posts','POST',postbody(official=True),expected=403))
 check('member cannot post into unrelated chapter',lambda:member.request(base+'/posts','POST',postbody(unitId=remote),expected=403))
 check('member cannot expand audience to society',lambda:member.request(base+'/posts','POST',postbody(visibility='Society'),expected=403))
 check('member submits content for approval',lambda:state.update(p=draft(member)))
 check('author cannot approve own post',lambda:member.request(base+'/posts/'+state['p']['id']+'/decision','POST',{'decision':'Approve','note':'self','revision':0},expected=403))
 check('authorized post approval publishes content',lambda:state.update(p=decision(owner,'posts',state['p'])))
 check('chapter peer sees approved feed post',lambda:yes(any(x['id']==state['p']['id'] for x in peer.request(base+'/feed')['items'])))
 check('private post denied to unrelated chapter',lambda:remoteclient.request(base+'/posts/'+state['p']['id'],expected=403))
 check('cross-society feed denied',lambda:other.request(base+'/feed',expected=403))
 check('cross-society post URL denied',lambda:other.request(base+'/posts/'+state['p']['id'],expected=403))
 official=draft(owner,official=True,title='Official chapter news')
 check('official publishing preserves actual author and organization',lambda:yes(official['authorId']==p.accounts['owner'][1] and official['official'] and official['unitId']==chapter))
 check('official post requires independent approval',lambda:state.update(official=decision(reviewer,'posts',official)))
 regionrole=owner.request(prefix+'/roles','POST',{'name':'P3 regional reader','permissions':['units.read','activities.read','activities.analytics'],'responsibilities':'Scoped fixture'})
 regional,_,regionaluser=account('regional',admin=False)
 with psycopg.connect(p.DSN) as db:
  db.execute('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s)',(tenant,regionaluser))
  db.execute('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt") VALUES (%s,%s,%s,%s,%s,now())',(uuid.uuid4(),tenant,regionaluser,regionrole['id'],p.units['region']))
 check('regional feed aggregates subordinate chapter activity',lambda:yes(any(x['id']==state['p']['id'] for x in regional.request(base+'/feed')['items'])))
 check('society feed aggregates official and member activity',lambda:yes(len(owner.request(base+'/feed')['items'])>=2))
 unsafe=draft(member,body='<script>alert(1)</script><img src=x onerror=alert(1)><strong onclick="x">Unsafe</strong>')['body']
 check('rich text blocks executable HTML and attributes',lambda:yes('<script' not in unsafe and '<img' not in unsafe and '<strong onclick' not in unsafe and '&lt;script' in unsafe))
 edited=member.request(base+'/posts/'+state['p']['id'],'PUT',postbody(title='Revised story',revision=state['p']['revision']))
 check('material edit restarts approval',lambda:yes(edited['status']=='Pending Approval' and not any(x['id']==edited['id'] for x in peer.request(base+'/feed')['items'])))
 check('post versions retain immutable snapshots',lambda:yes(len(member.request(base+'/posts/'+edited['id']+'/history')['versions'])==2))
 state['p']=decision(owner,'posts',edited)
 check('stale edits rejected',lambda:member.request(base+'/posts/'+edited['id'],'PUT',postbody(revision=0),expected=409))
 rejected=draft(member)
 check('post rejection retained',lambda:yes(decision(owner,'posts',rejected,'Reject')['status']=='Rejected'))
 scheduled=draft(member,scheduledAt=iso(2))
 scheduled=decision(owner,'posts',scheduled)
 check('approved future post remains scheduled',lambda:yes(scheduled['status']=='Scheduled'))
 with psycopg.connect(p.DSN) as db:db.execute('UPDATE "ActivityPost" SET "ScheduledAt"=now()-interval \'1 minute\' WHERE "Id"=%s',(scheduled['id'],))
 for _ in range(30):
  if member.request(base+'/posts/'+scheduled['id'])['post']['status']=='Published':break
  time.sleep(.2)
 check('background scheduler publishes approved due post',lambda:yes(member.request(base+'/posts/'+scheduled['id'])['post']['status']=='Published'))
 post=state['p'];pid=post['id']
 check('reaction created',lambda:peer.request(base+'/posts/'+pid+'/reaction','PUT',{'kind':'Celebrate'},expected=204))
 check('duplicate reactions are idempotent',lambda:peer.request(base+'/posts/'+pid+'/reaction','PUT',{'kind':'Celebrate'},expected=204))
 check('one reaction row per account and post',lambda:yes(next(x for x in peer.request(base+'/feed')['items'] if x['id']==pid)['reactions']==1))
 comment=peer.request(base+'/posts/'+pid+'/comments','POST',{'body':'Thank you volunteers!','parentId':None})
 check('comments and replies work',lambda:member.request(base+'/posts/'+pid+'/comments','POST',{'body':'You are welcome','parentId':comment['id']}))
 check('comment moderation retains history',lambda:owner.request(base+'/comments/'+comment['id']+'/hide','POST',{'reason':'Test moderation'},expected=204))
 check('hidden comment excluded from public conversation',lambda:yes(not any(c['id']==comment['id'] for c in peer.request(base+'/posts/'+pid)['comments'])))
 check('bookmark saved privately',lambda:peer.request(base+'/posts/'+pid+'/bookmark','PUT',expected=204))
 check('bookmark feed returns saved post',lambda:yes(peer.request(base+'/feed?bookmarks=true')['items'][0]['id']==pid))
 check('post reporting works',lambda:peer.request(base+'/posts/'+pid+'/report','POST',{'reason':'Review requested'},expected=204))
 check('moderation queue scoped to authorized officer',lambda:yes(owner.request(base+'/reports')[0]['postId']==pid))
 check('chapter profile respects directory scope',lambda:yes(peer.request(base+'/chapters/'+chapter)['members']>=2))
 check('unauthorized chapter profile denied',lambda:remoteclient.request(base+'/chapters/'+chapter,expected=403))
 check('photo upload uses private storage',lambda:state.update(media=member.upload(base+'/media?resource='+pid+'&kind=Post',p.png)))
 check('media edit resets publication',lambda:yes(member.request(base+'/posts/'+pid)['post']['status']=='Draft'))
 check('invalid media upload rejected',lambda:member.upload(base+'/media?resource='+pid+'&kind=Post',b'not a PNG',expected=400))
 check('private media IDOR blocked',lambda:remoteclient.request(base+'/media/'+state['media']['id'],expected=403))
 current=member.request(base+'/posts/'+pid)['post'];current=member.request(base+'/posts/'+pid,'PUT',postbody(revision=current['revision']));state['p']=decision(owner,'posts',current)
 memberid=peer.request('/membership/me')[0]['id']
 check('member mention validates existing directory identity',lambda:yes(draft(member,mentions=[memberid])['mentions']==[memberid]))
 check('cross-tenant mention identifier rejected',lambda:member.request(base+'/posts','POST',postbody(mentions=[str(uuid.uuid4())]),expected=403))
 event=create_event()
 check('event creation snapshots approval policy',lambda:yes(event['status']=='Pending Approval'))
 check('ordinary member cannot create event',lambda:member.request(base+'/events','POST',eventbody(),expected=403))
 check('event cannot approve itself',lambda:owner.request(base+'/events/'+event['id']+'/decision','POST',{'decision':'Approve','note':'Self','revision':0},expected=403))
 check('event approval publishes',lambda:state.update(event=decision(reviewer,'events',event)))
 event=state['event'];eid=event['id'];ep=base+'/events/'+eid
 check('bounded calendar contains published event',lambda:yes(any(e['id']==eid for e in member.request(base+'/events')['items'])))
 check('calendar rejects excessive date range',lambda:owner.request(base+'/events?from='+urllib.parse.quote(iso(-10000))+'&to='+urllib.parse.quote(iso(10000)),expected=400))
 check('unrelated chapter event denied',lambda:remoteclient.request(ep,expected=403))
 check('cross-tenant event denied',lambda:other.request(ep,expected=403))
 check('RSVP Going is recorded',lambda:member.request(ep+'/rsvp','PUT',{'kind':'Going'},expected=204))
 check('RSVP does not reserve a seat',lambda:yes(owner.request(ep)['confirmedSeats']==0))
 check('RSVP can be edited',lambda:member.request(ep+'/rsvp','PUT',{'kind':'Maybe'},expected=204))
 audience=owner.request(ep+'/audience');ids=[x['id'] for x in audience['members']]
 check('audience builder deduplicates and reports eligibility',lambda:yes(audience['uniquePeople']>=2))
 invites=owner.request(ep+'/invitations','POST',{'membershipIds':ids,'channel':'In App','days':7})
 check('in-app invitation created without false delivery claim',lambda:yes(len(invites['items'])>=2 and 'Available' in invites['delivery']))
 check('member sees personal invitation',lambda:yes(any(i['eventId']==eid for i in member.request(base+'/invitations'))))
 check('invalid invitation rejected',lambda:member.request(base+'/invitations/accept','POST',{'kind':'invalid'},expected=400))
 expired=invites['items'][0]
 with psycopg.connect(p.DSN) as db:db.execute('UPDATE "EventInvitation" SET "ExpiresAt"=now()-interval \'1 hour\' WHERE "Id"=%s',(expired['id'],))
 token=expired['url'].split('invitation=')[1]
 check('expired invitation rejected',lambda:member.request(base+'/invitations/accept','POST',{'kind':token},expected=400))
 reg=member.request(ep+'/registrations','POST',{})
 check('member registration confirmed independently',lambda:yes(reg['status']=='Confirmed'))
 peerreg=peer.request(ep+'/registrations','POST',{})
 third,_,_=account('third',chapter)
 waiting=third.request(ep+'/registrations','POST',{})
 check('capacity enforces waitlist',lambda:yes(waiting['status']=='Waitlisted'))
 check('duplicate registration rejected',lambda:member.request(ep+'/registrations','POST',{},expected=400))
 check('private registrations blocked for ordinary peers',lambda:peer.request(ep+'/registrations',expected=403))
 check('participant cancellation frees capacity',lambda:member.request(ep+'/registrations/'+reg['id']+'/decision','POST',{'decision':'Cancel','note':'Test','revision':reg['revision']}))
 check('waitlist promotes in order',lambda:yes(third.request(ep)['registration']['status']=='Confirmed'))
 passdata=peer.request(ep+'/passes?registration='+peerreg['id'],'POST')
 check('digital pass contains opaque secure QR reference',lambda:yes(len(passdata['token'])==64 and peeremail not in passdata['token']))
 check('ordinary member cannot check in',lambda:peer.request(ep+'/checkins','POST',{'token':passdata['token']},expected=403))
 check('QR check-in records actual attendance',lambda:yes(owner.request(ep+'/checkins','POST',{'token':passdata['token'],'entryPoint':'North gate'})['state']=='Valid'))
 check('duplicate check-in is idempotent',lambda:yes(owner.request(ep+'/checkins','POST',{'token':passdata['token']})['state']=='Already Checked In'))
 check('invalid QR rejected',lambda:yes(owner.request(ep+'/checkins','POST',{'token':'invalid'})['state']=='Invalid Pass'))
 wrong=decision(reviewer,'events',create_event(title='Another event'))
 check('wrong event QR rejected',lambda:yes(owner.request(base+'/events/'+wrong['id']+'/checkins','POST',{'token':passdata['token']})['state']=='Wrong Event'))
 check('pass revocation enforced',lambda:owner.request(ep+'/passes/'+passdata['id']+'/revoke','POST',expected=204))
 check('revoked pass rejected',lambda:yes(owner.request(ep+'/checkins','POST',{'token':passdata['token']})['state']=='Revoked'))
 at=owner.request(ep+'/attendance')[0]
 check('attendance correction retains reason',lambda:owner.request(ep+'/attendance/'+at['id']+'/correct','POST',{'reason':'Duplicate entry resolved'},expected=204))
 analytics=owner.request(ep+'/analytics')
 check('event analytics separates registrations from attendance',lambda:yes(analytics['registrations']==3 and analytics['checkedIn']==0 and analytics['confirmed']==2))
 check('no-shows unavailable before event finishes',lambda:yes(analytics['noShows'] is None))
 # Genuine race: independent authenticated clients contend for one event capacity lock.
 concurrent_event=decision(reviewer,'events',create_event(title='Concurrent capacity',capacity=1))
 members=[account('race'+str(i),chapter)[0] for i in range(5)]
 def register(c):return c.request(base+'/events/'+concurrent_event['id']+'/registrations','POST',{})
 with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:race=list(pool.map(register,members))
 check('concurrent registrations cannot overbook capacity',lambda:yes(sum(r['status']=='Confirmed' for r in race)==1 and sum(r['status']=='Waitlisted' for r in race)==4))
 winner=next((c,r) for c,r in zip(members,race) if r['status']=='Confirmed');qr=winner[0].request(base+'/events/'+concurrent_event['id']+'/passes?registration='+winner[1]['id'],'POST')
 op1,_,_=account('operator1',admin=True);op2,_,_=account('operator2',admin=True)
 def scan(c):return c.request(base+'/events/'+concurrent_event['id']+'/checkins','POST',{'token':qr['token']})['state']
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:scans=list(pool.map(scan,[op1,op2]))
 check('concurrent check-in operators create one attendance record',lambda:yes(sorted(scans)==sorted(['Valid','Already Checked In'])))
 guest,guestemail,guestuser=account('guest')
 guestevent=decision(reviewer,'events',create_event(title='Guest program',allowGuests=True,invitationOnly=True,capacity=3))
 gi=owner.request(base+'/events/'+guestevent['id']+'/invitations','POST',{'guestEmails':[guestemail],'days':7})['items'][0]
 check('guest securely accepts recipient-bound invitation',lambda:guest.request(base+'/invitations/accept','POST',{'kind':gi['url'].split('invitation=')[1]}))
 check('invited guest registration allowed without duplicate person master',lambda:yes(guest.request(base+'/events/'+guestevent['id']+'/registrations','POST',{})['status']=='Confirmed'))
 approvalevent=decision(reviewer,'events',create_event(title='Approval required',registrationApproval=True))
 pending=member.request(base+'/events/'+approvalevent['id']+'/registrations','POST',{})
 check('registration approval remains distinct from RSVP',lambda:yes(pending['status']=='Awaiting Approval'))
 check('organizer approves registration',lambda:yes(owner.request(base+'/events/'+approvalevent['id']+'/registrations/'+pending['id']+'/decision','POST',{'decision':'Approve','note':'Accepted','revision':pending['revision']})['status']=='Confirmed'))
 # Cursor pagination is stable across posts with equal timestamps.
 policy=owner.request(base+'/setup')['policy'];owner.request(base+'/policy','PUT',{**policy,'postStages':'[]'},expected=204)
 for i in range(25):draft(member,title='Pagination story '+str(i))
 first=member.request(base+'/feed');nextq='&before='+urllib.parse.quote(first['next']['before'])+'&beforeId='+first['next']['beforeId'];second=member.request(base+'/feed?x=1'+nextq)
 check('large feed cursor pagination has no duplicated rows',lambda:yes(len(first['items'])==20 and not ({x['id'] for x in first['items']}&{x['id'] for x in second['items']})))
 check('chapter activity metrics use actual published posts',lambda:yes(owner.request(base+'/intelligence')['posts']>=25))
 check('regional intelligence stays in assigned branch',lambda:yes(all(c['id']!=remote for c in regional.request(base+'/intelligence')['byChapter'])))
 check('member cannot read administrative intelligence',lambda:member.request(base+'/intelligence',expected=403))
 check('post block hides author from reader',lambda:peer.request(base+'/blocks/'+user,'PUT',expected=204))
 check('blocked author posts excluded',lambda:yes(not any(x['authorId']==user for x in peer.request(base+'/feed')['items'])))
 check('unblock restores permission-filtered content',lambda:peer.request(base+'/blocks/'+user,'DELETE',expected=204))
 check('tenant-scope media cannot be retrieved cross-society',lambda:other.request(base+'/media/'+state['media']['id'],expected=403))
 def immutable_check():
  try:
   with psycopg.connect(p.DSN) as db:db.execute('DELETE FROM "ActivityPost" WHERE "Id"=%s',(pid,))
  except psycopg.errors.RaiseException:return
  raise AssertionError('Post deletion was not protected')
 check('PostgreSQL retains canonical activity history',immutable_check)
 check('cross-tenant co-host reference rejected',lambda:owner.request(base+'/events','POST',eventbody(coHosts=[str(uuid.uuid4())]),expected=403))
 # Additional policy, approval and attendance edge cases.
 policy=owner.request(base+'/setup')['policy'];owner.request(base+'/policy','PUT',{**policy,'publicAllowed':True,'postStages':'[]'},expected=204)
 public=draft(owner,official=True,visibility='Public',title='Explicit public story')
 anonymous=p.Client()
 check('explicitly permitted public post is readable anonymously',lambda:yes(anonymous.request('/public'+base+'/posts/'+public['id'])['id']==public['id']))
 check('private post never leaks through public URL',lambda:anonymous.request('/public'+base+'/posts/'+pid,expected=404))
 check('private media never leaks through public URL',lambda:anonymous.request('/public'+base+'/media/'+state['media']['id'],expected=404))
 policy=owner.request(base+'/setup')['policy'];owner.request(base+'/policy','PUT',{**policy,'publicAllowed':False,'postStages':json.dumps(['activities.approve','activities.approve'])},expected=204)
 check('disabling public policy closes public read access',lambda:anonymous.request('/public'+base+'/posts/'+public['id'],expected=404))
 two=draft(member);two=decision(owner,'posts',two)
 check('first stage leaves content unpublished',lambda:yes(two['status']=='Pending Approval'))
 check('same reviewer cannot approve both stages',lambda:owner.request(base+'/posts/'+two['id']+'/decision','POST',{'decision':'Approve','note':'Repeated reviewer','revision':two['revision']},expected=400))
 check('second independent reviewer completes approval',lambda:yes(decision(reviewer,'posts',two)['status']=='Published'))
 party=decision(reviewer,'events',create_event(title='Party attendance',capacity=4,maxCompanions=2,questions=json.dumps([{'key':'access','label':'Access needs','required':True}])))
 partyurl=base+'/events/'+party['id']
 check('required registration answer enforced',lambda:member.request(partyurl+'/registrations','POST',{'companions':2},expected=400))
 partyreg=member.request(partyurl+'/registrations','POST',{'companions':2,'answers':json.dumps({'access':'None'})})
 check('companion registration reserves whole party capacity',lambda:yes(owner.request(partyurl)['confirmedSeats']==3))
 partyqr=member.request(partyurl+'/passes?registration='+partyreg['id'],'POST');newqr=member.request(partyurl+'/passes?registration='+partyreg['id'],'POST')
 check('pass reissue revokes previous QR',lambda:yes(owner.request(partyurl+'/checkins','POST',{'token':partyqr['token']})['state']=='Revoked'))
 check('check-in rejects attendance exceeding registered party',lambda:owner.request(partyurl+'/checkins','POST',{'token':newqr['token'],'peopleCount':4},expected=400))
 check('check-in records physically present party size',lambda:yes(owner.request(partyurl+'/checkins','POST',{'token':newqr['token'],'peopleCount':1})['state']=='Valid'))
 check('actual attendance does not assume all companions arrived',lambda:yes(owner.request(partyurl+'/analytics')['attendedSeats']==1))
 optout=peer.request(prefix+'/membership/members/'+memberid);om=optout['member']
 peer.request(prefix+'/membership/members/'+memberid,'PUT',{'fullName':om['fullName'],'chapterId':om['chapterId'],'categoryId':om['categoryId'],'number':om['number'],'status':om['status'],'profile':json.dumps(optout['profile']),'privacy':json.dumps({**optout['privacy'],'communication':False}),'revision':om['revision']},expected=204)
 check('existing membership communication opt-out prevents invitations',lambda:yes(owner.request(partyurl+'/invitations','POST',{'membershipIds':[memberid],'channel':'In App'})['skipped']==1))
 check('unconfigured SMS channel makes no delivery claim',lambda:owner.request(partyurl+'/invitations','POST',{'membershipIds':[],'channel':'SMS'},expected=400))
 check('regional officer cannot manage unrelated event',lambda:regional.request(partyurl+'/cancel','POST',{'reason':'Unauthorized'},expected=403))
 recover=owner.request(base+'/events','POST',eventbody(submit=False,title='Recoverable draft'))
 check('own event drafts remain recoverable',lambda:yes(any(e['id']==recover['id'] for e in owner.request(base+'/my/events?status=Draft'))))
 check('member activity history filters posts by viewer authority',lambda:yes(isinstance(peer.request(base+'/members/'+memberid+'/activity'),list)))
 owner.request(partyurl+'/cancel','POST',{'reason':'Cancelled fictional test program'},expected=204)
 check('participant can still view cancellation in original audience',lambda:yes(member.request(partyurl)['eventData']['status']=='Cancelled'))
 check('cancelled event denies pass check-in',lambda:yes(owner.request(partyurl+'/checkins','POST',{'token':newqr['token']})['state']=='Cancelled'))
 samples=[]
 for _ in range(20):
  start=time.perf_counter();member.request(base+'/feed');samples.append((time.perf_counter()-start)*1000)
 perf={'scope':'20 warm authenticated local feed requests on integration fixture','median_ms':round(statistics.median(samples),3),'p95_ms':round(sorted(samples)[18],3),'target_p95_ms':500};perf['target_met']=perf['p95_ms']<500
 (ROOT/'tests/phase3-http-performance.json').write_text(json.dumps(perf,indent=2))
 check('authenticated feed meets documented local target',lambda:yes(perf['target_met']))
finally:
 server.terminate();server.wait(timeout=15);log.close();(ROOT/'tests/phase3-results.json').write_text(json.dumps({'passed':sum(x['passed'] for x in results),'total':len(results),'results':results},indent=2))
