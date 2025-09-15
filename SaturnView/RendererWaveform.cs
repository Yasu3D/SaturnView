using SkiaSharp;

namespace SaturnView;

public static class RendererWaveform
{
    private static readonly SKPaint WaveformPaint = new()
    {
        Color = new(0xFF, 0xFF, 0xFF, 0x60),
        StrokeWidth = 1,
        IsAntialias = false,
    };
    
    public static void RenderSeekSlider(SKCanvas canvas, CanvasInfo canvasInfo, SKColor clearColor, float[]? waveform)
    {
        canvas.Clear(clearColor);
        
        if (canvasInfo.Width < 1) return;
        
        for (int x = 0; x < canvasInfo.Width; x += 2)
        {
            float t = x / canvasInfo.Width;

            float sample = 0;

            if (waveform != null)
            {
                int sampleIndex = (int)(t * waveform.Length);
                sample = waveform[sampleIndex];
            }
            
            float y0 = (canvasInfo.Height + canvasInfo.Height * sample * 0.65f) * 0.5f;
            float y1 = (canvasInfo.Height - canvasInfo.Height * sample * 0.65f) * 0.5f;

            if (y0 - y1 < 1)
            {
                y0 -= 1;
                y1 += 1;
            }

            canvas.DrawLine(x, y0, x, y1, WaveformPaint);
        }
    }
}