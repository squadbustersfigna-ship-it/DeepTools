using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DeepTools
{
    // Одна категория для очистки (например Lang.T("Temp пользователя", "User Temp"))
    public class CleanupCategory
    {
        public string DisplayName;
        public string Path;
        public bool RequiresAdmin;
        public long SizeBytes;
        public int FileCount;
        public bool Scanned;
    }

    public class SmartCleanupPanel : Panel
    {
        private List<CleanupCategory> categories = new List<CleanupCategory>();
        private List<CheckBox> categoryChecks = new List<CheckBox>();
        private List<Label> categorySizeLabels = new List<Label>();
        private Label totalLabel;
        private Label statusLabel;

        public SmartCleanupPanel()
        {
            Size = new Size(700, 566);
            BackColor = Theme.BgColor;

            BuildCategoryList();
            BuildUi();
        }

        private void BuildCategoryList()
        {
            CleanupCategory temp = new CleanupCategory();
            temp.DisplayName = Lang.T("Temp пользователя", "User Temp");
            temp.Path = System.IO.Path.GetTempPath();
            temp.RequiresAdmin = false;
            categories.Add(temp);

            CleanupCategory winTemp = new CleanupCategory();
            winTemp.DisplayName = "Windows Temp";
            winTemp.Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            winTemp.RequiresAdmin = true;
            categories.Add(winTemp);

            CleanupCategory prefetch = new CleanupCategory();
            prefetch.DisplayName = "Prefetch";
            prefetch.Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            prefetch.RequiresAdmin = true;
            categories.Add(prefetch);

            // Кэши шейдеров: разрастаются до десятков ГБ, а "протухший" кэш вызывает
            // статтеры. После очистки игры пересоберут их сами (первый запуск чуть дольше)
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            CleanupCategory dxCache = new CleanupCategory();
            dxCache.DisplayName = Lang.T("Кэш шейдеров DirectX", "DirectX shader cache");
            dxCache.Path = System.IO.Path.Combine(localAppData, "D3DSCache");
            dxCache.RequiresAdmin = false;
            categories.Add(dxCache);

            CleanupCategory nvCache = new CleanupCategory();
            nvCache.DisplayName = Lang.T("Кэш шейдеров NVIDIA", "NVIDIA shader cache");
            nvCache.Path = System.IO.Path.Combine(localAppData, "NVIDIA\\DXCache");
            nvCache.RequiresAdmin = false;
            categories.Add(nvCache);

            CleanupCategory steamShader = new CleanupCategory();
            steamShader.DisplayName = Lang.T("Кэш шейдеров Steam", "Steam shader cache");
            steamShader.Path = FindSteamShaderCache();
            steamShader.RequiresAdmin = false;
            categories.Add(steamShader);
        }

        // Папка shadercache в установке Steam; ищем по стандартным путям и конфигу
        private string FindSteamShaderCache()
        {
            string custom = AppConfig.Get("steam_path", "");
            string[] roots = {
                custom,
                "C:\\Program Files (x86)\\Steam",
                "C:\\Program Files\\Steam"
            };
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.IsNullOrEmpty(roots[i])) continue;
                string cache = System.IO.Path.Combine(roots[i], "steamapps\\shadercache");
                if (Directory.Exists(cache)) return cache;
            }
            // не нашли - вернём стандартный путь, категория честно покажет 0 байт
            return "C:\\Program Files (x86)\\Steam\\steamapps\\shadercache";
        }

        private void BuildUi()
        {
            var titleLbl = new Label
            {
                Text = "SmartCleanup",
                ForeColor = Theme.TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(24, 24),
                AutoSize = true
            };
            Controls.Add(titleLbl);

            int y = 70;
            for (int i = 0; i < categories.Count; i++)
            {
                CleanupCategory cat = categories[i];
                var card = Theme.MakeCard(this, new Point(24, y), new Size(650, 54));

                bool blocked = cat.RequiresAdmin && !Program.IsRunningAsAdmin();

                var check = new CheckBox
                {
                    Text = cat.DisplayName,
                    ForeColor = blocked ? Theme.TextDim : Theme.TextMain,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Location = new Point(20, 6),
                    AutoSize = true,
                    Checked = !blocked,
                    Enabled = !blocked
                };
                card.Controls.Add(check);
                categoryChecks.Add(check);

                var pathLbl = new Label
                {
                    Text = cat.Path,
                    ForeColor = Theme.TextDim,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8F),
                    Location = new Point(40, 30),
                    AutoSize = true
                };
                card.Controls.Add(pathLbl);

                var sizeLbl = new Label
                {
                    Text = blocked ? Lang.T("Нужны права администратора", "Administrator rights required") : Lang.T("Не просканировано", "Not scanned"),
                    ForeColor = blocked ? Theme.Warning : Theme.TextDim,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9F),
                    Location = new Point(460, 16),
                    AutoSize = true
                };
                card.Controls.Add(sizeLbl);
                categorySizeLabels.Add(sizeLbl);

                y += 62;
            }

            var scanBtn = new RoundedButton
            {
                Text = Lang.T("Сканировать", "Scan"),
                ButtonColor = Theme.KeyColor,
                HoverColor = Theme.KeyHover,
                TextColor = Theme.TextMain,
                Location = new Point(24, y + 10),
                Size = new Size(140, 36)
            };
            scanBtn.Click += (s, e) => ScanAll();
            Controls.Add(scanBtn);

            var cleanBtn = new RoundedButton
            {
                Text = Lang.T("Очистить выбранное", "Clean selected"),
                ButtonColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                TextColor = Theme.BgColor,
                Location = new Point(174, y + 10),
                Size = new Size(160, 36)
            };
            cleanBtn.Click += (s, e) => CleanSelected();
            Controls.Add(cleanBtn);

            var debloatBtn = new RoundedButton
            {
                Text = Lang.T("Встроенный мусор Windows", "Windows bloatware"),
                ButtonColor = Theme.KeyColor,
                HoverColor = Theme.KeyHover,
                TextColor = Theme.TextMain,
                Location = new Point(344, y + 10),
                Size = new Size(210, 36)
            };
            debloatBtn.Click += (s, e) => {
                using (var f = new DebloatForm())
                {
                    f.ShowDialog(FindForm());
                }
            };
            Controls.Add(debloatBtn);

            totalLabel = new Label
            {
                Text = Lang.T("Всего к очистке: 0 Б", "Total to clean: 0 B"),
                ForeColor = Theme.TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(24, y + 54),
                AutoSize = true
            };
            Controls.Add(totalLabel);

            statusLabel = new Label
            {
                Text = "",
                ForeColor = Theme.Accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(24, y + 78),
                AutoSize = true
            };
            Controls.Add(statusLabel);
        }

        private void ScanAll()
        {
            long total = 0;
            for (int i = 0; i < categories.Count; i++)
            {
                CleanupCategory cat = categories[i];
                if (cat.RequiresAdmin && !Program.IsRunningAsAdmin()) continue;

                long size;
                int count;
                GetDirectorySize(cat.Path, out size, out count);
                cat.SizeBytes = size;
                cat.FileCount = count;
                cat.Scanned = true;

                categorySizeLabels[i].Text = FormatSize(size) + ", " + count + Lang.T(" файлов", " files");
                categorySizeLabels[i].ForeColor = Theme.TextMain;

                if (categoryChecks[i].Checked) total += size;
            }
            totalLabel.Text = Lang.T("Всего к очистке: ", "Total to clean: ") + FormatSize(total);
            statusLabel.Text = Lang.T("Сканирование завершено", "Scan complete");
        }

        private void CleanSelected()
        {
            long total = 0;
            List<CleanupCategory> toClean = new List<CleanupCategory>();
            for (int i = 0; i < categories.Count; i++)
            {
                if (categoryChecks[i].Checked && categoryChecks[i].Enabled)
                {
                    if (!categories[i].Scanned)
                    {
                        MessageBox.Show(Lang.T("Сначала нажми \"Сканировать\", чтобы увидеть, что будет удалено.", "Press Scan first to see what will be deleted."), "DeepTools",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    total += categories[i].SizeBytes;
                    toClean.Add(categories[i]);
                }
            }

            if (toClean.Count == 0)
            {
                statusLabel.Text = Lang.T("Нечего очищать - ничего не выбрано", "Nothing to clean - nothing selected");
                return;
            }

            DialogResult result = MessageBox.Show(
                Lang.T("Будет очищено примерно ", "Approximately ") + FormatSize(total) + Lang.T(".\n\nПродолжить?", " will be cleaned.\n\nContinue?"),
                Lang.T("Подтверждение очистки", "Cleanup confirmation"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            long freed = 0;
            for (int i = 0; i < toClean.Count; i++)
            {
                freed += CleanFolderContents(toClean[i].Path);
            }

            statusLabel.Text = Lang.T("Очищено ", "Cleaned ") + FormatSize(freed) + Lang.T(" (часть файлов могла быть занята и пропущена)", " (some files may have been in use and skipped)");
            ScanAll();
        }

        // Считает суммарный размер файлов внутри папки (рекурсивно), пропуская недоступные файлы
        private void GetDirectorySize(string path, out long size, out int count)
        {
            size = 0;
            count = 0;
            if (!Directory.Exists(path)) return;

            string[] files;
            try { files = Directory.GetFiles(path, "*", SearchOption.AllDirectories); }
            catch { return; }

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    FileInfo info = new FileInfo(files[i]);
                    size += info.Length;
                    count++;
                }
                catch
                {
                    // файл мог исчезнуть или быть недоступен - пропускаем
                }
            }
        }

        // Удаляет содержимое папки, саму папку оставляет. Возвращает сколько байт реально освободили
        private long CleanFolderContents(string path)
        {
            long freed = 0;
            if (!Directory.Exists(path)) return freed;

            // Удаляем файлы во всех подпапках
            string[] files;
            try { files = Directory.GetFiles(path, "*", SearchOption.AllDirectories); }
            catch { return freed; }

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    FileInfo info = new FileInfo(files[i]);
                    long len = info.Length;
                    File.Delete(files[i]);
                    freed += len;
                }
                catch
                {
                    // файл занят или недоступен - пропускаем
                }
            }

            // Пытаемся удалить пустые подпапки
            try
            {
                string[] dirs = Directory.GetDirectories(path);
                for (int i = 0; i < dirs.Length; i++)
                {
                    try
                    {
                        Directory.Delete(dirs[i], true);
                    }
                    catch { }
                }
            }
            catch { }

            return freed;
        }

        private string FormatSize(long bytes)
        {
            double kb = bytes / 1024.0;
            double mb = kb / 1024.0;
            double gb = mb / 1024.0;

            if (gb >= 1) return gb.ToString("0.##") + Lang.T(" ГБ", " GB");
            if (mb >= 1) return mb.ToString("0.#") + Lang.T(" МБ", " MB");
            if (kb >= 1) return kb.ToString("0.#") + Lang.T(" КБ", " KB");
            return bytes + Lang.T(" Б", " B");
        }
    }
}