using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NMH_Media_Player.Modules.Handlers
{
    public static class SliderHandler
    {
        // Update fill and thumb based on slider value
        public static void UpdateSliderVisual(Slider slider)
        {
            if (slider.Template == null) return;

            var fill = (Rectangle)slider.Template.FindName("TrackFill", slider);
            var thumb = (Thumb)slider.Template.FindName("PART_Thumb", slider);
            var container = (Canvas)slider.Template.FindName("PART_Container", slider);

            if (fill == null || thumb == null || container == null) return;

            double ratio = slider.Value / slider.Maximum;
            double fullWidth = container.ActualWidth;

            fill.Width = ratio * fullWidth;
            double thumbPos = ratio * fullWidth - thumb.Width / 2;
            if (thumbPos < 0) thumbPos = 0;
            Canvas.SetLeft(thumb, thumbPos);
        }

        // Called when user starts dragging
        public static void OnSliderMouseDown(ref bool isDragging)
        {
            isDragging = true;
        }

        // Called when user releases thumb
        public static void OnSliderMouseUp(Slider slider, ref bool isDragging, MediaElement player)
        {
            player.Position = TimeSpan.FromSeconds(slider.Value);
            isDragging = false;
        }

        // Called during thumb drag
        public static void OnThumbDrag(Slider slider, DragDeltaEventArgs e)
        {
            var thumb = (Thumb)slider.Template.FindName("PART_Thumb", slider);
            var container = (Canvas)slider.Template.FindName("PART_Container", slider);
            var fill = (Rectangle)slider.Template.FindName("TrackFill", slider);

            if (thumb == null || container == null || fill == null) return;

            double newLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;
            newLeft = Math.Max(0, Math.Min(newLeft, container.ActualWidth - thumb.Width));

            Canvas.SetLeft(thumb, newLeft);

            double ratio = (newLeft + thumb.Width / 2) / container.ActualWidth;
            slider.Value = ratio * slider.Maximum;

            fill.Width = ratio * container.ActualWidth;
        }

        // Called when user clicks on slider track
        public static void OnSliderClick(Slider slider, MediaElement player, MouseButtonEventArgs e)
        {
            var container = (Canvas)slider.Template.FindName("PART_Container", slider);
            if (container == null) return;

            Point clickPoint = e.GetPosition(container);
            double ratio = clickPoint.X / container.ActualWidth;
            ratio = Math.Max(0, Math.Min(1, ratio));

            slider.Value = ratio * slider.Maximum;
            UpdateSliderVisual(slider);

            player.Position = TimeSpan.FromSeconds(slider.Value);
        }
    }
}
