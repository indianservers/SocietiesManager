import {useState} from 'react';
import {api} from './api';
import {Badge, Drawer, Field, useAction} from './shared';

type Preview = {id:string;total:number;valid:number;invalid:number;committedAt?:string;rows:{row:number;fullName:string;role:string;number:string;email?:string;mobile?:string;reference:string;errors:string[]}[]};

export default function OrganizationRosterImport({path,onClose}:{path:string;onClose:()=>void}){
  const action=useAction();
  const [file,setFile]=useState<File|null>(null),[preview,setPreview]=useState<Preview|null>(null);
  const [page,setPage]=useState(0),[skip,setSkip]=useState(false);
  const pageSize=50;
  async function inspect(){
    await action.run(async()=>{
      if(!file)throw new Error('Choose an Excel .xlsx or CSV file');
      if(file.size>8*1024*1024)throw new Error('Use a file up to 8 MB');
      const format=file.name.toLowerCase().split('.').pop();
      if(format!=='xlsx'&&format!=='csv')throw new Error('Save this workbook as .xlsx or CSV first');
      let content:string;
      if(format==='csv')content=await file.text();
      else content=await new Promise<string>((resolve,reject)=>{const reader=new FileReader();reader.onload=()=>resolve(String(reader.result).split(',')[1]);reader.onerror=()=>reject(new Error('Could not read this file'));reader.readAsDataURL(file)});
      setPreview(await api<Preview>(path+'/imports/preview','POST',{format,content}));setPage(0);setSkip(false);
    },'Preview ready. No members have been added yet.');
  }
  return <Drawer title="Bulk upload students & faculty" onClose={onClose}><div className="roster-import-flow">
    <p>Download the Excel template, fill its Members sheet, and preview it here. Full name and member type are required. Up to 10,000 rows, 8 MB.</p>
    <a className="secondary" href={'/api/v1'+path+'/template'} download>Download Excel template</a>
    <p>Member ID uses your supplied ID, then roll number, mobile or email, in that order. With none supplied, a unique ID is generated. Keep identifier columns as Text to preserve leading zeros. Use country codes consistently for mobile numbers.</p>
    <Field label="Excel or CSV file"><input aria-label="Excel or CSV file" type="file" accept=".xlsx,.csv" disabled={action.busy} onChange={e=>{setFile(e.target.files?.[0]||null);setPreview(null);setPage(0);setSkip(false)}}/></Field>
    <button className="primary" disabled={!file||action.busy} onClick={inspect}>{action.busy?'Checking file…':'Preview & check duplicates'}</button>
    {preview&&<div className="roster-import-preview" aria-live="polite"><h3>Upload preview</h3><p>{preview.total} rows · {preview.valid} valid · {preview.invalid} need attention</p>
      <p>Duplicates are checked by member ID, roll number, mobile and email within this file and this organization. When identifiers are missing, repeated names need identifying details before import. Individual memberships are separate. Importing creates roster entries, not login accounts or admin roles.</p>
      {preview.committedAt?<p role="status">Imported {preview.valid} members. {preview.invalid} rows skipped. Their IDs are shown below.</p>:<>
        {preview.invalid>0&&<label className="checkbox"><input type="checkbox" checked={skip} onChange={e=>setSkip(e.target.checked)}/>Import only valid rows and skip the highlighted rows</label>}
        <button className="primary" disabled={action.busy||preview.valid===0||(preview.invalid>0&&!skip)} onClick={()=>action.run(async()=>setPreview(await api<Preview>(path+'/imports/'+preview.id+'/commit','POST')),'Members imported with unique IDs')}>Import {preview.valid} valid members</button>
        <p>If the roster changes after this preview, upload and preview again before importing.</p>
      </>}
      <div className="renewal-history">{preview.rows.slice(page*pageSize,(page+1)*pageSize).map(r=><div key={r.row}><span><strong>Row {r.row}: {r.fullName||'Missing name'} · {r.role||'Missing type'}</strong><small className="roster-number">Member ID: {r.number||'Not assigned — correct this row'}</small><small>{[r.reference,r.mobile,r.email].filter(Boolean).join(' · ')}</small>{r.errors.map(error=><small className="roster-import-error" key={error}>{error}</small>)}</span><Badge>{r.errors.length?'Needs correction':preview.committedAt?'Imported':'Ready'}</Badge></div>)}</div>
      {preview.rows.length>pageSize&&<div className="member-portal-actions"><button className="secondary" disabled={page===0} onClick={()=>setPage(page-1)}>Previous rows</button><span>Page {page+1} of {Math.ceil(preview.rows.length/pageSize)}</span><button className="secondary" disabled={(page+1)*pageSize>=preview.rows.length} onClick={()=>setPage(page+1)}>Next rows</button></div>}
    </div>}
  </div></Drawer>;
}
