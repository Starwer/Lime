/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     22-02-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System.Windows.Controls;

namespace LimeLauncher.Controls
{

	/// <summary>
	/// Pseudo type: inherit fully from LimeListView.
	/// This enables to set different styles for LimeListView and LimeComposite.
	/// </summary>
	public class LimeComposite : LimeListView
    {
        public LimeComposite() : base()
        {
            Grid.SetIsSharedSizeScope(wxMain, true);
        }
    }
}
