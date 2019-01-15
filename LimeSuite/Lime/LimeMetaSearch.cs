/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     17-05-2016
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMDbLib.Client;
using TagLib;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using System.IO;
using TMDbLib.Objects.Movies;
using System.Net;
using System.Drawing;

namespace Lime
{

	/// <summary>
	/// Retrieve metadata of a file (Video...) and handle file-tags 
	/// </summary>
	public class LimeMetaSearch
	{

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
		/// Define the prefered language to retrieve information of files.
		/// </summary>
		public string Language;

		/// <summary>
		/// Language field to be used internally
		/// </summary>
		private string _Language
		{
			get
			{
				return string.IsNullOrEmpty(Language) ? LimeLanguage.Language : Language;
			}
		}

		/// <summary>
		/// Enable to retrieve adult-rated movie
		/// </summary>
		public bool Adult;

		/// <summary>
		/// TMDB API Key for Lime Suite, and Lime Suite only
		/// </summary>
		private const string TmdbApiKey = "2298af1834a249a947f64a048080c7f9";

		/// <summary>
		/// https://github.com/LordMike/TMDbLib/
		/// </summary>
		private TMDbClient TmdbClient;


		/// <summary>
		/// Store a Web access to download pitures
		/// </summary>
		private WebClient WebClient;


		/// <summary>
		/// Keep GenreId/Genre LUT cached
		/// </summary>
		private List<Genre> GenresLUT = null;

		/// <summary>
		/// Language at which the GenreId/Genre LUT got cached
		/// </summary>
		private string GenresLUTLanguage = null;


		/// <summary>
		/// Replacer to use to clean up the persons/movie descriptions
		/// </summary>
		private static readonly ReplacerChain CleanupDescription = new ReplacerChain
		{
			new Replacer(@"From Wikipedia.*", "", ReplacerOptions.Regex | ReplacerOptions.All),
		};

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region ctors


		/// <summary>
		/// Constructor of LimeTag singleton
		/// </summary>
		public LimeMetaSearch()
		{
		}

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Tag Methods


		private Picture DownloadPic(string url, PictureType type)
		{
			if (url == null) return null;

			// Get URL for original picture size
			var posterUri = TmdbClient.GetImageUrl(TmdbClient.Config.Images.PosterSizes.Last(), url);

			// Download the image
			var bytes = WebClient.DownloadData(posterUri);

			string ext;
			try
			{
				ext = Path.GetExtension(url);
			}
			catch
			{
				ext = null;
			}
			var ret = new Picture(bytes);

			if (string.IsNullOrEmpty(ext))
			{
				ext = Picture.GetExtensionFromMime(ret.MimeType);
			}
			else
			{
				ret.MimeType = Picture.GetMimeFromExtension(ext);
			}

			ret.Type = type;
			ret.Filename = ret.Description = type.ToString() + ext;

			return ret;
		}


		private void DownloadPic(ref List<LimePicture> pics, string url, PictureType type)
		{
			var pic = DownloadPic(url, type);
			if (pic == null) return;
			pics.Add(new LimePicture(pic));
		}


		private void DownloadPic(ref List<Picture> pics, string url, PictureType type)
		{
			var pic = DownloadPic(url, type);
			if (pic == null) return;
			pics.Add(pic);
		}


		/// <summary>
		/// Connect to IMDB Database
		/// </summary>
		public void Connect()
		{
			LimeMsg.Debug("LimeMetaSearch: Connecting...");

			// Initialize WEB client to download data
			WebClient = new WebClient();

			// Initialize TMDB
			TmdbClient = new TMDbClient(TmdbApiKey);

			// Retrieve TMDB config
			TmdbClient.GetConfig();

			LimeMsg.Debug("LimeMetaSearch: Connected.");
		}


