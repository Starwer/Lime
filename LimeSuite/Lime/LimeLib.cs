/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     14-05-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using System;
using MSjogren.Samples.ShellLink;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ShellDll;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Media;

namespace Lime
{
    /// <summary>
    /// Library of general purpose functions (commodities) for handling Lime
    /// </summary>
    public static class LimeLib
	{
		// --------------------------------------------------------------------------------------------------
		#region Constants

		/// <summary>
		/// List of all the known file-extensions for Video
		/// </summary>
		public static readonly string[] cstVideoExtensions = { "webm", "mkv", "vob", "ogv", "ogg",
			"drc", "gif", "gifv", "mng", "avi", "mov", "qt", "wmv", "yuv", "rm", "rmvb", "asf", "amv", "mp4",
			"m4p", "m4v", "mpg", "mp2", "mpeg", "mpe", "mpv", "mpg", "mpeg", "m2v", "m4v",
			"svi", "3gp", "3g2", "mxf", "roq", "nsv", "flv", "f4v", "f4p", "f4a", "f4b" };

		public static string[] VideoExtensions = cstVideoExtensions;


		/// <summary>
		/// List of all the known file-extensions for Audio
		/// </summary>
		public static readonly string[] cstAudioExtensions = {"3gp", "aa", "aac", "aax", "act", "aiff", "amr", "ape",
			"au", "awb", "dct", "dss", "dvf", "flac", "gsm", "iklax", "ivs", "m4a", "m4b", "m4p", "mmf", "mp3",
			"mpc", "msv", "ogg", "oga, mogg", "opus", "ra", "rm", "raw", "sln", "tta", "vox", "wav", "wma",
			"wv", "webm", "8svx" };

		public static string[] AudioExtensions = cstAudioExtensions;

		/// <summary>
		/// List of all the known file-extensions for Image
		/// </summary>
		public static readonly string[] cstImageExtensions = { "ani", "anim", "apng", "art", "bmp", "bpg", "bsave",
			"cal", "cin", "cpc", "cpt", "dds", "dpx", "ecw", "exr", "fits", "flic", "flif", "fpx", "gif", "hdri",
			"hevc", "icer", "icns", "ico", "cur", "ics", "ilbm", "jbig", "jbig2", "jng", "jpeg", "kra", "mng",
			"miff", "nrrd", "ora", "pam", "pbm", "pgm", "ppm", "pnm", "pcx", "pgf", "pictor", "png", "psd", "psb",
			"psp", "qtvr", "ras", "rbe", "sgi", "tga", "tiff", "ufo", "ufp", "wbmp", "webp", "xbm", "xcf", "xpm",
			"xwd", "ciff", "dng", "ai", "cdr", "cgm", "dxf", "eva", "emf", "gerber", "hvif", "iges", "pgml", "svg",
			"vml", "wmf", "xar", "cdf", "djvu", "eps", "pdf", "pict", "ps", "swf", "xaml", };

		public static string[] ImageExtensions = cstImageExtensions;

		/// <summary>
		/// List of all the known file-extensions for Document
		/// </summary>
		public static readonly string[] cstDocumentExtensions = { "doc", "docx", "html", "htm", "odt", "pdf", "xls",
			"xlsx", "ods", "ppt", "pptx", "txt" };

		public static string[] DocumentExtensions = cstDocumentExtensions;

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Class functions

		/// <summary>
		/// Resolve a file-path. Follows the links if the last element is a link.
		/// </summary>
		/// <param name="path">path to resolve</param>
		/// <returns>path after link-resolution. This can be a non-existing file/directory.</returns>
		public static string ResolvePath(string path)
        {
            while (true)
            {
                try
                {
                    if (Path.GetExtension(path).ToLower() == ".lnk")
                    { }
                    else if (!File.Exists(path))
                    {
                        if (File.Exists(path + ".lnk"))
                            path += ".lnk";
                        else if (File.Exists(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)) + ".lnk"))
                            path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)) + ".lnk";
                        else break;
                    }
                    else break;

                    ShellShortcut link = new ShellShortcut(path);
                    LimeMsg.Debug("LimeLib ResolvePath: link: {0}", path);
                    path = link.Path;

