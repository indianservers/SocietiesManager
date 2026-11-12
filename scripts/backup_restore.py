"""Encrypted PostgreSQL/object/key backup and isolated restore verification.

Secrets are environment-only: PGHOST/PGPORT/PGUSER/PGPASSWORD/PGDATABASE,
BACKUP_ENCRYPTION_KEY (base64 32 random bytes). No credentials are printed.
Requires matching PostgreSQL client tools, psycopg and cryptography.
"""
from __future__ import annotations
import argparse,base64,datetime,hashlib,json,os,secrets,shutil,subprocess,tarfile,tempfile,time,uuid
from pathlib import Path
import psycopg
from psycopg import sql
from cryptography.hazmat.primitives.ciphers import Cipher,algorithms,modes

MAGIC=b'ASSEMBLIA-BACKUP-1\n'
def key():
    encoded=Path(os.environ['BACKUP_ENCRYPTION_KEY_FILE']).read_text().strip() if os.environ.get('BACKUP_ENCRYPTION_KEY_FILE') else os.environ['BACKUP_ENCRYPTION_KEY']
    value=base64.b64decode(encoded,validate=True)
    if len(value)!=32:raise ValueError('BACKUP_ENCRYPTION_KEY must be base64-encoded 32 random bytes')
    return value
def tool(name):
    suffix='.exe' if os.name=='nt' else ''
    candidate=Path(os.environ.get('PG_BIN',''))/(name+suffix)
    found=str(candidate) if candidate.is_file() else shutil.which(name)
    if not found:raise RuntimeError('Required PostgreSQL client tool is unavailable: '+name)
    return found
