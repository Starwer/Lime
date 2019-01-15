/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     28-01-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System.Windows.Controls;
using Lime;
using System.Windows.Data;
using System.Windows;
using System;
using System.ComponentModel;
using WPFhelper;
using System.Windows.Media;
using System.Collections;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Linq;

namespace LimeLauncher.Controls
{

	// --------------------------------------------------------------------------------------------------
	#region Types

	/// <summary>
	/// Define a Slide-able element
	/// </summary>
	public interface IAutoSlide
	{
		/// <summary>
		/// Go to next slide
		/// </summary>
		void NextSlide();

		/// <summary>
		/// Dependency property conveying the NExt-slide message
		/// </summary>
		bool AutoSlideListener { get; set; }
	}


	#endregion


	/// <summary>
	/// LimeControl provides the proper control for a LimePropoerty, depending on its content-type
	/// </summary>
	public partial class LimeControl : UserControl
    {

        // --------------------------------------------------------------------------------------------------
        #region Constants & Properties

        /// <summary>
        /// Column Group name for options
        /// </summary>
        private const string GridGroupColOptions = "LimeControlColOptions";

        /// <summary>
        /// Column Group name for header
        /// </summary>
        private const string GridGroupColHeader = "LimeControlColHeader";

		/// <summary>
		/// Language section
		/// </summary>
		private const string LanguageSection = "Translator";

		/// <summary>
		/// Contains the base LimeControl, potential embedding the object (this)
		/// </summary>
		public LimeControl Base { get; protected set; }

#if DEBUG
		/// <summary>
		/// Debug Only: Give a string helpig identifying the object in the debugger
		/// </summary>
		public string ADebug { get; protected set; }
#endif

        /// <summary>
        /// Keep trak of the main control grid
        /// </summary>
        protected Grid MainGrid;


        #endregion
        

        // --------------------------------------------------------------------------------------------------
        #region ctors & Factory


        /// <summary>
        /// Base control constructor
        /// </summary>
        public LimeControl()
        {
            LimeLib.LifeTrace(this);

            Base = this;

            // This Control will be defined when bound
            this.SetBinding(BoundDataContextProperty, new Binding());
        }


		/// <summary>
		/// Must be called by the derived LimeControl after its InitializeComponent() call.
		/// </summary>
		/// <param name="hasHeader">Define if the header with the property name is visible</param>
		/// <param name="hasOptions">Define if the header with the property options is visible</param>
		protected void Factory(bool hasHeader = true, bool hasOptions = true)
        {
            /* Code to create the Xaml equivalent to: 

                <Grid x:Name="wxGrid"  ToolTip="{Binding Desc}" >
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="Auto" SharedSizeGroup="sysGrid"/>
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>

                    <c:SysIcons Grid.Column="0"/>
                    <TextBlock Grid.Column="1" Text="{Binding Name}" />
                    <!-- Grid.Column="2" set programmatically -->
                </Grid>
            */

            Base = this;

            // Create Grid ---

            var wxGrid = new Grid();
			if (hasHeader)
			{
				wxGrid.SetBinding(Grid.ToolTipProperty, new Binding("Desc"));
			}

            int idx = 0;

            // Option column ---

            if (hasOptions)
            {
                var wxCol = new ColumnDefinition
                {
                    Width = new GridLength(0, GridUnitType.Auto),
                    SharedSizeGroup = GridGroupColOptions
                };
                wxGrid.ColumnDefinitions.Add(wxCol);

                var wxopt = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                Grid.SetColumn(wxopt, idx++);
                wxGrid.Children.Add(wxopt);
            }


            // Header column ---

            if (hasHeader)
            {
                var wxCol = new ColumnDefinition
                {
                    Width = new GridLength(0, GridUnitType.Auto),
                    SharedSizeGroup = GridGroupColHeader
                };
                wxGrid.ColumnDefinitions.Add(wxCol);

                var wxtext = new TextBlock();
                wxtext.SetResourceReference(StyleProperty, "LimeControlHeaderTextStyle");
                wxtext.SetBinding(TextBlock.TextProperty, new Binding("Name"));

                Grid.SetColumn(wxtext, idx++);
                wxGrid.Children.Add(wxtext);
            }

            // Value column ---

            var wxCol2 = new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            };
            wxGrid.ColumnDefinitions.Add(wxCol2);

