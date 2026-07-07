using KeyFinder.Models;
using KeyFinder.Services;
using Newtonsoft.Json;

namespace KeyFinder;

public partial class MainForm : Form
{
    private AppConfig _config;
    private ScannerService _scanner;
    private VerifierService _verifier;
    private List<KeyFinding> _findings = new();
    private CancellationTokenSource _cts;

    private DataGridView dgvResults;
    private ComboBox cmbProvider;
    private TextBox txtToken;
    private Button btnScan;
    private Button btnStop;
    private Button btnVerify;
    private Button btnExport;
    private RichTextBox txtLog;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;
    private ToolStripProgressBar progressBar;
    private CheckBox chkVerifyOnScan;
    private NumericUpDown numMaxResults;

    public MainForm()
    {
        Text = "KeyFinder - GitHub API Key Scanner";
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Shield;

        LoadConfig();

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };

        var topPanel = CreateTopPanel();
        mainPanel.Controls.Add(topPanel, 0, 0);

        dgvResults = CreateResultsGrid();
        mainPanel.Controls.Add(dgvResults, 0, 1);

        txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.Lime,
            Font = new Font("Consolas", 9),
            WordWrap = false,
            MaxLength = 1_000_000
        };
        mainPanel.Controls.Add(txtLog, 0, 2);

        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        Controls.Add(mainPanel);

        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel("Ready");
        progressBar = new ToolStripProgressBar { Visible = false, Style = ProgressBarStyle.Marquee };
        statusStrip.Items.Add(lblStatus);
        statusStrip.Items.Add(progressBar);
        Controls.Add(statusStrip);
    }

    private Panel CreateTopPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

        var tokenPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        tokenPanel.Controls.Add(new Label { Text = "GitHub Token:", AutoSize = true, Anchor = AnchorStyles.Left });
        txtToken = new TextBox
        {
            Text = _config.GitHub.Tokens.Count > 0 ? _config.GitHub.Tokens[0] : "",
            Width = 500,
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        tokenPanel.Controls.Add(txtToken);
        panel.Controls.Add(tokenPanel);

        var controlsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        controlsPanel.Controls.Add(new Label { Text = "Provider:", AutoSize = true, Anchor = AnchorStyles.Left });
        cmbProvider = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        cmbProvider.Items.Add("all");
        foreach (var (name, id, _) in PatternProvider.GetAll())
            cmbProvider.Items.Add(id);
        cmbProvider.SelectedIndex = 0;
        controlsPanel.Controls.Add(cmbProvider);

        controlsPanel.Controls.Add(new Label { Text = "Max Results:", AutoSize = true, Anchor = AnchorStyles.Left });
        numMaxResults = new NumericUpDown { Minimum = 10, Maximum = 1000, Value = _config.Scan.MaxResults, Width = 80 };
        controlsPanel.Controls.Add(numMaxResults);

        chkVerifyOnScan = new CheckBox { Text = "Verify on scan", AutoSize = true };
        controlsPanel.Controls.Add(chkVerifyOnScan);

        btnScan = new Button { Text = "Scan", Width = 100, Height = 30, BackColor = Color.DodgerBlue, ForeColor = Color.White };
        btnScan.Click += BtnScan_Click;
        controlsPanel.Controls.Add(btnScan);

        btnStop = new Button { Text = "Stop", Width = 100, Height = 30, BackColor = Color.Crimson, ForeColor = Color.White, Enabled = false };
        btnStop.Click += BtnStop_Click;
        controlsPanel.Controls.Add(btnStop);

        btnVerify = new Button { Text = "Verify Selected", Width = 120, Height = 30, Enabled = false };
        btnVerify.Click += BtnVerify_Click;
        controlsPanel.Controls.Add(btnVerify);

        btnExport = new Button { Text = "Export JSON", Width = 100, Height = 30, Enabled = false };
        btnExport.Click += BtnExport_Click;
        controlsPanel.Controls.Add(btnExport);

        panel.Controls.Add(controlsPanel);

        var lblBanner = new Label
        {
            Text = "▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓\r\n▓ KeyFinder ▓ ▓ GitHub   ▓ ▓ API Key  ▓ ▓ Scanner   ▓ ▓ v1.0     ▓ ▓ by fadidevv\r\n▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓",
            Font = new Font("Consolas", 8),
            ForeColor = Color.Cyan,
            AutoSize = true
        };
        panel.Controls.Add(lblBanner);

        return panel;
    }

    private DataGridView CreateResultsGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            BackgroundColor = Color.White,
            Font = new Font("Consolas", 9),
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        };
        grid.Columns.Add("Provider", "Provider");
        grid.Columns.Add("Key", "Key (masked)");
        grid.Columns.Add("Repo", "Repository");
        grid.Columns.Add("Owner", "Owner");
        grid.Columns.Add("FilePath", "File");
        grid.Columns.Add("Verified", "Verified");
        grid.Columns.Add("FileUrl", "URL");
        grid.Columns["FileUrl"]!.Visible = false;

        var copyCol = new DataGridViewButtonColumn
        {
            Name = "Copy",
            HeaderText = "",
            Text = "📋 Copy",
            UseColumnTextForButtonValue = true,
            Width = 70,
            FlatStyle = FlatStyle.Flat
        };
        grid.Columns.Add(copyCol);
        grid.CellClick += GridCopy_CellClick;

        return grid;
    }

    private void GridCopy_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != dgvResults.Columns["Copy"]!.Index) return;
        if (e.RowIndex >= _findings.Count) return;
        Clipboard.SetText(_findings[e.RowIndex].Key);
        Log($"Copied key to clipboard: {_findings[e.RowIndex].KeyMasked}", Color.Cyan);
    }

    private static string ConfigDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KeyFinder");
    private static string ConfigPath => Path.Combine(ConfigDir, "settings.json");

    private void LoadConfig()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                _config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                return;
            }
            catch { }
        }
        _config = new AppConfig();
    }

    private void SaveConfig()
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_config, Formatting.Indented));
    }

    private void Log(string msg, Color? color = null)
    {
        if (txtLog.IsDisposed) return;
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => Log(msg, color));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.SelectionLength = 0;
        txtLog.SelectionColor = Color.Gray;
        txtLog.AppendText($"[{timestamp}] ");
        txtLog.SelectionColor = color ?? Color.Lime;
        txtLog.AppendText(msg + "\n");
        txtLog.ScrollToCaret();
    }

    private void SetBusy(bool busy)
    {
        btnScan.Enabled = !busy;
        btnStop.Enabled = busy;
        progressBar.Visible = busy;
        lblStatus.Text = busy ? "Scanning..." : "Ready";
    }

    private async void BtnScan_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtToken.Text) || txtToken.Text.Contains("xxxx"))
        {
            MessageBox.Show("Please enter a valid GitHub token.", "Token Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _config.GitHub.Tokens = new() { txtToken.Text.Trim() };
        _config.Scan.MaxResults = (int)numMaxResults.Value;
        SaveConfig();

        _scanner = new ScannerService(_config);
        _scanner.OnNetworkLog = msg => Log(msg, Color.Orange);
        _verifier = new VerifierService();
        _cts = new CancellationTokenSource();

        dgvResults.Rows.Clear();
        txtLog.Clear();
        SetBusy(true);
        btnVerify.Enabled = false;
        btnExport.Enabled = false;

        Log("Starting scan...", Color.Cyan);
        Log($"Provider: {cmbProvider.SelectedItem}, Max results: {numMaxResults.Value}", Color.White);

        var progress = new Progress<ScanProgress>(p =>
        {
            Log(p.Message);
            lblStatus.Text = p.Message;
        });

        try
        {
            _findings = await _scanner.ScanAll(cmbProvider.SelectedItem.ToString()!, progress);

            foreach (var f in _findings)
            {
                var verified = f.Verified.HasValue ? (f.Verified.Value ? "✓" : "✗") : "-";
                dgvResults.Rows.Add(f.Provider, f.KeyMasked, f.RepoName, f.Owner, f.FilePath, verified, f.FileUrl);
            }

            Log($"Scan complete. Found {_findings.Count} keys.", Color.GreenYellow);
            lblStatus.Text = $"Found {_findings.Count} keys";

            if (_findings.Count > 0)
            {
                _scanner.SaveResults(_findings);
                Log($"Results saved to {_config.Output.OutputPath}/", Color.Cyan);

                if (chkVerifyOnScan.Checked)
                {
                    await VerifyResults();
                }

                btnVerify.Enabled = true;
                btnExport.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}", Color.Red);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        _scanner?.Stop();
        Log("Stopping scan...", Color.Yellow);
    }

    private async void BtnVerify_Click(object? sender, EventArgs e)
    {
        if (_findings.Count == 0) return;
        await VerifyResults();
    }

    private async Task VerifyResults()
    {
        if (_verifier == null) _verifier = new VerifierService();

        Log("Starting verification...", Color.Magenta);

        var progress = new Progress<string>(msg => Log(msg));

        var verified = await _verifier.Verify(_findings.ToList(), null, progress);

        var activeCount = verified.Count(v => v.IsActive);
        Log($"Verification complete. Active: {activeCount}, Invalid: {verified.Count - activeCount}", Color.GreenYellow);

        dgvResults.Rows.Clear();
        foreach (var v in verified)
        {
            var verifiedStr = v.IsActive ? "✓ ACTIVE" : "✗ INVALID";
            var rowColor = v.IsActive ? Color.LightPink : Color.White;
            var idx = dgvResults.Rows.Add(v.Finding.Provider, v.Finding.KeyMasked, v.Finding.RepoName,
                v.Finding.Owner, v.Finding.FilePath, verifiedStr, v.Finding.FileUrl);
            if (v.IsActive)
                dgvResults.Rows[idx].DefaultCellStyle.BackColor = Color.LightSalmon;
        }
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv",
            DefaultExt = "json",
            FileName = $"keyfinder_results_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (sfd.ShowDialog() != DialogResult.OK) return;

        if (sfd.FilterIndex == 1)
        {
            File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(_findings, Formatting.Indented));
        }
        else
        {
            var lines = new List<string> { "provider,key_masked,repo,owner,file_path,file_url" };
            lines.AddRange(_findings.Select(f =>
                $"\"{f.Provider}\",\"{f.KeyMasked}\",\"{f.RepoName}\",\"{f.Owner}\",\"{f.FilePath}\",\"{f.FileUrl}\""));
            File.WriteAllLines(sfd.FileName, lines);
        }

        Log($"Exported to {sfd.FileName}", Color.Cyan);
    }
}
