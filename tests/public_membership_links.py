import json
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
ROOT=Path.cwd();checks=[]
with sync_playwright() as pw:
 b=pw.chromium.launch();page=b.new_page(viewport={'width':1366,'height':900});expect.set_options(timeout=30000)
 for path in ['https://aimersociety.com/','https://aimersociety.com/register/']:
  page.goto(path,wait_until='domcontentloaded',timeout=60000);section=page.locator('#aimer-membership-payments');expect(section).to_be_visible();assert section.count()==1;expect(section.locator('a[href="https://rzp.io/l/AIMERSIndividualFee"]')).to_be_visible();expect(section.locator('a[href="https://rzp.io/l/AIMERSEduMembershipFee"]')).to_be_visible();expect(section.locator('a[href="https://members.aimersociety.com/"]')).to_be_visible();checks.append(path);page.screenshot(path=str(ROOT/'tests'/('public-membership-'+('register' if 'register' in path else 'home')+'.png')),full_page=True)
  page.set_viewport_size({'width':390,'height':844});assert section.evaluate('e=>e.getBoundingClientRect().width')<=390;page.set_viewport_size({'width':1366,'height':900})
 b.close()
(ROOT/'tests/public-membership-links-results.json').write_text(json.dumps({'checks':checks},indent=2));print('PASS main website Home and Register pages show both payment links and portal entry points')
