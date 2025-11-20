using System.Windows.Controls;
using System.Windows;
public class MovingTextBlock
{
    public TextBlock TextBlock { get; set; }
    public Vector Velocity { get; set; }        // movement direction/speed
    public double RotationAngle { get; set; }   // current rotation
    public double BaseFontSize { get; set; }    // original font size for scaling with amplitude
}
