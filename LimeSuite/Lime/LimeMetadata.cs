/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     03-09-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using MSjogren.Samples.ShellLink;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Media;

namespace Lime
{
	// --------------------------------------------------------------------------------------------------

	/// <summary>
	/// List the available media types
	/// </summary>
	[Flags]
	public enum MediaType
	{
		None = 0,
		Audio = 1,
		Video = 2,
		Image = 4,
		Document = 8
	}


    /// <summary>
    /// Specify how a new window should appear when the ShellLink opens.
    /// </summary>
    public enum ShellLinkWindowStyle
    {
        Normal = 0,
        Minimized = 2,
        Maximized = 3
    }


	// --------------------------------------------------------------------------------------------------




	/// <summary>
	/// Represent the Metadata contained in a <see cref="LimeItem"/> as a collection of LimeProperty
	/// </summary>
	[SupportedOSPlatform("windows")]
	public class LimeMetadata : LimePropertyCollection
	{
		// --------------------------------------------------------------------------------------------------
		#region Constants & Static properties

		
		/// <summary>
		/// Section for language access
		/// </summary>
		private const string IniLanguageSection = "Text";

		/// <summary>
		/// Maximum number of performers shown in tooltip
		/// </summary>
		private const int MaxPerformersTip = 4;

		/// <summary>
		/// Format template to put the property/value in text (language-dependent)
		/// </summary>
		private static string FormatProperty = null;

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Properties

		/// <summary>
		/// Get the type of media.
		/// </summary>
		public readonly MediaType Type;
		
		/// <summary>
		/// Store the Tag informations
		/// </summary>
		private readonly TagLib.File TagLibFile = null;

        /// <summary>
        /// Generate a tooltip from the metadata
        /// </summary>
        public string Tooltip { get; private set; } = null;


		/// <summary>
		/// Return the picture(s) associated with the media
		/// </summary>
		public object Pictures
		{
			get
			{
				return _Pictures;
			}
			private set
			{
				if (value != _Pictures)
				{
					_Pictures = value;
					OnPropertyChanged(false);
				}
			}
		}
		private object _Pictures = null;

		/// <summary>
		/// Get the Search name
		/// </summary>
		public LimeProperty Search
		{
			get 
			{
				return _Search;
			}
			set
			{
				if (value != _Search)
				{
					_Search = value;
					OnPropertyChanged(false);
				}
			}
		}
		private LimeProperty _Search = null;


		/// <summary>
		/// Get the Title metadata
		/// </summary>
		public string Title
		{
			get
			{
				var title = Get("Title");
				if (title == null || string.IsNullOrEmpty(title.Value))
				{
					title = Get("Name");
				}
				return title?.Value;
			}
		}

		/// <summary>
		/// Get the Description metadata
		/// </summary>
		public string Description
		{
			get
			{
				var desc = Get("Description");
				return desc?.Value;
			}
		}


		/// <summary>
		/// Get the Persons metadata
		/// </summary>
		public ObservableCollection<LimePerson> Persons
		{
			get
			{
				return _Persons;
			}
			private set
			{
				if (value != _Persons)
				{
					_Persons = value;
					OnPropertyChanged(false);
				}
			}
		}
		private ObservableCollection<LimePerson> _Persons = null;


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region ctors

		/// <summary>
		/// Construct an empty Metadata object
		/// </summary>
		public LimeMetadata(MediaType type = MediaType.None) : base (null)
		{
			// Inititalize object
			Type = type;
		}


