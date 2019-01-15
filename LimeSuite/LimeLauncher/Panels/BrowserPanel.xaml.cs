/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     18-03-2018
* Copyright :   Sebastien Mouy © 2018 
**************************************************************************/

using Lime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using WPFhelper;

namespace LimeLauncher
{
    /// <summary>
    /// Main panel containing the browseable items (directory/forlders)
    /// </summary>
    public partial class BrowserPanel : UserControl
    {
        // --------------------------------------------------------------------------------------------------
        #region Types

        /// <summary>
        /// Store the parameters relative to a LimeItem panel. 
        /// </summary>
        private class PanelParameters
        {
            public LimeItem focus = null;
            public double scroll = 0.0;
            public int currentPanel = 0;
            public int currentItem = 0;

            public PanelParameters()
            { }
        }


        /// <summary>
        /// Handle an implicit search while typing directly in a control.
        /// </summary>
        private class ImplicitFind
        {
            /// <summary>
            /// Timeout in milliseconds
            /// </summary>
            public double timeout = 1000;

            /// <summary>
            /// Text to search for
            /// </summary>
            public string text;

            /// <summary>
            /// Previous control wherin the search has been applied. 
            /// </summary>
            private object sender;

            /// <summary>
            /// Previous time when the search has been applied.
            /// </summary>
            private DateTime time;

            /// <summary>
            /// Initialize an implicit find.
            /// </summary>
            public ImplicitFind()
            {
                text = "";
                sender = null;
                time = DateTime.Now;
            }

            /// <summary>
            /// Add a character to the search pattern
            /// </summary>
            /// <param name="sender">object where the search is applied</param>
            /// <param name="keyChar">new character to be added to the implicit search text</param>
            /// <returns>true if new search pattern initiated, false if continue (incremental) search pattern</returns>
            public bool AddChar(object sender, char keyChar)
            {
                bool ret = false;

                if (DateTime.Now > time.AddMilliseconds(timeout) || sender != this.sender)
                {
                    this.text = "";
                    this.sender = sender;
                    ret = true;
                }

                this.time = DateTime.Now;
                text += keyChar;

                return ret;
            }
        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Attributes/Properties

        /// <summary>
        /// LimeItem representign the Task-switcher
        /// </summary> 
        public LimeItem TaskSwitcher { get; private set; }

        // Handle panels (Main data-binding and history navigation to panels)
        private ObservableCollection<LimeItem> PanItems;
        private List<PanelParameters> Pan;
        private int PrevPanIdx;

        // Focus handling
        private LimeItem Refocus;
        private ButtonBase wxButtonFocus;

        // Timers
        private ImplicitFind implicitFind;
		private DispatcherTimer AutoSelectionTimer = null;

		/// <summary>
		/// Define the index of the first panel
		/// </summary>
		public int PanMin { get; private set; }

        /// <summary>
        /// Define the index of the last panel
        /// </summary>
        public int PanMax { get { return PanItems.Count - 1; } }

