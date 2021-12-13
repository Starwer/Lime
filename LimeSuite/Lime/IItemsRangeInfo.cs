/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     11-03-2018
* Copyright :   Sebastien Mouy © 2019  
**************************************************************************/

using System;
using System.Collections.Generic;

namespace Lime
{

	public class ItemIndexRange
	{
		public ItemIndexRange(int firstIndex, uint length)
		{
			FirstIndex = firstIndex;
			LastIndex = firstIndex + (int)length - 1;
			Length = length;
		}

		public int FirstIndex { get; private set; }

		public int LastIndex { get; private set; }

		public uint Length { get; private set; }
	}


	/// <summary>
	/// Provides info about a range of items in the data source.
	/// </summary>
	public interface IItemsRangeInfo : IDisposable
	{
		/// <summary>
		/// Releases system resources that are exposed by a Windows Runtime object.
		/// </summary>
		void Close();

		/// <summary>
		/// Updates the ranges of items in the data source that are visible in the list control and that are tracked in the instance of the object that implements the <see cref="IItemsRangeInfo"/> interface.
		/// </summary>
		/// <param name="visibleRange">The updated range of items in the data source that are visible in the list control.</param>
		/// <param name="trackedItems">The updated collection of ranges of items in the data source that are tracked in the instance of the object that implements the <see cref="IItemsRangeInfo"/> interface.</param>
		void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems);
	}


	/// <summary>
	/// Manages whether items and ranges of items in the data source are selected in the list control.
	/// </summary>
	public interface ISelectionInfo
	{
		/// <summary>
		/// Marks the items in the data source specified by itemIndexRange as not selected in the list control.
		/// </summary>
		/// <param name="itemIndexRange">A range of items in the data source.</param>
		void DeselectRange(ItemIndexRange itemIndexRange);

		/// <summary>
		/// Marks the items in the data source specified by itemIndexRange as selected in the list control.
		/// </summary>
		/// <param name="itemIndexRange">A range of items in the data source.</param>
		void SelectRange(ItemIndexRange itemIndexRange);

		/// <summary>
		/// Marks the item in the data source specified by the index as having the logical focus 
		/// </summary>
		/// <param name="index">index in the data source</param>
		void SetFocus(int index);

		/// <summary>
		/// Marks the item in the data source specified by the index as losing the logical focus 
		/// </summary>
		/// <param name="index">index in the data source</param>
		void ClearFocus(int index);
	}


}
