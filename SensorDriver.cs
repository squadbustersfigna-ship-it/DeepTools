using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DeepTools
{
    // Установщик драйвера датчиков PawnIO. PawnIO - подписанный драйвер, которого нет
    // в блок-листе Windows, поэтому LibreHardwareMonitor может читать через него датчики
    // даже когда обычный WinRing0 заблокирован «Целостностью памяти» в Windows 11.
    // Установщик встроен в exe как ресурс (см. build.ps1), извлекается и запускается
    public static class SensorDriver
    {
        private const string ResourceName = "DeepTools.PawnIO_setup.exe";

        public static void InstallBundled()
        {
            try
            {
                string path = ExtractInstaller();
                if (path == null)
                {
                    // Запасной вариант: файл рядом с exe (dev-сборка без встраивания)
                    string local = Path.Combine(Application.StartupPath, "PawnIO_setup.exe");
                    if (File.Exists(local)) path = local;
                }
                if (path == null)
                {
                    MessageBox.Show(
                        Lang.T("Установщик PawnIO не найден.", "PawnIO installer not found."),
                        "DeepTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var psi = new ProcessStartInfo(path) { UseShellExecute = true };
                Process.Start(psi);

                MessageBox.Show(
                    Lang.T("Пройди установку PawnIO, затем перезапусти DeepTools — температуры должны появиться.",
                           "Complete the PawnIO setup, then restart DeepTools — temperatures should appear."),
                    "DeepTools", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.T("Не удалось запустить установщик: ", "Failed to launch installer: ") + ex.Message,
                    "DeepTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string ExtractInstaller()
        {
            try
            {
                Assembly self = Assembly.GetExecutingAssembly();
                using (Stream s = self.GetManifestResourceStream(ResourceName))
                {
                    if (s == null) return null;
                    string path = Path.Combine(Path.GetTempPath(), "PawnIO_setup.exe");
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        s.CopyTo(fs);
                    }
                    return path;
                }
            }
            catch { return null; }
        }
    }
}
