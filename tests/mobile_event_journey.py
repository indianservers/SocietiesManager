"""Mobile approval -> RSVP -> registration -> PDF pass -> check-in journey."""
import datetime
import importlib
import json
import secrets
import sys
import urllib.parse
from pathlib import Path
import psycopg
from playwright.sync_api import sync_playwright, expect

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts'))
Client = importlib.import_module('seed-society-example').Client
account = json.loads((ROOT / '.local/society-example-account.json').read_text())
dev = json.loads((ROOT / '.local/dev.json').read_text())
dsn = dict(host='127.0.0.1', port=55439, dbname='assemblia', user='postgres', password=dev['password'])
base = 'http://127.0.0.1:5092/api/v1'
owner = Client(base)
owner.call('/auth/login', 'POST', account)
prefix = '/societies/' + account['tenant']
member_file = ROOT / '.local/mobile-participant.json'
participant = Client(base)
if member_file.exists():
    member = json.loads(member_file.read_text())
else:
    member = {'email': 'participant-' + secrets.token_hex(5) + '@example.test', 'password': secrets.token_urlsafe(24) + '!Aa1'}
    participant.call('/auth/signup', 'POST', {**member, 'fullName': 'Example Mobile Participant'})
    with psycopg.connect(**dsn) as db:
        user = db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s', (member['email'],)).fetchone()[0]
        body = db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s ORDER BY "CreatedAt" DESC LIMIT 1', (member['email'],)).fetchone()[0]
    token = urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
    participant.call('/auth/verify?userId=' + str(user) + '&token=' + urllib.parse.quote(token, safe=''), 'POST')
    member['user'] = str(user)
    member_file.write_text(json.dumps(member))
participant.call('/auth/login', 'POST', member)
with psycopg.connect(**dsn) as db:
    event = db.execute('SELECT "Id" FROM "CommunityEvent" WHERE "TenantId"=%s AND "Title" LIKE %s AND "Status"=%s ORDER BY "CreatedAt" DESC LIMIT 1', (account['tenant'], 'Mobile workflow gathering %', 'Pending Approval')).fetchone()
assert event, 'Run mobile_workflows.py to create a pending event first'
event_id = str(event[0])
record = owner.call(prefix + '/membership/members?search=IND-0001')['items'][0]
with psycopg.connect(**dsn) as db:
    linked = db.execute('SELECT "UserId" FROM "Persons" WHERE "Id"=%s', (record['personId'],)).fetchone()[0]
if linked is None:
    invite = owner.call(prefix + '/membership/invitations', 'POST', {'chapterId': record['chapterId'], 'categoryId': record['categoryId'], 'personId': record['personId'], 'email': member['email'], 'days': 1, 'usageLimit': 1})
    participant.call('/membership/invitations/accept', 'POST', {'token': urllib.parse.parse_qs(urllib.parse.urlparse(invite['url']).query)['invitation'][0]})
role = next(r for r in owner.call(prefix + '/roles') if r['name'] == 'Society President')
assignment = owner.call(prefix + '/role-assignments', 'POST', {'userId': member['user'], 'roleId': role['id'], 'unitId': None, 'startsAt': (datetime.datetime.now(datetime.timezone.utc)-datetime.timedelta(days=1)).isoformat(), 'endsAt': None})['id']
checks, errors = [], []


def check(name, condition=True):
    assert condition, name
    checks.append(name)
    print('PASS', name, flush=True)


def login(context, credentials):
    url = 'http://127.0.0.1:5176/api/v1'
    token = context.request.get(url + '/auth/csrf').json()['token']
    assert context.request.post(url + '/auth/login', data=credentials, headers={'X-CSRF-TOKEN': token}).ok


with sync_playwright() as p:
    browser = p.chromium.launch()
    try:
        context = browser.new_context(viewport={'width': 390, 'height': 844}, is_mobile=True, has_touch=True)
        login(context, member)
        page = context.new_page()
        page.on('pageerror', lambda error: errors.append(str(error)))
        event_url = 'http://127.0.0.1:5176/app/community/events/' + event_id
        page.goto(event_url)
        page.wait_for_load_state('networkidle')
        page.get_by_label('Review note', exact=True).fill('Independent review during synthetic mobile journey')
        page.get_by_role('button', name='Approve', exact=True).click()
        expect(page.locator('main .badge').filter(has_text='Published').first).to_be_visible()
        check('Independent officer approves event on mobile')
        owner.call(prefix + '/role-assignments/' + assignment, 'DELETE')
        assignment = None
        page.reload()
        page.wait_for_load_state('networkidle')
        check('Ordinary member has no approval or check-in controls', page.get_by_role('button', name='Approve', exact=True).count() == 0 and page.get_by_role('button', name='Check-in', exact=True).count() == 0)
        page.get_by_role('button', name='Going', exact=True).click()
        expect(page.get_by_role('button', name='Going', exact=True)).to_have_class('active')
        check('Member RSVP persists')
        page.get_by_role('button', name='Register for this event', exact=True).click()
        expect(page.get_by_role('button', name='Issue / reissue digital pass', exact=True)).to_be_visible()
        check('Member registration confirms a seat')
        with page.expect_response(lambda r: r.request.method == 'POST' and '/passes?' in r.url) as response:
            page.get_by_role('button', name='Issue / reissue digital pass', exact=True).click()
        issued = response.value.json()
        dialog = page.get_by_role('dialog', name='Your digital event pass')
        expect(dialog).to_contain_text(account['name'])
        expect(dialog.get_by_alt_text('Opaque event verification QR code')).to_be_visible()
        with page.expect_download() as download:
            dialog.get_by_role('button', name='Download printable PDF', exact=True).click()
        download.value.save_as(str(ROOT / 'tests/mobile-event-pass.pdf'))
        check('Tenant-branded QR pass renders and PDF downloads')
        page.screenshot(path=str(ROOT / 'tests/mobile-member-pass.png'), full_page=True)
        context.close()
        context = browser.new_context(viewport={'width': 390, 'height': 844}, is_mobile=True, has_touch=True)
        login(context, account)
        page = context.new_page()
        page.goto(event_url)
        page.wait_for_load_state('networkidle')
        page.get_by_role('button', name='Check-in', exact=True).click()
        page.get_by_label('Pass verification reference', exact=True).fill(issued['token'])
        page.get_by_role('button', name='Verify and check in', exact=True).click()
        expect(page.locator('.checkin-result')).to_contain_text('Valid')
        check('Authorized operator checks in the opaque pass')
        page.get_by_role('button', name='Verify and check in', exact=True).click()
        expect(page.locator('.checkin-result')).to_contain_text('Already Checked In')
        check('Duplicate check-in is rejected without counting twice')
        check('No uncaught browser errors', not errors)
    finally:
        if assignment:
            owner.call(prefix + '/role-assignments/' + assignment, 'DELETE')
        browser.close()
        (ROOT / 'tests/mobile-event-journey-results.json').write_text(json.dumps({'passed': len(checks), 'checks': checks, 'pageErrors': errors, 'event': event_id}, indent=2))
