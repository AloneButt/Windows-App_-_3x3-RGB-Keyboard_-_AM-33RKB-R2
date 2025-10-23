// Forms/MainForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using static ArchMasterConfig.Forms.MainForm;

namespace ArchMasterConfig.Forms
{
    public partial class MainForm : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private readonly Button btnMinimize = new();
        private readonly Button btnClose = new();
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
        //  NEW Color Palette: 081c15 → 1b4332 → 2d6a4f → 40916c → 52b788 → 74c69d → 95d5b2 → b7e4c7 → d8f3dc
        // ────────
        public static class Colors
        {
            public static readonly Color DeepGreen      = ColorTranslator.FromHtml("#081c15");
            public static readonly Color DarkGreen      = ColorTranslator.FromHtml("#1b4332");
            public static readonly Color ForestGreen    = ColorTranslator.FromHtml("#2d6a4f");
            public static readonly Color TealGreen      = ColorTranslator.FromHtml("#40916c");
            public static readonly Color MidGreen       = ColorTranslator.FromHtml("#52b788");
            public static readonly Color LightTeal      = ColorTranslator.FromHtml("#74c69d");
            public static readonly Color PaleGreen      = ColorTranslator.FromHtml("#95d5b2");
            public static readonly Color MintCream      = ColorTranslator.FromHtml("#b7e4c7");
            public static readonly Color LightMint      = ColorTranslator.FromHtml("#d8f3dc");
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
            Text                = "ARCHMASTER Keyboard Config";
            Size                = new Size(760, 620);
            StartPosition       = FormStartPosition.CenterScreen;
            FormBorderStyle     = FormBorderStyle.None;
            BackColor           = Colors.DarkGreen;
            Font                = new Font("Poppins-regular", 9F);
            Icon                = new Icon(@"media\archmaster.ico");

