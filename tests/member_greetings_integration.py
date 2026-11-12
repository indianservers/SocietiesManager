"""Focused worker test against the existing isolated assemblia_test database."""
import datetime, json, os, subprocess, time
from pathlib import Path
import psycopg

root = Path(__file__).resolve().parents[1]
config = json.loads((root / '.local/dev.json').read_text())
dsn = f"host=127.0.0.1 port=55439 dbname=assemblia_test user=postgres password={config['password']}"
env = {**os.environ, 'ConnectionStrings__Database': f"Host=127.0.0.1;Port=55439;Database=assemblia_test;Username=postgres;Password={config['password']}", 'ASPNETCORE_ENVIRONMENT': 'Development', 'Logging__LogLevel__Default': 'Warning'}
dll = str(root / 'backend/bin/Debug/net10.0/Society.Api.dll')
server = None
with psycopg.connect(dsn, autocommit=True) as db:
    tenant, author = db.execute('''SELECT a."TenantId",a."UserId" FROM "Assignments" a JOIN "GovernanceRoles" r ON r."Id"=a."RoleId" JOIN "Tenants" t ON t."Id"=a."TenantId" WHERE a."UnitId" IS NULL AND a."RevokedAt" IS NULL AND t."Status"='Active' AND r."Name"='Society Administrator' AND EXISTS(SELECT 1 FROM "Memberships" m JOIN "Persons" p ON p."Id"=m."PersonId" WHERE m."TenantId"=a."TenantId" AND m."Status"='Active' AND p."UserId" IS NOT NULL AND EXISTS(SELECT 1 FROM "CommunicationPreference" cp WHERE cp."TenantId"=m."TenantId" AND cp."UserId"=p."UserId")) LIMIT 1''').fetchone()
    member, user, profile, privacy = db.execute('''SELECT m."Id",p."UserId",m."Profile",m."Privacy" FROM "Memberships" m JOIN "Persons" p ON p."Id"=m."PersonId" WHERE m."TenantId"=%s AND m."Status"='Active' AND p."UserId" IS NOT NULL AND (m."EndsAt" IS NULL OR m."EndsAt">now()) AND EXISTS(SELECT 1 FROM "CommunicationPreference" cp WHERE cp."TenantId"=m."TenantId" AND cp."UserId"=p."UserId") LIMIT 1''', (tenant,)).fetchone()
    pref = db.execute('SELECT "InApp","QuietStart","QuietEnd" FROM "CommunicationPreference" WHERE "TenantId"=%s AND "UserId"=%s', (tenant,user)).fetchone()
    assert pref, 'Run the communication integration suite first to create preferences'
    try:
        today = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=5,minutes=30))).date()
        data = dict(profile) if isinstance(profile,dict) else json.loads(profile)
        data.update(dateOfBirth=f'1990-{today:%m-%d}', anniversary=f'2010-{today:%m-%d}')
        db.execute('UPDATE "Memberships" SET "Profile"=%s::jsonb,"Privacy"=%s::jsonb WHERE "Id"=%s', (json.dumps(data),json.dumps({'communication':True}),member))
        db.execute('UPDATE "CommunicationPreference" SET "InApp"=true,"QuietStart"=0,"QuietEnd"=0 WHERE "TenantId"=%s AND "UserId"=%s',(tenant,user))
        for _ in range(2):
            result=subprocess.run(['dotnet',dll,'--setup-member-greetings'],cwd=root/'backend',env={**env,'GREETINGS_TENANT':str(tenant),'GREETINGS_AUTHOR':str(author)},capture_output=True,text=True)
            assert result.returncode==0,result.stdout+result.stderr
        rules=db.execute('''SELECT "Id","Trigger" FROM "CommunicationAutomation" WHERE "TenantId"=%s AND "Trigger" IN ('MemberBirthday','WeddingAnniversary') AND "Channel"='InApp' AND "UnitId" IS NULL''',(tenant,)).fetchall()
        assert len(rules)==2,'Setup must be idempotent'
        # A fresh source date is guaranteed by removing only this isolated test member's prior executions.
        keys=[str(today)+':'+str(member)]
        db.execute('DELETE FROM "AutomationExecution" WHERE "RuleId"=ANY(%s) AND "SourceKey"=ANY(%s)',([r[0] for r in rules],keys))
        started=datetime.datetime.now(datetime.timezone.utc)
        with (root/'.local/greetings-test.log').open('w') as log:
            server=subprocess.Popen(['dotnet',dll,'--urls','http://127.0.0.1:5095'],cwd=root/'backend',env=env,stdout=log,stderr=log)
            def delivered():
                return db.execute('''SELECT count(*) FROM "Outbox" o JOIN "CommunicationCampaign" c ON c."Id"=o."CampaignId" JOIN "AutomationExecution" e ON e."CampaignId"=c."Id" WHERE e."RuleId"=ANY(%s) AND e."SourceKey"=%s AND o."MembershipId"=%s AND o."Status"='Delivered' AND o."Body" NOT LIKE '%%{{%%' AND c."Status"!='Pending Approval' ''',([r[0] for r in rules],keys[0],member)).fetchone()[0]
            deadline=time.monotonic()+55
            while delivered()!=2 and time.monotonic()<deadline: time.sleep(.3)
            assert delivered()==2,'Both personalized greetings must be delivered automatically'
            db.execute('UPDATE "CommunicationAutomation" SET "LastRun"=NULL WHERE "Id"=ANY(%s)',([r[0] for r in rules],))
            time.sleep(3)
            assert delivered()==2,'A second worker pass must not duplicate greetings'
        print('PASS: idempotent setup; birthday and wedding anniversary delivered with rendered names; no duplicate delivery.')
    finally:
        if server: server.terminate(); server.wait(timeout=15)
        db.execute('UPDATE "Memberships" SET "Profile"=%s::jsonb,"Privacy"=%s::jsonb WHERE "Id"=%s',(json.dumps(profile) if isinstance(profile,dict) else profile,json.dumps(privacy) if isinstance(privacy,dict) else privacy,member))
        db.execute('UPDATE "CommunicationPreference" SET "InApp"=%s,"QuietStart"=%s,"QuietEnd"=%s WHERE "TenantId"=%s AND "UserId"=%s',(*pref,tenant,user))
        db.execute('''UPDATE "CommunicationAutomation" SET "Enabled"=false WHERE "TenantId"=%s AND "Trigger" IN ('MemberBirthday','WeddingAnniversary')''',(tenant,))
