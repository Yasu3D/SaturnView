using System.Diagnostics;
using SaturnData.Notation;
using SaturnData.Notation.Core;
using SaturnData.Notation.Events;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SkiaSharp;

namespace SaturnView;

public static class Renderer3D
{
    private static readonly float[][] SyncOutlineRadius = 
    [
        [0.971f, 0.983f, 1.014f, 1.026f, 0.977f, 1.020f],
        [0.963f, 0.975f, 1.025f, 1.037f, 0.969f, 1.031f],
        [0.946f, 0.958f, 1.043f, 1.055f, 0.952f, 1.049f],
        [0.926f, 0.938f, 1.063f, 1.075f, 0.932f, 1.069f],
        [0.910f, 0.922f, 1.080f, 1.092f, 0.915f, 1.086f],
    ];

    private static int frameCounter = 0;
    private static string fps = "";
    
    /// <summary>
    /// Renders a snapshot of a chart at the provided timestamp.
    /// </summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="canvasInfo">Various attributes of the canvas.</param>
    /// <param name="settings">The render settings to follow.</param>
    /// <param name="clearColor">The background color of the canvas.</param>
    /// <param name="chart">The chart to draw.</param>
    /// <param name="entry">The entry to draw.</param>
    /// <param name="time">The time of the snapshot to draw.</param>
    public static void Render(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Chart chart, Entry entry, float time, bool playing)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        canvas.Clear(canvasInfo.BackgroundColor);
        canvas.DrawCircle(canvasInfo.Center, canvasInfo.Radius, new() { Color = new(0xFF000000) });
        
        lock (chart)
        {
            renderLanes();
            renderNotes();
        }
        
        DrawInterface(canvas, canvasInfo, settings, entry, time);
        canvas.DrawText(fps, new(0, 30), NotePaints.GetBoldFont(20), NotePaints.DebugPaint3);
        
        stopwatch.Stop();

        frameCounter++;
        if (frameCounter > 5)
        {
            frameCounter = 0;
            fps = (1000.0f / (stopwatch.ElapsedTicks / 10000.0f)).ToString("0.0");
        }
        
        //Console.WriteLine((1000.0f / (stopwatch.ElapsedTicks / 10000.0f)).ToString("0.0"));
        return;
        
        void renderLanes()
        {
            if (settings.IgnoreLaneToggleAnimations)
            {
                DrawLanes(canvas, canvasInfo, settings, 0, 60, time);
                return;
            }
            
            bool[] lanes = new bool[60];
            
            foreach (Note note in chart.LaneToggles)
            {
                if (note is not ILaneToggle laneToggle) continue;
                if (note is not ITimeable timeable) continue;
                if (note is not IPositionable positionable) continue;
                
                bool state = note is LaneShowNote; // not fully explicit but good enough.
                
                float delta = time - timeable.Timestamp.Time;
                float duration = laneToggle.Direction switch
                {
                    LaneSweepDirection.Counterclockwise => positionable.Size * 8.3333333f,
                    LaneSweepDirection.Clockwise => positionable.Size * 8.3333333f,
                    LaneSweepDirection.Center => positionable.Size * 4.1666666f,
                    LaneSweepDirection.Instant => 0,
                    _ => 0,
                };

                // Instant or after the sweep. Set lanes without animating.
                if (duration == 0 || delta > duration)
                {
                    for (int i = positionable.Position; i < positionable.Position + positionable.Size; i++)
                    {
                        lanes[i % 60] = state;
                    }

                    continue;
                }

                float progress = delta / duration;
                
                // In range for a sweep animation. Set lanes based on animation.
                if (laneToggle.Direction is LaneSweepDirection.Center)
                {
                    float halfSize = positionable.Size * 0.5f;
                    int floor = (int)halfSize;
                    int steps = (int)MathF.Ceiling(halfSize);
                    int centerClockwise = positionable.Position + floor;
                    int centerCounterclockwise = positionable.Size % 2 != 0 ? centerClockwise : centerClockwise + 1;
                    int offset = positionable.Size % 2 != 0 ? 60 : 59;

                    for (int i = 0; i < (int)(steps * progress); i++)
                    {
                        lanes[(centerClockwise - i + offset) % 60] = state;
                        lanes[(centerCounterclockwise + i + offset) % 60] = state;
                    }
                }
                else if (laneToggle.Direction is LaneSweepDirection.Clockwise)
                {
                    for (int i = 0; i < (int)(positionable.Size * progress); i++)
                    {
                        lanes[(positionable.Position + positionable.Size - i + 59) % 60] = state;
                    }
                }
                else if (laneToggle.Direction is LaneSweepDirection.Counterclockwise)
                {
                    for (int i = 0; i < (int)(positionable.Size * progress); i++)
                    {
                        lanes[(i + positionable.Position + 60) % 60] = state;
                    }
                }
            }

            bool allActive = true;
            bool allInactive = true;
            
            for (int i = 0; i < 60; i++)
            {
                if (!allActive && !allInactive) break;
                
                if (!lanes[i]) allActive = false;
                if (lanes[i]) allInactive = false;
            }

            if (allActive)
            {
                DrawLanes(canvas, canvasInfo, settings, 0, 60, time);
            }
            else if (!allInactive)
            {
                bool lastState = lanes[59];
                bool batchActive = false;

                int position = 0;
                int size = 0;

                int firstBatchPosition = -1;
                
                // Fancy batching to draw large chunks of lanes all at once.
                for (int i = 0; i < 120; i++)
                {
                    // Stop once position of first batch is reached again.
                    if (i % 60 == firstBatchPosition) break;
                    
                    bool currentState = lanes[i % 60];
                    
                    // Begin of new batch
                    if (!lastState && currentState)
                    {
                        batchActive = true;
                        position = i % 60;
                        size = 0;
                        
                        if (firstBatchPosition == -1)
                        {
                            firstBatchPosition = position;
                        }
                    }

                    if (batchActive && lastState && !currentState)
                    {
                        batchActive = false;
                        DrawLanes(canvas, canvasInfo, settings, position, size, time);
                    }
                    
                    if (batchActive)
                    {
                        size++;
                    }

                    lastState = currentState;
                }
            }
        }

