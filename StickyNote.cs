using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DeepTools
{
    // Заметки-стикеры на рабочем столе. Живут независимо от главного окна,
    // сохраняются в AppData и восстанавливаются при запуске программы.
    // Файл заметки: первая строка "x|y|w|h", остальное - текст
    public static class NotesManager
    {
        private static readonly List<StickyNoteForm> open = new List<StickyNoteForm>();

        public static string Dir
        {
            get
            {
                return Path.Combine(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeepTools"),
                    "notes");
            }
        }

        public static void CreateNew()
        {
            string id = Guid.NewGuid().ToString("N").Substring(0, 12);
            var wa = Screen.PrimaryScreen.WorkingArea;
            var bounds = new Rectangle(wa.Left + wa.Width / 2 - 110, wa.Top + wa.Height / 2 - 90, 220, 180);
            Spawn(id, "", bounds);
        }

        public static void RestoreAll()
        {
            try
            {
                if (!Directory.Exists(Dir)) return;
                string[] files = Directory.GetFiles(Dir, "*.txt");
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        string id = Path.GetFileNameWithoutExtension(files[i]);
                        string[] lines = File.ReadAllLines(files[i]);
                        Rectangle b = ParseBounds(lines.Length > 0 ? lines[0] : "");
                        string content = lines.Length > 1 ? string.Join(Environment.NewLine, Sub(lines, 1)) : "";
                        Spawn(id, content, b);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void Spawn(string id, string content, Rectangle bounds)
        {
            var note = new StickyNoteForm(id, content, bounds);
            note.FormClosed += (s, e) => open.Remove(note);
            open.Add(note);
            note.Show();
        }

        public static void Save(string id, Rectangle bounds, string content)
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                var lines = new List<string>();
                lines.Add(bounds.X + "|" + bounds.Y + "|" + bounds.Width + "|" + bounds.Height);
                lines.AddRange((content ?? "").Replace("\r\n", "\n").Split('\n'));
                File.WriteAllLines(Path.Combine(Dir, id + ".txt"), lines.ToArray());
            }
            catch { }
        }

        public static void Delete(string id)
        {
            try
            {
                string path = Path.Combine(Dir, id + ".txt");
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        private static Rectangle ParseBounds(string line)
        {
            try
            {
                string[] p = line.Split('|');
                if (p.Length >= 4)
                {
                    int x = int.Parse(p[0]), y = int.Parse(p[1]), w = int.Parse(p[2]), h = int.Parse(p[3]);
                    if (w < 140) w = 220;
                    if (h < 100) h = 180;
                    return new Rectangle(x, y, w, h);
                }
            }
            catch { }
            var wa = Screen.PrimaryScreen.WorkingArea;
            return new Rectangle(wa.Left + 60, wa.Top + 60, 220, 180);
        }

        private static string[] Sub(string[] arr, int from)
        {
            var list = new List<string>();
            for (int i = from; i < arr.Length; i++) list.Add(arr[i]);
            return list.ToArray();
        }
    }

    public class StickyNoteForm : Form
    {
        private static readonly Color NoteBg = Color.FromArgb(255, 232, 138);
        private static readonly Color NoteBar = Color.FromArgb(245, 214, 110);
        private static readonly Color NoteText = Color.FromArgb(45, 40, 20);

        private readonly string id;
        private TextBox textBox;
        private Point dragStart;
        private bool dragging = false;
        private bool suppressSave = true;

        public StickyNoteForm(string noteId, string content, Rectangle bounds)
        {
            id = noteId;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            MinimumSize = new Size(140, 100);
            BackColor = NoteBg;

            var bar = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = NoteBar };
            bar.MouseDown += (s, e) => { dragging = true; dragStart = new Point(e.X, e.Y); };
            bar.MouseMove += (s, e) => {
                if (dragging) Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y);
            };
            bar.MouseUp += (s, e) => { dragging = false; SaveNote(); };
            Controls.Add(bar);

            var addBtn = new Label
            {
                Text = "+",
                ForeColor = NoteText,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(24, 24),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            addBtn.Click += (s, e) => NotesManager.CreateNew();
            bar.Controls.Add(addBtn);

            var closeBtn = new Label
            {
                Text = "✕",
                ForeColor = NoteText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 24, 0)
            };
            closeBtn.Click += (s, e) => { NotesManager.Delete(id); Close(); };
            bar.Controls.Add(closeBtn);

            textBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = NoteBg,
                ForeColor = NoteText,
                Font = new Font("Segoe UI", 10F),
                ScrollBars = ScrollBars.Vertical,
                Text = content ?? ""
            };
            textBox.TextChanged += (s, e) => SaveNote();
            Controls.Add(textBox);
            textBox.BringToFront();

            // Изменение размера за правый-нижний угол
            var grip = new Label
            {
                Text = "◢",
                ForeColor = NoteText,
                Font = new Font("Segoe UI", 8F),
                Size = new Size(16, 16),
                TextAlign = ContentAlignment.BottomRight,
                Cursor = Cursors.SizeNWSE,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(Width - 16, Height - 16)
            };
            bool resizing = false; Point resizeStart = Point.Empty; Size startSize = Size.Empty;
            grip.MouseDown += (s, e) => { resizing = true; resizeStart = grip.PointToScreen(e.Location); startSize = Size; };
            grip.MouseMove += (s, e) => {
                if (!resizing) return;
                Point now = grip.PointToScreen(e.Location);
                Width = Math.Max(MinimumSize.Width, startSize.Width + (now.X - resizeStart.X));
                Height = Math.Max(MinimumSize.Height, startSize.Height + (now.Y - resizeStart.Y));
            };
            grip.MouseUp += (s, e) => { resizing = false; SaveNote(); };
            Controls.Add(grip);
            grip.BringToFront();

            LocationChanged += (s, e) => SaveNote();
            Load += (s, e) => { suppressSave = false; SaveNote(); };
        }

        private void SaveNote()
        {
            if (suppressSave) return;
            NotesManager.Save(id, Bounds, textBox.Text);
        }
    }
}
