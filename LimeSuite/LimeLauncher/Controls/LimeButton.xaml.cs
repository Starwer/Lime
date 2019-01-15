/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     07-03-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using Lime;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System;
using System.Windows.Input;

namespace LimeLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LimeButton.xaml
    /// </summary>
    public partial class LimeButton : LimeControl
    {

		/// <summary>
		/// Language section
		/// </summary>
		private const string LanguageSection = "Translator";


		public LimeButton()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: false, hasOptions: false);
		}


		protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
		{


			if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Icon" || e.PropertyName == "Type")
			{
				// Free previous resources of the control
				if (Content is ButtonBase wxobj)
				{
					wxobj.PreviewKeyDown -= FixFocus_PreviewKeyDown;
					wxobj.IsEnabledChanged -= Button_IsEnabledChanged;
				}

				// Create the Content of the wxMain Border: 
				//     <ButtonBase ToolTip="{Binding Desc}"  PreviewKeyDown="FixFocus_PreviewKeyDown">
				//         <StackPanel x:Name="wxPanel" Orientation="Horizontal">
				//         </StackPanel>
				//     </ButtonBase>

				ButtonBase wxButton;
				if (prop.Type == typeof(bool) || prop.Type == typeof(bool?))
				{
					wxButton = new ToggleButton();
					wxButton.SetBinding(ToggleButton.IsCheckedProperty, new Binding("Content"));
				}
				else
				{
					wxButton = new Button();
					wxButton.SetBinding(ButtonBase.CommandProperty, new Binding("Content"));
				}

				wxButton.SetBinding(ToolTipProperty, new Binding("Desc"));
				wxButton.PreviewKeyDown += FixFocus_PreviewKeyDown;
				wxButton.IsEnabledChanged += Button_IsEnabledChanged;

				wxButton.Content = new StackPanel()
				{
					Orientation = Orientation.Horizontal
				};

				wxMain.Child = wxButton;
			}

			var wxPanel = (Panel)((ButtonBase)wxMain.Child).Content;


			if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Name" || e.PropertyName == "ReqAdmin" || e.PropertyName == "ReqRestart" || e.PropertyName == "Icon")
			{
				wxPanel.Children.Clear();

				if (prop.ReqAdmin)
				{
					wxPanel.Children.Add(new LimeIcon()
					{
						IconKey = "Shield",
						ToolTip = LimeLanguage.Translate(LanguageSection, "ShieldTip", "ShieldTip")
					});
				}

				if (prop.ReqRestart)
				{
					wxPanel.Children.Add(new LimeIcon()
					{
						IconKey = "Warning",
						ToolTip = LimeLanguage.Translate(LanguageSection, "RestartTip", "RestartTip")
					});
				}

				if (!string.IsNullOrEmpty(prop.Icon))
				{
					wxPanel.Children.Add(new LimeIcon()
					{
						IconKey = prop.Icon
					});
				}

				wxPanel.Children.Add(new TextBlock()
				{
					Text = prop.Name
				});
			
			}
		}


		private void Button_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var wxobj = sender as ButtonBase;
			LimeMsg.Debug("LimeButton Button_IsEnabledChanged: {0}, {1}", wxobj.IsEnabled, wxobj.IsFocused);

			if (!wxobj.IsEnabled && wxobj.IsFocused)
			{
				LimeMsg.Debug("LimeButton Button_IsEnabledChanged: Fix focus");
				TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
				wxobj.MoveFocus(tRequest);
			}
		}
	}
}
