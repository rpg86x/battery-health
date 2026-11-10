using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

// Records battery sessions: how long the laptop runs on battery, and how long charging takes.
// Runs as a hidden background process ("Battery Health.exe --log") and appends finished sessions to a CSV.
static class SessionLog
{
    internal static string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Battery Health");
    internal static string CsvPath { get { return Path.Combine(Folder, "sessions.csv"); } }
    static string StatePath { get { return Path.Combine(Folder, "session-state.txt"); } }
    internal const int IntervalSeconds = 30;
    const string Header = "type;start;end;duration_min;awake_min;start_pct;end_pct;est_full_hours";
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

    internal class State { public string Mode = "idle"; public DateTime Start, Last; public int StartPct, LastPct; public double GapSeconds; }
    internal class Session { public string Type; public DateTime Start, End; public double DurationMin, AwakeMin, EstFullHours; public int StartPct, EndPct; }

    [StructLayout(LayoutKind.Sequential)]
    struct PowerStatus { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatus; public uint BatteryLifeTime, BatteryFullLifeTime; }
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetSystemPowerStatus(out PowerStatus status);

    public static void Run()
    {
        bool created;
        using (Mutex mutex = new Mutex(true, "Local\\BatteryHealthSessionLogger", out created))
        {
            if (!created) return; // already running
            while (true)
            {
                try { Sample(DateTime.Now); } catch (Exception ex) { Note(ex.GetType().Name + ": " + ex.Message); }
                Thread.Sleep(IntervalSeconds * 1000);
            }
        }
    }

    internal static void StartBackground()
    {
        try { Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--log") { UseShellExecute = false, CreateNoWindow = true }); } catch { }
    }

    static void Sample(DateTime now)
    {
        PowerStatus p;
        if (!GetSystemPowerStatus(out p) || p.BatteryLifePercent > 100) return;
        if (p.BatteryFlag != 255 && (p.BatteryFlag & 128) != 0) return; // no battery
        bool charging = p.BatteryFlag != 255 && (p.BatteryFlag & 8) != 0;
        string mode = p.ACLineStatus == 0 ? "discharge" : charging ? "charge" : "idle";
        Step(mode, p.BatteryLifePercent, now);
    }

    // One measurement. Separate from Sample so it can be tested with simulated readings.
    internal static void Step(string mode, int pct, DateTime now)
    {
        Directory.CreateDirectory(Folder);
        State s = LoadState();
        bool bigGap = s.Mode != "idle" && (now - s.Last).TotalSeconds > IntervalSeconds * 3; // sleep, hibernate or shutdown
        if (mode != s.Mode)
        {
            // After a long gap the session really ended at the last reading before the gap.
            if (s.Mode != "idle") { if (bigGap) Close(s, s.Last, s.LastPct); else Close(s, now, pct); }
            s = new State { Mode = mode, Start = now, StartPct = pct };
        }
        else if (bigGap) s.GapSeconds += (now - s.Last).TotalSeconds - IntervalSeconds;
        s.Last = now; s.LastPct = pct;
        SaveState(s);
    }

    static void Close(State s, DateTime end, int endPct)
    {
        double duration = (end - s.Start).TotalMinutes;
        double awake = Math.Max(0, duration - s.GapSeconds / 60);
        int delta = s.Mode == "discharge" ? s.StartPct - endPct : endPct - s.StartPct;
        if (duration < 2 || delta < 1) return; // brief unplug or no measurable change
        double est = awake / 60 / delta * 100;
        bool newFile = !File.Exists(CsvPath);
        using (StreamWriter w = new StreamWriter(CsvPath, true))
        {
            if (newFile) w.WriteLine(Header);
            w.WriteLine(String.Join(";", new string[] {
                s.Mode, s.Start.ToString(TimeFormat, Inv), end.ToString(TimeFormat, Inv),
                duration.ToString("0.0", Inv), awake.ToString("0.0", Inv),
                s.StartPct.ToString(Inv), endPct.ToString(Inv), est.ToString("0.00", Inv) }));
        }
    }

    internal static State LoadState()
    {
        State s = new State();
        if (!File.Exists(StatePath)) return s;
        try
        {
            Dictionary<string, string> v = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(StatePath)) { int i = line.IndexOf('='); if (i > 0) v[line.Substring(0, i)] = line.Substring(i + 1); }
            s.Mode = v["mode"];
            s.Start = DateTime.ParseExact(v["start"], TimeFormat, Inv); s.Last = DateTime.ParseExact(v["last"], TimeFormat, Inv);
            s.StartPct = Int32.Parse(v["startPct"], Inv); s.LastPct = Int32.Parse(v["lastPct"], Inv);
            s.GapSeconds = Double.Parse(v["gapSeconds"], Inv);
        }
        catch { s = new State(); }
        return s;
    }

