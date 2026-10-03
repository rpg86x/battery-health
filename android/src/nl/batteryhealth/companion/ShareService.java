package nl.batteryhealth.companion;

import android.app.*;
import android.content.*;
import android.content.pm.ServiceInfo;
import android.os.*;

public final class ShareService extends Service {
    public static volatile boolean running;
    public static volatile boolean failed;
    private LocalBatteryServer server;
    public IBinder onBind(Intent intent) { return null; }
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent != null && "stop".equals(intent.getAction())) { stopSelf(); return START_NOT_STICKY; }
        if (server != null) return START_NOT_STICKY;
        failed = false;
        boolean english = BatteryData.prefs(this).getBoolean("english", false);
        NotificationManager manager = (NotificationManager)getSystemService(NOTIFICATION_SERVICE);
        manager.createNotificationChannel(new NotificationChannel("sharing", english ? "Battery sharing" : "Batterij delen", NotificationManager.IMPORTANCE_LOW));
        PendingIntent open = PendingIntent.getActivity(this, 0, new Intent(this, MainActivity.class), PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);
        PendingIntent stop = PendingIntent.getService(this, 1, new Intent(this, ShareService.class).setAction("stop"), PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);
        Notification notification = new Notification.Builder(this, "sharing").setSmallIcon(R.drawable.battery_icon)
            .setContentTitle("Battery Health").setContentText(english ? "Sharing with Windows on your local network" : "Delen met Windows op je lokale netwerk")
            .setContentIntent(open).addAction(new Notification.Action.Builder(null, english ? "Stop sharing" : "Stop delen", stop).build()).setOngoing(true).build();
        if (Build.VERSION.SDK_INT >= 34) startForeground(10, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE);
        else startForeground(10, notification);
        try {
            server = new LocalBatteryServer(8765, BatteryData.token(this), new LocalBatteryServer.Source() {
                public String snapshot() throws Exception { return BatteryData.snapshot(ShareService.this).toString(); }
            });
            running = true;
        } catch (Exception error) { failed = true; running = false; stopSelf(); }
        return START_NOT_STICKY;
    }
    public void onDestroy() {
        running = false; if (server != null) { server.close(); server = null; }
        stopForeground(STOP_FOREGROUND_REMOVE); super.onDestroy();
    }
}
