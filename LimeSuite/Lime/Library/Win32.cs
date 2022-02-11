#region Copyright (C) 2005-2009 Team MediaPortal
// Credit: Team MediaPortal - IRSS

// Copyright (C) 2005-2009 Team MediaPortal
// http://www.team-mediaportal.com
// 
// This Program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2, or (at your option)
// any later version.
// 
// This Program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GNU Make; see the file COPYING.  If not, write to
// the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
// http://www.gnu.org/copyleft/gpl.html

#endregion

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Win32 native method class.
/// </summary>
[SupportedOSPlatform("windows")]
public static class Win32
{
    // --------------------------------------------------------------------------------------------------
    #region Constants

    private const int GCL_HICON = -14;
    private const int GCL_HICONSM = -34;


    private const int ICON_BIG = 1;
    private const int ICON_SMALL = 0;

    /// <summary>
    /// Maximum length of unmanaged Windows Path strings.
    /// </summary>
    private const int MAX_PATH = 260;

    /// <summary>
    /// Maximum length of unmanaged Typename.
    /// </summary>
    private const int MAX_TYPE = 80;

    private const int MINIMIZE_ALL = 419;
    private const int MINIMIZE_ALL_UNDO = 416;
    private const int WPF_RESTORETOMAXIMIZED = 2;


    /// <summary>Required to enable or disable the privileges in an access token.</summary>
    private const int TOKEN_ADJUST_PRIVILEGES = 0x20;
    /// <summary>Required to query an access token.</summary>
    private const int TOKEN_QUERY = 0x8;
    /// <summary>The privilege is enabled.</summary>
    private const int SE_PRIVILEGE_ENABLED = 0x2;
    /// <summary>Specifies that the function should search the system message-table resource(s) for the requested message.</summary>
    private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
    /// <summary>Forces processes to terminate. When this flag is set, the system does not send the WM_QUERYENDSESSION and WM_ENDSESSION messages. This can cause the applications to lose data. Therefore, you should only use this flag in an emergency.</summary>
    private const int EWX_FORCE = 4;


    #endregion Constants

    // --------------------------------------------------------------------------------------------------
    #region Enumerations

    /// <summary>
    /// Windows Message App Commands.
    /// </summary>
    public enum AppCommand
    {
        /// <summary>
        /// APPCOMMAND_BROWSER_BACKWARD
        /// </summary>
        APPCOMMAND_BROWSER_BACKWARD = 1,
        /// <summary>
        /// APPCOMMAND_BROWSER_FORWARD
        /// </summary>
        APPCOMMAND_BROWSER_FORWARD = 2,
        /// <summary>
        /// APPCOMMAND_BROWSER_REFRESH
        /// </summary>
        APPCOMMAND_BROWSER_REFRESH = 3,
        /// <summary>
        /// APPCOMMAND_BROWSER_STOP
        /// </summary>
        APPCOMMAND_BROWSER_STOP = 4,
        /// <summary>
        /// APPCOMMAND_BROWSER_SEARCH
        /// </summary>
        APPCOMMAND_BROWSER_SEARCH = 5,
        /// <summary>
        /// APPCOMMAND_BROWSER_FAVORITES
        /// </summary>
        APPCOMMAND_BROWSER_FAVORITES = 6,
        /// <summary>
        /// APPCOMMAND_BROWSER_HOME
        /// </summary>
        APPCOMMAND_BROWSER_HOME = 7,
        /// <summary>
        /// APPCOMMAND_VOLUME_MUTE
        /// </summary>
        APPCOMMAND_VOLUME_MUTE = 8,
        /// <summary>
        /// APPCOMMAND_VOLUME_DOWN
        /// </summary>
        APPCOMMAND_VOLUME_DOWN = 9,
        /// <summary>
        /// APPCOMMAND_VOLUME_UP
        /// </summary>
        APPCOMMAND_VOLUME_UP = 10,
        /// <summary>
        /// APPCOMMAND_MEDIA_NEXTTRACK
        /// </summary>
        APPCOMMAND_MEDIA_NEXTTRACK = 11,
        /// <summary>
        /// APPCOMMAND_MEDIA_PREVIOUSTRACK
        /// </summary>
        APPCOMMAND_MEDIA_PREVIOUSTRACK = 12,
        /// <summary>
        /// APPCOMMAND_MEDIA_STOP
        /// </summary>
        APPCOMMAND_MEDIA_STOP = 13,
        /// <summary>
        /// APPCOMMAND_MEDIA_PLAY_PAUSE
        /// </summary>
        APPCOMMAND_MEDIA_PLAY_PAUSE = 4143,
        /// <summary>
        /// APPCOMMAND_MEDIA_PLAY
        /// </summary>
        APPCOMMAND_MEDIA_PLAY = 4142,
        /// <summary>
        /// APPCOMMAND_MEDIA_PAUSE
        /// </summary>
        APPCOMMAND_MEDIA_PAUSE = 4143,
        /// <summary>
        /// APPCOMMAND_MEDIA_RECORD
        /// </summary>
        APPCOMMAND_MEDIA_RECORD = 4144,
        /// <summary>
        /// APPCOMMAND_MEDIA_FASTFORWARD
        /// </summary>
        APPCOMMAND_MEDIA_FASTFORWARD = 4145,
        /// <summary>
        /// APPCOMMAND_MEDIA_REWIND
        /// </summary>
        APPCOMMAND_MEDIA_REWIND = 4146,
        /// <summary>
        /// APPCOMMAND_MEDIA_CHANNEL_UP
        /// </summary>
        APPCOMMAND_MEDIA_CHANNEL_UP = 4147,
        /// <summary>
        /// APPCOMMAND_MEDIA_CHANNEL_DOWN
        /// </summary>
        APPCOMMAND_MEDIA_CHANNEL_DOWN = 4148,
    }

    /// <summary>
    /// Exit Windows method.
    /// </summary>
    [Flags]
    public enum ExitWindows
    {
        /// <summary>
        /// LogOff
        /// </summary>
        LogOff = 0,
        /// <summary>
        /// ShutDown
        /// </summary>
        ShutDown = 1,
        /// <summary>
        /// Reboot
        /// </summary>
        Reboot = 2,
        /// <summary>
        /// PowerOff
        /// </summary>
        PowerOff = 8,
    }

    /// <summary>
    /// GWL.
    /// </summary>
    public enum GWL : int
    {
        /// <summary>
        /// WndProc.
        /// </summary>
        GWL_WNDPROC = (-4),
        /// <summary>
        /// HInstance.
        /// </summary>
        GWL_HINSTANCE = (-6),
        /// <summary>
        /// hWnd Parent.
        /// </summary>
        GWL_HWNDPARENT = (-8),
        /// <summary>
        /// Style.
        /// </summary>
        GWL_STYLE = (-16),
        /// <summary>
        /// Extended Style.
        /// </summary>
        GWL_EXSTYLE = (-20),
        /// <summary>
        /// User Data.
        /// </summary>
        GWL_USERDATA = (-21),
        /// <summary>
        /// ID.
        /// </summary>
        GWL_ID = (-12),
    }

    /// <summary>
    /// The relationship between the specified window and the window whose handle is to be retrieved. This parameter can be one of the following values.
    /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633515(v=vs.85).aspx"/>
    /// </summary>
    public enum GW : uint
    {
        /// <summary>
        /// The retrieved handle identifies the window of the same type that is highest in the Z order. 
        /// If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        First = 0,
        
        /// <summary>
        /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
        /// If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        Last = 1,
        
        /// <summary>
        /// The retrieved handle identifies the window below the specified window in the Z order.
        /// If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        Next = 2,
        
        /// <summary>
        /// The retrieved handle identifies the window above the specified window in the Z order.
        /// If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        Prev = 3,

        /// <summary>
        /// The retrieved handle identifies the specified window's owner window, if any.
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632599(v=vs.85).aspx#owned_windows"/>
        /// </summary>
        Owner = 4,

        /// <summary>
        /// The retrieved handle identifies the child window at the top of the Z order, if the specified window is a parent window; otherwise, the retrieved handle is NULL. The function examines only child windows of the specified window. It does not examine descendant windows.
        /// </summary>
        Child = 5,

        /// <summary>
        /// The retrieved handle identifies the enabled popup window owned by the specified window (the search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled popup windows, the retrieved handle is that of the specified window. 
        /// </summary>
        EnabledPopup = 6
    }

    /// <summary>
    /// Send Windows Message with Timeout Flags.
    /// </summary>
    [Flags]
    public enum SendMessageTimeoutFlags
    {
        /// <summary>
        /// Normal.
        /// </summary>
        SMTO_NORMAL = 0x0000,
        /// <summary>
        /// Block.
        /// </summary>
        SMTO_BLOCK = 0x0001,
        /// <summary>
        /// Abort if hung.
        /// </summary>
        SMTO_ABORTIFHUNG = 0x0002,
        /// <summary>
        /// To timeout if not hung.
        /// </summary>
        SMTO_NOTIMEOUTIFNOTHUNG = 0x0008,
    }

    /// <summary>
    /// Shutdown Reasons.
    /// </summary>
    [Flags]
    public enum ShutdownReasons : int
    {
        /// <summary>
        /// MajorApplication
        /// </summary>
        MajorApplication = 0x00040000,
        /// <summary>
        /// MajorHardware
        /// </summary>
        MajorHardware = 0x00010000,
        /// <summary>
        /// MajorLegacyApi
        /// </summary>
        MajorLegacyApi = 0x00070000,
        /// <summary>
        /// MajorOperatingSystem
        /// </summary>
        MajorOperatingSystem = 0x00020000,
        /// <summary>
        /// MajorOther
        /// </summary>
        MajorOther = 0x00000000,
        /// <summary>
        /// MajorPower
        /// </summary>
        MajorPower = 0x00060000,
        /// <summary>
        /// MajorSoftware
        /// </summary>
        MajorSoftware = 0x00030000,
        /// <summary>
        /// MajorSystem
        /// </summary>
        MajorSystem = 0x00050000,

        /// <summary>
        /// MinorBlueScreen
        /// </summary>
        MinorBlueScreen = 0x0000000F,
        /// <summary>
        /// MinorCordUnplugged
        /// </summary>
        MinorCordUnplugged = 0x0000000b,
        /// <summary>
        /// MinorDisk
        /// </summary>
        MinorDisk = 0x00000007,
        /// <summary>
        /// MinorEnvironment
        /// </summary>
        MinorEnvironment = 0x0000000c,
        /// <summary>
        /// MinorHardwareDriver
        /// </summary>
        MinorHardwareDriver = 0x0000000d,
        /// <summary>
        /// MinorHotfix
        /// </summary>
        MinorHotfix = 0x00000011,
        /// <summary>
        /// MinorHung
        /// </summary>
        MinorHung = 0x00000005,
        /// <summary>
        /// MinorInstallation
        /// </summary>
        MinorInstallation = 0x00000002,
        /// <summary>
        /// MinorMaintenance
        /// </summary>
        MinorMaintenance = 0x00000001,
        /// <summary>
        /// MinorMMC
        /// </summary>
        MinorMMC = 0x00000019,
        /// <summary>
        /// MinorNetworkConnectivity
        /// </summary>
        MinorNetworkConnectivity = 0x00000014,
        /// <summary>
        /// MinorNetworkCard
        /// </summary>
        MinorNetworkCard = 0x00000009,
        /// <summary>
        /// MinorOther
        /// </summary>
        MinorOther = 0x00000000,
        /// <summary>
        /// MinorOtherDriver
        /// </summary>
        MinorOtherDriver = 0x0000000e,
        /// <summary>
        /// MinorPowerSupply
        /// </summary>
        MinorPowerSupply = 0x0000000a,
        /// <summary>
        /// MinorProcessor
        /// </summary>
        MinorProcessor = 0x00000008,
        /// <summary>
        /// MinorReconfig
        /// </summary>
        MinorReconfig = 0x00000004,
        /// <summary>
        /// MinorSecurity
        /// </summary>
        MinorSecurity = 0x00000013,
        /// <summary>
        /// MinorSecurityFix
        /// </summary>
        MinorSecurityFix = 0x00000012,
        /// <summary>
        /// MinorSecurityFixUninstall
        /// </summary>
        MinorSecurityFixUninstall = 0x00000018,
        /// <summary>
        /// MinorServicePack
        /// </summary>
        MinorServicePack = 0x00000010,
        /// <summary>
        /// MinorServicePackUninstall
        /// </summary>
        MinorServicePackUninstall = 0x00000016,
        /// <summary>
        /// MinorTermSrv
        /// </summary>
        MinorTermSrv = 0x00000020,
        /// <summary>
        /// MinorUnstable
        /// </summary>
        MinorUnstable = 0x00000006,
        /// <summary>
        /// MinorUpgrade
        /// </summary>
        MinorUpgrade = 0x00000003,
        /// <summary>
        /// MinorWMI
        /// </summary>
        MinorWMI = 0x00000015,

        /// <summary>
        /// FlagUserDefined
        /// </summary>
        FlagUserDefined = 0x40000000,

        //FlagPlanned               = 0x80000000,
    }

    /// <summary>
    /// Windows Message System Commands.
    /// </summary>
    public enum SysCommand
    {
        /// <summary>
        /// SC_SIZE
        /// </summary>
        SC_SIZE = 0xF000,
        /// <summary>
        /// SC_MOVE
        /// </summary>
        SC_MOVE = 0xF010,
        /// <summary>
        /// SC_MINIMIZE
        /// </summary>
        SC_MINIMIZE = 0xF020,
        /// <summary>
        /// SC_MAXIMIZE
        /// </summary>
        SC_MAXIMIZE = 0xF030,
        /// <summary>
        /// SC_NEXTWINDOW
        /// </summary>
        SC_NEXTWINDOW = 0xF040,
        /// <summary>
        /// SC_PREVWINDOW
        /// </summary>
        SC_PREVWINDOW = 0xF050,
        /// <summary>
        /// SC_CLOSE
        /// </summary>
        SC_CLOSE = 0xF060,
        /// <summary>
        /// SC_VSCROLL
        /// </summary>
        SC_VSCROLL = 0xF070,
        /// <summary>
        /// SC_HSCROLL
        /// </summary>
        SC_HSCROLL = 0xF080,
        /// <summary>
        /// SC_MOUSEMENU
        /// </summary>
        SC_MOUSEMENU = 0xF090,
        /// <summary>
        /// SC_KEYMENU
        /// </summary>
        SC_KEYMENU = 0xF100,
        /// <summary>
        /// SC_ARRANGE
        /// </summary>
        SC_ARRANGE = 0xF110,
        /// <summary>
        /// SC_RESTORE
        /// </summary>
        SC_RESTORE = 0xF120,
        /// <summary>
        /// SC_TASKLIST
        /// </summary>
        SC_TASKLIST = 0xF130,
        /// <summary>
        /// SC_SCREENSAVE
        /// </summary>
        SC_SCREENSAVE = 0xF140,
        /// <summary>
        /// SC_HOTKEY
        /// </summary>
        SC_HOTKEY = 0xF150,
        /// <summary>
        /// SC_DEFAULT
        /// </summary>
        SC_DEFAULT = 0xF160,
        /// <summary>
        /// SC_MONITORPOWER
        /// </summary>
        SC_MONITORPOWER = 0xF170,
        /// <summary>
        /// SC_CONTEXTHELP
        /// </summary>
        SC_CONTEXTHELP = 0xF180,
        /// <summary>
        /// SC_SEPARATOR
        /// </summary>
        SC_SEPARATOR = 0xF00F,

        /// <summary>
        /// SCF_ISSECURE
        /// </summary>
        SCF_ISSECURE = 0x00000001,

