using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Windows.Forms;

namespace LabourBudgetCalculator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WelcomeForm());
        }
    }
}