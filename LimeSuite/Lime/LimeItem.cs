/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using MSjogren.Samples.ShellLink;
using System.Windows;
using System.Windows.Media;
using Peter;
using System.Collections.ObjectModel;
using ShellDll;
using Trinet.Core.IO.Ntfs;
using System.ComponentModel;
using ShellLib;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Text.RegularExpressions;

namespace Lime
{

    // --------------------------------------------------------------------------------------------------
    #region Types

    /// <summary>
    /// Deletage to enable callbacks on item change
    /// </summary>
    /// <param name="parent">Parent Item</param>
    /// <param name="item">Item which has changed</param>
    /// <param name="index">Index of the item in its parent</param>
    public delegate void LimeItemChangeDelegate(LimeItem parent, LimeItem item, int index);


    /// <summary>
    /// Define the basic attribues that the LimeItems should share in the same tree
    /// </summary>
    public class LimeItemTreeAttibute
    {
        /// <summary>
        /// Size, in pixel, of the big icon property: ImgSrcBig (only used if <see cref="ImgSrcEnableBigSize"/> = true)
        /// </summary>
        public uint ImgSrcBigSize = 256;

        /// <summary>
        /// Size, in pixel, of the small icon property: ImgSrcSmall
        /// </summary>
        public uint ImgSrcSmallSize = 32;

        /// <summary>
        /// Enable to round-off the size of the icons to get sharper rendering (but less accurate sizing)
        /// </summary>
        public bool ImgSrcSizeRoundOff = true;

        /// <summary>
        /// Enable to use big size icons
        /// </summary>
        public bool ImgSrcEnableBigSize = true;

		/// <summary>
		/// Use Cover for image source
		/// </summary>
		public bool ImgSrcUseCover = true;

		/// <summary>
		/// Enable the TaskSwitchers to retrieve the WUP Application
		/// </summary>
		public bool TaskSwitcherShowApp = true;

        /// <summary>
        /// Enable to create a Shell link to a link/URL
        /// </summary>
        public bool AllowLinkOfLink = false;

        /// <summary>
        /// Register the Tree Root UIElement (if any)
        /// </summary>
        public UIElement UIElement = null;

        /// <summary>
        /// Default File template 
        /// </summary>
        public LimeItem DefaultFile = null;

        /// <summary>
        /// Default Directory template
        /// </summary>
        public LimeItem DefaultDir = null;

        /// <summary>
        /// Regulate the the async loading
        /// </summary>
        public SemaphoreSlim Mutex = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Set the current Queue
        /// </summary>
        public BlockingCollection<LimeItem> Queue;

        /// <summary>
        /// Event triggered when there is change in the directory.
        /// </summary>
        public event LimeItemChangeDelegate ChildrenChanged;

        /// <summary>
        /// Trigger a ChildrenChanged event
        /// </summary>
        /// <param name="parent">Parent Item</param>
        /// <param name="item">Item which has changed</param>
        /// <param name="index">Index of the item in its parent</param>
        public void OnChildrenChanged(LimeItem parent, LimeItem item, int index)
        {
            ChildrenChanged?.Invoke(parent, item, index);
        }

    }


    #endregion


    // --------------------------------------------------------------------------------------------------
    /// <summary>
    /// Define a View-Model Item in LIME (Light Integrated Multimedia Environment).
    /// An item can represent anything that can be manipulated by Lime, like file, folder, task, configuration items, shortcut...
    /// Items are organized as a tree, so these can have a parent node and child nodes.
    /// </summary>
    public class LimeItem : INotifyPropertyChangedWeak, IDataObjectCompatible
    {

		// --------------------------------------------------------------------------------------------------
		#region INotifyPropertyChangedWeak implementation

		// Boilerplate code for INotifyPropertyChanged

		protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;


		// INotifyPropertyChangedWeak implementation
		public event EventHandler<PropertyChangedEventArgs> PropertyChangedWeak
		{
			add { WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.AddHandler(this, "PropertyChanged", value); }
			remove { WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.RemoveHandler(this, "PropertyChanged", value); }
		}

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Types & Constants

		private const string IniLanguageSection = "Translator";

        /// <summary>
        /// Use Flags to store boolean properties. 
        /// This saves some space (one byte in total instead of one byte per property).
        /// </summary>
        [Flags]
        private enum LimeItemFlags : ushort
        {
            Directory = 0x01,
            Panel = 0x02,
            Task = 0x04,
            IsLoading = 0x08,
            IsSaving = 0x10,
            IsPanelVisible = 0x20,
			IsTaskThumbVisible = 0x40,
			IsUseCover = 0x80,
			IsSizeRoundOff = 0x100
		}


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Properties

		/// <summary>
		/// Basic attribues that the LimeItems should share in the same tree
		/// </summary>
		public LimeItemTreeAttibute Tree;

        /// <summary>
        /// Boolean Properties default values
        /// </summary>
        private LimeItemFlags Flags;

