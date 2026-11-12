"""Adversarial backup authentication/extraction and backup-health checks; no DB writes."""
import os,json,base64,secrets,importlib.util,tempfile,tarfile,io,datetime,subprocess
from pathlib import Path
from cryptography.exceptions import InvalidTag
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('backup_restore',ROOT/'scripts/backup_restore.py');backup=importlib.util.module_from_spec(spec);spec.loader.exec_module(backup)
os.environ['BACKUP_ENCRYPTION_KEY']=base64.b64encode(secrets.token_bytes(32)).decode();os.environ.pop('BACKUP_ENCRYPTION_KEY_FILE',None);os.environ['PGPASSWORD']='unused-fixture-no-database-connection';results=[]
def check(name,fn):fn();results.append({'name':name,'passed':True})
def denied(fn,kind):
 try:fn()
 except kind:return
 raise AssertionError('Expected rejection')
with tempfile.TemporaryDirectory(prefix='phase6-backup-security-',dir=ROOT/'.local') as temp:
 root=Path(temp).resolve();assert (ROOT/'.local').resolve() in root.parents
 source=root/'source';source.write_bytes(b'Authenticated local fixture');archive=root/'fixture.abk';plain=root/'plain';backup.encrypt(source,archive);backup.decrypt(archive,plain)
 check('AES-GCM round trip returns exact bytes',lambda:None if source.read_bytes()==plain.read_bytes() else (_ for _ in ()).throw(AssertionError()))
 altered=root/'altered.abk';raw=bytearray(archive.read_bytes());raw[-1]^=1;altered.write_bytes(raw)
 check('ciphertext authentication rejects a modified tag',lambda:denied(lambda:backup.decrypt(altered,plain),InvalidTag))
 originalkey=os.environ['BACKUP_ENCRYPTION_KEY'];os.environ['BACKUP_ENCRYPTION_KEY']=base64.b64encode(secrets.token_bytes(32)).decode()
 check('wrong encryption key cannot decrypt an archive',lambda:denied(lambda:backup.decrypt(archive,plain),InvalidTag));os.environ['BACKUP_ENCRYPTION_KEY']=originalkey
 for name,link in [('parent-traversal',False),('symbolic-link',True)]:
  tarpath=root/(name+'.tar')
  with tarfile.open(tarpath,'w') as tar:
   entry=tarfile.TarInfo('../escaped' if not link else 'link')
   if link:entry.type=tarfile.SYMTYPE;entry.linkname='../escaped';tar.addfile(entry)
   else:entry.size=1;tar.addfile(entry,io.BytesIO(b'x'))
  encrypted=root/(name+'.abk');backup.encrypt(tarpath,encrypted)
  check('restore rejects '+name+' before database creation',lambda:denied(lambda:backup.restore_verify(encrypted,root/'restore'),ValueError))
 assert not (root.parent/'escaped').exists()
 health=root/'health';health.mkdir();healthenv={**os.environ,'BACKUP_DESTINATION':str(health)}
 def status(code):
  result=subprocess.run(['python',str(ROOT/'scripts/check_backup_health.py')],env=healthenv,capture_output=True);assert result.returncode==code
 (health/'last-backup.json').write_text(json.dumps({'at':datetime.datetime.now(datetime.timezone.utc).isoformat()}))
 check('fresh backup status is healthy',lambda:status(0))
 (health/'backup-failure.json').write_text(json.dumps({'atEpoch':datetime.datetime.now(datetime.timezone.utc).timestamp()+1}))
 check('later backup failure makes health fail',lambda:status(1));(health/'backup-failure.json').unlink()
 (health/'last-backup.json').write_text(json.dumps({'at':(datetime.datetime.now(datetime.timezone.utc)-datetime.timedelta(days=3)).isoformat()}))
 check('stale backup status makes health fail',lambda:status(1))
(ROOT/'tests/phase6-backup-security.json').write_text(json.dumps({'passed':len(results),'total':len(results),'results':results,'scope':'Synthetic authenticated archives and health status. Invalid archives rejected before database creation. No production key or target.'},indent=2),encoding='utf-8');print('Backup security and health checks:',len(results),'passed')
