/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     11-02-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System;
using System.Windows.Controls;
using System.Windows.Input;
using Lime;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace LimeLauncher.Controls
{
    /// <summary>
    /// Interaction logic for LimeNumBox.xaml
    /// </summary>
    public partial class LimeNumBox : LimeControl
    {
        public LimeNumBox()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory(hasHeader: true);
        }


        protected override void OnBoundDataContextChanged(DependencyPropertyChangedEventArgs e)
        {
            // Initialize
            wxSlider.Visibility = Visibility.Collapsed;
        }


        protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) )
            {
                wxValidation.Source = prop;
            }

			if (prop == null)
			{
				// do nothing
			}
			else if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Value" || e.PropertyName == "Percentage" 
			      || e.PropertyName == "Maximum" || e.PropertyName == "Minimum" || e.PropertyName == "ReadOnly"
                   || e.PropertyName == "AllowEmpty")
            {
                var min = prop.Minimum;
                var max = prop.Maximum;
                if (min < max && !prop.ReadOnly)
                {
                    wxCol.Width = new GridLength(1, GridUnitType.Star);
                    wxSlider.Visibility = Visibility.Visible;
                    if (prop.Percentage)
                    {
                        min *= 100;
                        max *= 100;
                    }
                    wxSlider.Minimum = min;
                    wxSlider.Maximum = max;
                }
                else
                {
                    wxCol.Width = new GridLength(0);
                    wxSlider.Visibility = Visibility.Collapsed;
                }

                wxMinus.Visibility = prop.ReadOnly ? Visibility.Collapsed : Visibility.Visible;
                wxPlus.Visibility = wxMinus.Visibility;


                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Value")
                {
                    if ( !prop.Percentage && (prop.Type == typeof(float) || prop.Type == typeof(double)))
                    {
                        var diff = max - min;
                        wxSlider.SmallChange = 
                            diff > 20 ? 1 :
                            diff > 1  ? 0.1 :
                            diff > 0  ? diff / 100 : 
                            0.01;
                    }
                    else
                    {
                        wxSlider.SmallChange = 1;
                    }

                    wxSlider.LargeChange = wxSlider.SmallChange * 10;

                    if (prop.AllowEmpty && prop.Value == "")
                    {
                        wxTextBox.Text = "";
                    } 
                    else if (double.TryParse(prop.Value, out double val))
					{
						if (prop.Percentage) val *= 100;
						if (wxSlider.Visibility == Visibility.Visible)
						{
							wxSlider.Value = val;
						}
						else
						{
							wxTextBox.Text = val.ToString() + (prop.Percentage && !wxTextBox.IsFocused ? "%" : "");
						}
					}
				}

            }

        }


        DispatcherTimer Timer = null;


        /// <summary>
        /// Validate the value
        /// </summary>
        /// <param name="sender">Callback from DispatcherTimer</param>
        /// <param name="e">Callback from DispatcherTimer</param>
        private void ValidateValue(object sender = null, EventArgs e = null)
        {
            if (Timer != null)
            {
                Timer.Stop();
                Timer = null;
            }

            // Handle percents
            string txt = wxTextBox.Text.Trim();
            if (txt.EndsWith("%")) txt = txt.Substring(0, txt.Length - 1);

            LimeMsg.Debug("LimeNumBox ValidateValue: {0}", txt);

            if (double.TryParse(txt, out double val) || txt == "" && wxMain.DataContext is LimeProperty lp && lp.AllowEmpty)
            {
                // Trigger Validation
                if (wxMain.DataContext is LimeProperty prop)
                {
                    if (prop.Percentage) val /= 100;
                    if (prop.CanConvertFrom(val.ToString()))
                    {
                        prop.Value = val.ToString();
                    }
                }

				// Required in case it is called from timer, it seems
				CommandManager.InvalidateRequerySuggested();
			}
        }



        /// <summary>
        /// Trigger event when text changes
        /// </summary>
        private void wxTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = wxTextBox.Text.Trim();
            //LimeMsg.Debug("LimeNumBox TextChanged: {0}", txt);

            // Validate (or invalidate)
            var binding = wxTextBox.GetBindingExpression(TextBox.TextProperty);
            if (binding != null)
            {
                binding.UpdateSource();
                if (binding.HasValidationError && wxTextBox.IsFocused) return;
            }

            // Handle percents
            if (txt.EndsWith("%")) txt = txt.Substring(0, txt.Length - 1);

            if (txt == "" && wxMain.DataContext is LimeProperty prop && prop.AllowEmpty)
            {
                // Trigger Validation
                if (ValidateOnChange)
                {
                    ValidateValue();
                }
            }
            else if (double.TryParse(txt, out double val))
            {
                // Slider
                if (wxSlider.Visibility == Visibility.Visible)
                {
                    wxSlider.Value = val;
                }

                // Trigger Validation
                if (ValidateOnChange)
                {
                    ValidateValue();
                }
            }
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string txt = wxTextBox.Text.Trim();
            if (txt.EndsWith("%")) txt = txt.Substring(0, txt.Length - 1);

            var prop = wxMain.DataContext as LimeProperty;

            if (double.TryParse(txt, out double val) || prop != null && prop.AllowEmpty)
            {
                val += e.OriginalSource == wxMinus ? -wxSlider.SmallChange : wxSlider.SmallChange;

                // Coerce
                if (wxSlider.Visibility == Visibility.Visible)
                {
                    if (val < wxSlider.Minimum) val = wxSlider.Minimum;
                    if (val >= wxSlider.Maximum) val = wxSlider.Maximum;
                }

                if (prop != null)
                {
                    if (prop.CanConvertFrom(val.ToString()))
                    {
                        wxTextBox.Text = val.ToString() + (prop.Percentage && !wxTextBox.IsFocused ? "%" : "");

                        if (!ValidateOnChange)
                        {
                            if (Timer != null)
                            {
                                Timer.Stop();
                            }
                            else
                            {
                                Timer = new DispatcherTimer();
                                Timer.Tick += ValidateValue;
                                Timer.Interval = TimeSpan.FromMilliseconds(500);
                            }
                            Timer.Start();
                        }
                    }
                }
            }
        }


        private void wxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (wxSlider.Visibility != Visibility.Visible) return;
            if (wxMain.DataContext is LimeProperty prop)
            {
                double val = e.NewValue;
                var txt = val.ToString() + (prop.Percentage && !wxTextBox.IsFocused ? "%" : "");
                LimeMsg.Debug("LimeNumBox wxSlider_ValueChanged: {0}", txt);
                wxTextBox.Text = txt;
            }

        }

        private void wxSlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (!ValidateOnChange)
            {
                ValidateValue();
            }
        }


        private void wxTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Handle percents
            string txt = wxTextBox.Text.Trim();
            LimeMsg.Debug("LimeNumBox wxTextBox_GotFocus: {0}", txt);
            if (txt.EndsWith("%")) wxTextBox.Text = txt.Substring(0, txt.Length - 1);
        }

        private void wxTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Handle percents
            string txt = wxTextBox.Text.Trim();
            LimeMsg.Debug("LimeNumBox wxTextBox_LostFocus: {0}", txt);
            if (txt.EndsWith("%")) txt = txt.Substring(0, txt.Length - 1);

			_KeyPressing = false;

			if (wxMain.DataContext is LimeProperty prop)
            {
                if (txt != "" || !prop.AllowEmpty)
                {
                    if (!double.TryParse(txt, out double val))
                    {
                        // Recover from invalidation
                        if (double.TryParse(prop.Value, out val))
                        {
                            if (prop.Percentage) val *= 100;
                        }
                    }

                    // Reformat
                    if (val == 0.0 && prop.AllowEmpty)
                    {
                        wxTextBox.Text = "";
                    }
                    else
                    { 
                        wxTextBox.Text = val.ToString() + (prop.Percentage ? "%" : "");
                    }
                }

                if (!ValidateOnChange)
                {
                    ValidateValue();
                }
            }
        }


        private bool _KeyPressing = false;


        /// <summary>
        /// Enable to validate/invalidate a TextBox entry when pressing Enter/Esc key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxUpdateSourceOnKey_KeyEvent(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;
            _KeyPressing = false;

            if (Keyboard.Modifiers != 0) return;

            switch (e.Key)
            {

                case Key.Enter:
                    if (textBox.DataContext != null && (textBox.DataContext as LimeProperty)?.Value != textBox.Text)
                    {
                        e.Handled = true;
                        var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                        if (binding != null) binding.UpdateSource();

                        if (!ValidateOnChange)
                        {
                            ValidateValue();
                        }
                    }
                    break;

                case Key.Escape:
                    if (textBox.DataContext != null)
                    {
                        e.Handled = true;
                        var prop = textBox.DataContext as LimeProperty;
                        textBox.Text = prop != null ? prop.Value : textBox.DataContext.ToString();
                        var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                        if (binding != null) binding.UpdateTarget();
                    }
                    break;

            }
        }

        private void wxTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var wxobj = sender as TextBox;
            if (wxobj == null || wxobj.Text == null) return;
            if (Keyboard.Modifiers != 0) return;

            TraversalRequest tRequest = null;
            switch (e.Key)
            {
                case Key.Up:
                    tRequest = new TraversalRequest(FocusNavigationDirection.Up);
                    break;

                case Key.Down:
                    tRequest = new TraversalRequest(FocusNavigationDirection.Down);
                    break;

                case Key.Left:
                    if (wxobj.SelectionStart == 0 && wxobj.SelectionLength == 0 && !_KeyPressing)
                    {
                        tRequest = new TraversalRequest(FocusNavigationDirection.Left);
                    }
                    break;

                case Key.Right:
                    if (wxobj.IsReadOnly && !wxobj.IsReadOnlyCaretVisible)
                    {
                        e.Handled = true;
                        wxobj.IsReadOnlyCaretVisible = true;
                    }
                    else if (wxobj.SelectionStart == wxobj.Text.Length && wxobj.SelectionLength == 0 && !_KeyPressing)
                    {
                        tRequest = new TraversalRequest(FocusNavigationDirection.Right);
                    }
                    break;
            }


            if (tRequest != null)
            {
                wxobj.MoveFocus(tRequest);
                e.Handled = true;
            }
			else
			{
				_KeyPressing = true;
			}


		}

        private void wxTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            _KeyPressing = false;
        }


        private void Button_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var wxobj = e.OriginalSource as RepeatButton;
            if (wxobj == null) return;
            if (Keyboard.Modifiers != 0) return;

            TraversalRequest tRequest = null;
            switch (e.Key)
            {
                case Key.Left :
                    if (wxobj != wxMinus)
                    {
                        Keyboard.Focus(wxMinus);
                        e.Handled = true;
                    }
                    break;

                case Key.Right:
                    if (wxobj != wxPlus)
                    {
                        Keyboard.Focus(wxPlus);
                        e.Handled = true;
                    }
                    else
                    {
                        tRequest = new TraversalRequest(FocusNavigationDirection.Right);
                    }
                    break;

                case Key.Up: tRequest = new TraversalRequest(FocusNavigationDirection.Up); break;

                case Key.Down: tRequest = new TraversalRequest(FocusNavigationDirection.Down); break;
            }

            if (tRequest != null)
            {
                wxobj.MoveFocus(tRequest);
                e.Handled = true;
            }

        }

    }
}
