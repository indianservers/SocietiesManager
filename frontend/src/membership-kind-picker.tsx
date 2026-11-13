import "./member-portal.css";
export const membershipKinds = ["Individual","Institutional","Corporate"] as const;
export function MembershipKindPicker({value,onChange,name,offers}: {value:string;onChange:(kind:string)=>void;name?:string;offers?:any[]}) {
  return <fieldset className="membership-kind-picker"><legend>Choose your membership</legend>{membershipKinds.map(kind=>{const offer=offers?.find(c=>c.kind===kind&&c.offer?.price!=null)?.offer;return <label key={kind} className={value===kind?'selected':''}>
    <input type="radio" name={name||'membershipKind'} value={kind} checked={value===kind} onChange={()=>onChange(kind)}/><span><strong>{kind}</strong><small>{kind==='Individual'?'Join as a person':kind==='Institutional'?'For colleges, universities and institutes':'For companies and industry partners'}</small>{offer&&<small className="membership-offer-price">{new Intl.NumberFormat('en-IN',{style:'currency',currency:offer.currency,maximumFractionDigits:0}).format(offer.price)} / {offer.termMonths===12?'year':offer.termMonths+' months'}</small>}</span>
  </label>})}</fieldset>;
}
