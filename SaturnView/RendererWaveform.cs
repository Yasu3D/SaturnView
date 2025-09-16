using SkiaSharp;

namespace SaturnView;

public static class RendererWaveform
{
    private static readonly SKPaint WaveformPaint = new()
    {
        StrokeWidth = 1,
        IsAntialias = false,
    };
    
    public static void RenderSeekSlider(SKCanvas canvas, CanvasInfo canvasInfo, SKColor clearColor, SKColor waveformColor, float[]? waveform, float audioOffset, float audioLength, float sliderLength)
    {
        canvas.Clear(clearColor);
        
        if (canvasInfo.Width < 1) return;
        
        for (int x = 0; x < canvasInfo.Width; x += 2)
        {
            float sample = 0;
            
            if (waveform != null && waveform.Length != 0 && audioLength != 0)
            {
                // Get 0-1 progress across slider.
                float t = x / canvasInfo.Width;
                
                // Rescale to fit slider properly.
                t *= sliderLength / audioLength;
                
                // Apply audio offset.
                t -= audioOffset / audioLength;
            
                int sampleIndex = (int)(t * waveform.Length);

                sample = sampleIndex < 0 || sampleIndex >= waveform.Length ? 0 : waveform[sampleIndex];
            }
            
            float y0 = (canvasInfo.Height + canvasInfo.Height * sample * 0.65f) * 0.5f;
            float y1 = (canvasInfo.Height - canvasInfo.Height * sample * 0.65f) * 0.5f;

            if (y0 - y1 < 1)
            {
                y0 -= 1;
                y1 += 1;
            }

            WaveformPaint.Color = waveformColor;
            canvas.DrawLine(x, y0, x, y1, WaveformPaint);
        }
    }
}