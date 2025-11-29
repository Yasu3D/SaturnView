using SaturnData.Notation.Core;
using SaturnData.Notation.Notes;
using SaturnData.Utilities;
using SkiaSharp;

namespace SaturnView;

public static class RendererWaveform
{
    private static readonly SKPaint Paint = new()
    {
        StrokeWidth = 1,
        IsAntialias = false,
    };
    
    public static void RenderSeekSlider(SKCanvas canvas, CanvasInfo canvasInfo, SKColor waveformColor, float[]? waveform, float audioOffset, float audioLength, float sliderLength)
    {
        canvas.Clear(canvasInfo.BackgroundColor);
        
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

            Paint.Color = waveformColor;
            canvas.DrawLine(x, y0, x, y1, Paint);
        }
    }

    public static void RenderWaveform(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, SKColor waveformColor, SKColor judgelineColor, SKColor measureLineColor, SKColor beatLineColor, Chart chart, float[]? waveform, float audioOffset, float audioLength, float time)
    {
        const float judgeLineMargin = 50;
        const float pixelsPerMilliseconds = 0.25f;

        canvas.Clear(canvasInfo.BackgroundColor);
        if (canvasInfo.Width < 1) return;

        float startTime = time - audioOffset - (judgeLineMargin * pixelsPerMilliseconds);
        float endTime = startTime + canvasInfo.Height * pixelsPerMilliseconds;
        
        if (chart.Layers.Count != 0)
        {
            float chartStartTime = time - (judgeLineMargin * pixelsPerMilliseconds);
            float chartEndTime = chartStartTime + canvasInfo.Height * pixelsPerMilliseconds;
            
            foreach (Note note in chart.Layers[0].GeneratedNotes)
            {
                if (note is not MeasureLineNote measureLine) continue;
                if (measureLine.IsBeatLine && !settings.ShowBeatLineNotes) continue;
                if (!measureLine.IsBeatLine && !settings.ShowMeasureLineNotes) continue;
                
                float t = SaturnMath.InverseLerp(chartEndTime, chartStartTime, measureLine.Timestamp.Time);
                if (t < 0) continue;
                if (t > 1) continue;

                float y = canvasInfo.Height * t;

                Paint.Color = measureLine.IsBeatLine ? beatLineColor : measureLineColor;
                canvas.DrawLine(0, y, canvasInfo.Width, y, Paint);
            }
        }

        Paint.Color = judgelineColor;
        canvas.DrawLine(0, canvasInfo.Height - judgeLineMargin, canvasInfo.Width, canvasInfo.Height - judgeLineMargin, Paint);
        
        if (waveform == null)
        {
            float x = canvasInfo.Width * 0.5f;

            Paint.Color = waveformColor;
            canvas.DrawLine(x, 0, x, canvasInfo.Height, Paint);
            return;
        }

        int startSample = (int)(startTime / audioLength * waveform.Length);
        int endSample = (int)(endTime / audioLength * waveform.Length);

        for (int y = 0; y < canvasInfo.Height; y++)
        {
            float sample = 0;

            if (waveform != null && waveform.Length != 0 && audioLength != 0)
            {
                // Get 0-1 progress across canvas.
                float t = 1 - (y / canvasInfo.Height);

                int sampleIndex = (int)SaturnMath.Lerp(startSample, endSample, t);

                sample = sampleIndex < 0 || sampleIndex >= waveform.Length ? 0 : waveform[sampleIndex];
            }

            float x0 = (canvasInfo.Width + canvasInfo.Width * sample * 0.9f) * 0.5f;
            float x1 = (canvasInfo.Width - canvasInfo.Width * sample * 0.9f) * 0.5f;

            if (x0 - x1 < 1)
            {
                x0 = (int)x0;
                x1 = x0 - 0.5f;
            }

            Paint.Color = waveformColor;
            canvas.DrawLine(x0, y, x1, y, Paint);
        }
    }
}