        /// <summary>
        /// SC_ICON
        /// </summary>
        SC_ICON = SC_MINIMIZE,
        /// <summary>
        /// SC_ZOOM
        /// </summary>
        SC_ZOOM = SC_MAXIMIZE,
    }

    /// <summary>
    /// Win32 Window Extended Styles.
    /// </summary>
    [Flags]
    public enum WindowExStyles : int
    {
        /// <summary>
        /// Specifies that a window created with this style accepts drag-drop files.
        /// </summary>
        WS_EX_ACCEPTFILES = 0x00000010,
        /// <summary>
        /// Forces a top-level window onto the taskbar when the window is visible.
        /// </summary>
        WS_EX_APPWINDOW = 0x00040000,
        /// <summary>
        /// Specifies that a window has a border with a sunken edge.
        /// </summary>
        WS_EX_CLIENTEDGE = 0x00000200,
        /// <summary>
        /// Windows XP: Paints all descendants of a window in bottom-to-top painting order using double-buffering. For more information, see Remarks. This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.
        /// </summary>
        WS_EX_COMPOSITED = 0x02000000,
        /// <summary>
        /// Includes a question mark in the title bar of the window. When the user clicks the question mark, the cursor changes to a question mark with a pointer. If the user then clicks a child window, the child receives a WM_HELP message. The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command. The Help application displays a pop-up window that typically contains help for the child window.
        /// WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.
        /// </summary>
        WS_EX_CONTEXTHELP = 0x00000400,
        /// <summary>
        /// The window itself contains child windows that should take part in dialog box navigation. If this style is specified, the dialog manager recurses into children of this window when performing navigation operations such as handling the TAB key, an arrow key, or a keyboard mnemonic.
        /// </summary>
        WS_EX_CONTROLPARENT = 0x00010000,
        /// <summary>
        /// Creates a window that has a double border; the window can, optionally, be created with a title bar by specifying the WS_CAPTION style in the dwStyle parameter.
        /// </summary>
        WS_EX_DLGMODALFRAME = 0x00000001,
        /// <summary>
        /// Windows 2000/XP: Creates a layered window. Be aware that this cannot be used for child windows. Also, this cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.
        /// </summary>
        WS_EX_LAYERED = 0x00080000,
        /// <summary>
        /// Arabic and Hebrew versions of Windows 98/Me, Windows 2000/XP: Creates a window whose horizontal origin is on the right edge. Increasing horizontal values advance to the left.
        /// </summary>
        WS_EX_LAYOUTRTL = 0x00400000,
        /// <summary>
        /// Creates a window that has generic left-aligned properties. This is the default.
        /// </summary>
        WS_EX_LEFT = 0x00000000,
        /// <summary>
        /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the vertical scroll bar (if present) is to the left of the client area. For other languages, the style is ignored.
        /// </summary>
        WS_EX_LEFTSCROLLBAR = 0x00004000,
        /// <summary>
        /// The window text is displayed using left-to-right reading-order properties. This is the default.
        /// </summary>
        WS_EX_LTRREADING = 0x00000000,
        /// <summary>
        /// Creates a multiple-document interface (MDI) child window.
        /// </summary>
        WS_EX_MDICHILD = 0x00000040,
        /// <summary>
        /// Windows 2000/XP: A top-level window created with this style does not become the foreground window when the user clicks it. The system does not bring this window to the foreground when the user minimizes or closes the foreground window.
        /// To activate the window, use the SetActiveWindow or SetForegroundWindow function.
        /// The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
        /// </summary>
        WS_EX_NOACTIVATE = 0x08000000,
        /// <summary>
        /// Windows 2000/XP: A window created with this style does not pass its window layout to its child windows.
        /// </summary>
        WS_EX_NOINHERITLAYOUT = 0x00100000,
        /// <summary>
        /// Specifies that a child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
        /// </summary>
        WS_EX_NOPARENTNOTIFY = 0x00000004,
        /// <summary>
        /// Combines the WS_EX_CLIENTEDGE and WS_EX_WINDOWEDGE styles.
        /// </summary>
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
        /// <summary>
        /// Combines the WS_EX_WINDOWEDGE, WS_EX_TOOLWINDOW, and WS_EX_TOPMOST styles.
        /// </summary>
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
        /// <summary>
        /// The window has generic "right-aligned" properties. This depends on the window class. This style has an effect only if the shell language is Hebrew, Arabic, or another language that supports reading-order alignment; otherwise, the style is ignored.
        /// Using the WS_EX_RIGHT style for static or edit controls has the same effect as using the SS_RIGHT or ES_RIGHT style, respectively. Using this style with button controls has the same effect as using BS_RIGHT and BS_RIGHTBUTTON styles.
        /// </summary>
        WS_EX_RIGHT = 0x00001000,
        /// <summary>
        /// Vertical scroll bar (if present) is to the right of the client area. This is the default.
        /// </summary>
        WS_EX_RIGHTSCROLLBAR = 0x00000000,
        /// <summary>
        /// If the shell language is Hebrew, Arabic, or another language that supports reading-order alignment, the window text is displayed using right-to-left reading-order properties. For other languages, the style is ignored.
        /// </summary>
        WS_EX_RTLREADING = 0x00002000,
        /// <summary>
        /// Creates a window with a three-dimensional border style intended to be used for items that do not accept user input.
        /// </summary>
        WS_EX_STATICEDGE = 0x00020000,
        /// <summary>
        /// Creates a tool window; that is, a window intended to be used as a floating toolbar. A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font. A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB. If a tool window has a system menu, its icon is not displayed on the title bar. However, you can display the system menu by right-clicking or by typing ALT+SPACE.
        /// </summary>
        WS_EX_TOOLWINDOW = 0x00000080,
        /// <summary>
        /// Specifies that a window created with this style should be placed above all non-topmost windows and should stay above them, even when the window is deactivated. To add or remove this style, use the SetWindowPos function.
        /// </summary>
        WS_EX_TOPMOST = 0x00000008,
        /// <summary>
        /// Specifies that a window created with this style should not be painted until siblings beneath the window (that were created by the same thread) have been painted. The window appears transparent because the bits of underlying sibling windows have already been painted.
        /// To achieve transparency without these restrictions, use the SetWindowRgn function.
        /// </summary>
        WS_EX_TRANSPARENT = 0x00000020,
        /// <summary>
        /// Specifies that a window has a border with a raised edge.
        /// </summary>
        WS_EX_WINDOWEDGE = 0x00000100
    }

