using SkiaSharp;

namespace SaturnView;

public static class Renderer2D
{
    public static void Render(SKCanvas canvas, CanvasInfo canvasInfo, SKColor clearColor)
    {
        canvas.Clear(clearColor);
        canvas.DrawPoint(canvasInfo.Center, new SKPaint { Color = SKColors.Red, StrokeWidth = 10 } );
    }
}