using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DepixClicker
{
    // Тумблер-переключатель (кастомный контрол)
    public class ToggleSwitch : Control
    {
        public bool Checked = false;
        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            Size = new Size(46, 24);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            Invalidate();
            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color trackOn = Color.FromArgb(46, 214, 140);
            Color trackOff = Color.FromArgb(55, 62, 80);
            Color track = Checked ? trackOn : trackOff;

            using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2))
            using (var brush = new SolidBrush(track))
            {
                g.FillPath(brush, path);
            }

            int d = Height - 6;
            int x = Checked ? Width - d - 3 : 3;
            using (var thumb = new SolidBrush(Color.White))
            {
                g.FillEllipse(thumb, x, 3, d, d);
            }
        }

        private GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Слайдер (кастомный контрол)
    public class NeonSlider : Control
    {
        public int Minimum = 10;
        public int Maximum = 1000;
        private int _value = 100;
        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(Minimum, Math.Min(Maximum, value)); Invalidate(); }
        }
        public event EventHandler ValueChanged;
        private bool dragging = false;

        public NeonSlider()
        {
            Height = 24;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            dragging = true;
            UpdateFromMouse(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging) UpdateFromMouse(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
        }

        private void UpdateFromMouse(int x)
        {
            int pad = 8;
            float ratio = (float)(x - pad) / (Width - pad * 2);
            ratio = Math.Max(0, Math.Min(1, ratio));
            Value = Minimum + (int)Math.Round(ratio * (Maximum - Minimum));
            if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int pad = 8;
            int trackY = Height / 2;

            using (var trackPen = new Pen(Color.FromArgb(55, 62, 80), 4))
            {
                g.DrawLine(trackPen, pad, trackY, Width - pad, trackY);
            }

            float ratio = (float)(Value - Minimum) / (Maximum - Minimum);
            int fillX = pad + (int)(ratio * (Width - pad * 2));

            using (var fillPen = new Pen(Color.FromArgb(46, 214, 140), 4))
            {
                g.DrawLine(fillPen, pad, trackY, fillX, trackY);
            }

            using (var thumbBrush = new SolidBrush(Color.White))
            using (var thumbBorder = new Pen(Color.FromArgb(46, 214, 140), 2))
            {
                g.FillEllipse(thumbBrush, fillX - 7, trackY - 7, 14, 14);
                g.DrawEllipse(thumbBorder, fillX - 7, trackY - 7, 14, 14);
            }
        }
    }

    // Главная форма
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const int HOTKEY_ID = 1;
        private const uint VK_F8 = 0x77;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;
        private const int WM_HOTKEY = 0x0312;

        private static readonly Color BgColor = Color.FromArgb(15, 20, 31);
        private static readonly Color CardColor = Color.FromArgb(22, 28, 43);
        private static readonly Color BorderColor = Color.FromArgb(38, 46, 64);
        private static readonly Color Accent = Color.FromArgb(46, 214, 140);
        private static readonly Color TextMain = Color.FromArgb(232, 236, 243);
        private static readonly Color TextDim = Color.FromArgb(138, 147, 166);

        private bool running = false;
        private bool useRightClick = false;
        private System.Windows.Forms.Timer clickTimer = new System.Windows.Forms.Timer();

        private Label statusLabel;
        private Panel statusDot;
        private ToggleSwitch powerToggle;
        private ToggleSwitch mouseModeToggle;
        private Label lkmLabel;
        private Label pkmLabel;
        private Label intervalValueLabel;
        private TextBox intervalEditBox;
        private NeonSlider slider;
        private Label cpsNumber;

        private Panel opacityPanel;
        private NeonSlider opacitySlider;
        private Label opacityValueLabel;

        private Point dragStart;
        private bool draggingForm = false;

        public MainForm()
        {
            Text = "DepixClicker";
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(640, 360);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgColor;
            DoubleBuffered = true;

            try { Icon = new Icon("logo.ico"); } catch { }

            BuildUi();

            clickTimer.Tick += (s, e) => DoClick();
            Load += (s, e) => ApplyRoundedRegion();
            Resize += (s, e) => ApplyRoundedRegion();
        }

        private void ApplyRoundedRegion()
        {
            var path = new GraphicsPath();
            int r = 16, d = r * 2;
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
            // Шапка окна
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = BgColor };
            titleBar.MouseDown += (s, e) => { draggingForm = true; dragStart = new Point(e.X, e.Y); };
            titleBar.MouseMove += (s, e) => {
                if (draggingForm) Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y);
            };
            titleBar.MouseUp += (s, e) => { draggingForm = false; };

            var dot = new Panel { Size = new Size(10, 10), Location = new Point(16, 12), BackColor = Color.Transparent };
            dot.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(Accent)) e.Graphics.FillEllipse(b, 0, 0, 10, 10);
            };
            titleBar.Controls.Add(dot);

            var titleLbl = new Label
            {
                Text = "DepixClicker v1.2",
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(34, 7),
                AutoSize = true
            };
            titleLbl.MouseDown += (s, e) => { draggingForm = true; dragStart = new Point(e.X, e.Y); };
            titleLbl.MouseMove += (s, e) => {
                if (draggingForm) Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y);
            };
            titleLbl.MouseUp += (s, e) => { draggingForm = false; };
            titleBar.Controls.Add(titleLbl);

            var closeBtn = new Label
            {
                Text = "\u2715",
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 11F),
                Size = new Size(30, 26),
                Location = new Point(Width - 40, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => Close();
            closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = Color.FromArgb(240, 90, 90);
            closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = TextDim;
            titleBar.Controls.Add(closeBtn);

            var minimizeBtn = new Label
            {
                Text = "\u2013",
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Size = new Size(30, 26),
                Location = new Point(Width - 72, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            minimizeBtn.Click += (s, e) => { WindowState = FormWindowState.Minimized; };
            minimizeBtn.MouseEnter += (s, e) => minimizeBtn.ForeColor = TextMain;
            minimizeBtn.MouseLeave += (s, e) => minimizeBtn.ForeColor = TextDim;
            titleBar.Controls.Add(minimizeBtn);

            var gearBtn = new Label
            {
                Text = "\u2699",
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 12F),
                Size = new Size(30, 26),
                Location = new Point(Width - 104, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            gearBtn.Click += (s, e) => { opacityPanel.Visible = !opacityPanel.Visible; opacityPanel.BringToFront(); };
            gearBtn.MouseEnter += (s, e) => gearBtn.ForeColor = TextMain;
            gearBtn.MouseLeave += (s, e) => gearBtn.ForeColor = TextDim;
            titleBar.Controls.Add(gearBtn);

            Controls.Add(titleBar);

            // Левая карточка: статус, вкл-выкл, кнопка мыши
            var leftCard = MakeCard(new Point(12, 46), new Size(300, 302));

            statusDot = new Panel { Size = new Size(14, 14), Location = new Point(18, 18), BackColor = Color.Transparent };
            statusDot.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color c = running ? Accent : TextDim;
                using (var glow = new SolidBrush(Color.FromArgb(60, c)))
                    e.Graphics.FillEllipse(glow, -3, -3, 20, 20);
                using (var b = new SolidBrush(c))
                    e.Graphics.FillEllipse(b, 0, 0, 14, 14);
            };
            leftCard.Controls.Add(statusDot);

            statusLabel = new Label
            {
                Text = "СТАТУС: ОСТАНОВЛЕН",
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(42, 12),
                AutoSize = true
            };
            leftCard.Controls.Add(statusLabel);

            var powerRowLabel = new Label
            {
                Text = "Автокликер вкл/выкл",
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(18, 64),
                AutoSize = true
            };
            leftCard.Controls.Add(powerRowLabel);

            powerToggle = new ToggleSwitch { Location = new Point(240, 58), Checked = false };
            powerToggle.CheckedChanged += (s, e) => SetRunning(powerToggle.Checked);
            leftCard.Controls.Add(powerToggle);

            var mouseRowLabel = new Label
            {
                Text = "Кнопка мыши",
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(18, 118),
                AutoSize = true
            };
            leftCard.Controls.Add(mouseRowLabel);

            lkmLabel = new Label
            {
                Text = "ЛКМ",
                ForeColor = Accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(18, 148),
                AutoSize = true
            };
            leftCard.Controls.Add(lkmLabel);

            mouseModeToggle = new ToggleSwitch { Location = new Point(60, 144), Checked = false };
            mouseModeToggle.CheckedChanged += (s, e) => SetMouseMode(mouseModeToggle.Checked);
            leftCard.Controls.Add(mouseModeToggle);

            pkmLabel = new Label
            {
                Text = "ПКМ",
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(112, 148),
                AutoSize = true
            };
            leftCard.Controls.Add(pkmLabel);

            var footer = new Label
            {
                Text = "Нажми F8: Старт/Пауза",
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(18, 250),
                AutoSize = true
            };
            leftCard.Controls.Add(footer);

            var hotkeyFooter = new Label
            {
                Text = "Горячие клавиши: F8",
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(18, 270),
                AutoSize = true
            };
            leftCard.Controls.Add(hotkeyFooter);

            // Правая карточка: интервал и CPS
            var rightCard = MakeCard(new Point(324, 46), new Size(304, 302));

            var intervalLabel = new Label
            {
                Text = "Интервал кликов (мс):",
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(18, 16),
                AutoSize = true
            };
            rightCard.Controls.Add(intervalLabel);

            intervalValueLabel = new Label
            {
                Text = "100",
                ForeColor = Accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Location = new Point(200, 12),
                AutoSize = true,
                Cursor = Cursors.IBeam
            };
            intervalValueLabel.Click += (s, e) => ShowIntervalEditor();
            rightCard.Controls.Add(intervalValueLabel);

            intervalEditBox = new TextBox
            {
                Location = new Point(200, 10),
                Size = new Size(60, 24),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 37, 54),
                ForeColor = TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                MaxLength = 4
            };
            intervalEditBox.KeyPress += (s, e) => {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true;
            };
            intervalEditBox.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { CommitIntervalEditor(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape) { intervalEditBox.Visible = false; intervalValueLabel.Visible = true; }
            };
            intervalEditBox.Leave += (s, e) => CommitIntervalEditor();
            rightCard.Controls.Add(intervalEditBox);

            slider = new NeonSlider { Location = new Point(18, 50), Size = new Size(268, 24), Minimum = 10, Maximum = 1000, Value = 100 };
            slider.ValueChanged += (s, e) => UpdateInterval();
            rightCard.Controls.Add(slider);

            var minLbl = new Label { Text = "10", ForeColor = TextDim, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8F), Location = new Point(18, 76), AutoSize = true };
            var maxLbl = new Label { Text = "1000", ForeColor = TextDim, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8F), Location = new Point(262, 76), AutoSize = true };
            rightCard.Controls.Add(minLbl);
            rightCard.Controls.Add(maxLbl);

            cpsNumber = new Label
            {
                Text = "10",
                ForeColor = Accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 30F, FontStyle.Bold),
                AutoSize = true
            };
            rightCard.Controls.Add(cpsNumber);

            var cpsSub = new Label
            {
                Text = "КЛИКОВ/СЕК",
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F),
                AutoSize = true
            };
            rightCard.Controls.Add(cpsSub);

            cpsNumber.Location = new Point((304 - cpsNumber.PreferredWidth) / 2, 180);
            cpsSub.Location = new Point((304 - cpsSub.PreferredWidth) / 2, 232);

            // Плавающая панель прозрачности (скрыта по умолчанию)
            opacityPanel = new Panel { Size = new Size(220, 66), Location = new Point(404, 36), BackColor = CardColor, Visible = false };
            opacityPanel.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, opacityPanel.Width - 1, opacityPanel.Height - 1);
                using (var path = RoundedRect(rect, 10))
                using (var brush = new SolidBrush(CardColor))
                using (var pen = new Pen(BorderColor))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            var opacityTitle = new Label
            {
                Text = "Прозрачность окна",
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(12, 8),
                AutoSize = true
            };
            opacityPanel.Controls.Add(opacityTitle);

            opacityValueLabel = new Label
            {
                Text = "100%",
                ForeColor = Accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(178, 8),
                AutoSize = true
            };
            opacityPanel.Controls.Add(opacityValueLabel);

            opacitySlider = new NeonSlider { Location = new Point(12, 32), Size = new Size(196, 24), Minimum = 40, Maximum = 100, Value = 100 };
            opacitySlider.ValueChanged += (s, e) => {
                Opacity = opacitySlider.Value / 100.0;
                opacityValueLabel.Text = opacitySlider.Value + "%";
            };
            opacityPanel.Controls.Add(opacitySlider);

            Controls.Add(opacityPanel);
            opacityPanel.BringToFront();

            UpdateInterval();
            UpdateMouseModeLabels();
        }

        private Panel MakeCard(Point loc, Size size)
        {
            var card = new Panel { Location = loc, Size = size, BackColor = CardColor };
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (var path = RoundedRect(rect, 10))
                using (var brush = new SolidBrush(CardColor))
                using (var pen = new Pen(BorderColor))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            Controls.Add(card);
            return card;
        }

        private GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ShowIntervalEditor()
        {
            intervalEditBox.Text = slider.Value.ToString();
            intervalEditBox.Visible = true;
            intervalValueLabel.Visible = false;
            intervalEditBox.Focus();
            intervalEditBox.SelectAll();
        }

        private void CommitIntervalEditor()
        {
            if (!intervalEditBox.Visible) return;
            int v;
            if (int.TryParse(intervalEditBox.Text, out v))
            {
                slider.Value = v;
                UpdateInterval();
            }
            intervalEditBox.Visible = false;
            intervalValueLabel.Visible = true;
        }

        private void UpdateInterval()
        {
            intervalValueLabel.Text = slider.Value.ToString();
            clickTimer.Interval = slider.Value;
            double cps = 1000.0 / slider.Value;
            cpsNumber.Text = cps >= 10 ? Math.Round(cps).ToString() : cps.ToString("0.#");
            cpsNumber.Location = new Point((304 - cpsNumber.PreferredWidth) / 2, cpsNumber.Location.Y);
        }

        private void SetMouseMode(bool rightClick)
        {
            useRightClick = rightClick;
            UpdateMouseModeLabels();
        }

        private void UpdateMouseModeLabels()
        {
            lkmLabel.ForeColor = useRightClick ? TextDim : Accent;
            pkmLabel.ForeColor = useRightClick ? Accent : TextDim;
        }

        private void SetRunning(bool value)
        {
            running = value;
            clickTimer.Enabled = running;
            statusLabel.Text = running ? "СТАТУС: РАБОТАЕТ" : "СТАТУС: ОСТАНОВЛЕН";
            statusLabel.ForeColor = running ? Accent : TextDim;
            powerToggle.Checked = running;
            powerToggle.Invalidate();
            statusDot.Invalidate();
        }

        private void ToggleRunning()
        {
            SetRunning(!running);
        }

        private void DoClick()
        {
            if (useRightClick)
            {
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            }
            else
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RegisterHotKey(this.Handle, HOTKEY_ID, 0, VK_F8);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleRunning();
            }
            base.WndProc(ref m);
        }
    }
}