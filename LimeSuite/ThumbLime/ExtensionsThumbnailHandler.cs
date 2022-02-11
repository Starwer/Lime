/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     13-06-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using SharpShell.Attributes;
using System.Runtime.Versioning;

namespace ThumbLime
{
    /// <summary>
    /// Create all the extension classes supported by ThumbLime
    /// </summary>
    [SupportedOSPlatform("windows")]
    class ExtensionsThumbnailHandler
    {
        [COMServerAssociation(AssociationType.ClassOfExtension, ".avi")]
        public class AviThumbnailHandler : LimeThumbnailHandler { }

        [COMServerAssociation(AssociationType.ClassOfExtension, ".mkv")]
        public class MkvThumbnailHandler : LimeThumbnailHandler { }

        [COMServerAssociation(AssociationType.ClassOfExtension, ".m4v")]
        public class M4vThumbnailHandler : LimeThumbnailHandler { }

        [COMServerAssociation(AssociationType.ClassOfExtension, ".wmv")]
        public class WmvThumbnailHandler : LimeThumbnailHandler { }

        [COMServerAssociation(AssociationType.ClassOfExtension, ".mp4")]
        public class Mp4ThumbnailHandler : LimeThumbnailHandler { }

    }
}
