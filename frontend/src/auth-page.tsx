import {useEffect, useState} from 'react';
import type {FormEvent} from 'react';
import {Link, useLocation} from 'react-router-dom';
import {ArrowRight, CheckCircle2, IdCard, Eye, EyeOff, HeartPulse, Layers, LockKeyhole, MonitorPlay, UsersRound} from 'lucide-react';
import {api} from './api';
import {Drawer, Field, useApp, useLoad} from './shared';
import {membershipKinds} from './membership-kind-picker';
import './auth-page.css';

export default function Auth({onLogin}:{onLogin:()=>void}){
  const path=useLocation().pathname;
  return <AuthScreen key={path} onLogin={onLogin}/>;
}

function AuthScreen({onLogin}:{onLogin:()=>void}){
  const location=useLocation(),params=new URLSearchParams(location.search),{notify}=useApp();
  const [busy,setBusy]=useState(false),[message,setMessage]=useState(''),[twoFactor,setTwoFactor]=useState(false);
  const [dialCode,setDialCode]=useState('');
  const [showPassword,setShowPassword]=useState(false),[kind,setKind]=useState(params.get('kind')||'Individual');
  const [policy,setPolicy]=useState<'terms'|'privacy'|null>(null);
  const brand=useLoad<any>('/branding/resolve'+(params.get('society')?'?society='+encodeURIComponent(params.get('society')!):''));
  const signup=location.pathname==='/signup',resend=location.pathname==='/resend';
  const forgot=location.pathname==='/forgot'||resend,reset=location.pathname==='/reset',verify=location.pathname==='/verify';
  const catalog=useLoad<any>('/membership/catalog?tenant='+brand.data?.tenantId,signup&&!!brand.data?.tenantId);
  const content=brand.data?.authLanding||{},society=brand.data?.displayName||'Your society';
  const community=content.communityName||society;
  const feeFor=(value:string)=>{const offer=catalog.data?.categories?.find((c:any)=>c.kind===value)?.offer;return offer?.price!=null?new Intl.NumberFormat('en-IN',{style:'currency',currency:offer.currency,maximumFractionDigits:0}).format(offer.price)+' / '+(offer.termMonths===12?'year':offer.termMonths+' months'):''};
  const logo=brand.data?.logo||content.logo||'/images/community-brain-chip.png';
  const route=(path:string)=>path+(params.get('society')?'?society='+encodeURIComponent(params.get('society')!):'');
  useEffect(()=>{
    if(brand.data?.language)document.documentElement.lang=brand.data.language;
    const icon=document.querySelector<HTMLLinkElement>('link[rel=icon]');
    if(icon&&(brand.data?.favicon||content.logo)){const previous=icon.href;icon.href=brand.data?.favicon||content.logo;return()=>{icon.href=previous}}
  },[brand.data,content.logo]);

  async function submit(event:FormEvent<HTMLFormElement>){
    event.preventDefault();setBusy(true);
    const fields=Object.fromEntries(new FormData(event.currentTarget));
    try{
      let result;
      if(twoFactor)result=await api('/auth/two-factor','POST',{code:fields.code,recovery:fields.recovery==='on'});
      else if(verify)result=await api('/auth/verify?userId='+params.get('userId')+'&token='+encodeURIComponent(params.get('token')||''),'POST');
      else if(reset)result=await api('/auth/reset','POST',{...fields,email:params.get('email'),token:params.get('token')});
      else result=await api('/auth/'+(signup?'signup':resend?'resend-verification':forgot?'forgot':'login'),'POST',{
        ...fields,password:fields.password||'',...(signup?{membershipKind:kind,societyId:brand.data?.tenantId||null,
          phoneNumber:fields.mobile?String(fields.countryCode)+String(fields.mobile).replace(/[^0-9]/g,''):null}:{})
      });
      if(result.requiresTwoFactor){setTwoFactor(true);return}
      if(!signup&&!forgot&&!reset&&!verify)onLogin();else setMessage(result.message);
    }catch(error){notify((error as Error).message)}finally{setBusy(false)}
  }

  const title=twoFactor?'Verify it’s you':resend?'Verify your email':signup?'Join the '+community+' community':forgot?'Reset your password':reset?'Choose a new password':verify?'Verify your email':content.loginHeading||brand.data?.loginTitle||'Welcome to the '+community+' community';
  const subtitle=twoFactor?'Enter your authenticator or single-use recovery code.':signup?'Create your account and start connecting.':forgot?'We’ll help you get back to your community.':reset?'Create a strong password for your account.':verify?'Confirm your email to continue.':'Sign in and continue connecting.';
  const features=[{Icon:IdCard,title:'Member identity',text:'Your profile, membership and digital ID.',id:'membership'},{Icon:UsersRound,title:'Chapters & community',text:'Connect with your institutional network.',id:'chapters'},{Icon:MonitorPlay,title:'Interactive learning',text:'Access tools, resources and skill-building content.',id:'learning'},{Icon:Layers,title:'Research & opportunities',text:'Discover publications, workshops and internships.',id:'research'}];

  return <div className="community-auth">
    <svg width="0" height="0" aria-hidden="true"><defs><linearGradient id="community-icon-gradient" x1="0" y1="1" x2="1" y2="0"><stop stopColor="#7714cf"/><stop offset=".5" stopColor="#7410c9"/><stop offset="1" stopColor="#00c4df"/></linearGradient></defs></svg>
    <header className="community-header">
      <Link className="community-brand" to={route('/login')} aria-label={society+' home'}><img src={logo} alt={society+' logo'} width="104" height="80"/><span><strong>{society}</strong><small>{content.brandSubtitle||'MEMBERS & CHAPTERS'}</small></span></Link>
      <nav aria-label="Explore the community"><a href="#membership">Membership</a><a href="#chapters">{brand.data?.chapterTerm?brand.data.chapterTerm+'s':'Chapters'}</a><a href="#learning">Learning Tools</a><a href="#research">Research</a></nav>
    </header>
    <main className="community-auth-main">
      <section className="community-story" aria-label="Our community">
        <p className="community-kicker">{content.eyebrow||'RESEARCH • LEARNING • COMMUNITY'}</p>
        <h1>{content.headline||'Connect with minds.'}<em>{content.accentHeadline||'Create what’s next.'}</em></h1>
        <p className="community-introduction">{content.description||'Connect with researchers, educators, engineers and medical professionals advancing innovation through AI.'}</p>
        <div className="community-artwork"><img src={content.illustration||'/images/community-brain-chip.png'} width="490" height="340" alt="A glowing brain above a connected microchip" fetchPriority="high"/></div>
        <div className="community-pills"><a href="#research"><HeartPulse/><span>Research<br/>collaboration</span></a><a href="#chapters"><UsersRound/><span>Institutional<br/>chapters</span></a><a href="#learning"><MonitorPlay/><span>Interactive<br/>learning</span></a></div>
      </section>
      <section className={'community-auth-card'+(!signup?' community-login-card':'')} aria-label={signup?'Create an account':'Account access'}>
        <div className="community-auth-tabs" aria-label="Account access options"><Link className={!signup?'selected':''} aria-current={!signup?'page':undefined} to={route('/login')}>Login</Link><Link className={signup?'selected':''} aria-current={signup?'page':undefined} to={route('/signup')}>Sign Up</Link></div>
        <div className="community-card-heading"><h2>{title}</h2><p>{subtitle}</p></div>
        {message?<div className="community-auth-success" role="status"><CheckCircle2/><p>{message}</p><Link to={route('/resend')}>Request a fresh verification email</Link><Link className="auth-submit primary" to={route('/login')}>Continue to sign in <ArrowRight/></Link></div>:<form className="community-account-form" onSubmit={submit}>
          {twoFactor?<><Field label="Authenticator or recovery code" name="code" autoComplete="one-time-code" required/><label className="community-consent"><input type="checkbox" name="recovery"/>Use a single-use recovery code</label></>:<>
            {signup&&<div className="community-name-phone"><Field label="Full name" name="fullName" autoComplete="name" placeholder="Enter your full name" maxLength={160} required/><Field label="Mobile number"><div className="community-mobile"><select aria-label="Country calling code" name="countryCode" value={dialCode||content.defaultDialCode||'+1'} onChange={event=>setDialCode(event.target.value)}>{[...new Set([content.defaultDialCode||'+1','+91','+1','+44','+61','+971','+65'])].map(code=><option key={code}>{code}</option>)}</select><input aria-label="Mobile number" name="mobile" autoComplete="tel-national" type="tel" pattern="[0-9 ]{7,20}" placeholder="Enter mobile number"/></div></Field></div>}
            {!verify&&!reset&&<Field label="Email address" name="email" type="email" autoComplete="email" placeholder="you@example.com" required/>}
            {signup&&<Field label="I am a"><select name="membershipKind" value={kind} onChange={event=>setKind(event.target.value)} aria-describedby="signup-fees">{membershipKinds.map(value=><option key={value} value={value}>{value} member{feeFor(value)?' — '+feeFor(value):''}</option>)}</select><small id="signup-fees" className="community-membership-prices">{membershipKinds.map(value=>{const offer=catalog.data?.categories?.find((c:any)=>c.kind===value)?.offer;return offer?.price!=null?<span key={value}>{value}: {new Intl.NumberFormat('en-IN',{style:'currency',currency:offer.currency,maximumFractionDigits:0}).format(offer.price)} / {offer.termMonths===12?'year':offer.termMonths+' months'}</span>:null})}</small></Field>}
            {!verify&&!forgot&&<Field label={reset?'New password':'Password'}><div className="community-password"><input aria-label={reset?'New password':'Password'} name="password" type={showPassword?'text':'password'} minLength={signup||reset?12:undefined} autoComplete={signup||reset?'new-password':'current-password'} placeholder={signup||reset?'Create a strong password':'Enter your password'} required/><button type="button" aria-label={showPassword?'Hide password':'Show password'} aria-pressed={showPassword} onClick={()=>setShowPassword(!showPassword)}>{showPassword?<EyeOff/>:<Eye/>}</button></div>{(signup||reset)&&<small className="community-password-hint">12+ characters with uppercase, lowercase, a number and symbol.</small>}</Field>}
            {signup&&<label className="community-consent"><input name="terms" type="checkbox" required/><span>I agree to the <button type="button" onClick={()=>setPolicy('terms')}>Terms of Use</button> and <button type="button" onClick={()=>setPolicy('privacy')}>Privacy Policy</button>.</span></label>}
            {!signup&&!forgot&&!reset&&!verify&&<div className="community-login-help"><Link to={route('/forgot')}>Forgot password?</Link></div>}
          </>}
          <button type="submit" disabled={busy} className="auth-submit primary">{busy?'Please wait…':twoFactor?'Verify & sign in':verify?'Verify email':signup?'Create account':resend?'Resend verification email':forgot?'Send recovery email':reset?'Reset password':'Login'}<ArrowRight/></button>
        </form>}
        {!message&&<p className="community-switch">{signup?'Already a member?':'New to the community?'} <Link to={route(signup?'/login':'/signup')}>{signup?'Log in':'Sign up'}</Link></p>}
        <p className="community-private"><LockKeyhole size={22}/>Your information is kept private and secure.</p>
      </section>
    </main>
    <div className="community-society-band">{content.fullName||society}</div>
    <footer className="community-features">{features.map(({Icon,title,text,id})=><section id={id} key={id}><Icon/><div><h2>{title}</h2><p>{text}</p></div></section>)}</footer>
    {policy&&<Drawer title={policy==='terms'?'Terms of Use':'Privacy Policy'} onClose={()=>setPolicy(null)}><div className="community-policy">{(policy==='terms'?content.terms:content.privacy)?<p>{policy==='terms'?content.terms:content.privacy}</p>:<><h3>{society}</h3><p>{policy==='terms'?'Use this portal to apply for and manage memberships. Membership is subject to society review and the published membership fees. Keep your account details accurate and your login credentials secure.':'Your name and contact details are used to manage your account and memberships. Member records are protected by role-based access. You can manage directory visibility and communication preferences in your profile.'}</p><p>Contact your society administrator for the society’s full {policy==='terms'?'membership terms':'privacy policy'}.</p></>}</div></Drawer>}
  </div>;
}
