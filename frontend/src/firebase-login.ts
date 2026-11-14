import {getApps,initializeApp} from 'firebase/app';
import {getAuth,GoogleAuthProvider,OAuthProvider,inMemoryPersistence,setPersistence,signInWithPopup,signOut,linkWithCredential} from 'firebase/auth';
import type {Auth,AuthCredential} from 'firebase/auth';

export type FirebasePublicConfig={apiKey:string;authDomain:string;projectId:string;appId:string};
let auth:Auth;
let pendingCredential:AuthCredential|null=null;
let prepared:Promise<void>|undefined;
export function prepare(config:FirebasePublicConfig){
  if(!prepared){
    const name='society-membership-'+config.projectId;
    const app=getApps().find(a=>a.name===name)||initializeApp(config,name);
    auth=getAuth(app);auth.useDeviceLanguage();
    prepared=setPersistence(auth,inMemoryPersistence);
  }
  return prepared;
}
export async function popup(providerId:'google.com'|'microsoft.com'){
  const provider=providerId==='google.com'?new GoogleAuthProvider():new OAuthProvider('microsoft.com');
  provider.setCustomParameters({prompt:'select_account'});
  try{
    const result=await signInWithPopup(auth,provider);
    if(pendingCredential){await linkWithCredential(result.user,pendingCredential);pendingCredential=null}
    return await result.user.getIdToken(true);
  }catch(error:any){
    if(error.code==='auth/account-exists-with-different-credential'){
      pendingCredential=OAuthProvider.credentialFromError(error);
      throw new Error('This email already uses another provider. Confirm with that provider first to connect the accounts.');
    }
    const messages:Record<string,string>={
      'auth/popup-closed-by-user':'Sign-in was cancelled. You can try again or use your username and password.',
      'auth/cancelled-popup-request':'Another sign-in window is already open.',
      'auth/popup-blocked':'Allow sign-in popups for this site, then try again.',
      'auth/unauthorized-domain':'This portal domain must be authorized in Firebase Authentication.',
      'auth/operation-not-allowed':'This sign-in provider is not enabled in Firebase Authentication.',
      'auth/network-request-failed':'Could not reach the sign-in provider. Check your connection and try again.',
      'auth/invalid-credential':'The provider sign-in could not be verified. Try signing in again.'
    };
    throw new Error(messages[error.code]||'Provider sign-in failed. Try again or use your username and password.');
  }finally{
    if(auth?.currentUser)await signOut(auth);
  }
}
