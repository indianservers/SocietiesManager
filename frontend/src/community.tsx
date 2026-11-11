import { useEffect, useRef, useState } from "react";
import {
  Link,
  NavLink,
  Route,
  Routes,
  useParams,
  useSearchParams,
} from "react-router-dom";
import { useInfiniteQuery, useQueryClient } from "@tanstack/react-query";
import { useVirtualizer } from "@tanstack/react-virtual";
import {
  ArrowRight,
  Bookmark,
  CalendarDays,
  Check,
  ChevronLeft,
  ChevronRight,
  Heart,
  ImagePlus,
  MapPin,
  MessageCircle,
  Plus,
  Search,
  Send,
  ShieldCheck,
  Sparkles,
  Users,
  Video,
} from "lucide-react";
import { api } from "./api";
import {
  Badge,
  DataState,
  Drawer,
  Empty,
  Field,
  Heading,
  Panel,
  useAction,
  useApp,
  useLoad,
} from "./shared";
import "./community.css";

const date = (s: string, timeZone?: string) =>
  new Date(s).toLocaleDateString(undefined, {
    day: "numeric",
    month: "short",
    year: "numeric",
    ...(timeZone ? { timeZone } : {}),
  });
const clock = (s: string, timeZone?: string) =>
  new Date(s).toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    ...(timeZone ? { timeZone } : {}),
  });
