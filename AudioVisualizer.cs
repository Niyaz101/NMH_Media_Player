using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave;

namespace NMH_Media_Player
{
    public class AudioVisualizer
    {
        #region Fields

        private readonly Canvas canvas;
        private readonly DispatcherTimer timer;
        private readonly Random rnd = new Random();
        private int preset = 0;
        private double phase = 0;
        private List<Shape> shapes = new List<Shape>();
        private bool running = false;

        private List<MovingTextBlock> movingTextBlocks = new List<MovingTextBlock>();
        private string[] words = { "Niyaz", "Mohammad", "Hairan" };
        private int maxCopiesPerWord = 10;

        private AudioReactiveAnimator? audioAnimator; // nullable
        private float currentAmplitude = 0;

        private WasapiLoopbackCapture? capture;
        public bool IsCapturing => capture != null;

        #endregion

        #region Constructor

        public AudioVisualizer(Canvas targetCanvas)
        {
            canvas = targetCanvas ?? throw new ArgumentNullException(nameof(targetCanvas));
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) }; // ~25 FPS
            timer.Tick += Timer_Tick;
        }

        #endregion

        #region Public Methods

        public bool IsRunning => running;

        public void SetPreset(int idx)
        {
            preset = Math.Max(0, Math.Min(9, idx));
            RestartLayout();
        }

        public void Start()
        {
            if (running) return;

            canvas.Visibility = Visibility.Visible;
            canvas.IsHitTestVisible = false;

            RestartLayout();

            audioAnimator = new AudioReactiveAnimator(canvas, shapes)
            {
                Enabled = true
            };

            running = true;
            timer.Start();
        }

        public void Stop()
        {
            if (!running) return;

            timer.Stop();
            running = false;

            audioAnimator?.Enabled = false;
            StopAudioCapture();

            canvas.Children.Clear();
            shapes.Clear();
            movingTextBlocks.Clear();
            canvas.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Audio Capture (NAudio)

        public void StartAudioCapture()
        {
            try
            {
                capture = new WasapiLoopbackCapture();
                capture.DataAvailable += Capture_DataAvailable;
                capture.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Audio capture failed: " + ex.Message);
            }
        }

        public void StopAudioCapture()
        {
            try
            {
                if (capture != null)
                {
                    capture.DataAvailable -= Capture_DataAvailable;
                    capture.StopRecording();
                    capture.Dispose();
                    capture = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Audio capture stop failed: " + ex.Message);
            }
        }

        private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                float sum = 0;
                int bytesPerSample = 4;
                int samples = e.BytesRecorded / bytesPerSample;

                for (int i = 0; i < samples; i++)
                {
                    float sample = BitConverter.ToSingle(e.Buffer, i * bytesPerSample);
                    sum += sample * sample;
                }

                float rms = (float)Math.Sqrt(sum / samples);
                currentAmplitude = Math.Min(1f, rms * 8f);
            }
            catch { currentAmplitude = 0; }
        }

        #endregion

        #region Layout / Shape Initialization

        private void RestartLayout()
        {
            // Clear everything
            canvas.Children.Clear();
            shapes.Clear();
            movingTextBlocks.Clear();
            phase = 0;

            switch (preset)
            {
                case 0: CreateCenterName(); break;
                case 1: CreateWaveLines(50); break;
                case 2: CreateCircles(80); break;
                case 3: CreateRadialBars(280); break;
                case 4: CreateParticles(600); break;
                case 5: CreateBars(40, 2.0, mirror: true); break;
                case 6: CreateCircularWave(50); break;
                case 7: CreateRandomizedCircles(62); break; // preset 7
                case 8: CreateBars(82); break;
                case 9: CreateSpiral(100); break;
                default: CreateBars(50); break;
            }
        }

        #endregion

        #region Shape Factories

        private void CreateBars(int count, double spacingFactor = 2.0, bool mirror = false)
{
    double w = Math.Max(100, canvas.ActualWidth);
    double h = Math.Max(60, canvas.ActualHeight);
    double barWidth = Math.Max(4, w / (count * spacingFactor));

    for (int i = 0; i < count; i++)
    {
        // Generate a random color for this bar
        Color barColor = Color.FromRgb(
            (byte)rnd.Next(50, 256),
            (byte)rnd.Next(50, 256),
            (byte)rnd.Next(50, 256)
        );

        var rect = new Rectangle
        {
            Width = barWidth,
            Height = 4,
            RadiusX = 2,
            RadiusY = 2,
            Fill = new SolidColorBrush(barColor),
            Opacity = 0.9
        };

        double left = (w - (count * (barWidth + 2))) / 2 + i * (barWidth + 2);
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, h - rect.Height - 6);
        canvas.Children.Add(rect);
        shapes.Add(rect);

        if (!mirror) continue;

        var rect2 = new Rectangle
        {
            Width = barWidth,
            Height = 4,
            RadiusX = 2,
            RadiusY = 2,
            Fill = new SolidColorBrush(barColor),
            Opacity = 0.8
        };
        Canvas.SetLeft(rect2, left);
        Canvas.SetTop(rect2, 6);
        canvas.Children.Add(rect2);
        shapes.Add(rect2);
    }
}


        private void CreateWaveLines(int lines)
        {
            double w = Math.Max(100, canvas.ActualWidth);
            double h = Math.Max(60, canvas.ActualHeight);

            for (int j = 0; j < lines; j++)
            {
                var path = new Polyline
                {
                    Stroke = new SolidColorBrush(Color.FromRgb((byte)rnd.Next(120, 255), (byte)rnd.Next(120, 255), (byte)rnd.Next(120, 255))),
                    StrokeThickness = 2,
                    Opacity = 0.9
                };
                canvas.Children.Add(path);
                shapes.Add(path);
            }
        }

        private void CreateCircles(int count = 80)
        {
            shapes.Clear();
            double w = canvas.ActualWidth / 2;
            double h = canvas.ActualHeight / 2;

            for (int i = 0; i < count; i++)
            {
                double radius = 40 + rnd.NextDouble() * (Math.Min(w, h) / 2 - 20); // random radius
                var el = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    StrokeThickness = 2,
                    Opacity = 0.7,
                };

                Color c = Color.FromRgb((byte)rnd.Next(50, 256), (byte)rnd.Next(50, 256), (byte)rnd.Next(50, 256));
                el.Stroke = new SolidColorBrush(c);

                el.Tag = new CircularWaveInfo
                {
                    Angle = rnd.NextDouble() * 2 * Math.PI,
                    Radius = radius,
                    BaseSize = 15 + rnd.NextDouble() * 6,   // random size
                    Speed = 0.01 + rnd.NextDouble() * 0.03, // random speed
                    OrbitDirection = (rnd.NextDouble() > 0.5 ? 1 : -1),
                    Color = c
                };

                double x = w + Math.Cos(((CircularWaveInfo)el.Tag).Angle) * radius - el.Width / 2;
                double y = h + Math.Sin(((CircularWaveInfo)el.Tag).Angle) * radius - el.Height / 2;

                Canvas.SetLeft(el, x);
                Canvas.SetTop(el, y);

                canvas.Children.Add(el);
                shapes.Add(el);
            }
        }


        private void CreateRadialBars(int count)
        {
            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            double cx = w / 2;
            double cy = h / 2;
            double radius = Math.Min(w, h) / 4;

            for (int i = 0; i < count; i++)
            {
                var color = Color.FromRgb((byte)rnd.Next(50, 256), (byte)rnd.Next(50, 256), (byte)rnd.Next(50, 256));
                var rect = new Rectangle
                {
                    Width = 4,
                    Height = 20,
                    Fill = new SolidColorBrush(color),
                    RenderTransformOrigin = new Point(0.5, 1)
                };
                double angle = i * (360.0 / count);
                rect.RenderTransform = new RotateTransform(angle);
                Canvas.SetLeft(rect, cx - rect.Width / 2);
                Canvas.SetTop(rect, cy - rect.Height);
                canvas.Children.Add(rect);
                shapes.Add(rect);
            }
        }

        private void CreateParticles(int count = 400)
        {
            shapes.Clear();
            double centerX = canvas.ActualWidth / 2;
            double centerY = canvas.ActualHeight / 2;

            for (int i = 0; i < count; i++)
            {
                var el = new Ellipse
                {
                    Width = 4 + rnd.NextDouble() * 4,  // random small size
                    Height = 4 + rnd.NextDouble() * 4,
                    Fill = new SolidColorBrush(Color.FromRgb(
                        (byte)rnd.Next(100, 256),
                        (byte)rnd.Next(100, 256),
                        (byte)rnd.Next(100, 256))),
                    Opacity = 0.8
                };

                // Assign ParticleInfo to Tag
                el.Tag = new ParticleInfo
                {
                    X = centerX,
                    Y = centerY,
                    BaseColor = ((SolidColorBrush)el.Fill).Color,
                    Initialized = false
                };

                // Start at center
                Canvas.SetLeft(el, centerX - el.Width / 2);
                Canvas.SetTop(el, centerY - el.Height / 2);

                canvas.Children.Add(el);
                shapes.Add(el);
            }
        }


        private void CreateCircularWave(int dummy = 0)
        {
            shapes.Clear();
            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            double centerX = w / 2;
            double centerY = h / 2;

            int count = 90;
            for (int i = 0; i < count; i++)
            {
                double radius = 10 + i * 6;
                var el = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Stroke = Brushes.Cyan,
                    StrokeThickness = 2,
                    Opacity = 0.6
                };
                el.Tag = new CircularWaveInfo
                {
                    Angle = rnd.NextDouble() * 2 * Math.PI,
                    Radius = radius,
                    BaseSize = el.Width
                };
                double x = centerX + Math.Cos(((CircularWaveInfo)el.Tag).Angle) * radius - el.Width / 2;
                double y = centerY + Math.Sin(((CircularWaveInfo)el.Tag).Angle) * radius - el.Height / 2;

                Canvas.SetLeft(el, x);
                Canvas.SetTop(el, y);
                canvas.Children.Add(el);
                shapes.Add(el);
            }
        }

        private void CreateRandomizedCircles(int count = 62)
        {
            shapes.Clear();
            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;

            for (int i = 0; i < count; i++)
            {
                var el = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Stroke = Brushes.Cyan,
                    StrokeThickness = 1.5,
                    Opacity = 0.5
                };

                Canvas.SetLeft(el, rnd.NextDouble() * w);
                Canvas.SetTop(el, rnd.NextDouble() * h);

                el.Tag = new CircularWaveInfo
                {
                    Angle = rnd.NextDouble() * 2 * Math.PI,
                    Radius = rnd.NextDouble() * 20,
                    BaseSize = el.Width
                };

                canvas.Children.Add(el);
                shapes.Add(el);
            }
        }

        private void CreateCenterName()
        {
            movingTextBlocks.Clear();
            canvas.Children.Clear();

            double canvasCenterX = canvas.ActualWidth / 2;
            double canvasCenterY = canvas.ActualHeight / 2;

            foreach (var word in words)
            {
                for (int c = 0; c < maxCopiesPerWord; c++)
                {
                    var tb = new TextBlock
                    {
                        Text = word,
                        FontSize = rnd.Next(24, 48),
                        FontWeight = (rnd.NextDouble() > 0.5) ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = new SolidColorBrush(Color.FromRgb(
                            (byte)rnd.Next(50, 256),
                            (byte)rnd.Next(50, 256),
                            (byte)rnd.Next(50, 256)))
                    };

                    Canvas.SetLeft(tb, canvasCenterX - tb.ActualWidth / 2);
                    Canvas.SetTop(tb, canvasCenterY - tb.ActualHeight / 2);

                    canvas.Children.Add(tb);

                    var angle = rnd.NextDouble() * 2 * Math.PI;
                    double speed = rnd.NextDouble() * 3 + 1;
                    movingTextBlocks.Add(new MovingTextBlock
                    {
                        TextBlock = tb,
                        Velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed)
                    });
                }
            }
        }

        private void CreateSpiral(int segments)
        {
            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            for (int i = 0; i < segments; i++)
            {
                var el = new Ellipse { Width = 6, Height = 6, Stroke = Brushes.Magenta, StrokeThickness = 1.5, Opacity = 0.6 };
                el.Tag = new CircularWaveInfo { Angle = i * 0.2, Radius = i * 2, BaseSize = el.Width };
                canvas.Children.Add(el);
                shapes.Add(el);
            }
        }

        #endregion

        #region Animation Methods

        private void Timer_Tick(object? sender, EventArgs e)
        {
            phase += 0.05;

            float amp = currentAmplitude;

            switch (preset)
            {
                case 0: AnimateCenterName(amp); break;
                case 1: AnimateWaveLines(amp); break;
                case 2: AnimateCircles(amp); break;
                case 3: AnimateRadialBars(amp); break;
                case 4: AnimateParticles(amp); break;
                case 5: AnimateBars(amp); break;
                case 6: AnimateCircularWave(amp); break;
                case 7: AnimateRandomizedCircles(amp); break;
                case 8: AnimateBars(amp); break;
                case 9: AnimateSpiral(amp); break;
            }
        }

        private void AnimateBars(float amp)
        {
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            int barCount = shapes.OfType<Rectangle>().Count();

            if (barCount == 0) return;

            double spacing = canvasWidth / barCount;

            int i = 0;
            foreach (var s in shapes.OfType<Rectangle>())
            {
                // Base height scaled by amplitude and some random variation
                double targetHeight = 20 + 200 * amp * (0.5 + rnd.NextDouble());

                // Smooth interpolation for height
                s.Height = s.Height + (targetHeight - s.Height) * 0.2;

                // Y position so bar grows from bottom
                Canvas.SetTop(s, canvasHeight - s.Height);

                // X position evenly spaced
                Canvas.SetLeft(s, i * spacing + spacing / 4);

                // Optional: pulsate opacity
                s.Opacity = 0.3 + 0.7 * amp;

                // Optional: dynamic color based on amplitude
                Color baseColor = ((SolidColorBrush)s.Fill).Color;
                byte r = (byte)Math.Min(255, baseColor.R + amp * 150);
                byte g = (byte)Math.Min(255, baseColor.G + amp * 150);
                byte b = (byte)Math.Min(255, baseColor.B + amp * 150);
                s.Fill = new SolidColorBrush(Color.FromRgb(r, g, b));

                i++;
            }
        }


        private void AnimateWaveLines(float amp)
        {
            if (shapes.Count == 0) return;

            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            int layers = shapes.OfType<Polyline>().Count(); // each Polyline is a layer
            double timeFactor = phase * 0.05; // slow phase movement for smooth wave
            int points = 100; // more points = smoother curve

            int layerIndex = 0;
            foreach (var s in shapes.OfType<Polyline>())
            {
                var pl = s;
                pl.Points.Clear();

                // Calculate dynamic wave amplitude for this layer
                double layerAmp = 20 + layerIndex * 5; // different layers have different base heights
                double speedMultiplier = 0.5 + layerIndex * 0.1; // speed difference per layer

                // Color cycling based on layer
                if (pl.Stroke is SolidColorBrush brush)
                {
                    byte r = (byte)(50 + (Math.Sin(phase * 0.1 + layerIndex) + 1) * 100);
                    byte g = (byte)(50 + (Math.Cos(phase * 0.08 + layerIndex) + 1) * 100);
                    byte b = (byte)(150 + (Math.Sin(phase * 0.12 + layerIndex) + 1) * 50);
                    brush.Color = Color.FromRgb(r, g, b);
                }

                for (int i = 0; i < points; i++)
                {
                    double x = i * (w / (points - 1));

                    // Base wave formula
                    double y = h / 2
                               + Math.Sin(i * 0.2 + timeFactor * speedMultiplier + layerIndex) * layerAmp * amp // audio scaling
                               + Math.Cos(phase * 0.1 + i * 0.3) * 10 * amp; // small secondary motion

                    // Optional: subtle vertical offset for randomness
                    y += Math.Sin(i * 0.05 + layerIndex) * 5;

                    pl.Points.Add(new Point(x, y));
                }

                // Optional: vary stroke thickness with audio
                pl.StrokeThickness = 1.5 + amp * 3;

                // Fade in/out with audio
                pl.Opacity = 0.3 + 0.7 * amp;

                layerIndex++;
            }

            // Slowly increment phase for continuous animation
            phase += 0.08;
        }


        
        private void AnimateCircles(float amp)
        {
            double centerX = canvas.ActualWidth / 2;
            double centerY = canvas.ActualHeight / 2;

            foreach (var s in shapes.OfType<Ellipse>())
            {
                if (s.Tag is not CircularWaveInfo info) continue;

                // Update angle by speed and audio amplitude
                info.Angle += info.Speed * info.OrbitDirection * (1 + amp * 2);

                // Pulsing size with audio
                double scale = info.BaseSize * (1 + amp * 1.5);
                s.Width = s.Height = scale;

                // Orbit around center
                double x = centerX + Math.Cos(info.Angle) * info.Radius - s.Width / 2;
                double y = centerY + Math.Sin(info.Angle) * info.Radius - s.Height / 2;

                Canvas.SetLeft(s, x);
                Canvas.SetTop(s, y);

                // Optional: pulsate opacity
                s.Opacity = 0.5 + 0.5 * Math.Abs(Math.Sin(info.Angle * 2 + amp * 5));
            }
        }


        private void AnimateRadialBars(float amp)
        {
            double cx = canvas.ActualWidth / 2;
            double cy = canvas.ActualHeight / 2;

            int count = shapes.OfType<Rectangle>().Count();
            if (count == 0) return;

            double maxRadius = Math.Min(cx, cy); // max distance from center
            for (int i = 0; i < count; i++)
            {
                if (shapes[i] is not Rectangle rect) continue;

                // Calculate angle for radial placement
                double angle = i * (360.0 / count);
                double rad = angle * Math.PI / 180;

                // Make bars longer, scale by canvas size and audio amplitude
                double baseLength = maxRadius * 0.6 + amp * maxRadius * 0.4;
                double dynamicLength = baseLength + Math.Sin(phase + i * 0.3) * (maxRadius * 0.1);

                rect.Height = dynamicLength;
                rect.Width = Math.Max(6, canvas.ActualWidth / (count * 2)); // make bars thicker

                // Color gradient based on position
                byte r = (byte)(128 + 127 * Math.Sin(i + phase));
                byte g = (byte)(128 + 127 * Math.Cos(i + phase));
                byte b = (byte)(255 - i * 255 / count);
                rect.Fill = new SolidColorBrush(Color.FromRgb(r, g, b));

                // Rotate around bottom center
                rect.RenderTransformOrigin = new Point(0.5, 1);
                rect.RenderTransform = new RotateTransform(angle + phase * 10); // orbit rotation

                // Position bars at center
                Canvas.SetLeft(rect, cx - rect.Width / 2);
                Canvas.SetTop(rect, cy - rect.Height);
            }
        }



        private void AnimateParticles(float amp)
        {
            double cx = canvas.ActualWidth / 2;
            double cy = canvas.ActualHeight / 2;

            foreach (var s in shapes.OfType<Ellipse>())
            {
                if (s.Tag is not ParticleInfo info) continue;

                // Start from center if first frame
                if (!info.Initialized)
                {
                    info.X = cx;
                    info.Y = cy;
                    // random direction
                    double angle = rnd.NextDouble() * 2 * Math.PI;
                    info.Velocity = new Vector(Math.Cos(angle), Math.Sin(angle));
                    info.Initialized = true;
                }

                // Move particle in its direction, scaled by audio amplitude
                double speed = 1 + amp * 10; // audio-reactive speed
                info.X += info.Velocity.X * speed;
                info.Y += info.Velocity.Y * speed;

                // Bounce off edges (optional)
                if (info.X < 0 || info.X > canvas.ActualWidth - s.Width) info.Velocity = new Vector(-info.Velocity.X, info.Velocity.Y);
                if (info.Y < 0 || info.Y > canvas.ActualHeight - s.Height) info.Velocity = new Vector(info.Velocity.X, -info.Velocity.Y);

                // Update particle position
                Canvas.SetLeft(s, info.X);
                Canvas.SetTop(s, info.Y);

                // Pulsate size by audio
                double scale = 2 + amp * 4;
                s.Width = s.Height = scale;

                // Optional: color changes
                byte r = (byte)(info.BaseColor.R * (0.5 + amp / 2));
                byte g = (byte)(info.BaseColor.G * (0.5 + amp / 2));
                byte b = (byte)(info.BaseColor.B * (0.5 + amp / 2));
                s.Fill = new SolidColorBrush(Color.FromRgb(r, g, b));
            }
        }


        private void AnimateCircularWave(float amp)
        {
            double cx = canvas.ActualWidth / 2;
            double cy = canvas.ActualHeight / 2;

            foreach (var s in shapes.OfType<Ellipse>())
            {
                if (s.Tag is not CircularWaveInfo info) continue;
                double r = info.Radius + Math.Sin(phase + info.Angle) * 20 * (0.5 + amp);
                double x = cx + Math.Cos(info.Angle + phase) * r - s.Width / 2;
                double y = cy + Math.Sin(info.Angle + phase) * r - s.Height / 2;
                Canvas.SetLeft(s, x);
                Canvas.SetTop(s, y);
                s.Width = s.Height = info.BaseSize + 8 * amp;
                s.Opacity = 0.4 + 0.6 * amp;
            }
        }

        private void AnimateRandomizedCircles(float amp)
        {
            foreach (var s in shapes.OfType<Ellipse>())
            {
                if (s.Tag is not CircularWaveInfo info) continue;

                double x = Canvas.GetLeft(s) + Math.Cos(phase + info.Angle) * 5 * amp;
                double y = Canvas.GetTop(s) + Math.Sin(phase + info.Angle) * 5 * amp;

                Canvas.SetLeft(s, Math.Max(0, Math.Min(canvas.ActualWidth - s.Width, x)));
                Canvas.SetTop(s, Math.Max(0, Math.Min(canvas.ActualHeight - s.Height, y)));

                s.Width = s.Height = info.BaseSize + 8 * amp;
                s.Opacity = 0.3 + 0.7 * amp;
            }
        }

        private void AnimateSpiral(float amp)
        {
            double cx = canvas.ActualWidth / 2;
            double cy = canvas.ActualHeight / 2;
            double maxRadius = Math.Min(cx, cy) - 20; // leave some margin

            foreach (var s in shapes.OfType<Ellipse>())
            {
                if (s.Tag is not CircularWaveInfo info) continue;

                // Slowly increase radius but clamp to maxRadius
                info.Radius += 0.1 + amp;
                if (info.Radius > maxRadius) info.Radius = 20 + rnd.NextDouble() * 30; // reset to inner spiral

                // Slowly rotate each element
                info.Angle += 0.01 + amp * 0.05;

                // Compute position
                double x = cx + Math.Cos(info.Angle + phase) * info.Radius - s.Width / 2;
                double y = cy + Math.Sin(info.Angle + phase) * info.Radius - s.Height / 2;
                Canvas.SetLeft(s, x);
                Canvas.SetTop(s, y);

                // Pulsing size by audio
                double scale = info.BaseSize * (1 + amp * 1.5);
                s.Width = s.Height = scale;

                // Optional: pulsate opacity
                s.Opacity = 0.5 + 0.5 * Math.Abs(Math.Sin(info.Angle * 2 + amp * 5));
            }
        }


        private void AnimateCenterName(float amp)
        {
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;

            foreach (var m in movingTextBlocks)
            {
                var tb = m.TextBlock;

                // Calculate new position scaled by audio amplitude
                double posX = Canvas.GetLeft(tb) + m.Velocity.X * amp * 2;
                double posY = Canvas.GetTop(tb) + m.Velocity.Y * amp * 2;

                // Bounce off left/right edges
                if (posX < 0)
                {
                    posX = 0;
                    m.Velocity = new Vector(-m.Velocity.X, m.Velocity.Y);
                }
                else if (posX > canvasWidth - tb.ActualWidth)
                {
                    posX = canvasWidth - tb.ActualWidth;
                    m.Velocity = new Vector(-m.Velocity.X, m.Velocity.Y);
                }

                // Bounce off top/bottom edges
                if (posY < 0)
                {
                    posY = 0;
                    m.Velocity = new Vector(m.Velocity.X, -m.Velocity.Y);
                }
                else if (posY > canvasHeight - tb.ActualHeight)
                {
                    posY = canvasHeight - tb.ActualHeight;
                    m.Velocity = new Vector(m.Velocity.X, -m.Velocity.Y);
                }

                // Apply updated position
                Canvas.SetLeft(tb, posX);
                Canvas.SetTop(tb, posY);

                // Optional: add slight rotation for more dynamic effect
                tb.RenderTransform = new RotateTransform(
                    (rnd.NextDouble() - 0.5) * 10 * amp,
                    tb.ActualWidth / 2,
                    tb.ActualHeight / 2
                );
            }
        }


        #endregion

        #region Helper Classes

        private class CircularWaveInfo
        {
            public double Angle { get; set; }
            public double Radius { get; set; }
            public double BaseSize { get; set; }
            public double Speed { get; set; }      // Rotation speed
            public double OrbitDirection { get; set; } // 1 = clockwise, -1 = counter-clockwise
            public Color Color { get; set; }        // Circle color
        }

        private class MovingTextBlock
        {
            public TextBlock TextBlock { get; set; } = null!;
            public Vector Velocity { get; set; }
        }

        private class AudioReactiveAnimator
        {
            public bool Enabled { get; set; } = true;
            public AudioReactiveAnimator(Canvas canvas, List<Shape> shapes) { }
        }


        class ParticleInfo
        {
            public double X;              // Current X position
            public double Y;              // Current Y position
            public Vector Velocity;       // Movement direction
            public Color BaseColor;       // Original color
            public bool Initialized = false; // To initialize velocity only once
        }


        #endregion


    }
}
