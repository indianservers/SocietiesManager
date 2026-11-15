import {useEffect, useRef, useState} from 'react';
import type {FormEvent} from 'react';
import {Link, useLocation} from 'react-router-dom';
import {ArrowRight, CheckCircle2, Eye, EyeOff, LockKeyhole, Mail, UserRound, IdCard, UsersRound, MonitorPlay, Layers, Microscope, FlaskConical, Handshake, GraduationCap, ChartNoAxesColumnIncreasing} from 'lucide-react';
import {api, ApiError} from './api';
import {Drawer, Field, useLoad} from './shared';
import {membershipKinds} from './membership-kind-picker';
import {authTree, authText} from './auth-language';
import type {AuthLanguage} from './auth-language';
import {passkey, passkeysAvailable, safeMemberReturn} from './login-tools';
import './auth-page.css';

export default function Auth({onLogin,expectedUserId}:{onLogin:()=>void;expectedUserId?:string}) {
  const location = useLocation();
  return <AuthScreen key={location.pathname} onLogin={onLogin} expectedUserId={expectedUserId}/>;
}
function AuthScreen({onLogin,expectedUserId}:{onLogin:()=>void;expectedUserId?:string}) {
  const location=useLocation(), params=new URLSearchParams(location.search);
  const signup=location.pathname==='/signup', resend=location.pathname==='/resend', forgot=location.pathname==='/forgot'||resend, reset=location.pathname==='/reset', verify=location.pathname==='/verify', magic=location.pathname==='/magic', magicRequest=location.pathname==='/magic-request';
  const [language,setLanguage]=useState<AuthLanguage>(()=>{const saved=localStorage.getItem('member.auth.language');return saved==='hi'||saved==='te'?saved:'en'});
  const [busy,setBusy]=useState(false),[message,setMessage]=useState(''),[error,setError]=useState('');
  const [rememberMe,setRememberMe]=useState(false),[showPassword,setShowPassword]=useState(false),[capsLock,setCapsLock]=useState(false);
  const [twoFactor,setTwoFactor]=useState(false),[recovery,setRecovery]=useState(false),[linking,setLinking]=useState(false);
  const [kind,setKind]=useState(params.get('kind')||sessionStorage.getItem('member.social.kind')||'Individual'),[dialCode,setDialCode]=useState('');
  const [social,setSocial]=useState<typeof import('./firebase-login')|null>(null),[redirectProvider,setRedirectProvider]=useState<'google.com'|'microsoft.com'|null>(null);
  const [policy,setPolicy]=useState<'terms'|'privacy'|'help'|null>(null);
  const formRef=useRef<HTMLFormElement>(null),errorRef=useRef<HTMLDivElement>(null),statusRef=useRef<HTMLDivElement>(null),pendingLink=useRef<string|null>(null);
  const validationField=useRef<HTMLInputElement|null>(null);
  const token=useRef(new URLSearchParams(location.hash.slice(1)).get('token')||''),redirectHandled=useRef(false);
  const providers=useLoad<any>('/auth/providers',!forgot&&!reset&&!verify&&!magic&&!magicRequest);
  const brand=useLoad<any>('/branding/resolve'+(params.get('society')?'?society='+encodeURIComponent(params.get('society')!):''));
  const catalog=useLoad<any>('/membership/catalog?tenant='+brand.data?.tenantId,signup&&!!brand.data?.tenantId);
  const content=brand.data?.authLanding||{},society=brand.data?.displayName||'Your society',community=content.communityName||society;
  const registering=signup&&!linking,login=!signup&&!forgot&&!reset&&!verify&&!magic&&!magicRequest;
  const returnTo=safeMemberReturn(params.get('returnTo')||sessionStorage.getItem('member.returnTo'));
  const text=(s:string)=>authText(s,language);
  const route=(path:string)=>path+'?'+new URLSearchParams({...params.get('society')?{society:params.get('society')!}:{},...(returnTo!=='/app/membership'?{returnTo}:{})}).toString();
  const feeFor=(value:string)=>{const offer=catalog.data?.categories?.find((c:any)=>c.kind===value)?.offer;return offer?.price!=null?new Intl.NumberFormat('en-IN',{style:'currency',currency:offer.currency,maximumFractionDigits:0}).format(offer.price)+' / '+(offer.termMonths===12?'year':offer.termMonths+' months'):''};

  useEffect(()=>{document.documentElement.lang=language;localStorage.setItem('member.auth.language',language)},[language]);
  useEffect(()=>{if(magic&&location.hash)history.replaceState(null,'',location.pathname+location.search)},[magic]);
  useEffect(()=>{if(twoFactor)formRef.current?.querySelector<HTMLInputElement>('input[name=code]')?.focus()},[twoFactor,recovery]);
  useEffect(()=>{if(error)(validationField.current||errorRef.current)?.focus()},[error]);
  useEffect(()=>{if(message)statusRef.current?.focus()},[message]);
  useEffect(()=>{const icon=document.querySelector<HTMLLinkElement>('link[rel=icon]');if(icon&&(brand.data?.favicon||content.logo)){const previous=icon.href;icon.href=brand.data?.favicon||content.logo;return()=>{icon.href=previous}}},[brand.data,content.logo]);
  useEffect(()=>{
    if(!providers.data?.enabled)return;
    let current=true;
    import('./firebase-login').then(async module=>{
      await module.prepare(providers.data.firebase);if(!current)return;setSocial(module);
      if(!redirectHandled.current){redirectHandled.current=true;const idToken=await module.redirectResult();if(idToken){setBusy(true);try{await handleSocial(idToken)}finally{setBusy(false)}}}
    }).catch(fail);
    return()=>{current=false};
  },[providers.data]);

  function fail(cause:unknown){
    setError(cause instanceof Error?cause.message:'Check your details and try again.');
    if(cause instanceof ApiError&&cause.errors){for(const [key,messages] of Object.entries(cause.errors)){const name=key.charAt(0).toLowerCase()+key.slice(1),input=formRef.current?.elements.namedItem(name) as HTMLInputElement|null;if(input){input.setCustomValidity(language==='en'?messages.join(' '):text('Please check this field.'));validationField.current=input;input.reportValidity();input.setAttribute('aria-invalid','true');input.setAttribute('aria-describedby','auth-error');input.focus();break}}}
  }
  async function finishLogin(){
    if(expectedUserId){const current=await api('/users/me');if(current.id!==expectedUserId){await api('/auth/logout','POST');throw new Error('Sign in with the same account to continue your open work.')}}
    if(pendingLink.current){await api('/auth/firebase/link','POST',{idToken:pendingLink.current});pendingLink.current=null;setLinking(false)}
    sessionStorage.setItem('member.returnTo',safeMemberReturn(sessionStorage.getItem('member.magicReturn')||returnTo));sessionStorage.removeItem('member.magicReturn');onLogin();
  }
  async function handleSocial(idToken:string){
    sessionStorage.removeItem('member.social.kind');
    const result=await api('/auth/firebase','POST',{idToken,membershipKind:kind,societyId:brand.data?.tenantId||null});
    if(result.requiresAccountLink){pendingLink.current=idToken;setLinking(true);setError(result.message);return}
    if(result.requiresEmailVerification){setMessage(result.message);return}
    if(result.requiresTwoFactor){setTwoFactor(true);return}
    await finishLogin();
  }
  async function socialLogin(provider:'google.com'|'microsoft.com'){
    if(!social)return;
    if(registering&&!formRef.current?.querySelector<HTMLInputElement>('input[name=terms]')?.checked){setError('Please agree to the Terms of Use and Privacy Policy before creating an account.');return}
    setBusy(true);setError('');setRedirectProvider(null);
    try{await handleSocial(await social.popup(provider))}catch(e){if((e as any)?.code==='auth/popup-blocked')setRedirectProvider(provider);fail(e)}finally{setBusy(false)}
  }
  async function restart(){setBusy(true);try{await api('/auth/restart','POST');pendingLink.current=null;setLinking(false);setTwoFactor(false);setRecovery(false);setError('');formRef.current?.reset()}catch(e){fail(e)}finally{setBusy(false)}}
  async function alternateLogin(){setBusy(true);setError('');try{const result=magic?await api('/auth/magic/consume','POST',{token:token.current}):await passkey(false,'My passkey',rememberMe);if(result.requiresTwoFactor){setTwoFactor(true);return}await finishLogin()}catch(e){fail(e)}finally{setBusy(false)}}
  async function submit(event:FormEvent<HTMLFormElement>){
    event.preventDefault();setBusy(true);setError('');validationField.current=null;const fields=Object.fromEntries(new FormData(event.currentTarget));
    try{
      let result;
      if(twoFactor)result=await api('/auth/two-factor','POST',{code:fields.code,recovery,rememberMe});
      else if(magicRequest)result=await api('/auth/magic/request','POST',{email:fields.email,returnTo});
      else if(verify)result=await api('/auth/verify?userId='+params.get('userId')+'&token='+encodeURIComponent(params.get('token')||''),'POST');
      else if(reset)result=await api('/auth/reset','POST',{...fields,email:params.get('email'),token:params.get('token')});
      else result=await api('/auth/'+(registering?'signup':resend?'resend-verification':forgot?'forgot':'login'),'POST',{...fields,password:fields.password||'',rememberMe,...(registering?{membershipKind:kind,societyId:brand.data?.tenantId||null,phoneNumber:fields.mobile?String(fields.countryCode)+String(fields.mobile).replace(/[^0-9]/g,''):null}:{})});
      if(result.requiresTwoFactor){setTwoFactor(true);return}
      if(twoFactor||linking||login)await finishLogin();else setMessage(result.message);
    }catch(e){fail(e)}finally{setBusy(false)}
  }
  const title=twoFactor?'Verify it’s you':linking?'Confirm your existing account':magic?'Confirm sign-in':magicRequest?'Email me a sign-in link':resend?'Verify your email':signup?'Join the '+community+' community':forgot?'Reset your password':reset?'Choose a new password':verify?'Verify your email':content.loginHeading||brand.data?.loginTitle||'Welcome back';
  const subtitle=twoFactor?'Enter your authenticator or single-use recovery code.':forgot?'We’ll help you get back to your community.':reset?'Create a strong password for your account.':verify?'Confirm your email to continue.':language==='hi'?'अपने '+society+' खाते में साइन इन करें।':language==='te'?society+' ఖాతాలో సైన్ ఇన్ చేయండి.':'Sign in to your '+society+' workspace.';
  const features=[{Icon:IdCard,title:'Member identity',text:'Your profile, membership and digital ID.',id:'membership'},{Icon:UsersRound,title:'Institutional chapters',text:'Connect with your institutional network.',id:'chapters'},{Icon:MonitorPlay,title:'Interactive learning',text:'Access tools, resources and skill-building content.',id:'learning'},{Icon:Layers,title:'Research opportunities',text:'Discover publications, workshops and internships.',id:'research'}];

  return <>{authTree(<div className="community-auth">
    <svg width="0" height="0" aria-hidden="true"><defs><linearGradient id="community-icon-gradient" gradientUnits="userSpaceOnUse" x1="0" y1="24" x2="24" y2="0"><stop stopColor="#7714cf"/><stop offset=".5" stopColor="#7410c9"/><stop offset="1" stopColor="#00c4df"/></linearGradient></defs></svg>
    <header className="community-header"><Link className="community-brand" to={route('/login')} aria-label={society+' home'}><img src={brand.data?.logo||content.logo||'/images/community-brain-chip.png'} alt={society+' logo'} width="104" height="80"/><span><strong>{society}</strong><small>{content.brandSubtitle||'MEMBERS & CHAPTERS'}</small></span></Link><label className="auth-language"><span>Language / भाषा / భాష</span><select aria-label="Language" value={language} onChange={e=>setLanguage(e.target.value as AuthLanguage)}><option value="en">English</option><option value="hi">हिन्दी</option><option value="te">తెలుగు</option></select></label><nav aria-label="Explore the community"><a href="#membership">Membership</a><a href="#chapters">{brand.data?.chapterTerm?brand.data.chapterTerm+'s':'Chapters'}</a><a href="#learning">Learning tools</a><a href="#research">Research</a><Link className="community-join" to={route('/signup')}>Join the community</Link></nav></header>
    <main className="community-auth-main" id="main-content">
      <section className="community-story" aria-label="Our community"><p className="community-kicker">RESEARCH · LEARNING · COMMUNITY</p><h1>{content.headline||<>Great minds.<br/>Meaningful connections.</>}<em>{content.accentHeadline||'New possibilities.'}</em></h1><p className="community-introduction">{content.description||'A community for researchers, educators and innovators shaping what comes next.'}</p><div className="community-artwork" aria-hidden="true"><span className="auth-orbit orbit-a"/><span className="auth-orbit orbit-b"/><span className="auth-orbit orbit-c"/><img src={content.illustration||'/images/community-glass-brain.png'} width="600" height="400" alt="" fetchPriority="high"/><span className="auth-floating-icon research"><Microscope/></span><span className="auth-floating-icon layers"><Layers/></span><span className="auth-floating-icon people"><UsersRound/></span><span className="auth-floating-icon science"><FlaskConical/></span><i className="auth-orbit-dot dot-one"/><i className="auth-orbit-dot dot-two"/></div><div className="community-pills"><a href="#chapters"><Handshake/><span>Collaborate</span></a><a href="#learning"><GraduationCap/><span>Learn</span></a><a href="#research"><ChartNoAxesColumnIncreasing/><span>Grow</span></a></div></section>
      <section className={'community-auth-card'+(!signup?' community-login-card':'')} aria-label={signup?'Create an account':'Account access'}>
        <div className="community-auth-tabs" aria-label="Account access options"><Link className={!signup?'selected':''} aria-current={!signup?'page':undefined} to={route('/login')}>Login</Link><Link className={signup?'selected':''} aria-current={signup?'page':undefined} to={route('/signup')}>Sign up</Link></div>
        <div className="community-card-heading"><span className="community-heading-icon"><UserRound/></span><div><h2>{title}</h2><p>{subtitle}</p></div></div>
        {error&&<div id="auth-error" ref={errorRef} tabIndex={-1} role="alert" className="auth-error">{language==='en'?error:text('Check your details and try again. You can also reset your password or request a verification email.')}</div>}
        {message?<div ref={statusRef} tabIndex={-1} className="community-auth-success" role="status"><CheckCircle2/><p>{language==='en'?message:reset?text('Password reset. Sign in with your new password.'):verify?text('Email verified. You can sign in.'):text('Request completed. Check your inbox and spam folder for the next step.')}</p>{!reset&&!verify&&<><p>Check your spam folder and confirm you entered the correct email.</p><button type="button" onClick={()=>{setMessage('');setError('')}}>Correct email / try again</button>{!magicRequest&&<Link to={route('/resend')}>Request a fresh verification email</Link>}</>}<Link className="auth-submit primary" to={route('/login')}>Continue to sign in <ArrowRight/></Link></div>:magic&&!twoFactor?<div className="community-account-form"><p>{language==='hi'?'इस ब्राउज़र में ईमेल लिंक से साइन-इन की पुष्टि करें।':language==='te'?'ఈ బ్రౌజర్‌లో ఈమెయిల్ లింక్‌తో సైన్ ఇన్ నిర్ధారించండి.':'Confirm that you want to sign in on this browser using your email link.'}</p><button className="auth-submit primary" disabled={busy||!token.current} onClick={alternateLogin}>Confirm sign-in</button><Link to={route('/magic-request')}>Email me a sign-in link</Link></div>:<form ref={formRef} className="community-account-form" aria-busy={busy} onInvalidCapture={e=>{if(language!=='en'){const input=e.target as HTMLInputElement;input.setCustomValidity(text(input.validity.valueMissing?'Please complete this field.':'Please check this field.'));input.setAttribute('aria-invalid','true')}}} onInput={e=>{const input=e.target as HTMLInputElement;input.setCustomValidity?.('');input.removeAttribute('aria-invalid')}} onSubmit={submit}>
          {twoFactor?<><Field label={recovery?'Recovery code':'Authenticator code'} name="code" key={String(recovery)} autoComplete="one-time-code" inputMode={recovery?'text':'numeric'} pattern={recovery?undefined:'[0-9 -]{6,8}'} required/><button type="button" className="text-btn" onClick={()=>{setRecovery(!recovery);setError('')}}>{recovery?'Use authenticator instead':'Use a single-use recovery code'}</button><button type="button" className="text-btn" disabled={busy} onClick={restart}>Use another account / restart</button></>:<>
            {registering&&<div className="community-name-phone"><Field label="Full name" name="fullName" autoComplete="name" placeholder="Enter your full name" maxLength={160} required/><Field label="Mobile number"><div className="community-mobile"><select aria-label="Country calling code" name="countryCode" value={dialCode||content.defaultDialCode||'+1'} onChange={e=>setDialCode(e.target.value)}>{[...new Set([content.defaultDialCode||'+1','+91','+1','+44','+61','+971','+65'])].map(code=><option key={code}>{code}</option>)}</select><input aria-label="Mobile number" name="mobile" autoComplete="tel-national" type="tel" pattern="[0-9 ]{7,20}" placeholder="Enter mobile number"/></div></Field></div>}
            {!verify&&!reset&&<Field label={registering||forgot||magicRequest?'Email address':'Email or username'}><div className="community-input-icon"><Mail aria-hidden="true"/><input name="email" type={registering||forgot||magicRequest?'email':'text'} autoComplete={registering||forgot||magicRequest?'email':'username'} placeholder="you@example.com" maxLength={254} required/></div></Field>}
            {registering&&<details className="community-username"><summary>Choose a username (optional)</summary><Field label="Username" name="username" autoComplete="username" minLength={3} maxLength={80} placeholder="Choose a unique username"/></details>}
            {registering&&<Field label="I am a"><select name="membershipKind" value={kind} onChange={e=>setKind(e.target.value)} aria-describedby="signup-fees">{membershipKinds.map(value=><option key={value} value={value}>{value} member{feeFor(value)?' — '+feeFor(value):''}</option>)}</select><small id="signup-fees" className="community-membership-prices">{membershipKinds.map(value=>feeFor(value)?<span key={value}>{value}: {feeFor(value)}</span>:null)}</small></Field>}
            {!verify&&!forgot&&!magicRequest&&<Field label={reset?'New password':'Password'}><div className="community-password"><LockKeyhole className="community-input-leading" aria-hidden="true"/><input aria-label={reset?'New password':'Password'} name="password" type={showPassword?'text':'password'} minLength={registering||reset?12:undefined} autoComplete={registering||reset?'new-password':'current-password'} placeholder={registering||reset?'Create a strong password':'Enter your password'} onKeyUp={e=>setCapsLock(e.getModifierState('CapsLock'))} onKeyDown={e=>setCapsLock(e.getModifierState('CapsLock'))} onBlur={()=>setCapsLock(false)} required/><button type="button" aria-label={showPassword?'Hide password':'Show password'} aria-pressed={showPassword} onClick={()=>setShowPassword(!showPassword)}>{showPassword?<EyeOff/>:<Eye/>}</button></div>{capsLock&&<small role="status">Caps Lock is on</small>}{(registering||reset)&&<small className="community-password-hint">12+ characters with uppercase, lowercase, a number and symbol.</small>}</Field>}
            {registering&&<label className="community-consent"><input name="terms" type="checkbox" required/><span>I agree to the <button type="button" onClick={()=>setPolicy('terms')}>Terms of Use</button> and <button type="button" onClick={()=>setPolicy('privacy')}>Privacy Policy</button>.</span></label>}
            {login&&<><div className="community-login-help"><label><input type="checkbox" checked={rememberMe} onChange={e=>setRememberMe(e.target.checked)}/>Remember me</label><Link to={route('/forgot')}>Forgot password?</Link></div><small>Use a personal device. Remember me keeps your cookie for up to 8 hours of inactivity; activity renews it.</small></>}
          </>}
          <button type="submit" disabled={busy} className="auth-submit primary">{busy?'Please wait…':twoFactor?'Verify & sign in':magicRequest?'Send sign-in link':verify?'Verify email':linking?'Confirm account':signup?'Create account':resend?'Resend verification email':forgot?'Send recovery email':reset?'Reset password':'Sign in'}<ArrowRight/></button>
        </form>}
        {!message&&!twoFactor&&(login||signup)&&providers.data?.enabled&&<div className="community-social"><div className="community-social-divider"><span>or continue with</span></div><div className="community-social-buttons">{(['google.com','microsoft.com'] as const).filter(p=>providers.data.providers.includes(p)).map(p=><button key={p} type="button" disabled={busy||!social} onClick={()=>socialLogin(p)}>{p==='google.com'?'Google':'Microsoft'}</button>)}</div>{linking&&<p className="community-link-hint" role="status">Confirm the existing account’s password or connected provider. Its MFA, memberships and roles are retained.</p>}</div>}
        {!message&&!twoFactor&&login&&<div className="auth-alternatives">{passkeysAvailable()&&<button type="button" disabled={busy} onClick={alternateLogin}>Sign in with a passkey</button>}<Link to={route('/magic-request')}>Email me a sign-in link</Link><Link to={route('/resend')}>Resend verification email</Link></div>}
        {redirectProvider&&social&&<button type="button" disabled={busy} onClick={()=>{setBusy(true);sessionStorage.setItem('member.social.kind',kind);social.redirect(redirectProvider).catch(fail).finally(()=>setBusy(false))}}>Open sign-in in this tab</button>}
        {magicRequest&&<Link to={route('/login')}>Use your password instead</Link>}
        {(login||signup)&&!twoFactor&&<p className="community-switch">Already registered with the society? Use your registered personal email or approved organization contact email. Existing memberships appear after verification.</p>}
        {!message&&(login||signup)&&<p className="community-switch">{signup?'Already a member?':'New to '+society+'?'} <Link to={route(signup?'/login':'/signup')}>{signup?'Sign in':'Create an account'}</Link></p>}
        <span role="status" aria-live="polite" className="auth-sr-only">{busy?text('Please wait…'):''}</span><p className="community-private"><LockKeyhole size={22}/>Secure access to your community</p>
      </section>
    </main>
    <footer className="community-features">{features.map(({Icon,title,text:description,id})=><section id={id} key={id}><Icon/><div><h2>{title}</h2><p>{description}</p></div></section>)}</footer>
    <footer className="community-footer"><span>{content.fullName||society}</span><nav aria-label="Legal and help"><button onClick={()=>setPolicy('privacy')}>Privacy</button><i>/</i><button onClick={()=>setPolicy('terms')}>Terms</button><i>/</i><button onClick={()=>setPolicy('help')}>Help</button></nav></footer>
    {policy&&<Drawer title={policy==='help'?'Help':policy==='terms'?'Terms of Use':'Privacy Policy'} onClose={()=>setPolicy(null)}><div className="community-policy">{policy==='help'?<><h3>How can we help?</h3><p>Use your registered email or username to sign in. Recover your password, request a new verification email, or contact your society administrator for membership assistance.</p><Link to={route('/forgot')}>Reset your password</Link><Link to={route('/resend')}>Resend verification email</Link></>:(policy==='terms'?content.terms:content.privacy)?<p>{policy==='terms'?content.terms:content.privacy}</p>:<><h3>{society}</h3><p>{policy==='terms'?'Membership is subject to society review and the published fees. Keep your account details accurate and your login credentials secure.':'Your name and contact details are used to manage your account and memberships. You can manage directory visibility and communication preferences in your profile.'}</p><p>Contact your society administrator for the society’s full policy.</p></>}</div></Drawer>}
  </div>,language)}</>;
}
