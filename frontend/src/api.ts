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
  if (!res.ok) {
    const e = await res.json().catch(() => ({}));
    throw new Error(
      e.title ||
        Object.values(e.errors || {})
          .flat()
          .join(" ") ||
        `Request failed (${res.status})`,
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
};
