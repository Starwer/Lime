/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     13-09-2018
* Copyright :   Sebastien Mouy © 2018 
**************************************************************************/


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Lime
{
	/// <summary>
	/// Describe the Options which can be applied for a replacement
	/// </summary>
	[Flags]
	public enum ReplacerOptions
	{
		/// <summary>
		/// Specifies that no options are set.
		/// </summary>
		Replace = 0,

		/// <summary>
		/// Use regular expression
		/// </summary>
		Regex = 1,

		/// <summary>
		/// Specifies case-insensitive matching.
		/// </summary>		
		IgnoreCase = 2,

		/// <summary>
		/// Replace all occurances
		/// </summary>
		All = 4
	}


	/// <summary>
	/// Define rules to replace strings
	/// </summary>
	public class Replacer : INotifyPropertyChangedWeak
	{

		// --------------------------------------------------------------------------------------------------
		#region Boilerplate INotifyPropertyChangedWeak

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
		#region Observable properties 
		
		/// <summary>
		/// Pattern for the replacement
		/// </summary>
		public string Pattern
		{
			get { return _Pattern; }
			set
			{
				if (value != _Pattern)
				{
					_Pattern = value;
					Regexp = null;
					OnPropertyChanged();
				}
			}
		}
		private string _Pattern = null;

		/// <summary>
		/// Replacement string
		/// </summary>
		public string Replacement
		{
			get { return _Replacement; }
			set
			{
				if (value != _Replacement)
				{
					_Replacement = value;
					OnPropertyChanged();
				}
			}
		}
		private string _Replacement = null;

		/// <summary>
		/// Options for the replacement
		/// </summary>
		public ReplacerOptions Options
		{
			get { return _Options; }
			set
			{
				if (value != _Options)
				{
					_Options = value;
					Regexp = null;
					OnPropertyChanged();
				}
			}
		}
		private ReplacerOptions _Options = ReplacerOptions.Replace;

		#endregion

		// --------------------------------------------------------------------------------------------------
		#region Fields

		/// <summary>
		/// Keep the regexp (cached)
		/// </summary>
		private Regex Regexp = null;

		#endregion

		// --------------------------------------------------------------------------------------------------
		#region ctors

		/// <summary>
		/// Parameterless contrusctor
		/// </summary>
		public Replacer()
		{
		}

		/// <summary>
		/// Construct a Replacer by specifying its pattern.
		/// </summary>
		/// <param name="pattern">pattern for replacement</param>
		/// <param name="replacement">replacement string</param>
		/// <param name="options">Replace options</param>
		public Replacer(string pattern , string replacement = null, ReplacerOptions options = ReplacerOptions.Replace)
		{
			Pattern = pattern;
			Replacement = replacement;
			Options = options;
		}

		#endregion

			
		// --------------------------------------------------------------------------------------------------
		#region Methods

		/// <summary>
		/// Apply the replacement on a string
		/// </summary>
		/// <param name="str">input to the replacement</param>
		/// <returns>output of string after replacement</returns>
		public string Replace(string str)
		{
			if (string.IsNullOrEmpty(Pattern) || str == null) return str;

			if (Regexp == null)
			{
				var opts = RegexOptions.Compiled | RegexOptions.Multiline;

				if (Options.HasFlag(ReplacerOptions.IgnoreCase)) opts |= RegexOptions.IgnoreCase;
				Regexp = new Regex(Pattern, opts);

			}

			if (!Options.HasFlag(ReplacerOptions.Regex))
			{
				str = Regex.Escape(str);
			}

			var ret = Options.HasFlag(ReplacerOptions.All) ?
				Regexp.Replace(str, Replacement) : Regexp.Replace(str, Replacement, 1);

			return ret;
		}

		#endregion

	}


	/// <summary>
	/// Create a chain of string replacement
	/// </summary>
	public class ReplacerChain : ObservableCollection<Replacer>
	{
		/// <summary>
		/// Apply the replacement on a string
		/// </summary>
		/// <param name="str">input to the replacement</param>
		/// <returns>output of string after replacement</returns>
		public string Replace(string str)
		{
			if (str == null) return str;

			try
			{
				foreach (var repl in this)
				{
					if (repl != null)
					{
						str = repl.Replace(str);
					}
				}
			}
			catch
			{
				// return string as-is
			}

			return str;
		}

	}


}
