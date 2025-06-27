using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using LabourBudgetCalculator.Helpers;
using LabourBudgetCalculator.Models;
// No System.Reflection needed if using DoubleBufferedPanel class

namespace LabourBudgetCalculator
{
    // Data-driven grid view model for the results window
    internal class ResultsGridViewModel
    {
        public List<ResultsGridColumn> Columns { get; set; } = new List<ResultsGridColumn>();
        public List<ResultsGridRow> Rows { get; set; } = new List<ResultsGridRow>();
    }

    internal class ResultsGridColumn
    {
        public string Header { get; set; }
        public DateTime? Date { get; set; } // Null for row headers
        public int DisplayIndex { get; set; }
    }

    internal class ResultsGridRow
    {
        public string Label { get; set; }
        public ResultsGridRowType RowType { get; set; }
        public List<ResultsGridCell> Cells { get; set; } = new List<ResultsGridCell>();
        public object Tag { get; set; } // For linking to underlying data if needed
    }

    internal enum ResultsGridRowType
    {
        Data,
        Subtotal,
        Total,
        Header
    }

    internal class ResultsGridCell
    {
        public string Value { get; set; }
        public ResultsGridCellType CellType { get; set; }
        public bool IsEditable { get; set; }
        public bool IsHighlighted { get; set; }
        public Color? BackColor { get; set; }
        public Color? ForeColor { get; set; }
        public object Tag { get; set; } // For linking to underlying data if needed
    }

    internal enum ResultsGridCellType
    {
        Planned,
        Actual,
        Delta,
        RowHeader,
        Empty
    }

    public partial class CommissioningResultsWindow : Form
    {
        private CommissioningProject _project;
        private TableLayoutPanel _summaryPanel;
        private Panel _mainPanel;
        private Button _exportButton;
        private Button _closeButton;
        private Button _darkModeButton;
        private bool _isDarkMode = false;
        private Color darkBackColor = Color.FromArgb(40, 44, 52);
        private Color darkTextColor = Color.FromArgb(220, 223, 228);
        private Color darkControlBackColor = Color.FromArgb(54, 60, 69);
        private Color darkBorderColor = Color.FromArgb(90, 100, 120);
        private Color darkButtonBackColor = Color.FromArgb(60, 70, 85);
        private Color darkButtonForeColor = Color.FromArgb(220, 223, 228);
        private Color darkPanelBackColor = Color.FromArgb(50, 55, 65);
        private Color darkGridBackColor = Color.FromArgb(45, 50, 60);
        private Color darkGridHeaderBackColor = Color.FromArgb(60, 70, 85);
        private Color darkGridCellBackColor = Color.FromArgb(55, 65, 80);
        private List<ResourcePanel> _resourcePanels = new List<ResourcePanel>();
        private bool _isClosing = false;
        private Panel _contentHostPanel;

        private void EnableDoubleBuffering()
        {
            // Enable double buffering for the entire form
            this.DoubleBuffered = true;

            // Enable double buffering for any DataGridViews
            foreach (Control control in this.Controls)
            {
                if (control is DataGridView dataGridView)
                {
                    // Use reflection to set the protected DoubleBuffered property
                    typeof(DataGridView).GetProperty("DoubleBuffered",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                        .SetValue(dataGridView, true, null);
                }
            }
        }
        private void OptimizeDataGridView(DataGridView grid)
        {
            if (grid == null) return;

            // Double buffer the grid for better performance
            typeof(DataGridView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
                .SetValue(grid, true, null);

            // Only redraw when necessary
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Speed up load times
            grid.VirtualMode = true;

            // Enable full row selection to improve selection performance
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        public CommissioningResultsWindow(CommissioningProject project)
        {
            this.DoubleBuffered = true;
            InitializeComponent();
            EnableDoubleBuffering();
            _project = project ?? throw new ArgumentNullException(nameof(project));

            // Load dark mode preference
            _isDarkMode = Properties.Settings.Default.ResultsDarkMode;

            this.Text = $"Results - {_project.ProjectName}";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1200, 600);

            this.Load += (s, e) =>
            {
                SetupUI();
                ApplyTheme();
                RefreshData();
            };
        }



        private void SetupUI()
        {
            try
            {
                this.SuspendLayout();
                var mainContainer = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 3,
                    ColumnCount = 1,
                    Padding = new Padding(10)
                };
                mainContainer.RowStyles.Clear();
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

                _summaryPanel = CreateSummaryPanel();
                mainContainer.Controls.Add(_summaryPanel, 0, 0);

                _mainPanel = new DoubleBufferedPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BorderStyle = BorderStyle.FixedSingle
                };
                mainContainer.Controls.Add(_mainPanel, 0, 1);

                _contentHostPanel = new DoubleBufferedFlowLayoutPanel
                {
                    Name = "contentHostPanel",
                    Dock = DockStyle.None, // Changed from DockStyle.Top
                    Location = Point.Empty, // Set location as it's no longer docked to fill/top
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    Padding = new Padding(10)
                };
                _mainPanel.Controls.Add(_contentHostPanel);

                var buttonPanel = CreateButtonPanel();
                mainContainer.Controls.Add(buttonPanel, 0, 2);
                this.Controls.Add(mainContainer);
                this.ResumeLayout(false);
            }
            catch (Exception ex) { MessageBox.Show("Error setting up UI: " + ex.Message); }
        }