            if (GetType().IsSubclassOf(typeof(LimeControl)))
            {
                var wxobj = Content as FrameworkElement;
                this.Content = null;

                //wxobj.DataContext = obj;
                wxobj.HorizontalAlignment = HorizontalAlignment.Stretch;
                Grid.SetColumn(wxobj, idx++);
                wxGrid.Children.Add(wxobj);

                // Manual binding to enable the detection of binding change
                this.SetBinding(BoundDataContextProperty, new Binding());
            }


            // Set Grid as content ---

            MainGrid = wxGrid;
            Content = wxGrid;

		}



		#endregion


		// --------------------------------------------------------------------------------------------------
		#region DataContext Handling

		public static readonly DependencyProperty BoundDataContextProperty = DependencyProperty.Register(
            "BoundDataContext", typeof(object), typeof(LimeControl), new PropertyMetadata(null, OnBoundDataContextChanged)
            );


        private static void OnBoundDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wxThis = d as LimeControl;
			var isLimeControl = wxThis !=null && wxThis.GetType() == typeof(LimeControl);

			//LimeMsg.Debug("LimeControl OnBoundDataContextChanged: {0}", e.NewValue);
			if (e.OldValue is LimeProperty old)
            {
                old.PropertyChangedWeak -= wxThis.TriggerDataContextChanged;
            }

            var prop = e.NewValue as LimeProperty;
            var content = prop != null ? prop.Content : e.NewValue;

            // Unpack from Matryoshka
            if (content is IMatryoshka matr)
            {
                // Find the most inner matryoshka object
                while (matr.Content is IMatryoshka sub)
                {
                    matr = sub;
                }
                content = matr.Content;

                // Repack into a LimeProperty
                if (prop != null)
                {
                    prop = new LimeProperty(null, matr, "Content", prop);
                }
                else
                {
                    prop = new LimeProperty(null, matr, "Content");
                }
            }

            // Encapsulate into LimeProperty
            if (prop == null && content != null)
            {
                prop = new LimeProperty(null, content);
                e = new DependencyPropertyChangedEventArgs(e.Property, e.OldValue, prop);
            }

            // Select the right LimeControl sub-class depending on the Type of the LimeProperty
            LimeControl wxbind = wxThis;

            if (isLimeControl)
            {
#if DEBUG
                wxThis.ADebug = prop != null ? string.Format("{1} [{0}] (base)", prop.Type, prop.Ident ?? prop.Name) : "null (base)";
#endif
				// Type dispatcher
				var type = LimeControlSelector(prop);

				LimeControl wxobj = null;
				if (type == null)
				{
					// do nothing
				}
				else if (type != wxThis.Content?.GetType())
				{
					wxobj = (LimeControl)Activator.CreateInstance(type);
					wxobj.Base = wxThis;
				}
				else
				{
					wxobj = (LimeControl)wxThis.Content;
				}

				wxThis.Content = wxbind = wxobj;

#if DEBUG
				if (wxobj != null)
					wxobj.ADebug = prop != null ? string.Format("{1} [{0}] (content)", prop.Type, prop.Ident ?? prop.Name) : "null (content)";
#endif

			}

			// Bind property to this Control (or content)
			if (prop != null)
			{
				prop.PropertyChangedWeak += wxbind.TriggerDataContextChanged;
			}

			if (wxThis != null && wxThis.Content is FrameworkElement wxcont)
			{
				wxcont.DataContext = prop;
			}

            // Event trigger
            wxbind?.OnBoundDataContextChanged(e);


