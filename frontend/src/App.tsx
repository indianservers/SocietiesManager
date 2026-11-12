import {FormTabs} from './form-tabs';
import {AreaTabs} from './area-tabs';
import {WorkspaceSections,WorkspaceAlias} from './workspace-navigation';
import { useState, useEffect, lazy, Suspense } from "react";

import type { FormEvent } from "react";

import {

  Routes,

  Route,

  Link,

  NavLink,

  useNavigate,

  useLocation,

  useParams,

} from "react-router-dom";

import { useQueryClient } from "@tanstack/react-query";

import {

  MessageCircle,
  Wallet,
  FileBadge,

  LayoutDashboard,

  Network,

  MapPin,

  Flag,

  ShieldCheck,

  Settings,


  Plus,

  Search,

  ArrowUpRight,

  ArrowRight,

  ChevronRight,

  Check,

  LogOut,

  Menu,

  X,

  Layers,

  Globe,

  Users,

} from "lucide-react";

import { api, details } from "./api";

import type { Level, Unit } from "./api";

import {

  Ctx,

  useApp,

  Field,

  Badge,

  Empty,

  Heading,

  Panel,

  Drawer,

  DataState,

  useLoad,

  useAction,

} from "./shared";

const PublicActivity = lazy(() =>

  import("./community").then((m) => ({ default: m.PublicActivity })),

);

const IntelligenceModule = lazy(() => import("./intelligence"));
const WhiteLabelSettings = lazy(() => import("./platform").then(m=>({default:m.WhiteLabelSettings})));
const SecurityWorkspace = lazy(() => import("./platform").then(m=>({default:m.SecurityWorkspace})));
const PlatformWorkspace = lazy(() => import("./platform").then(m=>({default:m.PlatformWorkspace})));
const HealthWorkspace = lazy(() => import("./platform").then(m=>({default:m.HealthWorkspace})));
const ServiceModule = lazy(() => import("./service"));
const StudioModule = lazy(() => import("./studio"));
const MyDocuments = lazy(()=>import('./studio').then(m=>({default:m.MyDocuments})));
const MyAchievements = lazy(()=>import('./service').then(m=>({default:m.MyAchievements})));
const MyMemberships = lazy(()=>import('./membership').then(m=>({default:m.SelfService})));
const CredentialVerification = lazy(() => import("./studio").then(m => ({default:m.CredentialVerification})));
const FinanceModule = lazy(() => import("./finance"));
const CommunicationsModule = lazy(() => import("./communications"));

const CommunicationNotifications = lazy(() => import("./communications").then(m => ({default:m.CommunicationNotifications})));

const Unsubscribe = lazy(() => import("./communications").then(m => ({default:m.Unsubscribe})));

const CommunityModule = lazy(() => import("./community"));

const MembershipModule = lazy(() => import("./membership"));

const JoinMembership = lazy(() =>

  import("./membership").then((m) => ({ default: m.JoinMembership })),

);

