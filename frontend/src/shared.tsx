import { useState, useEffect, useRef, createContext, useContext } from "react";
import { useQuery } from "@tanstack/react-query";
import { Layers, X, LoaderCircle } from "lucide-react";
import { api } from "./api";
export type Context = {
  tenant: string;
  workspace: any;
  scope: string;
  notify: (s: string) => void;
  refresh: () => void;
  can: (p: string) => boolean;
  switchWorkspace: (tenant: string, scope?: string) => void;
};
export const Ctx = createContext<Context>(null!);
export const useApp = () => useContext(Ctx);
export function Field({ label, children, ...props }: any) {
  return (
    <label className="field">
      <span>{label}</span>
      {children || <input {...props} />}
    </label>
  );
}
export function Badge({ children }: any) {
  return (
    <span
      className={"badge " + String(children).toLowerCase().replaceAll(" ", "-")}
    >
      {children}
    </span>
  );
}
export function Empty({ title = "Nothing here yet", text, children }: any) {
  return (
    <div className="empty">
      <Layers size={32} />
      <h3>{title}</h3>
      <p>{text}</p>
      {children}
    </div>
  );
}
export function Heading({ eyebrow, title, text, children }: any) {
  return (
    <div className="heading">
      <div>
        <div className="eyebrow">
          {eyebrow || "YOUR ORGANIZATION, CONNECTED"}
        </div>
        <h1>{title}</h1>
        <p>{text}</p>
      </div>
      <div>{children}</div>
    </div>
  );
}
export function Panel({ children, title }: any) {
  return (
    <section className="panel">
      {title && <div className="panel-title">{title}</div>}
      {children}
    </section>
  );
}
export function Drawer({ title, onClose, children }: any) {
  const dialog = useRef<HTMLElement>(null);
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null;
    const bodyOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    dialog.current
      ?.querySelector<HTMLElement>("input,select,textarea,button")
      ?.focus();
    return () => {
      document.body.style.overflow = bodyOverflow;
      previous?.focus();
    };
  }, []);
  useEffect(() => {
    const f = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "Tab") {
        const nodes = [
          ...(dialog.current?.querySelectorAll<HTMLElement>(
            "button:not(:disabled),input:not(:disabled),select:not(:disabled),textarea:not(:disabled),a[href]",
          ) || []),
        ];
        const first = nodes[0],
          last = nodes[nodes.length - 1];
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last?.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first?.focus();
        }
      }
    };
    document.addEventListener("keydown", f);
    return () => document.removeEventListener("keydown", f);
  }, [onClose]);
  return (
    <div className="overlay" onClick={onClose}>
      <section
        ref={dialog}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="drawer"
        onClick={(e) => e.stopPropagation()}
      >
        <header>
          <h2>{title}</h2>
          <button
            className="icon-btn"
            aria-label="Close drawer"
            onClick={onClose}
          >
            <X />
          </button>
        </header>
        {children}
      </section>
    </div>
  );
}
export function DataState({ query, children }: any) {
  if (query.isPending)
    return (
      <div className="skeleton">
        <LoaderCircle className="spin" /> Loading records…
      </div>
    );
  if (query.isError)
    return (
      <div role="alert" className="error">
        {query.error.message}
        <button onClick={() => query.refetch()}>Retry</button>
      </div>
    );
  return children;
}
export function useLoad<T = any>(path: string, enabled = true) {
  return useQuery<T>({
    queryKey: [path],
    queryFn: () => api<T>(path),
    enabled,
    refetchInterval:
      path.includes("/membership") || path === "/workspaces" ? 30000 : false,
  });
}
export function useAction() {
  const { notify, refresh } = useApp();
  const [busy, setBusy] = useState(false);
  return {
    busy,
    run: async (fn: () => Promise<any>, success = "Saved successfully") => {
      setBusy(true);
      try {
        await fn();
        refresh();
        notify(success);
        return true;
      } catch (e) {
        notify((e as Error).message);
        return false;
      } finally {
        setBusy(false);
      }
    },
  };
}
