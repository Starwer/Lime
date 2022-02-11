/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     13-09-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using Lime;
using System;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFhelper;

namespace LimeLauncher
{
	/// <summary>
	/// Interaction logic for InfoPanel.xaml
	/// </summary>
	[SupportedOSPlatform("windows7.0")]
	public partial class InfoPanel : UserControl
	{
		// --------------------------------------------------------------------------------------------------
		#region Basic Control logic

		public InfoPanel()
		{
			InitializeComponent();

			// Manual binding to enable the detection of binding change
			this.SetBinding(BoundDataContextProperty, new Binding());

		}



		protected override void OnContentChanged(object oldContent, object newContent)
		{
			if (oldContent != null)
				throw new InvalidOperationException("Content can't be set!");
		}

		public static readonly DependencyProperty BoundDataContextProperty = DependencyProperty.Register(
			"BoundDataContext", typeof(object), typeof(InfoPanel), new PropertyMetadata(null, OnBoundDataContextChanged)
			);



		/// <summary>
		/// React on Binding change
		/// </summary>
		/// <param name="d"></param>
		/// <param name="e"></param>
		private static void OnBoundDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var wxThis = d as InfoPanel;

			if (e.OldValue is LimeItem old)
			{
				old.PropertyChangedWeak -= wxThis.Item_PropertyChanged;
			}

			if (e.NewValue is LimeItem item)
			{
				LimeMsg.Debug("InfoPanel: BoundDataContext: {0}", item.Name);
				LimeLib.LifeCheck();

				item.PropertyChangedWeak += wxThis.Item_PropertyChanged;

				wxThis.Item_PropertyChanged(item, null);

			}

		}

		/// <summary>
		/// Track changes in the Bound LimeItem
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var item = (LimeItem)sender;

			if (e == null || e.PropertyName == "Handle")
			{
				// Adjust width of the Thumbnail (seems not possible to do it properly in Xaml)
				if (item.Handle != IntPtr.Zero)
				{
					wxInfoPanelThumbnail.SetBinding(
						MaxWidthProperty, 
						new Binding("ActualWidth")
						{
							Source = wxCover,
							Converter = (IValueConverter) Resources["ScaleConvert"],
							ConverterParameter = 0.6
						});
				}
				else
				{
					wxInfoPanelThumbnail.MaxWidth = 0;
				}
			}
		}

		private const string LanguageSection = "Translator";


		//public bool Big { get; private set; } = false;

		public bool Big
		{
			get { return (bool)this.GetValue(BigProperty); }
			private set { this.SetValue(BigProperty, value); }
		}
		public static readonly DependencyProperty BigProperty = DependencyProperty.RegisterAttached(
			"Big", typeof(bool), typeof(InfoPanel), new PropertyMetadata()
			);


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region GUI Callbacks

		/// <summary>
		/// Handle the Async Loading
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StackPanel_LayoutUpdated(object sender, EventArgs e)
		{
			//LimeMsg.Debug("wxPropCtrl_LayoutUpdated -------------");
			if (wxPropCtrl.DataContext is LimeMetadata meta && meta != _Metadata)
			{
				LimePerson.Cancel();

				_Metadata = meta;

				meta.LoadAsync((int)Global.User.PersonAutoDownload);
			}
		}
		private LimeMetadata _Metadata = null;


		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var wxobj = sender as FrameworkElement;
			double refsize = (Global.Local.Skin.IconBigSize.Content as DoubleScaled).Scaled;
			Big = wxobj.ActualWidth > refsize * 2.5;

			if (!Big)
			{
				wxInfoPanelGridHighRow.MaxHeight = wxInfoPanelGrid.ActualHeight / 4.0;
				wxColumnIcon.MinWidth = 0;
			}
			else
			{
				wxInfoPanelGridHighRow.MaxHeight = wxInfoPanelGrid.ActualHeight / 2.0;
				wxColumnIcon.MinWidth = refsize;
			}
		}


		private void Person_Click(object sender, RoutedEventArgs e)
		{
			var wxpers = (Button)e.OriginalSource;
			var person = (LimePerson) wxpers.DataContext;
			if (person != null)
			{
				person.LoadAsync(true);
			}
		}

		#endregion


	}

}
