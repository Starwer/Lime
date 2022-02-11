/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     28-01-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using Lime;
using System.Windows;
using System.ComponentModel;
using System;
using System.Runtime.Versioning;

namespace LimeLauncher.Controls
{
	/// <summary>
	/// Interaction logic for LimeListView.xaml
	/// </summary>
	[SupportedOSPlatform("windows")]
	public partial class LimeListView : LimeControl
    {
        public LimeListView()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: true);
        }


        protected override void OnBoundDataContextChanged(DependencyPropertyChangedEventArgs e)
        {
            // Initialize
        }


        protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        {

            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
            {
				LimeMsg.Debug("LimeListView OnPropertyChanged: {0} : {1} : {2}", prop, prop?.Ident, e?.PropertyName);

				int idx = 0;

				// Bellow we try to re-use the existing controls to avoid rendering overhead

				if (prop != null)
				{
					LimePropertyCollection collec;

					if (typeof(LimePropertyCollection).IsAssignableFrom(prop.Type))
					{
						collec = (LimePropertyCollection)prop.Content;
					}
					else
					{
						collec = new LimePropertyCollection(null, prop);
					}

					for (int i = 0; i < collec.Count; i++)
					{
						var sprop = collec[i];
						LimeControl wxctrl = null;
						var type = LimeControlSelector(sprop);
						if (type == null) continue;

						if (idx < wxMain.Items.Count)
						{
							var wxitem = (LimeControl)wxMain.Items[idx];
							var propitem = (LimeProperty)wxitem.DataContext;
							if (type == wxitem.GetType())
							{
								LimeMsg.Debug("LimeListView OnPropertyChanged: recycle {0} : {1} --> {2}", idx, propitem, sprop);
								wxitem.DataContext = sprop;
								wxitem.Visibility = sprop?.Visible == true ? Visibility.Visible : Visibility.Collapsed;
							}
							else
							{
								LimeMsg.Debug("LimeListView OnPropertyChanged: replaced {0} : {1} --> {2}", idx, propitem, sprop);
								wxMain.Items[idx] = wxctrl = (LimeControl)Activator.CreateInstance(type);
							}

						}
						else
						{
							LimeMsg.Debug("LimeListView OnPropertyChanged: new {0} : {1}", idx, sprop);
							wxMain.Items.Add(wxctrl = (LimeControl)Activator.CreateInstance(type));
						}

						if (wxctrl != null)
						{
							wxctrl.IsTabStop = false;
							wxctrl.Level = Level > 0.11 ? Level - 0.1 : Level;
							wxctrl.ReadOnly = ReadOnly;
							wxctrl.HeaderEnabled = HeaderEnabled;
							wxctrl.ValidateOnChange = ValidateOnChange;
							wxctrl.DataContext = sprop;
						}

						idx++;
					}
				}

				// Hide remaining items from previous bindings
				for (; idx < wxMain.Items.Count; idx++)
				{
					var wxitem = (LimeControl)wxMain.Items[idx];
					wxitem.DataContext = null;
					wxitem.Visibility = Visibility.Collapsed;
				}

			}
        }


		protected override void OnLevelPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			foreach (var wxitem in wxMain.Items)
			{
				var wxctrl = (LimeControl) wxitem;
				wxctrl.Level = Level > 0.11 ? Level - 0.1 : Level;
			}
		}

		protected override void OnReadOnlyPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			foreach (var wxitem in wxMain.Items)
			{
				var wxctrl = (LimeControl)wxitem;
				wxctrl.ReadOnly = ReadOnly;
			}
		}

		protected override void OnHeaderEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			foreach (var wxitem in wxMain.Items)
			{
				var wxctrl = (LimeControl)wxitem;
				wxctrl.HeaderEnabled = HeaderEnabled;
			}
		}

		protected override void OnValidateOnChangePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			foreach (var wxitem in wxMain.Items)
			{
				var wxctrl = (LimeControl)wxitem;
				wxctrl.ReadOnly = ReadOnly;
			}
		}


	}
}
