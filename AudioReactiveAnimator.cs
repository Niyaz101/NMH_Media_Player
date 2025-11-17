using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NMH_Media_Player
{
    public class AudioReactiveAnimator
    {
        private readonly Canvas canvas;
        private readonly List<Shape> shapes;
        private double phase = 0;
        private float smoothedAmplitude = 0; // smooths amplitude for low-volume sounds


        public float CurrentAmplitude { get; set; } // 0..1 normalized
        public bool Enabled { get; set; } = true;

        public AudioReactiveAnimator(Canvas targetCanvas, List<Shape> shapeList)
        {
            canvas = targetCanvas ?? throw new ArgumentNullException(nameof(targetCanvas));
            shapes = shapeList ?? throw new ArgumentNullException(nameof(shapeList));
        }

        public void Animate(double deltaPhase = 0.12)
        {
            if (!Enabled || shapes.Count == 0) return;

            phase += deltaPhase;

            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            // float amp = CurrentAmplitude; // audio amplitude (0..1)

            // Scale quiet sounds and smooth peaks
            float targetAmp = (float)Math.Pow(CurrentAmplitude, 0.5) * 3f; // amplify quiet sounds
            targetAmp = Math.Min(1f, targetAmp); // clamp 0..1

            // Optional smoothing for smoother animation
            float alpha = 0.3f; // smoothing factor (0..1)
            smoothedAmplitude = alpha * targetAmp + (1 - alpha) * smoothedAmplitude;

            float amp = smoothedAmplitude;


            foreach (var s in shapes)
            {
                if (s is Rectangle rect)
                {
                    double baseHeight = 6;
                    double maxHeight = h * 0.7;
                    rect.Height = baseHeight + amp * maxHeight;
                    double initialTop = Canvas.GetTop(rect);
                    bool isTop = initialTop < h / 2;
                    Canvas.SetTop(rect, isTop ? 6 : h - rect.Height - 6);
                    rect.Opacity = 0.5 + 0.5 * amp;
                }
                else if (s is Ellipse el)
                {
                    double baseSize = 10;
                    double maxSize = 80;
                    double newSize = baseSize + amp * maxSize;
                    el.Width = el.Height = newSize;
                    el.Opacity = 0.4 + 0.6 * amp;
                }
                else if (s is Polyline poly)
                {
                    poly.Points.Clear();
                    int steps = Math.Max(40, (int)(w / 8));
                    for (int j = 0; j < steps; j++)
                    {
                        double x = (j / (double)(steps - 1)) * w;
                        double y = (h / 2) + Math.Sin(phase * 0.8 + j * 0.14) * (40 + amp * 100); // was 30 + amp*80
                        poly.Points.Add(new Point(x, y));
                    }
                    poly.Opacity = 0.6 + 0.4 * amp;
                }
            }
        }
    }
}
