using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

class AndroidDashboard : Form
{
    static readonly string[] Names = { "Google Pixel 10", "Oppo Find X5 Pro", "Samsung S10", "Galaxy Tab S9" };
    readonly string[] endpoints = new string[4], identities = new string[4];
    readonly string[] accessKeys = new string[4];
    readonly bool[] appModes = { true, true, true, true };
    readonly AndroidReading[] readings = new AndroidReading[4];
    readonly Label[] cardTitles = new Label[4], cardLevels = new Label[4], cardDetails = new Label[4];
    Label heading, subtitle, deviceLabel, pairLabel, codeLabel, connectLabel, instructions, operation, footer;
    Button nl, en, refresh, link;
    ComboBox device, connectionMode;
    TextBox pairAddress, pairCode, connectAddress;
    Timer timer = new Timer();
    bool english, busy, operationError, translatingMode;
    string operationKey = "ready";
    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AndroidDevices.xml");
    public AndroidDashboard(bool useEnglish)
    {
        english = useEnglish;
        Text = "Battery Health - Android Wi-Fi";
        ClientSize = new Size(1060, 780); MinimumSize = new Size(870, 620);
        AutoScroll = true; AutoScrollMinSize = new Size(1050, 840);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 251); Font = new Font("Segoe UI", 10);
        heading = LabelAt(28, 25, 820, 42, new Font("Segoe UI", 23, FontStyle.Bold));
        subtitle = LabelAt(30, 75, 740, 28, null);
        connectionMode = new ComboBox { Location = new Point(787, 74), Size = new Size(240, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        connectionMode.Items.AddRange(new string[] { "Android-app", "Draadloze debugging / ADB" });
        connectionMode.SelectedIndex = 0;
        nl = Language(false, 908); en = Language(true, 966);
        for (int i = 0; i < 4; i++) {
            int x = 30 + (i % 2) * 510, y = 116 + (i / 2) * 212;
            Panel card = new Panel { Location = new Point(x, y), Size = new Size(490, 198), BackColor = Color.White };
            cardTitles[i] = new Label { Location = new Point(16, 12), Size = new Size(455, 26), Font = new Font("Segoe UI", 13, FontStyle.Bold), Text = Names[i] };
            cardLevels[i] = new Label { Location = new Point(16, 44), Size = new Size(160, 45), Font = new Font("Segoe UI", 25, FontStyle.Bold), ForeColor = Color.FromArgb(32, 92, 190) };
            cardDetails[i] = new Label { Location = new Point(16, 94), Size = new Size(460, 96), Font = new Font("Segoe UI", 9) };
            card.Controls.AddRange(new Control[] { cardTitles[i], cardLevels[i], cardDetails[i] }); Controls.Add(card);
        }
        instructions = LabelAt(30, 548, 995, 61, null);
        deviceLabel = LabelAt(30, 619, 190, 23, null);
        pairLabel = LabelAt(238, 619, 215, 23, null);
        codeLabel = LabelAt(466, 619, 150, 23, null);
        connectLabel = LabelAt(632, 619, 230, 23, null);
        device = new ComboBox { Location = new Point(30, 645), Size = new Size(190, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        device.Items.AddRange(Names);
        pairAddress = Input(238, 215); pairCode = Input(466, 150); pairCode.MaxLength = 6; pairCode.UseSystemPasswordChar = true;
        connectAddress = Input(632, 230);
        link = new Button { Location = new Point(877, 641), Size = new Size(150, 37), FlatStyle = FlatStyle.Flat };
        refresh = new Button { Location = new Point(30, 730), Size = new Size(190, 40), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(32, 92, 190), ForeColor = Color.White };
        operation = LabelAt(30, 687, 995, 31, null);
        footer = LabelAt(238, 734, 790, 90, new Font("Segoe UI", 9)); footer.ForeColor = Color.DimGray;
        Controls.AddRange(new Control[] { device, pairAddress, pairCode, connectAddress, link, refresh, nl, en, connectionMode });
        LoadConfig();
        device.SelectedIndexChanged += delegate {
            int i = device.SelectedIndex; connectAddress.Text = endpoints[i] ?? ""; pairAddress.Clear(); pairCode.Clear();
            connectionMode.SelectedIndex = appModes[i] ? 0 : 1;
            if (appModes[i]) pairAddress.Text = accessKeys[i] ?? "";
            Render();
        };
        connectionMode.SelectedIndexChanged += delegate { if (translatingMode) return; pairAddress.Clear(); pairCode.Clear(); Render(); };
        device.SelectedIndex = 0;
        link.Click += delegate { LinkDevice(); };
        refresh.Click += delegate { RefreshReadings(); };
        timer.Interval = 60000; timer.Tick += delegate { RefreshReadings(); };
        Shown += delegate { timer.Start(); RefreshReadings(); };
        FormClosed += delegate { timer.Stop(); timer.Dispose(); };
        Render();
    }
    Label LabelAt(int x, int y, int w, int h, Font font)
    {
        Label label = new Label { Location = new Point(x, y), Size = new Size(w, h) };
        if (font != null) label.Font = font;
        Controls.Add(label); return label;
    }
    TextBox Input(int x, int width) { return new TextBox { Location = new Point(x, 645), Size = new Size(width, 28), MaxLength = 64 }; }
    Button Language(bool useEnglish, int x)
    {
        Button b = new Button { Location = new Point(x, 27), Size = new Size(52, 36), Image = BatteryHealth.Flag(useEnglish), FlatStyle = FlatStyle.Flat, AccessibleName = useEnglish ? "English" : "Nederlands" };
        b.Click += delegate { english = useEnglish; Render(); }; return b;
    }
    string T(string dutch, string eng) { return english ? eng : dutch; }
    void Render()
    {
        heading.Text = T("Je Android-batterijen", "Your Android batteries");
        subtitle.Text = T("Via wifi • automatisch vernieuwen elke minuut • hetzelfde lokale netwerk", "Over Wi-Fi • refreshes every minute • same local network");
        nl.FlatAppearance.BorderSize = english ? 1 : 3; en.FlatAppearance.BorderSize = english ? 3 : 1;
        nl.FlatAppearance.BorderColor = english ? Color.LightGray : Color.RoyalBlue;
        en.FlatAppearance.BorderColor = english ? Color.RoyalBlue : Color.LightGray;
        bool companion = connectionMode.SelectedIndex == 0;
        translatingMode = true;
        try { connectionMode.Items[0] = T("Android-app", "Android app"); connectionMode.Items[1] = T("Draadloze debugging / ADB", "Wireless debugging / ADB"); }
        finally { translatingMode = false; }
        instructions.Text = companion ? T(
            "Installeer Battery Health Android.apk op je apparaat en tik op ‘Delen met Windows’.\nSelecteer hieronder het juiste apparaat. Vul de toegangscode en het wifi-adres uit de Android-app in.\nKlik op Verbinden. Draadloze debugging is hierbij niet nodig.",
            "Install Battery Health Android.apk on your device and tap ‘Share with Windows’.\nSelect the correct device below. Enter the access key and Wi-Fi address shown in the Android app.\nClick Connect. Wireless debugging is not needed.") : T(
            "Koppelen: zet op je apparaat Ontwikkelaarsopties → Draadloze debugging aan. Kies ‘Koppelen met code’.\nVul hieronder het koppeladres en de 6-cijferige code in. Het verbindingsadres staat op het hoofdscherm\nvan Draadloze debugging en heeft meestal een andere poort. Zonder code verbind je een eerder gekoppeld apparaat.",
            "Pair: enable Developer options → Wireless debugging on your device. Select ‘Pair with pairing code’.\nEnter the pairing address and 6-digit code below. The connection address is on the main Wireless debugging\nscreen and usually has a different port. Leave the code empty to connect an already paired device.");
        deviceLabel.Text = T("Apparaat", "Device"); pairLabel.Text = companion ? T("Toegangscode uit Android-app", "Access key from Android app") : T("Koppeladres (IP:poort)", "Pairing address (IP:port)");
        pairAddress.UseSystemPasswordChar = companion;
        pairCode.Visible = codeLabel.Visible = !companion;
        codeLabel.Text = T("Koppelcode", "Pairing code"); connectLabel.Text = T("Verbindingsadres (IP:poort)", "Connection address (IP:port)");
        link.Text = companion ? T("Verbinden", "Connect") : T("Koppelen / verbinden", "Pair / connect"); refresh.Text = T("Alles vernieuwen", "Refresh all");
        footer.Text = companion ? T("Laat ‘Delen met Windows’ aan staan. Capaciteit en laadcycli kunnen ontbreken.\n‘Goed’ is een Android-status, geen percentage batterijconditie. Gebruiksduur is een schatting\nuit resterende lading en gemiddeld verbruik. Bij een nieuw IP-adres: werk het verbindingsadres bij.",
            "Keep ‘Share with Windows’ enabled. Capacity and cycle count may be unavailable.\n‘Good’ is an Android status, not a battery health percentage. Runtime is estimated from\nremaining charge and average consumption. If the IP changes, update the connection address.") : T("ADB-modus gebruikt draadloze debugging. Capaciteit en laadcycli kunnen ontbreken.\n‘Goed’ is een Android-status, geen percentage batterijconditie. Gebruiksduur is een schatting.\nBij een gewijzigde wifi-poort: vul het nieuwe verbindingsadres in en klik op Verbinden.",
            "ADB mode uses wireless debugging. Capacity and cycle count may be unavailable.\n‘Good’ is an Android status, not a battery health percentage. Runtime is an estimate.\nIf the Wi-Fi port changes: enter the new connection address and click Connect.");
        operation.ForeColor = operationError ? Color.Firebrick : Color.DimGray;
        switch (operationKey) {
            case "reading": operation.Text = T("Batterijgegevens ophalen…", "Reading battery data…"); break;
            case "linking": operation.Text = T("Koppelen en verbinden…", "Pairing and connecting…"); break;
            case "linked": operation.Text = T("Verbonden. Het adres is opgeslagen.", "Connected. The address has been saved."); break;
            case "invalid": operation.Text = companion ? T("Vul het lokale IP-adres met poort en de toegangscode van 32 tekens uit de Android-app in.", "Enter the local IP address with port and the 32-character access key from the Android app.") : T("Vul een geldig lokaal IP-adres met poort en, voor koppelen, zes cijfers in.", "Enter a valid local IP address with port and, for pairing, six digits."); break;
            case "failed": operation.Text = companion ? T("Verbinding mislukt. Controleer wifi, de toegangscode en of ‘Delen met Windows’ aan staat.", "Connection failed. Check Wi-Fi, the access key and whether ‘Share with Windows’ is enabled.") : T("Verbinding mislukt. Controleer wifi, draadloze debugging, de twee poorten en de actuele koppelcode.", "Connection failed. Check Wi-Fi, wireless debugging, both ports and the current pairing code."); break;
            case "savefailed": operation.Text = T("Verbonden, maar het adres kon niet worden opgeslagen. Controleer de schrijfrechten van de appmap.", "Connected, but the address could not be saved. Check write access to the app folder."); break;
            case "configfailed": operation.Text = T("Opgeslagen adressen konden niet worden geladen. Verbind de apparaten opnieuw.", "Saved addresses could not be loaded. Reconnect the devices."); break;
            default: operation.Text = T("Vul voor elk apparaat één keer de verbindingsgegevens in.", "Enter the connection details once for each device."); break;
        }
        for (int i = 0; i < 4; i++) RenderCard(i);
    }
    void RenderCard(int i)
    {
        AndroidReading r = readings[i];
        if (r == null || !r.Online) {
            cardLevels[i].Text = "—";
            cardDetails[i].Text = r != null && r.AccessDenied ? T("Toegangscode onjuist. Verbind opnieuw met de code uit de app.", "Incorrect access key. Reconnect using the key from the app.") : r != null && r.WrongDevice ? T("Ander apparaat op dit adres. Verbind het bedoelde apparaat opnieuw.", "Different device at this address. Reconnect the intended device.") : String.IsNullOrEmpty(endpoints[i]) ? T("Nog niet verbonden\nKies dit apparaat onderaan om te beginnen.", "Not connected yet\nSelect this device below to get started.") : (appModes[i] ? T("Niet bereikbaar — controleer wifi en ‘Delen met Windows’.\nOpgeslagen adres: ", "Unreachable — check Wi-Fi and ‘Share with Windows’.\nSaved address: ") : T("Niet bereikbaar — controleer wifi en draadloze debugging.\nOpgeslagen adres: ", "Unreachable — check Wi-Fi and wireless debugging.\nSaved address: ")) + endpoints[i];
            return;
        }
        CultureInfo culture = CultureInfo.GetCultureInfo(english ? "en-GB" : "nl-NL");
        string unavailable = T("Niet beschikbaar", "Not available");
        cardLevels[i].Text = r.Level.HasValue ? r.Level.Value.ToString("0", culture) + "%" : "—";
        string condition = r.Design.HasValue && r.Full.HasValue ? (r.Full.Value / r.Design.Value * 100).ToString("0.0", culture) + "%" : unavailable;
        string state = r.Status == 2 ? T("Opladen", "Charging") : r.Status == 3 ? T("Op batterij", "On battery") : r.Status == 4 ? T("Laadt niet op", "Not charging") : r.Status == 5 ? T("Vol", "Full") : unavailable;
        string health = HealthText(r.Health);
        string runtime = r.Status == 2 || r.Status == 5 ? T("Niet van toepassing", "Not applicable") : r.RuntimeSeconds.HasValue ? ((int)TimeSpan.FromSeconds(r.RuntimeSeconds.Value).TotalHours).ToString() + ":" + TimeSpan.FromSeconds(r.RuntimeSeconds.Value).Minutes.ToString("00") + T(" uur", " hr") : unavailable;
        cardDetails[i].Text = state + "   •   " + (r.Temperature.HasValue ? r.Temperature.Value.ToString("0.0", culture) + " °C" : unavailable) + "\n" +
            T("Conditieschatting: ", "Health estimate: ") + condition + "   •   " + T("Android-status: ", "Android status: ") + health + "\n" +
            T("Ontwerpcapaciteit: ", "Design capacity: ") + Capacity(r.Design, culture) + "   |   " + T("Volle capaciteit: ", "Full-charge capacity: ") + Capacity(r.Full, culture) + "\n" +
            T("Laadcycli: ", "Cycles: ") + (r.Cycles.HasValue ? r.Cycles.Value.ToString() : unavailable) + "   |   " + T("Gebruiksduur: ", "Runtime: ") + runtime + "\n" +
            r.Model + "   •   " + T("Gemeten: ", "Measured: ") + r.Measured.ToString("HH:mm:ss");
    }
    string Capacity(double? value, CultureInfo culture) { return value.HasValue ? value.Value.ToString("N0", culture) + " mAh" : T("Niet beschikbaar", "Not available"); }
    string HealthText(int health)
    {
        switch (health) {
            case 2: return T("Goed", "Good"); case 3: return T("Oververhit", "Overheated");
            case 4: return T("Defect", "Dead"); case 5: return T("Overspanning", "Overvoltage");
            case 6: return T("Fout", "Failure"); case 7: return T("Te koud", "Cold");
            default: return T("Onbekend", "Unknown");
        }
    }
    void SetBusy(bool value)
    {
        busy = value; refresh.Enabled = link.Enabled = device.Enabled = connectionMode.Enabled = pairAddress.Enabled = pairCode.Enabled = connectAddress.Enabled = !value;
    }
    async void LinkDevice()
    {
        if (busy) return;
        int index = device.SelectedIndex;
        bool companion = connectionMode.SelectedIndex == 0;
        string endpoint = connectAddress.Text.Trim(), pair = pairAddress.Text.Trim(), code = pairCode.Text.Trim();
        if (!AndroidBridge.ValidEndpoint(endpoint) || (companion ? !CompanionBridge.ValidKey(pair) : code.Length > 0 && (!Regex.IsMatch(code, @"^[0-9]{6}$") || !AndroidBridge.ValidEndpoint(pair)))) {
            operationKey = "invalid"; operationError = true; Render(); return;
        }
        SetBusy(true); operationKey = "linking"; operationError = false; Render();
        bool connected = false;
        AndroidReading firstReading = null;
        try {
            connected = await Task.Run(delegate {
                if (companion) { firstReading = CompanionBridge.Read(endpoint, pair.ToLowerInvariant(), null); return firstReading.Online; }
                if (code.Length > 0) { string result = AndroidBridge.Run("pair " + pair, code, 20000); if (result.IndexOf("Successfully paired", StringComparison.OrdinalIgnoreCase) < 0) return false; }
                AndroidBridge.Run("connect " + endpoint, null, 12000);
                return AndroidBridge.Run("-s " + endpoint + " get-state", null, 6000).Trim() == "device";
            });
            if (IsDisposed) return;
            if (connected) {
                endpoints[index] = endpoint; identities[index] = firstReading == null ? null : firstReading.Identity; readings[index] = firstReading;
                appModes[index] = companion; accessKeys[index] = companion ? pair.ToLowerInvariant() : null;
                operationKey = "linked";
                try { SaveConfig(); } catch { operationKey = "savefailed"; operationError = true; }
            } else { operationKey = "failed"; operationError = true; }
        } catch { if (!IsDisposed) { operationKey = "failed"; operationError = true; } }
        finally { if (!IsDisposed) { pairCode.Clear(); SetBusy(false); Render(); } }
        if (connected && !IsDisposed) RefreshReadings();
    }
    async void RefreshReadings()
    {
        if (busy || IsDisposed) return;
        SetBusy(true); string previous = operationKey;
        operationKey = "reading"; Render();
        try {
            Task<AndroidReading>[] tasks = new Task<AndroidReading>[4];
            for (int i = 0; i < 4; i++) {
                string endpoint = endpoints[i], identity = identities[i], key = accessKeys[i]; bool companion = appModes[i];
                tasks[i] = Task.Run(delegate { return companion ? CompanionBridge.Read(endpoint, key, identity) : AndroidBridge.Read(endpoint, identity); });
            }
            AndroidReading[] results = await Task.WhenAll(tasks);
            if (IsDisposed) return;
            bool changed = false;
            for (int i = 0; i < 4; i++) {
                readings[i] = results[i];
                if (results[i].Online && !String.IsNullOrEmpty(results[i].Endpoint) && endpoints[i] != results[i].Endpoint) { endpoints[i] = results[i].Endpoint; changed = true; }
                if (results[i].Online && !String.IsNullOrEmpty(results[i].Identity) && identities[i] != results[i].Identity) { identities[i] = results[i].Identity; changed = true; }
            }
            operationKey = previous == "linked" || previous == "savefailed" || previous == "configfailed" ? previous : "ready";
            if (changed) { try { SaveConfig(); } catch { operationKey = "savefailed"; operationError = true; } }
        } finally { if (!IsDisposed) { SetBusy(false); Render(); } }
    }
    void LoadConfig()
    {
        if (!File.Exists(configPath)) return;
        try {
            XmlDocument doc = new XmlDocument(); doc.XmlResolver = null; doc.Load(configPath);
            foreach (XmlNode item in doc.SelectNodes("/devices/device")) {
                int index;
                if (!Int32.TryParse(item.Attributes["index"].Value, out index) || index < 0 || index > 3) continue;
                string endpoint = item.Attributes["endpoint"].Value;
                if (AndroidBridge.ValidEndpoint(endpoint)) endpoints[index] = endpoint;
                if (item.Attributes["identity"] != null) identities[index] = item.Attributes["identity"].Value;
                appModes[index] = item.Attributes["mode"] != null && item.Attributes["mode"].Value == "app";
                if (item.Attributes["key"] != null && appModes[index]) {
                    try { accessKeys[index] = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(item.Attributes["key"].Value), null, DataProtectionScope.CurrentUser)); }
                    catch { accessKeys[index] = null; }
                }
            }
        } catch { operationKey = "configfailed"; operationError = true; }
    }
    void SaveConfig()
    {
        XmlDocument doc = new XmlDocument(); XmlElement root = doc.CreateElement("devices"); doc.AppendChild(root);
        for (int i = 0; i < 4; i++) {
            XmlElement item = doc.CreateElement("device"); item.SetAttribute("index", i.ToString());
            item.SetAttribute("endpoint", endpoints[i] ?? ""); item.SetAttribute("identity", identities[i] ?? ""); root.AppendChild(item);
            item.SetAttribute("mode", appModes[i] ? "app" : "adb");
            if (appModes[i] && !String.IsNullOrEmpty(accessKeys[i])) item.SetAttribute("key", Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(accessKeys[i]), null, DataProtectionScope.CurrentUser)));
        }
        doc.Save(configPath);
    }
}