        /// <summary>
        /// Get the Lime Item represented by the current panel
        /// </summary>
        public LimeItem ItemPanel { get { return PanItems[PanIdx]; } }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        public BrowserPanel()
        {
            // Initialize Implict Find feature
            implicitFind = new ImplicitFind();

			// Initialize Auto Selection
			AutoSelectionTimer = new DispatcherTimer();
			AutoSelectionTimer.Tick += AutoSelection_Callback;

			// Initialize Task-switcher, having same attributes as the Lime root
			TaskSwitcher = LimeItem.TaskSwitcher(LimeLanguage.Translate("Translator", "TaskSwitcher", "Task Switcher"));
            TaskSwitcher.Tree = Global.Root.Tree;

            // Initialize Panels
            PrevPanIdx = -1;
            PanMin = 1; // 0 if no TaskSwitcher
            _PanIdx = PanMin;
            PanItems = new ObservableCollection<LimeItem>() { TaskSwitcher, Global.Root };
            Refocus = null;
            Pan = new List<PanelParameters>();
            wxButtonFocus = null;

            // Panel initialization
            InitializeComponent();
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Methods (and Properties)

		/// <summary>
		/// Load the BrowserPanel content
		/// </summary>
        public void Load()
        {
            LimeMsg.Debug("BrowserPanel Load");

            // LimeItem node to be represented in this Panel
            var node = Global.Root;

            // Start GUI
            node.IsPanelVisible = true;
            wxRoot.DataContext = PanItems;

            // Enable Directory Watch (UIElement is used for Watch thread-Safety)
            node.Tree.UIElement = this;
            node.Tree.ChildrenChanged += OnChangeInPanelContent;
            node.Watch();

            // Preload directory content
            node.Refresh();
            node.MetadataLoad();

            // Preload Icons
            if (node.Directory && node.Children != null)
            {
                LimeMsg.Debug("BrowserPanel Load: Preload");
                foreach (var item in node.Children)
                {
                    item.LoadAsync();
                }
            }
        }



        /// <summary>
        /// Get or set the current panel index. Modifying the value changes the visible (current) panel.
        /// </summary>
        public int PanIdx
        {
            set { ChangePanIdx(value); }
            get { return _PanIdx; }
        }
        private int _PanIdx;

        /// <summary>
        /// Change/navigate the panel
        /// </summary>
        /// <param name="newIdx">Index of the panel to go to</param>
        /// <param name="open">LimeItem directory to be opened (default: just navigate)</param>
        private void ChangePanIdx(int newIdx, LimeItem open = null)
        {
            if (newIdx != _PanIdx && newIdx >= PanMin && (newIdx <= PanMax || open != null))
            {
                if (open != null)
                {
                    LimeMsg.Debug("------------------------------------------------------------------------------");
                    LimeMsg.Debug("ChangePanIdx: Idx {0} --> {1} Open: {2}", _PanIdx, newIdx, open.Name);
                    if (!open.Open())
                    {
                        LimeMsg.Debug("ChangePanIdx: failed to open.");
                        return;
                    }
                }
                else
                {
                    LimeMsg.Debug("ChangePanIdx: Idx {0} --> {1} (count: {2})", _PanIdx, newIdx, PanItems.Count);
                }

                HideTasks();

                // Don't change focus if the focus in on a navigation button which won't disappear
                bool NoRefocus = wxBack.IsFocused && newIdx > PanMin
                              || wxNext.IsFocused && newIdx < PanMax;

                string sbName = newIdx >= _PanIdx ? "wxPanelNext" : "wxPanelPrev";

                AnimateAction.Do(sbName + "_Enter", () =>
                {
                    if (open != null)
                    {
                        // Re-arrange history and add the open node (directory) in it
                        if (!(newIdx < PanItems.Count && PanItems[newIdx] == open))
                        {
                            // Remove *future* panels from the history
                            if (PanItems.Count > newIdx && PanItems[newIdx] != open)
                            {
                                while (PanItems.Count > newIdx)
                                {
                                    int idx = PanItems.Count - 1;
                                    PanItems[idx].Clear();
                                    PanItems.RemoveAt(idx);
                                }
                                Pan.RemoveRange(newIdx, Pan.Count - newIdx);
                            }

                            // Add/modify new panel to history
                            if (PanItems.Count <= newIdx)
                            {
                                PanItems.Add(open);
								PanelParameters child = new PanelParameters
								{
									focus = open.Children != null && open.Children.Count > 0 ? open.Children[0] : null
								};
								open.Watch();
                                Pan.Add(child); // select first child-item
                            }
                        }

                    }


                    // Close current panels
                    PanItems[_PanIdx].IsPanelVisible = false;
                    var tvisible = TaskSwitcher.IsPanelVisible;
                    TaskSwitcher.IsPanelVisible = false;

                    // Adjust panels
                    _PanIdx = newIdx; // Must be done before the visible panel is restored (callback)
                    if (!NoRefocus)
                    {
                        Refocus = Pan[newIdx].focus;
                        if (Refocus == null) Refocus = TaskSwitcher; // trigger fallback focus
                    }

                    // Open panels
                    TaskSwitcher.IsPanelVisible = tvisible;
                    PanItems[_PanIdx].IsPanelVisible = true;

                    // Try to restore the scroll position
                    wxScroll.ScrollToVerticalOffset(Pan[_PanIdx].scroll);

                    // Force Prev/Next Button to activate
                    CommandManager.InvalidateRequerySuggested();

                    LimeMsg.Debug("ChangePanIdx: done: {0}", _PanIdx);

                }, sbName + "_Leave", Refresh);

            }
        }


        /// <summary>
        /// Get the LimeItem that has focus (strict).
        /// </summary>
        public LimeItem GetFocus
        {
            get
            {
                return PanIdx < Pan.Count && wxButtonFocus != null ? Global.Local.FocusItem : null;
            }
        }



        /// <summary>
        /// Request to refresh the GUI of the Browser panel
        /// </summary>
        public void Refresh()
        {
            LimeMsg.Debug("------------------------------------------------------------------------------");
            LimeMsg.Debug("Refresh");

            PrevPanIdx = -1; // force refresh

            UpdatePanels();

            // Update the enabled/disabled states of commands
            CommandManager.InvalidateRequerySuggested();
        }




        /// <summary>
        /// Hide tasks on the GUI in order to avoid Thumbnail problems when animating
        /// </summary>
        public void HideTasks()
        {
            foreach (LimeItem pan in PanItems)
            {
                if (pan.IsPanelVisible && pan.Children != null)
                {
                    foreach (var node in pan.Children)
                    {
                        node.IsTaskThumbVisible = false;
                    }
                }
            }
        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Callbacks


        /// <summary>
        /// Update all the visible panels rendering, handle (manual) virtualization and schedule background task to load icons.
        /// </summary>
        /// <param name="sender">Typically, ScrollViewer object is the caller</param>
        /// <param name="e"></param>
        private void UpdatePanels(object sender = null, ScrollChangedEventArgs e = null)
        {
            if (AnimateAction.IsAnimated)
            {
                LimeMsg.Debug("UpdatePanels: Cancelled because animated");
                return;
            }

            if (Commands.MainWindow.Zooming)
            {
                LimeMsg.Debug("UpdatePanels: Cancelled because Zooming");
                return;
            }

			if ( e != null && PanIdx < Pan.Count && 
				Math.Abs(e.VerticalOffset - Pan[PanIdx].scroll) < Commands.MainWindow.Geometry.IconWidth )
			{
				LimeMsg.Debug("UpdatePanels: Cancelled because small offset: {0} - {1}", 
					e.VerticalOffset, Pan[PanIdx].scroll);
				return;
			}

			// Cancel previous Item Loading (not only on Root, but on all the tree)
				Global.Root.Cancel();

            // Set Selection
            if (!Global.Local.ConfigVisible || Global.User.ConfigWindow)
            {
                if (Global.Local.InfoEditMode && Global.Local.SelectedItem == null)
                {
                    Global.Local.SelectedItem = ItemPanel;
                }
            }

            // Get current focus
            var wxfocus = Keyboard.FocusedElement as FrameworkElement;
            LimeMsg.Debug("UpdatePanels: Start: Focused: {0} --> {1}, Refocus: {2}", wxfocus, wxfocus?.Name, Refocus);

            // Set refocus
            LimeItem refocus = null;
            FrameworkElement focusFallbackUi = null;
            LimeItem focusFallbackNode = null;
            if (wxfocus == null || wxfocus == this || !wxfocus.IsEnabled || Refocus != null)
            {
                if (this.Refocus != null)
                    refocus = this.Refocus;
                else if (Global.Local.SelectedItem != null)
                    refocus = Global.Local.SelectedItem;
                else if (Global.Local.FocusItem != null)
                    refocus = Global.Local.FocusItem;
                else if (PanIdx < Pan.Count && Pan[PanIdx].focus != null)
                    refocus = Pan[PanIdx].focus;
                else if (PanIdx < PanItems.Count && PanItems[PanIdx].Children != null && PanItems[PanIdx].Children.Count > 0)
                    refocus = PanItems[PanIdx].Children[0];

                this.Refocus = null;

                LimeMsg.Debug("UpdatePanels: refocus: {0} *****", refocus?.Name);

                // Define focus second choice
                if (PanIdx + 1 < PanItems.Count)
                {
                    focusFallbackNode = PanItems[PanIdx + 1];
                }


				// Auto Selection
				if (refocus == null)
				{
					LimeMsg.Debug("UpdatePanels: refocus: failed");
				}
				else if ((Global.Local.CtrlMode & Global.User.AutoSelection) != 0 && !Global.Local.InfoEditMode)
				{
					AutoSelectionTimer.Interval = TimeSpan.FromMilliseconds(Global.User.AutoSelectionAfter);
					AutoSelectionTimer.Start();
				}

            }

            // Set focus
            LimeItem focus = refocus;
            if (focus == null && PanIdx < Pan.Count) focus = Pan[PanIdx].focus;

            // Detect current focus
            FrameworkElement focusCurrentUi = null;

            // Adjust current Selection
            var selectedItem = Global.Local.SelectedItem;

            // Create priority queues
            var queue = new List<LimeItem>(100);
            var before = new List<LimeItem>(100);
            var after = new List<LimeItem>(100);

            // Handle geometry
            bool enableRescroll = wxScroll != null
                && sender == wxScroll
                && Mouse.LeftButton == MouseButtonState.Released
                && Global.Local.CtrlMode != CtrlMode.Mouse;
            double rescroll = -1.0; // negative means disabled
            double rescrollDown = -1.0; // negative means disabled
            double buttonHeight = 0;
            double buttonWidth = 0;
            int columnCount = 0;

            bool detectDirectoryTypeByExt = PrevPanIdx == PanIdx || Global.Local.ShowInfoPane;

            int panelIdx = -1;
            foreach (LimeItem item in PanItems)
            {
                panelIdx++;
                if (panelIdx >= Pan.Count) Pan.Add(new PanelParameters());

                if (item.IsPanelVisible)
                {
                    // Retrieve instances of panel representing the item
                    var wxPanel = WPF.FindFirstChild<StackPanel>(wxRoot, (wx) => wx.DataContext == item);
                    var wxLimeItems = WPF.FindFirstChild<ItemsControl>(wxPanel, (wx) => wx.DataContext == item);

                    if (wxLimeItems != null)
                    {
                        int panelColumnCount = 0;
                        double panelMaxBound = -1.0;
                        int last = wxLimeItems.Items.Count - 1;
                        LimeMsg.Debug("UpdatePanels: refocus {1}, {2} elements : {0}", item.Name, refocus != null, last + 1);

                        // Load icon panel in priority
                        if (!item.IconValidated) queue.Insert(0, item);

                        for (int i = 0; i <= last; i++)
                        {
                            // Get node and GUI element
                            var uiElement = (FrameworkElement)wxLimeItems.ItemContainerGenerator.ContainerFromIndex(i);

                            if (uiElement == null)
                            {
                                LimeMsg.Debug("UpdatePanels: Aborted: null UI reference item: {0} idx {1}", item.Name, i);
                                return;
                            }
                            var node = (LimeItem)uiElement.DataContext;

                            // Info Pane handling
                            if (!detectDirectoryTypeByExt
                                && Global.User.ShowInfoPaneAuto == ConfigUser.ShowInfoPaneAutoType.WhenDirectoryContainsMedia
                                && (node.MediaType & Global.User.ShowInfoPaneMediaTypes) != 0)
                            {
                                detectDirectoryTypeByExt = true;
                            }


                            // Handle geometry
                            var wxButton = WPF.FindFirstChild<ToggleButton>(uiElement);
                            if (wxButton != null) // null can happen when restoring the window focus
                            {
                                if (wxButton.IsFocused)
                                {
                                    LimeMsg.Debug("UpdatePanels: Detected Focused Button: {0}", node.Name);
                                    focusCurrentUi = wxButton;
                                }

                                // Adjust selection
                                var selected = selectedItem == node;
                                if (selected != wxButton.IsChecked)
                                {
                                    LimeMsg.Debug("UpdatePanels: Fix Selected Button: {0}", node.Name);
                                    wxButton.IsChecked = selected;
                                }


                                Point coord = wxButton.TransformToAncestor(wxScroll).Transform(new Point(0, 0));
                                if (i == 0)
                                {

                                    buttonHeight = wxButton.ActualHeight;
                                    buttonWidth = wxButton.ActualWidth + wxButton.Margin.Left + wxButton.Margin.Right;
                                    var wxGrid = WPF.FindFirstChild<Grid>(wxButton);
                                    if (wxGrid != null) Global.Local.Skin.IconButtonWidth = wxGrid.ActualWidth;

                                    if (rescrollDown > -0.5)
                                    {
                                        double offset = coord.Y - rescrollDown;
                                        if (offset < wxScroll.ViewportHeight - wxButton.ActualHeight)
                                        {
                                            rescroll = e.VerticalOffset + offset;
                                            LimeMsg.Debug("UpdatePanels: SnapScroll Down {0} + {1} - {2} = {3}", e.VerticalOffset, coord.Y, rescrollDown, rescroll);
                                        }
                                        rescrollDown = -1.0;
                                    }
                                    else if (enableRescroll && coord.Y > -0.5 && coord.Y < 0.5 && e.VerticalChange < -0.5)
                                    {
                                        // Scroll Offset alignment on scroll event (ScollViewer callback only)  
                                        Point panCoord = wxPanel.TransformToAncestor(wxScroll).Transform(new Point(0, 0));
                                        double offset = -panCoord.Y;
                                        if (offset < wxScroll.ViewportHeight - wxButton.ActualHeight)
                                        {
                                            rescroll = e.VerticalOffset + panCoord.Y;
                                            LimeMsg.Debug("UpdatePanels: SnapScroll Up {0} : {1} + {2} = {3}", e.VerticalChange, e.VerticalOffset, panCoord.Y, rescroll);
                                        }
                                    }

                                }

                                if (enableRescroll && i == last)
                                {
                                    double borderButton = coord.Y + wxButton.ActualHeight;
                                    double borderScroll = wxScroll.ViewportHeight;
                                    double borderDistance = borderScroll - borderButton;
                                    if (enableRescroll && borderDistance > -0.5 && borderDistance < 0.5 && e.VerticalChange > 0.5)
                                    {
                                        rescrollDown = borderButton;
                                    }
                                    //LimeMsg.Debug(String.Format("UpdatePanels: SnapScrollDown {0} :  {1} - {2} = {3} --> {4} ", e.VerticalChange, borderButton, borderScroll, borderDistance,  recrollDown));
                                }

                                if (coord.X > panelMaxBound)
                                {
                                    panelColumnCount++;
                                    panelMaxBound = coord.X;
                                }


								// Handle icon
								var visible = WPF.IsFullyOrPartiallyVisible(uiElement, wxScroll);
								if (visible)
                                {
                                    //LimeMsg.Debug("UpdatePanels: Set {0} visible", node.name);

                                    if (!item.Task) node.TaskMatcher(TaskSwitcher, Global.User.TaskMatchEnable);

                                    if (!node.IconValidated)
                                    {
                                        if (node == focus)
                                        {
                                            queue.Insert(0, node);
                                        }
                                        else
                                        {
                                            queue.Add(node);
                                        }
                                    }
                                }
                                else if (node != null && !node.IconValidated)
                                {
                                    // Set priority to the item up/down the visible zone
                                    if (queue.Count > 0)
                                    {
                                        after.Add(node);
                                        if (before.Count > 0)
                                        {
                                            after.Add(before[before.Count - 1]);
                                            before.RemoveAt(before.Count - 1);
                                        }
                                    }
                                    else
                                    {
                                        before.Add(node);
                                    }
                                }


                                // Handle focus
                                if (node == refocus)
                                {
                                    var button = WPF.FindFirstChild<ToggleButton>(uiElement);
                                    if (button != null)
                                    {
                                        LimeMsg.Debug("UpdatePanels: SetFocus: {0}: {1}", i, node.Name);
                                        Keyboard.Focus(button);
                                        refocus = null; // focus found
                                    }

                                }
                                else if (node == focusFallbackNode || focusFallbackUi == null)
                                {
                                    focusFallbackUi = uiElement;
                                }
								else if (wxButton.IsChecked != true && Global.Local.FocusItem != node && node.Metadata != null)
								{
									// Save memory on unselected and not visible items
									node.MetadataUnload();
								}
                            }
                            else if (node != null)
                            {
                                LimeMsg.Debug("UpdatePanels: loading: {0} ", node.Name);
                                after.Add(node);
                            }

                        }

                        // Finalize geometry
                        if (panelColumnCount > columnCount)
                        {
                            columnCount = panelColumnCount;
                        }
                    }
                }

            }

            // Panel Geometry
            Commands.MainWindow.Geometry.ColumnCount = columnCount;
            Commands.MainWindow.Geometry.IconWidth = buttonWidth;
            LimeMsg.Debug("UpdatePanels: Geometry: columns: {1}, button: {0}", buttonWidth, columnCount);
            Commands.MainWindow.AdjustInfoPane();

            // Handle background queue priorities
            before.Reverse();
            foreach (var item in queue) item.LoadAsync();
            foreach (var item in after) item.LoadAsync();
            foreach (var item in before) item.LoadAsync();


            // Handle focus
            if (refocus != null)
            {
                // Prefered focus not found: apply second choice focus
                if (focusFallbackUi != null)
                {
                    var button = WPF.FindFirstChild<ToggleButton>(focusFallbackUi);
                    if (button != null)
                    {
                        LimeMsg.Debug("UpdatePanels: SetFocus: Fallback");
                        Keyboard.Focus(button);
                        refocus = null; // focus found
                    }
                }

            }

            // Apply rescroll
            if (enableRescroll && e.VerticalChange > 1.0 && e.VerticalOffset < wxScroll.ScrollableHeight && e.VerticalOffset + buttonHeight > wxScroll.ScrollableHeight)
            {
                LimeMsg.Debug("UpdatePanels: SnapBottom : {0} {1}", e.VerticalOffset, wxScroll.ScrollableHeight);
                rescroll = wxScroll.ScrollableHeight;

            }
            if (rescroll > -0.5)
            {
                LimeMsg.Debug("UpdatePanels: rescroll : {0}", rescroll);
                wxScroll.ScrollToVerticalOffset(rescroll);
            }

            // Save scroll index
            if (enableRescroll && Pan.Count > PanIdx)
            {
                Pan[PanIdx].scroll = wxScroll.VerticalOffset;
            }


            // Info Pane handling
            if (Global.Local.ShowInfoPane && !Global.Local.InfoPaneVisible
                || Global.Local.InfoEditMode
                || Global.Local.ConfigVisible && !Global.User.ConfigWindow)
            {
                Global.Local.InfoPaneVisible = true;
            }
            else if (PrevPanIdx != PanIdx && !Global.Local.ShowInfoPane)
            {
                Global.Local.InfoPaneVisible = detectDirectoryTypeByExt;
            }

            // Concludes the first successful update of this pan
            PrevPanIdx = PanIdx;
        }

        /// <summary>
        /// Delegate function to be called back when the panel-content has changed and the panels should be refreshed
        /// </summary>
        private void OnChangeInPanelContent(LimeItem parent, LimeItem item, int index)
        {
            // Check if item is still used, and if it is visible
            if (!PanItems.Contains(parent) || !parent.IsPanelVisible || parent.Children == null)
            {
                LimeMsg.Debug("OnChangeInPanelContent: skipped");
                return;
            }

            LimeMsg.Debug("OnChangeInPanelContent: {0}, {1}, {2}", parent.Name, item?.Name, index);

            if (index == parent.Children.Count) index--;
            if (index>=0 && index < parent.Children.Count)
            {
				var itm = parent.Children[index];
				itm.LoadAsync();
				if (Keyboard.FocusedElement == null) Refocus = itm;

			}
        }


        /// <summary>
        /// Enable focus to get on the side-buttons (Back/Forward) by using key left/right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxRoot_KeyDown(object sender, KeyEventArgs e)
        {
            LimeMsg.Debug("wxRoot_KeyDown");
			if (Keyboard.Modifiers != 0) return;

			// Panel Handling
			if (PanIdx >= PanMin && PanIdx < PanItems.Count && PanItems[PanIdx] != null)
            {
                if (e.Key == Key.Left || e.Key == Key.Right)
                {
                    Global.Local.CtrlMode = CtrlMode.Keyboard;

					// Retrieve Panel and focused element
                    var focus = Keyboard.FocusedElement as UIElement;
					var panel = WPF.FindFirstParent<ItemsControl>(focus);
					var current = focus.TransformToAncestor(panel).Transform(new Point(0, 0));

					// Retrieve index of the selection
					var idx = Pan[PanIdx].currentItem;
					LimeMsg.Debug("wxRoot_KeyDown: PanIdx: {0}  idx: {1} / {2}", PanIdx, idx, panel.Items.Count);


					if (e.Key == Key.Left)
					{
						var prev = current;
						if (idx > 0)
						{
							var wx = WPF.FindFirstChild<FrameworkElement>(panel, (w) => w.DataContext == panel.Items[idx - 1]);
							prev = wx.TransformToAncestor(panel).Transform(new Point(0, 0));
						}

						if (current.X <= prev.X + 1.0) // Left boundary
						{
							LimeMsg.Debug("wxRoot_KeyDown: Left");
							Keyboard.Focus(wxBack);
							e.Handled = true;
						}
					}
					else if (e.Key == Key.Right)
					{
						var next = current;
						if (idx < panel.Items.Count - 1)
						{
							var wx = WPF.FindFirstChild<FrameworkElement>(panel, (w) => w.DataContext == panel.Items[idx + 1]);
							next = wx.TransformToAncestor(panel).Transform(new Point(0, 0));
						}

						if (current.X >= next.X - 1.0) // right boundary
						{
							LimeMsg.Debug("wxRoot_KeyDown: Right");

							if (wxNext.IsEnabled || wxToolBar.IsEnabled)
							{
								if (!wxNext.IsEnabled)
								{
									wxToolBar.SetFocus(Global.Properties[nameof(Global.Local.ConfigVisible)]);
								}
								else if (!wxToolBar.IsEnabled)
								{
									Keyboard.Focus(wxNext);
								}
								else
								{
									// Find which control is vertically the closest
									var curgPoint = focus.TransformToAncestor(wxToolGrid).Transform(new Point(0, 0));
									var toolBarPoint = wxToolBar.TransformToAncestor(wxToolGrid).Transform(new Point(0, 0));
									var nextPoint = wxNext.TransformToAncestor(wxToolGrid).Transform(new Point(0, 0));
									double limit = toolBarPoint.Y + (nextPoint.Y - toolBarPoint.Y) / 2.0;
									if (curgPoint.Y < limit)
									{
										wxToolBar.SetFocus(Global.Properties[nameof(Global.Local.ConfigVisible)]);
									}
									else
									{
										Keyboard.Focus(wxNext);
									}
								}
							}
							else if (Global.Local.InfoEditMode || Global.Local.ConfigVisible && !Global.User.ConfigWindow)
							{
								TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
								var enabled = wxNext.IsEnabled;
								wxNext.IsEnabled = true;
								wxNext.MoveFocus(tRequest);
								wxNext.IsEnabled = enabled;
							}

							e.Handled = true;
						}
					}

				}
				else if (e.Key == Key.Up || e.Key == Key.Down)
                {
                    Global.Local.CtrlMode = CtrlMode.Keyboard;

                }
                else if (e.Key == Key.Tab)
                {
                    e.Handled = true;
                    var wxobj = Keyboard.Modifiers == ModifierKeys.Shift ? wxBack : wxNext;
                    if (wxobj.IsEnabled)
                    {
                        Keyboard.Focus(wxobj);
                    }
                    else if (Global.Local.InfoEditMode || Global.Local.ConfigVisible && !Global.User.ConfigWindow)
                    {
                        var direction = Keyboard.Modifiers == ModifierKeys.Shift ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
                        TraversalRequest tRequest = new TraversalRequest(direction);
                        var enabled = wxobj.IsEnabled;
                        wxobj.IsEnabled = true;
                        wxobj.MoveFocus(tRequest);
                        wxobj.IsEnabled = enabled;

                    }
                }
            }
        }


        /// <summary>
        /// Update the panel whenever a new panel is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LimeMsg.Debug("WxPanelLoaded");
            UpdatePanels();
        }


        /// <summary>
        /// Update the panels everytime there is a change in the scroll-panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxScroll_LayoutUpdated(object sender, EventArgs e)
        {
			var wxobj = WPF.FindFirstChild<ScrollBar>(wxScroll, (wx) => { return wx.Orientation == Orientation.Vertical; });
			var width = wxobj != null && wxobj.IsVisible ? wxobj.Width : 0;
			if (width != wxColumnScroll.Width.Value)
			{
				wxColumnScroll.Width = new GridLength(width);
			}
			if ( !double.IsNaN(wxNext.ActualHeight) )
				wxRowScrollTop.MinHeight = (wxScroll.ActualHeight - wxNext.ActualHeight) / 2;

			if (Refocus != null)
            {
                LimeMsg.Debug("wxScrollLayoutUpdated: refocus True");
                UpdatePanels();
            }
        }


        /// <summary>
        /// Handle Mouse over Auto-focus and tooltips 
        /// </summary>
        /// <param name="sender">Any Control</param>
        /// <param name="e"></param>
        public void Button_MouseEnter(object sender, MouseEventArgs e = null)
        {
			if (!(sender is ButtonBase wxobj)) return;

			if (Global.Local.OnTop && wxobj.IsEnabled)
            {

				if (wxobj.DataContext is LimeItem item)
				{
					if (Global.Local.CtrlMode == CtrlMode.Mouse)
					{
						AutoSelectionTimer.Stop();

						// Updated Mouse-Overed states
						if (Global.Local.InfoEditMode && wxButtonFocus == null)
						{
							LimeMsg.Debug("Button_MouseEnter: Edit");
							// When no LimeItem is focused yet, avoid to steal focus, but decorate the item anyway
							VisualStateManager.GoToState(wxobj, "Focused", true);
						}
						else
						{
							LimeMsg.Debug("Button_MouseEnter: Force focus");
							Keyboard.Focus(wxobj);
						}
					}
					else
					{
						LimeMsg.Debug("Button_MouseEnter");
						// Avoid ToolTip to show up
						if (item != null && wxobj.DataContext != null)
						{
							wxobj.ToolTip = null;
						}
					}
				}
			}
        }


        /// <summary>
        /// Handle Mouse over Auto-focus and tooltips
        /// </summary>
        /// <param name="sender">Any Control</param>
        /// <param name="e"></param>
        public void Button_MouseLeave(object sender, MouseEventArgs e = null)
        {
			if (!(sender is ButtonBase wxobj)) return;

			if (Global.Local.OnTop && wxobj.IsEnabled)
            {
                LimeMsg.Debug("Button_MouseLeave");

				if (Global.Local.CtrlMode == CtrlMode.Mouse)
				{
					AutoSelectionTimer.Stop();
				}

				var wxToggle = wxobj as ToggleButton;
                VisualStateManager.GoToState(wxobj, wxToggle == null || wxToggle.IsChecked == true ? "Checked" : "Normal", true);

                // Restore ToolTip if it has been disabled by Button_MouseEnter
                if (wxobj.ToolTip == null && wxobj.DataContext != null && wxobj.DataContext is LimeItem)
                {
                    wxobj.ToolTip = ((LimeItem)wxobj.DataContext).Tooltip;
                }
            }

        }


        /// <summary>
        /// Handle the Implicit Find
        /// </summary>
        /// <param name="sender">Panel representing a LimeItem directory</param>
        /// <param name="e"></param>
        private void wxPanel_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var wxObj = sender as StackPanel;

            char c = WPF.GetCharFromKey(e.Key);
            if (e.Handled = char.IsLetterOrDigit(c) || char.IsPunctuation(c) || c == ' ')
            {
                bool isNew = implicitFind.AddChar(sender, c);

                e.Handled = !isNew || c != ' ';

                if (e.Handled)
                {
                    LimeMsg.Debug("wxPanel_PreviewKeyDown : '{0}'", implicitFind.text);

                    // find first match
                    var items = wxObj.DataContext as LimeItem;
                    if (items.Children != null)
                    {
                        foreach (var item in items.Children)
                        {
                            if (item.Name.StartsWith(implicitFind.text, true, null))
                            {
                                // match found: convert it to its button
                                var match = WPF.FindFirstChild<ToggleButton>(wxObj, (wx) => wx.DataContext == item);
                                if (match != null)
                                {
                                    LimeMsg.Debug("wxPanel_KeyDown : found {0}", item.Name);
                                    Keyboard.Focus(match);
                                }
                                break;
                            }

                        }
                    }
                }
            }
        }


