import {Children, cloneElement, isValidElement} from 'react';
import type {ReactNode} from 'react';

export type AuthLanguage = 'en' | 'hi' | 'te';
const copy: Record<string, [string, string]> = {
  'Login':['लॉग इन','లాగిన్'], 'Sign up':['पंजीकरण','నమోదు'], 'Sign in':['साइन इन करें','సైన్ ఇన్'],
  'Email or username':['ईमेल या उपयोगकर्ता नाम','ఈమెయిల్ లేదా వినియోగదారు పేరు'],
  'Email address':['ईमेल पता','ఈమెయిల్ చిరునామా'], 'Password':['पासवर्ड','పాస్‌వర్డ్'], 'New password':['नया पासवर्ड','కొత్త పాస్‌వర్డ్'],
  'Full name':['पूरा नाम','పూర్తి పేరు'], 'Mobile number':['मोबाइल नंबर','మొబైల్ నంబర్'], 'Username':['उपयोगकर्ता नाम','వినియోగదారు పేరు'],
  'Choose a username (optional)':['उपयोगकर्ता नाम चुनें (वैकल्पिक)','వినియోగదారు పేరు ఎంచుకోండి (ఐచ్ఛికం)'],
  'Remember me':['मुझे याद रखें','నన్ను గుర్తుంచుకోండి'], 'Forgot password?':['पासवर्ड भूल गए?','పాస్‌వర్డ్ మర్చిపోయారా?'],
  'Welcome back':['वापसी पर स्वागत है','తిరిగి స్వాగతం'], 'Verify it’s you':['अपनी पहचान की पुष्टि करें','మీ గుర్తింపును నిర్ధారించండి'],
  'Verify your email':['अपना ईमेल सत्यापित करें','మీ ఈమెయిల్‌ను ధృవీకరించండి'], 'Reset your password':['अपना पासवर्ड रीसेट करें','మీ పాస్‌వర్డ్‌ను రీసెట్ చేయండి'],
  'Choose a new password':['नया पासवर्ड चुनें','కొత్త పాస్‌వర్డ్ ఎంచుకోండి'], 'Create account':['खाता बनाएं','ఖాతా సృష్టించండి'],
  'Create an account':['खाता बनाएं','ఖాతా సృష్టించండి'], 'Please wait…':['कृपया प्रतीक्षा करें…','దయచేసి వేచి ఉండండి…'],
  'Verify & sign in':['सत्यापित करें और साइन इन करें','ధృవీకరించి సైన్ ఇన్ చేయండి'], 'Verify email':['ईमेल सत्यापित करें','ఈమెయిల్ ధృవీకరించండి'],
  'Reset password':['पासवर्ड रीसेट करें','పాస్‌వర్డ్ రీసెట్ చేయండి'], 'Send recovery email':['पुनर्प्राप्ति ईमेल भेजें','రికవరీ ఈమెయిల్ పంపండి'],
  'Resend verification email':['सत्यापन ईमेल फिर भेजें','ధృవీకరణ ఈమెయిల్ మళ్లీ పంపండి'],
  'Request a fresh verification email':['नया सत्यापन ईमेल मांगें','కొత్త ధృవీకరణ ఈమెయిల్ కోరండి'],
  'Continue to sign in':['साइन इन करने के लिए आगे बढ़ें','సైన్ ఇన్‌కు కొనసాగండి'],
  'Authenticator code':['प्रमाणक कोड','ఆథెంటికేటర్ కోడ్'], 'Recovery code':['पुनर्प्राप्ति कोड','రికవరీ కోడ్'],
  'Use a single-use recovery code':['एक बार उपयोग वाला पुनर्प्राप्ति कोड उपयोग करें','ఒకసారి ఉపయోగించే రికవరీ కోడ్ వాడండి'],
  'Use authenticator instead':['प्रमाणक का उपयोग करें','ఆథెంటికేటర్ వాడండి'], 'Use another account / restart':['दूसरा खाता / फिर शुरू करें','మరో ఖాతా / మళ్లీ ప్రారంభించండి'],
  'Caps Lock is on':['कैप्स लॉक चालू है','క్యాప్స్ లాక్ ఆన్‌లో ఉంది'], 'Show password':['पासवर्ड दिखाएं','పాస్‌వర్డ్ చూపండి'], 'Hide password':['पासवर्ड छिपाएं','పాస్‌వర్డ్ దాచండి'],
  'Sign in with a passkey':['पासकी से साइन इन करें','పాస్‌కీతో సైన్ ఇన్ చేయండి'],
  'Email me a sign-in link':['मुझे साइन-इन लिंक ईमेल करें','నాకు సైన్ ఇన్ లింక్ పంపండి'],
  'Send sign-in link':['साइन-इन लिंक भेजें','సైన్ ఇన్ లింక్ పంపండి'], 'Confirm sign-in':['साइन-इन की पुष्टि करें','సైన్ ఇన్ నిర్ధారించండి'],
  'Open a sign-in link':['साइन-इन लिंक खोलें','సైన్ ఇన్ లింక్ తెరవండి'],
  'Use your password instead':['अपने पासवर्ड का उपयोग करें','మీ పాస్‌వర్డ్ వాడండి'],
  'Open sign-in in this tab':['इसी टैब में साइन-इन खोलें','ఈ ట్యాబ్‌లో సైన్ ఇన్ తెరవండి'],
  'Use a personal device. Remember me keeps your cookie for up to 8 hours of inactivity; activity renews it.':['निजी उपकरण पर उपयोग करें। निष्क्रियता के 8 घंटे तक कुकी रहती है; गतिविधि से समय बढ़ता है।','వ్యక్తిగత పరికరంలో వాడండి. 8 గంటల నిష్క్రియ వరకు కుకీ ఉంటుంది; వినియోగంతో సమయం పెరుగుతుంది.'],
  'Check your spam folder and confirm you entered the correct email.':['स्पैम फ़ोल्डर देखें और अपना ईमेल पता जांचें।','స్పామ్ ఫోల్డర్ చూడండి, మీ ఈమెయిల్ చిరునామాను తనిఖీ చేయండి.'],
  'Correct email / try again':['ईमेल सुधारें / फिर प्रयास करें','ఈమెయిల్ సరిచేయండి / మళ్లీ ప్రయత్నించండి'],
  'Check your details and try again. You can also reset your password or request a verification email.':['विवरण जांचकर फिर प्रयास करें। पासवर्ड रीसेट या सत्यापन ईमेल का विकल्प भी उपलब्ध है।','వివరాలు తనిఖీ చేసి మళ్లీ ప్రయత్నించండి. పాస్‌వర్డ్ రీసెట్ లేదా ధృవీకరణ ఈమెయిల్ కూడా కోరవచ్చు.'],
  'Request completed. Check your inbox and spam folder for the next step.':['अनुरोध पूरा हुआ। अगले चरण के लिए इनबॉक्स और स्पैम फ़ोल्डर देखें।','అభ్యర్థన పూర్తయింది. తదుపరి దశ కోసం ఇన్‌బాక్స్ మరియు స్పామ్ చూడండి.'],
  'Enter your authenticator or single-use recovery code.':['प्रमाणक या एक बार उपयोग वाला पुनर्प्राप्ति कोड दर्ज करें।','ఆథెంటికేటర్ లేదా ఒకసారి ఉపయోగించే రికవరీ కోడ్ నమోదు చేయండి.'],
  'We’ll help you get back to your community.':['हम आपको अपने समुदाय में वापस आने में मदद करेंगे।','మీ సంఘంలోకి తిరిగి రావడానికి సహాయం చేస్తాము.'],
  'Create a strong password for your account.':['अपने खाते के लिए मजबूत पासवर्ड बनाएं।','మీ ఖాతాకు బలమైన పాస్‌వర్డ్ సృష్టించండి.'],
  'Confirm your email to continue.':['आगे बढ़ने के लिए ईमेल की पुष्टि करें।','కొనసాగడానికి మీ ఈమెయిల్ నిర్ధారించండి.'],
  '12+ characters with uppercase, lowercase, a number and symbol.':['12+ अक्षर: बड़े और छोटे अक्षर, अंक और चिह्न।','12+ అక్షరాలు: పెద్ద, చిన్న అక్షరాలు, సంఖ్య, గుర్తు.'],
  'Enter your password':['अपना पासवर्ड दर्ज करें','మీ పాస్‌వర్డ్ నమోదు చేయండి'], 'Enter your full name':['पूरा नाम दर्ज करें','మీ పూర్తి పేరు నమోదు చేయండి'],
  'Enter mobile number':['मोबाइल नंबर दर्ज करें','మొబైల్ నంబర్ నమోదు చేయండి'], 'Create a strong password':['मजबूत पासवर्ड बनाएं','బలమైన పాస్‌వర్డ్ సృష్టించండి'],
  'Privacy':['गोपनीयता','గోప్యత'], 'Terms':['शर्तें','నిబంధనలు'], 'Help':['सहायता','సహాయం'],
  'Secure access to your community':['आपके समुदाय तक सुरक्षित पहुंच','మీ సంఘానికి సురక్షిత ప్రవేశం'],
  'Membership':['सदस्यता','సభ్యత్వం'], 'Chapters':['शाखाएं','శాఖలు'], 'Learning tools':['सीखने के साधन','అభ్యాస సాధనాలు'], 'Research':['अनुसंधान','పరిశోధన'],
  'Confirm your existing account':['अपने मौजूदा खाते की पुष्टि करें','మీ ప్రస్తుత ఖాతాను నిర్ధారించండి'], 'Confirm account':['खाते की पुष्टि करें','ఖాతాను నిర్ధారించండి'],
  'I am a':['सदस्यता प्रकार','సభ్యత్వ రకం'], 'Individual':['व्यक्तिगत','వ్యక్తిగత'], 'Institutional':['संस्थागत','సంస్థాగత'], 'Corporate':['कॉर्पोरेट','కార్పొరేట్'], 'member':['सदस्य','సభ్యుడు'],
  'Choose a unique username':['अलग उपयोगकर्ता नाम चुनें','ప్రత్యేక వినియోగదారు పేరు ఎంచుకోండి'],
  'I agree to the':['मैं सहमत हूं:','నేను అంగీకరిస్తున్నాను:'], 'Terms of Use':['उपयोग की शर्तें','వినియోగ నిబంధనలు'], 'Privacy Policy':['गोपनीयता नीति','గోప్యతా విధానం'], 'and':['और','మరియు'],
  'or continue with':['या इनके साथ जारी रखें','లేదా దీనితో కొనసాగండి'],
  'Already a member?':['पहले से सदस्य हैं?','ఇప్పటికే సభ్యులా?'],
  'Already registered with the society? Use your registered personal email or approved organization contact email. Existing memberships appear after verification.':['पहले से पंजीकृत हैं? अपने पंजीकृत निजी ईमेल या स्वीकृत संगठन संपर्क ईमेल का उपयोग करें। सत्यापन के बाद सदस्यताएं दिखेंगी।','ఇప్పటికే నమోదయ్యారా? మీ నమోదైన వ్యక్తిగత ఈమెయిల్ లేదా ఆమోదించిన సంస్థ సంప్రదింపు ఈమెయిల్ వాడండి. ధృవీకరణ తర్వాత సభ్యత్వాలు కనిపిస్తాయి.'],
  'Confirm the existing account’s password or connected provider. Its MFA, memberships and roles are retained.':['मौजूदा खाते के पासवर्ड या जुड़े प्रदाता से पुष्टि करें। MFA, सदस्यताएं और भूमिकाएं सुरक्षित रहेंगी।','ప్రస్తుత ఖాతా పాస్‌వర్డ్ లేదా అనుసంధానించిన ప్రొవైడర్‌తో నిర్ధారించండి. MFA, సభ్యత్వాలు, పాత్రలు కొనసాగుతాయి.'],
  'Please agree to the Terms of Use and Privacy Policy before creating an account.':['खाता बनाने से पहले उपयोग की शर्तें और गोपनीयता नीति स्वीकार करें।','ఖాతా సృష్టించే ముందు వినియోగ నిబంధనలు, గోప్యతా విధానం అంగీకరించండి.'],
  'Sign in with the same account to continue your open work.':['खुले काम को जारी रखने के लिए उसी खाते से साइन इन करें।','మీ ప్రస్తుత పనిని కొనసాగించడానికి అదే ఖాతాతో సైన్ ఇన్ చేయండి.'],
  'Please complete this field.':['यह जानकारी भरें।','ఈ వివరాన్ని నమోదు చేయండి.'],
  'Please check this field.':['यह जानकारी जांचें।','ఈ వివరాన్ని తనిఖీ చేయండి.'],
  'Password reset. Sign in with your new password.':['पासवर्ड रीसेट हो गया। नए पासवर्ड से साइन इन करें।','పాస్‌వర్డ్ రీసెట్ అయింది. కొత్త పాస్‌వర్డ్‌తో సైన్ ఇన్ చేయండి.'],
  'Email verified. You can sign in.':['ईमेल सत्यापित हो गया। अब साइन इन करें।','ఈమెయిల్ ధృవీకరించబడింది. ఇప్పుడు సైన్ ఇన్ చేయవచ్చు.'],
};
export function authText(text: string, lang: AuthLanguage) {
  if(lang==='en')return text;
  const value=text.trim(),pair=copy[value],index=lang==='hi'?0:1;
  if(pair)return text.replace(value,pair[index]);
  const join=value.match(/^Join the (.+) community$/);
  if(join)return lang==='hi'?join[1]+' समुदाय से जुड़ें':join[1]+' సంఘంలో చేరండి';
  const newcomer=value.match(/^New to (.+)\?$/);
  if(newcomer)return lang==='hi'?newcomer[1]+' में नए हैं?':newcomer[1]+'కు కొత్తవారా?';
  return text;
}
// Translate static form labels and accessible names together, including Field props.
export function authTree(node: ReactNode, lang: AuthLanguage): ReactNode {
  return Children.map(node, child => {
    if (typeof child === 'string') return authText(child, lang);
    if (!isValidElement(child)) return child;
    const props = child.props as Record<string, any>, next: Record<string, any> = {};
    for (const key of ['label', 'title', 'placeholder', 'aria-label']) if (typeof props[key] === 'string') next[key] = authText(props[key], lang);
    if ('children' in props) next.children = authTree(props.children, lang);
    return cloneElement(child, next);
  });
}
