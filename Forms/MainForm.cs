// Forms/MainForm.cs
using System.IO.Ports;
using System.Text.Json;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ArchMasterConfig.Forms
{
    public partial class MainForm : Form
    {
        private SerialPort? serialPort;
        private readonly string configPath = "config.json";
        private readonly Dictionary<string, Dictionary<string, string>> config = new();

        // UI controls
        private readonly DataGridView dgvKeys = new();
        private readonly ComboBox cmbMode = new();
        private readonly Button btnConnect = new();
        private readonly Button btnSave = new();
        private readonly Label lblStatus = new();

        public MainForm()
        {
            LoadConfig();
            SetupUI();
            PopulateGrid();
        }

        #region UI Setup
        private void SetupUI()
        {
            this.Text = "ARCHMASTER Keyboard Config";
            this.Size = new Size(620, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // ---- Mode selector ----
            var lblMode = new Label { Text = "Mode:", Left = 20, Top = 20, Width = 50 };
            cmbMode.Items.AddRange(new[] { "Mode 1", "Mode 2" });
            cmbMode.SelectedIndex = 0;
            cmbMode.Left = 70; cmbMode.Top = 18; cmbMode.Width = 100;
            cmbMode.SelectedIndexChanged += (s, e) => PopulateGrid();

            // ---- DataGridView ----
            dgvKeys.Left = 20; dgvKeys.Top = 60; dgvKeys.Width = 570; dgvKeys.Height = 300;
            dgvKeys.AllowUserToAddRows = false;
            dgvKeys.Columns.Add("phys", "Physical Key");
            dgvKeys.Columns.Add("remap", "Remap To");
            dgvKeys.Columns[0].Width = 100;
            dgvKeys.Columns[1].Width = 450;
            dgvKeys.EditingControlShowing += DgvKeys_EditingControlShowing;

            // ---- Buttons ----
            btnConnect.Text = "Connect Arduino"; btnConnect.Left = 20; btnConnect.Top = 380; btnConnect.Width = 130;
            btnConnect.Click += BtnConnect_Click;

            btnSave.Text = "Save Config"; btnSave.Left = 160; btnSave.Top = 380; btnSave.Width = 100;
            btnSave.Click += BtnSave_Click;

            // ---- Status ----
            lblStatus.Text = "Not connected"; lblStatus.Left = 20; lblStatus.Top = 420; lblStatus.Width = 560;
            lblStatus.ForeColor = Color.Red;

            this.Controls.AddRange(new Control[] { lblMode, cmbMode, dgvKeys, btnConnect, btnSave, lblStatus });
        }

        private void DgvKeys_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dgvKeys.CurrentCell.ColumnIndex == 1 && e.Control is TextBox tb)
            {
                tb.KeyDown += (s, ke) =>
                {
                    if (ke.Control) tb.Text = "Ctrl+";
                    else if (ke.Alt) tb.Text = "Alt+";
                    else if (ke.Shift) tb.Text = "Shift+";
                };
            }
        }
        #endregion

        #region Config
        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                if (loaded != null) config.Clear(); foreach (var kv in loaded) config[kv.Key] = kv.Value;
            }
            if (!config.ContainsKey("Mode1")) config["Mode1"] = new();
            if (!config.ContainsKey("Mode2")) config["Mode2"] = new();
        }

        private void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configPath, json);
            lblStatus.Text = "Config saved!";
            lblStatus.ForeColor = Color.Green;
        }
        #endregion

        #region Grid
        private void PopulateGrid()
        {
            dgvKeys.Rows.Clear();
            string mode = "Mode" + (cmbMode.SelectedIndex + 1);
            char[] keys = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I' };

            foreach (char k in keys)
            {
                string phys = k.ToString();
                string remap = config[mode].ContainsKey(phys) ? config[mode][phys] : phys;
                dgvKeys.Rows.Add(phys, remap);
            }
        }
        #endregion

        #region Buttons
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string mode = "Mode" + (cmbMode.SelectedIndex + 1);
            config[mode].Clear();
            foreach (DataGridViewRow row in dgvKeys.Rows)
            {
                if (row.Cells[0].Value != null)
                {
                    string phys = row.Cells[0].Value.ToString()!;
                    string remap = row.Cells[1].Value?.ToString() ?? phys;
                    config[mode][phys] = remap;
                }
            }
            SaveConfig();
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (serialPort?.IsOpen == true)
            {
                serialPort.Close();
                btnConnect.Text = "Connect Arduino";
                lblStatus.Text = "Disconnected";
                lblStatus.ForeColor = Color.Red;
                return;
            }

            string[] ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                MessageBox.Show("No COM ports found!");
                return;
            }

            serialPort = new SerialPort(ports[0], 9600);
            serialPort.DataReceived += SerialPort_DataReceived;
            try
            {
                serialPort.Open();
                btnConnect.Text = "Disconnect";
                lblStatus.Text = $"Connected to {serialPort.PortName}";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        #endregion

        #region Serial
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = serialPort!.ReadLine().Trim();
                if (line.StartsWith("KEY:"))
                {
                    var parts = line.Split(':');
                    string mode = "Mode" + parts[1];
                    string physKey = parts[2];
                    string remap = config[mode].ContainsKey(physKey) ? config[mode][physKey] : physKey;
                    serialPort.WriteLine(remap);
                }
            }
            catch { /* ignore malformed lines */ }
        }
        #endregion
    }
}