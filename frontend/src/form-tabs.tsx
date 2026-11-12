import {useId,useRef,useState,type ReactNode} from 'react';
import {flushSync} from 'react-dom';

/** Form controls stay mounted so values and FormData survive tab switches. */
export function FormTabs({label,sections}:{label:string;sections:{id:string;label:string;content:ReactNode}[]}){
 const [active,setActive]=useState(sections[0]?.id),id=useId(),invalidHandled=useRef(false);
 return <div className="form-tabs" onInvalidCapture={e=>{
  if(invalidHandled.current)return;invalidHandled.current=true;queueMicrotask(()=>{invalidHandled.current=false;});
  const target=e.target as HTMLElement,section=target.closest<HTMLElement>('[data-form-tab]');
  if(section?.hidden)flushSync(()=>setActive(section.dataset.formTab!));
 }}><div className="form-tab-buttons" role="tablist" aria-label={label}>{sections.map((s,index)=><button type="button" role="tab" id={id+s.id} aria-selected={s.id===active} aria-controls={id+s.id+'-panel'} tabIndex={s.id===active?0:-1} key={s.id} onClick={()=>setActive(s.id)} onKeyDown={e=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;e.preventDefault();const next=e.key==='Home'?0:e.key==='End'?sections.length-1:(index+(e.key==='ArrowRight'?1:-1)+sections.length)%sections.length;setActive(sections[next].id);(e.currentTarget.parentElement?.children[next] as HTMLElement)?.focus();}}>{s.label}</button>)}</div>{sections.map(s=><div role="tabpanel" aria-label={s.label} id={id+s.id+'-panel'} data-form-tab={s.id} hidden={s.id!==active} key={s.id}>{s.content}</div>)}</div>;
}
