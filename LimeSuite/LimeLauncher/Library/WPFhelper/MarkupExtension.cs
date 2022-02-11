/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/


using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace WPFhelper
{

    /// <summary>
    /// Base class enabling to create a Markup Extension creating a Binding from an object dynamically.
    /// </br>
    /// Usage:
    /// <code>
    /// public class MyTypeExtension : BindingConstructorExtension&lt;ByteScaled&gt;
    /// {   // Boiler plate
    ///     public MyTypeExtension() { binding = new BindingSustained("MyPath"); }
    ///     public MyTypeExtension(object value) : this() { Value = value; }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">Type of the object handled by the derived class</typeparam>
    [SupportedOSPlatform("windows")]
    [MarkupExtensionReturnType(typeof(object))]
    public abstract class BindingConstructorExtension<T> : MarkupExtension
    {
        /// <summary>
        /// Class to embed and keep the constructed object alive with its binding (Binding use weak reference for its source).
        /// </summary>
        protected class BindingSustained : Binding
        {

            /// <summary>
            /// Source of the Binding
            /// </summary>
            public new object Source
            {
                get { return base.Source; }
                set { base.Source = _Source = value; }

            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", 
                Justification = " Don't 'simplify': this object is absolutely required to keep Source alive")]
            private object _Source = null;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="path"></param>
            public BindingSustained(string path = null) : base(path)
            { }

        }

        /// <summary>
        /// Binding object to be created and bound to the created object by the Markup Extension
        /// </summary>
        protected BindingSustained binding;

        /// <summary>
        /// Object to be created in the Markup Extension
        /// </summary>
        [ConstructorArgument("value")]
        public object Value
        {
            get { return (binding.Source as DoubleScaled).Value; }
            set
            {
                if (value != null)
                {
                    // obtain the conveter for the target type
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                    var culture = CultureInfo.InvariantCulture;

                    try
                    {
                        // determine if the supplied value is of a suitable type
                        if (converter.CanConvertFrom(value.GetType()))
                        {
                            // return the converted value
                            binding.Source = converter.ConvertFrom(null, culture, value);
                        }
                        else
                        {
                            // try to convert from the string representation
                            binding.Source = converter.ConvertFrom(null, culture, value.ToString());
                        }
                    }
                    catch (Exception)
                    {
                        new ValidationResult(false, "Format error");
                    }
                }
            }
        }


        /// <summary>
        /// Provide the Binding
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns>Binding, or defered Binding</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Default: return Binding (Style Setter)
            object ret = binding;

            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target)
            {
                // Template: defer construction
                if (target.TargetObject.GetType().FullName == "System.Windows.SharedDp") return this;

                // Retrieve object and property

                if ( target.TargetObject is DependencyObject targetObject && 
                     target.TargetProperty is DependencyProperty targetProperty )
                {
                    // Instanciated DependencyObject
                    BindingOperations.SetBinding(targetObject, targetProperty, binding);
                    ret = binding.ProvideValue(serviceProvider);
                }
            }

            return ret;
        }

    }

}