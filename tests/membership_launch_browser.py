"""Mobile browser QA for configured launch branding, signup and dual membership."""
import importlib
import json
import re
import secrets
import sys
import urllib.parse
from pathlib import Path
import psycopg
from playwright.sync_api import sync_playwright, expect

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'scripts'))
Client=importlib.import_module('seed-society-example').Client
account=json.loads((ROOT/'.local/society-example-account.json').read_text())
preview=json.loads((ROOT/'.local/preview-account.json').read_text())
member=json.loads((ROOT/'.local/mobile-participant.json').read_text())
config=json.loads((ROOT/'.local/dev.json').read_text())
launch=json.loads((ROOT/'scripts/society-example.json').read_text())
owner=Client('http://127.0.0.1:5092/api/v1');owner.call('/auth/login','POST',account)
prefix='/societies/'+account['tenant'];base=prefix+'/membership'
institution=owner.call(base+'/members?search=INST-0001')['items'][0]
representatives=owner.call(base+'/members/'+institution['id']+'/representatives')
if not any(r['userId']==member['user'] for r in representatives):owner.call(base+'/members/'+institution['id']+'/representatives','POST',{'email':member['email'],'role':'Controller'})
owner.call(base+'/members/'+institution['id']+'/chapters/'+institution['chapterId'],'PUT')
checks=[];errors=[]

def check(name,value=True):
 assert value,name
 checks.append(name);print('PASS BROWSER',name,flush=True)

def login(context,credentials):
 csrf=context.request.get(url+'/api/v1/auth/csrf').json()['token']
 assert context.request.post(url+'/api/v1/auth/login',data=credentials,headers={'X-CSRF-TOKEN':csrf}).ok

