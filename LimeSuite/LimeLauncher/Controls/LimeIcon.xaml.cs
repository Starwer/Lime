/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     23-07-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System.Windows;
using System.Windows.Controls;

namespace LimeLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LimeIcon.xaml
    /// </summary>
    public partial class LimeIcon : UserControl
    {
        public LimeIcon()
        {
            InitializeComponent();
        }


		/// <summary>
		/// Get or set the Icon Key. This should be one of the references of the Icon dictionary 
		/// </summary>
		public string IconKey
		{
			get { return (string)GetValue(IconKeyProperty); }
			set { SetValue(IconKeyProperty, value); }
		}
		public static readonly DependencyProperty IconKeyProperty = DependencyProperty.Register(
			"IconKey", typeof(string), typeof(LimeIcon),
			new FrameworkPropertyMetadata(null, OnIconKeyPropertyChanged)
			);

		/// <summary>
		/// Apply the IconKey template from Icons.xaml to the actual UserControl
		/// </summary>
		/// <param name="d">LimeIcon object</param>
		/// <param name="e"></param>
		private static void OnIconKeyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var wxThis = d as LimeIcon;

			DataTemplate template = null;
			if (e.NewValue is string key)
			{
				template = (string.IsNullOrEmpty(key) ? null : wxThis.Resources[key]) as DataTemplate;
			}

			wxThis.ContentTemplate = template;
		}

	}
}
