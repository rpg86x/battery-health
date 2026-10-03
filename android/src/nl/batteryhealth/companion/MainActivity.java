package nl.batteryhealth.companion;

import android.Manifest;
import android.app.Activity;
import android.content.*;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.*;
import android.view.*;
import android.widget.*;
import org.json.JSONObject;
import java.util.Locale;

public final class MainActivity extends Activity {
    private boolean english;
    private TextView heading, model, level, details, address, token, description, sharing, note;
    private Button toggle, copy, refresh, nl, en;
    private final Handler handler = new Handler(Looper.getMainLooper());
    private final Runnable tick = new Runnable() { public void run() { update(); handler.postDelayed(this, 15000); } };
    private String t(String nl, String en) { return english ? en : nl; }
    private int dp(int value) { return (int)(value * getResources().getDisplayMetrics().density + 0.5f); }
    public void onCreate(Bundle state) {
        super.onCreate(state); english = BatteryData.prefs(this).getBoolean("english", false);
        getWindow().setStatusBarColor(Color.rgb(245,247,251)); getWindow().setNavigationBarColor(Color.rgb(245,247,251));
        getWindow().getDecorView().setSystemUiVisibility(View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR);
        ScrollView scroll = new ScrollView(this); scroll.setFillViewport(true); scroll.setBackgroundColor(Color.rgb(245,247,251));
        final LinearLayout root = new LinearLayout(this); root.setOrientation(LinearLayout.VERTICAL); root.setPadding(dp(24), dp(18), dp(24), dp(24));
        scroll.addView(root); setContentView(scroll);
        if (Build.VERSION.SDK_INT >= 30) scroll.setOnApplyWindowInsetsListener(new View.OnApplyWindowInsetsListener() {
            public WindowInsets onApplyWindowInsets(View view, WindowInsets insets) {
                android.graphics.Insets bars = insets.getInsets(WindowInsets.Type.systemBars());
                root.setPadding(dp(24) + bars.left, dp(18) + bars.top, dp(24) + bars.right, dp(24) + bars.bottom); return insets;
            }
        });
        LinearLayout languages = new LinearLayout(this); languages.setGravity(Gravity.RIGHT); root.addView(languages);
        nl = new Button(this); nl.setText("🇳🇱 NL"); en = new Button(this); en.setText("🇬🇧 ENG"); languages.addView(nl); languages.addView(en);
        nl.setContentDescription("Nederlands"); en.setContentDescription("English");
        nl.setOnClickListener(v -> language(false)); en.setOnClickListener(v -> language(true));
        heading = text(root, 27, true); model = text(root, 14, false); model.setText(Build.MANUFACTURER + " " + Build.MODEL);
        LinearLayout card = new LinearLayout(this); card.setOrientation(LinearLayout.VERTICAL); card.setPadding(dp(20), dp(16), dp(20), dp(16));
        GradientDrawable background = new GradientDrawable(); background.setColor(Color.WHITE); background.setCornerRadius(dp(18)); card.setBackground(background);
        LinearLayout.LayoutParams cardLayout = new LinearLayout.LayoutParams(-1, -2); cardLayout.topMargin = dp(20); cardLayout.bottomMargin = dp(18); root.addView(card, cardLayout);
        level = text(card, 48, true); level.setTextColor(Color.rgb(32,92,190)); details = text(card, 15, false); details.setLineSpacing(dp(5), 1);
        refresh = button(root); refresh.setOnClickListener(v -> update());
        description = text(root, 15, true); address = text(root, 19, true); address.setTextIsSelectable(true);
        token = text(root, 14, false); token.setTypeface(Typeface.MONOSPACE); token.setTextIsSelectable(true);
        copy = button(root); copy.setOnClickListener(v -> {
            String ip = BatteryData.address(this); if (ip.isEmpty()) { Toast.makeText(this, t("Verbind eerst met wifi", "Connect to Wi-Fi first"), Toast.LENGTH_SHORT).show(); return; }
            ClipboardManager clipboard = (ClipboardManager)getSystemService(CLIPBOARD_SERVICE);
            clipboard.setPrimaryClip(ClipData.newPlainText("Battery Health", ip + "\n" + BatteryData.token(this)));
            Toast.makeText(this, t("Adres en code gekopieerd", "Address and key copied"), Toast.LENGTH_SHORT).show();
        });
        toggle = button(root); toggle.setOnClickListener(v -> {
            if (ShareService.running) { stopService(new Intent(this, ShareService.class)); handler.postDelayed(() -> update(), 300); }
            else if (BatteryData.address(this).isEmpty()) Toast.makeText(this, t("Verbind eerst met wifi", "Connect to Wi-Fi first"), Toast.LENGTH_LONG).show();
            else if (Build.VERSION.SDK_INT >= 33 && checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) requestPermissions(new String[] { Manifest.permission.POST_NOTIFICATIONS }, 7);
            else startSharing();
        });
        sharing = text(root, 14, true); note = text(root, 13, false); note.setTextColor(Color.DKGRAY);
        update();
    }
    private TextView text(LinearLayout parent, int size, boolean bold) {
        TextView view = new TextView(this); view.setTextSize(size); view.setTextColor(Color.rgb(25,35,48)); if (bold) view.setTypeface(null, Typeface.BOLD);
        LinearLayout.LayoutParams layout = new LinearLayout.LayoutParams(-1, -2); layout.bottomMargin = dp(8); parent.addView(view, layout); return view;
    }
    private Button button(LinearLayout parent) {
        Button view = new Button(this); view.setAllCaps(false); LinearLayout.LayoutParams layout = new LinearLayout.LayoutParams(-1, dp(54)); layout.bottomMargin = dp(8); parent.addView(view, layout); return view;
    }
    private void language(boolean value) { english = value; BatteryData.prefs(this).edit().putBoolean("english", value).apply(); update(); }
    private void startSharing() {
        try { startForegroundService(new Intent(this, ShareService.class)); handler.postDelayed(() -> update(), 500); }
        catch (Exception error) { ShareService.failed = true; update(); }
    }
    public void onRequestPermissionsResult(int code, String[] permissions, int[] results) { super.onRequestPermissionsResult(code, permissions, results); if (code == 7) startSharing(); }
    private String number(JSONObject j, String key, String format, String unit) {
        return j.isNull(key) ? t("Niet beschikbaar", "Not available") : String.format(english ? Locale.UK : new Locale("nl", "NL"), format, j.optDouble(key)) + unit;
    }
    private String health(int value) {
        switch (value) { case 2: return t("Goed", "Good"); case 3: return t("Oververhit", "Overheated"); case 4: return t("Defect", "Dead"); case 5: return t("Overspanning", "Overvoltage"); case 6: return t("Fout", "Failure"); case 7: return t("Te koud", "Cold"); default: return t("Onbekend", "Unknown"); }
    }
    private void update() {
        heading.setText(t("Je batterij", "Your battery")); nl.setAlpha(english ? .5f : 1); en.setAlpha(english ? 1 : .5f);
        refresh.setText(t("Opnieuw meten", "Refresh")); description.setText(t("Delen met je Windows-laptop", "Share with your Windows laptop"));
        String ip = BatteryData.address(this); address.setText(ip.isEmpty() ? t("Maak verbinding met wifi", "Connect to Wi-Fi") : ip);
        token.setText(t("Toegangscode:\n", "Access key:\n") + BatteryData.token(this));
        copy.setText(t("Kopieer adres en code", "Copy address and key")); toggle.setText(ShareService.running ? t("Stop delen", "Stop sharing") : t("Delen met Windows", "Share with Windows"));
        sharing.setText(ShareService.failed ? t("Delen kon niet starten. Probeer opnieuw.", "Sharing could not start. Try again.") : ShareService.running ? t("Delen is actief", "Sharing is active") : t("Delen staat uit", "Sharing is off"));
        note.setText(t("Kies in Windows Android / Wi-Fi → Android-app. Vul dit adres en deze code in. Beide apparaten moeten op hetzelfde netwerk zitten.\n\nCapaciteit en laadcycli kunnen ontbreken. ‘Goed’ is de Android-status, geen percentage batterijconditie. Gebruiksduur is een schatting op basis van resterende lading en gemiddeld verbruik.\n\nAndroid kan de verbinding tijdens diepe slaap of door batterijbesparing onderbreken. Open de app opnieuw als Windows geen verbinding krijgt.",
            "In Windows choose Android / Wi-Fi → Android app. Enter this address and key. Both devices must be on the same network.\n\nCapacity and cycles may be unavailable. ‘Good’ is the Android status, not a battery health percentage. Runtime is estimated from remaining charge and average consumption.\n\nAndroid may interrupt connectivity during deep sleep or battery saving. Reopen this app if Windows cannot connect."));
        try {
            JSONObject j = BatteryData.snapshot(this); int status = j.optInt("status", 1);
            level.setText(number(j, "level", "%.0f", "%"));
            String state = status == 2 ? t("Aan het opladen", "Charging") : status == 3 ? t("Werkt op batterij", "On battery") : status == 4 ? t("Laadt niet op", "Not charging") : status == 5 ? t("Vol", "Full") : t("Onbekend", "Unknown");
            String condition = t("Niet beschikbaar", "Not available");
            if (!j.isNull("designMah") && !j.isNull("fullMah")) condition = String.format(english ? Locale.UK : new Locale("nl", "NL"), "%.1f%%", j.optDouble("fullMah") / j.optDouble("designMah") * 100);
            String runtime = t("Niet beschikbaar", "Not available");
            if (status == 2 || status == 5) runtime = t("Niet van toepassing", "Not applicable");
            else if (!j.isNull("runtimeSeconds")) { long seconds = j.optLong("runtimeSeconds"); runtime = String.format(Locale.ROOT, "%d:%02d", seconds / 3600, seconds / 60 % 60) + t(" uur", " hr"); }
            details.setText(state + " • " + number(j, "temperatureC", "%.1f", " °C") + "\n" +
                t("Conditieschatting: ", "Health estimate: ") + condition + "\n" +
                t("Android-status: ", "Android status: ") + health(j.optInt("health")) + "\n" +
                t("Ontwerpcapaciteit: ", "Design capacity: ") + number(j, "designMah", "%,.0f", " mAh") + "\n" +
                t("Volle laadcapaciteit: ", "Full-charge capacity: ") + number(j, "fullMah", "%,.0f", " mAh") + "\n" +
                t("Laadcycli: ", "Cycle count: ") + number(j, "cycles", "%.0f", "") + "\n" +
                t("Gebruiksduur (schatting): ", "Runtime (estimate): ") + runtime);
        } catch (Exception missing) { level.setText("—"); details.setText(t("Batterijgegevens niet beschikbaar", "Battery data not available")); }
    }
    public void onResume() { super.onResume(); handler.removeCallbacks(tick); handler.post(tick); }
    public void onPause() { handler.removeCallbacks(tick); super.onPause(); }
}
