﻿using System.Collections.Generic;
using ShellDll;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
//using VistaBridge.Shell;
using System.Diagnostics;
using System.Linq;
using System.IO.Utils;
using System.Runtime.Versioning;

namespace System.IO
{
    public delegate bool FileCancelDelegate(ushort completePercent);
    public delegate bool CancelDelegate();

    [SupportedOSPlatform("windows")]
    public static class IOTools
    {
        #region Constants
        public static string IID_Desktop = "::{00021400-0000-0000-C000-000000000046}";
        public static string IID_UserFiles = "::{59031A47-3F72-44A7-89C5-5595FE6b30EE}";
        public static string IID_Public = "::{4336A54D-038B-4685-AB02-99BB52D3FB8B}";
        public static string IID_Library = "::{031E4825-7B94-4DC3-B131-E946B44C8DD5}";

        //private static readonly Lazy<Dictionary<Environment.SpecialFolder, ShellAPI.CSIDL>> _shellFolderLookupDic =
        //    new Lazy<Dictionary<Environment.SpecialFolder, ShellAPI.CSIDL>>(() => constructShellFolderLookupDic());
        //private static readonly Lazy<Dictionary<Environment.SpecialFolder, string>> _directoryFullNameLookupDic =
        //    new Lazy<Dictionary<Environment.SpecialFolder, string>>(() => constructDirectoryFullNameLookupDic());
        //private static readonly Lazy<Dictionary<string, KnownFolder>> _pathToKnownFolderLookupDic =
        //    new Lazy<Dictionary<string, KnownFolder>>(() => constructpathToKnownFolderLookupDic());

        private static Dictionary<string, KnownFolder> constructpathToKnownFolderLookupDic()
        {
            Dictionary<string, KnownFolder> dic = new Dictionary<string, KnownFolder>();

            foreach (var kfId in Enum.GetValues(typeof(KnownFolderIds)))
            {
                var category = EnumAttributeUtils<FolderCategoryAttribute, KnownFolderIds>.FindAttribute(kfId);
                if (category == null)
                    Debug.WriteLine(String.Format("Attribute {0} does not have FolderCategoryAttribute", kfId));
                else
                {
                    if (category.Category != KnownFolderCategory.Virtual)
                    {
                        KnownFolder kf = KnownFolder.FromKnownFolderId((KnownFolderIds)kfId);
                        string path = kf.Path;
                        if (!dic.ContainsKey(path))
                            dic.Add(path, kf);
                    }
                        
                }
            }

            //foreach (var kf in KnownFolder.GetKnownFolders())
            //    if (kf.Category != KnownFolderCategory.Virtual)
            //    {
            //        string path = kf.Path;
            //        if (path != null)
            //        {
            //            if (!dic.ContainsKey(path))
            //                dic.Add(path, kf);
            //        }
            //    }

            return dic;
        }


