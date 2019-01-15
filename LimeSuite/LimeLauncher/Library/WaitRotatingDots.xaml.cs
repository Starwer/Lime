/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     23-01-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WPFhelper
{
    /// <summary>
    /// Interaction logic for WaitRotatingDots.xaml
    /// </summary>
    public partial class WaitRotatingDots : UserControl
    {

        public WaitRotatingDots()
        {
            InitializeComponent();

            // Start or Stop the animation depending on the control state
            Animate(IsEnabled);
        }


        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool enable = (bool)e.NewValue;
            Visibility = enable ? Visibility.Visible : Visibility.Collapsed;

			// Start or Stop the animation depending on the control state
			Animate(enable);
        }


        /// <summary>
        /// Animate Dot color
        /// </summary>
        /// <param name="enable">Enable or stop the animation</param>
        private void Animate(bool enable)
        {
			// Create Fill gradient from Foreground in the code
			Brush fill = Foreground;
			if (enable)
			{
				if (Foreground is SolidColorBrush brush)
				{
					var color = brush.Color;
					fill = new RadialGradientBrush()
					{
						ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation,
						GradientStops = new GradientStopCollection()
					{
						new GradientStop(color, 0),
						new GradientStop(color, 0.5),
						new GradientStop(Colors.Transparent, 1),
					}
					};
				}

				var wxCanvas = new Canvas()
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Width = 100, 
					Height = 100
				};

				Content = new Viewbox()
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Child = wxCanvas

				};


				for (int index = 0; index < 11; index++)
				{
					// Compute dots positions
					Ellipse ellipse = new Ellipse
					{
						Opacity = 0,
						Width = 12,
						Height = 12
					};

					// Size of the Canvas is 100
					const double center = 40.0;
					const double radius = 25.0;
					const double offset = Math.PI / 2.0;
					const double delta = -2.0 * Math.PI / 10.0;

					double angle = offset + index * delta;

					ellipse.SetValue(Canvas.LeftProperty, center + radius * Math.Sin(angle));
					ellipse.SetValue(Canvas.TopProperty, center + radius * Math.Cos(angle));

					wxCanvas.Children.Add(ellipse);


					// Update the Fill property
					ellipse.Fill = fill;

					// Animate dot (color)
					DoubleAnimation anim = null;
					if (enable)
					{
						anim = new DoubleAnimation(-1.6, 1, new Duration(new TimeSpan(0, 0, 1)), new FillBehavior())
						{
							AutoReverse = true,
							RepeatBehavior = RepeatBehavior.Forever,
							BeginTime = new TimeSpan(0, 0, 0, 0, index * 100)
						};
					}

					ellipse.BeginAnimation(OpacityProperty, anim);

				}

			}
			else if (Content != null)
			{
				// Stop animations right now to save CPU
				var wxCanvas = (Canvas)((Viewbox)Content).Child;
				foreach (var ellipse in wxCanvas.Children)
				{
					((Ellipse)ellipse).BeginAnimation(OpacityProperty, null);
				}

				Content = null;
			}
        }

    }
}
