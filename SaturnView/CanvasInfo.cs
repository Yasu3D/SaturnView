using SkiaSharp;

namespace SaturnView;

public class CanvasInfo
{
    /// <summary>
    /// Width of the canvas control in pixels.
    /// </summary>
    public float Width;

    /// <summary>
    /// Height of the canvas control in pixels.
    /// </summary>
    public float Height;

    /// <summary>
    /// Half the width of the canvas control in pixels.
    /// </summary>
    public float Radius;

    /// <summary>
    /// The radius to the center of the judgement line in pixels.
    /// </summary>
    public float JudgementLineRadius => Radius * 0.913f;

    /// <summary>
    /// The relative scale of the canvas.
    /// </summary>
    public float Scale3D => Width / 1060;

    /// <summary>
    /// The relative horizontal scale of the canvas.
    /// </summary>
    public float ScaleX => Width / 1060;
    
    /// <summary>
    /// The relative vertical scale of the canvas.
    /// </summary>
    public float ScaleY => Height / 1060;

    /// <summary>
    /// The center point of the canvas.
    /// </summary>
    public SKPoint Center;

    /// <summary>
    /// The background color of the canvas.
    /// </summary>
    public SKColor BackgroundColor;
}