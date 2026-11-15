<?php
// Idempotent update for the explicitly authorized AIMER home and registration pages.
if (!defined('ABSPATH')) { throw new RuntimeException('Run with wp eval-file.'); }
if (rtrim(get_option('home'), '/') !== 'https://aimersociety.com') { throw new RuntimeException('Unexpected website'); }
$register = get_post(1039); $home_id = (int)get_option('page_on_front'); $home = get_post($home_id);
if (!$register || $register->post_name !== 'register' || !$home || $home_id !== 12353) { throw new RuntimeException('Unexpected page identifiers'); }
$backup_dir = '/mnt/volume_sfo3_1785477034157/aimer-members/backups';
if (!is_dir($backup_dir)) { mkdir($backup_dir, 0700, true); }
$backup_file = $backup_dir.'/wordpress-membership-links-'.gmdate('Ymd-His').'.json';
file_put_contents($backup_file, wp_json_encode(['home_id'=>$home_id,'home_content'=>$home->post_content,'register_content'=>$register->post_content,'home_elementor'=>get_post_meta($home_id,'_elementor_data',false),'register_elementor'=>get_post_meta(1039,'_elementor_data',false)], JSON_PRETTY_PRINT)); chmod($backup_file,0600);
$markup = <<<'HTML'
<section id="aimer-membership-payments" style="max-width:1200px;margin:24px auto;padding:24px;border:1px solid #dac7ef;border-radius:16px;background:linear-gradient(110deg,#fff,#f3eaff);color:#26133b;box-sizing:border-box">
<h2 style="margin:0 0 8px;font-size:28px">Join or renew your AIMER membership</h2>
<p>Choose the secure payment link for a new membership or renewal.</p>
<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:18px;margin:18px 0">
<div style="background:white;border:1px solid #ddc9ed;padding:20px;border-radius:12px"><h3 style="margin:0 0 10px">Individual membership</h3><a href="https://rzp.io/l/AIMERSIndividualFee" target="_blank" rel="noopener noreferrer" style="display:inline-block;padding:12px 18px;background:#6719aa;color:white;border-radius:8px;text-decoration:none">New membership / renewal</a><p style="margin:12px 0 0"><a href="https://members.aimersociety.com/signup?kind=Individual">Create your individual portal account</a></p></div>
<div style="background:white;border:1px solid #ddc9ed;padding:20px;border-radius:12px"><h3 style="margin:0 0 10px">Institutional membership</h3><a href="https://rzp.io/l/AIMERSEduMembershipFee" target="_blank" rel="noopener noreferrer" style="display:inline-block;padding:12px 18px;background:#6719aa;color:white;border-radius:8px;text-decoration:none">New membership / renewal</a><p style="margin:12px 0 0"><a href="https://members.aimersociety.com/signup?kind=Institutional">Create your institutional portal account</a></p></div>
</div><p>Already registered? <a href="https://members.aimersociety.com/">Sign in to the membership portal</a> for your profile, membership details and resources.</p><p style="font-size:14px">Membership activation and validity updates follow payment confirmation and AIMER review. Use your registered email for renewals.</p>
</section>
HTML;
$content=$register->post_content;
$content=preg_replace('~<li>Individual Membership:.*?</li>~s','<li>Individual membership: ₹500 per annum. <a href="https://rzp.io/l/AIMERSIndividualFee" target="_blank" rel="noopener noreferrer">New membership / renewal — pay individual membership fee</a>.</li>',$content,1,$individual_count);
$content=preg_replace('~<li>Educational Institutions Membership:.*?</li>~s','<li>Educational institutions membership: ₹10,000 per annum. <a href="https://rzp.io/l/AIMERSEduMembershipFee" target="_blank" rel="noopener noreferrer">New membership / renewal — pay institutional membership fee</a>. <a href="https://aimersociety.com/institutional-membership-benefits/">Benefits for institutional members</a>.</li>',$content,1,$institution_count);
if (strpos($content,'aimer-membership-payments')===false) { $content=$markup.$content; }
$result=wp_update_post(wp_slash(['ID'=>1039,'post_content'=>$content]),true);if(is_wp_error($result)){throw new RuntimeException($result->get_error_message());}
$data=get_post_meta($home_id,'_elementor_data',true);$elements=json_decode($data,true);
if (!is_array($elements)) { throw new RuntimeException('Home Elementor data is not valid JSON'); }
$elements=array_values(array_filter($elements,fn($element)=>($element['id']??'')!=='amfees01'));
$section=['id'=>'amfees01','elType'=>'section','settings'=>['content_width'=>'boxed'],'elements'=>[['id'=>'amfees02','elType'=>'column','settings'=>['_column_size'=>100],'elements'=>[['id'=>'amfees03','elType'=>'widget','widgetType'=>'html','settings'=>['html'=>$markup],'elements'=>[]]]]]];
$register_data=json_decode(get_post_meta(1039,'_elementor_data',true),true);
if(is_array($register_data)){$register_data=array_values(array_filter($register_data,fn($element)=>($element['id']??'')!=='amfees01'));array_unshift($register_data,$section);update_post_meta(1039,'_elementor_data',wp_slash(wp_json_encode($register_data)));}
foreach([$home_id,1039] as $id){delete_post_meta($id,'_elementor_element_cache');}
array_unshift($elements,$section);update_post_meta($home_id,'_elementor_data',wp_slash(wp_json_encode($elements)));
$home_content=$home->post_content;if(strpos($home_content,'aimer-membership-payments')===false){$home_content=$markup.$home_content;}
$result=wp_update_post(wp_slash(['ID'=>$home_id,'post_content'=>$home_content]),true);if(is_wp_error($result)){throw new RuntimeException($result->get_error_message());}
foreach([$home_id,1039] as $id){clean_post_cache($id);if(class_exists('Elementor\\Core\\Files\\CSS\\Post')){$css=new \Elementor\Core\Files\CSS\Post($id);$css->delete();}}
echo wp_json_encode(['updated'=>[$home_id,1039],'backup'=>$backup_file,'individualFee'=>'https://rzp.io/l/AIMERSIndividualFee','institutionalFee'=>'https://rzp.io/l/AIMERSEduMembershipFee']);