                    if (path == "" || IsNetworkDrive(path) || IsLibrary(path))
                    {
                        // Prefer PIDL to unhandled path-formats
                        IntPtr pidl = link.PIDL;
                        path = String.Format(":{0}", pidl);
                        break;
                    }
                }
                catch
                {
                    return path;
                }
            }

            return path;
        }


        /// <summary>
        /// Returns true if the path in the string is a network drive.
        /// </summary>
        /// <param name="path">path to be checked</param>
        /// <returns>true if network drive, false otherwise</returns>
        public static bool IsNetworkDrive(string path)
        {
            return (path != null && path.Length > 3 && path.Substring(0, 2) == "\\\\" && path.IndexOf('\\', 3) < 0);
        }

        /// <summary>
        /// Returns true if the path in the string is a PIDL.
        /// </summary>
        /// <param name="path">path to be checked</param>
        /// <returns>true if PIDL, false otherwise</returns>
        public static bool IsPIDL(string path)
        {
            return (path != null && path.Length > 2 && path[0] == ':' && path[1] != ':');
        }

        /// <summary>
        /// Return the PIDL pointer from a PIDL path (:12345).
        /// </summary>
        /// <param name="path">PIDL path (:12345)</param>
        /// <returns>Pointer to the PIDL</returns>
        public static IntPtr GetPIDL(string path)
        {
            IntPtr ret = IntPtr.Zero;
            if (path != null && path.Length > 2 && path[0] == ':' && path[1] != ':')
            {
                int pidli = int.Parse(path.Substring(1));
                ret = (IntPtr)pidli;
            }
            return ret;
        }


        /// <summary>
        /// Free PIDL, if the path actually represents a PIDL
        /// </summary>
        /// <param name="path">PIDL path (e.g. ":12345")</param>
        /// <returns>true if PIDL, false otherwise</returns>
        public static bool FreePIDL(string path)
        {
            if (path != null && path.Length > 2 && path[0] == ':' && path[1] != ':')
            {
                LimeMsg.Debug("LimeLib FreePIDL: PIDL: {0}", path);
                int pidli = int.Parse(path.Substring(1));
                Win32.ILFree((IntPtr)pidli);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reserve a PIDL by cloning an existent PIDL and convert it to a path ':1234' format.
        /// </summary>
        /// <param name="pidl">Source PIDL (IntPtr) to be cloned</param>
        /// <returns>A PIDL path in ':1234' format</returns>
        public static string ReservePIDL(IntPtr pidl)
        {
            string ret = null;
            if (pidl != IntPtr.Zero)
            {
                var cpidl = PIDL.ILClone(pidl);
                ret = String.Format(":{0}", cpidl);
                LimeMsg.Debug("LimeLib ReservePIDL: PIDL: {0} --> {1}", pidl, ret);
            }
            return ret;
        }


        /// <summary>
        /// Returns true if the path in the string is a SSPD.
        /// </summary>
        /// <param name="path">path to be checked</param>
        /// <returns>true if SSPD, false otherwise</returns>
        public static bool IsSSPD(string path)
        {
            return (path != null && path.Length > 3 && path[0] == ':' && path[1] == ':');
        }


        /// <summary>
        /// Returns true if the path in the string is a Library folder.
        /// </summary>
        /// <param name="path">path to be checked</param>
        /// <returns>true if PIDL, false otherwise</returns>
        public static bool IsLibrary(string path)
        {
            return (path != null && Path.GetExtension(path).ToLower() == ".library-ms");
        }


        /// <summary>
        /// Copy all property values of same name from one object to another.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void CopyPropertyValues(object source, object destination)
        {
            var destProperties = destination.GetType().GetProperties();

            foreach (var sourceProperty in source.GetType().GetProperties())
            {
                foreach (var destProperty in destProperties)
                {
                    if (destProperty.Name == sourceProperty.Name
                        && destProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType)
                        && destProperty.CanWrite)
                    {
                        destProperty.SetValue(destination, sourceProperty.GetValue(source, new object[] { }), new object[] { });

                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Return true if the OS is Windows 8 or greater
        /// </summary>
        public static bool IsWindows8
        {
            get
            {
                OperatingSystem os = Environment.OSVersion;
                return (os.Version.Major > 6 || os.Version.Major == 6 && os.Version.Minor > 1);
            }
        }


		/// <summary>
		/// Get the type of media of a file from its extension.
		/// </summary>
		/// <param name="path">path or extension of the file</param>
		/// <returns>MediaType for the file extension</returns>
		public static MediaType GetMediaType(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) return MediaType.None;

			string ext = Path.GetExtension(path);
			if (string.IsNullOrWhiteSpace(ext)) return MediaType.None;

			// skip the '.'
			ext = ext.Substring(1).ToLower();

			foreach (var tst in LimeLib.VideoExtensions)
			{
				if (ext == tst) return MediaType.Video;
			}

			foreach (var tst in LimeLib.AudioExtensions)
			{
				if (ext == tst) return MediaType.Audio;
			}

			foreach (var tst in LimeLib.ImageExtensions)
			{
				if (ext == tst) return MediaType.Image;
			}

			foreach (var tst in LimeLib.DocumentExtensions)
			{
				if (ext == tst) return MediaType.Document;
			}

			return MediaType.None;
		}


		/// <summary>
		/// Parse a string representing element to add or remove from a source list.
		/// </summary>
		/// <param name="parseString">Comma-separated elements to add (prefix: +) or remove (prefix: -) from <paramref name="source"/> list</param>
		/// <param name="source">List to which the modification should be applied to</param>
		/// <returns>List resulting of the source list, modified by the <paramref name="parseString"/></returns>
		public static string[] DeltaListParse(string parseString, string [] source)
		{
			var ret = source.ToList();

            if (!string.IsNullOrEmpty(parseString))
            {
                var parse = parseString.ToLower().Split(',');
                for (int i = 0; i < parse.Length; i++)
                {
                    var ext = parse[i].Trim();
                    if (string.IsNullOrEmpty(ext)) throw new InvalidDataException();
                    var delta = ext[0];
                    bool remove = delta == '-';
                    if (remove || delta == '+')
                    {
                        ext = ext.Substring(1);
                        if (string.IsNullOrEmpty(ext)) throw new InvalidDataException();
                    }
                    int idx = ret.IndexOf(ext);
                    if (idx >= 0)
                    {
                        if (remove) ret.RemoveAt(idx);
                    }
                    else if (!remove)
                    {
                        ret.Add(ext);
                    }
                }
            }

			return ret.ToArray();
		}


		/// <summary>
		/// Create a string representing element to add or remove from a source list.
		/// </summary>
		/// <param name="destination">List after the modification</param>
		/// <param name="source">List before the modification</param>
		/// <returns>Comma-separated elements representing the modification to go from <paramref name="source"/> 
		/// to <paramref name="destination"/>, with prefix + (added) or - (removed)</returns>
		public static string DeltaListFrom(string[] destination, string[] source)
		{
			var ret = "";

			for (int i = 0; i < source.Length; i++)
			{
				var ext = source[i].Trim();
				if (!destination.Contains(ext))
				{
					ret += ",-" + ext;
				}
			}

			for (int i = 0; i < destination.Length; i++)
			{
				var ext = destination[i].Trim();
				if (! source.Contains(ext) )
				{
					ret += ",+" + ext;
				}
			}

			return ret.Trim(',');
		}

		/// <summary>
		/// Returns the first word in a list
		/// </summary>
		/// <param name="str">string containing a list</param>
		/// <param name="separator">list-separator</param>
		/// <returns></returns>
		public static string FirstWord(string str, string separator = ",", bool trim = true)
		{
			if (string.IsNullOrEmpty(str)) return "";
			var idx = str.IndexOf(separator);
			var ret = idx >= 0 ? str.Substring(0, idx) : str;
			if (trim) ret = ret.Trim();
			return ret;
		}




		/// <summary>
		/// Render an image of any type supported by Lime
		/// </summary>
		/// <param name="image">image of any type</param>
		public static ImageSource ImageSourceFrom(object image)
		{
			ImageSource ret;
			if (image is Uri uri)
			{
				ret = new BitmapImage(uri);
			}
			else if (image is string str)
			{
				ret = new BitmapImage(new System.Uri(str));
			}
			else if (image is BitmapImage btm)
			{
				ret = btm;
			}
			else if (image is LimePicture lpic)
			{
				ret = BitmapFromRaw(lpic.Raw);
			}
			else if (image is Image img)
			{
				ImageConverter converter = new ImageConverter();
				var vect = (byte[])converter.ConvertTo(img, typeof(byte[]));
				ret = BitmapFromRaw(vect);
			}
			else if (image is TagLib.IPicture ipic)
			{
				ret = BitmapFromRaw(ipic.Data?.ToArray());
			}
			else
			{
				ret = null;
			}

			return ret;
		}

		/// <summary>
		/// Build an image from Picture bytes
		/// </summary>
		/// <param name="imageData">Picture as array of bytes</param>
		/// <returns>Pictures as BitmapImage</returns>
		public static BitmapImage BitmapFromRaw(byte[] imageData)
		{
			if (imageData == null) return null;
			BitmapImage ret = null;

			try
			{
				if (imageData == null) return ret;
				var image = new BitmapImage();
				using (var mem = new MemoryStream(imageData))
				{
					mem.Position = 0;
					image.BeginInit();
					image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
					image.CacheOption = BitmapCacheOption.OnLoad;
					image.UriSource = null;
					image.StreamSource = mem;
					image.EndInit();
				}

				image.Freeze();
				ret = image;
			}
			catch
			{
				LimeMsg.Debug("LimeLib BitmapFromRaw: Failed");
			}

			return ret;
		}

		/// <summary>
		/// Resize and center the Bitmap image to fit a target size
		/// </summary>
		/// <param name="bmp">Bitmap image</param>
		/// <param name="width">target width</param>
		/// <param name="height">target height</param>
		/// <returns></returns>
		public static Bitmap BitmapResize(Bitmap bmp, int width, int height, bool allowSmallerWidth = true)
		{
			// Force DPI to normal (96 DPI = no rescale) to avoid ugly rescaling in WPF when image comes from format with DPI
			bmp.SetResolution(96, 96);

			if (bmp.Width != width || bmp.Height != height)
			{
				int x = bmp.Width;
				int y = bmp.Height;

				bool enlarged = false;

				// minimum is not lower than 1 fouth of total size
				int minx = width / 4;
				if (x > width)
				{
					y = y * width / x;
					x = width;
				}
				else if (x < minx)
				{
					y = y * minx / x;
					x = minx;
					enlarged = true;
				}

				int miny = height / 4;
				if (y > height)
				{
					x = x * height / y;
					y = height;
				}
				else if (y < miny && !enlarged)
				{
					x = x * miny / y;
					y = miny;
				}

				if (allowSmallerWidth && width > x)
				{
					width = x;
				}

				int offsetX = (width - x) / 2;
				int offsetY = (height - y) / 2;
				try
				{
					Bitmap ret = new Bitmap(width, height);
					using (Graphics g = Graphics.FromImage(ret))
					{
						g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
						g.DrawImage(bmp, offsetX, offsetY, x, y);
					}
					return ret;
				}
				catch
				{
					LimeMsg.Debug("BitmapResize: Image can't be resized");
				}
			}

			return bmp;
		}


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Memory Leak detection (Debugging Only)

#if DEBUG
		private static Dictionary<Type, List<WeakReference>> _LifeTrace = null;
#endif

        /// <summary>
        /// Enable instrumentation of an object to gloabbly trace the amount of reserved/freed objects.
        /// This is mainly designed to diagnoze memory leaks.
        /// Usage: Insert in object-constructors: <code>LifeTrace(this);</code>
        /// </summary>
        /// <param name="obj">this object life (reserved --> freed) will be traced</param>
        [Conditional("DEBUG")]
        public static void LifeTrace(object obj)
        {
#if DEBUG
			if (_LifeTrace == null)
			{
				// Enable LifeTrace
				_LifeTrace = new Dictionary<Type, List<WeakReference>>();
			}
			var type = obj.GetType();
            var wref = new WeakReference(obj);
            try
            {
                _LifeTrace[type].Add(wref);
            }
            catch
            {
                _LifeTrace.Add(type, new List<WeakReference> { wref });
            }

            //LimeMsg.Debug("LifeTrace: {0} : {1}", type.Name, _LifeTrace[type].Count);
#endif
		}

		/// <summary>
		/// Display status of objects observed with <see cref="LifeTrace(object)"/>, sorted by types.
		/// The first call to this function enables the object LifeTrace instrumentation. 
		/// Before this first call, requests to trace objects instances (i.e. every call to 
		/// <see cref="LifeTrace(object)"/>) will be ignored. 
		/// </summary>
		[Conditional("DEBUG")]
        public static void LifeCheck()
        {
#if DEBUG
            if (_LifeTrace == null)
            {
                // Enable LifeTrace
                _LifeTrace = new Dictionary<Type, List<WeakReference>>();
            }

            LimeMsg.Debug("LifeCheck: +++++++++++++++++++++++");
            GC.Collect();
            GC.WaitForPendingFinalizers();

            int tot_count = 0;
            int tot_freed = 0;
            foreach (var dict in _LifeTrace)
            {
                var type = dict.Key;
				int alive = 0;
				int freed = 0;
				int count = dict.Value.Count;
                while (alive < dict.Value.Count)
                {
                    if (dict.Value[alive].IsAlive)
                    {
                        alive++;
                    }
                    else
                    {
                        dict.Value.RemoveAt(alive);
						freed++;
					}
                }
                tot_count += alive;
                tot_freed += freed;
                LimeMsg.Debug("LifeCheck: {0} : {1} (Freed: {2})", type.Name, alive, freed);
            }

            LimeMsg.Debug("LifeCheck: Total : {0} (Freed: {1})", tot_count, tot_freed);
            LimeMsg.Debug("LifeCheck: -----------------------");
#endif
		}


		#endregion
	}
}