        void renderNotes()
        {
            float viewDistance = 3333.333f / (settings.NoteSpeed * 0.1f);
            
            List<RenderObject> notesToDraw = [];
            List<RenderObject> holdEndsToDraw = [];
            List<RenderObject> holdsToDraw = [];

            for (int l = 0; l < chart.Layers.Count; l++)
            {
                Layer layer = chart.Layers[l];
                
                VisibilityChangeEvent? visibilityChange = NotationUtils.LastVisibilityChange(layer, time);
                if (visibilityChange != null && visibilityChange.Visible == false) continue;
                
                float scaledTime = Timestamp.ScaledTimeFromTime(layer, time);

                for (int n = 0; n < layer.Notes.Count; n++)
                {
                    Note note = layer.Notes[n];

                    if (note is HoldNote holdNote && holdNote.Points.Count != 0)
                    {
                        // Hold Notes
                        
                        if (settings.ShowSpeedChanges)
                        {
                            if (holdNote.Points[^1].Timestamp.Time < time) continue;
                            if (holdNote.Points[^1].Timestamp.ScaledTime < scaledTime) continue;
                            if (holdNote.Points[ 0].Timestamp.ScaledTime > scaledTime + viewDistance) continue;
                        }
                        else
                        {
                            if (holdNote.Points[^1].Timestamp.Time < time) continue;
                            if (holdNote.Points[ 0].Timestamp.Time > time + viewDistance) continue;
                        }
                        
                        bool isVisible = RenderUtils.IsVisible(holdNote, settings);
                        
                        holdsToDraw.Add(new(holdNote, layer, l, 0, false, isVisible));
                        
                        // Hold Start
                        float tStart = settings.ShowSpeedChanges
                            ? 1 - (holdNote.Points[0].Timestamp.ScaledTime - scaledTime) / viewDistance
                            : 1 - (holdNote.Points[0].Timestamp.Time - time) / viewDistance;

                        if (tStart is >= 0 and <= 1)
                        {
                            Note? prev = n > 0 ? layer.Notes[n - 1] : null;
                            Note? next = n < layer.Notes.Count - 1 ? layer.Notes[n + 1] : null;
                            bool sync = NotationUtils.IsSync(note, prev) || NotationUtils.IsSync(note, next);

                            notesToDraw.Add(new(note, layer, l, tStart, sync, isVisible));
                        }

                        // Hold End
                        float tEnd = settings.ShowSpeedChanges
                            ? 1 - (holdNote.Points[^1].Timestamp.ScaledTime - scaledTime) / viewDistance
                            : 1 - (holdNote.Points[^1].Timestamp.Time - time) / viewDistance;
                        
                        if (tEnd is >= 0 and <= 1)
                        {
                            holdEndsToDraw.Add(new(holdNote.Points[^1], layer, l, tEnd, false, isVisible));
                        }

                        // Hold Points
                        if (!settings.HideHoldControlPointsDuringPlayback || !playing)
                        {
                            for (int j = 1; j < holdNote.Points.Count - 1; j++)
                            {
                                HoldPointNote point = holdNote.Points[j];
                                float t = settings.ShowSpeedChanges
                                    ? 1 - (point.Timestamp.ScaledTime - scaledTime) / viewDistance
                                    : 1 - (point.Timestamp.Time - time) / viewDistance;

                                notesToDraw.Add(new(point, layer, l, t, false, isVisible));
                            }
                        }
                    }
                    else
                    {
                        // Normal Notes
                        
                        if (settings.ShowSpeedChanges)
                        {
                            if (note.Timestamp.Time < time) continue;
                            if (note.Timestamp.ScaledTime < scaledTime) continue;
                            if (note.Timestamp.ScaledTime > scaledTime + viewDistance) continue;
                        }
                        else
                        {
                            if (note.Timestamp.Time < time) continue;
                            if (note.Timestamp.Time > time + viewDistance) continue;
                        }

                        float t = settings.ShowSpeedChanges
                            ? 1 - (note.Timestamp.ScaledTime - scaledTime) / viewDistance
                            : 1 - (note.Timestamp.Time - time) / viewDistance;
                        
                        if (t is < 0 or > 1) continue;

                        Note? prev = n > 0 ? layer.Notes[n - 1] : null;
                        Note? next = n < layer.Notes.Count - 1 ? layer.Notes[n + 1] : null;
                        bool sync = NotationUtils.IsSync(note, prev) || NotationUtils.IsSync(note, next);

                        notesToDraw.Add(new(note, layer, l, t, sync, RenderUtils.IsVisible(note, settings)));
                    }
                }

                foreach (Note note in layer.GeneratedNotes)
                {
                    if (settings.ShowSpeedChanges)
                    {
                        if (note.Timestamp.Time < time) continue;
                        if (note.Timestamp.ScaledTime < scaledTime) continue;
                        if (note.Timestamp.ScaledTime > scaledTime + viewDistance) continue;
                    }
                    else
                    {
                        if (note.Timestamp.Time < time) continue;
                        if (note.Timestamp.Time > time + viewDistance) continue;
                    }

                    float t = settings.ShowSpeedChanges
                        ? 1 - (note.Timestamp.ScaledTime - scaledTime) / viewDistance
                        : 1 - (note.Timestamp.Time - time) / viewDistance;

                    notesToDraw.Add(new(note, layer, l, t, false, RenderUtils.IsVisible(note, settings)));
                }
            }

            notesToDraw = notesToDraw
                .OrderBy(x => x.IsVisible)
                .ThenBy(x => x.LayerIndex)
                .ThenBy(x => x.Scale)
                .ThenByDescending(x => x.Object is SyncNote)
                .ThenByDescending(x => x.Object is HoldNote or HoldPointNote)
                .ThenByDescending(x => (x.Object as IPositionable)?.Size ?? 60)
                .ToList();

            foreach (RenderObject renderObject in holdEndsToDraw)
            {
                if (renderObject.Object is not HoldPointNote holdPointNote) continue;
                
                DrawHoldEndNote(canvas, canvasInfo, settings, holdPointNote, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : 0.1f);
            }
            
            foreach (RenderObject renderObject in holdsToDraw)
            {
                if (renderObject.Object is not HoldNote holdNote) continue;
                if (renderObject.Layer == null) continue;
                
                DrawHoldSurface(canvas, canvasInfo, settings, holdNote, renderObject.Layer, time, playing, renderObject.IsVisible ? 1 : 0.1f);
            }
            
            foreach (RenderObject renderObject in notesToDraw)
            {
                if (renderObject.Object is HoldPointNote holdPointNote)
                {
                    DrawHoldPointNote(canvas, canvasInfo, holdPointNote, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : 0.1f);
                }
                else if (renderObject.Object is SyncNote syncNote)
                {
                    DrawSyncNote(canvas, canvasInfo, settings, syncNote, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : 0.1f);
                }
                else if (renderObject.Object is MeasureLineNote)
                {
                    DrawMeasureLineNote(canvas, canvasInfo, RenderUtils.Perspective(renderObject.Scale), renderObject.Scale, false, renderObject.IsVisible ? 1 : 0.1f);
                }
                else if (renderObject.Object is Note note)
                {
                    DrawNote(canvas, canvasInfo, settings, note, RenderUtils.Perspective(renderObject.Scale), renderObject.Scale, renderObject.Sync, renderObject.IsVisible ? 1 : 0.1f);
                }
            }
        }
    }

    /// <summary>
    /// Draws a standard note body, sync outline, r-effect, and arrows.
    /// </summary>
    private static void DrawNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Note note, float perspectiveScale, float linearScale, bool sync, float opacity)
    {
        if (perspectiveScale is <= 0 or > 1) return;
        if (note is not IPositionable positionable) return;
        
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
        float pixelScale = canvasInfo.Scale * perspectiveScale;
        
        // Note Body
        if (positionable.Size == 60)
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
            
            float start = (positionable.Position + 1) * -6; 
            float sweep = Math.Min(0, (positionable.Size - 2) * -6);
            
            // R-Effect Glow
            if (playable != null && playable.BonusType == BonusType.R)
            {
                canvas.DrawArc(rect, start + 3, sweep - 6, false, NotePaints.GetRNotePaint(settings, pixelScale, opacity));
            }
            
            // Body
            canvas.DrawArc(rect, start, sweep, false, NotePaints.GetNoteBasePaint(canvasInfo, settings, colorId, pixelScale, perspectiveScale, opacity));

            // Caps
            float capStart = positionable.Position * -6 - 4.5f;
            canvas.DrawArc(rect, capStart, -1.5f, false, NotePaints.GetNoteCapPaint(canvasInfo, settings, pixelScale, perspectiveScale, opacity));

            if (positionable.Size > 1)
            {
                capStart = (positionable.Position + positionable.Size - 1) * -6;
                canvas.DrawArc(rect, capStart, -1.5f, false, NotePaints.GetNoteCapPaint(canvasInfo, settings, pixelScale, perspectiveScale, opacity));
            }
        }

        // Sync Outline
        if (sync)
        {
            if (positionable.Size == 60)
            {
                 float radius0 = radius * SyncOutlineRadius[(int)settings.NoteThickness][4];
                 float radius1 = radius * SyncOutlineRadius[(int)settings.NoteThickness][5];

                 SKPaint paint = NotePaints.GetSyncOutlineStrokePaint(pixelScale, opacity);
                 canvas.DrawCircle(canvasInfo.Center, radius0, paint);
                 canvas.DrawCircle(canvasInfo.Center, radius1, paint);
            }
            else
            {
                float radius0 = radius * SyncOutlineRadius[(int)settings.NoteThickness][0];
                float radius1 = radius * SyncOutlineRadius[(int)settings.NoteThickness][1];
                float radius2 = radius * SyncOutlineRadius[(int)settings.NoteThickness][2];
                float radius3 = radius * SyncOutlineRadius[(int)settings.NoteThickness][3];
                
                SKRect rect0 = new(canvasInfo.Center.X - radius0, canvasInfo.Center.Y - radius0, canvasInfo.Center.X + radius0, canvasInfo.Center.Y + radius0);
                SKRect rect1 = new(canvasInfo.Center.X - radius1, canvasInfo.Center.Y - radius1, canvasInfo.Center.X + radius1, canvasInfo.Center.Y + radius1);
                SKRect rect2 = new(canvasInfo.Center.X - radius2, canvasInfo.Center.Y - radius2, canvasInfo.Center.X + radius2, canvasInfo.Center.Y + radius2);
                SKRect rect3 = new(canvasInfo.Center.X - radius3, canvasInfo.Center.Y - radius3, canvasInfo.Center.X + radius3, canvasInfo.Center.Y + radius3);
                
                float startAngle = positionable.Position * -6;
                float sweepAngle = positionable.Size * -6;
                
                float endAngle = startAngle + sweepAngle;
                
                SKPath path = new();
                
                SKPoint control0 = RenderUtils.PointOnArc(canvasInfo.Center, radius, endAngle + 0.25f);
                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius3, endAngle + 2.5f);

                SKPoint control1 = RenderUtils.PointOnArc(canvasInfo.Center, radius, startAngle - 0.25f);
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, radius0, startAngle - 2.5f);
                
                SKPoint control2 = RenderUtils.PointOnArc(canvasInfo.Center, radius, startAngle - 1.1f);
                SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, radius2, startAngle - 2.55f);
                
                SKPoint control3 = RenderUtils.PointOnArc(canvasInfo.Center, radius, endAngle + 1.1f);
                SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, radius1, endAngle + 2.55f);

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
        }
        
        // Chain Stripes
        if (note is ChainNote)
        {
            int stripes = positionable.Size * 2;

            float innerRadius = radius - NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] * 0.5f * pixelScale;
            float outerRadius = radius + NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] * 0.5f * pixelScale;
            float start = (positionable.Position + 1) * -6;
            
            SKPath path = new();

            for (int i = 0; i < stripes; i++)
            {
                if (positionable.Size != 60)
                {
                    if (i == 0) continue; // skip first stripe
                    if (i >= stripes - 3) continue; // skip last 3 stripes

                    if (i == 1)
                    {
                        SKPoint p4 = RenderUtils.PointOnArc(canvasInfo.Center, innerRadius, start + i * -3 + 3);
                        SKPoint p5 = RenderUtils.PointOnArc(canvasInfo.Center, innerRadius, start + i * -3 + 1.5f);
                        SKPoint p6 = RenderUtils.PointOnArc(canvasInfo.Center, outerRadius, start + i * -3 + 3);

                        path.MoveTo(p4);
                        path.LineTo(p5);
                        path.LineTo(p6);
                    }
                    
                    if (i == stripes - 4)
                    {
                        SKPoint p4 = RenderUtils.PointOnArc(canvasInfo.Center, innerRadius, start + i * -3);
                        SKPoint p5 = RenderUtils.PointOnArc(canvasInfo.Center, outerRadius, start + i * -3);
                        SKPoint p6 = RenderUtils.PointOnArc(canvasInfo.Center, outerRadius, start + i * -3 + 1.5f);
                        
                        path.MoveTo(p4);
                        path.LineTo(p5);
                        path.LineTo(p6);

                        continue;
                    }
                }

                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, innerRadius, start + i * -3);
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, innerRadius, start + i * -3 - 1.5f);
                SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, outerRadius, start + i * -3);
                SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, outerRadius, start + i * -3 + 1.5f);
                
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

            int count = positionable.Size == 60 
                ? positionable.Size 
                : positionable.Size - 2;
            float start = positionable.Size == 60 
                ? positionable.Position * -6
                : positionable.Position * -6 - 6;
            
            for (int i = 0; i < count; i++)
            {
                bool even = i % 2 == 0;

                float angleA = start + i * -6;
                float angleB = start + (i + 1) * -6;

                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, innerRadius, even ? angleA : angleB);
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, outerRadius, even ? angleA : angleB);
                SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, innerRadius, even ? angleB : angleA);

                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
            }

            canvas.DrawPath(path, NotePaints.GetNoteBonusPaint(canvasInfo, settings, colorId, pixelScale, perspectiveScale, opacity));
        }
        
        // Snap Arrows
        if (note is SnapForwardNote or SnapBackwardNote && positionable.Size > 2)
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
            
            int count = positionable.Size / 3;
            float startPosition = positionable.Position * -6;
            
            int m = positionable.Size % 3;
            
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
                
                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius0, center);
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, radius2, right1);
                SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, radius3, right2);
                SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, radius1, center);
                SKPoint p4 = RenderUtils.PointOnArc(canvasInfo.Center, radius3, left2);
                SKPoint p5 = RenderUtils.PointOnArc(canvasInfo.Center, radius2, left1);
                
                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
                path.LineTo(p3);
                path.LineTo(p4);
                path.LineTo(p5);
                path.Close();
                
                p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius4, center);
                p1 = RenderUtils.PointOnArc(canvasInfo.Center, radius6, right3);
                p2 = RenderUtils.PointOnArc(canvasInfo.Center, radius7, right4);
                p3 = RenderUtils.PointOnArc(canvasInfo.Center, radius5, center);
                p4 = RenderUtils.PointOnArc(canvasInfo.Center, radius7, left4);
                p5 = RenderUtils.PointOnArc(canvasInfo.Center, radius6, left3);
                
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

            float arrowCount = positionable.Size * 0.5f + 1;

            float startAngle = positionable.Size * 6;
            
            SKPath path = new();
            SKPath maskPath = new();
            
            // inner side
            float maskAngle;
            float maskRadius;
            SKPoint maskPoint;
            
            for (int i = 0; i <= positionable.Size; i++)
            {
                float x = flip
                    ? (float)i / positionable.Size
                    : 1 - (float)i / positionable.Size;
                
                maskAngle = startAngle + i * -6;
                maskRadius = radius1 + (radius2 - radius1) * slideArrowMask(x);

                maskPoint = RenderUtils.PointOnArc(canvasInfo.Center, maskRadius, maskAngle);
                if (i == 0) maskPath.MoveTo(maskPoint);
                else maskPath.LineTo(maskPoint);
            }
            
            // center point
            maskAngle = startAngle + positionable.Size * -6;
            maskPoint = RenderUtils.PointOnArc(canvasInfo.Center, radius1, maskAngle);
            maskPath.LineTo(maskPoint);
            
            // outer side
            for (int i = positionable.Size; i >= 0; i--)
            {
                float x = flip
                    ? (float)i / positionable.Size
                    : 1 - (float)i / positionable.Size;
                
                maskAngle = startAngle + i * -6;
                maskRadius = radius1 + (radius0 - radius1) * slideArrowMask(x);

                maskPoint = RenderUtils.PointOnArc(canvasInfo.Center, maskRadius, maskAngle);
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
                
                float angle = startAngle + (scroll - i - 0.5f) * 12;
                float offset = flip ? -6 : 6;
                
                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius0, angle         );
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, radius0, angle - offset);
                SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, radius1, angle         );
                SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, radius2, angle - offset);
                SKPoint p4 = RenderUtils.PointOnArc(canvasInfo.Center, radius2, angle         );
                SKPoint p5 = RenderUtils.PointOnArc(canvasInfo.Center, radius1, angle + offset);
                
                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
                path.LineTo(p3);
                path.LineTo(p4);
                path.LineTo(p5);
                path.Close();
            }
            
            
            canvas.Save();
            
            canvas.RotateDegrees((positionable.Position + positionable.Size) * -6, canvasInfo.Center.X, canvasInfo.Center.Y);

            canvas.ClipPath(maskPath, SKClipOperation.Intersect, true);
            canvas.DrawPath(path, NotePaints.GetSlideFillPaint(canvasInfo, settings, positionable.Size, colorId, opacity, flip));
            
            if (!settings.LowPerformanceMode)
            {
                canvas.DrawPath(path, NotePaints.GetSlideStrokePaint(colorId, pixelScale, opacity));
            }
            
            canvas.Restore();
            
            float slideArrowMask(float x)
            {
                return x < 0.88f 
                    ? (0.653f * x + 0.175f) / 0.75f 
                    : (-6.25f * x + 6.25f) / 0.75f;
            }
        }
    }

    /// <summary>
    /// Draws a hold end note.
    /// </summary>
    private static void DrawHoldEndNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldPointNote note, float perspectiveScale, float opacity)
    {
        if (perspectiveScale is <= 0 or > 1) return;

        int colorId = (int)settings.HoldNoteColor;
        
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = canvasInfo.Scale * perspectiveScale;

        if (note.Size == 60)
        {
            if (settings.LowPerformanceMode)
            {
                canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetHoldEndBaseStrokePaint(colorId, pixelScale, opacity));
            }
            else
            {
                float radius0 = radius * 0.984f;
                float radius1 = radius * 1.016f;
                
                canvas.DrawCircle(canvasInfo.Center, radius,  NotePaints.GetHoldEndBaseStrokePaint(colorId, pixelScale * 0.8f, opacity));
                canvas.DrawCircle(canvasInfo.Center, radius0, NotePaints.GetHoldEndOutlinePaint(colorId, pixelScale, opacity));
                canvas.DrawCircle(canvasInfo.Center, radius1, NotePaints.GetHoldEndOutlinePaint(colorId, pixelScale, opacity));
            }
        }
        else
        {
            SKRect rect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);
            
            if (settings.LowPerformanceMode)
            {
                float start = note.Position * -6 - 3; 
                float sweep = Math.Min(0, note.Size * -6 + 6);

                canvas.DrawArc(rect, start, sweep, false, NotePaints.GetHoldEndBaseStrokePaint(colorId, pixelScale, opacity));
            }
            else
            {
                float radius0 = radius * 0.984f;
                float radius1 = radius * 1.016f;
                
                SKRect rect0 = new(canvasInfo.Center.X - radius0, canvasInfo.Center.Y - radius0, canvasInfo.Center.X + radius0, canvasInfo.Center.Y + radius0);
                SKRect rect1 = new(canvasInfo.Center.X - radius1, canvasInfo.Center.Y - radius1, canvasInfo.Center.X + radius1, canvasInfo.Center.Y + radius1);

                SKPath path = new();

                if (note.Size == 1)
                {
                    SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius, (note.Position + 0.35f) * -6);

                    path.ArcTo(rect0, note.Position * -6 - 6, 2.4f, true);
                    path.LineTo(p0);
                    path.ArcTo(rect1, note.Position * -6 - 3.6f, -2.4f, false);
                }
                else
                {
                    SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius, (note.Position + 0.35f) * -6);
                    SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, radius, (note.Position + note.Size - 0.35f) * -6);
                    
                    path.MoveTo(p0);
                    path.ArcTo(rect0, (note.Position + 0.6f) * -6, (note.Size - 1.2f) * -6, false);
                    path.LineTo(p1);
                    path.ArcTo(rect1, (note.Position + note.Size - 0.6f) * -6, (note.Size - 1.2f) * 6, false);
                    path.Close();
                }
                
                canvas.DrawPath(path, NotePaints.GetHoldEndBaseFillPaint(colorId, opacity));
                canvas.DrawPath(path, NotePaints.GetHoldEndOutlinePaint(colorId, pixelScale, opacity));
            }
        }
    }

    /// <summary>
    /// Draws a hold control point.
    /// </summary>
    private static void DrawHoldPointNote(SKCanvas canvas, CanvasInfo canvasInfo, HoldPointNote note, float perspectiveScale, float opacity)
    {
        if (perspectiveScale is <= 0 or > 1) return;
        
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float radius0 = radius * 0.98f;
        float radius1 = radius * 1.02f;
        float capRadius = (radius1 - radius0) * 0.5f;
        
        float pixelScale = canvasInfo.Scale * perspectiveScale;

        float startAngle = (note.Position + 0.5f) * -6;
        float sweepAngle = (note.Size - 1) * -6;
        float endAngle = startAngle + sweepAngle;
        
        SKRect longArcRect0 = new(canvasInfo.Center.X - radius0, canvasInfo.Center.Y - radius0, canvasInfo.Center.X + radius0, canvasInfo.Center.Y + radius0);
        SKRect longArcRect1 = new(canvasInfo.Center.X - radius1, canvasInfo.Center.Y - radius1, canvasInfo.Center.X + radius1, canvasInfo.Center.Y + radius1);
        
        SKPoint capPoint0 = RenderUtils.PointOnArc(canvasInfo.Center, radius, startAngle);
        SKPoint capPoint1 = RenderUtils.PointOnArc(canvasInfo.Center, radius, endAngle);
        SKRect capRect0 = new(capPoint0.X - capRadius, capPoint0.Y - capRadius, capPoint0.X + capRadius, capPoint0.Y + capRadius);
        SKRect capRect1 = new(capPoint1.X - capRadius, capPoint1.Y - capRadius, capPoint1.X + capRadius, capPoint1.Y + capRadius);
        
        SKPath path = new();
        
        path.ArcTo(longArcRect0, startAngle, sweepAngle, true);
        path.ArcTo(capRect1, endAngle - 180, 180, false);
        path.ArcTo(longArcRect1, endAngle, -sweepAngle, false);
        path.ArcTo(capRect0, startAngle, 180, false);

        canvas.DrawPath(path, NotePaints.GetHoldPointPaint(pixelScale, note.RenderType, opacity));
    }

    /// <summary>
    /// Draws a hold note surface.
    /// </summary>
    private static void DrawHoldSurface(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldNote hold, Layer layer, float time, bool playing, float opacity)
    {
        List<SKPoint> vertexScreenCoords = [];
        List<SKPoint> vertexTextureCoords = [];

        HoldPointNote[] points = hold.Points.Where(x => x.RenderType is HoldPointRenderType.Visible).ToArray();
        
        float scaledTime = settings.ShowSpeedChanges ? Timestamp.ScaledTimeFromTime(layer, time) : time;
        int maxSize = hold.MaxSize;
        int arcs = 0;
        
        bool active = hold.Timestamp.Time < time && playing;
        
        // Generate parts (groups of arcs) for every hold point, except the last.
        for (int y = 0; y < points.Length - 1; y++)
        {
            RenderHoldPoint start = new(hold, points[y], maxSize);
            RenderHoldPoint end = new(hold, points[y + 1], maxSize);

            if (start.GlobalTime < time && end.GlobalTime > time)
            {
                // Judgement line is between start and end. Insert a third point on the judgement line.
                // Then generate from start to center, and center to end.
                float t = RenderUtils.InverseLerp(start.GlobalTime, end.GlobalTime, time);
                
                RenderHoldPoint center = new()
                {
                    GlobalTime = time, 
                    GlobalScaledTime = scaledTime, 
                    LocalTime = RenderUtils.Lerp(start.LocalTime, end.LocalTime, t),
                    Interval  = RenderUtils.Lerp(start.Interval,  end.Interval,  t),
                    Start = RenderUtils.LerpCyclic(start.Start, end.Start, t, 360),
                };

                generatePart(start, center);
                generatePart(center, end);
            }
            else
            {
                // This segment does not cross the judgement line. No special handling is necessary.
                generatePart(start, end);
            }
        }
        
        // Generate the final arc for last point.
        RenderHoldPoint last = new(hold, points[^1], maxSize);
        generateArc(last.GlobalTime, last.GlobalScaledTime, last.LocalTime, last.Start, last.Interval);
        
        // Build mesh
        SKPoint[] triangles = new SKPoint[maxSize * arcs * 6];
        SKPoint[] textureCoords = new SKPoint[maxSize * arcs * 6];

        int vert = 0;
        int tris = 0;
        for (int y = 0; y < arcs - 1; y++)
        {
            for (int x = 0; x < maxSize; x++)
            {
                triangles[tris + 0] = vertexScreenCoords[vert];
                triangles[tris + 1] = vertexScreenCoords[vert + 1];
                triangles[tris + 2] = vertexScreenCoords[vert + maxSize + 1];

                triangles[tris + 5] = vertexScreenCoords[vert + 1];
                triangles[tris + 4] = vertexScreenCoords[vert + maxSize + 1];
                triangles[tris + 3] = vertexScreenCoords[vert + maxSize + 2];
                
                textureCoords[tris + 0] = vertexTextureCoords[vert];
                textureCoords[tris + 1] = vertexTextureCoords[vert + 1];
                textureCoords[tris + 2] = vertexTextureCoords[vert + maxSize + 1];

                textureCoords[tris + 5] = vertexTextureCoords[vert + 1];
                textureCoords[tris + 4] = vertexTextureCoords[vert + maxSize + 1];
                textureCoords[tris + 3] = vertexTextureCoords[vert + maxSize + 2];

                vert++;
                tris += 6;
            }

            vert++;
        }
        
        // Draw mesh
        SKRect rect = new(canvasInfo.Center.X - canvasInfo.Radius, canvasInfo.Center.Y - canvasInfo.Radius, canvasInfo.Center.X + canvasInfo.Radius, canvasInfo.Center.Y + canvasInfo.Radius);
        SKRoundRect roundRect = new(rect, canvasInfo.Radius);
        
        canvas.Save();
        canvas.ClipRoundRect(roundRect);
        canvas.DrawVertices(SKVertexMode.Triangles, triangles, textureCoords, null, NotePaints.GetHoldSurfacePaint(active, opacity));
        canvas.Restore();
        return;

        void generatePart(RenderHoldPoint start, RenderHoldPoint end)
        {
            float interval = 1;

            bool differentTime = start.GlobalTime != end.GlobalTime;
            bool differentShape = start.Start != end.Start || start.Interval != end.Interval;
            
            if (differentTime && differentShape)
            {
                interval = 20.0f / (end.GlobalTime - start.GlobalTime);
            }
            
            // For every imaginary "sub point" between start and end.
            for (float t = 0; t < 1; t += interval)
            {
                float globalTime       = RenderUtils.Lerp(start.GlobalTime,       end.GlobalTime,       t);
                float globalScaledTime = RenderUtils.Lerp(start.GlobalScaledTime, end.GlobalScaledTime, t);
                float localTime        = RenderUtils.Lerp(start.LocalTime,        end.LocalTime,        t);
                float intervalAngle    = RenderUtils.Lerp(start.Interval,         end.Interval,         t);
                float startAngle       = RenderUtils.LerpCyclic(start.Start, end.Start, t, 360);

                generateArc(globalTime, globalScaledTime, localTime, startAngle, intervalAngle);
            }
        }

        void generateArc(float globalTime, float globalScaledTime, float localTime, float startAngle, float intervalAngle)
        {
            arcs++;

            float delta = time > globalTime || !settings.ShowSpeedChanges
                ? globalTime - time
                : globalScaledTime - scaledTime;

            float scale = RenderUtils.InverseLerp(3333.333f / (settings.NoteSpeed * 0.1f), 0, delta);
            scale = Math.Max(0, scale);
            scale = RenderUtils.Perspective(scale);

            float radius = canvasInfo.JudgementLineRadius * scale;

            for (int x = 0; x <= maxSize; x++)
            {
                float angle = startAngle + intervalAngle * x;
                SKPoint screen = RenderUtils.PointOnArc(canvasInfo.Center, radius, angle);

                float texX = 512 * ((int)settings.HoldNoteColor + 0.5f) / 13.0f;
                float texY = 512 * (1 - localTime);
                SKPoint tex = new(texX, texY);

                vertexScreenCoords.Add(screen);
                vertexTextureCoords.Add(tex);
            }
        }
    }
    
    /// <summary>
    /// Draws a sync connector.
    /// </summary>
    private static void DrawSyncNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, SyncNote note, float perspectiveScale, float opacity)
    {
        if (perspectiveScale is <= 0 or > 1) return;
        
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

    /// <summary>
    /// Draws a measure or beat line.
    /// </summary>
    private static void DrawMeasureLineNote(SKCanvas canvas, CanvasInfo canvasInfo, float perspectiveScale, float linearScale, bool isBeatLine, float opacity)
    {
        if (perspectiveScale is <= 0 or > 1) return;
        
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;

        if (isBeatLine)
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetBeatLinePaint(canvasInfo, linearScale, opacity));
        }
        else
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetMeasureLinePaint(canvasInfo, linearScale, opacity));
        }
    }

    /// <summary>
    /// Draws a row of lanes.
    /// </summary>
    private static void DrawLanes(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, int position, int size, float time)
    {
        if (size == 0) return;
        
        // Lane Background
        SKRect rect = new(canvasInfo.Center.X - canvasInfo.Radius, canvasInfo.Center.Y - canvasInfo.Radius, canvasInfo.Center.X + canvasInfo.Radius, canvasInfo.Center.Y + canvasInfo.Radius);
        canvas.DrawArc(rect, position * -6, size * -6, true, NotePaints.GetLanePaint(canvasInfo, settings, time));
        
        // Guide Lines
        if (settings.GuideLineType != RenderSettings.GuideLineTypeOption.None)
        {
            SKPaint guideLinePaint = NotePaints.GetGuideLinePaint(canvasInfo);
            
            for (int i = position; i < position + size; i++)
            {
                // Type A: always draw.
                
                // Type B: draw every 2nd lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.B && (i + 1) % 2 != 0) continue;
                
                // Type C: draw every 3rd lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.C && i % 3 != 0) continue;

                // Type D: draw every 4th lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.D && (i + 1) % 4 != 0) continue;
                
                // Type E: draw every 5th lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.E && i % 5 != 0) continue;

                // Type F: draw every 10th lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.F && (i + 5) % 10 != 0) continue;

                // Type G: draw every 15th lane.
                if (settings.GuideLineType == RenderSettings.GuideLineTypeOption.G && i % 15 != 0) continue;

                // This batches drawcalls for lines together where possible.
                // 
                if (i - 30 >= position) continue;
                if (i + 30 < position + size)
                {
                    SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, canvasInfo.JudgementLineRadius, i * -6);
                    SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, canvasInfo.JudgementLineRadius, i * -6 - 180);
                    canvas.DrawLine(p0, p1, guideLinePaint);
                }
                else
                {
                    SKPoint p = RenderUtils.PointOnArc(canvasInfo.Center, canvasInfo.JudgementLineRadius, i * -6);
                    canvas.DrawLine(canvasInfo.Center, p, guideLinePaint);
                }
            }
        }
        
        // Judgement Line
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

    /// <summary>
    /// Draws part of the in-game UI.
    /// </summary>
    private static void DrawInterface(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Entry entry, float time)
    {
        if (entry.ChartEnd.Time > 0)
        {
            float radius = 0.98113f * canvasInfo.Radius;
            SKRect rect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);

            float t = time / entry.ChartEnd.Time;
            canvas.DrawArc(rect, 270, 360 - t * 360, false, NotePaints.GetSongTimerPaint(canvasInfo.Scale));
        }

        float textRadius = canvasInfo.JudgementLineRadius * 0.987f;
        SKRect textRect = new(canvasInfo.Center.X - textRadius, canvasInfo.Center.Y - textRadius, canvasInfo.Center.X + textRadius, canvasInfo.Center.Y + textRadius);
        
        SKPath path = new();
        path.ArcTo(textRect, 0, 359, true);
        path.ArcTo(textRect, 359, 359, false);

        string difficultyString = entry.Difficulty switch
        {
            Difficulty.None => "",
            Difficulty.Normal => "N O R M A L / Lv.",
            Difficulty.Hard => "H A R D / Lv.",
            Difficulty.Expert => "E X P E R T / Lv.",
            Difficulty.Inferno => "I N F E R N O / Lv.",
            Difficulty.WorldsEnd => "W O R L D ' S  E N D / Lv.",
            _ => "",
        };
        
        float circumference = textRadius * float.Pi;

        float titleAngle = circumference * 0.865f;
        float levelAngle = circumference * 0.837f;
        float difficultyAngle = entry.Difficulty switch
        {
            Difficulty.None => 0f,
            Difficulty.Normal => 0.7815f,
            Difficulty.Hard => 0.7945f,
            Difficulty.Expert => 0.7843f,
            Difficulty.Inferno => 0.7782f,
            Difficulty.WorldsEnd => 0.7565f,
            _ => 0f,
        };
        difficultyAngle *= circumference;
        
        uint diffTextColor = entry.Difficulty switch
        {
            Difficulty.None => 0xFFBFBFBF,
            Difficulty.Normal => 0xFF1B7CFF,
            Difficulty.Hard => 0xFFFFC300,
            Difficulty.Expert => 0xFFFF0084,
            Difficulty.Inferno => 0xFF400084,
            Difficulty.WorldsEnd => 0xFF000000,
            _ => 0xFFBFBFBF,
        };

        canvas.DrawTextOnPath(difficultyString, path, new(difficultyAngle, 0), NotePaints.GetBoldFont(20 * canvasInfo.Scale), NotePaints.GetTextPaint(diffTextColor));
        canvas.DrawTextOnPath(entry.LevelString, path, new(levelAngle, 0), NotePaints.GetBoldFont(25 * canvasInfo.Scale), NotePaints.GetTextPaint(diffTextColor));
        canvas.DrawTextOnPath(entry.Title, path, new(titleAngle, 0), NotePaints.GetBoldFont(20 * canvasInfo.Scale), NotePaints.GetTextPaint(0xFFFB67B7));
    }
}

