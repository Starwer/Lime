/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/


using System;
using System.Windows;
using System.Windows.Threading;

namespace WPFhelper
{
	
    // --------------------------------------------------------------------------------------------------
    #region Extension Methods

    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate() { };

        /// <summary>
        /// Refresh (the layout) of a WPF UI element.
        /// Credit: Muljadi Budiman
        /// URL: http://geekswithblogs.net/NewThingsILearned/archive/2008/08/25/refresh--update-wpf-controls.aspx
        /// </summary>
        /// <param name="uiElement"></param>
        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }

    }

    #endregion

}