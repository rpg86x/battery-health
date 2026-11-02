package com.wakelight;
import android.app.*;
import android.content.*;
import android.os.Build;
import java.util.TimeZone;
final class Schedule {
 static android.content.SharedPreferences prefs(Context c){return c.getSharedPreferences("wake",0);}
 static void initialize(Context c){
  if(!prefs(c).getBoolean("simple12",false)) prefs(c).edit().putBoolean("simple12",true).putBoolean("enabled",true).putInt("days",WeeklySchedule.DEFAULT_DAYS).putInt("hour",7).putInt("minute",15).putBoolean("useLevels",false).putBoolean("running",false).remove("error").commit();
  if(!prefs(c).getBoolean("friday12",false)) prefs(c).edit().putBoolean("friday12",true).putInt("days",prefs(c).getInt("days",30)|32).putInt("fridayHour",8).putInt("fridayMinute",0).commit();
  if(!prefs(c).getBoolean("sunday12",false)) prefs(c).edit().putBoolean("sunday12",true).putInt("days",prefs(c).getInt("days",62)|1).putInt("sundayHour",9).putInt("sundayMinute",0).commit();
 }
 static PendingIntent operation(Context c){return PendingIntent.getBroadcast(c,1,new Intent(c,AlarmReceiver.class),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);}
 static boolean canSchedule(Context c){return Build.VERSION.SDK_INT<31 || c.getSystemService(AlarmManager.class).canScheduleExactAlarms();}
 static long reschedule(Context c){
  AlarmManager manager=c.getSystemService(AlarmManager.class);manager.cancel(operation(c));
  if(!prefs(c).getBoolean("enabled",false)){prefs(c).edit().remove("next").remove("scheduleError").apply();return 0;}
  if(!canSchedule(c)){prefs(c).edit().remove("next").putString("scheduleError","Sta exacte alarmen toe om 07:15 betrouwbaar in te plannen.").apply();return 0;}
  long next=WeeklySchedule.next(System.currentTimeMillis(),prefs(c).getInt("days",63),prefs(c).getInt("hour",7),prefs(c).getInt("minute",15),prefs(c).getInt("fridayHour",8),prefs(c).getInt("fridayMinute",0),prefs(c).getInt("sundayHour",9),prefs(c).getInt("sundayMinute",0),TimeZone.getDefault());
  if(next==0){prefs(c).edit().remove("next").putString("scheduleError","Kies minimaal één dag.").apply();return 0;}
  try {
   PendingIntent show=PendingIntent.getActivity(c,0,new Intent(c,MainActivity.class),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);
   manager.setAlarmClock(new AlarmManager.AlarmClockInfo(next,show),operation(c));
   prefs(c).edit().putLong("next",next).remove("scheduleError").apply();return next;
  }catch(Exception e){prefs(c).edit().remove("next").putString("scheduleError","Het alarm kon niet worden ingepland. Controleer alarmtoegang.").apply();return 0;}
 }
 static void verifyAlarm(Context c,int seconds){
  if(seconds<10 || seconds>60 || !canSchedule(c))return;
  PendingIntent test=PendingIntent.getBroadcast(c,99,new Intent(c,AlarmReceiver.class).setAction("VERIFY"),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);
  c.getSystemService(AlarmManager.class).setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP,System.currentTimeMillis()+seconds*1000L,test);
  android.util.Log.i("Wakeify","VERIFY scheduled seconds="+seconds);
 }
}
