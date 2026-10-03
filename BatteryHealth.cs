using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

class BatteryHealth : Form
{
    Label status = new Label(), health = new Label(), details = new Label();
    Button refresh = new Button();
    BackgroundWorker worker = new BackgroundWorker();
    Label title, foot;
    Button dutch, english;
    bool useEnglish, readFailed;
    Result lastResult;
    public BatteryHealth()
    {
        Text = "Battery Health"; ClientSize = new Size(620, 570);
        MinimumSize = new Size(636, 609); MaximumSize = MinimumSize;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 251);
        Font = new Font("Segoe UI", 11);
        dutch = LanguageButton(false, 476); english = LanguageButton(true, 534);
        Button android = new Button { Text = "Android / Wi-Fi", Location = new Point(30, 20), Size = new Size(180, 36), FlatStyle = FlatStyle.Flat };
        android.Click += delegate { new AndroidDashboard(useEnglish).Show(this); };
        Controls.Add(android);
        title = new Label { Location = new Point(28, 70), Size = new Size(570, 38), Font = new Font("Segoe UI", 20, FontStyle.Bold) };
        status.SetBounds(30, 122, 560, 48);
        health.SetBounds(28, 177, 560, 76); health.Font = new Font("Segoe UI", 40, FontStyle.Bold);
        details.SetBounds(30, 266, 560, 205);
        refresh.SetBounds(30, 495, 175, 42);
        refresh.BackColor = Color.FromArgb(32, 92, 190); refresh.ForeColor = Color.White;
        refresh.FlatStyle = FlatStyle.Flat;
        foot = new Label { Location = new Point(220, 497), Size = new Size(370, 48), Font = new Font("Segoe UI", 9), ForeColor = Color.DimGray };
        Controls.AddRange(new Control[] { title, status, health, details, refresh, foot, dutch, english });
        worker.DoWork += delegate(object sender, DoWorkEventArgs e) { e.Result = Read(); };
        worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
            refresh.Enabled = true;
            readFailed = e.Error != null; lastResult = readFailed ? null : (Result)e.Result; Render();
        };
        refresh.Click += delegate { Measure(); }; Shown += delegate { Measure(); };
        Render();
    }
    Button LanguageButton(bool en, int x)
    {
        Button button = new Button { Location = new Point(x, 20), Size = new Size(52, 36), Image = Flag(en), FlatStyle = FlatStyle.Flat, AccessibleName = en ? "English" : "Nederlands", TabStop = true };
        button.Click += delegate { useEnglish = en; Render(); };
        ToolTip tip = new ToolTip(); tip.SetToolTip(button, en ? "English" : "Nederlands");
        return button;
    }
    internal static Bitmap Flag(bool en)
    {
        Bitmap bitmap = new Bitmap(30, 20);
        using (Graphics g = Graphics.FromImage(bitmap)) {
            if (!en) {
                g.Clear(Color.White); g.FillRectangle(Brushes.Firebrick, 0, 0, 30, 7); g.FillRectangle(Brushes.DarkBlue, 0, 14, 30, 6);
            } else {
                g.Clear(Color.FromArgb(1, 33, 105));
                using (Pen white = new Pen(Color.White, 5)) { g.DrawLine(white, 0, 0, 30, 20); g.DrawLine(white, 0, 20, 30, 0); }
                using (Pen red = new Pen(Color.FromArgb(200, 16, 46), 2)) { g.DrawLine(red, 0, 0, 30, 20); g.DrawLine(red, 0, 20, 30, 0); }
                g.FillRectangle(Brushes.White, 11, 0, 8, 20); g.FillRectangle(Brushes.White, 0, 6, 30, 8);
                g.FillRectangle(Brushes.Firebrick, 13, 0, 4, 20); g.FillRectangle(Brushes.Firebrick, 0, 8, 30, 4);
            }
        }
        return bitmap;
    }
    string Localize(string text) { return useEnglish ? Translate(text) : text; }
    static string Translate(string text)
    {
        string[,] translations = {
            { "Hoe gezond is je batterij?", "How healthy is your battery?" },
            { "Opnieuw meten", "Refresh" },
            { "Capaciteit volgens de laptopfirmware.", "Capacity reported by the laptop firmware." },
            { "Gebruiksduur is een schatting van Windows.", "Runtime is an estimate provided by Windows." },
            { "Batterijgegevens ophalen…", "Reading battery data…" },
            { "Batterijgegevens konden niet worden uitgelezen.", "Battery data could not be read." },
            { "Probeer opnieuw. Sommige laptops geven deze gegevens niet door.", "Try again. Some laptops do not report this data." },
            { "Geen bruikbare batterijcapaciteit gevonden.", "No usable battery capacity found." },
            { "Resterende capaciteit ten opzichte van nieuw", "Remaining capacity compared with new" },
            { "Resterende capaciteit van ", "Combined remaining capacity of " },
            { " batterijen samen", " batteries" },
            { "Oorspronkelijke capaciteit:", "Design capacity:" },
            { "Volle laadcapaciteit nu:", "Current full-charge capacity:" },
            { "Laadcycli:", "Cycle count:" },
            { "Huidig laadniveau:", "Current charge level:" },
            { "Laadstatus:", "Charging status:" },
            { "Geschatte gebruiksduur:", "Estimated runtime:" },
            { "Niet van toepassing (netstroom / opladen)", "Not applicable (AC power / charging)" },
            { "Aan het opladen", "Charging" }, { "Netstroom", "AC power" },
            { "Werkt op batterij", "Running on battery" },
            { "Niet beschikbaar", "Not available" }, { "Geen batterij", "No battery" }, { "Onbekend", "Unknown" },
            { "Minder dan 1 minuut", "Less than 1 minute" }, { " uur ", " hr " },
            { "Boven 100% kan voorkomen door fabrieksmarge of kalibratie.", "Above 100% can occur due to factory margin or calibration." }
        };
        for (int i = 0; i < translations.GetLength(0); i++) text = text.Replace(translations[i, 0], translations[i, 1]);
        return Regex.Replace(text, @"\d{1,3}(?:\.\d{3})+(?:,\d+)?|\d+,\d+", delegate(Match m) {
            return Decimal.Parse(m.Value, CultureInfo.GetCultureInfo("nl-NL")).ToString(m.Value.Contains(",") ? "N1" : "N0", CultureInfo.GetCultureInfo("en-GB"));
        });
    }
    void Render()
    {
        title.Text = Localize("Hoe gezond is je batterij?"); refresh.Text = Localize("Opnieuw meten");
        foot.Text = Localize("Capaciteit volgens de laptopfirmware.\nGebruiksduur is een schatting van Windows.");
        dutch.FlatAppearance.BorderSize = useEnglish ? 1 : 3; english.FlatAppearance.BorderSize = useEnglish ? 3 : 1;
        dutch.FlatAppearance.BorderColor = useEnglish ? Color.LightGray : Color.RoyalBlue;
        english.FlatAppearance.BorderColor = useEnglish ? Color.RoyalBlue : Color.LightGray;
        if (worker.IsBusy) { status.Text = Localize("Batterijgegevens ophalen…"); health.Text = "…"; details.Text = ""; }
        else if (readFailed) { status.Text = Localize("Batterijgegevens konden niet worden uitgelezen."); health.Text = Localize("Onbekend"); details.Text = Localize("Probeer opnieuw. Sommige laptops geven deze gegevens niet door."); }
        else if (lastResult != null) { status.Text = Localize(lastResult.Status); health.Text = Localize(lastResult.Health); details.Text = Localize(lastResult.Details); health.ForeColor = lastResult.Color; }
    }
    void Measure() { if (worker.IsBusy) return; refresh.Enabled = false; worker.RunWorkerAsync(); Render(); }
    class Result { public string Status, Health, Details; public Color Color = Color.FromArgb(32, 92, 190); }
    class Battery { public double Design, Full; public uint? Cycles; }
    [StructLayout(LayoutKind.Sequential)]
    struct PowerStatus {
        public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatus;
        public uint BatteryLifeTime, BatteryFullLifeTime;
    }
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetSystemPowerStatus(out PowerStatus status);
    static string CurrentPowerDetails()
    {
        PowerStatus p;
        if (!GetSystemPowerStatus(out p)) return "Huidig laadniveau: Niet beschikbaar\nLaadstatus: Niet beschikbaar\nGeschatte gebruiksduur: Niet beschikbaar";
        if (p.BatteryFlag != 255 && (p.BatteryFlag & 128) != 0) return "Huidig laadniveau: Geen batterij\nLaadstatus: Geen batterij\nGeschatte gebruiksduur: Niet beschikbaar";
        string level = p.BatteryLifePercent <= 100 ? p.BatteryLifePercent + "%" : "Niet beschikbaar";
        bool charging = p.BatteryFlag != 255 && (p.BatteryFlag & 8) != 0;
        string state = charging ? "Aan het opladen" : p.ACLineStatus == 1 ? "Netstroom" : p.ACLineStatus == 0 ? "Werkt op batterij" : "Niet beschikbaar";
        string runtime = "Niet beschikbaar";
        if (p.ACLineStatus == 1 || charging) runtime = "Niet van toepassing (netstroom / opladen)";
        else if (p.BatteryLifeTime != UInt32.MaxValue) {
            TimeSpan time = TimeSpan.FromSeconds(p.BatteryLifeTime);
            runtime = p.BatteryLifeTime < 60 ? "Minder dan 1 minuut" : ((int)time.TotalHours > 0 ? (int)time.TotalHours + " uur " : "") + time.Minutes + " min";
        }
        return "Huidig laadniveau: " + level + "\nLaadstatus: " + state + "\nGeschatte gebruiksduur: " + runtime;
    }
    static Dictionary<string, Battery> Query(string cls, string field, Action<Battery, double> setter, Dictionary<string, Battery> rows)
    {
        using (ManagementObjectSearcher search = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM " + cls))
        using (ManagementObjectCollection found = search.Get())
        foreach (ManagementObject item in found)
        using (item) {
            string key = Convert.ToString(item["InstanceName"]);
            if (String.IsNullOrEmpty(key)) continue;
            Battery battery; if (!rows.TryGetValue(key, out battery)) { battery = new Battery(); rows.Add(key, battery); }
            if (item[field] != null) setter(battery, Convert.ToDouble(item[field]));
        }
        return rows;
    }
    static Result Read()
    {
        Dictionary<string, Battery> rows = new Dictionary<string, Battery>();
        Query("BatteryStaticData", "DesignedCapacity", delegate(Battery b, double v) { b.Design = v; }, rows);
        Query("BatteryFullChargedCapacity", "FullChargedCapacity", delegate(Battery b, double v) { b.Full = v; }, rows);
        try { Query("BatteryCycleCount", "CycleCount", delegate(Battery b, double v) { b.Cycles = (uint)v; }, rows); } catch (ManagementException) { }
        double design = 0, full = 0; int count = 0; List<string> cycles = new List<string>();
        foreach (Battery b in rows.Values) {
            if (b.Design <= 0 || b.Full <= 0 || b.Design >= UInt32.MaxValue || b.Full >= UInt32.MaxValue) continue;
            design += b.Design; full += b.Full; count++;
            cycles.Add(b.Cycles.HasValue && b.Cycles.Value < UInt32.MaxValue ? b.Cycles.Value.ToString() : "Niet beschikbaar");
        }
        if (count == 0) return new Result { Status = "Geen bruikbare batterijcapaciteit gevonden.", Health = "Onbekend", Details = "Oorspronkelijke capaciteit: Niet beschikbaar\nVolle laadcapaciteit nu: Niet beschikbaar\nLaadcycli: Niet beschikbaar\n\n" + CurrentPowerDetails() };
        double percent = full / design * 100;
        return new Result {
            Status = count == 1 ? "Resterende capaciteit ten opzichte van nieuw" : "Resterende capaciteit van " + count + " batterijen samen",
            Health = percent.ToString("0.0", CultureInfo.GetCultureInfo("nl-NL")) + "%",
            Color = percent >= 80 ? Color.FromArgb(25, 128, 91) : Color.FromArgb(177, 106, 20),
            Details = "Oorspronkelijke capaciteit:  " + design.ToString("N0", CultureInfo.GetCultureInfo("nl-NL")) + " mWh\nVolle laadcapaciteit nu:         " + full.ToString("N0", CultureInfo.GetCultureInfo("nl-NL")) + " mWh\nLaadcycli:  " + String.Join(" / ", cycles.ToArray()) + "\n\n" + CurrentPowerDetails() + (percent > 100 ? "\nBoven 100% kan voorkomen door fabrieksmarge of kalibratie." : "")
        };
    }
    [STAThread] static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--check") {
            try { Result r = Read(); Console.WriteLine(r.Status + "\n" + r.Health + "\n" + r.Details); }
            catch (Exception ex) { Console.WriteLine(ex.GetType().Name + ": " + ex.Message); Environment.ExitCode = 1; }
            return;
        }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new BatteryHealth());
    }
}