export default function App() {

  const client = useQueryClient();

  const nav = useNavigate();

  const loc = useLocation();

  const me = useLoad("/users/me");

  const spaces = useLoad<any[]>("/workspaces", !!me.data);

  const [selection, setSelection] = useState("");

  const [toast, notify] = useState("");

  const [mobile, setMobile] = useState(false);
  const [theme,setTheme]=useState(()=>localStorage.getItem("assemblia.theme")||"light");
  useEffect(()=>{document.documentElement.dataset.theme=theme;localStorage.setItem("assemblia.theme",theme)},[theme]);

  const workspace =

    spaces.data?.find((w) => w.id === selection.split(":")[0]) ||

    spaces.data?.[0];

  const tenant = workspace?.id || "";

  const scope = selection.split(":")[1] || "";

  useEffect(() => {

    if (

      !selection &&

      workspace?.scopes?.length &&

      !workspace.scopes.some((s: any) => !s.unitId)

    )

      setSelection(workspace.id + ":" + workspace.scopes[0].unitId);

  }, [selection, workspace?.id, workspace?.scopes]);

  const scopes = workspace?.scopes || [];

  const branding = details(workspace?.profile);

  useEffect(() => {

    const color = branding.brandColor;

    if (/^#[0-9a-fA-F]{6}$/.test(color || ""))

      document.documentElement.style.setProperty("--brand-color", color);

    else document.documentElement.style.removeProperty("--brand-color");

  }, [branding.brandColor]);

  const enabled=(module:string)=>!module||!workspace?.modules||workspace.modules.includes(module);
  const can = (p: string) =>
    enabled(p.startsWith("finance.")?"finance":p.startsWith("studio.")?"studio":p.startsWith("service.")?"service":p.startsWith("intelligence.")||p.startsWith("reports.")||p.startsWith("assistant.")?"intelligence":p.startsWith("communications.")?"communications":p.startsWith("activities.")||p.startsWith("events.")?"community":"") && scopes.some(

      (s: any) =>

        (!s.unitId || s.unitId === scope) && s.permissions.includes(p),

    );

  const refresh = () => {

    void client.invalidateQueries();

  };

  useEffect(() => {

    if (toast) {

      const t = setTimeout(() => notify(""), 6000);

      return () => clearTimeout(t);

    }

  }, [toast]);

  useEffect(() => setMobile(false), [loc.pathname, loc.search]);

  const value = {

    tenant,

    workspace,

    scope,

    notify,

    refresh,

    can,

    switchWorkspace: (tenant: string, scope = "") =>

      setSelection(tenant + ":" + scope),

  };

  const publicPath =

    ["/", "/login", "/signup", "/forgot", "/reset", "/verify", "/unsubscribe"].includes(

      loc.pathname,

    ) || loc.pathname.startsWith("/public/community/") || loc.pathname.startsWith("/verify/credential/");

  return (

    <Ctx.Provider value={value}>

      {publicPath ? (

        <Routes>

          <Route path="/" element={<Landing />} /><Route path="/verify/credential/:token" element={<Suspense fallback={<p>Checking credential…</p>}><CredentialVerification/></Suspense>}/><Route path="/unsubscribe" element={<Suspense fallback={<p>Loading…</p>}><Unsubscribe/></Suspense>}/>

          <Route

            path="/public/community/:tenant/posts/:id"

            element={

              <Suspense fallback={<p>Loading public activity…</p>}>

                <PublicActivity />

              </Suspense>

            }

          />

          <Route

            path="*"

            element={

              <Auth

                onLogin={() => {

                  refresh();

                  nav("/app");

                }}

              />

            }

          />

        </Routes>

      ) : me.isPending ? (

        <div className="skeleton">Opening your workspace…</div>

      ) : !me.data ? (

        <Auth

          onLogin={() => {

            refresh();

            nav(loc.pathname + loc.search);

          }}

        />

      ) : (

        <div className="app-shell">

          <aside className={mobile ? "sidebar open" : "sidebar"}>

            <Link to="/app" className="brand">

              <div className="brand-mark">

                <Network size={23} />

              </div>

              <span>

                {branding.displayName || workspace?.name || "Assemblia"}

                <span className="brand-sub">ASSOCIATION WORKSPACE</span>

              </span>

            </Link>

            <div className="workspace-label">WORKSPACE</div>

            <select

              aria-label="Active workspace"

              className="workspace-select"

              value={selection || tenant + ":"}

              onChange={(e) => {

                setSelection(e.target.value);

                client.removeQueries({

                  predicate: (q) =>

                    String(q.queryKey[0]).startsWith("/societies/"),

                });

                nav("/app");

              }}

            >

              {!spaces.data?.length && <option>No society workspace</option>}

              {spaces.data?.flatMap((w) => [

                <option key={w.id} value={w.id + ":"}>

                  {w.name}

                </option>,

                ...w.scopes

                  .filter((s: any) => s.unitId)

                  .map((s: any, i: number) => (

                    <option key={w.id + i} value={w.id + ":" + s.unitId}>

                      ↳ {s.unitName || "Organization"} · {s.name}

                    </option>

                  )),

              ])}

            </select>

            <nav aria-label="Main workspace navigation"><span className="nav-label">MAIN MENU</span>
              <NavLink end to="/app" className={()=>loc.pathname==='/app'||loc.pathname.startsWith('/app/intelligence')?'active':''}><LayoutDashboard/>Home</NavLink>
              {tenant&&<>
                <NavLink to="/app/members" className={()=>['/app/members','/app/organization','/app/units'].some(p=>loc.pathname.startsWith(p))?'active':''}><Users/>People</NavLink>
                {(enabled('community')||enabled('service'))&&<NavLink to={enabled('community')?'/app/community':'/app/service/projects'} className={()=>loc.pathname.startsWith('/app/community')||loc.pathname.startsWith('/app/service')?'active':''}><Globe/>Activities</NavLink>}
                {enabled('communications')&&<NavLink to="/app/communications"><MessageCircle/>Messages</NavLink>}
                {enabled('finance')&&<NavLink to="/app/finance"><Wallet/>Finance</NavLink>}
                {(can('studio.read')||can('studio.bulk')||can('studio.issue')||can('studio.settings'))&&<NavLink to="/app/studio"><FileBadge/>Documents</NavLink>}
                {(can('settings.manage')||can('branding.manage')||can('roles.manage')||can('audit.read'))&&<NavLink to="/app/settings"><Settings/>Settings</NavLink>}
              </>}
              {me.data.platformAdmin&&<NavLink to="/app/platform"><ShieldCheck/>Platform</NavLink>}
            </nav>

            <div className="sidebar-foot">

              <div className="phase-dot" />

              Phase 6 · Connected society platform

            </div>

            <button className="account" onClick={() => nav("/app/account")}>

              <span className="avatar">

                {me.data.fullName.slice(0, 2).toUpperCase()}

              </span>

              <span>

                {me.data.fullName}

                <small>

                  My account

                </small>

              </span>

              <ChevronRight size={15} />

            </button>

          </aside>

          <div className="main-shell">

            <header className="topbar">

              <button

                aria-label="Toggle navigation"

                className="mobile-menu icon-btn"

                onClick={() => setMobile(!mobile)}

              >

                <Menu />

              </button>

              <span className="breadcrumb">

                {loc.pathname.startsWith('/app/finance')?'Finance':loc.pathname.startsWith('/app/members')||loc.pathname.startsWith('/app/organization')||loc.pathname.startsWith('/app/units')?'People':loc.pathname.startsWith('/app/community')||loc.pathname.startsWith('/app/service')?'Activities':loc.pathname.startsWith('/app/communications')?'Messages':loc.pathname.startsWith('/app/studio')?'Documents':loc.pathname.startsWith('/app/settings')?'Settings':loc.pathname.startsWith('/app/account')?'My account':'Home'} <ChevronRight size={13} />{" "}

                <strong>{workspace?.name || "Get started"}</strong>

              </span>

              <div className="top-right">

                <span className="secure">

                  <ShieldCheck size={14} />

                  Secure workspace

                </span>

                <button aria-label="Toggle light or dark appearance" className="icon-btn" onClick={()=>setTheme(theme==="dark"?"light":"dark")}>{theme==="dark"?"☀":"☾"}</button><Suspense fallback={null}><CommunicationNotifications/></Suspense>

                <button

                  className="icon-btn"

                  aria-label="Sign out"

                  onClick={async () => {

                    await api("/auth/logout", "POST");
                    setSelection("");

                    client.clear();

                    nav("/login");

                  }}

                >

                  <LogOut size={18} />

                </button>

              </div>

            </header>

            <main key={tenant + scope}>

              <Routes><Route path="/app/intelligence/*" element={<Suspense fallback={<p>Loading intelligence…</p>}><IntelligenceModule/></Suspense>}/><Route path="/app/branding" element={<WorkspaceAlias to="/app/settings" query={{view:"branding"}}/>}/><Route path="/app/security" element={<WorkspaceAlias to="/app/account" query={{view:"security"}}/>}/><Route path="/app/saas" element={<WorkspaceAlias to="/app/platform" query={{view:"operations"}}/>}/><Route path="/app/system-health" element={<WorkspaceAlias to="/app/settings" query={{view:"readiness"}}/>}/><Route path="/app/service/*" element={<Suspense fallback={<p>Opening service workspace…</p>}><ServiceModule/></Suspense>}/><Route path="/app/studio/*" element={<Suspense fallback={<p>Opening document studio…</p>}><StudioModule/></Suspense>}/><Route path="/app/finance/*" element={<Suspense fallback={<p>Loading finance…</p>}><FinanceModule/></Suspense>}/><Route path="/app/communications/*" element={<Suspense fallback={<p>Loading communications…</p>}><CommunicationsModule/></Suspense>}/>

                <Route path="/app/join" element={<WorkspaceAlias to="/app/account" query={{view:'join'}}/>}/>
                <Route path="/app/account" element={<AccountWorkspace me={me.data}/>}/>

                <Route

                  path="/app/community/*"

                  element={

                    <Suspense fallback={<p>Loading community…</p>}>

                      <CommunityModule />

                    </Suspense>

                  }

                />

                <Route

                  path="/app/members/*"

                  element={

                    <Suspense fallback={<p>Loading membership workspace…</p>}>

                      <MembershipModule />

                    </Suspense>

                  }

                />

                <Route

                  path="/app"

                  element={tenant ? <><AreaTabs area="home"/><Dashboard /></> : <Welcome />}

                />

                <Route

                  path="/app/applications"

                  element={<WorkspaceAlias to={me.data.platformAdmin?"/app/platform":"/app/account"} query={{view:me.data.platformAdmin?"approvals":"applications"}}/>}

                />

                <Route path="/app/register-society" element={<SocietyForm />} />

                <Route path="/app/profile" element={<WorkspaceAlias to="/app/account" query={{view:"profile"}}/>} />

                {me.data.platformAdmin && (

                  <Route path="/app/platform" element={<PlatformHub />} />

                )}

                <Route

                  path="/app/accept-invitation"

                  element={<AcceptInvitation />}

                />

                {tenant && (

                  <>

                    <Route path="/app/organization" element={<OrganizationWorkspace/>}/><Route path="/app/hierarchy" element={<WorkspaceAlias to="/app/organization" query={{view:"hierarchy"}}/>} />

                    <Route

                      path="/app/regions"

                      element={<WorkspaceAlias to="/app/organization" query={{view:"regions"}}/>}

                    />

                    <Route

                      path="/app/chapters"

                      element={<WorkspaceAlias to="/app/organization" query={{view:"chapters"}}/>}

                    />

                    <Route path="/app/units/:id" element={<UnitOverview />} />

                    <Route path="/app/roles" element={<WorkspaceAlias to="/app/settings" query={{view:"roles"}}/>} />

                    <Route path="/app/setup" element={<WorkspaceAlias to="/app/settings" query={{view:"setup"}}/>} />

                    <Route path="/app/settings" element={<AdministrationWorkspace/>} />

                    <Route path="/app/audit" element={<WorkspaceAlias to="/app/settings" query={{view:"audit"}}/>} />

                  </>

                )}

                <Route

                  path="*"

                  element={

                    <Empty

                      title="Workspace unavailable"

                      text="Select an authorized workspace to continue."

                    />

                  }

                />

              </Routes>

            </main>

            <footer className="page-footer">

              <span>Assemblia · Built for stronger communities</span>

              <span>Organization management / Phase 06</span>

            </footer>

          </div>

        </div>

      )}

      {toast && (

        <div className="toast" role="status">

          <span>{toast}</span>

          <button aria-label="Dismiss notification" onClick={() => notify("")}>

            <X size={16} />

          </button>

        </div>

      )}

    </Ctx.Provider>

  );

}

function HubContent({children}:any){return <Suspense fallback={<p>Opening workspace section…</p>}>{children}</Suspense>}
function OrganizationWorkspace(){const {workspace}=useApp();const branding=details(workspace?.profile);return <><AreaTabs area="people"/><WorkspaceSections label="Organization" sections={[{id:'hierarchy',label:'Hierarchy & structure',content:<Hierarchy/>},{id:'regions',label:(branding.regionTerm||'Region')+' directory',content:<Directory kind="Region"/>},{id:'chapters',label:(branding.chapterTerm||'Chapter')+' directory',content:<Directory kind="Chapter"/>}]}/></>}
function AdministrationWorkspace(){const {can}=useApp();return <WorkspaceSections label="Society administration" sections={[{id:'society',label:'General',content:<SocietySettings/>,visible:can('settings.manage')},{id:'branding',label:'Branding',content:<HubContent><WhiteLabelSettings/></HubContent>,visible:can('branding.manage')},{id:'roles',label:'Permissions',content:<Roles/>,visible:can('roles.manage')},{id:'setup',label:'Setup',content:<Setup/>,visible:can('settings.manage')},{id:'audit',label:'Audit',content:<AuditLog/>,visible:can('audit.read')},{id:'readiness',label:'Health',content:<HubContent><HealthWorkspace/></HubContent>,visible:can('settings.manage')}]}/>}
function AccountWorkspace({me}:any){const {tenant,workspace}=useApp();const enabled=(m:string)=>!!tenant&&(!workspace?.modules||workspace.modules.includes(m));return <WorkspaceSections label="My account & memberships" sections={[{id:'profile',label:'Profile',content:<Profile me={me}/>},{id:'security',label:'Security',content:<WorkspaceSections label="Account security" queryKey="securityTab" sections={[{id:'authenticator',label:'Authenticator',content:<HubContent><SecurityWorkspace/></HubContent>},{id:'password',label:'Password',content:<Profile me={me} mode="password"/>}]}/>},{id:'memberships',label:'Memberships',content:<HubContent><MyMemberships/></HubContent>},{id:'documents',label:'Documents',content:<HubContent><MyDocuments/></HubContent>,visible:enabled('studio')},{id:'achievements',label:'Achievements',content:<HubContent><MyAchievements/></HubContent>,visible:enabled('service')},{id:'applications',label:'Applications',content:<Applications platform={false}/>,visible:!me.platformAdmin},{id:'join',label:'Join',content:<HubContent><JoinMembership/></HubContent>}]}/>}
function PlatformHub(){return <WorkspaceSections label="Platform administration" sections={[{id:'operations',label:'SaaS operations & health',content:<HubContent><PlatformWorkspace/></HubContent>},{id:'approvals',label:'Society approvals',content:<Applications platform/>},{id:'registry',label:'Society registry',content:<Platform/>}]}/>}

function Landing() {

  return (

    <div className="landing">

      <header>

        <Link className="brand" to="/">

          <span className="brand-mark">

            <Network />

          </span>

          Assemblia

        </Link>

        <Link to="/login" className="secondary">

          Sign in <ArrowUpRight size={16} />

        </Link>

      </header>

      <div className="hero">

        <div className="eyebrow">ONE COMMUNITY. EVERY CHAPTER.</div>

        <h1>

          Great organizations.

          <br />

          <em>Stronger connections.</em>

        </h1>

        <p>

          A thoughtful workspace for your society, regions, and chapters. Bring

          structure to your organization and give every community a place to

          grow.

        </p>

        <div className="hero-actions">

          <Link className="primary" to="/signup">

            Create your account <ArrowRight size={17} />

          </Link>

          <Link className="secondary" to="/login">

            Open your workspace

          </Link>

        </div>

        <div className="hero-trust">

          <ShieldCheck size={17} /> Private workspaces <span>·</span> Flexible

          hierarchy <span>·</span> Scoped access

        </div>

      </div>

      <div className="landing-visual">

        <div className="orbit o1" />

        <div className="orbit o2" />

        <div className="community-root">

          <Network />

          <strong>Your society</strong>

          <small>Connected at every level</small>

        </div>

        <div className="node n1">

          <Globe />

          National body

        </div>

        <div className="node n2">

          <MapPin />

          Your regions

        </div>

        <div className="node n3">

          <Flag />

          Local chapters

        </div>

        <div className="node n4">

          <Users />

          Your community

        </div>

      </div>

      <div className="landing-bottom">

        A foundation for societies, associations, and communities of every size.

      </div>

    </div>

  );

}

function Auth({ onLogin }: any) {

  const path = useLocation().pathname;

  const params = new URLSearchParams(useLocation().search);

  const { notify } = useApp();

  const [busy, setBusy] = useState(false);

  const [message, setMessage] = useState("");
  const [twoFactor,setTwoFactor] = useState(false);
  const brand=useLoad<any>("/branding/resolve"+(params.get("society")?"?society="+encodeURIComponent(params.get("society")!):""));
  useEffect(()=>{if(brand.data?.primaryColor)document.documentElement.style.setProperty("--brand-color",brand.data.primaryColor);if(brand.data?.language)document.documentElement.lang=brand.data.language;const icon=document.querySelector<HTMLLinkElement>("link[rel=icon]");if(icon&&brand.data?.favicon){const previous=icon.href;icon.href=brand.data.favicon;return()=>{icon.href=previous}}},[brand.data]);

  const signup = path === "/signup",

    forgot = path === "/forgot",

    reset = path === "/reset",

    verify = path === "/verify";

  const title = signup

    ? "Start something meaningful"

    : forgot

      ? "Let’s get you back in"

      : reset

        ? "Choose a new password"

        : verify

          ? "Verify your email"

          : "Welcome back";

  async function submit(e: FormEvent<HTMLFormElement>) {

    e.preventDefault();

    setBusy(true);

    const f = Object.fromEntries(new FormData(e.currentTarget));

    try {

      let r;

      if(twoFactor) r=await api("/auth/two-factor","POST",{code:f.code,recovery:f.recovery==="on"});
      else if (verify)

        r = await api(

          "/auth/verify?userId=" +

            params.get("userId") +

            "&token=" +

            encodeURIComponent(params.get("token") || ""),

          "POST",

        );

      else if (reset)

        r = await api("/auth/reset", "POST", {

          ...f,

          email: params.get("email"),

          token: params.get("token"),

        });

      else

        r = await api(

          "/auth/" + (signup ? "signup" : forgot ? "forgot" : "login"),

          "POST",

          { ...f, password: f.password || "" },

        );

      if(r.requiresTwoFactor){setTwoFactor(true);return;}
      if (!signup && !forgot && !reset && !verify) onLogin();

      else setMessage(r.message);

    } catch (e) {

      notify((e as Error).message);

    } finally {

      setBusy(false);

    }

  }

  return (

    <div className="auth-page">

      <div className="auth-story">

        <Link className="brand" to="/">

          <span className="brand-mark">

            <Network />

          </span>

          {brand.data?.logo&&<img alt="Society logo" src={brand.data.logo} style={{height:32,maxWidth:100,objectFit:"contain"}}/>}{brand.data?.displayName||"Assemblia"}

        </Link>

        <div>

          <div className="eyebrow">A PLACE FOR EVERY CHAPTER</div>

          <h1>

            Bring your

            <br />

            community

            <br />

            <em>together.</em>

          </h1>

          <p>

            One connected foundation.

            <br />

            Endless possibilities for your organization.

          </p>

        </div>

        <small>Purpose-built for organizations that make a difference.</small>

      </div>

      <div className="auth-form">

        <div>

          <span className="eyebrow">YOUR NEXT CHAPTER STARTS HERE</span>

          <h1>{!signup&&!forgot&&!reset&&!verify?(brand.data?.loginTitle||title):title}</h1>

          <p>

            {signup

              ? "Create one identity for all your society workspaces."

              : "Your organization. Your people. Your workspace."}

          </p>

          {message ? (

            <div className="success">

              <Check />

              {message}

              <Link to="/login">Continue to sign in →</Link>

            </div>

          ) : (

            <form onSubmit={submit}>
              {twoFactor&&<><Field label="Authenticator or recovery code" name="code" autoComplete="one-time-code" required/><label><input type="checkbox" name="recovery"/>Use a single-use recovery code</label></>}

              {signup && (

                <Field

                  label="Full name"

                  name="fullName"

                  required

                  autoComplete="name"

                />

              )}

              {!twoFactor && !verify && !reset && (

                <Field

                  label="Email address"

                  name="email"

                  type="email"

                  autoComplete="email"

                  required

                />

              )}

              {!twoFactor && !verify && !forgot && (

                <Field

                  label={reset ? "New password" : "Password"}

                  name="password"

                  type="password"

                  minLength={signup || reset ? 12 : undefined}

                  autoComplete={

                    signup || reset ? "new-password" : "current-password"

                  }

                  required

                />

              )}

              {signup && (

                <small>

                  At least 12 characters, with uppercase, lowercase, a number,

                  and a symbol.

                </small>

              )}

              <button disabled={busy} className="primary">

                {busy

                  ? "Please wait…"

                  : verify

                    ? "Verify email"

                    : signup

                      ? "Create account"

                      : forgot

                        ? "Send recovery email"

                        : reset

                          ? "Reset password"

                          : "Sign in"}

                <ArrowRight size={16} />

              </button>

            </form>

          )}

          <div className="auth-links">

            <Link to={signup ? "/login" : "/signup"}>

              {signup

                ? "Already have an account? Sign in"

                : "New here? Create an account"}

            </Link>

            <Link to="/forgot">Forgot password?</Link>

          </div>

        </div>

      </div>

    </div>

  );

}

function Welcome() {

  return (

    <>

      <Heading

        title="Your next chapter starts here"

        text="Register your society to create a connected organizational workspace."

      />

      <div className="welcome-banner">

        <div className="eyebrow">BUILT AROUND YOUR ORGANIZATION</div>

        <h2>

          One society.

          <br />A world of possibilities.

        </h2>

        <p>

          Start with an application. After verification and approval,

          <br />

          configure your hierarchy and invite your leadership.

        </p>

        <Link className="primary" to="/app/register-society">

          Register a society <ArrowRight size={16} />

        </Link>

      </div>

      <div className="three-cols">

        {[

          ["01", "Apply", "Tell us about your society and its purpose."],

          [

            "02",

            "Get approved",

            "A platform administrator reviews your application.",

          ],

          [

            "03",

            "Make it yours",

            "Set up your hierarchy, regions, and chapters.",

          ],

        ].map(([n, t, d]) => (

          <Panel key={n}>

            <span className="step-number">{n}</span>

            <h3>{t}</h3>

            <p>{d}</p>

          </Panel>

        ))}

      </div>

    </>

  );

}

function Dashboard() {

  const { tenant, workspace, scope, can } = useApp();

  const q = useLoad(

    "/societies/" +

      tenant +

      "/dashboard" +

      (scope ? "?workspace=" + scope : ""),

  );

  const units = useLoad(

    "/societies/" +

      tenant +

      "/organizations/units?pageSize=5" +

      (scope ? "&workspace=" + scope : ""),

  );

  return (

    <>

      <Heading

        eyebrow="SOCIETY WORKSPACE"

        title="A clearer view of your community"

        text="Your organization at a glance. Every region. Every chapter."

      >

        {can("settings.manage") && (

          <Link className="secondary" to="/app/setup">

            Continue setup <ArrowRight size={16} />

          </Link>

        )}

      </Heading>

      <WorkspaceSections label="Overview" queryKey="overviewTab" sections={[{id:'summary',label:'Summary',content:<>      <div className="welcome-banner dashboard-banner">

        <div>

          <span className="eyebrow">CONNECTED BY PURPOSE</span>

          <h2>{workspace.name}</h2>

          <p>A shared foundation for your people and the places they serve.</p>

          <Link to="/app/hierarchy">

            Explore your organization <ArrowUpRight size={16} />

          </Link>

        </div>

        <div className="banner-symbol">

          <Network size={100} strokeWidth={1} />

        </div>

      </div>

      <DataState query={q}>

        <div className="stats-grid">

          {[

            ["Organizational units", q.data?.units, Network],

            ["Regions & districts", q.data?.regions, MapPin],

            ["Local chapters", q.data?.chapters, Flag],

            ["Active units", q.data?.active, Check],

          ].map(([label, value, Icon]: any) => (

            <div className="stat" key={label}>

              <span>

                {label}

                <Icon size={19} />

              </span>

              <strong>{value ?? "—"}</strong>

              <small>Within your authorized scope</small>

            </div>

          ))}

        </div>

      </DataState>

</>},{id:'network',label:'Organization & shortcuts',content:<>      <div className="dashboard-grid">

        <Panel title="Your organizational network">

          <DataState query={units}>

            {units.data?.items.length ? (

              <div className="unit-list">

                {units.data.items.map((u: Unit) => (

                  <Link key={u.id} to={"/app/units/" + u.id}>

                    <span className="unit-icon">

                      <Flag size={19} />

                    </span>

                    <div>

                      <strong>{u.name}</strong>

                      <small>

                        {u.code} · {u.level}

                      </small>

                    </div>

                    <Badge>{u.status}</Badge>

                    <ChevronRight size={16} />

                  </Link>

                ))}

              </div>

            ) : (

              <Empty

                title="Let’s build your network"

                text="Create your first region, then add chapters as your society grows."

              >

                <Link className="secondary" to="/app/regions">

                  Open regions <ArrowRight size={15} />

                </Link>

              </Empty>

            )}

          </DataState>

        </Panel>

        <Panel title="Workspace essentials">

          <div className="quick-links">

            {[

              [

                "/app/hierarchy",

                "Your structure",

                "Configure a hierarchy that fits",

                Network,

              ],

              [

                "/app/chapters",

                "Your chapters",

                "Manage local communities",

                Flag,

              ],

              ...(can("roles.manage")

                ? [

                    [

                      "/app/roles",

                      "Your leadership",

                      "Define access and responsibilities",

                      ShieldCheck,

                    ],

                  ]

                : []),

            ].map(([url, title, text, Icon]: any) => (

              <Link to={url} key={url}>

                <Icon size={20} />

                <div>

                  <strong>{title}</strong>

                  <small>{text}</small>

                </div>

                <ArrowUpRight size={17} />

              </Link>

            ))}

          </div>

          <div className="note">

            <ShieldCheck size={18} />

            <p>

              Access follows your role and organizational scope. Every change

              has a history.

            </p>

          </div>

        </Panel>

      </div>

</>}]}/>
    </>

  );

}

function Directory({ kind }: any) {

  const { tenant, scope, can } = useApp();

  const [search, setSearch] = useState("");

  const [term, setTerm] = useState("");

  const [status, setStatus] = useState("");

  const [sort, setSort] = useState("name");

  const [page, setPage] = useState(1);

  const [edit, setEdit] = useState<Unit | null | undefined>();

  useEffect(() => {

    const t = setTimeout(() => {

      setTerm(search);

      setPage(1);

    }, 300);

    return () => clearTimeout(t);

  }, [search]);

  const q = useLoad(

    "/societies/" +

      tenant +

      "/organizations/units?kind=" +

      kind +

      "&search=" +

      encodeURIComponent(term) +

      "&status=" +

      encodeURIComponent(status) +

      "&sort=" +

      sort +

      "&page=" +

      page +

      (scope ? "&workspace=" + scope : ""),

  );

  const plural = kind === "Region" ? "Regions" : "Chapters";

  return (

    <>

      <Heading

        eyebrow={"ORGANIZATION / " + plural.toUpperCase()}

        title={plural}

        text={

          kind === "Region"

            ? "Give every region a place in your bigger picture."

            : "Local communities. Shared purpose. One connected society."

        }

      >

        {can("units.write") && (

          <button className="primary" onClick={() => setEdit(null)}>

            <Plus size={17} />

            Create {kind.toLowerCase()}

          </button>

        )}

      </Heading>

      <Panel>

        <div className="directory-head">

          <h3>

            {plural} directory{" "}

            <span className="count">{q.data?.total ?? 0}</span>

          </h3>

          <span className="muted">Manage your organizational network</span>

        </div>

        <div className="toolbar">

          <div className="search">

            <Search size={17} />

            <input

              aria-label={"Search " + plural}

              placeholder={

                "Search " + plural.toLowerCase() + " by name or code…"

              }

              value={search}

              onChange={(e) => setSearch(e.target.value)}

            />

          </div>

          <select

            aria-label="Filter by status"

            value={status}

            onChange={(e) => {

              setStatus(e.target.value);

              setPage(1);

            }}

          >

            <option value="">All statuses</option>

            {[

              "Proposed",

              "Pending Approval",

              "Active",

              "Suspended",

              "Inactive",

              "Closed",

              "Merged",

              "Archived",

            ].map((s) => (

              <option key={s}>{s}</option>

            ))}

          </select>

          <select

            aria-label="Sort directory"

            value={sort}

            onChange={(e) => {

              setSort(e.target.value);

              setPage(1);

            }}

          >

            <option value="name">Sort by name</option>

            <option value="code">Sort by code</option>

            <option value="status">Sort by status</option>

          </select>

        </div>

        <DataState query={q}>

          {q.data?.items.length ? (

            <div className="table-wrap">

              <table>

                <thead>

                  <tr>

                    <th>{kind} name</th>

                    <th>Location</th>

                    <th>Status</th>

                    <th>{kind === "Region" ? "Chapters" : "Child units"}</th>

                    {kind === "Region" && <th>Regional head</th>}

                    <th></th>

                  </tr>

                </thead>

                <tbody>

                  {q.data.items.map((u: Unit) => {

                    const d = details(u.details);

                    return (

                      <tr key={u.id}>

                        <td>

                          <Link className="unit-name" to={"/app/units/" + u.id}>

                            <span className="unit-icon">

                              <Flag size={18} />

                            </span>

                            <span>

                              <strong>{u.name}</strong>

                              <small>{u.code}</small>

                            </span>

                          </Link>

                        </td>

                        <td>

                          {[d.city, d.state, d.country]

                            .filter(Boolean)

                            .join(", ") || "—"}

                        </td>

                        <td>

                          <Badge>{u.status}</Badge>

                        </td>

                        <td>{kind === "Region" ? u.chapters : u.children}</td>

                        {kind === "Region" && <td>{u.head || "Unassigned"}</td>}

                        <td>

                          {can("units.write") && (

                            <button

                              className="text-btn"

                              onClick={() => setEdit(u)}

                            >

                              Edit

                            </button>

                          )}

                          <Link

                            aria-label={"View " + u.name}

                            to={"/app/units/" + u.id}

                          >

                            <ArrowUpRight size={17} />

                          </Link>

                        </td>

                      </tr>

                    );

                  })}

                </tbody>

              </table>

            </div>

          ) : (

            <Empty

              title={"Your first " + kind.toLowerCase() + " belongs here"}

              text={

                "Add " +

                plural.toLowerCase() +

                " to bring your organizational structure to life."

              }

            />

          )}

        </DataState>

        <div className="pagination">

          <span>

            {q.data?.total ?? 0} {plural.toLowerCase()} · Page {page}

          </span>

          <div>

            <button disabled={page === 1} onClick={() => setPage(page - 1)}>

              Previous

            </button>

            <button

              disabled={page * 20 >= (q.data?.total ?? 0)}

              onClick={() => setPage(page + 1)}

            >

              Next

            </button>

          </div>

        </div>

      </Panel>

      {edit !== undefined && (

        <UnitDrawer

          kind={kind}

          unit={edit}

          onClose={() => setEdit(undefined)}

        />

      )}

    </>

  );

}

function UnitDrawer({ kind, unit, onClose }: any) {

  const { tenant } = useApp();

  const action = useAction();

  const levels = useLoad<Level[]>(

    "/societies/" + tenant + "/organizations/levels",

  );

  const [levelId, setLevelId] = useState(unit?.levelId || "");

  const [parentSearch, setParentSearch] = useState("");

  const parents = useLoad(

    "/societies/" +

      tenant +

      "/organizations/units?pageSize=100&search=" +

      encodeURIComponent(parentSearch),

  );

  const d = details(unit?.details);

  const level = levels.data?.find(

    (l) => l.id === (levelId || levels.data?.find((l) => l.kind === kind)?.id),

  );

  async function submit(e: FormEvent<HTMLFormElement>) {

    e.preventDefault();

    const f = Object.fromEntries(new FormData(e.currentTarget));

    const body = {

      levelId: f.levelId,

      parentId: f.parentId || null,

      name: f.name,

      code: f.code,

      status: f.status,

      revision: unit?.revision || 0,

      details: JSON.stringify({

        ...d,

        ...Object.fromEntries(

          Object.entries(f).filter(

            ([k]) =>

              !["levelId", "parentId", "name", "code", "status"].includes(k),

          ),

        ),

      }),

    };

    if (

      await action.run(() =>

        api(

          "/societies/" +

            tenant +

            "/organizations/units" +

            (unit ? "/" + unit.id : ""),

          unit ? "PUT" : "POST",

          body,

        ),

      )

    )

      onClose();

  }

  return (

    <Drawer

      title={(unit ? "Edit " : "Create ") + kind.toLowerCase()}

      onClose={onClose}

    >

      <form onSubmit={submit}>

        <div className="form-section">ORGANIZATION DETAILS</div>

        <Field

          label="Name"

          name="name"

          required

          maxLength={160}

          defaultValue={unit?.name}

          autoFocus

        />

        <div className="form-grid">

          <Field

            label="Unique code"

            name="code"

            pattern="[A-Za-z0-9_-]{2,40}"

            required

            defaultValue={unit?.code}

          />

          <Field label="Status">

            <select name="status" defaultValue={unit?.status || "Proposed"}>

              {[

                "Proposed",

                "Pending Approval",

                "Active",

                "Suspended",

                "Inactive",

                "Closed",

                "Merged",

                "Archived",

              ].map((s) => (

                <option key={s}>{s}</option>

              ))}

            </select>

          </Field>

        </div>

        <Field label="Organizational level">

          <select

            required

            name="levelId"

            value={level?.id || ""}

            onChange={(e) => setLevelId(e.target.value)}

          >

            <option value="">Select level</option>

            {levels.data

              ?.filter((l) => l.kind === kind || kind === "Unit")

              .map((l) => (

                <option value={l.id} key={l.id}>

                  {l.name}

                </option>

              ))}

          </select>

        </Field>

        <Field

          label="Find parent by name or code"

          value={parentSearch}

          onChange={(e: any) => setParentSearch(e.target.value)}

        />

        <Field label="Parent organization">

          <select

            name="parentId"

            required={!level?.allowRoot}

            defaultValue={unit?.parentId || ""}

          >

            <option value="">Society root</option>

            {parents.data?.items

              .filter(

                (u: Unit) =>

                  u.id !== unit?.id &&

                  level?.allowedParents.includes(u.levelId),

              )

              .map((u: Unit) => (

                <option key={u.id} value={u.id}>

                  {u.name} ({u.code})

                </option>

              ))}

          </select>

        </Field>

        <div className="form-section">LOCATION & CONTACT</div>

        <div className="form-grid">

          {[

            ["country", "Country"],

            ["state", "State / Province"],

            ["city", "City"],

            ["shortName", "Short name"],

            ["contactEmail", "Contact email"],

            ["contactPhone", "Contact phone"],

            ["website", "Website"],

            ["timeZone", "Time zone"],

            ["formationDate", "Formation date"],



            ["coverUrl", "Cover image URL"],

          ].map(([name, label]) => (

            <Field

              key={name}

              label={label}

              name={name}

              type={

                name === "contactEmail"

                  ? "email"

                  : ["website", "logoUrl", "coverUrl"].includes(name)

                    ? "url"

                    : name === "formationDate"

                      ? "date"

                      : "text"

              }

              defaultValue={d[name] || ""}

            />

          ))}

        </div>

        <ImageUpload defaultValue={d.logoUrl} unitId={unit?.id} />

        <Field label="Address" name="address" defaultValue={d.address} />

        <Field label="Description">

          <textarea name="description" defaultValue={d.description} />

        </Field>

        {(kind === "Chapter" || level?.kind === "Chapter") && (

          <div className="form-grid">

            <Field

              label="Default meeting venue"

              name="meetingVenue"

              defaultValue={d.meetingVenue}

            />

            <Field

              label="Meeting schedule"

              name="meetingSchedule"

              defaultValue={d.meetingSchedule}

            />

          </div>

        )}

        <div className="drawer-actions">

          <button type="button" className="secondary" onClick={onClose}>

            Cancel

          </button>

          <button className="primary" disabled={action.busy}>

            {action.busy ? "Saving…" : "Save " + kind.toLowerCase()}

          </button>

        </div>

      </form>

    </Drawer>

  );

}

function Hierarchy() {

  const { tenant, can } = useApp();

  const levels = useLoad<Level[]>(

    "/societies/" + tenant + "/organizations/levels",

  );

  const [drawer, setDrawer] = useState(false);

  const [editingLevel, setEditingLevel] = useState<Level | null>(null);

  const [create, setCreate] = useState(false);

  const action = useAction();

  return (

    <>

      <Heading

        title="Your organization, connected"

        text="One flexible hierarchy. Built around the way your society works."

      >

        {can("hierarchy.manage") && (

          <button

            className="secondary"

            onClick={() => {

              setEditingLevel(null);

              setDrawer(true);

            }}

          >

            <Plus size={17} />

            Add level

          </button>

        )}

        {can("units.write") && (

          <button className="primary" onClick={() => setCreate(true)}>

            <Plus size={17} />

            Create unit

          </button>

        )}

      </Heading>

      <div className="hierarchy-layout">

        <Panel title="Organizational structure">

          <TreeBranch />

        </Panel>

        <Panel title="Configured levels">

          <DataState query={levels}>

            <div className="levels-list">

              {levels.data?.map((l) => (

                <div key={l.id}>

                  <span className="unit-icon">

                    <Layers size={17} />

                  </span>

                  <div>

                    <strong>{l.name}</strong>

                    {can("hierarchy.manage") && (

                      <button

                        className="text-btn"

                        onClick={() => {

                          setEditingLevel(l);

                          setDrawer(true);

                        }}

                      >

                        Edit

                      </button>

                    )}

                    <small>

                      {l.allowRoot

                        ? "Can start at society root"

                        : "Parent required"}{" "}

                      · {l.kind}

                    </small>

                    <small>

                      {l.allowedParents

                        .map(

                          (id) => levels.data?.find((p) => p.id === id)?.name,

                        )

                        .join(", ")}

                    </small>

                  </div>

                </div>

              ))}

            </div>

          </DataState>

          <div className="note">

            Parent relationships follow these configured rules. Units load one

            branch at a time.

          </div>

        </Panel>

      </div>

      {create && <UnitDrawer kind="Unit" onClose={() => setCreate(false)} />}{" "}

      {drawer && (

        <Drawer

          title={

            editingLevel

              ? "Edit organizational level"

              : "Add organizational level"

          }

          onClose={() => {

            setDrawer(false);

            setEditingLevel(null);

          }}

        >

          <form

            onSubmit={async (e) => {

              e.preventDefault();

              const f = new FormData(e.currentTarget);

              if (

                await action.run(() =>

                  api(

                    "/societies/" +

                      tenant +

                      "/organizations/levels" +

                      (editingLevel ? "/" + editingLevel.id : ""),

                    editingLevel ? "PUT" : "POST",

                    {

                      name: f.get("name"),

                      kind: f.get("kind"),

                      allowRoot: f.get("root") === "on",

                      allowedParents: f.getAll("parents"),

                    },

                  ),

                )

              )

                setDrawer(false);

            }}

          >

            <Field

              label="Level name"

              name="name"

              required

              maxLength={80}

              defaultValue={editingLevel?.name}

            />

            <Field label="Directory classification">

              <select name="kind" defaultValue={editingLevel?.kind}>

                <option>Other</option>

                <option>Region</option>

                <option>Chapter</option>

              </select>

            </Field>

            <label className="checkbox">

              <input

                type="checkbox"

                name="root"

                defaultChecked={editingLevel?.allowRoot}

              />

              May be created at society root

            </label>

            <h3>Permitted parent levels</h3>

            {levels.data?.map((l) => (

              <label className="checkbox" key={l.id}>

                <input

                  type="checkbox"

                  name="parents"

                  value={l.id}

                  defaultChecked={editingLevel?.allowedParents.includes(l.id)}

                />

                {l.name}

              </label>

            ))}

            <button className="primary" disabled={action.busy}>

              Save level

            </button>

          </form>

        </Drawer>

      )}

    </>

  );

}

function TreeBranch({ parentId, depth = 0 }: any) {

  const { tenant, scope } = useApp();

  const [page, setPage] = useState(1);

  const [open, setOpen] = useState<string[]>([]);

  const q = useLoad(

    "/societies/" +

      tenant +

      "/organizations/units?" +

      (parentId ? "parentId=" + parentId : "kind=roots") +

      (scope ? "&workspace=" + scope : "") +

      "&page=" +

      page,

  );

  return (

    <DataState query={q}>

      {!q.data?.items.length ? (

        <p className="muted tree-empty">No organizational units yet.</p>

      ) : (

        q.data.items.map((u: Unit) => (

          <div className="tree-branch" key={u.id}>

            <div className="tree-row">

              <button

                aria-label={"Expand " + u.name}

                disabled={!u.children}

                className={"icon-btn " + (open.includes(u.id) ? "rotated" : "")}

                onClick={() =>

                  setOpen(

                    open.includes(u.id)

                      ? open.filter((x) => x !== u.id)

                      : [...open, u.id],

                  )

                }

              >

                <ChevronRight size={17} />

              </button>

              <span className="unit-icon">

                <Network size={17} />

              </span>

              <Link to={"/app/units/" + u.id}>

                <strong>{u.name}</strong>

                <small>

                  {u.level} · {u.code}

                </small>

              </Link>

              <Badge>{u.status}</Badge>

              <span className="count">{u.children}</span>

            </div>

            {open.includes(u.id) && depth < 64 && (

              <div className="tree-children">

                <TreeBranch parentId={u.id} depth={depth + 1} />

              </div>

            )}

          </div>

        ))

      )}

      {q.data?.total > 20 && (

        <div className="pagination">

          <button disabled={page === 1} onClick={() => setPage(page - 1)}>

            Previous

          </button>

          <span>Page {page}</span>

          <button

            disabled={page * 20 >= q.data.total}

            onClick={() => setPage(page + 1)}

          >

            Next

          </button>

        </div>

      )}

    </DataState>

  );

}

function UnitOverview() {

  const { id } = useParams();

  const { tenant, can } = useApp();

  const q = useLoad<Unit>(

    "/societies/" + tenant + "/organizations/units/" + id,

  );

  const [edit, setEdit] = useState(false);

  const d = details(q.data?.details);

  return (

    <DataState query={q}>

      <Heading

        eyebrow="ORGANIZATION / UNIT OVERVIEW"

        title={q.data?.name}

        text={q.data?.code}

      >

        {q.data && <Badge>{q.data.status}</Badge>}

        {can("units.write") && (

          <button className="primary" onClick={() => setEdit(true)}>

            Edit organization

          </button>

        )}

      </Heading>

      <div className="tabs">

        <span className="selected">Overview</span>

        <Link to="/app/members">Members</Link>

        <Link to="/app/members/leadership">Leadership</Link>

        <Link to="/app/community">Activities &amp; events</Link>

        <Link to="/app/communications/inbox">Communication</Link>

        <Link to="/app/finance/member-dues">Dues</Link>

        <Link to="/app/studio/cards">ID cards</Link>

      </div>

      <div className="dashboard-grid">

        <Panel title="Organization details">

          {d.logoUrl && (

            <img

              className="organization-logo"

              src={d.logoUrl}

              alt={q.data?.name + " logo"}

            />

          )}

          <dl>

            {[

              [

                "Location",

                [d.city, d.state, d.country].filter(Boolean).join(", "),

              ],

              ["Address", d.address],

              ["Contact email", d.contactEmail],

              ["Contact phone", d.contactPhone],

              ["Formation date", d.formationDate],

              ["Time zone", d.timeZone],

              ["Meeting venue", d.meetingVenue],

              ["Meeting schedule", d.meetingSchedule],

              ["Description", d.description],

            ].map(([k, v]) => (

              <div key={k}>

                <dt>{k}</dt>

                <dd>{v || "Not provided"}</dd>

              </div>

            ))}

          </dl>

        </Panel>

        <Panel title="Subordinate units">

          <TreeBranch parentId={id} />

        </Panel>

      </div>

      {edit && (

        <UnitDrawer unit={q.data} kind="Unit" onClose={() => setEdit(false)} />

      )}

    </DataState>

  );

}

function SocietyForm() {

  const nav = useNavigate();

  const action = useAction();

  return (

    <>

      <Heading

        title="Give your society a home"

        text="Submit an application. Your society’s workspace opens after review and approval."

      />

      <Panel>

        <form

          className="wide-form"

          onSubmit={async (e) => {

            e.preventDefault();

            const f = Object.fromEntries(new FormData(e.currentTarget));

            if (

              await action.run(

                () => api("/societies/applications", "POST", f),

                "Application submitted for review",

              )

            )

              nav("/app/applications");

          }}

        >

          <div className="form-section">SOCIETY PROFILE</div>

          <div className="form-grid">

            {[

              ["name", "Society name"],

              ["category", "Organization category"],

              ["country", "Country"],

              ["state", "State"],

              ["city", "City"],

              ["officialEmail", "Official email"],

              ["contactNumber", "Contact number"],

              ["website", "Website (optional)"],

              ["reference", "Registration reference (optional)"],

              ["applicantMobile", "Your mobile number"],

            ].map(([name, label]) => (

              <Field

                key={name}

                name={name}

                label={label}

                required={!["website", "reference"].includes(name)}

                type={

                  name === "officialEmail"

                    ? "email"

                    : name === "website"

                      ? "url"

                      : "text"

                }

              />

            ))}

          </div>

          <Field label="Registered address" name="address" required />

          <div className="note">

            <ShieldCheck size={19} />

            Your verified account identifies the applicant. Administrator access

            is assigned only after approval.

          </div>

          <button className="primary" disabled={action.busy}>

            Submit application <ArrowRight size={16} />

          </button>

        </form>

      </Panel>

    </>

  );

}

function Applications({ platform }: any) {

  const [page, setPage] = useState(1);

  const q = useLoad<any[]>("/societies/applications?page=" + page);

  const [review, setReview] = useState<any>();

  const action = useAction();

  return (

    <>

      <Heading

        title={

          platform ? "Society approval center" : "Your society applications"

        }

        text={

          platform

            ? "Review applications and establish trusted society workspaces."

            : "Track your registration from submission to approval."

        }

      >

        <Link className="primary" to="/app/register-society">

          <Plus size={16} />

          Register society

        </Link>

      </Heading>

      <Panel>

        <DataState query={q}>

          {q.data?.length ? (

            <div className="table-wrap">

              <table>

                <thead>

                  <tr>

                    <th>Society</th>

                    <th>Submitted</th>

                    <th>Status</th>

                    <th>Review note</th>

                    <th></th>

                  </tr>

                </thead>

                <tbody>

                  {q.data.map((a) => (

                    <tr key={a.id}>

                      <td>

                        <strong>{a.name}</strong>

                        <small>{a.id}</small>

                      </td>

                      <td>{new Date(a.createdAt).toLocaleDateString()}</td>

                      <td>

                        <Badge>{a.status}</Badge>

                      </td>

                      <td>{a.reviewNote || "—"}</td>

                      <td>

                        {platform && a.status === "Pending" && (

                          <button

                            className="secondary"

                            onClick={() => setReview(a)}

                          >

                            Review

                          </button>

                        )}

                      </td>

                    </tr>

                  ))}

                </tbody>

              </table>

            </div>

          ) : (

            <Empty

              title="No applications yet"

              text="Applications will appear here as they are submitted."

            />

          )}

        </DataState>

        <div className="pagination">

          <button disabled={page === 1} onClick={() => setPage(page - 1)}>

            Previous

          </button>

          <span>Page {page}</span>

          <button

            disabled={(q.data?.length || 0) < 100}

            onClick={() => setPage(page + 1)}

          >

            Next

          </button>

        </div>

      </Panel>

      {review && (

        <Drawer title={"Review " + review.name} onClose={() => setReview(null)}>

          <dl>

            {Object.entries(details(review.details)).map(([k, v]) => (

              <div key={k}>

                <dt>{k}</dt>

                <dd>{String(v || "—")}</dd>

              </div>

            ))}

          </dl>

          <form

            onSubmit={async (e) => {

              e.preventDefault();

              const f = Object.fromEntries(new FormData(e.currentTarget));

              if (

                await action.run(() =>

                  api(

                    "/societies/applications/" + review.id + "/review",

                    "POST",

                    { approve: f.decision === "approve", note: f.note },

                  ),

                )

              )

                setReview(null);

            }}

          >

            <Field label="Decision">

              <select name="decision">

                <option value="approve">Approve and create workspace</option>

                <option value="reject">Reject application</option>

              </select>

            </Field>

            <Field label="Review note">

              <textarea name="note" required />

            </Field>

            <button className="primary" disabled={action.busy}>

              Confirm decision

            </button>

          </form>

        </Drawer>

      )}

    </>

  );

}

function Roles() {

  const { tenant } = useApp();

  const [editingRole, setEditingRole] = useState<any>(null);

  const [candidateSearch, setCandidateSearch] = useState("");

  const [unitSearch, setUnitSearch] = useState("");

  const candidates = useLoad<any[]>(

    "/societies/" +

      tenant +

      "/role-candidates?search=" +

      encodeURIComponent(candidateSearch),

  );

  const scopeUnits = useLoad(

    "/societies/" +

      tenant +

      "/organizations/units?pageSize=100&search=" +

      encodeURIComponent(unitSearch),

  );

  const q = useLoad<any[]>("/societies/" + tenant + "/roles");

  const perms = useLoad<string[]>("/societies/" + tenant + "/permissions");

  const [assignmentPage, setAssignmentPage] = useState(1);

  const assignments = useLoad<any[]>(

    "/societies/" + tenant + "/role-assignments?page=" + assignmentPage,

  );

  const [mode, setMode] = useState("");

  const [tab, setTab] = useState("roles");

  const action = useAction();

  return (

    <>

      <Heading

        title="Leadership with clear boundaries"

        text="Define responsibilities, grant scoped access, and set effective dates."

      >

        <button className="secondary" onClick={() => setMode("invite")}>

          Invite officer

        </button>

        <button className="secondary" onClick={() => setMode("assignment")}>

          <Users size={17} />

          Assign role

        </button>

        <button

          className="primary"

          onClick={() => {

            setEditingRole(null);

            setMode("role");

          }}

        >

          <Plus size={17} />

          Create role

        </button>

      </Heading>

      <div className="tabs">

        {["roles", "assignments", "permission matrix"].map((t) => (

          <button

            className={tab === t ? "selected" : ""}

            key={t}

            onClick={() => setTab(t)}

          >

            {t}

          </button>

        ))}

      </div>

      <Panel>

        <DataState query={q}>

          {tab === "roles" ? (

            <div className="role-grid">

              {q.data?.map((r) => (

                <div className="role-card" key={r.id}>

                  <ShieldCheck />

                  <h3>{r.name}</h3>

                  {!r.managed && (

                    <button

                      className="text-btn"

                      onClick={() => {

                        setEditingRole(r);

                        setMode("role");

                      }}

                    >

                      Edit role

                    </button>

                  )}

                  <p>

                    {r.responsibilities ||

                      "Organization-scoped application role"}

                  </p>

                  <span className="count">

                    {r.permissions.length} permissions

                  </span>

                </div>

              ))}

            </div>

          ) : tab === "permission matrix" ? (

            <div className="table-wrap">

              <table>

                <thead>

                  <tr>

                    <th>Permission</th>

                    {q.data?.map((r) => (

                      <th key={r.id}>{r.name}</th>

                    ))}

                  </tr>

                </thead>

                <tbody>

                  {perms.data?.map((p) => (

                    <tr key={p}>

                      <td>{p}</td>

                      {q.data?.map((r) => (

                        <td key={r.id}>

                          {r.permissions.includes(p) ? (

                            <Check size={16} />

                          ) : (

                            <span className="muted">—</span>

                          )}

                        </td>

                      ))}

                    </tr>

                  ))}

                </tbody>

              </table>

            </div>

          ) : (

            <DataState query={assignments}>

              <div className="table-wrap">

                <table>

                  <thead>

                    <tr>

                      <th>Person</th>

                      <th>Role / scope</th>

                      <th>Effective dates</th>

                      <th></th>

                    </tr>

                  </thead>

                  <tbody>

                    {assignments.data?.map((a) => (

                      <tr key={a.id}>

                        <td>

                          {a.user}

                          <small>{a.userId}</small>

                        </td>

                        <td>

                          {a.role}

                          <small>{a.unitId || "Society-wide"}</small>

                        </td>

                        <td>

                          {new Date(a.startsAt).toLocaleDateString()} →{" "}

                          {a.endsAt

                            ? new Date(a.endsAt).toLocaleDateString()

                            : "Ongoing"}

                        </td>

                        <td>

                          <button

                            className="text-btn"

                            onClick={() => {

                              if (window.confirm("Revoke this assignment now?"))

                                action.run(() =>

                                  api(

                                    "/societies/" +

                                      tenant +

                                      "/role-assignments/" +

                                      a.id,

                                    "DELETE",

                                  ),

                                );

                            }}

                          >

                            Revoke

                          </button>

                        </td>

                      </tr>

                    ))}

                  </tbody>

                </table>

              </div>

            </DataState>

          )}

        </DataState>

        {tab === "assignments" && (

          <div className="pagination">

            <button

              disabled={assignmentPage === 1}

              onClick={() => setAssignmentPage(assignmentPage - 1)}

            >

              Previous

            </button>

            <span>Page {assignmentPage}</span>

            <button

              disabled={(assignments.data?.length || 0) < 100}

              onClick={() => setAssignmentPage(assignmentPage + 1)}

            >

              Next

            </button>

          </div>

        )}

      </Panel>

      {mode && (

        <Drawer

          title={

            mode === "role"

              ? "Create role"

              : mode === "invite"

                ? "Invite an officer"

                : "Assign a role"

          }

          onClose={() => setMode("")}

        >

          <form

            onSubmit={async (e) => {

              e.preventDefault();

              const f = new FormData(e.currentTarget);

              const body =

                mode === "invite"

                  ? { email: f.get("email") }

                  : mode === "role"

                    ? {

                        name: f.get("name"),

                        responsibilities: f.get("responsibilities"),

                        permissions: f.getAll("permissions"),

                      }

                    : {

                        userId: f.get("userId"),

                        roleId: f.get("roleId"),

                        unitId: f.get("unitId") || null,

                        startsAt: new Date(

                          String(f.get("startsAt")),

                        ).toISOString(),

                        endsAt: f.get("endsAt")

                          ? new Date(String(f.get("endsAt"))).toISOString()

                          : null,

                      };

              if (

                await action.run(() =>

                  api(

                    "/societies/" +

                      tenant +

                      (mode === "invite"

                        ? "/invitations"

                        : mode === "role"

                          ? "/roles" + (editingRole ? "/" + editingRole.id : "")

                          : "/role-assignments"),

                    mode === "role" && editingRole ? "PUT" : "POST",

                    body,

                  ),

                )

              )

                setMode("");

            }}

          >

            {mode === "invite" ? (

              <>

                <Field

                  label="Officer email"

                  name="email"

                  type="email"

                  required

                />

                <p>

                  The recipient must verify their account and accept the

                  invitation. Assign a scoped role after they join.

                </p>

              </>

            ) : mode === "role" ? (

              <>

                <Field

                  label="Role name"

                  name="name"

                  required

                  defaultValue={editingRole?.name}

                />

                <Field label="Responsibilities">

                  <textarea

                    name="responsibilities"

                    defaultValue={editingRole?.responsibilities}

                  />

                </Field>

                <h3>Permissions</h3>

                {perms.data?.map((p) => (

                  <label className="checkbox" key={p}>

                    <input

                      type="checkbox"

                      name="permissions"

                      value={p}

                      defaultChecked={editingRole?.permissions.includes(p)}

                    />

                    {p}

                  </label>

                ))}

              </>

            ) : (

              <>

                <Field

                  label="Find a person"

                  value={candidateSearch}

                  onChange={(e: any) => setCandidateSearch(e.target.value)}

                />

                <Field label="Person">

                  <select name="userId" required>

                    <option value="">Select an authorized person</option>

                    {candidates.data?.map((u) => (

                      <option key={u.id} value={u.id}>

                        {u.fullName}

                      </option>

                    ))}

                  </select>

                </Field>

                <Field label="Role">

                  <select name="roleId" required>

                    {q.data?.map((r) => (

                      <option key={r.id} value={r.id}>

                        {r.name}

                      </option>

                    ))}

                  </select>

                </Field>

                <Field

                  label="Find an organization"

                  value={unitSearch}

                  onChange={(e: any) => setUnitSearch(e.target.value)}

                />

                <Field label="Organization scope">

                  <select name="unitId">

                    <option value="">Entire society</option>

                    {scopeUnits.data?.items.map((u: Unit) => (

                      <option key={u.id} value={u.id}>

                        {u.name} · {u.code}

                      </option>

                    ))}

                  </select>

                </Field>

                <Field

                  label="Effective from"

                  name="startsAt"

                  type="datetime-local"

                  required

                />

                <Field

                  label="Effective until (optional)"

                  name="endsAt"

                  type="datetime-local"

                />

                <div className="note">

                  The person must already have an authorized society link.

                  Granting roles never creates membership.

                </div>

              </>

            )}

            <button className="primary" disabled={action.busy}>

              Save {mode}

            </button>

          </form>

        </Drawer>

      )}

    </>

  );

}

const wizardSteps = [

  "Society profile",

  "Logo & branding",

  "Organizational hierarchy",

  "Regions & districts",

  "Chapters",

  "Initial officers",

  "Membership settings",

  "Review & activate",

];

function Setup() {

  const { tenant } = useApp();

  const q = useLoad("/societies/" + tenant + "/onboarding");

  const [step, setStep] = useState<number | null>(null);

  const action = useAction();

  const n = step ?? q.data?.step ?? 0;

  const data = details(q.data?.data);

  return (

    <>

      <Heading

        title="Make this workspace yours"

        text="A few thoughtful steps to lay the foundation. Save now and continue whenever you’re ready."

      />

      <div className="wizard-layout">

        <Panel>

          <ol className="wizard-steps">

            {wizardSteps.map((s, i) => (

              <li key={s}>

                <button

                  className={n === i ? "current" : ""}

                  onClick={() => setStep(i)}

                >

                  <span>{i + 1}</span>

                  {s}

                </button>

              </li>

            ))}

          </ol>

        </Panel>

        <Panel>

          <div className="wizard-content">

            <span className="eyebrow">STEP {n + 1} OF 8</span>

            <h2>{wizardSteps[n]}</h2>

            <DataState query={q}>

              <form

                key={n + q.data?.data}

                onSubmit={async (e) => {

                  e.preventDefault();

                  const f = Object.fromEntries(new FormData(e.currentTarget));

                  const next = { ...data, ...f };

                  if (

                    await action.run(

                      () =>

                        api("/societies/" + tenant + "/onboarding", "PUT", {

                          step: Math.min(n + 1, 7),

                          data: JSON.stringify(next),

                          activate: n === 7,

                        }),

                      n === 7 ? "Society activated" : "Progress saved",

                    )

                  )

                    setStep(Math.min(n + 1, 7));

                }}

              >

                {n === 0 ? (

                  <>

                    <Field

                      label="Society display name"

                      name="displayName"

                      defaultValue={data.displayName}

                    />

                    <Field label="Purpose">

                      <textarea name="purpose" defaultValue={data.purpose} />

                    </Field>

                  </>

                ) : n === 1 ? (

                  <>

                    <Field

                      label="Brand color"

                      name="brandColor"

                      type="color"

                      defaultValue={data.brandColor || "#147d73"}

                    />

                    <ImageUpload defaultValue={data.logoUrl} />

                  </>

                ) : n === 2 ? (

                  <>

                    <p>

                      Configure your level names and permitted parent

                      relationships in the hierarchy workspace.

                    </p>

                    <Link className="secondary" to="/app/hierarchy">

                      Configure hierarchy <ArrowUpRight size={16} />

                    </Link>

                  </>

                ) : n === 3 ? (

                  <>

                    <p>

                      Create your first region or district. You can add more

                      later.

                    </p>

                    <Link className="secondary" to="/app/regions">

                      Open regions <ArrowUpRight size={16} />

                    </Link>

                  </>

                ) : n === 4 ? (

                  <>

                    <p>

                      Add chapters at your own pace. Creating all chapters is

                      optional during onboarding.

                    </p>

                    <Link className="secondary" to="/app/chapters">

                      Open chapters <ArrowUpRight size={16} />

                    </Link>

                  </>

                ) : n === 5 ? (

                  <>

                    <p>

                      Define roles and assign your initial officers with

                      explicit scopes and effective dates.

                    </p>

                    <Link className="secondary" to="/app/roles">

                      Manage officers <ArrowUpRight size={16} />

                    </Link>

                  </>

                ) : n === 6 ? (

                  <>

                    <Field label="Member directory visibility">

                      <select

                        name="directoryVisibility"

                        defaultValue={data.directoryVisibility || "chapter"}

                      >

                        <option value="chapter">Chapter only (default)</option>

                        <option value="region">

                          Region (requires permission)

                        </option>

                        <option value="society">

                          Society (requires permission)

                        </option>

                      </select>

                    </Field>

                    <p>

                      Membership records are managed in the membership

                      workspace. Sensitive data remains restricted by policy.

                    </p>

                  </>

                ) : (

                  <>

                    <div className="success">

                      <ShieldCheck />

                      Your society is approved. Review the profile, hierarchy,

                      and roles before activation.

                    </div>

                    <dl>

                      {Object.entries(data).map(([k, v]) => (

                        <div key={k}>

                          <dt>{k}</dt>

                          <dd>{String(v)}</dd>

                        </div>

                      ))}

                    </dl>

                    {q.data?.complete && <Badge>Active</Badge>}

                  </>

                )}

                <div className="wizard-actions">

                  <button

                    className="secondary"

                    type="button"

                    disabled={n === 0}

                    onClick={() => setStep(n - 1)}

                  >

                    Back

                  </button>

                  <button className="primary" disabled={action.busy}>

                    {n === 7 ? "Activate society" : "Save & continue"}

                    <ArrowRight size={16} />

                  </button>

                </div>

              </form>

            </DataState>

          </div>

        </Panel>

      </div>

    </>

  );

}

function SocietySettings() {

  const { tenant } = useApp();

  const q = useLoad("/societies/" + tenant + "/settings");

  const action = useAction();

  const d = details(q.data?.profile);

  return (

    <>

      <Heading

        title="Society settings"

        text="Keep your organization’s profile and directory policy up to date."

      />

      <Panel>

        <DataState query={q}>

          <form

            key={q.data?.profile}

            className="wide-form"

            onSubmit={async (e) => {

              e.preventDefault();

              await action.run(() =>

                api("/societies/" + tenant + "/settings", "PUT", {

                  ...d,

                  ...Object.fromEntries(new FormData(e.currentTarget)),

                }),

              );

            }}

          >

            <FormTabs label="General settings" sections={[{id:'location',label:'Location',content:<div className="form-grid"><Field key='country' label='Country' name='country' defaultValue={d['country']||d['Country']||''}/><Field key='state' label='State' name='state' defaultValue={d['state']||d['State']||''}/><Field key='city' label='City' name='city' defaultValue={d['city']||d['City']||''}/><Field key='address' label='Address' name='address' defaultValue={d['address']||d['Address']||''}/></div>},{id:'contact',label:'Contact details',content:<div className="form-grid"><Field key='officialEmail' label='Official email' name='officialEmail' defaultValue={d['officialEmail']||d['OfficialEmail']||''}/><Field key='contactNumber' label='Contact number' name='contactNumber' defaultValue={d['contactNumber']||d['ContactNumber']||''}/><Field key='website' label='Website' name='website' defaultValue={d['website']||d['Website']||''}/><Field key='logoUrl' label='Logo URL' name='logoUrl' defaultValue={d['logoUrl']||d['LogoUrl']||''}/></div>},{id:'policy',label:'Directory & time zone',content:<><Field key='timeZone' label='Time zone' name='timeZone' defaultValue={d['timeZone']||d['TimeZone']||''}/>            <Field label="Directory visibility">

              <select

                name="directoryVisibility"

                defaultValue={d.directoryVisibility || "chapter"}

              >

                <option value="chapter">Chapter only</option>

                <option value="region">Region with permission</option>

                <option value="society">Society with permission</option>

              </select>

            </Field>

</>}]}/>
            <button className="primary" disabled={action.busy}>

              Save settings

            </button>

          </form>

        </DataState>

      </Panel>

    </>

  );

}

function Profile({ me,mode='profile' }: any) {

  const action = useAction();

  return (

    <>

      <Heading

        title="Your account"

        text="One verified identity. All your authorized workspaces."

      />

      <div className="dashboard-grid">

        {mode==='profile'&&<Panel title="Personal profile">

          <form

            onSubmit={async (e) => {

              e.preventDefault();

              await action.run(() =>

                api(

                  "/users/me",

                  "PUT",

                  Object.fromEntries(new FormData(e.currentTarget)),

                ),

              );

            }}

          >

            <Field

              label="Full name"

              name="fullName"

              defaultValue={me.fullName}

              required

            />

            <Field label="Email" value={me.email} readOnly />

            <Badge>Verified</Badge>

            <p className="muted">Account ID: {me.id}</p>

            <button className="primary" disabled={action.busy}>

              Save profile

            </button>

          </form>

        </Panel>}

        {mode==='password'&&<Panel title="Change password">

          <form

            onSubmit={async (e) => {

              e.preventDefault();

              const form = e.currentTarget;

              if (

                await action.run(

                  () =>

                    api(

                      "/users/me/password",

                      "POST",

                      Object.fromEntries(new FormData(form)),

                    ),

                  "Password changed",

                )

              )

                form.reset();

            }}

          >

            <Field

              label="Current password"

              name="currentPassword"

              type="password"

              required

              autoComplete="current-password"

            />

            <Field

              label="New password"

              name="newPassword"

              type="password"

              minLength={12}

              required

              autoComplete="new-password"

            />

            <button className="secondary" disabled={action.busy}>

              Update password

            </button>

          </form>

        </Panel>}

      </div>

    </>

  );

}

function AuditLog() {

  const { tenant } = useApp();

  const [page, setPage] = useState(1);

  const q = useLoad<any[]>("/societies/" + tenant + "/audit-logs?page=" + page);

  return (

    <>

      <Heading

        title="Every change has a history"

        text="An accountable record of actions in your society workspace."

      />

      <Panel>

        <DataState query={q}>

          {q.data?.length ? (

            <div className="table-wrap">

              <table>

                <thead>

                  <tr>

                    <th>Action</th>

                    <th>Actor</th>

                    <th>Resource</th>

                    <th>Time</th>

                  </tr>

                </thead>

                <tbody>

                  {q.data.map((a) => (

                    <tr key={a.id}>

                      <td>

                        <Badge>{a.action}</Badge>

                      </td>

                      <td>

                        <small>{a.actorId}</small>

                      </td>

                      <td>

                        <small>{a.resourceId}</small>

                      </td>

                      <td>{new Date(a.at).toLocaleString()}</td>

                    </tr>

                  ))}

                </tbody>

              </table>

            </div>

          ) : (

            <Empty

              title="No recorded changes"

              text="Your organization’s audit history will appear here."

            />

          )}

          <div className="pagination">

            <button disabled={page === 1} onClick={() => setPage(page - 1)}>

              Previous

            </button>

            <span>Page {page}</span>

            <button

              disabled={(q.data?.length || 0) < 30}

              onClick={() => setPage(page + 1)}

            >

              Next

            </button>

          </div>

        </DataState>

      </Panel>

    </>

  );

}



function AcceptInvitation() {

  const { notify } = useApp();

  const nav = useNavigate();

  const action = useAction();

  const token = new URLSearchParams(useLocation().search).get("token");

  return (

    <>

      <Heading

        title="Join your society workspace"

        text="Accept this invitation with the verified account it was addressed to."

      />

      <Panel>

        <div className="wide-form">

          <button

            className="primary"

            disabled={!token || action.busy}

            onClick={async () => {

              if (

                await action.run(() =>

                  api("/invitations/accept", "POST", { token }),

                )

              ) {

                notify(

                  "Invitation accepted. Your administrator can now assign your role.",

                );

                nav("/app");

              }

            }}

          >

            Accept invitation <ArrowRight size={16} />

          </button>

        </div>

      </Panel>

    </>

  );

}



function Platform() {

  const [page, setPage] = useState(1);

  const q = useLoad<any[]>("/platform/societies?page=" + page);

  const action = useAction();

  return (

    <>

      <Heading

        title="Platform administration"

        text="Manage society lifecycle. Private society data requires a separate authorized membership."

      />

      <Panel>

        <DataState query={q}>

          <div className="table-wrap">

            <table>

              <thead>

                <tr>

                  <th>Society</th>

                  <th>Status</th>

                  <th>Registered</th>

                  <th>Lifecycle</th>

                </tr>

              </thead>

              <tbody>

                {q.data?.map((s) => (

                  <tr key={s.id}>

                    <td>

                      {s.name}

                      <small>{s.id}</small>

                    </td>

                    <td>

                      <Badge>{s.status}</Badge>

                    </td>

                    <td>{new Date(s.createdAt).toLocaleDateString()}</td>

                    <td>

                      <button

                        className="secondary"

                        disabled={action.busy}

                        onClick={() => {

                          const note = window.prompt(

                            "Reason for lifecycle change",

                          );

                          if (note)

                            action.run(() =>

                              api(

                                "/platform/societies/" + s.id + "/status",

                                "POST",

                                { approve: s.status === "Suspended", note },

                              ),

                            );

                        }}

                      >

                        {s.status === "Suspended" ? "Reactivate" : "Suspend"}

                      </button>

                    </td>

                  </tr>

                ))}

              </tbody>

            </table>

          </div>

          <div className="pagination">

            <button disabled={page === 1} onClick={() => setPage(page - 1)}>

              Previous

            </button>

            <span>Page {page}</span>

            <button

              disabled={(q.data?.length || 0) < 30}

              onClick={() => setPage(page + 1)}

            >

              Next

            </button>

          </div>

        </DataState>

      </Panel>

    </>

  );

}



function ImageUpload({ defaultValue, unitId }: any) {

  const { tenant, notify } = useApp();

  const [value, setValue] = useState(defaultValue || "");

  const [busy, setBusy] = useState(false);

  return (

    <div className="image-upload">

      <Field

        label="Logo reference (optional)"

        name="logoUrl"

        value={value}

        onChange={(e: any) => setValue(e.target.value)}

      />

      <label className="field">

        <span>{busy ? "Uploading…" : "Upload a PNG logo · up to 2 MB"}</span>

        <input

          type="file"

          accept="image/png"

          disabled={busy}

          onChange={async (e) => {

            const file = e.target.files?.[0];

            if (!file) return;

            if (file.size > 2097152) {

              notify("Choose a PNG smaller than 2 MB");

              return;

            }

            setBusy(true);

            try {

              const csrf = await api("/auth/csrf");

              const res = await fetch(

                "/api/v1/societies/" +

                  tenant +

                  "/assets" +

                  (unitId ? "?unit=" + unitId : ""),

                {

                  method: "POST",

                  headers: {

                    "Content-Type": "image/png",

                    "X-CSRF-TOKEN": csrf.token,

                  },

                  body: file,

                  credentials: "include",

                },

              );

              const result = await res.json();

              if (!res.ok) throw new Error(result.title || "Upload failed");

              setValue(result.url);

              notify("Logo uploaded. Save the form to apply it.");

            } catch (error) {

              notify((error as Error).message);

            } finally {

              setBusy(false);

            }

          }}

        />

      </label>

      {value && (

        <img className="organization-logo" src={value} alt="Logo preview" />

      )}

    </div>

  );

}
