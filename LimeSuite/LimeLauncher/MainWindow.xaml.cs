/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/


using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.IO;
using System.ComponentModel;
using System.Windows.Media;

// For TrayIcon
using Hardcodet.Wpf.TaskbarNotification;

// Framework
using Lime;
using WPFhelper;
using static LimeLauncher.ConfigLocal;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using LimeLauncher.Controls;
using System.Runtime.Versioning;

namespace LimeLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public partial class MainWindow : Window
    {
        // --------------------------------------------------------------------------------------------------
        #region Types

        /// <summary>
        /// Describe the some measures of the main window
        /// </summary>
        public struct WindowGeometry
        {
			public bool WinStateChanging;
            public double IconWidth;
            public int ColumnCount;
        }
        
        /// <summary>
        /// Store the data to trace the mouse motion
        /// </summary>
        private struct TrackMove
        {
            public enum Action
            {
                None,
                FullScreen,
                Restore
            }

            public const int threshold = 10;
            public bool enable;
            public Action action;
            public Point origin;
        }
        
        
        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Attributes/Properties

        /// <summary>
        /// Windows Handle reference of this window
        /// </summary>
        public IntPtr Handle { get; private set; }

        // Tray Icon GUI
        private static TaskbarIcon trayIcon;

		// Global Input devices
		private HwndSource hwndSource;
        private TrackMove trackMove;


        // Geometry
        public WindowGeometry Geometry;

        // Others
        private bool RestoreWindow = false;
        
        /// <summary>
        /// State of the window when shown (cannot be: Minimized)
        /// </summary>
        public WindowState WindowStateShow { get; private set; }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        /// <summary>
        /// Create the LimeLauncher main window
        /// </summary>
        public MainWindow()
        {
            // Store windows instance to the Commands
            Commands.MainWindow = this;

            AnimateAction.EnableAnimations = Global.User.EnableAnimations;

			// Window initialization
			InitializeComponent();

            WindowStateShow = WindowState.Normal;
            if (Global.User.HideOnLaunch) // Start Minimized, saved state when shown
            { 
                WindowState = WindowState.Minimized;
                if (Global.Local.WindowMain.WindowState!= WindowState.Minimized)
                    WindowStateShow = Global.Local.WindowMain.WindowState;
            }
            else
            {
                WindowState = Global.Local.WindowMain.WindowState != WindowState.Maximized ? Global.Local.WindowMain.WindowState : WindowState.Normal;
            }

            if (Global.User.ShowWindowBorders)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
            else
            {
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
            }

			// Load the skin
			LoadSkin();

            // Initialize NotifyIcon with resource in MainWindow.xaml
            trayIcon = (TaskbarIcon)FindResource("LimeTrayIcon");

            // Replace default popup-window by Tooltip-balloon
            LimeMsg.Handlers += Notify;
            LimeMsg.Handlers -= LimeMsg.WinDialog;

            // Callback initialization
            trackMove.enable = false;

            // Initialize the handle
            var helper = new WindowInteropHelper(this); // .NET 4.0
            Handle = helper.EnsureHandle();
            LimeMsg.Debug("MainWindow: Handle: {0}", Handle);
            Browser.TaskSwitcher.Handle = Handle;

            // Command Binding registration
            Commands.Bind(this, Global.ConfigTree.AppCommands);

            // Initialize Hot Keys
            EventManager();
            Window_LocationChanged();

            // Finished
            LimeMsg.Debug("MainWindow: Done.");
            LimeMsg.Debug("------------------------------------------------------------------------------");

            // Load content
            Browser.Load();

            // Show window depending on the settings
            if (Global.User.ShowInTaskbar || WindowState != WindowState.Minimized)
            {
                base.Show();
                Global.Local.WindowState = WindowState;
            }
        }

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Methods


		/// <summary>
		/// Request to refresh the GUI of LimeLauncher
		/// </summary>
		public void Refresh()
		{
			// Refesh Layout
			Dispatcher.Invoke(DispatcherPriority.Render, _EmptyDelegate);

			// Refresh Panels
			Browser.Refresh();
		}
		private static Action _EmptyDelegate = delegate () { };

		/// <summary>
		/// Refresh the display to respect the current control-mode
		/// </summary>
		/// <param name="mode">Control-mode to be set</param>
		public void RefreshCtrlMode(CtrlMode mode)
        {
            LimeMsg.Debug("***** CtrlMode: Set {0}", mode.ToString());

			// Get button which is overed by the mouse, and emulate mouse enter/leave
			var coord = Mouse.GetPosition(this);
			HitTestResult result = VisualTreeHelper.HitTest(this, coord);
			if (result != null)
			{
				var wxOvered = WPF.FindFirstParent<Control>(result.VisualHit);
				if (wxOvered != null)
				{
                    LimeMsg.Debug("RefreshCtrlMode: Emulate Mouse Enter/Leave: {0}", wxOvered);
                    if (mode == CtrlMode.Mouse)
                    {
                        Browser.Button_MouseEnter(wxOvered);
                    }
                    else
                    {
                        Browser.Button_MouseLeave(wxOvered);
                    }
                }
            }

			switch (mode)
            {
                case CtrlMode.Mouse:
                    {
                        Mouse.OverrideCursor = null;
						break;
                    }

                case CtrlMode.Touch:
                    {
                        if(Commands.CfgWindow==null) Mouse.OverrideCursor = Cursors.None;
                        break;
                    }

                case CtrlMode.Keyboard:
                case CtrlMode.CLI:
                    {
                        if (Commands.CfgWindow == null) Mouse.OverrideCursor = Cursors.None;
						break;
                    }
            }
        }

        /// <summary>
        /// Define whether the mouse pointer is over the window border and in which side/corner.
        /// In addition, this change the pointer into the resize form according to the direction.
        /// </summary>
        /// <returns>One of the eight possible sides/corners if over the border, None otherwise</returns>
        private WPF.ResizeDirection MouseOverWinBorder()
        {
            const int borderTolerancePix = 10;
            WPF.ResizeDirection ret = WPF.ResizeDirection.None;

            if (Global.Local.CtrlMode == CtrlMode.Mouse && wxWinBorder.IsMouseDirectlyOver)
            {
                // Compute position of the mouse-pointer relative to the Border content
                Point pos = Mouse.GetPosition(wxWinBorder);
                int cX = (int)pos.X - (int)wxWinBorder.BorderThickness.Left;
                int cY = (int)pos.Y - (int)wxWinBorder.BorderThickness.Top;
                int wX = (int)wxWinBorder.ActualWidth - (int)wxWinBorder.BorderThickness.Left - (int)wxWinBorder.BorderThickness.Right - borderTolerancePix;
                int wY = (int)wxWinBorder.ActualHeight - (int)wxWinBorder.BorderThickness.Top - (int)wxWinBorder.BorderThickness.Bottom - borderTolerancePix;

                Cursor cursor = null;

                if (cX < borderTolerancePix)
                {
                    if (cY < borderTolerancePix)
                    {
                        ret = WPF.ResizeDirection.TopLeft;
                        cursor = Cursors.SizeNWSE;
                    }
                    else if (cY > wY)
                    {
                        ret = WPF.ResizeDirection.BottomLeft;
                        cursor = Cursors.SizeNESW;
                    }
                    else
                    {
                        ret = WPF.ResizeDirection.Left;
                        cursor = Cursors.SizeWE;
                    }
                }
                else if (cX > wX)
                {
                    if (cY < borderTolerancePix)
                    {
                        ret = WPF.ResizeDirection.TopRight;
                        cursor = Cursors.SizeNESW;
                    }
                    else if (cY > wY)
                    {
                        ret = WPF.ResizeDirection.BottomRight;
                        cursor = Cursors.SizeNWSE;
                    }
                    else
                    {
                        ret = WPF.ResizeDirection.Right;
                        cursor = Cursors.SizeWE;
                    }
                }
                else
                {
                    ret = cY < borderTolerancePix ? WPF.ResizeDirection.Top
                        : WPF.ResizeDirection.Bottom;
                    cursor = Cursors.SizeNS;
                }

                if (this.WindowState == WindowState.Maximized)
                {
                    Mouse.OverrideCursor = null;
                }
                else
                {
                    Mouse.OverrideCursor = cursor;
                }

                //LimeMsg.Debug(String.Format("MouseOverWinBorder: {0} : pos: {1}, {2}  size: {3}, {4}", ret.ToString(), cX, cY, wX, wY));
            }

            return ret;
        }


		/// <summary>
		/// Load the Xaml Skin
		/// </summary>
		/// <param name="name">Name of the skin to be loaded, default no skin loaded</param>
		/// <param name="loadParam">Load the parameters from user-config</param>
		/// <returns>true if loading the screen succeed.</returns>
		public bool LoadSkin(string name = null, bool loadParam = true)
        {
            if (name==null)  name = Global.User.Skin;

			Notify();

            LimeMsg.Debug("*** LoadSkin: {0}", name ?? "none");

            // Cancel previous Item Loading (not only on Root, but on all the tree)
            Global.Root.Cancel();

			Skin skin = null;
			try
			{
				// Hook Skin Keywords to the skin class
				skin = new Skin(name, loadParam);
			}
			catch (Exception ex)
			{
				if (Global.User.DevMode)
				{
					// TODO: more detailed Error handling (for skin debugging)
					string diag = string.Format("{0}\n{1}\n{2}",
						name ?? "none", 
						ex?.Message,
						ex?.InnerException?.Message);
					LimeMsg.Error("ErrSkinOpen", diag);
				}
				else
				{
					LimeMsg.Error("ErrLoadSkin", name ?? "none");
				}
			}

			LimeLib.LifeCheck();

			// Catch error
			if (skin == null)
            {
                LimeMsg.Debug("*** LoadSkin: Failed.");

                // This will fall back to previous skin if any loaded yet
                if (Global.Local.Skin != null)
                {
					LimeMsg.Debug("*** LoadSkin: Restore previous skin: {0}", Global.Local.Skin.Name);
				}
				else if (name != Skin.FallbackSkin)
                {
					// Fallback to default skin if initial Skin loading
					LimeMsg.Debug("*** LoadSkin: Try to load Fallback skin: {0}", Skin.FallbackSkin);
                    Global.User.Skin = Skin.FallbackSkin;
                }
                else
                {
                    // Die
                    throw new Exception("Failed to load Skin");
                }

				return false;
			}
			else
			{
				// Validate this skin
				Global.Local.Skin = skin;
				LimeMsg.Debug("*** LoadSkin: Done.");
				return true;
			}

		}


        /// <summary>
        /// Hide LimeLauncher window
        /// </summary>
        public new void Hide()
        {
            LimeMsg.Debug("Hide");
            Global.Local.WindowState = WindowState.Minimized;
        }


        /// <summary>
        /// Show LimeLauncher window
        /// </summary>
        public new void Show()
        {
            LimeMsg.Debug("Show: WindowState: {0}, WindowStateShow: {1}", WindowState, WindowStateShow);

            // Bring to foreground
            if (WindowState != WindowState.Minimized) Global.Local.OnTop = true;

            // Restore window (if required)
            if (WindowStateShow != WindowState.Minimized)
                Global.Local.WindowState = WindowStateShow;
            else
                Global.Local.WindowState = WindowState.Normal;

            LimeMsg.Debug("Show End: WindowState: {0}, WindowStateShow: {1}", WindowState, WindowStateShow);
        }


        /// <summary>
        /// Must be called back by Global.Local.WindowState only
        /// </summary>
        public void UpdateWindowState()
        {
			Geometry.WinStateChanging = true;
			WindowState state = Global.Local.WindowState;

            LimeMsg.Debug("UpdateWindowState: {0}, {1}, WindowStateShow: {2}", state, Visibility, WindowStateShow);

            if (WindowState != WindowState.Minimized) WindowStateShow = WindowState;


            if (state == WindowState.Minimized)
            {
                // Hide

                if (WindowState != WindowState.Minimized) WindowStateShow = WindowState;

                AnimateAction.Do("wxWindow_hide", () =>
                {
                    Mouse.OverrideCursor = null;
                    WindowState = WindowState.Minimized;

                    if (! Global.User.ShowInTaskbar)
                    {
                        base.Hide();
                    }
                });
            }
            else
            {
                // Show

                WindowStateShow = state;

                RestoreWindow = true;
                Browser.HideTasks();

                // Hide/Show window border if enabled
                if (!Global.User.ShowWindowBordersFullScreen && !AllowsTransparency)
                { 
                    WindowStyle = state == WindowState.Maximized ? WindowStyle.None : WindowStyle.SingleBorderWindow;
                }


                if (Visibility != Visibility.Visible)
                {
                    base.Show();
                    WindowState = WindowStateShow;
                }
                else
                {
                    WindowState = WindowStateShow;
                    WindowRefresh();
                }
            }

			Geometry.WinStateChanging = false;

			LimeMsg.Debug("UpdateWindowState End: {0}, {1}, WindowStateShow: {2}", state, Visibility, WindowStateShow);

        }


        /// <summary>
        /// Move the window to a screen by name
        /// </summary>
        /// <param name="screen">Identifier of the screen</param>
        public void MoveToScreen(string screen)
        {
            LimeMsg.Debug("MoveToScreen: {0}", screen);

            foreach (var scr in System.Windows.Forms.Screen.AllScreens)
            {
                string scname;
                try
                {
                    scname = Path.GetFileName(scr.DeviceName);
                }
                catch
                {
                    scname = scr.DeviceName;
                }

                if (scname == screen)
                {
                    // ScreenMotion
                    var wpl = WPF.GetWindowPlacement(Handle);

                    // Get dimensions
                    int x = scr.WorkingArea.Left;
                    int y = scr.WorkingArea.Top;
                    int cx = wpl.NormalPosition.Right - wpl.NormalPosition.Left;
                    int cy = wpl.NormalPosition.Bottom - wpl.NormalPosition.Top;
                    const int margin = 5;

                    // Resize if bigger than window
                    if (cx > scr.WorkingArea.Width - 2 * margin) cx = scr.WorkingArea.Width - 2 * margin;
                    if (cy > scr.WorkingArea.Height - 2 * margin) cy = scr.WorkingArea.Height - 2 * margin;

                    // Center
                    x += (scr.WorkingArea.Width - cx) / 2;
                    y += (scr.WorkingArea.Height - cy) / 2;

                    // Set Normal position
                    wpl.NormalPosition.Left = x;
                    wpl.NormalPosition.Top = y;
                    wpl.NormalPosition.Right = x + cx;
                    wpl.NormalPosition.Bottom = y + cy;

                    // Apply the motion/resize
                    Global.Local.WindowMain = wpl;
                    RestoreWindow = true;
                    if (IsLoaded) WPF.SetWindowPlacement(Handle, wpl, this);
                    break;
                }

            }

            LimeMsg.Debug("MoveToScreen End: {0}", screen);
        }


		/// <summary>
		/// Adjust Info Pane according to its requirements
		/// </summary>
		/// <param name="load">Load setting if true, or adjust to current size if false (default)</param>
		/// <param name="save">Save to setting if true (default: false)</param>
		/// <param name="setColumn">Set size according to MainPaneColumns setting if true (default: false)</param>
		public void AdjustInfoPane(bool load = false, bool save = false, bool setColumn = false)
        {
			// avoid re-entrance
			if (_AdjustInfoPane_reentry) return;
			_AdjustInfoPane_reentry = true;

			int columns = Geometry.ColumnCount > 1 ? Geometry.ColumnCount : 1;

			if (Global.Local.InfoPaneVisible)
            {
				double rootWidth = Browser.wxRoot.ActualWidth;

				if (rootWidth == 0)
				{
					LimeMsg.Debug("AdjustInfoPane: {0} : Init: {1}", load, Global.Local.MainPaneSize);
					if (load && Global.Local.MainPaneSize > -0.5)
					{
						wxMainPaneColumn.Width = new GridLength(Global.Local.MainPaneSize * TypeScaled.Scale);
					}
					_AdjustInfoPane_reentry = false;
					return;
				}

				double size = 0.0;
                double width = wxMainPaneColumn.Width.Value; if (width<=1) width = wxMainPaneColumn.ActualWidth;
				double margins = Browser.wxRoot.Margin.Left + Browser.wxRoot.Margin.Right;
				double gap = Browser.wxScroll.ViewportWidth - rootWidth - margins;
				double iconSize = (Global.Local.Skin.IconBigSize.Content as DoubleScaled).Scaled;
				double minPaneSize = width - rootWidth + iconSize;
                if (minPaneSize < 0) minPaneSize = 0;


                wxInfoPaneColumn.Width = new GridLength(1, GridUnitType.Star);
				if (ActualWidth> minPaneSize) wxMainPaneColumn.MaxWidth =  ActualWidth - minPaneSize;
				wxMainPaneColumn.MinWidth = minPaneSize;


				// Set Default size
				if (load && Global.Local.MainPaneSize < -0.5)
				{
					if (Global.Local.ListView) 
					{
						Global.Local.MainPaneSize = (width - Browser.wxScroll.ViewportWidth - margins + iconSize * 2) / TypeScaled.Scale;
					}
					else
					{
						Global.Local.MainPaneSize = ActualWidth / 2;
					}
				}

				// Adjust dimensions
				if (Global.Local.ListView)
				{
					// List View displacement
					size = !load ? width : Global.Local.MainPaneSize * TypeScaled.Scale;
					wxSplitter.DragIncrement = 1;
				}
				else if (setColumn && Geometry.ColumnCount > 0 && Geometry.IconWidth > 10.0)
				{
					// Derive size from MainPaneColumns
					int diff = Geometry.ColumnCount - Global.Local.MainPaneColumns;
					size = width - gap - diff * Geometry.IconWidth;

					// coerce
					if (size > wxMainPaneColumn.MaxWidth) size = width - gap;
				}
				else if (Geometry.IconWidth > 10.0 && (Geometry.ColumnCount > 1 || gap > 0.5 * Geometry.IconWidth))
                {
					// Quantum displacement
					if (load)
					{
						size = Global.Local.MainPaneSize * TypeScaled.Scale;
					}
					else
					{
						int diff = gap > Geometry.IconWidth / 2.0 ? -1 : 0;
						size = width - gap - diff * Geometry.IconWidth;

						// coerce
						if (size > wxMainPaneColumn.MaxWidth) size = width - gap;
					}
					wxSplitter.DragIncrement = Geometry.IconWidth;
                }
				else
				{
					// Analogic displacement
					size = !load ? width : Global.Local.MainPaneSize * TypeScaled.Scale;
					wxSplitter.DragIncrement = 1;
				}

				// coerce
				if (size > wxMainPaneColumn.MaxWidth) size = wxMainPaneColumn.MaxWidth;
				if (size < wxMainPaneColumn.MinWidth) size = wxMainPaneColumn.MinWidth;


				wxMainPaneColumn.Width = new GridLength(size);

                LimeMsg.Debug("AdjustInfoPane: {0} : Main: {1}, columns: {2}, IconWidth: {3}, Gap: {4} --> size: {5}",
					load, width, Geometry.ColumnCount, Geometry.IconWidth, gap, size);

				// Save size
				if (save) Global.Local.MainPaneSize = size / TypeScaled.Scale;
				
			}
			else
            {
                LimeMsg.Debug("AdjustInfoPane: {0}", Global.Local.InfoPaneVisible);
				wxMainPaneColumn.MaxWidth = double.PositiveInfinity;
				wxMainPaneColumn.Width = new GridLength(1, GridUnitType.Star);
                wxInfoPaneColumn.Width = new GridLength(0);
			}

			if (!setColumn) Global.Local.MainPaneColumns = columns;
			_AdjustInfoPane_reentry = false;
		}
		private bool _AdjustInfoPane_reentry = false;

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Zoom

		private DispatcherTimer _RenderZoomTime = null;
		private bool _RenderZoomEndDelay = true;

		/// <summary>
		/// Handle Mouse-Scroll
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                LimeMsg.Debug("Window_PreviewMouseWheel: Delta {0}", e.Delta);
                Global.Local.OnTop = true;
                if (e.Delta > 0) Commands.ZoomIn.Execute(); else Commands.ZoomOut.Execute();
                e.Handled = true;
            }
        }


        /// <summary>
        /// Handle the Zoom end
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                LimeMsg.Debug("Window_PreviewKeyUp: Released Ctrl");
                if (!_RenderZoomEndDelay) RenderZoom(false);
            }

			Notify();

            e.Handled = false;
        }

        /// <summary>
        /// Render the ongoing Zooming
        /// </summary>
        /// <param name="enable">Enable or disable the Zoom rendering</param>
        public void RenderZoom(bool enable)
        {
            LimeMsg.Debug("RenderZoom: {0} : {1}, delay: {2}", enable, Global.Local.Zoom, _RenderZoomEndDelay);
            if (enable)
            {
				// Initialize zoom
				if (!Zooming)
				{
					Browser.HideTasks();
					wxMainGrid.RenderTransform = new ScaleTransform();
				}

				var transform = (ScaleTransform) wxMainGrid.RenderTransform;

				double factor = Global.Local.Zoom / Global.Local.Scale;

				LimeMsg.Info("ZoomShow", (int)(Global.Local.Zoom * 100.0));

                // Cancel any ongoing End-Delay
                _RenderZoomTime?.Stop();


				transform.ScaleX = transform.ScaleY = factor;

				// Start End Delay Timer
				if (_RenderZoomEndDelay)
                {
                    if (_RenderZoomTime == null)
                    {
                        _RenderZoomTime = new DispatcherTimer();
                        _RenderZoomTime.Tick += OnRenderZoomEndDelayEllapsed;
                    }

                    _RenderZoomTime.Interval = TimeSpan.FromMilliseconds(Global.User.ApplyZoomAfter);
                    _RenderZoomTime.Start();
                }

            }
            else if (Zooming)
            {
                // End the Zoom
                _RenderZoomTime?.Stop();
				wxMainGrid.RenderTransform = null;
				Notify();

				Global.Local.Scale = Global.Local.Zoom;
				AdjustInfoPane(load: true);
				Browser.Refresh();

                // Required in case it is called from timer, it seems
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void OnRenderZoomEndDelayEllapsed(object sender, EventArgs e)
        {
            LimeMsg.Debug("RenderZoom: OnRenderZoomEndDelayEllapsed: {0}", _RenderZoomEndDelay);
            _RenderZoomTime.Stop();
            if (_RenderZoomEndDelay)
            {
                // Control Key is pressed
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && Global.Local.OnTop)
                    _RenderZoomEndDelay = false;
                else
                    RenderZoom(false);
            }
        }


        /// <summary>
        /// Return true if the Zoom in ongoing
        /// </summary>
        public bool Zooming { get { return wxMainGrid.RenderTransform is ScaleTransform; } }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Notifier


        /// <summary>
        /// Delegate function to handle popup message from LimeMsg. 
        /// </summary>
        /// <param name="lvl">severity level of the message</param>
        /// <param name="msg">Textual description of the message</param>
        private static void Notify(LimeMsg.Severity lvl, string msg)
        {
            if (lvl < LimeMsg.Popup) return;

			LimeMsg.Debug("Notify: {0}", msg);

			var win = Commands.MainWindow;
			win.Dispatcher.Invoke(() =>
			{


				if (win != null && (win.WindowState != WindowState.Minimized || lvl < LimeMsg.Severity.Warning))
				{
					// --- Use OSD Notifier ---

					switch (lvl)
					{
						case LimeMsg.Severity.Error: win.wxNotifierBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)); break;
						case LimeMsg.Severity.Warning: win.wxNotifierBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 128, 0)); break;
						default: win.wxNotifierBorder.ClearValue(Border.BorderBrushProperty); break;
					}

					win.wxNotifierText.Text = msg;
					win.wxNotifier.IsOpen = true;
				}
				else
				{
					// --- Use Tray Icon ---

					BalloonIcon icon;
					switch (lvl)
					{
						case LimeMsg.Severity.Error: icon = BalloonIcon.Error; break;
						case LimeMsg.Severity.Warning: icon = BalloonIcon.Warning; break;
						default: icon = BalloonIcon.Info; break;
					}

					trayIcon.ShowBalloonTip(lvl.ToString(), msg, icon);
				}
			});
        }

        /// <summary>
        /// Stop all notifications
        /// </summary>
        public void Notify()
        {
            // OSD Notifier
            //LimeMsg.Debug("Notify: End");
            wxNotifier.IsOpen = false;
        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Event Manager

        /// <summary>
        /// Initialize the event manager
        /// </summary>
        private void EventManager()
        {
            // System events
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            _WinEventProc = new Win32.WinEventDelegate(WinEventProc);
            IntPtr m_hhook = Win32.SetWinEventHook(
                Win32.WinEvent.EVENT_SYSTEM_FOREGROUND, Win32.WinEvent.EVENT_SYSTEM_FOREGROUND, 
                IntPtr.Zero, _WinEventProc, 0, 0, Win32.WinEventFlags.WINEVENT_OUTOFCONTEXT);


            // Enable Touch Handling (WM_TOUCH events)
            //try
            //{
            //    // Registering the window for multi-touch, using the default settings.
            //    // p/invoking into user32.dll
            //    if (!Win32.RegisterTouchWindow(handle, 0))
            //    {
            //        LimeMsg.Debug("Could not register window for multi-touch");
            //    }
            //}
            //catch 
            //{
            //    LimeMsg.Debug("Could not register window multi-touch API");
            //}


            // Hook the global input device callback
            hwndSource = HwndSource.FromHwnd(Handle);
            hwndSource.AddHook(HwndHook);

            // Global Hotkeys
            //Win32.UnregisterHotKey(handle, 0);
            Win32.RegisterHotKey(Handle, 0, Win32.KeyModifier.CTRL, System.Windows.Forms.Keys.Space);

            // Application keys
            LimeMsg.Debug("EventManager: TODO: remove when Command Key handling done");
            this.InputBindings.Clear();
            this.InputBindings.Add(new KeyBinding(Commands.ItemMenu, Key.F, ModifierKeys.Alt));
            this.InputBindings.Add(new KeyBinding(new LimeCommand(Commands.Maximize, toggle: true), Key.Enter, ModifierKeys.Alt));
        }

        /// <summary>
        /// Handle windows messages
        /// See: http://blog.magnusmontin.net/2015/03/31/implementing-global-hot-keys-in-wpf/
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr HwndHook(IntPtr hwnd, int wmsg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            var msg = (Win32.WindowsMessage) wmsg;

    //        LimeMsg.Debug("HwndHook: {0} : 0x{1:X} - wParam: 0x{2:X}, lParam: 0x{3:X}", 
				//Enum.GetName(typeof(Win32.WindowsMessage), msg), wmsg, wParam, lParam);
           

            if (msg == Win32.WindowsMessage.HOTKEY)
            {
                switch (wParam.ToInt32())
                {
                    case 0:
                        LimeMsg.Debug("HwndHook: GlobalKey 0");
                        Global.Local.CtrlMode = CtrlMode.Keyboard;
                        Commands.Show.Toggle();
                        handled = true;
                        break;
                }
            }
            else if (msg == Win32.WindowsMessage.MOUSEMOVE) 
            {
                if (IsMouseOver)
                {
                    if (Global.Local.CtrlMode == CtrlMode.Mouse)
                    {
                        _mousePos = -1;
                        MouseOverWinBorder();
                    }
                    else
                    {
						// Mouse motion detection

						var xPos = (Int16) (lParam.ToInt32() & 0xFFFF);
						var yPos = (Int16) (lParam.ToInt32() >> 16);

						var xPrev = (Int16) (_mousePos & 0xFFFF);
						var yPrev = (Int16) (_mousePos >> 16);


						if (_mousePos == -1)
						{
							_mousePos = (yPos << 16) | (xPos & 0x0000FFFF);
						}
						else if (Math.Abs(xPos - xPrev) >= _mouseCountThresh || Math.Abs(yPos - yPrev) >= _mouseCountThresh)
						{
							// Not really a distance formula, but cheaper and good enough for the use

							LimeMsg.Debug("HwndHook: MOUSEMOVE ({0}, {1}) - ({2}, {3})", xPos, yPos, xPrev, yPrev);
							Global.Local.CtrlMode = CtrlMode.Mouse;
						}


                    }
                }

                if (trackMove.enable)
                {
                    var coord = Mouse.GetPosition(this);
                    if( Math.Abs((int)coord.X - (int)trackMove.origin.X) > TrackMove.threshold || Math.Abs((int)coord.Y - (int)trackMove.origin.Y) > TrackMove.threshold)
                    {
                        double width = ActualWidth;
                        double height = ActualHeight;
                        Point win = Application.Current.MainWindow.PointToScreen(new Point(0, 0)); // Get window coordinate in pixels
                        LimeMsg.Debug("HwndHook: trackMove End Drag: pos: {0}, {1} / width: {2}, {3}", coord.X, coord.Y, width, Width);
                        WindowState = WindowState.Normal;
                        Point winOff = Application.Current.MainWindow.PointFromScreen(win); // Get window offset to its full-screen location
                        LimeMsg.Debug("HwndHook: trackMove End Drag: off: {0}, {1} --> {2}, {3}", win.X, win.Y, winOff.X, winOff.Y);
                        Left += winOff.X + coord.X * (1.0 - Width / width);
                        Top += winOff.Y + coord.Y * (1.0 - Height / height);
                        if (WindowState == WindowState.Maximized)  WindowState = WindowState.Normal;
                        trackMove.enable = false;
                        Window_MouseLeftButtonDown();
                    }

                }

            }
            else if (msg == Win32.WindowsMessage.LBUTTONUP)
            {
                Notify();

                if (trackMove.enable)
                {
                    LimeMsg.Debug("HwndHook: trackMove End {0}", trackMove.action.ToString());

                    if (trackMove.action == TrackMove.Action.Restore)
                    {
                        if (Global.User.ClickOnBorderAction == ConfigUser.ClickOnBorderActionType.Restore)
                            WindowState = WindowState.Normal;
                        else if (Global.User.ClickOnBorderAction == ConfigUser.ClickOnBorderActionType.Hide)
                            WindowState = WindowState.Minimized;
                    }
                    else if (trackMove.action == TrackMove.Action.FullScreen)
                    {
                        WindowState = WindowState.Maximized;
                    }

                    trackMove.enable = false;
                }
            }
            else if (msg > Win32.WindowsMessage.MOUSEMOVE && msg <= Win32.WindowsMessage.MOUSEHWHEEL)
            {
                if (!Zooming) Notify();

                // LimeMsg.Debug(String.Format("GlobalMouse: 0x{0:x}", msg));
                if (IsMouseOver)
                {
                    Global.Local.CtrlMode = CtrlMode.Mouse;
                }
            }

            return IntPtr.Zero;
        }
        private const int _mouseCountThresh = 20;
        private int _mousePos = -1;


        /// <summary>
        /// Detect change in screen settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            LimeMsg.Debug("SystemEvents_DisplaySettingsChanged");
            Global.Local.ScreenConfig = System.Windows.Forms.Screen.AllScreens;
            Window_LocationChanged();
        }

        /// <summary>
        /// Detect change in Window task in foreground
        /// </summary>
        /// <param name="hWinEventHook"></param>
        /// <param name="eventType"></param>
        /// <param name="hwnd"></param>
        /// <param name="idObject"></param>
        /// <param name="idChild"></param>
        /// <param name="dwEventThread"></param>
        /// <param name="dwmsEventTime"></param>
        private void WinEventProc(IntPtr hWinEventHook, Win32.WinEvent eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            LimeMsg.Debug("WinEventProc");

            // Refresh the taskSwitcher
            if ( hwnd != Handle && WindowState != WindowState.Minimized && (Global.User.ShowTaskSwitcher || Global.User.TaskMatchEnable))
            {
                Browser.TaskSwitcher.Refresh();
            }

            Global.Local.TaskHandle = hwnd;
        }
        private Win32.WinEventDelegate _WinEventProc = null;



        #endregion



        // --------------------------------------------------------------------------------------------------
        #region GUI/Programmatic Callbacks


        /// <summary>
        /// Update all the visible windows Tasks and refresh the display. 
        /// </summary>
        /// <param name="sender">Typically, called by Window_Loaded or Window_Activated</param>
        /// <param name="e"></param>
        private void WindowRefresh(object sender = null, EventArgs e = null)
        {
            if (Commands.Closing) return;

            LimeMsg.Debug("------------------------------------------------------------------------------");
            LimeMsg.Debug("WindowRefresh: Refresh... {0}", sender);

            // Handle window state change
            if (RestoreWindow)
            {
                AnimateAction.Do("wxWindow_show", () =>
                {
                    Global.Local.OnTop = true;
                    WindowState = WindowStateShow;
                    RestoreWindow = false;
                    WindowRefresh();
                });

                return;
            }


			// Get Windows handle and restore window focus (in case it was lost)
			Global.Local.OnTop = true;

            // Restore Settings state
            if (Commands.CfgWindow == null) Topmost = Global.User.TopMost;
            if (Visibility == Visibility.Visible && WindowState != WindowState.Minimized)
            {
                AnimateAction.EnableAnimations = Global.User.EnableAnimations;
            }

            // Restore Control mode display
            RefreshCtrlMode(Global.Local.CtrlMode);

            // Populate the taskSwitcher
            if (Global.User.ShowTaskSwitcher || Global.User.TaskMatchEnable)
            {
                Browser.TaskSwitcher.Refresh();
                Browser.TaskSwitcher.IsPanelVisible = Global.User.ShowTaskSwitcher;
            }

            // Update window properties
            Window_LocationChanged();

            // Refresh the panels
            Browser.Refresh();

            // Done
            LimeMsg.Debug("WindowRefresh: done.");
        }


        /// <summary>
        /// Track location/Size of the window and the current Monitor/DPI
        /// </summary>
        /// <param name="sender">Typically, called by Window LocationChanged event</param>
        /// <param name="e"></param>
        private void Window_LocationChanged(object sender = null, EventArgs e = null)
        {
            System.Drawing.Point wpos;
            Size wsize;
            double dpi;

            if (this.ActualWidth == 0)
            {
                var wcfg = Global.Local.WindowMain;
                wpos = new System.Drawing.Point(wcfg.NormalPosition.Left, wcfg.NormalPosition.Top);
                wsize = new Size(wcfg.NormalPosition.Right - wcfg.NormalPosition.Left, wcfg.NormalPosition.Bottom - wcfg.NormalPosition.Top);
                dpi = 1.0;
            }
            else
            {
                // Retrieve Pixel coordinates
                wpos = WPF.Windows2DrawingPoint(new Point(0, 0));

                // Retrieve Window Size in Pixel
                var source = PresentationSource.FromVisual(this);
                Matrix transformToDevice = source.CompositionTarget.TransformToDevice;
                wsize = (Size)transformToDevice.Transform(new Point(ActualWidth, ActualHeight));

                // Retrieve DPI
                dpi = wsize.Width / this.ActualWidth;
            }

            var wnd = new System.Drawing.Rectangle(wpos.X, wpos.Y, (int)wsize.Width, (int)wsize.Height);

            // Retrieve the *most* current screen (the one containing the center of the window)
            System.Windows.Forms.Screen screen = null;
            var wcenter = new System.Drawing.Point(wpos.X + (int)wsize.Width / 2, wpos.Y + (int)wsize.Height / 2);

            if (Global.Local.ScreenParameter == null)
                Global.Local.ScreenParameter = new SerializableDictionary<string, ScreenParameterType>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var scr in Global.Local.ScreenParameter) scr.Value.Enabled = false;

            foreach (var scr in System.Windows.Forms.Screen.AllScreens)
            {
                if (scr.Bounds.Contains(wcenter)) screen = scr;
                string scname;
                try
                {
                    scname = Path.GetFileName(scr.DeviceName);
                }
                catch
                {
                    scname = scr.DeviceName;
                }

                if (!Global.Local.ScreenParameter.ContainsKey(scname))
                {
                    Global.Local.ScreenParameter.Add(scname, new ScreenParameterType());
                }

                Global.Local.ScreenParameter[scname].Enabled = true;

            }

            // Fallback to area method
            if (screen == null)
            {
                int warea = 0;
                foreach (var scr in System.Windows.Forms.Screen.AllScreens)
                {
                    var irect = scr.Bounds;
                    irect.Intersect(wnd);
                    var sarea = irect.Width * irect.Height;
                    if (sarea > warea)
                    {
                        warea = sarea;
                        screen = scr;
                    }
                }
                LimeMsg.Debug("Window_LocationChanged: Area on screen: {0}", warea);
            }

            // Report and update the Lime Properties
            LimeMsg.Debug("Window_LocationChanged: WindowPosition: {0}, WindowSize: {1}, ScreenDPI: {2}", wpos, wsize, dpi);
            Global.Local.WindowPosition = wpos;
            Global.Local.WindowSize = new System.Drawing.Size((int)wsize.Width, (int)wsize.Height);
            Global.Local.ScreenDPI = dpi;

            // Set current screen (if not off-screen)
            if (screen != null)
            {
                string sname;
                try
                {
                    sname = Path.GetFileName(screen.DeviceName);
                }
                catch
                {
                    sname = screen.DeviceName;
                }

                if (Global.Local.ScreenName != sname)
                {
                    Global.Local.ScreenName = null; // Avoid screen motion
                    Global.Local.ScreenName = sname;
                }
            }
        }

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region GUI Callbacks

		/// <summary>
		/// Window got shown the first time
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LimeMsg.Debug("Window_Loaded: WindowState {0}, {1}, RestoreWindow: {2}", WindowState, Visibility, RestoreWindow);

            // Restore Local window settings
            var wcfg = Global.Local.WindowMain;
            if (wcfg.Length > 0)
            {
                WPF.SetWindowPlacement(Handle, wcfg, this);
            }

        }


        /// <summary>
        /// Update the Application WindowState
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            LimeMsg.Debug("Window_StateChanged: {0}, {1}, RestoreWindow: {2}", WindowState, Visibility, RestoreWindow);
            RenderZoom(false);
            if (!RestoreWindow) Global.Local.WindowState = WindowState;
        }

		/// <summary>
		/// Update the TaskSwitcher when the focus gets to another window
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Deactivated(object sender, EventArgs e)
        {
            LimeMsg.Debug("Window_Deactivated");
            RenderZoom(false);
            Global.Local.OnTop = false;
            if (!Global.User.HideOnLaunch && Global.User.ShowTaskSwitcher)
            {
                Browser.TaskSwitcher.Refresh();
            }
        }


        /// <summary>
        /// Intercept the closing event to hide window instead of closing application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            LimeMsg.Debug("Window_Closing");

            if (Global.User.ShowWindowBorders || Global.User.ShowInTaskbar)
            {
                Commands.Exit.Execute();
            }
            else
            {
                Hide();
            }
            e.Cancel = true;
        }


		/// <summary>
		/// Update geometries
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_SizeChanged(object sender = null, SizeChangedEventArgs e = null)
		{
			LimeMsg.Debug("Window_SizeChanged: {0}", wxInfoPaneColumn.ActualWidth);
			AdjustInfoPane();
		}


		/// <summary>
		/// Update the MainPaneColumns property according to the splitter position
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void wxSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			LimeMsg.Debug("wxSplitter_DragCompleted: columns {0}", Geometry.ColumnCount);
			AdjustInfoPane(save: true);
			Refresh();
		}


		/// <summary>
		/// Detect mouse activity on context menu to restore mouse-mode (usefull for NotifyIcon menu!)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Menu_MouseEnter(object sender, MouseEventArgs e)
        {
            LimeMsg.Debug("Menu_MouseEnter");
            Global.Local.CtrlMode = CtrlMode.Mouse;
        }


        /// <summary>
        /// Handle Window drag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Window_MouseLeftButtonDown(object sender = null, MouseButtonEventArgs e = null)
        {
            if (e != null && e.ClickCount == 2)
            {
                trackMove.enable = false;
                if (Global.User.DoubleClickFullScreen)
                {
                    var coord = Mouse.GetPosition(this);
                    HitTestResult result = VisualTreeHelper.HitTest(this, coord);
                    if (result != null 
                        && (WPF.FindFirstParent<ButtonBase>(result.VisualHit) != null 
						|| WPF.FindFirstParent<LimeControl>(result.VisualHit) != null
						|| WPF.FindFirstParent<Border>(result.VisualHit) == wxNotifierBorder))
                    {
                        LimeMsg.Debug("Window_MouseLeftButtonDown: Toggle Fullscreen Cancelled: over Button");
                    }
                    else
                    {
                        LimeMsg.Debug("Window_MouseLeftButtonDown: Toggle Fullscreen");
                        Commands.Maximize.Toggle();
                    }
                }
            }
            else if (sender!=null && WindowState != WindowState.Normal)
            {
                if (!trackMove.enable)
                {
                    trackMove.origin = Mouse.GetPosition(this);
                    trackMove.action = TrackMove.Action.None;
                    trackMove.enable = true;
                    LimeMsg.Debug("Window_MouseLeftButtonDown: trackMove {0}", trackMove.action.ToString());
                }
            }
            else if(!Global.User.ShowWindowBorders && !ClipDragDrop.MayDrag)
            {
				try
				{
					DragMove();
				}
				catch { }
            }
        }

        /// <summary>
        /// Render resize-pointer on window-border
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxWinBorder_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Global.Local.CtrlMode == CtrlMode.Mouse)
            {
                if ((bool)e.NewValue)
                    MouseOverWinBorder();
                else
                    Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Start resize on window-border or start window-drag (full-screen)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxWinBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WPF.ResizeDirection direction = MouseOverWinBorder();
            if (direction != WPF.ResizeDirection.None)
            {
                if (e.ClickCount == 2 && direction == WPF.ResizeDirection.Top)
                {
                    if (WindowState != WindowState.Maximized)
                    {
                        // Prepare for full-screen (double-click)
                        trackMove.origin = Mouse.GetPosition(this);
                        trackMove.action = TrackMove.Action.FullScreen;
                        trackMove.enable = true;
                        LimeMsg.Debug("wxWinBorder_PreviewMouseLeftButtonDown: trackMove {0}", trackMove.action.ToString());
                        e.Handled = false;
                    }
                }
                else if (WindowState == WindowState.Maximized)
                {
                    // Prepare to drag-window (if drag) or to restore window (if click)  
                    trackMove.origin = Mouse.GetPosition(this);
                    trackMove.action = TrackMove.Action.Restore;
                    trackMove.enable = true;
                    LimeMsg.Debug("wxWinBorder_PreviewMouseLeftButtonDown: trackMove {0}", trackMove.action.ToString());
                    e.Handled = false;
                }
                else
                {
                    LimeMsg.Debug("wxWinBorder_PreviewMouseLeftButtonDown: Resize");
                    WPF.WindowResize(wxWinBorder, direction);
                    e.Handled = true;
                }
            }
        }
		
		#endregion

	}
}
