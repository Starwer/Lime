/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     11-02-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using Lime;
using System.Windows;
using System.ComponentModel;

namespace LimeLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LimeLabel.xaml
    /// </summary>
    public partial class LimeLabel : LimeControl
    {
        public LimeLabel()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: false, hasOptions: false);
        }


		protected override void OnBoundDataContextChanged(DependencyPropertyChangedEventArgs e)
        {
            // Initialize
        }

        private const string LanguageSection = "Translator";


		/// <summary>
		/// Get or set whether the data context should be translated 
		/// </summary>
		public bool Translate
		{
			get { return (bool)GetValue(TranslateProperty); }
			set { SetValue(TranslateProperty, value); }
		}
		public static readonly DependencyProperty TranslateProperty = DependencyProperty.Register(
			"Translate", typeof(bool), typeof(LimeLabel),
			new FrameworkPropertyMetadata(true, OnTranslatePropertyChanged)
			);

		private static void OnTranslatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
		}



		protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
            {
				if (prop == null)
				{
					wxText.Text = "";
				}
                else if (prop.Content != null)
                {
					if (Translate)
					{
						wxText.Text = LimeLanguage.Translate(LanguageSection, prop.Value, prop.Value);
					}
					else
					{
						wxText.Text = prop.Value;
					}
				}
                else
                {
                    wxText.Text = prop.Name;
                }
            }
        }

    }
}