        /// <summary>
        /// Handle the Left/right/up/down keys when focused is on side-buttons (Back/Forward)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxBackNext_KeyDown(object sender, KeyEventArgs e)
        {
            LimeMsg.Debug("wxBackNext_KeyDown");
			if (Keyboard.Modifiers != 0) return;

			if (AnimateAction.IsAnimated)
            {
                e.Handled = true;
            }
            else
            {
                switch (e.Key)
                {
					case Key.Up:
						{
							// Do nothing for wxBack
							e.Handled = sender == wxBack;
						}
						break;

					case Key.Down:
						{
							// Do nothing
							e.Handled = true;
						}
						break;


					case Key.Left:
                        {
                            if (sender == wxBack)
                            {
								if (Global.User.KeysToNavigateEnable)
								{
									Commands.Backward.Execute();
								}
								e.Handled = true;
							}
							else
							{
								Refocus = Pan[PanIdx].focus;
								if (Refocus == null) Refocus = TaskSwitcher; // trigger fallback focus
								UpdatePanels();
								e.Handled = true;
							}
						}
                        break;

                    case Key.Right:
                        {
                            if (sender == wxNext)
                            {
                                if (Global.Local.InfoEditMode || Global.Local.ConfigVisible && !Global.User.ConfigWindow)
                                {
									e.Handled = true;
									TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
                                    Commands.MainWindow.wxInfoPane.MoveFocus(tRequest);
                                }
                                else if (Global.User.KeysToNavigateEnable)
                                {
									e.Handled = true;
									Commands.Forward.Execute();
                                }

                            }
							else
							{
								Refocus = Pan[PanIdx].focus;
								if (Refocus == null) Refocus = TaskSwitcher; // trigger fallback focus
								UpdatePanels();
								e.Handled = true;
							}
						}
                        break;

                }
            }
        }


