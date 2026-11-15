"""Interactive live verification. Codes stay in browser/process memory and are never stored in artifacts."""
import json,sys,time
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
URL='https://members.aimersociety.com';EMAIL='saisatish@indianservers.com';NUMBER='+919618222220';ROOT=Path.cwd()
def emit(value):print(json.dumps(value),flush=True)
with sync_playwright() as pw:
 browser=pw.chromium.launch(headless=False);ctx=browser.new_context(viewport={'width':1280,'height':900},user_agent='Mozilla/5.0');p=ctx.new_page();expect.set_options(timeout=30000)
 def state():
  if '/app' in p.url:
   me=ctx.request.get(URL+'/api/v1/users/me');
   if me.ok:
    user=me.json();methods=ctx.request.get(URL+'/api/v1/security/methods').json();return {'state':'SIGNED_IN','requestedAccountMatches':user['email'].lower()==EMAIL,'phoneVerified':methods.get('phoneVerified'),'requestedPhoneMatches':methods.get('phoneNumber')==NUMBER}
  if p.locator('iframe[src*="bframe"]').count() and p.locator('iframe[src*="bframe"]').first.is_visible():return {'state':'CAPTCHA'}
  for label,name in [('SMS verification code','SMS_CODE'),('Email sign-in code','EMAIL_CODE'),('Authenticator code','MFA')]:
   if p.get_by_label(label,exact=True).is_visible():return {'state':name}
  if p.get_by_role('button',name='Send email code',exact=True).is_visible():return {'state':'EMAIL_ACCOUNT_CONFIRMATION'}
  alerts=p.locator('[role=alert]');texts=[s for s in alerts.all_inner_texts() if s.strip()]
  return {'state':'WAITING','messages':texts}
 def settle(seconds=15):
  last=None
  for _ in range(seconds*2):
   current=state()
   if current['state'] in ['SIGNED_IN','EMAIL_ACCOUNT_CONFIRMATION','EMAIL_CODE','MFA'] or current.get('messages'):return current
   if current['state'] in ['CAPTCHA','SMS_CODE'] and last==current:return current
   last=current;p.wait_for_timeout(500)
  return state()
 try:
  p.goto(URL+'/',wait_until='domcontentloaded',timeout=60000);p.get_by_role('button',name='Sign in with mobile SMS',exact=True).click();p.get_by_label('Verified mobile number',exact=True).fill(NUMBER);p.get_by_role('button',name='Send SMS code',exact=True).click();emit(settle())
  while True:
   emit({'readyForCommand':True});line=input();command=json.loads(line);action=command.get('action')
   if action=='inspect':emit(settle(5))
   elif action=='sms':
    p.get_by_label('SMS verification code',exact=True).fill(command['code']);p.get_by_role('button',name='Verify mobile number',exact=True).click();p.wait_for_timeout(1000);emit(settle())
   elif action=='email-request':
    p.get_by_label('Email address',exact=True).fill(EMAIL);p.get_by_role('button',name='Send email code',exact=True).click();p.wait_for_timeout(1000);emit(settle())
   elif action=='email':
    p.get_by_label('Email sign-in code',exact=True).fill(command['code']);p.get_by_role('button',name='Verify code & sign in',exact=True).click();p.wait_for_timeout(1000);emit(settle())
   elif action=='mfa':
    p.get_by_label('Authenticator code',exact=True).fill(command['code']);p.get_by_role('button',name='Verify & sign in',exact=True).click();p.wait_for_timeout(1000);emit(settle())
   elif action=='finish':
    result=state();assert result.get('state')=='SIGNED_IN' and result.get('requestedAccountMatches') and result.get('phoneVerified') and result.get('requestedPhoneMatches');(ROOT/'tests/auth-sms-live-linked-results.json').write_text(json.dumps(result,indent=2));token=ctx.request.get(URL+'/api/v1/auth/csrf').json()['token'];ctx.request.post(URL+'/api/v1/auth/logout',data={},headers={'X-CSRF-TOKEN':token});emit({'completed':True});break
   elif action=='close':emit({'closed':True});break
 except EOFError:emit({'closed':'Input ended'})
 finally:browser.close()
