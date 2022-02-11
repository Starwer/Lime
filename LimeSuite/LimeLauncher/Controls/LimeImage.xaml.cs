/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     31-03-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using Lime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace LimeLauncher.Controls
{
	/// <summary>
	/// Interaction logic for LimeImage.xaml
	/// </summary>
	[SupportedOSPlatform("windows")]
	public partial class LimeImage : LimeControl, IAutoSlide
	{
        public LimeImage()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: false, hasOptions: false);

			// Initialize IAutoSlide
			SetBinding(AutoSlideListenerProperty, new Binding("Content") { Source = AutoSlideMessenger });
        }


		protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
            {
				// Convert the property to IList
				if (prop == null)
				{
					Cache = new List<object>();
				}
				else if (prop.Content is IList list)
                {
                    Cache = list;
                }
                else if (prop.Content is IEnumerable enu && !(prop.Content is string))
                {
                    Cache = new List<object>();
                    foreach (var item in enu)
                    {
                        Cache.Add(item);
                    }
                }
                else if (prop.Content != null)
                {
                    Cache = new List<object> { prop.Content };
                }
                else
                {
                    Cache = new List<object>();
                }

				Index = 0;

			}

            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content" || e.PropertyName == "ReadOnly")
            {
                if (prop == null || prop.ReadOnly || ReadOnly || prop.Content is string || !(prop.Content is IEnumerable))
                {
                    wxBack.Visibility = Visibility.Collapsed;
                    wxNext.Visibility = Visibility.Collapsed;
                }
                else
                {
                    wxBack.Visibility = Visibility.Visible;
                    wxNext.Visibility = Visibility.Visible;
                }

                Update();
            }
        }


		protected override void OnReadOnlyPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			var prop = wxMain.DataContext as LimeProperty;

			if (ReadOnly)
			{
				wxBack.Visibility = Visibility.Collapsed;
				wxNext.Visibility = Visibility.Collapsed;
			}
			else if (prop != null && !( prop.ReadOnly || prop.Content is string || !(prop.Content is IEnumerable) ))
			{
				wxBack.Visibility = Visibility.Visible;
				wxNext.Visibility = Visibility.Visible;
			}

			Update();
		}


		/// <summary>
		/// Get or set the relative level in hierarchy of the label (1.0: highest, 0.0: lowest) 
		/// </summary>
		public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }
        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(
            "Index", typeof(int), typeof(LimeImage),
            new FrameworkPropertyMetadata(0, OnIndexPropertyChanged)
            );

        private static void OnIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
			if (!(d is LimeImage wxThis)) return;

			var index = (int)e.NewValue;
            var count = wxThis.Cache != null ? wxThis.Cache.Count : 0;

            // Coerce
            if (index >= count) index = count - 1;
            if (index < 0) index = 0;
            if (index != (int)e.NewValue)
            {
                wxThis.Index = index;
            }
            else
            {
                wxThis.Update();
            }
        }


		/// <summary>
		/// Get or set the Icon Key. This should be within the references of the Icon dictionary 
		/// </summary>
		public string IconKey
		{
			get { return (string)GetValue(IconKeyProperty); }
			set { SetValue(IconKeyProperty, value); }
		}
		public static readonly DependencyProperty IconKeyProperty = DependencyProperty.Register(
			"IconKey", typeof(string), typeof(LimeImage),
			new FrameworkPropertyMetadata(null, OnIconKeyPropertyChanged)
			);

		private static void OnIconKeyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is LimeImage wxThis)) return;

			wxThis.wxIcon.IconKey = e.NewValue as string;
		}

		/// <summary>
		/// Gets or sets the image to be represented
		/// </summary>
		public IList Cache
        {
            get { return _Cache; }
            private set
            {
                if (value != _Cache) {
                    _Cache = value;
                    Update();
                }
            }
        }
        private IList _Cache = null;

		private const string LanguageSection = "Types";

		// Double buffer content
		private Border wxForeground = null;
		private Border wxBackground = null;

		/// <summary>
		/// Loading/refreshing of the Image
		/// </summary>
		public void Update()
        {
            var count = Cache != null ? Cache.Count : 0;

			var obj = Index < count ? Cache[Index] : null;
			var img = LimeLib.ImageSourceFrom(obj);

			// Swap buffers 
			var tmp = wxBackground;
			wxBackground = wxForeground;
			wxForeground = tmp;

			// Create buffer if required
			if (wxForeground == null)
			{
				wxForeground = new Border()
				{
					Child = new Image()
				};

				wxMain.Children.Add(wxForeground);
			}

			// Complete the swap
			if (wxBackground != null)
			{
				Panel.SetZIndex(wxBackground, 2);
				wxBackground.IsEnabled = false;

				// Free image
				if (img == null || !Global.User.EnableAnimations)
				{
					((Image)wxBackground.Child).Source = null;
				}
			}

			Panel.SetZIndex(wxForeground, 5);
			wxForeground.IsEnabled = false;

			// Update foreground image
			Image wxImage = (Image)wxForeground.Child;
			wxImage.Source = img;

			wxForeground.ToolTip = null;

			if (img == null)
			{
				wxIcon.Visibility = Visibility.Visible;
				wxForeground.IsEnabled = false;
			}
			else
			{
				LimeLib.LifeTrace(img);

				wxIcon.Visibility = Visibility.Collapsed;
				wxForeground.IsEnabled = true;

				if (ToolTipEnable && obj is TagLib.IPicture pic)
				{
					var key = pic.Type.ToString();
					var keyval = string.Format("{0}.{1}.name", typeof(TagLib.PictureType).Name, key);
					var translated = LimeLanguage.Translate(LanguageSection, keyval, key);
					keyval = string.Format("{0}.{1}.desc", typeof(TagLib.PictureType).Name, key);
					var desc = LimeLanguage.Translate(LanguageSection, keyval);
					string tooltip = translated;

					if (!string.IsNullOrWhiteSpace(desc) && translated != desc && keyval != desc)
					{
						tooltip += " - "  + desc;
					}

					if (!string.IsNullOrWhiteSpace(pic.Filename) && !ReadOnly)
					{
						tooltip += Environment.NewLine + "[" + pic.Filename + "]";
					}

					if (!string.IsNullOrWhiteSpace(pic.Description) && pic.Filename != pic.Description)
					{
						tooltip += Environment.NewLine + pic.Description;
					}

					wxForeground.ToolTip = tooltip;
				}
			}


			if (wxBack.Visibility == Visibility.Visible)
            {
				if (Index == 0 && wxBack.IsFocused)
				{
					Keyboard.Focus(wxNext);
				}
                wxBack.IsEnabled = Index > 0;

				if (Index == count - 1 && wxNext.IsFocused)
				{
					Keyboard.Focus(wxBack);
				}
				wxNext.IsEnabled = Index < count - 1;

				var format = LimeLanguage.Translate("Text", "FormatRatio", "{0}/{1}");
				if (string.IsNullOrEmpty(format)) format = "{0}/{1}";
				wxText.Text = count > 0 ? string.Format(format, Index + 1, count) : "";
			}
		}


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var wxobj = e.OriginalSource as FrameworkElement;
            Index += wxobj.Name == "wxBack" ? -1 : 1;
        }

		private void Image_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (ReadOnly && Visibility == Visibility.Visible && IsEnabled && e.ChangedButton == MouseButton.Left)
			{
				var count = Cache != null ? Cache.Count : 0;
				Index = Index >= count - 1 ? 0 : Index + 1;
				Skip = true;
			}
		}
		private bool Skip = false;

		/// <summary>
		/// Enable to display tooltips about the image
		/// </summary>
		public bool ToolTipEnable
		{
			get { return (bool)GetValue(ToolTipEnableProperty); }
			set { SetValue(ToolTipEnableProperty, value); }
		}
		public static readonly DependencyProperty ToolTipEnableProperty = DependencyProperty.Register(
			"ToolTipEnable", typeof(bool), typeof(LimeImage),
			new FrameworkPropertyMetadata(true, OnToolTipEnablePropertyChanged)
			);

		private static void OnToolTipEnablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is LimeImage wxThis)) return;
			wxThis.Update();
		}



		// --------------------------------------------------------------------------------------------------
		#region IAutoSlide Implementation

		/// <summary>
		///  Create a dummy DependencyProperty which receive the "Next-Slide" message via the NotifyPropertyChanged
		///  mechanism. Its value toogles every new request.
		/// </summary>
		/// <remarks>
		/// This Dependency property must be bound in the control constructor, using:
		/// <code>
		///     SetBinding(AutoSlideListenerProperty, new Binding("Content") { Source = AutoSlideMessenger });
		/// </code>
		/// </remarks>
		public bool AutoSlideListener
		{
			get { return (bool)GetValue(AutoSlideListenerProperty); }
			set { SetValue(AutoSlideListenerProperty, value); }
		}
		public static readonly DependencyProperty AutoSlideListenerProperty = DependencyProperty.Register(
			"AutoSlideListener", typeof(bool), typeof(LimeImage),
			new FrameworkPropertyMetadata(false, OnAutoSlideListenerPropertyChanged)
			);

		private static void OnAutoSlideListenerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is LimeImage wxThis)) return;
			wxThis.NextSlide();
		}

		/// <summary>
		/// Go to next Image in or loop back to start
		/// </summary>
		public void NextSlide()
		{
			if (ReadOnly && Visibility == Visibility.Visible && IsEnabled && !Skip)
			{
				var count = Cache != null ? Cache.Count : 0;
				Index = Index >= count - 1 ? 0 : Index + 1;
			}

			Skip = false;
		}

		#endregion

	}
}