        private TableLayoutPanel CreateSummaryPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 5, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single };
            panel.ColumnStyles.Clear();
            for (int i = 0; i < 5; i++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            string[] headers = { "Quoted", "Planned", "Forecast", "Versus Planned", "Versus Quoted" };
            for (int i = 0; i < headers.Length; i++)
            {
                panel.Controls.Add(new Label { Text = headers[i], TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Bold), BackColor = SystemColors.ControlLight }, i, 0);
            }
            string[] names = { "lblQuoted", "lblPlanned", "lblForecast", "lblDeltaPlanned", "lblDelta" };
            for (int i = 0; i < names.Length; i++)
            {
                panel.Controls.Add(new Label { Name = names[i], Text = "$0.00", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold) }, i, 1);
            }
            return panel;
        }

        private Panel CreateButtonPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            _exportButton = new Button { Text = "Export Details (CSV)", Size = new Size(150, 30), Location = new Point(10, 10) };
            _exportButton.Click += ExportButton_Click;
            _closeButton = new Button { Text = "Close", Size = new Size(100, 30), Location = new Point(170, 10) };
            _closeButton.Click += CloseButton_Click;
            // _darkModeButton = new Button { Text = "Toggle Dark Mode", Size = new Size(150, 30), Location = new Point(280, 10) };
            // _darkModeButton.Click += (s, e) => ToggleDarkMode();
            panel.Controls.AddRange(new Control[] { _exportButton, _closeButton /*, _darkModeButton */ });
            return panel;
        }

        private void CloseButton_Click(object sender, EventArgs e) { this.Close(); }

        private void CalculateExpenseTotalsWithMarkup() // This method's primary impact isn't on _project totals shown in summary.
        {
            if (_project == null || _project.Resources == null) return;
            foreach (var resource in _project.Resources)
            {
                Helpers.ExpenseCalculator.CalculateResourceExpenses(resource);
                // ... rest of your original logic for summing actuals if needed for other purposes
            }
        }

        private void RefreshData() 
        {
            if (_project == null || _isClosing) return;
            try
            {
                EnsureResourcesInitialized(); 

                foreach (var resource in _project.Resources)
                {
                    // Debug output: print all ResourceDayData keys and dates
                    System.Diagnostics.Debug.WriteLine($"Resource: {resource.TechnicianName}");
                    if (resource.DailyData != null)
                    {
                        foreach (var kvp in resource.DailyData.OrderBy(kvp => kvp.Value.Date))
                        {
                            var dayData = kvp.Value;
                            System.Diagnostics.Debug.WriteLine($"  Key: {kvp.Key}, Date: {dayData.Date:yyyy-MM-dd ddd}, Type: {dayData.ManualDayType?.ToString() ?? "Auto"}, PlannedLabour: {dayData.PlannedRegularLabourHours}, PlannedTravel: {dayData.PlannedRegularTravelHours + dayData.PlannedOvertimeTravelHours + dayData.PlannedPremiumTravelHours}");
                        }
                    }
                    Helpers.ExpenseCalculator.CalculateResourceExpenses(resource);
                    resource.CalculateResourceTotals(); // Calculates aggregate totals on resource object
                }
                _project.CalculateTotals(); // Calculates overall project aggregates
                UpdateSummaryPanel();

                _mainPanel.SuspendLayout();
                _contentHostPanel.SuspendLayout();
                _contentHostPanel.Controls.Clear();
                _resourcePanels.Clear();
                DateTime projectMinDate = DateTime.MaxValue;
                DateTime projectMaxDate = DateTime.MinValue;
                bool datesFound = false;
                if (_project?.Resources != null)
                {
                    foreach (var resource in _project.Resources)
                    {
                        if (resource.DailyData != null)
                        {
                            foreach (var dayDataKvp in resource.DailyData)
                            {
                                if (dayDataKvp.Value != null && dayDataKvp.Value.Date != DateTime.MinValue)
                                {
                                    if (dayDataKvp.Value.Date < projectMinDate) projectMinDate = dayDataKvp.Value.Date;
                                    if (dayDataKvp.Value.Date > projectMaxDate) projectMaxDate = dayDataKvp.Value.Date;
                                    datesFound = true;
                                }
                            }
                        }
                    }
                }

                List<DateTime> projectDisplayDates = new List<DateTime>();
                if (datesFound)
                {
                    for (DateTime date = projectMinDate; date <= projectMaxDate; date = date.AddDays(1))
                    {
                        projectDisplayDates.Add(date.Date); // Store only the Date part
                    }
                }
                // If projectDisplayDates is empty, ResourcePanel constructor handles it.

                foreach (var resource in _project.Resources)
                {
                    var resourcePanel = new ResourcePanel(resource, this, projectDisplayDates); // Pass common dates
                    resourcePanel.Margin = new Padding(0, 0, 0, 10);
                    _contentHostPanel.Controls.Add(resourcePanel);
                    _resourcePanels.Add(resourcePanel);
                }
                _contentHostPanel.ResumeLayout(false);
                _mainPanel.ResumeLayout(false);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("RefreshData error: " + ex.Message); }
        }

        private void EnsureResourcesInitialized()
        {
            if (_project?.Resources == null) return;
            foreach (var resource in _project.Resources)
            {
                if (resource.DailyData == null || !resource.DailyData.Any() || resource.IsDirty)
                {
                    resource.InitializeFromSchedule();
                }
            }
        }

        private void UpdateSummaryPanel()
        {
            if (_project == null || _summaryPanel == null) return;

            try
            {
                if (_summaryPanel.Controls["lblQuoted"] is Label lblQuoted)
                {
                    lblQuoted.Text = FormatCurrency(_project.InitialEstimate);
                }
                if (_summaryPanel.Controls["lblPlanned"] is Label lblPlanned)
                {
                    lblPlanned.Text = FormatCurrency(_project.PlannedTotal);
                }
                if (_summaryPanel.Controls["lblForecast"] is Label lblForecast)
                {
                    // Forecast is always the sum of all actuals (CurrentTotal)
                    lblForecast.Text = FormatCurrency(_project.CurrentTotal);
                }
                if (_summaryPanel.Controls["lblDeltaPlanned"] is Label lblDeltaPlanned)
                {
                    decimal deltaPlanned = _project.CurrentTotal - _project.PlannedTotal;
                    lblDeltaPlanned.Text = FormatCurrency(deltaPlanned);
                    lblDeltaPlanned.ForeColor = deltaPlanned == 0 ? Color.Black : (deltaPlanned < 0 ? Color.Green : Color.Red);
                }
                if (_summaryPanel.Controls["lblDelta"] is Label lblDelta)
                {
                    decimal delta = _project.CurrentTotal - _project.InitialEstimate;
                    lblDelta.Text = FormatCurrency(delta);
                    lblDelta.ForeColor = delta == 0 ? Color.Black : (delta < 0 ? Color.Green : Color.Red);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateSummaryPanel error: " + ex.Message);
            }
        }

        private string FormatCurrency(decimal value) { return value.ToString("C2"); }

        internal void SaveData()
        {
            if (_isClosing) return;
            try
            {
                _project.CalculateTotals();
                CommissioningDataManager.Instance.SaveCurrentProject();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("SaveData error: " + ex.Message); }
        }

        public void NotifyResourceDataChanged() // Called by ResourcePanel after an edit
        {
            if (_project == null || _isClosing) return;
            try
            {
                _project.CalculateTotals(); // Recalculate project aggregates
                MarkProjectDirty();         // Sets IsDirty and Updates Summary Panel
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error in NotifyResourceDataChanged: " + ex.Message); }
        }

        internal void MarkProjectDirty()
        {
            if (_project != null)
            {
                _project.IsDirty = true;
                UpdateSummaryPanel();
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = $"{_project.ProjectName}_Results_{DateTime.Now:yyyyMMdd}.csv" })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK) ExportToCSV(saveDialog.FileName);
            }
        }

        private void ExportToCSV(string fileName)
        {
            try
            {
                var sb = new StringBuilder("Resource,Date,Day,Service Reg Hours,Service OT Hours,Service Premium Hours,Travel Reg Hours,Travel OT Hours,Travel Premium Hours,Mileage,Per Diem,Flight,Car Rental,Hotel,Total\r\n");
                foreach (var rp in _resourcePanels) rp.AppendCSVData(sb);
                File.WriteAllText(fileName, sb.ToString());
                MessageBox.Show("Export completed successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error exporting data: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isClosing = true; SaveData(); base.OnFormClosing(e);
        }

        public void UpdateResults(CommissioningProject project)
        {
            _project = project; RefreshData();
        }

        
        internal class ResourcePanel : Panel
        {
            private CommissioningResource _resource;
            private CommissioningResultsWindow _parentWindow;
            internal TableLayoutPanel _gridPanel;
            private Label _headerLabel, _totalLabel;
            private bool _isExpanded = true;
            private Button _toggleButton;

            private Dictionary<string, EditableCell> _editableCells;
            private Dictionary<string, Label> _plannedLabels;
            private Dictionary<string, Label> _deltaLabels;
            private Dictionary<string, Label> _subtotalAndTotalLabels;

            private List<DateTime> _overallDisplayDates;

            private TableLayoutPanel _minimizedGridPanel;
            private int _expandedWidth;

            // Helper struct for delta tagging
            private struct DeltaTag { public string Type; public decimal Value; public DeltaTag(string t, decimal v) { Type = t; Value = v; } }

            // --- Helper methods for data retrieval and calculation ---
            private decimal GetRegularLabourRate() { decimal r = _resource.RegularLabourRate, d = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumLabourRate * d : r * d; }
            private decimal GetOvertimeLabourRate() { decimal r = _resource.OvertimeLabourRate, d = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumLabourRate * d : r * d; }
            private decimal GetPremiumLabourRate() { return _resource.PremiumLabourRate * (1 - (_resource.DiscountPercent / 100m)); }
            private decimal GetRegularTravelRate() { decimal r = _resource.RegularTravelRate, d = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumTravelRate * d : r * d; }
            private decimal GetOvertimeTravelRate() { decimal r = _resource.OvertimeTravelRate, d = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumTravelRate * d : r * d; }
            private decimal GetPremiumTravelRate() { return _resource.PremiumTravelRate * (1 - (_resource.DiscountPercent / 100m)); }

            private decimal CalculateSubtotal(ResourceDayData dD, bool isCharges, bool isPlanned)
            {
                if (dD == null) return 0;
                if (isCharges) // Labour and Travel Charges
                {
                    return isPlanned ?
                        (dD.PlannedRegularLabourHours * GetRegularLabourRate()) +
                        (dD.PlannedOvertimeLabourHours * GetOvertimeLabourRate()) +
                        (dD.PlannedPremiumLabourHours * GetPremiumLabourRate()) +
                        (dD.PlannedRegularTravelHours * GetRegularTravelRate()) +
                        (dD.PlannedOvertimeTravelHours * GetOvertimeTravelRate()) +
                        (dD.PlannedPremiumTravelHours * GetPremiumTravelRate()) :
                        (dD.ActualRegularLabourHours * GetRegularLabourRate()) +
                        (dD.ActualOvertimeLabourHours * GetOvertimeLabourRate()) +
                        (dD.ActualPremiumLabourHours * GetPremiumLabourRate()) +
                        (dD.ActualRegularTravelHours * GetRegularTravelRate()) +
                        (dD.ActualOvertimeTravelHours * GetOvertimeTravelRate()) +
                        (dD.ActualPremiumTravelHours * GetPremiumTravelRate());
                }
                else // Expenses
                {
                    return isPlanned ?
                        dD.PlannedMileageCost + dD.PlannedPerDiemCost + dD.PlannedFlightCost +
                        dD.PlannedRentalCarCost + dD.PlannedHotelCost :
                        dD.ActualMileageCost + dD.ActualPerDiemCost + dD.ActualFlightCost +
                        dD.ActualRentalCarCost + dD.ActualHotelCost;
                }
            }

            private decimal CalculateDayTotal(ResourceDayData dD, bool isPlanned)
            {
                if (dD == null) return 0;
                return CalculateSubtotal(dD, true, isPlanned) + CalculateSubtotal(dD, false, isPlanned);
            }

            internal void UpdateTotalLabel()
            {
                decimal totalActualCost = 0;
                if (_resource?.DailyData != null)
                {
                    foreach (var dayDataEntry in _resource.DailyData.Values)
                    {
                        if (dayDataEntry != null)
                        {
                            totalActualCost += CalculateDayTotal(dayDataEntry, false); // Sum of actual daily totals
                        }
                    }
                }
                _totalLabel.Text = $"Total: {totalActualCost:C2}";
            }





            public ResourcePanel(CommissioningResource resource, CommissioningResultsWindow parentWindow, List<DateTime> overallDisplayDates)
            {
                this.DoubleBuffered = true;
                _resource = resource;
                _parentWindow = parentWindow;
                _overallDisplayDates = overallDisplayDates ?? new List<DateTime>(); // Store the common dates

                _editableCells = new Dictionary<string, EditableCell>();
                _plannedLabels = new Dictionary<string, Label>();
                _deltaLabels = new Dictionary<string, Label>();
                _subtotalAndTotalLabels = new Dictionary<string, Label>();

                this.BorderStyle = BorderStyle.FixedSingle;
                // Current MinimumSize is 1100,350. Consider if this needs adjustment based on _overallDisplayDates.
                // For now, let AutoSize manage it.
                this.MinimumSize = new Size(600, 200); // Adjusted minimum, can be tweaked.
                this.AutoSize = true;
                this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Initialize();
            }

            private void Initialize()
            {
                CreateHeader();
                CreateGrid();
                PopulateData(); // Builds grid and stores control references
            }

            private void CreateHeader()
            {
                var headerPanel = new Panel { Height = 30, Dock = DockStyle.Top, BackColor = SystemColors.ActiveCaption };
                _toggleButton = new Button { Text = "−", Size = new Size(25, 25), Location = new Point(5, 2) };
                _toggleButton.Click += ToggleButton_Click;
                _headerLabel = new Label { Location = new Point(35, 5), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold), ForeColor = SystemColors.ActiveCaptionText };
                _totalLabel = new Label { AutoSize = true, Font = new Font(this.Font, FontStyle.Bold), ForeColor = SystemColors.ActiveCaptionText, Anchor = AnchorStyles.Top | AnchorStyles.Right };
                headerPanel.SizeChanged += (s, e) => { if (_totalLabel.Parent == headerPanel) _totalLabel.Location = new Point(headerPanel.Width - _totalLabel.Width - 10, 5); };
                headerPanel.Controls.AddRange(new Control[] { _toggleButton, _headerLabel, _totalLabel });
                this.Controls.Add(headerPanel);
                UpdateHeaderText(0); // Call with 0 initially, PopulateData will update it.
            }

            private void UpdateHeaderText(int displayDaysCount) // Modified signature
            {
                // int displayDaysCount = _resource.DailyData?.Count(kvp => kvp.Value.Date != DateTime.MinValue) ?? 0; // Old way
                _headerLabel.Text = $"Resource: {_resource.TechnicianName} ({displayDaysCount} display days)";
            }
            private void CreateGrid()
            {
                _gridPanel = new TableLayoutPanel { Location = new Point(0, 30), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single };
                this.Controls.Add(_gridPanel);
            }

            private void ToggleButton_Click(object sender, EventArgs e)
            {
                _isExpanded = !_isExpanded;
                _toggleButton.Text = _isExpanded ? "−" : "+";
                _gridPanel.Visible = _isExpanded;
                if (!_isExpanded) { this.AutoSize = false; this.Height = 30; } else { this.AutoSize = true; }
                this.Parent?.PerformLayout();
            }

            private void PopulateData()
            {
                if (_resource == null) 
                { 
                    UpdateTotalLabel(); 
                    return; 
                }
                // Old approach: build the grid directly
                _gridPanel.SuspendLayout();
                _gridPanel.Controls.Clear();
                SetupGridStructure(_overallDisplayDates.Count);
                AddHeaders(_overallDisplayDates);
                int row = 2;
                AddDataRow(row++, "Regular Labour", _overallDisplayDates, "ServiceReg");
                AddDataRow(row++, "Overtime Labour", _overallDisplayDates, "ServiceOT");
                AddDataRow(row++, "Premium Labour", _overallDisplayDates, "ServicePrem");
                AddHourSubtotalRow(row++, "Subtotal Hours - Labour", _overallDisplayDates, new[]{"ServiceReg","ServiceOT","ServicePrem"});
                AddDataRow(row++, "Regular Travel", _overallDisplayDates, "TravelReg");
                AddDataRow(row++, "Overtime Travel", _overallDisplayDates, "TravelOT");
                AddDataRow(row++, "Premium Travel", _overallDisplayDates, "TravelPrem");
                AddHourSubtotalRow(row++, "Subtotal Hours - Travel", _overallDisplayDates, new[]{"TravelReg","TravelOT","TravelPrem"});
                AddSubtotalRow(row++, "Subtotals - Charges", _overallDisplayDates, true);
                AddSubtotalRow(row++, "Subtotals - Expenses", _overallDisplayDates, false);
                AddDataRow(row++, "Mileage", _overallDisplayDates, "Mileage");
                AddDataRow(row++, "Per Diem", _overallDisplayDates, "PerDiem");
                AddDataRow(row++, "Flight", _overallDisplayDates, "Flight");
                AddDataRow(row++, "Car Rental", _overallDisplayDates, "CarRental");
                AddDataRow(row++, "Hotel", _overallDisplayDates, "Hotel");
                AddTotalsRow(row++, "Totals", _overallDisplayDates);
                _gridPanel.ResumeLayout(true);
                UpdateTotalLabel();
                UpdateHeaderText(_overallDisplayDates?.Count ?? 0);
            }

            private void SetupGridStructure(int dayCount)
            {
                _gridPanel.RowCount = 16; _gridPanel.ColumnCount = 1 + (Math.Max(0, dayCount) * 3);
                _gridPanel.ColumnStyles.Clear(); _gridPanel.RowStyles.Clear();
                if (_gridPanel.ColumnCount > 0) _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                for (int i = 0; i < dayCount; i++) { _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); }
                for (int i = 0; i < _gridPanel.RowCount; i++) _gridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            }

            // Centralized color logic for cell types
            private (Color back, Color fore) GetCellColors(string cellType, bool isDarkMode, decimal? deltaValue = null)
            {
                Color darkHeader = _parentWindow.darkGridHeaderBackColor;
                Color darkCell = _parentWindow.darkGridCellBackColor;
                Color darkText = _parentWindow.darkTextColor;
                Color lightHeader = SystemColors.ControlLight;
                Color lightCell = Color.White;
                Color lightText = Color.Black;
                Color subtotalBack = Color.Yellow;
                Color subtotalFore = Color.Black;
                Color plannedHeader = isDarkMode ? Color.FromArgb(60, 90, 120) : Color.FromArgb(235, 245, 255);
                Color actualHeader = isDarkMode ? Color.FromArgb(60, 120, 80) : Color.FromArgb(235, 255, 235);
                Color deltaHeader = isDarkMode ? Color.FromArgb(120, 120, 60) : Color.FromArgb(255, 255, 235);
                switch (cellType)
                {
                    case "DayHeader": return (isDarkMode ? darkHeader : lightHeader, lightText);
                    case "ColumnHeaderPlanned": return (plannedHeader, lightText);
                    case "ColumnHeaderActual": return (actualHeader, lightText);
                    case "ColumnHeaderDelta": return (deltaHeader, lightText);
                    case "Subtotal": return (subtotalBack, subtotalFore);
                    case "Total": return (subtotalBack, subtotalFore);
                    case "Delta":
                        if (deltaValue.HasValue)
                            return (Color.Transparent, deltaValue == 0 ? Color.Black : (deltaValue > 0 ? Color.Red : Color.Green));
                        else
                            return (Color.Transparent, isDarkMode ? darkText : lightText);
                    case "Editable": return (isDarkMode ? darkCell : lightCell, isDarkMode ? darkText : lightText);
                    default: return (isDarkMode ? darkCell : lightCell, isDarkMode ? darkText : lightText);
                }
            }

            // Apply styles to all grid cells based on their logical type
            internal void ApplyCellStyles(TableLayoutPanel grid)
            {
                // Only set yellow background for subtotal/total Delta cells in UpdateSubtotalOrTotalRowLabels_InPlace
                foreach (Control ctrl in grid.Controls)
                {
                    // Skip if this is a subtotal/total Delta cell
                    if (ctrl is Label label && _subtotalAndTotalLabels.Values.Contains(label) && _subtotalAndTotalLabels.Any(kvp => kvp.Value == label && kvp.Key.EndsWith("_D")))
                        continue;
                    if (ctrl.Tag is DeltaTag deltaTag && deltaTag.Type == "Delta")
                    {
                        // For non-subtotal/total delta cells
                        ctrl.BackColor = Color.Transparent;
                        if (ctrl is Label lbl2)
                        {
                            if (decimal.TryParse(lbl2.Text.Replace("$", "").Replace(",", ""), out decimal val))
                            {
                                lbl2.ForeColor = val == 0 ? Color.Black : (val > 0 ? Color.Red : Color.Green);
                            }
                            else
                            {
                                lbl2.ForeColor = _parentWindow._isDarkMode ? _parentWindow.darkTextColor : Color.Black;
                            }
                        }
                    }
                    else if (ctrl.Tag is string cellType)
                    {
                        var (back, fore) = GetCellColors(cellType, _parentWindow._isDarkMode);
                        ctrl.BackColor = back;
                        ctrl.ForeColor = fore;
                    }
                    else if (ctrl is EditableCell ec)
                    {
                        ec.ApplyTheme(_parentWindow._isDarkMode, GetCellColors("Editable", _parentWindow._isDarkMode));
                    }
                }
            }

            // Update AddHeaders to assign logical types
            private void AddHeaders(List<DateTime> displayDates)
            {
                if (_gridPanel.ColumnCount == 0 || !displayDates.Any()) return;
                var empty = CreateLabel("", true); empty.Tag = "DayHeader";
                AddControlToGrid(empty, 0, 0); // Top-left empty cell
                int currentGridColumn = 1;
                foreach (DateTime displayDate in displayDates)
                {
                    if (currentGridColumn + 2 >= _gridPanel.ColumnCount) break; // Ensure space for 3 cells per date
                    var dateHeaderLabel = CreateLabel(displayDate.ToString("ddd MMM dd"), true); dateHeaderLabel.Tag = "DayHeader";
                    dateHeaderLabel.BackColor = Color.FromArgb(230, 230, 250);
                    dateHeaderLabel.BorderStyle = BorderStyle.FixedSingle;
                    dateHeaderLabel.TextAlign = ContentAlignment.MiddleCenter;
                    AddControlToGrid(dateHeaderLabel, currentGridColumn, 0);
                    _gridPanel.SetColumnSpan(dateHeaderLabel, 3);

                    var planned = CreateLabel("Planned", true); planned.Tag = "ColumnHeaderPlanned";
                    planned.TextAlign = ContentAlignment.MiddleCenter;
                    var actual = CreateLabel("Actual", true); actual.Tag = "ColumnHeaderActual";
                    actual.TextAlign = ContentAlignment.MiddleCenter;
                    var delta = CreateLabel("Delta", true); delta.Tag = "ColumnHeaderDelta";
                    delta.TextAlign = ContentAlignment.MiddleCenter;
                    AddControlToGrid(planned, currentGridColumn, 1);
                    AddControlToGrid(actual, currentGridColumn + 1, 1);
                    AddControlToGrid(delta, currentGridColumn + 2, 1);
                    currentGridColumn += 3;
                }
            }

            // Update AddDataRow, AddSubtotalRow, AddTotalsRow to assign logical types
            private void AddDataRow(int gridRow, string rowLabelText, List<DateTime> displayDates, string dataType)
            {
                var headerLabel = CreateLabel(rowLabelText, false); headerLabel.Tag = "RowHeader";
                headerLabel.TextAlign = ContentAlignment.MiddleLeft;
                headerLabel.Padding = new Padding(5, 0, 0, 0);
                AddControlToGrid(headerLabel, 0, gridRow);
                int currentGridColumn = 1;
                foreach (var displayDate in displayDates)
                {
                    if (currentGridColumn + 2 >= _gridPanel.ColumnCount) break;
                    ResourceDayData dayData = _resource.DailyData.Values.FirstOrDefault(rd => rd != null && rd.Date.Date == displayDate.Date);
                    int dayKey = -1;
                    if (dayData != null)
                    {
                        var kvp = _resource.DailyData.FirstOrDefault(entry => entry.Value == dayData);
                        dayKey = kvp.Value != null ? kvp.Key : -1;
                    }
                    // Debug output for travel rows
                    if (dataType == "TravelReg" || dataType == "TravelOT" || dataType == "TravelPrem")
                    {
                        System.Diagnostics.Debug.WriteLine($"[ResultsGrid] {displayDate:yyyy-MM-dd ddd} Travel: Reg={dayData?.PlannedRegularTravelHours}, OT={dayData?.PlannedOvertimeTravelHours}, Prem={dayData?.PlannedPremiumTravelHours}");
                    }
                    if (dayData != null)
                    {
                        decimal plannedValue = ResultsGridCalculationHelper.GetPlannedValue(dayData, dataType);
                        decimal actualValue = ResultsGridCalculationHelper.GetActualValue(dayData, dataType);
                        decimal deltaValue = actualValue - plannedValue;
                        var plannedLabel = CreateLabel(ResultsGridCalculationHelper.FormatValue(plannedValue, dataType), false); plannedLabel.Tag = "Normal";
                        if (ResultsGridCalculationHelper.IsHoursType(dataType)) plannedLabel.Text = plannedValue.ToString("F1");
                        else plannedLabel.Text = plannedValue.ToString("C2");
                        AddControlToGrid(plannedLabel, currentGridColumn, gridRow);
                        if (dayKey != -1) _plannedLabels[$"P_{dataType}_{dayKey}"] = plannedLabel;
                        AddEditableCell(gridRow, currentGridColumn + 1, actualValue, dataType, dayKey, dayData);
                        var deltaLabel = CreateLabel(ResultsGridCalculationHelper.FormatValue(deltaValue, dataType), false); deltaLabel.Tag = new DeltaTag("Delta", deltaValue);
                        deltaLabel.BackColor = Color.Transparent;
                        SetDeltaLabelColor(deltaLabel, deltaValue);
                        AddControlToGrid(deltaLabel, currentGridColumn + 2, gridRow);
                        if (dayKey != -1) _deltaLabels[$"D_{dataType}_{dayKey}"] = deltaLabel;
                    }
                    else
                    {
                        // No ResourceDayData: blank cells
                        var empty1 = CreateLabel(string.Empty, false); empty1.Tag = "Normal";
                        var empty2 = CreateLabel(string.Empty, false); empty2.Tag = "Normal";
                        var empty3 = CreateLabel(string.Empty, false); empty3.Tag = "Normal";
                        AddControlToGrid(empty1, currentGridColumn, gridRow);
                        AddControlToGrid(empty2, currentGridColumn + 1, gridRow);
                        AddControlToGrid(empty3, currentGridColumn + 2, gridRow);
                    }
                    currentGridColumn += 3;
                }
            }
            private void AddSubtotalRow(int gridRow, string labelText, List<DateTime> displayDates, bool isChargesSubtotal)
            {
                var rowHeaderLabel = CreateLabel(labelText, true); rowHeaderLabel.Tag = "RowHeader";
                rowHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
                rowHeaderLabel.Padding = new Padding(5, 0, 0, 0);
                AddControlToGrid(rowHeaderLabel, 0, gridRow);
                int currentGridColumn = 1;
                foreach (var displayDate in displayDates)
                {
                    if (currentGridColumn + 2 >= _gridPanel.ColumnCount) break;
                    ResourceDayData dayData = _resource.DailyData.Values.FirstOrDefault(rd => rd != null && rd.Date.Date == displayDate.Date);
                    int dayKey = -1;
                    if (dayData != null)
                    {
                        var kvp = _resource.DailyData.FirstOrDefault(entry => entry.Value == dayData);
                        dayKey = kvp.Value != null ? kvp.Key : -1;
                    }
                    decimal plannedSubtotal = 0, actualSubtotal = 0, deltaSubtotal = 0;
                    if (dayData != null)
                    {
                        plannedSubtotal = isChargesSubtotal
                            ? ResultsGridCalculationHelper.CalculateSubtotal(dayData, _resource, true, true)
                            : ResultsGridCalculationHelper.CalculateSubtotal(dayData, _resource, false, true);
                        actualSubtotal = isChargesSubtotal
                            ? ResultsGridCalculationHelper.CalculateSubtotal(dayData, _resource, true, false)
                            : ResultsGridCalculationHelper.CalculateSubtotal(dayData, _resource, false, false);
                        deltaSubtotal = actualSubtotal - plannedSubtotal;
                        string cleanLabelText = labelText.Replace(" ", "").Replace("-", "");
                        string baseKey = $"SUB_{cleanLabelText}_{dayKey}";
                        var pL = CreateLabel(ResultsGridCalculationHelper.FormatValue(plannedSubtotal, labelText), true); pL.Tag = "Subtotal"; pL.BackColor = Color.Yellow;
                        AddControlToGrid(pL, currentGridColumn, gridRow);
                        if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_P"] = pL;
                        var aL = CreateLabel(ResultsGridCalculationHelper.FormatValue(actualSubtotal, labelText), true); aL.Tag = "Subtotal"; aL.BackColor = Color.Yellow;
                        AddControlToGrid(aL, currentGridColumn + 1, gridRow);
                        if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_A"] = aL;
                        var dL = CreateLabel(ResultsGridCalculationHelper.FormatValue(deltaSubtotal, labelText), true); dL.Tag = new DeltaTag("Delta", deltaSubtotal); dL.BackColor = Color.Yellow;
                        SetDeltaLabelColor(dL, deltaSubtotal);
                        AddControlToGrid(dL, currentGridColumn + 2, gridRow);
                        if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_D"] = dL;
                    }
                    else
                    {
                        // No ResourceDayData: blank cells
                        var empty1 = CreateLabel(string.Empty, true); empty1.Tag = "Subtotal"; empty1.BackColor = Color.Yellow;
                        var empty2 = CreateLabel(string.Empty, true); empty2.Tag = "Subtotal"; empty2.BackColor = Color.Yellow;
                        var empty3 = CreateLabel(string.Empty, true); empty3.Tag = new DeltaTag("Delta", 0); empty3.BackColor = Color.Yellow;
                        AddControlToGrid(empty1, currentGridColumn, gridRow);
                        AddControlToGrid(empty2, currentGridColumn + 1, gridRow);
                        AddControlToGrid(empty3, currentGridColumn + 2, gridRow);
                    }
                    currentGridColumn += 3;
                }
            }
            private void AddTotalsRow(int gridRow, string labelText, List<DateTime> displayDates)
            {
                var rowHeaderLabel = CreateLabel(labelText, true); rowHeaderLabel.Tag = "RowHeader";
                rowHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
                rowHeaderLabel.Padding = new Padding(5, 0, 0, 0);
                AddControlToGrid(rowHeaderLabel, 0, gridRow);
                int currentGridColumn = 1;
                foreach (var displayDate in displayDates)
                {
                    if (currentGridColumn + 2 >= _gridPanel.ColumnCount) break;
                    ResourceDayData dayData = _resource.DailyData.Values.FirstOrDefault(rd => rd != null && rd.Date.Date == displayDate.Date);
                    int dayKey = -1;
                    if (dayData != null)
                    {
                        var kvp = _resource.DailyData.FirstOrDefault(entry => entry.Value == dayData);
                        dayKey = kvp.Value != null ? kvp.Key : -1;
                    }
                    decimal plannedTotal = 0, actualTotal = 0, deltaTotal = 0;
                    if (dayData != null)
                    {
                        plannedTotal = ResultsGridCalculationHelper.CalculateDayTotal(dayData, _resource, true);
                        actualTotal = ResultsGridCalculationHelper.CalculateDayTotal(dayData, _resource, false);
                        deltaTotal = actualTotal - plannedTotal;
                        string baseKey = $"TOTAL_Day_{dayKey}";
                        var pL = CreateLabel(ResultsGridCalculationHelper.FormatValue(plannedTotal, labelText), true); pL.Tag = "Total"; pL.BackColor = Color.Yellow;
                        AddControlToGrid(pL, currentGridColumn, gridRow);
                        if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_P"] = pL;
                        var aL = CreateLabel(ResultsGridCalculationHelper.FormatValue(actualTotal, labelText), true); aL.Tag = "Total"; aL.BackColor = Color.Yellow;
                        AddControlToGrid(aL, currentGridColumn + 1, gridRow);
                        if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_A"] = aL;
                        var dL = CreateLabel(ResultsGridCalculationHelper.FormatValue(deltaTotal, labelText), true); dL.Tag = new DeltaTag("Delta", deltaTotal); dL.BackColor = Color.Yellow;
                        SetDeltaLabelColor(dL, deltaTotal);
                        AddControlToGrid(dL, currentGridColumn + 2, gridRow);
                        if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_D"] = dL;
                    }
                    else
                    {
                        // No ResourceDayData: blank cells
                        var empty1 = CreateLabel(string.Empty, true); empty1.Tag = "Total"; empty1.BackColor = Color.Yellow;
                        var empty2 = CreateLabel(string.Empty, true); empty2.Tag = "Total"; empty2.BackColor = Color.Yellow;
                        var empty3 = CreateLabel(string.Empty, true); empty3.Tag = new DeltaTag("Delta", 0); empty3.BackColor = Color.Yellow;
                        AddControlToGrid(empty1, currentGridColumn, gridRow);
                        AddControlToGrid(empty2, currentGridColumn + 1, gridRow);
                        AddControlToGrid(empty3, currentGridColumn + 2, gridRow);
                    }
                    currentGridColumn += 3;
                }
            }
            private void AddEditableCell(int gridRow, int gridCol, decimal value, string dataType, int dayKey, ResourceDayData dayData, Color? backColor = null)
            {
                var eC = new EditableCell(value, dataType, dayKey, dayData, _resource, this) { Dock = DockStyle.Fill, Tag = "Editable" };
                _editableCells[$"A_{dataType}_{dayKey}"] = eC; // Store reference to the EditableCell
                AddControlToGrid(eC, gridCol, gridRow); // Add to grid
            }

            public void RefreshGridDisplay()
            {
                PopulateData();
            }

            // Modify UpdateSubtotalOrTotalRowLabels_InPlace to accept List<DateTime>
            private void UpdateSubtotalOrTotalRowLabels_InPlace(List<DateTime> displayDates, string baseLabelTextForType, bool? isChargesForSubtotal)
            {
                foreach (var displayDate in displayDates)
                {
                    ResourceDayData dD = _resource.DailyData.Values.FirstOrDefault(rd => rd != null && rd.Date.Date == displayDate.Date);
                    int dK = -1;
                    if (dD != null)
                    {
                        var kvp = _resource.DailyData.FirstOrDefault(entry => entry.Value == dD);
                        dK = kvp.Value != null ? kvp.Key : -1;
                    }

                    if (dD == null || dK == -1) continue; // Skip if no data for this resource on this date

                    decimal pV, aV, dV;
                    string keyPrefix;

                    if (isChargesForSubtotal.HasValue) // It's a subtotal row
                    {
                        pV = ResultsGridCalculationHelper.CalculateSubtotal(dD, _resource, true, true);
                        aV = ResultsGridCalculationHelper.CalculateSubtotal(dD, _resource, true, false);
                        string cleanLabelText = baseLabelTextForType.Replace(" ", "").Replace("-", "");
                        keyPrefix = $"SUB_{cleanLabelText}_{dK}";
                    }
                    else // It's the main "Totals" row
                    {
                        pV = CalculateDayTotal(dD, true);
                        aV = CalculateDayTotal(dD, false);
                        keyPrefix = $"TOTAL_Day_{dK}";
                    }
                    dV = aV - pV;

                    if (_subtotalAndTotalLabels.TryGetValue($"{keyPrefix}_P", out Label pL)) { pL.Text = pV.ToString("C2"); pL.BackColor = Color.Yellow; }
                    if (_subtotalAndTotalLabels.TryGetValue($"{keyPrefix}_A", out Label aL)) { aL.Text = aV.ToString("C2"); aL.BackColor = Color.Yellow; }
                    if (_subtotalAndTotalLabels.TryGetValue($"{keyPrefix}_D", out Label dL)) { dL.Text = dV.ToString("C2"); dL.BackColor = Color.Yellow; SetDeltaLabelColor(dL, dV); }
                }
            }

            // Add this method to update all regular data row Delta cells
            private void UpdateAllRegularDeltaCells()
            {
                foreach (var kvp in _deltaLabels)
                {
                    // Skip subtotal/total delta cells (those in _subtotalAndTotalLabels)
                    if (_subtotalAndTotalLabels.Values.Contains(kvp.Value))
                        continue;
                    // Key format: D_{dataType}_{dayKey}
                    var keyParts = kvp.Key.Split('_');
                    if (keyParts.Length < 3) continue;
                    string dataType = keyParts[1];
                    if (!int.TryParse(keyParts[2], out int dayKey)) continue;
                    if (!_resource.DailyData.TryGetValue(dayKey, out var dayData) || dayData == null) continue;
                    decimal planned = ResultsGridCalculationHelper.GetPlannedValue(dayData, dataType);
                    decimal actual = ResultsGridCalculationHelper.GetActualValue(dayData, dataType);
                    decimal delta = actual - planned;
                    kvp.Value.Text = ResultsGridCalculationHelper.IsHoursType(dataType) ? delta.ToString("F1") : delta.ToString("C2");
                    SetDeltaLabelColor(kvp.Value, delta);
                }
            }

            internal void NotifyValueChanged(string dataType = null, int dayKey = -1)
            {
                _resource.CalculateResourceTotals();
                // 1. Update the edited Actual cell (handled by EditableCell itself)
                // 2. Update the corresponding Delta cell
                if (!string.IsNullOrEmpty(dataType) && dayKey != -1)
                {
                    UpdateDeltaLabel(dataType, dayKey);
                }
                // 3. Update affected subtotals and totals for the day
                if (dayKey != -1)
                {
                    // Charges subtotal
                    UpdateSubtotalOrTotalRowLabels_InPlace(_overallDisplayDates, "Subtotals - Charges", true);
                    // Expenses subtotal
                    UpdateSubtotalOrTotalRowLabels_InPlace(_overallDisplayDates, "Subtotals - Expenses", false);
                    // Hour subtotals
                    UpdateSubtotalOrTotalRowLabels_InPlace(_overallDisplayDates, "Subtotal Hours - Labour", null);
                    UpdateSubtotalOrTotalRowLabels_InPlace(_overallDisplayDates, "Subtotal Hours - Travel", null);
                    // Totals row
                    UpdateSubtotalOrTotalRowLabels_InPlace(_overallDisplayDates, "Totals", null);
                }
                // 4. Update Per Diem if affected
                if (dataType == "ServiceReg" || dataType == "ServiceOT" || dataType == "ServicePrem" || dataType == "TravelReg" || dataType == "TravelOT" || dataType == "TravelPrem")
                {
                    // Per Diem is calculated from total hours, so update its Actual and Delta
                    if (dayKey != -1)
                    {
                        UpdateDeltaLabel("PerDiem", dayKey);
                    }
                }
                // 5. Update summary panel
                _parentWindow.NotifyResourceDataChanged();
                UpdateTotalLabel();
            }

            internal void AppendCSVData(StringBuilder sb) { if (_resource.DailyData == null || !_resource.DailyData.Any()) return; var oD = _resource.DailyData.Where(kvp => kvp.Value.Date != DateTime.MinValue).OrderBy(kvp => kvp.Value.Date).ToList(); Helpers.ExpenseCalculator.CalculateResourceExpenses(_resource); foreach (var dE in oD) { var dD = dE.Value; decimal lC = (dD.ActualRegularLabourHours * GetRegularLabourRate()) + (dD.ActualOvertimeLabourHours * GetOvertimeLabourRate()) + (dD.ActualPremiumLabourHours * GetPremiumLabourRate()); decimal tC = (dD.ActualRegularTravelHours * GetRegularTravelRate()) + (dD.ActualOvertimeTravelHours * GetOvertimeTravelRate()) + (dD.ActualPremiumTravelHours * GetPremiumTravelRate()); decimal dayTotal = lC + tC + dD.ActualMileageCost + dD.ActualPerDiemCost + dD.ActualFlightCost + dD.ActualRentalCarCost + dD.ActualHotelCost; sb.AppendLine($"\"{EscapeCSV(_resource.TechnicianName)}\",{dD.Date:MM/dd/yyyy},{dD.Date:dddd},{dD.ActualRegularLabourHours},{dD.ActualOvertimeLabourHours},{dD.ActualPremiumLabourHours},{dD.ActualRegularTravelHours},{dD.ActualOvertimeTravelHours},{dD.ActualPremiumTravelHours},{dD.ActualMileageCost},{dD.ActualPerDiemCost},{dD.ActualFlightCost},{dD.ActualRentalCarCost},{dD.ActualHotelCost},{dayTotal}"); } }
            private string EscapeCSV(string v) { if (v == null) return ""; if (v.Contains(",") || v.Contains("\"") || v.Contains("\n")) return $"\"{v.Replace("\"", "\"\"")}\""; return v; }
            public void ReapplyGridSpecialFormatting()
            {
                // Reapply subtotal/total backgrounds and delta label colors
                foreach (var kvp in _subtotalAndTotalLabels)
                {
                    var key = kvp.Key;
                    var label = kvp.Value;
                    if (key.Contains("TOTAL") || key.Contains("SUB_"))
                    {
                        label.BackColor = Color.Yellow;
                        label.ForeColor = Color.Black;
                    }
                }
                foreach (var kvp in _deltaLabels)
                {
                    var label = kvp.Value;
                    // Try to parse the value to determine color
                    if (decimal.TryParse(label.Text.Replace("$", "").Replace(",", ""), out decimal delta))
                    {
                        if (delta == 0) label.ForeColor = Color.Black;
                        else if (delta > 0) label.ForeColor = Color.Red;
                        else label.ForeColor = Color.Green;
                    }
                }
            }

            // Utility: Add a control to the grid at (col, row)
            private void AddControlToGrid(Control ctrl, int col, int row)
            {
                _gridPanel.Controls.Add(ctrl, col, row);
            }

            // Utility: Create a label with optional bold font
            private Label CreateLabel(string text, bool bold)
            {
                return new Label
                {
                    Text = text,
                    TextAlign = ContentAlignment.MiddleRight,
                    Dock = DockStyle.Fill,
                    Font = bold ? new Font(this.Font, FontStyle.Bold) : this.Font,
                    AutoSize = false,
                    Margin = new Padding(0),
                    Padding = new Padding(0)
                };
            }

            // Utility: Format value for display
            private string FormatValue(decimal value, string dataType)
            {
                return ResultsGridCalculationHelper.IsHoursType(dataType) ? value.ToString("F1") : value.ToString("C2");
            }

            // Utility: Determine if a data type is hours
            internal bool IsHoursType(string dataType)
            {
                return ResultsGridCalculationHelper.IsHoursType(dataType);
            }

            // Utility: Set delta label color
            private void SetDeltaLabelColor(Label label, decimal delta)
            {
                if (delta == 0)
                    label.ForeColor = Color.Black;
                else if (delta > 0)
                    label.ForeColor = Color.Red;
                else
                    label.ForeColor = Color.Green;
            }

            // AddDataRowsAndTotals: Add all data rows and totals to the grid
            private void AddDataRowsAndTotals(List<DateTime> displayDates)
            {
                int row = 2;
                // Add hour subtotals for labour and travel
                AddDataRow(row++, "Regular Labour", displayDates, "ServiceReg");
                AddDataRow(row++, "Overtime Labour", displayDates, "ServiceOT");
                AddDataRow(row++, "Premium Labour", displayDates, "ServicePrem");
                // Add subtotal row for labour hours
                AddHourSubtotalRow(row++, "Subtotal Hours - Labour", displayDates, new[] { "ServiceReg", "ServiceOT", "ServicePrem" });
                AddSubtotalRow(row++, "Subtotals - Charges", displayDates, true);
                AddDataRow(row++, "Regular Travel", displayDates, "TravelReg");
                AddDataRow(row++, "Overtime Travel", displayDates, "TravelOT");
                AddDataRow(row++, "Premium Travel", displayDates, "TravelPrem");
                // Add subtotal row for travel hours
                AddHourSubtotalRow(row++, "Subtotal Hours - Travel", displayDates, new[] { "TravelReg", "TravelOT", "TravelPrem" });
                AddSubtotalRow(row++, "Subtotals - Expenses", displayDates, false);
                AddDataRow(row++, "Mileage", displayDates, "Mileage");
                AddDataRow(row++, "Per Diem", displayDates, "PerDiem");
                AddDataRow(row++, "Flight", displayDates, "Flight");
                AddDataRow(row++, "Car Rental", displayDates, "CarRental");
                AddDataRow(row++, "Hotel", displayDates, "Hotel");
                AddTotalsRow(row++, "Totals", displayDates);
            }

            // Add a subtotal row for hours (labour or travel)
            private void AddHourSubtotalRow(int gridRow, string labelText, List<DateTime> displayDates, string[] hourTypes)
            {
                var rowHeaderLabel = CreateLabel(labelText, true); rowHeaderLabel.Tag = "RowHeader";
                rowHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
                rowHeaderLabel.Padding = new Padding(5, 0, 0, 0);
                AddControlToGrid(rowHeaderLabel, 0, gridRow);
                int currentGridColumn = 1;
                foreach (var displayDate in displayDates)
                {
                    if (currentGridColumn + 2 >= _gridPanel.ColumnCount) break;
                    ResourceDayData dayData = _resource.DailyData.Values.FirstOrDefault(rd => rd != null && rd.Date.Date == displayDate.Date);
                    int dayKey = -1;
                    if (dayData != null)
                    {
                        var kvp = _resource.DailyData.FirstOrDefault(entry => entry.Value == dayData);
                        dayKey = kvp.Value != null ? kvp.Key : -1;
                    }
                    decimal plannedSubtotal = 0, actualSubtotal = 0, deltaSubtotal = 0;
                    if (dayData != null)
                    {
                        foreach (var t in hourTypes)
                        {
                            plannedSubtotal += ResultsGridCalculationHelper.GetPlannedValue(dayData, t);
                            actualSubtotal += ResultsGridCalculationHelper.GetActualValue(dayData, t);
                        }
                        deltaSubtotal = actualSubtotal - plannedSubtotal;
                    }
                    string baseKey = $"SUB_Hours_{dayKey}";
                    var pL = CreateLabel(plannedSubtotal.ToString("F1"), true); pL.Tag = "Subtotal"; pL.BackColor = Color.Yellow;
                    AddControlToGrid(pL, currentGridColumn, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_P"] = pL;
                    var aL = CreateLabel(actualSubtotal.ToString("F1"), true); aL.Tag = "Subtotal"; aL.BackColor = Color.Yellow;
                    AddControlToGrid(aL, currentGridColumn + 1, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_A"] = aL;
                    var dL = CreateLabel(deltaSubtotal.ToString("F1"), true); dL.Tag = new DeltaTag("Delta", deltaSubtotal); dL.BackColor = Color.Yellow;
                    SetDeltaLabelColor(dL, deltaSubtotal);
                    AddControlToGrid(dL, currentGridColumn + 2, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_D"] = dL;
                    currentGridColumn += 3;
                }
            }

            // Add this method to ResourcePanel
            internal void UpdateDeltaLabel(string dataType, int dayKey)
            {
                if (_deltaLabels.TryGetValue($"D_{dataType}_{dayKey}", out Label deltaLabel))
                {
                    var dayData = _resource.DailyData.ContainsKey(dayKey) ? _resource.DailyData[dayKey] : null;
                    if (dayData != null)
                    {
                        decimal plannedValue = ResultsGridCalculationHelper.GetPlannedValue(dayData, dataType);
                        decimal actualValue = ResultsGridCalculationHelper.GetActualValue(dayData, dataType);
                        decimal deltaValue = actualValue - plannedValue;
                        deltaLabel.Text = ResultsGridCalculationHelper.IsHoursType(dataType) ? deltaValue.ToString("F1") : deltaValue.ToString("C2");
                        SetDeltaLabelColor(deltaLabel, deltaValue);
                    }
                }
            }
        }

        internal class EditableCell : UserControl
        {
            private Label _label; private TextBox _textBox; private decimal _value; private string _dataType; private int _dayKey; private ResourceDayData _dayData; private CommissioningResource _resource; private ResourcePanel _parentPanel; private bool _isEditing = false;
            public EditableCell(decimal v, string dT, int dK, ResourceDayData dD, CommissioningResource res, ResourcePanel pP) { _value = v; _dataType = dT; _dayKey = dK; _dayData = dD; _resource = res; _parentPanel = pP; InitializeControls(); }
            public void UpdateValueFromDayData() {
                if (_dayData != null)
                    _value = ResultsGridCalculationHelper.GetActualValue(_dayData, _dataType);
                if (ResultsGridCalculationHelper.IsHoursType(_dataType)) {
                    _label.Text = _value.ToString("F1");
                    _label.Font = new Font(_label.Font, _value != 0 ? FontStyle.Bold : FontStyle.Regular);
                } else {
                    _label.Text = _value.ToString("C2");
                }
                if (_textBox.Visible)
                    _textBox.Text = ResultsGridCalculationHelper.IsHoursType(_dataType) ? _value.ToString("F1") : _value.ToString("F2");
            }
            public override Color BackColor { get => base.BackColor; set { base.BackColor = value; if (_label != null) _label.BackColor = value; } }
            public void ApplyTheme(bool isDarkMode, (Color back, Color fore) colors)
            {
                _label.BackColor = colors.back;
                _label.ForeColor = colors.fore;
                _textBox.BackColor = colors.back;
                _textBox.ForeColor = colors.fore;
            }
            private void InitializeControls() { _label = new Label { TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Cursor = Cursors.Hand }; _textBox = new TextBox { TextAlign = HorizontalAlignment.Right, Dock = DockStyle.Fill, Visible = false }; UpdateValueFromDayData(); _label.MouseClick += Label_MouseClick; _textBox.KeyDown += TextBox_KeyDown; _textBox.Leave += TextBox_Leave; _textBox.KeyPress += TextBox_KeyPress; this.Controls.Add(_label); this.Controls.Add(_textBox); }
            private void Label_MouseClick(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) StartEdit(); }
            private void StartEdit() { _isEditing = true; _label.Visible = false; _textBox.Visible = true; _textBox.Text = _parentPanel.IsHoursType(_dataType) ? _value.ToString("F1") : _value.ToString("F2"); _textBox.SelectAll(); _textBox.Focus(); }
            private void EndEdit(bool save) { if (!_isEditing) return; _isEditing = false; if (save) { if (decimal.TryParse(_textBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out decimal nV)) { if (nV != _value) { _value = nV; UpdateDayDataInternal(nV); _parentPanel.NotifyValueChanged(_dataType, _dayKey); } } } UpdateValueFromDayData(); _textBox.Visible = false; _label.Visible = true; }
            private void UpdateDayDataInternal(decimal nV) { if (_dataType == "ServiceReg") _dayData.ActualRegularLabourHours = nV; else if (_dataType == "ServiceOT") _dayData.ActualOvertimeLabourHours = nV; else if (_dataType == "ServicePrem") _dayData.ActualPremiumLabourHours = nV; else if (_dataType == "TravelReg") _dayData.ActualRegularTravelHours = nV; else if (_dataType == "TravelOT") _dayData.ActualOvertimeTravelHours = nV; else if (_dataType == "TravelPrem") _dayData.ActualPremiumTravelHours = nV; else if (_dataType == "Mileage") _dayData.ActualMileageCost = nV; else if (_dataType == "PerDiem") _dayData.ActualPerDiemCost = nV; else if (_dataType == "Flight") _dayData.ActualFlightCost = nV; else if (_dataType == "CarRental") _dayData.ActualRentalCarCost = nV; else if (_dataType == "Hotel") _dayData.ActualHotelCost = nV; if (_parentPanel.IsHoursType(_dataType)) { if (_dayData.ActualPerDiemCost == _dayData.PlannedPerDiemCost || _dayData.ActualPerDiemCost == Helpers.ExpenseCalculator.CalculatePerDiemCost(_resource.PerDiemRate, _dayData.GetPlannedLabourHoursTotal(), _dayData.GetPlannedTravelHoursTotal())) { decimal lH = _dayData.ActualRegularLabourHours + _dayData.ActualOvertimeLabourHours + _dayData.ActualPremiumLabourHours; decimal tH = _dayData.ActualRegularTravelHours + _dayData.ActualOvertimeTravelHours + _dayData.ActualPremiumTravelHours; _dayData.ActualPerDiemCost = Helpers.ExpenseCalculator.CalculatePerDiemCost(_resource.PerDiemRate, lH, tH); } } _resource.IsDirty = true; }
            private void TextBox_KeyPress(object s, KeyPressEventArgs e) { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.')) e.Handled = true; if ((e.KeyChar == '.') && ((TextBox)s).Text.IndexOf('.') > -1) e.Handled = true; }
            private void TextBox_KeyDown(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { EndEdit(true); e.Handled = true; e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Escape) { _textBox.Text = _parentPanel.IsHoursType(_dataType) ? _value.ToString("F1") : _value.ToString("F2"); EndEdit(false); e.Handled = true; e.SuppressKeyPress = true; } }
            private void TextBox_Leave(object s, EventArgs e) { if (_textBox.Visible) EndEdit(true); }
        }

        private void ToggleDarkMode()
        {
            _isDarkMode = !_isDarkMode;
            Properties.Settings.Default.ResultsDarkMode = _isDarkMode;
            Properties.Settings.Default.Save();
            ApplyTheme();
        }
        private void ApplyTheme()
        {
            if (_isDarkMode)
            {
                this.BackColor = darkBackColor;
                this.ForeColor = darkTextColor;
                if (_darkModeButton != null)
                {
                    _darkModeButton.BackColor = darkButtonBackColor;
                    _darkModeButton.ForeColor = darkButtonForeColor;
                }
                if (_exportButton != null)
                {
                    _exportButton.BackColor = darkButtonBackColor;
                    _exportButton.ForeColor = darkButtonForeColor;
                }
                if (_closeButton != null)
                {
                    _closeButton.BackColor = darkButtonBackColor;
                    _closeButton.ForeColor = darkButtonForeColor;
                }
                ApplyDarkThemeToControl(this);
            }
            else
            {
                this.BackColor = SystemColors.Control;
                this.ForeColor = SystemColors.ControlText;
                if (_darkModeButton != null)
                {
                    _darkModeButton.BackColor = SystemColors.Control;
                    _darkModeButton.ForeColor = SystemColors.ControlText;
                }
                if (_exportButton != null)
                {
                    _exportButton.BackColor = SystemColors.Control;
                    _exportButton.ForeColor = SystemColors.ControlText;
                }
                if (_closeButton != null)
                {
                    _closeButton.BackColor = SystemColors.Control;
                    _closeButton.ForeColor = SystemColors.ControlText;
                }
                ApplyLightThemeToControl(this);
            }
            // Reapply special formatting and refresh grid for all resource panels after theme is applied
            foreach (var rp in _resourcePanels) {
                rp.ApplyCellStyles(rp._gridPanel); // Always reapply cell styles after theme
            }
        }
        private void ApplyDarkThemeToControl(Control control)
        {
            // Only apply to panels, buttons, and summary labels, not grid cells
            if (control is Panel || control is TableLayoutPanel)
            {
                control.BackColor = darkPanelBackColor;
                control.ForeColor = darkTextColor;
            }
            else if (control is Button)
            {
                control.BackColor = darkButtonBackColor;
                control.ForeColor = darkButtonForeColor;
            }
            else if (control is Label lbl)
            {
                // Only apply to summary panel labels (not grid cells)
                if (lbl.Parent == _summaryPanel)
                {
                    lbl.BackColor = darkPanelBackColor;
                    lbl.ForeColor = darkTextColor;
                }
                // else: skip grid cell labels
            }
            // Skip EditableCell and grid cell labels
            foreach (Control child in control.Controls)
            {
                ApplyDarkThemeToControl(child);
            }
        }
        private void ApplyLightThemeToControl(Control control)
        {
            // Only apply to panels, buttons, and summary labels, not grid cells
            if (control is Panel || control is TableLayoutPanel)
            {
                control.BackColor = SystemColors.Control;
                control.ForeColor = SystemColors.ControlText;
            }
            else if (control is Button)
            {
                control.BackColor = SystemColors.Control;
                control.ForeColor = SystemColors.ControlText;
            }
            else if (control is Label lbl)
            {
                // Only apply to summary panel labels (not grid cells)
                if (lbl.Parent == _summaryPanel)
                {
                    lbl.BackColor = SystemColors.Control;
                    lbl.ForeColor = SystemColors.ControlText;
                }
                // else: skip grid cell labels
            }
            // Skip EditableCell and grid cell labels
            foreach (Control child in control.Controls)
            {
                ApplyLightThemeToControl(child);
            }
        }

        internal ResultsGridViewModel BuildResultsGridViewModel(CommissioningResource resource, List<DateTime> displayDates)
        {
            var model = new ResultsGridViewModel();
            // Build columns: first is row header, then for each date: Planned, Actual, Delta
            model.Columns.Add(new ResultsGridColumn { Header = "", Date = null, DisplayIndex = 0 });
            int colIdx = 1;
            foreach (var date in displayDates)
            {
                model.Columns.Add(new ResultsGridColumn { Header = date.ToString("ddd MMM dd"), Date = date, DisplayIndex = colIdx });
                colIdx++;
            }

            // Define row layout (with requested order)
            var rowDefs = new List<(string Label, ResultsGridRowType RowType, string[] DataTypes, bool IsSubtotal, bool IsTotal)>
            {
                ("Regular Labour", ResultsGridRowType.Data, new[]{"ServiceReg"}, false, false),
                ("Overtime Labour", ResultsGridRowType.Data, new[]{"ServiceOT"}, false, false),
                ("Premium Labour", ResultsGridRowType.Data, new[]{"ServicePrem"}, false, false),
                ("Subtotal Hours - Labour", ResultsGridRowType.Subtotal, new[]{"ServiceReg","ServiceOT","ServicePrem"}, true, false),
                ("Regular Travel", ResultsGridRowType.Data, new[]{"TravelReg"}, false, false),
                ("Overtime Travel", ResultsGridRowType.Data, new[]{"TravelOT"}, false, false),
                ("Premium Travel", ResultsGridRowType.Data, new[]{"TravelPrem"}, false, false),
                ("Subtotal Hours - Travel", ResultsGridRowType.Subtotal, new[]{"TravelReg","TravelOT","TravelPrem"}, true, false),
                ("Subtotals - Charges", ResultsGridRowType.Subtotal, null, true, false),
                ("Subtotals - Expenses", ResultsGridRowType.Subtotal, null, true, false),
                ("Mileage", ResultsGridRowType.Data, new[]{"Mileage"}, false, false),
                ("Per Diem", ResultsGridRowType.Data, new[]{"PerDiem"}, false, false),
                ("Flight", ResultsGridRowType.Data, new[]{"Flight"}, false, false),
                ("Car Rental", ResultsGridRowType.Data, new[]{"CarRental"}, false, false),
                ("Hotel", ResultsGridRowType.Data, new[]{"Hotel"}, false, false),
                ("Totals", ResultsGridRowType.Total, null, false, true)
            };

            foreach (var rowDef in rowDefs)
            {
                var row = new ResultsGridRow { Label = rowDef.Label, RowType = rowDef.RowType };
                // Row header cell
                row.Cells.Add(new ResultsGridCell { Value = rowDef.Label, CellType = ResultsGridCellType.RowHeader, IsEditable = false });
                foreach (var date in displayDates)
                {
                    var dayData = resource.DailyData.Values.FirstOrDefault(rd => rd != null && rd.Date.Date == date.Date);
                    // For each subcolumn: Planned, Actual, Delta
                    foreach (var cellType in new[]{ResultsGridCellType.Planned, ResultsGridCellType.Actual, ResultsGridCellType.Delta})
                    {
                        var cell = new ResultsGridCell { CellType = cellType, IsEditable = (cellType == ResultsGridCellType.Actual && rowDef.RowType == ResultsGridRowType.Data) };
                        decimal planned = 0, actual = 0, delta = 0;
                        if (rowDef.DataTypes != null && dayData != null)
                        {
                            foreach (var dt in rowDef.DataTypes)
                            {
                                planned += ResultsGridCalculationHelper.GetPlannedValue(dayData, dt);
                                actual += ResultsGridCalculationHelper.GetActualValue(dayData, dt);
                            }
                            delta = actual - planned;
                        }
                        // Subtotals and totals
                        if (rowDef.IsSubtotal && dayData != null)
                        {
                            if (rowDef.Label == "Subtotals - Charges")
                            {
                                planned = ResultsGridCalculationHelper.CalculateSubtotal(dayData, resource, true, true);
                                actual = ResultsGridCalculationHelper.CalculateSubtotal(dayData, resource, true, false);
                                delta = actual - planned;
                            }
                            else if (rowDef.Label == "Subtotals - Expenses")
                            {
                                planned = ResultsGridCalculationHelper.CalculateSubtotal(dayData, resource, false, true);
                                actual = ResultsGridCalculationHelper.CalculateSubtotal(dayData, resource, false, false);
                                delta = actual - planned;
                            }
                            else if (rowDef.Label.StartsWith("Subtotal Hours"))
                            {
                                // Already summed above
                            }
                        }
                        if (rowDef.IsTotal && dayData != null)
                        {
                            planned = ResultsGridCalculationHelper.CalculateDayTotal(dayData, resource, true);
                            actual = ResultsGridCalculationHelper.CalculateDayTotal(dayData, resource, false);
                            delta = actual - planned;
                        }
                        // Set value and formatting
                        if (cellType == ResultsGridCellType.Planned)
                            cell.Value = rowDef.RowType == ResultsGridRowType.Data && rowDef.DataTypes != null && ResultsGridCalculationHelper.IsHoursType(rowDef.DataTypes[0]) ? planned.ToString("F1") : planned.ToString("C2");
                        else if (cellType == ResultsGridCellType.Actual)
                            cell.Value = rowDef.RowType == ResultsGridRowType.Data && rowDef.DataTypes != null && ResultsGridCalculationHelper.IsHoursType(rowDef.DataTypes[0]) ? actual.ToString("F1") : actual.ToString("C2");
                        else if (cellType == ResultsGridCellType.Delta)
                        {
                            if (rowDef.RowType == ResultsGridRowType.Data && rowDef.DataTypes != null && ResultsGridCalculationHelper.IsHoursType(rowDef.DataTypes[0]))
                                cell.Value = delta.ToString("F1");
                            else
                                cell.Value = delta.ToString("C2");
                            // Highlighting for delta
                            if (rowDef.RowType == ResultsGridRowType.Subtotal || rowDef.RowType == ResultsGridRowType.Total)
                                cell.IsHighlighted = true;
                            if (delta > 0) cell.ForeColor = Color.Red;
                            else if (delta < 0) cell.ForeColor = Color.Green;
                            else cell.ForeColor = Color.Black;
                        }
                        // Highlighting for subtotal/total rows
                        if (rowDef.RowType == ResultsGridRowType.Subtotal || rowDef.RowType == ResultsGridRowType.Total)
                            cell.BackColor = Color.Yellow;
                        row.Cells.Add(cell);
                    }
                }
                model.Rows.Add(row);
            }
            return model;
        }

        internal void RenderResultsGridFromViewModel(ResultsGridViewModel model, TableLayoutPanel grid)
        {
            grid.SuspendLayout();
            grid.Controls.Clear();
            grid.ColumnStyles.Clear();
            grid.RowStyles.Clear();
            
            // Calculate column count: 1 row header + 3 columns (Planned, Actual, Delta) for each date
            int columnCount = 1 + (model.Columns.Count - 1) * 3; // -1 because first column is row header
            grid.ColumnCount = columnCount;
            grid.RowCount = model.Rows.Count;

            // Set up columns: first is row header, then for each date: Planned, Actual, Delta
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // Row header
            for (int i = 1; i < model.Columns.Count; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); // Planned
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); // Actual
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); // Delta
            }

            // Add rows
            for (int rowIdx = 0; rowIdx < model.Rows.Count; rowIdx++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
                var row = model.Rows[rowIdx];
                int gridCol = 0;
                
                for (int cellIdx = 0; cellIdx < row.Cells.Count; cellIdx++)
                {
                    var cell = row.Cells[cellIdx];
                    Control ctrl;
                    
                    if (cell.CellType == ResultsGridCellType.RowHeader)
                    {
                        ctrl = new Label
                        {
                            Text = cell.Value,
                            TextAlign = ContentAlignment.MiddleLeft,
                            Dock = DockStyle.Fill,
                            Font = new Font(this.Font, FontStyle.Bold),
                            Padding = new Padding(5, 0, 0, 0),
                            BackColor = SystemColors.ControlLight
                        };
                    }
                    else if (cell.IsEditable)
                    {
                        // Should not be used; fallback to label
                        ctrl = new Label
                        {
                            Text = cell.Value,
                            TextAlign = ContentAlignment.MiddleRight,
                            Dock = DockStyle.Fill,
                            Font = this.Font
                        };
                        // Optionally: throw new NotSupportedException("EditableCell not supported in view model renderer");
                    }
                    else
                    {
                        ctrl = new Label
                        {
                            Text = cell.Value,
                            TextAlign = ContentAlignment.MiddleRight,
                            Dock = DockStyle.Fill,
                            Font = this.Font
                        };
                    }
                    
                    // Apply formatting
                    if (cell.BackColor.HasValue) ctrl.BackColor = cell.BackColor.Value;
                    if (cell.ForeColor.HasValue) ctrl.ForeColor = cell.ForeColor.Value;
                    if (cell.IsHighlighted) ctrl.BackColor = Color.Yellow;
                    
                    grid.Controls.Add(ctrl, gridCol, rowIdx);
                    gridCol++;
                }
            }
            grid.ResumeLayout(true);
        }

        private void UpdateResultsGrid()
        {
            if (_project == null || _isClosing) return;

            // This method is called for individual resource updates
            // The main grid updates happen in ResourcePanel.RefreshGridDisplay()
            // We'll integrate the new view model approach there
        }
    }
}

// Extension method for thread-safe control property setting (optional, but good practice if you ever multithread UI updates)
// You can put this in a separate static class file, e.g., ControlExtensions.cs
// namespace LabourBudgetCalculator { // Or your root namespace
//     public static class ControlExtensions
//     {
//         public static void SetPropertyThreadSafe<TControl>(this TControl control, Action<TControl> action)
//             where TControl : Control
//         {
//             if (control.InvokeRequired)
//             {
//                 control.Invoke(new Action(() => action(control)));
//             }
//             else
//             {
//                 action(control);
//             }
//         }
//     }
// }