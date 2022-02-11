/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     13-06-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using SharpShell.Attributes;
using SharpShell.SharpThumbnailHandler;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Media.Imaging;

namespace ThumbLime
{
    /// <summary>
    /// Base class to be use as a factory for the each ThumbnailHandler Extension class
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ComVisible(true)]
    public abstract class LimeThumbnailHandler : SharpThumbnailHandler
    {
        /// <summary>
        /// Represents the extension supported by the derived class
        /// </summary>
        private readonly string extension = null;

        /// <summary>
        /// Factory for the derived class
        /// </summary>
        public LimeThumbnailHandler()
        {
            // Retrieve the supported extension from the derived Class Attribute: COMServerAssociationAttribute
            if (extension == null)
            {
                object[] attrs = this.GetType().GetCustomAttributes(true);
                foreach (object attr in attrs)
                {
					if (attr is COMServerAssociationAttribute comAttr)
					{
						extension = comAttr.Associations.First();
						break;
					}
				}
            }

        }


        /// <summary>
        /// Return the icon associated with the current extension
        /// </summary>
        /// <param name="width">Size of the icon</param>
        /// <returns>The extension icon, as Bitmap</returns>
        private Bitmap ExtensionSystemIcon(uint width)
        {
            IconManager.IImageListSize size =
                width <= 16 ? IconManager.IImageListSize.SHIL_SMALL :
                width <= 32 ? IconManager.IImageListSize.SHIL_LARGE :
                width <= 48 ? IconManager.IImageListSize.SHIL_EXTRALARGE :
                IconManager.IImageListSize.SHIL_JUMBO;

            BitmapSource icon = IconManager.GetFileIcon(extension, size, false);
            return IconManager.BitmapSourceToBitmap(icon);
        }

        private static Bitmap BytesToBitmap(byte[] binData)
        {
            // Get Image from bytes in memory
            var stream = new MemoryStream(binData);
            return new Bitmap(stream);
        }

        // TODO: Use a library function instead
        /// <summary>
        /// Retrieve an image as Bitmap from an URL/URI
        /// </summary>
        /// <param name="uri">URI representing an image</param>
        /// <returns>the bitmap object representing the image</returns>
        private static Bitmap URItoBitmap(string uri)
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
                ret = BytesToBitmap(binData);
            }

            return ret;
        }


        /// <summary>
        /// Called back when a thumbnail must be retrieved from a file in SelectedItemStream
        /// </summary>
        /// <param name="width">Size of the icon</param>
        /// <returns>The thumbnail icon, as Bitmap</returns>
        protected override Bitmap GetThumbnailImage(uint width)
        {
            Bitmap bmp = null;
            TagLib.File tagf = null;

            //  Create a stream reader for the selected item stream.
            try
            {
                // Get tags
                var vfs = new StreamFileAbstraction(extension, SelectedItemStream);
                tagf = TagLib.File.Create(vfs);

                // Retrieve FrontCover picture
                var pics = tagf.Tag.Pictures;
                var pic = pics?[0];
                if(pic != null)
                    bmp = BytesToBitmap(pic.Data.Data);


                // Convert URI to bitmap
                // TODO: call external proc Lime.LimeTag.URItoBitmap
                //string posterUri = tagf.Tag.Performers?[0];
                //bmp = URItoBitmap(posterUri);

            }
            catch
            {
                //  Log the exception and return null for failure.
                Log("ThumbLime: Poster not found when opening the file-type " + extension);
            }


            if (bmp == null) bmp = ExtensionSystemIcon(width);
            if (bmp == null)
            {
                LogError("ThumbLime: Icon not found for file-type " + extension);
                return null;
            }

            // Resize/center the image to adapt to the thumbnail size

            Bitmap ret;

            int x = bmp.Width;
            int y = bmp.Height;
            int xoffset = 0;
            int yoffset = 0;

            if (x > y)
            {
                y = (y * (int)width) / x;
                x = (int)width;
                yoffset = (x - y) / 2;
            }
            else
            {
                x = (x * (int)width) / y;
                y = (int)width;
                xoffset = (y - x) / 2;
            }

            ret = new Bitmap((int)width, (int)width, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(ret))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, xoffset, yoffset, x, y);
            }


#if DEBUG
            // Add metadata infos on top of he bitmap
            if (tagf != null)
            {
                string debug = tagf.Tag.Title + "\n\r" + String.Format("w:{0} x:{1} y:{2}", width, x, y);
                using (Graphics flagGraphics = Graphics.FromImage(ret))
                {
                    Font drawFont = new Font("Arial", 12);
                    SolidBrush drawBrush = new SolidBrush(Color.Red);
                    StringFormat drawFormat = new StringFormat();
                    flagGraphics.DrawString(debug, drawFont, drawBrush, 1.0F, 30.0F, drawFormat);
                    drawFont.Dispose();
                    drawBrush.Dispose();
                    flagGraphics.Dispose();
                }
            }
#endif
            return ret;


        }

    }
}
