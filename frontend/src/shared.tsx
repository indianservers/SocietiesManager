import {Card, IconButton} from "./ui";
import { useState, useEffect, useRef, createContext, useContext, useId } from "react";
import { useQuery } from "@tanstack/react-query";
import { Layers, X, LoaderCircle, Crown, PenLine, Building2, UserRound } from "lucide-react";
import { api } from "./api";
export type Context = {
  tenant: string;
  platformAdmin?: boolean;
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
 const [error,setError]=useState('');const id=useId();return <label className="field" onInvalidCapture={e=>{const input=e.target as HTMLInputElement;setError(input.validationMessage);input.setAttribute('aria-invalid','true');input.setAttribute('aria-describedby',id)}} onInputCapture={e=>{const input=e.target as HTMLInputElement;setError('');input.removeAttribute('aria-invalid')}}><span>{label}</span>{children||<input {...props}/ >}{error&&<small id={id} role="alert" className="field-error">{error}</small>}</label>;
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
export function Panel({children,title}:any){return <Card title={title}>{children}</Card>}
export function Drawer({ title, onClose, children }: any) {
  const close=()=>{if(dialog.current?.querySelector('form[data-dirty="true"]')&&!window.confirm("Discard unsaved changes?"))return;onClose()};
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
      if (e.key === "Escape") close();
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
    <div className="overlay" onClick={close}>
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
          <IconButton label="Close drawer" onClick={close}><X/></IconButton>
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
      path.includes("/communications") || path.includes("/finance") || path.includes("/studio") || path.includes("/service") ? 5000 : path.includes("/membership") || path === "/workspaces" ? 30000 : false,
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
        window.dispatchEvent(new Event("aimer:saved"));
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

export function RoleBadge({children}: {children:string}) {
 const hash=[...children.toLowerCase()].reduce((n,c)=>((n*31+c.charCodeAt(0))>>>0),0), hue=hash%360;
 const Icon=/president/i.test(children)?Crown:/secretary/i.test(children)?PenLine:/institution/i.test(children)?Building2:UserRound;
 return <span className="role-badge" style={{color:`hsl(${hue} 65% 28%)`,background:`hsl(${hue} 70% 94%)`,borderColor:`hsl(${hue} 45% 65%)`,borderRadius:/president/i.test(children)?'6px':'999px'}}><Icon size={13} aria-hidden="true"/>{children}</span>;
}

export function formatMembershipExpiry(value?:string){
 return value ? new Date(new Date(value).getTime()-1).toLocaleDateString('en-IN',{timeZone:'Asia/Kolkata',day:'numeric',month:'short',year:'numeric'}) : 'No expiry';
}
