export class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;
  constructor(message: string, status: number, errors?: Record<string, string[]>) { super(message); this.name = 'ApiError'; this.status = status; this.errors = errors; }
}
export async function api<T = any>(
  path: string,
  method = "GET",
  body?: unknown,
): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };
  if (method !== "GET") {
    const r = await fetch("/api/v1/auth/csrf");
    headers["X-CSRF-TOKEN"] = (await r.json()).token;
  }
  const res = await fetch("/api/v1" + path, {
    method,
    headers,
    credentials: "include",
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const expiry = res.headers.get('X-Member-Session-Expires');
  if (expiry) window.dispatchEvent(new CustomEvent('member:session-expiry', {detail: expiry}));
  const returnTo = res.headers.get('X-Member-Return');
  if (returnTo) sessionStorage.setItem('member.magicReturn', returnTo);
  if (!res.ok) {
    if (res.status === 401 && !path.startsWith('/auth/') && path !== '/users/me') window.dispatchEvent(new Event('member:session-expired'));
    const e = await res.json().catch(() => ({}));
    throw new ApiError(
      e.title ||
        Object.values(e.errors || {})
          .flat()
          .join(" ") ||
        `Request failed (${res.status})`, res.status, e.errors,
    );
  }
  return res.status === 204 ? (undefined as T) : res.json();
}
export const details = (s?: string) => {
  try {
    return JSON.parse(s || "{}");
  } catch {
    return {};
  }
};
export type Level = {
  id: string;
  name: string;
  kind: string;
  allowRoot: boolean;
  allowedParents: string[];
};
export type Unit = {
  id: string;
  name: string;
  code: string;
  levelId: string;
  parentId: string | null;
  status: string;
  details: string;
  revision: number;
  level?: string;
  children?: number;
  chapters?: number;
  head?: string;
  kind?: string;
  canEdit?: boolean;
  canCreate?: boolean;
  canArchive?: boolean;
  institutionMembershipId?: string | null;
};
