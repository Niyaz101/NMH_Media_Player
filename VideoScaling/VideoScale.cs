using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NMH_Media_Player.Modules;
using NMH_Media_Player.Handlers;

namespace NMH_Media_Player.VideoScaling
{
    internal class VideoScale
    {
        private readonly MediaController _mediaController;
        private readonly MainWindow _mainWindow;

        public VideoScale(MediaController mediaController, MainWindow mainWindow)
        {
            _mediaController = mediaController;
            _mainWindow = mainWindow;
        }

        // ================== Stretch Modes ==================
        public void SetOriginal()
        {
            ResetContainerTransform();
            _mediaController.Player.Stretch = Stretch.None;
            ViewMenuHandler.ShowToast(_mainWindow, "Scaling mode set to Original.");
        }

        public void SetFit()
        {
            ResetContainerTransform();
            _mediaController.Player.Stretch = Stretch.Uniform;
            ViewMenuHandler.ShowToast(_mainWindow, "Scaling mode set to Fit.");
        }

        public void SetFill()
        {
            ResetContainerTransform();
            _mediaController.Player.Stretch = Stretch.Fill;
            ViewMenuHandler.ShowToast(_mainWindow, "Scaling mode set to Fill.");
        }

        public void SetZoom()
        {
            ResetContainerTransform();
            _mediaController.Player.Stretch = Stretch.UniformToFill;
            ViewMenuHandler.ShowToast(_mainWindow, "Scaling mode set to Zoom.");
        }

        // ================== Aspect Ratios ==================
        public void SetAspectRatio16_9() => ApplyAspectRatio(16.0 / 9.0, "16:9");
        public void SetAspectRatio4_3() => ApplyAspectRatio(4.0 / 3.0, "4:3");
        public void SetAspectRatio16_12() => ApplyAspectRatio(16.0 / 12.0, "16:12");
        public void SetAspectRatio9_6() => ApplyAspectRatio(9.0 / 6.0, "9:6");
        public void SetAspectRatioOriginal()
        {
            double ratio = _mediaController.Player.NaturalVideoHeight > 0
                ? (double)_mediaController.Player.NaturalVideoWidth / _mediaController.Player.NaturalVideoHeight
                : 16.0 / 9.0; // fallback
            ApplyAspectRatio(ratio, "Original");
        }

        private void ApplyAspectRatio(double targetRatio, string label)
        {
            if (_mediaController.Player.Source == null)
            {
                ViewMenuHandler.ShowToast(_mainWindow, "No video loaded to change aspect ratio.");
                return;
            }

            if (_mediaController.Player.Parent is not FrameworkElement container) return;

            ResetContainerTransform();

            double containerWidth = container.ActualWidth;
            double containerHeight = container.ActualHeight;
            double containerRatio = containerWidth / containerHeight;

            if (containerRatio > targetRatio)
            {
                container.Width = containerHeight * targetRatio;
                container.Height = containerHeight;
            }
            else
            {
                container.Width = containerWidth;
                container.Height = containerWidth / targetRatio;
            }

            ViewMenuHandler.ShowToast(_mainWindow, $"Aspect ratio set to {label}.");
        }

        // ================== Utility ==================
        private void ResetContainerTransform()
        {
            if (_mediaController.Player.Parent is FrameworkElement container)
            {
                container.Width = double.NaN;  // reset width to auto
                container.Height = double.NaN; // reset height to auto
                container.LayoutTransform = Transform.Identity;
            }
        }
    }
}
