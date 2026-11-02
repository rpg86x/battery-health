package com.wakelight;
import android.content.*;
public class RestoreReceiver extends BroadcastReceiver {
 public void onReceive(Context context,Intent intent){
  if(Intent.ACTION_BOOT_COMPLETED.equals(intent.getAction()))Schedule.prefs(context).edit().putBoolean("running",false).putBoolean("torchOn",false).apply();
  Schedule.reschedule(context);
 }
}
