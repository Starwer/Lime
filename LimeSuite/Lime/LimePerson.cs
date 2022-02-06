/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     22-03-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using ZeroFormatter;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

namespace Lime
{
    /// <summary>
    /// Represents a person (performer, writer, director...) and 
    /// enables to save/retrieve information on this person from a local Person database or on Internet
    /// </summary>
    [ZeroFormattable]
    public class LimePerson : StringConvertible, INotifyPropertyChanged
    {

        // --------------------------------------------------------------------------------------------------
        #region Types

        /// <summary>
        /// Use Flags to store boolean properties. 
        /// This saves some space (one byte in total instead of one byte per property).
        /// </summary>
        [Flags]
        private enum LimePersonFlags : ushort
        {
			IsLoading = 0x01,
            IsSaving = 0x02,
            IsLoaded = 0x04,
			IsDownloading = 0x08,
			RolesReadOnly = 0x10
		}

		/// <summary>
		/// Person Gender
		/// </summary>
		public enum PersonGender
        {
            Unknown = 0,
            Female = 1,
            Male = 2
        }


        #endregion

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
		/// Current supported version of the Binary format (LPZ)
		/// </summary>
		private const double FormatVersion = 1.0;

		/// <summary>
		/// Section for language access
		/// </summary>
		private const string IniLanguageSection = "Text";

		/// <summary>
		/// Extension used for the LimePerson Binary files (Lime Person ZeroFormatter)
		/// </summary>
		public const string Extension = ".lpz";


        /// <summary>
        /// Set the Directory path where the Persons should be looked up
        /// </summary>
        public static string LocalDbPath = null;


		/// <summary>
		/// Thread-safe stack to schedule the Loading/saving of persons in the background.
		/// This object is also used as key for lock.
		/// </summary>
		private static BlockingCollection<LimePerson> Stack = null;

		/// <summary>
		/// Download information on the Person from Internet using this MetaSearch instance
		/// </summary>
		public static LimeMetaSearch MetaSearch = null;

		/// <summary>
		/// Enable to save the downloaded data automatically to local DB
		/// </summary>
		public static bool AutoSave = false;


		#endregion

		// --------------------------------------------------------------------------------------------------
		#region Non-Serializable Properties

		/// <summary>
		/// Boolean Properties values
		/// </summary>
		private LimePersonFlags Flags;


        /// <summary>
        /// Define whether the LimePerson is busy Loading itself
        /// </summary>
        [IgnoreFormat, LimePropertyAttribute(Visible = false)]
        public bool IsLoading
        {
            get { return Flags.HasFlag(LimePersonFlags.IsLoading); }
            private set
            {
                const LimePersonFlags mask = LimePersonFlags.IsLoading;
                LimePersonFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                    OnPropertyChanged("IsBusy");
                }
            }
        }

		/// <summary>
		/// Define whether the LimePerson is busy Downloading
		/// </summary>
		[IgnoreFormat, LimePropertyAttribute(Visible = false)]
		public bool IsDownloading
		{
			get { return Flags.HasFlag(LimePersonFlags.IsDownloading); }
			private set
			{
				const LimePersonFlags mask = LimePersonFlags.IsDownloading;
				LimePersonFlags val = value ? mask : 0;
				if ((Flags & mask) != val)
				{
					Flags = (Flags & ~mask) | val;
					OnPropertyChanged();
					OnPropertyChanged("IsBusy");
				}
			}
		}

