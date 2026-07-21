using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace DeepTools
{
    public class Program
    {
        [STAThread]
        static void Main()
        {
            // Должно быть до первого обращения к встроенным сборкам (датчики температур)
            EmbeddedAssemblies.Install();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppConfig.Load();

            // При первом запуске выбираем язык по языку Windows:
            // русская система - русский, любая другая - английский
            string savedLang = AppConfig.Get("language", "");
            if (savedLang == "")
            {
                bool systemIsRussian = System.Globalization.CultureInfo
                    .CurrentUICulture.TwoLetterISOLanguageName == "ru";
                savedLang = systemIsRussian ? "ru" : "en";
                AppConfig.Set("language", savedLang);
            }
            Lang.IsEn = savedLang == "en";
            Theme.Apply(AppConfig.Get("theme", "dark") == "light");

            bool isAdmin = IsRunningAsAdmin();

            // exe собран с манифестом requireAdministrator, поэтому Windows сама показывает UAC.
            // Этот блок - подстраховка на случай запуска в обход манифеста
            if (!isAdmin)
            {
                if (TryRelaunchAsAdmin())
                {
                    return; // текущий процесс завершится, новый запустится с правами
                }
            }

            Application.Run(new MainForm(isAdmin));
        }

        // Проверка, запущено ли приложение от имени администратора
        public static bool IsRunningAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Перезапуск текущего exe с запросом прав администратора через UAC
        public static bool TryRelaunchAsAdmin()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                ProcessStartInfo startInfo = new ProcessStartInfo(exePath);
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                Process.Start(startInfo);
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Пользователь нажал "Нет" в окне UAC - просто продолжаем без прав
                return false;
            }
        }
    }
}