        /// <summary>
        /// Name of the item
        /// </summary>
        public string Name
        {
            get { return _Name ?? ""; }
            set
            {
                if (value != _Name)
                {
                    _Name = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _Name;

        /// <summary>
        /// Description of the item
        /// </summary>
        public string Description
        {
            get { return _Description; }
            set
            {
                if (value != _Description)
                {
                    _Description = value;
                    OnPropertyChanged();
					BuildToolTip();
                }
            }
        }
        private string _Description;


        /// <summary>
        /// Path of the item
        /// </summary>
        public string Path
        {
            get
            {
                return _Path;
            }

            private set
            {
                if (_Path == value) return;

                string desc = "";

                _Path = value;
                if (_Path == null)
                {
                    OnPropertyChanged();
                    return;
                }

                if (string.IsNullOrEmpty(Name))
                {
                    this.Name = System.IO.Path.GetFileNameWithoutExtension(_Path);
                }

                desc = System.IO.Path.GetFileName(_Path);

                if (this.FileAttributes == 0)
                {
                    try
                    {
                        if (!File.Exists(_Path) && !System.IO.Directory.Exists(_Path))
                        {
                            this.FileAttributes = FileAttributes.ReadOnly;
                            return;
                        }
                        this.FileAttributes = File.GetAttributes(_Path);
                    }
                    catch
                    {
                        LimeMsg.Warning("UnableFileDir", _Path);
                    }
                }


                if ((this.FileAttributes & (FileAttributes.Directory | FileAttributes.Device)) != 0 || LimeLib.IsLibrary(_Path))
                {
                    // Directory
                    this.Directory = true;
                    this.FileAttributes &= ~FileAttributes.ReadOnly;

                    if (LimeLib.IsNetworkDrive(_Path))
                    {
                        // unhandled path-format: use PIDL instead
                        PIDL pidl = DirectoryInfoEx.PathtoPIDL(_Path);
                        LimeMsg.Debug("LimeItem path: PIDL: {0} --> :{1}", _Path, pidl.Ptr.ToString());
                        _Path = LimeLib.ReservePIDL(pidl.Ptr);
                    }
                }
                else
                {
                    // Handle links
                    string ext = System.IO.Path.GetExtension(Path).ToLower();
                    if (ext == ".lnk")
                    {
                        try
                        {
                            //  /!\  A ShellLink Windows API must be accessed from a STA thread like Thread class, 
                            //       and not a MTA thread like background-worker or UI thread.
                            ShellShortcut link = new ShellShortcut(_Path);
                            this.Link = LimeLib.ResolvePath(_Path);
                            if (LimeLib.IsPIDL(this.Link))
                            {
                                var dInfo = new DirectoryInfoEx(new PIDL(LimeLib.GetPIDL(this.Link), true));

                                // Special folder, directory, network drive...
                                if (dInfo != null)
                                {
                                    this.FileAttributes = dInfo.Attributes;
                                    this.Directory = dInfo.IsBrowsable || dInfo.IsFolder;
                                    IsApp = dInfo.Name.Contains('!');
                                    this._UseLinkIcon = IsApp;
                                    LimeMsg.Debug("LimeItem link PIDL: {0}, name: {1}", this.Link, dInfo.Name);
                                }
                                else
                                {
                                    // Default: assume directory
                                    this.Directory = true;
                                    this.FileAttributes |= FileAttributes.ReadOnly;
                                    this._UseLinkIcon = false;
                                }
                            }
                            else
                            {
                                this.Directory = System.IO.Directory.Exists(this.Link);
                                this._UseLinkIcon = string.IsNullOrEmpty(link.IconPath) && (File.Exists(this.Link) || System.IO.Directory.Exists(this.Link));
                            }
                            if (!string.IsNullOrEmpty(link.Description)) desc += Environment.NewLine + link.Description;
                            LimeMsg.Debug("LimeItem link: {0} (useLinkIcon: {1})", this.Link, this._UseLinkIcon);

                        }
                        catch
                        {
                            LimeMsg.Warning("UnableResolve", _Path);
                        }
                    }
                }

                if (string.IsNullOrEmpty(Description))
                {
                    Description = desc;
                }

                OnPropertyChanged();

            }
        }
        private string _Path;


        /// <summary>
        /// File Atributes of the item-file
        /// </summary>
        public FileAttributes FileAttributes { get; private set; }

        /// <summary>
        /// Return the path to the resolved link if a shell-shortcut, null otherwise
        /// </summary>
        public string Link { get; private set; }

        /// <summary>
        /// Return true is the item represents a WUP application
        /// </summary>
        public bool IsApp { get; private set; }


        /// <summary>
        /// Define if the destination file of a link should be used to render the icon instead of the link itself.
        /// This is required because a link API cannot be accessed from a MTA thread (background worker or UI thread).
        /// Also the shell-link can't be access in another thread, so this information must be extracted upfront.
        /// </summary>
        private bool _UseLinkIcon;

        /// <summary>
        /// Store the window handle (or null if not bound to a window)
        /// </summary>
        public IntPtr Handle
        {
            get { return _Handle; }
            set
            {
                if (value != _Handle)
                {
                    _Handle = value;
                    OnPropertyChanged();
                    IsTaskThumbVisible = value != IntPtr.Zero;
                }
            }
        }
        private IntPtr _Handle;

        /// <summary>
        /// Return true if the item represent a directory
        /// </summary>
        public bool Directory
        {
            get { return (Flags & LimeItemFlags.Directory) != 0; }
            private set
            {
                const LimeItemFlags mask = LimeItemFlags.Directory;
                LimeItemFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Return true if the item represent a panel
        /// </summary>
        public bool Panel
        {
            get { return (Flags & LimeItemFlags.Panel) != 0; }
            private set
            {
                const LimeItemFlags mask = LimeItemFlags.Panel;
                LimeItemFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }



        /// <summary>
        /// Return true if the item is Read-Only
        /// </summary>
        public bool ReadOnly { get { return (FileAttributes & FileAttributes.ReadOnly) != 0; } }


        /// <summary>
        /// Return true if the item represent a task
        /// </summary>
        public bool Task
        {
            get { return (Flags & LimeItemFlags.Task) != 0; }
            private set
            {
                const LimeItemFlags mask = LimeItemFlags.Task;
                LimeItemFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Return the media type of the item
        /// </summary>
        public MediaType MediaType
        {
            get
            {
                if (!Task && !Directory)
                    return LimeLib.GetMediaType(Path);
                else
                    return MediaType.None;
            }
        }

        /// <summary>
        /// Path of the item
        /// </summary>
        public string Tooltip
        {
            get
            {
				if (_Tooltip == null) BuildToolTip();
				return _Tooltip;
            }

			private set
			{
				if (value != _Tooltip)
				{
					_Tooltip = value;
					OnPropertyChanged();
				}
			}

        }
		private string _Tooltip = null;


        /// <summary>
        /// Returns a thread-safe Image-source representing the icon which can be bound to a WPF image
        /// </summary>
        public ImageSource ImgSrc
        {
            get
            {
                if (_ImgSrc != null) return _ImgSrc;

                // Use Fallback Icon
                var item = Directory ? Tree.DefaultDir : Tree.DefaultFile;
                return item != this ? item.IconLoad() : null;
            }
            private set
            {
                if (value != _ImgSrc)
                {
                    _ImgSrc = value;
                    OnPropertyChanged();
                }
            }
        }
        private ImageSource _ImgSrc;
        

        /// <summary>
        /// Return true if the right icon for the LimeItem is already loaded.
        /// </summary>
        public bool IconValidated
        {
            get
            {
				// Get expected size
				uint size = (IsTaskThumbVisible || !Tree.ImgSrcEnableBigSize) && Children == null ? 
					        Tree.ImgSrcSmallSize : Tree.ImgSrcBigSize;

				// Round-off Icon size
				uint iconSize = size;
				if (Tree.ImgSrcSizeRoundOff)
				{
					iconSize &= 0xFFFFFFF8;
					if (iconSize == 0) iconSize = size;
				}

				return _ImgSrc != null && _ImgSrc.Height == iconSize
					&& Flags.HasFlag(LimeItemFlags.IsUseCover) == Tree.ImgSrcUseCover;
            }
        }


        /// <summary>
        /// Return true if the item has been modified and could be saved.
        /// </summary>
        public bool Modified
        {
            get
            {
                return Metadata != null && Metadata.Modified;
            }
        }

        /// <summary>
        /// Action to be executed when a change in the directory occurs.
        /// </summary>
        private FileSystemWatcher _Watch;

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Observable Properties

        /// <summary>
        /// Contained children nodes of the LimeItem in the tree of LimeItems (null if no children).
        /// </summary>
        public ObservableCollection<LimeItem> Children
        {
            get { return _Children; }
            protected set
            {
                if (value != _Children)
                {
                    _Children = value;

                    // Inherite attributes
                    if (value != null)
                    {
                        foreach (var node in value)
                        {
                            node.Tree = Tree;
                        }
                    }

                    OnPropertyChanged();
                }
            }
        }
        private ObservableCollection<LimeItem> _Children;


        /// <summary>
        /// Defines whether the LimeItem Panel is visible (GUI).
        /// </summary>
        public bool IsPanelVisible
        {
            get { return (Flags & LimeItemFlags.IsPanelVisible) != 0; }
            set
            {
                const LimeItemFlags mask = LimeItemFlags.IsPanelVisible;
                LimeItemFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;

                    // Auto-refesh
                    if (value && this.Directory && this.Children == null)
                    {
                        this.Refresh();
                    }

                    // Avoid problem of Thumbnail showing up in hidden panel
                    if (this.Children != null)
                    {
                        foreach (LimeItem node in this.Children)
                        {
                            node.IsTaskThumbVisible = value && node.Handle != IntPtr.Zero;
                        }
                    }

                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Defines whether the Task Thumbnail is visible (GUI)
        /// </summary>
        public bool IsTaskThumbVisible
        {
            get { return (Flags & LimeItemFlags.IsTaskThumbVisible) != 0; }
            set
            {
                const LimeItemFlags mask = LimeItemFlags.IsTaskThumbVisible;
                LimeItemFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Define whether the LimeItem is busy Loading itself
        /// </summary>
        public bool IsLoading
        {
            get { return (Flags & LimeItemFlags.IsLoading) != 0; }
            private set
            {
                const LimeItemFlags mask = LimeItemFlags.IsLoading;
                LimeItemFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsBusy));
					BuildToolTip();
                }
            }
        }


        /// <summary>
        /// Define whether the LimeItem is busy Saving itself
        /// </summary>
        public bool IsSaving
        {
            get { return (Flags & LimeItemFlags.IsSaving) != 0; }
            private set
            {
                const LimeItemFlags mask = LimeItemFlags.IsSaving;
                LimeItemFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsBusy));
					BuildToolTip();
                }
            }
        }


        /// <summary>
        /// Define whether the LimeItem is busy laoding or saving
        /// </summary>
        public bool IsBusy
        {
            get { return (Flags & (LimeItemFlags.IsLoading | LimeItemFlags.IsSaving)) != 0; }
        }


        /// <summary>
        /// Represents the properties (tags...) describing the item.
        /// </summary>
        public LimeMetadata Metadata
        {
            get
            {
                return _Metadata;
            }

            private set
            {
                if (value != _Metadata)
                {
                    _Metadata = value;
                    OnPropertyChanged();
                }

            }
        }
        private LimeMetadata _Metadata = null;

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        /// <summary>
        /// Create a basic LimeItem.
        /// </summary>
        protected LimeItem()
        {
            LimeLib.LifeTrace(this);

            // Initialize properties
            _Name = null;
            _Handle = IntPtr.Zero;
            _UseLinkIcon = false;
            _Watch = null;

            Children = null;
            FileAttributes = 0;
            Link = null;
            IsApp = false;
            Description = "";
            Flags = 0;

            _Path = null;
            Tree = null;

        }


        /// <summary>
        /// Create a new LimeItem from a path.
        /// </summary>
        /// <param name="path">the item represents this file/directory path</param>
        /// <param name="parent">integrate the new item in the children of this parent-item</param>
        public LimeItem(string path, LimeItem parent = null) : this()
        {
            if (LimeLib.IsPIDL(path))
            {
                path = LimeLib.ReservePIDL(LimeLib.GetPIDL(path));
            }

            this.Path = path;

            // Tie it to its parent in the right location
            if (parent != null)
            {
                parent.Add(this);
            }
            else
            {
                Tree = new LimeItemTreeAttibute();
            }

        }


        /// <summary>
        /// Create a new LimeItem from an window task.
        /// </summary>
        /// <param name="handle">the item represents this task/windows handle</param>
        /// <param name="parent">integrate the new item in the children of this parent-item</param>
        public LimeItem(IntPtr handle, LimeItem parent = null) : this("", parent)
        {
            this.Handle = handle;
            this.Task = true;
            this.FileAttributes = FileAttributes.ReadOnly | FileAttributes.System;
            this.Refresh();
        }


        /// <summary>
        /// Destructor: dispose the IDisposable objects and unmanaged objects
        /// </summary>
        ~LimeItem()
        {
            //LimeMsg.Debug("LimeItem ~LimeItem: {0} ({1})", Name, _Path);
            _Watch?.Dispose();
            LimeLib.FreePIDL(_Path);
            LimeLib.FreePIDL(Link);
        }


        /// <summary>
        /// Create a new LimeItem representing a task-switcher (list of LimeItem representing Windows tasks) 
        /// </summary>
        /// <param name="name">Name of the item</param>
        /// <returns>the new task-switcher LimeItem</returns>
        public static LimeItem TaskSwitcher(string name = "")
        {
			// New root
			var item = new LimeItem("")
			{
				Name = name,
				Task = true,
				Directory = true,
				FileAttributes = FileAttributes.ReadOnly | FileAttributes.System
			}; 

			return item;
        }


        /// <summary>
        /// Create the LimeItem tree by looking at the given file/directory/link specified in path.
        /// </summary>
        /// <param name="path">Path of the file/directory/link Item representation.</param>
        /// <returns>the resulting LimeItem, or null if failed.</returns>
        public static LimeItem Load(string path)
        {
            LimeMsg.Debug("------------------------------------------------------------------------------");

            LimeMsg.Debug("LimeItem Load: {0}", path);

            if (!System.IO.Directory.Exists(path))
            {
                LimeMsg.Error("UnableOpenDir", path);
                return null;
            }

            // create the item
            LimeItem item = new LimeItem(path); // New root

            // Return it
            LimeMsg.Debug("------------------------------------------------------------------------------");
            return item;
        }


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region IDataObjectCompatible implementation


		/// <summary>
		/// Retrieve a clipboard-compatible DataObject representation on the actual object
		/// </summary>
		/// <param name="method">Method requesting the DataObject</param>
		/// <returns>a clipboard compatible DataObject</returns>
		public System.Windows.IDataObject GetDataObject(DataObjectMethod method)
		{
			if (Task) return null;
			return new DataObject(DataFormats.FileDrop, new string[] { Path } );
		}

		/// <summary>
		/// Defines whether the actual object can handle the given action
		/// </summary>
		/// <param name="e">DataObject event representation</param>
		public void DataObjectCanDo(DataObjectCompatibleEventArgs e)
		{
			LimeMsg.Debug("LimeItem DataObjectCanDo: {0} : {1} {2} @ {3}", Name, e.Action, e.Direction, e.Direction == DataObjectDirection.From ? e.SourceIndex : e.DestinationIndex);
			if (e.Direction == DataObjectDirection.To)
			{
				e.Handled = !Task && Directory;

				if (e.Handled)
				{
					if (e.Action == DataObjectAction.Open)
					{
						if (! e.Data.GetDataPresent("Shell IDList Array")) // Shell Link
						{
							e.Action = DataObjectAction.Link;
						}
						else
						{ 
							// Not recognized format
							e.Handled = false;
						}
					}
					else if (! e.Data.GetDataPresent(DataFormats.FileDrop, true) )
					{
						// Not recognized format
						e.Handled = false;
					}
				}
			}
			else if (e.Direction == DataObjectDirection.From)
			{
				e.Handled = !Task;
			}

			e.DestinationName = Name;
		}

		/// <summary>
		/// Request the object to take action
		/// </summary>
		/// <param name="e">DataObject event reauest</param>
		public void DataObjectDo(DataObjectCompatibleEventArgs e)
		{
			LimeMsg.Debug("LimeItem DataObjectDo: {0} : {1} {2} @ {3}", Name, e.Action, e.Direction, e.Direction == DataObjectDirection.From ? e.SourceIndex : e.DestinationIndex);

			e.Handled = e.Preliminary;
			if (e.Preliminary) return;

			var action = e.Action;
			if (e.Direction == DataObjectDirection.From)
			{
				e.Handled = true;
				if (action == DataObjectAction.Move)
				{
					Delete();
				}
			}
			else if (e.Direction == DataObjectDirection.To)
			{
				e.Handled = true;
				e.SourceHandled = true;

				// try to retrieve sources
				var paths = (string[])e.Data.GetData(DataFormats.FileDrop, true);

				// Handle special folders
				IntPtr[] pidls = null;
				if (paths == null)
				{
					var stream = (MemoryStream)e.Data.GetData("Shell IDList Array");
					if (stream != null) pidls = Win32.CILDtoPIDL(stream);
					if (stream == null || action != DataObjectAction.Link) action = DataObjectAction.None;
				}


				switch (action)
				{
					case DataObjectAction.Move:
						LimeMsg.Debug("Drop: Move to {0}", Path);
						MovePaths(paths);
						break;

					case DataObjectAction.Copy:
						LimeMsg.Debug("Drop: Copy to {0}", Path);
						CopyPaths(paths);
						break;

					case DataObjectAction.Link:
						LimeMsg.Debug("Drop: Link to {0}", Path);
						LinkPaths(paths);
						LinkPIDLs(pidls);
						break;

					default:
						LimeMsg.Debug("Drop: Do nothing to {0}", Path);
						e.Handled = false;
						e.SourceHandled = false;
						break;
				}

				// Free unmanaged resources
				if (pidls != null) {
					foreach (var pidl in pidls) Win32.ILFree(pidl);
				}
			}
		}


		#endregion



		// --------------------------------------------------------------------------------------------------
		#region Methods +
		// --------------------------------------------------------------------------------------------------

		// --------------------------------------------------------------------------------------------------
		#region Basics

		/// <summary>
		/// Default Open the Lime Item
		/// </summary>
		/// <param name="refreshDirectory">If true and the opened item is a directory, refresh if it has not been explored yet, otherwise do nothing (default).</param>
		/// <returns>true if successful in opening the item.</returns>
		public bool Open(bool refreshDirectory = true)
        {
            bool ret = true;

            LimeMsg.Debug("LimeItem Open: {0}: {1}", this.Name, this.Path);

            if (this.Handle != IntPtr.Zero && !IsApp)
            {
                // Task open
                Win32.ActivateWindowByHandle(Handle);
            }
            else if (this.Path != null)
            {
                if (this.Directory)
                {
                    // Directory open
                    if (this.Children == null && refreshDirectory)
                    {
                        ret = this.Refresh();
                    }
                }
                else
                {   // File open
                    try
                    {
                        System.Diagnostics.Process.Start(this.Path);
                    }
                    catch
                    {
                        LimeMsg.Error("ErrOpenFile", this.Path);
                        ret = false;
                    }
                }
            }

            return ret;
        }


        /// <summary>
        /// Save the LimeItem (Metadata), if it has changed
        /// </summary>
        public async Task SaveAsync()
        {
            LimeMsg.Debug("LimeItem Save: {0}", Name);
            if (Metadata != null && !IsBusy && !Task)
            {
                IsSaving = true;

                await Tree.Mutex.WaitAsync();
                try
                {
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        Metadata.Save();
                        Metadata = null; // Force invalidate of metadata
                    });
                }
                catch
                {
                    LimeMsg.Error("ErrMetaSave", Name);
                }
                finally
                {
                    IsSaving = false;
                    Tree.Mutex.Release(1);
                }
            }
        }


        /// <summary>
        /// Request for the current LoadAsync() Queue cancellation
        /// </summary>
        public void Cancel()
        {
            var queue = Tree.Queue;
            if (queue != null)
            {
                // Close this Queue
                LimeMsg.Debug("LimeItem Cancel: {0}", Tree.Queue.Count);

                // One these will eventually kill the Consumer in LoadAsync
                queue.Add(null);
                Tree.Queue = null;
            }
        }


        /// <summary>
        /// Schedule the loading asynchronously of the Items icons.
        /// The Order in which it was requested is respected, using a Thread-Safe Queue.
        /// A LimeItem Tree share the same Queue.
        /// This Queue can be cancelled using <see cref="Cancel"/>.
        /// </summary>
        public void LoadAsync()
        {
            // Retrieve current Queue
            var queue = Tree.Queue;

			// Start Queue 
			if (queue == null)
			{
				LimeMsg.Debug("LimeItem LoadAsync: Start Queue Consumer");
				Tree.Queue = queue = new BlockingCollection<LimeItem>();

				// Add Item to Queue
				queue.TryAdd(this);

				System.Threading.Tasks.Task.Run(() =>
				{
					foreach (var item in queue.GetConsumingEnumerable())
					{
						if (item == null) break; // Cancellation

						LimeMsg.Debug("LimeItem LoadAsync: {0}", item.Name);
						try
						{
							Tree.Mutex.WaitAsync();
							if (Tree.Queue != queue) break; // Cancellation
							item.IconLoad();
						}
						finally
						{
							Tree.Mutex.Release(1);
						}
					}

					LimeMsg.Debug("LimeItem LoadAsync: Stop Queue Consumer ({0})", queue.Count);
				});
			}
			else
			{
				// Add Item to Queue
				queue.TryAdd(this);
			}

        }


        /// <summary>
        /// Load/refresh the icon required to display this item
        /// </summary>
        public ImageSource IconLoad()
        {
            ImageSource ret = _ImgSrc;

            if (!IconValidated)
            {
                uint size = (IsTaskThumbVisible || !Tree.ImgSrcEnableBigSize) && Children == null ? Tree.ImgSrcSmallSize : Tree.ImgSrcBigSize;
				ret = null;

				if (Tree.ImgSrcUseCover)
				{
					var meta = Metadata;

					if (meta == null)
					{
						meta = MetadataLoad();
						MetadataUnload();
					}

					var pic = meta?.Pictures;
					if (pic is IEnumerable enu)
					{
						IEnumerator enumer = enu.GetEnumerator();
						if (enumer.MoveNext()) pic = enumer.Current;

						var img = LimeLib.ImageSourceFrom(pic);
						if (img is System.Windows.Media.Imaging.BitmapImage bmpi)
						{
							// Round-off Icon size
							uint iconSize = size;
							if (Tree.ImgSrcSizeRoundOff)
							{
								iconSize &= 0xFFFFFFF8;
								if (iconSize == 0) iconSize = size;
							}

							using (MemoryStream outStream = new MemoryStream())
							{
								System.Windows.Media.Imaging.BitmapEncoder enc = new System.Windows.Media.Imaging.BmpBitmapEncoder();
								enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmpi));
								enc.Save(outStream);
								var bmp = new Bitmap(outStream);
								var source = LimeLib.BitmapResize(bmp, (int)iconSize, (int)iconSize);

								IntPtr gdi = source.GetHbitmap();
								ret = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
									gdi, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()
									);
								Win32.DeleteObject(gdi);
								source.Dispose();
								ret.Freeze();
							}
						}
					}
				}

				if (ret == null)
				{
					var source = Bitmap(size);
					if (source != null)
					{
						IntPtr gdi = source.GetHbitmap();
						ret = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
							gdi, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()
							);
						Win32.DeleteObject(gdi);
						source.Dispose();
						ret.Freeze();
					}
				}

				if (ret != null)
                {
					// Handle flags
					Flags &= ~(LimeItemFlags.IsSizeRoundOff | LimeItemFlags.IsUseCover);
					if (Tree.ImgSrcSizeRoundOff) Flags |= LimeItemFlags.IsSizeRoundOff;
					if (Tree.ImgSrcUseCover) Flags |= LimeItemFlags.IsUseCover;

					ImgSrc = ret;
                }

            }

            return ret;
        }