    /// <summary>
    /// Windows Message types.
    /// see: http://www.pinvoke.net/default.aspx/Constants/WM.html
    /// </summary>
    public enum WindowsMessage : int
    {
        /// <summary>
        /// The WM_NULL message performs no operation. An application sends the WM_NULL message if it wants to post a message that the recipient window will ignore.
        /// </summary>
        NULL = 0x0000,
        /// <summary>
        /// The WM_CREATE message is sent when an application requests that a window be created by calling the CreateWindowEx or CreateWindow function. (The message is sent before the function returns.) The window procedure of the new window receives this message after the window is created, but before the window becomes visible.
        /// </summary>
        CREATE = 0x0001,
        /// <summary>
        /// The WM_DESTROY message is sent when a window is being destroyed. It is sent to the window procedure of the window being destroyed after the window is removed from the screen. 
        /// This message is sent first to the window being destroyed and then to the child windows (if any) as they are destroyed. During the processing of the message, it can be assumed that all child windows still exist.
        /// /// </summary>
        DESTROY = 0x0002,
        /// <summary>
        /// The WM_MOVE message is sent after a window has been moved. 
        /// </summary>
        MOVE = 0x0003,
        /// <summary>
        /// The WM_SIZE message is sent to a window after its size has changed.
        /// </summary>
        SIZE = 0x0005,
        /// <summary>
        /// The WM_ACTIVATE message is sent to both the window being activated and the window being deactivated. If the windows use the same input queue, the message is sent synchronously, first to the window procedure of the top-level window being deactivated, then to the window procedure of the top-level window being activated. If the windows use different input queues, the message is sent asynchronously, so the window is activated immediately. 
        /// </summary>
        ACTIVATE = 0x0006,
        /// <summary>
        /// The WM_SETFOCUS message is sent to a window after it has gained the keyboard focus. 
        /// </summary>
        SETFOCUS = 0x0007,
        /// <summary>
        /// The WM_KILLFOCUS message is sent to a window immediately before it loses the keyboard focus. 
        /// </summary>
        KILLFOCUS = 0x0008,
        /// <summary>
        /// The WM_ENABLE message is sent when an application changes the enabled state of a window. It is sent to the window whose enabled state is changing. This message is sent before the EnableWindow function returns, but after the enabled state (WS_DISABLED style bit) of the window has changed. 
        /// </summary>
        ENABLE = 0x000A,
        /// <summary>
        /// An application sends the WM_SETREDRAW message to a window to allow changes in that window to be redrawn or to prevent changes in that window from being redrawn. 
        /// </summary>
        SETREDRAW = 0x000B,
        /// <summary>
        /// An application sends a WM_SETTEXT message to set the text of a window. 
        /// </summary>
        SETTEXT = 0x000C,
        /// <summary>
        /// An application sends a WM_GETTEXT message to copy the text that corresponds to a window into a buffer provided by the caller. 
        /// </summary>
        GETTEXT = 0x000D,
        /// <summary>
        /// An application sends a WM_GETTEXTLENGTH message to determine the length, in characters, of the text associated with a window. 
        /// </summary>
        GETTEXTLENGTH = 0x000E,
        /// <summary>
        /// The WM_PAINT message is sent when the system or another application makes a request to paint a portion of an application's window. The message is sent when the UpdateWindow or RedrawWindow function is called, or by the DispatchMessage function when the application obtains a WM_PAINT message by using the GetMessage or PeekMessage function. 
        /// </summary>
        PAINT = 0x000F,
        /// <summary>
        /// The WM_CLOSE message is sent as a signal that a window or an application should terminate.
        /// </summary>
        CLOSE = 0x0010,
        /// <summary>
        /// The WM_QUERYENDSESSION message is sent when the user chooses to end the session or when an application calls one of the system shutdown functions. If any application returns zero, the session is not ended. The system stops sending WM_QUERYENDSESSION messages as soon as one application returns zero.
        /// After processing this message, the system sends the WM_ENDSESSION message with the wParam parameter set to the results of the WM_QUERYENDSESSION message.
        /// </summary>
        QUERYENDSESSION = 0x0011,
        /// <summary>
        /// The WM_QUERYOPEN message is sent to an icon when the user requests that the window be restored to its previous size and position.
        /// </summary>
        QUERYOPEN = 0x0013,
        /// <summary>
        /// The WM_ENDSESSION message is sent to an application after the system processes the results of the WM_QUERYENDSESSION message. The WM_ENDSESSION message informs the application whether the session is ending.
        /// </summary>
        ENDSESSION = 0x0016,
        /// <summary>
        /// The WM_QUIT message indicates a request to terminate an application and is generated when the application calls the PostQuitMessage function. It causes the GetMessage function to return zero.
        /// </summary>
        QUIT = 0x0012,
        /// <summary>
        /// The WM_ERASEBKGND message is sent when the window background must be erased (for example, when a window is resized). The message is sent to prepare an invalidated portion of a window for painting. 
        /// </summary>
        ERASEBKGND = 0x0014,
        /// <summary>
        /// This message is sent to all top-level windows when a change is made to a system color setting. 
        /// </summary>
        SYSCOLORCHANGE = 0x0015,
        /// <summary>
        /// The WM_SHOWWINDOW message is sent to a window when the window is about to be hidden or shown.
        /// </summary>
        SHOWWINDOW = 0x0018,
        /// <summary>
        /// An application sends the WM_WININICHANGE message to all top-level windows after making a change to the WIN.INI file. The SystemParametersInfo function sends this message after an application uses the function to change a setting in WIN.INI.
        /// Note  The WM_WININICHANGE message is provided only for compatibility with earlier versions of the system. Applications should use the WM_SETTINGCHANGE message.
        /// </summary>
        WININICHANGE = 0x001A,
        /// <summary>
        /// An application sends the WM_WININICHANGE message to all top-level windows after making a change to the WIN.INI file. The SystemParametersInfo function sends this message after an application uses the function to change a setting in WIN.INI.
        /// Note  The WM_WININICHANGE message is provided only for compatibility with earlier versions of the system. Applications should use the WM_SETTINGCHANGE message.
        /// </summary>
        SETTINGCHANGE = WININICHANGE,
        /// <summary>
        /// The WM_DEVMODECHANGE message is sent to all top-level windows whenever the user changes device-mode settings. 
        /// </summary>
        DEVMODECHANGE = 0x001B,
        /// <summary>
        /// The WM_ACTIVATEAPP message is sent when a window belonging to a different application than the active window is about to be activated. The message is sent to the application whose window is being activated and to the application whose window is being deactivated.
        /// </summary>
        ACTIVATEAPP = 0x001C,
        /// <summary>
        /// An application sends the WM_FONTCHANGE message to all top-level windows in the system after changing the pool of font resources. 
        /// </summary>
        FONTCHANGE = 0x001D,
        /// <summary>
        /// A message that is sent whenever there is a change in the system time.
        /// </summary>
        TIMECHANGE = 0x001E,
        /// <summary>
        /// The WM_CANCELMODE message is sent to cancel certain modes, such as mouse capture. For example, the system sends this message to the active window when a dialog box or message box is displayed. Certain functions also send this message explicitly to the specified window regardless of whether it is the active window. For example, the EnableWindow function sends this message when disabling the specified window.
        /// </summary>
        CANCELMODE = 0x001F,
        /// <summary>
        /// The WM_SETCURSOR message is sent to a window if the mouse causes the cursor to move within a window and mouse input is not captured. 
        /// </summary>
        SETCURSOR = 0x0020,
        /// <summary>
        /// The WM_MOUSEACTIVATE message is sent when the cursor is in an inactive window and the user presses a mouse button. The parent window receives this message only if the child window passes it to the DefWindowProc function.
        /// </summary>
        MOUSEACTIVATE = 0x0021,
        /// <summary>
        /// The WM_CHILDACTIVATE message is sent to a child window when the user clicks the window's title bar or when the window is activated, moved, or sized.
        /// </summary>
        CHILDACTIVATE = 0x0022,
        /// <summary>
        /// The WM_QUEUESYNC message is sent by a computer-based training (CBT) application to separate user-input messages from other messages sent through the WH_JOURNALPLAYBACK Hook procedure. 
        /// </summary>
        QUEUESYNC = 0x0023,
        /// <summary>
        /// The WM_GETMINMAXINFO message is sent to a window when the size or position of the window is about to change. An application can use this message to override the window's default maximized size and position, or its default minimum or maximum tracking size. 
        /// </summary>
        GETMINMAXINFO = 0x0024,
        /// <summary>
        /// Windows NT 3.51 and earlier: The WM_PAINTICON message is sent to a minimized window when the icon is to be painted. This message is not sent by newer versions of Microsoft Windows, except in unusual circumstances explained in the Remarks.
        /// </summary>
        PAINTICON = 0x0026,
        /// <summary>
        /// Windows NT 3.51 and earlier: The WM_ICONERASEBKGND message is sent to a minimized window when the background of the icon must be filled before painting the icon. A window receives this message only if a class icon is defined for the window; otherwise, WM_ERASEBKGND is sent. This message is not sent by newer versions of Windows.
        /// </summary>
        ICONERASEBKGND = 0x0027,
        /// <summary>
        /// The WM_NEXTDLGCTL message is sent to a dialog box procedure to set the keyboard focus to a different control in the dialog box. 
        /// </summary>
        NEXTDLGCTL = 0x0028,
        /// <summary>
        /// The WM_SPOOLERSTATUS message is sent from Print Manager whenever a job is added to or removed from the Print Manager queue. 
        /// </summary>
        SPOOLERSTATUS = 0x002A,
        /// <summary>
        /// The WM_DRAWITEM message is sent to the parent window of an owner-drawn button, combo box, list box, or menu when a visual aspect of the button, combo box, list box, or menu has changed.
        /// </summary>
        DRAWITEM = 0x002B,
        /// <summary>
        /// The WM_MEASUREITEM message is sent to the owner window of a combo box, list box, list view control, or menu item when the control or menu is created.
        /// </summary>
        MEASUREITEM = 0x002C,
        /// <summary>
        /// Sent to the owner of a list box or combo box when the list box or combo box is destroyed or when items are removed by the LB_DELETESTRING, LB_RESETCONTENT, CB_DELETESTRING, or CB_RESETCONTENT message. The system sends a WM_DELETEITEM message for each deleted item. The system sends the WM_DELETEITEM message for any deleted list box or combo box item with nonzero item data.
        /// </summary>
        DELETEITEM = 0x002D,
        /// <summary>
        /// Sent by a list box with the LBS_WANTKEYBOARDINPUT style to its owner in response to a WM_KEYDOWN message. 
        /// </summary>
        VKEYTOITEM = 0x002E,
        /// <summary>
        /// Sent by a list box with the LBS_WANTKEYBOARDINPUT style to its owner in response to a WM_CHAR message. 
        /// </summary>
        CHARTOITEM = 0x002F,
        /// <summary>
        /// An application sends a WM_SETFONT message to specify the font that a control is to use when drawing text. 
        /// </summary>
        SETFONT = 0x0030,
        /// <summary>
        /// An application sends a WM_GETFONT message to a control to retrieve the font with which the control is currently drawing its text. 
        /// </summary>
        GETFONT = 0x0031,
        /// <summary>
        /// An application sends a WM_SETHOTKEY message to a window to associate a hot key with the window. When the user presses the hot key, the system activates the window. 
        /// </summary>
        SETHOTKEY = 0x0032,
        /// <summary>
        /// An application sends a WM_GETHOTKEY message to determine the hot key associated with a window. 
        /// </summary>
        GETHOTKEY = 0x0033,
        /// <summary>
        /// The WM_QUERYDRAGICON message is sent to a minimized (iconic) window. The window is about to be dragged by the user but does not have an icon defined for its class. An application can return a handle to an icon or cursor. The system displays this cursor or icon while the user drags the icon.
        /// </summary>
        QUERYDRAGICON = 0x0037,
        /// <summary>
        /// The system sends the WM_COMPAREITEM message to determine the relative position of a new item in the sorted list of an owner-drawn combo box or list box. Whenever the application adds a new item, the system sends this message to the owner of a combo box or list box created with the CBS_SORT or LBS_SORT style. 
        /// </summary>
        COMPAREITEM = 0x0039,
        /// <summary>
        /// Active Accessibility sends the WM_GETOBJECT message to obtain information about an accessible object contained in a server application. 
        /// Applications never send this message directly. It is sent only by Active Accessibility in response to calls to AccessibleObjectFromPoint, AccessibleObjectFromEvent, or AccessibleObjectFromWindow. However, server applications handle this message. 
        /// </summary>
        GETOBJECT = 0x003D,
        /// <summary>
        /// The WM_COMPACTING message is sent to all top-level windows when the system detects more than 12.5 percent of system time over a 30- to 60-second interval is being spent compacting memory. This indicates that system memory is low.
        /// </summary>
        COMPACTING = 0x0041,
        /// <summary>
        /// WM_COMMNOTIFY is Obsolete for Win32-Based Applications
        /// </summary>
        [Obsolete]
        COMMNOTIFY = 0x0044,
        /// <summary>
        /// The WM_WINDOWPOSCHANGING message is sent to a window whose size, position, or place in the Z order is about to change as a result of a call to the SetWindowPos function or another window-management function.
        /// </summary>
        WINDOWPOSCHANGING = 0x0046,
        /// <summary>
        /// The WM_WINDOWPOSCHANGED message is sent to a window whose size, position, or place in the Z order has changed as a result of a call to the SetWindowPos function or another window-management function.
        /// </summary>
        WINDOWPOSCHANGED = 0x0047,
        /// <summary>
        /// Notifies applications that the system, typically a battery-powered personal computer, is about to enter a suspended mode.
        /// Use: POWERBROADCAST
        /// </summary>
        [Obsolete]
        POWER = 0x0048,
        /// <summary>
        /// An application sends the WM_COPYDATA message to pass data to another application. 
        /// </summary>
        COPYDATA = 0x004A,
        /// <summary>
        /// The WM_CANCELJOURNAL message is posted to an application when a user cancels the application's journaling activities. The message is posted with a NULL window handle. 
        /// </summary>
        CANCELJOURNAL = 0x004B,
        /// <summary>
        /// Sent by a common control to its parent window when an event has occurred or the control requires some information. 
        /// </summary>
        NOTIFY = 0x004E,
        /// <summary>
        /// The WM_INPUTLANGCHANGEREQUEST message is posted to the window with the focus when the user chooses a new input language, either with the hotkey (specified in the Keyboard control panel application) or from the indicator on the system taskbar. An application can accept the change by passing the message to the DefWindowProc function or reject the change (and prevent it from taking place) by returning immediately. 
        /// </summary>
        INPUTLANGCHANGEREQUEST = 0x0050,
        /// <summary>
        /// The WM_INPUTLANGCHANGE message is sent to the topmost affected window after an application's input language has been changed. You should make any application-specific settings and pass the message to the DefWindowProc function, which passes the message to all first-level child windows. These child windows can pass the message to DefWindowProc to have it pass the message to their child windows, and so on. 
        /// </summary>
        INPUTLANGCHANGE = 0x0051,
        /// <summary>
        /// Sent to an application that has initiated a training card with Microsoft Windows Help. The message informs the application when the user clicks an authorable button. An application initiates a training card by specifying the HELP_TCARD command in a call to the WinHelp function.
        /// </summary>
        TCARD = 0x0052,
        /// <summary>
        /// Indicates that the user pressed the F1 key. If a menu is active when F1 is pressed, WM_HELP is sent to the window associated with the menu; otherwise, WM_HELP is sent to the window that has the keyboard focus. If no window has the keyboard focus, WM_HELP is sent to the currently active window. 
        /// </summary>
        HELP = 0x0053,
        /// <summary>
        /// The WM_USERCHANGED message is sent to all windows after the user has logged on or off. When the user logs on or off, the system updates the user-specific settings. The system sends this message immediately after updating the settings.
        /// </summary>
        USERCHANGED = 0x0054,
        /// <summary>
        /// Determines if a window accepts ANSI or Unicode structures in the WM_NOTIFY notification message. WM_NOTIFYFORMAT messages are sent from a common control to its parent window and from the parent window to the common control.
        /// </summary>
        NOTIFYFORMAT = 0x0055,
        /// <summary>
        /// The WM_CONTEXTMENU message notifies a window that the user clicked the right mouse button (right-clicked) in the window.
        /// </summary>
        CONTEXTMENU = 0x007B,
        /// <summary>
        /// The WM_STYLECHANGING message is sent to a window when the SetWindowLong function is about to change one or more of the window's styles.
        /// </summary>
        STYLECHANGING = 0x007C,
        /// <summary>
        /// The WM_STYLECHANGED message is sent to a window after the SetWindowLong function has changed one or more of the window's styles
        /// </summary>
        STYLECHANGED = 0x007D,
        /// <summary>
        /// The WM_DISPLAYCHANGE message is sent to all windows when the display resolution has changed.
        /// </summary>
        DISPLAYCHANGE = 0x007E,
        /// <summary>
        /// The WM_GETICON message is sent to a window to retrieve a handle to the large or small icon associated with a window. The system displays the large icon in the ALT+TAB dialog, and the small icon in the window caption. 
        /// </summary>
        GETICON = 0x007F,
        /// <summary>
        /// An application sends the WM_SETICON message to associate a new large or small icon with a window. The system displays the large icon in the ALT+TAB dialog box, and the small icon in the window caption. 
        /// </summary>
        SETICON = 0x0080,
        /// <summary>
        /// The WM_NCCREATE message is sent prior to the WM_CREATE message when a window is first created.
        /// </summary>
        NCCREATE = 0x0081,
        /// <summary>
        /// The WM_NCDESTROY message informs a window that its nonclient area is being destroyed. The DestroyWindow function sends the WM_NCDESTROY message to the window following the WM_DESTROY message. WM_DESTROY is used to free the allocated memory object associated with the window. 
        /// The WM_NCDESTROY message is sent after the child windows have been destroyed. In contrast, WM_DESTROY is sent before the child windows are destroyed.
        /// </summary>
        NCDESTROY = 0x0082,
        /// <summary>
        /// The WM_NCCALCSIZE message is sent when the size and position of a window's client area must be calculated. By processing this message, an application can control the content of the window's client area when the size or position of the window changes.
        /// </summary>
        NCCALCSIZE = 0x0083,
        /// <summary>
        /// The WM_NCHITTEST message is sent to a window when the cursor moves, or when a mouse button is pressed or released. If the mouse is not captured, the message is sent to the window beneath the cursor. Otherwise, the message is sent to the window that has captured the mouse.
        /// </summary>
        NCHITTEST = 0x0084,
        /// <summary>
        /// The WM_NCPAINT message is sent to a window when its frame must be painted. 
        /// </summary>
        NCPAINT = 0x0085,
        /// <summary>
        /// The WM_NCACTIVATE message is sent to a window when its nonclient area needs to be changed to indicate an active or inactive state.
        /// </summary>
        NCACTIVATE = 0x0086,
        /// <summary>
        /// The WM_GETDLGCODE message is sent to the window procedure associated with a control. By default, the system handles all keyboard input to the control; the system interprets certain types of keyboard input as dialog box navigation keys. To override this default behavior, the control can respond to the WM_GETDLGCODE message to indicate the types of input it wants to process itself.
        /// </summary>
        GETDLGCODE = 0x0087,
        /// <summary>
        /// The WM_SYNCPAINT message is used to synchronize painting while avoiding linking independent GUI threads.
        /// </summary>
        SYNCPAINT = 0x0088,
        /// <summary>
        /// The WM_NCMOUSEMOVE message is posted to a window when the cursor is moved within the nonclient area of the window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCMOUSEMOVE = 0x00A0,
        /// <summary>
        /// The WM_NCLBUTTONDOWN message is posted when the user presses the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCLBUTTONDOWN = 0x00A1,
        /// <summary>
        /// The WM_NCLBUTTONUP message is posted when the user releases the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCLBUTTONUP = 0x00A2,
        /// <summary>
        /// The WM_NCLBUTTONDBLCLK message is posted when the user double-clicks the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCLBUTTONDBLCLK = 0x00A3,
        /// <summary>
        /// The WM_NCRBUTTONDOWN message is posted when the user presses the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCRBUTTONDOWN = 0x00A4,
        /// <summary>
        /// The WM_NCRBUTTONUP message is posted when the user releases the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCRBUTTONUP = 0x00A5,
        /// <summary>
        /// The WM_NCRBUTTONDBLCLK message is posted when the user double-clicks the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCRBUTTONDBLCLK = 0x00A6,
        /// <summary>
        /// The WM_NCMBUTTONDOWN message is posted when the user presses the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCMBUTTONDOWN = 0x00A7,
        /// <summary>
        /// The WM_NCMBUTTONUP message is posted when the user releases the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCMBUTTONUP = 0x00A8,
        /// <summary>
        /// The WM_NCMBUTTONDBLCLK message is posted when the user double-clicks the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCMBUTTONDBLCLK = 0x00A9,
        /// <summary>
        /// The WM_NCXBUTTONDOWN message is posted when the user presses the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCXBUTTONDOWN = 0x00AB,
        /// <summary>
        /// The WM_NCXBUTTONUP message is posted when the user releases the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCXBUTTONUP = 0x00AC,
        /// <summary>
        /// The WM_NCXBUTTONDBLCLK message is posted when the user double-clicks the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        NCXBUTTONDBLCLK = 0x00AD,
        /// <summary>
        /// The WM_INPUT_DEVICE_CHANGE message is sent to the window that registered to receive raw input. A window receives this message through its WindowProc function.
        /// </summary>
        INPUT_DEVICE_CHANGE = 0x00FE,
        /// <summary>
        /// The WM_INPUT message is sent to the window that is getting raw input. 
        /// </summary>
        INPUT = 0x00FF,
        /// <summary>
        /// This message filters for keyboard messages.
        /// </summary>
        KEYFIRST = 0x0100,
        /// <summary>
        /// The WM_KEYDOWN message is posted to the window with the keyboard focus when a nonsystem key is pressed. A nonsystem key is a key that is pressed when the ALT key is not pressed. 
        /// </summary>
        KEYDOWN = 0x0100,
        /// <summary>
        /// The WM_KEYUP message is posted to the window with the keyboard focus when a nonsystem key is released. A nonsystem key is a key that is pressed when the ALT key is not pressed, or a keyboard key that is pressed when a window has the keyboard focus. 
        /// </summary>
        KEYUP = 0x0101,
        /// <summary>
        /// The WM_CHAR message is posted to the window with the keyboard focus when a WM_KEYDOWN message is translated by the TranslateMessage function. The WM_CHAR message contains the character code of the key that was pressed. 
        /// </summary>
        CHAR = 0x0102,
        /// <summary>
        /// The WM_DEADCHAR message is posted to the window with the keyboard focus when a WM_KEYUP message is translated by the TranslateMessage function. WM_DEADCHAR specifies a character code generated by a dead key. A dead key is a key that generates a character, such as the umlaut (double-dot), that is combined with another character to form a composite character. For example, the umlaut-O character (Ö) is generated by typing the dead key for the umlaut character, and then typing the O key. 
        /// </summary>
        DEADCHAR = 0x0103,
        /// <summary>
        /// The WM_SYSKEYDOWN message is posted to the window with the keyboard focus when the user presses the F10 key (which activates the menu bar) or holds down the ALT key and then presses another key. It also occurs when no window currently has the keyboard focus; in this case, the WM_SYSKEYDOWN message is sent to the active window. The window that receives the message can distinguish between these two contexts by checking the context code in the lParam parameter. 
        /// </summary>
        SYSKEYDOWN = 0x0104,
        /// <summary>
        /// The WM_SYSKEYUP message is posted to the window with the keyboard focus when the user releases a key that was pressed while the ALT key was held down. It also occurs when no window currently has the keyboard focus; in this case, the WM_SYSKEYUP message is sent to the active window. The window that receives the message can distinguish between these two contexts by checking the context code in the lParam parameter. 
        /// </summary>
        SYSKEYUP = 0x0105,
        /// <summary>
        /// The WM_SYSCHAR message is posted to the window with the keyboard focus when a WM_SYSKEYDOWN message is translated by the TranslateMessage function. It specifies the character code of a system character key — that is, a character key that is pressed while the ALT key is down. 
        /// </summary>
        SYSCHAR = 0x0106,
        /// <summary>
        /// The WM_SYSDEADCHAR message is sent to the window with the keyboard focus when a WM_SYSKEYDOWN message is translated by the TranslateMessage function. WM_SYSDEADCHAR specifies the character code of a system dead key — that is, a dead key that is pressed while holding down the ALT key. 
        /// </summary>
        SYSDEADCHAR = 0x0107,
        /// <summary>
        /// The WM_UNICHAR message is posted to the window with the keyboard focus when a WM_KEYDOWN message is translated by the TranslateMessage function. The WM_UNICHAR message contains the character code of the key that was pressed. 
        /// The WM_UNICHAR message is equivalent to WM_CHAR, but it uses Unicode Transformation Format (UTF)-32, whereas WM_CHAR uses UTF-16. It is designed to send or post Unicode characters to ANSI windows and it can can handle Unicode Supplementary Plane characters.
        /// </summary>
        UNICHAR = 0x0109,
        /// <summary>
        /// This message filters for keyboard messages.
        /// </summary>
        KEYLAST = 0x0109,
        /// <summary>
        /// Sent immediately before the IME generates the composition string as a result of a keystroke. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_STARTCOMPOSITION = 0x010D,
        /// <summary>
        /// Sent to an application when the IME ends composition. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_ENDCOMPOSITION = 0x010E,
        /// <summary>
        /// Sent to an application when the IME changes composition status as a result of a keystroke. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_COMPOSITION = 0x010F,
        IME_KEYLAST = 0x010F,
        /// <summary>
        /// The WM_INITDIALOG message is sent to the dialog box procedure immediately before a dialog box is displayed. Dialog box procedures typically use this message to initialize controls and carry out any other initialization tasks that affect the appearance of the dialog box. 
        /// </summary>
        INITDIALOG = 0x0110,
        /// <summary>
        /// The WM_COMMAND message is sent when the user selects a command item from a menu, when a control sends a notification message to its parent window, or when an accelerator keystroke is translated. 
        /// </summary>
        COMMAND = 0x0111,
        /// <summary>
        /// A window receives this message when the user chooses a command from the Window menu, clicks the maximize button, minimize button, restore button, close button, or moves the form. You can stop the form from moving by filtering this out.
        /// </summary>
        SYSCOMMAND = 0x0112,
        /// <summary>
        /// The WM_TIMER message is posted to the installing thread's message queue when a timer expires. The message is posted by the GetMessage or PeekMessage function. 
        /// </summary>
        TIMER = 0x0113,
        /// <summary>
        /// The WM_HSCROLL message is sent to a window when a scroll event occurs in the window's standard horizontal scroll bar. This message is also sent to the owner of a horizontal scroll bar control when a scroll event occurs in the control. 
        /// </summary>
        HSCROLL = 0x0114,
        /// <summary>
        /// The WM_VSCROLL message is sent to a window when a scroll event occurs in the window's standard vertical scroll bar. This message is also sent to the owner of a vertical scroll bar control when a scroll event occurs in the control. 
        /// </summary>
        VSCROLL = 0x0115,
        /// <summary>
        /// The WM_INITMENU message is sent when a menu is about to become active. It occurs when the user clicks an item on the menu bar or presses a menu key. This allows the application to modify the menu before it is displayed. 
        /// </summary>
        INITMENU = 0x0116,
        /// <summary>
        /// The WM_INITMENUPOPUP message is sent when a drop-down menu or submenu is about to become active. This allows an application to modify the menu before it is displayed, without changing the entire menu. 
        /// </summary>
        INITMENUPOPUP = 0x0117,
        /// <summary>
        /// The WM_MENUSELECT message is sent to a menu's owner window when the user selects a menu item. 
        /// </summary>
        MENUSELECT = 0x011F,
        /// <summary>
        /// The WM_MENUCHAR message is sent when a menu is active and the user presses a key that does not correspond to any mnemonic or accelerator key. This message is sent to the window that owns the menu. 
        /// </summary>
        MENUCHAR = 0x0120,
        /// <summary>
        /// The WM_ENTERIDLE message is sent to the owner window of a modal dialog box or menu that is entering an idle state. A modal dialog box or menu enters an idle state when no messages are waiting in its queue after it has processed one or more previous messages. 
        /// </summary>
        ENTERIDLE = 0x0121,
        /// <summary>
        /// The WM_MENURBUTTONUP message is sent when the user releases the right mouse button while the cursor is on a menu item. 
        /// </summary>
        MENURBUTTONUP = 0x0122,
        /// <summary>
        /// The WM_MENUDRAG message is sent to the owner of a drag-and-drop menu when the user drags a menu item. 
        /// </summary>
        MENUDRAG = 0x0123,
        /// <summary>
        /// The WM_MENUGETOBJECT message is sent to the owner of a drag-and-drop menu when the mouse cursor enters a menu item or moves from the center of the item to the top or bottom of the item. 
        /// </summary>
        MENUGETOBJECT = 0x0124,
        /// <summary>
        /// The WM_UNINITMENUPOPUP message is sent when a drop-down menu or submenu has been destroyed. 
        /// </summary>
        UNINITMENUPOPUP = 0x0125,
        /// <summary>
        /// The WM_MENUCOMMAND message is sent when the user makes a selection from a menu. 
        /// </summary>
        MENUCOMMAND = 0x0126,
        /// <summary>
        /// An application sends the WM_CHANGEUISTATE message to indicate that the user interface (UI) state should be changed.
        /// </summary>
        CHANGEUISTATE = 0x0127,
        /// <summary>
        /// An application sends the WM_UPDATEUISTATE message to change the user interface (UI) state for the specified window and all its child windows.
        /// </summary>
        UPDATEUISTATE = 0x0128,
        /// <summary>
        /// An application sends the WM_QUERYUISTATE message to retrieve the user interface (UI) state for a window.
        /// </summary>
        QUERYUISTATE = 0x0129,
        /// <summary>
        /// The WM_CTLCOLORMSGBOX message is sent to the owner window of a message box before Windows draws the message box. By responding to this message, the owner window can set the text and background colors of the message box by using the given display device context handle. 
        /// </summary>
        CTLCOLORMSGBOX = 0x0132,
        /// <summary>
        /// An edit control that is not read-only or disabled sends the WM_CTLCOLOREDIT message to its parent window when the control is about to be drawn. By responding to this message, the parent window can use the specified device context handle to set the text and background colors of the edit control. 
        /// </summary>
        CTLCOLOREDIT = 0x0133,
        /// <summary>
        /// Sent to the parent window of a list box before the system draws the list box. By responding to this message, the parent window can set the text and background colors of the list box by using the specified display device context handle. 
        /// </summary>
        CTLCOLORLISTBOX = 0x0134,
        /// <summary>
        /// The WM_CTLCOLORBTN message is sent to the parent window of a button before drawing the button. The parent window can change the button's text and background colors. However, only owner-drawn buttons respond to the parent window processing this message. 
        /// </summary>
        CTLCOLORBTN = 0x0135,
        /// <summary>
        /// The WM_CTLCOLORDLG message is sent to a dialog box before the system draws the dialog box. By responding to this message, the dialog box can set its text and background colors using the specified display device context handle. 
        /// </summary>
        CTLCOLORDLG = 0x0136,
        /// <summary>
        /// The WM_CTLCOLORSCROLLBAR message is sent to the parent window of a scroll bar control when the control is about to be drawn. By responding to this message, the parent window can use the display context handle to set the background color of the scroll bar control. 
        /// </summary>
        CTLCOLORSCROLLBAR = 0x0137,
        /// <summary>
        /// A static control, or an edit control that is read-only or disabled, sends the WM_CTLCOLORSTATIC message to its parent window when the control is about to be drawn. By responding to this message, the parent window can use the specified device context handle to set the text and background colors of the static control. 
        /// </summary>
        CTLCOLORSTATIC = 0x0138,
        /// <summary>
        /// Use WM_MOUSEFIRST to specify the first mouse message. Use the PeekMessage() Function.
        /// </summary>
        MOUSEFIRST = 0x0200,
        /// <summary>
        /// The WM_MOUSEMOVE message is posted to a window when the cursor moves. If the mouse is not captured, the message is posted to the window that contains the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        MOUSEMOVE = 0x0200,
        /// <summary>
        /// The WM_LBUTTONDOWN message is posted when the user presses the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        LBUTTONDOWN = 0x0201,
        /// <summary>
        /// The WM_LBUTTONUP message is posted when the user releases the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        LBUTTONUP = 0x0202,
        /// <summary>
        /// The WM_LBUTTONDBLCLK message is posted when the user double-clicks the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        LBUTTONDBLCLK = 0x0203,
        /// <summary>
        /// The WM_RBUTTONDOWN message is posted when the user presses the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        RBUTTONDOWN = 0x0204,
        /// <summary>
        /// The WM_RBUTTONUP message is posted when the user releases the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        RBUTTONUP = 0x0205,
        /// <summary>
        /// The WM_RBUTTONDBLCLK message is posted when the user double-clicks the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        RBUTTONDBLCLK = 0x0206,
        /// <summary>
        /// The WM_MBUTTONDOWN message is posted when the user presses the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        MBUTTONDOWN = 0x0207,
        /// <summary>
        /// The WM_MBUTTONUP message is posted when the user releases the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        MBUTTONUP = 0x0208,
        /// <summary>
        /// The WM_MBUTTONDBLCLK message is posted when the user double-clicks the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        MBUTTONDBLCLK = 0x0209,
        /// <summary>
        /// The WM_MOUSEWHEEL message is sent to the focus window when the mouse wheel is rotated. The DefWindowProc function propagates the message to the window's parent. There should be no internal forwarding of the message, since DefWindowProc propagates it up the parent chain until it finds a window that processes it.
        /// </summary>
        MOUSEWHEEL = 0x020A,
        /// <summary>
        /// The WM_XBUTTONDOWN message is posted when the user presses the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse. 
        /// </summary>
        XBUTTONDOWN = 0x020B,
        /// <summary>
        /// The WM_XBUTTONUP message is posted when the user releases the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        XBUTTONUP = 0x020C,
        /// <summary>
        /// The WM_XBUTTONDBLCLK message is posted when the user double-clicks the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        XBUTTONDBLCLK = 0x020D,
        /// <summary>
        /// The WM_MOUSEHWHEEL message is sent to the focus window when the mouse's horizontal scroll wheel is tilted or rotated. The DefWindowProc function propagates the message to the window's parent. There should be no internal forwarding of the message, since DefWindowProc propagates it up the parent chain until it finds a window that processes it.
        /// </summary>
        MOUSEHWHEEL = 0x020E,
        /// <summary>
        /// Use WM_MOUSELAST to specify the last mouse message. Used with PeekMessage() Function.
        /// </summary>
        MOUSELAST = 0x020E,
        /// <summary>
        /// The WM_PARENTNOTIFY message is sent to the parent of a child window when the child window is created or destroyed, or when the user clicks a mouse button while the cursor is over the child window. When the child window is being created, the system sends WM_PARENTNOTIFY just before the CreateWindow or CreateWindowEx function that creates the window returns. When the child window is being destroyed, the system sends the message before any processing to destroy the window takes place.
        /// </summary>
        PARENTNOTIFY = 0x0210,
        /// <summary>
        /// The WM_ENTERMENULOOP message informs an application's main window procedure that a menu modal loop has been entered. 
        /// </summary>
        ENTERMENULOOP = 0x0211,
        /// <summary>
        /// The WM_EXITMENULOOP message informs an application's main window procedure that a menu modal loop has been exited. 
        /// </summary>
        EXITMENULOOP = 0x0212,
        /// <summary>
        /// The WM_NEXTMENU message is sent to an application when the right or left arrow key is used to switch between the menu bar and the system menu. 
        /// </summary>
        NEXTMENU = 0x0213,
        /// <summary>
        /// The WM_SIZING message is sent to a window that the user is resizing. By processing this message, an application can monitor the size and position of the drag rectangle and, if needed, change its size or position. 
        /// </summary>
        SIZING = 0x0214,
        /// <summary>
        /// The WM_CAPTURECHANGED message is sent to the window that is losing the mouse capture.
        /// </summary>
        CAPTURECHANGED = 0x0215,
        /// <summary>
        /// The WM_MOVING message is sent to a window that the user is moving. By processing this message, an application can monitor the position of the drag rectangle and, if needed, change its position.
        /// </summary>
        MOVING = 0x0216,
        /// <summary>
        /// Notifies applications that a power-management event has occurred.
        /// </summary>
        POWERBROADCAST = 0x0218,
        /// <summary>
        /// Notifies an application of a change to the hardware configuration of a device or the computer.
        /// </summary>
        DEVICECHANGE = 0x0219,
        /// <summary>
        /// An application sends the WM_MDICREATE message to a multiple-document interface (MDI) client window to create an MDI child window. 
        /// </summary>
        MDICREATE = 0x0220,
        /// <summary>
        /// An application sends the WM_MDIDESTROY message to a multiple-document interface (MDI) client window to close an MDI child window. 
        /// </summary>
        MDIDESTROY = 0x0221,
        /// <summary>
        /// An application sends the WM_MDIACTIVATE message to a multiple-document interface (MDI) client window to instruct the client window to activate a different MDI child window. 
        /// </summary>
        MDIACTIVATE = 0x0222,
        /// <summary>
        /// An application sends the WM_MDIRESTORE message to a multiple-document interface (MDI) client window to restore an MDI child window from maximized or minimized size. 
        /// </summary>
        MDIRESTORE = 0x0223,
        /// <summary>
        /// An application sends the WM_MDINEXT message to a multiple-document interface (MDI) client window to activate the next or previous child window. 
        /// </summary>
        MDINEXT = 0x0224,
        /// <summary>
        /// An application sends the WM_MDIMAXIMIZE message to a multiple-document interface (MDI) client window to maximize an MDI child window. The system resizes the child window to make its client area fill the client window. The system places the child window's window menu icon in the rightmost position of the frame window's menu bar, and places the child window's restore icon in the leftmost position. The system also appends the title bar text of the child window to that of the frame window. 
        /// </summary>
        MDIMAXIMIZE = 0x0225,
        /// <summary>
        /// An application sends the WM_MDITILE message to a multiple-document interface (MDI) client window to arrange all of its MDI child windows in a tile format. 
        /// </summary>
        MDITILE = 0x0226,
        /// <summary>
        /// An application sends the WM_MDICASCADE message to a multiple-document interface (MDI) client window to arrange all its child windows in a cascade format. 
        /// </summary>
        MDICASCADE = 0x0227,
        /// <summary>
        /// An application sends the WM_MDIICONARRANGE message to a multiple-document interface (MDI) client window to arrange all minimized MDI child windows. It does not affect child windows that are not minimized. 
        /// </summary>
        MDIICONARRANGE = 0x0228,
        /// <summary>
        /// An application sends the WM_MDIGETACTIVE message to a multiple-document interface (MDI) client window to retrieve the handle to the active MDI child window. 
        /// </summary>
        MDIGETACTIVE = 0x0229,
        /// <summary>
        /// An application sends the WM_MDISETMENU message to a multiple-document interface (MDI) client window to replace the entire menu of an MDI frame window, to replace the window menu of the frame window, or both. 
        /// </summary>
        MDISETMENU = 0x0230,
        /// <summary>
        /// The WM_ENTERSIZEMOVE message is sent one time to a window after it enters the moving or sizing modal loop. The window enters the moving or sizing modal loop when the user clicks the window's title bar or sizing border, or when the window passes the WM_SYSCOMMAND message to the DefWindowProc function and the wParam parameter of the message specifies the SC_MOVE or SC_SIZE value. The operation is complete when DefWindowProc returns. 
        /// The system sends the WM_ENTERSIZEMOVE message regardless of whether the dragging of full windows is enabled.
        /// </summary>
        ENTERSIZEMOVE = 0x0231,
        /// <summary>
        /// The WM_EXITSIZEMOVE message is sent one time to a window, after it has exited the moving or sizing modal loop. The window enters the moving or sizing modal loop when the user clicks the window's title bar or sizing border, or when the window passes the WM_SYSCOMMAND message to the DefWindowProc function and the wParam parameter of the message specifies the SC_MOVE or SC_SIZE value. The operation is complete when DefWindowProc returns. 
        /// </summary>
        EXITSIZEMOVE = 0x0232,
        /// <summary>
        /// Sent when the user drops a file on the window of an application that has registered itself as a recipient of dropped files.
        /// </summary>
        DROPFILES = 0x0233,
        /// <summary>
        /// An application sends the WM_MDIREFRESHMENU message to a multiple-document interface (MDI) client window to refresh the window menu of the MDI frame window. 
        /// </summary>
        MDIREFRESHMENU = 0x0234,
        /// <summary>
        /// Notifies the window when one or more touch points, such as a finger or pen, touches a touch-sensitive digitizer surface.
        /// </summary>
        TOUCH = 0x0240,
        /// <summary>
        /// Sent to an application when a window is activated. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_SETCONTEXT = 0x0281,
        /// <summary>
        /// Sent to an application to notify it of changes to the IME window. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_NOTIFY = 0x0282,
        /// <summary>
        /// Sent by an application to direct the IME window to carry out the requested command. The application uses this message to control the IME window that it has created. To send this message, the application calls the SendMessage function with the following parameters.
        /// </summary>
        IME_CONTROL = 0x0283,
        /// <summary>
        /// Sent to an application when the IME window finds no space to extend the area for the composition window. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_COMPOSITIONFULL = 0x0284,
        /// <summary>
        /// Sent to an application when the operating system is about to change the current IME. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_SELECT = 0x0285,
        /// <summary>
        /// Sent to an application when the IME gets a character of the conversion result. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_CHAR = 0x0286,
        /// <summary>
        /// Sent to an application to provide commands and request information. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_REQUEST = 0x0288,
        /// <summary>
        /// Sent to an application by the IME to notify the application of a key press and to keep message order. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_KEYDOWN = 0x0290,
        /// <summary>
        /// Sent to an application by the IME to notify the application of a key release and to keep message order. A window receives this message through its WindowProc function. 
        /// </summary>
        IME_KEYUP = 0x0291,
        /// <summary>
        /// The WM_MOUSEHOVER message is posted to a window when the cursor hovers over the client area of the window for the period of time specified in a prior call to TrackMouseEvent.
        /// </summary>
        MOUSEHOVER = 0x02A1,
        /// <summary>
        /// The WM_MOUSELEAVE message is posted to a window when the cursor leaves the client area of the window specified in a prior call to TrackMouseEvent.
        /// </summary>
        MOUSELEAVE = 0x02A3,
        /// <summary>
        /// The WM_NCMOUSEHOVER message is posted to a window when the cursor hovers over the nonclient area of the window for the period of time specified in a prior call to TrackMouseEvent.
        /// </summary>
        NCMOUSEHOVER = 0x02A0,
        /// <summary>
        /// The WM_NCMOUSELEAVE message is posted to a window when the cursor leaves the nonclient area of the window specified in a prior call to TrackMouseEvent.
        /// </summary>
        NCMOUSELEAVE = 0x02A2,
        /// <summary>
        /// The WM_WTSSESSION_CHANGE message notifies applications of changes in session state.
        /// </summary>
        WTSSESSION_CHANGE = 0x02B1,
        TABLET_FIRST = 0x02c0,
        TABLET_LAST = 0x02df,
        /// <summary>
        /// An application sends a WM_CUT message to an edit control or combo box to delete (cut) the current selection, if any, in the edit control and copy the deleted text to the clipboard in CF_TEXT format. 
        /// </summary>
        CUT = 0x0300,
        /// <summary>
        /// An application sends the WM_COPY message to an edit control or combo box to copy the current selection to the clipboard in CF_TEXT format. 
        /// </summary>
        COPY = 0x0301,
        /// <summary>
        /// An application sends a WM_PASTE message to an edit control or combo box to copy the current content of the clipboard to the edit control at the current caret position. Data is inserted only if the clipboard contains data in CF_TEXT format. 
        /// </summary>
        PASTE = 0x0302,
        /// <summary>
        /// An application sends a WM_CLEAR message to an edit control or combo box to delete (clear) the current selection, if any, from the edit control. 
        /// </summary>
        CLEAR = 0x0303,
        /// <summary>
        /// An application sends a WM_UNDO message to an edit control to undo the last operation. When this message is sent to an edit control, the previously deleted text is restored or the previously added text is deleted.
        /// </summary>
        UNDO = 0x0304,
        /// <summary>
        /// The WM_RENDERFORMAT message is sent to the clipboard owner if it has delayed rendering a specific clipboard format and if an application has requested data in that format. The clipboard owner must render data in the specified format and place it on the clipboard by calling the SetClipboardData function. 
        /// </summary>
        RENDERFORMAT = 0x0305,
        /// <summary>
        /// The WM_RENDERALLFORMATS message is sent to the clipboard owner before it is destroyed, if the clipboard owner has delayed rendering one or more clipboard formats. For the content of the clipboard to remain available to other applications, the clipboard owner must render data in all the formats it is capable of generating, and place the data on the clipboard by calling the SetClipboardData function. 
        /// </summary>
        RENDERALLFORMATS = 0x0306,
        /// <summary>
        /// The WM_DESTROYCLIPBOARD message is sent to the clipboard owner when a call to the EmptyClipboard function empties the clipboard. 
        /// </summary>
        DESTROYCLIPBOARD = 0x0307,
        /// <summary>
        /// The WM_DRAWCLIPBOARD message is sent to the first window in the clipboard viewer chain when the content of the clipboard changes. This enables a clipboard viewer window to display the new content of the clipboard. 
        /// </summary>
        DRAWCLIPBOARD = 0x0308,
        /// <summary>
        /// The WM_PAINTCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and the clipboard viewer's client area needs repainting. 
        /// </summary>
        PAINTCLIPBOARD = 0x0309,
        /// <summary>
        /// The WM_VSCROLLCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and an event occurs in the clipboard viewer's vertical scroll bar. The owner should scroll the clipboard image and update the scroll bar values. 
        /// </summary>
        VSCROLLCLIPBOARD = 0x030A,
        /// <summary>
        /// The WM_SIZECLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and the clipboard viewer's client area has changed size. 
        /// </summary>
        SIZECLIPBOARD = 0x030B,
        /// <summary>
        /// The WM_ASKCBFORMATNAME message is sent to the clipboard owner by a clipboard viewer window to request the name of a CF_OWNERDISPLAY clipboard format.
        /// </summary>
        ASKCBFORMATNAME = 0x030C,
        /// <summary>
        /// The WM_CHANGECBCHAIN message is sent to the first window in the clipboard viewer chain when a window is being removed from the chain. 
        /// </summary>
        CHANGECBCHAIN = 0x030D,
        /// <summary>
        /// The WM_HSCROLLCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window. This occurs when the clipboard contains data in the CF_OWNERDISPLAY format and an event occurs in the clipboard viewer's horizontal scroll bar. The owner should scroll the clipboard image and update the scroll bar values. 
        /// </summary>
        HSCROLLCLIPBOARD = 0x030E,
        /// <summary>
        /// This message informs a window that it is about to receive the keyboard focus, giving the window the opportunity to realize its logical palette when it receives the focus. 
        /// </summary>
        QUERYNEWPALETTE = 0x030F,
        /// <summary>
        /// The WM_PALETTEISCHANGING message informs applications that an application is going to realize its logical palette. 
        /// </summary>
        PALETTEISCHANGING = 0x0310,
        /// <summary>
        /// This message is sent by the OS to all top-level and overlapped windows after the window with the keyboard focus realizes its logical palette. 
        /// This message enables windows that do not have the keyboard focus to realize their logical palettes and update their client areas.
        /// </summary>
        PALETTECHANGED = 0x0311,
        /// <summary>
        /// The WM_HOTKEY message is posted when the user presses a hot key registered by the RegisterHotKey function. The message is placed at the top of the message queue associated with the thread that registered the hot key. 
        /// </summary>
        HOTKEY = 0x0312,
        /// <summary>
        /// The WM_PRINT message is sent to a window to request that it draw itself in the specified device context, most commonly in a printer device context.
        /// </summary>
        PRINT = 0x0317,
        /// <summary>
        /// The WM_PRINTCLIENT message is sent to a window to request that it draw its client area in the specified device context, most commonly in a printer device context.
        /// </summary>
        PRINTCLIENT = 0x0318,
        /// <summary>
        /// The WM_APPCOMMAND message notifies a window that the user generated an application command event, for example, by clicking an application command button using the mouse or typing an application command key on the keyboard.
        /// </summary>
        APPCOMMAND = 0x0319,
        /// <summary>
        /// The WM_THEMECHANGED message is broadcast to every window following a theme change event. Examples of theme change events are the activation of a theme, the deactivation of a theme, or a transition from one theme to another.
        /// </summary>
        THEMECHANGED = 0x031A,
        /// <summary>
        /// Sent when the contents of the clipboard have changed.
        /// </summary>
        CLIPBOARDUPDATE = 0x031D,
        /// <summary>
        /// The system will send a window the WM_DWMCOMPOSITIONCHANGED message to indicate that the availability of desktop composition has changed.
        /// </summary>
        DWMCOMPOSITIONCHANGED = 0x031E,
        /// <summary>
        /// WM_DWMNCRENDERINGCHANGED is called when the non-client area rendering status of a window has changed. Only windows that have set the flag DWM_BLURBEHIND.fTransitionOnMaximized to true will get this message. 
        /// </summary>
        DWMNCRENDERINGCHANGED = 0x031F,
        /// <summary>
        /// Sent to all top-level windows when the colorization color has changed. 
        /// </summary>
        DWMCOLORIZATIONCOLORCHANGED = 0x0320,
        /// <summary>
        /// WM_DWMWINDOWMAXIMIZEDCHANGE will let you know when a DWM composed window is maximized. You also have to register for this message as well. You'd have other windowd go opaque when this message is sent.
        /// </summary>
        DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
        /// <summary>
        /// Sent to request extended title bar information. A window receives this message through its WindowProc function.
        /// </summary>
        GETTITLEBARINFOEX = 0x033F,
        HANDHELDFIRST = 0x0358,
        HANDHELDLAST = 0x035F,
        AFXFIRST = 0x0360,
        AFXLAST = 0x037F,
        PENWINFIRST = 0x0380,
        PENWINLAST = 0x038F,
        /// <summary>
        /// The WM_APP constant is used by applications to help define private messages, usually of the form WM_APP+X, where X is an integer value. 
        /// </summary>
        APP = 0x8000,
        /// <summary>
        /// The WM_USER constant is used by applications to help define private messages for use by private window classes, usually of the form WM_USER+X, where X is an integer value. 
        /// </summary>
        USER = 0x0400,

