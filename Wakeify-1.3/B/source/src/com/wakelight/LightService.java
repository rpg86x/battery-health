package com.wakelight;
import android.app.*;
import android.content.*;
import android.hardware.camera2.CameraManager;
import android.media.session.*;
import android.os.*;
public class LightService extends Service {
 final Handler handler=new Handler(Looper.getMainLooper());Torch torch;PowerManager.WakeLock lock;
 CameraManager.TorchCallback callback;MediaController music;MediaController.Callback musicCallback;
 boolean started,lightDone,musicDone;long musicAt;
 public void onCreate(){super.onCreate();getSystemService(NotificationManager.class).createNotificationChannel(new NotificationChannel("light13","Wakeify licht en muziek",NotificationManager.IMPORTANCE_LOW));}
 Notification note(){
  PendingIntent stop=PendingIntent.getService(this,2,new Intent(this,LightService.class).setAction("STOP"),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);
  PendingIntent open=PendingIntent.getActivity(this,0,new Intent(this,MainActivity.class),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);
  String text=Schedule.prefs(this).getString("musicStatus","Zaklamp actief");
  return new Notification.Builder(this,"light13").setSmallIcon(android.R.drawable.ic_lock_idle_alarm).setContentTitle("Wakeify 1.3").setContentText(text).setContentIntent(open).setOngoing(true).addAction(android.R.drawable.ic_delete,"Alles stoppen",stop).build();
 }
 void status(String value){Schedule.prefs(this).edit().putString("musicStatus",value).apply();getSystemService(NotificationManager.class).notify(1,note());}
 public int onStartCommand(Intent intent,int flags,int startId){
  startForeground(1,note());
  if(intent!=null && "STOP".equals(intent.getAction())){MusicApps.pause(this);stopSelf();return START_NOT_STICKY;}
  if(started)return START_NOT_STICKY;started=true;
  boolean preview=intent!=null && "PLAY_ONLY".equals(intent.getAction());
  boolean verify=intent!=null && intent.getBooleanExtra("verify",false);
  boolean withMusic=!verify && (preview || Schedule.prefs(this).getBoolean("musicEnabled",true));
  int lag=WakeTimeline.validLag(Schedule.prefs(this).getInt("musicLag",5));
  long delay=preview?0:WakeTimeline.musicDelay(lag);
  lightDone=preview;musicDone=!withMusic;
  try{
   if(!preview){
    torch=new Torch(this);
    callback=new CameraManager.TorchCallback(){public void onTorchModeChanged(String id,boolean on){if(torch!=null && torch.id.equals(id)){Schedule.prefs(LightService.this).edit().putBoolean("torchOn",on).apply();android.util.Log.i("Wakeify13","TORCH="+on);}}};
    torch.manager.registerTorchCallback(callback,handler);torch.on();
    handler.postDelayed(()->{torch.off();lightDone=true;Schedule.prefs(this).edit().putBoolean("torchOn",false).apply();finishIfDone();},verify?5000:WakeTimeline.LIGHT_MS);
   }
   long end=verify?5000:preview?12000:WakeTimeline.end(withMusic,lag);
   lock=getSystemService(PowerManager.class).newWakeLock(PowerManager.PARTIAL_WAKE_LOCK,"Wakeify13:timeline");lock.acquire(end+30000L);
   Schedule.prefs(this).edit().putBoolean("running",true).putLong("started",System.currentTimeMillis()).remove("error").apply();
   if(withMusic){musicAt=System.currentTimeMillis()+delay;Schedule.prefs(this).edit().putLong("musicAt",musicAt).apply();status(delay==0?"Muziek starten…":"Muziek over "+lag+" minuten");handler.postDelayed(()->startMusic(),delay);}
   else status("Alleen zaklamp · automatisch uit na 10 minuten");
   android.util.Log.i("Wakeify13","START music="+withMusic+" delayMs="+delay);
  }catch(Exception e){Schedule.prefs(this).edit().putString("error","Zaklamp niet gestart. Controleer cameratoegang en andere camera-apps.").apply();stopSelf();}
  return START_NOT_STICKY;
 }
 void startMusic(){
  music=MusicApps.controller(this);
  if(music==null){status(MusicApps.ready(this));Schedule.prefs(this).edit().putString("error",MusicApps.ready(this)).apply();musicDone=true;finishIfDone();return;}
  musicCallback=new MediaController.Callback(){public void onPlaybackStateChanged(PlaybackState state){if(state!=null && state.getState()==PlaybackState.STATE_PLAYING)played();}};
  music.registerCallback(musicCallback,handler);
  try{status("Start aangevraagd bij "+MusicApps.label(this,MusicApps.selected(this)));music.getTransportControls().play();}
  catch(Exception e){musicDone=true;status("De muziek-app accepteert de startopdracht niet.");finishIfDone();return;}
  handler.postDelayed(()->{
   if(musicDone)return;PlaybackState current=music.getPlaybackState();
   if(current!=null && current.getState()==PlaybackState.STATE_PLAYING)played();
   else{musicDone=true;String error="Muziekstart niet bevestigd. Controleer de gekozen muziek-app.";status(error);Schedule.prefs(this).edit().putString("error",error).apply();finishIfDone();}
  },10000);
 }
 void played(){if(musicDone)return;musicDone=true;status(MusicApps.label(this,MusicApps.selected(this))+" speelt muziek");Schedule.prefs(this).edit().putLong("musicStarted",System.currentTimeMillis()).remove("error").apply();android.util.Log.i("Wakeify13","PLAYBACK_CONFIRMED");finishIfDone();}
 void finishIfDone(){if(lightDone && musicDone)stopSelf();}
 public void onDestroy(){
  handler.removeCallbacksAndMessages(null);
  if(torch!=null){torch.off();if(callback!=null)torch.manager.unregisterTorchCallback(callback);}
  if(music!=null && musicCallback!=null)music.unregisterCallback(musicCallback);
  if(lock!=null && lock.isHeld())lock.release();
  Schedule.prefs(this).edit().putBoolean("running",false).putBoolean("torchOn",false).remove("musicAt").putLong("ended",System.currentTimeMillis()).apply();
  super.onDestroy();
 }
 public IBinder onBind(Intent intent){return null;}
}