            if (!isLimeControl && wxbind != null)
            {
                // Trigger Binding initialization
                wxbind.TriggerDataContextChanged(prop, new PropertyChangedEventArgs(null));
            }
        }
        

        /// <summary>
        /// Trigger on DataContext change event.
        /// </summary>
        /// <param name="sender">LimeProperty source</param>
        /// <param name="e">Binding change if null</param>
        public void TriggerDataContextChanged(object sender, PropertyChangedEventArgs e)
        {
            var prop = sender as LimeProperty;

            // Options Handling
            if (MainGrid != null && (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "ReqAdmin" || e.PropertyName == "ReqRestart"))
            {
                var count = VisualTreeHelper.GetChildrenCount(MainGrid);
				if (VisualTreeHelper.GetChild(MainGrid, 0) is StackPanel wxobj && count > 1)
				{
					wxobj.Children.Clear();
					if (OptionsEnabled && prop != null)
					{
						if (prop.ReqAdmin)
						{
							wxobj.Children.Add(new LimeIcon()
							{
								IconKey = "Shield",
								ToolTip = LimeLanguage.Translate(LanguageSection, "ShieldTip", "ShieldTip")
							});
						}
						if (prop.ReqRestart)
						{
							wxobj.Children.Add(new LimeIcon()
							{
								IconKey = "Warning",
								ToolTip = LimeLanguage.Translate(LanguageSection, "RestartTip", "RestartTip")
							});
						}
					}
				}
			}

            // Header Handling
            if (MainGrid != null && (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Name"))
            {
                TextBlock wxobj = null;
                int idx = 1;
                var count = VisualTreeHelper.GetChildrenCount(MainGrid);
                if (count > 2) wxobj = VisualTreeHelper.GetChild(MainGrid, idx) as TextBlock;
                if (wxobj == null && count > 1) wxobj = VisualTreeHelper.GetChild(MainGrid, idx = 0) as TextBlock; // no options
                if (wxobj != null)
                {
                    var wxcol = MainGrid.ColumnDefinitions[idx];
                    if (HeaderEnabled && prop != null && !string.IsNullOrWhiteSpace(prop.Name))
                    {
                        wxobj.Visibility = Visibility.Visible;
                        wxcol.SharedSizeGroup = GridGroupColHeader;
                    }
                    else
                    {
                        wxobj.Visibility = Visibility.Collapsed;
                        wxcol.SharedSizeGroup = null;
                    }
                }
            }

            // Visibility Handling
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Visible")
            {
                Visibility = prop == null || prop.Visible ? Visibility.Visible : Visibility.Collapsed;
            }


            // Callback control
            OnPropertyChanged(prop, e);


            // Value Handling
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
            {
                var value = prop?.Content;
                var ev = new LimeControlEventArgs(this, sender, value);

                //LimeMsg.Debug("LimeTextBox TriggerDataContextChanged");
                Base.ValueValidated?.Invoke(this, ev);
            }

        }


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region AutoSlide handling

		/// <summary>
		/// Create a dummy LimeProperty which will convey the "Next-Slide" message via the NotifyPropertyChanged mechanism
		/// </summary>
		protected static LimeProperty AutoSlideMessenger = new LimeProperty("AutoSlideMessenger", false, visible: false);


		/// <summary>
		/// Auto Slide timer
		/// </summary>
		private static DispatcherTimer AutoSlideTimer = null;


		/// <summary>
		/// Gets or sets whether the Auto-Slide is enabled
		/// </summary>
		public static bool AutoSlideEnable
		{
			get
			{
				return _AutoSlideEnable;
			}

			set
			{
				if (value != _AutoSlideEnable)
				{
					_AutoSlideEnable = value;
					AutoSlideReset();
				}
			}
		}
		private static bool _AutoSlideEnable = true;


		/// <summary>
		/// Slide-Show period in Seconds
		/// </summary>
		public static uint SlideShowPeriod
		{
			get
			{
				return _SlideShowPeriod;
			}

			set
			{
				if (value != _SlideShowPeriod)
				{
					_SlideShowPeriod = value;
					AutoSlideReset();
				}
			}
		}
		private static uint _SlideShowPeriod = 5;

		/// <summary>
		/// Update all the Slide-able elements 
		/// </summary>
		public static void AutoSlideNext()
		{
			// Trigger change in the AutoSlideMessenger dummy variable
			// This will notify all bound objects (AutoSlideListener) to 
			// go to the next slide.
			var val = (bool)AutoSlideMessenger.Content;
			AutoSlideMessenger.Content = !val;
		}

		/// <summary>
		/// Enable or disable the AutoSlide according to the settings
		/// </summary>
		public static void AutoSlideReset(bool enable = true)
		{
			if (AutoSlideTimer == null)
			{
				AutoSlideTimer = new DispatcherTimer(DispatcherPriority.SystemIdle);
				AutoSlideTimer.Tick += AutoSlideTimer_Tick;
			}

			AutoSlideTimer.Stop();

			if (_AutoSlideEnable && enable)
			{
				AutoSlideTimer.Interval = new TimeSpan(0, 0, (int)_SlideShowPeriod);
				AutoSlideTimer.Start();
			}
		}

		private static void AutoSlideTimer_Tick(object sender, EventArgs e)
		{
			if (!ClipDragDrop.IsDragging)
			{
				AutoSlideNext();
			}
		}


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Events

		/// <summary>
		/// Handle dedicated LimeProperty binding/unbinding
		/// </summary>
		/// <param name="e">Base Dependency Property Changed Event Args, not necessarly a LimeProperty</param>
		protected virtual void OnBoundDataContextChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Trigger on Change of the associated LimeProperty
        /// </summary>
        /// <param name="prop">LimeProperty assiociated with the control, cannot be null</param>
        /// <param name="e">PropertyChangedEventArgs, e.PropertyName==null if property initialization</param>
        protected virtual void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        {
        }


        /// <summary>
        /// Trigger event when the value of the LimeProperty is validated
        /// </summary>
        public event EventHandler<LimeControlEventArgs> ValueValidated;


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region dependency properties

        /// <summary>
        /// Get or set the relative level in hierarchy of the label (1.0: highest, 0.0: lowest) 
        /// </summary>
        public double Level
        {
            get { return (double)Base.GetValue(LevelProperty); }
            set { Base.SetValue(LevelProperty, value); }
        }
        public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
            "Level", typeof(double), typeof(LimeControl), 
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.Inherits, OnLevelPropertyChanged)
            );

        private static void OnLevelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
			if (!(d is LimeControl wxThis)) return;
			wxThis.OnLevelPropertyChanged(e);
        }

        protected virtual void OnLevelPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }


		// --------------------------------------------------------------------------------------------------

		/// <summary>
		/// Get or set whther the control is ReadOnly
		/// </summary>
		public bool ReadOnly
		{
			get { return (bool)Base.GetValue(ReadOnlyProperty); }
			set { Base.SetValue(ReadOnlyProperty, value); }
		}
		public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
			"ReadOnly", typeof(bool), typeof(LimeControl),
			new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnReadOnlyPropertyChanged)
			);

		private static void OnReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is LimeControl wxThis)) return;
			wxThis.OnReadOnlyPropertyChanged(e);
		}

		protected virtual void OnReadOnlyPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}


		// --------------------------------------------------------------------------------------------------

		/// <summary>
		/// Get or set the visibility of the LimeControl options
		/// </summary>
		public bool OptionsEnabled
        {
            get { return (bool)Base.GetValue(OptionsEnabledProperty); }
            set { Base.SetValue(OptionsEnabledProperty, value); }
        }
        public static readonly DependencyProperty OptionsEnabledProperty = DependencyProperty.Register(
            "OptionsEnabled", typeof(bool), typeof(LimeControl),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, OnOptionsEnabledPropertyChanged)
            );

        private static void OnOptionsEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
			if (!(d is LimeControl wxThis)) return;

			var prop = wxThis.DataContext as LimeProperty;

            if (wxThis.MainGrid != null && prop != null)
            {
                var wxobj = WPF.FindFirstChild<StackPanel>(wxThis.MainGrid);
                wxobj.Children.Clear();
                if ((bool)e.NewValue)
                {
					if (prop.ReqAdmin)
					{
						wxobj.Children.Add(new LimeIcon()
						{
							IconKey = "Shield",
							ToolTip = LimeLanguage.Translate(LanguageSection, "ShieldTip", "ShieldTip")
						});
					}
					if (prop.ReqRestart)
					{
						wxobj.Children.Add(new LimeIcon()
						{
							IconKey = "Warning",
							ToolTip = LimeLanguage.Translate(LanguageSection, "RestartTip", "RestartTip")
						});
					}
				}
            }

            wxThis.OnOptionsEnabledPropertyChanged(e);
        }

        protected virtual void OnOptionsEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }


        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Get or set the visibility of the LimeControl header
        /// </summary>
        public bool HeaderEnabled
        {
            get { return (bool)Base.GetValue(HeaderEnabledProperty); }
            set { Base.SetValue(HeaderEnabledProperty, value); }
        }
        public static readonly DependencyProperty HeaderEnabledProperty = DependencyProperty.Register(
            "HeaderEnabled", typeof(bool), typeof(LimeControl),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, OnHeaderEnabledPropertyChanged)
            );

        private static void OnHeaderEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
			if (!(d is LimeControl wxThis)) return;

			var prop = wxThis.DataContext as LimeProperty;

            if (wxThis.MainGrid != null && prop != null && wxThis.MainGrid.ColumnDefinitions.Count > 2)
            {
                var wxobj = VisualTreeHelper.GetChild(wxThis.MainGrid, 1) as TextBlock;
                var wxcol = wxThis.MainGrid.ColumnDefinitions[1];
                if ((bool)e.NewValue && !string.IsNullOrWhiteSpace(prop.Name))
                {
                    wxobj.Visibility = Visibility.Visible;
                    wxcol.SharedSizeGroup = GridGroupColHeader;
                }
                else
                {
                    wxobj.Visibility = Visibility.Collapsed;
                    wxcol.SharedSizeGroup = null;
                }
            }

            wxThis.OnHeaderEnabledPropertyChanged(e);
        }

        protected virtual void OnHeaderEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }


        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Validate the DataContext every time there is a change (default: false).
        /// </summary>
        public bool ValidateOnChange
        {
            get { return (bool)Base.GetValue(ValidateOnChangeProperty); }
            set { Base.SetValue(ValidateOnChangeProperty, value); }
        }
        public static readonly DependencyProperty ValidateOnChangeProperty = DependencyProperty.Register(
            "ValidateOnChange", typeof(bool), typeof(LimeControl), 
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnValidateOnChangeChanged)
            );
        
        public static void OnValidateOnChangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
			if (!(d is LimeControl wxThis)) return;

			wxThis.OnValidateOnChangePropertyChanged(e);
        }

        protected virtual void OnValidateOnChangePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }


		// --------------------------------------------------------------------------------------------------

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Methods

		/// <summary>
		/// Return a specialized <see cref="LimeControl"/> type matching the Type and attributes of a <see cref="LimeProperty"/>.
		/// </summary>
		/// <param name="prop">LimeProperty to be represented</param>
		/// <returns>LimeControl type selected to represent the LimeProperty properly</returns>
		public static Type LimeControlSelector(LimeProperty prop)
		{
			Type ret;

			var attrs = prop?.PInfo?.GetCustomAttributes(typeof(PickCollectionAttr), true);

			if (prop == null)
				ret = null;
			else if (prop.Type == null)
				ret = typeof(LimeLabel);
			else if (typeof(ICommand).IsAssignableFrom(prop.Type))
				ret = typeof(LimeButton);
			else if (typeof(LimePerson).IsAssignableFrom(prop.Type))
				ret = typeof(LimePersonCtrl);
			else if (typeof(IStringComposite).IsAssignableFrom(prop.Type))
				ret = typeof(LimeTextBox);
			else if (typeof(IPickCollection).IsAssignableFrom(prop.Type) || attrs != null && attrs.Length > 0)
				ret = typeof(LimeDropDown);
			else if (typeof(System.Drawing.Image).IsAssignableFrom(prop.Type)
				  || typeof(ImageSource).IsAssignableFrom(prop.Type)
				  || typeof(System.Drawing.Image).IsAssignableFrom(prop.Type)
				  || typeof(TagLib.IPicture).IsAssignableFrom(prop.Type)
				  || typeof(IEnumerable<System.Drawing.Image>).IsAssignableFrom(prop.Type)
				  || typeof(IEnumerable<ImageSource>).IsAssignableFrom(prop.Type)
				  || typeof(IEnumerable<TagLib.IPicture>).IsAssignableFrom(prop.Type))
				ret = typeof(LimeImage);
			else if (typeof(IEnumerable).IsAssignableFrom(prop.Type) && prop.Type != typeof(string))
			{
				// Find item type of a generic (for example T in Collection<T>) 
				var itemType = prop.Type.GetInterfaces()
								.Where( t => t.IsGenericType && 
										t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
								.Select(t => t.GenericTypeArguments.Length == 1 ? 
										t.GenericTypeArguments[0] :
										null)
								.FirstOrDefault();

				if (itemType != null && !typeof(IEnumerable).IsAssignableFrom(itemType) && 
					itemType != typeof(object) && !typeof(LimeProperty).IsAssignableFrom(itemType))
					ret = typeof(LimeGridView);
				else
					ret = typeof(LimeListView);
			}
			else if (prop.Minimum < prop.Maximum)
				ret = typeof(LimeNumBox);
			else if (prop.Type == typeof(bool) || prop.Type == typeof(bool?))
				ret = typeof(LimeCheckBox);
			else if (prop.Type == typeof(Color) || prop.Type == typeof(System.Drawing.Color))
				ret = typeof(LimeColorPicker);
			else if (prop.Type.IsEnum || prop.Type == typeof(FontFamily)
					|| prop.Type == typeof(FontWeight) || prop.Type == typeof(FontStyle))
				ret = typeof(LimeDropDown);
			else if (prop.Type == typeof(byte) || prop.Type == typeof(short)
				  || prop.Type == typeof(int) || prop.Type == typeof(long)
				  || prop.Type == typeof(float) || prop.Type == typeof(double)
				  || prop.Type == typeof(ushort) || prop.Type == typeof(uint)
				  || prop.Type == typeof(ulong)
				  )
				ret = typeof(LimeNumBox);
			else if (prop.Type == typeof(string)
				|| typeof(DateTime).IsAssignableFrom(prop.Type)
				|| typeof(Uri).IsAssignableFrom(prop.Type))
				ret = typeof(LimeTextBox);
			else
				ret = typeof(LimeComposite);


			return ret;
		}



		/// <summary>
		/// Set focus on the first valid focusable element in the control
		/// </summary>
		public void SetFocus(object dataContext = null)
		{
			UIElement focus = null;

			if (IsEnabled && Focusable)
			{
				focus = this;
			}
			else
			{
				if (dataContext != null)
				{
					focus = WPF.FindFirstChild<FrameworkElement>(this,
						(wx) => wx.IsVisible && wx.IsEnabled && wx.Focusable && wx.DataContext == dataContext
						);
				}

				if (focus == null)
				{
					focus = WPF.FindFirstChild<UIElement>(this, 
						(wx) => wx.IsVisible && wx.IsEnabled && wx.Focusable
						);
				}
			}

			LimeMsg.Debug("LimeControl SetFocus: {0}", focus);
			if (focus != null)
			{
				Keyboard.Focus(focus);
			}
		}


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Shared resource

		/// <summary>
		/// Provide a generic callback to PreviewKeyDown event to fix navigation problems using arrow keys
		/// </summary>
		/// <param name="sender">Any FrameworkElement</param>
		/// <param name="e"></param>
		protected void FixFocus_PreviewKeyDown(object sender, KeyEventArgs e)
        {
			if (!(sender is FrameworkElement wxobj)) return;
			if (Keyboard.Modifiers != 0) return;

            TraversalRequest tRequest = null;
            switch (e.Key)
            {
                case Key.Up: tRequest = new TraversalRequest(FocusNavigationDirection.Up); break;
                case Key.Down: tRequest = new TraversalRequest(FocusNavigationDirection.Down); break;
                case Key.Left: tRequest = new TraversalRequest(FocusNavigationDirection.Left); break;
                case Key.Right: tRequest = new TraversalRequest(FocusNavigationDirection.Right); break;
            }

            if (tRequest != null)
            {
                wxobj.MoveFocus(tRequest);
                e.Handled = true;
            }
        }

        #endregion
    }
}
