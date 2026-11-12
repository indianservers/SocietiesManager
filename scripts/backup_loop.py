"""Operator-enabled backup schedule. Run only in a configured deployment."""
import json,os,time
from pathlib import Path
from backup_restore import backup,restore_verify

destination=Path(os.environ.get('BACKUP_DESTINATION','/backups'))
interval=max(3600,int(os.environ.get('BACKUP_INTERVAL_SECONDS','86400')))
verify_every=max(1,int(os.environ.get('BACKUP_VERIFY_EVERY','7')))
runs=0
while True:
    started=time.time()
    try:
        archive,status=backup(destination,Path(os.environ['BACKUP_OBJECTS']),Path(os.environ['BACKUP_KEYS']),int(os.environ.get('BACKUP_RETENTION_DAYS','7')))
        runs+=1
        if runs==1 or runs%verify_every==0:
            restored=restore_verify(archive,destination/'restore-verification')
            status['restoreVerified']=restored['passed'];(destination/'last-backup.json').write_text(json.dumps(status,indent=2),encoding='utf-8')
        print(json.dumps({'operation':'scheduled-backup','passed':True,'at':status['at'],'bytes':status['bytes'],'restoreVerified':status['restoreVerified']}),flush=True)
    except Exception as error:
        destination.mkdir(parents=True,exist_ok=True)
        (destination/'backup-failure.json').write_text(json.dumps({'passed':False,'atEpoch':time.time(),'errorType':type(error).__name__}),encoding='utf-8')
        print(json.dumps({'operation':'scheduled-backup','passed':False,'errorType':type(error).__name__}),flush=True)
    time.sleep(max(60,interval-(time.time()-started)))
