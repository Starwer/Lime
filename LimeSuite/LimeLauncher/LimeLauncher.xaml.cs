/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows;
using Lime;

namespace LimeLauncher
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public partial class App : Application 
    {
        /// <summary>
        /// Function called back when the application starts: Initialize the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void App_Startup(object sender, StartupEventArgs e)
        {
            // Initialize Logger
#if TRACE
            LimeMsg.Handlers += LimeMsg.DebugLog;
#endif
            //LimeMsg.Handlers += LimeMsg.WinLog;
            LimeMsg.Handlers += LimeMsg.WinDialog;

            // Display version
            LimeMsg.Debug("{0} version {1}", About.Name, About.Version);
            LimeMsg.Debug("ApplicationPath: {0}", About.ApplicationPath);

            // Initialize/Load LimeLauncher data-structures
            Global.Load();

			// Initialize GUI
			new MainWindow();

            // Parse command arguments
            Commands.Parse(e.Args);

        }

        /// <summary>
        /// Application entry point, handling the singleton application instance scheme
        /// </summary>
        /// <param name="args">command line arguments</param>
        [STAThread]
        public static void Main(string[] args)
        {
            var appName = About.ApplicationName;
            Process[] processList = Process.GetProcessesByName(appName);
            var currId = Environment.ProcessId;

            // Retrieve other (main) application instance
            foreach (var process in processList)
            {
                if (process.Id != currId)
                {
                    IntPtr wHandle = process.MainWindowHandle;

                    if (wHandle != IntPtr.Zero)
                    {
                        // Try to retrieve the real main root window, and not the popup or child windows.
                        // Still, this will keep stuck on sub windows like Configuration window
                        IntPtr ptr;
                        while ( (ptr = Win32.GetWindow(wHandle, Win32.GW.Owner)) != IntPtr.Zero)
                        {
                            wHandle = ptr;
                        }
                        while ((ptr = Win32.GetParent(wHandle)) != IntPtr.Zero)
                        {
                            wHandle = ptr;
                        }

                        string name = Win32.GetWindowTitle(wHandle);
                        if (name != About.Name)
                        {
                            // Alternative method to find the real main window, and not the sub windows
                            Win32.EnumerateWindows (
                                (IntPtr hWnd, IntPtr lParam) => 
                                { 
                                    if ( Win32.GetWindowPID(hWnd) == process.Id &&
                                         Win32.GetWindowTitle(hWnd) == About.Name )
                                    {
                                        wHandle = hWnd;
                                    } 
                                    return true; 
                                }, 
                                IntPtr.Zero );
                        }

                        // Send command arguments to main application instance as one string
                        var pack = string.Join((char)1, args);
                        Win32.SendWindowsMessageCopyData(wHandle, pack, currId);
                        return;
                    }
                }
            }

            // First application instance: Start application
            var application = new App();

            application.InitializeComponent();
            application.Run();


        }

    }

}