        /// <summary>
        /// An application sends the WM_CPL_LAUNCH message to Windows Control Panel to request that a Control Panel application be started. 
        /// </summary>
        CPL_LAUNCH = USER + 0x1000,
        /// <summary>
        /// The WM_CPL_LAUNCHED message is sent when a Control Panel application, started by the WM_CPL_LAUNCH message, has closed. The WM_CPL_LAUNCHED message is sent to the window identified by the wParam parameter of the WM_CPL_LAUNCH message that started the application. 
        /// </summary>
        CPL_LAUNCHED = USER + 0x1001,
        /// <summary>
        /// WM_SYSTIMER is a well-known yet still undocumented message. Windows uses WM_SYSTIMER for internal actions like scrolling.
        /// </summary>
        SYSTIMER = 0x118,

        /// <summary>
        /// The accessibility state has changed.
        /// </summary>
        HSHELL_ACCESSIBILITYSTATE = 11,
        /// <summary>
        /// The shell should activate its main window.
        /// </summary>
        HSHELL_ACTIVATESHELLWINDOW = 3,
        /// <summary>
        /// The user completed an input event (for example, pressed an application command button on the mouse or an application command key on the keyboard), and the application did not handle the WM_APPCOMMAND message generated by that input.
        /// If the Shell procedure handles the WM_COMMAND message, it should not call CallNextHookEx. See the Return Value section for more information.
        /// </summary>
        HSHELL_APPCOMMAND = 12,
        /// <summary>
        /// A window is being minimized or maximized. The system needs the coordinates of the minimized rectangle for the window.
        /// </summary>
        HSHELL_GETMINRECT = 5,
        /// <summary>
        /// Keyboard language was changed or a new keyboard layout was loaded.
        /// </summary>
        HSHELL_LANGUAGE = 8,
        /// <summary>
        /// The title of a window in the task bar has been redrawn.
        /// </summary>
        HSHELL_REDRAW = 6,
        /// <summary>
        /// The user has selected the task list. A shell application that provides a task list should return TRUE to prevent Windows from starting its task list.
        /// </summary>
        HSHELL_TASKMAN = 7,
        /// <summary>
        /// A top-level, unowned window has been created. The window exists when the system calls this hook.
        /// </summary>
        HSHELL_WINDOWCREATED = 1,
        /// <summary>
        /// A top-level, unowned window is about to be destroyed. The window still exists when the system calls this hook.
        /// </summary>
        HSHELL_WINDOWDESTROYED = 2,
        /// <summary>
        /// The activation has changed to a different top-level, unowned window.
        /// </summary>
        HSHELL_WINDOWACTIVATED = 4,
        /// <summary>
        /// A top-level window is being replaced. The window exists when the system calls this hook.
        /// </summary>
        HSHELL_WINDOWREPLACED = 13
    }

