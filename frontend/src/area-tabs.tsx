import {Link,useLocation,useNavigate} from 'react-router-dom';
import {useApp} from './shared';

export function AreaTabs({area}:{area:'home'|'people'|'activities'}){
 const {can,workspace}=useApp(),{pathname}=useLocation(),navigate=useNavigate();
 const enabled=(m:string)=>!workspace?.modules||workspace.modules.includes(m);
 const tabs=area==='home'?[
  {label:'Overview',to:'/app',active:pathname==='/app',show:true},
  {label:'Insights',to:'/app/intelligence',active:pathname==='/app/intelligence',show:enabled('intelligence')&&can('intelligence.read')},
  {label:'Reports',to:'/app/intelligence/reports',active:pathname.endsWith('/reports'),show:enabled('intelligence')&&can('reports.export')},
  {label:'Assistant',to:'/app/intelligence/assistant',active:pathname.endsWith('/assistant'),show:enabled('intelligence')},
 ]:area==='people'?[
  {label:'Members',to:'/app/members',active:pathname==='/app/members'||pathname.includes('/members/profile/'),show:true},
  {label:'Leadership',to:'/app/members/governance',active:pathname.includes('/members/governance'),show:true},
  {label:'Organization',to:'/app/organization',active:pathname==='/app/organization'||pathname.includes('/units/'),show:can('units.read')||can('directory.chapter')},
  {label:'Requests & imports',to:'/app/members/operations',active:pathname.includes('/members/operations'),show:true},
  {label:'Membership rules',to:'/app/members/settings',active:pathname.includes('/members/settings'),show:can('membership.settings')},
 ]:[
  {label:'Feed',to:'/app/community',active:pathname==='/app/community'||pathname.includes('/community/posts/'),show:enabled('community')},
  {label:'Events',to:'/app/community/events',active:pathname.includes('/community/events'),show:enabled('community')},
  {label:'Projects',to:'/app/service/projects',active:pathname.includes('/service/projects'),show:enabled('service')},
  {label:'Volunteers',to:'/app/service/volunteers',active:pathname.includes('/service/volunteers'),show:enabled('service')},
  {label:'Results & reviews',to:enabled('service')?'/app/service':'/app/community/oversight',active:pathname==='/app/service'||pathname.includes('/community/oversight'),show:enabled('service')||can('activities.approve')||can('events.approve')||can('activities.moderate')||can('activities.analytics')||can('events.analytics')},
  {label:'Community rules',to:'/app/community/settings',active:pathname==='/app/community/settings',show:enabled('community')&&can('activities.settings')},
 ];
 const reviews=can('activities.approve')||can('events.approve')||can('activities.moderate')||can('activities.analytics')||can('events.analytics');
 return <header className="area-heading"><h1>{area==='home'?'Home':area==='people'?'People':'Activities'}</h1><nav className="area-tabs" aria-label={(area==='home'?'Home':area==='people'?'People':'Activities')+' tabs'}>{tabs.filter(t=>t.show).map(t=><Link key={t.to} to={t.to} aria-current={t.active?'page':undefined}>{t.label}</Link>)}</nav>{area==='activities'&&(pathname==='/app/service'||pathname.includes('/community/oversight'))&&enabled('service')&&reviews&&<label className="tool-picker review-picker">Review area<select aria-label="Review area" value={pathname.startsWith('/app/service')?'service':'community'} onChange={e=>navigate(e.target.value==='service'?'/app/service':'/app/community/oversight')}><option value="service">Service results</option><option value="community">Community approvals & insights</option></select></label>}</header>;
}