class AndroidReading
{
    public bool Online, WrongDevice, AccessDenied;
    public double? Level, Temperature, Design, Full;
    public int Status, Health;
    public long? Cycles, RuntimeSeconds;
    public string Model = "Android", Identity, Endpoint;
    public DateTime Measured;
}

static class CompanionBridge
{
    public static bool ValidKey(string key) { return key != null && Regex.IsMatch(key, @"\A[a-fA-F0-9]{32}\z"); }
    public static AndroidReading Read(string endpoint, string key, string identity)
    {
        if (!AndroidBridge.ValidEndpoint(endpoint) || !ValidKey(key)) return new AndroidReading();
        try {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + endpoint + "/battery");
            request.Proxy = null; request.AllowAutoRedirect = false; request.Timeout = 5000; request.ReadWriteTimeout = 5000;
            request.Headers["X-Battery-Key"] = key.ToLowerInvariant();
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                if (response.StatusCode != HttpStatusCode.OK || response.ContentLength > 16384) return new AndroidReading();
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
                    char[] buffer = new char[16385]; int count = 0, received;
                    while (count < buffer.Length && (received = reader.Read(buffer, count, buffer.Length - count)) > 0) count += received;
                    if (count > 16384) return new AndroidReading();
                    AndroidReading r = Parse(new string(buffer, 0, count), identity); r.Endpoint = endpoint; return r;
                }
            }
        } catch (WebException e) {
            HttpWebResponse response = e.Response as HttpWebResponse;
            bool denied = response != null && response.StatusCode == HttpStatusCode.Unauthorized;
            if (response != null) response.Dispose();
            return new AndroidReading { AccessDenied = denied };
        } catch { return new AndroidReading(); }
    }
    static double? Number(Dictionary<string, object> values, string key, double minimum, double maximum)
    {
        object value; if (!values.TryGetValue(key, out value) || value == null || value is string || value is bool) return null;
        double number;
        try { number = Convert.ToDouble(value, CultureInfo.InvariantCulture); } catch { return null; }
        return !Double.IsNaN(number) && !Double.IsInfinity(number) && number >= minimum && number <= maximum ? (double?)number : null;
    }
    public static AndroidReading Parse(string json, string expectedIdentity)
    {
        try {
            JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = 16384, RecursionLimit = 8 };
            Dictionary<string, object> values = serializer.Deserialize<Dictionary<string, object>>(json);
            if (values == null || Number(values, "protocol", 1, 1) != 1) return new AndroidReading();
            object id; Guid guid;
            if (!values.TryGetValue("identity", out id) || !(id is string) || !Guid.TryParse((string)id, out guid)) return new AndroidReading();
            string identity = guid.ToString();
            if (!String.IsNullOrEmpty(expectedIdentity) && identity != expectedIdentity) return new AndroidReading { WrongDevice = true };
            object model;
            string modelName = values.TryGetValue("model", out model) && model is string ? (string)model : "Android";
            modelName = Regex.Replace(modelName, @"[\x00-\x1f\x7f]", " "); if (modelName.Length > 100) modelName = modelName.Substring(0, 100);
            AndroidReading r = new AndroidReading { Online = true, Identity = identity, Model = modelName, Measured = DateTime.Now };
            r.Level = Number(values, "level", 0, 100); r.Temperature = Number(values, "temperatureC", -50, 150);
            r.Design = Number(values, "designMah", 100, 50000); r.Full = Number(values, "fullMah", 100, 50000);
            r.Status = (int)(Number(values, "status", 1, 5) ?? 1); r.Health = (int)(Number(values, "health", 1, 7) ?? 1);
            double? cycles = Number(values, "cycles", 0, Int32.MaxValue - 1);
            if (cycles.HasValue && cycles.Value == Math.Floor(cycles.Value)) r.Cycles = (long)cycles.Value;
            double? runtime = Number(values, "runtimeSeconds", 0, 604800);
            if (r.Status == 3 && runtime.HasValue) r.RuntimeSeconds = (long)runtime.Value;
            return r;
        } catch { return new AndroidReading(); }
    }
    public static void SelfTest()
    {
        string json = "{\"protocol\":1,\"identity\":\"12345678-1234-1234-1234-123456789abc\",\"model\":\"Example phone\",\"level\":72,\"status\":3,\"health\":2,\"temperatureC\":31.5,\"designMah\":5000,\"fullMah\":4400,\"cycles\":0,\"runtimeSeconds\":11520}";
        AndroidReading r = Parse(json, null);
        if (!r.Online || r.Level != 72 || r.Design != 5000 || r.Full != 4400 || r.Cycles != 0 || r.RuntimeSeconds != 11520) throw new Exception("Companion protocol failed");
        if (!Parse(json, "00000000-0000-0000-0000-000000000000").WrongDevice || Parse("{}", null).Online || Parse(json.Replace("\"protocol\":1", "\"protocol\":2"), null).Online) throw new Exception("Companion identity/version validation failed");
        r = Parse(json.Replace("\"designMah\":5000", "\"designMah\":null").Replace("\"cycles\":0", "\"cycles\":-1").Replace("\"level\":72", "\"level\":150"), null);
        if (r.Design.HasValue || r.Cycles.HasValue || r.Level.HasValue) throw new Exception("Companion missing/invalid data failed");
        if (!ValidKey("0123456789abcdef0123456789abcdef") || ValidKey("123456")) throw new Exception("Companion key validation failed");
    }
}

