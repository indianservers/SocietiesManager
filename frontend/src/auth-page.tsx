import {useEffect, useRef, useState} from 'react';
import type {FormEvent} from 'react';
import {Link, useLocation} from 'react-router-dom';
import {ArrowRight, CheckCircle2, IdCard, Eye, EyeOff, FlaskConical, GraduationCap, Handshake, ChartNoAxesColumnIncreasing, Microscope, Mail, UserRound, Layers, LockKeyhole, MonitorPlay, UsersRound} from 'lucide-react';
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
  const [rememberMe,setRememberMe]=useState(false);
  const [busy,setBusy]=useState(false),[message,setMessage]=useState(''),[twoFactor,setTwoFactor]=useState(false);
  const [dialCode,setDialCode]=useState('');
  const [showPassword,setShowPassword]=useState(false),[kind,setKind]=useState(params.get('kind')||'Individual');
  const formRef=useRef<HTMLFormElement>(null),pendingLink=useRef<string|null>(null);
  const [linking,setLinking]=useState(false),[social,setSocial]=useState<typeof import('./firebase-login')|null>(null);
  const providers=useLoad<any>('/auth/providers',!['/forgot','/resend','/reset','/verify'].includes(location.pathname));
  const [policy,setPolicy]=useState<'terms'|'privacy'|'help'|null>(null);
  const brand=useLoad<any>('/branding/resolve'+(params.get('society')?'?society='+encodeURIComponent(params.get('society')!):''));
  const signup=location.pathname==='/signup',resend=location.pathname==='/resend';
  const forgot=location.pathname==='/forgot'||resend,reset=location.pathname==='/reset',verify=location.pathname==='/verify';
  const registering=signup&&!linking;
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

  useEffect(()=>{
    if(!providers.data?.enabled)return;
    let current=true;
    import('./firebase-login').then(async module=>{await module.prepare(providers.data.firebase);if(current)setSocial(module)}).catch(()=>notify('Social sign-in could not load. You can still use your username and password.'));
    return()=>{current=false};
  },[providers.data]);
  async function finishLogin(){
    if(pendingLink.current){
      await api('/auth/firebase/link','POST',{idToken:pendingLink.current});pendingLink.current=null;setLinking(false);
    }
    onLogin();
  }
  async function socialLogin(provider:'google.com'|'microsoft.com'){
    if(!social)return;
    if(registering&&!formRef.current?.querySelector<HTMLInputElement>('input[name=terms]')?.checked){notify('Please agree to the Terms of Use and Privacy Policy before creating an account.');return}
    setBusy(true);
    try{
      const idToken=await social.popup(provider);
      const result=await api('/auth/firebase','POST',{idToken,membershipKind:kind,societyId:brand.data?.tenantId||null});
      if(result.requiresAccountLink){pendingLink.current=idToken;setLinking(true);notify(result.message);return}
      if(result.requiresEmailVerification){setMessage(result.message);return}
      if(result.requiresTwoFactor){setTwoFactor(true);return}
      await finishLogin();
    }catch(error){notify((error as Error).message)}finally{setBusy(false)}
  }

  async function submit(event:FormEvent<HTMLFormElement>){
    event.preventDefault();setBusy(true);
    const fields=Object.fromEntries(new FormData(event.currentTarget));
    try{
      let result;
      if(twoFactor)result=await api('/auth/two-factor','POST',{code:fields.code,recovery:fields.recovery==='on',rememberMe});
      else if(verify)result=await api('/auth/verify?userId='+params.get('userId')+'&token='+encodeURIComponent(params.get('token')||''),'POST');
      else if(reset)result=await api('/auth/reset','POST',{...fields,email:params.get('email'),token:params.get('token')});
      else result=await api('/auth/'+(registering?'signup':resend?'resend-verification':forgot?'forgot':'login'),'POST',{
        ...fields,password:fields.password||'',rememberMe,...(registering?{membershipKind:kind,societyId:brand.data?.tenantId||null,
          phoneNumber:fields.mobile?String(fields.countryCode)+String(fields.mobile).replace(/[^0-9]/g,''):null}:{})
      });
      if(result.requiresTwoFactor){setTwoFactor(true);return}
      if(twoFactor||linking||!signup&&!forgot&&!reset&&!verify)await finishLogin();else setMessage(result.message);
    }catch(error){notify((error as Error).message)}finally{setBusy(false)}
  }

  const title=linking&&!twoFactor?'Confirm your existing account':twoFactor?'Verify it’s you':resend?'Verify your email':signup?'Join the '+community+' community':forgot?'Reset your password':reset?'Choose a new password':verify?'Verify your email':'Welcome back';
  const subtitle=linking&&!twoFactor?'Sign in once to securely connect your provider.':twoFactor?'Enter your authenticator or single-use recovery code.':signup?'Create your account and start connecting.':forgot?'We’ll help you get back to your community.':reset?'Create a strong password for your account.':verify?'Confirm your email to continue.':'Sign in to your AIMER workspace.';
  const features=[{Icon:IdCard,title:'Member identity',text:'Your profile, membership and digital ID.',id:'membership'},{Icon:UsersRound,title:'Institutional chapters',text:'Connect with your institutional network.',id:'chapters'},{Icon:MonitorPlay,title:'Interactive learning',text:'Access tools, resources and skill-building content.',id:'learning'},{Icon:Layers,title:'Research opportunities',text:'Discover publications, workshops and internships.',id:'research'}];

  return <div className="community-auth">
    <svg width="0" height="0" aria-hidden="true"><defs><linearGradient id="community-icon-gradient" x1="0" y1="1" x2="1" y2="0"><stop stopColor="#7714cf"/><stop offset=".5" stopColor="#7410c9"/><stop offset="1" stopColor="#00c4df"/></linearGradient></defs></svg>
    <header className="community-header">
      <Link className="community-brand" to={route('/login')} aria-label={society+' home'}><img src={logo} alt={society+' logo'} width="104" height="80"/><span><strong>{society}</strong><small>{content.brandSubtitle||'MEMBERS & CHAPTERS'}</small></span></Link>
      <nav aria-label="Explore the community"><a href="#membership">Membership</a><a href="#chapters">{brand.data?.chapterTerm?brand.data.chapterTerm+'s':'Chapters'}</a><a href="#learning">Learning tools</a><a href="#research">Research</a><Link className="community-join" to={route('/signup')}>Join the community</Link></nav>
    </header>
    <main className="community-auth-main">
      <section className="community-story" aria-label="Our community">
        <p className="community-kicker">RESEARCH · LEARNING · COMMUNITY</p>
        <h1>Great minds.<br/>Meaningful connections.<em>New possibilities.</em></h1>
        <p className="community-introduction">A community for researchers, educators and<br className="desktop-break"/> innovators shaping what comes next.</p>
        <div className="community-artwork" aria-hidden="true"><span className="auth-orbit orbit-a"/><span className="auth-orbit orbit-b"/><span className="auth-orbit orbit-c"/><img src="/images/community-glass-brain.png" width="600" height="400" alt="" fetchPriority="high"/><span className="auth-floating-icon research"><Microscope/></span><span className="auth-floating-icon layers"><Layers/></span><span className="auth-floating-icon people"><UsersRound/></span><span className="auth-floating-icon science"><FlaskConical/></span><i className="auth-orbit-dot dot-one"/><i className="auth-orbit-dot dot-two"/></div>
        <div className="community-pills"><a href="#chapters"><Handshake/><span>Collaborate</span></a><a href="#learning"><GraduationCap/><span>Learn</span></a><a href="#research"><ChartNoAxesColumnIncreasing/><span>Grow</span></a></div>
      </section>
      <section className={'community-auth-card'+(!signup?' community-login-card':'')} aria-label={signup?'Create an account':'Account access'}>
        <div className="community-auth-tabs" aria-label="Account access options"><Link className={!signup?'selected':''} aria-current={!signup?'page':undefined} to={route('/login')}>Login</Link><Link className={signup?'selected':''} aria-current={signup?'page':undefined} to={route('/signup')}>Sign up</Link></div>
        <div className="community-card-heading"><span className="community-heading-icon"><UserRound/></span><div><h2>{title}</h2><p>{subtitle}</p></div></div>
        {message?<div className="community-auth-success" role="status"><CheckCircle2/><p>{message}</p><Link to={route('/resend')}>Request a fresh verification email</Link><Link className="auth-submit primary" to={route('/login')}>Continue to sign in <ArrowRight/></Link></div>:<form ref={formRef} className="community-account-form" onSubmit={submit}>
          {twoFactor?<><Field label="Authenticator or recovery code" name="code" autoComplete="one-time-code" required/><label className="community-consent"><input type="checkbox" name="recovery"/>Use a single-use recovery code</label></>:<>
            {registering&&<div className="community-name-phone"><Field label="Full name" name="fullName" autoComplete="name" placeholder="Enter your full name" maxLength={160} required/><Field label="Mobile number"><div className="community-mobile"><select aria-label="Country calling code" name="countryCode" value={dialCode||content.defaultDialCode||'+1'} onChange={event=>setDialCode(event.target.value)}>{[...new Set([content.defaultDialCode||'+1','+91','+1','+44','+61','+971','+65'])].map(code=><option key={code}>{code}</option>)}</select><input aria-label="Mobile number" name="mobile" autoComplete="tel-national" type="tel" pattern="[0-9 ]{7,20}" placeholder="Enter mobile number"/></div></Field></div>}
            {!verify&&!reset&&<Field label="Email address"><div className="community-input-icon"><Mail aria-hidden="true"/><input name="email" type={registering||forgot?"email":"text"} autoComplete={registering||forgot?"email":"username"} placeholder="you@example.com" required/></div></Field>}{registering&&<details className="community-username"><summary>Choose a username (optional)</summary><Field label="Username" name="username" autoComplete="username" minLength={3} maxLength={80} placeholder="Choose a unique username"/></details>}
            {registering&&<Field label="I am a"><select name="membershipKind" value={kind} onChange={event=>setKind(event.target.value)} aria-describedby="signup-fees">{membershipKinds.map(value=><option key={value} value={value}>{value} member{feeFor(value)?' — '+feeFor(value):''}</option>)}</select><small id="signup-fees" className="community-membership-prices">{membershipKinds.map(value=>{const offer=catalog.data?.categories?.find((c:any)=>c.kind===value)?.offer;return offer?.price!=null?<span key={value}>{value}: {new Intl.NumberFormat('en-IN',{style:'currency',currency:offer.currency,maximumFractionDigits:0}).format(offer.price)} / {offer.termMonths===12?'year':offer.termMonths+' months'}</span>:null})}</small></Field>}
            {!verify&&!forgot&&<Field label={reset?'New password':'Password'}><div className="community-password"><LockKeyhole className="community-input-leading" aria-hidden="true"/><input aria-label={reset?'New password':'Password'} name="password" type={showPassword?'text':'password'} minLength={registering||reset?12:undefined} autoComplete={registering||reset?'new-password':'current-password'} placeholder={registering||reset?'Create a strong password':'Enter your password'} required/><button type="button" aria-label={showPassword?'Hide password':'Show password'} aria-pressed={showPassword} onClick={()=>setShowPassword(!showPassword)}>{showPassword?<EyeOff/>:<Eye/>}</button></div>{(registering||reset)&&<small className="community-password-hint">12+ characters with uppercase, lowercase, a number and symbol.</small>}</Field>}
            {registering&&<label className="community-consent"><input name="terms" type="checkbox" required/><span>I agree to the <button type="button" onClick={()=>setPolicy('terms')}>Terms of Use</button> and <button type="button" onClick={()=>setPolicy('privacy')}>Privacy Policy</button>.</span></label>}
            {!signup&&!forgot&&!reset&&!verify&&<div className="community-login-help"><label><input type="checkbox" checked={rememberMe} onChange={e=>setRememberMe(e.target.checked)}/>Remember me</label><Link to={route('/forgot')}>Forgot password?</Link></div>}
          </>}
          <button type="submit" disabled={busy} className="auth-submit primary">{busy?'Please wait…':twoFactor?'Verify & sign in':verify?'Verify email':linking?'Confirm account':signup?'Create account':resend?'Resend verification email':forgot?'Send recovery email':reset?'Reset password':'Sign in'}<ArrowRight/></button>
        </form>}
        {!message&&!twoFactor&&!forgot&&!reset&&!verify&&providers.data?.enabled&&<div className="community-social"><div className="community-social-divider"><span>{linking?'or confirm with a connected provider':'or continue with'}</span></div><div className="community-social-buttons">{providers.data.providers.includes('google.com')&&<button type="button" disabled={busy||!social} onClick={()=>socialLogin('google.com')}><svg viewBox="0 0 24 24" aria-hidden="true"><path fill="#4285F4" d="M21.6 12.23c0-.71-.06-1.39-.18-2.05H12v3.88h5.38a4.6 4.6 0 0 1-2 3.02v2.51h3.24c1.9-1.75 2.98-4.33 2.98-7.36Z"/><path fill="#34A853" d="M12 22c2.7 0 4.96-.9 6.62-2.41l-3.24-2.51c-.9.6-2.05.96-3.38.96-2.6 0-4.8-1.76-5.59-4.12H3.06v2.59A10 10 0 0 0 12 22Z"/><path fill="#FBBC05" d="M6.41 13.92a6 6 0 0 1 0-3.84V7.49H3.06a10 10 0 0 0 0 9.02l3.35-2.59Z"/><path fill="#EA4335" d="M12 5.96c1.47 0 2.79.5 3.82 1.5l2.87-2.87A9.6 9.6 0 0 0 12 2a10 10 0 0 0-8.94 5.49l3.35 2.59C7.2 7.72 9.4 5.96 12 5.96Z"/></svg>Google</button>}{providers.data.providers.includes('microsoft.com')&&<button type="button" disabled={busy||!social} onClick={()=>socialLogin('microsoft.com')}><svg viewBox="0 0 24 24" aria-hidden="true"><path fill="#F25022" d="M1 1h10v10H1z"/><path fill="#7FBA00" d="M13 1h10v10H13z"/><path fill="#00A4EF" d="M1 13h10v10H1z"/><path fill="#FFB900" d="M13 13h10v10H13z"/></svg>Microsoft</button>}</div>{linking&&<p className="community-link-hint" role="status">Confirm the existing account’s password or connected provider. Its MFA, memberships and roles are retained.</p>}</div>}
        {!message&&<p className="community-switch">{signup?'Already a member?':'New to AIMER?'} <Link to={route(signup?'/login':'/signup')}>{signup?'Sign in':'Create an account'}</Link></p>}
        <p className="community-private"><LockKeyhole size={22}/>Secure access to your community</p>
      </section>
    </main>
    <footer className="community-features">{features.map(({Icon,title,text,id})=><section id={id} key={id}><Icon/><div><h2>{title}</h2><p>{text}</p></div></section>)}</footer>
    <footer className="community-footer"><span>{content.fullName||"ARTIFICIAL INTELLIGENCE MEDICAL AND ENGINEERING RESEARCHERS SOCIETY"}</span><nav aria-label="Legal and help"><button onClick={()=>setPolicy('privacy')}>Privacy</button><i>/</i><button onClick={()=>setPolicy('terms')}>Terms</button><i>/</i><button onClick={()=>setPolicy('help')}>Help</button></nav></footer>
    {policy&&<Drawer title={policy==='help'?'Help':policy==='terms'?'Terms of Use':'Privacy Policy'} onClose={()=>setPolicy(null)}><div className="community-policy">{policy==='help'?<><h3>How can we help?</h3><p>Use Forgot password to recover your account. If you are an existing member, use your registered email address to access your memberships and ID cards.</p><p>Contact your society administrator for membership and chapter assistance.</p><Link to={route('/forgot')}>Reset your password</Link></>:(policy==='terms'?content.terms:content.privacy)?<p>{policy==='terms'?content.terms:content.privacy}</p>:<><h3>{society}</h3><p>{policy==='terms'?'Use this portal to apply for and manage memberships. Membership is subject to society review and the published membership fees. Keep your account details accurate and your login credentials secure.':'Your name and contact details are used to manage your account and memberships. Member records are protected by role-based access. You can manage directory visibility and communication preferences in your profile.'}</p><p>Contact your society administrator for the society’s full {policy==='terms'?'membership terms':'privacy policy'}.</p></>}</div></Drawer>}
  </div>;
}
