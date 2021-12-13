/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     14-04-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using Lime;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LimeLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LimePersonCtrl.xaml
    /// </summary>
    public partial class LimePersonCtrl : LimeControl
    {
        public LimePersonCtrl()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: true, hasOptions: true);
        }

        //protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        //{
        //    if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
        //    {
        //    }
        //}


    }
}
