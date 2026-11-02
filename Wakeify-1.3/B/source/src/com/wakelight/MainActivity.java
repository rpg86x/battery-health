package com.wakelight;
import android.app.*;
import android.content.*;
import android.content.pm.*;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.os.*;
import android.provider.Settings;
import android.view.*;
import android.widget.*;
import java.util.*;
public class MainActivity extends Activity {
 LinearLayout content;TextView status;int page=1;final Handler handler=new Handler(Looper.getMainLooper());final int mint=0xffa7edcc,white=0xffeff7f4;
 int dp(int n){return (int)(n*getResources().getDisplayMetrics().density+.5f);}
 TextView text(String value,int size,int color){TextView t=new TextView(this);t.setText(value);t.setTextSize(size);t.setTextColor(color);t.setPadding(0,dp(7),0,dp(7));return t;}
 GradientDrawable shape(int color){GradientDrawable d=new GradientDrawable();d.setColor(color);d.setCornerRadius(dp(18));return d;}
 Button makeButton(String value,View.OnClickListener click){Button b=new Button(this);b.setText(value);b.setTextColor(white);b.setAllCaps(false);b.setTextSize(17);b.setMinHeight(dp(54));b.setBackground(shape(0xff233c3a));b.setOnClickListener(click);return b;}
 Button button(String value,View.OnClickListener click){Button b=makeButton(value,click);LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(-1,-2);p.setMargins(0,dp(7),0,dp(7));content.addView(b,p);return b;}
 public void onCreate(Bundle saved){super.onCreate(saved);Schedule.initialize(this);if(saved!=null)page=saved.getInt("page",1);boolean debug=(getApplicationInfo().flags & ApplicationInfo.FLAG_DEBUGGABLE)!=0;int checkPage=getIntent().getIntExtra("verifyPage",0);boolean checkMusic=getIntent().getBooleanExtra("verifyMusic",false);getIntent().removeExtra("verifyPage");getIntent().removeExtra("verifyMusic");if(debug && checkPage>=1 && checkPage<=3)page=checkPage;show();if(debug && checkMusic)startForegroundService(new Intent(this,LightService.class).setAction("PLAY_ONLY"));}
 void show(){
  LinearLayout root=new LinearLayout(this);root.setOrientation(1);root.setBackgroundColor(0xff101c24);root.setPadding(dp(20),dp(16),dp(20),dp(14));setContentView(root);
  TextView heading=text("Wakeify 1.3",28,white);heading.setTypeface(null,Typeface.BOLD);root.addView(heading);
  ScrollView scroll=new ScrollView(this);root.addView(scroll,new LinearLayout.LayoutParams(-1,0,1));content=new LinearLayout(this);content.setOrientation(1);scroll.addView(content);
  if(page==1){
   content.addView(text("Licht om wakker te worden",23,white));content.addView(text("1.2 blijft apart beschikbaar. Stel dit nieuwe schema in op pagina 3.",14,0xffa6bdba));
   status=text("",17,mint);status.setPadding(dp(15),dp(15),dp(15),dp(15));status.setBackground(shape(0xff1d3037));content.addView(status);
   button("Zaklamp aan",v->{if(cameraReady()){startForegroundService(new Intent(this,LightService.class));}});
   button("Alles stoppen · licht en muziek",v->{MusicApps.pause(this);stopService(new Intent(this,LightService.class));try{new Torch(this).off();}catch(Exception ignored){}refresh();});
   content.addView(text("Zaklamp uit na 10 minuten. De muziek kan later starten, ook bij 15–30 minuten vertraging.",15,0xffa6bdba));
  }else if(page==2){
   content.addView(text("Muziek na het licht",24,white));
   Switch music=new Switch(this);music.setText("Muziek automatisch starten");music.setTextColor(white);music.setChecked(Schedule.prefs(this).getBoolean("musicEnabled",true));content.addView(music);music.setOnCheckedChangeListener((view,on)->Schedule.prefs(this).edit().putBoolean("musicEnabled",on).apply());
   content.addView(text("Gekozen app: "+MusicApps.label(this,MusicApps.selected(this)),17,mint));
   content.addView(text("Start na "+Schedule.prefs(this).getInt("musicLag",5)+" minuten. Wijzig de vertraging op pagina 3.",16,white));
   button("Muziek-app kiezen",v->{List<String> choices=MusicApps.choices(this);if(choices.isEmpty()){Toast.makeText(this,"Geen muziek-app gevonden",Toast.LENGTH_LONG).show();return;}String[] labels=new String[choices.size()];for(int n=0;n<labels.length;n++)labels[n]=MusicApps.label(this,choices.get(n));new AlertDialog.Builder(this).setTitle("Muziek-app").setItems(labels,(d,n)->{Schedule.prefs(this).edit().putString("musicPackage",choices.get(n)).apply();show();}).show();});
   button("1 · Meldingstoegang instellen",v->startActivity(new Intent(Settings.ACTION_NOTIFICATION_LISTENER_SETTINGS)));
   button("2 · Muziek-app openen",v->{Intent launch=getPackageManager().getLaunchIntentForPackage(MusicApps.selected(this));if(launch!=null)startActivity(launch);else Toast.makeText(this,"De gekozen app is niet geïnstalleerd",Toast.LENGTH_LONG).show();});
   button("3 · Muziekkoppeling testen",v->{if(Schedule.prefs(this).getBoolean("running",false)){Toast.makeText(this,"Stop eerst de actieve lichtreeks",Toast.LENGTH_LONG).show();return;}startForegroundService(new Intent(this,LightService.class).setAction("PLAY_ONLY"));});
   button("Muziek pauzeren",v->MusicApps.pause(this));
   content.addView(text("Kies een nummer of playlist, speel die kort af en pauzeer. Wakeify hervat de beschikbare afspeelsessie. Stel het mediavolume in de muziek-app in. Meldingstoegang is nodig voor bediening; Wakeify bewaart geen meldingsinhoud.",14,0xffa6bdba));
   status=text("",15,mint);content.addView(status);
  }else{
   content.addView(text("Instellingen",24,white));
   button("Muziekvertraging · "+Schedule.prefs(this).getInt("musicLag",5)+" min",v->{String[] values={"0 min","5 min","10 min","15 min","20 min","25 min","30 min"};new AlertDialog.Builder(this).setTitle("Na de zaklamp").setSingleChoiceItems(values,WakeTimeline.validLag(Schedule.prefs(this).getInt("musicLag",5))/5,(d,n)->{Schedule.prefs(this).edit().putInt("musicLag",n*5).apply();d.dismiss();show();}).setNegativeButton("Terug",null).show();});
   content.addView(text("Weekschema",21,white));Switch alarm=new Switch(this);alarm.setText("Lichtalarm in 1.3 aan");alarm.setTextColor(white);alarm.setChecked(Schedule.prefs(this).getBoolean("enabled",false));content.addView(alarm);alarm.setOnCheckedChangeListener((v,on)->{if(on && !cameraReady()){alarm.setChecked(false);return;}Schedule.prefs(this).edit().putBoolean("enabled",on).commit();Schedule.reschedule(this);refresh();});
   content.addView(text("Schakel het schema van 1.2 uit als je voortaan 1.3 gebruikt, zodat beide apps niet tegelijk starten.",14,0xffa6bdba));
   timeButton("Standaardtijd","hour","minute",7,15);timeButton("Vrijdag","fridayHour","fridayMinute",8,0);timeButton("Zondag","sundayHour","sundayMinute",9,0);
   String[] labels={"Maandag","Dinsdag","Woensdag","Donderdag","Vrijdag","Zaterdag","Zondag"};int[] values={2,3,4,5,6,7,1};for(int n=0;n<7;n++){int bit=1<<(values[n]-1);CheckBox box=new CheckBox(this);box.setText(labels[n]);box.setTextColor(white);box.setMinHeight(dp(44));box.setChecked((Schedule.prefs(this).getInt("days",63)&bit)!=0);content.addView(box);box.setOnCheckedChangeListener((v,on)->{int mask=Schedule.prefs(this).getInt("days",63);Schedule.prefs(this).edit().putInt("days",on?mask|bit:mask&~bit).commit();Schedule.reschedule(this);refresh();});}
   content.addView(text("Wijzigingen worden direct opgeslagen. Vrijdag en zondag hebben een eigen tijd.",14,0xffa6bdba));
   if(!Schedule.canSchedule(this))button("Exacte alarmen toestaan",v->startActivity(new Intent(Settings.ACTION_REQUEST_SCHEDULE_EXACT_ALARM,Uri.parse("package:"+getPackageName()))));
   status=text("",15,mint);content.addView(status);
  }
  LinearLayout nav=new LinearLayout(this);nav.setGravity(Gravity.CENTER_VERTICAL);Button previous=makeButton("<",v->{page--;show();});previous.setContentDescription("Vorige pagina");previous.setEnabled(page>1);nav.addView(previous,new LinearLayout.LayoutParams(dp(64),dp(52)));TextView number=text(page+" / 3",16,mint);number.setGravity(Gravity.CENTER);nav.addView(number,new LinearLayout.LayoutParams(0,-2,1));Button next=makeButton(">",v->{page++;show();});next.setContentDescription("Volgende pagina");next.setEnabled(page<3);nav.addView(next,new LinearLayout.LayoutParams(dp(64),dp(52)));root.addView(nav);refresh();
 }
 void timeButton(String title,String hourKey,String minuteKey,int h,int m){int hour=Schedule.prefs(this).getInt(hourKey,h),minute=Schedule.prefs(this).getInt(minuteKey,m);button(String.format(Locale.getDefault(),"%s · %02d:%02d",title,hour,minute),v->new TimePickerDialog(this,(picker,hh,mm)->{Schedule.prefs(this).edit().putInt(hourKey,hh).putInt(minuteKey,mm).commit();Schedule.reschedule(this);show();},hour,minute,true).show());}
 boolean cameraReady(){if(checkSelfPermission("android.permission.CAMERA")!=PackageManager.PERMISSION_GRANTED){requestPermissions(new String[]{"android.permission.CAMERA"},1);return false;}return true;}
 void refresh(){if(status==null)return;long next=Schedule.prefs(this).getLong("next",0);String value=page==2?MusicApps.ready(this):(Schedule.prefs(this).getBoolean("torchOn",false)?"● Zaklamp aan":"○ Zaklamp uit");value+="\n\n"+Schedule.prefs(this).getString("musicStatus","Muziekvertraging: "+Schedule.prefs(this).getInt("musicLag",5)+" minuten");if(page!=2)value+="\n\n"+(next>System.currentTimeMillis()?"Volgende keer: "+android.text.format.DateFormat.format("EEEE d MMMM · HH:mm",next):"Weekschema van 1.3 staat uit.");String error=Schedule.prefs(this).getString("error",Schedule.prefs(this).getString("scheduleError",null));if(error!=null)value+="\n\n"+error;status.setText(value);}
 final Runnable poll=new Runnable(){public void run(){refresh();handler.postDelayed(this,1000);}};
 protected void onResume(){super.onResume();Schedule.reschedule(this);handler.removeCallbacks(poll);handler.post(poll);}
 protected void onPause(){handler.removeCallbacks(poll);super.onPause();}
 protected void onSaveInstanceState(Bundle saved){saved.putInt("page",page);super.onSaveInstanceState(saved);}
 public void onRequestPermissionsResult(int r,String[] p,int[] g){super.onRequestPermissionsResult(r,p,g);show();}
 public void onBackPressed(){if(page>1){page--;show();}else super.onBackPressed();}
}
