/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     04-04-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System.Runtime.Versioning;
using TagLib;
using ZeroFormatter;

namespace Lime
{

    /// <summary>
    /// Provide a Serializable Picture representation compatible with TagLib#
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ZeroFormattable]
    public class LimePicture : IPicture
    {
        // --------------------------------------------------------------------------------------------------
        #region IPicture Implementation

        /// <summary>
        ///    Gets and sets the mime-type of the picture data
        ///    stored in the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the mime-type
        ///    of the picture data stored in the current instance.
        /// </value>
        [Index(0)]
        public virtual string MimeType { get; set; }

        /// <summary>
        ///    Gets and sets the type of content visible in the picture
        ///    stored in the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="PictureType" /> containing the type of
        ///    content visible in the picture stored in the current
        ///    instance.
        /// </value>
        [Index(1)]
        public virtual PictureType Type { get; set; }


        /// <summary>
        ///    Gets and sets a filename of the picture stored in the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the filename,
        ///    with its extension, of the picture stored in the current 
        ///    instance.
        /// </value>
        [Index(2)]
        public virtual string Filename { get; set; }


        /// <summary>
        ///    Gets and sets a description of the picture stored in the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing a description
        ///    of the picture stored in the current instance.
        /// </value>
        [Index(3)]
        public virtual string Description { get; set; }


        /// <summary>
        /// Gets or sets the raw picture data 
        /// </summary>
        [Index(4)]
        public virtual byte[] Raw { get; set; }


        /// <summary>
        ///    Gets and sets the picture data stored in the current
        ///    instance.
        /// </summary>
        /// <value>
        ///    A <see cref="ByteVector" /> object containing the picture
        ///    data stored in the current instance.
        /// </value>
        [IgnoreFormat]
        public ByteVector Data
        {
            get { return new ByteVector(Raw); }
            set { Raw = value?.Data; }
        }

        #endregion

        // --------------------------------------------------------------------------------------------------
        #region ctors

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public LimePicture()
        {
            LimeLib.LifeTrace(this);
        }

        /// <summary>
        /// Convert a TagLib IPicture to a LimePicture
        /// </summary>
        /// <param name="pic">IPicture source to copy</param>
        public LimePicture(IPicture pic)
        {
            LimeLib.CopyPropertyValues(pic, this);
        }

        #endregion
    }
}
