/* Extracted from ShellLib
 * Credit: Arik Poznanski
 * Source: http://www.codeproject.com/Articles/3590/C-does-Shell-Part
 * 
 * This article, along with any associated source code and files, is licensed under The Microsoft Public License (Ms-PL)
 * 
*/

namespace ShellBasics
{
    public class ShellNameMapping
    {
        private string destinationPath;
        private string renamedDestinationPath;

        public ShellNameMapping(string OldPath, string NewPath)
        {
            destinationPath = OldPath;
            renamedDestinationPath = NewPath;
        }

        public string DestinationPath
        {
            get { return destinationPath; }
        }

        public string RenamedDestinationPath
        {
            get { return renamedDestinationPath; }
        }
    }
}
