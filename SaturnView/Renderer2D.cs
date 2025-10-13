using SkiaSharp;

namespace SaturnView;

public static class Renderer2D
{
    public static void Render(SKCanvas canvas, CanvasInfo canvasInfo)
    {
        canvas.Clear(canvasInfo.BackgroundColor);
        canvas.DrawPoint(canvasInfo.Center, new SKPaint { Color = SKColors.Red, StrokeWidth = 10 } );
    }
}