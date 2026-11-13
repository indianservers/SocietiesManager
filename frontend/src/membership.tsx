import {MembershipKindPicker} from "./membership-kind-picker";
import {AreaTabs} from './area-tabs';
import {WorkspaceSections,WorkspaceAlias} from './workspace-navigation';
import { useState, useEffect } from "react";
import {
  Routes,
  Route,
  Link,
  useParams,
  useNavigate,
  useLocation,
} from "react-router-dom";
import {
  Plus,
  Search,
  ArrowRight,
  ArrowUpRight,
  Users,
  ShieldCheck,
  Check,
  Download,
  Upload,
  ChevronRight,
  Mail,
} from "lucide-react";
import { api, details } from "./api";
import {
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

function useBase() {
  return "/societies/" + useApp().tenant + "/membership";
}
function useDebounce(value: string) {
  const [v, setV] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setV(value), 300);
    return () => clearTimeout(t);
  }, [value]);
  return v;
}
function Pages({ page, setPage, total, size = 20 }: any) {
  return (
    <div className="pagination">
      <span>
        {total === undefined ? "" : total + " records · "}Page {page}
      </span>
      <div>
        <button disabled={page === 1} onClick={() => setPage(page - 1)}>
          Previous
        </button>
        <button
          disabled={total !== undefined && page * size >= total}
          onClick={() => setPage(page + 1)}
        >
          Next
        </button>
      </div>
    </div>
  );
}
function localInput(value?: string) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return "";
  return new Date(date.valueOf() - date.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);
}
const fields = [
  ["preferredName", "Preferred name"],
  ["email", "Personal email"],
  ["workEmail", "Work email"],
  ["phone", "Primary mobile"],
  ["dateOfBirth", "Date of birth"],
  ["gender", "Gender (optional)"],
  ["languages", "Languages"],
  ["country", "Country"],
  ["state", "State"],
  ["city", "City"],
  ["pincode", "Pincode / postal code"],
  ["maritalStatus", "Marital status"],
  ["spouseName", "Spouse name (optional)"],
  ["anniversary", "Wedding anniversary (optional)"],
  ["education", "Highest qualification"],
  ["specialization", "Education specialization"],
  ["institution", "School / college / university"],
  ["graduationYear", "Graduation year"],
  ["employmentStatus", "Employment status"],
  ["bloodGroup", "Blood group (optional)"],
  ["emergencyContactName", "Emergency contact name"],
  ["emergencyContactPhone", "Emergency contact mobile"],
  ["otherInformation", "Other information (optional)"],
];
function PersonalFields({ profile = {} }: any) {
  return (
    <>
      <div className="form-grid">
        {fields.map(([name, label]) => (
          <Field
            key={name}
            label={label}
            name={name}
            defaultValue={profile[name] || ""}
            maxLength={4000}
            type={
              name.toLowerCase().includes("email")
                ? "email"
                : ["dateOfBirth", "anniversary"].includes(name)
                  ? "date"
                  : "text"
            }
          />
        ))}
      </div>
      <Field label="Additional mobile numbers (one per line)">
        <textarea
          name="mobiles"
          defaultValue={(profile.mobiles || []).join("\n")}
        />
      </Field>
      <Field label="Addresses (one per line)">
        <textarea
          name="addresses"
          defaultValue={(profile.addresses || []).join("\n")}
        />
      </Field>
      <div className="form-section">PROFESSIONAL & NETWORKING</div>
      <div className="form-grid">
        {[
          ["organization", "Institute / company / employer name"],
          ["registrationNumber", "Organization registration number"],
          ["representativeName", "Authorized representative name"],
          ["representativeDesignation", "Representative designation"],
          ["workLocation", "Work location"],
          ["experience", "Years of experience"],
          ["website", "Website / LinkedIn"],
          ["designation", "Designation"],
          ["profession", "Profession"],
          ["industry", "Industry"],
          ["expertise", "Expertise"],
          ["skills", "Skills"],
          ["interests", "Interests"],
          ["networking", "Networking preferences"],
        ].map(([name, label]) => (
          <Field
            key={name}
            label={label}
            name={"pro_" + name}
            defaultValue={profile.professional?.[name] || ""}
          />
        ))}
      </div>
    </>
  );
}
function ProfileData(form: FormData, existing: any = {}) {
  const p = { ...existing, professional: { ...existing.professional } };
  for (const [key, value] of form) {
    if (key.startsWith("pro_")) p.professional[key.slice(4)] = value;
    else if (key === "organizationName" || fields.some(([f]) => f === key)) p[key] = value;
    else if (key === "mobiles" || key === "addresses")
      p[key] = String(value)
        .split("\n")
        .map((x) => x.trim())
        .filter(Boolean);
  }
  return p;
}
function PrivacyFields({ privacy = {} }: any) {
  return (
    <>
      <div className="form-section">PRIVACY & CONSENT</div>
      {[
        ["directory", "Appear in the permitted member directory", true],
        ["photo", "Share my photograph", false],
        [
          "contacts",
          "Share my contact details when society policy permits",
          false,
        ],
        [
          "professional",
          "Share professional and networking information",
          false,
        ],
        [
          "communication",
          "Receive society communication, birthday and wedding anniversary greetings, and event invitations",
          false,
        ],
      ].map(([name, label, def]) => (
        <label className="checkbox" key={String(name)}>
          <input
            type="checkbox"
            name={"privacy_" + name}
            defaultChecked={privacy[String(name)] ?? def}
          />
          {label}
        </label>
      ))}
    </>
  );
}
function PrivacyData(f: FormData) {
  return Object.fromEntries(
    ["directory", "photo", "contacts", "professional", "communication"].map(
      (k) => [k, f.has("privacy_" + k)],
    ),
  );
}

export default function MembershipModule(){const {can}=useApp();return <><AreaTabs area="people"/><Routes>
 <Route index element={<WorkspaceSections label="Membership records" sections={[{id:'overview',label:'Membership overview',content:<MembershipDashboard/>},{id:'directory',label:'Member directory',content:<MemberDirectory/>}]}/>}/>
 <Route path="profile/:id" element={<MemberProfile/>}/>
 <Route path="governance" element={<WorkspaceSections label="Leadership & committees" sections={[{id:'leadership',label:'Officer appointments',content:<Leadership/>},{id:'committees',label:'Committees',content:<Committees/>}]}/>}/>
 <Route path="operations" element={<WorkspaceSections label="Membership operations" sections={[{id:'applications',label:'Applications & review',content:<ApplicationReviews/>,visible:can('members.approve')||can('members.write')},{id:'transfers',label:'Chapter transfers',content:<Transfers/>},{id:'import',label:'Bulk import',content:<BulkImport/>,visible:can('members.import')}]}/>}/>
 <Route path="settings" element={<MembershipSettings/>}/>
 {Object.entries({directory:['','directory'],leadership:['governance','leadership'],committees:['governance','committees'],applications:['operations','applications'],transfers:['operations','transfers'],import:['operations','import']}).map(([path,[target,view]])=><Route key={path} path={path} element={<WorkspaceAlias to={'/app/members'+(target?'/'+target:'')} query={{view}}/>}/>)}
 <Route path="self" element={<WorkspaceAlias to="/app/account" query={{view:'memberships'}}/>}/><Route path="*" element={<Empty title="Membership view unavailable" text="Choose a section above."/>}/></Routes></>}
