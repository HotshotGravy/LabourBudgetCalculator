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
        private Panel _contentHostPanel;

        public CommissioningResultsWindow(CommissioningProject project)
        {
            InitializeComponent();
            _project = project ?? throw new ArgumentNullException(nameof(project));

            this.Text = string.Format("Results - {0}", _project.ProjectName);
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

                var mainContainer = new TableLayoutPanel();
                mainContainer.Dock = DockStyle.Fill;
                mainContainer.RowCount = 3;
                mainContainer.ColumnCount = 1;
                mainContainer.Padding = new Padding(10);

                mainContainer.RowStyles.Clear();
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

                _summaryPanel = CreateSummaryPanel();
                mainContainer.Controls.Add(_summaryPanel, 0, 0);

                _mainPanel = new Panel();
                _mainPanel.Dock = DockStyle.Fill;
                _mainPanel.AutoScroll = true;
                _mainPanel.BorderStyle = BorderStyle.FixedSingle;
                mainContainer.Controls.Add(_mainPanel, 0, 1);

                _contentHostPanel = new Panel
                {
                    Name = "contentHostPanel", // Good for debugging
                    Dock = DockStyle.None,     // Crucial: Do not dock, allow AutoSize to control size
                    AutoSize = true,           // Crucial: Panel will size to its content
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Location = Point.Empty     // Position at the top-left of _mainPanel's scrollable area
                };
                _mainPanel.Controls.Add(_contentHostPanel);




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

            panel.ColumnStyles.Clear();
            for (int i = 0; i < 5; i++)
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

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

                _mainPanel.SuspendLayout(); // Suspend layout for the main scrolling panel
                _contentHostPanel.SuspendLayout(); // Suspend layout for the content host

                // Clear previous ResourcePanels from _contentHostPanel, not _mainPanel
                _contentHostPanel.Controls.Clear();
                _resourcePanels.Clear();

                int yPos = 10; // Initial Y position for the first ResourcePanel (adds a small top margin)
                int xPos = 10; // Initial X position for all ResourcePanels (adds a small left margin)

                foreach (var resource in _project.Resources)
                {
                    var resourcePanel = new ResourcePanel(resource, this);
                    resourcePanel.Location = new Point(xPos, yPos);

                    // Anchor determines how it behaves if _contentHostPanel resizes,
                    // but AutoSize on ResourcePanel primarily dictates its size based on its own content.
                    resourcePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    // Explicit Width setting for resourcePanel is still removed.

                    _contentHostPanel.Controls.Add(resourcePanel); // Add to _contentHostPanel
                    _resourcePanels.Add(resourcePanel);

                    // The ResourcePanel's Height (due to AutoSize) will be calculated based on its content.
                    yPos += resourcePanel.Height + 10; // Stack vertically with a 10px margin
                }

                _contentHostPanel.ResumeLayout(false); // Allow _contentHostPanel to resize based on children
                _mainPanel.ResumeLayout(false); // Update _mainPanel to reflect _contentHostPanel's new size (for scrolling)
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

                sb.AppendLine("Resource,Date,Day,Service Reg Hours,Service OT Hours,Service Premium Hours," +
                                 "Travel Reg Hours,Travel OT Hours,Travel Premium Hours," +
                                 "Mileage,Per Diem,Flight,Car Rental,Hotel,Total");

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
            // Position total label dynamically or anchor to right
            _totalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            headerPanel.SizeChanged += (s, e) => { _totalLabel.Location = new Point(headerPanel.Width - _totalLabel.Width - 5, 5); };


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
            _gridPanel.RowCount = 15; // Row for headers + 1 for sub-headers + 6 for charges + 1 subtotal + 5 for expenses + 1 subtotal + 1 total

            _gridPanel.ColumnStyles.Clear();
            _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // Row Header

            _gridPanel.RowStyles.Clear(); // Clear existing row styles
            for (int i = 0; i < _gridPanel.RowCount; i++)
            {
                _gridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            }

            for (int i = 0; i < dayCount; i++)
            {
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); // Planned
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); // Actual
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); // Delta
            }
        }

        private void AddHeaders(List<KeyValuePair<int, ResourceDayData>> orderedDays)
        {
            AddCell(0, 0, "", true); // Top-left empty cell

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

            AddDataRow(row++, "Service (Reg)", orderedDays, "ServiceReg");
            AddDataRow(row++, "Service (OT)", orderedDays, "ServiceOT");
            AddDataRow(row++, "Service (Premium)", orderedDays, "ServicePrem");
            AddDataRow(row++, "Travel (Reg)", orderedDays, "TravelReg");
            AddDataRow(row++, "Travel (OT)", orderedDays, "TravelOT");
            AddDataRow(row++, "Travel (Premium)", orderedDays, "TravelPrem");
            AddSubtotalRow(row++, "Subtotals - Charges", orderedDays, true); // Charges Subtotal
            AddDataRow(row++, "Mileage", orderedDays, "Mileage");
            AddDataRow(row++, "Per Diem", orderedDays, "PerDiem");
            AddDataRow(row++, "Flight", orderedDays, "Flight");
            AddDataRow(row++, "Car Rental", orderedDays, "CarRental");
            AddDataRow(row++, "Hotel", orderedDays, "Hotel");
            AddSubtotalRow(row++, "Subtotals - Expenses", orderedDays, false); // Expenses Subtotal
            AddTotalsRow(row, orderedDays); // Grand Total
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
                decimal actualValue = GetActualValue(dayData, dataType); // This is hours for service/travel
                decimal delta;

                // For delta calculation, if it's hours, it's actual hours - planned hours
                // If it's cost, it's actual cost - planned cost
                if (dataType.StartsWith("Service") || dataType.StartsWith("Travel"))
                {
                    delta = actualValue - plannedValue; // Hours delta
                }
                else // For expenses, values are already costs
                {
                    delta = actualValue - plannedValue; // Cost delta
                }

                AddValueCell(row, col, plannedValue, false, dataType); // Pass dataType for formatting
                AddEditableCell(row, col + 1, actualValue, dataType, dayEntry.Key, dayData);
                AddDeltaCell(row, col + 2, delta, dataType); // Pass dataType for formatting

                col += 3;
            }
        }

        private void AddSubtotalRow(int row, string labelText, List<KeyValuePair<int, ResourceDayData>> orderedDays, bool isCharges)
        {
            var rowLabel = CreateLabel(labelText, true);
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

                AddValueCell(row, col, plannedSubtotal, true, null, Color.LightYellow); // null for dataType as it's always currency
                AddValueCell(row, col + 1, actualSubtotal, true, null, Color.LightYellow); // null for dataType
                AddDeltaCell(row, col + 2, delta, null, Color.LightYellow); // null for dataType

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

                AddValueCell(row, col, plannedTotal, true, null, Color.Yellow); // Always currency
                AddValueCell(row, col + 1, actualTotal, true, null, Color.Yellow); // Always currency
                AddDeltaCell(row, col + 2, delta, null, Color.Yellow); // Always currency

                col += 3;
            }
        }

        // Modified AddValueCell to accept dataType for formatting
        private void AddValueCell(int row, int col, decimal value, bool isBold, string dataType, Color? backColor = null)
        {
            var label = new Label();

            if (dataType != null && (dataType.StartsWith("Service") || dataType.StartsWith("Travel")))
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

            string cellKey = string.Format("{0}_{1}_{2}", dayKey, dataType, row); // Make key more unique if needed
            _editableCells[cellKey] = editableCell;

            _gridPanel.Controls.Add(editableCell, col, row);
        }

        // Modified AddDeltaCell to accept dataType for formatting
        private void AddDeltaCell(int row, int col, decimal delta, string dataType, Color? backColor = null)
        {
            var label = new Label();

            if (dataType != null && (dataType.StartsWith("Service") || dataType.StartsWith("Travel")))
            {
                label.Text = delta.ToString("F1"); // Format as hours
            }
            else
            {
                label.Text = delta.ToString("C2"); // Format as currency
            }

            label.TextAlign = ContentAlignment.MiddleRight;
            label.Dock = DockStyle.Fill;
            label.ForeColor = delta == 0 ? Color.Black : (delta > 0 ? Color.Red : Color.Green);
            if (backColor.HasValue)
                label.BackColor = backColor.Value;

            _gridPanel.Controls.Add(label, col, row);
        }

        private decimal GetPlannedValue(ResourceDayData dayData, string dataType)
        {
            // Service and Travel dataType return hours
            if (dataType == "ServiceReg") return dayData.PlannedRegularLabourHours;
            else if (dataType == "ServiceOT") return dayData.PlannedOvertimeLabourHours;
            else if (dataType == "ServicePrem") return dayData.PlannedPremiumLabourHours;
            else if (dataType == "TravelReg") return dayData.PlannedRegularTravelHours;
            else if (dataType == "TravelOT") return dayData.PlannedOvertimeTravelHours;
            else if (dataType == "TravelPrem") return dayData.PlannedPremiumTravelHours;
            // Expense dataType return costs
            else if (dataType == "Mileage") return dayData.PlannedMileageCost;
            else if (dataType == "PerDiem") return dayData.PlannedPerDiemCost;
            else if (dataType == "Flight") return dayData.PlannedFlightCost;
            else if (dataType == "CarRental") return dayData.PlannedRentalCarCost;
            else if (dataType == "Hotel") return dayData.PlannedHotelCost;
            else return 0;
        }

        private decimal GetActualValue(ResourceDayData dayData, string dataType)
        {
            // Service and Travel dataType return hours
            if (dataType == "ServiceReg") return dayData.ActualRegularLabourHours;
            else if (dataType == "ServiceOT") return dayData.ActualOvertimeLabourHours;
            else if (dataType == "ServicePrem") return dayData.ActualPremiumLabourHours;
            else if (dataType == "TravelReg") return dayData.ActualRegularTravelHours;
            else if (dataType == "TravelOT") return dayData.ActualOvertimeTravelHours;
            else if (dataType == "TravelPrem") return dayData.ActualPremiumTravelHours;
            // Expense dataType return costs
            else if (dataType == "Mileage") return dayData.ActualMileageCost;
            else if (dataType == "PerDiem") return dayData.ActualPerDiemCost;
            else if (dataType == "Flight") return dayData.ActualFlightCost;
            else if (dataType == "CarRental") return dayData.ActualRentalCarCost;
            else if (dataType == "Hotel") return dayData.ActualHotelCost;
            else return 0;
        }

        private decimal GetRegularLabourRate() { decimal rate = _resource.RegularLabourRate; decimal discountFactor = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumLabourRate * discountFactor : rate * discountFactor; }
        private decimal GetOvertimeLabourRate() { decimal rate = _resource.OvertimeLabourRate; decimal discountFactor = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumLabourRate * discountFactor : rate * discountFactor; }
        private decimal GetPremiumLabourRate() { decimal rate = _resource.PremiumLabourRate; decimal discountFactor = 1 - (_resource.DiscountPercent / 100m); return rate * discountFactor; }
        private decimal GetRegularTravelRate() { decimal rate = _resource.RegularTravelRate; decimal discountFactor = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumTravelRate * discountFactor : rate * discountFactor; }
        private decimal GetOvertimeTravelRate() { decimal rate = _resource.OvertimeTravelRate; decimal discountFactor = 1 - (_resource.DiscountPercent / 100m); return _resource.IsEmergency ? _resource.PremiumTravelRate * discountFactor : rate * discountFactor; }
        private decimal GetPremiumTravelRate() { decimal rate = _resource.PremiumTravelRate; decimal discountFactor = 1 - (_resource.DiscountPercent / 100m); return rate * discountFactor; }

        private decimal CalculateSubtotal(ResourceDayData dayData, bool isCharges, bool isPlanned)
        {
            if (isCharges) // Labour and Travel Charges
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
                else // Actual
                {
                    return (dayData.ActualRegularLabourHours * GetRegularLabourRate()) +
                           (dayData.ActualOvertimeLabourHours * GetOvertimeLabourRate()) +
                           (dayData.ActualPremiumLabourHours * GetPremiumLabourRate()) +
                           (dayData.ActualRegularTravelHours * GetRegularTravelRate()) +
                           (dayData.ActualOvertimeTravelHours * GetOvertimeTravelRate()) +
                           (dayData.ActualPremiumTravelHours * GetPremiumTravelRate());
                }
            }
            else // Expenses
            {
                if (isPlanned)
                {
                    return dayData.PlannedMileageCost + dayData.PlannedPerDiemCost +
                           dayData.PlannedFlightCost + dayData.PlannedRentalCarCost +
                           dayData.PlannedHotelCost;
                }
                else // Actual
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
                total += CalculateDayTotal(dayEntry, false); // Use actual values for the displayed total
            }
            _totalLabel.Text = string.Format("Total: {0:C2}", total);
        }

        internal void NotifyValueChanged()
        {
            UpdateTotalLabel();
            _parentWindow.MarkProjectDirty();
            // _parentWindow.SaveData(); // Consider if auto-save on every change is desired or too frequent
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
                    EscapeCSV(_resource.TechnicianName),
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
        private string EscapeCSV(string value)
        {
            if (value == null) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }

    internal class EditableCell : UserControl
    {
        private Label _label;
        private TextBox _textBox;
        private decimal _value;
        private string _dataType; // e.g., "ServiceReg", "Mileage"
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

            // *** CHANGE 1: Conditional formatting for initial display ***
            if (_dataType.StartsWith("Service") || _dataType.StartsWith("Travel"))
            {
                _label.Text = _value.ToString("F1"); // Hours format
            }
            else
            {
                _label.Text = _value.ToString("C2"); // Currency format
            }
            // *** The problematic overriding line `_label.Text = _value.ToString("C2");` that was here previously is now effectively handled by the else block above. ***

            _label.TextAlign = ContentAlignment.MiddleRight;
            _label.Dock = DockStyle.Fill;
            _label.Cursor = Cursors.Hand;
            _label.MouseClick += Label_MouseClick;

            _textBox = new TextBox();
            // Set TextBox initial text based on type as well for consistency
            if (_dataType.StartsWith("Service") || _dataType.StartsWith("Travel"))
            {
                _textBox.Text = _value.ToString("F1");
            }
            else
            {
                _textBox.Text = _value.ToString("F2"); // F2 for editing currency
            }
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
            if (_dataType.StartsWith("Service") || _dataType.StartsWith("Travel"))
            {
                _textBox.Text = _value.ToString("F1");
            }
            else
            {
                _textBox.Text = _value.ToString("F2"); // F2 for editing currency
            }
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
                if (decimal.TryParse(_textBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out newValue))
                {
                    if (newValue != _value)
                    {
                        _value = newValue;
                        UpdateDayData(newValue);
                        // *** CHANGE 2: Conditional formatting after edit ***
                        if (_dataType.StartsWith("Service") || _dataType.StartsWith("Travel"))
                        {
                            _label.Text = _value.ToString("F1"); // Hours format
                        }
                        else
                        {
                            _label.Text = _value.ToString("C2"); // Currency format
                        }
                        _parentPanel.NotifyValueChanged();
                    }
                }
                // If parsing fails, revert to original value display
                else
                {
                    if (_dataType.StartsWith("Service") || _dataType.StartsWith("Travel")) { _label.Text = _value.ToString("F1"); } else { _label.Text = _value.ToString("C2"); }
                }
            }
            else // If not saving (e.g. Escape pressed), revert display
            {
                if (_dataType.StartsWith("Service") || _dataType.StartsWith("Travel")) { _label.Text = _value.ToString("F1"); } else { _label.Text = _value.ToString("C2"); }
            }


            _textBox.Visible = false;
            _label.Visible = true;
        }

        private void UpdateDayData(decimal newValue)
        {
            // For service and travel, the value IS the hours (not cost)
            if (_dataType == "ServiceReg") _dayData.ActualRegularLabourHours = newValue;
            else if (_dataType == "ServiceOT") _dayData.ActualOvertimeLabourHours = newValue;
            else if (_dataType == "ServicePrem") _dayData.ActualPremiumLabourHours = newValue;
            else if (_dataType == "TravelReg") _dayData.ActualRegularTravelHours = newValue;
            else if (_dataType == "TravelOT") _dayData.ActualOvertimeTravelHours = newValue;
            else if (_dataType == "TravelPrem") _dayData.ActualPremiumTravelHours = newValue;
            // For expenses, the value is cost
            else if (_dataType == "Mileage") _dayData.ActualMileageCost = newValue;
            else if (_dataType == "PerDiem") _dayData.ActualPerDiemCost = newValue;
            else if (_dataType == "Flight") _dayData.ActualFlightCost = newValue;
            else if (_dataType == "CarRental") _dayData.ActualRentalCarCost = newValue;
            else if (_dataType == "Hotel") _dayData.ActualHotelCost = newValue;

            _resource.IsDirty = true; // Mark resource as dirty as actuals have changed
            // Project totals will be recalculated before saving or when summary is updated
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow numbers, decimal point, and control characters (like backspace)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // Allow only one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
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
                // Revert textbox text before ending edit without saving
                if (_dataType.StartsWith("Service") || _dataType.StartsWith("Travel")) { _textBox.Text = _value.ToString("F1"); } else { _textBox.Text = _value.ToString("F2"); }
                EndEdit(false);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_Leave(object sender, EventArgs e)
        {
            // Only save if the textbox is still visible (i.e., wasn't an Esc/Enter key press that already handled it)
            if (_textBox.Visible)
            {
                EndEdit(true);
            }
        }
    }
}