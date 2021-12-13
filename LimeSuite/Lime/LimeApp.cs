/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     17-04-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Lime
{

    /// <summary>
    /// Defines Settings in Lime to handle an application
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "LimeApp")]
    public class LimeApp : INotifyPropertyChanged
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
        #region Observable Properties

        /// <summary>
        /// Name of the Application
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set
            {
                if (value != _Name)
                {
                    _Name = value;
                    OnPropertyChanged("Name");
                }
            }
        }
        private string _Name;

        /// <summary>
        /// Path of the Application
        /// </summary>
        public string Path
        {
            get { return _Path; }
            set
            {
                if (value != _Path)
                {
                    _Path = value;
                    OnPropertyChanged("Path");
                }
            }
        }
        private string _Path;

        /// <summary>
        /// Pattern of the window name of the Application
        /// </summary>
        public string WinPattern
        {
            get { return _WinPattern; }
            set
            {
                if (value != _WinPattern)
                {
                    _WinPattern = value;
                    OnPropertyChanged("WinPattern");
                }
            }
        }
        private string _WinPattern;

        /// <summary>
        /// Enable Task Matching on Application executable itself
        /// </summary>
        public bool MatchExe
        {
            get { return _MatchExe; }
            set
            {
                if (value != _MatchExe)
                {
                    _MatchExe = value;
                    OnPropertyChanged("MatchExe");
                }
            }
        }
        private bool _MatchExe;


        /// <summary>
        /// Enable Task Matching on Application associated data
        /// </summary>
        public bool MatchData
        {
            get { return _MatchData; }
            set
            {
                if (value != _MatchData)
                {
                    _MatchData = value;
                    OnPropertyChanged("MatchData");
                }
            }
        }
        private bool _MatchData;



        public LimeApp (string name = null)
        {
            this.Name = name;
        }


        #endregion

    }
}
