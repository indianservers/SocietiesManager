"""Opt-in local example data. Names, chapter counts and categories come from JSON.

Creates a separate tenant, preserving the existing preview. No real invitations,
payments or provider messages are sent. Credentials stay in ignored .local/.
"""
import argparse
import http.cookiejar
import json
import secrets
import urllib.parse
import urllib.request
from pathlib import Path

import psycopg

ROOT = Path(__file__).resolve().parents[1]


class Client:
    def __init__(self, base):
        self.base = base
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

    def call(self, path, method='GET', body=None):
        headers = {'Content-Type': 'application/json'}
        if method != 'GET':
            headers['X-CSRF-TOKEN'] = self.call('/auth/csrf')['token']
        request = urllib.request.Request(self.base + path, method=method, headers=headers,
            data=json.dumps(body).encode() if body is not None else (b'' if method != 'GET' else None))
        with self.opener.open(request) as response:
            return json.loads(response.read() or 'null')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config', type=Path, default=ROOT / 'scripts/society-example.json')
    parser.add_argument('--output', type=Path, default=ROOT / '.local/society-example-account.json')
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding='utf-8'))
    dev = json.loads((ROOT / '.local/dev.json').read_text())
    preview = json.loads((ROOT / '.local/preview-account.json').read_text())
    base = 'http://127.0.0.1:5092/api/v1'
    owner, admin = Client(base), Client(base)
    admin.call('/auth/login', 'POST', {'email': preview['platformEmail'], 'password': preview['platformPassword']})
    # Save the tenant immediately after creation: reruns resume partially seeded fixtures.
    if args.output.exists():
        account = json.loads(args.output.read_text())
        if account['name'] != config['name']:
            raise SystemExit('Use a different --output for a different society example.')
        owner.call('/auth/login', 'POST', account)
    else:
        account = {'name': config['name'], 'email': 'example-' + secrets.token_hex(6) + '@example.test',
                   'password': secrets.token_urlsafe(24) + '!Aa1'}
        owner.call('/auth/signup', 'POST', {'email': account['email'], 'password': account['password'], 'fullName': 'Example Society Administrator'})
        with psycopg.connect(host='127.0.0.1', port=55439, dbname='assemblia', user='postgres', password=dev['password']) as db:
            user = db.execute('SELECT "Id" FROM "AspNetUsers" WHERE "Email"=%s', (account['email'],)).fetchone()[0]
            body = db.execute('SELECT "Body" FROM "Outbox" WHERE "Recipient"=%s ORDER BY "CreatedAt" DESC LIMIT 1', (account['email'],)).fetchone()[0]
        token = urllib.parse.parse_qs(urllib.parse.urlparse(body).query)['token'][0]
        owner.call('/auth/verify?userId=' + str(user) + '&token=' + urllib.parse.quote(token, safe=''), 'POST')
        owner.call('/auth/login', 'POST', account)
        application = owner.call('/societies/applications', 'POST', {**{k: config[k] for k in ['name', 'category', 'country', 'state', 'city']},
            'officialEmail': account['email'], 'contactNumber': '0000000000', 'address': 'Fictional local example data', 'applicantMobile': '0000000000'})
        account['tenant'] = admin.call('/societies/applications/' + application['id'] + '/review', 'POST', {'approve': True, 'note': 'Authorized local example fixture'})['tenantId']
        args.output.write_text(json.dumps(account, indent=2), encoding='utf-8')
    prefix = '/societies/' + account['tenant']
    owner.call(prefix + '/onboarding', 'PUT', {'step': 7, 'activate': True, 'data': json.dumps({'displayName': config['name'], 'purpose': 'Fictional example data for workflow testing'})})
    levels = owner.call(prefix + '/organizations/levels')
    chapter_level = next(level['id'] for level in levels if level['kind'] == 'Chapter')
    region_level = next(level['id'] for level in levels if level['kind'] == 'Region')
    units = owner.call(prefix + '/organizations/units?pageSize=100')['items']
    region = next((unit['id'] for unit in units if unit['code'] == 'EXAMPLE'), None)
    if not region:
        region = owner.call(prefix + '/organizations/units', 'POST', {'name': 'Example Region', 'code': 'EXAMPLE', 'levelId': region_level, 'parentId': None, 'status': 'Active', 'details': '{}'})['id']
    chapters = []
    for i, city in enumerate(config['chapters'], 1):
        code = f'EX-{i:02}'
        chapter = next((unit['id'] for unit in units if unit['code'] == code), None)
        if not chapter:
            chapter = owner.call(prefix + '/organizations/units', 'POST', {'name': city + ' Chapter', 'code': code, 'levelId': chapter_level, 'parentId': region, 'status': 'Active', 'details': json.dumps({'city': city, 'country': config['country'], 'description': 'Fictional example chapter'})})['id']
        chapters.append(chapter)
    categories = owner.call(prefix + '/membership/categories')
    for group in config['memberships']:
        category = next((cat['id'] for cat in categories if cat['name'] == group['category']), None)
        if not category:
            category = owner.call(prefix + '/membership/categories', 'POST', {'name': group['category'], 'description': 'Configurable example membership category', 'active': True})['id']
        for i in range(1, group['count'] + 1):
            number = f"{group['prefix']}-{i:04}"
            if owner.call(prefix + '/membership/members?search=' + number)['total']:
                continue
            owner.call(prefix + '/membership/members', 'POST', {'fullName': f"{group['holder']} {i:03}", 'chapterId': chapters[(i-1) % len(chapters)], 'categoryId': category,
                'number': number, 'status': 'Active', 'profile': json.dumps({'workEmail': f'{number.lower()}@example.test', 'professional': {'organization': f"{group['holder']} {i:03}", 'representativeName': f'Example Representative {i:03}', 'registrationNumber': number}}),
                'privacy': json.dumps({'directory': True, 'contacts': False, 'photo': False, 'professional': True})})
    summary = owner.call(prefix + '/membership/intelligence')
    assert summary['chapterCount'] == len(chapters)
    assert summary['total'] == sum(group['count'] for group in config['memberships'])
    print(json.dumps({'tenant': account['tenant'], 'chapters': summary['chapterCount'], 'memberships': summary['total'], 'categories': summary['categories']}, indent=2))


if __name__ == '__main__':
    main()
