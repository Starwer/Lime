/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     12-04-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using ZeroFormatter;

namespace Lime
{


    // --------------------------------------------------------------------------------------------------
    #region Types

    /// <summary>
    /// Defines which kind of Opus is represented.
    /// </summary>
    public enum LimeOpusType : ushort
    {
        None = 0x00,
        Movie = 0x01,
        TvShow = 0x02,
        Album = 0x04
    }



    #endregion



    /// <summary>
    /// Represents a Movie, TvShow, Album... ViewModel and Serializable to binary
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ZeroFormattable]
    public class LimeOpus : StringConvertible, INotifyPropertyChanged
    {
        // --------------------------------------------------------------------------------------------------
        #region Boilerplate INotifyPropertyChanged

        // Boilerplate code for INotifyPropertyChanged

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Constants & Static properties

        /// <summary>
        /// Section for language access
        /// </summary>
        private const string IniLanguageSection = "Text";

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Properties

        /// <summary>
        /// TMDb person Identifier
        /// </summary>
        [Index(0), LimePropertyAttribute(Visible = false)]
        public virtual int TmdbId
        {
            get { return _TmdbId; }
            set
            {
                if (value != _TmdbId)
                {
                    _TmdbId = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _TmdbId = 0;



        /// <summary>
        /// Title of the Opus (main name)
        /// </summary>
        [Index(1)]
        public virtual string Title
        {
            get { return _Title; }
            set
            {
                if (value != _Title)
                {
                    _Title = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _Title = null;


        /// <summary>
        /// Original title of the Opus (main name)
        /// </summary>
        [Index(2)]
        public virtual string OriginalTitle
        {
            get { return _OriginalTitle; }
            set
            {
                if (value != _OriginalTitle)
                {
                    _OriginalTitle = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _OriginalTitle = null;


        /// <summary>
        /// Roles of a person in this opus
        /// </summary>
        [Index(3)]
        public virtual IList<string> Roles
        {
            get { return _Roles; }
            set
            {
                if (value != _Roles)
                {
                    _Roles = value;
                    OnPropertyChanged();
                }
            }
        }
        private IList<string> _Roles = null;


        /// <summary>
        /// Opus Type
        /// </summary>
        [Index(4)]
        public virtual LimeOpusType Type
        {
            get { return _Type; }
            set
            {
                if (value != _Type)
                {
                    _Type = value;
                    OnPropertyChanged();
                }
            }
        }
        private LimeOpusType _Type = LimeOpusType.None;


        /// <summary>
        /// For Adult (Erotic, Porn)
        /// </summary>
        [Index(5)]
        public virtual bool Adult
        {
            get { return _Adult; }
            set
            {
                if (value != _Adult)
                {
                    _Adult = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _Adult = false;


        /// <summary>
        /// Poster URL
        /// </summary>
        [Index(6)]
        public virtual string PosterUrl
        {
            get { return _PosterUrl; }
            set
            {
                if (value != _PosterUrl)
                {
                    _PosterUrl = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _PosterUrl = null;



        /// <summary>
        /// Release year
        /// </summary>
        [Index(7)]
        public virtual uint Released
        {
            get { return _Released; }
            set
            {
                if (value != _Released)
                {
                    _Released = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _Released = 0;



        /// <summary>
        /// Original Language of the Opus
        /// </summary>
        [Index(8)]
        public virtual string OriginalLanguage
        {
            get { return _OriginalLanguage; }
            set
            {
                if (value != _OriginalLanguage)
                {
                    _OriginalLanguage = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _OriginalLanguage = null;

        /// <summary>
        /// Description of the Opus
        /// </summary>
        [Index(9), LimePropertyAttribute(Multiline = true)]
        public virtual string Description
        {
            get { return _Description; }
            set
            {
                if (value != _Description)
                {
                    _Description = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _Description = null;

        /// <summary>
        /// Score of the Opus (/5)
        /// </summary>
        [Index(10), LimePropertyAttribute(Maximum = 5.0)]
        public virtual double Score
        {
            get { return _Score; }
            set
            {
                if (value != _Score)
                {
                    _Score = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _Score = 0;

        /// <summary>
        /// Genres of the Opus
        /// </summary>
        [Index(11)]
        public virtual string[] Genres
        {
            get { return _Genres; }
            set
            {
                if (value != _Genres)
                {
                    _Genres = value;
                    OnPropertyChanged();
                    OnPropertyChanged("GenresString");
                }
            }
        }
        private string[] _Genres = null;


        /// <summary>
        /// Genres ot the Opus, as a string
        /// </summary>
        [IgnoreFormat]
        public virtual string GenresString
        {
            get
            {
                if (Genres == null) return "";
                var separator = LimeLanguage.Translate(IniLanguageSection, "ListSeparator", ", ");
                return string.Join(separator, Genres);
            }
        }


		/// <summary>
		/// Get a tooltip
		/// </summary>
		[IgnoreFormat]
        public string Tooltip
        {
            get { return _Tooltip; }
            private set
            {
                if (value != _Tooltip)
                {
                    _Tooltip = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _Tooltip = null;

        #endregion



        // --------------------------------------------------------------------------------------------------
        #region ctors

        public LimeOpus()
        {
        }


        public LimeOpus(LimeOpusType type, int id, string title, string role = null)
        {
            Type = type;
            TmdbId = id;
            Title = title;
            Roles = new List<string> { role };
		}

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region StringConvertion implementation

        /// <summary>
        /// Provide a string representation of the object public properties
        /// </summary>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>string representation of the object</returns>
        public override string ToString(CultureInfo culture)
        {
            return Title;
        }

        /// <summary>
        /// Update the object from its string representation (i.e. Deserialize).
        /// </summary>
        /// <param name="source">String representing the object content (to be)</param>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>true if the parsing of the string was successful, false otherwise</returns>
        public override bool FromString(string source, CultureInfo culture)
        {
            return false;
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Methods

        /// <summary>
        /// Build the Tooltip string
        /// </summary>
        /// <returns>Tooltip</returns>
        public string BuildToolTip()
        {
            string ret = "";


            ret += Title;

            if (!string.IsNullOrEmpty(OriginalTitle) && OriginalTitle != Title)
                ret += Environment.NewLine + OriginalTitle;

            if (Released != 0 && (Genres != null && Genres.Length > 0 || Score != 0.0))
            {
                string sep = LimeLanguage.Translate(IniLanguageSection, "ListSeparator", ", ");
                var format = LimeLanguage.Translate(IniLanguageSection, "FormatReleasedGenresScore", "FormatReleasedGenresScore");
                var msg = string.Format(format, Released, GenresString, Score);
                ret += Environment.NewLine + msg;
            }
            else
            {
                if (Released != 0)
                    ret += Environment.NewLine + Released.ToString();

                if (Genres != null && Genres.Length > 0)
                    ret += Environment.NewLine + GenresString;

                if (Score != 0)
                    ret += Environment.NewLine + Score.ToString();
            }

            if (! string.IsNullOrEmpty(Description))
                ret += Environment.NewLine + Description;


            return Tooltip = ret;
        }

        #endregion

    }
}
