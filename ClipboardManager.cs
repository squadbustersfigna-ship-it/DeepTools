using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DeepTools
{
    public class ClipboardEntry
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsPinned { get; set; }
        public string Type { get; set; } // "text", "image", "link"

        public string DisplayText
        {
            get
            {
                string preview = Content.Length > 50 ? Content.Substring(0, 50) + "..." : Content;
                return preview.Replace("\n", " ").Replace("\r", "");
            }
        }
    }

    public class ClipboardManager
    {
        private List<ClipboardEntry> history = new List<ClipboardEntry>();
        private string lastClipboard = "";
        private Timer monitoringTimer;
        private int maxHistorySize = 100;

        public event EventHandler HistoryChanged;

        public ClipboardManager()
        {
            monitoringTimer = new Timer { Interval = 1000 };
            monitoringTimer.Tick += (s, e) => MonitorClipboard();
            LoadHistory();
        }

        public void Start()
        {
            monitoringTimer.Start();
        }

        public void Stop()
        {
            monitoringTimer.Stop();
        }

        private void MonitorClipboard()
        {
            try
            {
                IDataObject data = Clipboard.GetDataObject();
                if (data == null) return;

                if (data.GetDataPresent(DataFormats.Text))
                {
                    string text = (string)data.GetData(DataFormats.Text);
                    if (!string.IsNullOrEmpty(text) && text != lastClipboard)
                    {
                        lastClipboard = text;
                        AddEntry(text, "text");
                    }
                }
            }
            catch { }
        }

        public void AddEntry(string content, string type = "text")
        {
            var entry = new ClipboardEntry
            {
                Id = Guid.NewGuid().ToString().Substring(0, 8),
                Content = content,
                Timestamp = DateTime.Now,
                IsPinned = false,
                Type = type
            };

            history.Insert(0, entry);

            if (history.Count > maxHistorySize)
                history.RemoveAt(history.Count - 1);

            SaveHistory();
            if (HistoryChanged != null)
                HistoryChanged(this, EventArgs.Empty);
        }

        public List<ClipboardEntry> GetHistory()
        {
            var pinned = history.Where(e => e.IsPinned).ToList();
            var unpinned = history.Where(e => !e.IsPinned).ToList();
            return pinned.Concat(unpinned).ToList();
        }

        public List<ClipboardEntry> Search(string query)
        {
            return history.Where(e => e.Content.ToLower().Contains(query.ToLower())).ToList();
        }

        public void PinEntry(string id)
        {
            var entry = history.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                entry.IsPinned = !entry.IsPinned;
                SaveHistory();
                if (HistoryChanged != null)
                    HistoryChanged(this, EventArgs.Empty);
            }
        }

        public void RemoveEntry(string id)
        {
            history.RemoveAll(e => e.Id == id);
            SaveHistory();
            if (HistoryChanged != null)
                HistoryChanged(this, EventArgs.Empty);
        }

        public void ClearHistory()
        {
            history.Clear();
            SaveHistory();
            if (HistoryChanged != null)
                HistoryChanged(this, EventArgs.Empty);
        }

        public void RestoreEntry(string id)
        {
            var entry = history.FirstOrDefault(e => e.Id == id);
            if (entry != null && entry.Type == "text")
            {
                Clipboard.SetText(entry.Content);
            }
        }

        private void SaveHistory()
        {
            // TODO: Implement file saving for history persistence
        }

        private void LoadHistory()
        {
            // TODO: Implement file loading for history persistence
        }
    }
}