function MembershipDashboard() {
  const { scope, can } = useApp();
  const base = useBase();
  const manager = can("members.read");
  const q = useLoad(
    base + "/intelligence" + (scope ? "?workspace=" + scope : ""),
    manager,
  );
  if (!manager) return <SelfService />;
  return (
    <>
      <Heading
        eyebrow="MEMBERSHIP INTELLIGENCE"
        title="People are at the heart of it"
        text="A live view of the memberships and chapters within your authority."
      >
        <Link className="primary" to="/app/members/directory">
          Explore members <ArrowRight size={16} />
        </Link>
      </Heading>
      <DataState query={q}>
        <div className="stats-grid member-stats">
          {[
            ["Memberships", q.data?.total],
            ["Membership holders", q.data?.uniquePersons],
            ["Active memberships", q.data?.active],
            ["Pending applications", q.data?.pending],
            ["New this month", q.data?.newThisMonth],
            ["Inactive memberships", q.data?.inactive],
            ["Transfers", q.data?.transfers],
            ["Current officers", q.data?.leadership],
          ].map(([name, value]) => (
            <div className="stat" key={String(name)}>
              <span>{name}</span>
              <strong>{value ?? "—"}</strong>
            </div>
          ))}
        </div>
        <div className="dashboard-grid">
          <Panel title="Membership by chapter">
            {q.data?.byChapter.length ? (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Chapter</th>
                      <th>Memberships</th>
                      <th>Active</th>
                    </tr>
                  </thead>
                  <tbody>
                    {q.data.byChapter.map((c: any) => (
                      <tr key={c.id}>
                        <td>{c.name}</td>
                        <td>{c.members}</td>
                        <td>{c.active}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <Empty
                title="Your membership story starts here"
                text="Add members or approve chapter applications to see real membership intelligence."
              />
            )}
          </Panel>
          <Panel title="Membership health">
            <dl>
              <div>
                <dt>Monthly retention</dt>
                <dd>
                  {q.data?.retentionPercent == null
                    ? "Insufficient dated history"
                    : q.data.retentionPercent + "%"}
                </dd>
              </div>
              <div>
                <dt>Chapter leadership vacancies</dt>
                <dd>
                  {q.data?.vacancies == null
                    ? "No required positions configured"
                    : q.data.vacancies}
                </dd>
              </div>
              <div>
                <dt>New-member growth</dt>
                <dd>
                  {q.data?.growthPercent == null
                    ? "No prior-month baseline"
                    : q.data.growthPercent + "%"}
                </dd>
              </div>
            </dl>
            <div className="note">
              Retention compares currently active memberships with the
              opening-month cohort. Growth compares new joins with the previous
              month.
            </div>
            {q.data?.categories.map((c: any) => (
              <div className="distribution" key={c.id}>
                <span>{c.name}</span>
                <strong>{c.count}</strong>
              </div>
            ))}
          </Panel>
        </div>
      </DataState>
    </>
  );
}
export function SelfService() {
  const { switchWorkspace } = useApp();
  const q = useLoad<any[]>("/membership/me");
  const apps = useLoad<any[]>("/membership/applications");
  return (
    <>
      <Heading
        eyebrow="MY MEMBERSHIPS"
        title="Your community, your next chapter"
        text="Manage your profile, privacy, chapter affiliations, and applications."
      >
        <Link className="primary" to="/app/join">
          <Plus size={16} />
          Apply to a chapter
        </Link>
      </Heading>
      <Panel title="Your memberships">
        <DataState query={q}>
          {q.data?.length ? (
            <div className="unit-list">
              {q.data.map((m) => (
                <Link
                  key={m.id}
                  onClick={() => switchWorkspace(m.tenantId, m.chapterId)}
                  to={"/app/members/profile/" + m.id}
                >
                  <span className="unit-icon">
                    <Users size={18} />
                  </span>
                  <div>
                    <strong>{m.chapter}</strong>
                    <small>
                      {m.society} · {m.number}
                    </small>
                  </div>
                  <Badge>{m.status}</Badge>
                  <ChevronRight size={17} />
                </Link>
              ))}
            </div>
          ) : (
            <Empty
              title="Find your community"
              text="Apply to a society chapter or accept an invitation from your officer."
            />
          )}
        </DataState>
      </Panel>
      <div className="section-spacer" />
      <Panel title="Your applications">
        <DataState query={apps}>
          {apps.data?.length ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Reference</th>
                    <th>Status</th>
                    <th>Submitted</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {apps.data.map((a) => (
                    <tr key={a.id}>
                      <td>{a.id.slice(0, 8).toUpperCase()}</td>
                      <td>
                        <Badge>{a.status}</Badge>
                      </td>
                      <td>{new Date(a.createdAt).toLocaleDateString()}</td>
                      <td>
                        {["Draft", "Changes Requested"].includes(a.status) && (
                          <Link to={"/app/join?application=" + a.id}>
                            Continue application <ArrowRight size={14} />
                          </Link>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <Empty
              title="No applications"
              text="Your chapter applications and progress appear here."
            />
          )}
        </DataState>
      </Panel>
      <div className="note">
        Explore activities and events in the Community workspace. Communication,
        dues, and identity cards follow in later phases.
      </div>
    </>
  );
}
function MemberDirectory() {
  const base = useBase();
  const { scope, can, notify } = useApp();
  const [search, setSearch] = useState("");
  const term = useDebounce(search);
  const [status, setStatus] = useState<string[]>([]);
  const [category, setCategory] = useState<string[]>([]);
  const [sort, setSort] = useState("name");
  const [page, setPage] = useState(1);
  const [columns, setColumns] = useState([
    "number",
    "chapter",
    "category",
    "designation",
    "joined",
    "status",
  ]);
  const [selected, setSelected] = useState<string[]>([]);
  const [edit, setEdit] = useState<any>();
  const [quick, setQuick] = useState<string>();
  const [invite, setInvite] = useState(false);
  const cats = useLoad<any[]>(base + "/categories");
  const views = useLoad<any[]>(base + "/views");
  const q = useLoad(
    base +
      "/members?search=" +
      encodeURIComponent(term) +
      "&statuses=" +
      status.join(",") +
      "&categories=" +
      category.join(",") +
      "&sort=" +
      sort +
      "&page=" +
      page +
      (scope ? "&workspace=" + scope : ""),
  );
  useEffect(() => setPage(1), [term, status, category, sort]);
  async function exportSelected() {
    try {
      const token = await api("/auth/csrf");
      const res = await fetch("/api/v1" + base + "/export", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-CSRF-TOKEN": token.token,
        },
        body: JSON.stringify(selected),
      });
      if (!res.ok) {
        const e = await res.json();
        throw new Error(e.title);
      }
      const url = URL.createObjectURL(await res.blob());
      const a = document.createElement("a");
      a.href = url;
      a.download = "members.csv";
      a.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      notify("Authorized member export downloaded");
    } catch (e) {
      notify((e as Error).message);
    }
  }
  return (
    <>
      <Heading
        eyebrow="PEOPLE / MEMBER DIRECTORY"
        title="Every member. A shared purpose."
        text="Search your authorized network. Privacy preferences follow every profile."
      >
        {can("members.write") && (
          <button className="secondary" onClick={() => setInvite(true)}>
            <Mail size={15} />
            Invite member
          </button>
        )}
        {can("members.write") && (
          <button className="primary" onClick={() => setEdit(null)}>
            <Plus size={16} />
            Add member
          </button>
        )}
      </Heading>
      <Panel>
        <div className="toolbar membership-toolbar">
          <div className="search">
            <Search size={16} />
            <input
              aria-label="Search members"
              placeholder="Search by name or membership number…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <select
            aria-label="Sort members"
            value={sort}
            onChange={(e) => setSort(e.target.value)}
          >
            <option value="name">Name</option>
            <option value="number">Member ID</option>
            <option value="joined">Joining date</option>
          </select>
          <details className="filter-menu">
            <summary>Filters · {status.length + category.length}</summary>
            <div>
              {[
                "Active",
                "Inactive",
                "Suspended",
                "Resigned",
                "Closed",
                "Deceased",
              ].map((s) => (
                <label className="checkbox" key={s}>
                  <input
                    type="checkbox"
                    checked={status.includes(s)}
                    onChange={() =>
                      setStatus(
                        status.includes(s)
                          ? status.filter((x) => x !== s)
                          : [...status, s],
                      )
                    }
                  />
                  {s}
                </label>
              ))}
              <hr />
              {cats.data?.map((c) => (
                <label className="checkbox" key={c.id}>
                  <input
                    type="checkbox"
                    checked={category.includes(c.id)}
                    onChange={() =>
                      setCategory(
                        category.includes(c.id)
                          ? category.filter((x) => x !== c.id)
                          : [...category, c.id],
                      )
                    }
                  />
                  {c.name}
                </label>
              ))}
            </div>
          </details>
          <details className="filter-menu">
            <summary>Columns</summary>
            <div>
              {[
                "number",
                "chapter",
                "category",
                "designation",
                "joined",
                "status",
              ].map((c) => (
                <label className="checkbox" key={c}>
                  <input
                    type="checkbox"
                    checked={columns.includes(c)}
                    onChange={() =>
                      setColumns(
                        columns.includes(c)
                          ? columns.filter((x) => x !== c)
                          : [...columns, c],
                      )
                    }
                  />
                  {c}
                </label>
              ))}
            </div>
          </details>
        </div>
        <div className="directory-views">
          <select
            aria-label="Saved member view"
            onChange={(e) => {
              if (!e.target.value) return;
              const v = details(
                views.data?.find((x) => x.id === e.target.value)?.data,
              );
              setSearch(v.search || "");
              setStatus(v.status || []);
              setCategory(v.category || []);
              setSort(v.sort || "name");
              setColumns(v.columns || columns);
            }}
          >
            <option value="">Saved views</option>
            {views.data?.map((v) => (
              <option key={v.id} value={v.id}>
                {v.name}
              </option>
            ))}
          </select>
          <button
            className="text-btn"
            onClick={async () => {
              const name = window.prompt("Name this directory view");
              if (!name) return;
              try {
                await api(base + "/views", "POST", {
                  name,
                  data: JSON.stringify({
                    search,
                    status,
                    category,
                    sort,
                    columns,
                  }),
                });
                await views.refetch();
                notify("View saved");
              } catch (e) {
                notify((e as Error).message);
              }
            }}
          >
            Save current view
          </button>
          {can("members.export") && (
            <button
              className="secondary"
              disabled={!selected.length}
              onClick={exportSelected}
            >
              <Download size={14} />
              Export {selected.length || ""}
            </button>
          )}
        </div>
        <DataState query={q}>
          {q.data?.items.length ? (
            <>
              <div className="table-wrap member-table">
                <table>
                  <thead>
                    <tr>
                      <th>
                        <input
                          aria-label="Select this page"
                          type="checkbox"
                          checked={q.data.items.every((m: any) =>
                            selected.includes(m.id),
                          )}
                          onChange={(e) =>
                            setSelected(
                              e.target.checked
                                ? [
                                    ...new Set([
                                      ...selected,
                                      ...q.data.items.map((m: any) => m.id),
                                    ]),
                                  ]
                                : selected.filter(
                                    (id) =>
                                      !q.data.items.some(
                                        (m: any) => m.id === id,
                                      ),
                                  ),
                            )
                          }
                        />
                      </th>
                      <th>Member</th>
                      {columns.map((c) => (
                        <th key={c}>{c === "number" ? "Member ID" : c}</th>
                      ))}
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {q.data.items.map((m: any) => (
                      <tr key={m.id}>
                        <td>
                          <input
                            type="checkbox"
                            aria-label={"Select " + m.fullName}
                            checked={selected.includes(m.id)}
                            onChange={(e) =>
                              setSelected(
                                e.target.checked
                                  ? [...selected, m.id]
                                  : selected.filter((x) => x !== m.id),
                              )
                            }
                          />
                        </td>
                        <td>
                          <Link
                            className="unit-name"
                            to={"/app/members/profile/" + m.id}
                          >
                            <span className="avatar">
                              {m.photo ? (
                                <img src={m.photo} alt="" />
                              ) : (
                                m.fullName.slice(0, 2).toUpperCase()
                              )}
                            </span>
                            <span>
                              <strong>{m.fullName}</strong>
                              <small>{m.city || "Member"}</small>
                            </span>
                          </Link>
                        </td>
                        {columns.map((c) => (
                          <td key={c}>
                            {c === "status" ? (
                              <Badge>{m.status}</Badge>
                            ) : c === "joined" ? (
                              new Date(m.joinedAt).toLocaleDateString()
                            ) : (
                              m[c] || "—"
                            )}
                          </td>
                        ))}
                        <td>
                          <button
                            className="text-btn"
                            onClick={() => setQuick(m.id)}
                          >
                            Quick view
                          </button>
                          {can("members.write") && m.administrative && (
                            <button
                              className="text-btn"
                              onClick={() => setEdit(m)}
                            >
                              Edit
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="member-mobile-list">
                {q.data.items.map((m: any) => (
                  <div key={m.id} className="mobile-member-row">
                    <input
                      type="checkbox"
                      aria-label={"Select " + m.fullName}
                      checked={selected.includes(m.id)}
                      onChange={(e) =>
                        setSelected(
                          e.target.checked
                            ? [...selected, m.id]
                            : selected.filter((x) => x !== m.id),
                        )
                      }
                    />
                    <Link to={"/app/members/profile/" + m.id}>
                      <span className="avatar">{m.fullName.slice(0, 2)}</span>
                      <div>
                        <strong>{m.fullName}</strong>
                        <small>
                          {m.number} · {m.chapter}
                        </small>
                      </div>
                      <Badge>{m.status}</Badge>
                      <ChevronRight size={16} />
                    </Link>
                  </div>
                ))}
              </div>
            </>
          ) : (
            <Empty
              title="No matching members"
              text="Try another filter, or add your first chapter member."
            />
          )}
        </DataState>
        <Pages page={page} setPage={setPage} total={q.data?.total || 0} />
      </Panel>
      {invite && <MemberInvitationDrawer onClose={() => setInvite(false)} />}
      {edit !== undefined && (
        <MemberEditor member={edit} onClose={() => setEdit(undefined)} />
      )}{" "}
      {quick && (
        <Drawer title="Member quick view" onClose={() => setQuick(undefined)}>
          <MemberSummary id={quick} />
        </Drawer>
      )}
    </>
  );
}
function MemberSummary({ id }: any) {
  const q = useLoad(useBase() + "/members/" + id);
  const m = q.data?.member;
  return (
    <DataState query={q}>
      <div className="member-summary">
        <span className="large-avatar">{m?.fullName.slice(0, 2)}</span>
        <h2>{m?.fullName}</h2>
        <p>{m?.designation || m?.category}</p>
        <Badge>{m?.status}</Badge>
        <dl>
          <div>
            <dt>Member ID</dt>
            <dd>{m?.number}</dd>
          </div>
          <div>
            <dt>Chapter</dt>
            <dd>{m?.chapter}</dd>
          </div>
          <div>
            <dt>Region / parent</dt>
            <dd>{m?.region || "—"}</dd>
          </div>
          {m?.contacts &&
            Object.entries(m.contacts).map(([key, value]) => (
              <div key={key}>
                <dt>{key}</dt>
                <dd>{String(value || "Not shared")}</dd>
              </div>
            ))}
        </dl>
        <Link className="secondary" to={"/app/members/profile/" + id}>
          Open profile <ArrowUpRight size={15} />
        </Link>
      </div>
    </DataState>
  );
}
function MemberEditor({ member, onClose }: any) {
  const base = useBase();
  const { tenant, can } = useApp();
  const action = useAction();
  const [identityId, setIdentityId] = useState("");
  const [identitySearch, setIdentitySearch] = useState("");
  const [fullName, setFullName] = useState(member?.fullName || "");
  const identities = useLoad(
    base +
      "/members?pageSize=100&search=" +
      encodeURIComponent(useDebounce(identitySearch)),
    !member,
  );
  const full = useLoad(base + "/members/" + member?.id, !!member);
  const cats = useLoad<any[]>(base + "/categories");
  const [chapterSearch, setChapterSearch] = useState("");
  const term = useDebounce(chapterSearch);
  const chapters = useLoad(
    "/societies/" +
      tenant +
      "/organizations/units?kind=Chapter&pageSize=100&search=" +
      encodeURIComponent(term),
  );
  const m = full.data?.member || member;
  const p = full.data?.profile || {};
  return (
    <Drawer
      title={member ? "Edit member" : "Add a chapter member"}
      onClose={onClose}
    >
      {cats.isPending || (member && full.isPending) ? (
        <p className="skeleton">Loading profile…</p>
      ) : cats.isError || (member &&
        (full.isError || !full.data?.canEdit || !full.data?.profile)) ? (
        <div className="error" role="alert">
          {cats.error?.message || full.error?.message ||
            "This private profile is unavailable for editing."}
          <button onClick={() => { if (member) void full.refetch(); void cats.refetch(); }}>Retry</button>
        </div>
      ) : (
        <form
          key={full.data?.member.revision}
          onSubmit={async (e) => {
            e.preventDefault();
            const f = new FormData(e.currentTarget);
            const body = {
              fullName: f.get("fullName"),
              chapterId: member ? m.chapterId : f.get("chapterId"),
              categoryId: f.get("categoryId") || m?.categoryId,
              personId: identityId || null,
              duplicateReviewed: f.has("duplicateReviewed"),
              number: member ? m.number : f.get("number"),
              status: f.get("status") || m?.status,
              profile: JSON.stringify(ProfileData(f, p)),
              privacy: JSON.stringify(PrivacyData(f)),
              revision: m?.revision || 0,
              joinedAt:
                f.get("joinedAt") &&
                f.get("joinedAt") !== localInput(m?.joinedAt).slice(0, 10)
                  ? new Date(
                      String(f.get("joinedAt")) + "T00:00:00",
                    ).toISOString()
                  : null,
              effectiveFrom:
                f.get("effectiveFrom") &&
                f.get("effectiveFrom") !== localInput(full.data?.effectiveFrom)
                  ? new Date(String(f.get("effectiveFrom"))).toISOString()
                  : null,
              endsAt:
                f.get("endsAt") === localInput(full.data?.endsAt)
                  ? full.data?.endsAt || null
                  : f.get("endsAt")
                    ? new Date(String(f.get("endsAt"))).toISOString()
                    : null,
            };
            if (
              await action.run(() =>
                api(
                  base + "/members" + (member ? "/" + member.id : ""),
                  member ? "PUT" : "POST",
                  body,
                ),
              )
            )
              onClose();
          }}
        >
          {!member && (
            <>
              <Field
                label="Find an existing person (optional)"
                value={identitySearch}
                onChange={(e: any) => setIdentitySearch(e.target.value)}
              />
              <Field label="Person identity">
                <select
                  value={identityId}
                  onChange={(e) => {
                    setIdentityId(e.target.value);
                    setFullName(
                      identities.data?.items.find(
                        (m: any) => m.personId === e.target.value,
                      )?.fullName || "",
                    );
                  }}
                >
                  <option value="">Create a new unclaimed person</option>
                  {identities.data?.items
                    .filter(
                      (m: any, i: number, all: any[]) =>
                        all.findIndex((x) => x.personId === m.personId) === i,
                    )
                    .map((m: any) => (
                      <option key={m.personId} value={m.personId}>
                        {m.fullName} · {m.number}
                      </option>
                    ))}
                </select>
              </Field>
            </>
          )}
          <Field
            label="Member / organization name"
            name="fullName"
            required
            maxLength={160}
            value={fullName}
            onChange={(e: any) => setFullName(e.target.value)}
            readOnly={!!identityId}
          />
          {!member && (
            <>
              <Field
                label="Find chapter"
                value={chapterSearch}
                onChange={(e: any) => setChapterSearch(e.target.value)}
              />
              <Field label="Chapter">
                <select name="chapterId" required>
                  <option value="">Select chapter</option>
                  {chapters.data?.items.map((u: any) => (
                    <option key={u.id} value={u.id}>
                      {u.name}
                    </option>
                  ))}
                </select>
              </Field>
              <Field
                label="Membership number (blank to generate)"
                name="number"
              />
            </>
          )}
          <div className="form-grid">
            <Field label="Category">
              <select
                name="categoryId"
                required
                disabled={!!member && !can("members.write")}
                defaultValue={m?.categoryId}
              >
                {cats.data
                  ?.filter((c) => c.active || c.id === m?.categoryId)
                  .map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
              </select>
            </Field>
            <Field label="Membership status">
              <select
                name="status"
                disabled={!!member && !can("members.write")}
                defaultValue={m?.status || "Inactive"}
              >
                {[
                  "Active",
                  "Inactive",
                  "Suspended",
                  "Resigned",
                  "Closed",
                  "Deceased",
                ].map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </Field>
          </div>
          {can("members.write") && (
            <div className="form-grid">
              <Field
                label="Joining date (optional)"
                name="joinedAt"
                type="date"
                defaultValue={localInput(m?.joinedAt).slice(0, 10)}
              />
              <Field
                label="Effective from (optional)"
                name="effectiveFrom"
                type="datetime-local"
                defaultValue={localInput(full.data?.effectiveFrom)}
              />
              <Field
                label="Effective until (optional)"
                name="endsAt"
                type="datetime-local"
                defaultValue={localInput(full.data?.endsAt)}
              />
            </div>
          )}
          <details className="editor-section">
            <summary>Personal & professional details</summary>
            <PersonalFields profile={p} />
          </details>
          <PrivacyFields privacy={full.data?.privacy} />
          {!member && (
            <label className="checkbox">
              <input type="checkbox" name="duplicateReviewed" />I reviewed
              matching email records and confirm this is a distinct person
            </label>
          )}
          <div className="note">
            A manually created person has no login privileges. Send a verified
            account-claim invitation after reviewing their membership.
          </div>
          <div className="drawer-actions">
            <button type="button" className="secondary" onClick={onClose}>
              Cancel
            </button>
            <button className="primary" disabled={action.busy}>
              Save member
            </button>
          </div>
        </form>
      )}
    </Drawer>
  );
}
function MemberProfile() {
  const { id } = useParams();
  const base = useBase();
  const { can, notify } = useApp();
  const action = useAction();
  const q = useLoad(base + "/members/" + id);
  const m = q.data?.member;
  const [tab, setTab] = useState("overview");
  const [edit, setEdit] = useState(false);
  const [invitation, setInvitation] = useState(false);
  const leadership = useLoad<any[]>(
    base + "/appointments?member=" + id,
    tab === "leadership",
  );
  const docs = useLoad<any[]>(
    base + "/members/" + id + "/documents",
    tab === "documents" && !!q.data?.profile,
  );
  const p = q.data?.profile;
  return (
    <DataState query={q}>
      <Heading
        eyebrow="MEMBER PROFILE"
        title={m?.fullName}
        text={(m?.number || "") + " · " + (m?.chapter || "")}
      >
        {q.data?.canEdit && (
          <button className="primary" onClick={() => setEdit(true)}>
            Edit profile
          </button>
        )}
        {can("members.write") && q.data?.canInvite && (
          <button className="secondary" onClick={() => setInvitation(true)}>
            <Mail size={15} />
            Account claim invitation
          </button>
        )}
      </Heading>
      <div className="profile-strip">
        <span className="large-avatar">
          {m?.photo ? (
            <img src={m.photo} alt={m.fullName} />
          ) : (
            m?.fullName.slice(0, 2)
          )}
        </span>
        <div>
          <h2>{m?.fullName}</h2>
          <p>
            {m?.designation || m?.category} · {m?.chapter}
          </p>
          <Badge>{m?.status}</Badge>
        </div>
      </div>
      {q.data?.canEdit && (
        <div className="photo-control">
          <label className="secondary">
            Upload profile photo
            <input
              type="file"
              accept="image/png"
              className="file-control"
              onChange={async (e) => {
                const file = e.target.files?.[0];
                if (!file) return;
                try {
                  const csrf = await api("/auth/csrf");
                  const response = await fetch(
                    "/api/v1" + base + "/members/" + id + "/photo",
                    {
                      method: "POST",
                      headers: {
                        "Content-Type": "image/png",
                        "X-CSRF-TOKEN": csrf.token,
                      },
                      body: file,
                    },
                  );
                  if (!response.ok)
                    throw new Error((await response.json()).title);
                  await q.refetch();
                  notify(
                    "Photograph saved. Directory sharing still follows your privacy choice.",
                  );
                } catch (error) {
                  notify((error as Error).message);
                }
              }}
            />
          </label>
        </div>
      )}
      <div className="tabs">
        {[
          "overview",
          "personal",
          "professional",
          "memberships",
          "leadership",
          "activities",
          "documents",
          "privacy",
        ].map((t) => (
          <button
            className={t === tab ? "selected" : ""}
            key={t}
            onClick={() => setTab(t)}
          >
            {t}
          </button>
        ))}
      </div>
      <Panel>
        {tab === "overview" ? (
          <dl>
            {[
              ["Member ID", m?.number],
              ["Chapter", m?.chapter],
              ["Region / parent", m?.region],
              ["Category", m?.category],
              ["Current position", m?.designation],
              ["Joining date", m && new Date(m.joinedAt).toLocaleDateString()],
              ["City", m?.city],
              ["Email", m?.contacts?.email],
              ["Mobile", m?.contacts?.phone],
            ].map(([k, v]) => (
              <div key={k}>
                <dt>{k}</dt>
                <dd>{v || "Not shared"}</dd>
              </div>
            ))}
          </dl>
        ) : tab === "personal" ? (
          p ? (
            <dl>
              {fields.map(([key, label]) => (
                <div key={key}>
                  <dt>{label}</dt>
                  <dd>{p[key] || "Not provided"}</dd>
                </div>
              ))}
              {["mobiles", "addresses"].map((key) => (
                <div key={key}>
                  <dt>{key}</dt>
                  <dd>{(p[key] || []).join(", ") || "Not provided"}</dd>
                </div>
              ))}
            </dl>
          ) : (
            <Empty
              title="Personal information is private"
              text="Your directory access does not include confidential personal records."
            />
          )
        ) : tab === "professional" ? (
          <dl>
            {Object.entries(p?.professional || m?.professional || {}).map(
              ([k, v]) => (
                <div key={k}>
                  <dt>{k}</dt>
                  <dd>{String(v || "Not shared")}</dd>
                </div>
              ),
            )}
          </dl>
        ) : tab === "memberships" ? (
          <>
            <dl>
              <div>
                <dt>Current affiliation</dt>
                <dd>{m?.chapter}</dd>
              </div>
              <div>
                <dt>Renewal</dt>
                <dd>
                  {q.data?.endsAt
                    ? new Date(q.data.endsAt) < new Date()
                      ? "Term ended — review required"
                      : "Current until " +
                        new Date(q.data.endsAt).toLocaleDateString()
                    : "No fixed term end"}
                </dd>
              </div>
            </dl>
            {q.data?.history ? (
              <div className="timeline">
                {q.data.history.map((h: any, i: number) => (
                  <div key={i}>
                    <span className="phase-dot" />
                    <div>
                      <strong>{h.action.replaceAll(".", " ")}</strong>
                      <small>{new Date(h.at).toLocaleString()}</small>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="note">
                Private membership history is available to the member and
                authorized administrators.
              </div>
            )}
          </>
        ) : tab === "leadership" ? (
          <DataState query={leadership}>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Position</th>
                    <th>Term</th>
                    <th>Status</th>
                    <th>Period</th>
                  </tr>
                </thead>
                <tbody>
                  {leadership.data?.map((ap) => (
                    <tr key={ap.id}>
                      <td>{ap.position}</td>
                      <td>
                        {new Date(ap.startsAt).toLocaleDateString()} —{" "}
                        {new Date(ap.endsAt).toLocaleDateString()}
                      </td>
                      <td>
                        <Badge>{ap.status}</Badge>
                      </td>
                      <td>{ap.period}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </DataState>
        ) : tab === "activities" ? (
          <MemberCommunityHistory id={m.id} />
        ) : tab === "documents" ? (
          p ? (
            <>
              <div className="wide-form">
                <Field
                  label="Upload supporting document (PNG or PDF, up to 2 MB)"
                  type="file"
                  accept="image/png,application/pdf"
                  onChange={async (e: any) => {
                    const file = e.target.files?.[0];
                    if (!file) return;
                    const name = window.prompt(
                      "Document name",
                      file.name.replace(/\.(png|pdf)$/i, ""),
                    );
                    if (!name) return;
                    try {
                      const csrf = await api("/auth/csrf");
                      const r = await fetch(
                        "/api/v1" +
                          base +
                          "/members/" +
                          id +
                          "/documents?name=" +
                          encodeURIComponent(name),
                        {
                          method: "POST",
                          headers: {
                            "Content-Type": "image/png",
                            "X-CSRF-TOKEN": csrf.token,
                          },
                          body: file,
                        },
                      );
                      if (!r.ok) throw new Error((await r.json()).title);
                      await docs.refetch();
                      notify("Supporting document stored securely");
                    } catch (e) {
                      notify((e as Error).message);
                    }
                  }}
                />
              </div>
              <DataState query={docs}>
                <div className="unit-list">
                  {docs.data?.map((d) => (
                    <a
                      key={d.id}
                      href={
                        "/api/v1" +
                        base +
                        "/members/" +
                        id +
                        "/documents/" +
                        d.id
                      }
                      download
                    >
                      <span className="unit-icon">
                        <Download size={17} />
                      </span>
                      <div>
                        <strong>{d.name}</strong>
                        <small>
                          {Math.ceil(d.length / 1024)} KB ·{" "}
                          {new Date(d.createdAt).toLocaleDateString()}
                        </small>
                      </div>
                      <ShieldCheck size={16} />
                    </a>
                  ))}
                </div>
              </DataState>
            </>
          ) : (
            <Empty
              title="Documents are restricted"
              text="Member documents require self-service or administrative access."
            />
          )
        ) : (
          <div className="wide-form">
            {q.data?.privacy ? (
              <>
                <dl>
                  {Object.entries(q.data.privacy).map(([k, v]) => (
                    <div key={k}>
                      <dt>{k}</dt>
                      <dd>{v ? "Enabled" : "Disabled"}</dd>
                    </div>
                  ))}
                </dl>
                {q.data.canEdit && (
                  <button className="secondary" onClick={() => setEdit(true)}>
                    Update privacy preferences
                  </button>
                )}
              </>
            ) : (
              <Empty
                title="Privacy preferences are personal"
                text="Only the member and authorized administrators can inspect these settings."
              />
            )}
          </div>
        )}
      </Panel>
      {edit && <MemberEditor member={m} onClose={() => setEdit(false)} />}{" "}
      {invitation && (
        <Drawer
          title="Invite member to claim their account"
          onClose={() => setInvitation(false)}
        >
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              if (
                await action.run(
                  () =>
                    api(base + "/invitations", "POST", {
                      chapterId: m.chapterId,
                      categoryId: m.categoryId,
                      personId: m.personId,
                      email: f.get("email"),
                      days: 7,
                      usageLimit: 1,
                    }),
                  "Account claim invitation created",
                )
              )
                setInvitation(false);
            }}
          >
            <Field label="Member email" name="email" type="email" required />
            <p>
              The recipient must verify this email and use the single-use claim
              link. Shared phone numbers never link identities.
            </p>
            <button className="primary" disabled={action.busy}>
              Send account claim invitation
            </button>
          </form>
        </Drawer>
      )}
    </DataState>
  );
}
export function JoinMembership() {
  const { notify, refresh } = useApp();
  const params = new URLSearchParams(useLocation().search);
  const nav = useNavigate();
  const [tenant, setTenant] = useState("");
  const brand=useLoad<any>("/branding/resolve");
  const me=useLoad<any>("/users/me");
  const [kind,setKind]=useState(params.get("kind")||"");
  const preferredKind=kind||me.data?.preferredMembershipKind||"Individual";
  useEffect(()=>{if(!tenant&&brand.data?.tenantId)setTenant(me.data?.signupTenantId||brand.data.tenantId)},[brand.data?.tenantId,me.data?.signupTenantId,tenant]);
  const [chapterSearch, setChapterSearch] = useState("");
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState("");
  const [inviteDone, setInviteDone] = useState(false);
  const societies = useLoad<any[]>(
    "/membership/catalog?search=" + encodeURIComponent(useDebounce(search)),
  );
  const catalog = useLoad(
    "/membership/catalog?tenant=" +
      tenant +
      "&search=" +
      encodeURIComponent(useDebounce(chapterSearch)),
    !!tenant,
  );
  const apps = useLoad<any[]>("/membership/applications");
  const application = apps.data?.find(
    (a) => a.id === (selected || params.get("application")),
  );
  useEffect(() => {
    if (application) setTenant(application.tenantId);
  }, [application?.tenantId]);
  const [busy, setBusy] = useState(false);
  async function accept() {
    setBusy(true);
    try {
      const result = await api("/membership/invitations/accept", "POST", {
        token: params.get("invitation"),
      });
      setInviteDone(true);
      setTenant(result.tenantId);
      setSelected(result.applicationId || "");
      refresh();
      notify(
        result.claimed
          ? "Membership identity securely claimed"
          : "Invitation accepted. Complete the application; approvals still apply.",
      );
      if (result.claimed) nav("/app/members/self");
    } catch (e) {
      notify((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <Heading
        eyebrow="MEMBERSHIP REGISTRATION"
        title="Find your next chapter"
        text="One verified identity, with meaningful connections across your societies."
      />
      {params.get("invitation") && !inviteDone ? (
        <Panel>
          <div className="wide-form">
            <h2>You’ve been invited</h2>
            <p>
              Accept with the verified email this invitation was addressed to.
              Joining a chapter still follows its approval policy.
            </p>
            <button className="primary" disabled={busy} onClick={accept}>
              Accept secure invitation <ArrowRight size={16} />
            </button>
          </div>
        </Panel>
      ) : (
        <Panel>
          <form
            className="wide-form"
            key={application?.id}
            onSubmit={async (e) => {
              e.preventDefault();
              setBusy(true);
              const f = new FormData(e.currentTarget);
              const submit =
                (e.nativeEvent as SubmitEvent).submitter?.getAttribute(
                  "value",
                ) === "submit";
              try {
                const body = {
                  tenantId: tenant,
                  chapterId: application?.chapterId || f.get("chapterId"),
                  categoryId: application?.categoryId || f.get("categoryId"),
                  profile: JSON.stringify(
                    {...ProfileData(f, details(application?.profile)), renewalReminders:f.has("renewalReminders")},
                  ),
                  submit,
                  revision: application?.revision || 0,
                };
                if (application)
                  await api(
                    "/membership/applications/" + application.id,
                    "PUT",
                    body,
                  );
                else await api("/membership/applications", "POST", body);
                refresh();
                notify(
                  submit
                    ? "Application submitted for review"
                    : "Draft application saved",
                );
                nav("/app/members/self");
              } catch (e) {
                notify((e as Error).message);
              } finally {
                setBusy(false);
              }
            }}
          >
            <div className="form-section">CHOOSE YOUR COMMUNITY</div>
            <label className="checkbox"><input type="checkbox" name="renewalReminders" defaultChecked={details(application?.profile).renewalReminders??true}/>Email me membership expiry and renewal reminders. No automatic charge.</label>
            {!application&&<MembershipKindPicker value={preferredKind} onChange={setKind} offers={catalog.data?.categories}/>}
            {preferredKind!=="Individual"&&<Field label="Institution / company name" name="organizationName" defaultValue={details(application?.profile).organizationName||""} required maxLength={160}/>}
            <Field
              label="Find society"
              value={search}
              onChange={(e: any) => setSearch(e.target.value)}
            />
            <Field label="Society">
              <select
                required
                value={tenant}
                disabled={!!application}
                onChange={(e) => setTenant(e.target.value)}
              >
                <option value="">Select society</option>
                {societies.data?.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </select>
            </Field>
            {tenant && (
              <>
                <Field
                  label="Find chapter"
                  value={chapterSearch}
                  onChange={(e: any) => setChapterSearch(e.target.value)}
                />
                <div className="form-grid">
                  <Field label="Chapter">
                    <select
                      required
                      name="chapterId"
                      defaultValue={application?.chapterId}
                      disabled={!!application}
                    >
                      <option value="">Select chapter</option>
                      {catalog.data?.chapters.map((c: any) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                  </Field>
                  <Field label="Membership category">
                    <select
                      required
                      name="categoryId"
                      key={preferredKind+":"+(catalog.data?.categories?.length||0)}
                      defaultValue={application?.categoryId}
                      disabled={!!application}
                    >
                      {catalog.data?.categories.filter((c:any)=>application||c.kind===preferredKind).map((c: any) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                  </Field>
                </div>
                <div className="note">
                  Your verified account provides your identity. Only authorized
                  reviewers can see the private information in this application.
                </div>
                <details className="editor-section">
                  <summary>Personal & professional details (optional)</summary>
                  <PersonalFields profile={details(application?.profile)} />
                </details>
                {application && (
                  <ApplicationEvidence
                    tenant={tenant}
                    application={application.id}
                    editable={true}
                  />
                )}
                <div className="drawer-actions">
                  <button
                    className="secondary"
                    name="action"
                    value="draft"
                    disabled={busy}
                  >
                    Save draft
                  </button>
                  <button
                    className="primary"
                    name="action"
                    value="submit"
                    disabled={busy}
                  >
                    Submit application <ArrowRight size={16} />
                  </button>
                </div>
              </>
            )}
          </form>
        </Panel>
      )}
    </>
  );
}
function ApplicationReviews() {
  const base = useBase();
  const { can, tenant } = useApp();
  const [page, setPage] = useState(1);
  const q = useLoad<any[]>(base + "/applications?page=" + page);
  const [review, setReview] = useState<any>();
  const action = useAction();
  return (
    <>
      <Heading
        eyebrow="MEMBERSHIP / APPROVALS"
        title="A considered welcome"
        text="Review chapter applications through each configured approval stage."
      />
      <Panel>
        <DataState query={q}>
          {q.data?.length ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Applicant</th>
                    <th>Stage</th>
                    <th>Status</th>
                    <th>Submitted</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {q.data.map((a) => (
                    <tr key={a.id}>
                      <td>
                        <strong>{a.applicant}</strong>
                        <small>Reference {a.id.slice(0, 8)}</small>
                      </td>
                      <td>
                        {details(a.stages)?.[a.stage]?.name || "Completed"}
                      </td>
                      <td>
                        <Badge>{a.status}</Badge>
                      </td>
                      <td>{new Date(a.createdAt).toLocaleDateString()}</td>
                      <td>
                        {["Submitted", "Under Review"].includes(a.status) &&
                          can(
                            details(a.stages)?.[a.stage]?.permission ||
                              "members.approve",
                          ) && (
                            <button
                              className="secondary"
                              onClick={() => setReview(a)}
                            >
                              Review application
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
              title="No applications in your review scope"
              text="Submitted applications appear here for authorized chapter reviewers."
            />
          )}
        </DataState>
        <Pages
          page={page}
          setPage={setPage}
          total={
            (q.data?.length || 0) < 30
              ? (page - 1) * 30 + (q.data?.length || 0)
              : undefined
          }
          size={30}
        />
      </Panel>
      {review && (
        <Drawer
          title={"Review " + review.applicant}
          onClose={() => setReview(null)}
        >
          <ApplicationEvidence
            tenant={tenant}
            application={review.id}
            editable={false}
          />
          <div className="review-details">
            <dl>
              {Object.entries(details(review.profile))
                .filter(([_, v]) => typeof v === "string" && v)
                .map(([k, v]) => (
                  <div key={k}>
                    <dt>{k}</dt>
                    <dd>{String(v)}</dd>
                  </div>
                ))}
            </dl>
          </div>
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              if (
                await action.run(
                  () =>
                    api(
                      base + "/applications/" + review.id + "/decision",
                      "POST",
                      {
                        decision: f.get("decision"),
                        comment: f.get("comment"),
                        revision: review.revision,
                      },
                    ),
                  "Application decision recorded",
                )
              )
                setReview(null);
            }}
          >
            <Field label="Decision">
              <select name="decision">
                <option>Approve</option>
                <option>Changes Requested</option>
                <option>Reject</option>
              </select>
            </Field>
            <Field label="Review comment">
              <textarea name="comment" required />
            </Field>
            <button className="primary" disabled={action.busy}>
              Record decision
            </button>
          </form>
        </Drawer>
      )}
    </>
  );
}
function MembershipSettings() {
  const base = useBase();
  const { tenant } = useApp();
  const [policyChapter, setPolicyChapter] = useState("");
  const chapterOptions = useLoad(
    "/societies/" + tenant + "/organizations/units?kind=Chapter&pageSize=100",
  );
  const levelOptions = useLoad<any[]>(
    "/societies/" + tenant + "/organizations/levels",
  );
  const policySuffix = policyChapter ? "?chapter=" + policyChapter : "";
  const cats = useLoad<any[]>(base + "/categories");
  const policy = useLoad(base + "/policy" + policySuffix);
  const [cat, setCat] = useState<any>();
  const action = useAction();
  return (
    <>
      <Heading
        title="Membership, with clear rules"
        text="Configure categories, directory boundaries, and approval stages without changing your hierarchy."
      >
        <button className="primary" onClick={() => setCat(null)}>
          <Plus size={16} />
          Create category
        </button>
      </Heading>
      <div className="dashboard-grid">
        <Panel title="Membership categories">
          <DataState query={cats}>
            <div className="unit-list">
              {cats.data?.map((c) => (
                <div className="settings-row" key={c.id}>
                  <div>
                    <strong>{c.name}</strong>
                    <small>{c.description || "Membership category"}</small>
                  </div>
                  <Badge>{c.active ? "Active" : "Inactive"}</Badge>
                  <button className="text-btn" onClick={() => setCat(c)}>
                    Edit
                  </button>
                </div>
              ))}
            </div>
          </DataState>
        </Panel>
        <Panel title="Membership policy">
          <div className="wide-form policy-picker">
            <Field label="Policy scope">
              <select
                value={policyChapter}
                onChange={(e) => setPolicyChapter(e.target.value)}
              >
                <option value="">Society default</option>
                {chapterOptions.data?.items.map((c: any) => (
                  <option key={c.id} value={c.id}>
                    {c.name} override
                  </option>
                ))}
              </select>
            </Field>
          </div>
          <DataState query={policy}>
            <form
              className="wide-form"
              key={policyChapter + ":" + policy.data?.revision}
              onSubmit={async (e) => {
                e.preventDefault();
                const f = new FormData(e.currentTarget);
                let stages;
                try {
                  stages = String(f.get("stages"))
                    .split("\n")
                    .filter(Boolean)
                    .map((line) => {
                      const [name, permission] = line
                        .split("|")
                        .map((s) => s.trim());
                      return {
                        name,
                        permission: permission || "members.approve",
                      };
                    });
                } catch {
                  return;
                }
                await action.run(() =>
                  api(base + "/policy" + policySuffix, "PUT", {
                    ...policy.data,
                    allowMultipleChapters: f.has("multiple"),
                    directoryScope: f.get("scope"),
                    directoryLevelId: f.get("directoryLevelId") || null,
                    selectedChapters: f.getAll("selectedChapters"),
                    contactsAllowed: f.has("contacts"),
                    sourceTransferReview: f.has("source"),
                    societyTransferReview: f.has("society"),
                    approvalStages: JSON.stringify(stages),
                  }),
                );
              }}
            >
              <label className="checkbox">
                <input
                  name="multiple"
                  type="checkbox"
                  defaultChecked={policy.data?.allowMultipleChapters}
                />
                Allow multiple chapter affiliations
              </label>
              <Field label="Directory visibility">
                <select name="scope" defaultValue={policy.data?.directoryScope}>
                  <option value="chapter">Own chapter</option>
                  <option value="district">Own district / parent</option>
                  <option value="region">Own region</option>
                  <option value="society">Society-wide</option>
                  <option value="international">
                    International within this society
                  </option>
                  <option value="selected">Explicitly selected chapters</option>
                </select>
              </Field>
              <Field label="Region / district ancestor level (optional)">
                <select
                  name="directoryLevelId"
                  defaultValue={policy.data?.directoryLevelId || ""}
                >
                  <option value="">Automatic region / immediate parent</option>
                  {levelOptions.data?.map((l) => (
                    <option key={l.id} value={l.id}>
                      {l.name}
                    </option>
                  ))}
                </select>
              </Field>
              <details className="chapter-choices">
                <summary>Selected chapter access</summary>
                {chapterOptions.data?.items.map((c: any) => (
                  <label className="checkbox" key={c.id}>
                    <input
                      type="checkbox"
                      name="selectedChapters"
                      value={c.id}
                      defaultChecked={policy.data?.selectedChapters.includes(
                        c.id,
                      )}
                    />
                    {c.name}
                  </label>
                ))}
              </details>
              <label className="checkbox">
                <input
                  name="contacts"
                  type="checkbox"
                  defaultChecked={policy.data?.contactsAllowed}
                />
                Allow contact sharing when members also consent
              </label>
              <label className="checkbox">
                <input
                  name="source"
                  type="checkbox"
                  defaultChecked={policy.data?.sourceTransferReview}
                />
                Require source chapter transfer review
              </label>
              <label className="checkbox">
                <input
                  name="society"
                  type="checkbox"
                  defaultChecked={policy.data?.societyTransferReview}
                />
                Require society transfer approval
              </label>
              <Field label="Approval stages — one stage name per line">
                <textarea
                  name="stages"
                  defaultValue={(policy.data
                    ? details(policy.data.approvalStages)
                    : []
                  )
                    .map((s: any) => s.name + " | " + s.permission)
                    .join("\n")}
                />
              </Field>
              <small>
                Each line can specify a review permission after “|”. Supported:
                members.approve, members.write, members.admin.
              </small>
              <button className="primary" disabled={action.busy}>
                Save membership policy
              </button>
            </form>
          </DataState>
        </Panel>
      </div>
      {cat !== undefined && (
        <Drawer
          title={cat ? "Edit category" : "New membership category"}
          onClose={() => setCat(undefined)}
        >
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              if (
                await action.run(() =>
                  api(
                    base + "/categories" + (cat ? "/" + cat.id : ""),
                    cat ? "PUT" : "POST",
                    {
                      name: f.get("name"),
                      description: f.get("description"),
                      active: f.has("active"),
                      kind: f.get("kind"),
                    },
                  ),
                )
              )
                setCat(undefined);
            }}
          >
            <Field
              label="Category name"
              name="name"
              defaultValue={cat?.name}
              required
              maxLength={80}
            />
            <Field label="Membership type"><select name="kind" defaultValue={cat?.kind||"Individual"}>{["Individual","Institutional","Corporate"].map(kind=><option key={kind}>{kind}</option>)}</select></Field>
            <Field label="Description">
              <textarea name="description" defaultValue={cat?.description} />
            </Field>
            <label className="checkbox">
              <input
                type="checkbox"
                name="active"
                defaultChecked={cat?.active ?? true}
              />
              Available for new memberships
            </label>
            <button className="primary" disabled={action.busy}>
              Save category
            </button>
          </form>
        </Drawer>
      )}
    </>
  );
}
function Leadership() {
  const base = useBase();
  const [page, setPage] = useState(1);
  const [handoverPage, setHandoverPage] = useState(1);
  const { can, tenant, scope } = useApp();
  const [unitSearch, setUnitSearch] = useState("");
  const [appointee, setAppointee] = useState("");
  const appointeeDocs = useLoad<any[]>(
    base + "/members/" + appointee + "/documents",
    !!appointee,
  );
  const positions = useLoad<any[]>(base + "/positions");
  const appointments = useLoad<any[]>(base + "/appointments?page=" + page);
  const handovers = useLoad<any[]>(
    base + "/handovers?page=" + handoverPage,
    can("governance.manage"),
  );
  const roles = useLoad<any[]>(
    "/societies/" + tenant + "/roles",
    can("membership.settings"),
  );
  const units = useLoad(
    "/societies/" +
      tenant +
      "/organizations/units?pageSize=100&search=" +
      encodeURIComponent(useDebounce(unitSearch)),
  );
  const [mode, setMode] = useState("");
  const [editing, setEditing] = useState<any>();
  const [tab, setTab] = useState("terms");
  const action = useAction();
  const [search, setSearch] = useState("");
  const found = useLoad(
    base +
      "/members?pageSize=100&search=" +
      encodeURIComponent(useDebounce(search)),
  );
  return (
    <>
      <Heading
        eyebrow="GOVERNANCE / LEADERSHIP"
        title="Leadership with a clear term"
        text="Approved appointments grant explicit scoped access. Incoming and past titles carry no automatic privileges."
      >
        {can("membership.settings") && (
          <button
            className="secondary"
            onClick={() => {
              setEditing(null);
              setMode("position");
            }}
          >
            Configure position
          </button>
        )}
        {can("governance.manage") && (
          <>
            <button className="secondary" onClick={() => setMode("handover")}>
              Prepare handover
            </button>
            <button className="primary" onClick={() => setMode("appointment")}>
              <Plus size={16} />
              Appoint officer
            </button>
          </>
        )}
      </Heading>
      <div className="tabs">
        {["terms", "position templates", "handovers"].map((t) => (
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
        {tab === "terms" ? (
          <DataState query={appointments}>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Officer</th>
                    <th>Position</th>
                    <th>Term</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {appointments.data?.map((ap) => (
                    <tr key={ap.id}>
                      <td>
                        <strong>{ap.person}</strong>
                        <small>{ap.method}</small>
                      </td>
                      <td>
                        {ap.position}
                        <small>{ap.period}</small>
                      </td>
                      <td>
                        {new Date(ap.startsAt).toLocaleDateString()} —{" "}
                        {new Date(ap.endsAt).toLocaleDateString()}
                      </td>
                      <td>
                        <Badge>{ap.status}</Badge>
                      </td>
                      <td>
                        {ap.canManage && (
                          <>
                            {ap.status === "Pending" && (
                              <>
                                <button
                                  className="text-btn"
                                  onClick={() =>
                                    action.run(() =>
                                      api(
                                        base +
                                          "/appointments/" +
                                          ap.id +
                                          "/decision",
                                        "POST",
                                        {
                                          decision: "Approve",
                                          comment:
                                            "Approved by authorized officer",
                                          revision: ap.revision,
                                        },
                                      ),
                                    )
                                  }
                                >
                                  Approve
                                </button>
                                <button
                                  className="text-btn"
                                  onClick={() =>
                                    action.run(() =>
                                      api(
                                        base +
                                          "/appointments/" +
                                          ap.id +
                                          "/decision",
                                        "POST",
                                        {
                                          decision: "Reject",
                                          comment: "Appointment declined",
                                          revision: ap.revision,
                                        },
                                      ),
                                    )
                                  }
                                >
                                  Reject
                                </button>
                              </>
                            )}
                            {ap.status === "Approved" && (
                              <button
                                className="text-btn"
                                onClick={() => {
                                  if (
                                    window.confirm(
                                      "Revoke this officer’s associated permissions immediately?",
                                    )
                                  )
                                    action.run(() =>
                                      api(
                                        base +
                                          "/appointments/" +
                                          ap.id +
                                          "/revoke",
                                        "POST",
                                      ),
                                    );
                                }}
                              >
                                Revoke
                              </button>
                            )}
                          </>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <Pages
                page={page}
                setPage={setPage}
                size={50}
                total={
                  (appointments.data?.length || 0) < 50
                    ? (page - 1) * 50 + (appointments.data?.length || 0)
                    : undefined
                }
              />
              {!appointments.data?.length && (
                <Empty
                  title="No leadership appointments"
                  text="Define positions, then appoint active members for explicit terms."
                />
              )}
            </div>
          </DataState>
        ) : tab === "position templates" ? (
          <DataState query={positions}>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Position</th>
                    <th>Level</th>
                    <th>Concurrent holders</th>
                    <th>Access role</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {positions.data?.map((p) => (
                    <tr key={p.id}>
                      <td>
                        {p.name}
                        <small>{p.responsibilities}</small>
                      </td>
                      <td>{p.level}</td>
                      <td>
                        {p.allowConcurrent ? "Permitted" : "One per term"}
                      </td>
                      <td>
                        {p.roleId
                          ? roles.data?.find((r) => r.id === p.roleId)?.name ||
                            "Explicit role linked"
                          : "Title only"}
                      </td>
                      <td>
                        {can("membership.settings") && (
                          <button
                            className="text-btn"
                            onClick={() => {
                              setEditing(p);
                              setMode("position");
                            }}
                          >
                            Edit
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </DataState>
        ) : can("governance.manage") ? (
          <DataState query={handovers}>
            <div className="unit-list">
              {handovers.data?.map((h) => (
                <div className="settings-row" key={h.id}>
                  <div>
                    <strong>Leadership handover</strong>
                    <small>{h.notes}</small>
                  </div>
                  <Badge>{h.status}</Badge>
                  {h.status === "Reviewed" && h.canApprove && (
                    <button
                      className="secondary"
                      onClick={() =>
                        action.run(() =>
                          api(
                            base + "/handovers/" + h.id + "/decision",
                            "POST",
                            {
                              decision: "Approve",
                              comment: "Transition approved",
                              revision: h.revision,
                            },
                          ),
                        )
                      }
                    >
                      Approve activation
                    </button>
                  )}{" "}
                  {h.status === "Prepared" && h.canReview && (
                    <button
                      className="secondary"
                      onClick={() =>
                        action.run(() =>
                          api(
                            base + "/handovers/" + h.id + "/decision",
                            "POST",
                            {
                              decision: "Review",
                              comment: "Incoming officer reviewed",
                              revision: h.revision,
                            },
                          ),
                        )
                      }
                    >
                      Incoming review
                    </button>
                  )}
                </div>
              ))}
            </div>
            <Pages
              page={handoverPage}
              setPage={setHandoverPage}
              size={100}
              total={
                (handovers.data?.length || 0) < 100
                  ? (handoverPage - 1) * 100 + (handovers.data?.length || 0)
                  : undefined
              }
            />
            {!handovers.data?.length && (
              <Empty
                title="No handovers prepared"
                text="Prepare notes, obtain incoming officer review, then approve the successor term."
              />
            )}
          </DataState>
        ) : (
          <IncomingHandovers />
        )}
      </Panel>
      {mode && (
        <Drawer
          title={
            mode === "position"
              ? "Officer position"
              : mode === "appointment"
                ? "Appoint an officer"
                : "Prepare leadership handover"
          }
          onClose={() => setMode("")}
        >
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              const body =
                mode === "position"
                  ? {
                      name: f.get("name"),
                      level: f.get("level"),
                      roleId: f.get("roleId") || null,
                      allowConcurrent: f.has("concurrent"),
                      responsibilities: f.get("responsibilities"),
                      active: f.has("active"),
                      required: f.has("required"),
                    }
                  : mode === "appointment"
                    ? {
                        membershipId: f.get("membershipId"),
                        unitId: f.get("unitId") || null,
                        positionId: f.get("positionId"),
                        startsAt: new Date(
                          String(f.get("startsAt")),
                        ).toISOString(),
                        endsAt: new Date(String(f.get("endsAt"))).toISOString(),
                        method: f.get("method"),
                        notes: f.get("notes"),
                        documentIds: f.getAll("documentIds"),
                      }
                    : {
                        outgoingId: f.get("outgoingId"),
                        incomingId: f.get("incomingId"),
                        notes: f.get("notes"),
                      };
              const path =
                mode === "position"
                  ? "/positions" + (editing ? "/" + editing.id : "")
                  : mode === "appointment"
                    ? "/appointments"
                    : "/handovers";
              if (
                await action.run(() =>
                  api(
                    base + path,
                    mode === "position" && editing ? "PUT" : "POST",
                    body,
                  ),
                )
              )
                setMode("");
            }}
          >
            {mode === "position" ? (
              <>
                <Field
                  label="Position name"
                  name="name"
                  required
                  defaultValue={editing?.name}
                />
                <Field label="Level">
                  <select
                    name="level"
                    defaultValue={editing?.level || "Chapter"}
                  >
                    {[
                      "Chapter",
                      "Region",
                      "District",
                      "National",
                      "Society",
                    ].map((s) => (
                      <option key={s}>{s}</option>
                    ))}
                  </select>
                </Field>
                <Field label="Explicit administrative role">
                  <select name="roleId" defaultValue={editing?.roleId || ""}>
                    <option value="">
                      Title only — no administrative access
                    </option>
                    {roles.data?.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name}
                      </option>
                    ))}
                  </select>
                </Field>
                <Field label="Responsibilities">
                  <textarea
                    name="responsibilities"
                    defaultValue={editing?.responsibilities}
                  />
                </Field>
                <label className="checkbox">
                  <input
                    type="checkbox"
                    name="required"
                    defaultChecked={editing?.required}
                  />
                  Required for chapter leadership coverage
                </label>
                <label className="checkbox">
                  <input
                    name="concurrent"
                    type="checkbox"
                    defaultChecked={editing?.allowConcurrent}
                  />
                  Permit concurrent holders
                </label>
                <label className="checkbox">
                  <input
                    name="active"
                    type="checkbox"
                    defaultChecked={editing?.active ?? true}
                  />
                  Available for appointment
                </label>
              </>
            ) : mode === "appointment" ? (
              <>
                <Field
                  label="Find an active member"
                  value={search}
                  onChange={(e: any) => {
                    setSearch(e.target.value);
                    setAppointee("");
                  }}
                />
                <Field label="Member">
                  <select
                    name="membershipId"
                    required
                    value={appointee}
                    onChange={(e) => setAppointee(e.target.value)}
                  >
                    <option value="">Select member</option>
                    {found.data?.items
                      .filter((m: any) => m.status === "Active")
                      .map((m: any) => (
                        <option key={m.id} value={m.id}>
                          {m.fullName} · {m.chapter}
                        </option>
                      ))}
                  </select>
                </Field>
                <Field label="Position">
                  <select name="positionId" required>
                    {positions.data
                      ?.filter((p) => p.active)
                      .map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name} · {p.level}
                        </option>
                      ))}
                  </select>
                </Field>
                <Field
                  label="Find organization"
                  value={unitSearch}
                  onChange={(e: any) => setUnitSearch(e.target.value)}
                />
                <Field label="Organization scope">
                  <select name="unitId" defaultValue={scope}>
                    <option value="">Entire society</option>
                    {units.data?.items.map((u: any) => (
                      <option key={u.id} value={u.id}>
                        {u.name}
                      </option>
                    ))}
                  </select>
                </Field>
                <div className="form-grid">
                  <Field
                    label="Term starts"
                    name="startsAt"
                    type="datetime-local"
                    required
                  />
                  <Field
                    label="Term ends"
                    name="endsAt"
                    type="datetime-local"
                    required
                  />
                </div>
                <Field label="Appointment method">
                  <select name="method">
                    {[
                      "Appointed",
                      "Elected",
                      "Acting",
                      "Temporary Delegation",
                    ].map((m) => (
                      <option key={m}>{m}</option>
                    ))}
                  </select>
                </Field>
                {appointeeDocs.data?.length ? (
                  <details className="editor-section">
                    <summary>Supporting documents</summary>
                    {appointeeDocs.data.map((d) => (
                      <label className="checkbox" key={d.id}>
                        <input
                          type="checkbox"
                          name="documentIds"
                          value={d.id}
                        />
                        {d.name}
                      </label>
                    ))}
                  </details>
                ) : null}
                <Field label="Supporting notes / document references">
                  <textarea name="notes" />
                </Field>
                <div className="note">
                  An appointment remains pending until approved by an authorized
                  officer. You cannot appoint or approve yourself.
                </div>
              </>
            ) : (
              <>
                <Field label="Outgoing officer">
                  <select name="outgoingId" required>
                    {appointments.data?.map((ap) => (
                      <option key={ap.id} value={ap.id}>
                        {ap.person} · {ap.position} ·{" "}
                        {new Date(ap.endsAt).toLocaleDateString()}
                      </option>
                    ))}
                  </select>
                </Field>
                <Field label="Incoming officer">
                  <select name="incomingId" required>
                    {appointments.data
                      ?.filter((ap) =>
                        ["Pending", "Approved"].includes(ap.status),
                      )
                      .map((ap) => (
                        <option key={ap.id} value={ap.id}>
                          {ap.person} · {ap.position} ·{" "}
                          {new Date(ap.startsAt).toLocaleDateString()}
                        </option>
                      ))}
                  </select>
                </Field>
                <Field label="Handover notes, pending work and document references">
                  <textarea name="notes" required />
                </Field>
                <div className="note">
                  Terms must be compatible and non-overlapping. The incoming
                  officer reviews before authorized activation.
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
function IncomingHandovers() {
  const base = useBase();
  const q = useLoad<any[]>(base + "/handovers");
  const action = useAction();
  return (
    <DataState query={q}>
      <div className="unit-list">
        {q.data?.map((h) => (
          <div className="settings-row" key={h.id}>
            <p>{h.notes}</p>
            <Badge>{h.status}</Badge>
            {h.status === "Prepared" && h.canReview && (
              <button
                className="secondary"
                onClick={() =>
                  action.run(() =>
                    api(base + "/handovers/" + h.id + "/decision", "POST", {
                      decision: "Review",
                      comment: "Reviewed by incoming officer",
                      revision: h.revision,
                    }),
                  )
                }
              >
                Review handover
              </button>
            )}
          </div>
        ))}
      </div>
      {!q.data?.length && (
        <Empty
          title="No handover awaiting you"
          text="Incoming officer reviews will appear here."
        />
      )}
    </DataState>
  );
}
function Committees() {
  const base = useBase();
  const [page, setPage] = useState(1);
  const [rosterPage, setRosterPage] = useState(1);
  const [activeOnly, setActiveOnly] = useState(false);
  const { tenant, can, scope } = useApp();
  const [unitSearch, setUnitSearch] = useState("");
  const q = useLoad<any[]>(
    base + "/committees?page=" + page + (activeOnly ? "&active=true" : ""),
  );
  const [edit, setEdit] = useState<any>();
  const [selected, setSelected] = useState<any>();
  const [add, setAdd] = useState(false);
  const [search, setSearch] = useState("");
  const members = useLoad(
    base +
      "/members?pageSize=100&search=" +
      encodeURIComponent(useDebounce(search)),
  );
  const units = useLoad(
    "/societies/" +
      tenant +
      "/organizations/units?pageSize=100&search=" +
      encodeURIComponent(useDebounce(unitSearch)),
  );
  const roster = useLoad<any[]>(
    base + "/committees/" + selected?.id + "/members?page=" + rosterPage,
    !!selected,
  );
  const action = useAction();
  return (
    <>
      <Heading
        eyebrow="GOVERNANCE / COMMITTEES"
        title="Shared responsibilities. Clear ownership."
        text="Build committees around the work your organization needs to do."
      >
        {can("committees.manage") && (
          <button className="primary" onClick={() => setEdit(null)}>
            <Plus size={16} />
            Create committee
          </button>
        )}
      </Heading>
      <Panel>
        <div className="toolbar membership-toolbar">
          <label className="checkbox">
            <input
              type="checkbox"
              checked={activeOnly}
              onChange={(e) => {
                setActiveOnly(e.target.checked);
                setPage(1);
              }}
            />
            Show active committees only
          </label>
        </div>
        <DataState query={q}>
          {q.data?.length ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Committee</th>
                    <th>Purpose</th>
                    <th>Members</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {q.data.map((c) => (
                    <tr key={c.id}>
                      <td>
                        <strong>{c.name}</strong>
                        <small>
                          {c.startsAt &&
                            new Date(c.startsAt).toLocaleDateString()}{" "}
                          —{" "}
                          {c.endsAt
                            ? new Date(c.endsAt).toLocaleDateString()
                            : "Ongoing"}
                        </small>
                      </td>
                      <td>{c.purpose}</td>
                      <td>{c.members}</td>
                      <td>
                        <Badge>{c.status}</Badge>
                      </td>
                      <td>
                        <button
                          className="text-btn"
                          onClick={() => {
                            setSelected(c);
                            setRosterPage(1);
                          }}
                        >
                          Open workspace
                        </button>
                        {c.canManage && (
                          <button
                            className="text-btn"
                            onClick={() => setEdit(c)}
                          >
                            Edit
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
              title="A place for shared work"
              text="Create your first membership, service, finance, or other committee."
            />
          )}
        </DataState>
        <Pages
          page={page}
          setPage={setPage}
          size={100}
          total={
            (q.data?.length || 0) < 100
              ? (page - 1) * 100 + (q.data?.length || 0)
              : undefined
          }
        />
      </Panel>
      {edit !== undefined && (
        <Drawer
          title={edit ? "Edit committee" : "Create committee"}
          onClose={() => setEdit(undefined)}
        >
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              const body = {
                name: f.get("name"),
                purpose: f.get("purpose"),
                unitId: edit ? edit.unitId : f.get("unitId") || null,
                status: f.get("status"),
                startsAt: new Date(String(f.get("startsAt"))).toISOString(),
                endsAt: f.get("endsAt")
                  ? new Date(String(f.get("endsAt"))).toISOString()
                  : null,
                revision: edit?.revision || 0,
              };
              if (
                await action.run(() =>
                  api(
                    base + "/committees" + (edit ? "/" + edit.id : ""),
                    edit ? "PUT" : "POST",
                    body,
                  ),
                )
              )
                setEdit(undefined);
            }}
          >
            <Field
              label="Committee name"
              name="name"
              required
              defaultValue={edit?.name}
            />
            <Field label="Purpose">
              <textarea name="purpose" defaultValue={edit?.purpose} />
            </Field>
            {!edit && (
              <>
                <Field
                  label="Find owning organization"
                  value={unitSearch}
                  onChange={(e: any) => setUnitSearch(e.target.value)}
                />
                <Field label="Owning organization">
                  <select name="unitId" defaultValue={scope}>
                    <option value="">Entire society</option>
                    {units.data?.items.map((u: any) => (
                      <option key={u.id} value={u.id}>
                        {u.name}
                      </option>
                    ))}
                  </select>
                </Field>
              </>
            )}
            <Field label="Status">
              <select name="status" defaultValue={edit?.status || "Active"}>
                {["Active", "Inactive", "Closed"].map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </Field>
            <Field
              label="Start date"
              type="datetime-local"
              name="startsAt"
              required
              defaultValue={localInput(edit?.startsAt)}
            />
            <Field
              label="End date (optional)"
              type="datetime-local"
              name="endsAt"
              defaultValue={localInput(edit?.endsAt)}
            />
            <button className="primary" disabled={action.busy}>
              Save committee
            </button>
          </form>
        </Drawer>
      )}
      {selected && (
        <Drawer
          title={selected.name}
          onClose={() => {
            setSelected(null);
            setAdd(false);
          }}
        >
          <div className="wide-form">
            <p>{selected.purpose}</p>
            <Badge>{selected.status}</Badge>
            {can("committees.manage") && (
              <button className="secondary" onClick={() => setAdd(!add)}>
                <Plus size={15} />
                Assign member
              </button>
            )}
          </div>
          {add && (
            <form
              onSubmit={async (e) => {
                e.preventDefault();
                const f = new FormData(e.currentTarget);
                if (
                  await action.run(() =>
                    api(
                      base + "/committees/" + selected.id + "/members",
                      "POST",
                      {
                        membershipId: f.get("membershipId"),
                        role: f.get("role"),
                        responsibilities: f.get("responsibilities"),
                        startsAt: new Date().toISOString(),
                        endsAt: null,
                      },
                    ),
                  )
                )
                  setAdd(false);
              }}
            >
              <Field
                label="Find member"
                value={search}
                onChange={(e: any) => setSearch(e.target.value)}
              />
              <Field label="Member">
                <select name="membershipId" required>
                  {members.data?.items
                    .filter((m: any) => m.status === "Active")
                    .map((m: any) => (
                      <option key={m.id} value={m.id}>
                        {m.fullName} · {m.chapter}
                      </option>
                    ))}
                </select>
              </Field>
              <Field label="Committee role">
                <select name="role">
                  {["Chairperson", "Secretary", "Member", "Coordinator"].map(
                    (s) => (
                      <option key={s}>{s}</option>
                    ),
                  )}
                </select>
              </Field>
              <Field label="Responsibilities">
                <textarea name="responsibilities" />
              </Field>
              <button className="primary" disabled={action.busy}>
                Assign committee membership
              </button>
            </form>
          )}
          <DataState query={roster}>
            <div className="committee-roster">
              {roster.data?.map((cm) => (
                <div className="settings-row" key={cm.id}>
                  <div>
                    <strong>{cm.name}</strong>
                    <small>
                      {cm.role} · {cm.responsibilities}
                    </small>
                    <small>
                      {cm.endsAt
                        ? "Past membership · " +
                          new Date(cm.endsAt).toLocaleDateString()
                        : "Current committee member"}
                    </small>
                  </div>
                  {!cm.endsAt && selected.canManage && (
                    <button
                      className="text-btn"
                      onClick={() => {
                        if (
                          window.confirm(
                            "End this committee membership and preserve its history?",
                          )
                        )
                          action.run(() =>
                            api(
                              base +
                                "/committees/" +
                                selected.id +
                                "/members/" +
                                cm.id,
                              "DELETE",
                            ),
                          );
                      }}
                    >
                      Remove
                    </button>
                  )}
                </div>
              ))}
            </div>
          </DataState>
          <Pages
            page={rosterPage}
            setPage={setRosterPage}
            size={200}
            total={
              (roster.data?.length || 0) < 200
                ? (rosterPage - 1) * 200 + (roster.data?.length || 0)
                : undefined
            }
          />
          <div className="note">
            Committee history is retained. Activities and events are available
            in the Community workspace.
          </div>
        </Drawer>
      )}
    </>
  );
}
function Transfers() {
  const base = useBase();
  const { tenant, can } = useApp();
  const [chapterSearch, setChapterSearch] = useState("");
  const [memberSearch, setMemberSearch] = useState("");
  const q = useLoad<any[]>(base + "/transfers");
  const mine = useLoad<any[]>("/membership/me");
  const adminMembers = useLoad(
    base +
      "/members?pageSize=100&search=" +
      encodeURIComponent(useDebounce(memberSearch)),
  );
  const chapters = useLoad(
    "/membership/catalog?tenant=" +
      tenant +
      "&search=" +
      encodeURIComponent(useDebounce(chapterSearch)),
  );
  const [show, setShow] = useState(false);
  const action = useAction();
  const choices = can("members.transfer")
    ? adminMembers.data?.items
    : mine.data
        ?.filter((m) => m.tenantId === tenant)
        .map((m) => ({ ...m, fullName: m.number }));
  return (
    <>
      <Heading
        eyebrow="MEMBERSHIP / TRANSFERS"
        title="A new chapter. The same history."
        text="Move memberships through source and destination review while keeping the person and original joining record."
      >
        <button className="primary" onClick={() => setShow(true)}>
          <Plus size={16} />
          Request transfer
        </button>
      </Heading>
      <Panel>
        <DataState query={q}>
          {q.data?.length ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Member</th>
                    <th>From → To</th>
                    <th>Effective</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {q.data.map((t) => (
                    <tr key={t.id}>
                      <td>{t.name}</td>
                      <td>
                        {t.source} → {t.destination}
                      </td>
                      <td>{new Date(t.effectiveAt).toLocaleDateString()}</td>
                      <td>
                        <Badge>{t.status}</Badge>
                      </td>
                      <td>
                        {t.canReview &&
                          !["Rejected", "Effective", "Approved"].includes(
                            t.status,
                          ) && (
                            <>
                              <button
                                className="text-btn"
                                onClick={() =>
                                  action.run(() =>
                                    api(
                                      base + "/transfers/" + t.id + "/decision",
                                      "POST",
                                      {
                                        decision: "Approve",
                                        comment: "Transfer review approved",
                                        revision: t.revision,
                                      },
                                    ),
                                  )
                                }
                              >
                                Approve stage
                              </button>
                              <button
                                className="text-btn"
                                onClick={() => {
                                  const comment =
                                    window.prompt("Rejection reason");
                                  if (comment)
                                    action.run(() =>
                                      api(
                                        base +
                                          "/transfers/" +
                                          t.id +
                                          "/decision",
                                        "POST",
                                        {
                                          decision: "Reject",
                                          comment,
                                          revision: t.revision,
                                        },
                                      ),
                                    );
                                }}
                              >
                                Reject
                              </button>
                            </>
                          )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <Empty
              title="No transfers in progress"
              text="Every effective move preserves the original person, membership number and joining history."
            />
          )}
        </DataState>
      </Panel>
      {show && (
        <Drawer title="Request chapter transfer" onClose={() => setShow(false)}>
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              if (
                await action.run(
                  () =>
                    api(base + "/transfers", "POST", {
                      membershipId: f.get("membershipId"),
                      destinationId: f.get("destinationId"),
                      effectiveAt: new Date(
                        String(f.get("effectiveAt")),
                      ).toISOString(),
                      notes: f.get("notes"),
                    }),
                  "Transfer requested for review",
                )
              )
                setShow(false);
            }}
          >
            {can("members.transfer") && (
              <Field
                label="Find membership"
                value={memberSearch}
                onChange={(e: any) => setMemberSearch(e.target.value)}
              />
            )}
            <Field label="Membership">
              <select name="membershipId" required>
                {choices
                  ?.filter((m: any) => m.status === "Active")
                  .map((m: any) => (
                    <option value={m.id} key={m.id}>
                      {m.fullName} · {m.chapter}
                    </option>
                  ))}
              </select>
            </Field>
            <Field
              label="Find destination chapter"
              value={chapterSearch}
              onChange={(e: any) => setChapterSearch(e.target.value)}
            />
            <Field label="Destination chapter">
              <select name="destinationId" required>
                {chapters.data?.chapters.map((c: any) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </Field>
            <Field
              label="Effective date and time"
              name="effectiveAt"
              type="datetime-local"
              required
            />
            <Field label="Reason / notes">
              <textarea name="notes" required />
            </Field>
            <div className="note">
              Effective transfers revoke source officer permissions and close
              source chapter committee affiliations. A new leadership
              appointment requires separate approval.
            </div>
            <button className="primary" disabled={action.busy}>
              Request transfer
            </button>
          </form>
        </Drawer>
      )}
    </>
  );
}
function BulkImport() {
  const base = useBase();
  const { tenant, notify, refresh } = useApp();
  const [chapterSearch, setChapterSearch] = useState("");
  const action = useAction();
  const chapters = useLoad(
    "/societies/" +
      tenant +
      "/organizations/units?kind=Chapter&pageSize=100&search=" +
      encodeURIComponent(useDebounce(chapterSearch)),
  );
  const cats = useLoad<any[]>(base + "/categories");
  const jobs = useLoad<any[]>(base + "/imports");
  const [job, setJob] = useState("");
  const [page, setPage] = useState(1);
  const detail = useLoad(base + "/imports/" + job + "?page=" + page, !!job);
  const [content, setContent] = useState("");
  const [format, setFormat] = useState("csv");
  const [headers, setHeaders] = useState<string[]>([]);
  const [mapping, setMapping] = useState<Record<string, string>>({
    fullName: "FullName",
    email: "Email",
    phone: "Phone",
    city: "City",
    number: "Number",
  });
  useEffect(() => {
    if (!job || !["Queued", "Processing"].includes(detail.data?.status)) return;
    const timer = setInterval(() => {
      void detail.refetch();
      void jobs.refetch();
    }, 1500);
    return () => clearInterval(timer);
  }, [job, detail.data?.status]);
  return (
    <>
      <Heading
        eyebrow="MEMBERSHIP / BULK IMPORT"
        title="Bring your member records together"
        text="Upload, map, validate and review. Confirmed jobs run durably in the background."
      />
      <div className="import-steps">
        {[
          "1 · Upload",
          "2 · Map fields",
          "3 · Validate & preview",
          "4 · Confirm import",
          "5 · Review results",
        ].map((s) => (
          <span key={s}>{s}</span>
        ))}
      </div>
      <div className="dashboard-grid">
        <Panel title="Prepare import">
          <form
            className="wide-form"
            onSubmit={async (e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              try {
                const result = await api(base + "/imports/preview", "POST", {
                  chapterId: f.get("chapterId"),
                  categoryId: f.get("categoryId"),
                  format,
                  content,
                  mapping,
                });
                setJob(result.id);
                setHeaders(result.headers);
                setPage(1);
                refresh();
                notify("Preview validated. Review each row before confirming.");
              } catch (e) {
                notify((e as Error).message);
              }
            }}
          >
            <Field label="CSV or Excel workbook">
              <input
                type="file"
                accept=".csv,.xlsx"
                required
                onChange={async (e) => {
                  const file = e.target.files?.[0];
                  if (!file) return;
                  if (file.size > 8000000) {
                    notify("Choose a file smaller than 8 MB");
                    return;
                  }
                  if (file.name.toLowerCase().endsWith(".xlsx")) {
                    setFormat("xlsx");
                    const reader = new FileReader();
                    reader.onload = () =>
                      setContent(String(reader.result).split(",")[1]);
                    reader.readAsDataURL(file);
                  } else {
                    setFormat("csv");
                    setContent(await file.text());
                  }
                }}
              />
            </Field>
            <Field
              label="Find destination chapter"
              value={chapterSearch}
              onChange={(e: any) => setChapterSearch(e.target.value)}
            />
            <div className="form-grid">
              <Field label="Destination chapter">
                <select name="chapterId" required>
                  {chapters.data?.items.map((c: any) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Category">
                <select name="categoryId" required>
                  {cats.data
                    ?.filter((c) => c.active)
                    .map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                </select>
              </Field>
            </div>
            <div className="form-section">MAP YOUR COLUMN HEADERS</div>
            {Object.keys(mapping).map((key) => (
              <Field
                key={key}
                label={
                  key === "fullName"
                    ? "Full name (required)"
                    : key === "number"
                      ? "Membership ID (optional)"
                      : key
                }
              >
                {headers.length ? (
                  <select
                    value={mapping[key]}
                    onChange={(e) =>
                      setMapping({ ...mapping, [key]: e.target.value })
                    }
                  >
                    <option value="">Not mapped</option>
                    {headers.map((h) => (
                      <option key={h}>{h}</option>
                    ))}
                  </select>
                ) : (
                  <input
                    value={mapping[key]}
                    onChange={(e) =>
                      setMapping({ ...mapping, [key]: e.target.value })
                    }
                  />
                )}
              </Field>
            ))}
            <div className="note">
              Imports create inactive, unclaimed person records. Existing
              verified data is never overwritten. Matching email or member IDs
              requires explicit review; shared phone numbers are permitted.
            </div>
            <button className="primary" disabled={!content}>
              Validate & preview <ArrowRight size={16} />
            </button>
          </form>
        </Panel>
        <Panel title="Import jobs">
          <DataState query={jobs}>
            <div className="unit-list">
              {jobs.data?.map((j) => (
                <button
                  className="settings-row job-row"
                  key={j.id}
                  onClick={() => {
                    setJob(j.id);
                    setPage(1);
                  }}
                >
                  <span className="unit-icon">
                    <Upload size={16} />
                  </span>
                  <div>
                    <strong>Import {j.id.slice(0, 8)}</strong>
                    <small>
                      {j.processed} / {j.total} rows ·{" "}
                      {new Date(j.createdAt).toLocaleString()}
                    </small>
                  </div>
                  <Badge>{j.status}</Badge>
                  <ChevronRight size={15} />
                </button>
              ))}
            </div>
            {!jobs.data?.length && (
              <Empty
                title="Your import history"
                text="Validated previews and confirmed job progress appear here."
              />
            )}
          </DataState>
        </Panel>
      </div>
      {job && (
        <div className="section-spacer">
          <Panel title="Row preview & results">
            <DataState query={detail}>
              <div className="directory-views">
                <Badge>{detail.data?.status}</Badge>
                <span className="muted">
                  {detail.data?.processed} of {detail.data?.total} processed
                </span>
                {detail.data?.status === "Preview" && (
                  <button
                    className="primary"
                    disabled={action.busy}
                    onClick={() => {
                      if (
                        window.confirm(
                          "Import all valid rows as inactive member records? Invalid rows will remain in the report.",
                        )
                      )
                        action.run(
                          () =>
                            api(base + "/imports/" + job + "/confirm", "POST"),
                          "Import queued",
                        );
                    }}
                  >
                    <Check size={15} />
                    Confirm import
                  </button>
                )}
                {((["Complete"].includes(detail.data?.status) &&
                  detail.data?.failedRows > 0) ||
                  detail.data?.status === "Blocked") && (
                  <button
                    className="secondary"
                    disabled={action.busy}
                    onClick={() =>
                      action.run(
                        () => api(base + "/imports/" + job + "/retry", "POST"),
                        "Failed rows queued for retry",
                      )
                    }
                  >
                    Retry failed rows
                  </button>
                )}
              </div>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Row</th>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Member ID</th>
                      <th>Status</th>
                      <th>Validation</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.data?.rows.map((r: any) => {
                      const d = details(r.data);
                      return (
                        <tr key={r.id}>
                          <td>{r.row}</td>
                          <td>{d.fullName}</td>
                          <td>{d.email || "—"}</td>
                          <td>{d.number || "Generated on import"}</td>
                          <td>
                            <Badge>{r.status}</Badge>
                          </td>
                          <td>{r.error || "Validated"}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
              <Pages
                page={page}
                setPage={setPage}
                total={detail.data?.total || 0}
                size={50}
              />
            </DataState>
          </Panel>
        </div>
      )}
    </>
  );
}

function MemberInvitationDrawer({ onClose }: any) {
  const base = useBase();
  const { tenant, notify } = useApp();
  const [search, setSearch] = useState("");
  const chapters = useLoad(
    "/societies/" +
      tenant +
      "/organizations/units?kind=Chapter&pageSize=100&search=" +
      encodeURIComponent(useDebounce(search)),
  );
  const categories = useLoad<any[]>(base + "/categories");
  const invitations = useLoad<any[]>(base + "/invitations");
  const [result, setResult] = useState<any>();
  const [qr, setQr] = useState("");
  const action = useAction();
  return (
    <Drawer title="Invite a new chapter member" onClose={onClose}>
      {result ? (
        <div className="wide-form">
          <div className="success">
            <Check />
            Invitation created. The recipient must verify their email and
            complete the membership approval process.
          </div>
          <Field label="Secure invitation link" value={result.url} readOnly />
          {qr && (
            <img
              src={qr}
              alt="Secure membership invitation QR code"
              className="invitation-qr"
            />
          )}
          <div className="hero-actions">
            <button
              className="secondary"
              onClick={async () => {
                try {
                  await navigator.clipboard.writeText(result.url);
                  notify("Invitation link copied");
                } catch {
                  notify("Select and copy the invitation link above");
                }
              }}
            >
              Copy link
            </button>
            {qr && (
              <a
                className="secondary"
                href={qr}
                download="membership-invitation.png"
              >
                Download QR code
              </a>
            )}
          </div>
          <p>
            Expires {new Date(result.expiresAt).toLocaleString()}. Share only
            with the intended recipient.
          </p>
          <button
            className="text-btn"
            onClick={() => {
              setResult(undefined);
              setQr("");
            }}
          >
            Create another invitation
          </button>
        </div>
      ) : (
        <form
          onSubmit={async (e) => {
            e.preventDefault();
            const f = new FormData(e.currentTarget);
            try {
              const invitation = await api(base + "/invitations", "POST", {
                chapterId: f.get("chapterId"),
                categoryId: f.get("categoryId"),
                email: f.get("email"),
                personId: null,
                days: Number(f.get("days")),
                usageLimit: 1,
              });
              setResult(invitation);
              const qrcode = await import("qrcode");
              setQr(
                await qrcode.toDataURL(invitation.url, {
                  width: 240,
                  margin: 2,
                  errorCorrectionLevel: "M",
                }),
              );
              await invitations.refetch();
              notify("Invitation created and queued for email delivery");
            } catch (error) {
              notify((error as Error).message);
            }
          }}
        >
          <Field label="Recipient email" name="email" type="email" required />
          <Field
            label="Find chapter"
            value={search}
            onChange={(e: any) => setSearch(e.target.value)}
          />
          <Field label="Chapter">
            <select name="chapterId" required>
              {chapters.data?.items
                .filter((c: any) => c.status === "Active")
                .map((c: any) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
            </select>
          </Field>
          <Field label="Intended category">
            <select name="categoryId" required>
              {categories.data
                ?.filter((c) => c.active)
                .map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
            </select>
          </Field>
          <Field
            label="Valid for (days)"
            name="days"
            type="number"
            min={1}
            max={30}
            defaultValue={7}
          />
          <div className="note">
            QR codes are generated locally. The invitation is recipient-bound,
            single-use and never bypasses approval.
          </div>
          <button className="primary">Create invitation</button>
        </form>
      )}
      <div className="wide-form">
        <h3>Invitation history</h3>
        {invitations.data?.map((i) => (
          <div className="settings-row" key={i.id}>
            <div>
              <strong>{i.email}</strong>
              <small>
                {i.reference} · {i.uses}/{i.limit} uses
              </small>
            </div>
            <Badge>{i.status}</Badge>
            {i.status === "Active" && (
              <button
                className="text-btn"
                disabled={action.busy}
                onClick={() =>
                  action.run(() =>
                    api(base + "/invitations/" + i.id + "/revoke", "POST"),
                  )
                }
              >
                Revoke
              </button>
            )}
          </div>
        ))}
      </div>
    </Drawer>
  );
}

function ApplicationEvidence({ tenant, application, editable }: any) {
  const { notify, refresh } = useApp();
  const path =
    "/societies/" + tenant + "/membership/applications/" + application;
  const q = useLoad<any[]>(path + "/documents");
  const history = useLoad<any[]>(path + "/history");
  return (
    <div className="application-evidence">
      <h3>Supporting documents</h3>
      {editable && (
        <Field
          label="Attach a PNG or PDF (up to 2 MB)"
          type="file"
          accept="image/png,application/pdf"
          onChange={async (e: any) => {
            const file = e.target.files?.[0];
            if (!file) return;
            try {
              const csrf = await api("/auth/csrf");
              const r = await fetch(
                "/api/v1" +
                  path +
                  "/documents?name=" +
                  encodeURIComponent(file.name.replace(/\.(png|pdf)$/i, "")),
                {
                  method: "POST",
                  headers: {
                    "Content-Type": file.type || "application/octet-stream",
                    "X-CSRF-TOKEN": csrf.token,
                  },
                  body: file,
                },
              );
              if (!r.ok) throw new Error((await r.json()).title);
              refresh();
              notify("Application evidence uploaded securely");
            } catch (error) {
              notify((error as Error).message);
            }
          }}
        />
      )}
      <div className="unit-list">
        {q.data?.map((d) => (
          <a key={d.id} href={"/api/v1" + path + "/documents/" + d.id} download>
            <Download size={15} />
            <div>
              <strong>{d.name}</strong>
              <small>{Math.ceil(d.length / 1024)} KB</small>
            </div>
          </a>
        ))}
      </div>
      {!q.data?.length && (
        <small>
          No supporting documents attached. Save a draft before adding files.
        </small>
      )}
      {history.data?.length ? (
        <details className="editor-section">
          <summary>Review history</summary>
          {history.data.map((d) => (
            <div className="settings-row" key={d.id}>
              <div>
                <strong>
                  {d.decision} · {d.reviewer}
                </strong>
                <small>{d.comment}</small>
                <small>{new Date(d.at).toLocaleString()}</small>
              </div>
            </div>
          ))}
        </details>
      ) : null}
    </div>
  );
}

function MemberCommunityHistory({ id }: { id: string }) {
  const { tenant } = useApp();
  const q = useLoad(
    "/societies/" + tenant + "/community/members/" + id + "/activity",
  );
  return (
    <DataState query={q}>
      {q.data?.length ? (
        q.data.map((p: any) => (
          <Link
            className="unit-list"
            key={p.id}
            to={"/app/community/posts/" + p.id}
          >
            <h3>{p.title || "Chapter activity"}</h3>
            <small>{new Date(p.publishedAt).toLocaleDateString()}</small>
          </Link>
        ))
      ) : (
        <Empty
          title="No visible activities yet"
          text="Published stories appear here according to their audience rules."
        />
      )}
    </DataState>
  );
}
