/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/


using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace WPFhelper
{
	/// <summary>
	/// Helper function for handling WPF in Code. 
	/// </summary>
	public class WPF
	{

		// ----------------------------------------------------------------------------------------------
		#region UI element Helpers

		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Find the first child in the WPF visual tree matching the given type and, optionally, a specific condition.
		/// </summary>
		/// <typeparam name="T">UI object-type to find</typeparam>
		/// <param name="element">UI object-type to start seaarching from.</param>
		/// <param name="condition">addtional condition that the UI object should verify.</param>
		/// <returns>first UI object found of the given type, or null if no object found</returns>
		public static T FindFirstChild<T>(DependencyObject element, Func<T, bool> condition = null) where T : DependencyObject
		{
			if (element == null) return null;

			int childrenCount;
			try
			{
				childrenCount = VisualTreeHelper.GetChildrenCount(element);
			}
			catch
			{
				return null;
			}

			var children = new DependencyObject[childrenCount];
			for (int i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild(element, i) as DependencyObject;
				children[i] = child;
				if (child is T match && (condition == null || condition(match)))
				{
					return (T)child;
				}
			}

			for (int i = 0; i < childrenCount; i++)
			{
				if (children[i] != null)
				{
					var subChild = FindFirstChild(children[i], condition);
					if (subChild != null)
					{
						return subChild;
					}
				}
			}

			return null;
		}


		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Find the first parent in the WPF visual tree matching the given type and, optionally, a specific condition.
		/// </summary>
		/// <typeparam name="T">UI object-type to find</typeparam>
		/// <param name="child">UI object-type to start seaarching from.</param>
		/// <param name="condition">addtional condition that the UI object should verify.</param>
		/// <returns>first UI object found of the given type, or null if no object found</returns>
		public static T FindFirstParent<T>(DependencyObject child, Func<T, bool> condition = null) where T : DependencyObject
		{
			//get parent item
			DependencyObject parentObject;
			try
			{
				parentObject = VisualTreeHelper.GetParent(child);
			}
			catch
			{
				// Fall back to using logical tree
				if (child is FrameworkElement elm)
					parentObject = elm.Parent;
				else
					parentObject = null;
			}
			if (parentObject == null) return null;

			//check if the parent matches the type we're looking for, and the condition (if applicable)
			if (parentObject is T parent && (condition == null || condition(parent)))
			{
				return parent;
			}
			else
			{
				return FindFirstParent(parentObject, condition);
			}
		}



		/// <summary>Tries to locate a given item within the visual tree, starting with the dependency object at a given position.</summary>
		/// <typeparam name="T">The type of the element to be found on the visual tree of the element at the given location.</typeparam>
		/// <param name="element">The main element in which the hit testing is happening.</param>
		/// <param name="iPoint">The WPF position relative to the element.</param>
		/// <param name="condition">addtional condition that the UI object should verify.</param>
		public static T FindFromPoint<T>(UIElement element, Point iPoint, Func<T, bool> condition = null) where T : DependencyObject
		{
			if (element == null) return null;

			// Get hit object
			if (!(element.InputHitTest(iPoint) is DependencyObject dep)) return null;

			//check if the object matches the type we're looking for, and the condition (if applicable)
			if (dep is T ret && (condition == null || condition(ret)))
			{
				return ret;
			}
			else
			{
				return FindFirstParent(dep, condition);
			}
		}


		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Detects if a GUI element is visible inside a GUI container.
		/// </summary>
		/// <param name="child">UI Element to be located</param>
		/// <param name="container">Container element</param>
		/// <returns>true if the element (child) is inside the container</returns>
		public static bool IsFullyOrPartiallyVisible(FrameworkElement child, FrameworkElement container)
		{
			if (child == null || child.Visibility != Visibility.Visible) return false;
			var childTransform = child.TransformToAncestor(container);
			var childRectangle = childTransform.TransformBounds(new Rect(new Point(0, 0), child.RenderSize));
			var ownerRectangle = new Rect(new Point(0, 0), container.RenderSize);
			return ownerRectangle.IntersectsWith(childRectangle);
		}

		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Get the visible area of a Control.
		/// </summary>
		/// <param name="elm">FrameworkElement Control</param>
		/// <returns>the visible Rect relative to the root element</returns>
		public static Rect GetVisibleArea(FrameworkElement elm, Visual reference = null)
		{
			if (reference==null) reference = PresentationSource.FromVisual(elm).RootVisual;
			GeneralTransform transformToRoot = elm.TransformToAncestor(reference);
			DependencyObject parent = VisualTreeHelper.GetParent(elm);

			// TranslatePoint should return the proper points *after* the render transforms are applied
			var topLeft = elm.TranslatePoint(new Point(0, 0), null);
			var bottomRight = elm.TranslatePoint(new Point(elm.ActualWidth, elm.ActualHeight), null);
			var screenRect = new Rect(transformToRoot.Transform(new Point(0, 0)), transformToRoot.Transform(new System.Windows.Point(elm.ActualWidth, elm.ActualHeight)));

			while (parent != null)
			{
				Control control = parent as Control;

				if (parent is Visual visual && control != null)
				{
					if(reference != elm) transformToRoot = visual.TransformToAncestor(reference);
					var pointAncestorTopLeft = transformToRoot.Transform(new Point(0, 0));
					var pointAncestorBottomRight = transformToRoot.Transform(new Point(control.ActualWidth, control.ActualHeight));
					var ancestorRect = new Rect(pointAncestorTopLeft, pointAncestorBottomRight);
					screenRect.Intersect(ancestorRect);
				}

				parent = VisualTreeHelper.GetParent(parent);
			}

			// at this point screenRect is the bounding rectangle for the visible portion of "this" element
			return screenRect;
		}



		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a Windows Point (WPF) to a Screen Point (WinForm)
		/// </summary>
		/// <param name="wpos"></param>
		/// <returns></returns>
		public static System.Drawing.Point Windows2DrawingPoint(Point wpos)
		{
			var wnd = Application.Current.MainWindow;
			var transform = PresentationSource.FromVisual(wnd).CompositionTarget.TransformFromDevice;
			var tpos = wnd.PointToScreen(transform.Transform(wpos));
			var dpos = new System.Drawing.Point((int)tpos.X, (int)tpos.Y);

			return dpos;
		}



		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Take a snapshot of a visual and return it as an RenderTargetBitmap (ImageSource)
		/// </summary>
		/// <param name="source">Visual to be converted to ImageSource</param>
		/// <param name="dpiX"></param>
		/// <param name="dpiY"></param>
		/// <param name="background">Set the background brush</param>
		/// <returns>ImageSource representing the visual</returns>
		public static RenderTargetBitmap ProduceImageSourceForVisual(Visual source, double dpiX = 96.0, double dpiY = 96.0, Brush background = null)
		{
			if (source == null) return null;
			var bounds = VisualTreeHelper.GetDescendantBounds(source);

			RenderTargetBitmap bitmap = new RenderTargetBitmap(
				(int)bounds.Width, (int)bounds.Height,
				dpiX, dpiY, PixelFormats.Pbgra32);

			// We need this trick to render to workaround the crop when the visual has margins
			DrawingVisual dv = new DrawingVisual();
			using (DrawingContext dc = dv.RenderOpen())
			{
				if (background != null) dc.DrawRectangle(background, null, new Rect(new Point(), bounds.Size));
				VisualBrush vb = new VisualBrush(source);
				dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
			}
			bitmap.Render(dv);

			return bitmap;
		}


		#endregion


		// ----------------------------------------------------------------------------------------------
		#region WindowResize

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// Define resize direction for the WindowResize function
		/// </summary>
		public enum ResizeDirection
		{
			None = 0,
			Left = 61441,
			Right = 61442,
			Top = 61443,
			TopLeft = 61444,
			TopRight = 61445,
			Bottom = 61446,
			BottomLeft = 61447,
			BottomRight = 61448
		}


		/// <summary>
		/// Call this function from a callback method attached to a PreviewMousLeftButtonDown event of a UI element to enable the resizing
		/// of the window in one direction.
		/// </summary>
		/// <param name="sender">The UI element from which the callback is attached</param>
		/// <param name="direction">Set the windows-resize direction</param>
		public static void WindowResize(object sender, ResizeDirection direction)
		{
			HwndSource hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;
			SendMessage(hwndSource.Handle, 0x112, (IntPtr)direction, IntPtr.Zero);
		}

		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Execution Helpers

		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Delegate command to simplify creation of ICommands
		/// <br/>Usage:
		/// cmd = new DelegateCommand { () => { return true; } , () => { DoSomethingUsefull(); } };
		/// 
		/// </summary>
		public class DelegateCommand : ICommand
		{
			public Func<bool> CanExecuteFunc { get; set; }
			public Action CommandAction { get; set; }

			public void Execute(object parameter)
			{
				CommandAction();
			}

			public bool CanExecute(object parameter)
			{
				return CanExecuteFunc == null || CanExecuteFunc();
			}

			public event EventHandler CanExecuteChanged
			{
				add { CommandManager.RequerySuggested += value; }
				remove { CommandManager.RequerySuggested -= value; }
			}
		}

		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Window placement/size

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct SysPoint
		{
			public int x;
			public int y;
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct SysSize
		{
			public int Width, Height;
		}



		[StructLayout(LayoutKind.Sequential)]
		public struct Win32Size
		{
			public int cx;
			public int cy;
		}


		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct SysRect
		{
			public SysRect(int x, int y, int x1, int y1)
			{
				Left = x;
				Top = y;
				Right = x1;
				Bottom = y1;
			}

			public int Left, Top, Right, Bottom;
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct WindowPlacement
		{
			public const int SW_HIDE = 0;
			public const int SW_SHOWNORMAL = 1;
			public const int SW_SHOWMINIMIZED = 2;
			public const int SW_SHOWMAXIMIZED = 3;
			public const int SW_SHOWNOACTIVATE = 4;
			public const int SW_SHOW = 5;

			public int Length;
			public int Flags;
			public int ShowCmd;
			public SysPoint MinPosition;
			public SysPoint MaxPosition;
			public SysRect NormalPosition;

			/// <summary>
			/// User-friendly state representation of ShowCmd property
			/// </summary>
			[XmlIgnore]
			public WindowState WindowState
			{
				get
				{
					WindowState ret;
					switch (ShowCmd)
					{
						case SW_SHOWNORMAL: ret = WindowState.Normal; break;
						case SW_SHOWMAXIMIZED: ret = WindowState.Maximized; break;
						default: ret = WindowState.Minimized; break;
					}
					return ret;
				}

				set
				{
					switch (value)
					{
						case WindowState.Normal: ShowCmd = SW_SHOWNORMAL; break;
						case WindowState.Maximized: ShowCmd = SW_SHOWMAXIMIZED; break;
						default: ShowCmd = SW_SHOWMINIMIZED; break;
					}
				}
			}
		}

		[DllImport("user32.dll")]
		private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement lpwndpl);

		[DllImport("user32.dll")]
		private static extern bool GetWindowPlacement(IntPtr hWnd, out WindowPlacement lpwndpl);

		// Set Window Normal --> Maximized
		private static void _SetWindowPlacement_RestoredMaximize(object sender, EventArgs e)
		{
			var win = sender as Window;
			win.ContentRendered -= _SetWindowPlacement_RestoredMaximize;
			win.StateChanged -= _SetWindowPlacement_RestoredMaximize;
			Lime.LimeMsg.Debug("* SetWindowPlacement: SW_SHOWMAXIMIZED");
			win.WindowState = WindowState.Maximized;
			Lime.LimeMsg.Debug("* SetWindowPlacement End: SW_SHOWMAXIMIZED, WindowState: {0}", win.WindowState);
		}

		/// <summary>
		/// Set position/dimension of a window.
		/// </summary>
		/// <param name="windowHandle">Window handle</param>
		/// <param name="placement">dimension and position of the window</param>
		/// <param name="windowObject">Window object</param>
		public static void SetWindowPlacement(IntPtr windowHandle, WindowPlacement placement, Window windowObject = null)
		{
			try
			{
				placement.Length = Marshal.SizeOf(typeof(WindowPlacement));
				placement.Flags = 0;
				//placement.ShowCmd = (placement.ShowCmd == WindowPlacement.SW_SHOWMINIMIZED ? WindowPlacement.SW_SHOWNORMAL : placement.ShowCmd);

				// To successfully maximize the window on the right screen, it must first be displayed as Normal
				if (placement.ShowCmd == WindowPlacement.SW_SHOWMAXIMIZED && windowObject != null)
				{
					Lime.LimeMsg.Debug("* SetWindowPlacement: SW_SHOWNORMAL");
					placement.ShowCmd = WindowPlacement.SW_SHOWNORMAL;
					if (!windowObject.IsLoaded)
						windowObject.ContentRendered += _SetWindowPlacement_RestoredMaximize;
					else
						windowObject.StateChanged += _SetWindowPlacement_RestoredMaximize;
				}
				SetWindowPlacement(windowHandle, ref placement);
			}
			catch (InvalidOperationException)
			{
				// Failed to restore placement
			}
		}

		/// <summary>
		/// Get the position/dimension of a window.
		/// </summary>
		/// <param name="windowHandle">Window handle</param>
		/// <returns>dimension and position of the window</returns>
		public static WindowPlacement GetWindowPlacement(IntPtr windowHandle)
		{
			WindowPlacement placement = new WindowPlacement();
			GetWindowPlacement(windowHandle, out placement);
			return placement;
		}


		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Window mouse Interop

		const int WS_EX_TRANSPARENT = 0x00000020;
		const int GWL_EXSTYLE = -20;

		[DllImport("user32.dll")]
		static extern int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32.dll")]
		static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

		/// <summary>
		/// Make a window transarent to mouse
		/// </summary>
		/// <param name="window">visual representing a window (for Popup, use: Popup.Child)</param>
		public static void SetWindowExTransparent(Visual window)
		{
			var hwnd = ((HwndSource)PresentationSource.FromVisual(window)).Handle;
			var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
		}


		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(out SysPoint lpPoint);


		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Key to Keypress

		public enum MapType : uint
		{
			MAPVK_VK_TO_VSC = 0x0,
			MAPVK_VSC_TO_VK = 0x1,
			MAPVK_VK_TO_CHAR = 0x2,
			MAPVK_VSC_TO_VK_EX = 0x3,
		}

		[DllImport("user32.dll")]
		public static extern int ToUnicode(
			uint wVirtKey,
			uint wScanCode,
			byte[] lpKeyState,
			[Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
			StringBuilder pwszBuff,
			int cchBuff,
			uint wFlags);

		[DllImport("user32.dll")]
		public static extern bool GetKeyboardState(byte[] lpKeyState);

		[DllImport("user32.dll")]
		public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

		/// <summary>
		/// Return the 'Keypress' character based on a key
		/// URL: George
		/// Credit: http://stackoverflow.com/questions/5825820/how-to-capture-the-character-on-different-locale-keyboards-in-wpf-c
		/// </summary>
		/// <param name="key">the input Key object, typically comming from a KeyEventArgs</param>
		/// <returns>the converted key as a char, or \0 if failed to convert</returns>
		public static char GetCharFromKey(Key key)
		{
			char ch = '\0'; // Starwer: changed default character

			int virtualKey = KeyInterop.VirtualKeyFromKey(key);
			byte[] keyboardState = new byte[256];
			GetKeyboardState(keyboardState);

			uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
			StringBuilder stringBuilder = new StringBuilder(2);

			int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
			switch (result)
			{
				case -1:
					break;
				case 0:
					break;
				case 1:
					{
						ch = stringBuilder[0];
						break;
					}
				default:
					{
						ch = stringBuilder[0];
						break;
					}
			}
			return ch;
		}


		#endregion


		// ----------------------------------------------------------------------------------------------
	}


}