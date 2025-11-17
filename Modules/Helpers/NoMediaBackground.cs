using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NMH_Media_Player.Modules.Helpers
{
    /// <summary>
    /// Provides a dynamic animated background used when no media is playing.
    /// Displays gradient-animated text, twinkling stars, and random shooting stars.
    /// </summary>
    internal class NoMediaBackgroundGrid
    {
        // --------------------------------------------------------------------
        // 🔷  Fields and Constants
        // --------------------------------------------------------------------
        private readonly Grid grid;
        private readonly TextBlock centerText = new();
        private readonly TextBlock footerText = new();
        private readonly DispatcherTimer shootingStarTimer = new();

        private readonly Random random = new();
       

        private const int StarsCount = 500;
        private const double BaseWidth = 1280;
        private const double BaseHeight = 720;

        // --------------------------------------------------------------------
        // 🔷  Constructor
        // --------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the NoMediaBackgroundGrid class.
        /// </summary>
        /// <param name="grid">The parent grid control where animations will render.</param>
        public NoMediaBackgroundGrid(Grid grid)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));

            try
            {
                // Create the text elements
                centerText = CreateTextBlock("NMH Media Player", 72, true);
                footerText = CreateTextBlock("Developed By Niyaz Mohammad Hairan", 28, true);

                grid.Children.Clear();
                grid.Children.Add(centerText);
                grid.Children.Add(footerText);

                // Event hooks
                grid.Loaded += Grid_Loaded;
                grid.SizeChanged += Grid_SizeChanged;

                // Shooting star timer
                shootingStarTimer = new DispatcherTimer();
                shootingStarTimer.Tick += ShootingStarTimer_Tick;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization error in NoMediaBackgroundGrid:\n{ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --------------------------------------------------------------------
        // 🔷  Event Handlers
        // --------------------------------------------------------------------
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                AddStarsWithFadeIn();
                StartAnimations();

                if (centerText.Foreground is LinearGradientBrush centerBrush)
                    AnimateGradient(centerBrush);

                if (footerText.Foreground is LinearGradientBrush footerBrush)
                    AnimateGradient(footerBrush);

                ScheduleNextShootingStar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Grid Load Error: {ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                // Remove all old stars
                for (int i = grid.Children.Count - 1; i >= 0; i--)
                    if (grid.Children[i] is Ellipse)
                        grid.Children.RemoveAt(i);

                AddStarsWithFadeIn();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Grid Resize Error: {ex.Message}",
                    "Resize Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --------------------------------------------------------------------
        // 🔷  Text Creation & Animation
        // --------------------------------------------------------------------
        /// <summary>
        /// Creates a stylized gradient TextBlock for use in the background.
        /// </summary>
        private TextBlock CreateTextBlock(string text, double fontSize, bool isCenter = false)
        {
            try
            {
                LinearGradientBrush brush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };

                Color[] colors = new Color[]
                {
                    Colors.BlueViolet, Colors.Orange, Colors.Yellow, Colors.Green,
                    Colors.Lime, Colors.Cyan, Colors.Blue, Colors.Indigo,
                    Colors.Violet, Colors.Magenta
                };

                for (int i = 0; i < colors.Length; i++)
                    brush.GradientStops.Add(new GradientStop(colors[i], i / (double)(colors.Length - 1)));

                var tb = new TextBlock
                {
                    Text = text,
                    FontSize = fontSize,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = brush,
                    HorizontalAlignment = isCenter ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                    VerticalAlignment = isCenter ? VerticalAlignment.Center : VerticalAlignment.Top,
                    TextAlignment = TextAlignment.Center
                };

                tb.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 12,
                    ShadowDepth = 6,
                    Opacity = 0.6
                };

                return tb;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Text creation error: {ex.Message}",
                    "Text Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new TextBlock { Text = "Error Loading Text" };
            }
        }

        /// <summary>
        /// Animates a linear gradient brush to create a flowing color effect.
        /// </summary>
        private void AnimateGradient(LinearGradientBrush brush)
        {
            if (brush == null) return;

            try
            {
                foreach (var stop in brush.GradientStops)
                {
                    var anim = new DoubleAnimation
                    {
                        From = stop.Offset,
                        To = stop.Offset + 1,
                        Duration = TimeSpan.FromSeconds(4 + random.NextDouble() * 2),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    stop.BeginAnimation(GradientStop.OffsetProperty, anim);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gradient animation error: {ex.Message}",
                    "Animation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --------------------------------------------------------------------
        // 🔷  Star Field Generation
        // --------------------------------------------------------------------
        /// <summary>
        /// Populates the grid with small, twinkling star ellipses.
        /// </summary>
        private void AddStarsWithFadeIn()
        {
            try
            {
                double gridWidth = grid.ActualWidth;
                double gridHeight = grid.ActualHeight;

                for (int i = 0; i < StarsCount; i++)
                {
                    var star = new Ellipse
                    {
                        Width = random.Next(2, 6),
                        Height = random.Next(2, 6),
                        Fill = Brushes.White,
                        Opacity = 0,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    double left = random.NextDouble() * (gridWidth - star.Width);
                    double top = random.NextDouble() * (gridHeight - star.Height);
                    star.Margin = new Thickness(left, top, 0, 0);

                    grid.Children.Add(star);
                    Panel.SetZIndex(star, -1);

                    var twinkle = new DoubleAnimation
                    {
                        From = 0,
                        To = random.NextDouble() * 0.8 + 0.2,
                        Duration = TimeSpan.FromSeconds(random.Next(2, 5)),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    star.BeginAnimation(UIElement.OpacityProperty, twinkle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Star creation error: {ex.Message}",
                    "Star Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --------------------------------------------------------------------
        // 🔷  Text and Footer Animations
        // --------------------------------------------------------------------
        /// <summary>
        /// Animates center and footer text positions slowly for a smooth breathing motion.
        /// </summary>
        private void StartAnimations()
        {
            try
            {
                double maxOffset = Math.Max(0, grid.ActualWidth - centerText.ActualWidth);

                var centerAnim = new ThicknessAnimation
                {
                    From = new Thickness(0, 0, 0, 0),
                    To = new Thickness(maxOffset / 2, 0, 0, 0),
                    Duration = TimeSpan.FromSeconds(6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                centerText.BeginAnimation(FrameworkElement.MarginProperty, centerAnim);

                double footerOffsetY = centerText.ActualHeight + 20;
                var footerAnim = new ThicknessAnimation
                {
                    From = new Thickness(0, footerOffsetY, 0, 0),
                    To = new Thickness(maxOffset / 2, footerOffsetY, 0, 0),
                    Duration = centerAnim.Duration,
                    AutoReverse = centerAnim.AutoReverse,
                    RepeatBehavior = centerAnim.RepeatBehavior,
                    EasingFunction = centerAnim.EasingFunction
                };
                footerText.BeginAnimation(FrameworkElement.MarginProperty, footerAnim);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Text animation error: {ex.Message}",
                    "Animation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --------------------------------------------------------------------
        // 🔷  Shooting Star Logic
        // --------------------------------------------------------------------
        private void ScheduleNextShootingStar()
        {
            try
            {
                int nextInterval = random.Next(10, 20) * 1000; // 10–20 seconds
                shootingStarTimer.Interval = TimeSpan.FromMilliseconds(nextInterval);
                shootingStarTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Timer scheduling error: {ex.Message}",
                    "Timer Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShootingStarTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                shootingStarTimer.Stop();
                SpawnShootingStar();
                ScheduleNextShootingStar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Shooting star tick error: {ex.Message}",
                    "Shooting Star Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Creates and animates a shooting star along a random trajectory.
        /// </summary>
        private void SpawnShootingStar()
        {
            try
            {
                double gridWidth = grid.ActualWidth;
                double gridHeight = grid.ActualHeight;

                if (gridWidth <= 0 || gridHeight <= 0) return;

                double startX = random.NextDouble() * gridWidth * 0.5;
                double startY = random.NextDouble() * gridHeight * 0.5;

                double angle = random.NextDouble() * Math.PI / 2;
                double length = random.NextDouble() * (gridWidth * 0.7) + (gridWidth * 0.3);

                double endX = startX + length * Math.Cos(angle);
                double endY = startY + length * Math.Sin(angle);

                if (Math.Abs(endX - startX) < 0.1 && Math.Abs(endY - startY) < 0.1)
                    return;

                var star = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = Brushes.White,
                    Opacity = 0.9
                };

                grid.Children.Add(star);
                Panel.SetZIndex(star, 1);

                // Create path
                var figure = new PathFigure { StartPoint = new Point(startX, startY) };
                figure.Segments.Add(new LineSegment { Point = new Point(endX, endY) });
                var path = new PathGeometry();
                path.Figures.Add(figure);

                var tt = new TranslateTransform();
                star.RenderTransform = tt;

                var animX = new DoubleAnimationUsingPath
                {
                    PathGeometry = path,
                    Duration = TimeSpan.FromSeconds(1 + random.NextDouble()),
                    Source = PathAnimationSource.X,
                    FillBehavior = FillBehavior.Stop
                };

                var animY = new DoubleAnimationUsingPath
                {
                    PathGeometry = path,
                    Duration = animX.Duration,
                    Source = PathAnimationSource.Y,
                    FillBehavior = FillBehavior.Stop
                };

                try
                {
                    tt.BeginAnimation(TranslateTransform.XProperty, animX);
                    tt.BeginAnimation(TranslateTransform.YProperty, animY);
                }
                catch
                {
                    grid.Children.Remove(star);
                    return;
                }

                animX.Completed += (s, e) => grid.Children.Remove(star);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Shooting star creation error: {ex.Message}",
                    "Shooting Star Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --------------------------------------------------------------------
        // 🔷  Visibility Control
        // --------------------------------------------------------------------
        /// <summary>Shows the no-media background.</summary>
        public void Show() => grid.Visibility = Visibility.Visible;

        /// <summary>Hides the no-media background.</summary>
        public void Hide() => grid.Visibility = Visibility.Collapsed;
    }
}