const local = (s?: string) => {
  const d = s ? new Date(s) : new Date();
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);
};
const iso = (s: string) => new Date(s).toISOString();
function useCommunity() {
  const app = useApp();
  return { ...app, base: "/societies/" + app.tenant + "/community" };
}
function Rich({ html }: { html: string }) {
  return (
    <div
      className="community-rich"
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}
function Choice({ label, checked, onChange }: any) {
  return (
    <label className="community-choice">
      <input type="checkbox" checked={checked} onChange={onChange} />
      {label}
    </label>
  );
}
function ToggleIds({ items, value, onChange, label = "name" }: any) {
  return (
    <div className="community-picker">
      {items?.map((item: any) => (
        <Choice
          key={item.id}
          label={item[label] || item.fullName}
          checked={value.includes(item.id)}
          onChange={() =>
            onChange(
              value.includes(item.id)
                ? value.filter((v: string) => v !== item.id)
                : [...value, item.id],
            )
          }
        />
      ))}
    </div>
  );
}
function Editor({ value, onChange }: any) {
  const ref = useRef<HTMLTextAreaElement>(null);
  function wrap(tag: string) {
    const el = ref.current!;
    const a = el.selectionStart,
      b = el.selectionEnd;
    onChange(
      value.slice(0, a) +
        `<${tag}>` +
        value.slice(a, b) +
        `</${tag}>` +
        value.slice(b),
    );
  }
  return (
    <div className="community-editor">
      <div className="editor-tools">
        {[
          ["strong", "Bold"],
          ["em", "Italic"],
          ["p", "Paragraph"],
          ["blockquote", "Quote"],
          ["li", "List item"],
        ].map(([tag, label]) => (
          <button type="button" key={tag} onClick={() => wrap(tag)}>
            {label}
          </button>
        ))}
      </div>
      <textarea
        ref={ref}
        aria-label="Post content"
        rows={7}
        maxLength={20000}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="Share the work, the people, and the difference you made…"
      />
      <small>
        Select text and choose formatting. Only safe text formatting is
        retained.
      </small>
    </div>
  );
}
export default function Community() {
  const { tenant, switchWorkspace, refresh } = useCommunity();
  const [entryParams] = useSearchParams();
  const inviteTenant = entryParams.get("tenant");
  const inviteToken = entryParams.get("invitation");
  const [claiming, setClaiming] = useState(false);
  const [claimError, setClaimError] = useState("");
  useEffect(() => {
    if (inviteTenant && inviteTenant !== tenant && inviteToken && !claiming) {
      setClaiming(true);
      api(
        "/societies/" + inviteTenant + "/community/invitations/accept",
        "POST",
        { kind: inviteToken },
      )
        .then(() => {
          refresh();
          switchWorkspace(inviteTenant);
        })
        .catch((e) => setClaimError(e.message))
        .finally(() => setClaiming(false));
    }
  }, [inviteTenant, inviteToken, tenant]);
  if (claimError)
    return <Empty title="Invitation unavailable" text={claimError} />;
  if (inviteTenant && inviteTenant !== tenant)
    return <p>Opening your recipient invitation…</p>;
  return tenant ? (
    <>
      <nav className="membership-nav community-nav">
        {[
          ["", "Community"],
          ["events", "Events"],
          ["invitations", "My invitations"],
          ["intelligence", "Intelligence"],
          ["review", "Approvals"],
          ["moderation", "Moderation"],
          ["settings", "Policies"],
        ].map(([path, label]) => (
          <NavLink
            key={path}
            end
            to={"/app/community" + (path ? "/" + path : "")}
          >
            {label}
          </NavLink>
        ))}
      </nav>
      <Routes>
        <Route index element={<Feed />} />
        <Route path="posts/:id" element={<PostDetail />} />
        <Route path="events" element={<Events />} />
        <Route path="events/:id" element={<EventDetail />} />
        <Route path="invitations" element={<Invitations />} />
        <Route path="review" element={<ReviewCenter />} />
        <Route path="moderation" element={<Moderation />} />
        <Route path="settings" element={<Settings />} />
        <Route path="intelligence" element={<Intelligence />} />
        <Route path="chapters/:id" element={<Chapter />} />
      </Routes>
    </>
  ) : (
    <Empty
      title="Choose a society"
      text="Select an authorized society to open its community."
    />
  );
}

function Feed({ chapter, achievement = false }: any) {
  const { base, scope } = useCommunity();
  const [extra, setExtra] = useState({
    organization: "",
    author: "",
    from: "",
    to: "",
    event: "",
  });
  const authors = useLoad(base + "/authors");
  const eventOptions = useLoad(base + "/events");
  const setup = useLoad(base + "/setup");
  const [composer, setComposer] = useState(false);
  const [search, setSearch] = useState(""),
    [debounced, setDebounced] = useState(""),
    [category, setCategory] = useState(""),
    [saved, setSaved] = useState(false);
  const scroller = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(search), 300);
    return () => clearTimeout(t);
  }, [search]);
  const params = new URLSearchParams();
  if (chapter) params.set("chapter", chapter);
  else if (scope) params.set("workspace", scope);
  if (extra.organization && !chapter) params.set("chapter", extra.organization);
  if (extra.author) params.set("author", extra.author);
  if (extra.event) params.set("relatedEvent", extra.event);
  if (extra.from)
    params.set("from", new Date(extra.from + "T00:00:00Z").toISOString());
  if (extra.to)
    params.set(
      "to",
      new Date(
        new Date(extra.to + "T00:00:00Z").getTime() + 86400000,
      ).toISOString(),
    );
  if (category) params.set("category", category);
  if (debounced) params.set("search", debounced);
  if (saved) params.set("bookmarks", "true");
  const path = base + "/feed?" + params;
  const feed = useInfiniteQuery({
    queryKey: [path],
    initialPageParam: "",
    queryFn: ({ pageParam }) => api(path + pageParam),
    getNextPageParam: (p) =>
      p.hasMore
        ? "&before=" +
          encodeURIComponent(p.next.before) +
          "&beforeId=" +
          p.next.beforeId
        : undefined,
    refetchInterval: 30000,
  });
  const items =
    feed.data?.pages
      .flatMap((p) => p.items)
      .filter((p: any) => !achievement || p.achievement) || [];
  const virtual = useVirtualizer({
    count: items.length,
    getScrollElement: () => scroller.current,
    estimateSize: () => 420,
    overscan: 3,
  });
  const lastVisible = virtual.getVirtualItems().at(-1)?.index ?? -1;
  useEffect(() => {
    if (
      lastVisible >= items.length - 3 &&
      lastVisible >= 0 &&
      feed.hasNextPage &&
      !feed.isFetchingNextPage
    )
      void feed.fetchNextPage();
  }, [lastVisible, items.length, feed.hasNextPage, feed.isFetchingNextPage]);
  return (
    <>
      <Heading
        eyebrow="COMMUNITY / EVERY CHAPTER, CONNECTED"
        title={
          achievement
            ? "Recognition that matters."
            : "Good work brings us together."
        }
        text="Stories, service and shared moments from your authorized community."
      >
        <button className="primary" onClick={() => setComposer(true)}>
          <Plus size={16} />
          Share an activity
        </button>
      </Heading>
      <div className="community-layout">
        <section>
          <div className="feed-composer" onClick={() => setComposer(true)}>
            <span className="community-avatar">
              <Sparkles size={20} />
            </span>
            <button>What is your chapter working on?</button>
            <ImagePlus size={20} />
          </div>
          <div className="feed-filters">
            <div className="search">
              <Search size={16} />
              <input
                aria-label="Search activities"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search stories…"
              />
            </div>
            <select
              aria-label="Activity category"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
            >
              <option value="">All categories</option>
              {setup.data?.categories
                .filter((c: any) => c.kind === "Activity" && c.active)
                .map((c: any) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
            </select>
            <button
              className={saved ? "active" : ""}
              onClick={() => setSaved(!saved)}
              aria-pressed={saved}
            >
              <Bookmark size={16} />
              Saved
            </button>
          </div>
          <details className="feed-more-filters">
            <summary>More filters</summary>
            <div className="form-grid padded">
              <Field label="Organization">
                <select
                  value={extra.organization}
                  onChange={(e) =>
                    setExtra({ ...extra, organization: e.target.value })
                  }
                >
                  <option value="">All authorized organizations</option>
                  {setup.data?.organizations.map((u: any) => (
                    <option key={u.id} value={u.id}>
                      {u.name}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Author">
                <select
                  value={extra.author}
                  onChange={(e) =>
                    setExtra({ ...extra, author: e.target.value })
                  }
                >
                  <option value="">All authors</option>
                  {authors.data?.map((u: any) => (
                    <option key={u.id} value={u.id}>
                      {u.name}
                    </option>
                  ))}
                </select>
              </Field>
              <Field
                label="From date (UTC)"
                type="date"
                value={extra.from}
                onChange={(e: any) =>
                  setExtra({ ...extra, from: e.target.value })
                }
              />
              <Field
                label="Through date (UTC)"
                type="date"
                value={extra.to}
                onChange={(e: any) =>
                  setExtra({ ...extra, to: e.target.value })
                }
              />
              <Field label="Related event">
                <select
                  value={extra.event}
                  onChange={(e) =>
                    setExtra({ ...extra, event: e.target.value })
                  }
                >
                  <option value="">Any event</option>
                  {eventOptions.data?.items.map((e: any) => (
                    <option value={e.id} key={e.id}>
                      {e.title}
                    </option>
                  ))}
                </select>
              </Field>
            </div>
          </details>
          <DataState query={feed}>
            {items.length ? (
              <div className="virtual-feed" ref={scroller}>
                <div
                  style={{
                    height: virtual.getTotalSize(),
                    position: "relative",
                    width: "100%",
                  }}
                >
                  {virtual.getVirtualItems().map((v) => (
                    <div
                      key={items[v.index].id}
                      data-index={v.index}
                      ref={virtual.measureElement}
                      style={{
                        position: "absolute",
                        top: 0,
                        left: 0,
                        width: "100%",
                        transform: `translateY(${v.start}px)`,
                      }}
                    >
                      <PostCard post={items[v.index]} path={path} />
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <Empty
                title="Your next story starts here"
                text="Share an activity, or choose another category."
              />
            )}
            {feed.hasNextPage && (
              <button
                className="load-more"
                disabled={feed.isFetchingNextPage}
                onClick={() => feed.fetchNextPage()}
              >
                Load more stories
              </button>
            )}
          </DataState>
        </section>
        <aside className="community-aside">
          <div className="community-note">
            <span className="eyebrow">A COMMUNITY WITH PURPOSE</span>
            <h2>
              Small actions.
              <br />
              Lasting impact.
            </h2>
            <p>
              Recognize a volunteer. Share a chapter milestone. Bring your next
              idea to life.
            </p>
            <Link to="/app/community/events">
              Discover upcoming events <ArrowRight size={16} />
            </Link>
          </div>
          <Upcoming />
          <div className="privacy-note">
            <ShieldCheck size={18} />
            <p>
              Every story follows its audience settings. Private chapter content
              stays within its authorized audience.
            </p>
          </div>
        </aside>
      </div>
      {composer && (
        <PostComposer
          initialUnit={chapter}
          setup={setup.data}
          onClose={() => {
            setComposer(false);
            feed.refetch();
          }}
        />
      )}
    </>
  );
}
function Upcoming() {
  const [windowStart] = useState(() => Date.now());
  const { base, scope } = useCommunity();
  const q = useLoad(
    base +
      "/events?" +
      new URLSearchParams({
        ...(scope ? { workspace: scope } : {}),
        from: new Date(windowStart).toISOString(),
        to: new Date(windowStart + 30 * 86400000).toISOString(),
      }),
  );
  return (
    <Panel title="On the calendar">
      {q.data?.items.slice(0, 3).map((e: any) => (
        <Link
          className="upcoming-event"
          key={e.id}
          to={"/app/community/events/" + e.id}
        >
          <span className="event-date">
            <strong>{new Date(e.startsAt).getDate()}</strong>
            {new Date(e.startsAt).toLocaleString(undefined, { month: "short" })}
          </span>
          <div>
            <strong>{e.title}</strong>
            <small>
              {e.organization} · {clock(e.startsAt, e.timeZone)}
            </small>
          </div>
        </Link>
      ))}
      {q.data?.items.length === 0 && (
        <p className="muted padded">No events in the next 30 days.</p>
      )}
    </Panel>
  );
}
function PostCard({ post: p, path }: any) {
  const { base, notify } = useCommunity();
  const action = useAction(),
    client = useQueryClient();
  async function react(kind: string) {
    const previous = client.getQueryData([path]);
    client.setQueryData([path], (d: any) =>
      d
        ? {
            ...d,
            pages: d.pages.map((page: any) => ({
              ...page,
              items: page.items.map((item: any) =>
                item.id === p.id
                  ? {
                      ...item,
                      mine: p.mine ? "" : kind,
                      reactions: item.reactions + (p.mine ? -1 : 1),
                    }
                  : item,
              ),
            })),
          }
        : d,
    );
    try {
      await api(base + "/posts/" + p.id + "/reaction", "PUT", {
        kind: p.mine ? "" : kind,
      });
    } catch (e) {
      client.setQueryData([path], previous);
      notify((e as Error).message);
    }
  }
  return (
    <article className="post-card">
      <header>
        <span className="community-avatar">{p.author.slice(0, 2)}</span>
        <div>
          <strong>
            {p.unitId ? (
              <Link to={"/app/community/chapters/" + p.unitId}>
                {p.official ? p.organization : p.author}
              </Link>
            ) : p.official ? (
              p.organization
            ) : (
              p.author
            )}
            {p.official && <ShieldCheck size={14} />}
          </strong>
          <small>
            {p.official ? "Posted by " + p.author : p.organization} ·{" "}
            {date(p.publishedAt || p.createdAt)}
          </small>
        </div>
        <Badge>{p.category}</Badge>
      </header>
      {p.featured && (
        <div className="featured-label">
          <Sparkles size={12} />
          Featured community story
        </div>
      )}
      <div className="post-copy">
        {p.title && (
          <Link to={"/app/community/posts/" + p.id}>
            <h2>{p.title}</h2>
          </Link>
        )}
        <Rich html={p.body} />
        {p.tags?.length > 0 && (
          <div className="post-tags">
            {p.tags.map((tag: string) => (
              <span key={tag}>#{tag}</span>
            ))}
          </div>
        )}
      </div>
      <MediaGallery items={p.media} base={base} />
      <div className="post-totals">
        <span>{p.reactions} appreciations</span>
        <Link to={"/app/community/posts/" + p.id}>{p.comments} comments</Link>
      </div>
      <footer>
        <button
          className={p.mine ? "liked" : ""}
          onClick={() => react("Appreciate")}
        >
          <Heart size={17} />
          {p.mine || "Appreciate"}
        </button>
        <Link to={"/app/community/posts/" + p.id}>
          <MessageCircle size={17} />
          Comment
        </Link>
        <button
          onClick={() =>
            action.run(
              () =>
                api(
                  base + "/posts/" + p.id + "/bookmark",
                  p.bookmarked ? "DELETE" : "PUT",
                ),
              "Saved collection updated",
            )
          }
        >
          <Bookmark size={17} />
          {p.bookmarked ? "Saved" : "Save"}
        </button>
        <button
          onClick={() => {
            navigator.clipboard
              .writeText(
                location.origin +
                  (p.visibility === "Public"
                    ? "/public/community/" + base.split("/")[2] + "/posts/"
                    : "/app/community/posts/") +
                  p.id,
              )
              .then(() => notify("Link copied; access rules still apply"))
              .catch(() => notify("Copy unavailable in this browser"));
          }}
        >
          Share
        </button>
      </footer>
    </article>
  );
}
function MediaGallery({ items, base }: any) {
  return items?.length > 0 ? (
    <div className={"post-gallery count-" + Math.min(items.length, 3)}>
      {items.map((m: any) =>
        m.contentType === "image/png" ? (
          <a
            key={m.id}
            href={"/api/v1" + base + "/media/" + m.id}
            target="_blank"
            rel="noreferrer"
          >
            <img
              loading="lazy"
              src={"/api/v1" + base + "/media/" + m.id}
              alt={m.name || "Activity photograph"}
            />
          </a>
        ) : m.contentType === "video/mp4" ? (
          <video
            key={m.id}
            controls
            preload="metadata"
            src={"/api/v1" + base + "/media/" + m.id}
          />
        ) : (
          <a
            className="attachment"
            key={m.id}
            href={"/api/v1" + base + "/media/" + m.id}
          >
            {m.name || "Download attachment"}
          </a>
        ),
      )}
    </div>
  ) : null;
}
function PostComposer({ post, setup, initialUnit, onClose }: any) {
  const { base } = useCommunity();
  const action = useAction();
  const [tab, setTab] = useState("Story"),
    [title, setTitle] = useState(post?.title || ""),
    [body, setBody] = useState(
      post?.body?.replaceAll("&lt;", "<").replaceAll("&gt;", ">") || "",
    ),
    [unit, setUnit] = useState(
      post?.unitId ||
        initialUnit ||
        setup?.mine[0]?.chapterId ||
        setup?.organizations[0]?.id ||
        "",
    ),
    [category, setCategory] = useState(
      post?.categoryId ||
        setup?.categories.find((c: any) => c.kind === "Activity")?.id ||
        "",
    ),
    [official, setOfficial] = useState(post?.official || false),
    [visibility, setVisibility] = useState(post?.visibility || "Chapter"),
    [audience, setAudience] = useState<string[]>(post?.audience || []),
    [saved, setSaved] = useState<any>(post),
    [preview, setPreview] = useState(false);
  const committees = useLoad(
    "/societies/" + useCommunity().tenant + "/membership/committees",
  );
  const [committee, setCommittee] = useState(post?.committeeId || ""),
    [event, setEvent] = useState(post?.eventId || ""),
    [tags, setTags] = useState(post?.tags?.join(", ") || ""),
    [locationText, setLocation] = useState(post?.location || ""),
    [schedule, setSchedule] = useState(
      post?.scheduledAt ? local(post.scheduledAt) : "",
    ),
    [achievement, setAchievement] = useState(post?.achievement || false),
    [mentions, setMentions] = useState<string[]>(post?.mentions || []);
  const directory = useLoad(
    "/societies/" + useCommunity().tenant + "/membership/members?size=100",
  );
  const events = useLoad(base + "/events");
  async function save(submit: boolean) {
    return action.run(
      async () => {
        let revision = saved?.revision || 0;
        if (saved) {
          revision = (await api(base + "/posts/" + saved.id)).post.revision;
        }
        const r = {
          unitId: unit || null,
          categoryId: category,
          title,
          body,
          official,
          visibility,
          audience,
          committeeId: committee || null,
          eventId: event || null,
          tags: tags
            .split(",")
            .map((s: string) => s.trim())
            .filter(Boolean),
          location: locationText,
          scheduledAt: schedule ? iso(schedule) : null,
          submit,
          achievement,
          mentions,
          revision,
        };
        const result = await api(
          base + "/posts" + (saved ? "/" + saved.id : ""),
          saved ? "PUT" : "POST",
          r,
        );
        setSaved(result);
        if (submit) onClose();
      },
      submit ? "Activity submitted" : "Draft saved",
    );
  }
  return (
    <Drawer
      title={post ? "Edit activity" : "Share an activity"}
      onClose={onClose}
    >
      <div className="community-tabs">
        {["Story", "Audience", "Media", "Review"].map((t) => (
          <button
            key={t}
            className={tab === t ? "active" : ""}
            onClick={() => setTab(t)}
          >
            {t}
          </button>
        ))}
      </div>
      <div className="drawer-body community-form">
        {tab === "Story" && (
          <>
            <Field
              label="Title (optional)"
              value={title}
              maxLength={180}
              onChange={(e: any) => setTitle(e.target.value)}
            />
            <Editor value={body} onChange={setBody} />
            <Field label="Activity category">
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              >
                {setup?.categories
                  .filter((c: any) => c.kind === "Activity" && c.active)
                  .map((c: any) => (
                    <option value={c.id} key={c.id}>
                      {c.name}
                    </option>
                  ))}
              </select>
            </Field>
            <div className="form-grid">
              <Field
                label="Location"
                value={locationText}
                onChange={(e: any) => setLocation(e.target.value)}
              />
              <Field
                label="Tags, separated by commas"
                value={tags}
                onChange={(e: any) => setTags(e.target.value)}
              />
            </div>
            <Field label="Related event">
              <select value={event} onChange={(e) => setEvent(e.target.value)}>
                <option value="">No related event</option>
                {events.data?.items.map((e: any) => (
                  <option key={e.id} value={e.id}>
                    {e.title}
                  </option>
                ))}
              </select>
            </Field>
            <details>
              <summary>Mention members</summary>
              <ToggleIds
                items={directory.data?.items}
                value={mentions}
                onChange={setMentions}
              />
            </details>
          </>
        )}
        {tab === "Audience" && (
          <>
            <Field label="Publishing organization">
              <select value={unit} onChange={(e) => setUnit(e.target.value)}>
                <option value="">Society headquarters</option>
                {setup?.organizations.map((u: any) => (
                  <option key={u.id} value={u.id}>
                    {u.name}
                  </option>
                ))}
              </select>
            </Field>
            <Choice
              label="Publish as an official organization"
              checked={official}
              onChange={() => setOfficial(!official)}
            />
            <p className="muted">
              Official publishing requires an explicit permission for this
              organization. Your actual author identity is retained.
            </p>
            {official && (
              <>
                <Field label="Committee identity">
                  <select
                    value={committee}
                    onChange={(e) => setCommittee(e.target.value)}
                  >
                    <option value="">Organization identity</option>
                    {committees.data
                      ?.filter((c: any) => c.unitId === unit)
                      .map((c: any) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                  </select>
                </Field>
                <Choice
                  label="Official achievement or recognition"
                  checked={achievement}
                  onChange={() => setAchievement(!achievement)}
                />
              </>
            )}
            <Field label="Visibility">
              <select
                value={visibility}
                onChange={(e) => setVisibility(e.target.value)}
              >
                {[
                  "Chapter",
                  "Selected Chapters",
                  "Region",
                  "District",
                  "Society",
                  "International",
                  ...(setup?.policy.publicAllowed ? ["Public"] : []),
                ].map((v) => (
                  <option key={v}>{v}</option>
                ))}
              </select>
            </Field>
            {visibility === "Selected Chapters" && (
              <ToggleIds
                items={setup?.organizations}
                value={audience}
                onChange={setAudience}
              />
            )}
            <div className="community-callout">
              Audience:{" "}
              {visibility === "Chapter"
                ? setup?.organizations.find((u: any) => u.id === unit)?.name
                : visibility === "Selected Chapters"
                  ? audience.length + " selected organizations"
                  : visibility +
                    ". Your permissions are checked before saving."}
            </div>
          </>
        )}
        {tab === "Media" && (
          <>
            {saved ? (
              <MediaManager resource={saved.id} kind="Post" />
            ) : (
              <>
                <p>
                  Save your draft to attach photographs, video or documents.
                </p>
                <button onClick={() => save(false)}>Save draft</button>
              </>
            )}
          </>
        )}
        {tab === "Review" && (
          <>
            <Field
              label="Schedule publication (optional)"
              type="datetime-local"
              value={schedule}
              onChange={(e: any) => setSchedule(e.target.value)}
            />
            <p className="muted">
              Approval stages:{" "}
              {JSON.parse(setup?.policy.postStages || "[]").length ||
                "Direct publishing"}
              . Scheduled content publishes after all required approvals.
            </p>
            <button onClick={() => setPreview(!preview)}>
              {" "}
              {preview ? "Hide" : "Show"} preview
            </button>
            {preview && (
              <div className="post-preview">
                <h2>{title}</h2>
                <p style={{ whiteSpace: "pre-wrap" }}>
                  {body.replace(/<[^>]*>/g, "")}
                </p>
                <Badge>{visibility}</Badge>
              </div>
            )}
          </>
        )}
      </div>
      <div className="drawer-actions">
        <button disabled={action.busy} onClick={() => save(false)}>
          Save draft
        </button>
        <button
          className="primary"
          disabled={action.busy || !body.trim()}
          onClick={() => save(true)}
        >
          <Send size={15} />
          Submit activity
        </button>
      </div>
    </Drawer>
  );
}
function MediaManager({ resource, kind }: any) {
  const { base, notify } = useCommunity();
  const q = useLoad(
    base + (kind === "Post" ? "/posts/" : "/events/") + resource,
  );
  const [pending, setPending] = useState<File | null>(null),
    [crop, setCrop] = useState(false),
    [zoom, setZoom] = useState(1),
    [busy, setBusy] = useState(false);
  const [imageUrl, setImageUrl] = useState("");
  useEffect(() => {
    if (!pending) return;
    const url = URL.createObjectURL(pending);
    setImageUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [pending]);
  async function upload(file: File) {
    setBusy(true);
    try {
      if (file.size > 20971520)
        throw new Error("Choose media no larger than 20 MB");
      if (
        file.type.startsWith("image/") &&
        !["image/png", "image/jpeg", "image/webp"].includes(file.type)
      )
        throw new Error("Choose a PNG, JPEG or WebP photograph");
      let data: Blob = file;
      if (file.type.startsWith("image/")) {
        const image = new Image();
        image.src = URL.createObjectURL(file);
        await image.decode();
        const canvas = document.createElement("canvas");
        const side = Math.min(image.width, image.height) / zoom;
        canvas.width = crop
          ? Math.min(side, 1600)
          : Math.min(image.width, 2000);
        canvas.height = crop
          ? canvas.width
          : Math.round((image.height * canvas.width) / image.width);
        const ctx = canvas.getContext("2d")!;
        if (crop)
          ctx.drawImage(
            image,
            (image.width - side) / 2,
            (image.height - side) / 2,
            side,
            side,
            0,
            0,
            canvas.width,
            canvas.height,
          );
        else ctx.drawImage(image, 0, 0, canvas.width, canvas.height);
        URL.revokeObjectURL(image.src);
        data = await new Promise<Blob>((resolve, reject) =>
          canvas.toBlob(
            (b) =>
              b ? resolve(b) : reject(new Error("Image encoding failed")),
            "image/png",
          ),
        );
      }
      const csrf = await api("/auth/csrf");
      const res = await fetch(
        "/api/v1" + base + "/media?" + new URLSearchParams({ resource, kind }),
        {
          method: "POST",
          headers: {
            "Content-Type": data.type,
            "X-CSRF-TOKEN": csrf.token,
            "X-Filename": file.name,
          },
          body: data,
        },
      );
      if (!res.ok) throw new Error((await res.json()).title || "Upload failed");
      setPending(null);
      await q.refetch();
      notify("Media saved. Submit the draft again for approval.");
    } catch (e) {
      notify((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  const media = q.data?.media || [];
  return (
    <>
      <div
        className="media-drop"
        onDragOver={(e) => e.preventDefault()}
        onDrop={(e) => {
          e.preventDefault();
          setPending(e.dataTransfer.files[0] || null);
        }}
      >
        <ImagePlus />
        <strong>Drop a photo, video or document</strong>
        <span>
          Images become sanitized PNG · MP4 up to 20 MB · PDF up to 2 MB
        </span>
        <input
          aria-label="Choose activity media"
          type="file"
          accept="image/png,image/jpeg,image/webp,video/mp4,application/pdf"
          multiple
          onChange={async (e) => {
            const files = Array.from(e.target.files || []);
            if (files.length === 1) setPending(files[0]);
            else for (const file of files) await upload(file);
          }}
        />
      </div>
      {pending && (
        <div className="crop-preview">
          {pending.type.startsWith("image/") && (
            <>
              <img
                src={imageUrl}
                style={{
                  objectFit: crop ? "cover" : "contain",
                  aspectRatio: crop ? "1" : "auto",
                }}
              />
              <Choice
                label="Crop to a centered square"
                checked={crop}
                onChange={() => setCrop(!crop)}
              />
              {crop && (
                <Field label="Crop zoom">
                  <input
                    type="range"
                    min="1"
                    max="3"
                    step="0.1"
                    value={zoom}
                    onChange={(e) => setZoom(Number(e.target.value))}
                  />
                </Field>
              )}
            </>
          )}
          <p>{pending.name}</p>
          <button
            disabled={busy}
            className="primary"
            onClick={() => upload(pending)}
          >
            Upload file
          </button>
          <button onClick={() => setPending(null)}>Cancel</button>
        </div>
      )}
      {media.map((m: any, index: number) => (
        <div className="media-item" key={m.id}>
          <span>{m.name || m.contentType}</span>
          <button
            disabled={busy || index === 0}
            onClick={async () => {
              await api(base + "/media/" + m.id, "PUT", {
                sort: index - 1,
                cover: m.cover,
              });
              await q.refetch();
            }}
          >
            Move earlier
          </button>
          <button
            onClick={async () => {
              await api(base + "/media/" + m.id, "PUT", {
                sort: index,
                cover: true,
              });
              await q.refetch();
            }}
          >
            Set cover
          </button>
          <button
            onClick={async () => {
              await api(base + "/media/" + m.id, "DELETE");
              await q.refetch();
            }}
          >
            Remove
          </button>
        </div>
      ))}
    </>
  );
}
function PostDetail() {
  const { id } = useParams();
  const { base, notify } = useCommunity();
  const q = useLoad(base + "/posts/" + id),
    setup = useLoad(base + "/setup");
  const [edit, setEdit] = useState(false),
    [comment, setComment] = useState(""),
    [reply, setReply] = useState(""),
    [history, setHistory] = useState(false);
  const action = useAction();
  const h = useLoad(base + "/posts/" + id + "/history", history);
  if (!q.data) return <DataState query={q} />;
  const p = q.data.post;
  return (
    <>
      <Link className="back-link" to="/app/community">
        <ChevronLeft size={16} />
        Community feed
      </Link>
      <Heading title={p.title || "A chapter story"} text={date(p.createdAt)}>
        <Badge>{p.status}</Badge>
        {q.data.canEdit && (
          <div className="detail-actions">
            <button onClick={() => setEdit(true)}>Edit activity</button>
            <button
              onClick={() =>
                action
                  .run(
                    () => api(base + "/posts/" + id, "DELETE"),
                    "Activity archived",
                  )
                  .then(() => q.refetch())
              }
            >
              Archive activity
            </button>
          </div>
        )}
      </Heading>
      <div className="community-detail">
        <Rich html={p.body} />
        <MediaGallery items={q.data.media} base={base} />
        <div className="post-tags">
          {p.tags.map((t: string) => (
            <span key={t}>#{t}</span>
          ))}
        </div>
        {p.eventId && (
          <Link to={"/app/community/events/" + p.eventId}>
            View related event <ArrowRight size={15} />
          </Link>
        )}
        <div className="detail-actions">
          <button
            onClick={() => {
              const reason = prompt("Reason for reporting this post");
              if (reason)
                action.run(
                  () =>
                    api(base + "/posts/" + id + "/report", "POST", { reason }),
                  "Report sent to moderators",
                );
            }}
          >
            Report post
          </button>
          <button
            onClick={() =>
              action.run(
                () => api(base + "/blocks/" + p.authorId, "PUT"),
                "Member blocked from your feed",
              )
            }
          >
            Block author
          </button>
          {q.data.canEdit && (
            <button onClick={() => setHistory(!history)}>
              Version history
            </button>
          )}
        </div>
        {history && (
          <DataState query={h}>
            <div className="community-callout">
              {h.data?.versions.map((v: any) => (
                <p key={v.id}>
                  Revision {v.revision} · {date(v.at)}
                </p>
              ))}
              {h.data?.decisions.map((v: any) => (
                <p key={v.id}>
                  {v.decision} · {v.note}
                </p>
              ))}
            </div>
          </DataState>
        )}
        {q.data.canReview && p.status === "Pending Approval" && (
          <ReviewButtons kind="posts" resource={p} onDone={() => q.refetch()} />
        )}
        <h3>Conversation</h3>
        {q.data.comments.map((c: any) => (
          <div className={"comment " + (c.parentId ? "reply" : "")} key={c.id}>
            <span className="community-avatar">{c.name.slice(0, 2)}</span>
            <div>
              <strong>{c.name}</strong>
              <Rich html={c.body} />
              <small>
                {date(c.at)} ·{" "}
                <button
                  onClick={() => {
                    setReply(c.id);
                    setComment("@" + c.name + " ");
                  }}
                >
                  Reply
                </button>
                {q.data.canModerate && (
                  <button
                    onClick={() =>
                      action.run(() =>
                        api(base + "/comments/" + c.id + "/hide", "POST", {
                          reason: "Moderator removed inappropriate content",
                        }),
                      )
                    }
                  >
                    Hide
                  </button>
                )}
              </small>
            </div>
          </div>
        ))}
        {p.status === "Published" && (
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              if (
                await action.run(() =>
                  api(base + "/posts/" + id + "/comments", "POST", {
                    body: comment,
                    parentId: reply || null,
                  }),
                )
              ) {
                setComment("");
                setReply("");
                q.refetch();
              }
            }}
          >
            {reply && (
              <button type="button" onClick={() => setReply("")}>
                Cancel reply
              </button>
            )}
            <Field label="Join the conversation">
              <textarea
                required
                maxLength={2000}
                rows={3}
                value={comment}
                onChange={(e) => setComment(e.target.value)}
              />
            </Field>
            <button
              className="primary"
              disabled={action.busy || !comment.trim()}
            >
              Post comment
            </button>
          </form>
        )}
      </div>
      {edit && (
        <PostComposer
          post={p}
          setup={setup.data}
          onClose={() => {
            setEdit(false);
            q.refetch();
            notify("Activity refreshed");
          }}
        />
      )}
    </>
  );
}
function ReviewButtons({ kind, resource, onDone }: any) {
  const { base } = useCommunity(),
    action = useAction();
  const [note, setNote] = useState("");
  return (
    <div className="review-box">
      <Field
        label="Review note"
        value={note}
        onChange={(e: any) => setNote(e.target.value)}
      />
      <div className="detail-actions">
        {["Approve", "Changes Requested", "Reject"].map((decision) => (
          <button
            key={decision}
            className={decision === "Approve" ? "primary" : ""}
            disabled={action.busy}
            onClick={async () => {
              if (
                await action.run(
                  () =>
                    api(
                      base + "/" + kind + "/" + resource.id + "/decision",
                      "POST",
                      { decision, note, revision: resource.revision },
                    ),
                  "Review recorded",
                )
              )
                onDone();
            }}
          >
            {decision}
          </button>
        ))}
      </div>
    </div>
  );
}

function Events() {
  const { base, scope, can } = useCommunity();
  const [view, setView] = useState("Agenda"),
    [cursor, setCursor] = useState(new Date()),
    [search, setSearch] = useState(""),
    [category, setCategory] = useState(""),
    [mine, setMine] = useState(false),
    [create, setCreate] = useState(false),
    [page, setPage] = useState(1);
  const setup = useLoad(base + "/setup");
  const start =
      view === "Week"
        ? new Date(
            cursor.getFullYear(),
            cursor.getMonth(),
            cursor.getDate() - cursor.getDay(),
          )
        : new Date(cursor.getFullYear(), cursor.getMonth(), 1),
    end =
      view === "Week"
        ? new Date(start.getFullYear(), start.getMonth(), start.getDate() + 7)
        : new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1);
  const params = new URLSearchParams({
    from: start.toISOString(),
    to: end.toISOString(),
    page: String(page),
  });
  if (scope) params.set("workspace", scope);
  if (search) params.set("search", search);
  if (category) params.set("category", category);
  if (mine) params.set("mine", "true");
  const q = useLoad(base + "/events?" + params);
  return (
    <>
      <Heading
        eyebrow="EVENTS / MEET, LEARN, MAKE A DIFFERENCE"
        title="Make room for what matters."
        text="Find your next chapter meeting, service project or shared experience."
      >
        {can("events.manage") && (
          <button className="primary" onClick={() => setCreate(true)}>
            <Plus size={16} />
            Create event
          </button>
        )}
      </Heading>
      <div className="event-toolbar">
        <div className="date-navigation">
          <button
            aria-label="Previous month"
            onClick={() => {
              setCursor(
                new Date(cursor.getFullYear(), cursor.getMonth() - 1, 1),
              );
              setPage(1);
            }}
          >
            <ChevronLeft size={16} />
          </button>
          <strong>
            {cursor.toLocaleString(undefined, {
              month: "long",
              year: "numeric",
            })}
          </strong>
          <button
            aria-label="Next month"
            onClick={() => {
              setCursor(
                new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1),
              );
              setPage(1);
            }}
          >
            <ChevronRight size={16} />
          </button>
        </div>
        <div className="community-tabs">
          {["Agenda", "Month", "Week", "List"].map((v) => (
            <button
              key={v}
              className={view === v ? "active" : ""}
              onClick={() => setView(v)}
            >
              {v}
            </button>
          ))}
        </div>
        <Choice
          label="My events"
          checked={mine}
          onChange={() => setMine(!mine)}
        />
      </div>
      <div className="feed-filters">
        <input
          aria-label="Search events"
          placeholder="Search title or venue…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          aria-label="Event type"
          value={category}
          onChange={(e) => setCategory(e.target.value)}
        >
          <option value="">All event types</option>
          {setup.data?.categories
            .filter((c: any) => c.kind === "Event" && c.active)
            .map((c: any) => (
              <option value={c.id} key={c.id}>
                {c.name}
              </option>
            ))}
        </select>
      </div>
      <DataState query={q}>
        {view === "Month" ? (
          <div className="month-calendar">
            {["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].map((day) => (
              <div className="calendar-weekday" key={day}>
                {day}
              </div>
            ))}
            {Array.from(
              {
                length: new Date(
                  cursor.getFullYear(),
                  cursor.getMonth(),
                  1,
                ).getDay(),
              },
              (_, i) => (
                <div
                  className="calendar-day calendar-blank"
                  key={"blank" + i}
                />
              ),
            )}
            {Array.from(
              {
                length: new Date(
                  cursor.getFullYear(),
                  cursor.getMonth() + 1,
                  0,
                ).getDate(),
              },
              (_, index) => (
                <div className="calendar-day" key={index}>
                  <strong>{index + 1}</strong>
                  {q.data?.items
                    .filter(
                      (e: any) => new Date(e.startsAt).getDate() === index + 1,
                    )
                    .map((e: any) => (
                      <Link key={e.id} to={"/app/community/events/" + e.id}>
                        {clock(e.startsAt, e.timeZone)} {e.title}
                      </Link>
                    ))}
                </div>
              ),
            )}
          </div>
        ) : (
          <div className={view === "List" ? "event-grid" : "event-agenda"}>
            {q.data?.items
              .filter(
                (e: any) =>
                  view !== "Week" ||
                  (new Date(e.startsAt) < end && new Date(e.endsAt) >= start),
              )
              .map((e: any) => (
                <EventCard event={e} key={e.id} />
              ))}
            {q.data?.items.length === 0 && (
              <Empty
                title="Space for your next gathering"
                text="No authorized events in this date range."
              />
            )}
          </div>
        )}
        {view === "Week" && (
          <div className="detail-actions">
            <button
              onClick={() =>
                setCursor(new Date(cursor.getTime() - 7 * 86400000))
              }
            >
              Previous week
            </button>
            <span>Week containing {date(cursor.toISOString())}</span>
            <button
              onClick={() =>
                setCursor(new Date(cursor.getTime() + 7 * 86400000))
              }
            >
              Next week
            </button>
          </div>
        )}
        <div className="pagination">
          <span>{q.data?.total || 0} events</span>
          <button disabled={page === 1} onClick={() => setPage(page - 1)}>
            Previous
          </button>
          <button
            disabled={page * 50 >= (q.data?.total || 0)}
            onClick={() => setPage(page + 1)}
          >
            Next
          </button>
        </div>
      </DataState>
      {create && (
        <EventWizard
          setup={setup.data}
          onClose={() => {
            setCreate(false);
            q.refetch();
          }}
        />
      )}
    </>
  );
}
function EventCard({ event: e }: any) {
  return (
    <Link className="event-card" to={"/app/community/events/" + e.id}>
      <div className="event-card-art">
        <CalendarDays size={36} />
        <span>{e.mode}</span>
      </div>
      <div className="event-card-body">
        <div className="eyebrow">
          {date(e.startsAt, e.timeZone)} · {clock(e.startsAt, e.timeZone)}
        </div>
        <h2>{e.title}</h2>
        <p>{e.organization}</p>
        <small>
          <MapPin size={13} />
          {e.venue || e.mode}
        </small>
        <div className="event-card-foot">
          <span>
            {e.confirmed} / {e.capacity} seats confirmed
          </span>
          <ArrowRight size={18} />
        </div>
      </div>
    </Link>
  );
}
function EventWizard({ setup, event, onClose }: any) {
  const { base, tenant } = useCommunity(),
    action = useAction();
  const [step, setStep] = useState(0),
    [saved, setSaved] = useState<any>(event);
  const [form, setForm] = useState<any>({
    ...event,
    unitId: event?.unitId || setup?.organizations[0]?.id || "",
    categoryId:
      event?.categoryId ||
      setup?.categories.find((c: any) => c.kind === "Event")?.id ||
      "",
    title: event?.title || "",
    body: event?.body || "",
    startsAt: local(
      event?.startsAt || new Date(Date.now() + 86400000).toISOString(),
    ),
    endsAt: local(
      event?.endsAt || new Date(Date.now() + 90000000).toISOString(),
    ),
    registrationOpens: local(
      event?.registrationOpens || new Date().toISOString(),
    ),
    registrationCloses: local(
      event?.registrationCloses ||
        new Date(Date.now() + 86400000).toISOString(),
    ),
    venue: event?.venue || "",
    mode: event?.mode || "In Person",
    timeZone:
      event?.timeZone || Intl.DateTimeFormat().resolvedOptions().timeZone,
    onlineUrl: event?.onlineUrl || "",
    capacity: event?.capacity || 100,
    allowGuests: event?.allowGuests || false,
    maxCompanions: event?.maxCompanions || 0,
    registrationApproval: event?.registrationApproval || false,
    invitationOnly: event?.invitationOnly || false,
    visibility: event?.visibility || "Chapter",
    audience: event?.audience || [],
    memberIds: event?.memberIds || [],
    categoryIds: event?.categoryIds || [],
    committeeIds: event?.committeeIds || [],
    coHosts: event?.coHosts || [],
    leadersOnly: event?.leadersOnly || false,
    questions: event?.questions || "[]",
    scheduledAt: event?.scheduledAt ? local(event.scheduledAt) : "",
  });
  const update = (key: string, value: any) =>
    setForm({ ...form, [key]: value });
  const members = useLoad(
      "/societies/" + tenant + "/membership/members?size=100",
    ),
    cats = useLoad("/societies/" + tenant + "/membership/categories"),
    committees = useLoad("/societies/" + tenant + "/membership/committees");
  const tabs = [
    "Details",
    "Date & venue",
    "Audience",
    "Registration",
    "Invitations",
    "Review",
  ];
  async function save(submit: boolean) {
    await action.run(
      async () => {
        const latest = saved ? await api(base + "/events/" + saved.id) : null;
        const r = {
          ...form,
          unitId: form.unitId || null,
          startsAt: iso(form.startsAt),
          endsAt: iso(form.endsAt),
          registrationOpens: iso(form.registrationOpens),
          registrationCloses: iso(form.registrationCloses),
          scheduledAt: form.scheduledAt ? iso(form.scheduledAt) : null,
          submit,
          revision: latest?.eventData.revision || 0,
        };
        const e = await api(
          base + "/events" + (saved ? "/" + saved.id : ""),
          saved ? "PUT" : "POST",
          r,
        );
        setSaved(e);
        if (submit) onClose();
      },
      submit ? "Event submitted" : "Event draft saved",
    );
  }
  return (
    <Drawer title={event ? "Edit event" : "Plan a gathering"} onClose={onClose}>
      <div className="wizard-steps">
        {tabs.map((t, i) => (
          <button
            key={t}
            className={step === i ? "active" : ""}
            onClick={() => setStep(i)}
          >
            <span>{i + 1}</span>
            {t}
          </button>
        ))}
      </div>
      <div className="drawer-body community-form">
        {step === 0 && (
          <>
            <Field
              label="Event title"
              value={form.title}
              maxLength={180}
              onChange={(e: any) => update("title", e.target.value)}
            />
            <Field label="Event type">
              <select
                value={form.categoryId}
                onChange={(e) => update("categoryId", e.target.value)}
              >
                {setup?.categories
                  .filter((c: any) => c.kind === "Event" && c.active)
                  .map((c: any) => (
                    <option value={c.id} key={c.id}>
                      {c.name}
                    </option>
                  ))}
              </select>
            </Field>
            <Editor
              value={form.body}
              onChange={(v: string) => update("body", v)}
            />
            {saved ? (
              <MediaManager resource={saved.id} kind="Event" />
            ) : (
              <p className="muted">
                Save the draft to add an event banner or supporting media.
              </p>
            )}
          </>
        )}
        {step === 1 && (
          <>
            <div className="form-grid">
              {[
                ["startsAt", "Starts"],
                ["endsAt", "Ends"],
              ].map(([key, label]) => (
                <Field
                  key={key}
                  label={label}
                  type="datetime-local"
                  value={form[key]}
                  onChange={(e: any) => update(key, e.target.value)}
                />
              ))}
            </div>
            <Field
              label="Time zone (IANA name)"
              value={form.timeZone}
              onChange={(e: any) => update("timeZone", e.target.value)}
            />
            <Field label="Format">
              <select
                value={form.mode}
                onChange={(e) => update("mode", e.target.value)}
              >
                {["In Person", "Online", "Hybrid"].map((v) => (
                  <option key={v}>{v}</option>
                ))}
              </select>
            </Field>
            <Field
              label="Venue"
              value={form.venue}
              onChange={(e: any) => update("venue", e.target.value)}
            />
            {form.mode !== "In Person" && (
              <Field
                label="Online venue HTTPS link"
                type="url"
                value={form.onlineUrl}
                onChange={(e: any) => update("onlineUrl", e.target.value)}
              />
            )}
          </>
        )}
        {step === 2 && (
          <>
            <Field label="Organizing organization">
              <select
                value={form.unitId}
                onChange={(e) => update("unitId", e.target.value)}
              >
                <option value="">Society headquarters</option>
                {setup?.organizations.map((u: any) => (
                  <option key={u.id} value={u.id}>
                    {u.name}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Visibility">
              <select
                value={form.visibility}
                onChange={(e) => update("visibility", e.target.value)}
              >
                {[
                  "Chapter",
                  "Selected Chapters",
                  "Region",
                  "District",
                  "Society",
                  "International",
                  ...(setup?.policy.publicAllowed ? ["Public"] : []),
                ].map((v) => (
                  <option key={v}>{v}</option>
                ))}
              </select>
            </Field>
            {form.visibility === "Selected Chapters" && (
              <ToggleIds
                items={setup?.organizations}
                value={form.audience}
                onChange={(v: any) => update("audience", v)}
              />
            )}
            <details>
              <summary>Co-hosts</summary>
              <ToggleIds
                items={setup?.organizations}
                value={form.coHosts}
                onChange={(v: any) => update("coHosts", v)}
              />
            </details>
            <details>
              <summary>Specific members (optional)</summary>
              <ToggleIds
                items={members.data?.items}
                value={form.memberIds}
                onChange={(v: any) => update("memberIds", v)}
              />
            </details>
            <details>
              <summary>Member categories (optional)</summary>
              <ToggleIds
                items={cats.data}
                value={form.categoryIds}
                onChange={(v: any) => update("categoryIds", v)}
              />
            </details>
            <details>
              <summary>Committees (optional)</summary>
              <ToggleIds
                items={committees.data}
                value={form.committeeIds}
                onChange={(v: any) => update("committeeIds", v)}
              />
            </details>
            <Choice
              label="Current leadership officers only"
              checked={form.leadersOnly}
              onChange={() => update("leadersOnly", !form.leadersOnly)}
            />
            <p className="muted">
              Rules intersect: a member must match the selected organization,
              category, committee and leadership restrictions. Duplicate
              affiliations resolve to one account.
            </p>
          </>
        )}
        {step === 3 && (
          <>
            <div className="form-grid">
              {[
                ["registrationOpens", "Registration opens"],
                ["registrationCloses", "Registration closes"],
              ].map(([key, label]) => (
                <Field
                  key={key}
                  label={label}
                  type="datetime-local"
                  value={form[key]}
                  onChange={(e: any) => update(key, e.target.value)}
                />
              ))}
            </div>
            <div className="form-grid">
              <Field
                label="Capacity (people)"
                type="number"
                min="1"
                max="100000"
                value={form.capacity}
                onChange={(e: any) =>
                  update("capacity", Number(e.target.value))
                }
              />
              <Field
                label="Maximum companions"
                type="number"
                min="0"
                max="10"
                value={form.maxCompanions}
                onChange={(e: any) =>
                  update("maxCompanions", Number(e.target.value))
                }
              />
            </div>
            <Choice
              label="Require registration approval"
              checked={form.registrationApproval}
              onChange={() =>
                update("registrationApproval", !form.registrationApproval)
              }
            />
            <Choice
              label="Allow invited external guests"
              checked={form.allowGuests}
              onChange={() => update("allowGuests", !form.allowGuests)}
            />
            <QuestionBuilder
              value={form.questions}
              onChange={(v: string) => update("questions", v)}
            />
            <p className="community-callout">
              RSVP records intent. Registration reserves capacity. Attendance is
              recorded only at check-in.
            </p>
          </>
        )}
        {step === 4 && (
          <>
            <Choice
              label="Invitation required to register"
              checked={form.invitationOnly}
              onChange={() => update("invitationOnly", !form.invitationOnly)}
            />
            <p>
              After publication, open Invitations on the event page to preview
              eligible recipients and send in-app or queued email invitations.
              Messaging provider delivery is reported separately.
            </p>
          </>
        )}
        {step === 5 && (
          <>
            <h2>{form.title || "Untitled event"}</h2>
            <p>
              {date(iso(form.startsAt))} · {clock(iso(form.startsAt))} ·{" "}
              {form.timeZone}
            </p>
            <p>
              {form.venue || form.mode} · {form.capacity} places ·{" "}
              {form.visibility}
            </p>
            <Field
              label="Schedule publication"
              type="datetime-local"
              value={form.scheduledAt}
              onChange={(e: any) => update("scheduledAt", e.target.value)}
            />
            <p className="muted">
              {JSON.parse(setup?.policy.eventStages || "[]").length} required
              approval stages. Editing approved details restarts review.
            </p>
          </>
        )}
      </div>
      <div className="drawer-actions">
        <button disabled={action.busy} onClick={() => save(false)}>
          Save draft
        </button>
        {step < 5 ? (
          <button className="primary" onClick={() => setStep(step + 1)}>
            Continue <ChevronRight size={15} />
          </button>
        ) : (
          <button
            className="primary"
            disabled={action.busy || !form.title || !form.body}
            onClick={() => save(true)}
          >
            Submit event
          </button>
        )}
      </div>
    </Drawer>
  );
}
function QuestionBuilder({ value, onChange }: any) {
  const questions = JSON.parse(value || "[]");
  return (
    <div>
      <h3>Registration questions</h3>
      {questions.map((q: any, i: number) => (
        <div className="question-row" key={i}>
          <input
            aria-label={"Question " + (i + 1)}
            value={q.label}
            onChange={(e) =>
              onChange(
                JSON.stringify(
                  questions.map((v: any, n: number) =>
                    n === i ? { ...v, label: e.target.value } : v,
                  ),
                ),
              )
            }
          />
          <Choice
            label="Required"
            checked={q.required}
            onChange={() =>
              onChange(
                JSON.stringify(
                  questions.map((v: any, n: number) =>
                    n === i ? { ...v, required: !v.required } : v,
                  ),
                ),
              )
            }
          />
          <button
            onClick={() =>
              onChange(
                JSON.stringify(
                  questions.filter((_: any, n: number) => n !== i),
                ),
              )
            }
          >
            Remove
          </button>
        </div>
      ))}
      <button
        disabled={questions.length >= 20}
        onClick={() =>
          onChange(
            JSON.stringify([
              ...questions,
              { key: "question_" + Date.now(), label: "", required: false },
            ]),
          )
        }
      >
        Add question
      </button>
    </div>
  );
}

function EventDetail() {
  const { id } = useParams();
  const { base, notify } = useCommunity();
  const q = useLoad(base + "/events/" + id),
    setup = useLoad(base + "/setup");
  const [tab, setTab] = useState("Overview"),
    [edit, setEdit] = useState(false),
    [pass, setPass] = useState<any>(null),
    [answers, setAnswers] = useState<Record<string, string>>({}),
    [companions, setCompanions] = useState(0);
  const action = useAction();
  const [params, setParams] = useSearchParams();
  const invite = params.get("invitation");
  useEffect(() => {
    if (invite)
      api(base + "/invitations/accept", "POST", { kind: invite })
        .then(() => {
          notify("Invitation accepted");
          setParams({});
          q.refetch();
        })
        .catch((e) => notify(e.message));
  }, [invite, base]);
  if (!q.data) return <DataState query={q} />;
  const e = q.data.eventData,
    reg = q.data.registration;
  return (
    <>
      <Link className="back-link" to="/app/community/events">
        <ChevronLeft size={16} />
        All events
      </Link>
      <div className="event-hero">
        <div>
          <div className="eyebrow">A GATHERING WITH PURPOSE</div>
          <h1>{e.title}</h1>
          <p>
            <CalendarDays size={17} />
            {date(e.startsAt, e.timeZone)} · {clock(e.startsAt, e.timeZone)}–
            {clock(e.endsAt, e.timeZone)} · {e.timeZone}
          </p>
          <p>
            <MapPin size={17} />
            {e.venue || e.mode}
          </p>
          <Badge>{e.status}</Badge>
        </div>
        <div className="event-hero-symbol">
          <Users size={65} />
          <span>
            {q.data.confirmedSeats} / {e.capacity} places confirmed
          </span>
        </div>
      </div>
      <div className="event-toolbar">
        <div className="community-tabs">
          {[
            "Overview",
            ...(q.data.canInvite ? ["Invitations"] : []),
            ...(q.data.canManage || q.data.canCheckIn
              ? ["Participants", "Check-in"]
              : []),
            ...(q.data.canManage ? ["Attendance", "Analytics"] : []),
          ].map((t) => (
            <button
              key={t}
              className={tab === t ? "active" : ""}
              onClick={() => setTab(t)}
            >
              {t}
            </button>
          ))}
        </div>
        {q.data.canManage && (
          <div className="detail-actions">
            <button onClick={() => setEdit(true)}>Edit</button>
            <button
              onClick={() =>
                action.run(async () => {
                  const copy = await api(
                    base + "/events/" + id + "/duplicate",
                    "POST",
                  );
                  location.assign("/app/community/events/" + copy.id);
                }, "Draft duplicated")
              }
            >
              Duplicate
            </button>
            {e.status !== "Cancelled" && (
              <button
                onClick={() => {
                  const reason = prompt("Reason for cancellation");
                  if (reason)
                    action
                      .run(
                        () =>
                          api(base + "/events/" + id + "/cancel", "POST", {
                            reason,
                          }),
                        "Event cancelled",
                      )
                      .then(() => q.refetch());
                }}
              >
                Cancel event
              </button>
            )}
          </div>
        )}
      </div>
      {tab === "Overview" && (
        <div className="event-detail-layout">
          <Panel>
            <div className="padded">
              <Rich html={e.body} />
              <MediaGallery items={q.data.media} base={base} />
              {e.onlineUrl && (
                <a href={e.onlineUrl} target="_blank" rel="noreferrer">
                  Open online venue <Video size={16} />
                </a>
              )}
              <p className="muted">
                Registration: {date(e.registrationOpens)} to{" "}
                {date(e.registrationCloses)}.{" "}
                {e.invitationOnly ? "Invitation required." : ""}{" "}
                {e.registrationApproval ? "Organizer approval required." : ""}
              </p>
              {q.data.canReview && e.status === "Pending Approval" && (
                <ReviewButtons
                  kind="events"
                  resource={e}
                  onDone={() => q.refetch()}
                />
              )}
            </div>
          </Panel>
          <aside>
            <Panel title="Your participation">
              <div className="padded">
                <h3>Will you join us?</h3>
                {!q.data.canParticipate && (
                  <p className="muted">
                    Your current membership or invitation is not eligible for
                    this event.
                  </p>
                )}
                <div className="rsvp-options">
                  {["Going", "Maybe", "Not Going"].map((response) => (
                    <button
                      key={response}
                      className={
                        q.data.rsvp?.response === response ? "active" : ""
                      }
                      disabled={
                        action.busy ||
                        e.status !== "Published" ||
                        !q.data.canParticipate
                      }
                      onClick={() =>
                        action
                          .run(
                            () =>
                              api(base + "/events/" + id + "/rsvp", "PUT", {
                                kind: response,
                              }),
                            "Response saved",
                          )
                          .then(() => q.refetch())
                      }
                    >
                      {response}
                    </button>
                  ))}
                </div>
                <p className="muted">
                  RSVP shares your intention. Register below to request a place.
                </p>
                {reg && (
                  <>
                    <Badge>{reg.status}</Badge>
                    <p>Reference: {reg.id.slice(0, 8).toUpperCase()}</p>
                    {reg.status === "Confirmed" &&
                      q.data.canParticipate &&
                      e.status === "Published" && (
                        <button
                          className="primary"
                          onClick={() =>
                            action.run(
                              async () =>
                                setPass(
                                  await api(
                                    base +
                                      "/events/" +
                                      id +
                                      "/passes?registration=" +
                                      reg.id,
                                    "POST",
                                  ),
                                ),
                              "Pass issued; previous passes revoked",
                            )
                          }
                        >
                          Issue / reissue digital pass
                        </button>
                      )}
                    {!["Cancelled", "Rejected"].includes(reg.status) && (
                      <button
                        onClick={() =>
                          action
                            .run(
                              () =>
                                api(
                                  base +
                                    "/events/" +
                                    id +
                                    "/registrations/" +
                                    reg.id +
                                    "/decision",
                                  "POST",
                                  {
                                    decision: "Cancel",
                                    note: "Participant cancellation",
                                    revision: reg.revision,
                                  },
                                ),
                              "Registration cancelled",
                            )
                            .then(() => q.refetch())
                        }
                      >
                        Cancel registration
                      </button>
                    )}
                  </>
                )}
                {(!reg || ["Cancelled", "Rejected"].includes(reg.status)) &&
                  e.status === "Published" &&
                  q.data.canParticipate && (
                    <form
                      onSubmit={async (ev) => {
                        ev.preventDefault();
                        await action.run(
                          () =>
                            api(
                              base + "/events/" + id + "/registrations",
                              "POST",
                              {
                                companions,
                                answers: JSON.stringify(answers),
                                invitationToken: invite || null,
                              },
                            ),
                          "Registration recorded",
                        );
                        q.refetch();
                      }}
                    >
                      {e.maxCompanions > 0 && (
                        <Field
                          label="Companions"
                          type="number"
                          min="0"
                          max={e.maxCompanions}
                          value={companions}
                          onChange={(ev: any) =>
                            setCompanions(Number(ev.target.value))
                          }
                        />
                      )}
                      {JSON.parse(e.questions).map((question: any) => (
                        <Field
                          key={question.key}
                          label={question.label}
                          required={question.required}
                          value={answers[question.key] || ""}
                          onChange={(ev: any) =>
                            setAnswers({
                              ...answers,
                              [question.key]: ev.target.value,
                            })
                          }
                        />
                      ))}
                      <button className="primary" disabled={action.busy}>
                        Register for this event
                      </button>
                    </form>
                  )}
              </div>
            </Panel>
          </aside>
        </div>
      )}
      {tab === "Invitations" && <EventInvitations event={e} />}
      {tab === "Participants" && (
        <Participants event={e} canManage={q.data.canManage} />
      )}
      {tab === "Check-in" && <CheckIn event={e} />}
      {tab === "Attendance" && <Attendance event={e} />}
      {tab === "Analytics" && <EventAnalytics event={e} />}
      {edit && (
        <EventWizard
          event={e}
          setup={setup.data}
          onClose={() => {
            setEdit(false);
            q.refetch();
          }}
        />
      )}
      {pass && <Pass pass={pass} onClose={() => setPass(null)} />}
    </>
  );
}
function EventInvitations({ event: e }: any) {
  const { base } = useCommunity();
  const q = useLoad(base + "/events/" + e.id + "/audience");
  const [ids, setIds] = useState<string[]>([]),
    [guests, setGuests] = useState(""),
    [channel, setChannel] = useState("In App"),
    [result, setResult] = useState<any>(null);
  const action = useAction();
  return (
    <Panel>
      <div className="padded">
        <h2>Invite the right people.</h2>
        <DataState query={q}>
          <div className="community-metrics">
            {[
              ["Eligible people", q.data?.uniquePeople],
              ["Excluded affiliations", q.data?.excluded],
              ["Communication opt-outs", q.data?.communicationExcluded],
              ["Duplicate affiliations", q.data?.duplicateAffiliations],
              ["Invitations created", q.data?.invitations],
            ].map(([l, v]) => (
              <div key={l}>
                <strong>{v}</strong>
                <span>{l}</span>
              </div>
            ))}
          </div>
          <p className="muted">
            Select recipients from the first 100 eligible affiliations.
            Communication opt-outs are checked when invitations are created.
          </p>
          <button
            onClick={() =>
              setIds(
                q.data.members
                  .filter((m: any) => m.claimed)
                  .map((m: any) => m.id),
              )
            }
          >
            Select eligible claimed members
          </button>
          <ToggleIds items={q.data?.members} value={ids} onChange={setIds} />
        </DataState>
        {e.allowGuests && (
          <Field label="External guest emails (one per line)">
            <textarea
              value={guests}
              onChange={(ev) => setGuests(ev.target.value)}
              rows={3}
            />
          </Field>
        )}
        <Field label="Delivery">
          <select
            value={channel}
            onChange={(ev) => setChannel(ev.target.value)}
          >
            <option>In App</option>
            <option>Email</option>
          </select>
        </Field>
        <p className="muted">
          Email uses the existing outbox. Queued means accepted for processing;
          it does not mean provider delivery.
        </p>
        <button
          disabled={action.busy}
          className="primary"
          onClick={() =>
            action.run(async () => {
              setResult(
                await api(base + "/events/" + e.id + "/invitations", "POST", {
                  membershipIds: ids,
                  guestEmails: guests
                    .split(/\n/)
                    .map((v) => v.trim())
                    .filter(Boolean),
                  channel,
                  days: 7,
                }),
              );
              q.refetch();
            }, "Invitations created")
          }
        >
          Create invitations
        </button>
        {result && (
          <div className="community-callout">
            <p>
              {result.delivery} · {result.skipped} skipped
            </p>
            {result.items.map((i: any) => (
              <div className="invitation-row" key={i.id}>
                <Badge>{i.status}</Badge>
                <button
                  onClick={() =>
                    navigator.clipboard.writeText(location.origin + i.url)
                  }
                >
                  Copy secure recipient link
                </button>
                <a
                  href={
                    "https://wa.me/?text=" +
                    encodeURIComponent(
                      "You are invited: " + location.origin + i.url,
                    )
                  }
                  target="_blank"
                  rel="noreferrer"
                >
                  Share via WhatsApp
                </a>
              </div>
            ))}
          </div>
        )}
      </div>
    </Panel>
  );
}
function Participants({ event: e, canManage }: any) {
  const { base } = useCommunity();
  const [search, setSearch] = useState(""),
    [page, setPage] = useState(1);
  const q = useLoad(
      base +
        "/events/" +
        e.id +
        "/registrations?" +
        new URLSearchParams({ search, page: String(page) }),
    ),
    action = useAction();
  return (
    <Panel>
      <div className="padded">
        <h2>Registration desk</h2>
        <Field
          label="Search participant"
          value={search}
          onChange={(ev: any) => {
            setSearch(ev.target.value);
            setPage(1);
          }}
        />
        <DataState query={q}>
          <div className="participant-list">
            {q.data?.items.map((r: any) => (
              <div key={r.id}>
                <span className="community-avatar">{r.name.slice(0, 2)}</span>
                <div>
                  <strong>{r.name}</strong>
                  <small>
                    {r.id.slice(0, 8)} · {r.companions} companions
                  </small>
                </div>
                <Badge>{r.status}</Badge>
                {r.checkedIn && <Badge>Checked in</Badge>}
                {canManage &&
                  r.status === "Awaiting Approval" &&
                  ["Approve", "Reject"].map((decision) => (
                    <button
                      key={decision}
                      onClick={() =>
                        action
                          .run(() =>
                            api(
                              base +
                                "/events/" +
                                e.id +
                                "/registrations/" +
                                r.id +
                                "/decision",
                              "POST",
                              {
                                decision,
                                note: "Organizer review",
                                revision: r.revision,
                              },
                            ),
                          )
                          .then(() => q.refetch())
                      }
                    >
                      {decision}
                    </button>
                  ))}
                {r.status === "Confirmed" && (
                  <button
                    onClick={() =>
                      action
                        .run(async () => {
                          const response = await api(
                            base + "/events/" + e.id + "/checkins",
                            "POST",
                            {
                              registrationId: r.id,
                              token: null,
                              entryPoint: "Manual registration desk",
                            },
                          );
                          if (
                            !["Valid", "Already Checked In"].includes(
                              response.state,
                            )
                          )
                            throw new Error(response.state);
                        }, "Check-in verified")
                        .then(() => q.refetch())
                    }
                  >
                    Check in
                  </button>
                )}
              </div>
            ))}
          </div>
          <div className="pagination">
            <span>{q.data?.total} registrations</span>
            <button disabled={page === 1} onClick={() => setPage(page - 1)}>
              Previous
            </button>
            <button
              disabled={page * 50 >= (q.data?.total || 0)}
              onClick={() => setPage(page + 1)}
            >
              Next
            </button>
          </div>
        </DataState>
      </div>
    </Panel>
  );
}
function CheckIn({ event: e }: any) {
  const { base, notify } = useCommunity();
  const [peopleCount, setPeopleCount] = useState(1);
  const [token, setToken] = useState(""),
    [entry, setEntry] = useState("Main entrance"),
    [result, setResult] = useState<any>(null),
    [scanning, setScanning] = useState(false);
  const video = useRef<HTMLVideoElement>(null),
    controls = useRef<any>(null),
    lock = useRef(false);
  const action = useAction();
  useEffect(
    () => () => {
      controls.current?.stop();
    },
    [],
  );
  async function check(value: string) {
    if (lock.current) return;
    lock.current = true;
    try {
      const res = await api(base + "/events/" + e.id + "/checkins", "POST", {
        token: value.trim(),
        registrationId: null,
        entryPoint: entry,
        peopleCount,
      });
      setResult(res);
      if (scanning) controls.current?.stop();
      setScanning(false);
    } catch (err) {
      notify((err as Error).message);
    } finally {
      lock.current = false;
    }
  }
  async function camera() {
    try {
      setScanning(true);
      const { BrowserQRCodeReader } = await import("@zxing/browser");
      const reader = new BrowserQRCodeReader();
      controls.current = await reader.decodeFromVideoDevice(
        undefined,
        video.current!,
        (result) => {
          if (result) check(result.getText());
        },
      );
    } catch (err) {
      setScanning(false);
      notify("Camera unavailable: " + (err as Error).message);
    }
  }
  return (
    <div className="checkin-layout">
      <Panel>
        <div className="padded">
          <div className="eyebrow">AUTHORIZED ENTRY DESK</div>
          <h2>Welcome them in.</h2>
          <p>
            Scan an opaque QR reference or use participant search. Scanning
            requires camera permission and an online connection.
          </p>
          <video
            ref={video}
            className={scanning ? "scanner" : "scanner hidden"}
            muted
            playsInline
          />
          <div className="detail-actions">
            {scanning ? (
              <button
                onClick={() => {
                  controls.current?.stop();
                  setScanning(false);
                }}
              >
                Stop camera
              </button>
            ) : (
              <button className="primary" onClick={camera}>
                Start camera scanner
              </button>
            )}
          </div>
          <Field
            label="Entry point"
            value={entry}
            onChange={(ev: any) => setEntry(ev.target.value)}
          />
          <Field
            label="People physically present (including companions)"
            type="number"
            min="1"
            max="11"
            value={peopleCount}
            onChange={(ev: any) => setPeopleCount(Number(ev.target.value))}
          />
          <form
            onSubmit={(ev) => {
              ev.preventDefault();
              check(token);
            }}
          >
            <Field
              label="Pass verification reference"
              value={token}
              onChange={(ev: any) => setToken(ev.target.value)}
              autoComplete="off"
            />
            <button disabled={action.busy || !token.trim()}>
              Verify and check in
            </button>
          </form>
          {result && (
            <div
              role="status"
              className={
                "checkin-result " +
                (result.state === "Valid" ? "valid" : "invalid")
              }
            >
              <Check size={28} />
              <h2>{result.state}</h2>
              {result.name && (
                <p>
                  {result.name} · {clock(result.at)}
                </p>
              )}
            </div>
          )}
        </div>
      </Panel>
      <Participants event={e} canManage={false} />
    </div>
  );
}
function Pass({ pass: p, onClose }: any) {
  const [qr, setQr] = useState("");
  useEffect(() => {
    import("qrcode")
      .then((q) =>
        q.toDataURL(p.token, {
          width: 320,
          margin: 3,
          errorCorrectionLevel: "M",
        }),
      )
      .then(setQr);
  }, [p.token]);
  async function pdf() {
    const { jsPDF } = await import("jspdf");
    const canvas = document.createElement("canvas");
    canvas.width = 1000;
    canvas.height = 1400;
    const c = canvas.getContext("2d")!;
    c.fillStyle = "#f6f8f3";
    c.fillRect(0, 0, 1000, 1400);
    c.fillStyle = "#146d63";
    c.fillRect(0, 0, 1000, 190);
    c.fillStyle = "#fff";
    c.font = "bold 40px Arial";
    c.fillText("ASSEMBLIA / EVENT PASS", 60, 110);
    c.fillStyle = "#183f39";
    c.font = "bold 38px Arial";
    const title = p.title.length > 38 ? p.title.slice(0, 38) + "…" : p.title;
    c.fillText(title, 60, 285);
    c.font = "30px Arial";
    c.fillText(p.participant.slice(0, 45), 60, 365);
    c.font = "25px Arial";
    c.fillText(
      date(p.startsAt, p.timeZone) + " · " + clock(p.startsAt, p.timeZone),
      60,
      430,
    );
    c.fillText(p.venue.slice(0, 60), 60, 480);
    c.fillText(p.category + " · " + p.companions + " companions", 60, 530);
    const image = new Image();
    image.src = qr;
    await image.decode();
    c.drawImage(image, 265, 610, 470, 470);
    c.font = "20px monospace";
    c.fillText(p.reference, 60, 1170);
    c.font = "22px Arial";
    c.fillText(
      "A valid pass and confirmed eligibility are required at entry.",
      60,
      1250,
    );
    const doc = new jsPDF({
      unit: "px",
      format: [1000, 1400],
      hotfixes: ["px_scaling"],
    });
    doc.addImage(canvas.toDataURL("image/png"), "PNG", 0, 0, 1000, 1400);
    doc.save("event-pass-" + p.reference.slice(0, 8) + ".pdf");
  }
  return (
    <Drawer title="Your digital event pass" onClose={onClose}>
      <div className="event-pass">
        <div className="eyebrow">ASSEMBLIA / ADMIT BY VERIFIED PASS</div>
        <h2>{p.title}</h2>
        <p>
          {date(p.startsAt, p.timeZone)} · {clock(p.startsAt, p.timeZone)}
        </p>
        <p>{p.venue}</p>
        <div className="pass-person">
          <strong>{p.participant}</strong>
          <span>
            {p.category} · {p.companions} companions
          </span>
        </div>
        {qr && <img src={qr} alt="Opaque event verification QR code" />}
        <code>{p.reference.slice(0, 8).toUpperCase()}</code>
        <p className="muted">
          The QR contains an opaque reference. Reissuing a pass revokes previous
          passes.
        </p>
        <button disabled={!qr} className="primary" onClick={pdf}>
          Download printable PDF
        </button>
      </div>
    </Drawer>
  );
}
function Attendance({ event: e }: any) {
  const { base } = useCommunity(),
    action = useAction();
  const q = useLoad(base + "/events/" + e.id + "/attendance");
  return (
    <Panel title="Actual attendance">
      <div className="padded">
        <DataState query={q}>
          {q.data?.map((at: any) => (
            <div className="attendance-row" key={at.id}>
              <div>
                <strong>{at.name}</strong>
                <small>
                  {date(at.at)} {clock(at.at)} · {at.entryPoint} ·{" "}
                  {at.companions} companions
                </small>
              </div>
              <Badge>{at.voided ? "Voided" : "Checked in"}</Badge>
              {at.voided ? (
                <p>{at.correction}</p>
              ) : (
                <button
                  onClick={() => {
                    const reason = prompt(
                      "Reason to void this attendance record",
                    );
                    if (reason)
                      action
                        .run(
                          () =>
                            api(
                              base +
                                "/events/" +
                                e.id +
                                "/attendance/" +
                                at.id +
                                "/correct",
                              "POST",
                              { reason },
                            ),
                          "Correction recorded",
                        )
                        .then(() => q.refetch());
                  }}
                >
                  Correct attendance
                </button>
              )}
            </div>
          ))}
          {q.data?.length === 0 && (
            <Empty
              title="No check-ins yet"
              text="Confirmed registrations appear here only after authorized check-in."
            />
          )}
        </DataState>
      </div>
    </Panel>
  );
}
function EventAnalytics({ event: e }: any) {
  const { base } = useCommunity();
  const q = useLoad(base + "/events/" + e.id + "/analytics");
  return (
    <DataState query={q}>
      <div className="community-metrics">
        {[
          ["Eligible people", q.data?.eligible],
          ["Invitations", q.data?.invitations],
          ["Registrations", q.data?.registrations],
          ["Confirmed seats", q.data?.confirmedSeats],
          ["Actual check-ins", q.data?.checkedIn],
          ["Attended seats", q.data?.attendedSeats],
          ["Guests registered", q.data?.guests],
          ["No-shows", q.data?.noShows ?? "After event"],
        ].map(([label, value]) => (
          <div key={label}>
            <strong>{value}</strong>
            <span>{label}</span>
          </div>
        ))}
      </div>
      <Panel title="Responses">
        <div className="padded">
          {q.data?.rsvps.map((r: any) => (
            <p key={r.status}>
              {r.status}: {r.count}
            </p>
          ))}
          <p className="muted">
            Registrations count requests; confirmed seats include companions;
            actual check-ins count verified attendance records. No-shows are
            calculated only after the event ends.
          </p>
        </div>
      </Panel>
    </DataState>
  );
}
function Invitations() {
  const { base } = useCommunity();
  const q = useLoad(base + "/invitations");
  return (
    <>
      <Heading
        eyebrow="YOUR COMMUNITY / INVITATIONS"
        title="You’re part of the plan."
        text="Your personal invitations, with their current response and expiry."
      />
      <Panel>
        <DataState query={q}>
          {q.data?.map((i: any) => (
            <Link
              className="invitation-list-item"
              key={i.id}
              to={"/app/community/events/" + i.eventId}
            >
              <span className="community-avatar">
                <CalendarDays size={20} />
              </span>
              <div>
                <h3>{i.title}</h3>
                <small>Expires {date(i.expiresAt)}</small>
              </div>
              <Badge>{i.status}</Badge>
              <ArrowRight size={17} />
            </Link>
          ))}
          {q.data?.length === 0 && (
            <Empty
              title="Your invitations will appear here"
              text="You can also discover events in your authorized community."
            />
          )}
        </DataState>
      </Panel>
    </>
  );
}
function ReviewCenter() {
  const [windowStart] = useState(() => Date.now());
  const { base } = useCommunity();
  const posts = useLoad(base + "/feed?status=Pending%20Approval"),
    events = useLoad(
      base +
        "/events?status=Pending%20Approval&from=" +
        new Date(windowStart - 30 * 86400000).toISOString() +
        "&to=" +
        new Date(windowStart + 62 * 86400000).toISOString(),
    );
  return (
    <>
      <Heading
        eyebrow="COMMUNITY / APPROVALS"
        title="Help good ideas move forward."
        text="Review content within your current organizational authority."
      />
      <Panel title="Activity submissions">
        <DataState query={posts}>
          {posts.data?.items.map((p: any) => (
            <div className="padded review-item" key={p.id}>
              <Link to={"/app/community/posts/" + p.id}>
                <h3>{p.title || "Activity submission"}</h3>
              </Link>
              <p>
                {p.author} · {p.organization}
              </p>
              <Link to={"/app/community/posts/" + p.id}>Open review</Link>
            </div>
          ))}
          {posts.data?.items.length === 0 && (
            <Empty title="No activities awaiting your review" />
          )}
        </DataState>
      </Panel>
      <Panel title="Event submissions">
        <DataState query={events}>
          {events.data?.items.map((e: any) => (
            <div className="padded review-item" key={e.id}>
              <Link to={"/app/community/events/" + e.id}>
                <h3>{e.title}</h3>
              </Link>
              <Link to={"/app/community/events/" + e.id}>Open review</Link>
            </div>
          ))}
          {events.data?.items.length === 0 && (
            <Empty title="No events awaiting your review" />
          )}
        </DataState>
      </Panel>
      <MyDrafts />
    </>
  );
}
function MyDrafts() {
  const { base } = useCommunity();
  const [status, setStatus] = useState("Draft");
  const q = useLoad(base + "/feed?status=" + encodeURIComponent(status));
  const events = useLoad(
    base + "/my/events?status=" + encodeURIComponent(status),
  );
  return (
    <Panel title="Drafts and returned activities">
      <div className="padded">
        <select
          aria-label="Activity state"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        >
          {[
            "Draft",
            "Changes Requested",
            "Rejected",
            "Scheduled",
            "Archived",
          ].map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
        {q.data?.items.map((p: any) => (
          <Link
            className="invitation-list-item"
            key={p.id}
            to={"/app/community/posts/" + p.id}
          >
            {p.title || "Untitled activity"}
            <Badge>{p.status}</Badge>
          </Link>
        ))}
        <h3>My event drafts and history</h3>
        {events.data?.map((e: any) => (
          <Link
            className="invitation-list-item"
            key={e.id}
            to={"/app/community/events/" + e.id}
          >
            {e.title}
            <Badge>{e.status}</Badge>
          </Link>
        ))}
      </div>
    </Panel>
  );
}
function Moderation() {
  const { base } = useCommunity(),
    action = useAction();
  const q = useLoad(base + "/reports");
  return (
    <>
      <Heading
        eyebrow="COMMUNITY / TRUST & CARE"
        title="Keep this a thoughtful space."
        text="Review reports and retain a reason for every moderation action."
      />
      <DataState query={q}>
        {q.data?.map((r: any) => (
          <Panel key={r.id}>
            <div className="padded">
              <Link to={"/app/community/posts/" + r.postId}>
                Open reported activity
              </Link>
              <p>{r.reason}</p>
              <Badge>{r.status}</Badge>
              <div className="detail-actions">
                {["Removed", "Archived", "Feature", "Unfeature"].map(
                  (decision) => (
                    <button
                      key={decision}
                      onClick={() =>
                        action.run(async () => {
                          const p = await api(base + "/posts/" + r.postId);
                          await api(
                            base + "/posts/" + r.postId + "/moderate",
                            "POST",
                            {
                              decision,
                              note: r.reason,
                              revision: p.post.revision,
                            },
                          );
                          q.refetch();
                        }, "Moderation recorded")
                      }
                    >
                      {decision}
                    </button>
                  ),
                )}
              </div>
            </div>
          </Panel>
        ))}
        {q.data?.length === 0 && <Empty title="No reports in your scope" />}
      </DataState>
    </>
  );
}
function Settings() {
  const { base, can } = useCommunity();
  const q = useLoad(base + "/setup");
  const [policy, setPolicy] = useState<any>(null),
    [name, setName] = useState(""),
    [kind, setKind] = useState("Activity");
  const action = useAction();
  useEffect(() => {
    if (q.data) setPolicy(q.data.policy);
  }, [q.data]);
  if (!can("activities.settings"))
    return (
      <Empty
        title="Policy administration is restricted"
        text="A society-wide policy permission is required."
      />
    );
  if (!policy) return <DataState query={q} />;
  return (
    <>
      <Heading
        title="Set the tone for your community."
        text="Configure approval, publishing and safety policies for this society."
      />
      <Panel title="Publishing policies">
        <div className="padded community-form">
          {[
            ["memberPosting", "Allow member chapter posts"],
            ["publicAllowed", "Allow explicitly authorized public posts"],
            ["blockingAllowed", "Allow member blocking"],
          ].map(([key, label]) => (
            <Choice
              key={key}
              label={label}
              checked={policy[key]}
              onChange={() => setPolicy({ ...policy, [key]: !policy[key] })}
            />
          ))}
          {[
            ["postStages", "Activity approvals", "activities.approve"],
            ["eventStages", "Event approvals", "events.approve"],
          ].map(([key, label, permission]) => (
            <Field key={key} label={label}>
              <select
                value={JSON.parse(policy[key]).length}
                onChange={(e) =>
                  setPolicy({
                    ...policy,
                    [key]: JSON.stringify(
                      Array(Number(e.target.value)).fill(permission),
                    ),
                  })
                }
              >
                <option value="0">Direct publishing</option>
                <option value="1">One independent officer review</option>
                <option value="2">Two review stages</option>
              </select>
            </Field>
          ))}
          <button
            className="primary"
            disabled={action.busy}
            onClick={() =>
              action
                .run(() => api(base + "/policy", "PUT", policy), "Policy saved")
                .then(() => q.refetch())
            }
          >
            Save policy
          </button>
        </div>
      </Panel>
      <Panel title="Activity categories and event types">
        <div className="padded">
          <div className="detail-actions">
            <input
              aria-label="New category name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="New category"
            />
            <select
              aria-label="Category kind"
              value={kind}
              onChange={(e) => setKind(e.target.value)}
            >
              <option>Activity</option>
              <option>Event</option>
            </select>
            <button
              disabled={!name.trim()}
              onClick={() =>
                action
                  .run(() =>
                    api(base + "/categories?kind=" + kind, "POST", {
                      name,
                      description: "",
                      active: true,
                    }),
                  )
                  .then(() => {
                    setName("");
                    q.refetch();
                  })
              }
            >
              Add category
            </button>
          </div>
          <div className="category-chips">
            {q.data?.categories.map((c: any) => (
              <button
                key={c.id}
                onClick={() =>
                  action
                    .run(() =>
                      api(base + "/categories/" + c.id, "PUT", {
                        name: c.name,
                        description: "",
                        active: !c.active,
                      }),
                    )
                    .then(() => q.refetch())
                }
              >
                {c.name} <Badge>{c.active ? "Active" : "Inactive"}</Badge>
              </button>
            ))}
          </div>
        </div>
      </Panel>
    </>
  );
}
function Intelligence() {
  const { base, scope } = useCommunity();
  const q = useLoad(
    base + "/intelligence" + (scope ? "?workspace=" + scope : ""),
  );
  return (
    <>
      <Heading
        eyebrow="COMMUNITY / MEASURED IMPACT"
        title="See the work taking shape."
        text="Database-backed activity and attendance for your authorized organizations, over the past 90 days."
      />
      <DataState query={q}>
        <div className="community-metrics">
          {[
            ["Published activities", q.data?.posts],
            ["Active contributors", q.data?.contributors],
            ["Chapters reporting", q.data?.chaptersReporting],
            ["Events in period", q.data?.events],
            ["Completed events", q.data?.completed],
            ["Upcoming (90 days)", q.data?.upcoming],
            ["Registrations", q.data?.registrations],
            ["Actual check-ins", q.data?.actualAttendance],
            ["Participating people", q.data?.participatingPeople],
            ["Reactions", q.data?.reactions],
            ["Visible comments", q.data?.comments],
          ].map(([label, value]) => (
            <div key={label}>
              <strong>{value}</strong>
              <span>{label}</span>
            </div>
          ))}
        </div>
        <Panel title="Regional comparisons">
          <div className="chapter-intelligence">
            {q.data?.byRegion.map((r: any) => (
              <div key={r.id}>
                <strong>{r.name}</strong>
                <p>
                  {r.posts} activities · {r.events} events · {r.attendance}{" "}
                  check-ins
                </p>
              </div>
            ))}
            {q.data?.byRegion.length === 0 && (
              <p>No regional activity in your current scope.</p>
            )}
          </div>
        </Panel>
        <Panel title="Activity categories">
          <div className="category-chips padded">
            {q.data?.byCategory.map((c: any) => (
              <span key={c.id}>
                <Badge>{c.name}</Badge> {c.count}
              </span>
            ))}
          </div>
        </Panel>
        <Panel title="Chapter activity">
          <div className="chapter-intelligence">
            {q.data?.byChapter.map((c: any) => (
              <Link key={c.id} to={"/app/community/chapters/" + c.id}>
                <strong>{c.name}</strong>
                <div className="activity-bar">
                  <span
                    style={{
                      width:
                        Math.max(
                          2,
                          (c.posts /
                            Math.max(
                              1,
                              ...q.data.byChapter.map((v: any) => v.posts),
                            )) *
                            100,
                        ) + "%",
                    }}
                  />
                </div>
                <small>
                  {c.posts} posts · {c.events} events · {c.attendance} check-ins
                </small>
              </Link>
            ))}
          </div>
        </Panel>
        <p className="muted">
          No impression, reach or follower estimates are collected. Attendance
          counts unique non-voided check-in records; companions appear in event
          seat totals.
        </p>
      </DataState>
    </>
  );
}
function Chapter() {
  const { id } = useParams();
  const { base } = useCommunity();
  const q = useLoad(base + "/chapters/" + id);
  const [tab, setTab] = useState("Overview");
  if (!q.data) return <DataState query={q} />;
  const c = q.data;
  return (
    <>
      <div className="chapter-hero">
        <div className="eyebrow">
          {c.region || "YOUR SOCIETY"} / CHAPTER PROFILE
        </div>
        <h1>{c.name}</h1>
        <p>
          {c.city} {c.formation ? " · Established " + c.formation : ""}
        </p>
        <div className="detail-actions">
          <Badge>{c.status}</Badge>
          <span>{c.members} permitted member records</span>
          <span>{c.completedPrograms} completed programs</span>
        </div>
      </div>
      <div className="community-tabs chapter-tabs">
        {[
          "Overview",
          "Activities",
          "Events",
          "Members",
          "Leadership",
          "Achievements",
          "Gallery",
        ].map((t) => (
          <button
            key={t}
            className={tab === t ? "active" : ""}
            onClick={() => setTab(t)}
          >
            {t}
          </button>
        ))}
      </div>
      {tab === "Overview" && (
        <>
          <Panel title="Current leadership">
            <div className="padded">
              {c.leadership.map((l: any) => (
                <p key={l.position}>
                  <strong>{l.name}</strong> · {l.position}
                </p>
              ))}
              {c.leadership.length === 0 && (
                <p>No current leadership records visible to you.</p>
              )}
            </div>
          </Panel>
          <Upcoming />
        </>
      )}
      {tab === "Activities" && <Feed chapter={id} />}
      {tab === "Achievements" && <Feed chapter={id} achievement />}
      {tab === "Events" && <ChapterEvents id={id} />}
      {tab === "Members" && (
        <Panel>
          <div className="padded">
            <h2>Your authorized chapter directory</h2>
            <p>
              Personal fields retain Phase 2 directory consent and
              administrative scope rules.
            </p>
            <Link
              className="primary"
              to={"/app/members/directory?chapter=" + id}
            >
              Open member directory
            </Link>
          </div>
        </Panel>
      )}
      {tab === "Leadership" && (
        <Panel>
          <div className="padded">
            {c.leadership.map((l: any) => (
              <p key={l.position}>
                {l.name} · {l.position} · Until {date(l.endsAt)}
              </p>
            ))}
          </div>
        </Panel>
      )}
      {tab === "Gallery" && (
        <MediaGallery
          items={c.gallery.map((m: any) => ({
            ...m,
            contentType: "image/png",
          }))}
          base={base}
        />
      )}
    </>
  );
}
function ChapterEvents({ id }: any) {
  const { base } = useCommunity();
  const q = useLoad(base + "/events?workspace=" + id);
  return (
    <DataState query={q}>
      <div className="event-grid">
        {q.data?.items.map((e: any) => (
          <EventCard key={e.id} event={e} />
        ))}
      </div>
    </DataState>
  );
}

export function PublicActivity() {
  const { tenant, id } = useParams();
  const base = "/public/societies/" + tenant + "/community";
  const q = useLoad(base + "/posts/" + id);
  return (
    <main className="community-public">
      <Link to="/">ASSEMBLIA / COMMUNITY</Link>
      <DataState query={q}>
        {q.data && (
          <article className="community-detail">
            <Badge>{q.data.organization}</Badge>
            <h1>{q.data.title || "Community activity"}</h1>
            <p>{date(q.data.publishedAt)}</p>
            <Rich html={q.data.body} />
            <MediaGallery items={q.data.media} base={base} />
            <Link to={"/app/community/posts/" + id}>
              Sign in to your community
            </Link>
          </article>
        )}
      </DataState>
    </main>
  );
}
