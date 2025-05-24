using System;
using System.Drawing;
using System.Windows.Forms;

namespace LabourBudgetCalculator
{
    public partial class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            InitializeComponent();
            this.Load += WelcomeForm_Load;
        }

        private void WelcomeForm_Load(object sender, EventArgs e)
        {
            // Set up the form
            this.Text = "Time & Expense Calculator";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create controls
            SetupControls();
        }

        private void SetupControls()
        {
            // Title label
            Label lblTitle = new Label();
            lblTitle.Text = "Time and Expense Budget Wizard";
            lblTitle.Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold);
            lblTitle.Size = new Size(350, 50);
            lblTitle.Location = new Point(25, 30);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // Subtitle label
            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Please choose an option:";
            lblSubtitle.Font = new Font(this.Font.FontFamily, 10);
            lblSubtitle.Size = new Size(350, 30);
            lblSubtitle.Location = new Point(25, 80);
            lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblSubtitle);

            // Quick Estimate button
            Button btnQuickEstimate = new Button();
            btnQuickEstimate.Text = "Quick Estimate";
            btnQuickEstimate.Size = new Size(150, 40);
            btnQuickEstimate.Location = new Point(125, 130);
            btnQuickEstimate.Font = new Font(this.Font.FontFamily, 10);
            btnQuickEstimate.Click += BtnQuickEstimate_Click;
            this.Controls.Add(btnQuickEstimate);

            // Commissioning Tracker button
            Button btnCommissioningTracker = new Button();
            btnCommissioningTracker.Text = "Commissioning / Service Tracker";
            btnCommissioningTracker.Size = new Size(240, 50);
            btnCommissioningTracker.Location = new Point(75, 180);
            btnCommissioningTracker.Font = new Font(this.Font.FontFamily, 10);
            btnCommissioningTracker.Click += BtnCommissioningTracker_Click;
            this.Controls.Add(btnCommissioningTracker);
        }

        private void BtnQuickEstimate_Click(object sender, EventArgs e)
        {
            // Open the main form (existing functionality)
            this.Hide();
            MainForm mainForm = new MainForm();
            mainForm.ShowDialog();
            this.Close();
        }

        private void BtnCommissioningTracker_Click(object sender, EventArgs e)
        {
            // Open the commissioning project selection form
            this.Hide();
            CommissioningProjectSelectionForm projectSelectionForm = new CommissioningProjectSelectionForm();
            projectSelectionForm.ShowDialog();
            this.Close();
        }
    }
}