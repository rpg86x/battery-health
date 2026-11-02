package com.wakelight;
import android.content.*;
public class AlarmReceiver extends BroadcastReceiver {
 public void onReceive(Context context,Intent intent){
  boolean verify="VERIFY".equals(intent.getAction());
  if(!verify && !Schedule.prefs(context).getBoolean("enabled",false))return;
  if(!verify)Schedule.reschedule(context);
  android.util.Log.i("Wakeify","ALARM verify="+verify);
  try{context.startForegroundService(new Intent(context,LightService.class).putExtra("verify",verify));}
  catch(Exception e){Schedule.prefs(context).edit().putString("error","Android kon de zaklamp niet starten vanuit het alarm.").apply();android.util.Log.e("Wakeify","ALARM failed "+e.getClass().getSimpleName());}
 }
}
