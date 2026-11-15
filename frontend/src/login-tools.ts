import {api} from './api';

export function safeMemberReturn(value: string | null | undefined) {
  return value && value.length < 2000 && /^\/app(?:\/|\?|$)/.test(value) && !/[\\%\x00-\x1f\x7f]/.test(value) ? value : '/app/membership';
}
function decode(value: string) {
  return Uint8Array.from(atob(value.replace(/-/g, '+').replace(/_/g, '/')), c => c.charCodeAt(0));
}
function encode(value: ArrayBuffer) {
  return btoa(String.fromCharCode(...new Uint8Array(value))).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}
export function passkeysAvailable() { return !!window.PublicKeyCredential && window.isSecureContext; }
export async function passkey(enroll = false, name = 'My passkey', rememberMe = false) {
  if (!passkeysAvailable()) throw new Error('Passkeys require a supported browser and a secure connection.');
  const options = await api<any>(enroll ? '/security/passkeys/options' : '/auth/passkeys/options', 'POST', {});
  options.challenge = decode(options.challenge);
  if (enroll) options.user.id = decode(options.user.id);
  for (const key of ['excludeCredentials', 'allowCredentials']) options[key]?.forEach((c: any) => c.id = decode(c.id));
  const credential = await (enroll ? navigator.credentials.create({publicKey: options}) : navigator.credentials.get({publicKey: options})) as PublicKeyCredential | null;
  if (!credential) throw new Error('Passkey verification was cancelled.');
  const response = credential.response;
  const value: any = {id: credential.id, rawId: encode(credential.rawId), type: credential.type, authenticatorAttachment: credential.authenticatorAttachment, clientExtensionResults: credential.getClientExtensionResults(), response: {clientDataJSON: encode(response.clientDataJSON)}};
  if (response instanceof AuthenticatorAttestationResponse) {
    value.response.attestationObject = encode(response.attestationObject);
    value.response.transports = response.getTransports?.() || [];
  } else {
    const assertion = response as AuthenticatorAssertionResponse;
    value.response.authenticatorData = encode(assertion.authenticatorData);
    value.response.signature = encode(assertion.signature);
    value.response.userHandle = assertion.userHandle ? encode(assertion.userHandle) : null;
  }
  return api<any>(enroll ? '/security/passkeys' : '/auth/passkeys/login', 'POST', {credentialJson: JSON.stringify(value), name, rememberMe});
}