		/// <summary>
		/// Define whether the LimePerson is busy Saving itself
		/// </summary>
		[IgnoreFormat, LimePropertyAttribute(Visible = false)]
        public bool IsSaving
        {
            get { return Flags.HasFlag(LimePersonFlags.IsSaving); }
            private set
            {
                const LimePersonFlags mask = LimePersonFlags.IsSaving;
                LimePersonFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                    OnPropertyChanged("IsBusy");
                }
            }
        }

        /// <summary>
        /// Define whether the LimePerson is busy loading or saving
        /// </summary>
        [IgnoreFormat, LimePropertyAttribute(Visible = false)]
        public bool IsBusy
        {
            get { return (Flags & (LimePersonFlags.IsLoading | LimePersonFlags.IsDownloading | LimePersonFlags.IsSaving)) != 0; }
        }

        /// <summary>
        /// Define whether the LimePerson is done Loading itself
        /// </summary>
        [IgnoreFormat, LimePropertyAttribute(Visible = false)]
        public bool IsLoaded
        {
            get { return Flags.HasFlag(LimePersonFlags.IsLoaded); }
            private set
            {
                const LimePersonFlags mask = LimePersonFlags.IsLoaded;
                LimePersonFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
					OnPropertyChanged("IsBusy");
				}
			}
        }




		/// <summary>
		/// Roles (character, position) of the person in a given context (movie, tune...)
		/// </summary>
		[IgnoreFormat]
		public string[] Roles
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
		private string[] _Roles = null;


		/// <summary>
		/// Make the roles read-only
		/// </summary>
		[IgnoreFormat, LimePropertyAttribute(Visible = false)]
		public bool RolesReadOnly
		{
			get { return Flags.HasFlag(LimePersonFlags.RolesReadOnly); }
			set
			{
				const LimePersonFlags mask = LimePersonFlags.RolesReadOnly;
				LimePersonFlags val = value ? mask : 0;
				if ((Flags & mask) != val)
				{
					Flags = (Flags & ~mask) | val;
					OnPropertyChanged();
				}
			}
		}


		/// <summary>
		/// List of the Opus, as a string
		/// </summary>
		[IgnoreFormat, LimePropertyAttribute(Visible = false)]
		public string OpusString
		{
			get
			{
				if (Opus == null) return "";
				var separator = LimeLanguage.Translate(IniLanguageSection, "ListSeparator", ", ");
				var format = LimeLanguage.Translate(IniLanguageSection, "FormatItemRole", "{0} ({1})");

				var now = (uint) DateTime.Now.Year;

				var sorted = Opus.OrderByDescending(o => o.Released);
				string ret = "";
				foreach (var opus in sorted)
				{
					if (opus.Released <= now)
					{
						if (ret != "") ret += separator;
						if (opus.Roles != null && opus.Roles.Count > 0)
						{
							ret += string.Format(format, opus.Title, string.Join(separator, opus.Roles));
						}
						else
						{
							ret += opus.Title;
						}
					}
				}
				return ret;
			}
		}


		/// <summary>
		/// List of alias, as a string
		/// </summary>
		[IgnoreFormat, LimePropertyAttribute(Visible = false)]
		public string AliasString
		{
			get
			{
				if (Alias == null || Alias.Length==0) return Name;
				var separator = LimeLanguage.Translate(IniLanguageSection, "ListSeparator", ", ");
				return Name + separator + string.Join(separator, Alias);
			}
		}


		#endregion

		// --------------------------------------------------------------------------------------------------
		#region Serialized Properties


		/// <summary>
		/// Version of the Binary format (To handle format/changes and backward compatibitity)
		/// </summary>
		[Index(0), LimePropertyAttribute(Visible = false)]
		public virtual double Version
		{
			get { return _Version; }
			set
			{
				if (value != _Version)
				{
					_Version = value;
					OnPropertyChanged();
				}
			}
		}
		private double _Version = FormatVersion;


		/// <summary>
		/// TMDb person Identifier
		/// </summary>
		[Index(1), LimePropertyAttribute(Visible = false)]
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
        /// IMDB person Identifier
        /// </summary>
        [Index(2), LimePropertyAttribute(Visible = false)]
        public virtual string ImdbId
        {
            get { return _ImdbId; }
            set
            {
                if (value != _ImdbId)
                {
                    _ImdbId = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _ImdbId = null;

        /// <summary>
        /// Name of the Person (main name)
        /// </summary>
        [Index(3)]
        public virtual string Name
        {
            get { return _Name; }
            set
            {
                if (value != _Name)
                {
                    _Name = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _Name = null;


        /// <summary>
        /// Other Names of the Person
        /// </summary>
        [Index(4)]
        public virtual string[] Alias
        {
            get { return _Alias; }
            set
            {
                if (value != _Alias)
                {
                    _Alias = value;
					OnPropertyChanged();
					OnPropertyChanged("AliasString");
				}
			}
        }
        private string[] _Alias = null;


        /// <summary>
        /// Gender
        /// </summary>
        [Index(5)]
        public virtual PersonGender Gender
        {
            get { return _Gender; }
            set
            {
                if (value != _Gender)
                {
                    _Gender = value;
                    OnPropertyChanged();
                }
            }
        }
        private PersonGender _Gender = PersonGender.Unknown;


        /// <summary>
        /// For Adult (Erotic, Porn)
        /// </summary>
        [Index(6)]
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
        /// Birthday
        /// </summary>
        [Index(7)]
        public virtual DateTime? Birthday
        {
            get { return _Birthday; }
            set
            {
                if (value != _Birthday)
                {
                    _Birthday = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime? _Birthday = null;


        /// <summary>
        /// Deathday
        /// </summary>
        [Index(8)]
        public virtual DateTime? Deathday
        {
            get { return _Deathday; }
            set
            {
                if (value != _Deathday)
                {
                    _Deathday = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime? _Deathday = null;


        /// <summary>
        /// Biography
        /// </summary>
        [LimePropertyAttribute(Multiline = true)]
        [Index(9)]
        public virtual string Biography
        {
            get { return _Biography; }
            set
            {
                if (value != _Biography)
                {
                    _Biography = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _Biography = null;


        /// <summary>
        /// Homepage
        /// </summary>
        [Index(10)]
        public virtual string Homepage
        {
            get { return _Homepage; }
            set
            {
                if (value != _Homepage)
                {
                    _Homepage = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _Homepage = null;

        /// <summary>
        /// URL to the TmDB Profile page
        /// </summary>
        [Index(11)]
        public virtual string TmdbPage
        {
            get { return _TmdbPage; }
            set
            {
                if (value != _TmdbPage)
                {
                    _TmdbPage = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _TmdbPage = null;


        /// <summary>
        /// Opus (movie, TV Show, album...) where this person is credited
        /// </summary>
        [Index(12)]
        public virtual IList<LimeOpus> Opus
        {
            get { return _Opus; }
            set
            {
                if (value != _Opus)
                {
                    _Opus = value;
                    OnPropertyChanged();
					OnPropertyChanged("OpusString");
				}
            }
        }
        private IList<LimeOpus> _Opus = null;


        /// <summary>
        /// Collection of pictures where this person is visible
        /// </summary>
        [Index(13)]
        public virtual IList<LimePicture> Pictures
        {
            get { return _Pictures; }
            set
            {
                if (value != _Pictures)
                {
                    _Pictures = value;
                    OnPropertyChanged();
                }
            }
        }
        private IList<LimePicture> _Pictures = null;


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region ctors

		public LimePerson()
        {
			LimeLib.LifeTrace(this);
        }

		public LimePerson(string namerole)
		{
			LimeLib.LifeTrace(this);

			FromString(namerole, CultureInfo.CurrentCulture);
		}

		public LimePerson(string name, string[] roles)
        {
			LimeLib.LifeTrace(this);
			
			Name = name;
            Roles = roles;
        }

        public LimePerson (int id, string name, string[] roles)
        {
			LimeLib.LifeTrace(this);
			
			TmdbId = id;
            Name = name;
            Roles = roles;
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
            var ret = Name;
			if (Roles != null && Roles.Length>0)
			{
				ret += " (" + string.Join(", ", Roles) + ")";  
			}

			return ret;

		}

        /// <summary>
        /// Update the object from its string representation (i.e. Deserialize).
        /// </summary>
        /// <param name="source">String representing the object content (to be)</param>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>true if the parsing of the string was successful, false otherwise</returns>
        public override bool FromString(string source, CultureInfo culture)
        {
			var ret = true;
			var name = source;
			string[] roles = null;

			if (source != null)
			{
				int idx = source.IndexOf(" (");
				if (idx >= 0)
				{
					name = source.Substring(0, idx);

					var rolestr = source.Substring(idx + 2).Trim();
					if (rolestr.Last() != ')') ret = false;
					if (rolestr.Length > 0)
						rolestr = rolestr.Substring(0, rolestr.Length - 1);

					roles = rolestr.Split(',');
					for (int i=0; i<roles.Length; i++)
					{
						roles[i] = roles[i].Trim();
					}
				}

				// Hanlde spaces
				name = name.Trim().Replace(Environment.NewLine, " ").Replace("  ", " ");
			}

			if (! string.Equals(Name, name, StringComparison.CurrentCultureIgnoreCase) )
			{
				// person has actually changed identitity...
				IsLoaded = false;

				TmdbId = 0;
				TmdbPage = null;
				Alias = null;
				Gender = PersonGender.Unknown;
				Adult = false;
				Birthday = null;
				Deathday = null;
				Biography = null;
				Homepage = null;
				Opus = null;
				Pictures = null;
			}

			Name = name;
			Roles = roles;
			Flags = 0;

            return ret;
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Methods


		/// <summary>
		/// Request for the current LoadAsync() Stack cancellation
		/// </summary>
		public static void Cancel()
		{
			if (Stack != null)
			{
				// Close this Queue
				LimeMsg.Debug("LimePerson Cancel: {0}", Stack.Count);

				// This will eventually kill the Consumer in LoadAsync
				Stack.Add(null);
			}
		}


		/// <summary>
		/// Schedule the loading asynchronously of the Persons icons.
		/// The Order in which it was requested is LIFO, using a Thread-Safe Stack.
		/// This Stack can be cancelled using <see cref="Cancel"/>.
		/// </summary>
		///<param name="download">Enable to download from Internet if the peroson in missing in the local DB</param>
		public void LoadAsync(bool download = false)
		{
			if (download) IsDownloading = true;

			// Start Queue 
			if (Stack == null)
			{
				LimeMsg.Debug("LimePerson LoadAsync: Start Stack Consumer: {0}", download);
				Stack = new BlockingCollection<LimePerson>(new ConcurrentStack<LimePerson>())
				{
					this
				};

				Task.Run(() =>
				{
					foreach (var person in Stack.GetConsumingEnumerable())
					{
						// Cancellation
						if (person == null)
						{
							LimeMsg.Debug("LimePerson LoadAsync: Stop Stack Consumer: {0}", Stack.Count);
							while (Stack.TryTake(out var pers))
							{
								if (pers != null)	pers.IsDownloading = false;
							}
							continue;
						}

						LimeMsg.Debug("LimePerson LoadAsync: {0}", person?.Name);
						lock (Stack)
                        {
							try
							{
								person.Load();
							}
							finally
							{
								person.IsDownloading = false;
							}
						}
					}
				});
			}
			else
			{
				// Add Item to Stack
				Stack.Add(this);
			}

		}


		/// <summary>
		/// Try to retrieve the Person data from the local database, or Internet
		/// </summary>
		/// <returns>true if successful (false if not found)</returns>
		public bool Load()
		{
			if (IsLoading || IsSaving || IsLoaded) return true;
			if (string.IsNullOrEmpty(Name)) return false;


			// Make filename from person name
			string path = null;
			string localDb = LimeLib.ResolvePath(LocalDbPath);
			if (localDb != null)
			{
				var name = Name;

				// Remove characters forbidden in filenames
				string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
				foreach (char c in invalid)
				{
					name = name.Replace(c.ToString(), "");
				}

				path = Path.Combine(localDb, name + Extension);
			}
			
			try
			{
				IsLoading = true;

				if (File.Exists(path))
				{
					LimePerson lp;
					// Local Database
					using (Stream file = File.Open(path, FileMode.Open))
					{
						lp = ZeroFormatterSerializer.Deserialize<LimePerson>(file);
					}

					// Properties to be preserved
					var roles = Roles;
					var rolesReadOnly = RolesReadOnly;

					// Copy properties
					LimeLib.CopyPropertyValues(lp, this);

					// Restore Properties to be preserved
					Roles = roles;
					RolesReadOnly = rolesReadOnly;

					// Done
					IsLoaded = true;
					return true;
				}
			}
			catch
			{
				LimeMsg.Error("UnableFileDir", path);
			}
			finally
			{
				IsLoading = false;
			}

			if (IsDownloading && MetaSearch != null)
			{
				bool ret = false;
				try
				{
					IsLoading = true;
					LimeMsg.Debug("Person Load: Download {0}", Name);
					ret = MetaSearch.GetPersonAsync(this).Result;
				}
				catch
				{
					LimeMsg.Error("ErrPersonDownload", Name);
				}
				finally
				{
					IsLoading = false;
					IsDownloading = false;
				}

				IsLoaded = true;

				if (ret && AutoSave)
				{
					Save();
				}

				return ret;
			}

			return false;
		}

		/// <summary>
		/// Try to save the Person data to the local database.
		/// </summary>
		public void Save()
        {
            if (IsBusy) return;
            if (string.IsNullOrEmpty(Name)) return;

			// Make filename from person name
			string path = null;
			string localDb = LimeLib.ResolvePath(LocalDbPath);
			if (localDb != null)
			{
				var name = Name;

				// Remove characters forbidden in filenames
				string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
				foreach (char c in invalid)
				{
					name = name.Replace(c.ToString(), "");
				}

				path = Path.Combine(localDb, name + Extension);
			}

			try
			{
                IsSaving = true;
                LimeMsg.Debug("Person SaveAsync: {0}", Name);
                using (Stream file = File.Open(path, FileMode.Create))
                {
                    ZeroFormatterSerializer.Serialize(file, this);
                }

            }
            catch
            {
                LimeMsg.Error("UnableFileDir", path);
            }
            finally
            {
                IsSaving = false;
                IsLoaded = true;
            }
        }

        #endregion


    }
}
