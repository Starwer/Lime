/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-03-2018
* Copyright :   Sebastien Mouy © 2018 
**************************************************************************/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;

namespace Lime
{

	// --------------------------------------------------------------------------------------------------
	/// <summary>
	/// Describe an action being applied on a DataObjectCompatible
	/// </summary>
	public enum DataObjectAction
	{
		/// <summary>
		/// No action
		/// </summary>
		None = 0x00,

		/// <summary>
		/// Enable to copy
		/// </summary>
		Copy = 0x01,

		/// <summary>
		/// Enable to move
		/// </summary>
		Move = 0x02,

		/// <summary>
		/// Enable to link
		/// </summary>
		Link = 0x04,

		/// <summary>
		/// Enable to show drop-menu
		/// </summary>
		Menu = 0x08,

		/// <summary>
		/// Enable to show drop-menu
		/// </summary>
		Open = 0x10
	}


	/// <summary>
	/// Describe the direction of an action being applied on a DataObjectCompatible 
	/// </summary>
	public enum DataObjectDirection
	{
		/// <summary>
		/// No action
		/// </summary>
		None = 0,

		/// <summary>
		/// Source of the action
		/// </summary>
		From = 1,

		/// <summary>
		/// destination of the action
		/// </summary>
		To = 2
	}

	/// <summary>
	///  Describe the sytem triggering an action being applied on a DataObjectCompatible 
	/// </summary>
	public enum DataObjectMethod
	{
		/// <summary>
		/// No action
		/// </summary>
		None = 0,

		/// <summary>
		/// Interface: Clipboard
		/// </summary>
		Clipboard = 1,

		/// <summary>
		/// Interface: Drag & Drop
		/// </summary>
		DragDrop = 2
	}



	// --------------------------------------------------------------------------------------------------
	/// <summary>
	/// Data involved during a DataObjectCompatible action (drag & drop or clipboard actions).
	/// </summary>
	public class DataObjectCompatibleEventArgs : ICloneable
	{

		#region Fields/properties

		/// <summary>
		/// Describe the action being done on the actual DataObjectCompatible
		/// </summary>
		public DataObjectAction Action = DataObjectAction.None;

		/// <summary>
		/// Describe the direction of an action being applied on a DataObjectCompatible 
		/// </summary>
		public DataObjectDirection Direction = DataObjectDirection.None;

		/// <summary>
		///  Describe the sytem triggering an action being applied on a DataObjectCompatible 
		/// </summary>
		public readonly DataObjectMethod Method = DataObjectMethod.None;

		/// <summary>
		/// Data representing the source (typically an element being dragged or copied/cut with the clipboard)
		/// </summary>
		public readonly object Source = null;

		/// <summary>
		/// IDataObject representation of the source
		/// </summary>
		public readonly IDataObject Data = null;

		/// <summary>
		/// Data representing the parent of the source (typically the DataContext of an ItemsControl)
		/// </summary>
		public readonly object SourceParent = null;

		/// <summary>
		/// Data representing the collection where the source is (typically the ItemsControl.ItemsSource)
		/// </summary>
		public readonly IEnumerable SourceCollection = null;

		/// <summary>
		/// Index in the SourceCollection where the action has been initiated (-1 if not applicable)
		/// </summary>
		public int SourceIndex { get; private set; } = -1;

		/// <summary>
		/// Data representing the destination (typically the DataContext of an ItemsControl)
		/// </summary>
		public readonly object Destination = null;

		/// <summary>
		/// Index in the destination to which the action should be applied (like move to that index in destination collection)
		/// </summary>
		public readonly int DestinationIndex = -1;

		/// <summary>
		/// Destination name to be shown when draging over
		/// </summary>
		public string DestinationName = null;

		/// <summary>
		/// Set this flag to indicate that the requested action is valid and/or has been performed.
		/// </summary>
		public bool Handled = false;

		/// <summary>
		/// Set this flag to indicate that the source of the action has already been handled in the destination callback.
		/// </summary>
		public bool SourceHandled = false;

		#endregion

		#region construtor

		/// <summary>
		/// Construct by copy
		/// </summary>
		/// <param name="copy">object to clone</param>
		public DataObjectCompatibleEventArgs(DataObjectCompatibleEventArgs copy)
		{
			Action = copy.Action;
			Direction = copy.Direction;
			Method = copy.Method;
			Source = copy.Source;
			Data = copy.Data;
			SourceParent = copy.SourceParent;
			SourceCollection = copy.SourceCollection;
			SourceIndex = copy.SourceIndex;
			Destination = copy.Destination;
			DestinationIndex = copy.DestinationIndex;
			DestinationName = copy.DestinationName;
			Handled = copy.Handled;
			SourceHandled = copy.SourceHandled;
		}

