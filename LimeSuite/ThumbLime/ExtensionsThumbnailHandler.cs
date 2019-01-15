/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     13-06-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using SharpShell.Attributes;

namespace ThumbLime
{
    /// <summary>
    /// Create all the extension classes supported by ThumbLime
    /// </summary>
    class ExtensionsThumbnailHandler
    {
        [COMServerAssociation(AssociationType.FileExtension, ".avi")]
        public class AviThumbnailHandler : LimeThumbnailHandler { }

        [COMServerAssociation(AssociationType.FileExtension, ".mkv")]
        public class MkvThumbnailHandler : LimeThumbnailHandler { }

        [COMServerAssociation(AssociationType.FileExtension, ".m4v")]
        public class M4vThumbnailHandler : LimeThumbnailHandler { }

        [COMServerAssociation(AssociationType.FileExtension, ".wmv")]
        public class WmvThumbnailHandler : LimeThumbnailHandler { }

        [COMServerAssociation(AssociationType.FileExtension, ".mp4")]
        public class Mp4ThumbnailHandler : LimeThumbnailHandler { }

    }
}
