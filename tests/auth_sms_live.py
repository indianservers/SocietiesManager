"""One user-authorized live SMS delivery attempt; never solves a CAPTCHA or bypasses verification."""
import json,time
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
ROOT=Path.cwd();url='https://members.aimersociety.com';number='+919618222220'
with sync_playwright() as pw:
 browser=pw.chromium.launch(headless=False);ctx=browser.new_context(viewport={'width':1280,'height':900},user_agent='Mozilla/5.0');p=ctx.new_page();expect.set_options(timeout=30000);p.goto(url+'/',wait_until='domcontentloaded',timeout=60000);p.get_by_role('button',name='Sign in with mobile SMS',exact=True).click();p.get_by_label('Verified mobile number',exact=True).fill(number);p.get_by_role('button',name='Send SMS code',exact=True).click();outcome=None
 for _ in range(60):
  if p.get_by_label('SMS verification code',exact=True).is_visible():outcome='SMS code requested successfully';break
  errors=p.locator('.community-account-form [role=alert]')
  if errors.count():outcome=errors.first.inner_text();break
  if p.locator('iframe[src*="bframe"]').count() and p.locator('iframe[src*="bframe"]').first.is_visible():
   print('CAPTCHA challenge is present; user must complete it in the visible browser. No automation will solve it.',flush=True)
   outcome='Interactive CAPTCHA requires user completion';break
  p.wait_for_timeout(1000)
 if outcome is None:outcome='SMS request did not complete within 60 seconds'
 print(outcome,flush=True);p.screenshot(path=str(ROOT/'tests/auth-sms-live-attempt.png'),full_page=True);(ROOT/'tests/auth-sms-live-results.json').write_text(json.dumps({'number':number,'result':outcome,'smsCodeConsumed':False},indent=2))
 # Keep the visible browser open for user completion only when a challenge is shown.
 if outcome=='Interactive CAPTCHA requires user completion':
  print('Waiting for user to complete CAPTCHA; press Enter in terminal after completion.',flush=True);input()
  p.wait_for_timeout(3000)
  if p.get_by_label('SMS verification code',exact=True).is_visible():outcome='SMS code requested successfully after user CAPTCHA completion';print(outcome,flush=True)
  else:outcome=p.locator('.community-account-form [role=alert]').first.inner_text() if p.locator('.community-account-form [role=alert]').count() else 'CAPTCHA completion is still pending';print(outcome,flush=True)
  (ROOT/'tests/auth-sms-live-results.json').write_text(json.dumps({'number':number,'result':outcome,'smsCodeConsumed':False},indent=2))
 browser.close()
