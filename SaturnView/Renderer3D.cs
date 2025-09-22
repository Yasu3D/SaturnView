using System.Diagnostics;
using SaturnData.Notation.Core;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SkiaSharp;

namespace SaturnView;

public static class Renderer3D
{
    public static void Render(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, SKColor clearColor, Chart chart, float time)
    {
        canvas.Clear(clearColor);
        canvas.DrawCircle(canvasInfo.Center, canvasInfo.Radius, new() { Color = SKColors.DimGray.WithAlpha(0x10) });
        
        lock (chart)
        {
            ChainNote note = new(new(0), 0, 15, BonusType.R, JudgementType.Normal);

            SnapForwardNote  snapForward  = new(new(0), 46, 14, BonusType.Normal, JudgementType.Normal);
            SnapBackwardNote snapBackward = new(new(0), 30, 14, BonusType.Normal, JudgementType.Normal);
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            DrawNote(canvas, settings, note, canvasInfo, Perspective(1.00f), 1);
            
            DrawNote(canvas, settings, snapForward, canvasInfo, Perspective(0.9495f), 1);
            DrawNote(canvas, settings, snapBackward, canvasInfo, Perspective(0.9495f), 1);
            
            DrawNote(canvas, settings, snapForward, canvasInfo, Perspective(0.865f), 1);
            DrawNote(canvas, settings, snapBackward, canvasInfo, Perspective(0.874f), 1);
            
            stopwatch.Stop();
            //Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }
    }

    private static void DrawNote(SKCanvas canvas, RenderSettings settings, IPositionable note, CanvasInfo canvasInfo, float rawScale, float opacity)
    {
        if (rawScale <= 0) return;
        
        IPlayable? playable = note as IPlayable;
        
        int colorId = note switch
        {
            TouchNote                 => (int)settings.TouchNoteColor,
            ChainNote                 => (int)settings.ChainNoteColor,
            HoldNote                  => (int)settings.HoldNoteColor,
            SlideClockwiseNote        => (int)settings.SlideClockwiseNoteColor,
            SlideCounterclockwiseNote => (int)settings.SlideCounterclockwiseNoteColor,
            SnapForwardNote           => (int)settings.SnapForwardNoteColor,
            SnapBackwardNote          => (int)settings.SnapBackwardNoteColor,
            _ => -1,
        };
        
        if (colorId == -1) return;

        float radius = canvasInfo.ScaledRadius * rawScale;
        float pixelScale = rawScale * canvasInfo.Scale;
        
        // Note Body
        if (note.Size == 60)
        {
            // R-Effect Glow
            if (playable != null && playable.BonusType == BonusType.R)
            {
                canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetRNotePaint(settings, pixelScale, opacity));
            }

            // Body
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetNoteBasePaint(canvasInfo, settings, colorId, pixelScale, rawScale, opacity));
        }
        else
        {
            SKRect baseRect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);
            
            float start = (note.Position + 1) * -6;
            float sweep = (note.Size - 2) * -6;
            
            // R-Effect Glow
            if (playable != null && playable.BonusType == BonusType.R)
            {
                canvas.DrawArc(baseRect, start + 3, sweep - 6, false, NotePaints.GetRNotePaint(settings, pixelScale, opacity));
            }
            
            // Body
            canvas.DrawArc(baseRect, start, sweep, false, NotePaints.GetNoteBasePaint(canvasInfo, settings, colorId, pixelScale, rawScale, opacity));

            // Caps
            float capStart = note.Position * -6 - 4.5f;
            canvas.DrawArc(baseRect, capStart, -1.5f, false, NotePaints.GetNoteCapPaint(canvasInfo, settings, pixelScale, rawScale, opacity));

            capStart = (note.Position + note.Size - 1) * -6;
            canvas.DrawArc(baseRect, capStart, -1.5f, false, NotePaints.GetNoteCapPaint(canvasInfo, settings, pixelScale, rawScale, opacity));
        }

        // Chain Stripes
        if (note is ChainNote)
        {
            int stripes = note.Size * 2;

            float innerRadius = radius - NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] * 0.5f * pixelScale;
            float outerRadius = radius + NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] * 0.5f * pixelScale;
            float start = (note.Position + 1) * -6;
            
            SKPath path = new();

            for (int i = 0; i < stripes; i++)
            {
                if (note.Size != 60)
                {
                    if (i == 0) continue; // skip first stripe
                    if (i >= stripes - 3) continue; // skip last 3 stripes

                    if (i == 1)
                    {
                        SKPoint p4 = PointOnArc(canvasInfo.Center, innerRadius, start + i * -3 + 3);
                        SKPoint p5 = PointOnArc(canvasInfo.Center, innerRadius, start + i * -3 + 1.5f);
                        SKPoint p6 = PointOnArc(canvasInfo.Center, outerRadius, start + i * -3 + 3);

                        path.MoveTo(p4);
                        path.LineTo(p5);
                        path.LineTo(p6);
                    }
                    
                    if (i == stripes - 4)
                    {
                        SKPoint p4 = PointOnArc(canvasInfo.Center, innerRadius, start + i * -3);
                        SKPoint p5 = PointOnArc(canvasInfo.Center, outerRadius, start + i * -3);
                        SKPoint p6 = PointOnArc(canvasInfo.Center, outerRadius, start + i * -3 + 1.5f);
                        
                        path.MoveTo(p4);
                        path.LineTo(p5);
                        path.LineTo(p6);

                        continue;
                    }
                }

                SKPoint p0 = PointOnArc(canvasInfo.Center, innerRadius, start + i * -3);
                SKPoint p1 = PointOnArc(canvasInfo.Center, innerRadius, start + i * -3 - 1.5f);
                SKPoint p2 = PointOnArc(canvasInfo.Center, outerRadius, start + i * -3);
                SKPoint p3 = PointOnArc(canvasInfo.Center, outerRadius, start + i * -3 + 1.5f);
                
                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
                path.LineTo(p3);
            }
            
            canvas.DrawPath(path, NotePaints.GetChainStripePaint());
        }

        // Bonus Triangles
        if (playable != null && playable.BonusType == BonusType.Bonus)
        {
            
        }
    }

    private static SKPoint PointOnArc(SKPoint center, float radius, float angle)
    {
        return new(
            (float)(radius * Math.Cos(angle * Math.PI / 180.0)) + center.X,
            (float)(radius * Math.Sin(angle * Math.PI / 180.0)) + center.Y);
    }

    private static float Perspective(float x)
    {
        // Huge thanks to CG505 for figuring out the perspective math:
        // https://www.desmos.com/calculator/9a0srmgktj
        return 3.325f * x / (13.825f - 10.5f * x);
    }
}