        private static Dictionary<Environment.SpecialFolder, string> constructDirectoryFullNameLookupDic()
        {
            Dictionary<Environment.SpecialFolder, string> retVal = new Dictionary<Environment.SpecialFolder, string>();
            foreach (var enumItem in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                Environment.SpecialFolder sf = (Environment.SpecialFolder)enumItem;
                try
                {
                    System.IO.DirectoryInfoEx di = new System.IO.DirectoryInfoEx(sf);
                    retVal.Add(sf, di.FullName);
                }
                catch
                {
                }
            }
            return retVal;
        }
        #region constructShellFolderLookupDic
        private static Dictionary<Environment.SpecialFolder, ShellAPI.CSIDL> constructShellFolderLookupDic()
        {
            Dictionary<Environment.SpecialFolder, ShellAPI.CSIDL> retVal = new Dictionary<Environment.SpecialFolder, ShellAPI.CSIDL>();
            
            retVal.Add(Environment.SpecialFolder.ApplicationData, ShellAPI.CSIDL.CSIDL_APPDATA);
            retVal.Add(Environment.SpecialFolder.CommonApplicationData, ShellAPI.CSIDL.CSIDL_COMMON_APPDATA);
            retVal.Add(Environment.SpecialFolder.Cookies, ShellAPI.CSIDL.CSIDL_COOKIES);
            retVal.Add(Environment.SpecialFolder.Desktop, ShellAPI.CSIDL.CSIDL_DESKTOP);
            retVal.Add(Environment.SpecialFolder.DesktopDirectory, ShellAPI.CSIDL.CSIDL_DESKTOPDIRECTORY);
            retVal.Add(Environment.SpecialFolder.Favorites, ShellAPI.CSIDL.CSIDL_FAVORITES);
            retVal.Add(Environment.SpecialFolder.History, ShellAPI.CSIDL.CSIDL_HISTORY);
            retVal.Add(Environment.SpecialFolder.InternetCache, ShellAPI.CSIDL.CSIDL_INTERNET_CACHE);
            retVal.Add(Environment.SpecialFolder.LocalApplicationData, ShellAPI.CSIDL.CSIDL_LOCAL_APPDATA);
            retVal.Add(Environment.SpecialFolder.MyComputer, ShellAPI.CSIDL.CSIDL_DRIVES);
            retVal.Add(Environment.SpecialFolder.MyDocuments, ShellAPI.CSIDL.CSIDL_MYDOCUMENTS);
            retVal.Add(Environment.SpecialFolder.MyMusic, ShellAPI.CSIDL.CSIDL_MYMUSIC);
            retVal.Add(Environment.SpecialFolder.MyVideos, ShellAPI.CSIDL.CSIDL_MYVIDEO);
            retVal.Add(Environment.SpecialFolder.MyPictures, ShellAPI.CSIDL.CSIDL_MYPICTURES);
            retVal.Add(Environment.SpecialFolder.NetworkShortcuts, ShellAPI.CSIDL.CSIDL_NETHOOD);
            retVal.Add(Environment.SpecialFolder.Fonts, ShellAPI.CSIDL.CSIDL_FONTS);
            retVal.Add(Environment.SpecialFolder.CommonStartMenu, ShellAPI.CSIDL.CSIDL_COMMON_STARTMENU);
            retVal.Add(Environment.SpecialFolder.CommonStartup, ShellAPI.CSIDL.CSIDL_COMMON_STARTUP);
            retVal.Add(Environment.SpecialFolder.CommonDesktopDirectory, ShellAPI.CSIDL.CSIDL_COMMON_DESKTOPDIRECTORY);
            retVal.Add(Environment.SpecialFolder.UserProfile, ShellAPI.CSIDL.CSIDL_PROFILE);
            retVal.Add(Environment.SpecialFolder.SystemX86, ShellAPI.CSIDL.CSIDL_SYSTEMX86);
            retVal.Add(Environment.SpecialFolder.ProgramFilesX86, ShellAPI.CSIDL.CSIDL_PROGRAM_FILESX86);
            retVal.Add(Environment.SpecialFolder.CommonPrograms, ShellAPI.CSIDL.CSIDL_COMMON_PROGRAMS);
            retVal.Add(Environment.SpecialFolder.CommonProgramFiles, ShellAPI.CSIDL.CSIDL_PROGRAM_FILES);
            retVal.Add(Environment.SpecialFolder.CommonProgramFilesX86, ShellAPI.CSIDL.CSIDL_PROGRAM_FILES_COMMONX86);
            retVal.Add(Environment.SpecialFolder.CommonTemplates, ShellAPI.CSIDL.CSIDL_COMMON_TEMPLATES);
            retVal.Add(Environment.SpecialFolder.CommonDocuments, ShellAPI.CSIDL.CSIDL_COMMON_DOCUMENTS);
            retVal.Add(Environment.SpecialFolder.CommonAdminTools, ShellAPI.CSIDL.CSIDL_COMMON_ADMINTOOLS);
            retVal.Add(Environment.SpecialFolder.AdminTools, ShellAPI.CSIDL.CSIDL_ADMINTOOLS);
            retVal.Add(Environment.SpecialFolder.CommonMusic, ShellAPI.CSIDL.CSIDL_COMMON_MUSIC);
            retVal.Add(Environment.SpecialFolder.CommonPictures, ShellAPI.CSIDL.CSIDL_COMMON_PICTURES);
            retVal.Add(Environment.SpecialFolder.CommonVideos, ShellAPI.CSIDL.CSIDL_COMMON_VIDEO);
            retVal.Add(Environment.SpecialFolder.Resources, ShellAPI.CSIDL.CSIDL_RESOURCES);
            retVal.Add(Environment.SpecialFolder.LocalizedResources, ShellAPI.CSIDL.CSIDL_RESOURCES_LOCALIZED);
            retVal.Add(Environment.SpecialFolder.CommonOemLinks, ShellAPI.CSIDL.CSIDL_COMMON_OEM_LINKS);
            retVal.Add(Environment.SpecialFolder.CDBurning, ShellAPI.CSIDL.CSIDL_CDBURN_AREA);

            //retVal.Add(Environment.SpecialFolder.Personal, ShellAPI.CSIDL.PERSONAL); //Personal = MyDocuments
            retVal.Add(Environment.SpecialFolder.ProgramFiles, ShellAPI.CSIDL.CSIDL_PROGRAM_FILES);
            retVal.Add(Environment.SpecialFolder.Programs, ShellAPI.CSIDL.CSIDL_PROGRAMS);
            retVal.Add(Environment.SpecialFolder.Recent, ShellAPI.CSIDL.CSIDL_RECENT);
            retVal.Add(Environment.SpecialFolder.SendTo, ShellAPI.CSIDL.CSIDL_SENDTO);
            retVal.Add(Environment.SpecialFolder.StartMenu, ShellAPI.CSIDL.CSIDL_STARTMENU);
            retVal.Add(Environment.SpecialFolder.Startup, ShellAPI.CSIDL.CSIDL_STARTUP);
            retVal.Add(Environment.SpecialFolder.System, ShellAPI.CSIDL.CSIDL_SYSTEM);
            retVal.Add(Environment.SpecialFolder.Templates, ShellAPI.CSIDL.CSIDL_TEMPLATES);
            retVal.Add(Environment.SpecialFolder.PrinterShortcuts, ShellAPI.CSIDL.CSIDL_PRINTERS);
            retVal.Add(Environment.SpecialFolder.Windows, ShellAPI.CSIDL.CSIDL_WINDOWS);
            return retVal;
        }
        #endregion

