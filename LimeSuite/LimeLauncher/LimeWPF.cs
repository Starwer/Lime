/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     05-09-2016
* Copyright :   Sebastien Mouy © 2017
**************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using Lime;
using LimeLauncher.Controls;

namespace LimeLauncher
{


    // --------------------------------------------------------------------------------------------------
    #region Types

    /// <summary>
    /// Provides data for the Lime Control Event
    /// </summary>
    public class LimeControlEventArgs : EventArgs
    {
        /// <summary>
        /// Reporting source (Lime Control) of the event
        /// </summary>
        public FrameworkElement Source;

        /// <summary>
        /// DataContext of the source
        /// </summary>
        public object Context;

        /// <summary>
        /// New value of the DataContext
        /// </summary>
        public object Value;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Source">Reporting source (Lime Control) of the event</param>
        /// <param name="Context">DataContext of the source</param>
        /// <param name="Value">New value of the DataContext</param>
        public LimeControlEventArgs(FrameworkElement Source = null, object Context = null, object Value = null)
        {
            this.Source = Source;
            this.Context = Context;
            this.Value = Value;
        }
    }



    #endregion


    // --------------------------------------------------------------------------------------------------
    #region Translate

    /// <summary>
    /// LimeLanguage Translate function accessible from WPF. Translate object of any type. See also: TranslateExtension.
    /// <br/>Usage in Xaml: <Button Content="{Binding Converter={StaticResource TranslateConverter}, ConverterParameter='Hello'} " />
    /// <br/>Usage in Xaml: <Button Content="{Binding Converter={StaticResource TranslateConverter}, Path=ObjString} " />
    /// </summary>
    public class TranslateConverter : IValueConverter
    {

        private const string LanguageSection = "Translator";


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // obtain the converter to string
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(string));
            if (parameter != null) value = parameter;
            string key;

            // Auto-convert from any type to string
            try
            {
                // determine if the supplied value is of a suitable type
                if (converter.CanConvertFrom(value.GetType()))
                {
                    // return the converted value
                    key = (string)converter.ConvertFrom(value);
                }
                else
                {
                    // try to convert from the string representation
                    key = (string)converter.ConvertFrom(value.ToString());
                }
            }
            catch (Exception)
            {
                key = (string)value;
            }

            return LimeLanguage.Translate(LanguageSection, key, key);

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// LimeLanguage Translate function accessible from WPF. Translate a constant string.
    /// <br/>Usage in Xaml: <Button Content="{l:Translate Hello} " />
    /// <br/>Usage in Xaml: <Button Content="{l:Translate Key=Hello, Section=Translator, Format='\{0\}.name'} " />
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class TranslateExtension : MarkupExtension
    {

        public TranslateExtension()
        {
        }

        public TranslateExtension(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Language Section (default: "Translator")
        /// </summary>
        public string Section { get; set; } = "Translator";

        /// <summary>
        /// Base Key to be translated
        /// </summary>
        public string Key { get; set; } = null;

        /// <summary>
        /// Format used to form the value-key. {0} maps to Key. Default: take Key as value-key.
        /// </summary>
        public string Format { get; set; } = null;


        /// <summary>
        /// Provide the Translated key
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns>ItemsSource as a array of ItemsSourceType</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key)) return "";

