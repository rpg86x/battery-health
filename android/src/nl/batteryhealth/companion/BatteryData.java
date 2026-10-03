package nl.batteryhealth.companion;

import android.content.*;
import android.net.*;
import android.os.*;
import org.json.JSONObject;
import java.io.*;
import java.net.*;
import java.security.SecureRandom;
import java.util.*;

final class BatteryData {
    static SharedPreferences prefs(Context context) { return context.getSharedPreferences("battery", Context.MODE_PRIVATE); }
    static synchronized String token(Context context) {
        SharedPreferences prefs = prefs(context); String key = prefs.getString("token", "");
        if (!key.matches("[a-f0-9]{32}")) {
            byte[] bytes = new byte[16]; new SecureRandom().nextBytes(bytes); StringBuilder text = new StringBuilder();
            for (byte b : bytes) text.append(String.format(Locale.ROOT, "%02x", b & 255));
            key = text.toString(); prefs.edit().putString("token", key).commit();
        }
        return key;
    }
    static synchronized String identity(Context context) {
        SharedPreferences p = prefs(context); String value = p.getString("identity", "");
        if (value.isEmpty()) { value = UUID.randomUUID().toString(); p.edit().putString("identity", value).commit(); }
        return value;
    }
    static Double capacity(String field) {
        try (BufferedReader reader = new BufferedReader(new FileReader("/sys/class/power_supply/battery/" + field))) {
            double value = Double.parseDouble(reader.readLine());
            return value >= 100000 && value <= 50000000 ? value / 1000 : null;
        } catch (Exception missing) { return null; }
    }
    static Long cycles(Intent battery) {
        if (Build.VERSION.SDK_INT >= 34 && battery.hasExtra(BatteryManager.EXTRA_CYCLE_COUNT)) {
            int value = battery.getIntExtra(BatteryManager.EXTRA_CYCLE_COUNT, -1);
            if (value >= 0 && value < Integer.MAX_VALUE) return (long)value;
        }
        try (BufferedReader reader = new BufferedReader(new FileReader("/sys/class/power_supply/battery/cycle_count"))) {
            long value = Long.parseLong(reader.readLine()); return value >= 0 && value < Integer.MAX_VALUE ? value : null;
        } catch (Exception missing) { return null; }
    }
    static JSONObject snapshot(Context context) throws Exception {
        Intent b = context.registerReceiver(null, new IntentFilter(Intent.ACTION_BATTERY_CHANGED));
        if (b == null || !b.getBooleanExtra(BatteryManager.EXTRA_PRESENT, true)) throw new IOException("Battery not available");
        BatteryManager manager = (BatteryManager)context.getSystemService(Context.BATTERY_SERVICE);
        int scale = b.getIntExtra(BatteryManager.EXTRA_SCALE, -1), level = b.getIntExtra(BatteryManager.EXTRA_LEVEL, -1);
        int status = b.getIntExtra(BatteryManager.EXTRA_STATUS, BatteryManager.BATTERY_STATUS_UNKNOWN);
        int temp = b.getIntExtra(BatteryManager.EXTRA_TEMPERATURE, Integer.MIN_VALUE);
        Double design = capacity("charge_full_design"), full = capacity("charge_full");
        Long runtime = null;
        // Remaining charge / average discharge current; not a full-capacity measurement.
        if (status == BatteryManager.BATTERY_STATUS_DISCHARGING && manager != null) {
            int charge = manager.getIntProperty(BatteryManager.BATTERY_PROPERTY_CHARGE_COUNTER);
            int average = manager.getIntProperty(BatteryManager.BATTERY_PROPERTY_CURRENT_AVERAGE);
            if (charge > 0 && average < -1000 && average != Integer.MIN_VALUE) {
                double seconds = charge / -(double)average * 3600;
                if (seconds >= 0 && seconds <= 604800) runtime = (long)seconds;
            }
        }
        JSONObject json = new JSONObject();
        json.put("protocol", 1); json.put("identity", identity(context)); json.put("model", Build.MANUFACTURER + " " + Build.MODEL);
        json.put("level", scale > 0 && level >= 0 && level <= scale ? level * 100.0 / scale : JSONObject.NULL);
        json.put("status", status); json.put("health", b.getIntExtra(BatteryManager.EXTRA_HEALTH, 1));
        json.put("temperatureC", temp >= -500 && temp <= 1500 ? temp / 10.0 : JSONObject.NULL);
        json.put("designMah", design == null ? JSONObject.NULL : design); json.put("fullMah", full == null ? JSONObject.NULL : full);
        Long cycles = cycles(b); json.put("cycles", cycles == null ? JSONObject.NULL : cycles);
        json.put("runtimeSeconds", runtime == null ? JSONObject.NULL : runtime); json.put("runtimeMethod", "remaining-charge-over-average-current");
        json.put("sampledAt", System.currentTimeMillis()); return json;
    }
    static String address(Context context) {
        try {
            ConnectivityManager cm = (ConnectivityManager)context.getSystemService(Context.CONNECTIVITY_SERVICE);
            for (Network network : cm.getAllNetworks()) {
                NetworkCapabilities caps = cm.getNetworkCapabilities(network);
                if (caps == null || !caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)) continue;
                LinkProperties links = cm.getLinkProperties(network); if (links == null) continue;
                for (LinkAddress link : links.getLinkAddresses()) {
                    InetAddress ip = link.getAddress();
                    if (ip instanceof Inet4Address && !ip.isLoopbackAddress() && (ip.isSiteLocalAddress() || ip.isLinkLocalAddress())) return ip.getHostAddress() + ":8765";
                }
            }
        } catch (Exception missing) { }
        return "";
    }
}
