/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     06-02-2018
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

namespace LimeLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LimeDropDown.xaml
    /// </summary>
    public partial class LimeDropDown : LimeControl
    {
        public LimeDropDown()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory();
        }



        protected override void OnBoundDataContextChanged(DependencyPropertyChangedEventArgs e)
        {
            // Initialize
            wxMenu.ItemsSource = null;
        }


        protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Type")
            {

				// Create the Menu (ListBox): Use different schemes depending on the type

				wxMenu.Items.Clear();
				wxMenu.ItemTemplate = null;
				wxMenu.ItemsSource = null;

				var attrs = prop?.PInfo?.GetCustomAttributes(typeof(PickCollectionAttr), true);

				if (prop == null)
				{
					// Not supported
					wxMenu.ItemsSource = null;
				}
                else if (attrs != null && attrs.Length > 0)
                {
                    // PickCollectionAttr
                    var attr = (PickCollectionAttr)attrs[0];
                    wxMenu.DisplayMemberPath = attr.Name ?? attr.Key ?? attr.Value;
                    wxMenu.SelectedValuePath = attr.Key;
                    wxMenu.SetBinding(ListBox.ItemsSourceProperty, new Binding(attr.Items) { Source = prop.Source });
                }
                else if (prop.Type.IsEnum)
                {
                    bool checkbox = prop.Type.IsDefined(typeof(FlagsAttribute), inherit: false);
                    if (checkbox)
                    {
                        // Enum Flags
                        SetItemSourceEnumFlags(prop.Type);
					}
					else
                    {
                        // Enum
                        var items = new List<ItemViewModel>();
                        foreach (var key in Enum.GetNames(prop.Type))
                        {
                            items.Add(new ItemViewModel(key, prop.Type));
                        }
                        wxMenu.ItemTemplate = (DataTemplate)Resources["EnumItemTemplate"];
                        wxMenu.ItemsSource = items;
                    }
                }
                else if (typeof(IPickCollection).IsAssignableFrom(prop.Type))
                {
                    // do nothing for now
                    var items = new List<ItemViewModel>();

                    if (prop.Content is IPickCollection collec)
                    {
                        foreach (var key in collec.Keys)
                        {
                            items.Add(new ItemViewModel(key));
                        }

                        if (collec.Names != null)
                        {
                            int idx = 0;
                            foreach (var val in collec.Names)
                            {
                                items[idx++].Name = val;
                            }
                        }

                    }

                    wxMenu.ItemTemplate = (DataTemplate)Resources["PickCollectionItemTemplate"];
                    wxMenu.ItemsSource = items;
                }
                else if (prop.Type == typeof(FontFamily))
                {
                    // FontFamily
                    var familySrc = Fonts.SystemFontFamilies;
                    var items = new List<ItemViewModel>();
                    items.Add(new ItemViewModel("", "<default>"));
                    foreach (var val in familySrc)
                    {
                        items.Add(new ItemViewModel(val.ToString()));
                    }

                    wxText.SetBinding(TextBlock.FontFamilyProperty,
                        new Binding("Value")
                        {
                            Converter = (IValueConverter)Resources["FontFamilyConvert"]
                        }
                        );
                    wxMenu.ItemTemplate = (DataTemplate)Resources["FontFamilyItemTemplate"];
                    wxMenu.ItemsSource = items;
                }
                else if (prop.Type == typeof(FontWeight) || prop.Type == typeof(FontStyle))
                {
                    var type = prop.Type == typeof(FontWeight) ? typeof(FontWeights) : typeof(FontStyles);
                    var elms = type.GetProperties();
                    var items = new List<ItemViewModel>();
                    foreach (var pi in elms)
                    {
                        items.Add(new ItemViewModel(pi.Name, prop.Type));
                    }
                    wxMenu.ItemTemplate = (DataTemplate)Resources[prop.Type.Name + "ItemTemplate"];
                    wxMenu.ItemsSource = items;
                }
                else
                {
                    // Not supported
                    wxMenu.ItemsSource = null;
                }
            }


            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
            {
				if (prop == null)
				{
					// Do nothing
				}
				else if (Cache == null || !Cache.Equals(prop.Content))
                {
                    bool checkbox = prop.Type.IsDefined(typeof(FlagsAttribute), inherit: false);
                    Cache = prop.Value;
                    ValidateValue(set: false);
                }
            }
        }


        private const string LanguageSection = "Types";


        private class ItemViewModel
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string Desc { get; set; }


            public ItemViewModel(string key, string name = null, string desc = null)
            {
                Key = key;
                Name = name ?? key;
                Desc = desc;
            }

            public ItemViewModel(string key, Type type)
            {
                Key = key;

                if (type != null)
                {
                    var keyval = string.Format("{0}.{1}.name", type.Name, key);
                    var translated = LimeLanguage.Translate(LanguageSection, keyval, key);
                    keyval = string.Format("{0}.{1}.desc", type.Name, key);
                    var desc = LimeLanguage.Translate(LanguageSection, keyval);
                    string tooltip = keyval != desc ? desc : null;

                    Name = translated;
                    Desc = tooltip;
                }
                else
                {
                    Name = key;
                }
            }
        }


        private string Cache
        {
            get
            {
                return (string)wxMain.Tag;
            }

            set
            {
                LimeMsg.Debug("LimeDropDown Cache={0}", value);
                var prop = wxMain.DataContext as LimeProperty;

                // Value override
                if (value == "" && prop.Type == typeof(FontFamily))
                {
                    value = " ";
                }

                wxMain.Tag = value;

                if (wxMenu.ItemsSource != null)
                {
                    wxMenu.SelectedValue = value;
                }
				else if (prop.Type != null && prop.Type.IsEnum)
				{
					// Update Enum Flags checkbox's
					var inval = 0;
					var type = prop.Type;
					if (value != null)
					{
						TypeConverter converter = TypeDescriptor.GetConverter(type);
						inval = (int)converter.ConvertFrom(value);
					}

					int idx = 0;
					foreach (var key in Enum.GetNames(type))
					{
						int val = (int)Enum.Parse(type, key);
						if (val != 0)
						{
							var wxObj = (ListBoxItem)wxMenu.Items[idx++];
							var content = (CheckBox)wxObj.Content;
							content.IsChecked = (val & inval) != 0;
						}
					}
				}


                if (ValidateOnChange)
                {
                    ValidateValue(set: true);
                }
            }
        }


        private void ValidateValue(bool set)
        {
            var prop = wxMain.DataContext as LimeProperty;
			var type = prop.Type;

			if (set)
			{
				prop.Value = Cache;
			}


			if (wxMenu.SelectedItem is ItemViewModel item)
            {
                wxText.Text = item.Name;
            }
			else if (type.IsEnum && type.IsDefined(typeof(FlagsAttribute), inherit: false))
			{
				var sep = LimeLanguage.Translate("Text", "ListSeparator", ", ");
				var sb = new StringBuilder();
				foreach (ListBoxItem wxObj in wxMenu.Items)
				{
					var wxcheck = (CheckBox)wxObj.Content;
					var content = wxcheck.Content as string;
					if (wxcheck.IsChecked == true)
					{
						if (sb.Length != 0) sb.Append(sep);
						sb.Append(content);
					}
				}
				wxText.Text = sb.ToString();
			}
			else
            {
                wxText.Text = prop.Value;
            }
        }


        /// <summary>
        /// Create an ItemsSource from an Enum
        /// </summary>
        /// <param name="type">Type of the data bound (must be an Enum)</param>
        private void SetItemSourceEnumFlags(Type type)
        {
			wxMenu.Items.Clear();

			foreach (var key in Enum.GetNames(type))
            {
                int val = (int)Enum.Parse(type, key);
                if (val != 0)
                {
                    var data = new ItemViewModel(key, type);

					var wxObj = new ListBoxItem()
					{
						DataContext = key,
						Content = new CheckBox
						{
							Content = data.Name,
							ToolTip = data.Desc
                        }
                    };

                    wxMenu.Items.Add(wxObj);
                }
            }

        }

        /// <summary>
        /// Convert Flags checked back to the source Enum
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var wxObj = e.OriginalSource as CheckBox;
            bool check = (bool)wxObj.IsChecked;

            var prop = wxMain.DataContext as LimeProperty;

            int ret = 0;
            TypeConverter converter = TypeDescriptor.GetConverter(prop.Type);
            if (Cache != null) ret = (int)converter.ConvertFrom(Cache);
            int val = (int)converter.ConvertFrom(wxObj.DataContext);

            // Mask bit
            if (check)
                ret |= val;
            else
                ret &= ~val;

            Cache = (string)converter.ConvertTo(ret, typeof(string));
        }


        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count != 1) return;
            if (e.AddedItems[0] is ItemViewModel cont)
            {
                Cache = cont.Key;
            }
            else if (e.AddedItems[0] is string str)
            {
                Cache = str;
            }
        }

        private void ListBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var wxObj = e.OriginalSource as FrameworkElement;
            if (wxObj == null) return;

            var prop = wxMain.DataContext as LimeProperty;
            if (prop == null) return;

            bool checkbox = prop.Type.IsDefined(typeof(FlagsAttribute), inherit: false);
            if (!checkbox)
            {
                wxPopup.IsOpen = false;
                wxMain.IsChecked = false;
                Keyboard.Focus(wxMain);
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
                    LimeMsg.Debug("LimeDropDown Popup_Opened: subscribe _ScrollChangedEventHandler: {0}", scrollViewer);
                    scrollViewer.ScrollChanged += _ScrollChangedEventHandler;
                }
            }

            // Autosize
            if (wxPopup.Placement == PlacementMode.Bottom || wxPopup.Placement == PlacementMode.Top)
            {
                wxMenu.MinWidth = wxMain.ActualWidth;
                wxPopup.HorizontalOffset = wxMain.Margin.Left;
            }

            // Readjust value if not yet selected
            if (wxMenu.ItemsSource != null && Cache != null)
            {
                wxMenu.SelectedValue = Cache;
            }

            ListBoxItem item = null;
            if (wxMenu.SelectedItem != null)
            {
                item = wxMenu.ItemContainerGenerator.ContainerFromItem(wxMenu.SelectedItem) as ListBoxItem;
            }
            if (item == null) item = WPF.FindFirstChild<ListBoxItem>(wxMenu);
            if (item != null) Keyboard.Focus(item);
        }


        private void Popup_Closed(object sender, EventArgs e)
        {
            LimeMsg.Debug("Popup_Closed");
            wxMain.IsChecked = wxMain.IsFocused && wxMain.IsChecked == true;
            var prop = wxMain.DataContext as LimeProperty;
            if (Cache != null) ValidateValue(set: true);

            // Unsubscribe to every parent scrollviewers
            if (_ScrollChangedEventHandler != null)
            {
                foreach (var scrollViewer in GetParentScrollViewers())
                {
                    LimeMsg.Debug("LimeDropDown Popup_Closed: unsubscribe _ScrollChangedEventHandler: {0}", scrollViewer);
                    scrollViewer.ScrollChanged -= _ScrollChangedEventHandler;
                }

                _ScrollChangedEventHandler = null;
            }
        }


        void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Ignore Extend changes
            if (wxPopup.IsOpen && (e.VerticalChange != 0 || e.HorizontalChange != 0 || e.ViewportHeightChange != 0 || e.ViewportWidthChange != 0))
            {
                wxPopup.IsOpen = false;
                wxMain.IsChecked = false;
                Keyboard.Focus(wxMain);
            }
        }
        private ScrollChangedEventHandler _ScrollChangedEventHandler = null;

        IEnumerable<ScrollViewer> GetParentScrollViewers()
        {
            for (DependencyObject element = this; element != null; element = VisualTreeHelper.GetParent(element))
                if (element is ScrollViewer wxscroll) yield return wxscroll;
        }


        private void Popup_KeyDown(object sender, KeyEventArgs e)
        {
            // Avoid bubbling keys up
            LimeMsg.Debug("Popup_KeyDown: {0}", e.Key);
            e.Handled = true;
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            LimeMsg.Debug("ListBox_PreviewKeyDown: {0}", e.Key);

            var wxobj = e.OriginalSource as ListBoxItem;
            if (wxobj == null) return;
            if (Keyboard.Modifiers != 0) return;

            bool close = false;
            switch (e.Key)
            {
                case Key.Enter:
                    var wxcheck = WPF.FindFirstChild<CheckBox>(wxobj);
                    if (wxcheck != null)
                    {
                        wxcheck.IsChecked = !wxcheck.IsChecked;
                        e.Handled = true;
                    }
                    else
                    {
                        close = true;
                    }
                    break;

                case Key.Escape:
                    var prop = wxMain.DataContext as LimeProperty;
                    Cache = prop.Value;
                    close = true;
                    break;

				case Key.Up:
					close = wxMenu.SelectedIndex == 0;
					break;

				case Key.Right:
                case Key.Left:
                    close = true;
                    break;
            }


            if (close)
            {
                wxPopup.IsOpen = false;
                wxMain.IsChecked = false;
                Keyboard.Focus(wxMain);
                e.Handled = true;
            }
        }


    }
}