            string keyval = Format != null ? string.Format(Format, Key) : Key;
            string ret = LimeLanguage.Translate(Section, keyval, keyval);
            return ret;
        }

    }

    #endregion


    // --------------------------------------------------------------------------------------------------
    #region LimeProperty & LimeCommand

    /// <summary>
    /// Dedicated Validation procedure for the LimeProperty types
    /// </summary>
    public class LimePropertyValidationRule : ValidationRule
    {
        /// <summary>
        /// This must be set to the LimeProperty to be validated
        /// </summary>
        public LimeProperty Source { get; set; } = null;

        /// <summary>
        /// Use the LimeProperty CanConvertFrom method to validate the data content
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var prop = Source;

            if (prop != null && !prop.CanConvertFrom(value))
            {
                if (prop.AllowEmpty && value is string str && str == "")
                {
                    return ValidationResult.ValidResult;
                }
                
                LimeMsg.Debug("LimePropertyValidationRule: Parameter Error from {0} to {1}", value, prop.Ident);
                return new ValidationResult(false, "Parameter error");
            }

            // Check Range
            if (prop != null && prop.Maximum>prop.Minimum && value is string)
            {
				// Handle percents
				if (double.TryParse(value as string, out double val))
				{
					var min = prop.Minimum;
					var max = prop.Maximum;
					if (prop.Percentage)
					{
						min *= 100;
						max *= 100;
					}

					if (val < min || val > max)
					{
						LimeMsg.Debug("LimePropertyValidationRule: Range Error from {0} to {1} (range {2}..{3})",
							value, prop.Ident, min, max);
						return new ValidationResult(false, "Range error");
					}
				}
			}

            return ValidationResult.ValidResult;
        }
    }
    

    /// <summary>
    /// Convert a LimeProperty to its CLI representation (string).
    /// </summary>
    public class LimeProperty2CliConverter : IValueConverter
    {
        // Define formats

        public static string Get(object value)
        {
            string ret;

            if (value is LimeProperty prop)
            {
                ret = prop.Ident;
                if (!prop.ReadOnly) ret += "=" + prop.Serialize;
            }
            else
            {
                throw new NotImplementedException();
            }

            return ret;
        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Get(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion


    // --------------------------------------------------------------------------------------------------
    #region WPFFont

    /// <summary>
    /// Represent a WPF Font as one object
    /// </summary>
    public class WPFfont : StringConvertible, INotifyPropertyChanged
    {

        // --------------------------------------------------------------------------------------------------
        #region Boilerplate INotifyPropertyChanged

        // Boilerplate code for INotifyPropertyChanged : Instances

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        // --------------------------------------------------------------------------------------------------
        #region Observable properties
        
        /// <summary>
        /// Family of the font
        /// </summary>
        public FontFamily Family
        {
            get { return _Family; }
            set
            {
                if (value != _Family)
                {
                    _Family = value;
                    OnPropertyChanged();
                }
            }
        }
        private FontFamily _Family = null;

        /// <summary>
        /// Weight of the font
        /// </summary>
        public FontWeight Weight
        {
            get { return _Weight; }
            set
            {
                if (value != _Weight)
                {
                    _Weight = value;
                    OnPropertyChanged();
                }
            }
        }
        private FontWeight _Weight;

        /// <summary>
        /// Style of the font
        /// </summary>
        public FontStyle Style
        {
            get { return _Style; }
            set
            {
                if (value != _Style)
                {
                    _Style = value;
                    OnPropertyChanged();
                }
            }
        }
        private FontStyle _Style;

        #endregion

		// --------------------------------------------------------------------------------------------------
		#region ctors

		/// <summary>
		/// Create default WPFfont
		/// </summary>
		public WPFfont()
        {
            Family = new FontFamily();
        }

        /// <summary>
        /// Create a WPFfont from its properties.
        /// </summary>
        /// <param name="family">FontFamily</param>
        /// <param name="weight">FontWeight</param>
        /// <param name="style">FontStyle</param>
        public WPFfont(FontFamily family, FontWeight weight, FontStyle style)
        {
            Family = family;
            Weight = weight;
            Style = style;
        }

        /// <summary>
        /// Copy a WPFfont
        /// </summary>
        /// <param name="src">WPFfont to duplicate</param>
        public WPFfont(WPFfont src)
        {
            Family = src.Family;
            Weight = src.Weight;
            Style = src.Style;
        }

        #endregion

    }

    
    /// <summary>
    /// FontFamily converter from string to FontFamily, enabling to use null as FontFamily (system default)
    /// </summary>
    public class FontFamilyConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                return ConvertBack(value, targetType, parameter, culture);
            }

            // Convert FontFamily --> string 
            string ret = null;
            try
            {
                ret = (string)TypeDescriptor.GetConverter(typeof(FontFamily)).ConvertTo(value, typeof(string));
            }
            catch { }

            return ret;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FontFamily)
            {
                return Convert(value, targetType, parameter, culture);
            }

            // Convert string --> FontFamily
            FontFamily ret = null;
            try
            {
                ret = (FontFamily)TypeDescriptor.GetConverter(typeof(FontFamily)).ConvertFrom(value);
            }
            catch
            {
                ret = new FontFamily();
            }

            return ret;
        }
    }

    
    #endregion


    // --------------------------------------------------------------------------------------------------
    #region Enum

    /// <summary>
    /// Create an ItemsSource (for ComboBox, ContextMenu...) from an enumerate.
    /// </br>
    /// Usage:
    /// <code>
    /// <ContextMenu ItemsSource="{l:EnumToItemsSource {x:Type l:DragDropActionType}}"  />
    /// </code>
    /// </summary>
    [MarkupExtensionReturnType(typeof(Dictionary<string, string>))]
    public class EnumToItemsSourceExtension : MarkupExtension
    {
        private const string LanguageSection = "Types";

        private Type _type;

        public EnumToItemsSourceExtension(Type type)
        {
            _type = type;
        }

        /// <summary>
        /// Provide the ItemsSource
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns>ItemsSource as a array of ItemsSourceType</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var enums = Enum.GetNames(_type);
            var ret = new Dictionary<string, string>();

            for (int i = 0; i < enums.Length; i++)
            {
                var keyval = string.Format("{0}.{1}.name", _type.Name, enums[i]);
                ret.Add( enums[i], LimeLanguage.Translate(LanguageSection, keyval, keyval) );
            }

            return ret;
        }

    }



	#endregion


	// --------------------------------------------------------------------------------------------------
	#region Converters

	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// Convert any <see cref="IEnumerable"/> to a <see cref="string"/> representation.
	/// </summary>
	public class ListToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string sep = LimeLanguage.Translate("Text", "ListSeparator", ", ");

			if (value is IEnumerable<string> list)
			{
				return string.Join(sep, list);
			}
			else if (value == null)
			{
				return "";
			}

			return new ValidationResult(false, "ListToStringConverter: Type not supported: " + value.GetType());
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new ValidationResult(false, "ListToStringConverter: ConvertBack not supported");
		}

	}


	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// Return an instance of LimeIcon from its key identifier.
	/// </summary>
	public class LimeIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string key)
			{
				var ret = new LimeIcon() {
					IconKey = key,
					Margin = new Thickness(0)
				};

				return ret;
			}
			else if (value == null)
			{
				return null;
			}

			return new ValidationResult(false, "LimeIconConverter: Type not supported: " + value.GetType());
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new ValidationResult(false, "LimeIconConverter: ConvertBack not supported");
		}

	}

	#endregion


	// --------------------------------------------------------------------------------------------------
	#region AnimateAction

	public static class AnimateAction
    {
        /// <summary>
        /// Indicate if an animation is ongoing.
        /// </summary>
        public static bool IsAnimated = false;
        public static bool EnableAnimations = false;


        private static Action _Storyboard_Completed_Action = null;
        private static Action _Storyboard_Closing_Action = null;
        private static string _Storyboard_Completed_board = null;
        private static string _Storyboard_Closing_board = null;
        private static Storyboard _Storyboard_board = null;

        /// <summary>
        /// Introduce an action by an animation and complete it with another animation/action
        /// </summary>
        /// <param name="board">Key of the storyboard to be played prior to the action.</param>
        /// <param name="action">Action to be executed after the board animation has been played. The board_Leave animation is played right after this action is completed.</param>
        /// <param name="closingBoard">Key of the storyboard to be played after the action has completed.</param>
        /// <param name="closingAction">Action to be executed after the closingBoard animation has been played.</param>
        public static void Do(string board, Action action, string closingBoard = null, Action closingAction = null)
        {
            if (!EnableAnimations || _Storyboard_Completed_Action != null || board == null || IsAnimated)
            {
                // re-entrant
                LimeMsg.Debug("AnimateAction bypassed: {0}, EnableAnimations: {1}, IsAnimated: {2}", board, EnableAnimations, IsAnimated);
                action();
                closingAction?.Invoke();
                return;
            }

            LimeMsg.Debug("AnimateAction: {0}", board);

            IsAnimated = true;
            _Storyboard_Completed_board = board;
            _Storyboard_Completed_Action = action;
            _Storyboard_Closing_board = closingBoard;
            _Storyboard_Closing_Action = closingAction;

			if (Commands.MainWindow.TryFindResource(_Storyboard_Completed_board) is Storyboard sb)
			{
				var winscope = _Storyboard_Completed_board.StartsWith("wxPanel");
				_Storyboard_board = sb.Clone();
				_Storyboard_board.Completed += _Storyboard_Completed;
				_Storyboard_board.Begin(winscope ? (FrameworkElement)Commands.MainWindow.Browser : Commands.MainWindow);
			}
			else
			{
				IsAnimated = false;
				var doit = _Storyboard_Completed_Action;
				_Storyboard_Completed_Action = null;
				doit();
			}
		}

        private static void _Storyboard_Completed(object sender, EventArgs e)
        {
            // Animation completed: remve callback
            _Storyboard_board.Completed -= _Storyboard_Completed;

            if (_Storyboard_Completed_Action != null)
            {

                Storyboard sbo = null;
                if (_Storyboard_Closing_board != null) sbo = Commands.MainWindow.TryFindResource(_Storyboard_Closing_board) as Storyboard;

                IsAnimated = sbo != null;
                var doit = _Storyboard_Completed_Action;
                _Storyboard_Completed_Action = null;
                doit();

                if (sbo != null)
                {
                    var winscope = _Storyboard_Closing_board.StartsWith("wxPanel");
                    _Storyboard_board = sbo.Clone();
                    LimeMsg.Debug("AnimateAction animate: {0}", IsAnimated);
                    _Storyboard_board.Completed += _Storyboard_Completed;
                    _Storyboard_board.Begin(winscope ? (FrameworkElement)Commands.MainWindow.Browser : Commands.MainWindow);
                }
                else
                {
                    IsAnimated = false;
                    var redoit = _Storyboard_Closing_Action;
                    _Storyboard_Closing_Action = null;
                    redoit?.Invoke();
                }
            }
            else
            {
                IsAnimated = false;
                LimeMsg.Debug("AnimateAction Completed: {0}", IsAnimated);
                var redoit = _Storyboard_Closing_Action;
                _Storyboard_Closing_Action = null;
                redoit?.Invoke();
            }

        }

    }

    #endregion

}