static class AndroidBridge
{
    public static bool ValidEndpoint(string endpoint)
    {
        if (String.IsNullOrEmpty(endpoint)) return false;
        Match m = Regex.Match(endpoint, @"^(\d{1,3}(?:\.\d{1,3}){3}):(\d{1,5})$");
        if (!m.Success) return false;
        IPAddress ip; int port;
        if (!IPAddress.TryParse(m.Groups[1].Value, out ip) || !Int32.TryParse(m.Groups[2].Value, out port) || port < 1 || port > 65535) return false;
        byte[] b = ip.GetAddressBytes();
        return b.Length == 4 && (b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254));
    }
    public static string Run(string arguments, string input, int timeout)
    {
        string adb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "adb.exe");
        using (Process p = new Process()) {
            p.StartInfo = new ProcessStartInfo(adb, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true };
            p.Start();
            Task<string> output = p.StandardOutput.ReadToEndAsync(), error = p.StandardError.ReadToEndAsync();
            if (input != null) p.StandardInput.WriteLine(input);
            p.StandardInput.Close();
            if (!p.WaitForExit(timeout)) { try { p.Kill(); } catch { } throw new TimeoutException("ADB timed out"); }
            Task.WaitAll(new Task[] { output, error }, 3000);
            if (!output.IsCompleted || !error.IsCompleted) throw new TimeoutException("ADB output timed out");
            return output.Result + "\n" + error.Result;
        }
    }
    public static AndroidReading Read(string endpoint, string identity)
    {
        if (!ValidEndpoint(endpoint)) return new AndroidReading();
        try {
            string serial = ConnectAndIdentify(endpoint);
            if (serial == null) {
                string discovered = Discover(identity);
                if (discovered == null) return new AndroidReading();
                endpoint = discovered;
                serial = ConnectAndIdentify(endpoint);
                if (serial == null) return new AndroidReading();
            }
            if (!String.IsNullOrEmpty(identity) && serial != identity) return new AndroidReading { WrongDevice = true };
            string dump = Run("-s " + endpoint + " shell dumpsys battery", null, 7000);
            AndroidReading r = Parse(dump, "");
            if (!r.Online) return r;
            r.Identity = serial;
            r.Endpoint = endpoint;
            string model = Run("-s " + endpoint + " shell getprop ro.product.model", null, 5000).Trim();
            if (!String.IsNullOrEmpty(model) && !model.StartsWith("error:") && !model.Contains("\n")) r.Model = model;
            // Standard power_supply charge fields use microamp-hours. Never substitute Charge counter for full capacity.
            string script = "for f in charge_full_design charge_full cycle_count; do printf '%s=' $f; cat /sys/class/power_supply/battery/$f 2>/dev/null; printf '\\n'; done";
            string extra = "";
            try { extra = Run("-s " + endpoint + " shell \"" + script + "\"", null, 5000); } catch { }
            FillCapacity(r, extra);
            if (r.Status != 2 && r.Status != 5) {
                try { r.RuntimeSeconds = ParseRuntime(Run("-s " + endpoint + " shell \"dumpsys batterystats --checkin | grep ',0,i,dtr,'\"", null, 7000)); } catch { }
            }
            r.Measured = DateTime.Now; return r;
        } catch { return new AndroidReading(); }
    }
    static string Discover(string identity)
    {
        if (String.IsNullOrEmpty(identity)) return null;
        string services;
        try { services = Run("mdns services", null, 5000); } catch { return null; }
        return FindService(services, identity);
    }
    static string ConnectAndIdentify(string endpoint)
    {
        try {
            Run("connect " + endpoint, null, 7000);
            string serial = Run("-s " + endpoint + " shell getprop ro.serialno", null, 5000).Trim();
            return serial.Length == 0 || Regex.IsMatch(serial, @"^[A-Za-z0-9_.-]+$") ? serial : null;
        } catch { return null; }
    }
    public static string FindService(string services, string identity)
    {
        if (String.IsNullOrEmpty(identity)) return null;
        foreach (string line in services.Split('\n')) {
            string[] parts = Regex.Split(line.Trim(), @"\s+");
            if (parts.Length < 3 || parts[1] != "_adb-tls-connect._tcp." && parts[1] != "_adb-tls-connect._tcp") continue;
            if ((parts[0].StartsWith("adb-" + identity + "-", StringComparison.Ordinal) || parts[0] == "adb-" + identity) && ValidEndpoint(parts[2])) return parts[2];
        }
        return null;
    }
    static double? Number(string data, string key)
    {
        Match m = Regex.Match(data, @"(?im)^\s*" + Regex.Escape(key) + @"\s*[:=]\s*(-?\d+(?:\.\d+)?)\s*$");
        double value;
        return m.Success && Double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? (double?)value : null;
    }
    public static AndroidReading Parse(string dump, string extra)
    {
        AndroidReading r = new AndroidReading();
        double? level = Number(dump, "level"), scale = Number(dump, "scale");
        // Simulated/frozen service readings must not be presented as a live measurement.
        if (dump.IndexOf("UPDATES STOPPED", StringComparison.OrdinalIgnoreCase) >= 0) return r;
        if (!level.HasValue || !scale.HasValue || scale.Value <= 0 || level.Value < 0 || level.Value > scale.Value) return r;
        r.Online = true; r.Level = level.Value / scale.Value * 100;
        r.Status = (int)(Number(dump, "status") ?? 1); r.Health = (int)(Number(dump, "health") ?? 1);
        double? temp = Number(dump, "temperature");
        if (temp.HasValue && temp.Value >= -500 && temp.Value <= 1500) r.Temperature = temp.Value / 10;
        double? cycles = Number(dump, "cycle count");
        if (cycles.HasValue && cycles.Value >= 0 && cycles.Value < Int32.MaxValue) r.Cycles = (long)cycles.Value;
        FillCapacity(r, extra); r.Measured = DateTime.Now; return r;
    }
    static void FillCapacity(AndroidReading r, string extra)
    {
        double? design = Number(extra, "charge_full_design"), full = Number(extra, "charge_full"), cycles = Number(extra, "cycle_count");
        if (design.HasValue && design.Value >= 100000 && design.Value <= 50000000) r.Design = design.Value / 1000;
        if (full.HasValue && full.Value >= 100000 && full.Value <= 50000000) r.Full = full.Value / 1000;
        if (cycles.HasValue && cycles.Value >= 0 && cycles.Value < Int32.MaxValue) r.Cycles = (long)cycles.Value;
    }
    public static long? ParseRuntime(string checkin)
    {
        // AOSP check-in dtr is estimated discharge time remaining in microseconds.
        Match m = Regex.Match(checkin, @"(?m)^\d+,0,i,dtr,(\d+)\s*$");
        long microseconds;
        return m.Success && Int64.TryParse(m.Groups[1].Value, out microseconds) ? (long?)(microseconds / 1000000) : null;
    }
    public static void SelfTest()
    {
        if (!ValidEndpoint("192.168.1.25:37123") || ValidEndpoint("192.168.1.25:0") || ValidEndpoint("192.168.1.25:99999") || ValidEndpoint("192.168.1.25:37123 & calc") || ValidEndpoint("8.8.8.8:5555")) throw new Exception("Endpoint validation failed");
        string dump = "Current Battery Service state:\n level: 72\n scale: 100\n status: 3\n health: 2\n temperature: 315\n Charge counter: 3200000";
        AndroidReading r = Parse(dump, "charge_full_design=5000000\ncharge_full=4400000\ncycle_count=0");
        if (!r.Online || r.Level != 72 || r.Temperature != 31.5 || r.Design != 5000 || r.Full != 4400 || r.Cycles != 0 || r.Status != 3 || r.Health != 2) throw new Exception("Battery parsing failed");
        r = Parse(dump, "charge_full_design=\ncharge_full=Permission denied\ncycle_count=-1");
        if (r.Design.HasValue || r.Full.HasValue || r.Cycles.HasValue || r.RuntimeSeconds.HasValue) throw new Exception("Missing data handling failed");
        if (Parse("UPDATES STOPPED\n" + dump, "").Online || Parse("error: device offline", "").Online) throw new Exception("Offline handling failed");
        if (Parse(dump.Replace("level: 72", "level: 150"), "").Online) throw new Exception("Invalid charge level accepted");
        if (ParseRuntime("9,0,i,dtr,11520000000\n") != 11520 || ParseRuntime("9,0,i,ctr,11520000000").HasValue || ParseRuntime("9,0,i,dtr,-1").HasValue) throw new Exception("Runtime parsing failed");
        string mdns = "adb-OTHER-abc _adb-tls-connect._tcp. 192.168.1.2:32123\nadb-PIXEL123-xyz _adb-tls-pairing._tcp. 192.168.1.3:32222\nadb-PIXEL123-xyz _adb-tls-connect._tcp. 192.168.1.3:32333";
        if (FindService(mdns, "PIXEL123") != "192.168.1.3:32333" || FindService(mdns, "PIXEL12") != null || FindService(mdns, "") != null) throw new Exception("Device rediscovery failed");
    }
}
