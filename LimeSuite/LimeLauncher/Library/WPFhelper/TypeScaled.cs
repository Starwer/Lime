/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/


using Lime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace WPFhelper
{

    #region Base TypeScaled classes

    /// <summary>
    /// Base class which enable to set the scale factor for all the TypeScaled object
    /// </summary>
    public abstract class TypeScaled : DependencyObject, INotifyPropertyChanged, IMatryoshka
    {
        // --------------------------------------------------------------------------------------------------
        #region Property change Boilerplate code

        // Boilerplate code for INotifyPropertyChanged : Instances

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private static List<WeakReference> instances = new List<WeakReference>();

        /// <summary>
        /// Scale factor that will be automatically applied to all TypeScaled objects
        /// </summary>
        public static double Scale
        {
            get { return _Scale; }
            set
            {
                if (value != _Scale)
                {
                    _Scale = value;

                    foreach (var instance in instances)
                    {
                        if (instance.IsAlive)
                        {
                            ((TypeScaled)instance.Target).OnPropertyChanged("Scaled");
                        }
                    }
                }
            }
        }
        private static double _Scale = 1.0;

        /// <summary>
        /// IMatryoshka implementation
        /// </summary>
        public abstract object Content { get; set; }


        public TypeScaled()
        {
            instances.Add(new WeakReference(this));
        }

        ~TypeScaled()
        {
            var match = new List<WeakReference>();
            var lst = instances.ToArray();
            foreach (var item in lst)
            {
                if (item.IsAlive) match.Add(item);
            }
            instances = match;
        }
    }

    /// <summary>
    /// Base class for deriving multiple TypeScaled class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ContentProperty("Value")]
    public abstract class TypeScaled<T> : TypeScaled
    {
        //public static readonly DependencyProperty ValueProperty =
        //    DependencyProperty.Register("Value", typeof(T), typeof(TypeScaled<T>), new PropertyMetadata(OnValuePropertyChanged));

        //private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    (d as TypeScaled<T>).OnPropertyChanged("Value");
        //    (d as TypeScaled<T>).OnPropertyChanged("Scaled");
        //}


        ///// <summary>
        ///// Gets or set the original value (not scaled)
        ///// </summary>
        //public T Value
        //{
        //    get { return (T)GetValue(ValueProperty); }
        //    set { SetValue(ValueProperty, value); }
        //}

        /// <summary>
        /// Gets or sets  the original value as an object
        /// </summary>
        public override object Content
        {
            get { return Value; }
            set { Value = (T)value; }
        }

        /// <summary>
        /// Gets or sets the original value (not scaled)
        /// </summary>
        public T Value
        {
            get
            {
                //Lime.LimeMsg.Debug("TypeScaled: get Value: {0}", _Value);
                return _Value;
            }
            set
            {
                //Lime.LimeMsg.Debug("TypeScaled: set Value: {0}", value);
                _Value = value;
                OnPropertyChanged("Value");
                OnPropertyChanged("Scaled");
                OnPropertyChanged("Content");
            }
        }
        protected T _Value { get; set; }


        /// <summary>
        /// Gets or sets the value multiplied by the Scale factor
        /// </summary>
        public T Scaled {
			get
			{
				return Scaler(_Value, Scale);
			}
			set
			{
				if (Scale != 0.0)
				{
					Value = Scaler(_Value, 1.0 / Scale);
				}
			}
		}


		/// <summary>
		/// Multiply the value with a factor: value * factor
		/// </summary>
		/// <param name="value">value to be multiplied</param>
		/// <param name="factor">factor to be applied</param>
		/// <returns>result of the multiplication</returns>
		protected abstract T Scaler(T value, double factor);


        public TypeScaled() : base()
        {
        }

        public TypeScaled(T value) : base()
        {
            Value = value; 
        }

        public static implicit operator T(TypeScaled<T> value)
        {
            return value.Value;
        }

        public override string ToString() { return Value.ToString(); }
    }

    /// <summary>
    /// Generic Type-converter for TypeScaled derived classes.
    /// </summary>
    /// <typeparam name="Td">Derived TypeScaled class to convert to.</typeparam>
    /// <typeparam name="Tv">Type of the value to the TypeScaled class.</typeparam>
    public class TypeScaledTypeConverter<Td, Tv> : TypeConverter where Td : TypeScaled<Tv>, new()
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(Tv)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type targetType)
        {
            if (targetType == typeof(string) || targetType == typeof(Tv)) return true;
            return base.CanConvertTo(context, targetType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            //Lime.LimeMsg.Debug("TypeScaledTypeConverter: ConvertFrom {0} to {1}", value, typeof(Tv));

            Type targetType = typeof(Tv);

            // obtain the conveter for the target type
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);

            Tv val;

            try
            {
                // determine if the supplied value is of a suitable type
                if (converter.CanConvertFrom(value.GetType()))
                {
                    // return the converted value
                    val = (Tv)converter.ConvertFrom(context, culture, value);
                }
                else
                {
                    // try to convert from the string representation
                    val = (Tv)converter.ConvertFrom(context, culture, value.ToString());
                }

                Td ret = new Td();
                ret.Value = val;
                return ret;

            }
            catch (Exception)
            {
                return null;
            }
        }


        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type targetType)
        {
            //Lime.LimeMsg.Debug("TypeScaledTypeConverter: ConvertTo {0} to {1}", value, typeof(Tv));
            if (targetType == null) throw new ArgumentNullException("targetType");

            Td ret = value as Td;

            if (ret != null)
            {
                if (targetType == typeof(Tv))
                {
                    return ret.Value;
                }
                else
                {
                    UniversalValueConverter conv = new UniversalValueConverter();
                    return conv.Convert(ret.Value, targetType, null, culture);
                }
            }
            return base.ConvertTo(context, culture, value, targetType);
        }
    }

    #endregion


    #region Derived TypeScaled classes


    /// <summary>
    /// Represents a Byte value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<ByteScaled, byte>))]
    public class ByteScaled : TypeScaled<byte>
    {
		protected override byte Scaler(byte value, double factor)
		{
            return (byte)(value * factor);
        }

        /// <summary>
        /// Convert the value multiplied by the Scale factor to a formated string
        /// </summary>
        /// <param name="format">Format of byte</param>
        /// <returns>string representation of the object</returns>
        public string ToString(string format) { return Value.ToString(format); }
    }
    public class ByteScaledExtension : BindingConstructorExtension<ByteScaled>
    {   // Boiler plate
        public ByteScaledExtension() { binding = new BindingSustained("Scaled"); }
        public ByteScaledExtension(object value) : this() { Value = value; }
    }


    /// <summary>
    /// Represents a Int16 value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<Int16Scaled, Int16>))]
    public class Int16Scaled : TypeScaled<Int16>
    {
		protected override Int16 Scaler(Int16 value, double factor)
		{
			return (Int16)(value * factor);
		}

		/// <summary>
		/// Convert the value multiplied by the Scale factor to a formated string
		/// </summary>
		/// <param name="format">Format of Int16</param>
		/// <returns>string representation of the object</returns>
		public string ToString(string format) { return Value.ToString(format); }
    }
    public class Int16ScaledExtension : BindingConstructorExtension<Int16Scaled>
    {   // Boiler plate
        public Int16ScaledExtension() { binding = new BindingSustained("Scaled"); }
        public Int16ScaledExtension(object value) : this() { Value = value; }
    }


    /// <summary>
    /// Represents a Int32 value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<Int32Scaled, Int32>))]
    public class Int32Scaled : TypeScaled<Int32>
    {
		protected override Int32 Scaler(Int32 value, double factor)
		{
			return (Int32)(value * factor);
		}

		/// <summary>
		/// Convert the value multiplied by the Scale factor to a formated string
		/// </summary>
		/// <param name="format">Format of Int32</param>
		/// <returns>string representation of the object</returns>
		public string ToString(string format) { return Value.ToString(format); }
    }
    public class Int32ScaledExtension : BindingConstructorExtension<Int32Scaled>
    {   // Boiler plate
        public Int32ScaledExtension() { binding = new BindingSustained("Scaled"); }
        public Int32ScaledExtension(object value) : this() { Value = value; }
    }


    /// <summary>
    /// Represents a Int64 value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<Int64Scaled, Int64>))]
    public class Int64Scaled : TypeScaled<Int64>
    {
		protected override Int64 Scaler(Int64 value, double factor)
		{
			return (Int64)(value * factor);
		}

		/// <summary>
		/// Convert the value multiplied by the Scale factor to a formated string
		/// </summary>
		/// <param name="format">Format of Int64</param>
		/// <returns>string representation of the object</returns>
		public string ToString(string format) { return Value.ToString(format); }
    }
    public class Int64ScaledExtension : BindingConstructorExtension<Int64Scaled>
    {   // Boiler plate
        public Int64ScaledExtension() { binding = new BindingSustained("Scaled"); }
        public Int64ScaledExtension(object value) : this() { Value = value; }
    }


    /// <summary>
    /// Represents a Single value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<SingleScaled, Single>))]
    public class SingleScaled : TypeScaled<Single>
    {
		protected override Single Scaler(Single value, double factor)
		{
			return (Single)(value * factor);
		}

		/// <summary>
		/// Convert the value multiplied by the Scale factor to a formated string
		/// </summary>
		/// <param name="format">Format of Single</param>
		/// <returns>string representation of the object</returns>
		public string ToString(string format) { return Value.ToString(format); }
    }
    public class SingleScaledExtension : BindingConstructorExtension<SingleScaled>
    {   // Boiler plate
        public SingleScaledExtension() { binding = new BindingSustained("Scaled"); }
        public SingleScaledExtension(object value) : this() { Value = value; }
    }


    /// <summary>
    /// Represents a double value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<DoubleScaled, double>))]
    public class DoubleScaled : TypeScaled<double>
    {
		protected override double Scaler(double value, double factor)
		{
			return (double)(value * factor);
		}

		/// <summary>
		/// Convert the value multiplied by the Scale factor to a formated string
		/// </summary>
		/// <param name="format">Format of double</param>
		/// <returns>string representation of the object</returns>
		public string ToString(string format) { return Value.ToString(format); }

		public static implicit operator DoubleScaled(double v)
		{
			throw new NotImplementedException();
		}
	}
    public class DoubleScaledExtension: BindingConstructorExtension<DoubleScaled>
    {   // Boiler plate
        public DoubleScaledExtension() { binding = new BindingSustained("Scaled"); }
        public DoubleScaledExtension(object value) : this() { Value = value; }
    }
    

    /// <summary>
    /// Represents a Thickness value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<ThicknessScaled, Thickness>))]
    public class ThicknessScaled : TypeScaled<Thickness>
    {
		protected override Thickness Scaler(Thickness value, double factor)
		{
			return new Thickness(value.Left * factor, value.Top * factor, value.Right * factor, value.Bottom * factor);
		}


		private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var val = (d as ThicknessScaled).Value;

            if (e.Property == LeftProperty) val.Left = (double)e.NewValue;
            else if (e.Property == RightProperty) val.Right = (double)e.NewValue;
            else if (e.Property == TopProperty) val.Top = (double)e.NewValue;
            else if (e.Property == BottomProperty) val.Bottom = (double)e.NewValue;

            (d as ThicknessScaled).Value = val;

            (d as ThicknessScaled).OnPropertyChanged("Value");
            (d as ThicknessScaled).OnPropertyChanged("Scaled");
        }


        /// <summary>
        /// Gets or Sets the width, in pixels, of the left side of the bounding rectangle (non scaled)
        /// </summary>
        public double Left
        {
            get { return (double)GetValue(LeftProperty); }
            set { SetValue(LeftProperty, value); }
        }
        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.Register("Left", typeof(double), typeof(ThicknessScaled), new PropertyMetadata(OnValuePropertyChanged));



        /// <summary>
        /// Gets or Sets the width, in pixels, of the right side of the bounding rectangle
        /// </summary>
        public double Right
        {
            get { return (double)GetValue(RightProperty); }
            set { SetValue(RightProperty, value); }
        }
        public static readonly DependencyProperty RightProperty =
            DependencyProperty.Register("Right", typeof(double), typeof(ThicknessScaled), new PropertyMetadata(OnValuePropertyChanged));

        /// <summary>
        /// Gets or Sets the width, in pixels, of the top side of the bounding rectangle
        /// </summary>
        public double Top
        {
            get { return (double)GetValue(TopProperty); }
            set { SetValue(TopProperty, value); }
        }
        public static readonly DependencyProperty TopProperty =
            DependencyProperty.Register("Top", typeof(double), typeof(ThicknessScaled), new PropertyMetadata(OnValuePropertyChanged));

        /// <summary>
        /// Gets or Sets the width, in pixels, of the bottom side of the bounding rectangle
        /// </summary>
        public double Bottom
        {
            get { return (double)GetValue(BottomProperty); }
            set { SetValue(BottomProperty, value); }
        }
        public static readonly DependencyProperty BottomProperty =
            DependencyProperty.Register("Bottom", typeof(double), typeof(ThicknessScaled), new PropertyMetadata(OnValuePropertyChanged));

    }
    public class ThicknessScaledExtension : BindingConstructorExtension<ThicknessScaled>
    {   // Boiler plate
        public ThicknessScaledExtension() { binding = new BindingSustained("Scaled"); }
        public ThicknessScaledExtension(object value) : this() {
            Value = value;
        }
    }


    /// <summary>
    /// Represents a Size value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<SizeScaled, Size>))]
    public class SizeScaled : TypeScaled<Size>
    {
		protected override Size Scaler(Size value, double factor)
		{
			return new Size(value.Width * factor, value.Height * factor);
		}
	}
	public class SizeScaledExtension : BindingConstructorExtension<SizeScaled>
    {   // Boiler plate
        public SizeScaledExtension() { binding = new BindingSustained("Scaled"); }
        public SizeScaledExtension(object value) : this() { Value = value; }
    }


    /// <summary>
    /// Represents a Rect value which can scale with the factor defined in TypeScaled.Scale.
    /// Only the size of the rectangle scales, its position remain unchanged.
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<RectScaled, Rect>))]
    public class RectScaled : TypeScaled<Rect>
    {
		protected override Rect Scaler(Rect value, double factor)
		{
			return new Rect(value.X, value.Y, value.Width * factor, value.Height * factor);
		}
	}
    public class RectScaledExtension : BindingConstructorExtension<RectScaled>
    {   // Boiler plate
        public RectScaledExtension() { binding = new BindingSustained("Scaled"); }
        public RectScaledExtension(object value) : this() { Value = value; }
    }


    /// <summary>
    /// Represents a Vector value which can scale with the factor defined in TypeScaled.Scale
    /// </summary>
    [TypeConverter(typeof(TypeScaledTypeConverter<VectorScaled, Vector>))]
    public class VectorScaled : TypeScaled<Vector>
    {
		protected override Vector Scaler(Vector value, double factor)
		{
			return new Vector(value.X * factor, value.Y * factor);
		}

	}
	public class VectorScaledExtension : BindingConstructorExtension<VectorScaled>
    {   // Boiler plate
        public VectorScaledExtension() { binding = new BindingSustained("Scaled"); }
        public VectorScaledExtension(object value) : this() { Value = value; }
    }

    #endregion

}