        /// <summary>
        /// Refresh a LimeItem (explore directory, populate task-switcher, refresh task...).
        /// </summary>
        /// <returns>true if succeeded to refresh.</returns>
        public bool Refresh()
        {
            if (IsSaving && !Directory)
            {
                LimeMsg.Debug("LimeItem Refresh: Cancelled (IsSaving): {0}", Path);
                return false;
            }

            if (this.Directory)
            {
                if (this.Task)
                {
                    return RefreshTaskSwitcher();
                }
                else
                {
                    return RefreshDirectory();
                }

            }
            else if (this.Task)
            {
                return RefreshTask();
            }

            return RefreshItem();
        }


        /// <summary>
        /// Get the Icon/Thumbnail of the item, as a Bitmap of the given size
        /// </summary>
        /// <param name="size">target size for the Icon/thumbnail, in pixels</param>
        /// <param name="options">ThumbnailOptions in case Thumbnail applies (default: no special option).</param>
        /// <returns>a Bitmap object representing the icon</returns>
        public Bitmap Bitmap(uint size, ThumbnailOptions options = ThumbnailOptions.None)
        {
            Bitmap bmp = null;
            Icon icon = null;

            // Do not allow size of 0
            if (size == 0) size = 1;

            // Round-off Icon size
            uint iconSize = size;
            if (Tree.ImgSrcSizeRoundOff)
            {
                iconSize &= 0xFFFFFFF8;
                if (iconSize == 0) iconSize = size;
            }

            bool isTask = this.Task || this.Handle != IntPtr.Zero && this.Link == null;

            if (isTask && (size <= 32 || (options & ThumbnailOptions.IconOnly) != 0))
            {
                // Icon for task
                LimeMsg.Debug("LimeItem Bitmap {0}: handle: {1}", iconSize, Name);
                if (this.Handle != IntPtr.Zero && !this.Directory)
                {
                    try
                    {
                        icon = Win32.GetWindowIcon(Handle);
                    }
                    catch { }
                }

                if (icon != null)
                {
                    try
                    {
                        bmp = new Bitmap(icon.ToBitmap(), (int)iconSize, (int)iconSize);
                    }
                    catch
                    {
                        bmp = new Bitmap(Properties.Resources.gear.ToBitmap(), (int)iconSize, (int)iconSize);
                    }
                    if (icon.Handle != IntPtr.Zero) Win32.DestroyIcon(icon.Handle);
                    icon.Dispose();
                }
            }
			else if (isTask && this.Path == "explorer" && Tree?.DefaultDir != null)
			{
				return Tree.DefaultDir.Bitmap(size, options);
			}
			else if (!string.IsNullOrEmpty(this.Path))
            {
                string path = _UseLinkIcon ? this.Link : this.Path;

                // Icon for file
                int thumbSize = (int)iconSize;
                LimeMsg.Debug("LimeItem Bitmap {0}: {1} --> {2}", iconSize, Name, path);

                string ext = System.IO.Path.GetExtension(this.Path).ToLower();

                if (ext == ".url")
                {
                    // URL files are not supported by GetThumbnail
                    const string favicon = "favicon";
                    FileInfo file = new FileInfo(path);
                    if (file.AlternateDataStreamExists(favicon))
                    {
                        LimeMsg.Debug("LimeItem Bitmap {0}: ADS: {1}", iconSize, path);
                        AlternateDataStreamInfo s = file.GetAlternateDataStream(favicon, FileMode.Open);
                        using (Stream f = s.Open(FileMode.Open, FileAccess.Read))
                        {
                            using (var image = Image.FromStream(f))
                            {
                                bmp = new Bitmap(image);
                            }
                        }
                    }
                    else
                    {
                        string iconPath = null; // fallback

                        try
                        {
                            IniFile ini = new IniFile(path);
                            string iconUrl = ini.IniReadValue("InternetShortcut", "IconFile");
                            LimeMsg.Debug("LimeItem Bitmap {0}: Url: {1}", iconSize, iconUrl);
                            if (!string.IsNullOrEmpty(iconUrl))
                            {
                                iconPath = WebCache.GetUrlCacheEntryFile(iconUrl);
                                if (iconPath != null)
                                {
                                    LimeMsg.Debug("LimeItem Bitmap {0}: cache: {1}", iconSize, path);
                                    using (var image = Image.FromFile(iconPath))
                                    {
                                        bmp = new Bitmap(image);
                                    }
                                }
                            }
                        }
                        catch { }

                        path = iconPath ?? About.HelpPath;
                    }

                }

                if (icon != null)
                {
                    bmp = icon.ToBitmap();
                    if (icon.Handle != IntPtr.Zero) Win32.DestroyIcon(icon.Handle);
                    icon.Dispose();
                }
                else if (bmp == null)
                {
                    LimeMsg.Debug("LimeItem Bitmap {0}: GetThumbnail: {1}", iconSize, path);

                    if (LimeLib.IsSSPD(path))
                    {
                        // unhandled path-format: use PIDL instead
                        PIDL pidl = DirectoryInfoEx.PathtoPIDL(path);
                        path = String.Format(":{0}", pidl.Ptr);
                    }

                    try
                    {
                        bmp = WindowsThumbnailProvider.GetThumbnail(path, thumbSize, thumbSize, options);
                    }
                    catch
                    {
                    }

                    // Fallback
                    if (bmp == null)
                    {
                        LimeMsg.Debug("LimeItem Bitmap {0}: Task Fallback: {1}", iconSize, path);
                        if (isTask) return Bitmap(size, ThumbnailOptions.IconOnly);
                    }

                }
            }

            // Fallback
            if (bmp == null)
            {
                LimeMsg.Debug("LimeItem Bitmap {0}: Fallback: {1}", iconSize, Name);
				bmp = new Bitmap(Properties.Resources.cpu, (int)iconSize, (int)iconSize);
            }

            // Resize and center the icons that are smaller than the target size
            return LimeLib.BitmapResize(bmp, (int)iconSize, (int)iconSize);
        }


