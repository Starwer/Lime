/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/

using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System;
using System.IO;

namespace Lime
{

    /// <summary>
    /// Handle all kinds of messages issued by Lime and use the appropriate way to display or log these.
    /// </summary>
    public class LimeMsg
    {

        // --------------------------------------------------------------------------------------------------
        #region Constants 

        private const string IniLanguageSection = "Messages";

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Types


        /// <summary>
        /// Message severity levels.
        /// </summary>
        public enum Severity
        {
            /// <summary>
            /// Ignored message (for log-verbosity level only)
            /// </summary>
            Off = 6,
            /// <summary>
            /// Error message
            /// </summary>
            Error = 5,
            /// <summary>
            /// Warning message
            /// </summary>
            Warning = 4,
            /// <summary>
            /// Information message
            /// </summary>
            Info = 3,
            /// <summary>
            /// Warning message
            /// </summary>
            Dev = 2,
#if TRACE
            /// <summary>
            /// Debug message
            /// </summary>
            Debug = 1
#endif
        }


        /// <summary>
        /// Handle a Lime message of a given severity level.
        /// This function should actually display or log the message.
        /// </summary>
        /// <param name="lvl">severity level of the message</param>
        /// <param name="msg">Textual description of the message</param>
        public delegate void Handler(Severity lvl, string msg);

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Properties

        /// <summary>
        /// Set the name of the EventLog used by the WinLog message handler.
        /// </summary>
        public static string WinLogName = "Lime";

        /// <summary>
        /// Set minimum level under which messages should not be logged.
        /// Level Off will ignore every messages.
        /// </summary>
        public static Severity Verbosity = 
#if TRACE
            Severity.Debug;
#else
            Severity.Warning;
#endif

        /// <summary>
        /// Set minimum level under which messages should not be displayed.
        /// Level Off will ignore every messages.
        /// </summary>
        public static Severity Popup = Severity.Info;


        /// <summary>
        /// List of delegate functions which should be called when a Lime message is issued.
        /// Every new message from Lime, all the functions in this list are called, regardless of the verbosity-levels.
        /// Default methods are logging to the Windows Event log and display the messages to a Popup dialog.
        /// </summary>
        public static Handler Handlers = null; 

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Functions

        /// <summary>
        /// Compose a Lime message from a given description.
        /// </summary>
        /// <param name="lvl">severity level of the message</param>
        /// <param name="msg">Textual description of the message</param>
        /// <param name="args">arguments to be expanded (using String.Format) in the msg.</param>
        /// <returns>The resulting Lime message</returns>
        public static string Compose(Severity lvl, string msg, params object[] args)
        {
            // Translate Popup messages
            if(lvl>=Popup)
            {
                msg = LimeLanguage.Translate(IniLanguageSection, msg, msg);
            }

            if (args != null && args.Length > 0)
            {
                try {
                    msg = String.Format(msg, args);
                }
                catch
                {
                    // Fallback
                    foreach (var arg in args) msg += " <" + arg + ">";
                }
            }

            return msg;
        }



        /// <summary>
        /// Issue a Lime message of a given severity level.
        /// It distributes this message to all the defined LimeMsgHandlers.
        /// If no LimeMsgHandler is assigned, the message is ignored.
        /// </summary>
        /// <param name="lvl">severity level of the message</param>
        /// <param name="msg">Textual description of the message</param>
        /// <param name="args">arguments to be expanded (using String.Format) in the msg.</param>
        /// <returns>The resulting Lime message</returns>
        public static void Level(Severity lvl, string msg, params object[] args)
        {
            if (lvl < Verbosity) return;

            msg = Compose(lvl, msg, args);

            Process(Handlers, lvl, msg);
        }

        /// <summary>
        /// Process the message/level with all the handlers.
        /// </summary>
        /// <param name="handler">List of handlers to be called back</param>
        /// <param name="lvl">severity level of the message</param>
        /// <param name="msg">Textual description of the message</param>
        private static void Process(Handler handler, Severity lvl, string msg)
        {
            handler?.Invoke(lvl, msg);
        }


        /// <summary>
        /// Issue a Lime message of a severity level: Error.
        /// <see cref="Level"/>
        /// </summary>
        /// <param name="msg">Textual description of the message</param>
        /// <param name="args">arguments to be expanded (using String.Format) in the msg.</param>
        /// <returns>The resulting Lime message</returns>
        public static void Error(string msg, params object[] args)
        {
            Level(Severity.Error, msg, args);
        }