    /// <summary>
    /// Events that are generated by the operating system and by server applications
    /// see: https://msdn.microsoft.com/en-us/library/windows/desktop/dd318066(v=vs.85).aspx
    /// </summary>
    public enum WinEvent : uint
    {
        EVENT_SYSTEM_FOREGROUND = 3
    }

    /// <summary>
    /// Flag values that specify the location of the hook function and of the events to be skipped. The following flags are valid
    /// See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd373640(v=vs.85).aspx
    /// </summary>
    [Flags]
    public enum WinEventFlags : uint
    {
        WINEVENT_OUTOFCONTEXT = 0,
        WINEVENT_SKIPOWNTHREAD = 1,
        WINEVENT_SKIPOWNPROCESS = 2,
        WINEVENT_INCONTEXT = 4
    }


    /// <summary>
    /// Win32 Window Styles.
    /// </summary>
    [Flags]
    public enum WindowStyles : uint
    {
        /// <summary>
        /// Overlapped.
        /// </summary>
        WS_OVERLAPPED = 0x00000000,
        /// <summary>
        /// Popup.
        /// </summary>
        WS_POPUP = 0x80000000,
        /// <summary>
        /// Child.
        /// </summary>
        WS_CHILD = 0x40000000,
        /// <summary>
        /// Minimize.
        /// </summary>
        WS_MINIMIZE = 0x20000000,
        /// <summary>
        /// Visible.
        /// </summary>
        WS_VISIBLE = 0x10000000,
        /// <summary>
        /// Disabled.
        /// </summary>
        WS_DISABLED = 0x08000000,
        /// <summary>
        /// Clip Siblings.
        /// </summary>
        WS_CLIPSIBLINGS = 0x04000000,
        /// <summary>
        /// Clip Children.
        /// </summary>
        WS_CLIPCHILDREN = 0x02000000,
        /// <summary>
        /// Maximize.
        /// </summary>
        WS_MAXIMIZE = 0x01000000,
        /// <summary>
        /// Border.
        /// </summary>
        WS_BORDER = 0x00800000,
        /// <summary>
        /// Dialog Frame.
        /// </summary>
        WS_DLGFRAME = 0x00400000,
        /// <summary>
        /// Vertical Scroll.
        /// </summary>
        WS_VSCROLL = 0x00200000,
        /// <summary>
        /// Horizontal Scroll.
        /// </summary>
        WS_HSCROLL = 0x00100000,
        /// <summary>
        /// System Menu.
        /// </summary>
        WS_SYSMENU = 0x00080000,
        /// <summary>
        /// Thick Frame.
        /// </summary>
        WS_THICKFRAME = 0x00040000,
        /// <summary>
        /// Group.
        /// </summary>
        WS_GROUP = 0x00020000,
        /// <summary>
        /// Tab Stop.
        /// </summary>
        WS_TABSTOP = 0x00010000,

