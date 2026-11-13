"""Root-operated authenticated backups for a dedicated additional-volume install.

Usage: native_volume_backup.py /absolute/install/root backup|verify [backup-file]
Database, runtime identity and data directories must belong to this installation.
The app is quiesced only while collecting a consistent database/object snapshot.
"""
import base64,datetime,hashlib,json,os,secrets,shutil,subprocess,sys,tarfile,tempfile,uuid
from pathlib import Path
from cryptography.hazmat.primitives.ciphers import Cipher,algorithms,modes

MAGIC=b'SOCIETY-VOLUME-BACKUP-1\n'
base=Path(sys.argv[1]).resolve()
if base.stat().st_dev==Path('/').stat().st_dev:raise SystemExit('Backup installation must be on the additional disk')
configuration=json.loads((base/'secrets/deployment.json').read_text())
if configuration['database']!='aimer_members' or configuration['databasePort']!=5544:raise SystemExit('Dedicated database identity differs')
destination=base/'backups';destination.mkdir(mode=0o700,exist_ok=True)
workspace=base/'backup-work';workspace.mkdir(mode=0o700,exist_ok=True)
keyfile=base/'secrets/backup.key'
try:key=base64.b64decode(keyfile.read_text().strip(),validate=True)
except Exception:key=b''
if len(key)!=32:
 if list(destination.glob('*.abk')):raise SystemExit('Existing backup key cannot be replaced')
 key=secrets.token_bytes(32);keyfile.write_text(base64.b64encode(key).decode());keyfile.chmod(0o600)

def postgres(tool,args=(),input=None,output=None,source=None):
 command=['runuser','-u','postgres','--','/usr/lib/postgresql/18/bin/'+tool,'-h','/run/aimer-members-db','-p','5544',*args]
 result=subprocess.run(command,input=input,stdin=source,stdout=output if output is not None else subprocess.PIPE,stderr=subprocess.PIPE)
 if result.returncode:raise RuntimeError('PostgreSQL backup operation failed: '+tool)
 return result.stdout

def counts(database):
 tables=['Tenants','Units','AspNetUsers','Memberships','FinancePayment','JournalLine']
 return {table:int(postgres('psql',['-d',database,'-Atc','SELECT count(*) FROM "'+table+'";'])) for table in tables}

def crypt(source,target,decrypt=False):
 with source.open('rb') as incoming,target.open('wb') as outgoing:
  if decrypt:
   if incoming.read(len(MAGIC))!=MAGIC:raise ValueError('Invalid backup format')
   nonce=incoming.read(12);incoming.seek(-16,2);tag=incoming.read(16);remaining=incoming.tell()-16-len(MAGIC)-12;incoming.seek(len(MAGIC)+12)
   cipher=Cipher(algorithms.AES(key),modes.GCM(nonce,tag)).decryptor()
  else:
   nonce=secrets.token_bytes(12);cipher=Cipher(algorithms.AES(key),modes.GCM(nonce)).encryptor();outgoing.write(MAGIC+nonce);remaining=None
  cipher.authenticate_additional_data(MAGIC)
  while remaining is None or remaining>0:
   block=incoming.read(1048576 if remaining is None else min(1048576,remaining))
   if not block:break
   outgoing.write(cipher.update(block))
   if remaining is not None:remaining-=len(block)
  outgoing.write(cipher.finalize())
  if not decrypt:outgoing.write(cipher.tag)
 target.chmod(0o600)

operation=sys.argv[2]
with tempfile.TemporaryDirectory(prefix='snapshot-',dir=workspace) as directory:
 root=Path(directory)
 if operation=='backup':
  active=subprocess.run(['systemctl','is-active','--quiet','aimer-members.service']).returncode==0
  try:
   if active:subprocess.run(['systemctl','stop','aimer-members.service'],check=True)
   snapshot=counts(configuration['database'])
   with (root/'database.dump').open('wb') as dump:postgres('pg_dump',['-d',configuration['database'],'--format=custom','--no-owner','--no-privileges'],output=dump)
   metadata={'at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'tableCounts':snapshot,'consistentSnapshot':True,'database':configuration['database']}
   (root/'snapshot.json').write_text(json.dumps(metadata,indent=2))
   with tarfile.open(root/'snapshot.tar.gz','w:gz') as archive:
    for name in ['database.dump','snapshot.json']:archive.add(root/name,arcname=name)
    for name in ['storage','keys']:
     folder=base/name
     if folder.is_symlink() or any(p.is_symlink() for p in folder.rglob('*')):raise ValueError('Backup data links are not permitted')
     archive.add(folder,arcname=name)
    for name in ['deployment.json','runtime.env','data-protection.pfx','data-protection.key','data-protection.crt']:
     archive.add(base/'secrets'/name,arcname='recovery/'+name)
  finally:
   if active:subprocess.run(['systemctl','start','aimer-members.service'],check=True)
  output=destination/('society-'+datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')+'-'+uuid.uuid4().hex[:6]+'.abk')
  crypt(root/'snapshot.tar.gz',output)
  digest=hashlib.sha256()
  with output.open('rb') as encrypted:
   for block in iter(lambda:encrypted.read(1048576),b''):digest.update(block)
  status={'file':output.name,'at':metadata['at'],'bytes':output.stat().st_size,'sha256':digest.hexdigest(),'encryption':'AES-256-GCM','restoreVerified':False}
  (destination/'last-backup.json').write_text(json.dumps(status,indent=2));print(json.dumps(status))
  cutoff=datetime.datetime.now(datetime.timezone.utc).timestamp()-14*86400
  for old in destination.glob('society-*.abk'):
   if old.resolve().parent!=destination.resolve():raise ValueError('Unsafe backup retention path')
   if old.stat().st_mtime<cutoff:old.unlink()
 elif operation=='verify':
  source=Path(sys.argv[3]).resolve()
  if source.parent!=destination.resolve():raise ValueError('Choose a backup in this installation')
  crypt(source,root/'snapshot.tar.gz',True)
  with tarfile.open(root/'snapshot.tar.gz','r:gz') as archive:
   metadata=json.load(archive.extractfile('snapshot.json'))
   with (root/'database.dump').open('wb') as output:shutil.copyfileobj(archive.extractfile('database.dump'),output)
  scratch='aimer_restore_verify_'+uuid.uuid4().hex[:12]
  try:
   postgres('createdb',[scratch])
   with (root/'database.dump').open('rb') as dump:postgres('pg_restore',['-d',scratch,'--no-owner','--no-privileges','--exit-on-error'],source=dump)
   if counts(scratch)!=metadata['tableCounts']:raise ValueError('Restored table counts differ')
  finally:
   if not scratch.startswith('aimer_restore_verify_'):raise ValueError('Unsafe recovery database target')
   postgres('dropdb',['--if-exists',scratch])
  status={'file':source.name,'at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'restoreVerified':True,'tableCounts':metadata['tableCounts']}
  (destination/'last-restore-verification.json').write_text(json.dumps(status,indent=2));print(json.dumps(status))
 else:raise ValueError('Use backup or verify')
