package com.wakelight;
import android.app.*;
import android.content.*;
import android.hardware.camera2.CameraManager;
import android.os.*;
public class LightService extends Service {
 final Handler handler=new Handler(Looper.getMainLooper());Torch torch;PowerManager.WakeLock lock;
 CameraManager.TorchCallback callback;
 public void onCreate(){super.onCreate();getSystemService(NotificationManager.class).createNotificationChannel(new NotificationChannel("light","Wakeify zaklamp",NotificationManager.IMPORTANCE_LOW));}
 Notification note(){
  PendingIntent stop=PendingIntent.getService(this,2,new Intent(this,LightService.class).setAction("STOP"),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);
  PendingIntent open=PendingIntent.getActivity(this,0,new Intent(this,MainActivity.class),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);
  return new Notification.Builder(this,"light").setSmallIcon(android.R.drawable.ic_lock_idle_alarm).setContentTitle("Wakeify · zaklamp aan").setContentText("Gaat automatisch na 10 minuten uit").setContentIntent(open).setOngoing(true).addAction(android.R.drawable.ic_delete,"Zaklamp uit",stop).build();
 }
 public int onStartCommand(Intent intent,int flags,int startId){
  startForeground(1,note());
  if(intent!=null && "STOP".equals(intent.getAction())){stopSelf();return START_NOT_STICKY;}
  if(torch!=null)return START_NOT_STICKY;
  try{
   long duration=intent!=null && intent.getBooleanExtra("verify",false)?5000L:600000L;
   torch=new Torch(this);
   callback=new CameraManager.TorchCallback(){public void onTorchModeChanged(String id,boolean on){if(torch!=null && torch.id.equals(id)){Schedule.prefs(LightService.this).edit().putBoolean("torchOn",on).apply();android.util.Log.i("Wakeify","TORCH="+on);}}};
   torch.manager.registerTorchCallback(callback,handler);
   lock=getSystemService(PowerManager.class).newWakeLock(PowerManager.PARTIAL_WAKE_LOCK,"Wakeify:flashlight");lock.acquire(duration+30000L);
   torch.on();Schedule.prefs(this).edit().putBoolean("running",true).putLong("started",System.currentTimeMillis()).remove("error").apply();
   android.util.Log.i("Wakeify","START fixed duration="+duration);handler.postDelayed(()->stopSelf(),duration);
  }catch(Exception e){Schedule.prefs(this).edit().putString("error","Zaklamp niet gestart. Controleer cameratoegang en sluit andere camera-apps.").apply();android.util.Log.e("Wakeify","START failed "+e.getClass().getSimpleName());stopSelf();}
  return START_NOT_STICKY;
 }
 public void onDestroy(){
  handler.removeCallbacksAndMessages(null);
  if(torch!=null){torch.off();if(callback!=null)torch.manager.unregisterTorchCallback(callback);}
  if(lock!=null && lock.isHeld())lock.release();
  Schedule.prefs(this).edit().putBoolean("running",false).putBoolean("torchOn",false).putLong("ended",System.currentTimeMillis()).apply();
  android.util.Log.i("Wakeify","STOP fixed");super.onDestroy();
 }
 public IBinder onBind(Intent intent){return null;}
}
