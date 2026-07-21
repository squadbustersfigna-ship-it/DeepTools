using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace DeepTools
{
    // Трекер игровых сессий. GameBoosterPanel сообщает о найденной игре (детект уже есть),
    // трекер копит статистику (средний CPU, макс. температуры), а когда процесс игры
    // завершается - шлёт отчёт в трей и дописывает сессию в журнал для статистики времени.
    // Журнал: AppData\DeepTools\gametime.log, строка = имя|дата|секунды|срCPU|максCPU°|максGPU°
    public static class GameSessionTracker
    {
        private const int MinSessionSec = 180; // случайные развороты окон на пару минут не считаем

        private static Process proc;
        private static string procName;
        private static DateTime start;
        private static double cpuSum;
        private static int cpuSamples;
        private static int maxCpuTemp = -1;
        private static int maxGpuTemp = -1;

        public static string LogPath
        {
            get
            {
                return Path.Combine(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeepTools"),
                    "gametime.log");
            }
        }

        // Зовётся из детекта GameBooster, когда полноэкранная игра на переднем плане
        public static void OnGameDetected(Process p)
        {
            if (proc != null)
            {
                // Уже следим за игрой; новую начинаем, только если старая закрылась
                bool exited = true;
                try { exited = proc.HasExited; } catch { }
                if (!exited) return;
                Finish();
            }

            proc = p;
            try { procName = p.ProcessName; } catch { procName = "game"; }
            start = DateTime.Now;
            cpuSum = 0;
            cpuSamples = 0;
            maxCpuTemp = -1;
            maxGpuTemp = -1;
        }

        // Зовётся каждый тик таймера детекта (~1.5 сек), даже когда игра свёрнута
        public static void Tick()
        {
            if (proc == null) return;

            bool exited = true;
            try { exited = proc.HasExited; } catch { }
            if (exited)
            {
                Finish();
                return;
            }

            if (SystemStats.CpuLoadNum >= 0)
            {
                cpuSum += SystemStats.CpuLoadNum;
                cpuSamples++;
            }
            if (SystemStats.CpuTempNum > maxCpuTemp) maxCpuTemp = SystemStats.CpuTempNum;
            if (SystemStats.GpuTempNum > maxGpuTemp) maxGpuTemp = SystemStats.GpuTempNum;
        }

        private static void Finish()
        {
            string name = procName;
            double durationSec = (DateTime.Now - start).TotalSeconds;
            double avgCpu = cpuSamples > 0 ? cpuSum / cpuSamples : -1;
            int cpuT = maxCpuTemp;
            int gpuT = maxGpuTemp;
            proc = null;

            if (durationSec < MinSessionSec) return;

            try
            {
                string dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string line = name.Replace("|", "_") + "|" + start.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                    + "|" + (int)durationSec + "|" + (avgCpu < 0 ? "-1" : avgCpu.ToString("0", CultureInfo.InvariantCulture))
                    + "|" + cpuT + "|" + gpuT;
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch { }

            // Отчёт после игры
            string stats = "";
            if (avgCpu >= 0) stats += Lang.T("средний CPU ", "avg CPU ") + avgCpu.ToString("0") + "%";
            if (cpuT > 0) stats += (stats == "" ? "" : ", ") + Lang.T("макс. CPU ", "max CPU ") + cpuT + "°C";
            if (gpuT > 0) stats += (stats == "" ? "" : ", ") + Lang.T("макс. GPU ", "max GPU ") + gpuT + "°C";

            TrayNotify.Info(
                name + " — " + FormatDuration((int)durationSec),
                stats == "" ? Lang.T("Сессия записана в статистику", "Session saved to statistics") : stats);
        }

        public static string FormatDuration(int seconds)
        {
            int h = seconds / 3600;
            int m = (seconds % 3600) / 60;
            if (h > 0) return h + Lang.T(" ч ", " h ") + m + Lang.T(" мин", " min");
            if (m > 0) return m + Lang.T(" мин", " min");
            return seconds + Lang.T(" сек", " s");
        }
    }

    // Окно статистики: сколько времени в какой игре, всего и за неделю
    public class GameTimeForm : Form
    {
        private class GameStat
        {
            public string Name;
            public int TotalSec;
            public int WeekSec;
            public int Sessions;
            public DateTime LastPlayed;
        }

        private Point dragStart;
        private bool draggingForm = false;

        public GameTimeForm()
        {
            Text = "DeepTools Game Time";
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(540, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.BgColor;
            DoubleBuffered = true;
            ShowInTaskbar = false;

            BuildUi();
            Load += (s, e) => ApplyRoundedRegion();
        }

        private void ApplyRoundedRegion()
        {
            var path = new GraphicsPath();
            int r = 12, d = r * 2;
            var rect = new Rectangle(0, 0, Width, Height);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            Region = new Region(path);
        }

        private void BuildUi()
        {
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.BgColor };
            titleBar.MouseDown += (s, e) => { draggingForm = true; dragStart = new Point(e.X, e.Y); };
            titleBar.MouseMove += (s, e) => {
                if (draggingForm) Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y);
            };
            titleBar.MouseUp += (s, e) => { draggingForm = false; };
            Controls.Add(titleBar);

            var titleLbl = new Label
            {
                Text = Lang.T("🕒 Время в играх", "🕒 Game time"),
                ForeColor = Theme.TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(18, 9),
                AutoSize = true
            };
            titleBar.Controls.Add(titleLbl);

            var closeBtn = new Label
            {
                Text = "✕",
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 11F),
                Size = new Size(30, 26),
                Location = new Point(Width - 42, 7),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => Close();
            closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = Theme.Danger;
            closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = Theme.TextDim;
            titleBar.Controls.Add(closeBtn);

            List<GameStat> stats = LoadStats();

            int totalAll = 0;
            int totalWeek = 0;
            foreach (GameStat g in stats) { totalAll += g.TotalSec; totalWeek += g.WeekSec; }

            var summaryLabel = new Label
            {
                Text = stats.Count == 0
                    ? Lang.T("Пока пусто. Сессии от 3 минут записываются автоматически, когда GameBooster видит игру",
                             "Nothing yet. Sessions over 3 minutes are recorded automatically when GameBooster detects a game")
                    : Lang.T("Всего: ", "Total: ") + GameSessionTracker.FormatDuration(totalAll)
                        + Lang.T("   За 7 дней: ", "   Last 7 days: ") + GameSessionTracker.FormatDuration(totalWeek),
                ForeColor = stats.Count == 0 ? Theme.TextDim : Theme.Accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(18, 48),
                Size = new Size(504, 34)
            };
            Controls.Add(summaryLabel);

            var card = Theme.MakeCard(this, new Point(16, 88), new Size(508, 352));

            var header = new Label
            {
                Text = Lang.T("Игра", "Game"),
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8F),
                Location = new Point(16, 10),
                AutoSize = true
            };
            card.Controls.Add(header);

            var headerTotal = new Label
            {
                Text = Lang.T("Всего / за неделю", "Total / this week"),
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8F),
                Location = new Point(300, 10),
                AutoSize = true
            };
            card.Controls.Add(headerTotal);

            var list = new FlowLayoutPanel
            {
                Location = new Point(10, 30),
                Size = new Size(488, 312),
                BackColor = Theme.SidebarColor,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            card.Controls.Add(list);

            foreach (GameStat g in stats)
            {
                var row = new Panel { Size = new Size(464, 40), BackColor = Color.Transparent, Margin = new Padding(2, 1, 0, 1) };

                var nameLbl = new Label
                {
                    Text = g.Name + ".exe",
                    ForeColor = Theme.TextMain,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Location = new Point(6, 3),
                    Size = new Size(280, 18),
                    AutoEllipsis = true
                };
                row.Controls.Add(nameLbl);

                var sessLbl = new Label
                {
                    Text = Lang.T("сессий: ", "sessions: ") + g.Sessions
                        + Lang.T(", последняя ", ", last ") + g.LastPlayed.ToString("dd.MM HH:mm"),
                    ForeColor = Theme.TextDim,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 7.5F),
                    Location = new Point(6, 22),
                    AutoSize = true
                };
                row.Controls.Add(sessLbl);

                var timeLbl = new Label
                {
                    Text = GameSessionTracker.FormatDuration(g.TotalSec) + " / " + GameSessionTracker.FormatDuration(g.WeekSec),
                    ForeColor = Theme.Accent,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Location = new Point(290, 10),
                    AutoSize = true
                };
                row.Controls.Add(timeLbl);

                list.Controls.Add(row);
            }
        }

        private List<GameStat> LoadStats()
        {
            var byName = new Dictionary<string, GameStat>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(GameSessionTracker.LogPath))
                {
                    string[] lines = File.ReadAllLines(GameSessionTracker.LogPath);
                    DateTime weekAgo = DateTime.Now.AddDays(-7);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string[] parts = lines[i].Split('|');
                        if (parts.Length < 3) continue;

                        DateTime when;
                        int sec;
                        if (!DateTime.TryParseExact(parts[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out when)) continue;
                        if (!int.TryParse(parts[2], out sec)) continue;

                        GameStat stat;
                        if (!byName.TryGetValue(parts[0], out stat))
                        {
                            stat = new GameStat { Name = parts[0] };
                            byName[parts[0]] = stat;
                        }
                        stat.TotalSec += sec;
                        if (when >= weekAgo) stat.WeekSec += sec;
                        stat.Sessions++;
                        if (when > stat.LastPlayed) stat.LastPlayed = when;
                    }
                }
            }
            catch { }

            var result = new List<GameStat>(byName.Values);
            result.Sort((a, b) => b.TotalSec.CompareTo(a.TotalSec));
            return result;
        }
    }
}
