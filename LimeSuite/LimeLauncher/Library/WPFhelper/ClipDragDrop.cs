/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     09-10-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/


using Lime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WPFhelper
{
	// ----------------------------------------------------------------------------------------------
	#region Types

	/// <summary>
	/// Specify the default operation which can be accomplished in Drag and drop.
	/// Convertible to a <see cref="DataObjectAction"/> 
	/// </summary>
	public enum ClipDragDefaultOperation
	{
		/// <summary>
		/// Copy by default
		/// </summary>
		Copy = 0x01,

		/// <summary>
		/// Move by default
		/// </summary>
		Move = 0x02,

		/// <summary>
		/// Create a link by default
		/// </summary>
		Link = 0x04,

		/// <summary>
		/// Show a drop-menu by default
		/// </summary>
		Menu = 0x08,

		/// <summary>
		/// Show a drop-menu by default
		/// </summary>
		Open = 0x10
	}


	/// <summary>
	/// Specify the Operations which can be accomplish in Drag and drop and Clipboard.
	/// Convertible to a <see cref="DataObjectAction"/> 
	/// </summary>
	[Flags]
	public enum ClipDragDropOperations
	{
		/// <summary>
		/// No action
		/// </summary>
		None = 0x00,

		/// <summary>
		/// Enable to copy
		/// </summary>
		Copy = 0x01,

		/// <summary>
		/// Enable to move
		/// </summary>
		Move = 0x02,

		/// <summary>
		/// Enable to link
		/// </summary>
		Link = 0x04,

		/// <summary>
		/// Enable to show drop-menu
		/// </summary>
		Menu = 0x08,

		/// <summary>
		/// Enable to open the object
		/// </summary>
		Open = 0x10,

		// --- end DataObjectAction compatibility ---

		/// <summary>
		/// Source of the action
		/// </summary>
		From = 0x20,

		/// <summary>
		/// destination of the action
		/// </summary>
		To = 0x40,

		/// <summary>
		/// Interface: Clipboard
		/// </summary>
		Clipboard = 0x80,

		/// <summary>
		/// Interface: Drag & Drop
		/// </summary>
		DragDrop = 0x100,

		/// <summary>
		/// Enable to autoscroll when on a border of a ScrollViewer
		/// </summary>
		Scroll = 0x0200,

		/// <summary>
		/// Enable to bring the window on top when it is a drop destination
		/// </summary>
		BringOnTop = 0x0400,

		/// <summary>
		/// Enable drag & drop from/to outside the Containing ItemsControl
		/// </summary>
		InterControl = 0x0800,

		/// <summary>
		/// Enable drag & drop from/to outside the application
		/// </summary>
		InterApplication = 0x1000,

		/// <summary>
		/// Enable the Cut/copy/paste menu
		/// </summary>
		ClipboardMenu = 0x2000,

		/// <summary>
		/// Enable the deletion of element using cut
		/// </summary>
		Delete = 0x4000,

		/// <summary>
		/// Enable all drag and drop operations
		/// </summary>
		All = 0x8000 - 1
	}

	/// <summary>
	/// Set the severity of the Drag & Drop self-check when in debug mode 
	/// </summary>
	public enum ClipDragDropSelfCheckSeverity
	{
		/// <summary>
		/// Disable self-check
		/// </summary>
		Off = 0,

		/// <summary>
		/// Issue warning on the standard output
		/// </summary>
		Warning = 1,

		/// <summary>
		/// Throw an error
		/// </summary>
		Error = 2
	}


	/// <summary>
	/// Drop Menu Command Type
	/// </summary>
	public class ClipDragDropMenuCommand : ICommand
	{
		public bool CanExecute(object parameter)
		{
			if (parameter == null) return false;
			var actionName = (string)parameter;
			var action = (DataObjectAction)Enum.Parse(typeof(DataObjectAction), actionName);
			return ClipDragDrop.CanDrop(action);
		}

		public void Execute(object parameter)
		{
			if (parameter == null) return;
			var actionName = (string)parameter;
			var action = (DataObjectAction)Enum.Parse(typeof(DataObjectAction), actionName);
			ClipDragDrop.Drop(action);
		}

		/// <summary>
		/// Subscribe to event (boilerplate)
		/// </summary>
		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
	}

	#endregion


	// ----------------------------------------------------------------------------------------------
	#region Class


	/// <summary>
	/// Provide a MVVM way to handle Drag & Drop operation and Clipboard operations in WPF.
	/// To fully leverage the use of this class, it is recommended to use IDataObjectCompatible objects
	/// for the View-Model, as Drag-Sources and Drop-Destination data.
	/// </summary>
	public static class ClipDragDrop
	{
		// ----------------------------------------------------------------------------------------------
		#region Types

		/// <summary>
		/// ClipDragDrop data context 
		/// </summary>
		private class ClipDragDropContext
		{
			public ClipDragDropOperations SourceOperations = ClipDragDropOperations.None;
			public FrameworkElement Source;
			public ItemsControl SourceItemsControl;
			public DataObjectCompatibleEventArgs DataObjectCompatibleEventArgs;
		}


		/// <summary>
		/// ClipDragDrop data context 
		/// </summary>
		private class DragDropContext
		{
			public FrameworkElement Destination;
			public ItemsControl DestinationItemsControl;

			public FrameworkElement Over;
			public DragDropEffects DragDropEffects = DragDropEffects.None;
			public ClipDragDefaultOperation DragDefaultOperation;
			public bool RightClick;
			public Point MouseHit;
			public Rect? HitZone;
		}



		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Fields and properties

		/// <summary>
		/// Scale of the Drag Icons
		/// </summary>
		public static double Scale = 0.75;

		/// <summary>
		/// Timeout in milliseconds
		/// </summary>
		public static double Timeout = 1500;

		/// <summary>
		/// Time in milliseconds to trigger a scroll or bring window to front during Drag & Drop
		/// </summary>
		public static double TriggerTime = 1000;

		/// <summary>
		/// MenuCommand to be used in the Drop menu
		/// </summary>
		public static readonly ClipDragDropMenuCommand MenuCommand = new ClipDragDropMenuCommand();

		/// <summary>
		/// CommandBinding to handle the clipboard Cut operation
		/// </summary>
		private static readonly CommandBinding CommandBindingCut = 
			new CommandBinding(ApplicationCommands.Cut, ClipboardExecuted, ClipboardCanExecute);

		/// <summary>
		/// CommandBinding to handle the clipboard Copy operation
		/// </summary>
		private static readonly CommandBinding CommandBindingCopy = 
			new CommandBinding(ApplicationCommands.Copy, ClipboardExecuted, ClipboardCanExecute);

		/// <summary>
		/// CommandBinding to handle the clipboard Paste operation
		/// </summary>
		private static readonly CommandBinding CommandBindingPaste = 
			new CommandBinding(ApplicationCommands.Paste, ClipboardExecuted, ClipboardCanExecute);

		/// <summary>
		/// CommandBinding to handle the clipboard Paste operation
		/// </summary>
		private static readonly CommandBinding CommandBindingDelete =
			new CommandBinding(ApplicationCommands.Delete, ClipboardExecuted, ClipboardCanExecute);

		/// <summary>
		/// Store the open Drop menu instance
		/// </summary>
		private static ContextMenu ContextMenu = null;

		/// <summary>
		/// Time of drag over application
		/// </summary>
		private static DateTime Time = DateTime.Now.AddMilliseconds(-Timeout);

		/// <summary>
		/// Duration of the Drag animation
		/// </summary>
		private static readonly Duration AnimDuration = new Duration(new TimeSpan(0, 0, 0, 0, 200));

		/// <summary>
		/// Store the Clipboard object data
		/// </summary>
		private static ClipDragDropContext ClipData = null;

		/// <summary>
		/// Store the Drag & Drop object data
		/// </summary>
		private static ClipDragDropContext DragData = null;

		/// <summary>
		/// Store the Drag & Drop oject context
		/// </summary>
		private static DragDropContext DragContext = null;

		/// <summary>
		/// Dragged control decorations
		/// </summary>
		private static DragSystemIcon DragIcon = null;


		/// <summary>
		/// Objects that have be queried by CanDragDrop method 
		/// </summary>
		private static Dictionary<DataObjectCompatibleEventArgs, DataObjectCompatibleEventArgs> CanDragDropCache;
		
		/// <summary>
		/// Handle Cache in HandleDestination
		/// </summary>
		private static ClipDragDropOperations HandleDestinationAction =
			ClipDragDropOperations.From | ClipDragDropOperations.To; // Invalidate

		/// <summary>
		/// Indicate whether a Drag-and-drop operation is ongoing
		/// </summary>
		public static bool IsDragging
		{
			get { return DragIcon != null; }
		}

		/// <summary>
		/// Indicate whether a Drag-and-drop operation is ongoing or may start after a click
		/// </summary>
		public static bool MayDrag
		{
			get { return DragData != null; }
		}

		/// <summary>
		/// Indicate whether a Drop Menu is open
		/// </summary>
		public static bool IsContextMenuOpen
		{
			get { return ContextMenu != null && ContextMenu.IsOpen; }
		}


		/// <summary>
		/// Retrieve the DataObjectCompatibleEventArgs object representing the current Drag & Drop operation
		/// </summary>
		public static DataObjectCompatibleEventArgs DataObjectCompatibleEventArgs
		{
			get { return DragData?.DataObjectCompatibleEventArgs; }
		}

		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Attached Properties

		/// <summary>
		/// Enable clipboard operations on a FrameworkElement. Must be set on the root WPF element only (Window)
		/// </summary>
		public static readonly DependencyProperty EnableProperty =
		DependencyProperty.RegisterAttached("Enable", typeof(bool), 
			typeof(ClipDragDrop), new PropertyMetadata(false, EnableChanged));

		public static bool GetEnable(FrameworkElement obj)
		{
			return (bool)obj.GetValue(EnableProperty);
		}

		public static void SetEnable(FrameworkElement obj, bool value)
		{
			obj.SetValue(EnableProperty, value);
		}


		/// <summary>
		/// Set the Drag data-Source (View-Model) and enable dragging the FrameworkElement (View).
		/// Preferably reference a <see cref="IDataObjectCompatible"/> there to leverage the MVVM.
		/// </summary>
		public static readonly DependencyProperty SourceProperty =
		DependencyProperty.RegisterAttached("Source", typeof(object),
			typeof(ClipDragDrop));

		public static object GetSource(FrameworkElement obj)
		{
			return obj.GetValue(SourceProperty);
		}

		public static void SetSource(FrameworkElement obj, object value)
		{
			obj.SetValue(SourceProperty, value);
		}

		/// <summary>
		/// Set the Drop data-Destination (View-Model) and enable droppping to the FrameworkElement (View).
		/// Preferably reference a <see cref="IDataObjectCompatible"/> there to leverage the MVVM.
		/// </summary>
		public static readonly DependencyProperty DestinationProperty =
		DependencyProperty.RegisterAttached("Destination", typeof(object),
			typeof(ClipDragDrop));

		public static object GetDestination(FrameworkElement obj)
		{
			return obj.GetValue(DestinationProperty);
		}

		public static void SetDestination(FrameworkElement obj, object value)
		{
			obj.SetValue(DestinationProperty, value);
		}

		/// <summary>
		/// Gets or sets the clipboard operations which are *not* allowed inside the FrameworkElement
		/// </summary>
		public static readonly DependencyProperty DisableProperty =
		DependencyProperty.RegisterAttached("Disable", typeof(ClipDragDropOperations), 
			typeof(ClipDragDrop), 
			new FrameworkPropertyMetadata(
				ClipDragDropOperations.None, 
				FrameworkPropertyMetadataOptions.Inherits));

		public static ClipDragDropOperations GetDisable(FrameworkElement obj)
		{
			return (ClipDragDropOperations)obj.GetValue(DisableProperty);
		}

		public static void SetDisable(FrameworkElement obj, ClipDragDropOperations value)
		{
			obj.SetValue(DisableProperty, value);
		}


		/// <summary>
		/// Gets or sets the default Drag & Drop operation for element Dragged from this FrameworkElement.
		/// For items dragged from outside the application, the <see cref="DragDefaultOperationProperty"/> 
		/// on the root Drag & Drop element (where the <see cref="EnableProperty"/> is set) applies.
		/// </summary>
		public static readonly DependencyProperty DragDefaultOperationProperty =
		DependencyProperty.RegisterAttached("DragDefaultOperation", typeof(ClipDragDefaultOperation),
			typeof(ClipDragDrop), 
			new FrameworkPropertyMetadata(
				ClipDragDefaultOperation.Open,
				FrameworkPropertyMetadataOptions.Inherits));

		public static ClipDragDefaultOperation GetDragDefaultOperation(FrameworkElement obj)
		{
			return (ClipDragDefaultOperation)obj.GetValue(DragDefaultOperationProperty);
		}

		public static void SetDragDefaultOperation(FrameworkElement obj, ClipDragDefaultOperation value)
		{
			obj.SetValue(DragDefaultOperationProperty, value);
		}


		/// <summary>
		/// Enable to animate the clipboard operations inside a FrameworkElement.
		/// </summary>
		public static readonly DependencyProperty AnimateProperty =
		DependencyProperty.RegisterAttached("Animate", typeof(bool), typeof(ClipDragDrop),
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

		public static bool GetAnimate(FrameworkElement obj)
		{
			return (bool)obj.GetValue(AnimateProperty);
		}

		public static void SetAnimate(FrameworkElement obj, bool value)
		{
			obj.SetValue(AnimateProperty, value);
		}



		/// <summary>
		/// Override the default Drop menu with a custom <see cref="ContextMenu"/> inside a FrameworkElement.
		/// </summary>
		public static readonly DependencyProperty ContextMenuProperty =
		DependencyProperty.RegisterAttached("ContextMenu", typeof(ContextMenu), typeof(ClipDragDrop),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

		public static ContextMenu GetContextMenu(FrameworkElement obj)
		{
			return (ContextMenu)obj.GetValue(ContextMenuProperty);
		}

		public static void SetContextMenu(FrameworkElement obj, ContextMenu value)
		{
			obj.SetValue(ContextMenuProperty, value);
		}


		/// <summary>
		/// Gets or sets the ClipDragDrop self check severity in Debug mode.
		/// It has no effect in Release mode.
		/// </summary>
		public static readonly DependencyProperty SelfCheckSeverityProperty =
		DependencyProperty.RegisterAttached("SelfCheckSeverity", typeof(ClipDragDropSelfCheckSeverity),
			typeof(ClipDragDrop),
			new FrameworkPropertyMetadata(
				ClipDragDropSelfCheckSeverity.Warning,
				FrameworkPropertyMetadataOptions.Inherits));

		public static ClipDragDropSelfCheckSeverity GetSelfCheckSeverity(FrameworkElement obj)
		{
#if DEBUG
			return (ClipDragDropSelfCheckSeverity)obj.GetValue(SelfCheckSeverityProperty);
#else
			return ClipDragDropSelfCheckSeverity.Off;
#endif
		}

		public static void SetSelfCheckSeverity(FrameworkElement obj, ClipDragDropSelfCheckSeverity value)
		{
#if DEBUG
			obj.SetValue(SelfCheckSeverityProperty, value);
#endif
		}


		#region Translation Attached Properties

		/// <summary>
		/// Set the default format text to display in drop action: Cancel.
		/// The {0} part will be expended with destination name.
		/// </summary>
		/// <example>"Cancel {0}" --> "Cancel MyDestination"</example>
		public static readonly DependencyProperty FormatCancelProperty =
		DependencyProperty.RegisterAttached("FormatCancel", typeof(string), typeof(ClipDragDrop),
			new FrameworkPropertyMetadata("Cancel", FrameworkPropertyMetadataOptions.Inherits));

		public static string GetFormatCancel(FrameworkElement obj)
		{
			return (string)obj.GetValue(FormatCancelProperty);
		}

		public static void SetFormatCancel(FrameworkElement obj, string value)
		{
			obj.SetValue(FormatCancelProperty, value);
		}


		/// <summary>
		/// Set the default format text to display in drop action: Copy.
		/// The {0} part will be expended with destination name.
		/// </summary>
		/// <example>"Copy to {0}" --> "Copy to MyDestination"</example>
		public static readonly DependencyProperty FormatCopyProperty =
		DependencyProperty.RegisterAttached("FormatCopy", typeof(string), typeof(ClipDragDrop),
			new FrameworkPropertyMetadata("Copy to {0}", FrameworkPropertyMetadataOptions.Inherits));

		public static string GetFormatCopy(FrameworkElement obj)
		{
			return (string)obj.GetValue(FormatCopyProperty);
		}

		public static void SetFormatCopy(FrameworkElement obj, string value)
		{
			obj.SetValue(FormatCopyProperty, value);
		}


		/// <summary>
		/// Set the default format text to display in drop action: Move.
		/// The {0} part will be expended with destination name.
		/// </summary>
		/// <example>"Move to {0}" --> "Move to MyDestination"</example>
		public static readonly DependencyProperty FormatMoveProperty =
		DependencyProperty.RegisterAttached("FormatMove", typeof(string), typeof(ClipDragDrop),
			new FrameworkPropertyMetadata("Move to {0}", FrameworkPropertyMetadataOptions.Inherits));

		public static string GetFormatMove(FrameworkElement obj)
		{
			return (string)obj.GetValue(FormatMoveProperty);
		}

		public static void SetFormatMove(FrameworkElement obj, string value)
		{
			obj.SetValue(FormatMoveProperty, value);
		}


		/// <summary>
		/// Set the default format text to display in drop action: Link.
		/// The {0} part will be expended with destination name.
		/// </summary>
		/// <example>"Link to {0}" --> "Link to MyDestination"</example>
		public static readonly DependencyProperty FormatLinkProperty =
		DependencyProperty.RegisterAttached("FormatLink", typeof(string), typeof(ClipDragDrop),
			new FrameworkPropertyMetadata("Link to {0}", FrameworkPropertyMetadataOptions.Inherits));

		public static string GetFormatLink(FrameworkElement obj)
		{
			return (string)obj.GetValue(FormatLinkProperty);
		}

		public static void SetFormatLink(FrameworkElement obj, string value)
		{
			obj.SetValue(FormatLinkProperty, value);
		}


		/// <summary>
		/// Set the default format text to display in drop action: Menu.
		/// The {0} part will be expended with destination name.
		/// </summary>
		/// <example>"Menu in {0}" --> "Menu in MyDestination"</example>
		public static readonly DependencyProperty FormatMenuProperty =
		DependencyProperty.RegisterAttached("FormatMenu", typeof(string), typeof(ClipDragDrop),
			new FrameworkPropertyMetadata("Menu on {0}", FrameworkPropertyMetadataOptions.Inherits));

		public static string GetFormatMenu(FrameworkElement obj)
		{
			return (string)obj.GetValue(FormatMenuProperty);
		}

		public static void SetFormatMenu(FrameworkElement obj, string value)
		{
			obj.SetValue(FormatMenuProperty, value);
		}


		/// <summary>
		/// Set the default format text to display in drop action: Open.
		/// The {0} part will be expended with destination name.
		/// </summary>
		/// <example>"Open in {0}" --> "Open in MyDestination"</example>
		public static readonly DependencyProperty FormatOpenProperty =
		DependencyProperty.RegisterAttached("FormatOpen", typeof(string), typeof(ClipDragDrop),
			new FrameworkPropertyMetadata("Open", FrameworkPropertyMetadataOptions.Inherits));

		public static string GetFormatOpen(FrameworkElement obj)
		{
			return (string)obj.GetValue(FormatOpenProperty);
		}

		public static void SetFormatOpen(FrameworkElement obj, string value)
		{
			obj.SetValue(FormatOpenProperty, value);
		}


		#endregion



		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Clipboard watcher


		private const int WM_CLIPBOARDUPDATE = 0x031D;

		/// <summary>
		///  See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
		/// </summary>
		/// <param name="hwnd"></param>
		/// <returns></returns>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AddClipboardFormatListener(IntPtr hwnd);

		private static HwndSource WinSource = null;
		private static bool WndProcSkip = false;

		/// <summary>
		/// Start or continue watching clipboard events to track changes
		/// </summary>
		/// <param name="elm"></param>
		private static void ClipboardWatcher(FrameworkElement elm)
		{
			LimeMsg.Debug("ClipDragDrop ClipboardWatcher: {0} --> {1}", elm, WinSource);

			if (WinSource != null)
			{
				// Alreay started: Ignore the next change
				WndProcSkip = true;
				return;
			}

			var windowSource = PresentationSource.FromVisual(elm).RootVisual as Window;

			if (!(PresentationSource.FromVisual(windowSource) is HwndSource source))
			{
				return;
			}


			WinSource = source;
			WinSource.AddHook(WndProc);

			// get window handle for interop
			IntPtr windowHandle = new WindowInteropHelper(windowSource).Handle;

			// register for clipboard events
			AddClipboardFormatListener(windowHandle);
		}

		/// <summary>
		/// Invalidate clipboard data if the clipboard update happen off control of the ClipDragDrop
		/// </summary>
		/// <param name="hwnd"></param>
		/// <param name="msg"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <param name="handled"></param>
		/// <returns></returns>
		private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_CLIPBOARDUPDATE)
			{
				LimeMsg.Debug("ClipDragDrop WndProc: {0} --> skip: {1}", WinSource, WndProcSkip);

				if (WndProcSkip)
				{
					WndProcSkip = false;
				}
				else if (ClipData != null)
				{
					// Update visual
					ClipboardSourceVisual(false);

					// Invalidate the data source
					ClipData = null;
				}
			}

			return IntPtr.Zero;
		}


		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Class functions


		/// <summary>
		/// Create or destroy the Drag & Drop functionality
		/// </summary>
		/// <param name="d"></param>
		/// <param name="e"></param>
		private static void EnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var wxthis = (FrameworkElement) d;
			if (wxthis == null) return;
			if ((bool)e.NewValue)
			{
				// Handle Drag & Drop
				wxthis.AllowDrop = true;

				wxthis.PreviewMouseDown += OnMouseButtonDown;
				wxthis.PreviewMouseUp += OnMouseButtonUp;

				wxthis.QueryContinueDrag += OnQueryContinueDrag;
				wxthis.GiveFeedback += OnGiveFeedback;
				wxthis.Drop += OnDrop;

				wxthis.DragOver += OnDragOver;
				wxthis.DragLeave += OnDragLeave;

				// Handle Clipboard
				wxthis.CommandBindings.Add(CommandBindingCut);
				wxthis.CommandBindings.Add(CommandBindingCopy);
				wxthis.CommandBindings.Add(CommandBindingPaste);
				wxthis.CommandBindings.Add(CommandBindingDelete);

				wxthis.ContextMenuOpening += OnClipboardContextMenuOpening;
			}
			else
			{
				// Remove Drag & Drop
				wxthis.PreviewMouseDown -= OnMouseButtonDown;
				wxthis.PreviewMouseUp -= OnMouseButtonUp;
				wxthis.PreviewMouseMove -= OnMouseMove;

				wxthis.QueryContinueDrag -= OnQueryContinueDrag;
				wxthis.GiveFeedback -= OnGiveFeedback;
				wxthis.Drop -= OnDrop;

				wxthis.DragOver -= OnDragOver;
				wxthis.DragLeave -= OnDragLeave;

				// Remove Clipboard
				wxthis.CommandBindings.Remove(CommandBindingCut);
				wxthis.CommandBindings.Remove(CommandBindingCopy);
				wxthis.CommandBindings.Remove(CommandBindingPaste);
				wxthis.CommandBindings.Remove(CommandBindingDelete);

				wxthis.ContextMenuOpening -= OnClipboardContextMenuOpening;
			}
		}


		/// <summary>
		///  Cancel any ongoing Drag & Drop operation
		/// </summary>
		public static void Cancel()
		{
			StopDragDrop(cancel: true);
		}


		/// <summary>
		///  Stop any ongoing Drag & Drop operation
		/// </summary>
		public static void StopDragDrop(bool cancel)
		{

			// Animate Source
			bool animate = false;

			if (DragData != null)
			{
				LimeMsg.Debug("ClipDragDrop Cancel: {0}", cancel);

				// Restore Drop destination aspect
				if (DragContext.Destination != null)
				{
					ClipDragDropDestinationVisual(false, DragContext.Destination);
					DragDropInCollection(ClipDragDropOperations.DragDrop);
				}

				// Handle Source and its attributes
				if (DragData.Source != null)
				{
					animate = GetAnimate(DragData.Source);
					DragData.Source.PreviewMouseMove -= OnMouseMove;
					DragDropSourceVisual(false);
				}

			}

			if (DragIcon != null)
			{
				if (cancel && animate)
				{
					var wxsrc = DragData.Source;
					// Bring dragged object back to its position
					var wnd = PresentationSource.FromVisual(wxsrc).RootVisual;
					var offset = new Point(wxsrc.ActualWidth / 2, wxsrc.ActualHeight / 2);
					Point pos = wxsrc.TransformToAncestor(wnd).Transform(offset);
					DragIcon.GotoPosition(wnd.PointToScreen(pos), AnimDuration, destroy: true);
				}
				else
				{
					DragIcon.Destroy();
				}
				DragIcon = null;
			}

			if (ContextMenu != null)
			{
				ContextMenu.IsOpen = false;
			}

#if DEBUG
			DebugAdorner.Hide();
#endif
			CanDragDropCache = null;
			HandleDestinationAction = ClipDragDropOperations.From | ClipDragDropOperations.To; // Invalidate
			DragData = null;
			DragContext = null;
			ContextMenu = null;
			Mouse.OverrideCursor = null;
		}


		/// <summary>
		/// Check whether an action can be used on the current Drag & Drop operation
		/// </summary>
		/// <param name="action">action to be tested</param>
		/// <returns>true if action can be applied to the current Clipboard operation</returns>
		public static bool CanDrop(DataObjectAction action)
		{
			if (DataObjectCompatibleEventArgs == null || DragData == null) return false;

			var direction = DataObjectCompatibleEventArgs.Direction;

			if (direction != DataObjectDirection.To) return false;
			if (action == DataObjectAction.None) return true;

			var method = DataObjectCompatibleEventArgs.Method;

			var operation =
				(direction == DataObjectDirection.From ? ClipDragDropOperations.From :
				direction == DataObjectDirection.To ? ClipDragDropOperations.To :
				ClipDragDropOperations.None) |
				(method == DataObjectMethod.Clipboard ? ClipDragDropOperations.Clipboard :
				method == DataObjectMethod.DragDrop ? ClipDragDropOperations.DragDrop :
				ClipDragDropOperations.None) |
				(ClipDragDropOperations)action;

			var effect = CanClipDragDrop(
				operation,
				(object)DragContext.Destination ?? DataObjectCompatibleEventArgs.Data,
				DataObjectCompatibleEventArgs.DestinationIndex,
				register: false,
				wxItemsControl: DragContext.DestinationItemsControl);

			return effect != DataObjectAction.None && effect == action;
		}


		/// <summary>
		/// Execute and complete the current Drag & Drop operation
		/// </summary>
		/// <param name="action">action to be executed (None: will just cancel the Drag & Drop operation)</param>
		/// <returns>true if the action has been handled</returns>
		public static bool Drop(DataObjectAction action)
		{
			if (DragData == null ||
				DragContext.DragDropEffects == DragDropEffects.None ||
				action == DataObjectAction.None ||
				DataObjectCompatibleEventArgs.Direction != DataObjectDirection.To ||
				!CanDrop(action))
			{
				LimeMsg.Debug("ClipDragDrop Drop: Cancelled: {0}", DragContext.DragDropEffects);
				Cancel();
				return false;
			}

			DataObjectCompatibleEventArgs.Action = action;

			return Execute(DragData, DragContext.Destination);
		}



		/// <summary>
		/// Execute and complete if applicable the current Clipboard or Drag & Drop operation
		/// </summary>
		/// <param name="context">context to be executed</param>
		/// <param name="wxDest">destination</param>
		/// <returns>true if the action has been handled</returns>
		private static bool Execute(ClipDragDropContext context, FrameworkElement wxDest)
		{
			bool ret = true;
			bool cancelled = false;

			var docEventArgs = context?.DataObjectCompatibleEventArgs;

			if (docEventArgs == null)
			{
				LimeMsg.Debug("ClipDragDrop Execute: Invalid");
				cancelled = true;
			}
			else if(docEventArgs.Direction == DataObjectDirection.From)
			{
				// Handle Source
				LimeMsg.Debug("ClipDragDrop Execute: Source");

				// Use the DataObjectCompatible object action handler on source
				if (!docEventArgs.SourceHandled)
				{
					if (docEventArgs.Source is IDataObjectCompatible source)
					{
						docEventArgs.Handled = false;
						docEventArgs.Direction = DataObjectDirection.From;
						source.DataObjectDo(docEventArgs);
					}
					else
					{
						// Try to handle collection automatically
						docEventArgs.DoOnCollection(null);
					}

					ret = docEventArgs.Handled;
				}

				return ret;
			}
			else if (wxDest == null || docEventArgs.Action == DataObjectAction.None)
			{
				LimeMsg.Debug("ClipDragDrop Execute: Cancelled");
				cancelled = true;
			}
			else 
			{
				// Handle Destination
				LimeMsg.Debug("ClipDragDrop Execute: Destination");

				// Make sure we make the right destination message
				docEventArgs.Handled = false;
				docEventArgs.SourceHandled = false;

				object data = GetDestination(wxDest) ?? wxDest.DataContext;
				if (docEventArgs.Action == DataObjectAction.Menu)
				{
					// Show Drop Menu
					ContextMenu = GetContextMenu(wxDest);
					if (ContextMenu == null)
					{
						// Create default menu

						ContextMenu = new ContextMenu();

						for (var enu = DataObjectAction.Copy; enu < DataObjectAction.Menu; enu = (DataObjectAction)((int)enu << 1))
						{
							var name = Enum.GetName(typeof(DataObjectAction), enu);
							var format = GetFormat(wxDest, enu);
							var text = string.Format(format, docEventArgs.DestinationName);

							ContextMenu.Items.Add(
								new MenuItem()
								{
									Name = name,
									Header = text,
									Command = MenuCommand,
									CommandParameter = name
								});
						}

						// Create Cancel MenuItem
						ContextMenu.Items.Add(new Separator());
						var textc = string.Format(GetFormatCancel(wxDest), docEventArgs.DestinationName);
						ContextMenu.Items.Add(
								new MenuItem()
								{
									Name = "Cancel",
									Header = textc,
									Command = MenuCommand,
									CommandParameter = DataObjectAction.None.ToString()
								});
					}
					else
					{
						ContextMenu.Closed -= OnDropContextMenuClosed;
					}

					ContextMenu.Closed += OnDropContextMenuClosed;
					ContextMenu.IsOpen = true;

					return true;

				}
				else if (data is IDataObjectCompatible dest)
				{
					// Use the DataObjectCompatible object action handler on destination
					docEventArgs.Handled = false;
					dest.DataObjectDo(docEventArgs);
					ret = docEventArgs.Handled;

					// Use the DataObjectCompatible object action handler on source
					if (ret && !docEventArgs.SourceHandled &&
						docEventArgs.Source is IDataObjectCompatible source)
					{
						docEventArgs.Handled = false;
						docEventArgs.Direction = DataObjectDirection.From;
						source.DataObjectDo(docEventArgs);
					}

					cancelled = !docEventArgs.Handled;

				}
				else if (wxDest is ICollection collec)
				{
					// Handle collections automatically
					if (docEventArgs.DoOnCollection(collec) &&
						!docEventArgs.SourceHandled)
					{
						// Handle source collection
						docEventArgs.Handled = false;
						docEventArgs.Direction = DataObjectDirection.From;
						docEventArgs.DoOnCollection(collec);
					}

					cancelled = !docEventArgs.Handled;
				}
			}

			// Stop drag & drop operation
			if (docEventArgs != null &&
				docEventArgs.Method == DataObjectMethod.DragDrop)
			{
				StopDragDrop(cancelled);
			}

			return ret;
		}


		/// <summary>
		/// Retrieve text format to be used for a given Clipboard action on a FrameworkElement.
		/// </summary>
		/// <param name="elm">FrameworkElement</param>
		/// <param name="action">Clipboard action to be represented as text format</param>
		/// <returns>Text format.</returns>
		public static string GetFormat(FrameworkElement elm, DataObjectAction action)
		{
			string ret = null;
			switch (action)
			{
				case DataObjectAction.Copy:
					ret = GetFormatCopy(elm);
					break;
				case DataObjectAction.Move:
					ret = GetFormatMove(elm);
					break;
				case DataObjectAction.Link:
					ret = GetFormatLink(elm);
					break;
				case DataObjectAction.Menu:
					ret = GetFormatMenu(elm);
					break;
				case DataObjectAction.Open:
					ret = GetFormatOpen(elm);
					break;
				case DataObjectAction.None:
					ret = GetFormatCancel(elm);
					break;
			}

			return ret;
		}


		/// <summary>
		/// Complete any ongoing Clipboard operation when a Drop menu is closed. 
		/// This must be called by all Drop ContextMenu assigned to <see cref="CLipDragDRop.ContextMenuProperty"/>.
		/// </summary>
		/// <param name="sender">ContextMenu</param>
		/// <param name="e"></param>
		public static void OnDropContextMenuClosed(object sender, RoutedEventArgs e)
		{
			if (DragData != null)
			{
				Cancel();
			}
		}


		/// <summary>
		/// Start dragging detection from the application
		/// </summary>
		/// <param name="sender">Control when <see cref="EnableProperty"/> is set</param>
		/// <param name="e"></param>
		private static void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
		{
			LimeMsg.Debug("ClipDragDrop OnMouseButtonDown: {0}", sender);

			// Cancel previous dragging operation
			if (DragData != null)
			{
				Cancel();
			}

			// Get the click state
			bool rightClick;
			if (e.LeftButton == MouseButtonState.Pressed)
				rightClick = false;
			else if (e.RightButton == MouseButtonState.Pressed)
				rightClick = true;
			else
				return;


			var wxSender = (FrameworkElement)sender;

			// Retrieve Drag-source
			var wxSource = WPF.FindFromPoint<FrameworkElement>(wxSender, e.GetPosition(wxSender),
				(elm) => GetSource(elm) != null);

			if (wxSource != null)
			{
				// initialize the Drag and Drop
				DragData = new ClipDragDropContext()
				{
					Source = wxSource
				};

				DragContext = new DragDropContext()
				{
					RightClick = rightClick,
					MouseHit = e.GetPosition(wxSource)
				};

				// Track mouse move from now
				wxSource.PreviewMouseMove += OnMouseMove;
			}
		}

		/// <summary>
		/// Detect potential Drag that didn't happen and stop it
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnMouseButtonUp(object sender, MouseButtonEventArgs e)
		{
			// Cancel any ongoing Drag & Drop operations
			if (DragData != null)
			{
				Cancel();
			}
		}


		/// <summary>
		/// Detect and start dragging from the application
		/// </summary>
		/// <param name="sender">Control where <see cref="EnableProperty"/> is set</param>
		/// <param name="e"></param>
		private static void OnMouseMove(object sender, MouseEventArgs e)
		{
			LimeMsg.Debug("ClipDragDrop OnMouseMove: {0}", sender);

			var wxSender = (FrameworkElement)sender;

			if (DragData == null || DragIcon != null)
			{
				// Should never come there... but handle this just in case...
				wxSender.PreviewMouseMove -= OnMouseMove;
				return;
			}

			// Visual feedbacks on Source
			var pos = e.GetPosition(wxSender);
			var click = DragContext.RightClick ? e.RightButton : e.LeftButton;

			if (click == MouseButtonState.Pressed &&
				(Math.Abs(DragContext.MouseHit.X - pos.X) >= SystemParameters.MinimumHorizontalDragDistance ||
				  Math.Abs(DragContext.MouseHit.Y - pos.Y) >= SystemParameters.MinimumVerticalDragDistance))
			{
				// --- Start dragging ---

				// Stop monitoring this element for nouse move, the DragDrop.DoDragDrop will follow up
				wxSender.PreviewMouseMove -= OnMouseMove;

				// Get drag and drop source and its hierarchy
				if ( !TryGetDragDropElement(true, DragContext.MouseHit, wxSender, out var wxSource, 
					out var wxSourceDecoration, out var wxSourceItemsControl, out var idx) )
				{
					Cancel(); // unexpected exception
					return;
				}

				// Get default operation
				DragContext.DragDefaultOperation = GetDragDefaultOperation(wxSource);

				// Define the kind of source drag-action
				var operation = ClipDragDropOperations.DragDrop | ClipDragDropOperations.From | 
					(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? ClipDragDropOperations.Move :
					Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? ClipDragDropOperations.Copy :
					Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ? ClipDragDropOperations.Link :
					DragContext.RightClick ? ClipDragDropOperations.Menu :
					wxSourceItemsControl != null && wxSource != wxSourceItemsControl  ? ClipDragDropOperations.Move :
					(ClipDragDropOperations)DragContext.DragDefaultOperation);

				// Start Source dragging
				DragContext.Over = wxSender;
				DragData.Source = wxSource;
				DragData.SourceItemsControl = wxSourceItemsControl;

				var actions = CanClipDragDrop(operation | (ClipDragDropOperations.From - 1), wxSource, idx, true, wxSourceItemsControl);

				// Detect if dragging is enabled
				if (actions == DataObjectAction.None)
				{
					Cancel();
					return;
				}

				// Start decoration and drag icon
				DragIcon = new DragSystemIcon(wxSource, DataObjectCompatibleEventArgs.Data,
					DragContext.DragDropEffects, Scale, create: true);

				// Visual on source element
				DragDropSourceVisual( actions.HasFlag(DataObjectAction.Move) && operation.HasFlag(ClipDragDropOperations.Move));

				// Execute Operation
				if (!Execute(DragData, null))
				{
					Cancel();
					return;
				}

				// Start drag and drop
				try
				{
					DragDrop.DoDragDrop(DragData.Source, DataObjectCompatibleEventArgs.Data, DragContext.DragDropEffects);
				}
				finally
				{
					// Make sure this is over
					e.Handled = true;
					if (ContextMenu == null) Cancel();
				}
			}
			else
			{
				return; // Don't enable Drag & Drop yet
			}
		}


		/// <summary>
		/// Detect and start dragging from outside of the application
		/// </summary>
		/// <param name="sender">Control where <see cref="EnableProperty"/> is set</param>
		/// <param name="e"></param>
		private static void OnDragOver(object sender, DragEventArgs e)
		{
			var wxSender = (FrameworkElement)sender;

			if (DragData == null)
			{
				// Initiate Drag when Drag source is coming  from outside of the app
				DragData = new ClipDragDropContext();

				DragContext = new DragDropContext()
				{
					DragDefaultOperation = GetDragDefaultOperation(wxSender)
				};


				var operations = ClipDragDropOperations.DragDrop | ClipDragDropOperations.From | (
					(ClipDragDropOperations)e.AllowedEffects & (ClipDragDropOperations.From - 1));

				var actions = CanClipDragDrop(operations, e.Data, -1, register: true);

				// Detect if dragging is enabled
				if (actions == DataObjectAction.None)
				{
					Cancel();
					return;
				}

				// Keep the system visual Icon in the application
				DragIcon = new DragSystemIcon(wxSender, DataObjectCompatibleEventArgs.Data,
					DragContext.DragDropEffects, Scale, create: false);

				// Prepare for Window on top
				Time = DateTime.Now;

			}
			else if (DragData.Source == null)
			{
				// Define the kind of source drag-action
				HandleDestination(wxSender);

				// Continue Dragging of a dragged source from outside the application
				DragIcon.UpdatePosition();

				// Handle Window on top
				var wxwin = Window.GetWindow(wxSender);
				var disable = GetDisable(wxSender);
				if (!wxwin.IsActive)
				{
					if (!disable.HasFlag(ClipDragDropOperations.BringOnTop) && DateTime.Now > Time.AddMilliseconds(TriggerTime))
					{
						wxwin.Activate();
					}
				}
			}

			// Track over element
			DragContext.Over = wxSender;

			// When dragging from outside the application, we want to detect a drop, even on a pseudo
			// non-drop-able position, to end the dragging properly
			e.Effects = DragData.Source == null && DragContext.DragDropEffects == DragDropEffects.None ?
				DragDropEffects.Scroll : DragContext.DragDropEffects;

			e.Handled = true;

			//LimeMsg.Debug("ClipDragDrop OnDragOver: {0} - {1}", e.Effects, e.AllowedEffects);
			//LimeMsg.Debug("ClipDragDrop OnDragOver: {0} - {1}", sender, e.OriginalSource);
		}


		/// <summary>
		/// Stop handling the Dragging from outside the application, when over a non-drop-able location
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnDragLeave(object sender, DragEventArgs e)
		{
			//LimeMsg.Debug("ClipDragDrop OnDragLeave: {0} - {1}", sender, e.OriginalSource);
			if (DragData != null && DragData.Source == null && DragContext.Over != null)
			{
				// Locate mouse relative to the currently selected window
				if (WPF.GetCursorPos(out WPF.SysPoint spos))
				{
					var mpos = new Point(spos.x, spos.y);
					var wxRoot = PresentationSource.FromVisual(DragContext.Over).RootVisual as FrameworkElement;
					var pos = wxRoot.PointFromScreen(mpos);

					if (wxRoot.InputHitTest(pos) == null)
					{
						// Ouside a drop-able position, we can't detect when the dragging is ended by mouse
						// release, so we'd better stop handling the dragging from now
						LimeMsg.Debug("ClipDragDrop OnDragLeave: {0} - {1}", e.Effects, e.AllowedEffects);
						Cancel();
					}
				}
			}

			e.Handled = true;
		}



		/// <summary>
		/// Handle the drag over a destination
		/// </summary>
		/// <param name="sender">Control where <see cref="EnableProperty"/> is set</param>
		/// <param name="e"></param>
		private static void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
		{
			//LimeMsg.Debug("ClipDragDrop OnQueryContinueDrag: {0}", e.Source);

			var click = DragContext.RightClick ? DragDropKeyStates.RightMouseButton : DragDropKeyStates.LeftMouseButton;

			if (DragIcon == null || e.EscapePressed)
			{
				e.Action = DragAction.Cancel;
				StopDragDrop(cancel: DragData != null);
				return;
			}
			else if (e.KeyStates.HasFlag(click))
			{
				e.Action = DragAction.Continue;
			}
			else
			{
				e.Action = DragAction.Drop;
			}

			DragIcon?.UpdatePosition();
			HandleDestination(DragContext.Over);

			e.Handled = true;
		}

		/// <summary>
		/// Change the Mouse cursor to depict the Drag effect
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
		{
			if (DragIcon != null)
			{
				//LimeMsg.Debug("ClipDragDrop OnGiveFeedback: {0}", e.Effects);
				e.UseDefaultCursors = false;
				Mouse.OverrideCursor = e.Effects == DragDropEffects.None ? Cursors.No : null;
				e.Handled = true;
			}
		}



		/// <summary>
		/// Handle Drag over Destination effects
		/// </summary>
		/// <param name="sender"></param>
		private static void HandleDestination(FrameworkElement sender)
		{ 
			if (!WPF.GetCursorPos(out WPF.SysPoint spos))
			{
				// unexpected exception
				StopDragDrop(cancel: DragData != null); 
				return;
			}

			var mpos = new Point(spos.x, spos.y);
			var wxRoot = PresentationSource.FromVisual(sender).RootVisual as FrameworkElement;
			var pos = wxRoot.PointFromScreen(mpos);
			var wxhit = wxRoot.InputHitTest(pos);

			// Handle auto-scroll
			if (wxhit is DependencyObject dep)
			{
				var elm = dep as ScrollViewer ?? WPF.FindFirstParent<FrameworkElement>(dep);
				if (elm != null && !GetDisable(elm).HasFlag(ClipDragDropOperations.Scroll))
				{
					if (!(elm is ScrollViewer wxscroll))
					{
						wxscroll = WPF.FindFirstParent<ScrollViewer>(elm);
					}

					if (wxscroll != null)
					{
						var scrollpos = wxscroll.PointFromScreen(mpos);

						// Vertical direction

						double scrpos = scrollpos.Y;
						double scrsize = wxscroll.ActualHeight;
						double margin = scrsize > 600 ? 60 : scrsize / 10;

						if (scrpos < margin) // Top
						{
							// Scroll up
							wxscroll.ScrollToVerticalOffset(wxscroll.VerticalOffset - margin + scrpos);
						}
						else if (scrpos > wxscroll.ActualHeight - margin) // Bottom 
						{
							// Scroll down
							wxscroll.ScrollToVerticalOffset(wxscroll.VerticalOffset + scrpos - wxscroll.ActualHeight + margin);
						}

						// Horizontal direction

						scrpos = scrollpos.X;
						scrsize = wxscroll.ActualWidth;

						margin = scrsize > 600 ? 60 : scrsize / 10;

						if (scrpos < margin) // left
						{
							// Scroll left
							wxscroll.ScrollToHorizontalOffset(wxscroll.HorizontalOffset - margin + scrpos);
						}
						else if (scrpos > wxscroll.ActualWidth - margin) // right 
						{
							// Scroll right
							wxscroll.ScrollToHorizontalOffset(wxscroll.HorizontalOffset + scrpos - wxscroll.ActualWidth + margin);
						}

					}
				}
			}


			// Get drag and drop source and its hierarchy
			ClipDragDropOperations operation = ClipDragDropOperations.DragDrop | ClipDragDropOperations.To;

			if (TryGetDragDropElement(false, pos, wxRoot, out var wxDest,
				out var wxDestDecoration, out var wxDestItemsControl, out var idx))
			{
				// Make destination message
				operation |=
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? ClipDragDropOperations.Move :
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? ClipDragDropOperations.Copy :
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ? ClipDragDropOperations.Link :
						 DragContext.RightClick ? ClipDragDropOperations.Menu :
						 DragData.SourceItemsControl == wxDestItemsControl && 
							(wxDest == wxDestItemsControl || wxDest == DragData.Source)? ClipDragDropOperations.Move :
						 (ClipDragDropOperations)DragContext.DragDefaultOperation;
			}
			else if (wxhit == null)
			{
				// Make destination message when getting out of the application
				operation |= 
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? ClipDragDropOperations.Move :
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? ClipDragDropOperations.Copy :
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ? ClipDragDropOperations.Link :
						 DragContext.RightClick ? ClipDragDropOperations.Menu :
						 (ClipDragDropOperations)DragContext.DragDefaultOperation;

				LimeMsg.Debug("ClipDragDrop HandleDestination: off {0}", operation);
			}
			

			bool updateDragIcon = false;

			// Handle change in destinations (lazily, only if required)
			if (wxDest != DragContext.Destination || HandleDestinationAction != operation)
			{
				HandleDestinationAction = operation;

				// Reset visuals on previous destination
				if (DragContext.Destination != null)
				{
					ClipDragDropDestinationVisual(false, DragContext.Destination);
					//DebugAdorner.Hide(Data.Destination);
				}

				// Invalidate the HitZone
				DragContext.HitZone = null;

				//DebugAdorner.Show(wxDest);

				// Update the Destination Data
				DragContext.Destination = wxDest;

				var action = CanClipDragDrop(operation, wxDest, -1, register: true, wxItemsControl: wxDestItemsControl);

				LimeMsg.Debug("ClipDragDrop HandleDestination: {0} --> {1}", wxDest, DataObjectCompatibleEventArgs.Action);

				// Visual feedbacks
				updateDragIcon = true;
				if (wxDest != null && action != DataObjectAction.None)
				{
					ClipDragDropDestinationVisual(true, DragContext.Destination);
					var move = action == DataObjectAction.Move && operation.HasFlag(ClipDragDropOperations.Move);
					if (wxDestItemsControl != DragData.SourceItemsControl || move)
					{
						DragDropSourceVisual(move);
					}
				}
			}

			// Handle change in DestinationItemsControl (lazily, only if required)
			if (wxDestItemsControl != DragContext.DestinationItemsControl)
			{
				// Reset visuals on previous destination
				DragDropInCollection(ClipDragDropOperations.DragDrop);

				// Update the destination Items Control
				DragContext.DestinationItemsControl = wxDestItemsControl;

				// Visual feedback
				updateDragIcon = true;
				if (wxDestItemsControl != null)
				{
					var dpos = wxDestItemsControl.PointFromScreen(mpos);
					DragDropInCollection(operation, dpos);
				}

			}
			else if (wxDestItemsControl != null && wxDestItemsControl == DragContext.DestinationItemsControl)
			{
				// Update destination Items Control (lazily, only if required)
				var dpos = wxDestItemsControl.PointFromScreen(mpos);

				if (DragContext.HitZone == null || !DragContext.HitZone.Value.Contains(dpos))
				{
					updateDragIcon = true;
					DragDropInCollection(operation, dpos);
				}
			}

			// Update DragIcon Effect
			if (updateDragIcon)
			{
				DropImageType type = DropImageType.Invalid;
				string format = null;
				var effects = DragContext.DragDropEffects;

				if (effects != DragDropEffects.None)
				{
					format = GetFormat(wxDest ?? sender, DataObjectCompatibleEventArgs.Action);

					switch (DataObjectCompatibleEventArgs.Action)
					{
						case DataObjectAction.Copy:
							type = DropImageType.Copy;
							break;
						case DataObjectAction.Move:
							type = DropImageType.Move;
							break;
						case DataObjectAction.Link:
							type = DropImageType.Link;
							break;
						case DataObjectAction.Menu:
							type = DropImageType.Label;
							break;
						case DataObjectAction.Open:
							type = DropImageType.Label;
							break;
					}
				}

				LimeMsg.Debug("ClipDragDrop HandleDestination: updateDragIcon {0} --> {1}", effects, type);

				DragIcon?.UpdateEffects(effects, type, format, DataObjectCompatibleEventArgs.DestinationName);
			}
		}


		/// <summary>
		/// Handle Drop on a destination
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnDrop(object sender, DragEventArgs e)
		{
			LimeMsg.Debug("ClipDragDrop OnDrop: {0}", DataObjectCompatibleEventArgs?.Action);
			if (DataObjectCompatibleEventArgs == null) return;
			e.Handled = Drop(DataObjectCompatibleEventArgs.Action);
		}





		/// <summary>
		/// Handle the visual effect on a drag source
		/// </summary>
		/// <param name="enable">indicate whether current source has visual effect</param>
		private static void DragDropSourceVisual(bool enable)
		{
			var wxSource = DragData.Source;
			if (wxSource == null) return;

			var animate = GetAnimate(wxSource);

			// Get container
			var wxSourceDecoration = wxSource;
			if (VisualTreeHelper.GetParent(wxSourceDecoration) is ContentPresenter pre) wxSourceDecoration = pre;
			var scaleTransform = wxSourceDecoration.RenderTransform as ScaleTransform;

			if (enable)
			{
				if (scaleTransform == null)
				{
					// Visual feedback on drag source (make it vanish)
					scaleTransform = new ScaleTransform(0, 0,
						wxSourceDecoration.ActualWidth / 2.0 + wxSourceDecoration.Margin.Left,
						wxSourceDecoration.ActualHeight / 2.0 + wxSourceDecoration.Margin.Top);
					wxSourceDecoration.RenderTransform = scaleTransform;

					if (animate)
					{
						var anim = new DoubleAnimation(1, 0, AnimDuration, new FillBehavior())
						{
							EasingFunction = new PowerEase()
						};
						scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
						scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
					}
				}
			}
			else
			{
				// Restore Dragged source item size and position

				if (animate && scaleTransform != null)
				{
					var animX = new DoubleAnimation(Scale, 1, AnimDuration, new FillBehavior())
					{
						BeginTime = AnimDuration.TimeSpan // start after dragAdorner animation
					};

					animX.Completed += (sender, e) =>
					{
						wxSourceDecoration.RenderTransform = null;
					};

					scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
					scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animX);
				}
				else
				{
					wxSourceDecoration.RenderTransform = null;
				}

			}

		}



		/// <summary>
		/// Handle the visual effect on a drop destination
		/// </summary>
		/// <param name="over">indicate whether current destination has visual effect</param>
		private static void ClipDragDropDestinationVisual(bool over, FrameworkElement dest)
		{
			var animate = GetAnimate(dest);

			if (over)
			{
				VisualStateManager.GoToState(dest, "Focused", animate);
			}
			else if (!dest.IsFocused)
			{
				VisualStateManager.GoToState(dest, "Normal", animate);
			}


			//double rescale = 1.0;
			//ScaleTransform transform = null;
			//if (wxDest.RenderTransform is ScaleTransform tr)
			//{
			//	transform = tr; // Recycle transform
			//}

			//var centerX = wxDest.ActualWidth / 2.0 + wxDest.Margin.Left;
			//var centerY = wxDest.ActualHeight / 2.0 + wxDest.Margin.Top;

			//if (transform == null)
			//{
			//	if (rescale == 1.0)
			//	{
			//		// Don't change anything yet if the object is not to be rescaled
			//		animate = false;
			//	}
			//	else
			//	{
			//		var factor = animate ? 1.0 : rescale;
			//		transform = new ScaleTransform(factor, factor, centerX, centerY);
			//		wxDest.RenderTransform = transform;
			//	}
			//}

			//if (animate)
			//{
			//	var animS = new DoubleAnimation(rescale, AnimDuration, new FillBehavior());

			//	if (rescale == 1.0)
			//	{
			//		animS.Completed += (lsender, le) => wxDest.RenderTransform = null;
			//	}

			//	transform.BeginAnimation(ScaleTransform.ScaleXProperty, animS);
			//	transform.BeginAnimation(ScaleTransform.ScaleYProperty, animS);
			//}

		}



		/// <summary>
		/// Make a visual feedback on the destination ItemsControl.
		/// </summary>
		/// <param name="operation">Describe the requested actions on destination</param>
		/// <param name="hit">Mouse hit point relative to <see cref="DragData.DestinationItemsControl"/></param>
		private static void DragDropInCollection (ClipDragDropOperations operation, Point hit = new Point())
		{
#if DEBUG
			// Setup debugging helpers
			bool debugShowItems = false;
			bool debugShowHitZone = false;
#endif

			// Data.Destination may be updated in this method on matching hit
			var wxDestItemsControl = DragContext.DestinationItemsControl;

			// Invalid destination
			if (wxDestItemsControl == null) return;

			var wxSourceItemsControl = DragData.SourceItemsControl;
			var wxSource = DragData.Source;

			// Define whether insertion actions should be enabled on destination
			var action = CanClipDragDrop(operation, wxDestItemsControl, 0, register: false, wxItemsControl: wxDestItemsControl);
			bool enableDrop = action != DataObjectAction.None;

			Point zeroPoint = new Point(0, 0);
			var compScale = 1 - Scale;

			var itemgen = wxDestItemsControl.ItemContainerGenerator;

			var wxscroll = WPF.FindFirstParent<ScrollViewer>(wxDestItemsControl);
			var wxcontainer = (FrameworkElement)wxscroll ?? wxDestItemsControl;

			var animEnabled = GetAnimate(wxDestItemsControl);

			var wxHit = VisualTreeHelper.HitTest(wxDestItemsControl, hit)?.VisualHit;

			// If object is inserted somewhere, these variables will change
			int newIndex = -1;

			// A new hit zone will be created
			DragContext.HitZone = null;
			Rect nonHit = WPF.GetVisibleArea(wxDestItemsControl, wxDestItemsControl);

			for (var idx = 0; idx < itemgen.Items.Count; idx++)
			{
				if (!(itemgen.ContainerFromIndex(idx) is ContentPresenter wxCont)) break;

				var wxDestItem = WPF.FindFirstChild<FrameworkElement>(wxCont);
				if (wxDestItem == null) continue;

				var visible = WPF.IsFullyOrPartiallyVisible(wxCont, wxcontainer);
				var animate = animEnabled && visible;

				
				// Do the rendering
				if (!enableDrop && !animate || !visible)
				{
					wxDestItem.RenderTransform = null;
				}
				else
				{
					var rescale = enableDrop ? Scale : 1.0;
					ScaleTransform transform = null;
					if (wxDestItem.RenderTransform is ScaleTransform tr)
					{
						transform = tr; // Recycle transform
					}

					// Retrieve rendered zone geometry
					var childTransform = wxDestItem.TransformToAncestor(wxDestItemsControl);
					var render = childTransform.TransformBounds(
						new Rect(0, 0, wxDestItem.ActualWidth, wxDestItem.ActualHeight));

					// Retrieve container zone geometry (including its margins)
					var contTransform = wxCont.TransformToAncestor(wxDestItemsControl);
					var marginX = wxCont.Margin.Left + wxCont.Margin.Right;
					var marginY = wxCont.Margin.Top + wxCont.Margin.Bottom;
					var layout = contTransform.TransformBounds(
						new Rect(-wxCont.Margin.Left, -wxCont.Margin.Right, 
						          wxCont.ActualWidth + marginX, wxCont.ActualHeight + marginY));

					var elementWidth = wxDestItem.ActualWidth + wxDestItem.Margin.Left + wxDestItem.Margin.Right;
					var centerX = wxDestItem.ActualWidth / 2.0 + wxDestItem.Margin.Left;

					var elementHeight = wxDestItem.ActualHeight + wxDestItem.Margin.Top + wxDestItem.Margin.Bottom;
					var centerY = wxDestItem.ActualHeight / 2.0 + wxDestItem.Margin.Top;

					if (enableDrop && visible)
					{

						// Retrieve the area that the icon controls (its neighbourhood)
						// This will overlap the neighbour icon-zones

						var addedX = layout.Width - render.Width;
						var addedY = layout.Height - render.Height;
						var offsetX = render.Right - layout.Right;
						var offsetY = render.Bottom - layout.Bottom;

						if (transform != null)
						{
							// reverse the transform effects (offset) on the zone position

							if (transform.CenterX < centerX - 1.0)
								offsetX = -wxDestItem.Margin.Left * Scale;
							else if (transform.CenterX > centerX + 1.0)
								offsetX = -wxDestItem.ActualWidth * compScale - (wxDestItem.Margin.Left + wxDestItem.Margin.Right) * Scale;

							if (transform.CenterY < centerY - 1.0)
								offsetY = -wxDestItem.Margin.Top * Scale;
							else if (transform.CenterY > centerY + 1.0)
								offsetY = -wxDestItem.ActualHeight * compScale - (wxDestItem.Margin.Top + wxDestItem.Margin.Bottom) * Scale;
						}

						var iconzone = new Rect(
							layout.X + offsetX, layout.Y + offsetY,
							layout.Width + addedX, layout.Height + addedY);

#if DEBUG
						if (debugShowItems) DebugAdorner.Show(wxDestItem, iconzone, wxDestItemsControl, null);
#endif

						//Lime.LimeMsg.Debug("ClipDragDrop VisualEffectDest: {0} --> {1}", render, iconzone);

						var spoton = wxHit != null &&
									 wxDestItem == WPF.FindFirstParent<DependencyObject>(wxHit,
													(helm) => helm == wxDestItem || helm == wxDestItemsControl);

						if (spoton)
						{
							if (wxDestItem == wxSource)
							{
								newIndex = idx;
							}
							else if (wxDestItem == DragContext.Destination)
							{
								rescale = Scale / 2 + 0.5;
							}

							DragContext.HitZone = render;

						}
						else if (iconzone.Contains(hit))
						{
							var index = idx;
							var moveX = centerX;
							var moveY = centerY;

							if (hit.X < render.Left)
							{
								moveX = elementWidth;
								iconzone.Width = render.Left - iconzone.Left;
							}
							else if (hit.X > render.Right)
							{
								index++;
								moveX = 0;
								iconzone.Width = iconzone.Right - render.Right;
								iconzone.X = render.Right;
							}
							else if (hit.Y < render.Top)
							{
								moveY = elementHeight;
								iconzone.Height = render.Top - iconzone.Top;
							}
							else if (hit.Y > render.Bottom)
							{
								index++;
								moveY = 0;
								iconzone.Height = iconzone.Bottom - render.Bottom;
								iconzone.Y = render.Bottom;
							}

							var act = CanClipDragDrop(operation, wxDestItemsControl, index, register: false, wxItemsControl: wxDestItemsControl);
							if (act != DataObjectAction.None)
							{
								// Object can be inserted here
								newIndex = index;
								centerX = moveX;
								centerY = moveY;
							}

							if (DragContext.HitZone != null) iconzone.Intersect(DragContext.HitZone.Value);
							DragContext.HitZone = iconzone;

						}
						else if (nonHit.IntersectsWith(iconzone))
						{
							// Reduce the non-Hit zone to not contain the iconzone
							iconzone.Intersect(nonHit);

							// Horizontal reduction
							if (hit.X < iconzone.Left)
							{
								nonHit.Width = iconzone.Left - nonHit.Left;
							}
							else if (hit.X > iconzone.Right)
							{
								nonHit.Width = nonHit.Right - iconzone.Right;
								nonHit.X = iconzone.Right;
							}

							// Vertical reduction
							if (hit.Y < iconzone.Top)
							{
								nonHit.Height = iconzone.Top - nonHit.Top;
							}
							else if (hit.Y > iconzone.Bottom)
							{
								nonHit.Height = nonHit.Bottom - iconzone.Bottom;
								nonHit.Y = iconzone.Bottom;
							}

						}

					}

					if (transform == null)
					{
						if (rescale == 1.0)
						{
							// Don't change anything yet if the object is not to be rescaled
							animate = false;
						}
						else
						{
							var factor = animate ? 1.0 : rescale;
							transform = new ScaleTransform(factor, factor, centerX, centerY);
							wxDestItem.RenderTransform = transform;
						}
					}

					if (animate)
					{
						var animS = new DoubleAnimation(rescale, AnimDuration, new FillBehavior());
						var animX = new DoubleAnimation(centerX, AnimDuration, new FillBehavior());
						var animY = new DoubleAnimation(centerY, AnimDuration, new FillBehavior());

						if (!enableDrop)
						{
							animS.Completed += (lsender, le) =>  wxDestItem.RenderTransform = null;
						}

#if DEBUG
						if (enableDrop && debugShowItems)
						{
							animS.Completed += (lsender, le) => DebugAdorner.Show(wxDestItem);
						}
#endif
						transform.BeginAnimation(ScaleTransform.ScaleXProperty, animS);
						transform.BeginAnimation(ScaleTransform.ScaleYProperty, animS);
						transform.BeginAnimation(ScaleTransform.CenterXProperty, animX);
						transform.BeginAnimation(ScaleTransform.CenterYProperty, animY);
					}
				}
			}

			// Validate the insertion
			if (newIndex>=0)
			{
				CanClipDragDrop(operation, wxDestItemsControl, newIndex, register: true, wxItemsControl: wxDestItemsControl);
			}

#if DEBUG
			if (debugShowHitZone)
			{
				if (DragContext.HitZone == null)
					DebugAdorner.Hide(wxDestItemsControl);
				else
					DebugAdorner.Show(wxDestItemsControl, DragContext.HitZone);
			}
#endif

			// Set the non-hit zone as a hit-zone (zone where there is no need to recompute
			// the animations and hit)
			if (DragContext.HitZone == null)
			{
				DragContext.HitZone = nonHit;
			}

			LimeMsg.Debug("ClipDragDrop DragDropInCollection: {0} --> {1}", DragContext.DestinationItemsControl, operation);

		}


		/// <summary>
		/// Open the Clipboard context menu
		/// </summary>
		/// <param name="sender">FrameworkElement with Enable attached property set</param>
		/// <param name="e"></param>
		private static void OnClipboardContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			LimeMsg.Debug("ClipDragDrop OnContextMenuOpening: {0} --> {1}", sender, e.OriginalSource);

			var wxsource = e.OriginalSource as UIElement;

			// Check Menu possibility
			if (wxsource is FrameworkElement elm || (elm = WPF.FindFirstParent<FrameworkElement>(wxsource)) != null)
			{
				if ((GetDisable(elm) & (ClipDragDropOperations.ClipboardMenu | ClipDragDropOperations.Clipboard)) != 0)
				{
					e.Handled = false;
					return;
				}
			}

			// Retrieve the target of the menu
			if (!TryGetClipboardElement(true, true, wxsource, 
				out var wxTarget, out var wxDecoration, out var wxItemControl, out var idx) ||
				(GetDisable(wxTarget) & (ClipDragDropOperations.ClipboardMenu | ClipDragDropOperations.Clipboard)) != 0)
			{
				e.Handled = false;
				return;
			}

			// Create Menu
			// Generate the menu lazily
			var wxmenu = new ContextMenu()
			{
				Placement = PlacementMode.Bottom
			};
			wxmenu.Closed += OnClipboardContextMenuClosed;

			var commands = new List<RoutedUICommand>()
				{
					ApplicationCommands.Cut,
					ApplicationCommands.Copy,
					ApplicationCommands.Paste
				};

			if (!GetDisable(wxTarget).HasFlag(ClipDragDropOperations.Delete))
			{
				commands.Add(ApplicationCommands.Delete);
			}

			foreach (var cmd in commands)
			{
				var mitem = new MenuItem()
				{
					Header = cmd.Name,
					Command = cmd,
					CommandParameter = true
				};
				mitem.SetBinding(MenuItem.CommandTargetProperty, "");

				if (cmd== ApplicationCommands.Delete)
				{
					wxmenu.Items.Add(new Separator());
				}

				wxmenu.Items.Add(mitem);
			}


			if (e.CursorLeft == -1 && e.CursorTop == -1)
			{
				// Place bellow the focused element
				wxmenu.PlacementTarget = wxTarget;
				wxmenu.PlacementRectangle = new Rect(wxTarget.ActualWidth * 0.6, wxTarget.ActualHeight * 0.6, 0 ,0);
			}
			else
			{
				// Place on Mouse position
				wxmenu.PlacementTarget = wxsource;
				wxmenu.PlacementRectangle = new Rect(e.CursorLeft, e.CursorTop, 0, 0);
			}

			wxmenu.DataContext = wxTarget;

			wxmenu.IsOpen = true;
			e.Handled = true;

			// Update visual
			ClipDragDropDestinationVisual(true, wxTarget);

		}

		/// <summary>
		/// Make sure the visual is updated after menu closure
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnClipboardContextMenuClosed(object sender, RoutedEventArgs e)
		{
			var wxmenu = (ContextMenu)sender;
			ClipDragDropDestinationVisual(false, wxmenu.DataContext as FrameworkElement);
		}



		/// <summary>
		/// Handle the ClipboardCanExecute and ClipboardExecuted functions
		/// </summary>
		/// <param name="sender">FrameworkElement with Enable attached property set</param>
		/// <param name="e">CanExecuteRoutedEventArgs</param>
		private static void ClipboardHandler(object sender, RoutedEventArgs e)
		{
			LimeMsg.Debug("ClipDragDrop ClipboardHandler: {0} --> {1}", sender, e.OriginalSource);
			var wxsender = (FrameworkElement)sender;
			var wxdest = e.OriginalSource as FrameworkElement;

			// Get command
			ICommand cmd;
			bool menu = false;
			bool execute;
			if (e is CanExecuteRoutedEventArgs ec)
			{
				execute = false;
				cmd = ec.Command;
				if (ec.Parameter is bool b) menu = b;
			}
			else if (e is ExecutedRoutedEventArgs ee)
			{
				execute = true;
				cmd = ee.Command;
				if (ee.Parameter is bool b) menu = b;
			}
			else
			{
				throw new InvalidOperationException();
			}

			// Readjust destination item to focused element in case of keyboard input
			if (!menu && wxdest == sender && GetEnable(wxdest) && 
				FocusManager.GetFocusedElement(wxsender) is FrameworkElement elm)
			{
				wxdest = elm;
			}

			ClipDragDropOperations operation;

			if (cmd == ApplicationCommands.Paste)
			{
				operation = ClipDragDropOperations.Clipboard | ClipDragDropOperations.To;

				// Retrieve data to get the actual opeation (cut or copy)
				IDataObject data = Clipboard.GetDataObject();
				var stream = (MemoryStream)data.GetData("Preferred DropEffect", true);
				if (stream != null)
				{
					int flag = stream.ReadByte();
					operation |=
						flag == 2 ? ClipDragDropOperations.Move :
						flag == 5 ? ClipDragDropOperations.Copy :
						ClipDragDropOperations.None;
				}
				else
				{
					operation |= ClipDragDropOperations.Copy;
				}

			}
			else
			{
				operation = ClipDragDropOperations.Clipboard | ClipDragDropOperations.From |
				(cmd == ApplicationCommands.Cut ? ClipDragDropOperations.Move :
				cmd == ApplicationCommands.Copy ? ClipDragDropOperations.Copy :
				cmd == ApplicationCommands.Delete ? ClipDragDropOperations.Delete | ClipDragDropOperations.Move :
				ClipDragDropOperations.None);
			}

			if (TryGetClipboardElement(
				operation.HasFlag(ClipDragDropOperations.From), 
				operation.HasFlag(ClipDragDropOperations.To), 
				wxdest, 
				out var wxTarget, out var wxDecoration, out var wxItemControl, out var index) )
			{

				// Re-arrange the focus in clipboard paste operations
				if (operation.HasFlag(ClipDragDropOperations.To))
				{
					if (!menu && index >= 0 &&
						TryGetClipboardElement(false, true, wxItemControl,
							out var wxTarget2, out var wxDecoration2, out var wxItemControl2, out var index2) &&
						wxTarget2 == wxItemControl)
					{
						wxTarget = wxTarget2;
						wxDecoration = wxDecoration2;
						wxItemControl = wxItemControl2;
					}
					else
					{
						index = -1;
					}
				}

				var prevClipData = ClipData;

				// Update visual
				if (ClipData != null && execute && !operation.HasFlag(ClipDragDropOperations.Delete))
				{
					ClipboardSourceVisual(false);
				}

				var action = CanClipDragDrop(operation, wxTarget, index, register: execute, wxItemsControl: wxItemControl);

				e.Handled = true;

				if (e is CanExecuteRoutedEventArgs ec2)
				{
					ec2.CanExecute = action != DataObjectAction.None;
				}
				else if (e is ExecutedRoutedEventArgs ee && action != DataObjectAction.None)
				{
					// Execute

					// Handle copy/cup to clipboard
					if (operation.HasFlag(ClipDragDropOperations.From))
					{
						// Special case for Delete
						if (operation.HasFlag(ClipDragDropOperations.Delete))
						{
							Execute(ClipData, null);
							ClipData = prevClipData;
							return;
						}

						IDataObject dataObject = ClipData.DataObjectCompatibleEventArgs.Data;
						if (dataObject != null && dataObject.GetDataPresent(DataFormats.FileDrop))
						{
							// Create system clipboard cut/copy code
							byte code =
								action == DataObjectAction.Move ? (byte)2 : // cut
								action == DataObjectAction.Copy ? (byte)5 : // copy
								(byte)0;

							if (code != 0)
							{
								try
								{
									var memo = new MemoryStream(4);
									var bytes = new byte[] { code, 0, 0, 0 };
									memo.Write(bytes, 0, bytes.Length);
									dataObject.SetData("Preferred DropEffect", memo);
								}
								catch { }
							}
						}

						// Set Clipboard data
						Clipboard.SetDataObject(dataObject);

						// Visual feedback
						ClipboardSourceVisual(true);

					}

					// Watch for changes in clipboard from now on
					ClipboardWatcher(wxsender);

					// Execute the object operation
					Execute(ClipData, wxTarget);

					// Invalidate the clipboard after a cut/paste
					if (action == DataObjectAction.Move && operation.HasFlag(ClipDragDropOperations.To))
					{
						ClipData = null;
						Clipboard.Clear();
					}

				}

			}

		}

		/// <summary>
		/// Check if the Clipboard Cut/Copy/Paste operation is allowed
		/// </summary>
		/// <param name="sender">FrameworkElement with Enable attached property set</param>
		/// <param name="e">CanExecuteRoutedEventArgs</param>
		private static void ClipboardCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.OriginalSource is FrameworkElement obj && obj.DataContext is LimeItem item)
			{
				LimeMsg.Debug("ClipDragDrop ClipboardCanExecute: {0} --> {1}", sender, item.Name);
			}
			else
			{
				LimeMsg.Debug("ClipDragDrop ClipboardCanExecute: {0} --> {1}", sender, e.OriginalSource);
			}
			ClipboardHandler(sender, e);
		}


		/// <summary>
		/// Execute the Clipboard Cut/Copy/Paste operation
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void ClipboardExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			LimeMsg.Debug("ClipDragDrop ClipboardExecuted: {0} --> {1}", sender, e.OriginalSource);
			ClipboardHandler(sender, e);
		}


		/// <summary>
		/// Handle the visual effect on a Clipboard source
		/// </summary>
		/// <param name="enable">indicate whether current source has visual effect</param>
		private static void ClipboardSourceVisual(bool enable)
		{
			const double factor = 0.4;
			var wxSource = ClipData.Source;
			if (wxSource == null || ClipData.DataObjectCompatibleEventArgs == null) return;
			var action = ClipData.DataObjectCompatibleEventArgs.Action;
			if (action != DataObjectAction.Move) return;

			// Get container
			var wxSourceDecoration = wxSource;
			if (VisualTreeHelper.GetParent(wxSourceDecoration) is ContentPresenter pre) wxSourceDecoration = pre;

			var opacity = wxSourceDecoration.Opacity;

			if (enable)
			{
				// Dim the element
				wxSourceDecoration.SetCurrentValue(UIElement.OpacityProperty, opacity * factor);
			}
			else
			{
				// Restore the element
				wxSourceDecoration.SetCurrentValue(UIElement.OpacityProperty, opacity / factor);
			}

		}


		/// <summary>
		/// Retrieve Clipboard element and its hierarchy
		/// </summary>
		/// <param name="matchSource">True if we are looking for a clipboard-source</param>
		/// <param name="matchDestination">True if we are looking for a clipboard-destination</param>
		/// <param name="sender">Focused element</param>
		/// <param name="element">element handling the the clipboard operation</param>
		/// <param name="elementDecoration">container handling the clipboard operation</param>
		/// <param name="elementItemsControl">parent ItemsControl handling the clipboard operation</param>
		/// <param name="idx">position of the element in the <see cref="elementItemsControl"/> (-1 if none) </param>
		/// <returns></returns>
		private static bool TryGetClipboardElement(
			bool matchSource,
			bool matchDestination,
			DependencyObject sender,
			out FrameworkElement element, out FrameworkElement elementDecoration,
			out ItemsControl elementItemsControl, out int idx)
		{
			elementItemsControl = null;
			idx = -1;
			element = null;

			if (sender == null)
			{
				elementDecoration = null;
				return false;
			}


			// Find element
			var dep = sender;
			while (dep != null)
			{
				if (dep is FrameworkElement elm)
				{
					if (matchSource)
					{
						if (GetSource(elm) != null)
						{
							// Found matching element
							element = elm;
							break;
						}
					}

					if (matchDestination)
					{
						if (GetDestination(elm) != null)
						{
							// Found matching element
							element = elm;
							break;
						}
					}

					// Forbid clipboard menu on some controls and do a pseudo bubbling for menu
					if (matchDestination && matchSource && (
						elm is ScrollBar || elm is TextBoxBase || elm.ContextMenu != null))
					{
						elementDecoration = null;
						return false;
					}
				}

				dep = VisualTreeHelper.GetParent(dep);
			}

			// default decoration
			elementDecoration = element;

			if (element == null)
			{
				return false;
			}

			if (element is ItemsControl ctr)
			{
				// item is itself an ItemsControl
				elementItemsControl = ctr;
			}
			else if (VisualTreeHelper.GetParent(elementDecoration) is ContentPresenter wxPresenter)
			{
				// Find out whether the element is an item in an ItemsControl
				elementDecoration = wxPresenter;
				var wxItemsControl = WPF.FindFirstParent<ItemsControl>(element);
				if (wxItemsControl != null)
				{
					var itemgen = wxItemsControl.ItemContainerGenerator;
					try
					{
						for (var i = 0; i < itemgen.Items.Count; i++)
						{
							var wxCont = itemgen.ContainerFromIndex(i) as ContentPresenter;
							if (wxCont == wxPresenter)
							{
								elementItemsControl = wxItemsControl;
								idx = i;
								break;
							}
						}
					}
					catch { }
				}
			}

			return true;

		}

		/// <summary>
		/// Retrieve Drag & Drop element and its hierarchy from a mouse position
		/// </summary>
		/// <param name="isSource">True if we are looking for a drag-source, false for drop-destination</param>
		/// <param name="hit">mouse hit coordinates relative to <see cref="sender"/></param>
		/// <param name="sender">Mouse relative reference</param>
		/// <param name="element">element handling the Drag and drop</param>
		/// <param name="elementDecoration">container handling the drag and drop</param>
		/// <param name="elementItemsControl">parent ItemsControl handling the drag and drop</param>
		/// <param name="idx">position of the element in the <see cref="elementItemsControl"/> (-1 if none) </param>
		/// <returns></returns>
		private static bool TryGetDragDropElement(
		bool isSource,
		Point hit, FrameworkElement sender,
		out FrameworkElement element, out FrameworkElement elementDecoration,
		out ItemsControl elementItemsControl, out int idx)
		{
			var wxsource = sender?.InputHitTest(hit) as FrameworkElement;

			// Check Drag & Drop possibility
			if (wxsource != null || (wxsource = WPF.FindFirstParent<FrameworkElement>(wxsource)) != null)
			{
				if ((GetDisable(wxsource) & ClipDragDropOperations.DragDrop) != 0)
				{
					element = null;
					elementDecoration = null;
					elementItemsControl = null;
					idx = -1;
					return false;
				}
			}

			// Use more generic TryGetClipboardElement function
			bool ret = TryGetClipboardElement(
				isSource, !isSource, wxsource,
				out element, out elementDecoration, out elementItemsControl, out idx);


			// Self-check will try to detect problems in the drag/drop-element and its parents
#if DEBUG
			if (element == null)
			{
				return false;
			}

			DependencyObject wxobj = element;
			while (wxobj != null)
			{
				if (wxobj is Control wxctrl)
				{
					string msg = null;

					// Check correct use of Background
					if (GetDestination(wxctrl) != null)
					{
						var background = wxctrl.Background;

						// Try to get background of panel if ItemsControl
						if (background == null && wxctrl is ItemsControl wxitems)
						{
							var wxpan = WPF.FindFirstChild<Panel>(wxitems);
							background = wxpan?.Background;

							if (background == null)
							{
								msg = "This Control has " + nameof(ClipDragDrop) +
									  ".Destination attached-property set," + Environment.NewLine +
									  "but cannot handle it because its Background is null";
							}
						}
					}

					// Check correct use of ClipDragDrop.Destination
					if (GetDestination(wxctrl) != null && !wxctrl.AllowDrop)
					{
						msg = "This Control has " + nameof(ClipDragDrop) +
							".Destination attached-property set," + Environment.NewLine +
							"but is not drop-able because AllowDrop=false";
					}

					// Check correct use of ClipDragDrop.Enable
					if (GetDestination(wxctrl) != null || GetSource(wxctrl) != null)
					{
						var root = WPF.FindFirstParent<FrameworkElement>(wxctrl, (elm) => GetEnable(elm));

						if (root == null)
						{
							msg = "This Control has a " + nameof(ClipDragDrop) +
								" attached-property set," + Environment.NewLine +
								"but there is no parent with " + nameof(ClipDragDrop) +
								".Enable=true to handle it";
						}
					}


					// Problem found
					if (msg != null)
					{
						var severity = GetSelfCheckSeverity(wxctrl);
						msg = String.Format(
								"[" + nameof(ClipDragDrop) + ":SelfCheckSeverity={0}] {1}: {2}",
								severity, msg, wxctrl.Name);

						switch (severity)
						{
							case ClipDragDropSelfCheckSeverity.Warning:
								System.Diagnostics.Debug.WriteLine(msg);
								DebugAdorner.Show(wxctrl, msg, Colors.Red);
								break;

							case ClipDragDropSelfCheckSeverity.Error:
								throw new Exception(msg);
						}
					}

				}

				wxobj = VisualTreeHelper.GetParent(wxobj);
			}
#endif

			return ret;
		}


		/// <summary>
		/// Define and track whether a Clipboard operation can be applied on data (element)
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="data"></param>
		/// <param name="index"></param>
		/// <param name="register"></param>
		/// <param name="wxItemsControl"></param>
		/// <returns>DragDropEffects</returns>
		private static DataObjectAction CanClipDragDrop(ClipDragDropOperations operation, object data,
			int index, bool register, ItemsControl wxItemsControl = null)
		{
			bool start = operation.HasFlag(ClipDragDropOperations.From);

			var method =
				operation.HasFlag(ClipDragDropOperations.Clipboard) ? DataObjectMethod.Clipboard :
				operation.HasFlag(ClipDragDropOperations.DragDrop) ? DataObjectMethod.DragDrop :
				DataObjectMethod.None;

#if DEBUG
			if (method == DataObjectMethod.None) throw new InvalidProgramException();
#endif

			if (data is FrameworkElement elm)
			{
				operation &= ~GetDisable(elm);
				if (start)
					data = GetSource(elm) ?? elm.DataContext;
				else
					data = GetDestination(elm) ?? elm.DataContext;
			}
			else
			{
				elm = null;
			}

			var action = (DataObjectAction)(operation & (ClipDragDropOperations.From - 1));

			DataObjectCompatibleEventArgs docEventArgs;

			if (start)
			{
				IDataObject dataObject = null;
				if (data is IDataObject ido)
				{
					dataObject = ido;
				}
				else if (data is IDataObjectCompatible sdoc)
				{
					dataObject = sdoc.GetDataObject(method);
				}
				else
				{
					dataObject = new DataObject(data.GetType(), data);
				}

				// Start Clipboard or Drag & Drop operation
				docEventArgs = new DataObjectCompatibleEventArgs(
					method: method,
					action: DataObjectAction.None,
					preliminary: !operation.HasFlag(ClipDragDropOperations.Delete),
					source: data,
					data: dataObject,
					sourceParent: wxItemsControl?.DataContext,
					sourceCollection: wxItemsControl?.ItemsSource,
					sourceIndex: index
					);

				if (data is IDataObjectCompatible idoc)
				{
					int matches = 0;


					for (var i = DataObjectAction.Copy; i <= DataObjectAction.Open; i = (DataObjectAction)((int)i << 1))
					{
						if ((action & i) != 0)
						{
							matches++;
							docEventArgs.Handled = false;
							docEventArgs.Action = i;
							idoc.DataObjectCanDo(docEventArgs);
							if (!docEventArgs.Handled) operation &= ~(ClipDragDropOperations)i;
						}
					}

					docEventArgs.Action = matches == 1 ? action : DataObjectAction.None;
				}

				if (register)
				{
					switch (method)
					{
						case DataObjectMethod.DragDrop:
							DragData.SourceOperations = operation;
							DragData.DataObjectCompatibleEventArgs = docEventArgs;
							break;

						case DataObjectMethod.Clipboard:

							var clipData = new ClipDragDropContext()
							{
								SourceOperations = operation,
								Source = elm,
								SourceItemsControl = wxItemsControl,
								DataObjectCompatibleEventArgs = docEventArgs
							};

							ClipData = clipData;

							break;
					}
				}
			}
			else if (operation.HasFlag(ClipDragDropOperations.To))
			{
				// Handle Cache
				DataObjectCompatibleEventArgs cache = null;
				if (CanDragDropCache != null && method == DataObjectMethod.DragDrop && elm != null)
				{
					foreach (var cdict in CanDragDropCache)
					{
						var cdoc = cdict.Key;
#if DEBUG
						// The Source of the drag must always be the same
						if (cdoc.Source != DataObjectCompatibleEventArgs.Source ||
							cdoc.Data != DataObjectCompatibleEventArgs.Data ||
							cdoc.SourceIndex != DataObjectCompatibleEventArgs.SourceIndex ||
							cdoc.SourceParent != DataObjectCompatibleEventArgs.SourceParent ||
							cdoc.SourceCollection != DataObjectCompatibleEventArgs.SourceCollection)
						{
							throw new Exception("Debug: Unexpected case: DragDrop Source doesn't match in CanDragDropCache");
						}
#endif

						// Check Destination match
						if (cdoc.DestinationIndex == index &&
							cdoc.Destination == elm &&
							cdoc.Method == method &&
							cdoc.Action == action)
						{
							cache = cdict.Value; // cache hit
							break;
						}
					}
				}


				if (cache != null)
				{
					// Result in cache: don't ask again if DragDrop is possible
					docEventArgs = cache;

					if (register)
					{
						DragData.DataObjectCompatibleEventArgs = docEventArgs;
					}
				}
				else
				{
					// Resume Clipboard operation

					ClipDragDropContext context;

					switch (method)
					{
						case DataObjectMethod.DragDrop:

							docEventArgs = new DataObjectCompatibleEventArgs(
							copy: DataObjectCompatibleEventArgs,
							action: action,
							destination: data,
							destinationIndex: index,
							destinationName: null);

							if (register)
							{
								DragData.DataObjectCompatibleEventArgs = docEventArgs;
							}

							context = DragData;

							break;


						case DataObjectMethod.Clipboard:

							// Retrieve ClipDragDropContext from that DataObject (if available)
							context = ClipData;
							if (context == null)
							{
								var dataObject = Clipboard.GetDataObject();

								docEventArgs = new DataObjectCompatibleEventArgs(
									method: method,
									action: action,
									preliminary: false,
									source: null,
									data: dataObject,
									sourceParent: null,
									sourceCollection: null,
									sourceIndex: -1
								);

								context = new ClipDragDropContext()
								{
									SourceOperations = (operation | ClipDragDropOperations.From) & ~ClipDragDropOperations.To,
									Source = null,
									SourceItemsControl = null,
									DataObjectCompatibleEventArgs = docEventArgs
								};
							}

							// Set destination data
							docEventArgs = new DataObjectCompatibleEventArgs(
								copy: context.DataObjectCompatibleEventArgs,
								action: action,
								destination: data,
								destinationIndex: index,
								destinationName: null);


							if (register)
							{
								context.DataObjectCompatibleEventArgs = docEventArgs;
								ClipData = context;
							}

							break;

						default:
							throw new InvalidOperationException();
					}


					// Operation compatibility between source and destination
					operation &= context.SourceOperations | ClipDragDropOperations.To;

					// Catch Drag & Drop from/to itself 
					if (context.Source == elm)
					{
						docEventArgs.Handled = false;
						docEventArgs.Action = DataObjectAction.None;
					}
					else if (
						// InterControl check
						context.SourceItemsControl != wxItemsControl && (

						// InterControl Source check
						context.SourceItemsControl != null && GetDisable(context.SourceItemsControl).HasFlag(ClipDragDropOperations.InterControl) ||

						// InterControl Destination check
						wxItemsControl != null && GetDisable(wxItemsControl).HasFlag(ClipDragDropOperations.InterControl)

						) ||

						// Inter-application Source check
						context.Source != null && elm == null && GetDisable(context.Source).HasFlag(ClipDragDropOperations.InterApplication) ||

						// Inter-application Destination check
						context.Source == null && elm != null && GetDisable(elm).HasFlag(ClipDragDropOperations.InterApplication)

						)
					{
						docEventArgs.Handled = false;
						docEventArgs.Action = DataObjectAction.None;
					}
					else
					{
						// Create cache key before any change in docEventArgs
						if (method == DataObjectMethod.DragDrop && elm != null)
						{
							cache = new DataObjectCompatibleEventArgs(
								copy: docEventArgs,
								action: docEventArgs.Action,
								destination: elm, // overrule destination
								destinationIndex: docEventArgs.DestinationIndex,
								destinationName: docEventArgs.DestinationName
								);
						}

						// action Compatibility with Destination
						if (data is IDataObjectCompatible ddoc)
						{
							docEventArgs.Handled = false;
							ddoc.DataObjectCanDo(docEventArgs);
							if (!docEventArgs.Handled) docEventArgs.Action = DataObjectAction.None;
						}
						else
						{
							// Use default decision
							docEventArgs.Handled = false;

							if (context.SourceItemsControl != null && wxItemsControl != null)
							{
								if (context.SourceItemsControl == wxItemsControl ||
									context.SourceItemsControl != null && wxItemsControl != null &&
									context.SourceItemsControl.ItemsSource != null && wxItemsControl.ItemsSource != null &&
									context.SourceItemsControl.ItemsSource.GetType() == wxItemsControl.ItemsSource.GetType()
									  )
								{
									// Can the use default ItemsControl DragDrop
									docEventArgs.Handled = true;
								}
							}
						}

						// Action compatibility with DataObjectCompatible Source
						if (docEventArgs.Handled && context.Source is IDataObjectCompatible sdoc)
						{
							docEventArgs.Direction = DataObjectDirection.From;
							docEventArgs.Handled = false;
							sdoc.DataObjectCanDo(docEventArgs);
							if (!docEventArgs.Handled) docEventArgs.Action = DataObjectAction.None;
						}

						// Add to cache
						if (method == DataObjectMethod.DragDrop && elm != null)
						{
							if (CanDragDropCache == null)
							{
								CanDragDropCache = new Dictionary<DataObjectCompatibleEventArgs, DataObjectCompatibleEventArgs>(20);
							}
							CanDragDropCache.Add(cache, docEventArgs);
						}
					}
				}

				// Retrieve active action from destination docEventArgs
				action = docEventArgs.Action;
			}


			LimeMsg.Debug("ClipDragDrop CanDragDrop: {0} --> {1} @ {2} (register: {3})", data, action, index, register);

			// action Compatibility with Source/Destination operations
			action &= (DataObjectAction)operation;

			if (!operation.HasFlag(method == DataObjectMethod.Clipboard ? ClipDragDropOperations.Clipboard : ClipDragDropOperations.DragDrop) ||
				!operation.HasFlag(start ? ClipDragDropOperations.From : ClipDragDropOperations.To))
			{
				action = DataObjectAction.None; // Incompatible
			}

			if (register)
			{
				switch (method)
				{
					case DataObjectMethod.DragDrop:

						var effects = action;

						if ((action & (DataObjectAction.Menu - 1)) != 0)
							effects &= DataObjectAction.Menu - 1; // Mask effects
						else if (action != DataObjectAction.None)
							effects = DataObjectAction.Copy; // default effect

						DragContext.DragDropEffects = (DragDropEffects)effects;
						break;


					case DataObjectMethod.Clipboard:
						break;

				}

			}

			return action;
		}


		#endregion


		// ----------------------------------------------------------------------------------------------
	}

	#endregion

}