		private void wxToolBar_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			LimeMsg.Debug("wxToolBar_PreviewKeyDown");
			if (Keyboard.Modifiers != 0) return;

			var wxobj = WPF.FindFirstChild<ToolBar>(wxToolBar);
			if (wxobj==null || wxobj.IsOverflowOpen) return;

			switch (e.Key)
			{
				case Key.Left:
					Refocus = Pan[PanIdx].focus;
					if (Refocus == null) Refocus = TaskSwitcher; // trigger fallback focus
					UpdatePanels();
					e.Handled = true;
					break;

				case Key.Down:
					// Detect whether focused item is the first enabled or the last enabled
					bool last = false;
					var wxfocus = Keyboard.FocusedElement;
					if (wxobj.HasOverflowItems)
					{
						if (wxfocus is ToggleButton wxov)
						{
							last = wxov.Name == "OverflowButton";
						}
					}
					else
					{ 
						foreach (var wxitem in wxobj.Items)
						{
							if (wxitem is UIElement wxui)
							{
								if (wxitem == wxfocus)
								{
									last = true;
								}
								else if (wxui.IsEnabled)
								{
									if (last)
									{
										last = false;
										break;
									}
								}
							}
						}
					}

					if (wxNext.IsEnabled && last)
					{
						Keyboard.Focus(wxNext);
						e.Handled = true;
					}
					break;
			}
		}


