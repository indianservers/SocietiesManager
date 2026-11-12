"""Real mobile browser mutations in the opt-in example tenant (no providers)."""
import datetime
import importlib
import json
import sys
import uuid
from pathlib import Path
from playwright.sync_api import sync_playwright, expect

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts'))
Client = importlib.import_module('seed-society-example').Client
account = json.loads((ROOT / '.local/society-example-account.json').read_text())
api = Client('http://127.0.0.1:5092/api/v1')
api.call('/auth/login', 'POST', account)
prefix = '/societies/' + account['tenant']
checks = []


def check(name, condition=True):
    assert condition, name
    checks.append(name)
    print('PASS', name, flush=True)


with sync_playwright() as p:
    browser = p.chromium.launch()
    context = browser.new_context(viewport={'width': 390, 'height': 844}, is_mobile=True, has_touch=True, timezone_id='Asia/Kolkata')
    base = 'http://127.0.0.1:5176'
    csrf = context.request.get(base + '/api/v1/auth/csrf').json()['token']
    assert context.request.post(base + '/api/v1/auth/login', data=account, headers={'X-CSRF-TOKEN': csrf}).ok
    page = context.new_page()
    errors = []
    page.on('pageerror', lambda error: errors.append(str(error)))
    try:
        page.goto(base + '/app/members?view=directory')
        page.wait_for_load_state('networkidle')
        page.get_by_role('button', name='Toggle navigation', exact=True).click()
        expect(page.locator('.sidebar')).to_have_class('sidebar open')
        expect(page.locator('.sidebar .brand')).to_contain_text(account['name'])
        page.get_by_role('link', name='Home', exact=True).click()
        expect(page.locator('.sidebar')).to_have_class('sidebar')
        check('Mobile navigation opens, shows tenant branding, and closes after navigation')
        for number in ['INST-0001', 'IND-0001', 'CORP-0001']:
            member = api.call(prefix + '/membership/members?search=' + number)['items'][0]
            before = api.call(prefix + '/membership/members/' + member['id'])
            page.goto(base + '/app/members/profile/' + member['id'])
            page.wait_for_load_state('networkidle')
            page.get_by_role('button', name='Edit profile', exact=True).click()
            dialog = page.get_by_role('dialog')
            dialog.get_by_text('Personal & professional details', exact=True).click()
            value = before['profile'].get('professional', {}).get('representativeName', '')
            dialog.get_by_label('Authorized representative name', exact=True).fill(value + ' · mobile verified')
            dialog.get_by_role('button', name='Save member', exact=True).click()
            expect(dialog).not_to_be_visible()
            updated = api.call(prefix + '/membership/members/' + member['id'])
            check(number + ' saves organizational/contact metadata', updated['profile']['professional']['representativeName'] == value + ' · mobile verified')
            check(number + ' preserves membership category and status', updated['member']['categoryId'] == before['member']['categoryId'] and updated['member']['status'] == before['member']['status'])
            page.reload()
            page.wait_for_load_state('networkidle')
            page.get_by_role('button', name='Edit profile', exact=True).click()
            dialog = page.get_by_role('dialog')
            dialog.get_by_text('Personal & professional details', exact=True).click()
            expect(dialog.get_by_label('Authorized representative name', exact=True)).to_have_value(value + ' · mobile verified')
            dialog.get_by_label('Authorized representative name', exact=True).fill(value)
            dialog.get_by_role('button', name='Save member', exact=True).click()
            expect(dialog).not_to_be_visible()
            check(number + ' reloads persisted changes and restores original data')
        title = 'Mobile workflow gathering ' + uuid.uuid4().hex[:8]
        page.goto(base + '/app/community/events')
        page.wait_for_load_state('networkidle')
        page.get_by_role('button', name='Create event', exact=True).click()
        dialog = page.get_by_role('dialog')
        dialog.get_by_label('Event title', exact=True).fill(title)
        dialog.locator('textarea').fill('Fictional event created by the automated mobile workflow test.')
        dialog.get_by_role('button', name='Continue', exact=True).click()
        expect(dialog.get_by_label('Starts', exact=True)).to_be_visible()
        now = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=5, minutes=30))).replace(tzinfo=None, second=0, microsecond=0)
        dialog.get_by_label('Starts', exact=True).fill((now + datetime.timedelta(hours=2)).isoformat(timespec='minutes'))
        dialog.get_by_label('Ends', exact=True).fill((now + datetime.timedelta(hours=4)).isoformat(timespec='minutes'))
        dialog.get_by_label('Time zone (IANA name)', exact=True).fill('Asia/Kolkata')
        dialog.get_by_label('Venue', exact=True).fill('Example conference hall')
        dialog.get_by_role('button', name='Continue', exact=True).click()
        expect(dialog.get_by_label('Organizing organization')).to_be_visible()
        dialog.get_by_label('Organizing organization').select_option(label='Vijayawada Chapter')
        dialog.get_by_label('Visibility').select_option('Society')
        dialog.get_by_role('button', name='Continue', exact=True).click()
        expect(dialog.get_by_label('Registration opens', exact=True)).to_be_visible()
        dialog.get_by_label('Registration opens', exact=True).fill((now - datetime.timedelta(hours=1)).isoformat(timespec='minutes'))
        dialog.get_by_label('Registration closes', exact=True).fill((now + datetime.timedelta(hours=1)).isoformat(timespec='minutes'))
        dialog.get_by_label('Capacity (people)', exact=True).fill('25')
        for _ in range(2):
            dialog.get_by_role('button', name='Continue', exact=True).click()
            page.wait_for_timeout(150)
        check('All six event wizard steps remain usable on mobile', dialog.get_by_role('button', name='Submit event', exact=True).is_enabled())
        with page.expect_response(lambda response: response.request.method == 'POST' and response.url.endswith('/community/events')) as response_info:
            dialog.get_by_role('button', name='Submit event', exact=True).click()
        response = response_info.value
        check('Mobile event submission reaches API successfully', response.ok)
        event = response.json()
        expect(dialog).not_to_be_visible()
        detail = api.call(prefix + '/community/events/' + event['id'])
        check('Event date, chapter, capacity and IANA timezone survive persistence', detail['eventData']['title'] == title and detail['eventData']['capacity'] == 25 and detail['eventData']['timeZone'] == 'Asia/Kolkata')
        page.goto(base + '/app/community/events/' + event['id'])
        page.wait_for_load_state('networkidle')
        expect(page.locator('main')).to_contain_text(title)
        check('Submitted event opens from its detail URL')
        page.screenshot(path=str(ROOT / 'tests/mobile-event-workflow.png'), full_page=True)
        page.goto(base + '/app/settings')
        page.wait_for_load_state('networkidle')
        for width in [360, 390, 768]:
            page.set_viewport_size({'width': width, 'height': 844})
            check(f'Settings tabs remain inside {width}px viewport', page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
        check('Mobile workflows have no uncaught JavaScript errors', not errors)
    finally:
        page.screenshot(path=str(ROOT / 'tests/mobile-workflow-last.png'), full_page=True)
        browser.close()
        (ROOT / 'tests/mobile-workflow-results.json').write_text(json.dumps({'passed': len(checks), 'checks': checks, 'pageErrors': errors}, indent=2))