		/// <summary>
		/// Retrieve the Genres from a list of TMDb genre-ids
		/// </summary>
		/// <param name="genreids">list of TMDb genre-ids</param>
		/// <param name="adult">Set if it is a movie for adults</param>
		/// <returns>Genres as array of string</returns>
		public async Task<string[]> GetGenresAsync(List<int> genreids, bool adult = false)
		{
			if (GenresLUTLanguage != _Language || GenresLUT == null)
			{
				// Retrieve LUT Genres Id --> Description
				LimeMsg.Debug("LimeMetaSearch: GetGenresAsync: {0}", _Language);
				GenresLUTLanguage = _Language;
				if (TmdbClient == null) Connect();
				GenresLUT = await TmdbClient.GetMovieGenresAsync(_Language);
			}

			List<string> ret = new List<string>(genreids.Count);
			if (adult) ret.Add(LimeLanguage.Translate(IniLanguageSection, "GenreAdult", "GenreAdult"));

			foreach (var id in genreids)
			{
				foreach (var genre in GenresLUT)
				{
					if (genre.Id == id)
					{
						ret.Add(genre.Name);
						break;
					}
				}
			}

			return ret.ToArray();
		}



		/// <summary>
		/// Search a movie by name
		/// </summary>
		/// <param name="name">Search string (Title of movie)</param>
		/// <returns>List of Metadata objects representing the match found</returns>
		public async Task<List<LimeOpus>> SearchVideoAsync(string name)
		{
			List<LimeOpus> ret = null;

			if (TmdbClient == null) Connect();

			SearchContainer<SearchMovie> results = await TmdbClient.SearchMovieAsync(name, _Language, 0, Adult);

			// TODO: Retrieve TvShow if a number is mentioned
			//var tvshow = await TmdbClient.SearchTvShowAsync(name);

			ret = new List<LimeOpus>(results.Results.Count);
			if (results.Results.Count == 0) return ret;

			foreach (SearchMovie result in results.Results)
			{
				var genres = await GetGenresAsync(result.GenreIds, result.Adult);

				Uri imageUri = null;
				if (result.PosterPath != null)
				{
					imageUri = TmdbClient.GetImageUrl(TmdbClient.Config.Images.PosterSizes.Last(), result.PosterPath);
				}

				var meta = new LimeOpus(LimeOpusType.Movie, result.Id, result.Title ?? "")
				{
					OriginalTitle = result?.OriginalTitle ?? "",
					OriginalLanguage = result?.OriginalLanguage ?? "",
					Description = CleanupDescription.Replace(result.Overview ?? "").Trim(),
					Score = result.VoteAverage > 0 ? result.VoteAverage / 2.0 : 0.0,
					Released = result.ReleaseDate != null ? (uint)result.ReleaseDate.Value.Year : 0,
					Genres = genres,
					PosterUrl = imageUri?.AbsoluteUri
				};

				meta.BuildToolTip();

				ret.Add(meta);
			}

			return ret;
		}