		/// <summary>
		/// Fix Mouse Wheel behaviour
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FixScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			e.Handled = true;
			var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
			{
				RoutedEvent = MouseWheelEvent
			};
			wxScroll.RaiseEvent(e2);
		}


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region LimeItem (Button/Panel-Title) Callback


		/// <summary>
		/// Update the data-context and the panel layout de facto
		/// </summary>
		/// <param name="sender">Callback from DispatcherTimer</param>
		/// <param name="e">Callback from DispatcherTimer</param>
		private void AutoSelection_Callback(object sender = null, EventArgs e = null)
		{
			AutoSelectionTimer.Stop();

			// retrieve the focused button
			var wxobj = Keyboard.FocusedElement as ToggleButton;
			var focus = wxobj?.DataContext as LimeItem;

			var item = Global.Local.FocusItem;

			LimeMsg.Debug("AutoSelection_Callback: item: {0} - Focus: {1}", item?.Name, focus?.Name);

			if (focus != null && item == focus && !Global.Local.InfoEditMode)
			{

				var prev = WPF.FindFirstChild<ToggleButton>(wxRoot, (wx) => wx.DataContext == Global.Local.SelectedItem);
				if (prev != null)
				{
					prev.IsChecked = false;
				}

				Global.Local.SelectedItem = item;
				wxobj.IsChecked = true;
			}
		}


