using SkiaSharp;

namespace SaturnView;

public class CanvasInfo
{
    /// <summary>
    /// Width of the canvas control in pixels.
    /// </summary>
    public float Width;

    /// <summary>
    /// Height of the canvas control in pixels-
    /// </summary>
    public float Height;

    /// <summary>
    /// Half the width of the canvas control in pixels.
    /// </summary>
    public float Radius;

    /// <summary>
    /// The center point of the canvas.
    /// </summary>
    public SKPoint Center;
}