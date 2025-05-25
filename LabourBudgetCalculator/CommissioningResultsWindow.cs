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

                _contentHostPanel = new DoubleBufferedPanel
                {
                    Name = "contentHostPanel",
                    Dock = DockStyle.None,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Location = Point.Empty
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

        private void RefreshData() // Full window refresh: re-initializes data, recreates ResourcePanel UIs
        {
            if (_project == null || _isClosing) return;
            try
            {
                EnsureResourcesInitialized(); // Calls InitializeFromSchedule (sets Planned & Actual=Planned in DailyData)

                foreach (var resource in _project.Resources)
                {
                    // This ensures Planned costs in DailyData are updated by ExpenseCalculator logic.
                    // InitializeFromSchedule already set initial Planned hours, costs, and Actual=Planned.
                    Helpers.ExpenseCalculator.CalculateResourceExpenses(resource);
                    resource.CalculateResourceTotals(); // Calculates aggregate totals on resource object
                }
                _project.CalculateTotals(); // Calculates overall project aggregates
                UpdateSummaryPanel();

                _mainPanel.SuspendLayout();
                _contentHostPanel.SuspendLayout();
                _contentHostPanel.Controls.Clear();
                _resourcePanels.Clear();

                int yPos = 10, xPos = 10;
                foreach (var resource in _project.Resources)
                {
                    var resourcePanel = new ResourcePanel(resource, this); // Calls PopulateData
                    resourcePanel.Location = new Point(xPos, yPos);
                    resourcePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    _contentHostPanel.Controls.Add(resourcePanel);
                    _resourcePanels.Add(resourcePanel);
                    yPos += resourcePanel.Height + 10;
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

        // --- INNER CLASS: ResourcePanel ---
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

            public ResourcePanel(CommissioningResource resource, CommissioningResultsWindow parentWindow)
            {
                this.DoubleBuffered = true;
                _resource = resource;
                _parentWindow = parentWindow;
                _editableCells = new Dictionary<string, EditableCell>();
                _plannedLabels = new Dictionary<string, Label>();
                _deltaLabels = new Dictionary<string, Label>();
                _subtotalAndTotalLabels = new Dictionary<string, Label>();

                this.BorderStyle = BorderStyle.FixedSingle;
                this.MinimumSize = new Size(1100, 350);
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
                UpdateHeaderText(); // Set initial header text
            }

            private void UpdateHeaderText()
            {
                int displayDaysCount = _resource.DailyData?.Count(kvp => kvp.Value.Date != DateTime.MinValue) ?? 0;
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

                if (_resource.DailyData == null || !_resource.DailyData.Any()) { _gridPanel.ResumeLayout(true); UpdateTotalLabel(); return; }
                var orderedDays = _resource.DailyData.Where(kvp => kvp.Value != null && kvp.Value.Date != DateTime.MinValue).OrderBy(kvp => kvp.Value.Date).ToList();
                if (!orderedDays.Any()) { SetupGridStructure(0); _gridPanel.ResumeLayout(true); UpdateTotalLabel(); return; }

                Helpers.ExpenseCalculator.CalculateResourceExpenses(_resource); // Ensures Planned costs in DailyData are current

                SetupGridStructure(orderedDays.Count);
                AddHeaders(orderedDays);
                AddDataRows(orderedDays);
                _gridPanel.ResumeLayout(true);
                UpdateTotalLabel();
                UpdateHeaderText(); // Update day count in header
            }

            private void SetupGridStructure(int dayCount)
            {
                _gridPanel.RowCount = 15; _gridPanel.ColumnCount = 1 + (Math.Max(0, dayCount) * 3);
                _gridPanel.ColumnStyles.Clear(); _gridPanel.RowStyles.Clear();
                if (_gridPanel.ColumnCount > 0) _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                for (int i = 0; i < dayCount; i++) { _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); }
                for (int i = 0; i < _gridPanel.RowCount; i++) _gridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            }

            private void AddHeaders(List<KeyValuePair<int, ResourceDayData>> orderedDays)
            {
                if (_gridPanel.ColumnCount == 0) return;
                AddControlToGrid(CreateLabel("", true), 0, 0);
                int c = 1; foreach (var dE in orderedDays) { if (c + 2 >= _gridPanel.ColumnCount) break; var h = CreateLabel(dE.Value.Date.ToString("ddd MMM dd"), true); h.BackColor = Color.FromArgb(230, 230, 250); h.BorderStyle = BorderStyle.FixedSingle; AddControlToGrid(h, c, 0); _gridPanel.SetColumnSpan(h, 3); AddControlToGrid(CreateLabel("Planned", true, Color.FromArgb(235, 245, 255)), c, 1); AddControlToGrid(CreateLabel("Actual", true, Color.FromArgb(235, 255, 235)), c + 1, 1); AddControlToGrid(CreateLabel("Delta", true, Color.FromArgb(255, 255, 235)), c + 2, 1); c += 3; }
            }

            private void AddControlToGrid(Control control, int column, int row) { if (column < _gridPanel.ColumnCount && row < _gridPanel.RowCount) _gridPanel.Controls.Add(control, column, row); }
            private Label CreateLabel(string text, bool isHeader, Color? backColor = null) { var lbl = new Label { Text = text, TextAlign = isHeader ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Font = isHeader ? new Font(this.Font, FontStyle.Bold) : this.Font }; if (backColor.HasValue) lbl.BackColor = backColor.Value; else if (isHeader) lbl.BackColor = SystemColors.ControlLight; return lbl; }
            private string FormatValue(decimal value, string dataType) { return IsHoursType(dataType) ? value.ToString("F1") : value.ToString("C2"); }
            internal bool IsHoursType(string dataType) { return dataType != null && (dataType.StartsWith("Service") || dataType.StartsWith("Travel")); }
            private void SetDeltaLabelColor(Label label, decimal delta) { label.ForeColor = delta == 0 ? Color.Black : (delta > 0 ? Color.DarkRed : Color.DarkGreen); } // Assuming delta > 0 is over budget/actual > planned

            private void AddDataRow(int gridRow, string rowLabelText, List<KeyValuePair<int, ResourceDayData>> orderedDays, string dataType)
            {
                var hLbl = CreateLabel(rowLabelText, false); hLbl.TextAlign = ContentAlignment.MiddleLeft; hLbl.Padding = new Padding(5, 0, 0, 0); AddControlToGrid(hLbl, 0, gridRow);
                int c = 1; foreach (var dE in orderedDays)
                {
                    if (c + 2 >= _gridPanel.ColumnCount) break; var dD = dE.Value; int dK = dE.Key; decimal pV = GetPlannedValue(dD, dataType); decimal aV = GetActualValue(dD, dataType); decimal dV = aV - pV;
                    var pL = CreateLabel(FormatValue(pV, dataType), false); if (IsHoursType(dataType) && pV != 0) pL.Font = new Font(this.Font, FontStyle.Bold); AddControlToGrid(pL, c, gridRow); _plannedLabels[$"P_{dataType}_{dK}"] = pL;
                    AddEditableCell(gridRow, c + 1, aV, dataType, dK, dD);
                    var dL = CreateLabel(FormatValue(dV, dataType), false); SetDeltaLabelColor(dL, dV); AddControlToGrid(dL, c + 2, gridRow); _deltaLabels[$"D_{dataType}_{dK}"] = dL;
                    c += 3;
                }
            }
            private void AddEditableCell(int gridRow, int gridCol, decimal value, string dataType, int dayKey, ResourceDayData dayData, Color? backColor = null) { var eC = new EditableCell(value, dataType, dayKey, dayData, _resource, this) { Dock = DockStyle.Fill }; if (backColor.HasValue) eC.BackColor = backColor.Value; _editableCells[$"A_{dataType}_{dayKey}"] = eC; AddControlToGrid(eC, gridCol, gridRow); }
            private void AddSubtotalRow(int gridRow, string lblTxt, List<KeyValuePair<int, ResourceDayData>> oD, bool isChg)
            {
                var rHL = CreateLabel(lblTxt, true); rHL.TextAlign = ContentAlignment.MiddleLeft; rHL.BackColor = Color.LightYellow; rHL.Padding = new Padding(5, 0, 0, 0); AddControlToGrid(rHL, 0, gridRow);
                int c = 1; foreach (var dE in oD)
                {
                    if (c + 2 >= _gridPanel.ColumnCount) break; var dD = dE.Value; int dK = dE.Key; decimal pS = CalculateSubtotal(dD, isChg, true); decimal aS = CalculateSubtotal(dD, isChg, false); decimal dS = aS - pS; string cL = lblTxt.Replace(" ", "").Replace("-", ""); string bK = $"SUB_{cL}_{dK}";
                    var pL = CreateLabel(pS.ToString("C2"), true); pL.BackColor = Color.LightYellow; AddControlToGrid(pL, c, gridRow); _subtotalAndTotalLabels[$"{bK}_P"] = pL;
                    var aL = CreateLabel(aS.ToString("C2"), true); aL.BackColor = Color.LightYellow; AddControlToGrid(aL, c + 1, gridRow); _subtotalAndTotalLabels[$"{bK}_A"] = aL;
                    var dL = CreateLabel(dS.ToString("C2"), true); dL.BackColor = Color.LightYellow; SetDeltaLabelColor(dL, dS); AddControlToGrid(dL, c + 2, gridRow); _subtotalAndTotalLabels[$"{bK}_D"] = dL;
                    c += 3;
                }
            }
            private void AddTotalsRow(int gridRow, string lblTxt, List<KeyValuePair<int, ResourceDayData>> oD)
            {
                var rHL = CreateLabel(lblTxt, true); rHL.TextAlign = ContentAlignment.MiddleLeft; rHL.BackColor = Color.Yellow; rHL.Padding = new Padding(5, 0, 0, 0); AddControlToGrid(rHL, 0, gridRow);
                int c = 1; foreach (var dE in oD)
                {
                    if (c + 2 >= _gridPanel.ColumnCount) break; var dD = dE.Value; int dK = dE.Key; decimal pT = CalculateDayTotal(dD, true); decimal aT = CalculateDayTotal(dD, false); decimal dT = aT - pT; string bK = $"TOTAL_Day_{dK}";
                    var pL = CreateLabel(pT.ToString("C2"), true); pL.BackColor = Color.Yellow; AddControlToGrid(pL, c, gridRow); _subtotalAndTotalLabels[$"{bK}_P"] = pL;
                    var aL = CreateLabel(aT.ToString("C2"), true); aL.BackColor = Color.Yellow; AddControlToGrid(aL, c + 1, gridRow); _subtotalAndTotalLabels[$"{bK}_A"] = aL;
                    var dL = CreateLabel(dT.ToString("C2"), true); dL.BackColor = Color.Yellow; SetDeltaLabelColor(dL, dT); AddControlToGrid(dL, c + 2, gridRow); _subtotalAndTotalLabels[$"{bK}_D"] = dL;
                    c += 3;
                }
            }
            private void AddDataRows(List<KeyValuePair<int, ResourceDayData>> orderedDays) { int r = 2; AddDataRow(r++, "Service (Reg)", orderedDays, "ServiceReg"); AddDataRow(r++, "Service (OT)", orderedDays, "ServiceOT"); AddDataRow(r++, "Service (Premium)", orderedDays, "ServicePrem"); AddDataRow(r++, "Travel (Reg)", orderedDays, "TravelReg"); AddDataRow(r++, "Travel (OT)", orderedDays, "TravelOT"); AddDataRow(r++, "Travel (Premium)", orderedDays, "TravelPrem"); AddSubtotalRow(r++, "Subtotals - Charges", orderedDays, true); AddDataRow(r++, "Mileage", orderedDays, "Mileage"); AddDataRow(r++, "Per Diem", orderedDays, "PerDiem"); AddDataRow(r++, "Flight", orderedDays, "Flight"); AddDataRow(r++, "Car Rental", orderedDays, "CarRental"); AddDataRow(r++, "Hotel", orderedDays, "Hotel"); AddSubtotalRow(r++, "Subtotals - Expenses", orderedDays, false); AddTotalsRow(r++, "Totals", orderedDays); }
            internal decimal GetPlannedValue(ResourceDayData dD, string dT) { if (dD == null) return 0; if (dT == "ServiceReg") return dD.PlannedRegularLabourHours; if (dT == "ServiceOT") return dD.PlannedOvertimeLabourHours; if (dT == "ServicePrem") return dD.PlannedPremiumLabourHours; if (dT == "TravelReg") return dD.PlannedRegularTravelHours; if (dT == "TravelOT") return dD.PlannedOvertimeTravelHours; if (dT == "TravelPrem") return dD.PlannedPremiumTravelHours; if (dT == "Mileage") return dD.PlannedMileageCost; if (dT == "PerDiem") return dD.PlannedPerDiemCost; if (dT == "Flight") return dD.PlannedFlightCost; if (dT == "CarRental") return dD.PlannedRentalCarCost; if (dT == "Hotel") return dD.PlannedHotelCost; return 0; }
            internal decimal GetActualValue(ResourceDayData dD, string dT) { if (dD == null) return 0; if (dT == "ServiceReg") return dD.ActualRegularLabourHours; if (dT == "ServiceOT") return dD.ActualOvertimeLabourHours; if (dT == "ServicePrem") return dD.ActualPremiumLabourHours; if (dT == "TravelReg") return dD.ActualRegularTravelHours; if (dT == "TravelOT") return dD.ActualOvertimeTravelHours; if (dT == "TravelPrem") return dD.ActualPremiumTravelHours; if (dT == "Mileage") return dD.ActualMileageCost; if (dT == "PerDiem") return dD.ActualPerDiemCost; if (dT == "Flight") return dD.ActualFlightCost; if (dT == "CarRental") return dD.ActualRentalCarCost; if (dT == "Hotel") return dD.ActualHotelCost; return 0; }
            private decimal GetRegularLabourRate() { decimal r = _resource.RegularLabourRate, d = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumLabourRate * d : r * d; }
            private decimal GetOvertimeLabourRate() { decimal r = _resource.OvertimeLabourRate, d = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumLabourRate * d : r * d; }
            private decimal GetPremiumLabourRate() { return _resource.PremiumLabourRate * (1 - (_resource.DiscountPercent / 100m)); }
            private decimal GetRegularTravelRate() { decimal r = _resource.RegularTravelRate, d = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumTravelRate * d : r * d; }
            private decimal GetOvertimeTravelRate() { decimal r = _resource.OvertimeTravelRate, d = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumTravelRate * d : r * d; }
            private decimal GetPremiumTravelRate() { return _resource.PremiumTravelRate * (1 - (_resource.DiscountPercent / 100m)); }
            private decimal CalculateSubtotal(ResourceDayData dD, bool iC, bool iP) { if (dD == null) return 0; if (iC) return iP ? (dD.PlannedRegularLabourHours * GetRegularLabourRate()) + (dD.PlannedOvertimeLabourHours * GetOvertimeLabourRate()) + (dD.PlannedPremiumLabourHours * GetPremiumLabourRate()) + (dD.PlannedRegularTravelHours * GetRegularTravelRate()) + (dD.PlannedOvertimeTravelHours * GetOvertimeTravelRate()) + (dD.PlannedPremiumTravelHours * GetPremiumTravelRate()) : (dD.ActualRegularLabourHours * GetRegularLabourRate()) + (dD.ActualOvertimeLabourHours * GetOvertimeLabourRate()) + (dD.ActualPremiumLabourHours * GetPremiumLabourRate()) + (dD.ActualRegularTravelHours * GetRegularTravelRate()) + (dD.ActualOvertimeTravelHours * GetOvertimeTravelRate()) + (dD.ActualPremiumTravelHours * GetPremiumTravelRate()); else return iP ? dD.PlannedMileageCost + dD.PlannedPerDiemCost + dD.PlannedFlightCost + dD.PlannedRentalCarCost + dD.PlannedHotelCost : dD.ActualMileageCost + dD.ActualPerDiemCost + dD.ActualFlightCost + dD.ActualRentalCarCost + dD.ActualHotelCost; }
            private decimal CalculateDayTotal(ResourceDayData dD, bool iP) { if (dD == null) return 0; return CalculateSubtotal(dD, true, iP) + CalculateSubtotal(dD, false, iP); }
            internal void UpdateTotalLabel() { decimal t = 0; if (_resource?.DailyData != null) foreach (var dE in _resource.DailyData.Values) if (dE != null) t += CalculateDayTotal(dE, false); _totalLabel.Text = $"Total: {t:C2}"; }

            private void RefreshGridDisplay()
            {
                if (_resource == null || _resource.DailyData == null || !_resource.DailyData.Any() || _gridPanel == null) return;
                _gridPanel.SuspendLayout();
                var orderedDays = _resource.DailyData.Where(kvp => kvp.Value != null && kvp.Value.Date != DateTime.MinValue).OrderBy(kvp => kvp.Value.Date).ToList();
                int expectedColumnCount = 1 + (orderedDays.Count * 3);

                if (_gridPanel.ColumnCount != expectedColumnCount || (!_editableCells.Any() && orderedDays.Any()) || (_plannedLabels.Count == 0 && orderedDays.Any()))
                {
                    _gridPanel.Controls.Clear();
                    _editableCells.Clear(); _plannedLabels.Clear(); _deltaLabels.Clear(); _subtotalAndTotalLabels.Clear();
                    PopulateData();
                    _gridPanel.ResumeLayout(true);
                    return;
                }

                string[] dataTypes = { "ServiceReg", "ServiceOT", "ServicePrem", "TravelReg", "TravelOT", "TravelPrem", "Mileage", "PerDiem", "Flight", "CarRental", "Hotel" };
                foreach (string dT in dataTypes)
                {
                    foreach (var dE in orderedDays)
                    {
                        ResourceDayData dD = dE.Value; int dK = dE.Key;
                        decimal pV = GetPlannedValue(dD, dT); decimal aV = GetActualValue(dD, dT); decimal dVal = aV - pV;
                        if (_plannedLabels.TryGetValue($"P_{dT}_{dK}", out Label pL)) { pL.Text = FormatValue(pV, dT); pL.Font = (IsHoursType(dT) && pV != 0) ? new Font(this.Font, FontStyle.Bold) : this.Font; }
                        if (_editableCells.TryGetValue($"A_{dT}_{dK}", out EditableCell aC)) { aC.UpdateValueFromDayData(); }
                        if (_deltaLabels.TryGetValue($"D_{dT}_{dK}", out Label dL)) { dL.Text = FormatValue(dVal, dT); SetDeltaLabelColor(dL, dVal); }
                    }
                }
                UpdateSubtotalOrTotalRowLabels_InPlace(orderedDays, "Subtotals - Charges", true);
                UpdateSubtotalOrTotalRowLabels_InPlace(orderedDays, "Subtotals - Expenses", false);
                UpdateSubtotalOrTotalRowLabels_InPlace(orderedDays, "Totals", null);
                _gridPanel.ResumeLayout(true);
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