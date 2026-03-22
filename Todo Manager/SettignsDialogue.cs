using System;
using System.Windows.Forms;

namespace Todo_Manager
{
    public partial class SettignsDialogue : Form
    {
        private AppSettings _settings;
        private Form1 _mainForm;

        public SettignsDialogue(AppSettings settings, Form1 mainForm)
        {
            InitializeComponent();
            _settings = settings;
            _mainForm = mainForm;
        }

        private void SettignsDialogue_Load(object sender, EventArgs e)
        {
            // Load current settings into checkboxes
            chkDarkMode.Checked = _settings.DarkMode;
            chkStrikethrough.Checked = _settings.ShowStrikethrough;
            chkSaveOnClose.Checked = _settings.SaveOnClose;

            // Wire button events
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Update settings from checkboxes
            _settings.DarkMode = chkDarkMode.Checked;
            _settings.ShowStrikethrough = chkStrikethrough.Checked;
            _settings.SaveOnClose = chkSaveOnClose.Checked;

            // Save to file
            _mainForm.SaveSettings();

            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
