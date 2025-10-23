// Forms/MainForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ArchMasterConfig.Forms
{
    public partial class MainForm : Form
    {
        private SerialPort? serialPort;
        private readonly string configPath = "config.json";
        private readonly Dictionary<string, Dictionary<string, string>> config = new();
        private readonly KeyButton[,] keyGrid = new KeyButton[3, 3];
        private KeyButton? selectedKeyButton;

        // UI Controls
        private readonly Panel pnlHeader   = new();
        private readonly Label  lblTitle   = new();
        private readonly Label  lblMode    = new();
        private readonly ComboBox cmbMode  = new();
        private readonly Panel pnlKeyGrid  = new();
        private readonly Button btnConnect = new();
        private readonly Button btnSave    = new();
        private readonly Label  lblStatus  = new();
        private readonly TextBox txtRemap  = new();
        private readonly Label  lblRemap   = new();

        // ──────────────────────────────────────
        //  Shared colours
        // ────────
        public static class Colors
        {
            public static readonly Color DarkGreen  = Color.FromArgb(61, 141, 122);   // #3D8D7A
            public static readonly Color LightGreen = Color.FromArgb(179, 216, 168);  // #B3D8A8
            public static readonly Color Cream      = Color.FromArgb(251, 255, 228);  // #FBFFE4
            public static readonly Color Mint       = Color.FromArgb(163, 209, 198);  // #A3D1C6
        }

        public MainForm()
        {
            LoadConfig();
            SetupUI();
            PopulateGrid();
        }

        #region UI Setup
        private void SetupUI()
        {
            // ── Form ─────────────────────────────────────
            Text                = "ARCHMASTER Keyboard Config";
            Size                = new Size(760, 620);
            StartPosition       = FormStartPosition.CenterScreen;
            FormBorderStyle     = FormBorderStyle.FixedSingle;
            BackColor           = Colors.DarkGreen;
            Font                = new Font("Segoe UI", 9F);
            Icon                = new Icon(@"media\archmaster.ico");

            // ── Header ───────────────────────────────────
            pnlHeader.Dock      = DockStyle.Top;
            pnlHeader.Height    = 70;
            pnlHeader.BackColor = Colors.LightGreen;
            pnlHeader.Padding   = new Padding(15);
            pnlHeader.Paint    += (s, e) =>
            {
                using var pen = new Pen(Colors.Mint, 3);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            lblTitle.Text       = "ARCHMASTER";
            lblTitle.Font       = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor  = Colors.DarkGreen;
            lblTitle.AutoSize   = true;
            lblTitle.Location   = new Point(15, 18);
            pnlHeader.Controls.Add(lblTitle);
            Controls.Add(pnlHeader);

            // ── Mode selector ─────────────────────────────
            lblMode.Text        = "Mode:";
            lblMode.ForeColor   = Colors.Cream;
            lblMode.Location    = new Point(30, 90);
            lblMode.AutoSize    = true;

            cmbMode.Items.AddRange(new[] { "Mode 1", "Mode 2" });
            cmbMode.SelectedIndex = 0;
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.BackColor   = Colors.Mint;
            cmbMode.ForeColor   = Colors.DarkGreen;
            cmbMode.FlatStyle   = FlatStyle.Flat;
            cmbMode.Location    = new Point(85, 87);
            cmbMode.Width       = 110;
            cmbMode.SelectedIndexChanged += (s, e) => PopulateGrid();

            // ── 3x3 key-grid panel ───────────────────────
            pnlKeyGrid.Location = new Point(30, 130);
            pnlKeyGrid.Size     = new Size(280, 280);
            pnlKeyGrid.BackColor = Color.FromArgb(240, 250, 242);
            pnlKeyGrid.Paint    += (s, e) =>
            {
                using var pen = new Pen(Colors.Mint, 4);
                using var path = RoundedRect(pnlKeyGrid.ClientRectangle, 24);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.DrawPath(pen, path);
            };

            const int btnSize = 82;
            const int spacing = 20;

            int startX = (pnlKeyGrid.Width  - (3 * btnSize + 2 * spacing)) / 2;
            int startY = (pnlKeyGrid.Height - (3 * btnSize + 2 * spacing)) / 2;

            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                var btn = new KeyButton
                {
                    Location   = new Point(startX + c * (btnSize + spacing),
                                        startY + r * (btnSize + spacing)),
                    Size       = new Size(btnSize, btnSize),
                    PhysicalKey= ((char)('A' + r * 3 + c)).ToString(),
                    Text       = ((char)('A' + r * 3 + c)).ToString(),
                    CornerRadius = 18,
                    OutlineColor = MainForm.Colors.Mint,
                    OutlineThickness = 4
                };

                btn.Click += KeyButton_Click;
                keyGrid[r, c] = btn;
                pnlKeyGrid.Controls.Add(btn);
            }

            // ── Remap field ───────────────────────────────
            lblRemap.Text       = "Remap to:";
            lblRemap.ForeColor  = Colors.Cream;
            lblRemap.Location   = new Point(330, 130);
            lblRemap.AutoSize   = true;

            txtRemap.Location   = new Point(330, 155);
            txtRemap.Size       = new Size(240, 28);
            txtRemap.BackColor  = Colors.Mint;
            txtRemap.ForeColor  = Colors.DarkGreen;
            txtRemap.Font       = new Font("Segoe UI", 10F);
            txtRemap.BorderStyle= BorderStyle.FixedSingle;
            txtRemap.KeyDown    += TxtRemap_KeyDown;

            // ── Connect / Save buttons ───────────────────
            btnConnect.Text               = "Connect Arduino";
            btnConnect.FlatStyle          = FlatStyle.Flat;
            btnConnect.BackColor          = Colors.LightGreen;
            btnConnect.ForeColor          = Colors.DarkGreen;
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Font               = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnConnect.Size               = new Size(150, 40);
            btnConnect.Location           = new Point(30, 430);
            btnConnect.Click              += BtnConnect_Click;
            btnConnect.MouseEnter         += (s, e) => btnConnect.BackColor = Colors.Mint;
            btnConnect.MouseLeave         += (s, e) => btnConnect.BackColor = Colors.LightGreen;

            btnSave.Text                  = "Save Config";
            btnSave.FlatStyle             = FlatStyle.Flat;
            btnSave.BackColor             = Colors.Mint;
            btnSave.ForeColor             = Colors.DarkGreen;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Font                  = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSave.Size                  = new Size(120, 40);
            btnSave.Location              = new Point(195, 430);
            btnSave.Click                 += BtnSave_Click;

            // ── Status label ─────────────────────────────
            lblStatus.Text                = "Not connected";
            lblStatus.ForeColor           = Colors.Cream;
            lblStatus.Font                = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblStatus.Location            = new Point(30, 490);
            lblStatus.AutoSize            = true;

            // Add all controls
            Controls.AddRange(new Control[]
            {
                lblMode, cmbMode, pnlKeyGrid, lblRemap, txtRemap,
                btnConnect, btnSave, lblStatus
            });
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var p = new GraphicsPath();  // Fixed: was 'p148'
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        #endregion

        #region Interaction
        private void TxtRemap_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && !e.Alt && !e.Shift) { txtRemap.Text = "Ctrl+"; e.Handled = e.SuppressKeyPress = true; }
            else if (e.Alt && !e.Control && !e.Shift) { txtRemap.Text = "Alt+";  e.Handled = e.SuppressKeyPress = true; }
            else if (e.Shift && !e.Control && !e.Alt) { txtRemap.Text = "Shift+"; e.Handled = e.SuppressKeyPress = true; }
        }

        private void KeyButton_Click(object? sender, EventArgs e)
        {
            if (sender is not KeyButton btn) return;

            // Desi
            foreach (var k in keyGrid)
            {
                k.IsSelected = false;
                k.Invalidate();
            }

            selectedKeyButton = btn;
            btn.IsSelected = true;
            btn.Invalidate();

            string mode = "Mode" + (cmbMode.SelectedIndex + 1);
            txtRemap.Text = config[mode].ContainsKey(btn.PhysicalKey)
                ? config[mode][btn.PhysicalKey]
                : btn.PhysicalKey;
        }
        #endregion

        #region Config
        private void LoadConfig()
        {
            if (System.IO.File.Exists(configPath))
            {
                var json = System.IO.File.ReadAllText(configPath);
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                if (loaded != null) foreach (var kv in loaded) config[kv.Key] = kv.Value;
            }
            if (!config.ContainsKey("Mode1")) config["Mode1"] = new();
            if (!config.ContainsKey("Mode2")) config["Mode2"] = new();
        }

        private void SaveConfig()
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            System.IO.File.WriteAllText(configPath, json);
            lblStatus.Text = "Config saved!";
            lblStatus.ForeColor = Colors.LightGreen;
            Task.Delay(2000).ContinueWith(_ => Invoke(() =>
                lblStatus.Text = serialPort?.IsOpen == true
                    ? $"Connected to {serialPort.PortName}"
                    : "Not connected"));
        }

        private void PopulateGrid()
        {
            string mode = "Mode" + (cmbMode.SelectedIndex + 1);
            char[] keys = { 'A','B','C','D','E','F','G','H','I' };

            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                int i = r * 3 + c;
                var btn = keyGrid[r, c];
                btn.Text = keys[i].ToString();
                btn.RemapText = config[mode].ContainsKey(keys[i].ToString())
                    ? config[mode][keys[i].ToString()]
                    : keys[i].ToString();
            }

            selectedKeyButton = null;
            txtRemap.Clear();
        }
        #endregion

        #region Buttons
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (selectedKeyButton != null && !string.IsNullOrWhiteSpace(txtRemap.Text))
            {
                string mode = "Mode" + (cmbMode.SelectedIndex + 1);
                config[mode][selectedKeyButton.PhysicalKey] = txtRemap.Text.Trim();
                selectedKeyButton.RemapText = txtRemap.Text.Trim();

                // visual reset
                selectedKeyButton.IsSelected = false;
                selectedKeyButton.BackColor = Colors.Cream;
                selectedKeyButton.Invalidate();
                selectedKeyButton = null;
                txtRemap.Clear();
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
                lblStatus.ForeColor = Colors.Cream;
                return;
            }

            var ports = SerialPort.GetPortNames();
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
                lblStatus.ForeColor = Colors.LightGreen;
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
                    var p = line.Split(':');
                    string mode = "Mode" + p[1];
                    string phys = p[2];
                    string remap = config[mode].ContainsKey(phys) ? config[mode][phys] : phys;
                    serialPort.WriteLine(remap);
                }
            }
            catch { /* ignore */ }
        }
        #endregion
    }

    // ──────────────────────────────────────────────────────
    //  Custom button – thick outline always + selection
    // ──────────────────────────────────────────────────────
    public class KeyButton : Panel
    {
        public string PhysicalKey { get; set; } = "";
        public string RemapText   { get; set; } = "";
        public bool   IsSelected  { get; set; } = false;
        public int    CornerRadius { get; set; } = 18;
        public Color  OutlineColor { get; set; } = MainForm.Colors.Mint;
        public int    OutlineThickness { get; set; } = 4;

        private bool isHovered = false;

        public KeyButton()
        {
            SetStyle(ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Margin = new Padding(0);
            Padding = new Padding(0);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
                OnClick(EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent?.BackColor ?? MainForm.Colors.Cream);

            var inset = OutlineThickness / 2f + 1;
            var rect = new Rectangle((int)inset, (int)inset, (int)(Width - inset * 2), (int)(Height - inset * 2));
            using var path = CreateRoundedRectangle(rect, CornerRadius);


            var shadow = new Rectangle((int)rect.X + 2, (int)rect.Y + 2, (int)rect.Width, (int)rect.Height);
            using (var sb = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                e.Graphics.FillPath(sb, CreateRoundedRectangle(shadow, CornerRadius));


            // Fill
            Color fill = IsSelected ? Color.FromArgb(179, 216, 168)
                    : isHovered  ? Color.FromArgb(200, 230, 215)
                    : MainForm.Colors.Cream;


            using (var brush = new SolidBrush(fill))
                e.Graphics.FillPath(brush, path);

            // Outline
            using (var pen = new Pen(OutlineColor, OutlineThickness))
                e.Graphics.DrawPath(pen, path);

            // Selection border
            if (IsSelected)
            {
                using var selPen = new Pen(MainForm.Colors.DarkGreen, OutlineThickness + 2);
                e.Graphics.DrawPath(selPen, path);
            }

            // Text
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                new Font("Segoe UI", 14F, FontStyle.Bold),
                rect,
                MainForm.Colors.DarkGreen,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}