using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeepTools
{
    // FPS-оверлей поверх игр: маленькое полупрозрачное окно в углу экрана.
    // FPS считаем через DwmFlush: он блокируется до следующего кадра композитора,
    // считаем, сколько таких кадров проходит за секунду. Это FPS композитора для
    // окна в borderless/windowed режиме. Плюс CPU/RAM, загрузка и температура GPU.
    // Что показывать, в каком углу и какого размера - настраивается в OverlayConfigForm
    // и хранится в AppConfig (см. OverlaySettings).
    public class OverlayForm : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
        private System.Threading.Thread fpsThread;
        private volatile bool fpsThreadStop = false;
        private volatile int currentFps = 0;
        private volatile float currentFrameMs = 0;

        private PerformanceCounter cpuCounter;
        private PerformanceCounter gpuCounter;
        private float cpuValue = 0;
        private float gpuLoad = -1;
        private int gpuTemp = -1;

        private OverlaySettings s;

        public OverlayForm()
        {
            s = OverlaySettings.Load();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            Opacity = 0.82;
            DoubleBuffered = true;

            try { cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
            catch { cpuCounter = null; }
            try { gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_Total"); }
            catch { gpuCounter = null; }

            LayoutBySettings();

            uiTimer.Interval = 1000;
            uiTimer.Tick += (s2, e) => RefreshStats();

            Load += (s2, e) => {
                // WS_EX_TRANSPARENT пропускает мышь сквозь оверлей, NOACTIVATE не даёт
                // ему красть фокус у игры
                int style = GetWindowLong(Handle, GWL_EXSTYLE);
                SetWindowLong(Handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                StartFpsCounter();
                uiTimer.Start();
            };
            FormClosed += (s2, e) => StopFpsCounter();
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        // Размер окна и позиция считаются из настроек: масштаб + число активных строк
        private void LayoutBySettings()
        {
            float k = s.Scale;
            int rows = s.MetricRowCount;                 // CPU/GPU/RAM строки (без FPS-блока)
            int fpsBlockH = s.ShowFps ? (int)(52 * k) : 0;
            int rowH = (int)(22 * k);
            int padTop = (int)(6 * k);
            int padBottom = (int)(8 * k);

            int w = (int)(s.HasBigFps ? 180 * k : 150 * k);
            int h = padTop + fpsBlockH + rows * rowH + padBottom;
            if (h < (int)(40 * k)) h = (int)(40 * k);
            Size = new Size(w, h);

            Rectangle b = Screen.PrimaryScreen.Bounds;
            int m = 16;
            int x, y;
            switch (s.Corner)
            {
                case 1: x = b.Right - Width - m; y = b.Top + m; break;   // верх-право
                case 2: x = b.Left + m; y = b.Bottom - Height - m; break; // низ-лево
                case 3: x = b.Right - Width - m; y = b.Bottom - Height - m; break; // низ-право
                default: x = b.Left + m; y = b.Top + m; break;           // верх-лево
            }
            Location = new Point(x, y);
        }

        // Поток меряет FPS композитора: DwmFlush ждёт следующий vsync-кадр.
        // Заодно копим время между кадрами - это frametime в мс
        private void StartFpsCounter()
        {
            fpsThreadStop = false;
            fpsThread = new System.Threading.Thread(() => {
                var sw = Stopwatch.StartNew();
                int frames = 0;
                while (!fpsThreadStop)
                {
                    try { DwmFlush(); }
                    catch { System.Threading.Thread.Sleep(16); }
                    frames++;
                    if (sw.ElapsedMilliseconds >= 1000)
                    {
                        int fps = (int)(frames * 1000L / sw.ElapsedMilliseconds);
                        currentFps = fps;
                        currentFrameMs = fps > 0 ? 1000f / fps : 0;
                        frames = 0;
                        sw.Restart();
                    }
                }
            });
            fpsThread.IsBackground = true;
            fpsThread.Start();
        }

        private void StopFpsCounter()
        {
            fpsThreadStop = true;
        }

        private void RefreshStats()
        {
            try { if (cpuCounter != null) cpuValue = cpuCounter.NextValue(); } catch { }
            try { gpuLoad = gpuCounter != null ? gpuCounter.NextValue() : -1; } catch { gpuLoad = -1; }
            gpuTemp = NvmlGpu.GetTemperature();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float k = s.Scale;

            using (var path = Theme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), (int)(10 * k)))
            using (var bg = new SolidBrush(Color.FromArgb(16, 20, 28)))
            using (var border = new Pen(Color.FromArgb(60, 70, 90)))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }

            Color dim = Color.FromArgb(150, 160, 175);
            Color main = Color.FromArgb(220, 226, 235);
            int leftPad = (int)(12 * k);
            int y = (int)(6 * k);

            // FPS крупно (+ frametime рядом, если включён)
            if (s.ShowFps)
            {
                Color fpsColor = currentFps >= 60 ? Color.FromArgb(46, 214, 140)
                    : currentFps >= 30 ? Color.FromArgb(240, 180, 70)
                    : Color.FromArgb(230, 90, 90);
                using (var fpsFont = new Font("Segoe UI", 22F * k, FontStyle.Bold))
                using (var fpsBrush = new SolidBrush(fpsColor))
                    g.DrawString(currentFps.ToString(), fpsFont, fpsBrush, leftPad - 2, y);

                using (var lblFont = new Font("Segoe UI", 8F * k))
                using (var lblBrush = new SolidBrush(dim))
                    g.DrawString("FPS", lblFont, lblBrush, leftPad + 2, y + (int)(38 * k));

                if (s.ShowFrametime)
                {
                    using (var ftFont = new Font("Segoe UI", 9F * k, FontStyle.Bold))
                    using (var ftBrush = new SolidBrush(main))
                    using (var ftLbl = new Font("Segoe UI", 8F * k))
                    using (var ftLblBrush = new SolidBrush(dim))
                    {
                        g.DrawString(string.Format("{0:0.0}", currentFrameMs), ftFont, ftBrush, leftPad + (int)(64 * k), y + (int)(6 * k));
                        g.DrawString("ms", ftLbl, ftLblBrush, leftPad + (int)(64 * k), y + (int)(26 * k));
                    }
                }
                y += (int)(52 * k);
            }

            // Строки метрик
            using (var lblFont = new Font("Segoe UI", 8.5F * k))
            using (var valFont = new Font("Segoe UI", 9F * k, FontStyle.Bold))
            using (var lblBrush = new SolidBrush(dim))
            using (var valBrush = new SolidBrush(main))
            {
                int rowH = (int)(22 * k);
                int valX = leftPad + (int)(48 * k);
                foreach (var row in BuildMetricRows())
                {
                    g.DrawString(row.Key, lblFont, lblBrush, leftPad, y + (int)(2 * k));
                    g.DrawString(row.Value, valFont, valBrush, valX, y);
                    y += rowH;
                }
            }
        }

        private List<KeyValuePair<string, string>> BuildMetricRows()
        {
            var rows = new List<KeyValuePair<string, string>>();
            if (s.ShowCpu) rows.Add(new KeyValuePair<string, string>("CPU", string.Format("{0:0}%", cpuValue)));
            if (s.ShowGpuLoad) rows.Add(new KeyValuePair<string, string>("GPU", gpuLoad >= 0 ? string.Format("{0:0}%", gpuLoad) : "-"));
            if (s.ShowGpuTemp) rows.Add(new KeyValuePair<string, string>(s.ShowGpuLoad ? "T°" : "GPU", gpuTemp >= 0 ? gpuTemp + "°C" : "-"));
            if (s.ShowRam) rows.Add(new KeyValuePair<string, string>("RAM", string.Format("{0:0}%", GetRamPercent())));
            return rows;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private float GetRamPercent()
        {
            try
            {
                var state = new MEMORYSTATUSEX();
                if (!GlobalMemoryStatusEx(state)) return 0;
                return state.dwMemoryLoad;
            }
            catch { return 0; }
        }
    }

    // Настройки оверлея: угол, размер, набор метрик. Хранятся в AppConfig
    public class OverlaySettings
    {
        public int Corner;      // 0=верх-лево, 1=верх-право, 2=низ-лево, 3=низ-право
        public int SizeIndex;   // 1=S, 2=M, 3=L
        public bool ShowFps;
        public bool ShowFrametime;
        public bool ShowCpu;
        public bool ShowGpuLoad;
        public bool ShowGpuTemp;
        public bool ShowRam;

        public float Scale
        {
            get { return SizeIndex <= 1 ? 0.85f : (SizeIndex >= 3 ? 1.2f : 1.0f); }
        }

        // Есть ли крупный FPS-блок (влияет на ширину окна)
        public bool HasBigFps { get { return ShowFps; } }

        public int MetricRowCount
        {
            get
            {
                int n = 0;
                if (ShowCpu) n++;
                if (ShowGpuLoad) n++;
                if (ShowGpuTemp) n++;
                if (ShowRam) n++;
                return n;
            }
        }

        public static OverlaySettings Load()
        {
            return new OverlaySettings
            {
                Corner = ParseInt(AppConfig.Get("overlay_corner", "0"), 0),
                SizeIndex = ParseInt(AppConfig.Get("overlay_size", "2"), 2),
                ShowFps = AppConfig.GetBool("overlay_fps", true),
                ShowFrametime = AppConfig.GetBool("overlay_frametime", false),
                ShowCpu = AppConfig.GetBool("overlay_cpu", true),
                ShowGpuLoad = AppConfig.GetBool("overlay_gpuload", false),
                ShowGpuTemp = AppConfig.GetBool("overlay_gputemp", true),
                ShowRam = AppConfig.GetBool("overlay_ram", true)
            };
        }

        public void Save()
        {
            AppConfig.Set("overlay_corner", Corner.ToString());
            AppConfig.Set("overlay_size", SizeIndex.ToString());
            AppConfig.SetBool("overlay_fps", ShowFps);
            AppConfig.SetBool("overlay_frametime", ShowFrametime);
            AppConfig.SetBool("overlay_cpu", ShowCpu);
            AppConfig.SetBool("overlay_gpuload", ShowGpuLoad);
            AppConfig.SetBool("overlay_gputemp", ShowGpuTemp);
            AppConfig.SetBool("overlay_ram", ShowRam);
        }

        private static int ParseInt(string v, int def)
        {
            int r;
            return int.TryParse(v, out r) ? r : def;
        }
    }
}