		/// <summary>
		/// Download the metadata on a movie from TMDB by its TMDB identifier
		/// </summary>
		/// <param name="id">TMDB movie identifier</param>
		/// <returns>Metadata object representing the movie</returns>
		public async Task<LimeMetadata> GetVideoAsync(int id)
		{
			LimeMetadata meta = null;

			if (TmdbClient == null) Connect();

			Movie movie = await TmdbClient.GetMovieAsync(id, _Language, MovieMethods.Images | MovieMethods.Videos | MovieMethods.Reviews | MovieMethods.Credits);

			// Create Metadata object representing the movie
			meta = new LimeMetadata(MediaType.Video);

			// Copy metadata from Web to Tag
			meta.Add("TMDB_ID", movie.Id, readOnly: true, visible: false);
			meta.Add("Title", movie.Title);
			meta.Add("Tagline", movie?.Tagline);
			meta.Add("Description", CleanupDescription.Replace(movie?.Overview).Trim());

			//if (movie.OriginalLanguage != null) meta.Add("OriginalLanguage", movie.OriginalLanguage);
			meta.Add("Score", movie.VoteAverage != 0 ? movie.VoteAverage / 2.0 : 0.0);
			meta.Add("Released", movie.ReleaseDate != null ? (uint)movie.ReleaseDate.Value.Year : 0);

			// Genres
			var genres = new string[movie.Genres.Count];
			for (int i = 0; i < genres.Length; i++)
			{
				genres[i] = movie.Genres[i].Name;
			}
			meta.Add("Genres", new StringComposite<string>(genres));

			// Actors
			var actors = new List<LimePerson>();
			foreach (var cast in movie.Credits.Cast)
			{
				var person = new LimePerson(cast.Id, cast.Name, new string[] { cast.Character });
				actors.Add(person);
			}
			meta.Add("Actors", new StringComposite<LimePerson>(actors, ";", Environment.NewLine));

			// Director
			string director = null;
			foreach (var crew in movie.Credits.Crew)
			{
				switch (crew.Department)
				{
					case "Directing":
						if (director == null)
						{
							director = crew.Name;
						}
						break;

					case "Writing": // do nothing so far
						break;

					default: // do nothing so far
						break;
				}
			}
			meta.Add("Director", director);

			var pics = new List<Picture>(1);
			DownloadPic(ref pics, movie.PosterPath, PictureType.OtherFileIcon);

			// Store more images
			foreach (var img in movie.Images.Posters)
			{
				DownloadPic(ref pics, img.FilePath, PictureType.OtherFileIcon);
			}

			foreach (var img in movie.Images.Backdrops)
			{
				DownloadPic(ref pics, img.FilePath, PictureType.Illustration);
			}


			// Collections
			string collection = null;
			uint season = 0;
			uint seasonCount = 0;
			if (movie.BelongsToCollection != null)
			{
				var collec = await TmdbClient.GetCollectionAsync(movie.BelongsToCollection.Id, _Language);

				if (collec != null && collec.Parts != null)
				{
					collection = collec.Name;
					seasonCount = (uint)collec.Parts.Count;
					for (int i = 0; i < collec.Parts.Count; i++)
					{
						if (collec.Parts[i].Id == movie.Id)
						{
							season = (uint)i + 1;
							break;
						}
					}

					DownloadPic(ref pics, collec.PosterPath, PictureType.FrontCover);
					DownloadPic(ref pics, collec.BackdropPath, PictureType.BackCover);
				}
			}
			meta.Add("Collection", collection);
			meta.Add("SeasonCount", seasonCount);
			meta.Add("Season", season);


			// Finalize images
			meta.Add("Pictures", pics.ToArray(), false, false);

			// Finalize
			meta.BuildToolTip();
			return meta;
		}


