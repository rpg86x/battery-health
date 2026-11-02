package com.wakelight;
import android.app.*;
import android.content.*;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.os.*;
import android.provider.Settings;
import android.view.*;
import android.widget.*;
public class MainActivity extends Activity {
 LinearLayout content;TextView status;int page=1;TimePicker picker;CheckBox[] days;Switch enabled;int fridayHour,fridayMinute,sundayHour,sundayMinute;Button fridayButton,sundayButton;
 final Handler handler=new Handler(Looper.getMainLooper());final int mint=0xffa7edcc,white=0xffeff7f4;
 int dp(int value){return (int)(value*getResources().getDisplayMetrics().density+.5f);}
 TextView text(String value,int size,int color){TextView t=new TextView(this);t.setText(value);t.setTextSize(size);t.setTextColor(color);t.setPadding(0,dp(6),0,dp(6));return t;}
 GradientDrawable shape(int color){GradientDrawable d=new GradientDrawable();d.setColor(color);d.setCornerRadius(dp(18));return d;}
 Button button(String value,View.OnClickListener click){Button b=new Button(this);b.setText(value);b.setTextColor(white);b.setAllCaps(false);b.setTextSize(17);b.setMinHeight(dp(58));b.setBackground(shape(0xff233c3a));LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(-1,-2);p.setMargins(0,dp(8),0,dp(8));content.addView(b,p);b.setOnClickListener(click);return b;}
 public void onCreate(Bundle saved){super.onCreate(saved);Schedule.initialize(this);if(saved!=null)page=saved.getInt("page",1);show();
  int seconds=getIntent().getIntExtra("verifyAlarmSeconds",0);getIntent().removeExtra("verifyAlarmSeconds");if((getApplicationInfo().flags & ApplicationInfo.FLAG_DEBUGGABLE)!=0)Schedule.verifyAlarm(this,seconds);
 }
 void show(){
  ScrollView scroll=new ScrollView(this);scroll.setFillViewport(true);scroll.setBackgroundColor(0xff101c24);
  content=new LinearLayout(this);content.setOrientation(1);content.setPadding(dp(24),dp(28),dp(24),dp(28));scroll.addView(content);setContentView(scroll);
  LinearLayout header=new LinearLayout(this);header.setGravity(Gravity.CENTER_VERTICAL);ImageView icon=new ImageView(this);icon.setImageResource(getResources().getIdentifier("wakeify_icon","drawable",getPackageName()));header.addView(icon,new LinearLayout.LayoutParams(dp(52),dp(52)));
  TextView title=text("Wakeify",30,white);title.setTypeface(null,Typeface.BOLD);title.setPadding(dp(12),0,0,0);header.addView(title);content.addView(header);
  content.addView(text("1.2 · zaklampwekker",14,mint));
  if(page==1){
   content.addView(text("Goedemorgen, met licht.",25,white));
   content.addView(text("Eenvoudig aan of uit. Geen helderheidsstanden en geen geluid.",16,0xffa6bdba));
   status=text("",17,mint);status.setPadding(dp(18),dp(18),dp(18),dp(18));status.setBackground(shape(0xff1d3037));content.addView(status);
   button("Zaklamp aan",v->{if(cameraReady()){startForegroundService(new Intent(this,LightService.class));refresh();}});
   button("Zaklamp uit",v->{stopService(new Intent(this,LightService.class));try{new Torch(this).off();}catch(Exception ignored){}Schedule.prefs(this).edit().putBoolean("running",false).putBoolean("torchOn",false).apply();refresh();});
   content.addView(text("Het licht gaat na 10 minuten automatisch uit. Uitzetten verandert je weekschema niet.",14,0xffa6bdba));
   button("2 · Dagen en tijd aanpassen  →",v->{page=2;show();});
  }else{
   content.addView(text("Dagen en tijd",26,white));
   enabled=new Switch(this);enabled.setText("Wekelijks lichtalarm aan");enabled.setTextColor(white);enabled.setPadding(0,dp(12),0,dp(12));enabled.setChecked(Schedule.prefs(this).getBoolean("enabled",true));content.addView(enabled);
   content.addView(text("Standaardtijd · vrijdag en zondag hebben een eigen tijd",14,0xffa6bdba));
   picker=new TimePicker(this);picker.setIs24HourView(true);picker.setHour(Schedule.prefs(this).getInt("hour",7));picker.setMinute(Schedule.prefs(this).getInt("minute",15));content.addView(picker);
   fridayHour=Schedule.prefs(this).getInt("fridayHour",8);fridayMinute=Schedule.prefs(this).getInt("fridayMinute",0);
   fridayButton=button(fridayLabel(),v->new TimePickerDialog(this,(view,h,m)->{fridayHour=h;fridayMinute=m;fridayButton.setText(fridayLabel());},fridayHour,fridayMinute,true).show());
   sundayHour=Schedule.prefs(this).getInt("sundayHour",9);sundayMinute=Schedule.prefs(this).getInt("sundayMinute",0);
   sundayButton=button(sundayLabel(),v->new TimePickerDialog(this,(view,h,m)->{sundayHour=h;sundayMinute=m;sundayButton.setText(sundayLabel());},sundayHour,sundayMinute,true).show());
   days=new CheckBox[7];String[] labels={"Maandag","Dinsdag","Woensdag","Donderdag","Vrijdag","Zaterdag","Zondag"};int[] values={2,3,4,5,6,7,1};int mask=Schedule.prefs(this).getInt("days",30);
   for(int n=0;n<7;n++){CheckBox box=new CheckBox(this);box.setText(labels[n]);box.setTextColor(white);box.setTextSize(16);box.setMinHeight(dp(44));box.setTag(values[n]);box.setChecked((mask & (1 << (values[n]-1)))!=0);days[n]=box;content.addView(box);}
   button("Schema opslaan",v->save());
   button("←  1 · Zaklamp",v->{page=1;show();});
   content.addView(text("Licht aan op je gekozen tijd, automatisch uit na 10 minuten. Instellingen blijven bewaard.",14,0xffa6bdba));
   status=text("",15,mint);content.addView(status);
  }
  if(!Schedule.canSchedule(this))button("Exacte alarmen toestaan",v->startActivity(new Intent(Settings.ACTION_REQUEST_SCHEDULE_EXACT_ALARM,Uri.parse("package:"+getPackageName()))));
  if(checkSelfPermission("android.permission.CAMERA")!=PackageManager.PERMISSION_GRANTED)button("Zaklamptoegang toestaan",v->cameraReady());
  refresh();
 }
 boolean cameraReady(){if(checkSelfPermission("android.permission.CAMERA")!=PackageManager.PERMISSION_GRANTED){requestPermissions(new String[]{"android.permission.CAMERA"},1);return false;}return true;}
 String fridayLabel(){return String.format(java.util.Locale.getDefault(),"Vrijdag · %02d:%02d  — aanpassen",fridayHour,fridayMinute);}
 String sundayLabel(){return String.format(java.util.Locale.getDefault(),"Zondag · %02d:%02d  — aanpassen",sundayHour,sundayMinute);}
 void save(){int mask=0;for(CheckBox box:days)if(box.isChecked())mask |= 1 << ((Integer)box.getTag()-1);if(enabled.isChecked()&&mask==0){status.setText("Kies minstens één dag.");return;}Schedule.prefs(this).edit().putInt("days",mask).putInt("hour",picker.getHour()).putInt("minute",picker.getMinute()).putInt("fridayHour",fridayHour).putInt("fridayMinute",fridayMinute).putInt("sundayHour",sundayHour).putInt("sundayMinute",sundayMinute).putBoolean("enabled",enabled.isChecked()).commit();Schedule.reschedule(this);refresh();Toast.makeText(this,"Schema opgeslagen",Toast.LENGTH_SHORT).show();}
 void refresh(){if(status==null)return;long next=Schedule.prefs(this).getLong("next",0);String error=Schedule.prefs(this).getString("error",Schedule.prefs(this).getString("scheduleError",null));
  String value=(Schedule.prefs(this).getBoolean("running",false)?"● Zaklamp aan":"○ Zaklamp uit");
  value+="\n\n"+(next>System.currentTimeMillis()?"Volgende keer\n"+android.text.format.DateFormat.format("EEEE d MMMM · HH:mm",next):"Lichtalarm staat uit of is nog niet ingepland.");
  if(error!=null)value+="\n\n"+error;status.setText(value);
 }
 final Runnable poll=new Runnable(){public void run(){refresh();handler.postDelayed(this,1000);}};
 protected void onResume(){super.onResume();Schedule.reschedule(this);handler.removeCallbacks(poll);handler.post(poll);}
 protected void onPause(){handler.removeCallbacks(poll);super.onPause();}
 protected void onSaveInstanceState(Bundle saved){saved.putInt("page",page);super.onSaveInstanceState(saved);}
 public void onRequestPermissionsResult(int request,String[] permissions,int[] results){super.onRequestPermissionsResult(request,permissions,results);show();}
 public void onBackPressed(){if(page==2){page=1;show();}else super.onBackPressed();}
}
