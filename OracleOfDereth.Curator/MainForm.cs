using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OracleOfDereth.Curator
{
    internal sealed class MainForm : Form
    {
        private readonly TextBox masterPath = new TextBox();
        private readonly ListBox submissions = new ListBox();
        private readonly DataGridView preview = new DataGridView();
        private readonly Label summary = new Label();
        private readonly Button save = new Button();
        private MergeResult pending;

        public MainForm()
        {
            Text = "Oracle of Dereth Quest Curator";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(850, 560);
            Size = new Size(1050, 700);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 1, RowCount = 7 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 105));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);

            var masterRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
            masterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            masterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            masterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            masterRow.Controls.Add(new Label { Text = "Master quests.csv", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            masterPath.Dock = DockStyle.Fill;
            masterPath.Text = FindDefaultMaster();
            masterPath.TextChanged += (_, __) => ClearPreview();
            masterRow.Controls.Add(masterPath, 1, 0);
            var browseMaster = new Button { Text = "Browse…", AutoSize = true };
            browseMaster.Click += BrowseMaster;
            masterRow.Controls.Add(browseMaster, 2, 0);
            layout.Controls.Add(masterRow);

            var inputRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            inputRow.Controls.Add(new Label { Text = "Submission CSV files", AutoSize = true, Padding = new Padding(0, 7, 8, 0) });
            var add = new Button { Text = "Add files…", AutoSize = true };
            add.Click += AddFiles;
            inputRow.Controls.Add(add);
            var remove = new Button { Text = "Remove selected", AutoSize = true };
            remove.Click += (_, __) => { while (submissions.SelectedIndices.Count > 0) submissions.Items.RemoveAt(submissions.SelectedIndices[0]); ClearPreview(); };
            inputRow.Controls.Add(remove);
            var clear = new Button { Text = "Clear", AutoSize = true };
            clear.Click += (_, __) => { submissions.Items.Clear(); ClearPreview(); };
            inputRow.Controls.Add(clear);
            layout.Controls.Add(inputRow);

            submissions.Dock = DockStyle.Fill;
            submissions.SelectionMode = SelectionMode.MultiExtended;
            layout.Controls.Add(submissions);

            var actionRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            var build = new Button { Text = "Preview merge", AutoSize = true };
            build.Click += PreviewMerge;
            actionRow.Controls.Add(build);
            save.Text = "Save merged quests.csv";
            save.AutoSize = true;
            save.Enabled = false;
            save.Click += SaveMerge;
            actionRow.Controls.Add(save);
            layout.Controls.Add(actionRow);

            preview.Dock = DockStyle.Fill;
            preview.ReadOnly = true;
            preview.AllowUserToAddRows = false;
            preview.AllowUserToDeleteRows = false;
            preview.AutoGenerateColumns = true;
            preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            preview.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            layout.Controls.Add(preview);

            summary.AutoSize = true;
            summary.Padding = new Padding(0, 6, 0, 6);
            layout.Controls.Add(summary);
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = "Saving creates a timestamped backup beside quests.csv. Existing quest metadata is never overwritten."
            });

            AllowDrop = true;
            DragEnter += (_, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            DragDrop += (_, e) => AddSubmissionPaths(((string[])e.Data.GetData(DataFormats.FileDrop)).Where(p => p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)));
        }

        private void BrowseMaster(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", FileName = "quests.csv" })
                if (dialog.ShowDialog(this) == DialogResult.OK) masterPath.Text = dialog.FileName;
        }

        private void AddFiles(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog { Filter = "Quest submissions (*.csv)|*.csv|All files (*.*)|*.*", Multiselect = true })
                if (dialog.ShowDialog(this) == DialogResult.OK) AddSubmissionPaths(dialog.FileNames);
        }

        private void AddSubmissionPaths(IEnumerable<string> paths)
        {
            var current = new HashSet<string>(submissions.Items.Cast<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths.Select(Path.GetFullPath)) if (current.Add(path)) submissions.Items.Add(path);
            ClearPreview();
        }

        private void PreviewMerge(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(masterPath.Text)) throw new FileNotFoundException("Choose the master quests.csv.", masterPath.Text);
                if (submissions.Items.Count == 0) throw new InvalidOperationException("Add at least one submission CSV.");
                pending = QuestMerge.Build(masterPath.Text, submissions.Items.Cast<string>());
                preview.DataSource = pending.Items;
                summary.Text = $"{pending.Added} new flags; {pending.Verified} existing flags newly verified; {pending.Unchanged} already verified.";
                save.Enabled = pending.Added + pending.Verified > 0;
            }
            catch (Exception ex) { ClearPreview(); MessageBox.Show(this, ex.Message, "Cannot preview merge", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void SaveMerge(object sender, EventArgs e)
        {
            if (pending == null) return;
            try
            {
                pending.Master.Write(masterPath.Text);
                save.Enabled = false;
                MessageBox.Show(this, $"Saved {pending.Added} new flags and {pending.Verified} verification changes.\n\nA timestamped backup was created beside the master file.", "Merge complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save merge", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ClearPreview()
        {
            pending = null;
            preview.DataSource = null;
            summary.Text = "";
            save.Enabled = false;
        }

        private static string FindDefaultMaster()
        {
            foreach (string start in new[] { Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory })
            {
                var directory = new DirectoryInfo(start);
                for (int i = 0; directory != null && i < 6; i++, directory = directory.Parent)
                {
                    string candidate = Path.Combine(directory.FullName, "OracleOfDereth", "Resources", "quests.csv");
                    if (File.Exists(candidate)) return candidate;
                }
            }
            return "";
        }
    }
}