def run(command):
    result=subprocess.run(command,env=os.environ,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if result.returncode:raise RuntimeError('PostgreSQL operation failed; inspect protected operator logs (exit '+str(result.returncode)+')')
    return result.stdout
def digest(path):
    h=hashlib.sha256()
    with open(path,'rb') as f:
        for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
    return h.hexdigest()
def encrypt(source,target):
    nonce=secrets.token_bytes(12);cipher=Cipher(algorithms.AES(key()),modes.GCM(nonce)).encryptor();cipher.authenticate_additional_data(MAGIC)
    with open(source,'rb') as plain,open(target,'wb') as out:
        out.write(MAGIC+nonce)
        for chunk in iter(lambda:plain.read(1048576),b''):out.write(cipher.update(chunk))
        out.write(cipher.finalize());out.write(cipher.tag)
    os.chmod(target,0o600)
def decrypt(source,target):
    with open(source,'rb') as data:
        if data.read(len(MAGIC))!=MAGIC:raise ValueError('Unsupported encrypted backup format')
        nonce=data.read(12);data.seek(-16,2);tag=data.read(16);remaining=data.tell()-16-len(MAGIC)-12;data.seek(len(MAGIC)+12)
        cipher=Cipher(algorithms.AES(key()),modes.GCM(nonce,tag)).decryptor();cipher.authenticate_additional_data(MAGIC)
        with open(target,'wb') as out:
            while remaining:
                chunk=data.read(min(1048576,remaining))
                if not chunk:raise ValueError('Truncated encrypted backup')
                remaining-=len(chunk);out.write(cipher.update(chunk))
            out.write(cipher.finalize())
def collect_manifest(root):
    return {str(p.relative_to(root)).replace('\\','/'):{'bytes':p.stat().st_size,'sha256':digest(p)} for p in root.rglob('*') if p.is_file() and p.name!='manifest.json'}
def backup(destination,objects,keys,retention_days=7):
    started=time.perf_counter();destination.mkdir(parents=True,exist_ok=True);os.chmod(destination,0o700)
    for label,path in [('objects',objects),('keys',keys)]:
        if not path.is_dir():raise ValueError('Explicit existing '+label+' directory is required')
        if path.is_symlink() or any(p.is_symlink() for p in path.rglob('*')):raise ValueError('Backup source links are not supported; select explicit directories')
    with tempfile.TemporaryDirectory(prefix='assemblia-backup-',dir=destination) as temp:
        root=Path(temp);os.chmod(root,0o700)
        with psycopg.connect() as source_db:
            source_db.execute('SET TRANSACTION ISOLATION LEVEL REPEATABLE READ, READ ONLY')
            snapshot=source_db.execute('SELECT pg_export_snapshot()').fetchone()[0]
            source_tables=source_db.execute("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename").fetchall()
            source_counts={name:source_db.execute(sql.SQL('SELECT count(*) FROM {}').format(sql.Identifier(name))).fetchone()[0] for (name,) in source_tables}
            run([tool('pg_dump'),'--snapshot',snapshot,'--format=custom','--no-owner','--no-privileges','--file',str(root/'database.dump')])
        shutil.copytree(objects,root/'objects');shutil.copytree(keys,root/'keys');manifest={'createdAt':datetime.datetime.now(datetime.timezone.utc).isoformat(),'database':os.environ['PGDATABASE'],'tableCounts':source_counts,'files':collect_manifest(root),'consistency':'Database snapshot plus object/key copy. Run against a quiesced application or immutable object store; mutable external writes must be coordinated.'};(root/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
        archive=root/'archive.tar'
        with tarfile.open(archive,'w') as tar:
            for name in ['database.dump','objects','keys','manifest.json']:tar.add(root/name,arcname=name,recursive=True)
        output=destination/('assemblia-'+datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')+'-'+uuid.uuid4().hex[:8]+'.abk');encrypt(archive,output)
        status={'operation':'backup','file':output.name,'bytes':output.stat().st_size,'sha256':digest(output),'elapsedSeconds':round(time.perf_counter()-started,3),'at':manifest['createdAt'],'encrypted':'AES-256-GCM','restoreVerified':False};(destination/'last-backup.json').write_text(json.dumps(status,indent=2),encoding='utf-8')
    # Retention applies only to this tool's files after a new successful backup.
    cutoff=time.time()-max(1,retention_days)*86400
    for old in destination.glob('assemblia-*.abk'):
        if old.resolve().parent!=destination.resolve():raise ValueError('Retention target escaped the backup directory')
        if old.stat().st_mtime<cutoff:old.unlink()
    return output,status
def restore_verify(source,destination):
    started=time.perf_counter();destination.mkdir(parents=True,exist_ok=True);os.chmod(destination,0o700)
    database='assemblia_restore_'+uuid.uuid4().hex[:12];created=False
    # Connection passwords remain in environment; the database identifier is generated here.
    conninfo={'host':os.environ.get('PGHOST','127.0.0.1'),'port':os.environ.get('PGPORT','5432'),'user':os.environ.get('PGUSER','postgres'),'password':os.environ['PGPASSWORD'],'dbname':'postgres'}
    with tempfile.TemporaryDirectory(prefix='assemblia-restore-',dir=destination) as temp:
        root=Path(temp);archive=root/'archive.tar';decrypt(source,archive)
        with tarfile.open(archive) as tar:
            for entry in tar.getmembers():
                target=(root/entry.name).resolve()
                if root.resolve() not in target.parents or entry.issym() or entry.islnk() or not(entry.isfile() or entry.isdir()):raise ValueError('Unsafe backup archive entry')
            tar.extractall(root,filter='data')
        manifest=json.loads((root/'manifest.json').read_text(encoding='utf-8'));actual=collect_manifest(root);actual.pop('archive.tar',None)
        if actual!=manifest['files']:raise ValueError('Restored file hash inventory differs from the authenticated manifest')
        with psycopg.connect(**conninfo,autocommit=True) as admin:
            try:
                admin.execute(sql.SQL('CREATE DATABASE {}').format(sql.Identifier(database)));created=True
                run([tool('pg_restore'),'--exit-on-error','--no-owner','--no-privileges','--dbname',database,str(root/'database.dump')])
                with psycopg.connect(**{**conninfo,'dbname':database}) as db:
                    tables=db.execute("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename").fetchall();counts={name:db.execute(sql.SQL('SELECT count(*) FROM {}').format(sql.Identifier(name))).fetchone()[0] for (name,) in tables}
                    if counts!=manifest['tableCounts']:raise ValueError('Restored table counts differ from the exported database snapshot')
                    balance=db.execute('SELECT count(*) FROM (SELECT j."Id" FROM "JournalEntry" j JOIN "JournalLine" l ON l."JournalId"=j."Id" WHERE j."Posted" GROUP BY j."Id" HAVING sum(l."Debit")<>sum(l."Credit")) invalid').fetchone()[0] if 'JournalEntry' in counts else 0
                    if balance:raise ValueError('Restored posted journals are unbalanced')
                    migrations=db.execute('SELECT count(*) FROM "__EFMigrationsHistory"').fetchone()[0] if '__EFMigrationsHistory' in counts else 0
                status={'operation':'restore-verification','passed':True,'source':source.name,'sourceSha256':digest(source),'isolatedDatabase':database,'databaseDroppedAfterVerification':True,'tables':counts,'migrations':migrations,'unbalancedPostedJournals':balance,'objectFiles':sum(name.startswith('objects/') for name in manifest['files']),'keyFiles':sum(name.startswith('keys/') for name in manifest['files']),'allFileHashesMatched':True,'allTableCountsMatched':True,'elapsedSeconds':round(time.perf_counter()-started,3),'at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'Isolated restore drill; no replacement of the running database or object store. Production key decryptability and deployment RPO/RTO require a deployment-specific drill.'}
            finally:
                if created:
                    assert database.startswith('assemblia_restore_') and len(database)==30
                    admin.execute(sql.SQL('DROP DATABASE {} WITH (FORCE)').format(sql.Identifier(database)))
        (destination/'last-restore-verification.json').write_text(json.dumps(status,indent=2),encoding='utf-8')
        return status
if __name__=='__main__':
    parser=argparse.ArgumentParser();sub=parser.add_subparsers(dest='command',required=True);b=sub.add_parser('backup');b.add_argument('--destination',type=Path,required=True);b.add_argument('--objects',type=Path,required=True);b.add_argument('--keys',type=Path,required=True);b.add_argument('--retention-days',type=int,default=7);r=sub.add_parser('verify');r.add_argument('--source',type=Path,required=True);r.add_argument('--destination',type=Path,required=True);args=parser.parse_args()
    try:
        result=backup(args.destination,args.objects,args.keys,args.retention_days)[1] if args.command=='backup' else restore_verify(args.source,args.destination)
        print(json.dumps(result,indent=2))
    except Exception as error:
        # Sanitized monitoring signal; never emit connection strings or secret-bearing subprocess output.
        print(json.dumps({'operation':args.command,'passed':False,'errorType':type(error).__name__}));raise SystemExit(1)
