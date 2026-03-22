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
        private List<TodoItem> _tasks = new List<TodoItem>();

        public Form1()
        {
            InitializeComponent();

            // storage path: %APPDATA%\TodoManager\tasks.xml
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "TodoManager");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            _storagePath = Path.Combine(dir, "tasks.xml");

            // wire events
            btnAdd.Click += BtnAdd_Click;
            btnRemove.Click += BtnRemove_Click;
            btnToggleDone.Click += BtnToggleDone_Click;
            listViewTasks.DoubleClick += ListViewTasks_DoubleClick;
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;

            // small UX
            textBox1.KeyDown += TextBox1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadTasks();
            RefreshListView();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveTasks();
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
                    lvi.Font = new Font(listViewTasks.Font, FontStyle.Strikeout);
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

        // keep the auto-generated event present (unused)
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            // no-op: date selected for new task
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _tasks.RemoveAll(i => i.IsDone);
            RefreshListView();
            SaveTasks();
        }
    }

    [Serializable]
    public class TodoItem
    {
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsDone { get; set; }
    }
}
