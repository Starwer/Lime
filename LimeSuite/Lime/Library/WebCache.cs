/**************************************************************************
* Author:       Copied from Scott McMaster, 
*               adapted by Sebastien Mouy
* Contact:      starwer@laposte.net
* Website:      http://www.codeproject.com/Articles/13179/WebCacheTool-Manipulate-the-IE-Browser-Cache-From
* Creation:     05-11-2015
**************************************************************************/


using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Lime
{
    public class WebCache
    {
        /// <summary>
        /// Contains information about an entry in the Internet cache.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct INTERNET_CACHE_ENTRY_INFO
        {
            public UInt32 dwStructSize;
            public string lpszSourceUrlName;
            public string lpszLocalFileName;
            public UInt32 CacheEntryType;
            public UInt32 dwUseCount;
            public UInt32 dwHitRate;
            public UInt32 dwSizeLow;
            public UInt32 dwSizeHigh;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastModifiedTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ExpireTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastSyncTime;
            public IntPtr lpHeaderInfo;
            public UInt32 dwHeaderInfoSize;
            public string lpszFileExtension;
            public UInt32 dwExemptDelta;
        };


        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool
                GetUrlCacheEntryInfo(string lpszUrlName,
                IntPtr lpCacheEntryInfo,
                out UInt32 lpdwCacheEntryInfoBufferSize);


        /// <summary>
        /// Retrieves information about a cache entry.
        /// </summary>
        /// <param name="url">URL to be retrieved</param>
        /// <returns>information about an entry in the Internet cache</returns>
        public static INTERNET_CACHE_ENTRY_INFO GetUrlCacheEntryInfo(string url)
        {
            IntPtr buffer = IntPtr.Zero;
            UInt32 structSize;
            bool apiResult = GetUrlCacheEntryInfo(url, buffer, out structSize);

            try
            {
                buffer = Marshal.AllocHGlobal((int)structSize);
                apiResult = GetUrlCacheEntryInfo(url, buffer, out structSize);
                if (apiResult == true)
                {
                    return (INTERNET_CACHE_ENTRY_INFO)
                            Marshal.PtrToStructure(buffer,
                            typeof(INTERNET_CACHE_ENTRY_INFO));
                }

            }
            finally
            {
                if (buffer.ToInt32() > 0)
                {
                    try { Marshal.FreeHGlobal(buffer); }
                    catch { }
                }
            }

            INTERNET_CACHE_ENTRY_INFO ret = new INTERNET_CACHE_ENTRY_INFO();
            ret.lpszLocalFileName = null;
            return ret;
        }


        /// <summary>
        /// Return the local (cached) file representing the given URL.
        /// </summary>
        /// <param name="url">URL to be retrieved</param>
        /// <returns>he path to the local file, or null if no local file.</returns>
        public static string GetUrlCacheEntryFile(string url)
        {
            INTERNET_CACHE_ENTRY_INFO info = GetUrlCacheEntryInfo(url);
            return info.lpszLocalFileName;
        }

    }
}