		/// <summary>
		/// Track the focus on the LimeItems
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void Button_GotFocus(object sender, RoutedEventArgs e = null)
        {
            // retrieve the calling button
            var wxobj = (e != null ? e.OriginalSource : sender) as ButtonBase;
            var item = wxobj.DataContext as LimeItem;

            VisualStateManager.GoToState(wxobj, "Focused", true);

            // Safeguard
            while (PanIdx >= Pan.Count)
            {
                Pan.Add(new PanelParameters());
            }

            // Change current focus information
            wxButtonFocus = wxobj;

            if (item != null)
            {
                Pan[PanIdx].focus = item;

                Pan[PanIdx].currentItem = PanItems[PanIdx].Children != null ? PanItems[PanIdx].Children.IndexOf(item) : -1;
                if (Pan[PanIdx].currentItem >= 0)
                {
                    Pan[PanIdx].currentPanel = PanIdx;
                }
                else
                {
                    for (int i = 0; i < PanIdx && Pan[PanIdx].currentItem < 0; i++)
                    {
                        Pan[PanIdx].currentItem = PanItems[i].Children.IndexOf(item);
                        Pan[PanIdx].currentPanel = i;
                    }
                }

                LimeMsg.Debug("Button_GotFocus: LimeItem: PanIdx:{0}, Pan[{0}].currentPanel:{2} Pan[{0}].currentItem:{3} : {1} ", PanIdx, item.Name, Pan[PanIdx].currentPanel, Pan[PanIdx].currentItem);
            }
            else
            {
                LimeMsg.Debug("Button_GotFocus: Button");

            }

			// Cleanup
			var focus = Global.Local.FocusItem;
			var selec = Global.Local.SelectedItem;

			if (focus != selec)
			{
				if (focus != null && focus != item)
				{
					focus.MetadataUnload();
				}
				if (selec != null && selec != item)
				{
					selec.MetadataUnload();
				}
			}


			Global.Local.FocusItem = item;

			// Auto Selection
			if ((Global.Local.CtrlMode & Global.User.AutoSelection) != 0 && !Global.Local.InfoEditMode)
			{
				AutoSelectionTimer.Interval = TimeSpan.FromMilliseconds(Global.User.AutoSelectionAfter);
				AutoSelectionTimer.Start();
			}


			// This will do the Metadata loading in the background to keep responsiveness on the UI, in a resource-safe way
			if (item != null)
            {
                await item.MetadataLoadAsync(() => Global.Local.FocusItem == item || Global.Local.SelectedItem == item);
            }
        }