    static void SaveState(State s)
    {
        string tmp = StatePath + ".tmp";
        File.WriteAllLines(tmp, new string[] {
            "mode=" + s.Mode, "start=" + s.Start.ToString(TimeFormat, Inv), "last=" + s.Last.ToString(TimeFormat, Inv),
            "startPct=" + s.StartPct.ToString(Inv), "lastPct=" + s.LastPct.ToString(Inv), "gapSeconds=" + s.GapSeconds.ToString("0", Inv) });
        if (File.Exists(StatePath)) File.Delete(StatePath);
        File.Move(tmp, StatePath);
    }

    internal static List<Session> ReadSessions()
    {
        List<Session> list = new List<Session>();
        if (!File.Exists(CsvPath)) return list;
        foreach (string line in File.ReadAllLines(CsvPath))
        {
            string[] f = line.Split(';');
            if (f.Length < 8 || f[0] == "type") continue;
            try
            {
                list.Add(new Session {
                    Type = f[0], Start = DateTime.ParseExact(f[1], TimeFormat, Inv), End = DateTime.ParseExact(f[2], TimeFormat, Inv),
                    DurationMin = Double.Parse(f[3], Inv), AwakeMin = Double.Parse(f[4], Inv),
                    StartPct = Int32.Parse(f[5], Inv), EndPct = Int32.Parse(f[6], Inv), EstFullHours = Double.Parse(f[7], Inv) });
            }
            catch { }
        }
        return list;
    }

    static void Note(string text)
    {
        try { Directory.CreateDirectory(Folder); File.AppendAllText(Path.Combine(Folder, "logger-errors.txt"), DateTime.Now.ToString(TimeFormat, Inv) + " " + text + Environment.NewLine); } catch { }
    }

    internal static string Span(double minutes)
    {
        int m = (int)Math.Round(minutes);
        return m >= 60 ? (m / 60) + " h " + (m % 60).ToString("00") + " min" : m + " min";
    }
}