		/// <summary>
		/// Build the Tooltip property
		/// </summary>
		private void BuildToolTip()
		{
			string ret;

			if (!string.IsNullOrEmpty(Description))
			{
				ret = Description;
			}
			else if (Link != null && !LimeLib.IsPIDL(Link))
			{
				string val = System.IO.Path.GetFileName(Link);
				ret = String.IsNullOrEmpty(val) ? Link : val;
			}
			else if (Path != null)
			{
				string val = System.IO.Path.GetFileName(Path);
				ret = String.IsNullOrEmpty(val) ? Path : val;
			}
			else
			{
				ret = Name;
			}

			// Add Metadata/Status tooltips

			if (IsSaving)
			{
				ret += Environment.NewLine + LimeLanguage.Translate(IniLanguageSection, "Saving");
			}
			else if (IsLoading)
			{
				ret += Environment.NewLine + LimeLanguage.Translate(IniLanguageSection, "Loading");
			}
			else
			{
				string msg = Metadata?.Tooltip;
				if (!string.IsNullOrEmpty(msg))
				{
					ret += Environment.NewLine + msg;
				}
			}

			Tooltip = ret;
		}


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Shell

		/// <summary>
		/// Show the Shell context menu for the Lime Item.
		/// </summary>
		/// <param name="pointScreen">The point on screen where the menu should pop up</param>
		public void ShellMenu(System.Drawing.Point pointScreen)
        {
			IntPtr appHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

			try
			{
                if (Task)
                {
					if (Handle != appHandle)
					{
						LimeMsg.Debug("LimeItem ShellMenu: task: {0}", this.Name);
						Win32.ShowSystemMenu(this.Handle, appHandle, pointScreen);
					}
                }
                else if (Path != null && !LimeLib.IsSSPD(Path))
                {
                    LimeMsg.Debug("LimeItem ShellMenu: path: {0}", this.Name);

                    if (LimeLib.IsPIDL(Path))
                    {
                        ShellContextMenu scm = new ShellContextMenu();
                        IntPtr[] pidls = new IntPtr[1] { LimeLib.GetPIDL(Path) };
                        scm.ShowContextMenu(pidls, pointScreen);
                    }
                    else
                    {
                        ShellContextMenu scm = new ShellContextMenu();
                        FileInfo[] files = new FileInfo[1] { new FileInfo(Path) };
                        scm.ShowContextMenu(files, pointScreen);
                    }

                }
            }
            catch
            {
                LimeMsg.Debug("LimeItem ShellMenu: path: {0} --> FAIL", this.Name);
            }
        }


