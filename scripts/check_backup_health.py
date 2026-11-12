import json,os,sys,time,datetime
from pathlib import Path
root=Path(os.environ.get('BACKUP_DESTINATION','/backups'));maximum=max(7200,int(os.environ.get('BACKUP_INTERVAL_SECONDS','86400'))*2)
try:
    status=json.loads((root/'last-backup.json').read_text(encoding='utf-8'))
    at=datetime.datetime.fromisoformat(status['at']).timestamp()
    healthy=time.time()-at<maximum
    failure=root/'backup-failure.json'
    if failure.exists() and json.loads(failure.read_text())['atEpoch']>at:healthy=False
except Exception:healthy=False
print(json.dumps({'backupHealthy':healthy}));sys.exit(0 if healthy else 1)
