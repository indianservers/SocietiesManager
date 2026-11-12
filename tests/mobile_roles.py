"""Local Chromium mobile role audit against the opt-in example tenant.

Uses one synthetic account with temporary role assignments; cleans assignments
after every role. No real provider, gateway or messaging calls are made.
"""
import json
import sys
import secrets
import uuid
from pathlib import Path
import urllib.parse

import psycopg
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts'))
from importlib import import_module
Client = import_module('seed-society-example').Client

account = json.loads((ROOT / '.local/society-example-account.json').read_text())
dev = json.loads((ROOT / '.local/dev.json').read_text())
dsn = dict(host='127.0.0.1', port=55439, dbname='assemblia', user='postgres', password=dev['password'])
base = 'http://127.0.0.1:5092/api/v1'
owner = Client(base)
owner.call('/auth/login', 'POST', account)
prefix = '/societies/' + account['tenant']
roles = owner.call(prefix + '/roles')
chapters = owner.call(prefix + '/organizations/units?kind=Chapter&pageSize=100')['items']
region = owner.call(prefix + '/organizations/units?kind=Region&pageSize=100')['items'][0]['id']
credentials_path = ROOT / '.local/mobile-role-account.json'
client = Client(base)
if credentials_path.exists():
    credentials = json.loads(credentials_path.read_text())
else:
    credentials = {'email': 'mobile-' + secrets.token_hex(5) + '@example.test', 'password': secrets.token_urlsafe(24) + '!Aa1'}
    client.call('/auth/signup', 'POST', {**credentials, 'fullName': 'Mobile Role Test Account'})
    with psycopg.connect(**dsn) as db:
        user = db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s', (credentials['email'],)).fetchone()[0]
        body = db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s ORDER BY "CreatedAt" DESC LIMIT 1', (credentials['email'],)).fetchone()[0]
    token = urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
    client.call('/auth/verify?userId=' + str(user) + '&token=' + urllib.parse.quote(token, safe=''), 'POST')
    credentials['user'] = str(user)
    credentials_path.write_text(json.dumps(credentials))
with psycopg.connect(**dsn) as db:
    db.execute('INSERT INTO "Links" ("TenantId","UserId") VALUES (%s,%s) ON CONFLICT DO NOTHING', (account['tenant'], credentials['user']))

results, errors = [], []
routes = ['/app', '/app/members', '/app/members/governance', '/app/organization', '/app/members/operations',
          '/app/community', '/app/community/events', '/app/service/projects', '/app/communications', '/app/finance', '/app/account', '/app/studio', '/app/settings']


def login(context, creds):
    csrf = context.request.get('http://127.0.0.1:5176/api/v1/auth/csrf').json()['token']
    response = context.request.post('http://127.0.0.1:5176/api/v1/auth/login', data={'email': creds['email'], 'password': creds['password']}, headers={'X-CSRF-TOKEN': csrf})
    assert response.ok, response.status


def inspect(page, role, route, width):
    if route == '/app' or page.url == 'about:blank':
        # Reload between roles so fresh permissions are discovered.
        page.goto('http://127.0.0.1:5176' + route)
    else:
        # Exercise actual SPA routing while retaining the current role session.
        page.evaluate('path => {history.pushState(null,"",path);dispatchEvent(new PopStateEvent("popstate"));}', route)
    page.wait_for_load_state('networkidle')
    try:
        page.get_by_role('button', name='Toggle navigation', exact=True).wait_for(timeout=15000)
    except Exception:
        page.screenshot(path=str(ROOT / 'tests/mobile-role-failure.png'), full_page=True)
        print('FAILED SCREEN', role, route, page.locator('body').inner_text()[:2000], flush=True)
        raise
    page.locator('.skeleton').first.wait_for(state='hidden', timeout=15000) if page.locator('.skeleton').count() else None
    metrics = page.evaluate('''() => ({width:innerWidth, scroll:document.documentElement.scrollWidth,
      overflow:[...document.querySelectorAll('main *')].filter(e=>{const r=e.getBoundingClientRect();return r.width>0&&r.right>innerWidth+2&&getComputedStyle(e).position!=='fixed'&&!e.closest('.table-wrap,.studio-table-wrap,.finance-table-wrap,.table-scroll')}).slice(0,6).map(e=>({tag:e.tagName,cls:e.className,text:e.textContent.slice(0,65)}))})''')
    alerts = page.locator('main [role="alert"]').all_text_contents()
    result = {'role': role, 'route': route, 'width': width, 'scrollWidth': metrics['scroll'], 'alerts': alerts, 'overflow': metrics['overflow']}
    results.append(result)
    print(json.dumps(result), flush=True)


with sync_playwright() as p:
    browser = p.chromium.launch()
    try:
        context = browser.new_context(viewport={'width': 390, 'height': 844}, is_mobile=True, has_touch=True)
        login(context, account)
        page = context.new_page()
        page.on('pageerror', lambda error: errors.append(str(error)))
        for width in [360, 390, 768]:
            page.set_viewport_size({'width': width, 'height': 844})
            for route in routes:
                inspect(page, 'Society Administrator', route, width)
            if width == 390:
                page.screenshot(path=str(ROOT / 'tests/mobile-society-admin.png'), full_page=True)
        context.close()
        context = browser.new_context(viewport={'width': 390, 'height': 844}, is_mobile=True, has_touch=True)
        login(context, credentials)
        page = context.new_page()
        page.on('pageerror', lambda error: errors.append(str(error)))
        for role in roles:
            if role['name'] == 'Society Administrator':
                continue
            assignment = str(uuid.uuid4())
            unit = chapters[0]['id'] if role['name'].startswith('Chapter') or role['name'] == 'Member Directory' else region if role['name'].startswith(('Regional', 'District', 'Zone')) else None
            with psycopg.connect(**dsn) as db:
                db.execute('INSERT INTO "Assignments" ("Id","TenantId","UserId","RoleId","UnitId","StartsAt") VALUES (%s,%s,%s,%s,%s,now()-interval \'1 day\')', (assignment, account['tenant'], credentials['user'], role['id'], unit))
            try:
                for route in routes:
                    inspect(page, role['name'], route, 390)
            finally:
                with psycopg.connect(**dsn) as db:
                    db.execute('DELETE FROM "Assignments" WHERE "Id"=%s AND "UserId"=%s', (assignment, credentials['user']))
        context.close()
        preview = json.loads((ROOT / '.local/preview-account.json').read_text())
        context = browser.new_context(viewport={'width': 390, 'height': 844}, is_mobile=True, has_touch=True)
        login(context, {'email': preview['platformEmail'], 'password': preview['platformPassword']})
        page = context.new_page()
        inspect(page, 'Platform Administrator', '/app/platform', 390)
    finally:
        browser.close()
        (ROOT / 'tests/mobile-role-results.json').write_text(json.dumps({'screens': len(results), 'roles': sorted(set(r['role'] for r in results)), 'pageErrors': errors, 'results': results}, indent=2))
assert not errors, errors
assert not [r for r in results if r['scrollWidth'] > r['width'] + 2], 'Mobile page overflow detected'
assert not [r for r in results if r['alerts']], 'A role workflow displayed a loading or API error'
assert set(r['role'] for r in results) == {r['name'] for r in roles} | {'Platform Administrator'}, 'Every role must be exercised'
print('PASS', len(results), 'mobile screens across', len(set(r['role'] for r in results)), 'roles', flush=True)
