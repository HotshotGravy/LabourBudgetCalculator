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
        private Panel _mainPanel;
        private Button _exportButton;
        private Button _closeButton;
        private List<ResourcePanel> _resourcePanels = new List<ResourcePanel>();
        private bool _isClosing = false;

        public CommissioningResultsWindow(CommissioningProject project)
        {
            InitializeComponent();
            _project = project ?? throw new ArgumentNullException(nameof(project));

            this.Text = string.Format("Results - {0}", _project.ProjectName);
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1200, 600);

            // Delay initialization to avoid antivirus triggers
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

                // Main container - create statically
                var mainContainer = new TableLayoutPanel();
                mainContainer.Dock = DockStyle.Fill;
                mainContainer.RowCount = 3;
                mainContainer.ColumnCount = 1;
                mainContainer.Padding = new Padding(10);

                // Configure rows
                mainContainer.RowStyles.Clear();
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

                // Create panels
                _summaryPanel = CreateSummaryPanel();
                mainContainer.Controls.Add(_summaryPanel, 0, 0);

                _mainPanel = new Panel();
                _mainPanel.Dock = DockStyle.Fill;
                _mainPanel.AutoScroll = true;
                _mainPanel.BorderStyle = BorderStyle.FixedSingle;
                mainContainer.Controls.Add(_mainPanel, 0, 1);

                var buttonPanel = CreateButtonPanel();
                mainContainer.Controls.Add(buttonPanel, 0, 2);

                this.Controls.Add(mainContainer);
                this.ResumeLayout(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting up UI: " + ex.Message);
            }
        }

        private TableLayoutPanel CreateSummaryPanel()
        {
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.RowCount = 2;
            panel.ColumnCount = 5;
            panel.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;

            // Configure columns
            panel.ColumnStyles.Clear();
            for (int i = 0; i < 5; i++)
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            // Add headers
            string[] headers = { "Quoted", "Planned", "Current", "Forecast", "Delta (vs Quoted)" };
            for (int i = 0; i < headers.Length; i++)
            {
                var label = new Label();
                label.Text = headers[i];
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Dock = DockStyle.Fill;
                label.Font = new Font(this.Font, FontStyle.Bold);
                label.BackColor = SystemColors.ControlLight;
                panel.Controls.Add(label, i, 0);
            }

            // Add value labels
            string[] names = { "lblQuoted", "lblPlanned", "lblCurrent", "lblForecast", "lblDelta" };
            for (int i = 0; i < names.Length; i++)
            {
                var label = new Label();
                label.Name = names[i];
                label.Text = "$0.00";
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Dock = DockStyle.Fill;
                label.Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold);
                panel.Controls.Add(label, i, 1);
            }

            return panel;
        }

        private Panel CreateButtonPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(5);

            _exportButton = new Button();
            _exportButton.Text = "Export Details (CSV)";
            _exportButton.Size = new Size(150, 30);
            _exportButton.Location = new Point(10, 10);
            _exportButton.Click += ExportButton_Click;

            _closeButton = new Button();
            _closeButton.Text = "Close";
            _closeButton.Size = new Size(100, 30);
            _closeButton.Location = new Point(170, 10);
            _closeButton.Click += CloseButton_Click;

            panel.Controls.Add(_exportButton);
            panel.Controls.Add(_closeButton);

            return panel;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RefreshData()
        {
            if (_project == null || _isClosing) return;

            try
            {
                EnsureResourcesInitialized();
                _project.CalculateTotals();
                UpdateSummaryPanel();

                _mainPanel.SuspendLayout();
                _mainPanel.Controls.Clear();
                _resourcePanels.Clear();

                int yPos = 10;
                foreach (var resource in _project.Resources)
                {
                    var resourcePanel = new ResourcePanel(resource, this);
                    resourcePanel.Location = new Point(10, yPos);
                    resourcePanel.Width = _mainPanel.ClientSize.Width - 40;
                    resourcePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                    _mainPanel.Controls.Add(resourcePanel);
                    _resourcePanels.Add(resourcePanel);

                    yPos += resourcePanel.Height + 10;
                }

                _mainPanel.ResumeLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RefreshData error: " + ex.Message);
            }
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
                var lblQuoted = _summaryPanel.Controls["lblQuoted"] as Label;
                var lblPlanned = _summaryPanel.Controls["lblPlanned"] as Label;
                var lblCurrent = _summaryPanel.Controls["lblCurrent"] as Label;
                var lblForecast = _summaryPanel.Controls["lblForecast"] as Label;
                var lblDelta = _summaryPanel.Controls["lblDelta"] as Label;

                if (lblQuoted != null) lblQuoted.Text = FormatCurrency(_project.InitialEstimate);
                if (lblPlanned != null) lblPlanned.Text = FormatCurrency(_project.PlannedTotal);
                if (lblCurrent != null) lblCurrent.Text = FormatCurrency(_project.CurrentTotal);
                if (lblForecast != null) lblForecast.Text = FormatCurrency(_project.ForecastTotal);

                if (lblDelta != null)
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

        private string FormatCurrency(decimal value)
        {
            return value.ToString("C2");
        }

        internal void SaveData()
        {
            if (_isClosing) return;

            try
            {
                _project.CalculateTotals();
                CommissioningDataManager.Instance.SaveCurrentProject();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SaveData error: " + ex.Message);
            }
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
            try
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "CSV files (*.csv)|*.csv";
                    saveDialog.FileName = string.Format("{0}_Results_{1}.csv",
                        _project.ProjectName,
                        DateTime.Now.ToString("yyyyMMdd"));

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportToCSV(saveDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export error: " + ex.Message);
            }
        }

        private void ExportToCSV(string fileName)
        {
            try
            {
                var sb = new StringBuilder();

                // Headers
                sb.AppendLine("Resource,Date,Day,Service Reg Hours,Service OT Hours,Service Premium Hours," +
                             "Travel Reg Hours,Travel OT Hours,Travel Premium Hours," +
                             "Mileage,Per Diem,Flight,Car Rental,Hotel,Total");

                // Data
                foreach (var resourcePanel in _resourcePanels)
                {
                    resourcePanel.AppendCSVData(sb);
                }

                File.WriteAllText(fileName, sb.ToString());

                MessageBox.Show("Export completed successfully!", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting data: " + ex.Message, "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isClosing = true;
            SaveData();
            base.OnFormClosing(e);
        }

        public void UpdateResults(CommissioningProject project)
        {
            _project = project;
            RefreshData();
        }
    }

    // Resource Panel Class
    internal class ResourcePanel : Panel
    {
        private CommissioningResource _resource;
        private CommissioningResultsWindow _parentWindow;
        private TableLayoutPanel _gridPanel;
        private Label _headerLabel;
        private Label _totalLabel;
        private bool _isExpanded = true;
        private Button _toggleButton;
        private Dictionary<string, EditableCell> _editableCells;

        public ResourcePanel(CommissioningResource resource, CommissioningResultsWindow parentWindow)
        {
            _resource = resource;
            _parentWindow = parentWindow;
            _editableCells = new Dictionary<string, EditableCell>();

            this.BorderStyle = BorderStyle.FixedSingle;
            this.MinimumSize = new Size(0, 400); 
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Initialize();
        }

        private void Initialize()
        {
            CreateHeader();
            CreateGrid();
            PopulateData();
        }

        private void CreateHeader()
        {
            var headerPanel = new Panel();
            headerPanel.Height = 30;
            headerPanel.Dock = DockStyle.Top;
            headerPanel.BackColor = SystemColors.ActiveCaption;

            _toggleButton = new Button();
            _toggleButton.Text = "−";
            _toggleButton.Size = new Size(25, 25);
            _toggleButton.Location = new Point(5, 2);
            _toggleButton.Click += ToggleButton_Click;

            string headerText = string.Format("Resource: {0} ({1} days)",
                _resource.TechnicianName,
                _resource.DailyData?.Count ?? 0);

            _headerLabel = new Label();
            _headerLabel.Text = headerText;
            _headerLabel.Location = new Point(35, 5);
            _headerLabel.AutoSize = true;
            _headerLabel.Font = new Font(this.Font, FontStyle.Bold);
            _headerLabel.ForeColor = SystemColors.ActiveCaptionText;

            _totalLabel = new Label();
            _totalLabel.Text = "Total: $0.00";
            _totalLabel.AutoSize = true;
            _totalLabel.Font = new Font(this.Font, FontStyle.Bold);
            _totalLabel.ForeColor = SystemColors.ActiveCaptionText;
            _totalLabel.Location = new Point(this.Width - 150, 5);

            headerPanel.Controls.Add(_toggleButton);
            headerPanel.Controls.Add(_headerLabel);
            headerPanel.Controls.Add(_totalLabel);

            this.Controls.Add(headerPanel);
        }

        private void CreateGrid()
        {
            _gridPanel = new TableLayoutPanel();
            _gridPanel.Location = new Point(0, 30);
            _gridPanel.AutoSize = true;
            _gridPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _gridPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            this.Controls.Add(_gridPanel);
        }

        private void ToggleButton_Click(object sender, EventArgs e)
        {
            _isExpanded = !_isExpanded;
            _toggleButton.Text = _isExpanded ? "−" : "+";
            _gridPanel.Visible = _isExpanded;

            if (!_isExpanded)
            {
                this.Height = 30;
            }
            else
            {
                this.AutoSize = true;
            }
        }

        private void PopulateData()
        {
            if (_resource.DailyData == null || !_resource.DailyData.Any()) return;

            var orderedDays = _resource.DailyData
                .Where(kvp => kvp.Value.Date != DateTime.MinValue)
                .OrderBy(kvp => kvp.Value.Date)
                .ToList();

            if (orderedDays.Count == 0) return;

            SetupGridStructure(orderedDays.Count);
            AddHeaders(orderedDays);
            AddDataRows(orderedDays);
            UpdateTotalLabel();
        }

        private void SetupGridStructure(int dayCount)
        {
            int columnCount = 1 + (dayCount * 3);
            _gridPanel.ColumnCount = columnCount;
            _gridPanel.RowCount = 15;

            _gridPanel.ColumnStyles.Clear();
            _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

            for (int i = 0; i < _gridPanel.RowCount; i++)
            {
                _gridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); 
            }

            for (int i = 0; i < dayCount; i++)
            {
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            }
        }

        private void AddHeaders(List<KeyValuePair<int, ResourceDayData>> orderedDays)
        {
            AddCell(0, 0, "", true);

            int col = 1;
            foreach (var dayEntry in orderedDays)
            {
                var date = dayEntry.Value.Date;
                var dayHeader = CreateLabel(date.ToString("ddd MMM dd"), true);
                dayHeader.BackColor = SystemColors.ControlLight;
                _gridPanel.Controls.Add(dayHeader, col, 0);
                _gridPanel.SetColumnSpan(dayHeader, 3);

                AddCell(1, col, "Planned", true, Color.LightGray);
                AddCell(1, col + 1, "Actual", true, Color.LightGray);
                AddCell(1, col + 2, "Delta", true, Color.LightGray);

                col += 3;
            }
        }

        private void AddDataRows(List<KeyValuePair<int, ResourceDayData>> orderedDays)
        {
            int row = 2;

            // Service rows
            AddDataRow(row++, "Service (Reg)", orderedDays, "ServiceReg");
            AddDataRow(row++, "Service (OT)", orderedDays, "ServiceOT");
            AddDataRow(row++, "Service (Premium)", orderedDays, "ServicePrem");

            // Travel rows
            AddDataRow(row++, "Travel (Reg)", orderedDays, "TravelReg");
            AddDataRow(row++, "Travel (OT)", orderedDays, "TravelOT");
            AddDataRow(row++, "Travel (Premium)", orderedDays, "TravelPrem");

            // Subtotal
            AddSubtotalRow(row++, "Subtotals - Charges", orderedDays, true);

            // Expenses
            AddDataRow(row++, "Mileage", orderedDays, "Mileage");
            AddDataRow(row++, "Per Diem", orderedDays, "PerDiem");
            AddDataRow(row++, "Flight", orderedDays, "Flight");
            AddDataRow(row++, "Car Rental, Taxis, Train", orderedDays, "CarRental");
            AddDataRow(row++, "Hotel", orderedDays, "Hotel");

            // Expense subtotal
            AddSubtotalRow(row++, "Subtotals - Charges", orderedDays, false);

            // Total
            AddTotalsRow(row, orderedDays);
        }

        private void AddCell(int row, int col, string text, bool isHeader, Color? backColor = null)
        {
            var label = CreateLabel(text, isHeader);
            if (backColor.HasValue)
                label.BackColor = backColor.Value;
            else if (isHeader)
                label.BackColor = SystemColors.ControlLight;

            _gridPanel.Controls.Add(label, col, row);
        }

        private Label CreateLabel(string text, bool isHeader)
        {
            var label = new Label();
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Dock = DockStyle.Fill;
            if (isHeader)
                label.Font = new Font(this.Font, FontStyle.Bold);
            return label;
        }

        private void AddDataRow(int row, string rowLabel, List<KeyValuePair<int, ResourceDayData>> orderedDays, string dataType)
        {
            var label = new Label();
            label.Text = rowLabel;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            label.Padding = new Padding(5, 0, 0, 0);
            _gridPanel.Controls.Add(label, 0, row);

            int col = 1;
            foreach (var dayEntry in orderedDays)
            {
                var dayData = dayEntry.Value;
                decimal plannedValue = GetPlannedValue(dayData, dataType);
                decimal actualValue = GetActualValue(dayData, dataType);
                decimal delta = actualValue - plannedValue;

                AddValueCell(row, col, plannedValue, false);
                AddEditableCell(row, col + 1, actualValue, dataType, dayEntry.Key, dayData);
                AddDeltaCell(row, col + 2, delta);

                col += 3;
            }
        }

        private void AddSubtotalRow(int row, string label, List<KeyValuePair<int, ResourceDayData>> orderedDays, bool isCharges)
        {
            var rowLabel = CreateLabel(label, true);
            rowLabel.TextAlign = ContentAlignment.MiddleLeft;
            rowLabel.BackColor = Color.LightYellow;
            rowLabel.Padding = new Padding(5, 0, 0, 0);
            _gridPanel.Controls.Add(rowLabel, 0, row);

            int col = 1;
            foreach (var dayEntry in orderedDays)
            {
                var dayData = dayEntry.Value;
                decimal plannedSubtotal = CalculateSubtotal(dayData, isCharges, true);
                decimal actualSubtotal = CalculateSubtotal(dayData, isCharges, false);
                decimal delta = actualSubtotal - plannedSubtotal;

                AddValueCell(row, col, plannedSubtotal, true, Color.LightYellow);
                AddValueCell(row, col + 1, actualSubtotal, true, Color.LightYellow);
                AddDeltaCell(row, col + 2, delta, Color.LightYellow);

                col += 3;
            }
        }

        private void AddTotalsRow(int row, List<KeyValuePair<int, ResourceDayData>> orderedDays)
        {
            var rowLabel = CreateLabel("Totals", true);
            rowLabel.TextAlign = ContentAlignment.MiddleLeft;
            rowLabel.BackColor = Color.Yellow;
            rowLabel.Padding = new Padding(5, 0, 0, 0);
            _gridPanel.Controls.Add(rowLabel, 0, row);

            int col = 1;
            foreach (var dayEntry in orderedDays)
            {
                var dayData = dayEntry.Value;
                decimal plannedTotal = CalculateDayTotal(dayData, true);
                decimal actualTotal = CalculateDayTotal(dayData, false);
                decimal delta = actualTotal - plannedTotal;

                AddValueCell(row, col, plannedTotal, true, Color.Yellow);
                AddValueCell(row, col + 1, actualTotal, true, Color.Yellow);
                AddDeltaCell(row, col + 2, delta, Color.Yellow);

                col += 3;
            }
        }

        private void AddValueCell(int row, int col, decimal value, bool isBold, Color? backColor = null)
        {
            var label = new Label();

            // Check if this is a service/travel row (rows 2-7) to format as hours
            if (row >= 2 && row <= 7)
            {
                label.Text = value.ToString("F1"); // Format as hours with 1 decimal
            }
            else
            {
                label.Text = value.ToString("C2"); // Format as currency
            }

            label.TextAlign = ContentAlignment.MiddleRight;
            label.Dock = DockStyle.Fill;
            if (isBold)
                label.Font = new Font(this.Font, FontStyle.Bold);
            if (backColor.HasValue)
                label.BackColor = backColor.Value;

            _gridPanel.Controls.Add(label, col, row);
        }

        private void AddEditableCell(int row, int col, decimal value, string dataType, int dayKey, ResourceDayData dayData)
        {
            var editableCell = new EditableCell(value, dataType, dayKey, dayData, _resource, this);
            editableCell.Dock = DockStyle.Fill;

            string cellKey = string.Format("{0}_{1}", dayKey, dataType);
            _editableCells[cellKey] = editableCell;

            _gridPanel.Controls.Add(editableCell, col, row);
        }

        private void AddDeltaCell(int row, int col, decimal delta, Color? backColor = null)
        {
            var label = new Label();
            label.Text = delta.ToString("C2");
            label.TextAlign = ContentAlignment.MiddleRight;
            label.Dock = DockStyle.Fill;
            label.ForeColor = delta == 0 ? Color.Black : (delta > 0 ? Color.Red : Color.Green);
            if (backColor.HasValue)
                label.BackColor = backColor.Value;

            _gridPanel.Controls.Add(label, col, row);
        }

        // Continuation of ResourcePanel class methods

        private decimal GetPlannedValue(ResourceDayData dayData, string dataType)
        {
            if (dataType == "ServiceReg")
                return dayData.PlannedRegularLabourHours; // Just hours, not × rate
            else if (dataType == "ServiceOT")
                return dayData.PlannedOvertimeLabourHours; // Just hours
            else if (dataType == "ServicePrem")
                return dayData.PlannedPremiumLabourHours; // Just hours
            else if (dataType == "TravelReg")
                return dayData.PlannedRegularTravelHours; // Just hours
            else if (dataType == "TravelOT")
                return dayData.PlannedOvertimeTravelHours; // Just hours
            else if (dataType == "TravelPrem")
                return dayData.PlannedPremiumTravelHours; // Just hours
            else if (dataType == "Mileage")
                return dayData.PlannedMileageCost;
            else if (dataType == "PerDiem")
                return dayData.PlannedPerDiemCost;
            else if (dataType == "Flight")
                return dayData.PlannedFlightCost;
            else if (dataType == "CarRental")
                return dayData.PlannedRentalCarCost;
            else if (dataType == "Hotel")
                return dayData.PlannedHotelCost;
            else
                return 0;
        }

        private decimal GetActualValue(ResourceDayData dayData, string dataType)
        {
            if (dataType == "ServiceReg")
                return dayData.ActualRegularLabourHours; // Just hours
            else if (dataType == "ServiceOT")
                return dayData.ActualOvertimeLabourHours; // Just hours
            else if (dataType == "ServicePrem")
                return dayData.ActualPremiumLabourHours; // Just hours
            else if (dataType == "TravelReg")
                return dayData.ActualRegularTravelHours; // Just hours
            else if (dataType == "TravelOT")
                return dayData.ActualOvertimeTravelHours; // Just hours
            else if (dataType == "TravelPrem")
                return dayData.ActualPremiumTravelHours; // Just hours
            else if (dataType == "Mileage")
                return dayData.ActualMileageCost;
            else if (dataType == "PerDiem")
                return dayData.ActualPerDiemCost;
            else if (dataType == "Flight")
                return dayData.ActualFlightCost;
            else if (dataType == "CarRental")
                return dayData.ActualRentalCarCost;
            else if (dataType == "Hotel")
                return dayData.ActualHotelCost;
            else
                return 0;
        }

        private decimal GetRegularLabourRate()
        {
            decimal rate = _resource.RegularLabourRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return _resource.IsEmergency ? _resource.PremiumLabourRate * discountFactor : rate * discountFactor;
        }

        private decimal GetOvertimeLabourRate()
        {
            decimal rate = _resource.OvertimeLabourRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return _resource.IsEmergency ? _resource.PremiumLabourRate * discountFactor : rate * discountFactor;
        }

        private decimal GetPremiumLabourRate()
        {
            decimal rate = _resource.PremiumLabourRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return rate * discountFactor;
        }

        private decimal GetRegularTravelRate()
        {
            decimal rate = _resource.RegularTravelRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return _resource.IsEmergency ? _resource.PremiumTravelRate * discountFactor : rate * discountFactor;
        }

        private decimal GetOvertimeTravelRate()
        {
            decimal rate = _resource.OvertimeTravelRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return _resource.IsEmergency ? _resource.PremiumTravelRate * discountFactor : rate * discountFactor;
        }

        private decimal GetPremiumTravelRate()
        {
            decimal rate = _resource.PremiumTravelRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return rate * discountFactor;
        }

        private decimal CalculateSubtotal(ResourceDayData dayData, bool isCharges, bool isPlanned)
        {
            if (isCharges)
            {
                if (isPlanned)
                {
                    return (dayData.PlannedRegularLabourHours * GetRegularLabourRate()) +
                           (dayData.PlannedOvertimeLabourHours * GetOvertimeLabourRate()) +
                           (dayData.PlannedPremiumLabourHours * GetPremiumLabourRate()) +
                           (dayData.PlannedRegularTravelHours * GetRegularTravelRate()) +
                           (dayData.PlannedOvertimeTravelHours * GetOvertimeTravelRate()) +
                           (dayData.PlannedPremiumTravelHours * GetPremiumTravelRate());
                }
                else
                {
                    return (dayData.ActualRegularLabourHours * GetRegularLabourRate()) +
                           (dayData.ActualOvertimeLabourHours * GetOvertimeLabourRate()) +
                           (dayData.ActualPremiumLabourHours * GetPremiumLabourRate()) +
                           (dayData.ActualRegularTravelHours * GetRegularTravelRate()) +
                           (dayData.ActualOvertimeTravelHours * GetOvertimeTravelRate()) +
                           (dayData.ActualPremiumTravelHours * GetPremiumTravelRate());
                }
            }
            else
            {
                if (isPlanned)
                {
                    return dayData.PlannedMileageCost + dayData.PlannedPerDiemCost +
                           dayData.PlannedFlightCost + dayData.PlannedRentalCarCost +
                           dayData.PlannedHotelCost;
                }
                else
                {
                    return dayData.ActualMileageCost + dayData.ActualPerDiemCost +
                           dayData.ActualFlightCost + dayData.ActualRentalCarCost +
                           dayData.ActualHotelCost;
                }
            }
        }

        private decimal CalculateDayTotal(ResourceDayData dayData, bool isPlanned)
        {
            return CalculateSubtotal(dayData, true, isPlanned) + CalculateSubtotal(dayData, false, isPlanned);
        }

        internal void UpdateTotalLabel()
        {
            decimal total = 0;
            foreach (var dayEntry in _resource.DailyData.Values)
            {
                total += CalculateDayTotal(dayEntry, false);
            }
            _totalLabel.Text = string.Format("Total: {0:C2}", total);
        }

        internal void NotifyValueChanged()
        {
            UpdateTotalLabel();
            _parentWindow.MarkProjectDirty();
            _parentWindow.SaveData();
        }

        internal void AppendCSVData(StringBuilder sb)
        {
            if (_resource.DailyData == null || !_resource.DailyData.Any()) return;

            var orderedDays = _resource.DailyData
                .Where(kvp => kvp.Value.Date != DateTime.MinValue)
                .OrderBy(kvp => kvp.Value.Date)
                .ToList();

            foreach (var dayEntry in orderedDays)
            {
                var dayData = dayEntry.Value;
                var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                    _resource.TechnicianName,
                    dayData.Date.ToShortDateString(),
                    dayData.Date.ToString("dddd"),
                    dayData.ActualRegularLabourHours,
                    dayData.ActualOvertimeLabourHours,
                    dayData.ActualPremiumLabourHours,
                    dayData.ActualRegularTravelHours,
                    dayData.ActualOvertimeTravelHours,
                    dayData.ActualPremiumTravelHours,
                    dayData.ActualMileageCost,
                    dayData.ActualPerDiemCost,
                    dayData.ActualFlightCost,
                    dayData.ActualRentalCarCost,
                    dayData.ActualHotelCost,
                    CalculateDayTotal(dayData, false)
                );
                sb.AppendLine(line);
            }
        }
    }

    // EditableCell Class
    internal class EditableCell : UserControl
    {
        private Label _label;
        private TextBox _textBox;
        private decimal _value;
        private string _dataType;
        private int _dayKey;
        private ResourceDayData _dayData;
        private CommissioningResource _resource;
        private ResourcePanel _parentPanel;
        private bool _isEditing = false;

        public EditableCell(decimal value, string dataType, int dayKey, ResourceDayData dayData,
                          CommissioningResource resource, ResourcePanel parentPanel)
        {
            _value = value;
            _dataType = dataType;
            _dayKey = dayKey;
            _dayData = dayData;
            _resource = resource;
            _parentPanel = parentPanel;

            InitializeControls();
        }

        private void InitializeControls()
        {
            _label = new Label();

            if (_dataType.StartsWith("Service") || _dataType.StartsWith("Travel"))
            {
                _label.Text = _value.ToString("F1"); // Hours format
            }
            else
            {
                _label.Text = _value.ToString("C2"); // Currency format
            }

            _label.Text = _value.ToString("C2");
            _label.TextAlign = ContentAlignment.MiddleRight;
            _label.Dock = DockStyle.Fill;
            _label.Cursor = Cursors.Hand;
            _label.MouseClick += Label_MouseClick;

            _textBox = new TextBox();
            _textBox.Text = _value.ToString("F2");
            _textBox.TextAlign = HorizontalAlignment.Right;
            _textBox.Dock = DockStyle.Fill;
            _textBox.Visible = false;
            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.Leave += TextBox_Leave;
            _textBox.KeyPress += TextBox_KeyPress;

            this.Controls.Add(_label);
            this.Controls.Add(_textBox);
        }

        private void Label_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                StartEdit();
            }
        }

        private void StartEdit()
        {
            _isEditing = true;
            _label.Visible = false;
            _textBox.Visible = true;
            _textBox.SelectAll();
            _textBox.Focus();
        }

        private void EndEdit(bool save)
        {
            if (!_isEditing) return;

            _isEditing = false;

            if (save)
            {
                decimal newValue;
                if (decimal.TryParse(_textBox.Text, out newValue))
                {
                    if (newValue != _value)
                    {
                        _value = newValue;
                        UpdateDayData(newValue);
                        _label.Text = _value.ToString("C2");
                        _parentPanel.NotifyValueChanged();
                    }
                }
            }

            _textBox.Visible = false;
            _label.Visible = true;
        }

        private void UpdateDayData(decimal newValue)
        {
            // For service and travel, the value IS the hours (not cost)
            if (_dataType == "ServiceReg")
                _dayData.ActualRegularLabourHours = newValue;
            else if (_dataType == "ServiceOT")
                _dayData.ActualOvertimeLabourHours = newValue;
            else if (_dataType == "ServicePrem")
                _dayData.ActualPremiumLabourHours = newValue;
            else if (_dataType == "TravelReg")
                _dayData.ActualRegularTravelHours = newValue;
            else if (_dataType == "TravelOT")
                _dayData.ActualOvertimeTravelHours = newValue;
            else if (_dataType == "TravelPrem")
                _dayData.ActualPremiumTravelHours = newValue;
            else if (_dataType == "Mileage")
                _dayData.ActualMileageCost = newValue;
            else if (_dataType == "PerDiem")
                _dayData.ActualPerDiemCost = newValue;
            else if (_dataType == "Flight")
                _dayData.ActualFlightCost = newValue;
            else if (_dataType == "CarRental")
                _dayData.ActualRentalCarCost = newValue;
            else if (_dataType == "Hotel")
                _dayData.ActualHotelCost = newValue;
        }

        private decimal GetRegularLabourRate()
        {
            decimal rate = _resource.RegularLabourRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return _resource.IsEmergency ? _resource.PremiumLabourRate * discountFactor : rate * discountFactor;
        }

        private decimal GetOvertimeLabourRate()
        {
            decimal rate = _resource.OvertimeLabourRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return _resource.IsEmergency ? _resource.PremiumLabourRate * discountFactor : rate * discountFactor;
        }

        private decimal GetPremiumLabourRate()
        {
            decimal rate = _resource.PremiumLabourRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return rate * discountFactor;
        }

        private decimal GetRegularTravelRate()
        {
            decimal rate = _resource.RegularTravelRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return _resource.IsEmergency ? _resource.PremiumTravelRate * discountFactor : rate * discountFactor;
        }

        private decimal GetOvertimeTravelRate()
        {
            decimal rate = _resource.OvertimeTravelRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return _resource.IsEmergency ? _resource.PremiumTravelRate * discountFactor : rate * discountFactor;
        }

        private decimal GetPremiumTravelRate()
        {
            decimal rate = _resource.PremiumTravelRate;
            decimal discountFactor = 1 - (_resource.DiscountPercent / 100m);
            return rate * discountFactor;
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            if (e.KeyChar == '.' && _textBox.Text.IndexOf('.') > -1)
            {
                e.Handled = true;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EndEdit(true);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _textBox.Text = _value.ToString("F2");
                EndEdit(false);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_Leave(object sender, EventArgs e)
        {
            EndEdit(true);
        }
    }
}