class SessionsForm : Form
{
    readonly bool en;
    readonly Label current = new Label(), summary = new Label();
    readonly ListView list = new ListView();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = SessionLog.IntervalSeconds * 1000 };

    string T(string nl, string english) { return en ? english : nl; }

    public SessionsForm(bool useEnglish)
    {
        en = useEnglish;
        Text = T("Batterijsessies", "Battery sessions"); ClientSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(245, 247, 251); Font = new Font("Segoe UI", 10);
        Label title = new Label { Text = T("Hoe lang gaat een acculading mee?", "How long does a charge last?"), Location = new Point(20, 16), Size = new Size(720, 34), Font = new Font("Segoe UI", 16, FontStyle.Bold) };
        current.SetBounds(22, 56, 716, 46); current.Font = new Font("Segoe UI", 11, FontStyle.Bold); current.ForeColor = Color.FromArgb(32, 92, 190);
        summary.SetBounds(22, 104, 716, 66);
        list.SetBounds(20, 176, 720, 324); list.View = View.Details; list.FullRowSelect = true; list.GridLines = true;
        list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        list.Columns.Add(T("Soort", "Type"), 110); list.Columns.Add(T("Begin", "Start"), 140); list.Columns.Add(T("Duur", "Duration"), 100);
        list.Columns.Add(T("Wakker", "Awake"), 100); list.Columns.Add(T("Lading", "Charge"), 100); list.Columns.Add(T("Schatting 0–100%", "Est. 0–100%"), 150);
        Button folder = new Button { Text = T("Open CSV-map", "Open CSV folder"), Location = new Point(20, 512), Size = new Size(160, 34), FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        folder.Click += delegate { System.IO.Directory.CreateDirectory(SessionLog.Folder); Process.Start("explorer.exe", "\"" + SessionLog.Folder + "\""); };
        Label note = new Label { Text = T("Wakker = tijd zonder slaapstand. Schattingen gebruiken wakkere tijd.", "Awake = time excluding sleep. Estimates use awake time."), Location = new Point(196, 518), Size = new Size(544, 30), Font = new Font("Segoe UI", 9), ForeColor = Color.DimGray, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        Controls.AddRange(new Control[] { title, current, summary, list, folder, note });
        timer.Tick += delegate { Fill(); }; timer.Start();
        FormClosed += delegate { timer.Dispose(); };
        Fill();
    }

    string Kind(string type) { return type == "discharge" ? T("Op batterij", "On battery") : T("Opladen", "Charging"); }

    void Fill()
    {
        SessionLog.State s = SessionLog.LoadState();
        bool live = s.Mode != "idle" && (DateTime.Now - s.Last).TotalMinutes < 3;
        if (!live) current.Text = T("Nu geen sessie bezig (netstroom en vol, of logger staat uit).", "No session in progress (AC and full, or logger not running).");
        else
        {
            double min = (s.Last - s.Start).TotalMinutes - s.GapSeconds / 60;
            current.Text = Kind(s.Mode) + T(" sinds ", " since ") + s.Start.ToString("HH:mm") + " — " + SessionLog.Span(min) + ", " + s.StartPct + "% → " + s.LastPct + "%";
        }

        List<SessionLog.Session> all = SessionLog.ReadSessions();
        string dis = Average(all, "discharge"), chg = Average(all, "charge");
        summary.Text = T("Gemiddeld volle accu (100→0%): ", "Average full battery (100→0%): ") + dis + "\n"
                     + T("Gemiddeld volledig opladen (0→100%): ", "Average full charge (0→100%): ") + chg + "\n"
                     + T("Op basis van de laatste 10 sessies van minstens 10% verschil.", "Based on the last 10 sessions with at least 10% change.");

        list.BeginUpdate(); list.Items.Clear();
        for (int i = all.Count - 1; i >= 0; i--)
        {
            SessionLog.Session x = all[i];
            list.Items.Add(new ListViewItem(new string[] {
                Kind(x.Type), x.Start.ToString("ddd d MMM HH:mm", en ? CultureInfo.GetCultureInfo("en-GB") : CultureInfo.GetCultureInfo("nl-NL")),
                SessionLog.Span(x.DurationMin), SessionLog.Span(x.AwakeMin), x.StartPct + "% → " + x.EndPct + "%", SessionLog.Span(x.EstFullHours * 60) }));
        }
        list.EndUpdate();
    }

    string Average(List<SessionLog.Session> all, string type)
    {
        double sum = 0; int n = 0;
        for (int i = all.Count - 1; i >= 0 && n < 10; i--)
            if (all[i].Type == type && Math.Abs(all[i].StartPct - all[i].EndPct) >= 10) { sum += all[i].EstFullHours; n++; }
        return n == 0 ? T("nog geen gegevens", "no data yet") : SessionLog.Span(sum / n * 60) + " (" + n + ")";
    }
}
