/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     28-01-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System;
using System.Windows.Controls;
using System.Windows.Input;
using Lime;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using WPFhelper;

namespace LimeLauncher.Controls
{
	/// <summary>
	/// Interaction logic for LimeTextBox.xaml
	/// </summary>
	public partial class LimeTextBox : LimeControl
    {
        public LimeTextBox()
        {
            // LimeControl Boilerplate
            InitializeComponent();
            Factory();
        }


        protected override void OnPropertyChanged(LimeProperty prop, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName))
            {
                wxValidation.Source = prop;
            }

            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Multiline")
            {
                if (prop != null && prop.Multiline)
                {
                    wxMain.TextWrapping = TextWrapping.Wrap;
					wxMain.MaxLines = 5;
					if (prop.Type == typeof(string))
					{
						wxMain.SetBinding(SpellCheck.IsEnabledProperty, new Binding("SpellCheck") { Source = Global.User });
					}
					else
					{
						SpellCheck.SetIsEnabled(wxMain, false);
					}
					ToolTip = null;
                }
                else
                {
					wxMain.TextWrapping = TextWrapping.NoWrap;
					SpellCheck.SetIsEnabled(wxMain, false);
					if (HeaderEnabled)
					{
						wxMain.SetBinding(ToolTipProperty, new Binding("Text")
						{
							RelativeSource = new RelativeSource(RelativeSourceMode.Self)
						});
					}
					else
					{
						ToolTip = null;
					}
                }
            }

			if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "ReadOnly")
			{
				wxMain.IsReadOnly = ReadOnly || (prop != null && prop.ReadOnly);
			}

        }

		protected override void OnReadOnlyPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (wxMain.DataContext is LimeProperty prop) {
				wxMain.IsReadOnly = ReadOnly || (prop != null && prop.ReadOnly);
			}
		}

		protected override void OnHeaderEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (wxMain.DataContext is LimeProperty prop)
			{
				if (HeaderEnabled && !prop.Multiline)
				{
					wxMain.SetBinding(ToolTipProperty, new Binding("Text")
					{
						RelativeSource = new RelativeSource(RelativeSourceMode.Self)
					});
				}
				else
				{
					ToolTip = null;
				}
			}
		}

		/// <summary>
		/// Trigger event when text changes
		/// </summary>
		private void wxTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //LimeMsg.Debug("LimeTextBox TextChanged");
            ValueChanged?.Invoke(this, e);

            if (ValidateOnChange)
            {
                // Trigger Validation
                var binding = wxMain.GetBindingExpression(TextBox.TextProperty);
                if (binding != null) binding.UpdateSource();
            }
        }


        /// <summary>
        /// Trigger event when the Text content changes
        /// </summary>
        public event TextChangedEventHandler ValueChanged;




        private bool _KeyPressing = false;


        /// <summary>
        /// Enable to validate/invalidate a TextBox entry when pressing Enter/Esc key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxUpdateSourceOnKey_KeyEvent(object sender, KeyEventArgs e)
		{
			var textBox = sender as TextBox;
			if (textBox == null) return;
			_KeyPressing = false;

			if (Keyboard.Modifiers != 0) return;

			switch (e.Key) {

				case Key.Enter:
					if (textBox.DataContext != null && (textBox.DataContext as LimeProperty)?.Value != textBox.Text)
					{
						if (textBox.TextWrapping == TextWrapping.NoWrap && !textBox.AcceptsReturn)
						{
							e.Handled = true;
							var binding = textBox.GetBindingExpression(TextBox.TextProperty);
							if (binding != null) binding.UpdateSource();
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
			var textBox = sender as TextBox;
			if (textBox == null || textBox.Text == null) return;
			if (Keyboard.Modifiers != 0) return;

			TraversalRequest tRequest = null;
			switch (e.Key)
			{
				case Key.Up:
					if (textBox.SelectionLength == 0 && !_KeyPressing) // Detect first line
					{
						if (textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart) == 0)
							tRequest = new TraversalRequest(FocusNavigationDirection.Up);
					}
					break;

				case Key.Down:
					if (textBox.SelectionLength == 0 && !_KeyPressing) // Detect last line
					{
						if (textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart) == textBox.LineCount - 1)
							tRequest = new TraversalRequest(FocusNavigationDirection.Down);
					}
					break;

				case Key.Left:
					if (textBox.SelectionStart==0 && textBox.SelectionLength==0 && !_KeyPressing)
					{
						tRequest = new TraversalRequest(FocusNavigationDirection.Left);
					}
					break;

				case Key.Right:
					if (textBox.IsReadOnly && !textBox.IsReadOnlyCaretVisible)
					{
						e.Handled = true;
						textBox.IsReadOnlyCaretVisible = true;
					}
					else if (textBox.SelectionStart == textBox.Text.Length && textBox.SelectionLength == 0 && !_KeyPressing)
					{
						tRequest = new TraversalRequest(FocusNavigationDirection.Right);
					}
					break;
			}

			if (tRequest != null)
			{
				e.Handled = true;
				textBox.MoveFocus(tRequest);
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

		private void wxMain_LostFocus(object sender, RoutedEventArgs e)
		{
			_KeyPressing = false;
		}

		private void wxMain_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (Keyboard.FocusedElement != wxMain)
			{
				var wxobj = WPF.FindFirstParent<ScrollViewer>(sender as UIElement);
				if (wxobj != null)
				{
					e.Handled = true;
					var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
					e2.RoutedEvent = MouseWheelEvent;
					wxobj.RaiseEvent(e2);
				}
			}
			
		}
	}
}
