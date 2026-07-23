using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DeepTools
{
    // Автозапуск DeepTools вместе с Windows (ключ реестра HKCU\...\Run).
    // По умолчанию выключен - добавляется, только когда пользователь включит тумблер
    public static class AutoStart
    {
        private const string RunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string ValueName = "DeepTools";

        public static bool Enabled
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
                        return key != null && key.GetValue(ValueName) != null;
                }
                catch { return false; }
            }
            set
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
                    {
                        if (key == null) return;
                        if (value) key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"");
                        else key.DeleteValue(ValueName, false);
                    }
                }
                catch { }
            }
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
