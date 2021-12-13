/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     11-02-2018
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

namespace LimeLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LimeCheckBox.xaml
    /// </summary>
    public partial class LimeCheckBox : LimeControl
    {
        public LimeCheckBox()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: false);
        }


        private void wxMain_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var wxobj = sender as CheckBox;
            if (wxobj == null) return;
            if (Keyboard.Modifiers != 0) return;

            TraversalRequest tRequest = null;
            switch (e.Key)
            {
				case Key.Up: tRequest = new TraversalRequest(FocusNavigationDirection.Up); break;
				case Key.Down: tRequest = new TraversalRequest(FocusNavigationDirection.Down); break;
				case Key.Left: tRequest = new TraversalRequest(FocusNavigationDirection.Left); break;
                case Key.Right: tRequest = new TraversalRequest(FocusNavigationDirection.Right); break;
                case Key.Enter: wxobj.IsChecked = wxobj.IsChecked == false; e.Handled = true; break;
            }

            if (tRequest != null)
            {
                wxobj.MoveFocus(tRequest);
                e.Handled = true;
            }
        }
        
    }
}
