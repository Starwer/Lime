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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
		/// Show a drop-menu by default
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
		/// Enable all drag and drop operation
		/// </summary>
		All = 0x2000 - 1
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
			return ClipDragDrop.CanExecute(action);
		}

		public void Execute(object parameter)
		{
			if (parameter == null) return;
			var actionName = (string)parameter;
			var action = (DataObjectAction)Enum.Parse(typeof(DataObjectAction), actionName);
			ClipDragDrop.Execute(action);
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
	/// Provide a MVVM way to handle Drag & Drop operation and Clipboard operation in WPF.
	/// To fully leverage the use of this class, it is recommended to use IDataObjectCompatible objects
	/// for the View-Model, as Drag-Sources and Drop-Destination data.
	/// </summary>
	public static class ClipDragDrop
	{
		// ----------------------------------------------------------------------------------------------
		#region Types

		/// <summary>
		/// ClipDragDrop data handled 
		/// </summary>
		public class ClipDragDropEventArgs
		{
			public FrameworkElement Source;
			public ItemsControl SourceItemsControl;

			public FrameworkElement Over;
			public FrameworkElement Destination;
			public ItemsControl DestinationItemsControl;
			public int DestinationIndex;

			public ClipDragDropOperations SourceOperations = ClipDragDropOperations.None;
			public DragDropEffects DragDropEffects = DragDropEffects.None;

			public ClipDragDefaultOperation DragDefaultOperation;
			public bool RightClick;
			public Point MouseHit;

			public bool Cancelled;
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
		/// Store the dragged object reference
		/// </summary>
		private static ClipDragDropEventArgs Data = null;

		/// <summary>
		/// Dragged control decorations
		/// </summary>
		private static DragSystemIcon DragIcon = null;

		/// <summary>
		/// Zone, relative to the destination control, where the hit doesn't need to be recomputed
		/// </summary>
		private static Rect? HitZone;

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
			get { return Data != null; }
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
		public static DataObjectCompatibleEventArgs DataObjectCompatibleEventArgs { get; private set; }


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
		/// Set the Drag data-Source (View-Model) and enable Dragging the FrameworkElement (View).
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
		/// Set the Drop data-Destination (View-Model) and enable Droppping to the FrameworkElement (View).
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
		/// It has no effect in Release.
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
			return DragDropSelfCheckSeverity.Off;
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
				wxthis.AllowDrop = true;

				wxthis.PreviewMouseDown += OnMouseButtonDown;
				wxthis.PreviewMouseUp += OnMouseButtonUp;

				wxthis.QueryContinueDrag += OnQueryContinueDrag;
				wxthis.GiveFeedback += OnGiveFeedback;
				wxthis.Drop += OnDrop;

				wxthis.DragOver += OnDragOver;
				wxthis.DragLeave += OnDragLeave;
			}
			else
			{
				wxthis.PreviewMouseDown -= OnMouseButtonDown;
				wxthis.PreviewMouseUp -= OnMouseButtonUp;
				wxthis.PreviewMouseMove -= OnMouseMove;

				wxthis.QueryContinueDrag -= OnQueryContinueDrag;
				wxthis.GiveFeedback -= OnGiveFeedback;
				wxthis.Drop -= OnDrop;

				wxthis.DragOver -= OnDragOver;
				wxthis.DragLeave -= OnDragLeave;
			}
		}

		/// <summary>
		///  Cancel any ongoing Drag & Drop operation
		/// </summary>
		public static void Cancel()
		{
			bool cancelled = false;

			// Animate Source
			bool animate = false;

			if (Data != null)
			{
				cancelled = Data.Cancelled;

				LimeMsg.Debug("ClipDragDrop Cancel: {0}", cancelled);

				// Restore Drop destination aspect
				if (Data.Destination != null)
				{
					DragDropDestinationVisual(false);
					DragDropInCollection(ClipDragDropOperations.None);
				}

				// Handle Source and its attributes
				if (Data.Source != null)
				{
					animate = GetAnimate(Data.Source);
					Data.Source.PreviewMouseMove -= OnMouseMove;
					DragDropSourceVisual(false);
				}

			}

			if (DragIcon != null)
			{
				if (cancelled && animate)
				{
					var wxsrc = Data.Source;
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

			DebugAdorner.Hide();

			HitZone = null;
			CanDragDropCache = null;
			HandleDestinationAction = ClipDragDropOperations.From | ClipDragDropOperations.To; // Invalidate
			Data = null;
			DataObjectCompatibleEventArgs = null;
			ContextMenu = null;
			Mouse.OverrideCursor = null;
		}


		/// <summary>
		/// Check whether a Clipboard action can be used on the current Drag & Drop operation
		/// </summary>
		/// <param name="action">action to be tested</param>
		/// <returns>true if action can be applied to the current Clipboard operation</returns>
		public static bool CanExecute(DataObjectAction action)
		{
			if (DataObjectCompatibleEventArgs == null || Data == null) return false;

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

			var effect = CanDragDrop(
				operation,
				(object) Data.Destination ?? DataObjectCompatibleEventArgs.Data,
				DataObjectCompatibleEventArgs.DestinationIndex,
				register: false,
				wxItemsControl: Data.DestinationItemsControl);

			return effect != DataObjectAction.None && effect == action;
		}


		/// <summary>
		/// Execute and complete the current Drag & Drop operation
		/// </summary>
		/// <param name="action">action to be execute (None: will just cancel the Drag & Drop operation)</param>
		/// <returns>true if the action has been handled</returns>
		public static bool Execute(DataObjectAction action)
		{
			bool ret = true;

			if (Data.DragDropEffects == DragDropEffects.None ||
				action == DataObjectAction.None ||
				DataObjectCompatibleEventArgs.Direction != DataObjectDirection.To ||
				!CanExecute(action))
			{
				LimeMsg.Debug("ClipDragDrop Execute: Cancelled: {0}", Data.DragDropEffects);
				Data.Cancelled = true;
			}
			else if (DataObjectCompatibleEventArgs != null)
			{
				if (Data.Destination is var wxDest)
				{
					// Make sure we make the right destination message
					DataObjectCompatibleEventArgs.Action = action;
					DataObjectCompatibleEventArgs.Handled = false;
					DataObjectCompatibleEventArgs.SourceHandled = false;

					object data = GetDestination(wxDest) ?? wxDest.DataContext;
					if (DataObjectCompatibleEventArgs.Action == DataObjectAction.Menu)
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
								var text = string.Format(format, DataObjectCompatibleEventArgs.DestinationName);

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
							var textc = string.Format(GetFormatCancel(wxDest), DataObjectCompatibleEventArgs.DestinationName);
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
							ContextMenu.Closed -= OnContextMenuClosed;
						}

						ContextMenu.Closed += OnContextMenuClosed;
						ContextMenu.IsOpen = true;

						return true;

					}
					else if (data is IDataObjectCompatible dest)
					{
						// Use the DataObjectCompatible object action handler on destination
						DataObjectCompatibleEventArgs.Handled = false;
						dest.DataObjectDo(DataObjectCompatibleEventArgs);
						ret = DataObjectCompatibleEventArgs.Handled;

						// Use the DataObjectCompatible object action handler on source
						if (ret && !DataObjectCompatibleEventArgs.SourceHandled &&
							DataObjectCompatibleEventArgs.Source is IDataObjectCompatible source)
						{
							DataObjectCompatibleEventArgs.Handled = false;
							DataObjectCompatibleEventArgs.Direction = DataObjectDirection.From;
							source.DataObjectDo(DataObjectCompatibleEventArgs);
						}

						Data.Cancelled = !DataObjectCompatibleEventArgs.Handled;

					}
					else if (wxDest is ICollection collec)
					{
						// Handle collections automatically
						if (DataObjectCompatibleEventArgs.DoOnCollection(collec) &&
							!DataObjectCompatibleEventArgs.SourceHandled)
						{
							// Handle source collection
							DataObjectCompatibleEventArgs.Handled = false;
							DataObjectCompatibleEventArgs.Direction = DataObjectDirection.From;
							DataObjectCompatibleEventArgs.DoOnCollection(collec);
						}

						Data.Cancelled = !DataObjectCompatibleEventArgs.Handled;
					}
				}
			}

			Cancel();

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
		public static void OnContextMenuClosed(object sender, RoutedEventArgs e)
		{
			if (Data != null)
			{
				Data.Cancelled = true;
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
			if (Data != null)
			{
				Data.Cancelled = true;
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
				(elm) => elm != null && GetSource(elm) != null);

			if (wxSource != null)
			{
				// initialize the Drag and Drop
				Data = new ClipDragDropEventArgs()
				{
					Source = wxSource,
					DestinationIndex = -1,
					RightClick = rightClick,
					MouseHit = e.GetPosition(wxSource),
					Cancelled = true
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
			if (Data != null)
			{
				Data.Cancelled = true;
				Cancel();
				return;
			}
		}


		/// <summary>
		/// Detect and start dragging from the application
		/// </summary>
		/// <param name="sender">Control when <see cref="EnableProperty"/> is set</param>
		/// <param name="e"></param>
		private static void OnMouseMove(object sender, MouseEventArgs e)
		{
			LimeMsg.Debug("ClipDragDrop OnMouseMove: {0}", sender);

			var wxSender = (FrameworkElement)sender;

			if (Data == null || DragIcon != null)
			{
				// Should never come there... but handle this just in case...
				wxSender.PreviewMouseMove -= OnMouseMove;
				return;
			}

			// Visual feedbacks on Source
			var pos = e.GetPosition(wxSender);
			var click = Data.RightClick ? e.RightButton : e.LeftButton;

			if (click == MouseButtonState.Pressed &&
				(Math.Abs(Data.MouseHit.X - pos.X) >= SystemParameters.MinimumHorizontalDragDistance ||
				  Math.Abs(Data.MouseHit.Y - pos.Y) >= SystemParameters.MinimumVerticalDragDistance))
			{
				// --- Start dragging ---

				// Stop monitoring this element for nouse move, the DragDrop.DoDragDrop will follow up
				wxSender.PreviewMouseMove -= OnMouseMove;

				// Get drag and drop source and its hierarchy
				if ( !TryGetDragDropElement(true, Data.MouseHit, wxSender, out var wxSource, 
					out var wxSourceDecoration, out var wxSourceItemsControl, out var idx) )
				{
					Cancel(); // unexpected exception
					return;
				}

				// Get default operation
				Data.DragDefaultOperation = GetDragDefaultOperation(wxSource);

				// Define the kind of source drag-action
				var operation = ClipDragDropOperations.DragDrop | ClipDragDropOperations.From | 
					(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? ClipDragDropOperations.Move :
					Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? ClipDragDropOperations.Copy :
					Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ? ClipDragDropOperations.Link :
					Data.RightClick ? ClipDragDropOperations.Menu :
					wxSourceItemsControl != null && wxSource != wxSourceItemsControl  ? ClipDragDropOperations.Move :
					(ClipDragDropOperations)Data.DragDefaultOperation);

				// Start Source dragging
				Data.Over = wxSender;
				Data.Source = wxSource;
				Data.SourceItemsControl = wxSourceItemsControl;

				var actions = CanDragDrop(operation | (ClipDragDropOperations.From - 1), wxSource, idx, true, wxSourceItemsControl);

				// Detect if dragging is enabled
				if (actions == DataObjectAction.None)
				{
					Cancel();
					return;
				}

				// Start decoration and drag icon
				DragIcon = new DragSystemIcon(wxSource, DataObjectCompatibleEventArgs.Data, 
					Data.DragDropEffects, Scale, create: true);

				// Visual on source element
				DragDropSourceVisual( actions.HasFlag(DataObjectAction.Move) && operation.HasFlag(ClipDragDropOperations.Move));

				// Start drag and drop
				try
				{
					DragDrop.DoDragDrop(Data.Source, DataObjectCompatibleEventArgs.Data, Data.DragDropEffects);
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
		/// <param name="sender">Control when <see cref="EnableProperty"/> is set</param>
		/// <param name="e"></param>
		private static void OnDragOver(object sender, DragEventArgs e)
		{
			var wxSender = (FrameworkElement)sender;

			if (Data == null)
			{
				// Initiate Drag when Drag source is coming  from outside of the app
				Data = new ClipDragDropEventArgs()
				{
					Cancelled = true,
					DragDefaultOperation = GetDragDefaultOperation(wxSender)
				};


				var operations = ClipDragDropOperations.DragDrop | ClipDragDropOperations.From | (
					(ClipDragDropOperations)e.AllowedEffects & (ClipDragDropOperations.From - 1));

				var actions = CanDragDrop(operations, e.Data, -1, register: true);

				// Detect if dragging is enabled
				if (actions == DataObjectAction.None)
				{
					Cancel();
					return;
				}

				// Keep the system visual Icon in the application
				DragIcon = new DragSystemIcon(wxSender, DataObjectCompatibleEventArgs.Data, 
					Data.DragDropEffects, Scale, create: false);

				// Prepare for Window on top
				Time = DateTime.Now;

			}
			else if (Data.Source == null)
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
			Data.Over = wxSender;

			// When dragging from outside the application, we want to detect a drop, even on a pseudo
			// non-drop-able position, to end the dragging properly
			e.Effects = Data.Source == null && Data.DragDropEffects == DragDropEffects.None ?
				DragDropEffects.Scroll : Data.DragDropEffects;

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
			if (Data != null && Data.Source == null && Data.Over != null)
			{
				// Locate mouse relative to the currently selected window
				if (WPF.GetCursorPos(out WPF.SysPoint spos))
				{
					var mpos = new Point(spos.x, spos.y);
					var wxRoot = PresentationSource.FromVisual(Data.Over).RootVisual as FrameworkElement;
					var pos = wxRoot.PointFromScreen(mpos);

					if (wxRoot.InputHitTest(pos) == null)
					{
						// Ouside a drop-able position, we can't detect when the dragging is ended by mouse
						// release, so we'd better stop handling the dragging from now
						LimeMsg.Debug("ClipDragDrop OnDragLeave: {0} - {1}", e.Effects, e.AllowedEffects);
						Data.Cancelled = true;
						Cancel();
					}
				}
			}

			e.Handled = true;
		}



		/// <summary>
		/// Handle the drag over a destination
		/// </summary>
		/// <param name="sender">Control when <see cref="EnableProperty"/> is set</param>
		/// <param name="e"></param>
		private static void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
		{
			//LimeMsg.Debug("ClipDragDrop OnQueryContinueDrag: {0}", e.Source);

			var click = Data.RightClick ? DragDropKeyStates.RightMouseButton : DragDropKeyStates.LeftMouseButton;

			if (DragIcon == null || e.EscapePressed)
			{
				e.Action = DragAction.Cancel;
				if (Data != null) Data.Cancelled = true;
				Cancel();
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
			HandleDestination(Data.Over);

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
				if (Data != null) Data.Cancelled = true;
				Cancel(); 
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
			ClipDragDropOperations operation = ClipDragDropOperations.None;

			if (TryGetDragDropElement(false, pos, wxRoot, out var wxDest,
				out var wxDestDecoration, out var wxDestItemsControl, out var idx))
			{
				// Make destination message
				operation = ClipDragDropOperations.DragDrop | ClipDragDropOperations.To | (
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? ClipDragDropOperations.Move :
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? ClipDragDropOperations.Copy :
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ? ClipDragDropOperations.Link :
						 Data.RightClick ? ClipDragDropOperations.Menu :
						 Data.SourceItemsControl == wxDestItemsControl && 
							(wxDest == wxDestItemsControl || wxDest == Data.Source)? ClipDragDropOperations.Move :
						 (ClipDragDropOperations)Data.DragDefaultOperation);
			}
			else if (wxhit == null)
			{
				// Make destination message when getting out of the application
				operation = ClipDragDropOperations.DragDrop | ClipDragDropOperations.To | (
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? ClipDragDropOperations.Move :
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? ClipDragDropOperations.Copy :
						 Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ? ClipDragDropOperations.Link :
						 Data.RightClick ? ClipDragDropOperations.Menu :
						 (ClipDragDropOperations)Data.DragDefaultOperation);

				LimeMsg.Debug("ClipDragDrop HandleDestination: off {0}", operation);
			}
			

			bool updateDragIcon = false;

			// Handle change in destinations (lazily, only if required)
			if (wxDest != Data.Destination || HandleDestinationAction != operation)
			{
				HandleDestinationAction = operation;

				// Reset visuals on previous destination
				if (Data.Destination != null)
				{
					DragDropDestinationVisual(false);
					//DebugAdorner.Hide(Data.Destination);
				}

				// Invalidate the HitZone
				HitZone = null;

				//DebugAdorner.Show(wxDest);

				// Update the Destination Data
				Data.Destination = wxDest;
				Data.DestinationIndex = idx;

				var action = CanDragDrop(operation, wxDest, idx, register: true);

				LimeMsg.Debug("ClipDragDrop HandleDestination: {0} --> {1}", wxDest, DataObjectCompatibleEventArgs.Action);

				// Visual feedbacks
				updateDragIcon = true;
				if (wxDest != null && action != DataObjectAction.None)
				{
					DragDropDestinationVisual(true);
					var move = action == DataObjectAction.Move && operation.HasFlag(ClipDragDropOperations.Move);
					if (wxDestItemsControl != Data.SourceItemsControl || move)
					{
						DragDropSourceVisual(move);
					}
				}
			}

			// Handle change in DestinationItemsControl (lazily, only if required)
			if (wxDestItemsControl != Data.DestinationItemsControl)
			{
				// Reset visuals on previous destination
				DragDropInCollection(ClipDragDropOperations.None);

				// Update the destination Items Control
				Data.DestinationItemsControl = wxDestItemsControl;

				// Visual feedback
				updateDragIcon = true;
				if (wxDestItemsControl != null)
				{
					var dpos = wxDestItemsControl.PointFromScreen(mpos);
					DragDropInCollection(operation, dpos);
				}

			}
			else if (wxDestItemsControl != null && wxDestItemsControl == Data.DestinationItemsControl)
			{
				// Update destination Items Control (lazily, only if required)
				var dpos = wxDestItemsControl.PointFromScreen(mpos);

				if (HitZone == null || !HitZone.Value.Contains(dpos))
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

				if (Data.DragDropEffects != DragDropEffects.None && wxDest != null)
				{
					format = GetFormat(wxDest, DataObjectCompatibleEventArgs.Action);

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

				DragIcon?.UpdateEffects(Data.DragDropEffects, type, format, DataObjectCompatibleEventArgs.DestinationName);
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
			e.Handled = Execute(DataObjectCompatibleEventArgs.Action);
		}



		/// <summary>
		/// Retrieve Drag & Drop element and its hierarchy from a mouse position
		/// </summary>
		/// <param name="isSource">True if we are looking for a drag-source, false for drop-destination</param>
		/// <param name="hit">mouse hit coordinated relative to <see cref="sender"/></param>
		/// <param name="sender">Mouse relative reference</param>
		/// <param name="element">element handling the the Drag and drop</param>
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
			var wxSource = Data.Source;
			elementItemsControl = null;
			idx = -1;
			element = null;

			if (sender == null)
			{
				elementDecoration = null;
				return false;
			}


			// Find element
			if (sender.InputHitTest(hit) is DependencyObject dep)
			{
				while (dep != null)
				{
					if (dep is FrameworkElement elm)
					{
						if (isSource)
						{
							if (GetSource(elm) != null)
							{
								// Found matching element
								element = elm;
								break;
							}
						}
						else
						{
							if (GetDestination(elm) != null)
							{
								// Found matching element
								element = elm;
								break;
							}
						}
					}

					dep = VisualTreeHelper.GetParent(dep);
				}
			}

			// default decoration
			elementDecoration = element;

			if (element == null)
			{
				return false;
			}


			// Self-check will try to detect problems in the drag/drop-element and its parents
#if DEBUG
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
		/// Define and track whether a Drag & Drop operation can be applied on data (element)
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="data"></param>
		/// <param name="index"></param>
		/// <param name="register"></param>
		/// <param name="wxItemsControl"></param>
		/// <returns>DragDropEffects</returns>
		private static DataObjectAction CanDragDrop(ClipDragDropOperations operation, object data,
			int index, bool register, ItemsControl wxItemsControl = null)
		{
			bool start = operation.HasFlag(ClipDragDropOperations.From);

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
			var method =
				operation.HasFlag(ClipDragDropOperations.Clipboard) ? DataObjectMethod.Clipboard :
				operation.HasFlag(ClipDragDropOperations.DragDrop) ? DataObjectMethod.DragDrop :
				DataObjectMethod.None;


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

				// Start dragging
				docEventArgs = new DataObjectCompatibleEventArgs(
					method: method,
					action: DataObjectAction.None,
					source: data,
					data: dataObject,
					sourceParent: wxItemsControl?.DataContext,
					sourceCollection: wxItemsControl?.ItemsSource,
					sourceIndex: index
					);

				if (data is IDataObjectCompatible idoc)
				{
					for (var i = DataObjectAction.Copy; i <= DataObjectAction.Open; i = (DataObjectAction)((int)i<<1))
					{
						if ((action & i) != 0)
						{
							docEventArgs.Handled = false;
							docEventArgs.Action = i;
							idoc.DataObjectCanDo(docEventArgs);
							if (!docEventArgs.Handled) operation &= ~(ClipDragDropOperations) i;
						}
					}

					docEventArgs.Action = DataObjectAction.None;
				}

				if (register)
				{

					Data.SourceOperations = operation;
					DataObjectCompatibleEventArgs = docEventArgs;
				}
			}
			else if (operation.HasFlag(ClipDragDropOperations.To))
			{
				// Handle Cache
				DataObjectCompatibleEventArgs cache = null;
				if (CanDragDropCache != null)
				{
					foreach (var cdict in CanDragDropCache)
					{
						var cdoc = cdict.Key;
#if DEBUG
						// The Source of the drag should always be the same
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
							cdoc.Destination == (elm ?? data) &&
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
						DataObjectCompatibleEventArgs = docEventArgs;
					}
				}
				else
				{
					// Ask if DragDrop is possible

					docEventArgs = new DataObjectCompatibleEventArgs(
						copy: DataObjectCompatibleEventArgs,
						action: action,
						destination: data,
						destinationIndex: index,
						destinationName: null);

					if (register)
					{
						DataObjectCompatibleEventArgs = docEventArgs;
					}

					// Operation compatibility between source and destination
					operation &= Data.SourceOperations | ClipDragDropOperations.To;

					// Catch Drag & Drop from/to itself 
					if (Data.Source == elm)
					{
						docEventArgs.Handled = false;
						action = DataObjectAction.None;
					}
					else if (
						// InterControl check
						Data.SourceItemsControl != Data.DestinationItemsControl && (

						// InterControl Source check
						Data.SourceItemsControl != null && GetDisable(Data.SourceItemsControl).HasFlag(ClipDragDropOperations.InterControl) ||

						// InterControl Destination check
						Data.DestinationItemsControl != null && GetDisable(Data.DestinationItemsControl).HasFlag(ClipDragDropOperations.InterControl)

						) ||

						// Inter-application Source check
						Data.Source != null && elm == null && GetDisable(Data.Source).HasFlag(ClipDragDropOperations.InterApplication) ||

						// Inter-application Destination check
						Data.Source == null && elm != null && GetDisable(elm).HasFlag(ClipDragDropOperations.InterApplication)

						)
					{
						docEventArgs.Handled = false;
						action = DataObjectAction.None;
					}
					else
					{
						// Create cache key
						cache = new DataObjectCompatibleEventArgs(
							copy: docEventArgs,
							action: docEventArgs.Action,
							destination: elm ?? data, // overrule destination
							destinationIndex: docEventArgs.SourceIndex,
							destinationName: docEventArgs.DestinationName
							);

						// action Compatibility with Destination
						if (data is IDataObjectCompatible ddoc)
						{
							docEventArgs.Handled = false;
							ddoc.DataObjectCanDo(docEventArgs);
							action = docEventArgs.Handled ?	docEventArgs.Action : DataObjectAction.None;
						}
						else
						{
							// Use default decision
							docEventArgs.Handled = false;

							if (Data.SourceItemsControl != null && Data.DestinationItemsControl != null)
							{
								if (Data.SourceItemsControl == Data.DestinationItemsControl ||
									Data.SourceItemsControl != null && Data.DestinationItemsControl != null &&
									Data.SourceItemsControl.ItemsSource != null && Data.DestinationItemsControl.ItemsSource != null &&
									Data.SourceItemsControl.ItemsSource.GetType() == Data.DestinationItemsControl.ItemsSource.GetType()
									  )
								{
									// Can the use default ItemsControl DragDrop
									docEventArgs.Handled = true;
								}
							}
						}

						// action compatibility with DataObjectCompatible Source
						if (docEventArgs.Handled && Data.Source is IDataObjectCompatible sdoc)
						{
							docEventArgs.Direction = DataObjectDirection.From;
							docEventArgs.Handled = false;
							sdoc.DataObjectCanDo(docEventArgs);
							if (!docEventArgs.Handled) action = DataObjectAction.None;
							docEventArgs.Action = action;
						}

						// Add to cache
						if (CanDragDropCache == null)
						{
							CanDragDropCache = new Dictionary<DataObjectCompatibleEventArgs, DataObjectCompatibleEventArgs>(20);
						}
						CanDragDropCache.Add(cache, docEventArgs);
					}
				}
			}


			LimeMsg.Debug("ClipDragDrop CanDragDrop: {0} --> {1} @ {2} (register: {3})", data, action, index, register);

			// action Compatibility with Source/Destination operations
			action &= (DataObjectAction)operation;

			if (!operation.HasFlag(ClipDragDropOperations.DragDrop) ||
				!operation.HasFlag(start ? ClipDragDropOperations.From : ClipDragDropOperations.To) ||
				action == DataObjectAction.None)
				action = DataObjectAction.None; // Incompatible

			if (register)
			{
				var effects = action;

				if ((action & (DataObjectAction.Menu - 1)) != 0)
					effects &= DataObjectAction.Menu - 1; // Mask effects
				else if (action != DataObjectAction.None)
					effects = DataObjectAction.Copy; // default effect

				Data.DragDropEffects = (DragDropEffects)effects;
				Data.Cancelled = action == DataObjectAction.None;
			}

			return action;
		}


		/// <summary>
		/// Handle the visual effect on a drag source
		/// </summary>
		/// <param name="enable">indicate whether current source has visual effect</param>
		private static void DragDropSourceVisual(bool enable)
		{
			var wxSource = Data.Source;
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
		private static void DragDropDestinationVisual(bool over)
		{
			var wxDest = Data.Destination;
			var animate = GetAnimate(wxDest);

			if (over)
			{
				VisualStateManager.GoToState(wxDest, "Focused", animate);
			}
			else
			{
				VisualStateManager.GoToState(wxDest, "Normal", animate);
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
		/// <param name="hit">Mouse hit point relative to <see cref="Data.DestinationItemsControl"/></param>
		private static void DragDropInCollection (ClipDragDropOperations operation, Point hit = new Point())
		{
#if DEBUG
			// Setup debugging helpers
			bool debugShowItems = false;
			bool debugShowHitZone = false;
#endif

			// Data.Destination may be updated in this method on matching hit
			var wxDestItemsControl = Data.DestinationItemsControl;

			// Invalid destination
			if (wxDestItemsControl == null) return;

			var wxSourceItemsControl = Data.SourceItemsControl;
			var wxSource = Data.Source;

			// Define whether insertion actions should be enabled on destination
			var action = CanDragDrop(operation, wxDestItemsControl, 0, register: false);
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
			HitZone = null;
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
							else if (wxDestItem == Data.Destination)
							{
								rescale = Scale / 2 + 0.5;
							}

							HitZone = render;

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

							var act = CanDragDrop(operation, wxDestItemsControl, index, register: false);
							if (act != DataObjectAction.None)
							{
								// Object can be inserted here
								newIndex = index;
								centerX = moveX;
								centerY = moveY;
							}

							if (HitZone != null) iconzone.Intersect(HitZone.Value);
							HitZone = iconzone;

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
				CanDragDrop(operation, wxDestItemsControl, newIndex, register: true);
			}

#if DEBUG
			if (debugShowHitZone)
			{
				if (HitZone == null)
					DebugAdorner.Hide(wxDestItemsControl);
				else
					DebugAdorner.Show(wxDestItemsControl, HitZone);
			}
#endif

			// Set the non-hit zone as a hit-zone (zone where there is no need to recompute
			// the animations and hit)
			if (HitZone == null)
			{
				HitZone = nonHit;
			}

			LimeMsg.Debug("ClipDragDrop DragDropInCollection: {0} --> {1}", Data.DestinationItemsControl, operation);

		}


		#endregion


		// ----------------------------------------------------------------------------------------------
	}

#endregion

}