        /// <summary>
        /// Issue a Lime message of a severity level: Warning.
        /// <see cref="Level"/>
        /// </summary>
        /// <param name="msg">Textual description of the message</param>
        /// <param name="args">arguments to be expanded (using String.Format) in the msg.</param>
        /// <returns>The resulting Lime message</returns>
        public static void Warning(string msg, params object[] args)
        {
            Level(Severity.Warning, msg, args);
        }

        /// <summary>
        /// Issue a Lime message of a severity level: Dev.
        /// <see cref="Level"/>
        /// </summary>
        /// <param name="msg">Textual description of the message</param>
        /// <param name="args">arguments to be expanded (using String.Format) in the msg.</param>
        /// <returns>The resulting Lime message</returns>
        public static void Dev(string msg, params object[] args)
        {
            Level(Severity.Dev, msg, args);
        }

        /// <summary>
        /// Issue a Lime message of a severity level: Info.
        /// <see cref="Level"/>
        /// </summary>
        /// <param name="msg">Textual description of the message</param>
        /// <param name="args">arguments to be expanded (using String.Format) in the msg.</param>
        /// <returns>The resulting Lime message</returns>
        public static void Info(string msg, params object[] args)
        {
            Level(Severity.Info, msg, args);
        }


        /// <summary>
        /// Issue a Lime message of a severity level: Debug (only available in debug version).
        /// <see cref="Level"/>
        /// </summary>
        /// <param name="msg">Textual description of the message</param>
        /// <param name="args">arguments to be expanded (using String.Format) in the msg.</param>
        /// <returns>The resulting Lime message</returns>
        [Conditional("TRACE")]
        public static void Debug(string msg, params object[] args)
        {
            Level(Severity.Debug, msg, args);
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Default Message Handlers


        /// <summary>
        /// Default LimeMsgHandler displaying messages in a normal system dialog box window.
        /// Only the message above the Verbosity-level set in Popup property are displayed. 
        /// </summary>
        /// <param name="lvl">severity level of the message</param>
        /// <param name="msg">Textual description of the message</param>
        public static void WinDialog(Severity lvl, string msg)
        {
            if (lvl < Popup) return;

            MessageBoxIcon icon;
            switch (lvl) {
                case Severity.Error: icon = MessageBoxIcon.Error; break;
                case Severity.Warning: icon = MessageBoxIcon.Warning; break;
                case Severity.Info: icon = MessageBoxIcon.Information; break;
                default: icon = MessageBoxIcon.Asterisk; break;
            }

            MessageBox.Show(msg, "Lime " + lvl.ToString(), MessageBoxButtons.OK, icon);

        }


        /// <summary>
        /// Default LimeMsgHandler logging messages to the Windows Log system.
        /// Only the message above the verbosity-level set in Verbosity property are displayed. 
        /// </summary>
        /// <param name="lvl">severity level of the message</param>
        /// <param name="msg">Textual description of the message</param>
        public static void WinLog(Severity lvl, string msg)
        {
            if (lvl < Verbosity) return;

            EventLogEntryType logtype;
            switch (lvl)
            {
                case Severity.Error: logtype = EventLogEntryType.Error; break;
                case Severity.Warning: logtype = EventLogEntryType.Warning; break;
                case Severity.Info: logtype = EventLogEntryType.Information; break;
                default: logtype = EventLogEntryType.Information; break;
            }

            if (!EventLog.SourceExists(WinLogName))
                EventLog.CreateEventSource(WinLogName, WinLogName + ".dll");

            EventLog.WriteEntry(WinLogName, msg, logtype);
        }


        /// <summary>
        /// Default LimeMsgHandler displaying messages on the C# Output.
        /// Only the message above the Verbosity-level set in Verbosity property are displayed. 
        /// </summary>
        /// <param name="lvl">severity level of the message</param>
        /// <param name="msg">Textual description of the message</param>
        public static void DebugLog(Severity lvl, string msg)
        {
            if (lvl < Verbosity) return;

            System.Diagnostics.Debug.WriteLine(lvl.ToString() + " : " + msg);

        }

        #endregion

    }
}
