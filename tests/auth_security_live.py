"""Live auth UI and user-authorized reset/OTP delivery requests; never reads or consumes emailed secrets."""
import json,time,uuid
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
ROOT=Path.cwd();LIVE='https://members.aimersociety.com';EMAIL='saisatish@indianservers.com';checks=[];errors=[]
with sync_playwright() as pw:
 browser=pw.chromium.launch();ctx=browser.new_context(viewport={'width':1366,'height':900},user_agent='Mozilla/5.0');p=ctx.new_page();p.on('pageerror',lambda e:errors.append(str(e)));expect.set_options(timeout=30000)
 providers=ctx.request.get(LIVE+'/api/v1/auth/providers').json();assert providers['enabled'] and all(i in providers['providers'] for i in ['google.com','microsoft.com','phone']);checks.append('Google, Microsoft and verified-mobile providers advertised')
 checkpoint=ROOT/'.local/auth-live-delivery-checkpoint.json'
 if not checkpoint.exists():
  p.goto(LIVE+'/forgot',wait_until='domcontentloaded',timeout=60000);p.get_by_label('Email address',exact=True).fill(EMAIL);p.get_by_role('button',name='Send recovery email',exact=True).click();expect(p.get_by_role('status').filter(has_text='recovery email')).to_be_visible();checks.append('Password-reset request accepted through live UI')
  p.goto(LIVE+'/',wait_until='domcontentloaded',timeout=60000);p.get_by_role('button',name='Sign in with email OTP',exact=True).click();p.get_by_label('Email address',exact=True).fill(EMAIL);p.get_by_role('button',name='Send email code',exact=True).click();expect(p.get_by_label('Email sign-in code',exact=True)).to_be_visible();p.screenshot(path=str(ROOT/'tests/auth-email-otp-live.png'),full_page=True);checks.append('Email OTP requested through live UI')
  checkpoint.write_text(json.dumps({'email':EMAIL,'requested':time.time()}))
 p.goto(LIVE+'/',wait_until='domcontentloaded',timeout=60000);expect(p.get_by_role('button',name='Google',exact=True)).to_be_enabled();expect(p.get_by_role('button',name='Microsoft',exact=True)).to_be_enabled();p.get_by_role('button',name='Sign in with mobile SMS',exact=True).click();expect(p.get_by_label('Verified mobile number',exact=True)).to_be_visible();p.set_viewport_size({'width':390,'height':844});assert p.evaluate('document.documentElement.scrollWidth<=innerWidth');p.screenshot(path=str(ROOT/'tests/auth-mobile-sms-live.png'),full_page=True);checks.append('Live mobile verification UI available without sending an unsolicited SMS')
 token=ctx.request.get(LIVE+'/api/v1/auth/csrf').json()['token'];r=ctx.request.post(LIVE+'/api/v1/auth/email-otp/verify',data={'challengeId':str(uuid.uuid4()),'code':'000000'},headers={'X-CSRF-TOKEN':token});assert r.status==401;checks.append('Invalid OTP refused on production')
 assert not errors,errors;browser.close()
(ROOT/'tests/auth-security-live-results.json').write_text(json.dumps({'checks':checks,'errors':errors,'emailTestRecipient':EMAIL,'smsDeliveryTested':False},indent=2));print('\n'.join('PASS '+s for s in checks))
