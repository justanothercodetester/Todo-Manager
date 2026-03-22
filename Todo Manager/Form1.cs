using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Todo_Manager
{
    public partial class Form1 : Form
    {
        private readonly string _storagePath;
        private readonly string _settingsPath;
        private List<TodoItem> _tasks = new List<TodoItem>();
        private AppSettings _settings = new AppSettings();

        public Form1()
        {
            InitializeComponent();

            // storage path: %APPDATA%\TodoManager\tasks.xml
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "TodoManager");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            _storagePath = Path.Combine(dir, "tasks.xml");
            _settingsPath = Path.Combine(dir, "settings.xml");

            // wire events
            btnAdd.Click += BtnAdd_Click;
            btnRemove.Click += BtnRemove_Click;
            btnToggleDone.Click += BtnToggleDone_Click;
            RCT.Click += RCT_Click;
            listViewTasks.DoubleClick += ListViewTasks_DoubleClick;
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;

            // small UX
            textBox1.KeyDown += TextBox1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSettings();
            ApplySettings();
            LoadTasks();
            RefreshListView();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_settings.SaveOnClose)
            {
                SaveTasks();
            }
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddCurrentTask();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            AddCurrentTask();
        }

        private void AddCurrentTask()
        {
            var desc = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(desc))
            {
                MessageBox.Show("Please enter a task description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var item = new TodoItem
            {
                Description = desc,
                DueDate = dateTimePicker1.Value.Date,
                IsDone = false
            };

            _tasks.Add(item);
            textBox1.Clear();
            RefreshListView();
            SaveTasks();
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (listViewTasks.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a task to remove.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var li = listViewTasks.SelectedItems[0];
            int idx = li.Index;
            if (idx >= 0 && idx < _tasks.Count)
            {
                _tasks.RemoveAt(idx);
                RefreshListView();
                SaveTasks();
            }
        }

        private void BtnToggleDone_Click(object sender, EventArgs e)
        {
            ToggleSelectedDone();
        }

        private void RCT_Click(object sender, EventArgs e)
        {
            _tasks.RemoveAll(i => i.IsDone);
            RefreshListView();
            SaveTasks();
        }

        private void ListViewTasks_DoubleClick(object sender, EventArgs e)
        {
            ToggleSelectedDone();
        }

        private void ToggleSelectedDone()
        {
            if (listViewTasks.SelectedItems.Count == 0)
                return;

            var li = listViewTasks.SelectedItems[0];
            int idx = li.Index;
            if (idx >= 0 && idx < _tasks.Count)
            {
                _tasks[idx].IsDone = !_tasks[idx].IsDone;
                RefreshListView();
                SaveTasks();
            }
        }

        private void RefreshListView()
        {
            listViewTasks.BeginUpdate();
            listViewTasks.Items.Clear();

            for (int i = 0; i < _tasks.Count; i++)
            {
                var t = _tasks[i];
                var lvi = new ListViewItem(t.Description);
                lvi.SubItems.Add(t.DueDate.ToShortDateString());
                lvi.SubItems.Add(t.IsDone ? "Yes" : "No");

                // styling: strikeout if done, gray text
                if (t.IsDone)
                {
                    lvi.ForeColor = Color.Gray;
                    if (_settings.ShowStrikethrough)
                    {
                        lvi.Font = new Font(listViewTasks.Font, FontStyle.Strikeout);
                    }
                    else
                    {
                        lvi.Font = new Font(listViewTasks.Font, FontStyle.Regular);
                    }
                }
                else
                {
                    // overdue highlight
                    if (t.DueDate < DateTime.Today)
                    {
                        lvi.ForeColor = Color.DarkRed;
                    }
                    else
                    {
                        lvi.ForeColor = Color.Black;
                    }
                    lvi.Font = new Font(listViewTasks.Font, FontStyle.Regular);
                }

                listViewTasks.Items.Add(lvi);
            }

            listViewTasks.EndUpdate();
        }

        private void LoadTasks()
        {
            try
            {
                if (!File.Exists(_storagePath))
                {
                    _tasks = new List<TodoItem>();
                    return;
                }

                using (var fs = File.OpenRead(_storagePath))
                {
                    var serializer = new XmlSerializer(typeof(List<TodoItem>));
                    _tasks = (List<TodoItem>)serializer.Deserialize(fs) ?? new List<TodoItem>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load tasks: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _tasks = new List<TodoItem>();
            }
        }

        private void SaveTasks()
        {
            try
            {
                using (var fs = File.Create(_storagePath))
                {
                    var serializer = new XmlSerializer(typeof(List<TodoItem>));
                    serializer.Serialize(fs, _tasks);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save tasks: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    _settings = new AppSettings();
                    SaveSettings();
                    return;
                }

                using (var fs = File.OpenRead(_settingsPath))
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    _settings = (AppSettings)serializer.Deserialize(fs) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load settings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _settings = new AppSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                using (var fs = File.Create(_settingsPath))
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    serializer.Serialize(fs, _settings);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save settings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySettings()
        {
            if (_settings.DarkMode)
            {
                this.BackColor = Color.FromArgb(45, 45, 48);
                this.ForeColor = Color.White;
                label1.ForeColor = Color.White;
                label2.ForeColor = Color.White;
                listViewTasks.BackColor = Color.Gray;
                listViewTasks.ForeColor = Color.White;
                textBox1.BackColor = Color.FromArgb(37, 37, 38);
                textBox1.ForeColor = Color.White;
                btnRemove.BackColor = Color.LightBlue;
                btnToggleDone.BackColor = Color.LightBlue;
                RCT.BackColor = Color.LightBlue;
                dateTimePicker1.BackColor = Color.FromArgb(37, 37, 38);
                dateTimePicker1.ForeColor = Color.White;
            }
            else
            {
                this.BackColor = Color.WhiteSmoke;
                this.ForeColor = Color.Black;
                label1.ForeColor = Color.FromArgb(45, 45, 48);
                label2.ForeColor = Color.FromArgb(80, 80, 80);
                listViewTasks.BackColor = Color.White;
                listViewTasks.ForeColor = Color.Black;
                textBox1.BackColor = Color.White;
                textBox1.ForeColor = Color.Black;
                btnRemove.BackColor = Color.White;
                btnToggleDone.BackColor = Color.White;
                RCT.BackColor = Color.White;
                dateTimePicker1.BackColor = Color.White;
                dateTimePicker1.ForeColor = Color.Black;
            }
        }

        private void reloadTasksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadTasks();
            RefreshListView();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_settings.SaveOnClose)
            {
                SaveTasks();
            }
            Close();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettignsDialogue dlg = new SettignsDialogue(_settings, this);
            dlg.ShowDialog();
            ApplySettings();
            RefreshListView();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            // no-op: date selected for new task
        }
    }

    [Serializable]
    public class TodoItem
    {
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsDone { get; set; }
    }

    [Serializable]
    public class AppSettings
    {
        public bool DarkMode { get; set; } = false;
        public bool ShowStrikethrough { get; set; } = true;
        public bool SaveOnClose { get; set; } = true;
    }
}
