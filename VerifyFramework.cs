using System;
using System.Reflection;
using System.Windows.Forms;
class VerifyFramework
{
    [STAThread] static int Main(string[] args)
    {
        try {
            if (args.Length == 2 && args[0] == "--network") {
                AndroidReading r = CompanionBridge.Read(args[1], "0123456789abcdef0123456789abcdef", "12345678-1234-1234-1234-123456789abc");
                if (!r.Online || r.Level != 72 || r.Model != "Example only") throw new Exception("Actual Java-server to Windows-client connection failed");
                if (!CompanionBridge.Read(args[1], "ffffffffffffffffffffffffffffffff", null).AccessDenied) throw new Exception("Windows client failed to detect wrong access key");
                Console.WriteLine("PASS: actual Java-to-Windows HTTP connection and rejected wrong access key."); return 0;
            }
            CompanionBridge.SelfTest(); Console.WriteLine("PASS: Android app JSON protocol, identity checks, missing values and key validation.");
            AndroidBridge.SelfTest(); Console.WriteLine("PASS: Existing ADB parsing and rediscovery.");
            using (AndroidDashboard form = new AndroidDashboard(false)) {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(AndroidDashboard);
                TextBox keyInput = (TextBox)type.GetField("pairAddress", flags).GetValue(form);
                ComboBox modes = (ComboBox)type.GetField("connectionMode", flags).GetValue(form);
                if (modes.SelectedIndex != 0 || !keyInput.UseSystemPasswordChar) throw new Exception("APK mode not selected by default");
                keyInput.Text = "0123456789abcdef0123456789abcdef";
                type.GetField("english", flags).SetValue(form, true); type.GetMethod("Render", flags).Invoke(form, null);
                if (keyInput.Text.Length != 32 || !keyInput.UseSystemPasswordChar) throw new Exception("Language switch cleared the entered access key");
                modes.SelectedIndex = 1;
                if (keyInput.UseSystemPasswordChar) throw new Exception("ADB address field is hidden");
                Console.WriteLine("PASS: APK/ADB switching, localized UI and access-key entry.");
            }
            return 0;
        } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
