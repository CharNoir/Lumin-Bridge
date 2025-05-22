using System;
using System.Windows.Forms;
using System.IO;

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
            try
            {
                File.AppendAllText("log.txt", "App starting...\n");

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    File.AppendAllText("log.txt", $"Unhandled: {e.ExceptionObject}\n");
                    MessageBox.Show("Unhandled exception:\n" + e.ExceptionObject.ToString(), "Crash");
                };

                Application.ThreadException += (s, e) =>
                {
                    File.AppendAllText("log.txt", $"Thread exception: {e.Exception}\n");
                    MessageBox.Show("Thread exception:\n" + e.Exception.Message, "Crash");
                };

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                File.AppendAllText("log.txt", "Creating MainForm...\n");

                var form = new MainForm();
                form.WindowState = FormWindowState.Minimized;
                form.ShowInTaskbar = false;
                form.Visible = false;

                File.AppendAllText("log.txt", "Running application...\n");
                Application.Run();
                File.AppendAllText("log.txt", "Application exited cleanly.\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", $"Startup crash: {ex}\n");
                MessageBox.Show("Fatal startup error:\n" + ex.ToString());
            }
        }
    }    
}