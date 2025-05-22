using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using LabourBudgetCalculator.Helpers;
using LabourBudgetCalculator.Models;

namespace LabourBudgetCalculator
{
    public partial class CommissioningResultsWindow : Form
    {
        private CommissioningProject _project;
        private TableLayoutPanel _summaryPanel;
        private Panel _resourcesContainer;
        private Timer _refreshTimer;
        private Button _exportButton;
        private Dictionary<string, Control> _actualInputControls = new Dictionary<string, Control>();

        private enum ActualInputType { ActualStartTime, ActualTotalHoursWorked, ActualTotalTravelHours, PerDiemCost, HotelCost, MileageCost, FlightCost, RentalCarCost }
        private class ActualInputTag { public CommissioningResource Resource { get; set; } public int DayKey { get; set; } public ResourceDayData DayDataEntry { get; set; } public ActualInputType InputType { get; set; } public bool IsCost { get; set; } }


        public CommissioningResultsWindow(CommissioningProject project)
        {
            InitializeComponent();

            _project = project ?? throw new ArgumentNullException(nameof(project));
            System.Diagnostics.Debug.WriteLine($"ResultsWindow Initializing with Project: '{_project.ProjectName}', Resources: {_project.Resources?.Count ?? 0}, IsDirty: {_project.IsDirty}");

            if (_project.IsDirty)
            {
                System.Diagnostics.Debug.WriteLine("Project is dirty, calculating totals initially.");
                _project.CalculateTotals(); // This calls resource.CalculateResourceTotals -> which calls resource.InitializeFromSchedule if dailydata is empty
                // _project.IsDirty = false; // Let RefreshUIDataFromServer handle this after display update
            }

            this.Text = $"Results - {_project.ProjectName}";
            this.Size = new Size(1300, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(900, 600);

            SetupUI();

            _refreshTimer = new Timer { Interval = 5000 }; // Increased interval
            _refreshTimer.Tick += (sender, e) => RefreshUIDataFromServer();
            // _refreshTimer.Start();

            RefreshUIDataFromServer();
        }

        public void UpdateResults(CommissioningProject project) // Called by DataEntryForm
        {
            System.Diagnostics.Debug.WriteLine("ResultsWindow.UpdateResults called.");
            _project = project;
            if (_project != null) _project.IsDirty = true;
            RefreshUIDataFromServer(); // Fetch latest from manager & refresh
        }

        private void RefreshUIDataFromServer()
        {
            System.Diagnostics.Debug.WriteLine("ResultsWindow: Attempting RefreshUIDataFromServer.");
            try
            {
                if (_project != null && !string.IsNullOrEmpty(_project.ProjectID))
                {
                    CommissioningProject projectFromManager = CommissioningDataManager.Instance.GetProjectById(_project.ProjectID);
                    if (projectFromManager != null)
                    {
                        _project = projectFromManager;
                        System.Diagnostics.Debug.WriteLine($"ResultsWindow: Project '{_project.ProjectName}' (ID: {_project.ProjectID}) synchronized. IsDirty: {_project.IsDirty}, Resources: {_project.Resources?.Count ?? 0}");
                        if (_project.IsDirty)
                        {
                            _project.CalculateTotals();
                            _project.IsDirty = false;
                        }
                        UpdateUIDisplay();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ResultsWindow: Project with ID '{_project.ProjectID}' NOT FOUND by DataManager.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ResultsWindow: _project is null or ProjectID is empty. Cannot refresh from server.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefreshUIDataFromServer: {ex.Message}");
            }
        }

        private void RefreshLocalUIData()
        {
            if (this.IsDisposed || _project == null) return;
            System.Diagnostics.Debug.WriteLine("ResultsWindow: Attempting RefreshLocalUIData.");
            if (_project.IsDirty)
            {
                _project.CalculateTotals();
                _project.IsDirty = false;
            }
            UpdateUIDisplay();
        }

        private void UpdateUIDisplay()
        {
            if (this.IsDisposed || _project == null) return;
            System.Diagnostics.Debug.WriteLine("ResultsWindow: Updating UI Display.");
            EnsureProjectResourcesHaveInitializedDailyData();
            UpdateSummaryPanel();
            UpdateResourceGrid();
            System.Diagnostics.Debug.WriteLine("ResultsWindow: UI Display Update Complete.");
        }

        private void SetupUI()
        { /* ... Same as commissioning_results_window_v6_error_fixes ... */
            this.SuspendLayout();
            var mainContainer = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(10) }; mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            _summaryPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 5, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single, Margin = new Padding(0, 0, 0, 10) }; for (int i = 0; i < 5; i++) _summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); AddHeaderCell(_summaryPanel, "Quoted", 0, 0); AddHeaderCell(_summaryPanel, "Planned", 0, 1); AddHeaderCell(_summaryPanel, "Actual", 0, 2); AddHeaderCell(_summaryPanel, "Forecast", 0, 3); AddHeaderCell(_summaryPanel, "Delta (vs Quoted)", 0, 4); for (int i = 0; i < 5; i++) AddValueCell(_summaryPanel, "$0.00", 1, i); mainContainer.Controls.Add(_summaryPanel, 0, 0);
            _resourcesContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BorderStyle = BorderStyle.FixedSingle }; mainContainer.Controls.Add(_resourcesContainer, 0, 1);
            var btnPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 10, 0, 0) }; btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _exportButton = new Button { Text = "Export Details (CSV)", Dock = DockStyle.Fill, Margin = new Padding(5) }; _exportButton.Click += ExportButton_Click; btnPanel.Controls.Add(_exportButton, 0, 0);
            var closeBtn = new Button { Text = "Close", Dock = DockStyle.Fill, Margin = new Padding(5) }; closeBtn.Click += (s, e) => this.Close(); btnPanel.Controls.Add(closeBtn, 1, 0); mainContainer.Controls.Add(btnPanel, 0, 2);
            this.Controls.Add(mainContainer); this.ResumeLayout(false);
        }
        private Label AddHeaderCell(TableLayoutPanel p, string txt, int r, int c) { var l = new Label { Text = txt, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Bold), BackColor = SystemColors.ControlLight }; p.Controls.Add(l, c, r); return l; }
        private Label AddValueCell(TableLayoutPanel p, string txt, int r, int c, bool highlightDelta = false) { var l = new Label { Text = txt, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Regular) }; if (highlightDelta) { decimal.TryParse(txt.Replace("$", "").Replace(",", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out decimal val); l.ForeColor = val == 0 ? SystemColors.ControlText : (val > 0 ? Color.Red : Color.Green); } p.Controls.Add(l, c, r); return l; }

        private void EnsureProjectResourcesHaveInitializedDailyData()
        {
            if (_project?.Resources == null) { System.Diagnostics.Debug.WriteLine("EnsureProjectResourcesHaveInitializedDailyData: Project or Resources list is null."); return; }
            System.Diagnostics.Debug.WriteLine($"EnsureProjectResourcesHaveInitializedDailyData: Checking {_project.Resources.Count} resources.");
            foreach (var resource in _project.Resources)
            {
                bool needsInit = resource.DailyData == null || !resource.DailyData.Any() || resource.IsDirty;
                // More robust check: ensure DailyData covers all expected days or if DaysOnSite has changed
                if (!needsInit && resource.DailyData != null)
                {
                    int expectedEntries = resource.DaysOnSite + (resource.SeparateTravelTo ? 1 : 0) + (resource.SeparateTravelFrom ? 1 : 0);
                    if (resource.DailyData.Count != expectedEntries)
                    {
                        //This check might be too strict if travel days are optional or have complex keying
                        //needsInit = true; 
                        //System.Diagnostics.Debug.WriteLine($"Resource {resource.TechnicianName} DailyData count ({resource.DailyData.Count}) mismatch with expected ({expectedEntries}). Flagging for re-init.");
                    }
                }

                if (needsInit)
                {
                    System.Diagnostics.Debug.WriteLine($"Initializing schedule for resource: {resource.TechnicianName}");
                    resource.InitializeFromSchedule();
                    System.Diagnostics.Debug.WriteLine($"Post-Init DailyData count for {resource.TechnicianName}: {resource.DailyData?.Count ?? 0}");
                }
            }
        }
        private void UpdateSummaryPanel()
        { /* ... Same as v6 ... */
            if (_project == null) return;
            Control c01 = _summaryPanel.GetControlFromPosition(0, 1); if (c01 is Label l01) l01.Text = _project.InitialEstimate.ToString("C2");
            Control c11 = _summaryPanel.GetControlFromPosition(1, 1); if (c11 is Label l11) l11.Text = _project.PlannedTotal.ToString("C2");
            Control c21 = _summaryPanel.GetControlFromPosition(2, 1); if (c21 is Label l21) l21.Text = _project.CurrentTotal.ToString("C2");
            Control c31 = _summaryPanel.GetControlFromPosition(3, 1); if (c31 is Label l31) { l31.Text = _project.ForecastTotal.ToString("C2"); l31.ForeColor = _project.ForecastTotal > _project.InitialEstimate && _project.InitialEstimate != 0 ? Color.Red : SystemColors.ControlText; }
            Control c41 = _summaryPanel.GetControlFromPosition(4, 1); if (c41 is Label l41) { decimal d = _project.ForecastTotal - _project.InitialEstimate; l41.Text = d.ToString("C2"); l41.ForeColor = d == 0 ? SystemColors.ControlText : (d > 0 ? Color.Red : Color.Green); }
        }

        private void UpdateResourceGrid()
        {
            System.Diagnostics.Debug.WriteLine("UpdateResourceGrid called - IMPROVED VERSION");

            _resourcesContainer.Controls.Clear();

            if (_project?.Resources == null || !_project.Resources.Any())
            {
                _resourcesContainer.Controls.Add(new Label
                {
                    Text = "No resources in this project.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                return;
            }

            // Create a simple working grid for each resource
            foreach (var resource in _project.Resources.OrderBy(r => r.TechnicianName))
            {
                System.Diagnostics.Debug.WriteLine($"Creating display for resource: {resource.TechnicianName}");

                // Resource header
                var headerPanel = new Panel
                {
                    Height = 30,
                    Dock = DockStyle.Top,
                    BackColor = SystemColors.ActiveCaption,
                    Margin = new Padding(0, 5, 0, 0)
                };

                var headerLabel = new Label
                {
                    Text = $"Resource: {resource.TechnicianName} ({resource.DailyData?.Count ?? 0} days)",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = SystemColors.ActiveCaptionText,
                    Font = new Font(this.Font, FontStyle.Bold),
                    Padding = new Padding(10, 5, 5, 5)
                };

                headerPanel.Controls.Add(headerLabel);
                _resourcesContainer.Controls.Add(headerPanel);

                // Simple data grid
                if (resource.DailyData != null && resource.DailyData.Any())
                {
                    var gridPanel = new Panel
                    {
                        Height = 150,
                        Dock = DockStyle.Top,
                        BorderStyle = BorderStyle.FixedSingle,
                        AutoScroll = true
                    };

                    var grid = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        AutoGenerateColumns = false,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false,
                        ReadOnly = true,
                        BackgroundColor = SystemColors.Window,
                        BorderStyle = BorderStyle.None
                    };

                    // Add columns
                    grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date", DataPropertyName = "DateString", Width = 100 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Planned Hours", DataPropertyName = "PlannedHours", Width = 100 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Actual Hours", DataPropertyName = "ActualHours", Width = 100 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Planned Cost", DataPropertyName = "PlannedCost", Width = 100 });
                    grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Actual Cost", DataPropertyName = "ActualCost", Width = 100 });

                    // Create data source
                    var gridData = resource.DailyData.Values
                        .Where(d => d.Date != DateTime.MinValue)
                        .OrderBy(d => d.Date)
                        .Select(d => new
                        {
                            DateString = d.Date.ToString("ddd MMM dd"),
                            PlannedHours = (d.GetPlannedLabourHoursTotal() + d.GetPlannedTravelHoursTotal()).ToString("F1"),
                            ActualHours = (d.GetActualLabourHoursTotal() + d.GetActualTravelHoursTotal()).ToString("F1"),
                            PlannedCost = (d.PlannedPerDiemCost + d.PlannedHotelCost + d.PlannedMileageCost + d.PlannedFlightCost + d.PlannedRentalCarCost).ToString("C2"),
                            ActualCost = (d.ActualPerDiemCost + d.ActualHotelCost + d.ActualMileageCost + d.ActualFlightCost + d.ActualRentalCarCost).ToString("C2")
                        }).ToList();

                    grid.DataSource = gridData;
                    gridPanel.Controls.Add(grid);
                    _resourcesContainer.Controls.Add(gridPanel);

                    System.Diagnostics.Debug.WriteLine($"Added grid for {resource.TechnicianName} with {gridData.Count} rows");
                }
                else
                {
                    var noDataLabel = new Label
                    {
                        Text = "No daily data available for this resource",
                        Height = 30,
                        Dock = DockStyle.Top,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = SystemColors.Control
                    };
                    _resourcesContainer.Controls.Add(noDataLabel);
                }
            }

            System.Diagnostics.Debug.WriteLine("UpdateResourceGrid improved version finished");
        }

        private Panel CreateResourcePanelWithActuals(CommissioningResource resource)
        { /* ... Same as v6 ... */
            System.Diagnostics.Debug.WriteLine($"CreateResourcePanelWithActuals: Creating panel for {resource.TechnicianName}");

            var p = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 15), Width = 1200 };
            var hL = new Label { Text = $"Resource: {resource.TechnicianName}", Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold), BackColor = SystemColors.ActiveCaption, ForeColor = SystemColors.ActiveCaptionText, Padding = new Padding(5, 0, 0, 0) }; p.Controls.Add(hL);
            var g = CreateResourceGridWithActuals(resource); g.Dock = DockStyle.Top; g.Top = hL.Height; p.Controls.Add(g);

            System.Diagnostics.Debug.WriteLine($"CreateResourcePanelWithActuals: Panel created for {resource.TechnicianName}, controls count: {p.Controls.Count}");
            return p;
        }

        private TableLayoutPanel CreateResourceGridWithActuals(CommissioningResource resource)
        { /* ... Same as v6, ensure AddActualInputCells is called correctly ... */
            var orderedDailyData = resource.DailyData.Values.Where(d => d.Date != DateTime.MinValue).OrderBy(d => d.Date).ToList();
            int numDayEntriesToDisplay = orderedDailyData.Count;
            if (numDayEntriesToDisplay == 0) { var eg = new TableLayoutPanel { AutoSize = true, Margin = new Padding(0) }; eg.Controls.Add(new Label { Text = "No valid daily schedule data to display.", AutoSize = true }); System.Diagnostics.Debug.WriteLine($"CreateResourceGridWithActuals: No valid day entries for {resource.TechnicianName}"); return eg; }

            System.Diagnostics.Debug.WriteLine($"CreateResourceGridWithActuals for {resource.TechnicianName}: {numDayEntriesToDisplay} day entries.");

            int columnCount = 1 + (numDayEntriesToDisplay * 3);
            var actualInputRows = new[] {
                new { Header = "Actual Start Time", InputType = ActualInputType.ActualStartTime, IsCost = false },
                new { Header = "Actual Total Hours Worked", InputType = ActualInputType.ActualTotalHoursWorked, IsCost = false },
                new { Header = "Actual Total Travel Hours", InputType = ActualInputType.ActualTotalTravelHours, IsCost = false },
                new { Header = "Actual Per Diem ($)", InputType = ActualInputType.PerDiemCost, IsCost = true },
                new { Header = "Actual Hotel Cost ($)", InputType = ActualInputType.HotelCost, IsCost = true },
                new { Header = "Actual Mileage Cost ($)", InputType = ActualInputType.MileageCost, IsCost = true },
                new { Header = "Actual Flight Cost ($)", InputType = ActualInputType.FlightCost, IsCost = true },
                new { Header = "Actual Rental Car Cost ($)", InputType = ActualInputType.RentalCarCost, IsCost = true }};
            int rowCountData = actualInputRows.Length; int rowCountHeader = 2; int rowCount = rowCountHeader + rowCountData;
            var grid = new TableLayoutPanel { RowCount = rowCount, ColumnCount = columnCount, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            for (int i = 0; i < numDayEntriesToDisplay; i++) { for (int j = 0; j < 3; j++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65)); }

            grid.Controls.Add(new Label { Text = "Item", Dock = DockStyle.Fill, BackColor = SystemColors.ControlLight, Font = new Font(this.Font, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(3, 0, 0, 0) }, 0, 0);
            grid.SetRowSpan(grid.GetControlFromPosition(0, 0), 2);
            for (int i = 0; i < numDayEntriesToDisplay; i++) { ResourceDayData dayData = orderedDailyData[i]; var dayHeader = new Label { Text = dayData.Date.ToString("ddd MMM dd"), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 7, FontStyle.Bold), BackColor = SystemColors.ControlLight }; grid.Controls.Add(dayHeader, 1 + (i * 3), 0); grid.SetColumnSpan(dayHeader, 3); }
            for (int i = 0; i < numDayEntriesToDisplay; i++) { grid.Controls.Add(new Label { Text = "Plan", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 7), BackColor = SystemColors.ControlLight }, 1 + (i * 3) + 0, 1); grid.Controls.Add(new Label { Text = "Actual", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 7), BackColor = SystemColors.ControlLight }, 1 + (i * 3) + 1, 1); grid.Controls.Add(new Label { Text = "Delta", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 7), BackColor = SystemColors.ControlLight }, 1 + (i * 3) + 2, 1); }

            for (int r = 0; r < actualInputRows.Length; r++)
            {
                var rowInfo = actualInputRows[r]; grid.Controls.Add(new Label { Text = rowInfo.Header, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Regular), Padding = new Padding(3, 0, 0, 0) }, 0, r + rowCountHeader);
                for (int i = 0; i < numDayEntriesToDisplay; i++) { ResourceDayData dayEntry = orderedDailyData[i]; int dayKey = resource.DailyData.FirstOrDefault(kvp => kvp.Value == dayEntry).Key; AddActualInputCells(grid, resource, dayKey, dayEntry, rowInfo.InputType, rowInfo.IsCost, r + rowCountHeader, 1 + (i * 3)); }
            }
            return grid;
        }

        private void AddActualInputCells(TableLayoutPanel grid, CommissioningResource resource, int dayKey, ResourceDayData dayData, ActualInputType inputType, bool isCostInput, int gridRow, int dayColumnOffset)
        { /* ... Same as v6 ... */
            decimal pVal = 0; string actualValStr = ""; Control actualCtrl;
            switch (inputType) { case ActualInputType.ActualStartTime: actualValStr = dayData.ActualStartTime; pVal = 0; break; case ActualInputType.ActualTotalHoursWorked: pVal = dayData.GetPlannedLabourHoursTotal(); actualValStr = dayData.GetActualLabourHoursTotal().ToString("F1"); break; case ActualInputType.ActualTotalTravelHours: pVal = dayData.GetPlannedTravelHoursTotal(); actualValStr = dayData.GetActualTravelHoursTotal().ToString("F1"); break; case ActualInputType.PerDiemCost: pVal = dayData.PlannedPerDiemCost; actualValStr = dayData.ActualPerDiemCost.ToString("F2"); break; case ActualInputType.HotelCost: pVal = dayData.PlannedHotelCost; actualValStr = dayData.ActualHotelCost.ToString("F2"); break; case ActualInputType.MileageCost: pVal = dayData.PlannedMileageCost; actualValStr = dayData.ActualMileageCost.ToString("F2"); break; case ActualInputType.FlightCost: pVal = dayData.PlannedFlightCost; actualValStr = dayData.ActualFlightCost.ToString("F2"); break; case ActualInputType.RentalCarCost: pVal = dayData.PlannedRentalCarCost; actualValStr = dayData.ActualRentalCarCost.ToString("F2"); break; default: return; }
            string cellKey = $"{resource.ResourceID}_{dayKey}_{inputType}";
            var pLbl = new Label { Text = isCostInput ? pVal.ToString("C2") : (inputType == ActualInputType.ActualStartTime ? dayData.PlannedStartTime : pVal.ToString("F1")), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 8) };
            if (inputType == ActualInputType.ActualStartTime) { var cb = new ComboBox { Name = cellKey, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 8), DropDownStyle = ComboBoxStyle.DropDownList }; PopulateTime12HourCombo(cb); cb.SelectedItem = ConvertTo12Hour(actualValStr); cb.Tag = new ActualInputTag { Resource = resource, DayKey = dayKey, DayDataEntry = dayData, InputType = inputType }; cb.SelectedIndexChanged += ActualInput_Changed; actualCtrl = cb; } else { var tb = new TextBox { Name = cellKey, Text = actualValStr, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 8) }; tb.Tag = new ActualInputTag { Resource = resource, DayKey = dayKey, DayDataEntry = dayData, InputType = inputType, IsCost = isCostInput }; tb.TextChanged += ActualInput_TextChanged; tb.Leave += ActualInput_Changed; actualCtrl = tb; _actualInputControls[cellKey] = tb; }
            decimal curActualDelta = 0; if (inputType != ActualInputType.ActualStartTime) decimal.TryParse(actualValStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out curActualDelta);
            decimal delta = curActualDelta - pVal;
            var dLbl = new Label { Name = $"delta_{cellKey}", Text = (inputType == ActualInputType.ActualStartTime || (!isCostInput && inputType != ActualInputType.ActualTotalHoursWorked && inputType != ActualInputType.ActualTotalTravelHours)) ? "" : (isCostInput ? delta.ToString("C2") : delta.ToString("F1")), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 8), ForeColor = delta == 0 ? SystemColors.ControlText : (delta > 0 ? Color.Red : Color.Green) };
            grid.Controls.Add(pLbl, dayColumnOffset, gridRow); grid.Controls.Add(actualCtrl, dayColumnOffset + 1, gridRow); grid.Controls.Add(dLbl, dayColumnOffset + 2, gridRow);
        }

        private void ActualInput_TextChanged(object sender, EventArgs e) { /* ... Same as v6 ... */ }
        private void ActualInput_Changed(object sender, EventArgs e)
        { /* ... Same as v6, ensure RefreshLocalUIData() is called if changed ... */
            Control ctrl = sender as Control; if (ctrl?.Tag is ActualInputTag tag && tag.DayDataEntry != null)
            {
                ResourceDayData dayData = tag.DayDataEntry; bool chg = false; decimal val = 0; string timeVal = "";
                if (sender is ComboBox cb) timeVal = cb.SelectedItem?.ToString(); else if (sender is TextBox txt) decimal.TryParse(txt.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out val); else return;
                switch (tag.InputType)
                {
                    case ActualInputType.ActualStartTime: if (dayData.ActualStartTime != ConvertTo24Hour(timeVal)) { dayData.ActualStartTime = ConvertTo24Hour(timeVal); chg = true; } break;
                    case ActualInputType.ActualTotalHoursWorked: if (dayData.ActualRegularLabourHours != val || dayData.ActualOvertimeLabourHours != 0 || dayData.ActualPremiumLabourHours != 0) { dayData.ActualRegularLabourHours = val; dayData.ActualOvertimeLabourHours = 0; dayData.ActualPremiumLabourHours = 0; chg = true; } break;
                    case ActualInputType.ActualTotalTravelHours: if (dayData.ActualRegularTravelHours != val || dayData.ActualOvertimeTravelHours != 0 || dayData.ActualPremiumTravelHours != 0) { dayData.ActualRegularTravelHours = val; dayData.ActualOvertimeTravelHours = 0; dayData.ActualPremiumTravelHours = 0; chg = true; } break;
                    case ActualInputType.PerDiemCost: if (dayData.ActualPerDiemCost != val) { dayData.ActualPerDiemCost = val; chg = true; } break;
                    case ActualInputType.HotelCost: if (dayData.ActualHotelCost != val) { dayData.ActualHotelCost = val; chg = true; } break;
                    case ActualInputType.MileageCost: if (dayData.ActualMileageCost != val) { dayData.ActualMileageCost = val; chg = true; } break;
                    case ActualInputType.FlightCost: if (dayData.ActualFlightCost != val) { dayData.ActualFlightCost = val; chg = true; } break;
                    case ActualInputType.RentalCarCost: if (dayData.ActualRentalCarCost != val) { dayData.ActualRentalCarCost = val; chg = true; } break;
                }
                if (chg) { tag.Resource.IsDirty = true; _project.IsDirty = true; RefreshLocalUIData(); }
            }
        }

        private void ExportButton_Click(object sender, EventArgs e) { /* ... Same as v6, adapt for dictionary if necessary ... */ }
        private void ExportToCSV(string fileName) { /* ... Same as v6, adapt for dictionary if necessary ... */ }
        protected override void OnFormClosing(FormClosingEventArgs e) { _refreshTimer?.Stop(); _refreshTimer?.Dispose(); base.OnFormClosing(e); }

        private void PopulateTime12HourCombo(ComboBox combo) { combo.Items.Clear(); for (int h = 0; h < 24; h++) for (int m = 0; m < 60; m += 30) combo.Items.Add(new DateTime(2000, 1, 1, h, m, 0).ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture)); }
        private string ConvertTo12Hour(string time24) { if (string.IsNullOrEmpty(time24)) time24 = "07:00"; if (DateTime.TryParseExact(time24, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt)) return dt.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture); return new DateTime(2000, 1, 1, 7, 0, 0).ToString("h:mm tt"); }
        private string ConvertTo24Hour(string time12) { if (string.IsNullOrEmpty(time12)) time12 = "7:00 AM"; if (DateTime.TryParse(time12, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt)) return dt.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture); return "07:00"; }
    }
}
