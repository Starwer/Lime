/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     06-10-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using Lime;
using System.Windows;
using System.ComponentModel;
using System.Collections;
using System;
using WPFhelper;
using System.Windows.Controls;
using System.Windows.Data;

namespace LimeLauncher.Controls
{
	/// <summary>
	/// Interaction logic for LimeGridView.xaml
	/// </summary>
	public partial class LimeGridView : LimeControl
    {
        public LimeGridView()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: false);
        }


		protected override void OnBoundDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			// Initialize
			wxList.DataContext = null;
		}


		protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
		{
			//LimeMsg.Debug("LimeListView OnPropertyChanged: {0} : {1} : {2}", prop, prop.Ident, e?.PropertyName);

			if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
			{
				LimePropertyCollection collec = null;

				if (typeof(LimePropertyCollection).IsAssignableFrom(prop.Type))
				{
					collec = (LimePropertyCollection)prop.Content;
				}
				else
				{
					collec = new LimePropertyCollection(null, prop);
				}


				wxGridView.Columns.Clear();

				// Create column definitons from a template first row
				if (collec.Count != 0)
				{
					var item = collec[0];

					LimePropertyCollection items;
					if (item.Content is LimePropertyCollection col)
						items = col;
					else
						items = new LimePropertyCollection(null, item.Content, prop);

					GridViewColumn last = null;
					int idx = 0;
					foreach (var iprop in items)
					{
						var path = "Content" + 
							( iprop.Ident != null ? 
							  string.Format(".{0}", iprop.Ident) : 
							  string.Format("[{0}]", idx)
							);

						// Not a recommended way to make DataTemplate in code-behind, but this word 
						// for this more-or-less static case.
						var datatemplate = new FrameworkElementFactory(typeof(LimeControl));
						datatemplate.SetBinding(DataContextProperty, new Binding(path));
						datatemplate.SetValue(OptionsEnabledProperty, false);
						datatemplate.SetValue(HeaderEnabledProperty, false);

						// Create the colum definition
						var gvcol = new GridViewColumn()
						{
							Header = iprop.Name,
							CellTemplate = new DataTemplate()
							{
								VisualTree = datatemplate
							}
						};

						last = gvcol;
						wxGridView.Columns.Add(gvcol);

						idx++;
					}

				}
				

				wxList.DataContext = collec;

			}
		}


		protected override void OnLevelPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		protected override void OnReadOnlyPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		protected override void OnHeaderEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		protected override void OnValidateOnChangePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}


	}
}