        #endregion

        #region Path routines

        /// <summary>
        /// Expand environment path to parasable path.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static string ExpandPath(string fullPath)
        {
            fullPath = Helper.RemoveSlash(fullPath);
            if (fullPath.StartsWith("%"))
                fullPath = Environment.ExpandEnvironmentVariables(fullPath);
            return fullPath;
        }

        /// <summary>
        /// Centralized method to return hash code.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static int GetHashCode(string fullPath)
        {
            return fullPath.ToLower().GetHashCode();
        }

        /// <summary>
        /// Centralized method to return hash code.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static int GetHashCode(FileSystemInfoEx item)
        {
            return GetHashCode(item.FullName);
        }

        /// <summary>
        /// Return whether PIDL match fileMask ( * and ? supported)
        /// </summary>
        /// <param name="pidl"></param>
        /// <param name="fileMask"></param>
        /// <returns></returns>
        public static bool MatchFileMask(PIDL pidl, string fileMask)
        {
            string path = FileSystemInfoEx.PIDLToPath(pidl);
            string name = PathEx.GetFileName(path);
            return MatchFileMask(name, fileMask);
        }

        private static string constructFileMaskRegexPattern(string fileMask, bool forceSlashCheck)
        {
            if (!forceSlashCheck)
            {
                return '^' +
                Regex.Escape(fileMask.Replace(".", "__DOT__")
                                .Replace("*", "__STAR__")
                                .Replace("?", "__QM__"))
                    .Replace("__DOT__", "[.]")
                    .Replace("__STAR__", ".*")
                    .Replace("__QM__", ".")
                + '$';
            }
            else
            {
                return '^' +
                 Regex.Escape(fileMask.Replace(".", "__DOT__")
                                 .Replace("\\", "__SLASH__")
                                 .Replace("**", "__DOUBLESTAR__")
                                 .Replace("*", "__STAR__")
                                 .Replace("#", "__VARIABLE__")
                                 .Replace("(", "__OPENQUOTE__")
                                 .Replace(")", "__CLOSEQUOTE__")
                                 .Replace("?", "__QM__"))
                     .Replace("__DOT__", "[.]")
                     .Replace("__DOUBLESTAR__", ".*")
                     .Replace("__STAR__", "[^\\\\]*")
                     .Replace("__SLASH__", "[\\\\]")
                     .Replace("__VARIABLE__", "?")
                     .Replace("__OPENQUOTE__", "(")
                     .Replace("__CLOSEQUOTE__", ")")
                     .Replace("__QM__", ".")
                 + '$';
            }
        }

