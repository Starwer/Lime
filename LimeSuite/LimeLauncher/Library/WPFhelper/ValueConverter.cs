/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/


using Lime;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;

namespace WPFhelper
{

    // ----------------------------------------------------------------------------------------------
    /// <summary>
    /// Universal value converter for WPF.
    /// Usage in Xaml: <Rectangle Fill="{Binding Path=MyProperty, Converter={StaticResource UniversalValueConverter}} " />
    /// Credit: Colin Eberhardt, modified by Starwer
    ///         http://blog.scottlogic.com/2010/07/09/a-universal-value-converter-for-wpf.html
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class UniversalValueConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // obtain the conveter for the target type
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);

            try
            {

                // determine if the supplied value is of a suitable type
                if (value == null && !targetType.IsValueType || value.GetType() == targetType)
                {
                    return value; // Nothing to do
                }
                else if ( value.GetType() == typeof(double) && targetType == typeof(string) )
                {
                    return String.Format(culture, "{0:0.0#}", (double)value);
				}
                else if (value.GetType() == typeof(string) && targetType == typeof(double))
                {
                    // Special care for double (strangely enough, not convertible from/to string using a converter)
                    return ((double)value).ToString(culture);
                }
                else if(converter.CanConvertFrom(value.GetType()))
                {
                    // return the converted value
                    return converter.ConvertFrom(null, culture, value);
                }
                else if (targetType == typeof(ImageSource))
                {
                    // Try to convert anything to ImageSource
                    return LimeLib.ImageSourceFrom(value);
                }
                else
                {
                    // try to convert from the string representation
                    return converter.ConvertFrom(null, culture, string.Format(culture, "{0}", value));
                }
            }
            catch (Exception)
            {
                //return TypeDescriptor.GetConverter(typeof(double)).ConvertFrom(value);
                return new ValidationResult(false, "Format error");
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }

	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// Return True/Visible if the value (double) is lower than the parameter value (double).
	/// </summary>
	public class LowerThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null)
            {
                try
                {
                    double comp;
                    if (parameter is string) comp = Double.Parse((string)parameter, culture);
                    else comp = (double)parameter;
                    bool ret = ((double)value) < comp;

					// Bool to anything
					if (targetType == typeof(Visibility)) return ret ? Visibility.Visible : Visibility.Collapsed;
					return ret;
				}
				catch {}
            }
            return new ValidationResult(false, "ScaleConverter: Parameter Format error");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new ValidationResult(false, "LowerThanConverter: ConvertBack not supported");
        }

    }

	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// Return True/Visible if the value (any type) is equal to the parameter value (any type).
	/// </summary>
	public class EqualConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool ret;

			try
			{
				if (value==null && parameter==null)
				{
					ret = true;
				}
				else if (value.GetType() == parameter.GetType())
				{
					ret = value.Equals(parameter); 
				}
				else if (value.GetType().IsEnum && parameter.GetType() == typeof(string))
				{
					ret = ((Enum) value).ToString() == (string)parameter;
				}
				else if ( parameter.GetType() == typeof(string) )
				{
					var val = string.Format(culture, "{0}", value);
					ret = val.Equals(parameter);
				}
				else
				{
					TypeConverter converter = TypeDescriptor.GetConverter(targetType); // won't work toward string
					var val = converter.ConvertFrom(null, culture, parameter);
					ret = value.Equals(val);
				}

				// Bool to anything
				if (targetType == typeof(Visibility))
				{
					return ret ? Visibility.Visible : Visibility.Collapsed;
				}
				return ret;
			}
			catch (Exception)
			{
				//return TypeDescriptor.GetConverter(typeof(double)).ConvertFrom(value);
				return new ValidationResult(false, "Format error");
			}

		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new ValidationResult(false, "EqualConverter: ConvertBack not supported");
		}

	}

	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// Return True/Visible if the value (any type) is not equal to the parameter value (any type).
	/// </summary>
	public class NotEqualConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var converter = new EqualConverter();
			var ret = ! (bool) converter.Convert(value, typeof(bool), parameter, culture);

			// Bool to anything
			if (targetType == typeof(Visibility)) return ret ? Visibility.Visible : Visibility.Collapsed;
			return ret;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new ValidationResult(false, "EqualConverter: ConvertBack not supported");
		}

	}

	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// Scale a value by multiplying it by the parameter (default: <see cref="TypeScaled.Scale"/>)
	/// </summary>
	public class ScaleConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Lime.LimeMsg.Debug("ScaleConverter: Convert {0} to {1} by {2}", value, targetType, parameter);
            double scale = TypeScaled.Scale;
            if (parameter != null)
            {
                try
                {
                    if (parameter is string) scale = Double.Parse((string)parameter, culture);
                    else scale = (double)parameter;
                }
                catch
                {
                    return new ValidationResult(false, "ScaleConverter: Parameter Format error");
                }
            }

            // Convert value to double
            double val;
            if (value.GetType() == typeof(double))
            {
                val = (double)value;
            }
            else
            {
                TypeConverter converter2 = TypeDescriptor.GetConverter(value.GetType());
                try
                {
                    // determine if the supplied value is of a suitable type
                    if (converter2.CanConvertTo(typeof(double)))
                    {
                        // get the converted value
                        val = (double)converter2.ConvertTo(null, culture, value, typeof(double));
                    }
                    else
                    {
                        // try to convert from the string representation
                        val = double.Parse(value.ToString(), culture);
                    }
                }
                catch (Exception)
                {
                    //return TypeDescriptor.GetConverter(typeof(double)).ConvertFrom(value);
                    return new ValidationResult(false, "ScaleConverter: Input Format error");
                }
            }

            // Scale value
            val *= scale;




            // Convert scaled value to target type
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            try
            {
                // Special care for double (strangely enough, not convertible from/to string using a converter)
                if (targetType == typeof(double)) return val;

                // determine if the supplied value is of a suitable type
                if (converter.CanConvertFrom(val.GetType()))
                {
                    // return the converted value
                    return converter.ConvertFrom(null, culture, val);
                }
                else
                {
                    // try to convert from the string representation
                    return converter.ConvertFrom(val.ToString(culture));
                }
            }
            catch (Exception)
            {
                // Fallback: just return a double
                return val;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Lime.LimeMsg.Debug("ScaleConverter: ConvertBack {0}", value);
            double scale = TypeScaled.Scale;
            if (parameter != null)
            {
                try
                {
                    if (parameter is string) scale = Double.Parse((string)parameter, culture);
                    else scale = (double)parameter;
                }
                catch
                {
                    return new ValidationResult(false, "ScaleConverter: Parameter Format error");
                }
            }

            // Use inverted scale to convert back
            scale = 1.0 / scale;

            return Convert(value, targetType, scale, culture);
        }
    }



	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// Multiply a color by a parameter
	/// </summary>
	public class ColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            // Convert value to double
            Color val;
            if (value.GetType() == typeof(Color))
            {
                val = (Color)value;
            }
            else
            {
                TypeConverter converter2 = TypeDescriptor.GetConverter(typeof(Color));
                try
                {
                    // determine if the supplied value is of a suitable type
                    if (converter2.CanConvertFrom(value.GetType()))
                    {
                        // get the converted value
                        val = (Color)converter2.ConvertFrom(value);
                    }
                    else
                    {
                        // try to convert from the string representation
                        val = (Color)converter2.ConvertFrom(value.ToString());
                    }
                }
                catch (Exception)
                {
                    //return TypeDescriptor.GetConverter(typeof(double)).ConvertFrom(value);
                    return new ValidationResult(false, "ColorConverter: Input Format error");
                }
            }

            // Get and apply Scale parameter
            if (parameter != null)
            {
                Color scale = new Color();
                TypeConverter converter1 = TypeDescriptor.GetConverter(typeof(Color));

                try
                {
                    if (parameter is Color)
                    {
                        scale = (Color)parameter;
                    }
                    else if (converter1.CanConvertFrom(parameter.GetType()))
                    {
                        scale = (Color)converter1.ConvertFrom(null, culture, parameter);
                    }
                    else
                    {
                        scale = (Color)converter1.ConvertFrom(parameter.ToString());
                    }


                }
                catch
                {
                    return new ValidationResult(false, "ColorConverter: Parameter Format error");
                }

                // Scale value
                val.ScA *= scale.ScA;
                val.ScR *= scale.ScR;
                val.ScG *= scale.ScG;
                val.ScB *= scale.ScB;

            }

            // Convert scaled value to target type
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            try
            {
                // determine if the supplied value is of a suitable type
                if (converter.CanConvertFrom(val.GetType()))
                {
                    // return the converted value
                    return converter.ConvertFrom(val);
                }
                else
                {
                    // try to convert from the string representation
                    return converter.ConvertFrom(val.ToString());
                }
            }
            catch (Exception)
            {
                //return TypeDescriptor.GetConverter(typeof(double)).ConvertFrom(value);
                return new ValidationResult(false, "ScaleConverter: Output Format error");
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new ValidationResult(false, "ColorConverter: ConvertBack not supported");
        }
    }



	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// Convert a string to URI (or URI to String).
	/// </summary>
	public class URIConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Uri ret;
			if (value is string str)
			{
				if (parameter is string param)
				{
					str = param + str;
				}
				
				ret = new Uri(str);
			}
			else if (value is Uri uri)
			{
				ret = uri;
			}
			else
			{
				ret = null;
			}

			return ret;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string ret;
			if (value is string str)
			{
				ret = str;
			}
			else if (value is Uri uri)
			{
				ret = uri.AbsoluteUri;
			}
			else
			{
				ret = null;
			}

			return ret;
		}

	}

}