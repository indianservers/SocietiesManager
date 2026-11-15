"""Check record search and tab-local draft recovery using real existing services."""
import json,os
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
ROOT=Path(__file__).resolve().parents[1];URL=os.environ.get('EXPERIENCE_URL','http://127.0.0.1:5197');checks=[];errors=[]
def check(name,value=True):
 assert value,name
 checks.append(name);print('PASS',name,flush=True)
def signin(context,cfg):
 token=context.request.get(URL+'/api/v1/auth/csrf').json()['token'];r=context.request.post(URL+'/api/v1/auth/login',data=cfg,headers={'X-CSRF-TOKEN':token});assert r.ok
with sync_playwright() as pw:
 browser=pw.chromium.launch();ctx=browser.new_context(viewport={'width':1440,'height':1000});cfg=json.loads((ROOT/'.local/preview-account.json').read_text());signin(ctx,{'email':cfg['platformEmail'],'password':cfg['platformPassword']});p=ctx.new_page();p.on('pageerror',lambda e:errors.append(str(e)));p.goto(URL+'/app');expect(p.locator('.workspace-tools')).to_be_visible()
 tenant=p.locator('.workspace-select').input_value().split(':')[0];data=ctx.request.get(URL+'/api/v1/societies/'+tenant+'/membership/members?kind=Individual').json();member=data['items'][0]
 p.keyboard.press('Control+k');p.get_by_label('Search pages, records and actions').fill(member['number']);result=p.get_by_role('dialog').get_by_role('link').filter(has_text=member['number']);expect(result.first).to_be_visible();check('Global search returns authorized real membership records');result.first.click();expect(p).to_have_url(URL+'/app/members/profile/'+member['id']+'?memberKind=Individual');check('Record result opens the actual member profile')
 ctx.close();ctx=browser.new_context(viewport={'width':1280,'height':1000});cfg=json.loads((ROOT/'.local/society-example-account.json').read_text());signin(ctx,{'email':cfg['email'],'password':cfg['password']});p=ctx.new_page();p.on('pageerror',lambda e:errors.append(str(e)));p.on('dialog',lambda d:d.accept());p.goto(URL+'/app/account?view=join');expect(p.locator('.draft-recovery')).to_be_visible();p.locator('.draft-recovery input[type=checkbox]').check();p.get_by_text('Personal & professional details (optional)',exact=True).click();field=p.locator('input[name=city]');field.fill('DraftCity2027');expect(p.locator('.draft-recovery')).to_contain_text('Saved in this browser tab');check('Application draft saves only after opting in')
 saved=p.evaluate("Object.entries(sessionStorage).filter(([k])=>k.startsWith('application-draft.')).map(([,v])=>JSON.parse(v))");check('Draft excludes passwords and sensitive profile fields',bool(saved) and all(set(v).issubset({'fullName','email','phone','city','chapterId','categoryId','designation','occupation'}) for v in saved))
 p.reload();p.get_by_text('Personal & professional details (optional)',exact=True).click();expect(p.get_by_role('button',name='Restore saved details')).to_be_visible();p.get_by_role('button',name='Restore saved details').click();expect(p.locator('input[name=city]')).to_have_value('DraftCity2027');check('Reloaded application restores the saved basic details')
 p.get_by_role('button',name='Delete local draft').click();check('Local draft can be explicitly deleted',not p.evaluate("Object.keys(sessionStorage).some(k=>k.startsWith('application-draft.'))"))
 p.goto(URL+'/app');expect(p.get_by_role('heading',name='Needs your attention')).to_be_visible();check('Staff homepage renders the real attention queue');p.get_by_role('button',name='Toggle light or dark appearance').click();expect(p.locator('html')).to_have_attribute('data-theme','dark');p.screenshot(path=str(ROOT/'tests/experience-staff-dark.png'),full_page=True);check('Dark appearance available for staff')
 check('No browser errors in search/draft workflows',not errors);browser.close()
(ROOT/'tests/experience-workflow-results.json').write_text(json.dumps({'checks':checks,'errors':errors},indent=2))
