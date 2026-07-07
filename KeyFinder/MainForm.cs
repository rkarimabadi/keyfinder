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
    private ComboBox cmbRecency;

    public MainForm()
    {
        Text = "KeyFinder - GitHub API Key Scanner";
        Size = new Size(1200, 800);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Shield;

        LoadConfig();

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(245, 245, 250)
        };

        var topPanel = CreateTopPanel();
        mainPanel.Controls.Add(topPanel, 0, 0);

        var controlsPanel = CreateControlsPanel();
        mainPanel.Controls.Add(controlsPanel, 0, 1);

        dgvResults = CreateResultsGrid();
        mainPanel.Controls.Add(dgvResults, 0, 2);

        txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.Lime,
            Font = new Font("Consolas", 9),
            WordWrap = false,
            MaxLength = 1_000_000,
            BorderStyle = BorderStyle.None
        };
        var logPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        logPanel.Controls.Add(txtLog);
        mainPanel.Controls.Add(logPanel, 0, 3);

        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        Controls.Add(mainPanel);

        statusStrip = new StatusStrip { BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White };
        lblStatus = new ToolStripStatusLabel("Ready") { ForeColor = Color.White };
        progressBar = new ToolStripProgressBar { Visible = false, Style = ProgressBarStyle.Marquee };
        statusStrip.Items.Add(lblStatus);
        statusStrip.Items.Add(progressBar);
        Controls.Add(statusStrip);
    }

    private Panel CreateTopPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Height = 36, Margin = new Padding(0) };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            Padding = new Padding(0)
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        table.Controls.Add(new Label
        {
            Text = "GitHub Token:", AutoSize = true, Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 4, 0)
        }, 0, 0);

        txtToken = new TextBox
        {
            Text = _config.GitHub.Tokens.Count > 0 ? _config.GitHub.Tokens[0] : "",
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Consolas", 9),
            Margin = new Padding(0, 0, 6, 0)
        };
        table.Controls.Add(txtToken, 1, 0);

        btnScan = new Button { Text = "Scan", Width = 90, Height = 30, BackColor = Color.DodgerBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 4, 0) };
        btnScan.Click += BtnScan_Click;
        table.Controls.Add(btnScan, 2, 0);

        btnStop = new Button { Text = "Stop", Width = 90, Height = 30, BackColor = Color.Crimson, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false, Margin = new Padding(0, 0, 4, 0) };
        btnStop.Click += BtnStop_Click;
        table.Controls.Add(btnStop, 3, 0);

        var settingsBtn = new Button { Text = "⚙ Settings", Width = 90, Height = 30, FlatStyle = FlatStyle.Flat, Margin = new Padding(0) };
        settingsBtn.Click += (_, _) =>
        {
            var result = MessageBox.Show($"Token: {txtToken.Text}\n\nProvider: {cmbProvider.SelectedItem}\nMax Results: {numMaxResults.Value}\nRecency: {cmbRecency.SelectedItem}\nVerify on scan: {chkVerifyOnScan.Checked}\n\nSettings saved to:\n{ConfigPath}",
                "Settings Overview", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        table.Controls.Add(settingsBtn, 4, 0);

        panel.Controls.Add(table);
        return panel;
    }

    private Panel CreateControlsPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Height = 34, Margin = new Padding(0, 4, 0, 4) };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0),
            AutoSize = true
        };

        flow.Controls.Add(new Label { Text = "Provider:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        cmbProvider = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        cmbProvider.Items.Add("all");
        foreach (var (name, id, _) in PatternProvider.GetAll())
            cmbProvider.Items.Add(id);
        cmbProvider.SelectedIndex = 0;
        flow.Controls.Add(cmbProvider);

        flow.Controls.Add(new Label { Text = "Max:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 2, 0) });
        numMaxResults = new NumericUpDown { Minimum = 10, Maximum = 1000, Value = _config.Scan.MaxResults, Width = 70 };
        flow.Controls.Add(numMaxResults);

        flow.Controls.Add(new Label { Text = "Recency:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 2, 0) });
        cmbRecency = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        cmbRecency.Items.AddRange(new[] { "Any time", "Last 24h", "Last 7 days", "Last 30 days", "Last 90 days", "Last year" });
        cmbRecency.SelectedIndex = _config.Scan.RecencyDays switch { 1 => 1, 7 => 2, 30 => 3, 90 => 4, 365 => 5, _ => 0 };
        flow.Controls.Add(cmbRecency);

        chkVerifyOnScan = new CheckBox { Text = "Auto-verify", AutoSize = true, Margin = new Padding(10, 2, 0, 0) };
        flow.Controls.Add(chkVerifyOnScan);

        btnVerify = new Button { Text = "Verify Selected", Width = 110, Height = 28, Enabled = false, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 0, 4, 0) };
        btnVerify.Click += BtnVerify_Click;
        flow.Controls.Add(btnVerify);

        btnExport = new Button { Text = "Export", Width = 80, Height = 28, Enabled = false, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 0, 0) };
        btnExport.Click += BtnExport_Click;
        flow.Controls.Add(btnExport);

        panel.Controls.Add(flow);
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
        _config.Scan.RecencyDays = cmbRecency.SelectedIndex switch
        {
            1 => 1, 2 => 7, 3 => 30, 4 => 90, 5 => 365, _ => 0
        };
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
