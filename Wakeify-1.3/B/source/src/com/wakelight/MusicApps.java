package com.wakelight;
import android.app.NotificationManager;
import android.content.*;
import android.content.pm.*;
import android.media.session.*;
import android.os.Build;
import android.provider.Settings;
import java.util.*;
final class MusicApps {
 static ComponentName listener(Context c){return new ComponentName(c,MusicAccess.class);}
 static boolean allowed(Context c){
  if(Build.VERSION.SDK_INT>=27)return c.getSystemService(NotificationManager.class).isNotificationListenerAccessGranted(listener(c));
  String enabled=Settings.Secure.getString(c.getContentResolver(),"enabled_notification_listeners");
  return enabled!=null && Arrays.asList(enabled.split(":")).contains(listener(c).flattenToString());
 }
 static String selected(Context c){return Schedule.prefs(c).getString("musicPackage","com.spotify.music");}
 static MediaController controller(Context c){
  if(!allowed(c))return null;
  try{for(MediaController controller:c.getSystemService(MediaSessionManager.class).getActiveSessions(listener(c)))if(selected(c).equals(controller.getPackageName()))return controller;}catch(SecurityException ignored){}
  return null;
 }
 static String label(Context c,String pkg){try{return c.getPackageManager().getApplicationLabel(c.getPackageManager().getApplicationInfo(pkg,0)).toString();}catch(Exception e){return pkg;}}
 static String ready(Context c){if(!allowed(c))return "Verbind eerst je muziek-app via meldingstoegang.";return controller(c)==null?"Open "+label(c,selected(c))+", start je muziek en pauzeer die. Test daarna de koppeling.":label(c,selected(c))+" heeft een beschikbare afspeelsessie.";}
 static List<String> choices(Context c){
  LinkedHashSet<String> choices=new LinkedHashSet<>();
  for(String pkg:new String[]{"com.spotify.music","com.sec.android.app.music"})try{c.getPackageManager().getApplicationInfo(pkg,0);choices.add(pkg);}catch(Exception ignored){}
  for(ResolveInfo info:c.getPackageManager().queryIntentActivities(new Intent("android.media.action.MEDIA_PLAY_FROM_SEARCH"),0))choices.add(info.activityInfo.packageName);
  if(allowed(c))try{for(MediaController item:c.getSystemService(MediaSessionManager.class).getActiveSessions(listener(c)))choices.add(item.getPackageName());}catch(Exception ignored){}
  return new ArrayList<>(choices);
 }
 static void pause(Context c){try{MediaController controller=controller(c);if(controller!=null)controller.getTransportControls().pause();}catch(Exception ignored){}}
}
