/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     18-02-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System;
using System.Windows.Controls;
using System.Windows.Input;
using Lime;
using System.Windows;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using WPFhelper;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Data;
using System.Runtime.Versioning;

namespace LimeLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LimeColorPicker.xaml
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class LimeColorPicker : LimeControl
    {
        public LimeColorPicker()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory();
        }


        protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
            {
                // Support different kinds of colors
				if (prop == null)
				{
					// Do Nothing
				}
                else if (prop.Type == typeof(System.Drawing.Color) )
                {
                    var val = (System.Drawing.Color) prop.Content;
                    Cache = Color.FromArgb(val.A, val.R, val.G, val.B);
                }
                else if (prop.Type == typeof(Color))
                {
                    Cache = (Color)prop.Content;
                }
            }
        }


        private Color Cache
        {
            get
            {
                return wxPicker.SelectedColor;
            }
            set
            {
                wxPicker.SelectedColor = value;
            }
        }


        private void wxPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            if (ValidateOnChange)
            {
                var prop = wxMain.DataContext as LimeProperty;
                prop.Value = e.NewValue.ToString();
            }
        }


        private void wxMain_Click(object sender, RoutedEventArgs e)
        {
            wxPopup.IsOpen = wxMain.IsChecked == true;
            e.Handled = true;
        }


        private void Popup_Opened(object sender, EventArgs e)
        {
            LimeMsg.Debug("Popup_Opened");

            // Subscribe to every parent scrollviewers to detect scrolling there (and close the DropDown)
            if (_ScrollChangedEventHandler == null)
            {
                _ScrollChangedEventHandler = new ScrollChangedEventHandler(ScrollViewer_ScrollChanged);
                foreach (var scrollViewer in GetParentScrollViewers())
                {
                    scrollViewer.ScrollChanged += _ScrollChangedEventHandler;
                }
            }

            // Autosize
            if (wxPopup.Placement == PlacementMode.Bottom || wxPopup.Placement == PlacementMode.Top)
            {
                wxPopupBorder.MinWidth = wxMain.ActualWidth;
                wxPopup.HorizontalOffset = wxMain.Margin.Left;
            }

            var item = WPF.FindFirstChild<TextBox>(wxPicker);
            if (item != null) Keyboard.Focus(item);
        }

        private void Popup_Closed(object sender, EventArgs e)
        {
            LimeMsg.Debug("Popup_Closed");
            wxMain.IsChecked = wxMain.IsFocused && wxMain.IsChecked == true;
            var prop = wxMain.DataContext as LimeProperty;
            prop.Value = Cache.ToString();

            // Unsubscribe to every parent scrollviewers
            if (_ScrollChangedEventHandler != null)
            {
                foreach (var scrollViewer in GetParentScrollViewers())
                {
                    scrollViewer.ScrollChanged -= _ScrollChangedEventHandler;
                }

                _ScrollChangedEventHandler = null;
            }
        }

        void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Ignore Extend changes
            if (e.OriginalSource != sender) return;
            if (wxPopup.IsOpen && (e.VerticalChange != 0 || e.HorizontalChange !=0 || e.ViewportHeightChange != 0 || e.ViewportWidthChange != 0) )
            {
                LimeMsg.Debug("LimeColorPicker ScrollViewer_ScrollChanged: {0}", e.OriginalSource);
                wxPopup.IsOpen = false;
                wxMain.IsChecked = false;
                Keyboard.Focus(wxMain);
            }
        }
        private ScrollChangedEventHandler _ScrollChangedEventHandler = null;

        IEnumerable<ScrollViewer> GetParentScrollViewers()
        {
            for (DependencyObject element = this; element != null; element = VisualTreeHelper.GetParent(element))
                if (element is ScrollViewer) yield return element as ScrollViewer;
        }


        private void Popup_KeyDown(object sender, KeyEventArgs e)
        {
            // Avoid bubbling keys up
            LimeMsg.Debug("Popup_KeyDown: {0}", e.Key);
            //e.Handled = true;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var wxobj = e.OriginalSource as TextBox;
            if (wxobj == null) return;
            if (Keyboard.Modifiers != 0) return;


            // Avoid bubbling keys up
            LimeMsg.Debug("LimeColorPicker TextBlock_PreviewKeyDown: {0}", e.Key);
            //e.Handled = true;
            TraversalRequest tRequest = null;
            switch (e.Key)
            {
                case Key.Up: tRequest = new TraversalRequest(FocusNavigationDirection.Up); break;
                case Key.Down: tRequest = new TraversalRequest(FocusNavigationDirection.Down); break;
                case Key.Enter:
                case Key.Escape:
                    if (e.Key == Key.Escape)
                    {
                        BindingExpression be = wxobj.GetBindingExpression(TextBox.TextProperty);
                        if (be != null) be.UpdateTarget();
                    }
                    wxPopup.IsOpen = false;
                    wxMain.IsChecked = false;
                    Keyboard.Focus(wxMain);
                    e.Handled = true;
                    break;
            }

            if (tRequest != null)
            {
                wxobj.MoveFocus(tRequest);
                e.Handled = true;
            }
        }
    }
}