            // ── Header ───────────────────────────────────
            pnlHeader.Dock      = DockStyle.Top;
            pnlHeader.Height    = 70;
            pnlHeader.BackColor = Colors.ForestGreen;
            pnlHeader.Padding   = new Padding(15);
            pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(Colors.TealGreen, 3);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };
            pnlHeader.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                }
            };

            lblTitle.Text       = "ARCHMASTER";
            lblTitle.Font       = new Font("Poppins-regular", 16F, FontStyle.Bold);
            lblTitle.ForeColor  = Colors.LightMint;
            lblTitle.AutoSize   = true;
            lblTitle.Location   = new Point(15, 18);
            pnlHeader.Controls.Add(lblTitle);
            Controls.Add(pnlHeader);

            // ── Custom Window Buttons ───────────────────────────────
            btnMinimize.Text = "MINIMIZE";
            btnMinimize.Font = new Font("Poppins-regular", 10F, FontStyle.Bold);
            btnMinimize.ForeColor = Colors.LightMint;
            btnMinimize.BackColor = Colors.ForestGreen;
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.Size = new Size(120, 40);
            btnMinimize.Location = new Point(pnlHeader.Width - 240, 15);
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;
            btnMinimize.MouseEnter += (s, e) => btnMinimize.BackColor = Colors.MidGreen;
            btnMinimize.MouseLeave += (s, e) => btnMinimize.BackColor = Colors.ForestGreen;

            btnClose.Text = "CLOSE";
            btnClose.Font = new Font("Poppins-regular", 10F, FontStyle.Bold);
            btnClose.ForeColor = Colors.LightMint;
            btnClose.BackColor = Colors.ForestGreen;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(120, 40);
            btnClose.Location = new Point(pnlHeader.Width - 120, 15);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => Close();
            btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.FromArgb(200, 50, 50);
            btnClose.MouseLeave += (s, e) => btnClose.BackColor = Colors.ForestGreen;

            pnlHeader.Controls.AddRange(new[] { btnMinimize, btnClose });

            // ── Mode selector ─────────────────────────────
            lblMode.Text        = "Mode:";
            lblMode.ForeColor   = Colors.MintCream;
            lblMode.Location    = new Point(30, 90);
            lblMode.AutoSize    = true;

            cmbMode.Items.AddRange(new[] { "Mode 1", "Mode 2" });
            cmbMode.SelectedIndex = 0;
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.BackColor   = Colors.LightTeal;
            cmbMode.ForeColor   = Colors.DarkGreen;
            cmbMode.FlatStyle   = FlatStyle.Flat;
            cmbMode.Location    = new Point(85, 87);
            cmbMode.Width       = 110;
            cmbMode.SelectedIndexChanged += (s, e) => PopulateGrid();

            // ── 3x3 key-grid panel ───────────────────────
            pnlKeyGrid.Size     = new Size(310, 310);
            pnlKeyGrid.BackColor = Colors.MintCream;

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
                    OutlineColor = Colors.TealGreen,
                    OutlineThickness = 4
                };

                btn.Click += KeyButton_Click;
                keyGrid[r, c] = btn;
                pnlKeyGrid.Controls.Add(btn);
            }

            // ── Remap field ───────────────────────────────
            lblRemap.Text       = "Remap to:";
            lblRemap.ForeColor  = Colors.MintCream;
            lblRemap.Location   = new Point(330, 130);
            lblRemap.AutoSize   = true;

            txtRemap.Location   = new Point(330, 155);
            txtRemap.Size       = new Size(240, 28);
            txtRemap.BackColor  = Colors.LightTeal;
            txtRemap.ForeColor  = Colors.DarkGreen;
            txtRemap.Font       = new Font("Poppins-regular", 10F);
            txtRemap.BorderStyle= BorderStyle.FixedSingle;
            txtRemap.KeyDown    += TxtRemap_KeyDown;

            // ── Connect / Save buttons ───────────────────
            btnConnect.Text               = "Connect Arduino";
            btnConnect.FlatStyle          = FlatStyle.Flat;
            btnConnect.BackColor          = Colors.ForestGreen;
            btnConnect.ForeColor          = Colors.LightMint;
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Font               = new Font("Poppins-regular", 9F, FontStyle.Bold);
            btnConnect.Size               = new Size(150, 40);
            btnConnect.Location           = new Point(30, 430);
            btnConnect.Click              += BtnConnect_Click;
            btnConnect.MouseEnter         += (s, e) => btnConnect.BackColor = Colors.MidGreen;
            btnConnect.MouseLeave         += (s, e) => btnConnect.BackColor = Colors.ForestGreen;

            btnSave.Text                  = "Save Config";
            btnSave.FlatStyle             = FlatStyle.Flat;
            btnSave.BackColor             = Colors.LightTeal;
            btnSave.ForeColor             = Colors.DarkGreen;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Font                  = new Font("Poppins-regular", 9F, FontStyle.Bold);
            btnSave.Size                  = new Size(120, 40);
            btnSave.Location              = new Point(195, 430);
            btnSave.Click                 += BtnSave_Click;

            // ── Status label ─────────────────────────────
            lblStatus.Text                = "Not connected";
            lblStatus.ForeColor           = Colors.MintCream;
            lblStatus.Font                = new Font("Poppins-regular", 9F, FontStyle.Italic);
            lblStatus.Location            = new Point(30, 490);
            lblStatus.AutoSize            = true;

            // ── Main layout ────────────────────────────────
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 80, 20, 20),
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Colors.DarkGreen
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var modePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Colors.DarkGreen
            };
            modePanel.Controls.Add(lblMode);
            modePanel.Controls.Add(cmbMode);

            var remapPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                BackColor = Colors.DarkGreen
            };
            remapPanel.Controls.Add(lblRemap);
            remapPanel.Controls.Add(txtRemap);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Colors.DarkGreen
            };
            buttonPanel.Controls.Add(btnConnect);
            buttonPanel.Controls.Add(btnSave);

            layout.Controls.Add(modePanel, 0, 0);
            layout.Controls.Add(pnlKeyGrid, 0, 1);
            layout.SetRowSpan(pnlKeyGrid, 2);
            layout.Controls.Add(remapPanel, 1, 0);
            layout.Controls.Add(buttonPanel, 0, 2);
            layout.Controls.Add(lblStatus, 0, 3);
            Controls.Add(layout);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
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
            lblStatus.ForeColor = Colors.MidGreen;
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

                selectedKeyButton.IsSelected = false;
                selectedKeyButton.BackColor = Colors.MintCream;
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
                lblStatus.ForeColor = Colors.MintCream;
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
                lblStatus.ForeColor = Colors.MidGreen;
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
            catch { }
        }
        #endregion
    }

    public class KeyButton : Panel
    {
        public string PhysicalKey { get; set; } = "";
        public string RemapText   { get; set; } = "";
        public bool   IsSelected  { get; set; } = false;
        public int    CornerRadius { get; set; } = 18;
        public Color  OutlineColor { get; set; } = MainForm.Colors.TealGreen;
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
            e.Graphics.Clear(Parent?.BackColor ?? MainForm.Colors.MintCream);

            var inset = OutlineThickness / 2f + 1;
            var rect = new Rectangle((int)inset, (int)inset, (int)(Width - inset * 2), (int)(Height - inset * 2));
            using var path = CreateRoundedRectangle(rect, CornerRadius);

            var shadow = new Rectangle((int)rect.X + 2, (int)rect.Y + 2, (int)rect.Width, (int)rect.Height);
            using (var sb = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                e.Graphics.FillPath(sb, CreateRoundedRectangle(shadow, CornerRadius));

            Color fill = IsSelected ? Colors.MidGreen
                        : isHovered  ? Colors.LightTeal
                        : Colors.MintCream;

            using (var brush = new SolidBrush(fill))
                e.Graphics.FillPath(brush, path);

            using (var pen = new Pen(OutlineColor, OutlineThickness))
                e.Graphics.DrawPath(pen, path);

            if (IsSelected)
            {
                using var selPen = new Pen(Colors.DarkGreen, OutlineThickness + 2);
                e.Graphics.DrawPath(selPen, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                new Font("Poppins-regular", 14F, FontStyle.Bold),
                rect,
                Colors.DarkGreen,
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