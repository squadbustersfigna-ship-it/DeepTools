using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeepTools
{
    // FPS-оверлей поверх игр: маленькое полупрозрачное окно в углу экрана.
    // FPS считается через ETW-провайдер DWM недоступен без спец. прав, поэтому
    // используем счётчик кадров через частоту обновления composition (DwmFlush):
    // считаем, сколько раз в секунду успевает пройти цикл ожидания vsync.
    // Это даёт FPS композитора для окна игры в borderless/windowed режиме.
    // Дополнительно показываем загрузку CPU/RAM и температуру GPU (NVML).
    public class OverlayForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

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

        private PerformanceCounter cpuCounter;
        private float cpuValue = 0;
        private int gpuTemp = -1;

        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            Opacity = 0.82;
            Size = new Size(180, 92);
            Location = new Point(16, 16);
            DoubleBuffered = true;

            try { cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
            catch { cpuCounter = null; }

            uiTimer.Interval = 1000;
            uiTimer.Tick += (s, e) => RefreshStats();

            Load += (s, e) => {
                // Кликов не перехватываем: WS_EX_TRANSPARENT пропускает мышь сквозь оверлей,
                // NOACTIVATE не даёт оверлею красть фокус у игры
                int style = GetWindowLong(Handle, GWL_EXSTYLE);
                SetWindowLong(Handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                StartFpsCounter();
                uiTimer.Start();
            };
            FormClosed += (s, e) => StopFpsCounter();
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        // Поток меряет FPS композитора: DwmFlush блокируется до следующего vsync-кадра.
        // Считаем кадры за секунду - на глаз совпадает с частотой обновления сцены
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
                        currentFps = (int)(frames * 1000L / sw.ElapsedMilliseconds);
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
            gpuTemp = NvmlGpu.GetTemperature();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var path = Theme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 10))
            using (var bg = new SolidBrush(Color.FromArgb(16, 20, 28)))
            using (var border = new Pen(Color.FromArgb(60, 70, 90)))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }

            // FPS крупно
            Color fpsColor = currentFps >= 60 ? Color.FromArgb(46, 214, 140)
                : currentFps >= 30 ? Color.FromArgb(240, 180, 70)
                : Color.FromArgb(230, 90, 90);
            using (var fpsFont = new Font("Segoe UI", 22F, FontStyle.Bold))
            using (var fpsBrush = new SolidBrush(fpsColor))
            {
                g.DrawString(currentFps.ToString(), fpsFont, fpsBrush, 10, 6);
            }
            using (var lblFont = new Font("Segoe UI", 8F))
            using (var lblBrush = new SolidBrush(Color.FromArgb(150, 160, 175)))
            {
                g.DrawString("FPS", lblFont, lblBrush, 14, 44);
            }

            // CPU и GPU справа
            using (var statFont = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var statBrush = new SolidBrush(Color.FromArgb(220, 226, 235)))
            using (var dimBrush = new SolidBrush(Color.FromArgb(150, 160, 175)))
            using (var smallFont = new Font("Segoe UI", 8F))
            {
                g.DrawString("CPU", smallFont, dimBrush, 92, 10);
                g.DrawString(string.Format("{0:0}%", cpuValue), statFont, statBrush, 126, 8);

                g.DrawString("GPU", smallFont, dimBrush, 92, 34);
                g.DrawString(gpuTemp >= 0 ? gpuTemp + "°C" : "-", statFont, statBrush, 126, 32);

                g.DrawString("RAM", smallFont, dimBrush, 92, 58);
                g.DrawString(string.Format("{0:0}%", GetRamPercent()), statFont, statBrush, 126, 56);
            }
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
}