		/// <summary>
		/// Construct and retrieve Metadata from a directory/file path
		/// </summary>
		/// <param name="item">The <see cref="LimeItem"/> element to construct Metadata for.</param>
		/// <param name="coverOnly">Try to load only a cover, not the system icon</param>
		public LimeMetadata(LimeItem item, bool coverOnly) : this()
		{
            LimeLib.LifeTrace(this);

			// Disable modification detection
			_Modified = null;


			LimeMsg.Debug("LimeMetadata: {0}", item.Name);
			string path = item.Path;

			if (!coverOnly)
            {
				// Add Name
				Add("Name", item.Name, true, false);
				if (item.Link != null)
				{
					// Link
					path = item.Link;
					Add("LinkLabel");
				}

				// Handle tasks
				if (item.Task)
				{
					Add("Task", item.Name, true);
				}

				// Display path
				Add("Path", item, "Path", readOnly: item.Task);

			}

			// Retrieve Tags
			if (!item.Task && !item.Directory && !LimeLib.IsPIDL(path) && !LimeLib.IsSSPD(path))
			{
                LimeMsg.Debug("LimeMetadata: TagLib: {0}", item.Name);
                try
                {
                    TagLibFile = TagLib.File.Create(path, TagLib.ReadStyle.Average | TagLib.ReadStyle.PictureLazy);
                }
				catch
				{
					LimeMsg.Debug("LimeMetadata: {0}: Failed TagLib.File.Create({1})", item.Name, path);
				}
			}

            // Extract Tags
            if (TagLibFile != null)
            {
                LimeMsg.Debug("LimeMetadata: TagLib done: {0}", item.Name);

                // Retrieve Type
                if (TagLibFile.Properties != null && TagLibFile.Properties.Codecs != null)
                {

                    TagLib.MediaTypes[] prioTypes = new TagLib.MediaTypes[] {
                    TagLib.MediaTypes.Video,  TagLib.MediaTypes.Audio, TagLib.MediaTypes.Photo, TagLib.MediaTypes.Text
                    };

                    var codecs = TagLibFile.Properties.Codecs;
                    foreach (var codec in codecs)
                    {
						if (codec != null)
						{
							TagLib.MediaTypes mask = codec.MediaTypes | (TagLib.MediaTypes)Type;

							foreach (var typ in prioTypes)
							{
								if ((mask & typ) != 0)
								{
									Type = (MediaType)typ;
									break;
								}
							}
						}
                    }
                }
                else if (TagLibFile is TagLib.Image.NoMetadata.File)
                {
                    Type = MediaType.Image;
                }
            }

            // Handle Links
            if (!coverOnly && item.Link != null)
            {
                using (var link = new ShellShortcut(item.Path))
                {
                    
                    Add("LinkWorkdir", link.WorkingDirectory);
                    //Add("LinkKey", link.Hotkey);
                    Add("LinkWindowStyle", (ShellLinkWindowStyle)link.WindowStyle);

                    if (Type == MediaType.None)
                    {
                        Add("LinkArguments", link.Arguments);
                    }

                    Add("LinkComment", link.Description);

                }


                Add("TagLabel");

                // Target Path
                if (LimeLib.IsPIDL(item.Link))
                {
                    // Retrieve name of the PIDL
                    try
                    {
                        var dInfo = new DirectoryInfoEx(new ShellDll.PIDL(LimeLib.GetPIDL(item.Link), true));
                        Add("LinkTarget", dInfo.Label, true);
                    }
                    catch { }
                }
                else
                {
                    Add("LinkTarget", item.Link, LimeLib.IsSSPD(item.Link));
                }
            }


            if (TagLibFile != null)
            { 
                LimeMsg.Debug("LimeMetadata: TagLib done 2: {0}", item.Name);
                // Build the Properties
                BuildProperties();


				// Build the Pictures image
				BuildCover(coverOnly);
			}
			else if (!coverOnly)
			{
				// Build the Pictures image from the file icon
				using (var bmp = item.Bitmap(256))
                {
					Pictures = LimeLib.ImageSourceFrom(bmp);
				}
			}

			if (!coverOnly)
            {
				// Finalize
				BuildToolTip();
			}

			// re-enable modification detection
			_Modified = false;
            LimeMsg.Debug("LimeMetadata End: {0}", item.Name);

        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Methods

        public void Copy(LimeMetadata source)
		{
			if (source == null) return;

			LimeMsg.Debug("LimeMetadata Copy: {0}", source.Title);

            bool buildcover = false;
			foreach (var src in source)
			{
				if (src.Visible && !src.ReadOnly || src.Ident == "Pictures")
				{
                    if (TryGet(src.Ident, out var dest))
                    {
                        LimeMsg.Debug("LimeMetadata Copy: key: {0} = {1}", src.Ident, src.Value);
                        dest.Content = src.Content;

                        if (src.Ident == "Pictures") buildcover = true;
                    }
				}
			}

            if (buildcover)
            {
                BuildCover(coverOnly: false);
            }
        }


        /// <summary>
        /// Format and return a string representing a Property, ended by a newline.
        /// </summary>
        /// <param name="key">key of the property</param>
        /// <returns>the property representation as text ended by newline, or empty string if the property is not found.</returns>
        private string FormatProp(string key)
		{
			string ret = "";
            var prop = Get(key);
            if (prop != null)
			{
				var value = FormatProp(prop.Name, prop, false);
				if (value != null)
				{
					ret = FormatProp(prop.Name, prop, false) + Environment.NewLine;
				}
			}

			return ret;
		}


        /// <summary>
        /// Compose a message defined by its Translate key.
        /// </summary>
        /// <param name="title">title of the message to expend</param>
        /// <param name="value">object to be displayed in the message.</param>
		/// <param name="translate">Translate the key if true.</param>
        /// <returns>the constructed message</returns>
        private string FormatProp(string title, object value, bool translate = true)
		{
			string msg = translate ? LimeLanguage.Translate(IniLanguageSection, title, title) : title;

			if (value != null)
			{
				if (value is LimeProperty prop)
				{
					value = prop.Content;
				}

				if (FormatProperty == null)
					FormatProperty = LimeLanguage.Translate(IniLanguageSection, "FormatProperty", "FormatProperty");

				if (value == null || string.IsNullOrEmpty(value.ToString())) return null;

				var val = value;


                if (value.GetType().IsArray && typeof(string).IsAssignableFrom(value.GetType().GetElementType()))
				{
					string sep = LimeLanguage.Translate(IniLanguageSection, "ListSeparator", ", ");
					val = string.Join(sep, (string[])value);
				}

				try
				{
					msg = String.Format(FormatProperty, msg, val);
				}
				catch
				{
					// Fallback
					return null;
				}
			}

			return msg;
		}


		/// <summary>
		/// Compose a property value defined by its Translate key.
		/// </summary>
		/// <param name="key">Key of the message to expend</param>
		/// <param name="args">arguments to be expanded (using String.Format) in the value.</param>
		/// <returns>the constructed message</returns>
		private string FormatValue(string key, params object[] args)
		{
			string msg = LimeLanguage.Translate(IniLanguageSection, key, key);

			if (args != null && args.Length > 0)
			{
				try
				{
					msg = string.Format(msg, args);
				}
				catch
				{
					// Fallback
					return null;
				}
			}

			return msg;
		}


		/// <summary>
		/// Build the Property collection
		/// </summary>
		private void BuildProperties()
		{
			bool isVideo = Type == MediaType.Video;

			Add("Title", TagLibFile.Tag, "Title");


			if (Type == MediaType.Image)
			{
				var ptag = TagLibFile.Tag as TagLib.Image.CombinedImageTag;

				if (ptag != null)
				{
					Add("Date", ptag, "DateTime", allowEmpty: true);
					Add("Model", ptag, "Model");
					Add("ISO", ptag, "ISOSpeedRatings");
				}
			}
			else if ((Type & (MediaType.Video | MediaType.Audio)) != 0)
			{
                Add("Tagline", TagLibFile.Tag, "Subtitle", multiline: true);
                Add("Description", TagLibFile.Tag, "Description", multiline: true);
                Add("Comment", TagLibFile.Tag, "Comment", multiline: true);

				Add("DateTagged", TagLibFile.Tag, "DateTagged", readOnly: true, allowEmpty: true);

				if (TagLibFile.Properties != null && TagLibFile.Properties.Duration > TimeSpan.Zero)
				{
					string val = FormatValue("DurationHMS",
						TagLibFile.Properties.Duration.Hours,
						TagLibFile.Properties.Duration.Minutes,
						TagLibFile.Properties.Duration.Seconds);

					Add("Duration", val, true, allowEmpty: true);
				}

				Add("Genres", new StringComposite<string>(TagLibFile.Tag.Genres));
                Add("Released", TagLibFile.Tag, "Year", allowEmpty: true);
				Add(isVideo ? "Collection" : "Album", TagLibFile.Tag, "Album");
				Add(isVideo ? "Season" : "Disc", TagLibFile.Tag, "Disc", allowEmpty: true);
				Add(isVideo ? "SeasonCount" : "DiscCount", TagLibFile.Tag, "DiscCount", allowEmpty: true);
				Add(isVideo ? "Episode" : "Track", TagLibFile.Tag, "Track", allowEmpty: true);
				Add(isVideo ? "EpisodeCount" : "TrackCount", TagLibFile.Tag, "TrackCount", allowEmpty: true);

				// retrieve the conductor
                Add(isVideo ? "Director" : "Conductor", TagLibFile.Tag, "Conductor");

				// Retrieve the persons/characters
				LimePerson[] persons;
				if (TagLibFile.Tag.Performers != null)
				{
					persons = new LimePerson[TagLibFile.Tag.Performers.Length];
					for (int i = 0; i < persons.Length; i++)
					{
						var name = TagLibFile.Tag.Performers[i];
						string[] roles = null;
						if (TagLibFile.Tag.PerformersRole != null && i < TagLibFile.Tag.PerformersRole.Length)
						{
							roles = TagLibFile.Tag.PerformersRole[i]?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
							if (roles != null)
							{
								for (int j = 0; j < roles.Length; j++)
								{
									roles[j] = roles[j].Trim();
								}
							}
						}
						persons[i] = new LimePerson(name, roles);
					}
				}
				else
				{
					persons = new LimePerson[0]; // enforce type
				}

				Add(isVideo ? "Actors" : "Artists", 
					new StringComposite<LimePerson>(persons, ";", Environment.NewLine), 
					null, multiline: true);

                Add("Pictures", TagLibFile.Tag, "Pictures", visible: false);

			}
		}


		/// <summary>
		/// Build the Tooltip string
		/// </summary>
		/// <returns>Tooltip</returns>
		public string BuildToolTip()
		{
			string ret = "";
			bool isVideo = Type == MediaType.Video;


			ret += FormatProp("Title");

			var origin = Get("OriginalTitle");
            var title = Get("Title");

            if (origin != null && (title == null || title.Value != origin.Value))
            {
                ret += FormatProp("OriginalTitle");
            }
			var collec = FormatProp(isVideo ? "Collection" : "Album");
			if (collec != "")
			{
				collec = collec.TrimEnd();

				var sn = Get(isVideo ? "Season" : "Disc");
				var sc = Get(isVideo ? "SeasonCount" : "DiscCount");
				var en = Get(isVideo ? "Episode" : "Track");
				var ec = Get(isVideo ? "EpisodeCount" : "TrackCount");

				if (sn != null && (uint)sn.Content>0)
				{
					collec += " - ";
					if (sc != null && (uint)sc.Content > 0)
						collec += FormatValue("FormatRatio", sn, sc);
					else
						collec += sn.Value;
				}

				if (en != null && (uint)en.Content > 0)
				{
					collec += " - ";
					if (ec != null && (uint)ec.Content > 0)
						collec += FormatValue("FormatRatio", en, ec);
					else
						collec += en.Value;
				}

				ret += collec + Environment.NewLine;
			}


			ret += FormatProp("Date");
			ret += FormatProp("DeviceModel");
			ret += FormatProp("ISO");
			ret += FormatProp("Duration");

			var genres = Get("Genres")?.Content as StringComposite<string>;

			var scoreo = Get("Score");
			double score = 0;
			if (scoreo != null) score = (double)scoreo.Content;

			var releaseo = Get("Released");
			uint release = 0;
			if (releaseo != null) release = (uint)releaseo.Content;

			string sep = LimeLanguage.Translate(IniLanguageSection, "ListSeparator", ", ");
			var relgenre = Get("ReleasedGenresScore");
			if (relgenre == null)
			{
				try
				{

					if (release != 0 && (genres != null && genres.Collection!= null && genres.Collection.Count > 0 || score!=0.0))
					{
						var format = LimeLanguage.Translate(IniLanguageSection, "FormatReleasedGenresScore", "FormatReleasedGenresScore");
						var msg = string.Format(format, release, string.Join(sep, genres.Collection), score);
						Add("ReleasedGenresScore", msg, true, false);
                        relgenre = Get("ReleasedGenresScore");
                    }
				}
				catch {}
			}

			if (relgenre != null)
			{
				ret += relgenre.Value + Environment.NewLine;
			}
			else
			{ 
				if (release != 0) ret += FormatProp("Released");
				if (genres != null && genres.Collection != null && genres.Collection.Count > 0)
					ret += FormatProp("Genres", string.Join(sep, genres.Collection));
				if (score != 0) ret += FormatProp("Score");
			}


			ret += FormatProp("Director");
			ret += FormatProp("Conductor");

			string performer = isVideo ? "Actors" : "Artists";
            var perf = Get(performer);
			if (perf != null && perf.Content is StringComposite<LimePerson> perfcomp && perfcomp.Collection != null)
			{
				var performers = perfcomp.Collection;
				string str = "";
				var maxPerf = performers.Count < MaxPerformersTip ? performers.Count : MaxPerformersTip;
				for (int i = 0; i < maxPerf; i++)
				{
					if (i > 0) str += ", ";
					str += performers[i].Name;
				}
				if (performers.Count > MaxPerformersTip) str += "...";
				ret += FormatProp(performer, str) + Environment.NewLine;
			}

            ret += FormatProp("Comment");

			if (!string.IsNullOrEmpty(ret)) ret = ret.TrimEnd();
			
			return Tooltip = ret;
		}


		/// <summary>
		/// Build the Pictures image from the content of the file
		/// </summary>
		/// <param name="path">path to the file</param>
		/// <param name="coverOnly">Try to load only a cover, not the system icon</param>
		/// <returns>Pictures image</returns>
		public void BuildCover(bool coverOnly)
		{
			TagLib.IPicture[] ret = null;

			if (TagLibFile != null)
			{
				string path = TagLibFile.Name;

				if (TagLibFile.Tag != null)
				{
					if (Type == MediaType.Image)
					{
                        ret = new TagLib.Picture[] { new TagLib.Picture(path) };
					}
					else if (TagLibFile.Tag.Pictures != null && TagLibFile.Tag.Pictures.Length > 0)
					{
						// Retain only the pictures, no the attachments of other types

						int count = 0;
						var pics = new List<TagLib.IPicture>(TagLibFile.Tag.Pictures.Length);
						foreach (TagLib.IPicture pic in TagLibFile.Tag.Pictures)
                        {
							if (pic.Type != TagLib.PictureType.NotAPicture)
                            {
								count++;
								pics.Add(pic);
							}
						}

						if (count>0)
                        {
							ret = count == TagLibFile.Tag.Pictures.Length ? TagLibFile.Tag.Pictures : pics.ToArray();
						}
					}
				}

				if (ret is null && !coverOnly)
				{
					BuildCover(path);
                    return;
				}
			}

			Pictures = ret;
		}


		/// <summary>
		/// Build a cover from the Jumbo icon of a file/directory
		/// </summary>
		/// <param name="path">path to the file</param>
		/// <returns>Pictures image</returns>
		public void BuildCover(string path)
		{
            LimeMsg.Debug("LimeMetadata BuildCover: path: {0}", path);
            ImageSource ret = null;

			try
			{
				LimeItem item = new LimeItem(path);
				using (var bmp = item.Bitmap(256))
				{
					ret = LimeLib.ImageSourceFrom(bmp);
				}
			}
			catch
			{
				LimeMsg.Debug("LimeMetadata BuildCover: Failed: path: {0}", path);
			}

			Pictures = ret;
		}



		/// <summary>
		/// Load the Connex data of the Metadata in the background
		/// </summary>
		///<param name="max">Max number of Persons which can be retrieved (downloaded/saved)</param>
		public void LoadAsync(int max = int.MaxValue)
        {
            if (TagLibFile != null && TagLibFile.Tag != null)
            {
                //LimeMsg.Debug("LimeMetadata LoadAsync: {0}", TagLibFile.Name);
                bool isVideo = Type == MediaType.Video;

				LimePerson.Cancel();

                var list = new List<LimePerson>(10);

                var cond = Get(isVideo ? "Director" : "Conductor");
                if (cond != null && cond.Content is string person)
                {
					if (!string.IsNullOrWhiteSpace(person))
					{
						list.Add(new LimePerson(person, new string[] { cond.Name }));
					}
                }

                var perf = Get(isVideo ? "Actors" : "Artists");
                if (perf != null && perf.Content is StringComposite<LimePerson> persons && persons.Collection != null)
                {
					foreach (var pers in persons.Collection)
					{
						list.Add(pers);
					}

					Persons = persons.Collection;
				}
				else
				{
					Persons = null;
				}

				// Start with the end as this is a LIFO stack
				for (int i = list.Count - 1; i>=0; i--)
                {
					var pers = list[i];
					if (!pers.IsLoaded)
                    {
                        pers.LoadAsync(i < max);
                    }
				}

            }

        }



        /// <summary>
        /// Save the Metadata, if these have changed
        /// </summary>
        public void Save()
		{
			LimeMsg.Debug("LimeMetadata Save: modified: {0}", Modified);
			if (!Modified) return;

            // Save Media
			if (TagLibFile != null && TagLibFile.Tag != null)
			{
                LimeMsg.Debug("LimeMetadata Save: TagLibFile: {0}", TagLibFile.Name);
                bool isVideo = Type == MediaType.Video;


				string[] genres = null;
				if (Get("Genres")?.Content is StringComposite<string> compgenres)
				{
					genres = compgenres.ToArray();
				}
				TagLibFile.Tag.Genres = genres;


                string conductor = null;
                if (Get(isVideo ? "Director" : "Conductor")?.Content is string person)
                {
                    conductor = person;
                }
                TagLibFile.Tag.Conductor = conductor;


				string[] performers = null;
                string[] performersRole = null;
                if (Get(isVideo ? "Actors" : "Artists")?.Content is StringComposite<LimePerson> comppers 
					&& comppers.Collection != null)
                {
					var persons = comppers.Collection;
                    performers = new string[persons.Count];
                    performersRole = new string[persons.Count];
                    for (int i = 0; i < persons.Count; i++)
                    {
                        performers[i] = persons[i].Name;
                        performersRole[i] = persons[i].Roles != null ? 
							string.Join("; ", persons[i].Roles) : null;
                    }
                }
                TagLibFile.Tag.Performers = performers;
                TagLibFile.Tag.PerformersRole = performersRole;


                TagLibFile.Tag.SetInfoTag();
				TagLibFile.Save();
            }

            // Save Link
            var lnk = Get("LinkTarget");
            if (lnk != null)
            {
                var path = Get("Path");
                LimeMsg.Debug("LimeMetadata Save: Link: {0}", path.Value);

                using (var link = new ShellShortcut(path.Value))
                {
                    if (!lnk.ReadOnly)
                    {
                        link.Path = lnk.Value;
                    }
                    link.WorkingDirectory = Get("LinkWorkdir").Value;
                    link.Description = Get("LinkComment").Value;

                    //link.Hotkey = (System.Windows.Forms.Keys)Get("LinkKey").Content;
                    link.WindowStyle = (System.Diagnostics.ProcessWindowStyle) Get("LinkWindowStyle").Content;

                    link.Arguments = Get("LinkArguments")?.Value;

                    link.Save();
                }
            }


            LimeMsg.Debug("LimeMetadata Save: done");
		}

		#endregion
	}
}