        /// <summary>
        /// Minimize Box.
        /// </summary>
        WS_MINIMIZEBOX = 0x00020000,
        /// <summary>
        /// Maximize Box.
        /// </summary>
        WS_MAXIMIZEBOX = 0x00010000,

        /// <summary>
        /// Caption (WS_BORDER | WS_DLGFRAME).
        /// </summary>
        WS_CAPTION = WS_BORDER | WS_DLGFRAME,
        /// <summary>
        /// Tiled (WS_OVERLAPPED).
        /// </summary>
        WS_TILED = WS_OVERLAPPED,
        /// <summary>
        /// Iconic (WS_MINIMIZE).
        /// </summary>
        WS_ICONIC = WS_MINIMIZE,
        /// <summary>
        /// Size Box (WS_THICKFRAME).
        /// </summary>
        WS_SIZEBOX = WS_THICKFRAME,
        /// <summary>
        /// Tiled Window (WS_OVERLAPPEDWINDOW).
        /// </summary>
        WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW,

        /// <summary>
        /// Overlapped Window (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX).
        /// </summary>
        WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        /// <summary>
        /// Popup Window (WS_POPUP | WS_BORDER | WS_SYSMENU).
        /// </summary>
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
        /// <summary>
        /// Child Window (WS_CHILD).
        /// </summary>
        WS_CHILDWINDOW = WS_CHILD,
    }

    [Flags]
    private enum SHGFI
    {
        /// <summary>get icon</summary>
        Icon = 0x000000100,
        /// <summary>get display name</summary>
        DisplayName = 0x000000200,
        /// <summary>get type name</summary>
        TypeName = 0x000000400,
        /// <summary>get attributes</summary>
        Attributes = 0x000000800,
        /// <summary>get icon location</summary>
        IconLocation = 0x000001000,
        /// <summary>return exe type</summary>
        ExeType = 0x000002000,
        /// <summary>get system icon index</summary>
        SysIconIndex = 0x000004000,
        /// <summary>put a link overlay on icon</summary>
        LinkOverlay = 0x000008000,
        /// <summary>show icon in selected state</summary>
        Selected = 0x000010000,
        /// <summary>get only specified attributes</summary>
        Attr_Specified = 0x000020000,
        /// <summary>get large icon</summary>
        LargeIcon = 0x000000000,
        /// <summary>get small icon</summary>
        SmallIcon = 0x000000001,
        /// <summary>get open icon</summary>
        OpenIcon = 0x000000002,
        /// <summary>get shell size icon</summary>
        ShellIconSize = 0x000000004,
        /// <summary>pszPath is a pidl</summary>
        PIDL = 0x000000008,
        /// <summary>use passed dwFileAttribute</summary>
        UseFileAttributes = 0x000000010,
        /// <summary>apply the appropriate overlays</summary>
        AddOverlays = 0x000000020,
        /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
        OverlayIndex = 0x000000040,
    }

    [Flags]
    public enum KeyModifier : int
    {
        // modifiers
        NONE = 0x0000,
        ALT = 0x0001,
        CTRL = 0x0002,
        SHIFT = 0x0004,
        WIN = 0x0008,

        // windows message id for hotkey
        WM_HOTKEY_MSG_ID = 0x0312
    }

    /// <summary>
    /// Enumeration of the different ways of showing a window using ShowWindow.
    /// </summary>
    private enum WindowShowStyle
    {
        /// <summary>Hides the window and activates another window.</summary>
        /// <remarks>See SW_HIDE</remarks>
        Hide = 0,
        /// <summary>Activates and displays a window. If the window is minimized
        /// or maximized, the system restores it to its original size and
        /// position. An application should specify this flag when displaying
        /// the window for the first time.</summary>
        /// <remarks>See SW_SHOWNORMAL</remarks>
        ShowNormal = 1,
        /// <summary>Activates the window and displays it as a minimized window.</summary>
        /// <remarks>See SW_SHOWMINIMIZED</remarks>
        ShowMinimized = 2,
        /// <summary>Activates the window and displays it as a maximized window.</summary>
        /// <remarks>See SW_SHOWMAXIMIZED</remarks>
        ShowMaximized = 3,
        /// <summary>Maximizes the specified window.</summary>
        /// <remarks>See SW_MAXIMIZE</remarks>
        Maximize = 3,
        /// <summary>Displays a window in its most recent size and position.
        /// This value is similar to "ShowNormal", except the window is not
        /// actived.</summary>
        /// <remarks>See SW_SHOWNOACTIVATE</remarks>
        ShowNormalNoActivate = 4,
        /// <summary>Activates the window and displays it in its current size
        /// and position.</summary>
        /// <remarks>See SW_SHOW</remarks>
        Show = 5,
        /// <summary>Minimizes the specified window and activates the next
        /// top-level window in the Z order.</summary>
        /// <remarks>See SW_MINIMIZE</remarks>
        Minimize = 6,
        /// <summary>Displays the window as a minimized window. This value is
        /// similar to "ShowMinimized", except the window is not activated.</summary>
        /// <remarks>See SW_SHOWMINNOACTIVE</remarks>
        ShowMinNoActivate = 7,
        /// <summary>Displays the window in its current size and position. This
        /// value is similar to "Show", except the window is not activated.</summary>
        /// <remarks>See SW_SHOWNA</remarks>
        ShowNoActivate = 8,
        /// <summary>Activates and displays the window. If the window is
        /// minimized or maximized, the system restores it to its original size
        /// and position. An application should specify this flag when restoring
        /// a minimized window.</summary>
        /// <remarks>See SW_RESTORE</remarks>
        Restore = 9,
        /// <summary>Sets the show state based on the SW_ value specified in the
        /// STARTUPINFO structure passed to the CreateProcess function by the
        /// program that started the application.</summary>
        /// <remarks>See SW_SHOWDEFAULT</remarks>
        ShowDefault = 10,
        /// <summary>Windows 2000/XP: Minimizes a window, even if the thread
        /// that owns the window is hung. This flag should only be used when
        /// minimizing windows from a different thread.</summary>
        /// <remarks>See SW_FORCEMINIMIZE</remarks>
        ForceMinimized = 11
    }

    [Flags]
    public enum EFileAttributes : uint
    {
        Readonly = 0x00000001,
        Hidden = 0x00000002,
        System = 0x00000004,
        Directory = 0x00000010,
        Archive = 0x00000020,
        Device = 0x00000040,
        Normal = 0x00000080,
        Temporary = 0x00000100,
        SparseFile = 0x00000200,
        ReparsePoint = 0x00000400,
        Compressed = 0x00000800,
        Offline = 0x00001000,
        NotContentIndexed = 0x00002000,
        Encrypted = 0x00004000,
        Write_Through = 0x80000000,
        Overlapped = 0x40000000,
        NoBuffering = 0x20000000,
        RandomAccess = 0x10000000,
        SequentialScan = 0x08000000,
        DeleteOnClose = 0x04000000,
        BackupSemantics = 0x02000000,
        PosixSemantics = 0x01000000,
        OpenReparsePoint = 0x00200000,
        OpenNoRecall = 0x00100000,
        FirstPipeInstance = 0x00080000,
    }

    [Flags]
    public enum FuFlags : uint
    {
        TPM_LEFTBUTTON = 0x0000,
        TPM_RIGHTBUTTON = 0x0002,
        TPM_NONOTIFY = 0x0080,
        TPM_RETURNCMD = 0x0100
    }

    [Flags]
    public enum SysDefMsg : uint
    {
        WM_SYSCOMMAND = 0x0112
    }

    [Flags]
    public enum WinPosFlags : uint
    {
        /// <summary>
        ///  If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request. 
        /// </summary>
        ASYNCWINDOWPOS = 0x4000,

        /// <summary>
        ///  Prevents generation of the WM_SYNCPAINT message. 
        DEFERERASE = 0x2000,

        /// <summary>
        ///  Draws a frame (defined in the window's class description) around the window.
        /// </summary>
        DRAWFRAME = 0x0020,

        /// <summary>
        ///  Applies new frame styles set using the SetWindowLong function.Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
        /// </summary>
        FRAMECHANGED = 0x0020,

        /// <summary>
        ///  Hides the window.
        /// </summary>
        HIDEWINDOW = 0x0080,

        /// <summary>
        ///  Does not activate the window.If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
        /// </summary>
        NOACTIVATE = 0x0010,

        /// <summary>
        ///  Discards the entire contents of the client area.If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
        /// </summary>
        NOCOPYBITS = 0x0100,

        /// <summary>
        ///  Retains the current position (ignores X and Y parameters).
        /// </summary>
        NOMOVE = 0x0002,

        /// <summary>
        ///  Does not change the owner window's position in the Z order.
        /// </summary>
        NOOWNERZORDER = 0x0200,

        /// <summary>
        ///  Does not redraw changes.If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved.When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
        /// </summary>
        NOREDRAW = 0x0008,

        /// <summary>
        ///  Same as the NOOWNERZORDER flag.
        /// </summary>
        NOREPOSITION = 0x0200,

        /// <summary>
        ///  Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
        /// </summary>
        NOSENDCHANGING = 0x0400,

        /// <summary>
        ///  Retains the current size (ignores the cx and cy parameters).
        /// </summary>
        NOSIZE = 0x0001,

        /// <summary>
        ///  Retains the current Z order (ignores the hWndInsertAfter parameter).
        /// </summary>
        NOZORDER = 0x0004,

        /// <summary>
        ///  Displays the window.
        /// </summary>
        SHOWWINDOW = 0x0040

    }


#endregion Enumerations

    // --------------------------------------------------------------------------------------------------
    #region Structures

    #region Nested type: COPYDATASTRUCT

    /// <summary>
    /// Data structure for sending data over a windows message.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        /// <summary>
        /// Data ID.
        /// </summary>
        public int dwData;

        /// <summary>
        /// Data size.
        /// </summary>
        public int cbData;