        /// <summary>
        /// Track the focus on the LimeItems
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_LostFocus(object sender, RoutedEventArgs e)
        {
			AutoSelectionTimer.Stop();
			LimeMsg.Debug("Button_LostFocus");

			var wxobj = e.OriginalSource as ButtonBase;
            var wxToggle = wxobj as ToggleButton;

            if (wxobj != null)
            {
                VisualStateManager.GoToState(wxobj, wxToggle != null && wxToggle.IsChecked == true ? "Checked" : "Normal", true);
            }

            wxButtonFocus = null;
        }


        /// <summary>
        /// Default open of an item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LimeItem_Click(object sender = null, RoutedEventArgs e = null)
        {

			// Retrieve item to open (from GUI event, or programmatically)
			if (e != null) e.Handled = true;
			if (!(e?.OriginalSource is ToggleButton wxobj)) wxobj = wxButtonFocus as ToggleButton;
			if (wxobj == null) return;

            wxobj.Focus();
			if (!(wxobj.DataContext is LimeItem item)) return;

			// Check whether this should be only selected and not opened
			if ((Global.Local.CtrlMode & Global.User.AutoSelection) == 0 || Global.Local.InfoEditMode)
            {
                if (item != Global.Local.SelectedItem)
                {
                    var prev = WPF.FindFirstChild<ToggleButton>(wxRoot, (wx) => wx.DataContext == Global.Local.SelectedItem);
                    if (prev != null)
                    {
                        prev.IsChecked = false;
                    }

                    Global.Local.SelectedItem = item;

                    if (Global.Local.ConfigVisible && !Global.User.ConfigWindow)
                    {
                        Global.Local.ConfigVisible = false;
                    }

                    // In Edit mode, don't do anything
                    LimeMsg.Debug("LimeItem_Click: Edit: {0} ", item.Name);
                    return;
                }
            }

            // Enable opening			
            if (item.Directory)
            {
                // Open directory
                ChangePanIdx(PanIdx + 1, item);
            }
            else if (item.Handle != IntPtr.Zero && Commands.CfgWindow != null && Commands.CfgWindow.Handle == item.Handle)
            {
                // Show Setting window
                item.Open();
            }
            else if (Global.User.HideOnLaunch)
            {
                // Hide window and launch
                HideTasks();
                AnimateAction.Do("wxWindow_hide", () =>
                {
                    var ok = item.Open();

                    AnimateAction.IsAnimated = true; // Avoid to animate again in Hide()
                    Commands.MainWindow.Hide();
                    AnimateAction.IsAnimated = false;

                    if (!ok)
                    {
                        Commands.MainWindow.Show();
                        wxobj.IsChecked = false;
                    }

                });

            }
            else
            {
                // Launch
                var ok = item.Open();
                if (!ok)
                {
                    wxobj.IsChecked = false;
                }
            }

        }