        /// <summary>
        /// Handle directory/file operations using windows API to the current lime item as destination. 
        /// </summary>
        /// <param name="sourcePaths">array of absolute directory/file paths to be processed</param>
        /// <param name="ownerWindow">window handle from which this is called.</param>
        /// <param name="fileOperatonType">Type of operation to be applied to the file-list</param>
        /// <returns></returns>
        private Thread FileOperationsAsync(string[] sourcePaths, IntPtr ownerWindow, ShellFileOperation.FileOperations fileOperatonType)
        {
			// Initiate File Operation
			var fo = new ShellFileOperation
			{
				OwnerWindow = ownerWindow,
				Operation = fileOperatonType
			};

			if (fileOperatonType == ShellFileOperation.FileOperations.FO_DELETE)
            {
                // Delete Item
                if (sourcePaths != null || Task || String.IsNullOrEmpty(Path) || LimeLib.IsPIDL(Path)) return null;
                fo.SourceFiles = new string[] { Path };
            }
            else
            {
                // Copy/Move/Rename sources to Item
                if (sourcePaths == null || sourcePaths.Length < 1 || Task || String.IsNullOrEmpty(Path) || !Directory) return null;

                string dest = Link ?? Path;

				// Handle duplicates
				var sources = sourcePaths.ToList();
				var destinations = new List<string>(sources.Count);
				var regex = new Regex(@"\((\d+)\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

				for (int i = 0; i < sources.Count; i++)
                {
					var src = sources[i];
					string fname = System.IO.Path.GetFileName(src);

					if (fname == "" && sources[i].EndsWith(":\\"))
                    {
                        // Drive
                        var dinf = new DriveInfo(src);
                        fname = String.Format("{0} ({1})", dinf.VolumeLabel, src[0]);
                    }

					bool doit = true;
					try
					{
						var spath = System.IO.Path.GetDirectoryName(src);

						if (dest.Equals(spath, StringComparison.OrdinalIgnoreCase))
						{
							if (fileOperatonType == ShellFileOperation.FileOperations.FO_MOVE ||
								fileOperatonType == ShellFileOperation.FileOperations.FO_RENAME)
							{
								// Skip move of this item
								sources.RemoveAt(i--);
								doit = false;
							}
							else
							{
								var sname = System.IO.Path.GetFileNameWithoutExtension(src);
								var sext = System.IO.Path.GetExtension(src);
								int idx = 1;

								MatchCollection matches = regex.Matches(sname);
								if (matches.Count == 1)
								{
									idx = int.Parse(matches[0].Value.Trim(new char[] { '(', ')' })) + 1;
									sname = regex.Replace(sname, "", 1);
								}

								for (; idx < sources.Count; idx++)
								{
									fname = string.Format("{0} ({1}){2}", sname, idx, sext);
									var fpath = System.IO.Path.Combine(dest, fname);
									if (!File.Exists(fpath)) break;
								}
							}

						}
					}
					catch { }

					if (doit)
					{
						destinations.Add( System.IO.Path.Combine(dest, fname) );
						LimeMsg.Debug("LimeItem FileOperationsAsync: {2} '{0}' to '{1}'", sources[i], destinations[i], fileOperatonType.ToString());
					}
				}

				fo.SourceFiles = sources.ToArray();
				fo.DestFiles = destinations.ToArray();
			}


			// Create thread to run this job asynchronously
			Thread thread = new Thread(FileOperationsAsync_DoWork);
            thread.Start(fo);

			return thread;
        }


        /// <summary>
        /// Delegate for <see cref="FileOperationsAsync"/>. 
        /// </summary>
        /// <param name="fileOperation"></param>
        private static void FileOperationsAsync_DoWork(object fileOperation)
        {
            var fo = fileOperation as ShellFileOperation;
            fo.DoOperation();
        }


        /// <summary>
        /// Delete the current directory/file represented by the LimeItem, using the Windows delete (to Trash). 
        /// </summary>
        /// <param name="sourcePaths">array of absolute directory/file paths to be copied to the current item</param>
        /// <returns>The thread where the copy is executed, null if invalid request.</returns>
        public Thread Delete()
        {
			IntPtr ownerWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
			return FileOperationsAsync(null, ownerWindow, ShellFileOperation.FileOperations.FO_DELETE);
        }


        /// <summary>
        /// Copy a list of directories/files to the current LimeItem representing a directory using the Windows copy. 
        /// </summary>
        /// <param name="sourcePaths">array of absolute directory/file paths to be copied to the current item</param>
        /// <returns>The thread where the copy is executed, null if invalid request.</returns>
        public Thread CopyPaths(string[] sourcePaths)
        {
			IntPtr ownerWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
			return FileOperationsAsync(sourcePaths, ownerWindow, ShellFileOperation.FileOperations.FO_COPY);
        }

        /// <summary>
        /// Move a list of directories/files to the current LimeItem representing a directory using the Windows move. 
        /// </summary>
        /// <param name="sourcePaths">array of absolute directory/file paths to be moved to the current item</param>
        /// <returns>The thread where the move is executed, null if invalid request.</returns>
        public Thread MovePaths(string[] sourcePaths)
        {
			IntPtr ownerWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
			return FileOperationsAsync(sourcePaths, ownerWindow, ShellFileOperation.FileOperations.FO_MOVE);
        }


        /// <summary>
        /// Create a ShellLink for a list of directories/files to the current LimeItem.
        /// </summary>
        /// <param name="sourcePaths">array of absolute directory/file paths to be linked to the current item</param>
        /// <returns>true is successfull, false otherwise.</returns>
        public bool LinkPaths(string[] sourcePaths)
        {
            if (sourcePaths == null || sourcePaths.Length < 1 || Task || string.IsNullOrEmpty(Path) || !Directory) return false;

            string fdest = Link ?? Path;
            var pidls = new List<IntPtr>();
            var copies = new List<string>();

            bool ret = true;


            foreach (var src in sourcePaths)
            {
                string ext;

                if (LimeLib.IsPIDL(src))
                {
                    // Handle PIDLs
                    if (src.Length > 2 && src[1] != ':')
                    {
                        pidls.Add(LimeLib.GetPIDL(src));
                    }
                    else
                    {
                        ret = false;
                        LimeMsg.Error("ErrLink", src, fdest);
                    }
                }
                else if (!Tree.AllowLinkOfLink && ((ext = System.IO.Path.GetExtension(src).ToLower()) == ".lnk" || ext == ".url"))
                {
                    // Do copy of link instead of link of link, because Windows doesn't handle Link of link, and because it preserves the ADS
                    LimeMsg.Debug("LimeItem LinkPaths: copy link '{0}'", src);
                    copies.Add(src);
                }
                else
                {
                    // Normal Paths
                    string fname = System.IO.Path.GetFileNameWithoutExtension(src);

                    if (fname == "" && src.EndsWith(":\\"))
                    {
                        // Drive
                        var dinf = new DriveInfo(src);
                        fname = dinf.VolumeLabel;
                    }

                    string dest = System.IO.Path.Combine(fdest, fname + ".lnk");

                    // Handle already existing file
                    int exist = 1;
                    while (File.Exists(dest) || System.IO.Directory.Exists(dest))
                    {
                        exist++;
                        dest = System.IO.Path.Combine(fdest, string.Format("{0} ({2}){1}", fname, ".lnk", exist));
                    }


                    LimeMsg.Debug("LimeItem LinkPaths: '{0}' to '{1}'", src, dest);
                    try
                    {
						var link = new ShellShortcut(dest)
						{
							Path = src,
							WorkingDirectory = System.IO.Path.GetDirectoryName(src)
						};
						link.Save();
                    }
                    catch
                    {
                        ret = false;
                        LimeMsg.Error("ErrLink", src, dest);
                    }
                }
            }

            // Create the PIDLs
            if (pidls.Count > 0)
            {
                LinkPIDLs(pidls.ToArray());
            }

            // Do the copies
            if (copies.Count > 0)
            {
                CopyPaths(copies.ToArray());
            }

            return ret;
        }


        /// <summary>
        /// Create a ShellLink for a list of PIDLs (i.e. special folders) to the current LimeItem.
        /// </summary>
        /// <param name="sourcePaths">array of PIDLs paths to be linked to the current item</param>
        /// <returns>true is successfull, false otherwise.</returns>
        public bool LinkPIDLs(IntPtr[] sourcePidls)
        {
            if (sourcePidls == null || sourcePidls.Length < 1 || Task || String.IsNullOrEmpty(Path) || !Directory) return false;

            string fdest = Link ?? Path;

            bool ret = true;
            foreach (var src in sourcePidls)
            {
                var dInfo = new DirectoryInfoEx(new PIDL(src, true));
                string dest = System.IO.Path.Combine(fdest, dInfo.Label + ".lnk");

                // Handle already existing file
                int exist = 1;
                while (File.Exists(dest) || System.IO.Directory.Exists(dest))
                {
                    exist++;
                    dest = System.IO.Path.Combine(fdest, string.Format("{0} ({2}){1}", dInfo.Label, ".lnk", exist));
                }

                LimeMsg.Debug("LimeItem LinkPIDLs: :{0} ({2}) to '{1}'", src, dest, dInfo.Label);

                try
                {
					var link = new ShellShortcut(dest)
					{
						PIDL = src
					};
					link.Save();
                }
                catch
                {
                    ret = false;
                    LimeMsg.Error("ErrLink", dInfo.Label, dest);
                }
            }

            return ret;
        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Files

        /// <summary>
        /// Reset the LimeItem and reload it, excepted directory content 
        /// </summary>
        /// <returns>true if succeeded to refresh.</returns>
        private bool RefreshItem()
        {
            string path = Path;
            LimeMsg.Debug("LimeItem RefreshItem: {0}", path);

            // Reset Icon visibility
            ImgSrc = null;
            IsTaskThumbVisible = false;

            // Reset file information
            Path = null;
            FileSystemInfoEx info = new FileSystemInfoEx(path);
            Handle = IntPtr.Zero;
            Link = null;
            Metadata = null;
            Description = "";
            Name = LimeLib.IsPIDL(path) || LimeLib.IsLibrary(path) ? info.Label : null;
            FileAttributes = info.Attributes;
            Path = info.FullName;

            // Reload the Metadata (not automatic if already focused)
            System.Threading.Tasks.Task.Run( () => MetadataLoadAsync() );

            return true;
        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Directories

        /// <summary>
        /// Refresh the LimeItem that represents a directory
        /// </summary>
        /// <returns>true if succeeded to refresh.</returns>
        private bool RefreshDirectory()
        {
            // Default path
            if (String.IsNullOrEmpty(Path)) return false;

            string path = Link ?? Path;

            LimeMsg.Debug("LimeItem RefreshDirectory: {0}", path);

            List<LimeItem> dirs;
            List<LimeItem> files;

            if (LimeLib.IsPIDL(path) || LimeLib.IsLibrary(path))
            {
                // --------------------
                // Special directory: use FileSystemInfoEx

                FileSystemInfoEx[] fInfos = null;
                try
                {
                    DirectoryInfoEx dInfo;
                    if (path.Length > 2 && path[1] != ':')
                    {
                        // Duplicate the PIDL because the one used by DirectoryInfoEx will be freed automatically
                        PIDL pidl = new PIDL(LimeLib.GetPIDL(path), true);
                        dInfo = new DirectoryInfoEx(pidl);
                        fInfos = dInfo.GetFileSystemInfos();
                    }
                    else
                    {
                        dInfo = new DirectoryInfoEx(path);
                        fInfos = dInfo.GetFileSystemInfos();
                    }
                }
                catch
                {
                    LimeMsg.Error("UnableSpecDir", path);
                    return false;
                }

                if (fInfos == null)
                {
                    LimeMsg.Error("UnableSpecDir", path);
                    return false;
                }

                Array.Sort(fInfos, (x, y) => StringComparer.InvariantCultureIgnoreCase.Compare(x.Name, y.Name));

                LimeMsg.Debug("LimeItem RefreshDirectory: Loaded FileSystemInfoEx");


                int size = fInfos.Count();
                dirs = new List<LimeItem>(size);
                files = new List<LimeItem>(size);

                foreach (var info in fInfos)
                {
                    FileAttributes attr = info.CachedAttributes; // don't reload attributes

                    // create the child item
                    var child = new LimeItem
                    {
                        Name = info.Label,
                        FileAttributes = attr,
                        Path = info.FullName
                    };

                    LimeMsg.Debug("LimeItem Refresh: Special: {0} : {1}", child.Name, child.Path);

                    // Add/sort to list
                    if (child.Directory) dirs.Add(child); else files.Add(child);
                }

            }
            else
            {
                // --------------------
                // Normal directory: use FileSystemInfo, because it is way faster than FileSystemInfoEx

                FileSystemInfo[] fInfos = null;
                try
                {
                    DirectoryInfo dInfo;
                    dInfo = new DirectoryInfo(path);
                    fInfos = dInfo.GetFileSystemInfos();
                }
                catch
                {
                    LimeMsg.Error("UnableOpenDir", path);
                    return false;
                }

                if (fInfos == null)
                {
                    LimeMsg.Error("UnableOpenDir", path);
                    return false;
                }

                Array.Sort(fInfos, (x, y) => StringComparer.InvariantCultureIgnoreCase.Compare(x.Name, y.Name));

                int size = fInfos.Count();
                dirs = new List<LimeItem>(size);
                files = new List<LimeItem>(size);

                LimeMsg.Debug("LimeItem RefreshDirectory: Loaded FileSystemInfo");

                foreach (var info in fInfos)
                {
                    FileAttributes attr = info.Attributes;
                    if ((attr & FileAttributes.Hidden) == 0)
                    {
                        // create the child item
                        LimeItem child = new LimeItem
                        {
                            FileAttributes = attr,
                            Path = info.FullName
                        };

                        // Add/sort to list
                        if (child.Directory) dirs.Add(child); else files.Add(child);
                    }
                }
            }

            // --------------------
            // Finalize

            LimeMsg.Debug("LimeItem RefreshDirectory: Finalize");

            dirs.AddRange(files);
            Children = new ObservableCollection<LimeItem>(dirs);

            LimeMsg.Debug("LimeItem RefreshDirectory: Done.");
            return true;
        }


        /// <summary>
        /// Add a child to a LimeItem node
        /// </summary>
        /// <param name="node">LimeItem to be added to the children of the object</param>
        /// <returns>the added (new) child-node, null if fail</returns>
        public LimeItem Add(LimeItem node = null)
        {
            int pos = FindInsertIndex(node);
            if (pos < 0) return null;
            if (node == null) node = new LimeItem("");
            node.Tree = Tree; // Inherite attribute from parent
            if (this.Children == null) this.Children = new ObservableCollection<LimeItem>();
            this.Children.Insert(pos, node);
            return node;
        }


        /// <summary>
        /// Remove all the children of a LimeItem
        /// </summary>
        public void Clear()
        {
            _Watch?.Dispose();
            _Watch = null;
            Children = null;
        }


        /// <summary>
        /// Find the right position in the children of a LimeItem directory to insert a node.
        /// </summary>
        /// <param name="node">the node to insert as a child</param>
        /// <returns>the position where this should be inserted, or -1 if fail</returns>
        public int FindInsertIndex(LimeItem node)
        {
            if (!this.Directory) return -1;
            if (this.Children == null) return 0;
            if (this.Task) return this.Children.Count;

            int ret = 0;
            for (; ret < this.Children.Count; ret++)
            {
                var item = this.Children[ret];
                if ((node.Directory == item.Directory && String.Compare(node.Name, item.Name, true) <= 0)
                   || (node.Directory && !item.Directory)
                   )
                {
                    if (item != node) break;
                }
            }

            LimeMsg.Debug("LimeItem FindInsertIndex: {0}: {1}", node.Name, ret);
            return ret;
        }


        /// <summary>
        /// Monitor every change in a LimeItem representing a directory.
        /// </summary>
        public void Watch()
        {
            if (!Directory || Task) return;

            string rpath = Link ?? Path;
            if (LimeLib.IsPIDL(rpath) || LimeLib.IsLibrary(rpath)) return;

            if (_Watch != null)
            {
                LimeMsg.Debug("LimeItem Watch: Already monitored: {0}", rpath);
                return;
            }

            LimeMsg.Debug("LimeItem Watch: {0}", rpath);

            try
            {
                _Watch = new FileSystemWatcher
                {
                    Path = rpath,
                    IncludeSubdirectories = false,
                    Filter = ""
                };
                _Watch.Created += new FileSystemEventHandler(OnChanged);
                _Watch.Changed += new FileSystemEventHandler(OnChanged);
                _Watch.Deleted += new FileSystemEventHandler(OnChanged);
                _Watch.Renamed += new RenamedEventHandler(OnRenamed);

                _Watch.EnableRaisingEvents = true;
            }
            catch
            {
                LimeMsg.Debug("LimeItem Watch: fail {0}", rpath);
                _Watch?.Dispose();
                _Watch = null;
            }

        }


        /// <summary>
        /// Delegate for <see cref="Watch"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Process on UI Thread if Tree is linked to an UI
            if (Tree.UIElement != null && source != null)
            {
                Tree.UIElement.Dispatcher.Invoke(() => OnChanged(null, e) );
                return;
            }

            LimeMsg.Debug("LimeItem OnChanged: {0} {1}", e.ChangeType.ToString(), e.FullPath);

            LimeItem item = null;
            int index = -1;

            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                item = new LimeItem(e.FullPath, this);
                index = Children.IndexOf(item);
            }
            else if (Children != null)
            {
                foreach (var node in Children)
                {
                    index++;
                    if (node.Path == e.FullPath)
                    {
                        if (e.ChangeType == WatcherChangeTypes.Deleted)
                        {
                            this.Children.Remove(node);
                        }
                        else if (node.IsSaving)
                        {
                            LimeMsg.Debug("LimeItem OnChanged: Delay: {0}: {1}", e.ChangeType.ToString(), e.FullPath);

                            // Delay the Change event to after the busy state has been reset
                            PropertyChangedEventHandler handler = null;
                            handler = (object sender, PropertyChangedEventArgs ev) =>
                            {
                                if (ev.PropertyName == "IsSaving")
                                {
                                    LimeMsg.Debug("LimeItem OnChanged: Apply Delayed: {0}: {1} --> {2}", e.ChangeType.ToString(), e.FullPath, handler);
                                    node.PropertyChanged -= handler; // Don't do it again
                                    OnChanged(handler, e); // Source can be anything but null
                                }
                            };
                            node.PropertyChanged += handler;

                            // Safety: If it's already not busy anymore, make sure the handler is called at least once
                            if (!node.IsSaving) handler.Invoke(node, new PropertyChangedEventArgs("IsSaving"));

                        }
                        else
                        {
                            // Reload item
                            node.RefreshItem();
                        }

                        item = node;
                        break;
                    }
                }
            }

            // Invoke delegate
            Tree.OnChildrenChanged(this, item, index);
        }


        /// <summary>
        /// Delegate for <see cref="Watch"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Process on UI Thread if Tree is linked to an UI
            if (Tree.UIElement != null && source != null)
            {
                Tree.UIElement.Dispatcher.Invoke(() => OnRenamed(null, e));
                return;
            }

            LimeMsg.Debug("LimeItem OnRenamed: {0} renamed to {1}", e.OldFullPath, e.FullPath);

            LimeItem item = null;
            int index = -1;

            if (this.Children != null)
            {
                foreach (var node in Children)
                {
                    if (node.Path == e.OldFullPath)
                    {
                        LimeMsg.Debug("LimeItem OnRenamed: Rename: {0}", node.Name);
                        if (System.IO.Path.GetExtension(node.Path) != System.IO.Path.GetExtension(e.FullPath))
                        {
                            node.ImgSrc = null;
                            node.IsTaskThumbVisible = false;
                        }

                        node.Name = "";
                        node.Path = e.FullPath;

                        int old = this.Children.IndexOf(node);
                        int pos = this.FindInsertIndex(node);
                        if (old >= 0 && pos >= 0 && old != pos)
                        {
                            if (old < pos) pos--; // fix move inconsistency
                            this.Children.Move(old, pos);
                        }
                        item = node;
                        break;
                    }
                }
            }

            // Invoke delegate
            if (item != null) index = Children.IndexOf(item);
            Tree.OnChildrenChanged(this, item, index);

        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Tasks

        /// <summary>
        /// Refresh the LimeItem that represents a task switcher
        /// </summary>
        /// <returns>true if succeeded to refresh.</returns>
        private bool RefreshTaskSwitcher()
        {
            bool ret = true;
            LimeMsg.Debug("LimeItem RefreshTaskSwitcher: {0}", this.Name);

            if (this.Children == null)
                this.Children = new ObservableCollection<LimeItem>();

            foreach (var node in this.Children)
            {
                node.IsTaskThumbVisible = false;
            }

            // Mark by null the limit between old qnd refreshed items
            this.Children.Add(null);

            try
            {
                Win32.EnumerateWindows(Refresh_Task_Add, IntPtr.Zero);
            }
            catch
            {
                ret = false;
            }

            // Remove old items (until null)
            while (this.Children[0] != null)
            {
                LimeMsg.Debug("LimeItem RefreshTaskSwitcher: remove {0}", this.Children[0].Handle.ToString());
                this.Children.RemoveAt(0);
            }
            this.Children.RemoveAt(0);

            // Refresh task found
            foreach (var node in this.Children)
            {
                node.Refresh();
                node.IsTaskThumbVisible = this.IsPanelVisible;
            }

            return ret;
        }

        /// <summary>
        /// Refresh the LimeItem that represents a task
        /// </summary>
        /// <returns>true if succeeded to refresh.</returns>
        private bool RefreshTask()
        {
            string path;
            string desc;

            bool isWUPApp = Win32.IsWindowModernApp(this.Handle);

            if (isWUPApp)
            {
                path = null;
                desc = Win32.GetWindowTitle(this.Handle);

				// TODO: Modern App: try using Windows.System.AppDiagnosticInfo

				//foreach (var p in System.Diagnostics.Process.GetProcesses())
				//{
				//    var pack = AppxPackage.FromProcess(p);
				//    if (pack != null)
				//    {
				//        LimeMsg.Debug("LimeItem RefreshTask: ProcApp: {0}: {1} -> {2}", desc, p.MainWindowHandle, p.Handle);
				//    }
				//}

				//try
				//{
				//    // Handle Modern App
				//    Win32.EnumChildWindows(handle,
				//        (IntPtr childHwnd, IntPtr lParam) =>
				//        {
				//            int childPid = Win32.GetWindowPID(childHwnd);
				//            if (childPid != -1)
				//            {
				//                LimeMsg.Debug("LimeItem RefreshTask: App: {0}: {1} -> {2}", desc, childHwnd.ToInt32(), childPid);
				//            }
				//            return true;
				//        },
				//        IntPtr.Zero);
				//}
				//catch { }


				//var package = AppxPackage.FromWindow(this.handle);

			}
			else
            {
                // Classic Window exe
                path = Win32.GetExecutablePathByHandle(this.Handle);
                desc = Win32.GetWindowTitle(this.Handle);
            }


            // Safeguard
            if (path == null) path = "";
            if (desc == null) desc = "";

            // Arrange title, path and description

            string title;
            if (isWUPApp)
            {
                title = desc;
            }
            else if (path == "iexplore")
            {
                title = "WEB: " + desc;

                // Ugly trick to get the path that helps to match the ShellLink shortcut with 64/86 virtualization
                path = (string)Microsoft.Win32.Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\IEXPLORE.EXE", "", path);
                if (8 == IntPtr.Size
                    || (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
                {
					string progFile = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
					string progFileX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    path = path.Replace(progFile, progFileX86);
                }
            }
            else if (path == "explorer")
            {
                title = desc;
            }
            else
            {
                string fname = System.IO.Path.GetFileNameWithoutExtension(path);
                if (desc.ToLower().StartsWith(fname.ToLower()))
                    title = desc;
                else
                    title = fname + ": " + desc;
            }

            // Finalize description
            if (path != "")
            {
                desc += Environment.NewLine + "[" + path + "]";
            }

            // create the object
            LimeMsg.Debug("LimeItem RefreshTask: {0}", title);
            this.Name = title;
            this.Path = path;
            this.Description = desc;

            // Load data
            MetadataLoad();

            return true;
        }

        /// <summary>
        /// Detect if the LimeItem matches a task (handle) from a taskSwitcher (LimeItem object containing nodes representing tasks).
        /// </summary>
        /// <param name="taskSwitcher">LimeItem object containing nodes representing tasks</param>
        /// <param name="enable">enable or disable the task-matching on this item</param>
        /// <returns>The index of the task if match, or -1 if not matched</returns>
        public int TaskMatcher(LimeItem taskSwitcher, bool enable = true)
        {
            int ret = -1;

            if (this.Directory) return ret;

            LimeItem match = null;
            if (taskSwitcher.Children != null && enable)
            {
                string path = Link ?? Path;
                string name = Name;

                if (Link != null && !LimeLib.IsPIDL(path) && !LimeLib.IsSSPD(path))
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path);
                }

                for (int i = 0; i < taskSwitcher.Children.Count; i++)
                {
                    var node = taskSwitcher.Children[i];
                    // TODO: stub: matching of data file
                    string wintitle = Win32.GetWindowTitle(node.Handle);
                    try
                    {
                        wintitle = System.IO.Path.GetFileNameWithoutExtension(wintitle);
                    }
                    catch { };

                    if (string.Compare(node.Path, path, true) == 0 || string.Compare(wintitle, name, true) == 0)
                    {
                        match = node;
                        ret = i;
                        break;
                    }
                }
            }

            if (match != null && match.Handle != IntPtr.Zero)
            {
                LimeMsg.Debug("LimeItem TaskMatcher: matched: {0} with {1}", this.Name, match.Handle);
                Handle = match.Handle;
                IsTaskThumbVisible = true;
            }
            else
            {
                this.Handle = IntPtr.Zero;
            }

            return ret;
        }


        /// <summary>
        /// Delegate called when listing all the windows (for task-switcher)
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="lParam">windows lParam</param>
        /// <returns>always true</returns>
        private bool Refresh_Task_Add(IntPtr hWnd, IntPtr lParam)
        {
            // Task selection by eliminating the non window tasks
            uint style = Win32.GetWindowLongPtr(hWnd, Win32.GWL.GWL_STYLE);
            uint exStyle = Win32.GetWindowLongPtr(hWnd, Win32.GWL.GWL_EXSTYLE);
            if (Win32.GetParent(hWnd) != IntPtr.Zero                            // has Parent window
               || Win32.GetWindow(hWnd, Win32.GW.Owner) != IntPtr.Zero            // is Owned window
               || (style & (uint)Win32.WindowStyles.WS_VISIBLE) == 0              // is Invisible window
               || (exStyle & (uint)Win32.WindowExStyles.WS_EX_TOOLWINDOW) != 0    // is ToolTip window
               || Win32.GetWindowTitle(hWnd) == null                              // has no title
               || hWnd == this.Handle                                             // is Lime Window itself
               || Win32.GetSystemMenu(hWnd, false) == IntPtr.Zero                 // has no System Menu (check required for Modern App)
               || !Tree.TaskSwitcherShowApp && Win32.IsWindowModernApp(hWnd)	  // Applications not enabled
               )
            {
                return true;
            }

            LimeMsg.Debug("LimeItem Refresh_Task_Add: {0} (0x{1:X8})", hWnd, hWnd.ToInt32());

            // Find existing item
            if (this.Children != null)
            {
                int end = this.Children.Count;
                for (int i = 0; this.Children[i] != null; i++)
                {
                    var item = this.Children[i];
                    if (item.Handle == hWnd)
                    {
                        this.Children.Move(i, end - 1);
                        return true;
                    }
                }
            }

            // Create new item
            LimeItem node = new LimeItem(hWnd, this);

            return true;
        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Metadata

        /// <summary>
        /// Load the Metadata contained in this item
        /// </summary>
        /// <returns>Metadata for this item</returns>
        public LimeMetadata MetadataLoad()
        {
            if (Metadata != null) return Metadata;

            LimeMsg.Debug("LimeItem MetadataLoad: {0}", Name);

            IsLoading = true;
            Metadata = new LimeMetadata(this);
            IsLoading = false;

            if (Metadata != null) BuildToolTip();

            return Metadata;
        }


		/// <summary>
		/// Unload the Metadata contained in this item to save memory
		/// </summary>
		/// <returns>True if the Metadata are removed.</returns>
		public bool MetadataUnload()
		{
			if (Metadata == null) return true;
			if (Modified || IsBusy || Task) return false;

			LimeMsg.Debug("LimeItem MetadataUnload: {0}", Name);
			_Metadata?.Clear();
			_Metadata = null;

			return true;
		}

		/// <summary>
		/// Load the Metadata contained in this item, asynchronously. One Metadata is loaded at a time.
		/// </summary>
		/// <param name="stillValid">This condition will be checked when the metadata is about to be loaded, to cancel it if false.</param>
		/// <returns>Metadata for this item</returns>
		public async Task<LimeMetadata> MetadataLoadAsync(Func<bool> stillValid = null)
        {
            if (Metadata != null) return Metadata;

            IsLoading = true;
            await Tree.Mutex.WaitAsync();

            try
            {
                if (stillValid == null || stillValid()) // Do it only if item is still valid request
                {
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        MetadataLoad();
                    });
                }
                else
                {
                    LimeMsg.Debug("LimeItem MetadataLoadAsync: Cancel: {0}", Name);
                }
            }
            finally
            {
                IsLoading = false;
                Tree.Mutex.Release(1);
            }

            return Metadata;
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #endregion



    }
}
