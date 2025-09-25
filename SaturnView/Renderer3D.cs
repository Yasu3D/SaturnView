using System.Diagnostics;
using SaturnData.Notation.Core;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SkiaSharp;

namespace SaturnView;

public static class Renderer3D
{
    private static readonly float[][] SyncOutlineRadius = 
    [
        [ 0.971f, 0.983f, 1.014f, 1.026f ],
        [ 0.963f, 0.975f, 1.025f, 1.037f ],
        [ 0.946f, 0.958f, 1.043f, 1.055f ],
        [ 0.926f, 0.938f, 1.063f, 1.075f ],
        [ 0.910f, 0.922f, 1.080f, 1.092f ],
    ];
    
    public static void Render(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, SKColor clearColor, Chart chart, float time)
    {
        canvas.Clear(clearColor);
        canvas.DrawCircle(canvasInfo.Center, canvasInfo.Radius, new() { Color = SKColors.DimGray.WithAlpha(0x10) });
        
        DrawLanes(canvas, canvasInfo, settings, 0, 60);
        
        lock (chart)
        {
            
            //ChainNote note = new(new(0), 0, 15, BonusType.R, JudgementType.Normal);
            //DrawNote(canvas, settings, note, canvasInfo, Perspective(1.00f), 1);
            
            SnapForwardNote  snapForward  = new(new(0), 46, 14, BonusType.Normal, JudgementType.Normal);
            SnapBackwardNote snapBackward = new(new(0), 30, 14, BonusType.Normal, JudgementType.Normal);
            SyncNote sync = new(new(0), 43, 4);
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            DrawNote(canvas, canvasInfo, settings, snapForward,  Perspective(0.865f),  0.865f,  false,  1);
            DrawNote(canvas, canvasInfo, settings, snapBackward, Perspective(0.874f),  0.874f,  false,  1);

            DrawSyncConnector(canvas, canvasInfo, settings, sync, Perspective(0.9495f), 1);
            DrawNote(canvas, canvasInfo, settings, snapForward,  Perspective(0.9495f), 0.9495f, true, 1);
            DrawNote(canvas, canvasInfo, settings, snapBackward, Perspective(0.9495f), 0.9495f, true, 1);
            
            //DrawMeasureLine(canvas, canvasInfo, Perspective(1), 1, false);
            
            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedTicks / 10000.0f);
        }
    }

    private static void DrawNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, IPositionable note, float perspectiveScale, float linearScale, bool sync, float opacity)
    {
        if (perspectiveScale <= 0) return;
        
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

        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = perspectiveScale * canvasInfo.Scale;

        // Sync Outline
        if (sync)
        {
            float radius0 = radius * SyncOutlineRadius[(int)settings.NoteThickness][0];
            float radius1 = radius * SyncOutlineRadius[(int)settings.NoteThickness][1];
            float radius2 = radius * SyncOutlineRadius[(int)settings.NoteThickness][2];
            float radius3 = radius * SyncOutlineRadius[(int)settings.NoteThickness][3];
            
            SKRect rect0 = new(canvasInfo.Center.X - radius0, canvasInfo.Center.Y - radius0, canvasInfo.Center.X + radius0, canvasInfo.Center.Y + radius0);
            SKRect rect1 = new(canvasInfo.Center.X - radius1, canvasInfo.Center.Y - radius1, canvasInfo.Center.X + radius1, canvasInfo.Center.Y + radius1);
            SKRect rect2 = new(canvasInfo.Center.X - radius2, canvasInfo.Center.Y - radius2, canvasInfo.Center.X + radius2, canvasInfo.Center.Y + radius2);
            SKRect rect3 = new(canvasInfo.Center.X - radius3, canvasInfo.Center.Y - radius3, canvasInfo.Center.X + radius3, canvasInfo.Center.Y + radius3);
            
            float startAngle = note.Position * -6;
            float sweepAngle = note.Size * -6;
            
            float endAngle = startAngle + sweepAngle;
            
            SKPath path = new();
            
            SKPoint control0 = PointOnArc(canvasInfo.Center, radius, endAngle + 0.25f);
            SKPoint p0 = PointOnArc(canvasInfo.Center, radius3, endAngle + 2.5f);

            SKPoint control1 = PointOnArc(canvasInfo.Center, radius, startAngle - 0.25f);
            SKPoint p1 = PointOnArc(canvasInfo.Center, radius0, startAngle - 2.5f);
            
            SKPoint control2 = PointOnArc(canvasInfo.Center, radius, startAngle - 1.1f);
            SKPoint p2 = PointOnArc(canvasInfo.Center, radius2, startAngle - 2.55f);
            
            SKPoint control3 = PointOnArc(canvasInfo.Center, radius, endAngle + 1.1f);
            SKPoint p3 = PointOnArc(canvasInfo.Center, radius1, endAngle + 2.55f);

            path.ArcTo(rect0, startAngle - 2.5f, sweepAngle + 5f, true);
            path.QuadTo(control0, p0);
            path.ArcTo(rect3, endAngle + 2.5f, -sweepAngle - 5f, false);
            path.QuadTo(control1, p1);
            path.Close();
            
            path.ArcTo(rect1, endAngle + 2.55f, -sweepAngle - 5.1f, true);
            path.QuadTo(control2, p2);
            path.ArcTo(rect2, startAngle - 2.55f, sweepAngle + 5.1f, false);
            path.QuadTo(control3, p3);
            path.Close();
            
            canvas.DrawPath(path, NotePaints.GetSyncOutlinePaint(opacity));
        }
        
        // Note Body
        if (note.Size == 60)
        {
            // R-Effect Glow
            if (playable != null && playable.BonusType == BonusType.R)
            {
                canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetRNotePaint(settings, pixelScale, opacity));
            }

            // Body
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetNoteBasePaint(canvasInfo, settings, colorId, pixelScale, perspectiveScale, opacity));
        }
        else
        {
            SKRect rect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);
            
            float start = (note.Position + 1) * -6; 
            float sweep = Math.Min(0, (note.Size - 2) * -6);
            
            // R-Effect Glow
            if (playable != null && playable.BonusType == BonusType.R)
            {
                canvas.DrawArc(rect, start + 3, sweep - 6, false, NotePaints.GetRNotePaint(settings, pixelScale, opacity));
            }
            
            // Body
            canvas.DrawArc(rect, start, sweep, false, NotePaints.GetNoteBasePaint(canvasInfo, settings, colorId, pixelScale, perspectiveScale, opacity));

            // Caps
            float capStart = note.Position * -6 - 4.5f;
            canvas.DrawArc(rect, capStart, -1.5f, false, NotePaints.GetNoteCapPaint(canvasInfo, settings, pixelScale, perspectiveScale, opacity));

            if (note.Size > 1)
            {
                capStart = (note.Position + note.Size - 1) * -6;
                canvas.DrawArc(rect, capStart, -1.5f, false, NotePaints.GetNoteCapPaint(canvasInfo, settings, pixelScale, perspectiveScale, opacity));
            }
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
            
            canvas.DrawPath(path, NotePaints.GetChainStripePaint(opacity));
        }

        // Bonus Triangles
        if (playable != null && playable.BonusType == BonusType.Bonus)
        {
            SKPath path = new();
            
            float innerRadius = radius - NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] * 0.5f * pixelScale;
            float outerRadius = radius + NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] * 0.5f * pixelScale;

            int count = note.Size == 60 
                ? note.Size 
                : note.Size - 2;
            float start = note.Size == 60 
                ? note.Position * -6
                : note.Position * -6 - 6;
            
            for (int i = 0; i < count; i++)
            {
                bool even = i % 2 == 0;

                float angleA = start + i * -6;
                float angleB = start + (i + 1) * -6;

                SKPoint p0 = PointOnArc(canvasInfo.Center, innerRadius, even ? angleA : angleB);
                SKPoint p1 = PointOnArc(canvasInfo.Center, outerRadius, even ? angleA : angleB);
                SKPoint p2 = PointOnArc(canvasInfo.Center, innerRadius, even ? angleB : angleA);

                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
            }

            canvas.DrawPath(path, NotePaints.GetNoteBonusPaint(canvasInfo, settings, colorId, pixelScale, perspectiveScale, opacity));
        }
        
        // Snap Arrows
        if (note is SnapForwardNote or SnapBackwardNote && note.Size > 2)
        {
            bool flip = note is SnapBackwardNote;
            
            float radius0 = flip ? radius * 0.960f : radius * 0.725f;
            float radius1 = flip ? radius * 0.899f : radius * 0.775f;
            float radius2 = flip ? radius * 0.844f : radius * 0.830f;
            float radius3 = flip ? radius * 0.794f : radius * 0.891f;
            float radius4 = flip ? radius * 0.878f : radius * 0.802f;
            float radius5 = radius * 0.840f;
            float radius6 = flip ? radius * 0.766f : radius * 0.920f;
            float radius7 = flip ? radius * 0.725f : radius * 0.960f;
            
            int count = note.Size / 3;
            float startPosition = note.Position * -6;
            
            int m = note.Size % 3;
            
            if (m == 0)
            {
                startPosition -= 9;
            }
            else if (m == 1)
            {
                startPosition -= 12;
            }
            else
            {
                startPosition -= 15;
            }
            
            SKPath path = new();
            
            for (int i = 0; i < count; i++)
            {
                const float arrowWidth = 4f;
                const float arrowSpacing = 18;
            
                float arrowOffset = arrowSpacing * i;
                
                float center = startPosition - arrowOffset;
                float left1  = startPosition - arrowOffset + arrowWidth;
                float left2  = startPosition - arrowOffset + arrowWidth * (flip ? 1.01f : 0.97f);
                float left3  = startPosition - arrowOffset + arrowWidth * (flip ? 1.04f : 0.94f);
                float left4  = startPosition - arrowOffset + arrowWidth * (flip ? 1.07f : 0.93f);
                
                float right1 = startPosition - arrowOffset - arrowWidth;
                float right2 = startPosition - arrowOffset - arrowWidth * (flip ? 1.01f : 0.97f);
                float right3 = startPosition - arrowOffset - arrowWidth * (flip ? 1.04f : 0.94f);
                float right4 = startPosition - arrowOffset - arrowWidth * (flip ? 1.07f : 0.93f);
                
                SKPoint p0 = PointOnArc(canvasInfo.Center, radius0, center);
                SKPoint p1 = PointOnArc(canvasInfo.Center, radius2, right1);
                SKPoint p2 = PointOnArc(canvasInfo.Center, radius3, right2);
                SKPoint p3 = PointOnArc(canvasInfo.Center, radius1, center);
                SKPoint p4 = PointOnArc(canvasInfo.Center, radius3, left2);
                SKPoint p5 = PointOnArc(canvasInfo.Center, radius2, left1);
                
                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
                path.LineTo(p3);
                path.LineTo(p4);
                path.LineTo(p5);
                path.Close();
                
                p0 = PointOnArc(canvasInfo.Center, radius4, center);
                p1 = PointOnArc(canvasInfo.Center, radius6, right3);
                p2 = PointOnArc(canvasInfo.Center, radius7, right4);
                p3 = PointOnArc(canvasInfo.Center, radius5, center);
                p4 = PointOnArc(canvasInfo.Center, radius7, left4);
                p5 = PointOnArc(canvasInfo.Center, radius6, left3);
                
                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
                path.LineTo(p3);
                path.LineTo(p4);
                path.LineTo(p5);
                path.Close();
            }
            
            canvas.DrawPath(path, NotePaints.GetSnapFillPaint(canvasInfo, settings, colorId, perspectiveScale, opacity, flip));

            if (!settings.LowPerformanceMode)
            {
                canvas.DrawPath(path, NotePaints.GetSnapStrokePaint(colorId, pixelScale, opacity));
            }
        }
        
        // Slide Arrows
        if (note is SlideClockwiseNote or SlideCounterclockwiseNote)
        {
            bool flip = note is SlideCounterclockwiseNote;

            float scroll = flip
                ? 1 - (linearScale * 6f % 1)
                : linearScale * 6f % 1;
            
            float radius0 = radius * 0.790f;
            float radius1 = radius * 0.864f;
            float radius2 = radius * 0.938f;

            float arrowCount = note.Size * 0.5f + 1;

            float start = note.Position * -6;
            
            SKPath path = new();
            SKPath maskPath = new();
            
            // inner side
            float maskAngle;
            float maskRadius;
            SKPoint maskPoint;
            
            for (int i = 0; i <= note.Size; i++)
            {
                float x = flip
                    ? (float)i / note.Size
                    : 1 - (float)i / note.Size;
                
                maskAngle = (note.Position + i) * -6;
                maskRadius = radius1 + (radius2 - radius1) * SlideArrowMask(x);

                maskPoint = PointOnArc(canvasInfo.Center, maskRadius, maskAngle);
                if (i == 0) maskPath.MoveTo(maskPoint);
                else maskPath.LineTo(maskPoint);
            }
            
            // center point
            maskAngle = (note.Position + note.Size) * -6;
            maskPoint = PointOnArc(canvasInfo.Center, radius1, maskAngle);
            maskPath.LineTo(maskPoint);
            
            // outer side
            for (int i = note.Size; i >= 0; i--)
            {
                float x = flip
                    ? (float)i / note.Size
                    : 1 - (float)i / note.Size;
                
                maskAngle = (note.Position + i) * -6;
                maskRadius = radius1 + (radius0 - radius1) * SlideArrowMask(x);

                maskPoint = PointOnArc(canvasInfo.Center, maskRadius, maskAngle);
                maskPath.LineTo(maskPoint);
            }
            
            maskPath.Close();
            
            for (int i = 0; i < arrowCount; i++)
            {
                //    p0____p1
                //   /     / 
                // p5    p2
                //   \     \
                //   p4_____p3
                
                float angle = start + (scroll - i - 0.5f) * 12;
                float offset = flip ? -6 : 6;
                
                SKPoint p0 = PointOnArc(canvasInfo.Center, radius0, angle         );
                SKPoint p1 = PointOnArc(canvasInfo.Center, radius0, angle - offset);
                SKPoint p2 = PointOnArc(canvasInfo.Center, radius1, angle         );
                SKPoint p3 = PointOnArc(canvasInfo.Center, radius2, angle - offset);
                SKPoint p4 = PointOnArc(canvasInfo.Center, radius2, angle         );
                SKPoint p5 = PointOnArc(canvasInfo.Center, radius1, angle + offset);
                
                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
                path.LineTo(p3);
                path.LineTo(p4);
                path.LineTo(p5);
                path.Close();
            }

            canvas.Save();
            canvas.ClipPath(maskPath, SKClipOperation.Intersect, true);
            
            canvas.DrawPath(path, NotePaints.GetSlideFillPaint(canvasInfo, settings, note.Position, note.Size, colorId, opacity, flip));
            
            if (!settings.LowPerformanceMode)
            {
                canvas.DrawPath(path, NotePaints.GetSlideStrokePaint(colorId, pixelScale, opacity));
            }
            
            canvas.Restore();
        }
    }

    private static void DrawSyncConnector(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, SyncNote note, float perspectiveScale, float opacity)
    {
        if (perspectiveScale <= 0) return;
        
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = perspectiveScale * canvasInfo.Scale;

        if (note.Size == 60)
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetSyncConnectorPaint(canvasInfo, settings, pixelScale, perspectiveScale, opacity));
        }
        else
        {
            SKRect baseRect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);
            
            float start = note.Position * -6; 
            float sweep = note.Size * -6;
            
            canvas.DrawArc(baseRect, start, sweep, false, NotePaints.GetSyncConnectorPaint(canvasInfo, settings, pixelScale, perspectiveScale, opacity));
        }
    }

    private static void DrawMeasureLine(SKCanvas canvas, CanvasInfo canvasInfo, float perspectiveScale, float linearScale, bool isBeatLine)
    {
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;

        if (isBeatLine)
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetBeatLinePaint(canvasInfo, linearScale));
        }
        else
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetMeasureLinePaint(canvasInfo, linearScale));
        }
    }

    private static void DrawLanes(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, int position, int size)
    {
        if (size == 0) return;

        if (settings.GuideLineType != RenderSettings.GuideLineTypeOption.None)
        {
            SKPaint guideLinePaint = NotePaints.GetGuideLInePaint(canvasInfo);
            
            for (int i = position; i < position + size; i++)
            {
                // Type A: always draw.
                
                // Type B: draw every 2nd lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.B && (i - 1) % 2 != 0) continue;
                
                // Type C: draw every 3rd lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.C && i % 3 != 0) continue;

                // Type D: draw every 4th lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.D && i % 4 != 0) continue;
                
                // Type E: draw every 5th lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.E && i % 5 != 0) continue;

                // Type F: draw every 10th lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.F && i % 10 != 0) continue;

                // Type G: draw every 15th lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.G && i % 15 != 0) continue;

                SKPoint p = PointOnArc(canvasInfo.Center, canvasInfo.JudgementLineRadius, i * -6);
                canvas.DrawLine(canvasInfo.Center, p, guideLinePaint);
            }
        }
        
        if (size == 60)
        {
            canvas.DrawCircle(canvasInfo.Center, canvasInfo.JudgementLineRadius, NotePaints.GetJudgementLinePaint(canvasInfo, settings));
        }
        else
        {
            SKRect judgementLineRect = new(canvasInfo.Center.X - canvasInfo.JudgementLineRadius, canvasInfo.Center.Y - canvasInfo.JudgementLineRadius, canvasInfo.Center.X + canvasInfo.JudgementLineRadius, canvasInfo.Center.Y + canvasInfo.JudgementLineRadius);
            float start = position * -6; 
            float sweep = size * -6;
            
            canvas.DrawArc(judgementLineRect, start, sweep, false, NotePaints.GetJudgementLinePaint(canvasInfo, settings));
        }
    }
    
    private static SKPoint PointOnArc(SKPoint center, float radius, float angle)
    {
        return new
        (
            (float)(radius * Math.Cos(angle * Math.PI / 180.0)) + center.X,
            (float)(radius * Math.Sin(angle * Math.PI / 180.0)) + center.Y
        );
    }

    private static float Perspective(float x)
    {
        // Huge thanks to CG505 for figuring out the perspective math:
        // https://www.desmos.com/calculator/9a0srmgktj
        return 3.325f * x / (13.825f - 10.5f * x);
    }

    private static float SlideArrowMask(float x)
    {
        return x < 0.88f 
            ? (0.653f * x + 0.175f) / 0.75f 
            : (-6.25f * x + 6.25f) / 0.75f;
    }
}