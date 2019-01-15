﻿/* 
 * Credit: unknown
 * Source: https://stackoverflow.com/questions/17222204/get-default-jumbo-system-icon-based-on-file-extension
 * 
 *  13-06-2017 - Changed by Sebastien Mouy (Starwer)  
 *               Added BitmapSourceToBitmap, IImageListSize, and support of small and large icon sizes
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

public class IconManager
{
    // Constants that we need in the function call

    private const int SHGFI_ICON = 0x100;
    private const int SHGFI_LARGEICON = 0x0;
    private const int SHGFI_SMALLICON = 0x1;
    private const int WM_CLOSE = 0x0010;

    /// <summary>
    /// Define the size of a System Icon
    /// </summary>
    public enum IImageListSize : int
    {
        /// <summary>
        /// 32x32
        /// </summary>
        SHIL_LARGE = 0x0,

        /// <summary>
        /// 16x16
        /// </summary>
        SHIL_SMALL = 0x1,

        /// <summary>
        /// 48x48
        /// </summary>
        SHIL_EXTRALARGE = 0x2,

        /// <summary>
        /// GetSystemMetrics
        /// </summary>
        SHIL_SYSSMALL = 0x3,

        /// <summary>
        /// 256x256
        /// </summary>
        SHIL_JUMBO = 0x4
    }


    // This structure will contain information about the file

    public struct SHFILEINFO
    {
        // Handle to the icon representing the file

        public IntPtr hIcon;

        // Index of the icon within the image list

        public int iIcon;

        // Various attributes of the file

        public uint dwAttributes;

        // Path to the file

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        // File type

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    [DllImport("Kernel32.dll")]
    public static extern Boolean CloseHandle(IntPtr handle);

    private struct IMAGELISTDRAWPARAMS
    {
        public int cbSize;
        public IntPtr himl;
        public int i;
        public IntPtr hdcDst;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int xBitmap; // x offest from the upperleft of bitmap
        public int yBitmap; // y offset from the upperleft of bitmap
        public int rgbBk;
        public int rgbFg;
        public int fStyle;
        public int dwRop;
        public int fState;
        public int Frame;
        public int crEffect;
    }

    [DllImport("user32")]
    private static extern
    IntPtr SendMessage(
        IntPtr handle,
        int Msg,
        IntPtr wParam,
        IntPtr lParam
        );

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGEINFO
    {
        private readonly IntPtr hbmImage;
        private readonly IntPtr hbmMask;
        private readonly int Unused1;
        private readonly int Unused2;
        private readonly RECT rcImage;
    }

    #region Private ImageList COM Interop (XP)

    [ComImport]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //helpstring("Image List"),
    private interface IImageList
    {
        [PreserveSig]
        int Add(
            IntPtr hbmImage,
            IntPtr hbmMask,
            ref int pi);

        [PreserveSig]
        int ReplaceIcon(
            int i,
            IntPtr hicon,
            ref int pi);

        [PreserveSig]
        int SetOverlayImage(
            int iImage,
            int iOverlay);

        [PreserveSig]
        int Replace(
            int i,
            IntPtr hbmImage,
            IntPtr hbmMask);

        [PreserveSig]
        int AddMasked(
            IntPtr hbmImage,
            int crMask,
            ref int pi);

        [PreserveSig]
        int Draw(
            ref IMAGELISTDRAWPARAMS pimldp);

        [PreserveSig]
        int Remove(
            int i);

        [PreserveSig]
        int GetIcon(
            int i,
            int flags,
            ref IntPtr picon);

        [PreserveSig]
        int GetImageInfo(
            int i,
            ref IMAGEINFO pImageInfo);

        [PreserveSig]
        int Copy(
            int iDst,
            IImageList punkSrc,
            int iSrc,
            int uFlags);

        [PreserveSig]
        int Merge(
            int i1,
            IImageList punk2,
            int i2,
            int dx,
            int dy,
            ref Guid riid,
            ref IntPtr ppv);

        [PreserveSig]
        int Clone(
            ref Guid riid,
            ref IntPtr ppv);

        [PreserveSig]
        int GetImageRect(
            int i,
            ref RECT prc);

        [PreserveSig]
        int GetIconSize(
            ref int cx,
            ref int cy);

        [PreserveSig]
        int SetIconSize(
            int cx,
            int cy);

        [PreserveSig]
        int GetImageCount(
            ref int pi);

        [PreserveSig]
        int SetImageCount(
            int uNewCount);

        [PreserveSig]
        int SetBkColor(
            int clrBk,
            ref int pclr);

        [PreserveSig]
        int GetBkColor(
            ref int pclr);

        [PreserveSig]
        int BeginDrag(
            int iTrack,
            int dxHotspot,
            int dyHotspot);

        [PreserveSig]
        int EndDrag();

        [PreserveSig]
        int DragEnter(
            IntPtr hwndLock,
            int x,
            int y);

        [PreserveSig]
        int DragLeave(
            IntPtr hwndLock);

        [PreserveSig]
        int DragMove(
            int x,
            int y);

        [PreserveSig]
        int SetDragCursorImage(
            ref IImageList punk,
            int iDrag,
            int dxHotspot,
            int dyHotspot);

        [PreserveSig]
        int DragShowNolock(
            int fShow);

        [PreserveSig]
        int GetDragImage(
            ref POINT ppt,
            ref POINT pptHotspot,
            ref Guid riid,
            ref IntPtr ppv);

        [PreserveSig]
        int GetItemFlags(
            int i,
            ref int dwFlags);

        [PreserveSig]
        int GetOverlayImage(
            int iOverlay,
            ref int piIndex);
    };

    #endregion

    ///
    /// SHGetImageList is not exported correctly in XP.  See KB316931
    /// http://support.microsoft.com/default.aspx?scid=kb;EN-US;Q316931
    /// Apparently (and hopefully) ordinal 727 isn't going to change.
    ///
    [DllImport("shell32.dll", EntryPoint = "#727")]
    private static extern int SHGetImageList(
        IImageListSize iImageList,
        ref Guid riid,
        out IImageList ppv
        );

    // The signature of SHGetFileInfo (located in Shell32.dll)
    [DllImport("Shell32.dll")]
    public static extern int SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo,
                                            uint uFlags);

    [DllImport("Shell32.dll")]
    public static extern int SHGetFileInfo(IntPtr pszPath, uint dwFileAttributes, ref SHFILEINFO psfi,
                                            int cbFileInfo, uint uFlags);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, Int32 nFolder, ref IntPtr ppidl);

    [DllImport("user32")]
    public static extern int DestroyIcon(IntPtr hIcon);

    public struct pair
    {
        public Icon icon { get; set; }
        public IntPtr iconHandleToDestroy { set; get; }
    }

    public static int DestroyIcon2(IntPtr hIcon)
    {
        return DestroyIcon(hIcon);
    }

    private static BitmapSource IconSource(Icon ic)
    {
        var ic2 = Imaging.CreateBitmapSourceFromHIcon(ic.Handle,
                                                        Int32Rect.Empty,
                                                        BitmapSizeOptions.FromEmptyOptions());
        ic2.Freeze();
        return ic2;
    }

    public static BitmapSource IconPath(string FileName, bool small, bool checkDisk, bool addOverlay)
    {
        var shinfo = new SHFILEINFO();

        const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        const uint SHGFI_LINKOVERLAY = 0x000008000;
        uint flags;

        if (small)
            flags = SHGFI_ICON | SHGFI_SMALLICON;
        else
            flags = SHGFI_ICON | SHGFI_LARGEICON;

        if (!checkDisk)
            flags |= SHGFI_USEFILEATTRIBUTES;

        if (addOverlay)
            flags |= SHGFI_LINKOVERLAY;

        var res = SHGetFileInfo(FileName, 0, ref shinfo, Marshal.SizeOf(shinfo), flags);

        if (res == 0)
            throw (new FileNotFoundException());

        var myIcon = Icon.FromHandle(shinfo.hIcon);
        var bs = IconSource(myIcon);

        myIcon.Dispose();
        bs.Freeze(); // importantissimo se no fa memory leak
        DestroyIcon(shinfo.hIcon);
        CloseHandle(shinfo.hIcon);

        return bs;
    }

    public static BitmapSource GetFileIcon(string FileName, IImageListSize size, bool checkDisk)
    {
        var shinfo = new SHFILEINFO();
        const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        const uint SHGFI_SYSICONINDEX = 0x4000;
        const int FILE_ATTRIBUTE_NORMAL = 0x80;
        var flags = SHGFI_SYSICONINDEX;

        if (!checkDisk) // This does not seem to work. If I try it, a folder icon is always returned.
            flags |= SHGFI_USEFILEATTRIBUTES;

        var res = SHGetFileInfo(FileName, FILE_ATTRIBUTE_NORMAL, ref shinfo, Marshal.SizeOf(shinfo), flags);

        if (res == 0)
            throw (new FileNotFoundException());

        var iconIndex = shinfo.iIcon;

        // Get the System IImageList object from the Shell:
        var iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");

        IImageList iml;
        SHGetImageList(size, ref iidImageList, out iml);
        var hIcon = IntPtr.Zero;
        const int ILD_TRANSPARENT = 1;
        iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);

        var myIcon = Icon.FromHandle(hIcon);
        var bs = IconSource(myIcon);

        myIcon.Dispose();
        bs.Freeze(); // very important to avoid memory leak
        DestroyIcon(hIcon);
        SendMessage(hIcon, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

        return bs;
    }


    /// <summary>
    /// Convert a BitmapSource to Bitmap
    /// </summary>
    /// <param name="source">BitmapSource</param>
    /// <returns>Bitmap</returns>
    public static Bitmap BitmapSourceToBitmap(BitmapSource source)
    {
        Bitmap bmp = new Bitmap(
          source.PixelWidth,
          source.PixelHeight,
          PixelFormat.Format32bppPArgb);
        BitmapData data = bmp.LockBits(
          new Rectangle(System.Drawing.Point.Empty, bmp.Size),
          ImageLockMode.WriteOnly,
          PixelFormat.Format32bppPArgb);
        source.CopyPixels(
          Int32Rect.Empty,
          data.Scan0,
          data.Height * data.Stride,
          data.Stride);
        bmp.UnlockBits(data);
        return bmp;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        private readonly int _Left;
        private readonly int _Top;
        private readonly int _Right;
        private readonly int _Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }
}

