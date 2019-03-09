/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     18-01-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using System;
using System.Windows.Controls;
using Lime;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace LimeLauncher
{
    /// <summary>
    /// Applications configuration Panel
    /// </summary>
    public partial class ApplicationPanel : UserControl
    {
        // --------------------------------------------------------------------------------------------------
        #region Boilerplate

        // Boilerplate code for INotifyPropertyChanged : Instances

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Basic Control logic

        public ApplicationPanel()
        {
            InitializeComponent();
            Refresh();
        }


        protected override void OnContentChanged(object oldContent, object newContent)
        {
            if (oldContent != null)
                throw new InvalidOperationException("Content can't be set!");
        }



        private const string LanguageSection = "Translator";

        #endregion


        public LimeItem AppTree { get; private set; }  = null;

        public List<LimeApp> AppList { get; private set; }  = null;


        /// <summary>
        /// Get or set the width of the items displayed in the ListBox 
        /// </summary>
        public double InnerWidth
        {
            get { return (double)this.GetValue(InnerWidthProperty); }
            set { this.SetValue(InnerWidthProperty, value); }
        }
        public static readonly DependencyProperty InnerWidthProperty = DependencyProperty.RegisterAttached(
            "InnerWidth", typeof(double), typeof(ApplicationPanel), new PropertyMetadata()
            );


        /// <summary>
        /// Initialize applicaton list
        /// </summary>
        public void Refresh()
        {
			// Initialize 
			//AppTree = LimeItem.Load(ConfigLocal.ApplicationsPath);
			//AppTree.Attribute.ImgSrcBigSize = 32;
			//AppTree.Attribute.ImgSrcSmallSize = 32;
			//AppTree.Refresh();
			//wxAppList.DataContext = AppTree;


			AppList = new List<LimeApp>
			{
				new LimeApp("one"),
				new LimeApp("two"),
				new LimeApp("three")
			};

			wxAppList.DataContext = AppList;

        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Point pos = wxAppList.TranslatePoint(new Point(0, 0), this);
            //wxAppList.Height = ActualHeight - pos.Y;
            //wxAppList.MaxWidth = ActualWidth - pos.X;
            //InnerWidth = wxAppList.ActualWidth  - 30;
        }
    }
}
