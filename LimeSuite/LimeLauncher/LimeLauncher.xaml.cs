/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Collections.Generic;
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
            CommandLineInterface(true, e.Args);

        }


        /// <summary>
        /// Handle the Command Line Interface parsing of the application.
        /// </summary>
        /// <param name="firstInstance">true if this is the first instance, false if the application is re-opened</param>
        /// <param name="args">list of arguments of the command</param>
        /// <returns>true if handled.</returns>
        public bool CommandLineInterface(bool firstInstance, IList<string> args)
        {
            LimeMsg.Debug("CommandLineInterface: {0}", firstInstance);

            Global.Local.CtrlMode = CtrlMode.CLI;

            for (int i = firstInstance ? 0 : 1; i < args.Count; i++)
            {
                LimeMsg.Debug("CommandLineInterface: {0}: arg: {1}", firstInstance, args[i]);
                string arg = args[i];
                                
                if (arg.Length > 0)
                {
                    // Options
                    LimeProperty prop;
                    bool isToggle = false;

                    // Parse argument (detect = and !)
                    string value = null;
                    int idx = arg.IndexOf('=');
                    if (idx>=0)
                    {
                        value = arg.Substring(idx+1);
                        arg = arg.Substring(0, idx).Trim();
                    }
                    else if (arg.EndsWith("!"))
                    {
                        isToggle = true;
                        arg = arg.Substring(0, arg.Length - 1).Trim();
                    }

                    if ((prop = Global.Properties.Get(arg)) != null) 
                    {
                        // Property
                        if (value != null)
                        {
                            if (prop.ReadOnly)
                            {
                                LimeMsg.Error("ErrReadOnlyProp", args[i]);
                            }
                            else
                            {
                                try
                                {
                                    prop.Serialize = value;
                                }
                                catch
                                {
                                    LimeMsg.Error("ErrInvProp", args[i], prop.Type.ToString());
                                }
                            }
                        }
                        else if (isToggle)
                        {
                            prop.Toggle();
                        }
                        else if (prop.Content is LimeCommand cmd)
                        {
                            cmd.Execute();
                        }
                    }
                    else
                    {
                        // Other options
                        bool handled = true;
                        switch (arg.ToLower())
                        {

                            case "?":
                            case "h":
                            case "help":
                                {
                                    // TODO: do something usefull here
                                    break;
                                }

                            default:
                                handled = false;
                                break;

                        }

                        // Try Skin-parameters
                        if (!handled && Global.Local.Skin != null)
                        {
                            var param = Global.Local.Skin.Get(arg);
                            if (param != null)
                            {
                                if(param.Visible && param.Content != null)
                                {
                                    handled = true;
                                    if (value != null)
                                    {
                                        try
                                        {
                                            param.Serialize = value;
                                        }
                                        catch
                                        {
                                            LimeMsg.Error("ErrInvSkinProp", args[i], param.Type.ToString());
                                        }
                                    }
                                    break;
                                }
                            }

                        }

						// No match found
						if (!handled)
                        {
                            LimeMsg.Error("ErrInvArg", args[i]);
                        }

                    }
                }
            }

            return true;
        }



        #region SingleInstance

        [STAThread]
        public static void Main()
        {
            var application = new App();

            application.InitializeComponent();
            application.Run();

        }

        #endregion

    }

}
