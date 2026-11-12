"""Quiesced local fixture backup/restore drill. No production target is overwritten."""
import os,json,base64,secrets,importlib.util
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];cfg=json.loads((ROOT/'.local/dev.json').read_text())
os.environ.update({'PGHOST':'127.0.0.1','PGPORT':'55439','PGUSER':'postgres','PGPASSWORD':cfg['password'],'PGDATABASE':'assemblia_test','PG_BIN':os.environ.get('PG_BIN',str(ROOT/'.local/pgsql/bin')),'BACKUP_ENCRYPTION_KEY':base64.b64encode(secrets.token_bytes(32)).decode()})
spec=importlib.util.spec_from_file_location('backup_restore',ROOT/'scripts/backup_restore.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
keys=ROOT/'.local/phase6-restore-test-keys';keys.mkdir(exist_ok=True);(keys/'test-marker.txt').write_text('Disposable key-directory copy verification marker, not a deployed Data Protection key.')
archive,backup=module.backup(ROOT/'.local/phase6-test-backups',ROOT/'.local/assets',keys)
restore=module.restore_verify(archive,ROOT/'.local/phase6-test-restores')
assert restore['passed'] and restore['allTableCountsMatched'] and restore['allFileHashesMatched'] and restore['unbalancedPostedJournals']==0
result={'backup':backup,'restore':restore,'scope':'Disposable test database and local immutable object files, with a test key-directory marker. Encryption key is ephemeral; this artifact is a test backup, not an operational recovery copy. Deployed Data Protection key recovery and production PITR are not certified.'}
(ROOT/'tests/phase6-backup-restore.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps({'passed':True,'tables':len(restore['tables']),'objects':restore['objectFiles'],'backupSeconds':backup['elapsedSeconds'],'restoreSeconds':restore['elapsedSeconds']},indent=2))