        public static bool MatchFileMask(string fileName, string fileMask, bool forceSlashCheck, out Match match)
        {
            string pattern = constructFileMaskRegexPattern(fileMask, forceSlashCheck);
            match = new Regex(pattern, RegexOptions.IgnoreCase).Match(fileName);
            return match.Success;
        }

        //http://stackoverflow.com/questions/725341/c-file-mask
        /// <summary>
        /// Return whether filename match fileMask ( * and ? supported)
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileMask"></param>
        /// <returns></returns>
        public static bool MatchFileMask(string fileName, string fileMask, bool forceSlashCheck)
        {
            if (fileName.Equals(fileMask, StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (fileMask == "*.*" || fileMask == "*" && !forceSlashCheck)
                return true;

            string pattern = constructFileMaskRegexPattern(fileMask, forceSlashCheck);

            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(fileName);
        }

        public static bool MatchFileMask(string fileName, string fileMask)
        {
            return MatchFileMask(fileName, fileMask, false);
        }

        public static bool MatchFileMasks(string fileName, string fileMasks)
        {
            string[] fileMaskList = fileMasks.Split(new char[] { ',', ';' });
            foreach (string fileMask in fileMaskList)
                if (MatchFileMask(fileName, fileMask))
                    return true;
            return false;
        }


        /// <summary>
        /// Get relative path of a entry based on baseDirectory.
        /// e.g. C:\Temp\AbC\1.txt (entry), C:\Temp\ (baseDirectory) will return ABC\1.txt
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="baseDirectory"></param>
        /// <returns></returns>
        public static string GetRelativePath(FileSystemInfoEx entry, DirectoryInfoEx baseDirectory)
        {
            if (entry.FullName.IndexOf(baseDirectory.FullName, StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return entry.Name;
            }
            else return entry.FullName.Substring(baseDirectory.FullName.Length + 1);
        }

        /// <summary>
        /// Get relative path of a entry based on baseDirectory.
        /// e.g. C:\Temp\AbC\1.txt (entry), C:\Temp\ (baseDirectory) will return ABC\1.txt
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="baseDirectory"></param>
        /// <returns></returns>
        public static string GetRelativePath(string name, DirectoryInfoEx baseDirectory)
        {
            if (name.IndexOf(baseDirectory.FullName, StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                return PathEx.GetFileName(name);
            }
            else return name.Substring(baseDirectory.FullName.Length + 1);
        }

        public static Environment.SpecialFolder? CSIDLToShellFolder(ShellAPI.CSIDL csidl)
        {
            foreach (var val in Enum.GetValues(typeof(KnownFolderIds)))
            {
                var csidlAttributes = EnumAttributeUtils<CsidlAttribute, KnownFolderIds>.FindAllAttributes(val).ToArray();
                var spFolderAttributes = EnumAttributeUtils<SpecialFolderAttribute, KnownFolderIds>.FindAllAttributes(val).ToArray();

                for (int i = 0; i < csidlAttributes.Length; i++)
                {                    
                    if (csidlAttributes[i].CSIDL == csidl)
                        if (spFolderAttributes.Length >= csidlAttributes.Length)
                            return spFolderAttributes[i].SpecialFolder;
                        else if (spFolderAttributes.Length >= 0)
                            return spFolderAttributes[0].SpecialFolder;
                }               
            }
            return null;

            //var enumerator = _shellFolderLookupDic.Value.GetEnumerator();

            //while (enumerator.MoveNext())
            //    if (enumerator.Current.Value.Equals(csidl))
            //        return enumerator.Current.Key;

            //throw new ArgumentException("This CSIDL path not supported");
        }

        public static ShellAPI.CSIDL? ShellFolderToCSIDL(Environment.SpecialFolder spFolder)
        {
            foreach (var val in Enum.GetValues(typeof(KnownFolderIds)))
            {
                var csidlAttributes = EnumAttributeUtils<CsidlAttribute, KnownFolderIds>.FindAllAttributes(val).ToArray();
                var spFolderAttributes = EnumAttributeUtils<SpecialFolderAttribute, KnownFolderIds>.FindAllAttributes(val).ToArray();

                for (int i = 0; i < spFolderAttributes.Length; i++)
                {
                    if (spFolderAttributes[i].SpecialFolder == spFolder)
                        if (csidlAttributes.Length >= spFolderAttributes.Length)
                            return csidlAttributes[i].CSIDL;
                        else if (spFolderAttributes.Length >= 0)
                            return csidlAttributes[0].CSIDL;
                }
            }
            return null;

            //var enumerator = _shellFolderLookupDic.Value.GetEnumerator();

            //while (enumerator.MoveNext())
            //    if (enumerator.Current.Key.Equals(shellFolder))
            //        return enumerator.Current.Value;

            //throw new ArgumentException("This SpecialFolder path not supported");
        }

        //public static Environment.SpecialFolder? PathToSpecialFolder(string path)
        //{            
        //    var foundKey =
        //        _directoryFullNameLookupDic.Value
        //        .Where(kvp => kvp.Value.Equals(path, StringComparison.CurrentCultureIgnoreCase))
        //        .Select(kvp => kvp.Key);
        //    if (foundKey.Count() >= 1)
        //        return foundKey.First();
        //    else return null;
        //}

        //public static string SpecialFolderToPath(Environment.SpecialFolder shellFolder)
        //{
        //    if (_directoryFullNameLookupDic.Value.ContainsKey(shellFolder))
        //        return _directoryFullNameLookupDic.Value[shellFolder];

        //    throw new ArgumentException("This SpecialFolder path not supported");
        //}

        //public static KnownFolder PathToKnownFolder(string path)
        //{
        //    var dic = _pathToKnownFolderLookupDic.Value;
        //    if (dic.ContainsKey(path))
        //        return dic[path];
        //    return null;
        //}

        #endregion

        #region PIDL Array routines

        /// <summary>
        /// Return a PIDL List from one or more FileSystemInfoExs
        /// </summary>
        /// <param name="item"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        public static PIDL[] GetPIDL(FileSystemInfoEx[] items, bool relative)
        {
            List<PIDL> retVal = new List<PIDL>();
            foreach (FileSystemInfoEx fi in items)
                if (fi.Exists)
                    fi.RequestPIDL((pidl, relPidl) =>
                        {
                            retVal.Add(relative ? relPidl : pidl);
                        });
            return retVal.ToArray();
        }

        /// <summary>
        /// Return a PIDL List from one or more FileSystemInfoExs
        /// </summary>
        /// <param name="item"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        public static PIDL[] GetPIDL(FileSystemInfoEx item, bool relative)
        {
            return GetPIDL(new FileSystemInfoEx[] { item }, relative);
        }

        /// <summary>
        /// Free up a PIDL IntPtr List.
        /// </summary>
        /// <param name="pidls"></param>
        public static void FreePIDL(PIDL[] pidls)
        {
            foreach (PIDL pidl in pidls)
                pidl.Free();
        }


        /// <summary>
        /// Extract IntPtr for the specified item(s), the result pointer is still owned by the input PIDL, thus dont needed to be freed.
        /// </summary>
        public static IntPtr[] GetPIDLPtr(PIDL[] items)
        {
            List<IntPtr> retVal = new List<IntPtr>();
            foreach (PIDL pidl in items)
                retVal.Add(pidl.Ptr);
            return retVal.ToArray();
        }

        /// <summary>
        /// Extract IntPtr for the specified item(s), the result pointer is still owned by the input PIDL, thus dont needed to be freed.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static IntPtr[] GetPIDLPtr(PIDL item)
        {
            return GetPIDLPtr(new PIDL[] { item });
        }
        #endregion

        #region FileEx/DirectoryEx routines

        //0.12: IsLibraryItem
        /// <summary>
        /// Return whether if specific path is part of the library item (e.g. Picture, Musics)
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public static bool IsLibraryItem(string fullName)
        {
            return (fullName.StartsWith(IOTools.IID_Library) && fullName.EndsWith(".library-ms"));
        }

        //0.13: Added HasParent
        /// <summary>
        /// Return whether parent directory contain child directory.
        /// Aware Library, UserFiles and Public directory too.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static bool HasParent(FileSystemInfoEx child, DirectoryInfoEx parent)
        {
            if (parent == null)
            {
                //if (Debugger.IsAttached)
                //    Debugger.Break();
                return false;
            }

            //::{031E4825-7B94-4DC3-B131-E946B44C8DD5}\Music.library-ms
            if (parent.FullName.StartsWith(IOTools.IID_Library) && parent.FullName.EndsWith(".library-ms"))
            {
                //Reverse
                foreach (DirectoryInfoEx subDir in parent.GetDirectories())
                    if (subDir.Equals(child) || HasParent(child, subDir))
                        return true;
                return false;
            }
            else
            {
                if (child.FullName.StartsWith(parent.FullName.TrimEnd('\\') + "\\", StringComparison.InvariantCultureIgnoreCase))
                    return true;

                if (child.FullName.StartsWith(IID_UserFiles) || child.FullName.StartsWith(IID_Public))
                    return false;
                FileSystemInfoEx current = child.Parent;
                while (current != null && !parent.Equals(current))
                    current = current.Parent;
                return (current != null);
            }
        }

        //0.13: Added HasParent
        /// <summary>
        /// Return whether parent directory contain child directory.
        /// Aware UserFiles and Public directory too.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parentFullName"></param>
        /// <returns></returns>
        public static bool HasParent(FileSystemInfoEx child, string parentFullName)
        {
            if (child.FullName.StartsWith(parentFullName, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (parentFullName == DirectoryInfoEx.DesktopDirectory.FullName)
                return true;

            if ((child.FullName.StartsWith(IID_UserFiles) || child.FullName.StartsWith(IID_Public)))
                return false;
            FileSystemInfoEx current = child.Parent;
            while (current != null && parentFullName != current.FullName)
                current = current.Parent;
            return (current != null);
        }


        public static bool Exists(string path)
        {
            return new FileSystemInfoEx(path).Exists;
        }
        /// <summary>
        /// Check if the file / directory exists.
        /// </summary>
        public static bool Exists(string path, bool isDir)
        {
            try
            {
                DirectoryInfoEx dir = new DirectoryInfoEx(Path.GetDirectoryName(path));
                bool temp;
                if (dir.Contains(Path.GetFileName(path), out temp))
                    return temp == isDir;
                else return false;
            }
            catch
            {
                return false;
            }
        }

        //public static void Delete(string path)
        //{
        //    DirectoryInfoEx dir;
        //    IStorage tempParentStorage = null;
        //    IntPtr tempParentStoragePtr = IntPtr.Zero;

        //    try
        //    {
        //        dir = new DirectoryInfoEx(Path.GetDirectoryName(path));
        //        string name = Path.GetFileName(path);
        //        IOTools.getIStorage(dir, out tempParentStoragePtr, out tempParentStorage);

        //        tempParentStorage.DestroyElement(name);
        //    }
        //    finally
        //    {
        //        if (tempParentStorage != null)
        //        {
        //            Marshal.ReleaseComObject(tempParentStorage);
        //            Marshal.Release(tempParentStoragePtr);
        //        }
        //    }
        //}

        /// <summary>
        /// Move directory or file, take full path of source and dest as parameter.
        /// </summary>
        public static void Move(string source, string dest)
        {
            DirectoryInfoEx fromDir, toDir;

            fromDir = new DirectoryInfoEx(Path.GetDirectoryName(source));
            toDir = new DirectoryInfoEx(Path.GetDirectoryName(dest));
            if (fromDir.Equals(toDir))
            {
                Rename(source, Path.GetFileName(dest));
                return;
            }


            if (fromDir.Storage == null)
                throw new IOException("Source directory does not support IStorage");
            if (toDir.Storage == null)
                throw new IOException("Destination directory does not support IStorage");

            int hr = fromDir.Storage.MoveElementTo(Path.GetFileName(source), toDir.Storage,
                Path.GetFileName(dest), ShellAPI.STGMOVE.MOVE);

            if (hr != ShellAPI.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }

        public static bool IsZip(ShellAPI.SFGAO attribs)
        {
            return ((attribs & ShellAPI.SFGAO.FOLDER) != 0 && (attribs & ShellAPI.SFGAO.STREAM) != 0);
        }

        /// <summary>
        /// Rename a file or folder in an directory.
        /// </summary>
        public static void Rename(string source, string destName)
        {
            //DirectoryInfoEx srcDir = new DirectoryInfoEx(Path.GetDirectoryName(source));
            FileSystemInfoEx srcElement = new FileSystemInfoEx(source);
            string srcName = Path.GetFileName(source);

            if (!srcElement.Exists || srcElement.Parent == null)
                throw new IOException("Source not exists");

            //0.15: Fixed ShellFolder not freed.
            using (ShellFolder2 srcParentShellFolder = srcElement.Parent.ShellFolder)
            {
                if (srcParentShellFolder == null)
                    throw new IOException("Source directory does not support IShellFolder");

                int hr = srcElement.RequestRelativePIDL(srcRelPidl =>
                    {
                        IntPtr tmpPtr;
                        int retVal = srcParentShellFolder.SetNameOf(IntPtr.Zero, srcRelPidl.Ptr,
                            destName, ShellAPI.SHGNO.FORPARSING, out tmpPtr);

                        PIDL tmpPIDL = new PIDL(tmpPtr, false); //consume the IntPtr, and free it.
                        tmpPIDL.Free();

                        return retVal;
                    });

                if (hr != ShellAPI.S_OK)
                    Marshal.ThrowExceptionForHR(hr);
            }
        }

        public static bool IsCancelTriggered(CancelDelegate cancel)
        {
            return cancel != null && cancel();
        }

        public static bool IsCancelTriggered(FileCancelDelegate cancel, ushort completePercent)
        {
            return cancel != null && cancel(completePercent);
        }

        /// <summary>
        /// Copy directory or file, take full path of source and dest as parameter.
        /// </summary>
        public static void CopyFile(string source, string dest, FileCancelDelegate cancel)
        {
            using (FileStreamEx srcStream = FileEx.OpenRead(source))
            {
                byte[] buffer = new byte[Math.Min(1024 * 1024 * 32, srcStream.Length)];  //32MB
                int readCount;
                ushort completePercent = 0;
                long completeCount = 0;
                using (FileStreamEx destStream = FileEx.Create(dest))
                {
                    while ((readCount = srcStream.Read(buffer, 0, buffer.Length)) > 0 && !IsCancelTriggered(cancel, completePercent))
                    {
                        completeCount += readCount;
                        destStream.Write(buffer, 0, readCount);
                        completePercent = srcStream.Length == 0 ? (ushort)100 : (ushort)((float)completeCount / (float)srcStream.Length * 100.0);
                    }
                    destStream.Flush();
                    destStream.Close();
                }
                srcStream.Close();
            }
        }

        /// <summary>
        /// Copy directory or file, take full path of source and dest as parameter.
        /// </summary>
        public static void Copy(string source, string dest)
        {
            DirectoryInfoEx fromDir, toDir;

            fromDir = new DirectoryInfoEx(PathEx.GetDirectoryName(source));
            toDir = new DirectoryInfoEx(PathEx.GetDirectoryName(dest));


            if (fromDir.Storage == null)
                throw new IOException("Source directory does not support IStorage");
            if (toDir.Storage == null)
                throw new IOException("Destination directory does not support IStorage");


            int hr = fromDir.Storage.MoveElementTo(PathEx.GetFileName(source), toDir.Storage,
                Path.GetFileName(dest), ShellAPI.STGMOVE.COPY);

            if (hr != ShellAPI.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }

        #endregion

        #region Obtain Shell interface (ShellAPI)
        /// <summary>
        /// Take a directory and return the IStorage PTr interface.
        /// </summary>
        internal static bool getIStorage(DirectoryInfoEx dir, out IntPtr storagePtr)
        {
            //0.19 Fixed ArgumentException when getting storage of C:\{User}\Desktop.
            if (dir.FullName.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
                dir = DirectoryInfoEx.DesktopDirectory;

            IntPtr storePtr = IntPtr.Zero;
            //0.15 : Fixed PIDL not freed correctly.      
            try
            {
                return dir.RequestRelativePIDL(dirPIDLRel =>
                    {

                        int hr;
                        if (dirPIDLRel.Size == 0)
                        {
                            IntPtr pidlLast = IntPtr.Zero;
                            hr = ShellAPI.SHBindToParent(dirPIDLRel.Ptr, ShellAPI.IID_IStorage,
                                out storePtr, ref pidlLast);
                        }
                        else
                            //0.15: Fixed ShellFolder not freed correctly.
                            using (ShellFolder2 dirParentShellFolder = dir.Parent.ShellFolder)
                                if (dirParentShellFolder != null)
                                    hr = dirParentShellFolder.BindToStorage(
                                    dirPIDLRel.Ptr, IntPtr.Zero, ref ShellAPI.IID_IStorage,
                                    out storePtr);
                                else
                                {
                                    storePtr = IntPtr.Zero;
                                    return false;
                                }

                        if ((hr != ShellAPI.S_OK))
                        {
                            storePtr = IntPtr.Zero;
                            Marshal.ThrowExceptionForHR(hr);
                            return false;
                        }

                        return true;
                    });
            }
            finally
            {
                storagePtr = storePtr;
            }
        }
        /// <summary>
        /// Take a directory and return the IStorage PTr interface.
        /// </summary>
        internal static bool getIStorage(DirectoryInfoEx dir, out IntPtr storagePtr, out IStorage storage)
        {
            bool retVal = getIStorage(dir, out storagePtr);
            storage = null;
            if (retVal)
                storage = (IStorage)Marshal.GetTypedObjectForIUnknown(storagePtr, typeof(IStorage));
            return retVal;
        }

        internal static bool getIStorage(DirectoryInfoEx dir, out Storage storage)
        {
            IntPtr storagePtr;
            bool retVal = getIStorage(dir, out storagePtr);
            storage = null;
            if (getIStorage(dir, out storagePtr))
                storage = new Storage(storagePtr);
            return retVal;
        }

        /// <summary>
        /// return STATSTG info of a file.
        /// </summary>
        internal static bool getFileStat(IStorage parentStorage, string filename, out ShellAPI.STATSTG statstg)
        {
            IntPtr streamPtr = IntPtr.Zero;
            IStream stream = null;
            statstg = new ShellAPI.STATSTG();

            try
            {
                if (parentStorage.OpenStream(
                            filename,
                            IntPtr.Zero,
                            ShellAPI.STGM.READ,
                            0,
                            out streamPtr) == ShellAPI.S_OK)
                {
                    stream = (IStream)Marshal.GetTypedObjectForIUnknown(streamPtr, typeof(IStream));
                    stream.Stat(out statstg, ShellAPI.STATFLAG.DEFAULT);
                    return true;
                }
            }
            finally
            {
                if (stream != null)
                {
                    Marshal.ReleaseComObject(stream);
                    stream = null;
                }

                if (streamPtr != IntPtr.Zero)
                {
                    Marshal.Release(streamPtr);
                    streamPtr = IntPtr.Zero;
                }

            }
            return false;
        }

        /// <summary>
        /// Open a file stream, used by FileStreamEx
        /// </summary>
        internal static void openStream(IStorage parentStorage, string filename, ref FileMode mode, ref FileAccess access, out IntPtr streamPtr, out IStream stream)
        {
            ShellAPI.STGM grfmode = ShellAPI.STGM.SHARE_DENY_WRITE;

            switch (access)
            {
                case FileAccess.ReadWrite:
                    grfmode |= ShellAPI.STGM.READWRITE;
                    break;

                case FileAccess.Write:
                    grfmode |= ShellAPI.STGM.WRITE;
                    break;
            }

            switch (mode)
            {
                case FileMode.Create:
                    if (FileEx.Exists(filename))
                        grfmode |= ShellAPI.STGM.CREATE;
                    break;
                case FileMode.CreateNew:
                    grfmode |= ShellAPI.STGM.CREATE;
                    break;
            }

            if (parentStorage != null)
            {
                if (parentStorage.OpenStream(
                        filename,
                        IntPtr.Zero,
                        grfmode,
                        0,
                        out streamPtr) == ShellAPI.S_OK)
                {
                    stream = (IStream)Marshal.GetTypedObjectForIUnknown(streamPtr, typeof(IStream));
                }
                else if (access != FileAccess.Read)
                {
                    //Create file if not exists
                    if (parentStorage.CreateStream(
                        filename, ShellAPI.STGM.WRITE, 0, 0, out streamPtr) == ShellAPI.S_OK)
                    {
                        stream = (IStream)Marshal.GetTypedObjectForIUnknown(streamPtr, typeof(IStream));
                    }
                    else
                        throw new IOException(String.Format("Can't open stream: {0}", filename));
                }
                else
                    throw new IOException(String.Format("Can't open stream: {0}", filename));
            }
            else
                throw new IOException(String.Format("Can't open stream: {0}", filename));
        }

        #endregion

        public static Stream LoadEmbeddedResource(Reflection.Assembly thisAsm, string assemblyName, string resourceName)
        {
            string name = assemblyName + "." + resourceName;
            if (new List<string>(thisAsm.GetManifestResourceNames()).IndexOf(name) != -1)
                return thisAsm.GetManifestResourceStream(name);
            else return null;
        }
    }
}
