using System.Windows.Forms;

namespace LabourBudgetCalculator // Ensure this namespace matches your project's namespace
{
    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
        }
    }
}