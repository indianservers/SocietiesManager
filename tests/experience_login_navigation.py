"""Authentication navigation must never be mistaken for unsaved workspace edits."""
import json,os
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
ROOT=Path(__file__).resolve().parents[1];URL=os.environ.get('EXPERIENCE_URL','http://127.0.0.1:5197');checks=[];dialogs=[]
def check(name,value=True):
 assert value,name
 checks.append(name);print('PASS',name,flush=True)
with sync_playwright() as pw:
 browser=pw.chromium.launch();context=browser.new_context();page=context.new_page();page.on('dialog',lambda d:(dialogs.append(d.message),d.dismiss()))
 page.goto(URL+'/');page.get_by_label('Email or username',exact=True).fill('navigation-test@example.invalid');page.get_by_label('Password',exact=True).fill('NotARealPassword!2027');page.get_by_role('link',name='Forgot password?',exact=True).click();expect(page).to_have_url(URL+'/forgot');check('Filled login can open password recovery without a warning',not dialogs)
 page.goto(URL+'/');page.get_by_label('Email or username',exact=True).fill('navigation-test@example.invalid');page.locator('form').evaluate("form=>form.dataset.dirty='true'");check('Stale dirty markers on authentication forms cannot block leaving',page.evaluate("()=>{const e=new Event('beforeunload',{cancelable:true});window.dispatchEvent(e);return !e.defaultPrevented}"));page.get_by_role('link',name='Create an account',exact=True).click();expect(page).to_have_url(URL+'/signup');check('Account creation link does not trigger unsaved-changes warning',not dialogs)
 page.get_by_label('Full name',exact=True).fill('Navigation test');page.get_by_label('Email address',exact=True).fill('navigation-test@example.invalid');page.get_by_role('link',name='Sign in',exact=True).click();expect(page.get_by_role('button',name='Sign in',exact=True)).to_be_visible();check('Filled signup can return to sign-in without a warning',not dialogs)
 page.get_by_role('link',name='Email me a sign-in link',exact=True).click();expect(page.get_by_role('button',name='Send sign-in link')).to_be_visible();check('Passwordless sign-in navigation has no warning',not dialogs)
 if not URL.startswith('https:'):
  cfg=json.loads((ROOT/'.local/society-example-account.json').read_text());token=context.request.get(URL+'/api/v1/auth/csrf').json()['token'];assert context.request.post(URL+'/api/v1/auth/login',data={'email':cfg['email'],'password':cfg['password']},headers={'X-CSRF-TOKEN':token}).ok
  page.goto(URL+'/app/account?view=join');page.get_by_text('Personal & professional details (optional)',exact=True).click();page.locator('input[name=city]').fill('An unsaved workspace edit');before=page.url;page.locator('.sidebar').get_by_role('link',name='Home',exact=True).click();check('Genuine workspace edits still warn before navigation',len(dialogs)==1 and 'unsaved changes' in dialogs[0]);check('Cancel keeps the workspace form and its input',page.url==before and page.locator('input[name=city]').input_value()=='An unsaved workspace edit')
 browser.close()
(ROOT/'tests/experience-login-navigation-results.json').write_text(json.dumps({'checks':checks,'authenticationWarnings':dialogs if URL.startswith('https:') else dialogs[:-1]},indent=2))