        /// <summary>
        /// Data.
        /// </summary>
        public IntPtr lpData;
    }

    #endregion

    #region Nested type: POINTAPI

    [StructLayout(LayoutKind.Sequential)]
    private struct POINTAPI
    {
        public int x;
        public int y;
    }

    #endregion

    #region Nested type: RECT

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    #endregion

    #region Nested type: SHFILEINFO

    /// <summary>
    /// Data structure for retreiving information on files from the shell.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        /// <summary>
        /// hIcon;
        /// </summary>
        public IntPtr hIcon;

        /// <summary>
        /// iIcon.
        /// </summary>
        public IntPtr iIcon;

        /// <summary>
        /// Attributes.
        /// </summary>
        public int dwAttributes;

        /// <summary>
        /// Display Name.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        /// <summary>
        /// Type Name.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    } ;

    #endregion

    #region Nested type: WINDOWPLACEMENT

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public WindowShowStyle showCmd;
        public POINTAPI ptMinPosition;
        public POINTAPI ptMaxPosition;
        public RECT rcNormalPosition;
    }

    #endregion

    #region Nested type: TOKEN_PRIVILEGES
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TOKEN_PRIVILEGES
    {
        /// <summary>
        /// Specifies the number of entries in the Privileges array.
        /// </summary>
        public int PrivilegeCount;
        /// <summary>
        /// Specifies an array of LUID_AND_ATTRIBUTES structures. Each structure contains the LUID and attributes of a privilege.
        /// </summary>
        public LUID_AND_ATTRIBUTES Privileges;
    }
    #endregion

    #region Nested type: LUID
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LUID
    {
        /// <summary>
        /// The low order part of the 64 bit value.
        /// </summary>
        public int LowPart;
        /// <summary>
        /// The high order part of the 64 bit value.
        /// </summary>
        public int HighPart;
    }
    #endregion

    #region Nested type: LUID_AND_ATTRIBUTES
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LUID_AND_ATTRIBUTES
    {
        /// <summary>
        /// Specifies an LUID value.
        /// </summary>
        public LUID pLuid;
        /// <summary>
        /// Specifies attributes of the LUID. This value contains up to 32 one-bit flags. Its meaning is dependent on the definition and use of the LUID.
        /// </summary>
        public int Attributes;
    }
    #endregion

    #region Nested type: DeviceInfoData

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DeviceInfoData
    {
        public int Size;
        public Guid Class;
        public int DevInst;
        public IntPtr Reserved;
    }

    #endregion

    #region Nested type: DeviceInterfaceData

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DeviceInterfaceData
    {
        public int Size;
        public Guid Class;
        public int Flags;
        public IntPtr Reserved;
    }

    #endregion

    #region Nested type: DeviceInterfaceDetailData

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct DeviceInterfaceDetailData
    {
        public int Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DevicePath;
    }

    #endregion

    #endregion Structures

    // --------------------------------------------------------------------------------------------------
    #region Delegates

    /// <summary>
    /// Delegate for enumerating open Windows with EnumWindows method.
    /// </summary>
    /// <param name="hWnd">Window Handle.</param>
    /// <param name="lParam">lParam.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    #endregion

    // --------------------------------------------------------------------------------------------------
    #region Interop

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(
      IntPtr hWnd,
      ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(
      IntPtr hWnd,
      WindowShowStyle style);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("shell32.dll")]
    private static extern IntPtr SHGetFileInfo(
      string pszPath,
      uint dwFileAttributes,
      ref SHFILEINFO psfi,
      uint cbSizeFileInfo,
      SHGFI uFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int ExtractIconEx(
      string lpszFile,
      int nIconIndex,
      IntPtr[] phIconLarge,
      IntPtr[] phIconSmall,
      int nIcons);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    public static extern int DestroyIcon(
      IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern int EnumWindows(
      EnumWindowsProc ewp,
      IntPtr lParam);

    [DllImport("user32.Dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(
        IntPtr parentHandle, 
        EnumWindowsProc callback, 
        IntPtr lParam);


    /// <summary>
    /// 
    /// See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd373640(v=vs.85).aspx
    /// </summary>
    /// <param name="eventMin"></param>
    /// <param name="eventMax"></param>
    /// <param name="hmodWinEventProc"></param>
    /// <param name="lpfnWinEventProc"></param>
    /// <param name="idProcess"></param>
    /// <param name="idThread"></param>
    /// <param name="dwFlags"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(WinEvent eventMin, WinEvent eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, WinEventFlags dwFlags);
    public delegate void WinEventDelegate(IntPtr hWinEventHook, WinEvent eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);


    /// <summary>
    /// Get a handle to the current foreground window.
    /// </summary>
    /// <returns>Handle to foreground window.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "ExitWindowsEx", CharSet = CharSet.Ansi)]
    private static extern int ExitWindowsEx(
      int uFlags,
      int dwReserved);

    [DllImport("advapi32.dll", EntryPoint = "OpenProcessToken", CharSet = CharSet.Ansi)]
    private static extern int OpenProcessToken(
      IntPtr ProcessHandle,
      int DesiredAccess,
      ref IntPtr TokenHandle);

    [DllImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueA", CharSet = CharSet.Ansi)]
    private static extern int LookupPrivilegeValue(
      string lpSystemName,
      string lpName,
      ref LUID lpLuid);

    [DllImport("advapi32.dll", EntryPoint = "AdjustTokenPrivileges", CharSet = CharSet.Ansi)]
    private static extern int AdjustTokenPrivileges(
      IntPtr TokenHandle,
      int DisableAllPrivileges,
      ref TOKEN_PRIVILEGES NewState,
      int BufferLength,
      ref TOKEN_PRIVILEGES PreviousState,
      ref int ReturnLength);

    [DllImport("kernel32.dll", EntryPoint = "LoadLibraryA", CharSet = CharSet.Ansi)]
    private static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", CharSet = CharSet.Ansi)]
    private static extern int FreeLibrary(IntPtr hLibModule);

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(
      IntPtr hModule,
      string lpProcName);

    [DllImport("user32.dll", EntryPoint = "FormatMessageA", CharSet = CharSet.Ansi)]
    private static extern int FormatMessage(
      int dwFlags,
      IntPtr lpSource,
      int dwMessageId,
      int dwLanguageId,
      StringBuilder lpBuffer,
      int nSize,
      int Arguments);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(
      string className,
      string windowName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
      IntPtr hWnd,
      int msg,
      IntPtr wParam,
      IntPtr lParam,
      SendMessageTimeoutFlags flags,
      int timeout,
      out IntPtr result);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    //[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    //static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    //[DllImport("user32.dll")]
    //[return: MarshalAs(UnmanagedType.Bool)]
    //static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(
      int threadId,
      int threadIdTo,
      [MarshalAs(UnmanagedType.Bool)] bool attach);

    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);
    

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(
      IntPtr hWnd,
      StringBuilder lpString,
      int maxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(
      IntPtr hWnd,
      out int processId);

    [DllImport("kernel32.dll")]
    private static extern int GetCurrentThreadId();


    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr hWnd, GW uCmd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern uint GetWindowLongPtr32(
      IntPtr hWnd,
      GWL nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern uint GetWindowLongPtr64(
      IntPtr hWnd,
      GWL nIndex);



	/// <summary>
	/// Changes an attribute of the specified window. The function also sets the 32-bit (long) value at the specified offset into the extra window memory.
	/// </summary>
	/// <param name="hwnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
	/// <param name="index">The zero-based offset to the value to be set. Valid values are in the range zero through the number of bytes of extra window memory, minus the size of an integer.</param>
	/// <param name="newStyle">The replacement value.</param>
	/// <returns></returns>
	[DllImport("user32.dll")]
	static extern int SetWindowLong(IntPtr hwnd, GWL index, WindowExStyles newStyle);


	[DllImport("user32.dll", EntryPoint = "GetClassLong")]
    private static extern uint GetClassLongPtr32(
      IntPtr hWnd,
      int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
    private static extern IntPtr GetClassLongPtr64(
      IntPtr hWnd,
      int nIndex);

    [DllImport("user32.dll")]
    private static extern int GetClassName(
      IntPtr hWnd,
      StringBuilder lpClassName,
      int nMaxCount);

    /// <summary>
    /// Changes the size, position, and Z order of a child, pop-up, or top-level window. These windows are ordered according to their appearance on the screen. The topmost window receives the highest rank and is the first window in the Z order.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="hWndInsertAfter">A handle to the window to precede the positioned window in the Z order. This parameter must be a window handle or one of the following values.</param>
    /// <param name="x">The new position of the left side of the window, in client coordinates. </param>
    /// <param name="Y">The new position of the top of the window, in client coordinates. </param>
    /// <param name="cx">The new width of the window, in pixels. </param>
    /// <param name="cy">The new height of the window, in pixels.</param>
    /// <param name="wFlags">The window sizing and positioning flags. This parameter can be a combination of the following values. </param>
    /// <returns>If the function succeeds, the return value is nonzero.</returns>
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int Y, int cx, int cy, WinPosFlags wFlags);


    /// <summary>
    /// Determines whether one window is a child of another.
    /// </summary>
    /// <param name="hWndParent">The parent.</param>
    /// <param name="hWndChild">The child.</param>
    /// <returns><c>true</c> if the window is a child of the parent; otherwise, <c>false</c>.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsChild(
      IntPtr hWndParent,
      IntPtr hWndChild);

    /// <summary>
    /// Gets the parent window.
    /// </summary>
    /// <param name="hWnd">The child.</param>
    /// <returns>Handle to parent window.</returns>

    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(
      IntPtr hWnd);

    /// <summary>
    /// Converts an item identifier list to a file system path. 
    /// </summary>
    /// <param name="pidl"></param>
    /// <param name="pszPath"></param>
    /// <returns></returns>
    [DllImport("shell32.dll")]
    public static extern Int32 SHGetPathFromIDList(
        IntPtr pidl, 
        StringBuilder pszPath);

    /// <summary>
    /// Combines two ITEMIDLIST structures (like PIDLs).
    /// </summary>
    /// <param name="pidl1"></param>
    /// <param name="pidl2"></param>
    /// <returns></returns>
    [DllImport("shell32.dll")]
    public static extern IntPtr ILCombine(IntPtr pidl1, IntPtr pidl2);

    /// <summary>
    /// Free a ITEMIDLIST structure (like PIDL).
    /// </summary>
    /// <param name="pidl"></param>
    [DllImport("shell32.dll")]
    public static extern void ILFree(IntPtr pidl);

    /*
    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    static extern bool PeekMessage(
      out System.Windows.Forms.Message msg,
      IntPtr hWnd,
      uint messageFilterMin,
      uint messageFilterMax,
      uint flags);
    */

    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWow64Process(
         [In] IntPtr hProcess,
         [Out] out bool lpSystemInfo);


    #region  Global Hot Key

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    #endregion

    /// <summary>
    /// Set the Mouse pointer (cursor) position relative to the screen.
    /// </summary>
    /// <param name="X">X position</param>
    /// <param name="Y">Y position</param>
    /// <returns></returns>
    [DllImport("User32.dll")]
    public static extern bool SetCursorPos(int X, int Y);


    #region System menu

    /// <summary>
    /// Enables the application to access the window menu (also known as the system menu or the control menu) for copying and modifying.
    /// </summary>
    /// <param name="hWnd">A handle to the window that will own a copy of the window menu. </param>
    /// <param name="bRevert">The action to be taken. If this parameter is FALSE, GetSystemMenu returns a handle to the copy of the window menu currently in use. The copy is initially identical to the window menu, but it can be modified. If this parameter is TRUE, GetSystemMenu resets the window menu back to the default state. The previous window menu, if any, is destroyed. </param>
    /// <returns>If the bRevert parameter is FALSE, the return value is a handle to a copy of the window menu. If the bRevert parameter is TRUE, the return value is NULL. </returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    /// <summary>
    /// Displays a shortcut menu at the specified location and tracks the selection of items on the shortcut menu. The shortcut menu can appear anywhere on the screen.
    /// </summary>
    /// <param name="hmenu">A handle to the shortcut menu to be displayed. This handle can be obtained by calling the CreatePopupMenu function to create a new shortcut menu or by calling the GetSubMenu function to retrieve a handle to a submenu associated with an existing menu item. </param>
    /// <param name="fuFlags">Specifies function options. </param>
    /// <param name="x">The horizontal location of the shortcut menu, in screen coordinates. </param>
    /// <param name="y">The vertical location of the shortcut menu, in screen coordinates. </param>
    /// <param name="hwnd">A handle to the window that owns the shortcut menu. This window receives all messages from the menu. The window does not receive a WM_COMMAND message from the menu until the function returns. If you specify TPM_NONOTIFY in the fuFlags parameter, the function does not send messages to the window identified by hwnd. However, you must still pass a window handle in hwnd. It can be any window handle from your application. </param>
    /// <param name="lptpm">A pointer to a TPMPARAMS structure that specifies an area of the screen the menu should not overlap. This parameter can be NULL. </param>
    /// <returns>If you specify TPM_RETURNCMD in the fuFlags parameter, the return value is the menu-item identifier of the item that the user selected. If the user cancels the menu without making a selection, or if an error occurs, the return value is zero.
    /// <br>If you do not specify TPM_RETURNCMD in the fuFlags parameter, the return value is nonzero if the function succeeds and zero if it fails. To get extended error information, call GetLastError.</br>
    /// </returns>
    [DllImport("user32.dll")]
    static extern uint TrackPopupMenuEx(IntPtr hmenu, FuFlags fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    /// <summary>
    /// Places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message.
    /// </summary>
    /// <param name="hWnd">A handle to the window whose window procedure is to receive the message.</param>
    /// <param name="Msg">The message to be posted.</param>
    /// <param name="wParam">Additional message-specific information.</param>
    /// <param name="lParam">Additional message-specific information.</param>
    /// <returns></returns>
    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool PostMessage(IntPtr hWnd, SysDefMsg Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Show the system menu of a window ata given location.
    /// </summary>
    /// <param name="appWindow">target application</param>
    /// <param name="myWindow">calling (foreground) application</param>
    /// <param name="point">point on screen where the menu should appear</param>
    public static void ShowSystemMenu(IntPtr appWindow, IntPtr myWindow, Point point)
    {
        IntPtr wMenu = GetSystemMenu(appWindow, false);
        if (wMenu == IntPtr.Zero) return;

        // Display the menu
        uint command = TrackPopupMenuEx(wMenu, FuFlags.TPM_LEFTBUTTON | FuFlags.TPM_RETURNCMD, (int)point.X, (int)point.Y, myWindow, IntPtr.Zero);
        if (command == 0) return;

        PostMessage(appWindow, SysDefMsg.WM_SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
    }

    /// <summary>
    /// Registers a window as being touch-capable.
    /// </summary>
    /// <param name="hWnd">The handle of the window being registered. The function fails with ERROR_ACCESS_DENIED if the calling thread does not own the specified window.</param>
    /// <param name="ulFlags">A set of bit flags that specify optional modifications. This field may contain 0 or one of the following values.</param>
    /// <returns></returns>
    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterTouchWindow(IntPtr hWnd, uint ulFlags);

    /// <summary>
    /// Retrieves the extra message information for the current thread. Extra message information is an application- or driver-defined value associated with the current thread's message queue. 
    /// </summary>
    /// <returns>The return value specifies the extra information. The meaning of the extra information is device specific.</returns>
    [DllImport("user32.dll", SetLastError = false)]
    static extern uint GetMessageExtraInfo();



	#endregion


	#region HID

	[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern SafeFileHandle CreateFile(
      String fileName,
      [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
      [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
      IntPtr securityAttributes,
      [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
      [MarshalAs(UnmanagedType.U4)] EFileAttributes flags,
      IntPtr template);

    [DllImport("hid")]
    public static extern void HidD_GetHidGuid(
      ref Guid guid);

    [DllImport("setupapi", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(
      ref Guid ClassGuid,
      int Enumerator,
      IntPtr hwndParent,
      int Flags);

    [DllImport("setupapi", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiEnumDeviceInfo(
      IntPtr handle,
      int Index,
      ref DeviceInfoData deviceInfoData);

    [DllImport("setupapi", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiEnumDeviceInterfaces(
      IntPtr handle,
      ref DeviceInfoData deviceInfoData,
      ref Guid guidClass,
      int MemberIndex,
      ref DeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(
      IntPtr handle,
      ref DeviceInterfaceData deviceInterfaceData,
      IntPtr unused1,
      int unused2,
      ref uint requiredSize,
      IntPtr unused3);

    [DllImport("setupapi", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(
      IntPtr handle,
      ref DeviceInterfaceData deviceInterfaceData,
      ref DeviceInterfaceDetailData deviceInterfaceDetailData,
      uint detailSize,
      IntPtr unused1,
      IntPtr unused2);

    [DllImport("setupapi")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr handle);

    #endregion

    #endregion Interop

    // --------------------------------------------------------------------------------------------------
    #region Methods

    /// <summary>
    /// Gets the desktop window handle.
    /// </summary>
    /// <returns></returns>
    public static IntPtr GetDesktopWindowHandle()
    {
        return GetDesktopWindow();
    }

    /// <summary>
    /// Gets the icon for a supplied file.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <returns>File icon.</returns>
    public static Icon GetIconFromFile(string fileName)
    {
        if (String.IsNullOrEmpty(fileName))
            return null;

        if (!File.Exists(fileName))
            return null;

        Icon icon = Icon.ExtractAssociatedIcon(fileName);
        if (icon == null)
            return null;

        return icon;
    }

    /// <summary>
    /// Gets the icon for a supplied file as Image.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <returns>File icon as Image.</returns>
    public static Image GetImageFromFile(string fileName)
    {
        Icon icon = GetIconFromFile(fileName);
        if (icon == null) icon = SystemIcons.Exclamation;
        return icon.ToBitmap();
    }

    /// <summary>
    /// Gets the window icon.
    /// </summary>
    /// <param name="handle">The window handle to get the icon for.</param>
    /// <returns>Window icon.</returns>
    public static Icon GetWindowIcon(IntPtr handle)
    {
        IntPtr icon = IntPtr.Zero;

        SendMessageTimeout(handle, (int)WindowsMessage.GETICON, new IntPtr(ICON_BIG), IntPtr.Zero,
                           SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 1000, out icon);

        if (icon == IntPtr.Zero)
            icon = GetClassLongPtr(handle, GCL_HICON);

        if (icon == IntPtr.Zero)
            SendMessageTimeout(handle, (int)WindowsMessage.QUERYDRAGICON, IntPtr.Zero, IntPtr.Zero,
                               SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 1000, out icon);

        if (icon != IntPtr.Zero)
            return Icon.FromHandle(icon);

        return null;
    }

    /// <summary>
    /// Extracts the icons from a resource.
    /// </summary>
    /// <param name="fileName">Name of the file to extract icons from.</param>
    /// <param name="index">The index to the icon inside the file.</param>
    /// <param name="large">The large icon.</param>
    /// <param name="small">The small icon.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    public static bool ExtractIcons(string fileName, int index, out Icon large, out Icon small)
    {
        IntPtr[] hLarge = new IntPtr[1] { IntPtr.Zero };
        IntPtr[] hSmall = new IntPtr[1] { IntPtr.Zero };

        large = null;
        small = null;

        try
        {
            int iconCount = ExtractIconEx(fileName, index, hLarge, hSmall, 1);

            if (iconCount > 0)
            {
                large = (Icon)Icon.FromHandle(hLarge[0]).Clone();
                small = (Icon)Icon.FromHandle(hSmall[0]).Clone();
                return true;
            }
        }
        finally
        {
            foreach (IntPtr ptr in hLarge)
                if (ptr != IntPtr.Zero)
                    DestroyIcon(ptr);

            foreach (IntPtr ptr in hSmall)
                if (ptr != IntPtr.Zero)
                    DestroyIcon(ptr);
        }

        return false;
    }

    /// <summary>
    /// Send a window message using the SendMessageTimeout method.
    /// </summary>
    /// <param name="hWnd">The window handle to send to.</param>
    /// <param name="msg">The message.</param>
    /// <param name="wParam">The wParam.</param>
    /// <param name="lParam">The lParam.</param>
    /// <returns>Result of message.</returns>
    public static IntPtr SendWindowsMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        IntPtr result = IntPtr.Zero;

        IntPtr returnValue = SendMessageTimeout(hWnd, msg, wParam, lParam, SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 1000,
                                                out result);
        int lastError = Marshal.GetLastWin32Error();

        if (returnValue == IntPtr.Zero && lastError != 0)
            throw new Win32Exception(lastError);

        return result;
    }

    /// <summary>
    /// Send a window message using the SendMessageTimeout method.
    /// </summary>
    /// <param name="hWnd">The window handle to send to.</param>
    /// <param name="msg">The message.</param>
    /// <param name="wParam">The wParam.</param>
    /// <param name="lParam">The lParam.</param>
    /// <returns>Result of message.</returns>
    public static IntPtr SendWindowsMessage(IntPtr hWnd, int msg, int wParam, int lParam)
    {
        return SendWindowsMessage(hWnd, msg, new IntPtr(wParam), new IntPtr(lParam));
    }


    /// <summary>
    /// Enumerates all windows by calling the supplied delegate for each window.
    /// </summary>
    /// <param name="ewp">Delegate to call for each window.</param>
    /// <param name="lParam">Used to identify this enumeration session.</param>
    /// <returns>Number of windows.</returns>
    public static int EnumerateWindows(EnumWindowsProc ewp, IntPtr lParam)
    {
        return EnumWindows(ewp, lParam);
    }

    /// <summary>
    /// Gets the name of a class from a handle.
    /// </summary>
    /// <param name="hWnd">The handle to retreive the class name for.</param>
    /// <returns>The class name.</returns>
    public static string GetClassName(IntPtr hWnd)
    {
        StringBuilder name = new StringBuilder(255);
        GetClassName(hWnd, name, name.Capacity);

        return name.ToString();
    }

    /// <summary>
    /// Used to logoff, shutdown or reboot.
    /// </summary>
    /// <param name="flags">The type of exit to perform.</param>
    /// <param name="reasons">The reason for the exit.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    public static bool WindowsExit(ExitWindows flags, ShutdownReasons reasons)
    {
        EnableToken("SeShutdownPrivilege");
        return ExitWindowsEx((int)flags, (int)reasons) != 0;
    }

    /// <summary>
    /// Tries to enable the specified privilege.
    /// </summary>
    /// <param name="privilege">The privilege to enable.</param>
    /// <exception cref="PrivilegeException">There was an error while requesting a required privilege.</exception>
    /// <remarks>Thanks to Michael S. Muegel for notifying us about a bug in this code.</remarks>
    private static void EnableToken(string privilege)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT || !CheckEntryPoint("advapi32.dll", "AdjustTokenPrivileges"))
            return;
        IntPtr tokenHandle = IntPtr.Zero;
        LUID privilegeLUID = new LUID();
        TOKEN_PRIVILEGES newPrivileges = new TOKEN_PRIVILEGES();
        TOKEN_PRIVILEGES tokenPrivileges;
        if (OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref tokenHandle) == 0)
            throw new PrivilegeException(FormatError(Marshal.GetLastWin32Error()));
        if (LookupPrivilegeValue("", privilege, ref privilegeLUID) == 0)
            throw new PrivilegeException(FormatError(Marshal.GetLastWin32Error()));
        tokenPrivileges.PrivilegeCount = 1;
        tokenPrivileges.Privileges.Attributes = SE_PRIVILEGE_ENABLED;
        tokenPrivileges.Privileges.pLuid = privilegeLUID;
        int size = 4;
        if (AdjustTokenPrivileges(tokenHandle, 0, ref tokenPrivileges, 4 + (12 * tokenPrivileges.PrivilegeCount), ref newPrivileges, ref size) == 0)
            throw new PrivilegeException(FormatError(Marshal.GetLastWin32Error()));
    }

    /// <summary>
    /// Checks whether a specified method exists on the local computer.
    /// </summary>
    /// <param name="library">The library that holds the method.</param>
    /// <param name="method">The entry point of the requested method.</param>
    /// <returns>True if the specified method is present, false otherwise.</returns>
    private static bool CheckEntryPoint(string library, string method)
    {
        IntPtr libPtr = LoadLibrary(library);
        if (!libPtr.Equals(IntPtr.Zero))
        {
            if (!GetProcAddress(libPtr, method).Equals(IntPtr.Zero))
            {
                FreeLibrary(libPtr);
                return true;
            }
            FreeLibrary(libPtr);
        }
        return false;
    }

    /// <summary>
    /// Formats an error number into an error message.
    /// </summary>
    /// <param name="number">The error number to convert.</param>
    /// <returns>A string representation of the specified error number.</returns>
    private static string FormatError(int number)
    {
        try
        {
            StringBuilder buffer = new StringBuilder(255);
            FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero, number, 0, buffer, buffer.Capacity, 0);
            return buffer.ToString();
        }
        catch (Exception)
        {
            return "Unspecified error [" + number.ToString() + "]";
        }
    }

    /// <summary>
    /// Get the window handle for a specified window class.
    /// </summary>
    /// <param name="className">Window class name.</param>
    /// <returns>Handle to a window.</returns>
    public static IntPtr FindWindowByClass(string className)
    {
        if (String.IsNullOrEmpty(className))
            throw new ArgumentNullException("className");

        return FindWindow(className, null);
    }

    /// <summary>
    /// Get the window handle for a specified window title.
    /// </summary>
    /// <param name="windowTitle">The window title.</param>
    /// <returns>Handle to a window.</returns>
    public static IntPtr FindWindowByTitle(string windowTitle)
    {
        if (String.IsNullOrEmpty(windowTitle))
            throw new ArgumentNullException("windowTitle");

        return FindWindow(null, windowTitle);
    }

    /// <summary>
    /// Get the window title for a specified window handle.
    /// </summary>
    /// <param name="hWnd">Handle to a window.</param>
    /// <returns>Window title.</returns>
    public static string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length == 0)
            return null;

        StringBuilder windowTitle = new StringBuilder(length + 1);

        GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

        return windowTitle.ToString();
    }

    /// <summary>
    /// Takes a given window from whatever state it is in and makes it the foreground window.
    /// </summary>
    /// <param name="hWnd">Handle to window.</param>
    /// <param name="force">Force from a minimized or hidden state.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    public static bool SetForegroundWindow(IntPtr hWnd, bool force=false)
    {
        IntPtr fgWindow = GetForegroundWindow();

        if (hWnd == fgWindow)
        {
            return true;
        }

        bool setAttempt = SetForegroundWindow(hWnd);
        if (!force || setAttempt)
            return setAttempt;

        if (fgWindow == IntPtr.Zero)
        {
            return false;
        }

		int fgWindowPID = GetWindowThreadProcessId(fgWindow, out int processId);

		if (fgWindowPID == -1)
        {
            return false;
        }

        // If we don't attach successfully to the windows thread then we're out of options
        int curThreadID = GetCurrentThreadId();
        bool attached = AttachThreadInput(curThreadID, fgWindowPID, true);
        int lastError = Marshal.GetLastWin32Error();

        if (!attached)
        {
            return false;
        }

        SetForegroundWindow(hWnd);
        BringWindowToTop(hWnd);
        SetFocus(hWnd);

        // Detach
        AttachThreadInput(curThreadID, fgWindowPID, false);


        // We've done all that we can so base our return value on whether we have succeeded or not
        return (GetForegroundWindow() == hWnd);
    }

    /// <summary>
    /// Get the Process ID of the current foreground window.
    /// </summary>
    /// <returns>Process ID.</returns>
    public static int GetForegroundWindowPID()
    {
        int pid = -1;

        IntPtr active = GetForegroundWindow();

        if (active.Equals(IntPtr.Zero))
            return pid;

        GetWindowThreadProcessId(active, out pid);

        return pid;
    }


    /// <summary>
    /// Retrieve the path of the Application Frame Host (Modern App)
    /// </summary>
    static readonly string ApplicationFrameHostPath = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\{B9B05098-3E30-483F-87F7-027CA78DA287}\LocalServer32", "", null);

    /// <summary>
    /// Detect if a window is a Modern App (hosted by ApplicationFrameHost).
    /// </summary>
    /// <param name="handle">The window handle.</param>
    /// <returns>true if this is a Modern App</returns>
    public static bool IsWindowModernApp(IntPtr handle)
    {
        int pid = -1;
        GetWindowThreadProcessId(handle, out pid);
        if (pid == -1) return false;

        // Detection of ModernApp
        try
        {
            if (ApplicationFrameHostPath == null) return false;

            Process process = Process.GetProcessById(pid);
            if (process == null) return false;
            var name = process.MainModule.FileName;
            return (process.MainModule.FileName == ApplicationFrameHostPath);
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Gets the process ID of a given window.
    /// For Modern App, this will return the process to the application itself, not the ApplicationFrameHost.
    /// </summary>
    /// <param name="handle">The window handle.</param>
    /// <returns>Process ID</returns>
    public static int GetWindowPID(IntPtr handle)
    {
        int pid = -1;
        GetWindowThreadProcessId(handle, out pid);
        if (pid == -1) return pid;

        return pid;
    }

    /// <summary>
    /// Activates the window by handle.
    /// </summary>
    /// <param name="hWnd">The handle to the window to activate.</param>
    public static void ActivateWindowByHandle(IntPtr hWnd)
    {
        WINDOWPLACEMENT windowPlacement = new WINDOWPLACEMENT();
        windowPlacement.length = Marshal.SizeOf(windowPlacement);
        GetWindowPlacement(hWnd, ref windowPlacement);

        switch (windowPlacement.showCmd)
        {
            case WindowShowStyle.Hide:
                ShowWindow(hWnd, WindowShowStyle.Restore);
                break;

            case WindowShowStyle.ShowMinimized:
                if (windowPlacement.flags == WPF_RESTORETOMAXIMIZED)
                    ShowWindow(hWnd, WindowShowStyle.ShowMaximized);
                else
                    ShowWindow(hWnd, WindowShowStyle.ShowNormal);
                break;

            default:
                SetForegroundWindow(hWnd, true);
                break;
        }
    }


    /// <summary>
    /// Gets a window long pointer.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="nIndex">Index of the data to retreive.</param>
    /// <returns>Unsigned integer (IntPtr) of retreived data.</returns>
    public static uint GetWindowLongPtr(IntPtr hWnd, GWL nIndex)
    {
        if (IntPtr.Size == 8)
            return GetWindowLongPtr64(hWnd, nIndex);
        else
            return GetWindowLongPtr32(hWnd, nIndex);
    }


    /// <summary>
    /// Gets a class long pointer.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="nIndex">Index of the data to retreive.</param>
    /// <returns>IntPtr of retreived data.</returns>
    public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return GetClassLongPtr64(hWnd, nIndex);
        else
            return new IntPtr(GetClassLongPtr32(hWnd, nIndex));
    }


	/// <summary>
	/// Make a window transparent to mouse events
	/// </summary>
	/// <param name="hwnd"></param>
	public static void SetWindowExTransparent(IntPtr hwnd)
	{
		var extendedStyle = GetWindowLongPtr(hwnd, GWL.GWL_EXSTYLE);
		SetWindowLong(hwnd, GWL.GWL_EXSTYLE, (WindowExStyles)extendedStyle |  WindowExStyles.WS_EX_TRANSPARENT);
	}

	/// <summary>
	/// Return the executable-path represented by the given Window-handle.
	/// </summary>
	/// <param name="hWnd">The handle to the window from which the file-path must be retrieved.</param>
	/// <returns>Full file-path of the executable file which opened the window, or null if failed</returns>
	public static string GetExecutablePathByHandle(IntPtr hWnd)
    {
        int pid = Win32.GetWindowPID(hWnd);
        if (pid == -1) return null;

        Process process = Process.GetProcessById(pid);
        if (process == null) return null;

        string ret = null;

        try
        {
            ret = process.MainModule.FileName;
        }
        catch
        {
            ret = process.ProcessName;
        }

        return ret;
    }

    
    /// <summary>
    /// Show the desktop.
    /// </summary>
    public static void ShowDesktop()
    {
        IntPtr trayWnd = FindWindow("Shell_TrayWnd", null);

        if (trayWnd == IntPtr.Zero)
            return;

		SendMessageTimeout(trayWnd, (int)WindowsMessage.COMMAND, new IntPtr(MINIMIZE_ALL), IntPtr.Zero,
						   SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 1000, out IntPtr result);
	}

    /// <summary>
    /// Get an IntPtr pointing to any object.
    /// </summary>
    /// <param name="obj">Object to get pointer for.</param>
    /// <returns>Pointer to object.</returns>
    public static IntPtr VarPtr(object obj)
    {
        GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
        IntPtr ptr = handle.AddrOfPinnedObject();
        handle.Free();
        return ptr;
    }

    public static bool Check64Bit()
    {
        //IsWow64Process is not supported under Windows2000 ( ver 5.0 )
        int osver = Environment.OSVersion.Version.Major * 10 + Environment.OSVersion.Version.Minor;
        if (osver <= 50) return false;

        Process p = Process.GetCurrentProcess();
        IntPtr handle = p.Handle;
		bool success = IsWow64Process(handle, out bool isWow64);
		if (!success)
        {
            throw new Win32Exception();
        }
        return isWow64;
    }


    public static string SHGetPathFromIDList(IntPtr pidl)
    {
        StringBuilder path = new System.Text.StringBuilder(256);
        SHGetPathFromIDList(pidl, path);
        return path.ToString();
    }


    public static bool RegisterHotKey(IntPtr hWnd, int id, KeyModifier modifier, System.Windows.Forms.Keys key)
    {
        return RegisterHotKey(hWnd, id, (int)modifier, (int)key);
    }

    /// <summary>
    /// Return true if last event was generated from Touch (gesture)
    /// </summary>
    public static bool IsTouchEvent
    {
        get
        {
            const uint MOUSEEVENTF_FROMTOUCH = 0xFF515700;
            return (GetMessageExtraInfo() & MOUSEEVENTF_FROMTOUCH) != 0;
        }
    }


    #endregion Methods


    // --------------------------------------------------------------------------------------------------
    #region PIDL handling

    /// <summary>
    /// Convert a Shell IDList Array structure to an array of PILD. Each PILDs must be freed with ILFree. 
    /// </summary>
    /// <param name="cidlData">Shell IDList Array structure, like the one you get from DragEventArgs.Data.GetData("Shell IDList Array")</param>
    /// <returns>Array of PILDs. Each PILDs must be freed with ILFree.</returns>
    public static IntPtr[] CILDtoPIDL(MemoryStream cidlData)
    {
        byte[] b = cidlData.ToArray();
        IntPtr p = Marshal.AllocHGlobal(b.Length);
        Marshal.Copy(b, 0, p, b.Length);

        // Get number of items.
        UInt32 cidl = (UInt32)Marshal.ReadInt32(p);

        // Get parent folder.
        int offset = sizeof(UInt32);
        IntPtr parentpidl = (IntPtr)((int)p + (UInt32)Marshal.ReadInt32(p, offset));
        StringBuilder fpath = new StringBuilder(256);
        Win32.SHGetPathFromIDList(parentpidl, fpath);

        // Get subitems.
        var ret = new IntPtr[cidl];
        for (int i = 0; i < cidl; i++)
        {
            offset += sizeof(UInt32);
            IntPtr relpidl = (IntPtr)((int)p + (UInt32)Marshal.ReadInt32(p, offset));
            ret[i] = Win32.ILCombine(parentpidl, relpidl);
            //Win32.SHGetPathFromIDList(ret[i], fpath);
        }

        return ret;
    }


    #endregion


    // --------------------------------------------------------------------------------------------------
    #region SetThreadExecutionState

    [FlagsAttribute]
    public enum EXECUTION_STATE : uint
    {
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001
        // Legacy flag, should not be used.
        // ES_USER_PRESENT = 0x00000004
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    #endregion SetThreadExecutionState
}

/// <summary>
/// The exception that is thrown when an error occures when requesting a specific privilege.
/// </summary>
public class PrivilegeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the PrivilegeException class.
    /// </summary>
    public PrivilegeException() : base() { }
    /// <summary>
    /// Initializes a new instance of the PrivilegeException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PrivilegeException(string message) : base(message) { }
}
