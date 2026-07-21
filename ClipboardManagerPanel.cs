using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace DeepTools
{
    public class ClipboardManagerPanel : Panel
    {
        private ClipboardManager clipboardManager;
        private ListBox historyListBox;
        private TextBox searchBox;
        private Label statusLabel;

        public ClipboardManagerPanel()
        {
            Size = new Size(760, 616);
            BackColor = Theme.BgColor;

            clipboardManager = new ClipboardManager();
            clipboardManager.HistoryChanged += (s, e) => RefreshHistory();

            BuildUi();
            clipboardManager.Start();
        }

        private void BuildUi()
        {
            var titleLbl = new Label
            {
                Text = Lang.T("📋 Буфер обмена", "📋 Clipboard"),
                ForeColor = Theme.TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(24, 16),
                AutoSize = true
            };
            Controls.Add(titleLbl);

            // Поле поиска
            var searchLabel = new Label
            {
                Text = Lang.T("Поиск", "Search"),
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(24, 50),
                AutoSize = true
            };
            Controls.Add(searchLabel);

            searchBox = new TextBox
            {
                Location = new Point(24, 70),
                Size = new Size(712, 28),
                Font = new Font("Segoe UI", 10F),
                BackColor = Theme.InputColor,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            searchBox.TextChanged += (s, e) => SearchHistory();
            Controls.Add(searchBox);

            // Карточка с историей
            var card = Theme.MakeCard(this, new Point(24, 110), new Size(712, 460));

            historyListBox = new ListBox
            {
                Location = new Point(12, 12),
                Size = new Size(688, 380),
                BackColor = Theme.SidebarColor,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                SelectionMode = SelectionMode.One
            };
            historyListBox.DoubleClick += (s, e) => RestoreSelected();
            card.Controls.Add(historyListBox);

            // Кнопки управления
            var btnRestore = new RoundedButton
            {
                Text = Lang.T("↩️ Восстановить", "↩️ Restore"),
                ButtonColor = Theme.KeyColor,
                HoverColor = Theme.KeyHover,
                TextColor = Theme.TextMain,
                Location = new Point(12, 400),
                Size = new Size(105, 32)
            };
            btnRestore.Click += (s, e) => RestoreSelected();
            card.Controls.Add(btnRestore);

            var btnDelete = new RoundedButton
            {
                Text = Lang.T("🗑️ Удалить", "🗑️ Delete"),
                ButtonColor = Theme.KeyColor,
                HoverColor = Theme.KeyHover,
                TextColor = Theme.TextMain,
                Location = new Point(125, 400),
                Size = new Size(105, 32)
            };
            btnDelete.Click += (s, e) => DeleteSelected();
            card.Controls.Add(btnDelete);

            var btnClear = new RoundedButton
            {
                Text = Lang.T("🧹 Очистить всё", "🧹 Clear all"),
                ButtonColor = Theme.Danger,
                HoverColor = Theme.DangerHover,
                TextColor = Theme.BgColor,
                Location = new Point(570, 400),
                Size = new Size(106, 32)
            };
            btnClear.Click += (s, e) => ClearAll();
            card.Controls.Add(btnClear);

            statusLabel = new Label
            {
                Text = Lang.T("Готово", "Ready"),
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(24, 580),
                AutoSize = true
            };
            Controls.Add(statusLabel);

            RefreshHistory();
        }

        private void RefreshHistory()
        {
            historyListBox.Items.Clear();
            var entries = clipboardManager.GetHistory();
            
            foreach (var entry in entries)
            {
                string icon = entry.IsPinned ? "📌 " : "";
                string typeIcon = entry.Type == "text" ? "📄 " : "🖼️ ";
                string timeStr = entry.Timestamp.ToString("HH:mm:ss");
                historyListBox.Items.Add(icon + typeIcon + entry.DisplayText + " [" + timeStr + "]");
                historyListBox.DisplayMember = "Text";
            }

            statusLabel.Text = Lang.T("Элементов: ", "Items: ") + entries.Count.ToString();
        }

        private void SearchHistory()
        {
            historyListBox.Items.Clear();
            string query = searchBox.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                RefreshHistory();
                return;
            }

            var results = clipboardManager.Search(query);
            foreach (var entry in results)
            {
                string icon = entry.IsPinned ? "📌 " : "";
                string typeIcon = entry.Type == "text" ? "📄 " : "🖼️ ";
                historyListBox.Items.Add(icon + typeIcon + entry.DisplayText);
            }

            statusLabel.Text = Lang.T("Найдено: ", "Found: ") + results.Count.ToString();
        }

        private void RestoreSelected()
        {
            if (historyListBox.SelectedIndex < 0)
            {
                MessageBox.Show(Lang.T("Выбери элемент из истории", "Select an item from history"), Lang.T("Буфер обмена", "Clipboard"));
                return;
            }

            var entries = clipboardManager.GetHistory();
            var filtered = string.IsNullOrWhiteSpace(searchBox.Text) ? entries : clipboardManager.Search(searchBox.Text);
            
            if (historyListBox.SelectedIndex < filtered.Count)
            {
                var entry = filtered[historyListBox.SelectedIndex];
                clipboardManager.RestoreEntry(entry.Id);
                statusLabel.Text = Lang.T("✓ Восстановлено в буфер обмена", "✓ Restored to clipboard");
            }
        }

        private void DeleteSelected()
        {
            if (historyListBox.SelectedIndex < 0) return;

            var entries = clipboardManager.GetHistory();
            if (historyListBox.SelectedIndex < entries.Count)
            {
                var entry = entries[historyListBox.SelectedIndex];
                clipboardManager.RemoveEntry(entry.Id);
                statusLabel.Text = Lang.T("✓ Удалено из истории", "✓ Deleted from history");
            }
        }

        private void ClearAll()
        {
            var result = MessageBox.Show(Lang.T("Очистить всю историю буфера обмена?", "Clear entire clipboard history?"), Lang.T("Подтверждение", "Confirmation"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                clipboardManager.ClearHistory();
                statusLabel.Text = Lang.T("✓ История очищена", "✓ History cleared");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                clipboardManager.Stop();
            }
            base.Dispose(disposing);
        }
    }
}