        /// <summary>
        /// Open context Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LimeItem_ContextMenu(object sender = null, ContextMenuEventArgs e = null)
        {

            LimeMsg.Debug("LimeItem_ContextMenu");

			// Retrieve item to open (from GUI event, or programmatically)
			if (e != null) e.Handled = true;
			if (!(sender is FrameworkElement wxobj)) wxobj = wxButtonFocus;
			if (wxobj == null) return;
            if (wxobj is ToggleButton) wxobj.Focus();
            LimeItem item = (LimeItem)wxobj.DataContext;
            if (item == null) return;

			// Select item
			if (wxobj is ToggleButton wxbut)
			{
				var prev = WPF.FindFirstChild<ToggleButton>(wxRoot, (wx) => wx.DataContext == Global.Local.SelectedItem);
				if (prev != null)
				{
					prev.IsChecked = false;
				}

				Global.Local.SelectedItem = item;
				wxbut.IsChecked = true;
			}

			// Execute
			Point wpos;
            if (Global.Local.CtrlMode == CtrlMode.Mouse)
            {
                wpos = Mouse.GetPosition(Application.Current.MainWindow);
            }
            else
            {
                wpos = wxobj.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(wxobj.ActualWidth * 0.75, wxobj.ActualHeight * 0.75));
            }

            Application.Current.MainWindow.Topmost = false;
            item.ShellMenu(WPF.Windows2DrawingPoint(wpos));

        }


        #endregion


	}
}
