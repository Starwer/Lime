﻿/**************************************************************************
* Author:       MCS Team
* Website:      https://github.com/kingpin2k/MCS
*  18-12-2018 - Changed by Sebastien Mouy (Starwer)  
*               Renamed Class to ComDataObject
*               Changed namespace to integrate to WPFhelper
*               Added DropImageType
*               Improved SetDropDescription
*               Removed Legacy (commented out) code
**************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices.ComTypes;


namespace WPFhelper
{
	// ----------------------------------------------------------------------------------------------
	#region Public types

	public enum DropImageType : int
	{
		Invalid = -1,
		None = 0,
		Copy = 1,
		Move = 2,
		Link = 4,
		Label = 6,
		Warning = 7,
		NoImage = 8
	}

	#endregion

	/// <summary>
	/// Implements the COM version of IDataObject including SetData.
	/// </summary>
	/// <remarks>
	/// <para>Use this object when using shell (or other unmanged) features
	/// that utilize the clipboard and/or drag and drop.</para>
	/// <para>The System.Windows.DataObject (.NET 3.0) and
	/// System.Windows.Forms.DataObject do not support SetData from their COM
	/// IDataObject interface implementation.</para>
	/// <para>To use this object with .NET drag and drop, create an instance
	/// of System.Windows.DataObject (.NET 3.0) or System.Window.Forms.DataObject
	/// passing an instance of DataObject as the only constructor parameter. For
	/// example:</para>
	/// <code>
	/// System.Windows.DataObject data = new System.Windows.DataObject(new DragDropLib.DataObject());
	/// </code>
	/// </remarks>
	[ComVisible(true)]
	public class ComDataObject : IDataObject, IDisposable
	{
		#region Unmanaged functions

		// These are helper functions for managing STGMEDIUM structures

		[DllImport("urlmon.dll")]
		private static extern int CopyStgMedium(ref STGMEDIUM pcstgmedSrc, ref STGMEDIUM pstgmedDest);
		[DllImport("ole32.dll")]
		private static extern void ReleaseStgMedium(ref STGMEDIUM pmedium);

		[DllImport("user32.dll")]
		private static extern void PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


		#endregion // Unmanaged functions

		// Our internal storage is a simple list
		private IList<KeyValuePair<FORMATETC, STGMEDIUM>> storage;

		// Keeps a progressive unique connection id
		private int nextConnectionId = 1;

		// List of advisory connections
		private IDictionary<int, AdviseEntry> connections;

		// Represents an advisory connection entry.
		private class AdviseEntry
		{
			public FORMATETC format;
			public ADVF advf;
			public IAdviseSink sink;

			public AdviseEntry(ref FORMATETC format, ADVF advf, IAdviseSink sink)
			{
				this.format = format;
				this.advf = advf;
				this.sink = sink;
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 1044)]
		public struct DropDescription
		{
			/// <summary>
			/// A DROPIMAGETYPE indicating the stock image to use.
			/// </summary>
			public DropImageType type;

			/// <summary>
			/// Text such as "Move to %1".
			/// </summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szMessage;

			/// <summary>
			/// Text such as "Documents", inserted as specified by szMessage.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szInsert;
		}


		/// <summary>
		/// Creates an empty instance of DataObject.
		/// </summary>
		public ComDataObject()
		{
			storage = new List<KeyValuePair<FORMATETC, STGMEDIUM>>();
			connections = new Dictionary<int, AdviseEntry>();
		}

		/// <summary>
		/// Releases unmanaged resources.
		/// </summary>
		~ComDataObject()
		{
			Dispose(false);
		}

		/// <summary>
		/// Clears the internal storage array.
		/// </summary>
		/// <remarks>
		/// ClearStorage is called by the IDisposable.Dispose method implementation
		/// to make sure all unmanaged references are released properly.
		/// </remarks>
		private void ClearStorage()
		{
			foreach (KeyValuePair<FORMATETC, STGMEDIUM> pair in storage)
			{
				STGMEDIUM medium = pair.Value;
				ReleaseStgMedium(ref medium);
			}
			storage.Clear();
		}

		/// <summary>
		/// Releases resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Releases resources.
		/// </summary>
		/// <param name="disposing">Indicates if the call was made by a managed caller, or the garbage collector.
		/// True indicates that someone called the Dispose method directly. False indicates that the garbage collector
		/// is finalizing the release of the object instance.</param>
		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				// No managed objects to release
			}

			// Always release unmanaged objects
			ClearStorage();
		}

		#region COM IDataObject Members

		#region COM constants

		private const int OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);

		private const int DV_E_FORMATETC = unchecked((int)0x80040064);
		private const int DV_E_TYMED = unchecked((int)0x80040069);
		private const int DV_E_CLIPFORMAT = unchecked((int)0x8004006A);
		private const int DV_E_DVASPECT = unchecked((int)0x8004006B);

		#endregion // COM constants

		#region Unsupported functions

		public int EnumDAdvise(out IEnumSTATDATA enumAdvise)
		{
			throw Marshal.GetExceptionForHR(OLE_E_ADVISENOTSUPPORTED);
		}

		public int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
		{
			formatOut = formatIn;
			return DV_E_FORMATETC;
		}

		#endregion // Unsupported functions

		/// <summary>
		/// Adds an advisory connection for the specified format.
		/// </summary>
		/// <param name="pFormatetc">The format for which this sink is called for changes.</param>
		/// <param name="advf">Advisory flags to specify callback behavior.</param>
		/// <param name="adviseSink">The IAdviseSink to call for this connection.</param>
		/// <param name="connection">Returns the new connection's ID.</param>
		/// <returns>An HRESULT.</returns>
		public int DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
		{
			// Check that the specified advisory flags are supported.
			const ADVF ADVF_ALLOWED = ADVF.ADVF_NODATA | ADVF.ADVF_ONLYONCE | ADVF.ADVF_PRIMEFIRST;
			if ((int)((advf | ADVF_ALLOWED) ^ ADVF_ALLOWED) != 0)
			{
				connection = 0;
				return OLE_E_ADVISENOTSUPPORTED;
			}

			// Create and insert an entry for the connection list
			AdviseEntry entry = new AdviseEntry(ref pFormatetc, advf, adviseSink);
			connections.Add(nextConnectionId, entry);
			connection = nextConnectionId;
			nextConnectionId++;

			// If the ADVF_PRIMEFIRST flag is specified and the data exists,
			// raise the DataChanged event now.
			if ((advf & ADVF.ADVF_PRIMEFIRST) == ADVF.ADVF_PRIMEFIRST)
			{
				if (GetDataEntry(ref pFormatetc, out KeyValuePair<FORMATETC, STGMEDIUM> dataEntry))
					RaiseDataChanged(connection, ref dataEntry);
			}

			// S_OK
			return 0;
		}

		/// <summary>
		/// Removes an advisory connection.
		/// </summary>
		/// <param name="connection">The connection id to remove.</param>
		public void DUnadvise(int connection)
		{
			connections.Remove(connection);
		}

		/// <summary>
		/// Gets an enumerator for the formats contained in this DataObject.
		/// </summary>
		/// <param name="direction">The direction of the data.</param>
		/// <returns>An instance of the IEnumFORMATETC interface.</returns>
		public IEnumFORMATETC EnumFormatEtc(DATADIR direction)
		{
			// We only support GET
			if (DATADIR.DATADIR_GET == direction)
				return new EnumFORMATETC(storage);

			throw new NotImplementedException("OLE_S_USEREG");
		}

		/// <summary>
		/// Gets the specified data.
		/// </summary>
		/// <param name="format">The requested data format.</param>
		/// <param name="medium">When the function returns, contains the requested data.</param>
		public void GetData(ref FORMATETC format, out STGMEDIUM medium)
		{
			medium = new STGMEDIUM();
			GetDataHere(ref format, ref medium);
		}

		/// <summary>
		/// Gets the specified data.
		/// </summary>
		/// <param name="format">The requested data format.</param>
		/// <param name="medium">When the function returns, contains the requested data.</param>
		/// <remarks>Differs from GetData only in that the STGMEDIUM storage is
		/// allocated and owned by the caller.</remarks>
		public void GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
		{
			// Locate the data
			if (GetDataEntry(ref format, out KeyValuePair<FORMATETC, STGMEDIUM> dataEntry))
			{
				STGMEDIUM source = dataEntry.Value;
				medium = CopyMedium(ref source);
				return;
			}

			//throw Marshal.GetExceptionForHR(DV_E_FORMATETC);
			// Didn't find it. Return an empty data medium.
			medium = new STGMEDIUM();
		}

		/// <summary>
		/// Determines if data of the requested format is present.
		/// </summary>
		/// <param name="format">The request data format.</param>
		/// <returns>Returns the status of the request. If the data is present, S_OK is returned.
		/// If the data is not present, an error code with the best guess as to the reason is returned.</returns>
		public int QueryGetData(ref FORMATETC format)
		{
			// We only support CONTENT aspect
			if ((DVASPECT.DVASPECT_CONTENT & format.dwAspect) == 0)
				return DV_E_DVASPECT;

			int ret = DV_E_TYMED;

			// Try to locate the data
			// TODO: The ret, if not S_OK, is only relevant to the last item
			foreach (KeyValuePair<FORMATETC, STGMEDIUM> pair in storage)
			{
				if ((pair.Key.tymed & format.tymed) > 0)
				{
					if (pair.Key.cfFormat == format.cfFormat)
					{
						// Found it, return S_OK;
						return 0;
					}
					else
					{
						// Found the medium type, but wrong format
						ret = DV_E_CLIPFORMAT;
					}
				}
				else
				{
					// Mismatch on medium type
					ret = DV_E_TYMED;
				}
			}

			return ret;
		}

		/// <summary>
		/// Sets data in the specified format into storage.
		/// </summary>
		/// <param name="formatIn">The format of the data.</param>
		/// <param name="medium">The data.</param>
		/// <param name="release">If true, ownership of the medium's memory will be transferred
		/// to this object. If false, a copy of the medium will be created and maintained, and
		/// the caller is responsible for the memory of the medium it provided.</param>
		public void SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
		{
			// If the format exists in our storage, remove it prior to resetting it
			foreach (KeyValuePair<FORMATETC, STGMEDIUM> pair in storage)
			{
				if ((pair.Key.tymed & formatIn.tymed) > 0
					 && pair.Key.dwAspect == formatIn.dwAspect
					 && pair.Key.cfFormat == formatIn.cfFormat)
				{
					STGMEDIUM releaseMedium = pair.Value;
					ReleaseStgMedium(ref releaseMedium);
					storage.Remove(pair);
					break;
				}
			}

			// If release is true, we'll take ownership of the medium.
			// If not, we'll make a copy of it.
			STGMEDIUM sm = medium;
			if (!release)
				sm = CopyMedium(ref medium);

			// Add it to the internal storage
			KeyValuePair<FORMATETC, STGMEDIUM> addPair = new KeyValuePair<FORMATETC, STGMEDIUM>(formatIn, sm);
			storage.Add(addPair);

			RaiseDataChanged(ref addPair);
		}

		/// <summary>
		/// Creates a copy of the STGMEDIUM structure.
		/// </summary>
		/// <param name="medium">The data to copy.</param>
		/// <returns>The copied data.</returns>
		private STGMEDIUM CopyMedium(ref STGMEDIUM medium)
		{
			STGMEDIUM sm = new STGMEDIUM();
			int hr = CopyStgMedium(ref medium, ref sm);
			if (hr != 0)
				throw Marshal.GetExceptionForHR(hr);

			return sm;
		}

		#endregion

		#region Helper methods

		/// <summary>
		/// Gets a data entry by the specified format.
		/// </summary>
		/// <param name="pFormatetc">The format to locate the data entry for.</param>
		/// <param name="dataEntry">The located data entry.</param>
		/// <returns>True if the data entry was found, otherwise False.</returns>
		private bool GetDataEntry(ref FORMATETC pFormatetc, out KeyValuePair<FORMATETC, STGMEDIUM> dataEntry)
		{
			foreach (KeyValuePair<FORMATETC, STGMEDIUM> entry in storage)
			{
				FORMATETC format = entry.Key;
				if (IsFormatCompatible(ref pFormatetc, ref format))
				{
					dataEntry = entry;
					return true;
				}
			}

			// Not found... default allocate the out param
			dataEntry = default(KeyValuePair<FORMATETC, STGMEDIUM>);
			return false;
		}

		/// <summary>
		/// Raises the DataChanged event for the specified connection.
		/// </summary>
		/// <param name="connection">The connection id.</param>
		/// <param name="dataEntry">The data entry for which to raise the event.</param>
		private void RaiseDataChanged(int connection, ref KeyValuePair<FORMATETC, STGMEDIUM> dataEntry)
		{
			AdviseEntry adviseEntry = connections[connection];
			FORMATETC format = dataEntry.Key;
			STGMEDIUM medium;
			if ((adviseEntry.advf & ADVF.ADVF_NODATA) != ADVF.ADVF_NODATA)
				medium = dataEntry.Value;
			else
				medium = default(STGMEDIUM);

			adviseEntry.sink.OnDataChange(ref format, ref medium);

			if ((adviseEntry.advf & ADVF.ADVF_ONLYONCE) == ADVF.ADVF_ONLYONCE)
				connections.Remove(connection);
		}

		/// <summary>
		/// Raises the DataChanged event for any advisory connections that
		/// are listening for it.
		/// </summary>
		/// <param name="dataEntry">The relevant data entry.</param>
		private void RaiseDataChanged(ref KeyValuePair<FORMATETC, STGMEDIUM> dataEntry)
		{
			foreach (KeyValuePair<int, AdviseEntry> connection in connections)
			{
				if (IsFormatCompatible(connection.Value.format, dataEntry.Key))
					RaiseDataChanged(connection.Key, ref dataEntry);
			}
		}

		/// <summary>
		/// Determines if the formats are compatible.
		/// </summary>
		/// <param name="format1">A FORMATETC.</param>
		/// <param name="format2">A FORMATETC.</param>
		/// <returns>True if the formats are compatible, otherwise False.</returns>
		/// <remarks>Compatible formats have the same DVASPECT, same format ID, and share
		/// at least one TYMED.</remarks>
		private bool IsFormatCompatible(FORMATETC format1, FORMATETC format2)
		{
			return IsFormatCompatible(ref format1, ref format2);
		}

		/// <summary>
		/// Determines if the formats are compatible.
		/// </summary>
		/// <param name="format1">A FORMATETC.</param>
		/// <param name="format2">A FORMATETC.</param>
		/// <returns>True if the formats are compatible, otherwise False.</returns>
		/// <remarks>Compatible formats have the same DVASPECT, same format ID, and share
		/// at least one TYMED.</remarks>
		private bool IsFormatCompatible(ref FORMATETC format1, ref FORMATETC format2)
		{
			return ((format1.tymed & format2.tymed) > 0
					  && format1.dwAspect == format2.dwAspect
					  && format1.cfFormat == format2.cfFormat);
		}

		#endregion // Helper methods

		#region EnumFORMATETC class

		/// <summary>
		/// Helps enumerate the formats available in our DataObject class.
		/// </summary>
		[ComVisible(true)]
		private class EnumFORMATETC : IEnumFORMATETC
		{
			// Keep an array of the formats for enumeration
			private FORMATETC[] formats;
			// The index of the next item
			private int currentIndex = 0;

			/// <summary>
			/// Creates an instance from a list of key value pairs.
			/// </summary>
			/// <param name="storage">List of FORMATETC/STGMEDIUM key value pairs</param>
			internal EnumFORMATETC(IList<KeyValuePair<FORMATETC, STGMEDIUM>> storage)
			{
				// Get the formats from the list
				formats = new FORMATETC[storage.Count];
				for (int i = 0; i < formats.Length; i++)
					formats[i] = storage[i].Key;
			}

			/// <summary>
			/// Creates an instance from an array of FORMATETC's.
			/// </summary>
			/// <param name="formats">Array of formats to enumerate.</param>
			private EnumFORMATETC(FORMATETC[] formats)
			{
				// Get the formats as a copy of the array
				this.formats = new FORMATETC[formats.Length];
				formats.CopyTo(this.formats, 0);
			}

			#region IEnumFORMATETC Members

			/// <summary>
			/// Creates a clone of this enumerator.
			/// </summary>
			/// <param name="newEnum">When this function returns, contains a new instance of IEnumFORMATETC.</param>
			public void Clone(out IEnumFORMATETC newEnum)
			{
				EnumFORMATETC ret = new EnumFORMATETC(formats)
				{
					currentIndex = currentIndex
				};
				newEnum = ret;
			}

			/// <summary>
			/// Retrieves the next elements from the enumeration.
			/// </summary>
			/// <param name="celt">The number of elements to retrieve.</param>
			/// <param name="rgelt">An array to receive the formats requested.</param>
			/// <param name="pceltFetched">An array to receive the number of element fetched.</param>
			/// <returns>If the fetched number of formats is the same as the requested number, S_OK is returned.
			/// There are several reasons S_FALSE may be returned: (1) The requested number of elements is less than
			/// or equal to zero. (2) The rgelt parameter equals null. (3) There are no more elements to enumerate.
			/// (4) The requested number of elements is greater than one and pceltFetched equals null or does not
			/// have at least one element in it. (5) The number of fetched elements is less than the number of
			/// requested elements.</returns>
			public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
			{
				// Start with zero fetched, in case we return early
				if (pceltFetched != null && pceltFetched.Length > 0)
					pceltFetched[0] = 0;

				// This will count down as we fetch elements
				int cReturn = celt;

				// Short circuit if they didn't request any elements, or didn't
				// provide room in the return array, or there are not more elements
				// to enumerate.
				if (celt <= 0 || rgelt == null || currentIndex >= formats.Length)
					return 1; // S_FALSE

				// If the number of requested elements is not one, then we must
				// be able to tell the caller how many elements were fetched.
				if ((pceltFetched == null || pceltFetched.Length < 1) && celt != 1)
					return 1; // S_FALSE

				// If the number of elements in the return array is too small, we
				// throw. This is not a likely scenario, hence the exception.
				if (rgelt.Length < celt)
					throw new ArgumentException("The number of elements in the return array is less than the number of elements requested");

				// Fetch the elements.
				for (int i = 0; currentIndex < formats.Length && cReturn > 0; i++, cReturn--, currentIndex++)
					rgelt[i] = formats[currentIndex];

				// Return the number of elements fetched
				if (pceltFetched != null && pceltFetched.Length > 0)
					pceltFetched[0] = celt - cReturn;

				// cReturn has the number of elements requested but not fetched.
				// It will be greater than zero, if multiple elements were requested
				// but we hit the end of the enumeration.
				return (cReturn == 0) ? 0 : 1; // S_OK : S_FALSE
			}

			/// <summary>
			/// Resets the state of enumeration.
			/// </summary>
			/// <returns>S_OK</returns>
			public int Reset()
			{
				currentIndex = 0;
				return 0; // S_OK
			}

			/// <summary>
			/// Skips the number of elements requested.
			/// </summary>
			/// <param name="celt">The number of elements to skip.</param>
			/// <returns>If there are not enough remaining elements to skip, returns S_FALSE. Otherwise, S_OK is returned.</returns>
			public int Skip(int celt)
			{
				if (currentIndex + celt > formats.Length)
					return 1; // S_FALSE

				currentIndex += celt;
				return 0; // S_OK
			}

			#endregion
		}

		#endregion // EnumFORMATETC class

		#region Extension

		#region DLL imports

		[DllImport("user32.dll")]
		private static extern uint RegisterClipboardFormat(string lpszFormatName);

		[DllImport("ole32.dll")]
		private static extern int CreateStreamOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease, out IStream ppstm);

		#endregion // DLL imports

		#region Native constants

		// CFSTR_DROPDESCRIPTION
		private const string DropDescriptionFormat = "DropDescription";

		#endregion // Native constants

		/// <summary>
		/// Sets the drop description for the drag image manager.
		/// </summary>
		/// <param name="dataObject">The DataObject to set.</param>
		/// <param name="dropDescription">The drop description.</param>
		public static void SetDropDescription(IDataObject dataObject, DropDescription dropDescription)
		{
			FillFormatETC(DropDescriptionFormat, TYMED.TYMED_HGLOBAL, out FORMATETC formatETC);

			// We need to set the drop description as an HGLOBAL.
			// Allocate space ...
			IntPtr pDD = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DropDescription)));
			try
			{
				// ... and marshal the data
				Marshal.StructureToPtr(dropDescription, pDD, false);

				// The medium wraps the HGLOBAL
				STGMEDIUM medium;
				medium.pUnkForRelease = null;
				medium.tymed = TYMED.TYMED_HGLOBAL;
				medium.unionmember = pDD;

				// Set the data
				dataObject.SetData(ref formatETC, ref medium, true);
			}
			catch
			{
				// If we failed, we need to free the HGLOBAL memory
				Marshal.FreeHGlobal(pDD);
				throw;
			}
		}

		public static void SetDropDescription(System.Windows.IDataObject dataObject, DropDescription dropDescription)
		{
			FillFormatETC("DropDescription", TYMED.TYMED_HGLOBAL, out FORMATETC formatETC);
			IntPtr num = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DropDescription)));
			try
			{
				Marshal.StructureToPtr((object)dropDescription, num, false);
				STGMEDIUM medium;
				medium.pUnkForRelease = (object)null;
				medium.tymed = TYMED.TYMED_HGLOBAL;
				medium.unionmember = num;
				((System.Runtime.InteropServices.ComTypes.IDataObject)dataObject).SetData(ref formatETC, ref medium, true);
			}
			catch
			{
				Marshal.FreeHGlobal(num);
				throw;
			}
			if (dropDescription.szMessage != null)
				SetBoolFormat(dataObject, "IsShowingText", true);
		}

		public static void SetDropDescription(System.Windows.IDataObject dataObject, DropImageType type, string format, string insert)
		{
			if (format != null && format.Length > 259)
				throw new ArgumentException("Format string exceeds the maximum allowed length of 259.", "format");
			if (insert != null && insert.Length > 259)
				throw new ArgumentException("Insert string exceeds the maximum allowed length of 259.", "insert");
			DropDescription dropDescription;
			dropDescription.type = type;
			dropDescription.szMessage = format;
			dropDescription.szInsert = insert;
			SetDropDescription(dataObject, dropDescription);
		}


		/// <summary>
		/// Gets an IntPtr from data acquired from a data object.
		/// </summary>
		/// <param name="data">The data that contains the IntPtr.</param>
		/// <returns>An IntPtr.</returns>
		private static IntPtr GetIntPtrFromData(object data)
		{
			byte[] buf = null;

			if (data is MemoryStream)
			{
				buf = new byte[4];
				if (4 != ((MemoryStream)data).Read(buf, 0, 4))
					throw new ArgumentException("Could not read an IntPtr from the MemoryStream");
			}
			if (data is byte[])
			{
				buf = (byte[])data;
				if (buf.Length < 4)
					throw new ArgumentException("Could not read an IntPtr from the byte array");
			}

			if (buf == null)
				throw new ArgumentException("Could not read an IntPtr from the " + data.GetType().ToString());

			int p = (buf[3] << 24) | (buf[2] << 16) | (buf[1] << 8) | buf[0];
			return new IntPtr(p);
		}

		// This is WM_USER + 3. The window controlled by the drag image manager
		// looks for this message (WM_USER + 2 seems to work, too) to invalidate
		// the drag image.
		private const uint WM_INVALIDATEDRAGIMAGE = 0x403;

		/// <summary>
		/// Invalidates the drag image.
		/// </summary>
		/// <param name="dataObject">The data object for which to invalidate the drag image.</param>
		/// <remarks>This call tells the drag image manager to reformat the internal
		/// cached drag image, based on the already set drag image bitmap and current drop
		/// description.</remarks>
		public static void InvalidateDragImage(System.Windows.IDataObject dataObject)
		{

			if (dataObject.GetDataPresent("DragWindow"))
			{
				IntPtr hwnd = GetIntPtrFromData(dataObject.GetData("DragWindow"));
				PostMessage(hwnd, WM_INVALIDATEDRAGIMAGE, IntPtr.Zero, IntPtr.Zero);
			}
		}



		private static void SetBoolFormat(System.Windows.IDataObject dataObject, string format, bool val)
		{
			FillFormatETC(format, TYMED.TYMED_HGLOBAL, out FORMATETC formatETC);
			IntPtr num = Marshal.AllocHGlobal(4);
			try
			{
				Marshal.Copy(BitConverter.GetBytes(val ? 1 : 0), 0, num, 4);
				STGMEDIUM medium;
				medium.pUnkForRelease = (object)null;
				medium.tymed = TYMED.TYMED_HGLOBAL;
				medium.unionmember = num;
				((System.Runtime.InteropServices.ComTypes.IDataObject)dataObject).SetData(ref formatETC, ref medium, true);
			}
			catch
			{
				Marshal.FreeHGlobal(num);
				throw;
			}
		}

		/// <summary>
		/// Gets the DropDescription format data.
		/// </summary>
		/// <param name="dataObject">The DataObject.</param>
		/// <returns>The DropDescription, if set.</returns>
		public static object GetDropDescription(IDataObject dataObject)
		{
			FillFormatETC(DropDescriptionFormat, TYMED.TYMED_HGLOBAL, out FORMATETC formatETC);

			if (0 == dataObject.QueryGetData(ref formatETC))
			{
				dataObject.GetData(ref formatETC, out STGMEDIUM medium);
				try
				{
					return (DropDescription)Marshal.PtrToStructure(medium.unionmember, typeof(DropDescription));
				}
				finally
				{
					ReleaseStgMedium(ref medium);
				}
			}

			return null;
		}

		// Combination of all non-null TYMEDs
		private const TYMED TYMED_ANY =
			 TYMED.TYMED_ENHMF
			 | TYMED.TYMED_FILE
			 | TYMED.TYMED_GDI
			 | TYMED.TYMED_HGLOBAL
			 | TYMED.TYMED_ISTORAGE
			 | TYMED.TYMED_ISTREAM
			 | TYMED.TYMED_MFPICT;

		/// <summary>
		/// Sets up an advisory connection to the data object.
		/// </summary>
		/// <param name="dataObject">The data object on which to set the advisory connection.</param>
		/// <param name="sink">The advisory sink.</param>
		/// <param name="format">The format on which to callback on.</param>
		/// <param name="advf">Advisory flags. Can be 0.</param>
		/// <returns>The ID of the newly created advisory connection.</returns>
		public static int Advise(IDataObject dataObject, IAdviseSink sink, string format, ADVF advf)
		{
			// Internally, we'll listen for any TYMED
			FillFormatETC(format, TYMED_ANY, out FORMATETC formatETC);

			int hr = dataObject.DAdvise(ref formatETC, advf, sink, out int connection);
			if (hr != 0)
				Marshal.ThrowExceptionForHR(hr);
			return connection;
		}

		/// <summary>
		/// Fills a FORMATETC structure.
		/// </summary>
		/// <param name="format">The format name.</param>
		/// <param name="tymed">The accepted TYMED.</param>
		/// <param name="formatETC">The structure to fill.</param>
		private static void FillFormatETC(string format, TYMED tymed, out FORMATETC formatETC)
		{
			formatETC.cfFormat = (short)RegisterClipboardFormat(format);
			formatETC.dwAspect = DVASPECT.DVASPECT_CONTENT;
			formatETC.lindex = -1;
			formatETC.ptd = IntPtr.Zero;
			formatETC.tymed = tymed;
		}

		// Identifies data that we need to do custom marshaling on
		private static readonly Guid ManagedDataStamp = new Guid("D98D9FD6-FA46-4716-A769-F3451DFBE4B4");

		/// <summary>
		/// Sets managed data to a clipboard DataObject.
		/// </summary>
		/// <param name="dataObject">The DataObject to set the data on.</param>
		/// <param name="format">The clipboard format.</param>
		/// <param name="data">The data object.</param>
		/// <remarks>
		/// Because the underlying data store is not storing managed objects, but
		/// unmanaged ones, this function provides intelligent conversion, allowing
		/// you to set unmanaged data into the COM implemented IDataObject.</remarks>
		public void SetManagedData(string format, object data)
		{
			// Initialize the format structure
			FillFormatETC(format, TYMED.TYMED_HGLOBAL, out FORMATETC formatETC);

			// Serialize/marshal our data into an unmanaged medium
			GetMediumFromObject(data, out STGMEDIUM medium);
			try
			{
				// Set the data on our data object
				SetData(ref formatETC, ref medium, true);
			}
			catch
			{
				// On exceptions, release the medium
				ReleaseStgMedium(ref medium);
				throw;
			}
		}

		#region Helper methods

		/// <summary>
		/// Serializes managed data to an HGLOBAL.
		/// </summary>
		/// <param name="data">The managed data object.</param>
		/// <returns>An STGMEDIUM pointing to the allocated HGLOBAL.</returns>
		private static void GetMediumFromObject(object data, out STGMEDIUM medium)
		{
			// We'll serialize to a managed stream temporarily
			MemoryStream stream = new MemoryStream();

			// Write an indentifying stamp, so we can recognize this as custom
			// marshaled data.
			stream.Write(ManagedDataStamp.ToByteArray(), 0, Marshal.SizeOf(typeof(Guid)));

			// Now serialize the data. Note, if the data is not directly serializable,
			// we'll try type conversion. Also, we serialize the type. That way,
			// during deserialization, we know which type to convert back to, if
			// appropriate.
			BinaryFormatter formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // I don't think use of this serializer could be avoided in COM context 
            formatter.Serialize(stream, data.GetType());
            formatter.Serialize(stream, GetAsSerializable(data));
#pragma warning restore SYSLIB0011 // Type or member is obsolete

			// Now copy to an HGLOBAL
			byte[] bytes = stream.GetBuffer();
			IntPtr p = Marshal.AllocHGlobal(bytes.Length);
			try
			{
				Marshal.Copy(bytes, 0, p, bytes.Length);
			}
			catch
			{
				// Make sure to free the memory on exceptions
				Marshal.FreeHGlobal(p);
				throw;
			}

			// Now allocate an STGMEDIUM to wrap the HGLOBAL
			medium.unionmember = p;
			medium.tymed = TYMED.TYMED_HGLOBAL;
			medium.pUnkForRelease = null;
		}

		/// <summary>
		/// Gets a serializable object representing the data.
		/// </summary>
		/// <param name="obj">The data.</param>
		/// <returns>If the data is serializable, then it is returned. Otherwise,
		/// type conversion is attempted. If successful, a string value will be
		/// returned.</returns>
		private static object GetAsSerializable(object obj)
		{
			// If the data is directly serializable, run with it
			if (obj.GetType().IsSerializable)
				return obj;

			// Attempt type conversion to a string, but only if we know it can be converted back
			TypeConverter conv = GetTypeConverterForType(obj.GetType());
			if (conv != null && conv.CanConvertTo(typeof(string)) && conv.CanConvertFrom(typeof(string)))
				return conv.ConvertToInvariantString(obj);

			throw new NotSupportedException("Cannot serialize the object");
		}

		/// <summary>
		/// Gets a TypeConverter instance for the specified type.
		/// </summary>
		/// <param name="dataType">The type.</param>
		/// <returns>An instance of a TypeConverter for the type, if one exists.</returns>
		private static TypeConverter GetTypeConverterForType(Type dataType)
		{
			TypeConverterAttribute[] typeConverterAttrs = (TypeConverterAttribute[])dataType.GetCustomAttributes(typeof(TypeConverterAttribute), true);
			if (typeConverterAttrs.Length > 0)
			{
				Type convType = Type.GetType(typeConverterAttrs[0].ConverterTypeName);
				return (TypeConverter)Activator.CreateInstance(convType);
			}

			return null;
		}

		#endregion // Helper methods

		#endregion
	}

}