		/// <summary>
		/// Update a Person data from TMDb
		/// </summary>
		/// <param name="person">Person to be updated</param>
		/// <returns>true if updated</returns>
		public async Task<bool> GetPersonAsync(LimePerson person)
		{
			if (TmdbClient == null) Connect();

			// Retrieve person-ID by name
			if (person.TmdbId == 0)
			{
				var search = await TmdbClient.SearchPersonAsync(person.Name, 0, Adult);
				if (search.Results != null && search.Results.Count > 0 && 
					string.Equals(search.Results[0].Name, person.Name, StringComparison.InvariantCultureIgnoreCase) )
				{
					person.TmdbId = search.Results[0].Id;
				}
			}

			// Retrieve person by ID
			if (person.TmdbId != 0)
			{
				var db = await TmdbClient.GetPersonAsync(
					person.TmdbId,
					TMDbLib.Objects.People.PersonMethods.Images |
					TMDbLib.Objects.People.PersonMethods.MovieCredits
					);

				if (db == null) return false;

				var pics = new List<LimePicture>(10);
				foreach (var img in db.Images.Profiles)
				{
					DownloadPic(ref pics, img.FilePath, PictureType.Artist);
				}
				person.Pictures = pics.ToArray();

				person.ImdbId = db.ImdbId;
				person.TmdbPage = db.ProfilePath;
				person.Name = db.Name;
				person.Alias = db.AlsoKnownAs?.ToArray();
				person.Gender = (LimePerson.PersonGender)db.Gender;
				person.Adult = db.Adult;
				person.Birthday = db.Birthday;
				person.Deathday = db.Deathday;
				person.Biography = CleanupDescription.Replace(db.Biography).Trim();
				person.Homepage = db.Homepage;

				// Build Opus

				var opus = new List<LimeOpus>();
				if (db.MovieCredits?.Cast != null)
					foreach (var cast in db.MovieCredits.Cast)
					{
						// Avoid duplicates
						var op = opus.Find(o => o.TmdbId == cast.Id);
						if (op != null)
						{
							if (! op.Roles.Contains(cast.Character) )
								op.Roles.Add(cast.Character);
						}
						else
						{
							op = new LimeOpus(LimeOpusType.Movie, cast.Id, cast.Title, cast.Character)
							{
								OriginalTitle = cast.OriginalTitle,
								Adult = cast.Adult,
								Released = cast.ReleaseDate != null ? (uint)cast.ReleaseDate.Value.Year : 0
							};
							if (cast.PosterPath != null)
							{
								op.PosterUrl = TmdbClient.GetImageUrl(TmdbClient.Config.Images.PosterSizes.Last(), cast.PosterPath)?.AbsoluteUri;
							}
							opus.Add(op);
						}
					}

				if (db.TvCredits?.Cast != null)
					foreach (var cast in db.TvCredits.Cast)
					{
						// Avoid duplicates
						var op = opus.Find(o => o.TmdbId == cast.Id);
						if (op != null)
						{
							if (!op.Roles.Contains(cast.Character))
								op.Roles.Add(cast.Character);
						}
						else
						{
							op = new LimeOpus(LimeOpusType.TvShow, cast.Id, cast.Name, cast.Character)
							{
								OriginalTitle = cast.OriginalName,
								Released = cast.FirstAirDate != null ? (uint)cast.FirstAirDate.Value.Year : 0
							};
							if (cast.PosterPath != null)
							{
								op.PosterUrl = TmdbClient.GetImageUrl(TmdbClient.Config.Images.PosterSizes.Last(), cast.PosterPath)?.AbsoluteUri;
							}
							opus.Add(op);
						}
					}

				if (db.MovieCredits?.Crew != null)
					foreach (var cast in db.MovieCredits.Crew)
					{
						// Avoid duplicates
						var op = opus.Find(o => o.TmdbId == cast.Id);
						if (op != null)
						{
							if (!op.Roles.Contains(cast.Department))
								op.Roles.Add(cast.Department);
						}
						else
						{
							op = new LimeOpus(LimeOpusType.Movie, cast.Id, cast.Title, cast.Department)
							{
								OriginalTitle = cast.OriginalTitle,
								Adult = cast.Adult,
								Released = cast.ReleaseDate != null ? (uint)cast.ReleaseDate.Value.Year : 0
							};
							if (cast.PosterPath != null)
							{
								op.PosterUrl = TmdbClient.GetImageUrl(TmdbClient.Config.Images.PosterSizes.Last(), cast.PosterPath)?.AbsoluteUri;
							}
							opus.Add(op);
						}
					}


				if (db.TvCredits?.Crew != null)
					foreach (var cast in db.TvCredits.Crew)
					{
						// Avoid duplicates
						var op = opus.Find(o => o.TmdbId == cast.Id);
						if (op != null)
						{
							if (!op.Roles.Contains(cast.Department))
								op.Roles.Add(cast.Department);
						}
						else
						{
							op = new LimeOpus(LimeOpusType.TvShow, cast.Id, cast.Name, cast.Department)
							{
								OriginalTitle = cast.OriginalName,
								Released = cast.FirstAirDate != null ? (uint)cast.FirstAirDate.Value.Year : 0
							};
							if (cast.PosterPath != null)
							{
								op.PosterUrl = TmdbClient.GetImageUrl(TmdbClient.Config.Images.PosterSizes.Last(), cast.PosterPath)?.AbsoluteUri;
							}
							opus.Add(op);
						}
					}

				// Set genres
				foreach (var op in opus)
				{
					if (op.Adult) op.Genres = new string[] { LimeLanguage.Translate(IniLanguageSection, "GenreAdult", "GenreAdult") };
					op.BuildToolTip();
				}

				person.Opus = opus.ToArray();

				return true;
			}

			return false;
		}


		#endregion



		// --------------------------------------------------------------------------------------------------
		#region Class functions

		/// <summary>
		/// Retrieve an image as Bitmap from an URL/URI
		/// </summary>
		/// <param name="uri">URI representing an image</param>
		/// <returns>the bitmap object representing the image</returns>
		public static Bitmap URItoBitmap(string uri)
		{
			Bitmap ret = null;
			if (String.IsNullOrEmpty(uri)) return null;

			if (uri.StartsWith("data:image/")) // data URI
			{
				// decode header of data URI
				int pos = uri.IndexOf(";base64,");
				if (pos < 0) return null;

				// Decode data body
				var binData = Convert.FromBase64String(uri.Substring(pos + 8));

				// Get Image from bytes in memory
				var stream = new MemoryStream(binData);
				ret = new Bitmap(stream);
			}

			return ret;
		}


		#endregion


	}
}
