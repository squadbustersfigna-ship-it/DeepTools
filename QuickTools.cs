using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DeepTools
{
    // Автозапуск DeepTools вместе с Windows.
    // Через Планировщик задач (schtasks), а НЕ через ключ Run: программе нужны права
    // администратора (requireAdministrator), а admin-приложения Windows не запускает
    // из ключа Run при входе (там нельзя показать UAC). Задача с флагом /RL HIGHEST
    // и триггером ONLOGON стартует программу уже с правами и без окна UAC.
    // По умолчанию выключено - задача создаётся, только когда пользователь включит тумблер
    public static class AutoStart
    {
        private const string TaskName = "DeepTools";
        private const string OldRunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        public static bool Enabled
        {
            get { return RunSchtasks("/Query /TN \"" + TaskName + "\"") == 0; }
            set
            {
                CleanupOldRunEntry();
                if (value)
                {
                    string exe = Application.ExecutablePath;
                    RunSchtasks("/Create /TN \"" + TaskName + "\" /TR \"\\\"" + exe + "\\\"\" /SC ONLOGON /RL HIGHEST /F");
                }
                else
                {
                    RunSchtasks("/Delete /TN \"" + TaskName + "\" /F");
                }
            }
        }

        // Убираем старую запись из ключа Run (осталась от версий до 1.3.x, всё равно не работала)
        private static void CleanupOldRunEntry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(OldRunKey, true))
                {
                    if (key != null && key.GetValue(TaskName) != null) key.DeleteValue(TaskName, false);
                }
            }
            catch { }
        }

        private static int RunSchtasks(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("schtasks.exe", args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.StandardOutput.ReadToEnd();
                    p.StandardError.ReadToEnd();
                    p.WaitForExit(8000);
                    return p.ExitCode;
                }
            }
            catch { return -1; }
        }
    }

    // Быстрые действия питания и системы
    public static class PowerTools
    {
        // Перезагрузка сразу в прошивку (BIOS/UEFI)
        public static void RestartToFirmware()
        {
            try { Process.Start("shutdown.exe", "/r /fw /t 0"); } catch { }
        }

        // Быстрый запуск Windows (гибридное завершение). HKLM - нужны права админа
        public static bool FastStartupEnabled
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                        "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power"))
                    {
                        if (key == null) return true;
                        object v = key.GetValue("HiberbootEnabled");
                        return v == null || Convert.ToInt32(v) != 0;
                    }
                }
                catch { return true; }
            }
            set
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(
                        "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power"))
                    {
                        if (key != null) key.SetValue("HiberbootEnabled", value ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
                catch { }
            }
        }

        public static void RestartExplorer()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("explorer");
                for (int i = 0; i < procs.Length; i++)
                {
                    try { procs[i].Kill(); } catch { }
                }
                System.Threading.Thread.Sleep(500);
                Process.Start("explorer.exe");
            }
            catch { }
        }
    }
}
