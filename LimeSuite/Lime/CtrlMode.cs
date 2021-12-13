/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     09-12-2016
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;

namespace Lime
{

    
    /// <summary>
    /// Define the different modes of control for the User Interface
    /// </summary>
	[Flags]
    public enum CtrlMode
    {
		/// <summary>
		/// No control mode selected
		/// </summary>
		None = 0x0,

		/// <summary>
		/// Mouse and pointing devices
		/// </summary>
		Mouse = 0x1,

        /// <summary>
        /// Touch-screen
        /// </summary>
        Touch = 0x2,

        /// <summary>
        /// Key inputs
        /// </summary>
        Keyboard = 0x4,

        /// <summary>
        /// Command Line Interface
        /// </summary>
        CLI = 0x8
    }


}
