import {useId,useRef,type ReactNode} from 'react';
import {Navigate, useLocation, useSearchParams} from 'react-router-dom';
import {Empty} from './shared';
import './workspace-navigation.css';

export type WorkspaceSection = {id:string; label:string; content:ReactNode; visible?:boolean};

/** One workspace, addressable sections, and only the selected feature mounted. */
export function WorkspaceSections({label, sections,queryKey='view',compact=false}:{label:string; sections:WorkspaceSection[];queryKey?:string;compact?:boolean}) {
  const [params,setParams]=useSearchParams();
  const id=useId(),buttons=useRef<Array<HTMLButtonElement|null>>([]);
  const available=sections.filter(s=>s.visible!==false);
  const requested=params.get(queryKey);
  const active=requested?available.find(s=>s.id===requested):available[0];
  const choose=(value:string)=>{const next=new URLSearchParams(params);next.set(queryKey,value);setParams(next);};
  return <div className="merged-workspace">
    <nav className="workspace-sections" aria-label={label+' sections'}>
      {compact?<label className="tool-picker">{label}<select aria-label={label+' tool'} value={active?.id||''} onChange={e=>choose(e.target.value)}>{!active&&<option value="">Choose a tool</option>}{available.map(s=><option key={s.id} value={s.id}>{s.label}</option>)}</select></label>:<div role="tablist" aria-label={label}>{available.map((s,index)=><button ref={el=>{buttons.current[index]=el;}} type="button" role="tab" id={id+'-'+s.id} aria-controls={id+'-panel'} tabIndex={active?.id===s.id||!active&&index===0?0:-1} key={s.id} aria-selected={active?.id===s.id} onClick={()=>choose(s.id)} onKeyDown={e=>{
        if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;e.preventDefault();const next=e.key==='Home'?0:e.key==='End'?available.length-1:(index+(e.key==='ArrowRight'?1:-1)+available.length)%available.length;buttons.current[next]?.focus();choose(available[next].id);
      }}>{s.label}</button>)}</div>}
    </nav>
    <section role={compact?'region':'tabpanel'} id={id+'-panel'} key={active?.id||'unavailable'} aria-label={active?.label||label}>
      {active?.content||<Empty title="Section unavailable" text="Choose one of the sections available to your current role."/>}
    </section>
  </div>;
}

/** Keep notification links, bookmarks and browser history compatible. */
export function WorkspaceAlias({to,query={},defaults={}}:{to:string;query?:Record<string,string>;defaults?:Record<string,string>}) {
  const location=useLocation();
  const params=new URLSearchParams(location.search);
  for(const [key,value] of Object.entries(defaults))if(!params.has(key))params.set(key,value);
  for(const [key,value] of Object.entries(query))params.set(key,value);
  return <Navigate replace to={{pathname:to,search:params.size?'?'+params.toString():'',hash:location.hash}}/>;
}
