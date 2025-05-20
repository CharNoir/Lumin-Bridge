using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuminBridgeFramework
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainForm form = new MainForm();
            form.WindowState = FormWindowState.Minimized;
            form.ShowInTaskbar = false;
            form.Visible = false;

            Application.Run();
        }
    }    
}