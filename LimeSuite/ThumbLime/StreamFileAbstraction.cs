/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     13-06-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using System.IO;

namespace ThumbLime
{
    /// <summary>
    /// Enable to make a TagLib# file abstraction from a Stream
    /// </summary>
    public class StreamFileAbstraction : TagLib.File.IFileAbstraction
    {
        public StreamFileAbstraction(string name, Stream stream)
        {
            Name = name;
            ReadStream = stream;
        }

        public string Name { get; private set; }

        public Stream ReadStream { get; }

        public Stream WriteStream
        {
            get { return null; }
        }

        public void CloseStream(Stream stream)
        {
            stream.Close();
        }
    }
}