		/// <summary>
		/// Construct from a source
		/// </summary>
		/// <param name="method">sytem triggering an action being applied on a DataObjectCompatible</param>
		/// <param name="action">action being done on the actual DataObjectCompatible</param>
		/// <param name="source">Data representing the source</param>
		/// <param name="data">IDataObject representation of the source</param>
		/// <param name="sourceParent">Data representing the parent of the source</param>
		/// <param name="sourceCollection">Data representing the collection where the source is</param>
		/// <param name="sourceIndex">Index in the SourceCollection where the action has been initiated (-1 if not applicable)</param>
		public DataObjectCompatibleEventArgs(
			DataObjectMethod method,
			DataObjectAction action,
			object source,
			IDataObject data,
			object sourceParent,
			IEnumerable sourceCollection,
			int sourceIndex
			)
		{
			Direction = DataObjectDirection.From;
			Action = action;
			Method = method;
			Source = source;
			Data = data;
			SourceParent = sourceParent;
			SourceCollection = sourceCollection;
			SourceIndex = sourceIndex;
		}

		/// <summary>
		/// Constrcut from source and destination
		/// </summary>
		/// <param name="copy">object to clone (typically representing a source)</param>
		/// <param name="action">action being done on the actual DataObjectCompatible</param>
		/// <param name="destination">Data representing the destination</param>
		/// <param name="destinationIndex">Index in the destination to which the action should be applied</param>
		/// <param name="destinationName">Destination name to be shown when draging over</param>
		public DataObjectCompatibleEventArgs(
			DataObjectCompatibleEventArgs copy,
			DataObjectAction action,
			object destination,
			int destinationIndex,
			string destinationName
			) : this(copy)
		{
			Direction = DataObjectDirection.To;
			Action = action;
			Destination = destination;
			DestinationIndex = destinationIndex;
			DestinationName = destinationName;
			Handled = false;
			SourceHandled = false;
		}

		#endregion



		#region IClonable implementation


		/// <summary>
		/// Clone the object
		/// </summary>
		/// <returns>Clone of the object</returns>
		public object Clone()
		{
			return new DataObjectCompatibleEventArgs(this);
		}

		#endregion



		#region Methods

		/// <summary>
		/// Automatically handle the action applied to a <see cref="ICollection"/> destination.
		/// This update the DataObjectCompatibleEventArgs object accordingly.
		/// </summary>
		/// <typeparam name="T">Type of the items in the destination collection</typeparam>
		/// <param name="destination"><see cref="ICollection"/> destination of the DataObjectCompatibleEventArgs</param>
		/// <returns>true if the action has actually been handled and performed.</returns>
		public bool DoOnCollection(ICollection destination)
		{
			if (Handled || destination == null) return false;

			if (Direction == DataObjectDirection.To)
			{
				if (DestinationIndex >= 0)
				{
					// Special treatment for Observable collection: try to use Move method
					if (Action == DataObjectAction.Move &&
						SourceIndex >= 0 &&
						SourceCollection == (object)destination &&
						SourceCollection is INotifyCollectionChanged)
					{
						// Because ObservableCollection<> has no interface IObservableCollection and there is 
						// not such pattern matching for generics yet, we need to use Reflection to handle that case
						var move = SourceCollection.GetType().GetMethod("Move", new Type[] { typeof(int), typeof(int) });
						if (move != null)
						{
							move.Invoke(SourceCollection, new object[] { SourceIndex, DestinationIndex });
							Handled = true;
							SourceHandled = true;
							return true;
						}
					}

					if (destination is IList lst)
					{
						if (Action == DataObjectAction.Move)
						{
							lst.Insert(DestinationIndex, Source);
							Handled = true;
						}
						else if (Action == DataObjectAction.Copy && Source is ICloneable iclone)
						{
							lst.Insert(DestinationIndex, iclone.Clone());
							Handled = true;
						}

						if (Handled)
						{
							// Adjust source index if we inserted an element in the same collection
							if (SourceCollection == (object)destination && DestinationIndex <= SourceIndex)
							{
								SourceIndex++;
							}
						}
					}

				}
			}
			else if (Direction == DataObjectDirection.From)
			{
				if (SourceIndex >= 0)
				{
					if (SourceCollection is IList lst)
					{
						if (Action == DataObjectAction.Move)
						{
							lst.RemoveAt(SourceIndex);
						}

						// Nothing more to do in most cases
						Handled = true;
					}
				}
			}

			return Handled;
		}

		#endregion

	}



	// --------------------------------------------------------------------------------------------------
	/// <summary>
	/// Describe a Class that can be easily converted from/to an ObjectData to be used in
	/// standard clipboard (copy/paste) and drag-and-drop operations.
	/// </summary>
	public interface IDataObjectCompatible
	{

		/// <summary>
		/// Retrieve a clipboard-compatible DataObject representation on the actual object
		/// </summary>
		/// <param name="method">Method requesting the DataObject</param>
		/// <returns>a clipboard compatible DataObject</returns>
		IDataObject GetDataObject(DataObjectMethod method);

		/// <summary>
		/// Defines whether the actual object can handle the given action
		/// </summary>
		/// <param name="e">DataObject event representation</param>
		void DataObjectCanDo(DataObjectCompatibleEventArgs e);

		/// <summary>
		/// Request the object to take action
		/// </summary>
		/// <param name="e">DataObject event reauest</param>
		void DataObjectDo(DataObjectCompatibleEventArgs e);
	}
}
