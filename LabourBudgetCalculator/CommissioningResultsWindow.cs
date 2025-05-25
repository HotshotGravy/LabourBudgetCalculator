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
   

    public partial class CommissioningResultsWindow : Form
    {
        private CommissioningProject _project;
        private TableLayoutPanel _summaryPanel;
        private Panel _mainPanel;
        private Button _exportButton;
        private Button _closeButton;
        private List<ResourcePanel> _resourcePanels = new List<ResourcePanel>();
        private bool _isClosing = false;
        private Panel _contentHostPanel;

        public CommissioningResultsWindow(CommissioningProject project)
        {
            this.DoubleBuffered = true;
            InitializeComponent();
            _project = project ?? throw new ArgumentNullException(nameof(project));

            this.Text = $"Results - {_project.ProjectName}";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1200, 600);

            this.Load += (s, e) =>
            {
                SetupUI();
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
            string[] headers = { "Quoted", "Planned", "Current", "Forecast", "Delta (vs Quoted)" };
            for (int i = 0; i < headers.Length; i++)
            {
                panel.Controls.Add(new Label { Text = headers[i], TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Bold), BackColor = SystemColors.ControlLight }, i, 0);
            }
            string[] names = { "lblQuoted", "lblPlanned", "lblCurrent", "lblForecast", "lblDelta" };
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
            panel.Controls.AddRange(new Control[] { _exportButton, _closeButton });
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
                if (_summaryPanel.Controls["lblCurrent"] is Label lblCurrent)
                {
                    lblCurrent.Text = FormatCurrency(_project.CurrentTotal);
                }
                if (_summaryPanel.Controls["lblForecast"] is Label lblForecast)
                {
                    lblForecast.Text = FormatCurrency(_project.ForecastTotal);
                }
                if (_summaryPanel.Controls["lblDelta"] is Label lblDelta)
                {
                    decimal delta = _project.ForecastTotal - _project.InitialEstimate;
                    lblDelta.Text = FormatCurrency(delta);
                    lblDelta.ForeColor = delta == 0 ? Color.Black : (delta > 0 ? Color.Red : Color.Green);
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
            private TableLayoutPanel _gridPanel;
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


            // --- Helper methods for data retrieval and calculation ---
            internal decimal GetPlannedValue(ResourceDayData dD, string dT)
            {
                if (dD == null) return 0;
                if (dT == "ServiceReg") return dD.PlannedRegularLabourHours;
                if (dT == "ServiceOT") return dD.PlannedOvertimeLabourHours;
                if (dT == "ServicePrem") return dD.PlannedPremiumLabourHours;
                if (dT == "TravelReg") return dD.PlannedRegularTravelHours;
                if (dT == "TravelOT") return dD.PlannedOvertimeTravelHours;
                if (dT == "TravelPrem") return dD.PlannedPremiumTravelHours;
                if (dT == "Mileage") return dD.PlannedMileageCost;
                if (dT == "PerDiem") return dD.PlannedPerDiemCost;
                if (dT == "Flight") return dD.PlannedFlightCost;
                if (dT == "CarRental") return dD.PlannedRentalCarCost;
                if (dT == "Hotel") return dD.PlannedHotelCost;
                return 0;
            }

            internal decimal GetActualValue(ResourceDayData dD, string dT)
            {
                if (dD == null) return 0;
                if (dT == "ServiceReg") return dD.ActualRegularLabourHours;
                if (dT == "ServiceOT") return dD.ActualOvertimeLabourHours;
                if (dT == "ServicePrem") return dD.ActualPremiumLabourHours;
                if (dT == "TravelReg") return dD.ActualRegularTravelHours;
                if (dT == "TravelOT") return dD.ActualOvertimeTravelHours;
                if (dT == "TravelPrem") return dD.ActualPremiumTravelHours;
                if (dT == "Mileage") return dD.ActualMileageCost;
                if (dT == "PerDiem") return dD.ActualPerDiemCost;
                if (dT == "Flight") return dD.ActualFlightCost;
                if (dT == "CarRental") return dD.ActualRentalCarCost;
                if (dT == "Hotel") return dD.ActualHotelCost;
                return 0;
            }

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
                _plannedLabels.Clear(); _deltaLabels.Clear(); _subtotalAndTotalLabels.Clear(); _editableCells.Clear();
                _gridPanel.SuspendLayout();
                _gridPanel.Controls.Clear();

                if (_resource == null) { _gridPanel.ResumeLayout(true); UpdateTotalLabel(); return; }

                // Use _overallDisplayDates; it should not be null due to constructor handling.
                if (!_overallDisplayDates.Any())
                {
                    SetupGridStructure(0); // Setup with no date columns
                    _gridPanel.ResumeLayout(true);
                    UpdateTotalLabel(); // Resource's total, not day-specific
                    UpdateHeaderText(0); // Update with 0 display days
                    return;
                }

                Helpers.ExpenseCalculator.CalculateResourceExpenses(_resource);

                SetupGridStructure(_overallDisplayDates.Count);
                AddHeaders(_overallDisplayDates); // Pass List<DateTime>
                AddDataRowsAndTotals(_overallDisplayDates); // New combined method or sequence of calls

                _gridPanel.ResumeLayout(true);
                UpdateTotalLabel();
                UpdateHeaderText(_overallDisplayDates.Count);
            }

            private void SetupGridStructure(int dayCount)
            {
                _gridPanel.RowCount = 16; _gridPanel.ColumnCount = 1 + (Math.Max(0, dayCount) * 3);
                _gridPanel.ColumnStyles.Clear(); _gridPanel.RowStyles.Clear();
                if (_gridPanel.ColumnCount > 0) _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                for (int i = 0; i < dayCount; i++) { _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); }
                for (int i = 0; i < _gridPanel.RowCount; i++) _gridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            }

            private void AddHeaders(List<DateTime> displayDates) // Changed parameter
            {
                if (_gridPanel.ColumnCount == 0 || !displayDates.Any()) return;
                AddControlToGrid(CreateLabel("", true), 0, 0); // Top-left empty cell
                int currentGridColumn = 1;
                foreach (DateTime displayDate in displayDates)
                {
                    if (currentGridColumn + 2 >= _gridPanel.ColumnCount) break; // Ensure space for 3 cells per date
                    var dateHeaderLabel = CreateLabel(displayDate.ToString("ddd MMM dd"), true);
                    dateHeaderLabel.BackColor = Color.FromArgb(230, 230, 250);
                    dateHeaderLabel.BorderStyle = BorderStyle.FixedSingle;
                    AddControlToGrid(dateHeaderLabel, currentGridColumn, 0);
                    _gridPanel.SetColumnSpan(dateHeaderLabel, 3);

                    AddControlToGrid(CreateLabel("Planned", true, Color.FromArgb(235, 245, 255)), currentGridColumn, 1);
                    AddControlToGrid(CreateLabel("Actual", true, Color.FromArgb(235, 255, 235)), currentGridColumn + 1, 1);
                    AddControlToGrid(CreateLabel("Delta", true, Color.FromArgb(255, 255, 235)), currentGridColumn + 2, 1);
                    currentGridColumn += 3;
                }
            }

            private void AddControlToGrid(Control control, int column, int row) { if (column < _gridPanel.ColumnCount && row < _gridPanel.RowCount) _gridPanel.Controls.Add(control, column, row); }
            private Label CreateLabel(string text, bool isHeader, Color? backColor = null) { var lbl = new Label { Text = text, TextAlign = isHeader ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Font = isHeader ? new Font(this.Font, FontStyle.Bold) : this.Font }; if (backColor.HasValue) lbl.BackColor = backColor.Value; else if (isHeader) lbl.BackColor = SystemColors.ControlLight; return lbl; }
            private string FormatValue(decimal value, string dataType) { return IsHoursType(dataType) ? value.ToString("F1") : value.ToString("C2"); }
            internal bool IsHoursType(string dataType) { return dataType != null && (dataType.StartsWith("Service") || dataType.StartsWith("Travel")); }
            private void SetDeltaLabelColor(Label label, decimal delta) { label.ForeColor = delta == 0 ? Color.Black : (delta > 0 ? Color.DarkRed : Color.DarkGreen); } // Assuming delta > 0 is over budget/actual > planned

            // Corrected AddDataRow (already provided in previous response, ensure it's used)
            private void AddDataRow(int gridRow, string rowLabelText, List<DateTime> displayDates, string dataType)
            {
                var headerLabel = CreateLabel(rowLabelText, false);
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
                        dayKey = kvp.Value != null ? kvp.Key : -1; // dayKey is the original dictionary key
                    }

                    if (dayData != null && dayKey != -1)
                    {
                        decimal plannedValue = GetPlannedValue(dayData, dataType);
                        decimal actualValue = GetActualValue(dayData, dataType);
                        decimal deltaValue = actualValue - plannedValue;

                        var plannedLabel = CreateLabel(FormatValue(plannedValue, dataType), false);
                        if (IsHoursType(dataType) && plannedValue != 0) plannedLabel.Font = new Font(this.Font, FontStyle.Bold);
                        AddControlToGrid(plannedLabel, currentGridColumn, gridRow);
                        _plannedLabels[$"P_{dataType}_{dayKey}"] = plannedLabel;

                        AddEditableCell(gridRow, currentGridColumn + 1, actualValue, dataType, dayKey, dayData);

                        var deltaLabel = CreateLabel(FormatValue(deltaValue, dataType), false);
                        SetDeltaLabelColor(deltaLabel, deltaValue);
                        AddControlToGrid(deltaLabel, currentGridColumn + 2, gridRow);
                        _deltaLabels[$"D_{dataType}_{dayKey}"] = deltaLabel;
                    }
                    else
                    {
                        AddControlToGrid(CreateLabel(string.Empty, false), currentGridColumn, gridRow);
                        AddControlToGrid(CreateLabel(string.Empty, false), currentGridColumn + 1, gridRow);
                        AddControlToGrid(CreateLabel(string.Empty, false), currentGridColumn + 2, gridRow);
                    }
                    currentGridColumn += 3;
                }
            }

            // Corrected AddSubtotalRow
            private void AddSubtotalRow(int gridRow, string labelText, List<DateTime> displayDates, bool isChargesSubtotal)
            {
                var rowHeaderLabel = CreateLabel(labelText, true);
                rowHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
                rowHeaderLabel.BackColor = Color.LightYellow;
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
                        plannedSubtotal = CalculateSubtotal(dayData, isChargesSubtotal, true);
                        actualSubtotal = CalculateSubtotal(dayData, isChargesSubtotal, false);
                        deltaSubtotal = actualSubtotal - plannedSubtotal;
                    }

                    string cleanLabelText = labelText.Replace(" ", "").Replace("-", "");
                    string baseKey = $"SUB_{cleanLabelText}_{dayKey}"; // Use dayKey if found for uniqueness

                    var pL = CreateLabel(plannedSubtotal.ToString("C2"), true); pL.BackColor = Color.LightYellow;
                    AddControlToGrid(pL, currentGridColumn, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_P"] = pL;

                    var aL = CreateLabel(actualSubtotal.ToString("C2"), true); aL.BackColor = Color.LightYellow;
                    AddControlToGrid(aL, currentGridColumn + 1, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_A"] = aL;

                    var dL = CreateLabel(deltaSubtotal.ToString("C2"), true); dL.BackColor = Color.LightYellow;
                    SetDeltaLabelColor(dL, deltaSubtotal);
                    AddControlToGrid(dL, currentGridColumn + 2, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_D"] = dL;

                    currentGridColumn += 3;
                }
            }

            private void AddEditableCell(int gridRow, int gridCol, decimal value, string dataType, int dayKey, ResourceDayData dayData, Color? backColor = null)
            {
                var eC = new EditableCell(value, dataType, dayKey, dayData, _resource, this) { Dock = DockStyle.Fill };
                if (backColor.HasValue) eC.BackColor = backColor.Value;
                _editableCells[$"A_{dataType}_{dayKey}"] = eC; // Store reference to the EditableCell
                AddControlToGrid(eC, gridCol, gridRow); // Add to grid
            }

            // Corrected AddTotalsRow (already provided in previous response, ensure it's used)
            private void AddTotalsRow(int gridRow, string labelText, List<DateTime> displayDates)
            {
                var rowHeaderLabel = CreateLabel(labelText, true);
                rowHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
                rowHeaderLabel.BackColor = Color.Yellow;
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
                        plannedTotal = CalculateDayTotal(dayData, true);
                        actualTotal = CalculateDayTotal(dayData, false);
                        deltaTotal = actualTotal - plannedTotal;
                    }

                    string baseKey = $"TOTAL_Day_{dayKey}"; // Use dayKey for uniqueness

                    var pL = CreateLabel(plannedTotal.ToString("C2"), true); pL.BackColor = Color.Yellow;
                    AddControlToGrid(pL, currentGridColumn, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_P"] = pL;

                    var aL = CreateLabel(actualTotal.ToString("C2"), true); aL.BackColor = Color.Yellow;
                    AddControlToGrid(aL, currentGridColumn + 1, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_A"] = aL;

                    var dL = CreateLabel(deltaTotal.ToString("C2"), true); dL.BackColor = Color.Yellow;
                    SetDeltaLabelColor(dL, deltaTotal);
                    AddControlToGrid(dL, currentGridColumn + 2, gridRow);
                    if (dayKey != -1) _subtotalAndTotalLabels[$"{baseKey}_D"] = dL;

                    currentGridColumn += 3;
                }
            }

            private void AddDataRowsAndTotals(List<DateTime> displayDates)
            {
                int r = 2; // Start grid row index for data
                           // Define your data types/rows
                var dataRowDefinitions = new[] {
        new { Label = "Service (Reg)", DataType = "ServiceReg" },
        new { Label = "Service (OT)", DataType = "ServiceOT" },
        new { Label = "Service (Premium)", DataType = "ServicePrem" },
        new { Label = "Travel (Reg)", DataType = "TravelReg" },
        new { Label = "Travel (OT)", DataType = "TravelOT" },
        new { Label = "Travel (Premium)", DataType = "TravelPrem" }
    };
                foreach (var def in dataRowDefinitions) { AddDataRow(r++, def.Label, displayDates, def.DataType); }
                AddSubtotalRow(r++, "Subtotals - Charges", displayDates, true);

                var expenseRowDefinitions = new[] {
        new { Label = "Mileage", DataType = "Mileage" },
        new { Label = "Per Diem", DataType = "PerDiem" },
        new { Label = "Flight", DataType = "Flight" },
        new { Label = "Car Rental", DataType = "CarRental" },
        new { Label = "Hotel", DataType = "Hotel" }
    };
                foreach (var def in expenseRowDefinitions) { AddDataRow(r++, def.Label, displayDates, def.DataType); }
                AddSubtotalRow(r++, "Subtotals - Expenses", displayDates, false);
                AddTotalsRow(r++, "Totals", displayDates); // This is the Resource Daily Totals Row
            }

            // Modify AddDataRow, AddSubtotalRow, AddTotalsRow signatures and logic
            // Example for AddDataRow:


            // Similar modifications for AddSubtotalRow and AddTotalsRow:
            // Change List<KeyValuePair<int, ResourceDayData>> oD to List<DateTime> displayDates
            // Loop through displayDates, find matching ResourceDayData for _resource.
            // If ResourceDayData found, calculate and add labels.
            // If not found, add empty labels for that day's subtotal/total columns.

            // Example for AddTotalsRow (showing the Resource Daily Totals):


            private void RefreshGridDisplay()
            {
                if (_resource == null || _gridPanel == null || _overallDisplayDates == null) return; // Check _overallDisplayDates

                _gridPanel.SuspendLayout();
                // No need to re-filter _resource.DailyData if _overallDisplayDates is the source of truth for columns.

                int expectedColumnCount = 1 + (_overallDisplayDates.Count * 3);

                // Condition for full rebuild: if column count mismatches, or if essential dictionaries are empty but there are dates to show.
                if (_gridPanel.ColumnCount != expectedColumnCount ||
                    (!_editableCells.Any() && _overallDisplayDates.Any()) ||
                    (!_plannedLabels.Any() && _overallDisplayDates.Any()))
                {
                    // Full rebuild if structure is wrong or controls not initialized
                    PopulateData(); // This will use _overallDisplayDates
                    _gridPanel.ResumeLayout(true); // PopulateData now handles its own Suspend/Resume
                    return;
                }

                // In-place update
                string[] dataTypes = { "ServiceReg", "ServiceOT", "ServicePrem", "TravelReg", "TravelOT", "TravelPrem", "Mileage", "PerDiem", "Flight", "CarRental", "Hotel" };

                foreach (var displayDate in _overallDisplayDates)
                {
                    ResourceDayData dD = _resource.DailyData.Values.FirstOrDefault(rd => rd != null && rd.Date.Date == displayDate.Date);
                    int dK = -1;
                    if (dD != null)
                    {
                        var kvp = _resource.DailyData.FirstOrDefault(entry => entry.Value == dD);
                        dK = kvp.Value != null ? kvp.Key : -1;
                    }

                    if (dD != null && dK != -1) // Only update if there's data and a key for this resource on this date
                    {
                        foreach (string dT in dataTypes)
                        {
                            decimal pV = GetPlannedValue(dD, dT);
                            decimal aV = GetActualValue(dD, dT);
                            decimal dVal = aV - pV;

                            if (_plannedLabels.TryGetValue($"P_{dT}_{dK}", out Label pL)) { pL.Text = FormatValue(pV, dT); pL.Font = (IsHoursType(dT) && pV != 0) ? new Font(this.Font, FontStyle.Bold) : this.Font; }
                            if (_editableCells.TryGetValue($"A_{dT}_{dK}", out EditableCell aC)) { aC.UpdateValueFromDayData(); } // UpdateValueFromDayData should use the dD and dT on the cell
                            if (_deltaLabels.TryGetValue($"D_{dT}_{dK}", out Label dL)) { dL.Text = FormatValue(dVal, dT); SetDeltaLabelColor(dL, dVal); }
                        }
                    }
                }
                // Update subtotal and total rows using _overallDisplayDates
                UpdateSubtotalOrTotalRowLabels_InPlace(_overallDisplayDates, "Subtotals - Charges", true);
                UpdateSubtotalOrTotalRowLabels_InPlace(_overallDisplayDates, "Subtotals - Expenses", false);
                UpdateSubtotalOrTotalRowLabels_InPlace(_overallDisplayDates, "Totals", null); // For the main "Totals" row

                _gridPanel.ResumeLayout(true);
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
                        pV = CalculateSubtotal(dD, isChargesForSubtotal.Value, true);
                        aV = CalculateSubtotal(dD, isChargesForSubtotal.Value, false);
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

                    if (_subtotalAndTotalLabels.TryGetValue($"{keyPrefix}_P", out Label pL)) pL.Text = pV.ToString("C2");
                    if (_subtotalAndTotalLabels.TryGetValue($"{keyPrefix}_A", out Label aL)) aL.Text = aV.ToString("C2");
                    if (_subtotalAndTotalLabels.TryGetValue($"{keyPrefix}_D", out Label dL)) { dL.Text = dV.ToString("C2"); SetDeltaLabelColor(dL, dV); }
                }
            }

            private void UpdateSubtotalOrTotalRowLabels_InPlace(List<KeyValuePair<int, ResourceDayData>> oD, string bLTFT, bool? iC) { foreach (var dE in oD) { var dD = dE.Value; int dK = dE.Key; decimal pV, aV, dV; if (iC.HasValue) { pV = CalculateSubtotal(dD, iC.Value, true); aV = CalculateSubtotal(dD, iC.Value, false); } else { pV = CalculateDayTotal(dD, true); aV = CalculateDayTotal(dD, false); } dV = aV - pV; string cLT = bLTFT.Replace(" ", "").Replace("-", ""); string kP = iC.HasValue ? $"SUB_{cLT}" : $"TOTAL_Day"; if (_subtotalAndTotalLabels.TryGetValue($"{kP}_{dK}_P", out Label pL)) pL.Text = pV.ToString("C2"); if (_subtotalAndTotalLabels.TryGetValue($"{kP}_{dK}_A", out Label aL)) aL.Text = aV.ToString("C2"); if (_subtotalAndTotalLabels.TryGetValue($"{kP}_{dK}_D", out Label dL)) { dL.Text = dV.ToString("C2"); SetDeltaLabelColor(dL, dV); } } }

            internal void NotifyValueChanged() { _resource.CalculateResourceTotals(); RefreshGridDisplay(); UpdateTotalLabel(); _parentWindow.NotifyResourceDataChanged(); }
            internal void AppendCSVData(StringBuilder sb) { if (_resource.DailyData == null || !_resource.DailyData.Any()) return; var oD = _resource.DailyData.Where(kvp => kvp.Value.Date != DateTime.MinValue).OrderBy(kvp => kvp.Value.Date).ToList(); Helpers.ExpenseCalculator.CalculateResourceExpenses(_resource); foreach (var dE in oD) { var dD = dE.Value; decimal lC = (dD.ActualRegularLabourHours * GetRegularLabourRate()) + (dD.ActualOvertimeLabourHours * GetOvertimeLabourRate()) + (dD.ActualPremiumLabourHours * GetPremiumLabourRate()); decimal tC = (dD.ActualRegularTravelHours * GetRegularTravelRate()) + (dD.ActualOvertimeTravelHours * GetOvertimeTravelRate()) + (dD.ActualPremiumTravelHours * GetPremiumTravelRate()); decimal dayTotal = lC + tC + dD.ActualMileageCost + dD.ActualPerDiemCost + dD.ActualFlightCost + dD.ActualRentalCarCost + dD.ActualHotelCost; sb.AppendLine($"\"{EscapeCSV(_resource.TechnicianName)}\",{dD.Date:MM/dd/yyyy},{dD.Date:dddd},{dD.ActualRegularLabourHours},{dD.ActualOvertimeLabourHours},{dD.ActualPremiumLabourHours},{dD.ActualRegularTravelHours},{dD.ActualOvertimeTravelHours},{dD.ActualPremiumTravelHours},{dD.ActualMileageCost},{dD.ActualPerDiemCost},{dD.ActualFlightCost},{dD.ActualRentalCarCost},{dD.ActualHotelCost},{dayTotal}"); } }
            private string EscapeCSV(string v) { if (v == null) return ""; if (v.Contains(",") || v.Contains("\"") || v.Contains("\n")) return $"\"{v.Replace("\"", "\"\"")}\""; return v; }
        }

        internal class EditableCell : UserControl
        {
            private Label _label; private TextBox _textBox; private decimal _value; private string _dataType; private int _dayKey; private ResourceDayData _dayData; private CommissioningResource _resource; private ResourcePanel _parentPanel; private bool _isEditing = false;
            public EditableCell(decimal v, string dT, int dK, ResourceDayData dD, CommissioningResource res, ResourcePanel pP) { _value = v; _dataType = dT; _dayKey = dK; _dayData = dD; _resource = res; _parentPanel = pP; InitializeControls(); }
            public void UpdateValueFromDayData() { if (_parentPanel != null && _dayData != null) _value = _parentPanel.GetActualValue(_dayData, _dataType); if (_parentPanel.IsHoursType(_dataType)) { _label.Text = _value.ToString("F1"); _label.Font = new Font(_label.Font, _value != 0 ? FontStyle.Bold : FontStyle.Regular); } else { _label.Text = _value.ToString("C2"); } if (_textBox.Visible) _textBox.Text = _parentPanel.IsHoursType(_dataType) ? _value.ToString("F1") : _value.ToString("F2"); }
            public override Color BackColor { get => base.BackColor; set { base.BackColor = value; if (_label != null) _label.BackColor = value; } }
            private void InitializeControls() { _label = new Label { TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Cursor = Cursors.Hand }; _textBox = new TextBox { TextAlign = HorizontalAlignment.Right, Dock = DockStyle.Fill, Visible = false }; UpdateValueFromDayData(); _label.MouseClick += Label_MouseClick; _textBox.KeyDown += TextBox_KeyDown; _textBox.Leave += TextBox_Leave; _textBox.KeyPress += TextBox_KeyPress; this.Controls.Add(_label); this.Controls.Add(_textBox); }
            private void Label_MouseClick(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) StartEdit(); }
            private void StartEdit() { _isEditing = true; _label.Visible = false; _textBox.Visible = true; _textBox.Text = _parentPanel.IsHoursType(_dataType) ? _value.ToString("F1") : _value.ToString("F2"); _textBox.SelectAll(); _textBox.Focus(); }
            private void EndEdit(bool save) { if (!_isEditing) return; _isEditing = false; if (save) { if (decimal.TryParse(_textBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out decimal nV)) { if (nV != _value) { _value = nV; UpdateDayDataInternal(nV); _parentPanel.NotifyValueChanged(); } } } UpdateValueFromDayData(); _textBox.Visible = false; _label.Visible = true; }
            private void UpdateDayDataInternal(decimal nV) { if (_dataType == "ServiceReg") _dayData.ActualRegularLabourHours = nV; else if (_dataType == "ServiceOT") _dayData.ActualOvertimeLabourHours = nV; else if (_dataType == "ServicePrem") _dayData.ActualPremiumLabourHours = nV; else if (_dataType == "TravelReg") _dayData.ActualRegularTravelHours = nV; else if (_dataType == "TravelOT") _dayData.ActualOvertimeTravelHours = nV; else if (_dataType == "TravelPrem") _dayData.ActualPremiumTravelHours = nV; else if (_dataType == "Mileage") _dayData.ActualMileageCost = nV; else if (_dataType == "PerDiem") _dayData.ActualPerDiemCost = nV; else if (_dataType == "Flight") _dayData.ActualFlightCost = nV; else if (_dataType == "CarRental") _dayData.ActualRentalCarCost = nV; else if (_dataType == "Hotel") _dayData.ActualHotelCost = nV; if (_parentPanel.IsHoursType(_dataType)) { if (_dayData.ActualPerDiemCost == _dayData.PlannedPerDiemCost || _dayData.ActualPerDiemCost == Helpers.ExpenseCalculator.CalculatePerDiemCost(_resource.PerDiemRate, _dayData.GetPlannedLabourHoursTotal(), _dayData.GetPlannedTravelHoursTotal())) { decimal lH = _dayData.ActualRegularLabourHours + _dayData.ActualOvertimeLabourHours + _dayData.ActualPremiumLabourHours; decimal tH = _dayData.ActualRegularTravelHours + _dayData.ActualOvertimeTravelHours + _dayData.ActualPremiumTravelHours; _dayData.ActualPerDiemCost = Helpers.ExpenseCalculator.CalculatePerDiemCost(_resource.PerDiemRate, lH, tH); } } _resource.IsDirty = true; }
            private void TextBox_KeyPress(object s, KeyPressEventArgs e) { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.')) e.Handled = true; if ((e.KeyChar == '.') && ((TextBox)s).Text.IndexOf('.') > -1) e.Handled = true; }
            private void TextBox_KeyDown(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { EndEdit(true); e.Handled = true; e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Escape) { _textBox.Text = _parentPanel.IsHoursType(_dataType) ? _value.ToString("F1") : _value.ToString("F2"); EndEdit(false); e.Handled = true; e.SuppressKeyPress = true; } }
            private void TextBox_Leave(object s, EventArgs e) { if (_textBox.Visible) EndEdit(true); }
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