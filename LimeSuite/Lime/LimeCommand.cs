/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     22-12-2016
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows.Input;

namespace Lime
{
    /// <summary>
    /// Properties defining a command in an exhaustive way.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class LimeCommand : ICommand
    {
        // --------------------------------------------------------------------------------------------------
        #region Properties

        /// <summary>
        /// Source Class containing all commands as static fields
        /// </summary>
        public static Type Commands;

        /// <summary>
        /// Next command to execute in case of toggle command
        /// </summary>
        public LimeCommand ToggleCmd
        {
            get
            {
                if (ToggleCmdName != null && Commands != null)
                {
                    var info = Commands.GetField(ToggleCmdName);
                    if (info != null)
                    {
                        _ToggleCmd = (LimeCommand)info.GetValue(this);
                    }
#if DEBUG
                    else // Check existance
                        LimeMsg.Error("CmpCmdToggleMiss", ToggleCmdName);
#endif
                    ToggleCmdName = null;
                }
                return _ToggleCmd;
            }
        }
        private LimeCommand _ToggleCmd = null;

        /// <summary>
        /// Store the ToggleCmd Name to delay its evaluation after the Commands are initialized 
        /// </summary>
        private string ToggleCmdName;


        /// <summary>
        /// return true if the command has toggle (complementary) command
        /// </summary>
        public bool IsToggle { get { return ToggleCmd != null || ToggleCmdName != null; } }

        /// <summary>
        /// Always toggle when possible
        /// </summary>
        public bool AlwaysToggle { get; private set; }


		/// <summary>
		/// Icon key reference representing this command
		/// </summary>
		public string Icon { get; private set; }

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region ctors

		/// <summary>
		/// Constructor normal command
		/// </summary>
		/// <param name="cmd">Application Command to bind to this command</param>
		/// <param name="canExecute">Action function returning true if the command is enabled</param>
		/// <param name="action">Action to run when the command should be executed</param>
		/// <param name="toggle">Toggle command to be executed next if CanExecute condition is not met</param>
		/// <param name="icon">Icon key reference representing this command</param>
		public LimeCommand(Func<bool> canExecute = null, Action action = null, string toggle = null, string icon = null)
        {
            CanExecuteFunc = canExecute;
            CommandAction = action;
            CommandBinding = null;
            ToggleCmdName = toggle;
			Icon = icon;
		}

        /// <summary>
        /// Constructor command bound to Application Command 
        /// </summary>
        /// <param name="cmd">Application Command to bind to this command</param>
        /// <param name="canExecute">Action function returning true if the command is enabled</param>
        /// <param name="action">Action to run when the command should be executed</param>
        /// <param name="toggle">Toggle command to be executed next if CanExecute condition is not met</param>
        public LimeCommand(ICommand cmd, Func<bool> canExecute = null, Action action = null, string toggle = null, string icon = null) : this(canExecute, action, toggle, icon)
        {
            CommandBinding = new CommandBinding(cmd, HandlerExecuted, HandlerCanExecute);
        }


        /// <summary>
        /// Duplicate a LimeCommand, and enable to make it a Toggle comand
        /// </summary>
        /// <param name="copy">Source command to copy</param>
        /// <param name="toggle">enable to create a Toggle command</param>
        public LimeCommand(LimeCommand copy, bool toggle)
        {
            // Copy properties
            LimeLib.CopyPropertyValues(copy, this);
            ToggleCmdName = copy.ToggleCmdName;
            _ToggleCmd = copy._ToggleCmd;

            // Convert to Toggle command
            AlwaysToggle = (copy.IsToggle && toggle);
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Delegate Command

        public Action CommandAction { get; protected set; }
        public Func<bool> CanExecuteFunc { get; protected set; }
        public CommandBinding CommandBinding { get; protected set; }



        void HandlerCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CanExecute(e.Parameter);
        }

        void HandlerExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Execute(e.Parameter);            
        }

        /// <summary>
        /// Subscribe to event
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }


        #endregion

        
        // --------------------------------------------------------------------------------------------------
        #region Methods

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="parameter">parameter of the command</param>
        public void Execute(object parameter = null)
        {
            if (AlwaysToggle) Toggle(parameter);
            else if (CanExecute(parameter))  CommandAction();
        }

        /// <summary>
        /// Execute the command. 
        /// Automatically execute toggle command if applicable.
        /// </summary>
        /// <param name="parameter">parameter of the command</param>
        public void Toggle(object parameter = null)
        {
            if (CanExecuteFunc == null || CanExecuteFunc())
            {
                CommandAction();
            }
            else if(ToggleCmd != null)
            {
                // Toggle command
                var cmd = ToggleCmd;
                while (cmd != this && cmd != null)
                {
                    if (cmd.CanExecuteFunc == null || cmd.CanExecuteFunc())
                    {
                        cmd.Execute(parameter);
                        break;
                    }
                    cmd = cmd.ToggleCmd;
                }
            }
        }

        /// <summary>
        /// Return true if the Toggle can be executed
        /// </summary>
        /// <param name="parameter">parameter of the command</param>
        /// <returns>true if the command can be executed</returns>
        public bool CanToggle(object parameter = null)
        {
            if (CanExecuteFunc == null || CanExecuteFunc())
            {
                return true;
            }
            else if (ToggleCmd != null)
            {
                // Toggle command
                var cmd = ToggleCmd;
                while (cmd != this && cmd != null)
                {
                    if (cmd.CanExecuteFunc == null || cmd.CanExecuteFunc())
                    {
                        return true;
                    }
                    cmd = cmd.ToggleCmd;
                }
            }

            return false;
        }

        /// <summary>
        /// Return true if the command can be executed
        /// </summary>
        /// <param name="parameter">parameter of the command</param>
        /// <returns>true if the command can be executed</returns>
        public bool CanExecute(object parameter = null)
        {
            if (AlwaysToggle) return CanToggle(parameter);
            return CanExecuteFunc == null || CanExecuteFunc();
        }


        #endregion


    }
}