file struct RenderObject(ITimeable @object, Layer? layer, int? layerIndex, float scale, bool sync, bool isVisible)
{
    // Universal
    public readonly ITimeable Object = @object;
    public readonly Layer? Layer = layer;
    public readonly int? LayerIndex = layerIndex;
    public readonly float Scale = scale;
    public readonly bool IsVisible = isVisible;
    
    // Note-Specific
    public readonly bool Sync = sync;
}

file struct RenderHoldPoint
{
    internal RenderHoldPoint(HoldNote hold, HoldPointNote point, int maxSize)
    {
        GlobalTime = point.Timestamp.Time;
        GlobalScaledTime = point.Timestamp.ScaledTime;

        LocalTime = hold.Points.Count > 1 && hold.Points[0].Timestamp.Time != hold.Points[^1].Timestamp.Time
            ? (point.Timestamp.Time - hold.Points[0].Timestamp.Time) / (hold.Points[^1].Timestamp.Time - hold.Points[0].Timestamp.Time)
            : 0;
        
        Start = point.Size == 60 
            ? point.Position * -6f 
            : point.Position * -6f - 4.2f;
        
        Interval = point.Size == 60 
            ? point.Size * -6f / maxSize 
            : (point.Size * -6f + 8.4f) / maxSize;
    }

    public float GlobalTime;
    public float GlobalScaledTime;
    public float LocalTime;
    public float Start;
    public float Interval;
}