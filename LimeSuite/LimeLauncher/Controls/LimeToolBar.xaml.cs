/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     04-08-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using Lime;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System.Collections;
using WPFhelper;

namespace LimeLauncher.Controls
{
	/// <summary>
	/// Interaction logic for LimeToolBar.xaml
	/// </summary>
	public partial class LimeToolBar : LimeControl
    {

		/// <summary>
		/// Language section
		/// </summary>
		private const string LanguageSection = "Translator";


		public LimeToolBar()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: false, hasOptions: false);
		}


		protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
		{


			if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Icon" || e.PropertyName == "Content")
			{

				wxToolBar.Items.Clear();

				// Add ToolBar Icon (if any)
				if (!string.IsNullOrEmpty(prop.Icon))
				{
					wxToolBar.Items.Add(new LimeIcon()
					{
						IconKey = prop.Icon
					});
				}


				// Get the list of LimeProperties
				var list = prop.Content as IEnumerable;
				if (list == null) return;
				foreach (var elm in list)
				{
					if (elm is LimeProperty sprop && sprop.Visible)
					{

						// Create the Content of the wxMain Border: 
						//     <ButtonBase ToolTip="{Binding Desc}"  PreviewKeyDown="FixFocus_PreviewKeyDown">
						//         <StackPanel x:Name="wxPanel" Orientation="Horizontal">
						//         </StackPanel>
						//     </ButtonBase>

						ButtonBase wxButton;
						if (sprop.Type == typeof(bool) || sprop.Type == typeof(bool?))
						{
							wxButton = new ToggleButton();
							wxButton.SetBinding(ToggleButton.IsCheckedProperty, new Binding("Content"));
						}
						else
						{
							wxButton = new Button();
							wxButton.SetBinding(ButtonBase.CommandProperty, new Binding("Content"));
						}

						wxButton.DataContext = sprop;
						wxButton.SetBinding(ToolTipProperty, new Binding("Desc"));
						wxButton.PreviewKeyDown += FixFocus_PreviewKeyDown;

						var wxPanel = new StackPanel()
						{
							Orientation = Orientation.Horizontal
						};
						wxButton.Content = wxPanel;

						if (!string.IsNullOrEmpty(sprop.Icon))
						{
							wxPanel.Children.Add(new LimeIcon()
							{
								IconKey = sprop.Icon
							});
						}

						//wxPanel.Children.Add(new TextBlock()
						//{
						//	Text = sprop.Name
						//});


						wxToolBar.Items.Add(wxButton);
					}
					else if (elm == null)
					{
						wxToolBar.Items.Add( new Separator());
					}

				}

			}

		}


		/// <summary>
		/// Get or set the control Orientation 
		/// </summary>
		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}
		public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
			"Orientation", typeof(Orientation), typeof(LimeToolBar),
			new FrameworkPropertyMetadata(Orientation.Horizontal, OnOrientationPropertyChanged)
			);

		private static void OnOrientationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var wxThis = d as LimeToolBar;
			wxThis.wxToolBarTray.Orientation = (Orientation)e.NewValue;
		}




		/// <summary>
		/// Fix the key-navigation
		/// </summary>
		/// <param name="sender">wxToolBar</param>
		/// <param name="e"></param>
		private void ToolBar_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			var wxobj = e.OriginalSource as FrameworkElement;
			if (wxobj == null) return;
			if (Keyboard.Modifiers != 0) return;

			LimeMsg.Debug("LimeToolBar wxToolBar_PreviewKeyDown: {0}", wxobj);

			TraversalRequest tRequest = null;

			if (wxToolBar.IsOverflowOpen)
			{
				switch (e.Key)
				{
					case Key.Up:
					case Key.Down:
					case Key.Escape:
						e.Handled = true;
						break;
				}

				if (e.Handled)
				{
					wxToolBar.IsOverflowOpen = false;

					// Keep focus on the ToolBar (preferably on the Overflow button)
					var wxovf = (UIElement)WPF.FindFirstChild<ToggleButton>(wxToolBar, (elm) => { return elm.Name == "OverflowButton"; });
					if (wxovf == null) wxovf = wxToolBar.Items?[0] as UIElement;
					if (wxovf != null) Keyboard.Focus(wxovf);
				}
			}
			else
			{
				// Detect whether focused item is the first enabled or the last enabled
				bool first = true;
				bool last = false;
				foreach (var wxitem in wxToolBar.Items)
				{
					if (wxitem is UIElement wxui)
					{
						if (wxitem == wxobj)
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
							first = false;
						}
					}
				}

				// Detect focused Overflow button
				if (e.Source == wxToolBar && wxobj is ToggleButton)
				{
					last = true;
				}

				// Get orientation
				bool hor = wxToolBar.Orientation == Orientation.Horizontal;

				switch (e.Key)
				{
					case Key.Up:
						if (first || hor) tRequest = new TraversalRequest(FocusNavigationDirection.Up);
						break;
					case Key.Down:
						if (last || hor) tRequest = new TraversalRequest(FocusNavigationDirection.Down);
						break;
					case Key.Left:
						if (first || !hor) tRequest = new TraversalRequest(FocusNavigationDirection.Left);
						break;
					case Key.Right:
						if (last || !hor) tRequest = new TraversalRequest(FocusNavigationDirection.Right);
						break;
					case Key.Tab:
						if (last || !hor) tRequest = new TraversalRequest(FocusNavigationDirection.Next);
						break;
				}
			}


			if (tRequest != null)
			{
				wxToolBar.MoveFocus(tRequest);
				e.Handled = true;
			}

		}

	}
}
