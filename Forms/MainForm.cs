// Forms/MainForm.cs
using System.IO.Ports;
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

        // UI Controls
        private readonly Panel pnlHeader = new();
        private readonly Label lblTitle = new();
        private readonly Label lblMode = new();
        private readonly ComboBox cmbMode = new();
        private readonly DataGridView dgvKeys = new();
        private readonly Button btnConnect = new();
        private readonly Button btnSave = new();
        private readonly Label lblStatus = new();

        // Colors
        private static readonly Color DarkGreen  = Color.FromArgb(61, 141, 122);   // #3D8D7A
        private static readonly Color LightGreen = Color.FromArgb(179, 216, 168);  // #B3D8A8
        private static readonly Color Cream      = Color.FromArgb(251, 255, 228);  // #FBFFE4
        private static readonly Color Mint       = Color.FromArgb(163, 209, 198);  // #A3D1C6

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
            this.Size = new Size(660, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.BackColor = DarkGreen;
            this.Font = new Font("Segoe UI", 9F);
            this.Icon = new Icon("media\\archmaster.ico");

            // === HEADER PANEL ===
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 70;
            pnlHeader.BackColor = LightGreen;
            pnlHeader.Padding = new Padding(15);
            pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(Mint, 3);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            lblTitle.Text = "ARCHMASTER";
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = DarkGreen;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(15, 18);

            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // === MODE SELECTOR ===
            lblMode.Text = "Mode:";
            lblMode.ForeColor = Cream;
            lblMode.Location = new Point(30, 90);
            lblMode.AutoSize = true;

            cmbMode.Items.AddRange(new[] { "Mode 1", "Mode 2" });
            cmbMode.SelectedIndex = 0;
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.BackColor = Mint;
            cmbMode.ForeColor = DarkGreen;
            cmbMode.FlatStyle = FlatStyle.Flat;
            cmbMode.Location = new Point(85, 87);
            cmbMode.Width = 110;
            cmbMode.SelectedIndexChanged += (s, e) => PopulateGrid();

            // === DATAGRIDVIEW ===
            dgvKeys.Location = new Point(30, 130);
            dgvKeys.Size = new Size(590, 260);
            dgvKeys.AllowUserToAddRows = false;
            dgvKeys.BackgroundColor = Cream;
            dgvKeys.GridColor = Mint;
            dgvKeys.BorderStyle = BorderStyle.None;
            dgvKeys.ColumnHeadersDefaultCellStyle.BackColor = LightGreen;
            dgvKeys.ColumnHeadersDefaultCellStyle.ForeColor = DarkGreen;
            dgvKeys.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvKeys.EnableHeadersVisualStyles = false;
            dgvKeys.RowTemplate.Height = 32;

            dgvKeys.Columns.Add("phys", "Physical Key");
            dgvKeys.Columns.Add("remap", "Remap To");
            dgvKeys.Columns[0].Width = 120;
            dgvKeys.Columns[1].Width = 450;

            dgvKeys.DefaultCellStyle.BackColor = Cream;
            dgvKeys.DefaultCellStyle.ForeColor = DarkGreen;
            dgvKeys.DefaultCellStyle.SelectionBackColor = Mint;
            dgvKeys.DefaultCellStyle.SelectionForeColor = DarkGreen;
            dgvKeys.EditingControlShowing += DgvKeys_EditingControlShowing;

            // === BUTTONS ===
            btnConnect.Text = "Connect Arduino";
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.BackColor = LightGreen;
            btnConnect.ForeColor = DarkGreen;
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnConnect.Size = new Size(140, 38);
            btnConnect.Location = new Point(30, 420);
            btnConnect.Click += BtnConnect_Click;
            btnConnect.MouseEnter += (s, e) => btnConnect.BackColor = Mint;
            btnConnect.MouseLeave += (s, e) => btnConnect.BackColor = LightGreen;

            btnSave.Text = "Save Config";
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.BackColor = Mint;
            btnSave.ForeColor = DarkGreen;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSave.Size = new Size(110, 38);
            btnSave.Location = new Point(185, 420);
            btnSave.Click += BtnSave_Click;

            // === STATUS ===
            lblStatus.Text = "Not connected";
            lblStatus.ForeColor = Cream;
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblStatus.Location = new Point(30, 470);
            lblStatus.AutoSize = true;

            // === ADD TO FORM ===
            this.Controls.AddRange(new Control[]
            {
                lblMode, cmbMode, dgvKeys, btnConnect, btnSave, lblStatus
            });
        }

        private void DgvKeys_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dgvKeys.CurrentCell.ColumnIndex == 1 && e.Control is TextBox tb)
            {
                tb.KeyDown += (s, ke) =>
                {
                    if (ke.Control && !ke.Alt && !ke.Shift) tb.Text = "Ctrl+";
                    else if (ke.Alt && !ke.Control && !ke.Shift) tb.Text = "Alt+";
                    else if (ke.Shift && !ke.Control && !ke.Alt) tb.Text = "Shift+";
                    ke.Handled = true;
                };
            }
        }
        #endregion

        #region Config & Grid
        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                if (loaded != null) foreach (var kv in loaded) config[kv.Key] = kv.Value;
            }
            if (!config.ContainsKey("Mode1")) config["Mode1"] = new();
            if (!config.ContainsKey("Mode2")) config["Mode2"] = new();
        }

        private void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configPath, json);
            lblStatus.Text = "Config saved!";
            lblStatus.ForeColor = LightGreen;
            Task.Delay(2000).ContinueWith(_ => Invoke(() => lblStatus.Text = serialPort?.IsOpen == true ? $"Connected to {serialPort.PortName}" : "Not connected"));
        }

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
                lblStatus.ForeColor = Cream;
                return;
            }

            string[] ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                MessageBox.Show("No COM ports found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            serialPort = new SerialPort(ports[0], 9600);
            serialPort.DataReceived += SerialPort_DataReceived;
            try
            {
                serialPort.Open();
                btnConnect.Text = "Disconnect";
                lblStatus.Text = $"Connected to {serialPort.PortName}";
                lblStatus.ForeColor = LightGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            catch { }
        }
        #endregion
    }
}