url='http://127.0.0.1:5176'
with sync_playwright() as p:
 browser=p.chromium.launch()
 try:
  context=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True)
  page=context.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
  page.goto(url+'/signup');page.wait_for_load_state('networkidle')
  check('launch society brands the signup page',launch['name'] in page.locator('body').inner_text())
  check('signup offers exactly Individual, Institutional and Corporate options',page.get_by_role('radio').count()==3)
  check('signup displays annual INR prices from platform-owner settings','₹500' in page.locator('body').inner_text() and page.locator('body').inner_text().count('₹10,000')==2)
  for kind in ['Individual','Institutional','Corporate']:
   page.get_by_role('radio',name=re.compile('^'+kind)).check()
   expect(page.get_by_role('radio',name=re.compile('^'+kind))).to_be_checked()
  check('each signup option is selectable on mobile')
  page.get_by_role('radio',name=re.compile('^Institutional')).check()
  email='launch-ui-'+secrets.token_hex(5)+'@example.test';password=secrets.token_urlsafe(22)+'!Aa1'
  page.get_by_label('Full name',exact=True).fill('Synthetic Mobile Applicant')
  page.get_by_label('Email address',exact=True).fill(email)
  page.get_by_label('Password',exact=True).fill(password)
  page.screenshot(path=str(ROOT/'tests/membership-launch-signup-mobile.png'),full_page=True)
  with page.expect_response(lambda r:r.request.method=='POST' and r.url.endswith('/auth/signup')) as signup:
   page.locator('form button[type=submit],form button.primary').last.click()
  check('institution signup posts successfully',signup.value.ok)
  with psycopg.connect(host='127.0.0.1',port=55439,dbname='assemblia',user='postgres',password=config['password']) as db:
   user=db.execute('SELECT "Id","PreferredMembershipKind","PlatformAdmin" FROM "AspNetUsers" WHERE "Email"=%s',(email,)).fetchone()
   body=db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s ORDER BY "CreatedAt" DESC LIMIT 1',(email,)).fetchone()[0]
  check('signup saves Institutional intent without granting administrator access',user[1]=='Institutional' and not user[2])
  token=urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
  verify=Client('http://127.0.0.1:5092/api/v1');verify.call('/auth/verify?userId='+str(user[0])+'&token='+urllib.parse.quote(token,safe=''),'POST')
  login(context,{'email':email,'password':password})
  page.goto(url+'/app/account?view=join');page.wait_for_load_state('networkidle')
  expect(page.get_by_role('radio',name=re.compile('^Institutional'))).to_be_checked()
  check('verified signup choice carries into the membership application')
  page.get_by_label('Institution / company name',exact=True).fill('Synthetic Mobile Institute')
  page.locator('select[name=chapterId]').select_option(index=1)
  category=page.locator('select[name=categoryId]')
  check('institution application cannot silently choose a free legacy individual category',category.locator('option').count()==1 and 'Institute Membership' in category.inner_text())
  with page.expect_response(lambda r:r.request.method=='POST' and r.url.endswith('/membership/applications')) as application_response:
   page.get_by_role('button',name='Submit application',exact=True).click()
  check('institution membership application submits on mobile',application_response.value.ok)
  application=application_response.value.json()
  approved=owner.call(base+'/applications/'+application['id']+'/decision','POST',{'decision':'Approve','comment':'Synthetic mobile launch QA','revision':0})
  page.goto(url+'/app/membership');page.wait_for_load_state('networkidle')
  expect(page.locator('main')).to_contain_text('Payment required')
  check('approved unpaid institution clearly awaits payment and has no resource access','Synthetic Mobile Institute' in page.locator('main').inner_text() and 'Active membership unlocks resources' in page.locator('main').inner_text())
  context.close()
  context=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True);login(context,member)
  page=context.new_page();page.on('pageerror',lambda e:errors.append(str(e)))
  page.goto(url+'/app/membership');page.wait_for_load_state('networkidle')
  check('one account sees its Individual membership and Institutional controller card','Individual · My membership' in page.locator('main').inner_text() and 'Institutional · Controller' in page.locator('main').inner_text())
  for width in [360,390,768]:
   page.set_viewport_size({'width':width,'height':844})
   check(str(width)+'px member portal has no horizontal page overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
  page.set_viewport_size({'width':390,'height':844})
  page.get_by_role('button',name=re.compile('Example Institute 001')).click()
  page.get_by_role('button',name='Organization team & chapters',exact=True).click()
  expect(page.locator('main')).to_contain_text('Institution chapters')
  check('institution controller can inspect organization team and linked chapter','No chapters linked' not in page.locator('main').inner_text())
  resource=page.locator('.member-resource-grid button').filter(has_text='Institutional member welcome guide')
  resource.click()
  expect(page.get_by_role('dialog')).to_contain_text('Representatives can retain their own Individual memberships.')
  check('institution-only resource opens for the verified controller')
  page.get_by_role('button',name='Close drawer',exact=True).click()
  page.screenshot(path=str(ROOT/'tests/membership-launch-dual-membership-mobile.png'),full_page=True)
  context.close()
  context=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True)
  login(context,{'email':preview['platformEmail'],'password':preview['platformPassword']})
  page=context.new_page();page.goto(url+'/app/platform?view=membership-pricing');page.wait_for_load_state('networkidle')
  page.get_by_label('Society',exact=False).select_option(account['tenant']);page.wait_for_load_state('networkidle')
  row=page.locator('.renewal-history>div').filter(has_text='Individual Membership')
  row.get_by_role('button',name='Set pricing',exact=True).click()
  expect(page.get_by_label('Membership fee',exact=True)).to_have_value('500')
  expect(page.get_by_label('Validity in months',exact=True)).to_have_value('12')
  check('platform pricing editor shows configured annual Individual fee')
  page.get_by_role('button',name='Save pricing',exact=True).click()
  expect(page.get_by_role('dialog')).not_to_be_visible()
  check('platform owner can save membership pricing on mobile')
  check('no uncaught browser runtime errors',not errors)
 finally:
  page.screenshot(path=str(ROOT/'tests/membership-launch-last-screen.png'),full_page=True)
  browser.close()
  (ROOT/'tests/membership-launch-browser-results.json').write_text(json.dumps({'passed':len(checks),'checks':checks,'pageErrors':errors},indent=2),encoding='utf-8')
