using SkiaSharp;

namespace SaturnView;

public static class Renderer3D
{
    public static void Render(SKCanvas canvas, CanvasInfo canvasInfo, SKColor clearColor)
    {
        canvas.Clear(clearColor);
        canvas.DrawCircle(canvasInfo.Center, canvasInfo.Radius, new() { Color = SKColors.MediumPurple });
    }
}