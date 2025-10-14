using System.Diagnostics;
using System.Drawing;
using SaturnData.Notation.Core;
using SaturnData.Notation.Events;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SkiaSharp;

namespace SaturnView;

// TODO:
// Improve Performance
// Judgement window and Hold window Visualizations
// Hit testing

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
    /// <param name="playing">The current playback state.</param>
    public static void Render(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Chart chart, Entry entry, float time, bool playing)
    {
        float viewDistance = 3333.333f / (settings.NoteSpeed * 0.1f);

        bool[] lanesToDraw = new bool[60];
        List<RenderObject> objectsToDraw = [];
        List<RenderObject> holdEndsToDraw = [];
        List<RenderObject> holdsToDraw = [];
        List<RenderObject> eventAreasToDraw = [];
        List<RenderBonusSweepEffect> bonusSweepEffectsToDraw = [];
        Note? activeRNote = null;
        
        lock (chart)
        {
            calculateLanes();
            calculateRenderObjects();
        }
        
        SKRect rect = new(0, 0, canvasInfo.Width, canvasInfo.Height);
        SKRoundRect roundRect = new(rect, canvasInfo.Radius);
        
        canvas.Clear(canvasInfo.BackgroundColor);
        canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);
        
        DrawBackground(canvas, canvasInfo, settings, chart, entry, time);
        
        renderRNoteEffects();
        renderLanes();
        renderEventAreas();
        renderHoldEnds();
        renderHoldSurfaces();
        renderObjects();
        renderBonusEffects();

        DrawInterface(canvas, canvasInfo, settings, entry, time);
        
        return;
        
        
        //canvas.DrawText($"{stopwatch.ElapsedTicks / 10000.0f}", new(canvasInfo.Width / 2, 30), SKTextAlign.Center, NotePaints.GetBoldFont(20), NotePaints.DebugPaint3);

        void calculateLanes()
        {
            if (!settings.ShowLaneToggleAnimations) return;
            
            // Get the current state of all lanes.
            foreach (Note note in chart.LaneToggles)
            {
                if (note.Timestamp.Time > time) continue;
                if (note is not ILaneToggle laneToggle) continue;
                if (note is not IPositionable positionable) continue;
                
                float delta = time - note.Timestamp.Time;
                float duration = RenderUtils.GetSweepDuration(laneToggle.Direction, positionable.Size);
                
                bool state = note is LaneShowNote; // not fully explicit but good enough.

                // Instant or after the sweep. Set lanes without animating.
                if (duration == 0 || delta > duration)
                {
                    for (int i = positionable.Position; i < positionable.Position + positionable.Size; i++)
                    {
                        lanesToDraw[i % 60] = state;
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
                        lanesToDraw[(centerClockwise - i + offset) % 60] = state;
                        lanesToDraw[(centerCounterclockwise + i + offset) % 60] = state;
                    }
                }
                else if (laneToggle.Direction is LaneSweepDirection.Clockwise)
                {
                    for (int i = 0; i < (int)(positionable.Size * progress); i++)
                    {
                        lanesToDraw[(positionable.Position + positionable.Size - i + 59) % 60] = state;
                    }
                }
                else if (laneToggle.Direction is LaneSweepDirection.Counterclockwise)
                {
                    for (int i = 0; i < (int)(positionable.Size * progress); i++)
                    {
                        lanesToDraw[(i + positionable.Position + 60) % 60] = state;
                    }
                }
            }
        }
        
        void calculateRenderObjects()
        {
            if (chart.Layers.Count == 0) return;
            
            float scaledTime = Timestamp.ScaledTimeFromTime(chart.Layers[0], time);
            
            bool checkForBonusNotes = settings.BonusEffectVisibility == RenderSettings.EffectVisibilityOption.AlwaysOn
                                || settings.BonusEffectVisibility == RenderSettings.EffectVisibilityOption.OnlyWhenPlaying && playing
                                || settings.BonusEffectVisibility == RenderSettings.EffectVisibilityOption.OnlyWhenPaused && !playing;
            
            bool checkForRNotes = settings.RNoteEffectOpacity != 0 &&
                                  (
                                         settings.RNoteEffectVisibility == RenderSettings.EffectVisibilityOption.AlwaysOn
                                      || settings.RNoteEffectVisibility == RenderSettings.EffectVisibilityOption.OnlyWhenPlaying && playing
                                      || settings.RNoteEffectVisibility == RenderSettings.EffectVisibilityOption.OnlyWhenPaused  && !playing
                                  );
            
            // Find all visible global events.
            foreach (Event @event in chart.Events)
            {
                if (settings.HideEventMarkersDuringPlayback && playing) break;
                
                if (!RenderUtils.GetProgress(@event, settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float progress)) continue;
                objectsToDraw.Add(new(@event, chart.Layers[0], 0, progress, false, RenderUtils.IsVisible(@event, settings)));
            }

            // Find all visible lane toggles.
            foreach (Note note in chart.LaneToggles)
            {
                if (settings.HideLaneToggleNotesDuringPlayback && playing) break;
                if (note is not ILaneToggle laneToggle) continue;
                if (note is not IPositionable positionable) continue;
                
                float tStart = 1 - (note.Timestamp.Time - time) / viewDistance;
                float tEnd = 1 - (note.Timestamp.Time + RenderUtils.GetSweepDuration(laneToggle.Direction, positionable.Size) - time) / viewDistance;

                if (tStart <= 0 && tEnd <= 0) continue;
                if (tStart > 1.01f && tEnd > 1.01f) continue;
                
                objectsToDraw.Add(new(note, null, 0, tStart, false, RenderUtils.IsVisible(note, settings)));
            }
            
            // Find all visible objects in layers.
            for (int l = 0; l < chart.Layers.Count; l++)
            {
                Layer layer = chart.Layers[l];

                if (l != 0)
                {
                    scaledTime = Timestamp.ScaledTimeFromTime(layer, time);
                }

                // Find all visible events.
                foreach (Event @event in layer.Events)
                {
                    if (settings.HideEventMarkersDuringPlayback && playing) break;

                    if (@event is StopEffectEvent stopEffectEvent && stopEffectEvent.SubEvents.Length == 2)
                    {
                        // Start Marker
                        if (RenderUtils.GetProgress(stopEffectEvent.SubEvents[0], false, viewDistance, time, scaledTime, out float t0))
                        {
                            objectsToDraw.Add(new(stopEffectEvent.SubEvents[0], layer, l, t0, false, layer.Visible && RenderUtils.IsVisible(@event, settings)));
                        }
                        
                        // End Marker
                        if (RenderUtils.GetProgress(stopEffectEvent.SubEvents[1], false, viewDistance, time, scaledTime, out float t1))
                        {
                            objectsToDraw.Add(new(stopEffectEvent.SubEvents[1], layer, l, t1, false, layer.Visible && RenderUtils.IsVisible(@event, settings)));
                        }

                        // Area Fill
                        if (stopEffectEvent.SubEvents[0].Timestamp.Time <= time + viewDistance && stopEffectEvent.SubEvents[1].Timestamp.Time >= time)
                        {
                            eventAreasToDraw.Add(new(stopEffectEvent, layer, l, 0, false, layer.Visible && RenderUtils.IsVisible(@event, settings)));
                        }
                    }
                    else if (@event is ReverseEffectEvent reverseEffectEvent && reverseEffectEvent.SubEvents.Length == 3)
                    {
                        // Start Marker
                        if (RenderUtils.GetProgress(reverseEffectEvent.SubEvents[0], false, viewDistance, time, scaledTime, out float t0))
                        {
                            objectsToDraw.Add(new(reverseEffectEvent.SubEvents[0], layer, l, t0, false, layer.Visible && RenderUtils.IsVisible(@event, settings)));
                        }
                        
                        // Middle Marker
                        if (RenderUtils.GetProgress(reverseEffectEvent.SubEvents[1], false, viewDistance, time, scaledTime, out float t1))
                        {
                            objectsToDraw.Add(new(reverseEffectEvent.SubEvents[1], layer, l, t1, false, layer.Visible && RenderUtils.IsVisible(@event, settings)));
                        }
                        
                        // End Marker
                        if (RenderUtils.GetProgress(reverseEffectEvent.SubEvents[2], false, viewDistance, time, scaledTime, out float t2))
                        {
                            objectsToDraw.Add(new(reverseEffectEvent.SubEvents[2], layer, l, t2, false, layer.Visible && RenderUtils.IsVisible(@event, settings)));
                        }
                        
                        // Area Fill
                        if (reverseEffectEvent.SubEvents[0].Timestamp.Time <= time + viewDistance && reverseEffectEvent.SubEvents[2].Timestamp.Time >= time)
                        {
                            eventAreasToDraw.Add(new(reverseEffectEvent, layer, l, 0, false, layer.Visible && RenderUtils.IsVisible(@event, settings)));
                        }
                    }
                    else
                    {
                        if (!RenderUtils.GetProgress(@event, settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float t)) continue;
                        objectsToDraw.Add(new(@event, layer, l, t, false, layer.Visible && RenderUtils.IsVisible(@event, settings)));
                    }
                }
                
                VisibilityChangeEvent? lastVisibilityChange = layer.LastVisibilityChange(time);
                if (settings.ShowVisibilityChanges && lastVisibilityChange != null && !lastVisibilityChange.Visible) continue;
                
                ReverseEffectEvent? lastReverseEffect = layer.LastReverseEffect(time);
                bool reverseActive = settings.ShowSpeedChanges && lastReverseEffect != null && lastReverseEffect.IsActive(time);
                
                // Find all visible notes.
                for (int n = 0; n < layer.Notes.Count; n++)
                {
                    Note note = layer.Notes[n];
                    
                    // Non-reversed notes are hidden during a reverse.
                    if (reverseActive && !lastReverseEffect!.ContainedNotes.Contains(note)) continue;

                    // Bonus Spin FX
                    if (checkForBonusNotes && note is SlideClockwiseNote or SlideCounterclockwiseNote && note is IPlayable { BonusType: BonusType.Bonus })
                    {
                        float bpm = chart.LastTempoChange(time)?.Tempo ?? 120;

                        float duration = bpm >= 200
                            ? 480000 / bpm
                            : 240000 / bpm;

                        if (note.Timestamp.Time < time && note.Timestamp.Time + duration > time && note is IPositionable positionable)
                        {
                            int startPosition = positionable.Position + positionable.Size / 2;
                            bool isCounterclockwise = note is SlideCounterclockwiseNote;
                            
                            bonusSweepEffectsToDraw.Add(new(startPosition, note.Timestamp.Time, duration, isCounterclockwise));
                        }
                    }
                    
                    // R-Note FX
                    if (checkForRNotes && note is IPlayable { BonusType: BonusType.R, JudgementType: not JudgementType.Fake })
                    {
                        if (note.Timestamp.Time <= time && note.Timestamp.Time + 550f > time)
                        {
                            activeRNote = note;
                        }
                    }
                    
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
                        
                        bool isVisible = layer.Visible && RenderUtils.IsVisible(holdNote, settings);
                        
                        holdsToDraw.Add(new(holdNote, layer, l, 0, false, isVisible));
                        
                        // Hold Start
                        if (RenderUtils.GetProgress(holdNote.Points[0], settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float tStart))
                        {
                            Note? prev = n > 0                     ? layer.Notes[n - 1] : null;
                            Note? next = n < layer.Notes.Count - 1 ? layer.Notes[n + 1] : null;
                            bool sync = note.IsSync(prev) || note.IsSync(next);

                            objectsToDraw.Add(new(note, layer, l, tStart, sync, isVisible));
                        }

                        // Hold End
                        if (RenderUtils.GetProgress(holdNote.Points[^1], settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float tEnd))
                        {
                            holdEndsToDraw.Add(new(holdNote.Points[^1], layer, l, tEnd, false, isVisible));
                        }

                        // Hold Points
                        for (int j = 1; j < holdNote.Points.Count - 1; j++)
                        {
                            if (settings.HideHoldControlPointsDuringPlayback && playing) break;
                            
                            HoldPointNote point = holdNote.Points[j];
                            float t = settings.ShowSpeedChanges
                                ? 1 - (point.Timestamp.ScaledTime - scaledTime) / viewDistance
                                : 1 - (point.Timestamp.Time - time) / viewDistance;

                            objectsToDraw.Add(new(point, layer, l, t, false, isVisible));
                        }
                    }
                    else
                    {
                        // Normal Notes
                        if (!RenderUtils.GetProgress(note, settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float t)) continue;

                        Note? prev = n > 0                     ? layer.Notes[n - 1] : null;
                        Note? next = n < layer.Notes.Count - 1 ? layer.Notes[n + 1] : null;
                        bool sync = note.IsSync(prev) || note.IsSync(next);

                        objectsToDraw.Add(new(note, layer, l, t, sync, layer.Visible && RenderUtils.IsVisible(note, settings)));
                    }
                }

                // Find all visible generated notes.
                foreach (Note note in layer.GeneratedNotes)
                {
                    if (reverseActive && !lastReverseEffect!.ContainedNotes.Contains(note)) continue;
                    if (!RenderUtils.GetProgress(note, settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float t)) continue;

                    objectsToDraw.Add(new(note, layer, l, t, false, layer.Visible && RenderUtils.IsVisible(note, settings)));
                }
            }
            
            objectsToDraw = objectsToDraw
                .OrderBy(x => x.IsVisible)
                .ThenBy(x => x.LayerIndex)
                .ThenByDescending(x => x.Object is ILaneToggle)
                .ThenBy(x => x.Scale)
                .ThenByDescending(x => x.Object is SyncNote or MeasureLineNote)
                .ThenByDescending(x => x.Object is Event)
                .ThenByDescending(x => x.Object is HoldNote or HoldPointNote)
                .ThenByDescending(x => (x.Object as IPositionable)?.Size ?? 60)
                .ToList();
        }
        
        void renderLanes()
        {
            // Skip Animations and draw all lanes.
            if (!settings.ShowLaneToggleAnimations)
            {
                DrawLanes(canvas, canvasInfo, settings, 0, 60, time);
                return;
            }
            
            // Check if all lanes are shown or hidden.
            bool allShown = true;
            bool allHidden = true;
            for (int i = 0; i < 60; i++)
            {
                if (!allShown && !allHidden) break;
                
                if (!lanesToDraw[i]) allShown = false;
                if (lanesToDraw[i]) allHidden = false;
            }

            // Skip drawing entirely.
            if (allHidden) return;
            
            // Skip batching and draw all lanes.
            if (allShown)
            {
                DrawLanes(canvas, canvasInfo, settings, 0, 60, time);
                return;
            }

            // Attempt batching to draw large chunks of lanes all at once.
            bool lastState = lanesToDraw[59];
            bool batchActive = false;

            int position = 0;
            int size = 0;

            int firstBatchPosition = -1;
            
            for (int i = 0; i < 120; i++)
            {
                // Stop once position of first batch is reached again.
                if (i % 60 == firstBatchPosition) break;
                
                bool currentState = lanesToDraw[i % 60];
                
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
        
        void renderRNoteEffects()
        {
            if (activeRNote == null) return;
            if (settings.RNoteEffectOpacity == 0) return;
            if (settings.RNoteEffectVisibility == RenderSettings.EffectVisibilityOption.AlwaysOff) return;
            if (settings.RNoteEffectVisibility == RenderSettings.EffectVisibilityOption.OnlyWhenPlaying && !playing) return;
            if (settings.RNoteEffectVisibility == RenderSettings.EffectVisibilityOption.OnlyWhenPaused && playing) return;
            
            DrawRNoteEffect(canvas, canvasInfo, settings, activeRNote.Timestamp.Time, time);
        }

        void renderEventAreas()
        {
            foreach (RenderObject renderObject in eventAreasToDraw)
            {
                if (renderObject.Object is not Event @event) continue;
                
                DrawEventArea(canvas, canvasInfo, @event, time, viewDistance, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
            }
        }

        void renderHoldEnds()
        {
            foreach (RenderObject renderObject in holdEndsToDraw)
            {
                if (renderObject.Object is not HoldPointNote holdPointNote) continue;
                
                DrawHoldEndNote(canvas, canvasInfo, settings, holdPointNote, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
            }
        }

        void renderHoldSurfaces()
        {
            foreach (RenderObject renderObject in holdsToDraw)
            {
                if (renderObject.Object is not HoldNote holdNote) continue;
                if (renderObject.Layer == null) continue;
                
                DrawHoldSurface(canvas, canvasInfo, settings, holdNote, renderObject.Layer, time, playing, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
            }
        }
        
        void renderObjects()
        {
            foreach (RenderObject renderObject in objectsToDraw)
            {
                if (renderObject.Object is HoldPointNote holdPointNote)
                {
                    DrawHoldPointNote(canvas, canvasInfo, settings, holdPointNote, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
                }
                else if (renderObject.Object is SyncNote syncNote)
                {
                    DrawSyncNote(canvas, canvasInfo, settings, syncNote, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
                }
                else if (renderObject.Object is MeasureLineNote measureLineNote)
                {
                    if (!settings.ShowBeatLineNotes && measureLineNote.IsBeatLine) continue;
                    
                    DrawMeasureLineNote(canvas, canvasInfo, settings, measureLineNote, RenderUtils.Perspective(renderObject.Scale), renderObject.Scale, measureLineNote.IsBeatLine, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
                }
                else if (renderObject.Object is ILaneToggle laneToggle)
                {
                    DrawLaneToggle(canvas, canvasInfo, settings, laneToggle, time, viewDistance, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
                }
                else if (renderObject.Object is Note note)
                {
                    DrawNote(canvas, canvasInfo, settings, note, RenderUtils.Perspective(renderObject.Scale), renderObject.Scale, renderObject.Sync, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
                }
                else if (renderObject.Object is Event @event)
                {
                    DrawEvent(canvas, canvasInfo, settings, @event, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f);
                }
            }
        }

        void renderBonusEffects()
        {
            foreach (RenderBonusSweepEffect sweepEffect in bonusSweepEffectsToDraw)
            {
                DrawBonusSweepEffect(canvas, canvasInfo, sweepEffect.StartPosition, sweepEffect.StartTime, sweepEffect.Duration, sweepEffect.IsCounterclockwise, time);
            }
        }
    }

    /// <summary>
    /// Renders a snapshot of a chart at the provided timestamp, then writes it to a file.
    /// </summary>
    /// <param name="filepath">The filepath to write to.</param>
    /// <param name="resolution">The resolution of the image. (Same value for X and Y)</param>
    /// <param name="settings">The render settings to follow.</param>
    /// <param name="chart">The chart to draw.</param>
    /// <param name="entry">The entry to draw.</param>
    /// <param name="time">The time of the snapshot to draw.</param>
    /// <param name="playing">The current playback state.</param>
    public static void RenderToPng(string filepath, int resolution, RenderSettings settings, Chart chart, Entry entry, float time, bool playing)
    {
        using SKBitmap bitmap = new(resolution, resolution);
        using SKCanvas canvas = new(bitmap);
        
        CanvasInfo canvasInfo = new()
        {
            BackgroundColor = new(0x00000000),
            Center = new(resolution / 2.0f, resolution / 2.0f),
            Width = resolution,
            Height = resolution,
            Radius = resolution / 2.0f,
        };

        Render(canvas, canvasInfo, settings, chart, entry, time, playing);

        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using Stream stream = File.OpenWrite(filepath);

        data.SaveTo(stream);
    }
    
    /// <summary>
    /// Draws a standard note body, sync outline, r-effect, and arrows.
    /// </summary>
    private static void DrawNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Note note, float perspectiveScale, float linearScale, bool sync, float opacity, HashSet<ITimeable>? selectedObjects = null, HashSet<ITimeable>? pointerOverObjects = null)
    {
        if (opacity == 0) return;
        if (perspectiveScale is <= 0 or > 1.25f) return;
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
                    if (i == 0) continue;
                    if (i >= stripes - 3) continue;
                    
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
        
        // Selection outline.
        bool selected = selectedObjects != null && selectedObjects.Contains(note);
        bool pointerOver = pointerOverObjects != null && pointerOverObjects.Contains(note);
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, positionable.Position, positionable.Size, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws a hold end note.
    /// </summary>
    private static void DrawHoldEndNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldPointNote note, float perspectiveScale, float opacity, HashSet<ITimeable>? selectedObjects = null, HashSet<ITimeable>? pointerOverObjects = null)
    {
        if (opacity == 0) return;
        if (perspectiveScale is <= 0 or > 1.25f) return;

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
        
        // Selection outline.
        bool selected = selectedObjects != null && selectedObjects.Contains(note);
        bool pointerOver = pointerOverObjects != null && pointerOverObjects.Contains(note);
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, note.Position, note.Size, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws a hold control point.
    /// </summary>
    private static void DrawHoldPointNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldPointNote note, float perspectiveScale, float opacity, HashSet<ITimeable>? selectedObjects = null, HashSet<ITimeable>? pointerOverObjects = null)
    {
        if (opacity == 0) return;
        if (perspectiveScale is <= 0 or > 1.25f) return;

        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = canvasInfo.Scale * perspectiveScale;
        float startAngle = (note.Position + 0.7f) * -6;
        float sweepAngle = (note.Size - 1.4f) * -6;
        
        if (settings.LowPerformanceMode)
        {
            SKRect rect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);
            canvas.DrawArc(rect, startAngle, sweepAngle, false, NotePaints.GetHoldPointPaint(settings, pixelScale, note.RenderType, opacity));
            
            return;
        }
        
        float radius0 = radius * 0.98f;
        float radius1 = radius * 1.02f;
        float capRadius = (radius1 - radius0) * 0.5f;
    
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
    
        canvas.DrawPath(path, NotePaints.GetHoldPointPaint(settings, pixelScale, note.RenderType, opacity));
        
        // Selection outline.
        bool selected = selectedObjects != null && selectedObjects.Contains(note);
        bool pointerOver = pointerOverObjects != null && pointerOverObjects.Contains(note);
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, note.Position, note.Size, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws a hold note surface.
    /// </summary>
    private static void DrawHoldSurface(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldNote hold, Layer layer, float time, bool playing, float opacity, HashSet<ITimeable>? selectedObjects = null, HashSet<ITimeable>? pointerOverObjects = null)
    {
        if (opacity == 0) return;
        List<SKPoint> vertexScreenCoords = [];
        List<SKPoint> vertexTextureCoords = [];

        HoldPointNote[] points = hold.Points.Where(x => x.RenderType is HoldPointRenderType.Visible).ToArray();

        float visibleTime = 3333.333f / (settings.NoteSpeed * 0.1f);
        
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
        generateArc(getScale(last.GlobalTime, last.GlobalScaledTime), last.LocalTime, last.Start, last.Interval);
        
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
        canvas.DrawVertices(SKVertexMode.Triangles, triangles, textureCoords, null, NotePaints.GetHoldSurfacePaint(active, opacity));
        
        // Selection outline.
        bool selected = selectedObjects != null && selectedObjects.Contains(hold);
        bool pointerOver = pointerOverObjects != null && pointerOverObjects.Contains(hold);
        
        if (selected || pointerOver)
        {
            canvas.DrawVertices(SKVertexMode.Triangles, triangles, null, null, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
        }
        return;

        void generatePart(RenderHoldPoint start, RenderHoldPoint end)
        {
            float startScale = getScale(start.GlobalTime, start.GlobalScaledTime);
            float endScale = getScale(end.GlobalTime, end.GlobalScaledTime);

            if (startScale > 1.25f && endScale > 1.25f) return;
            
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
                // Very brute-force optimization: Look ahead and behind by one step to see if the arc and its neighbors are entirely off-screen.
                float previousT = t - interval;
                float previousGlobalTime       = RenderUtils.Lerp(start.GlobalTime,       end.GlobalTime,       previousT);
                float previousGlobalScaledTime = RenderUtils.Lerp(start.GlobalScaledTime, end.GlobalScaledTime, previousT);
                float previousScale = getScale(previousGlobalTime, previousGlobalScaledTime);
                
                float nextT = t + interval;
                float nextGlobalTime       = RenderUtils.Lerp(start.GlobalTime,       end.GlobalTime,       nextT);
                float nextGlobalScaledTime = RenderUtils.Lerp(start.GlobalScaledTime, end.GlobalScaledTime, nextT);
                float nextScale = getScale(nextGlobalTime, nextGlobalScaledTime);
                
                float globalTime       = RenderUtils.Lerp(start.GlobalTime,       end.GlobalTime,       t);
                float globalScaledTime = RenderUtils.Lerp(start.GlobalScaledTime, end.GlobalScaledTime, t);
                float scale = getScale(globalTime, globalScaledTime);

                if (scale < 0)
                {
                    if (nextScale < 0 && nextT < 1) continue;
                    if (previousScale < 0 && previousT >= 0) continue;
                }
                else if (scale > 1.25f)
                {
                    if (nextScale > 1.25f && nextT < 1) continue;
                    if (previousScale > 1.25f && previousT >= 0) continue;
                }
                
                float localTime        = RenderUtils.Lerp(start.LocalTime,        end.LocalTime,        t);
                float intervalAngle    = RenderUtils.Lerp(start.Interval,         end.Interval,         t);
                float startAngle       = RenderUtils.LerpCyclic(start.Start, end.Start, t, 360);

                generateArc(scale, localTime, startAngle, intervalAngle);
            }
        }
        
        void generateArc(float scale, float localTime, float startAngle, float intervalAngle)
        {
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
            
            arcs++;
        }

        float getScale(float globalTime, float globalScaledTime)
        {
            return !settings.ShowSpeedChanges || time > globalTime
                ? RenderUtils.InverseLerp(time + visibleTime, time, globalTime)
                : RenderUtils.InverseLerp(scaledTime + visibleTime, scaledTime, globalScaledTime);
        }
    }
    
    /// <summary>
    /// Draws a sync connector.
    /// </summary>
    private static void DrawSyncNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, SyncNote note, float perspectiveScale, float opacity, HashSet<ITimeable>? selectedObjects = null, HashSet<ITimeable>? pointerOverObjects = null)
    {
        if (opacity == 0) return;
        if (perspectiveScale is <= 0 or > 1.25f) return;
        
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
        
        // Selection outline.
        bool selected = selectedObjects != null && selectedObjects.Contains(note);
        bool pointerOver = pointerOverObjects != null && pointerOverObjects.Contains(note);
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, note.Position, note.Size, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws a measure or beat line.
    /// </summary>
    private static void DrawMeasureLineNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, MeasureLineNote note, float perspectiveScale, float linearScale, bool isBeatLine, float opacity, HashSet<ITimeable>? selectedObjects = null, HashSet<ITimeable>? pointerOverObjects = null)
    {
        if (opacity == 0) return;
        if (perspectiveScale is <= 0 or > 1.25f) return;
        
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;

        if (isBeatLine)
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetBeatLinePaint(canvasInfo, linearScale, opacity));
        }
        else
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetMeasureLinePaint(canvasInfo, linearScale, opacity));
        }
        
        // Selection outline.
        bool selected = selectedObjects != null && selectedObjects.Contains(note);
        bool pointerOver = pointerOverObjects != null && pointerOverObjects.Contains(note);
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, canvasInfo.Scale * perspectiveScale, 0, 60, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws an event.
    /// </summary>
    private static void DrawEvent(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Event @event, float perspectiveScale, float opacity, HashSet<ITimeable>? selectedObjects = null, HashSet<ITimeable>? pointerOverObjects = null)
    {
        if (opacity == 0) return;
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = canvasInfo.Scale * perspectiveScale;

        if (opacity == 1 && @event is not EffectSubEvent)
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetEventMarkerFillPaint(canvasInfo, @event, perspectiveScale));
        }
        canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetEventMarkerPaint(@event, pixelScale, opacity));

        // Selection outline.
        bool selected = selectedObjects != null && selectedObjects.Contains(@event);
        bool pointerOver = pointerOverObjects != null && pointerOverObjects.Contains(@event);
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, 0, 60, selected, pointerOver);
        }
        
        canvas.Save();
        
        if (@event is TempoChangeEvent tempoChangeEvent)
        {
            SKPath path = new();
            path.AddCircle(canvasInfo.Center.X, canvasInfo.Center.Y, radius * 0.86f);
            
            canvas.RotateDegrees(70, canvasInfo.Center.X, canvasInfo.Center.Y);
            canvas.DrawTextOnPath(tempoChangeEvent.Tempo.ToString("0.000"), path, 0, 0, SKTextAlign.Center, NotePaints.GetStandardFont(40 * pixelScale), NotePaints.GetTextPaint(NotePaints.GetEventColor(@event).WithAlpha((byte)(255 * opacity))));
        }
        else if (@event is MetreChangeEvent metreChangeEvent)
        {
            SKPath path = new();
            path.AddCircle(canvasInfo.Center.X, canvasInfo.Center.Y, radius * 0.86f);
            
            canvas.RotateDegrees(90, canvasInfo.Center.X, canvasInfo.Center.Y);
            canvas.DrawTextOnPath($"{metreChangeEvent.Upper} / {metreChangeEvent.Lower}", path, 0, 0, SKTextAlign.Center, NotePaints.GetStandardFont(40 * pixelScale), NotePaints.GetTextPaint(NotePaints.GetEventColor(@event).WithAlpha((byte)(255 * opacity))));
        }
        else if (@event is SpeedChangeEvent speedChangeEvent)
        {
            SKPath path = new();
            path.AddCircle(canvasInfo.Center.X, canvasInfo.Center.Y, radius * 0.86f);
            
            canvas.RotateDegrees(110, canvasInfo.Center.X, canvasInfo.Center.Y);
            canvas.DrawTextOnPath($"{speedChangeEvent.Speed:0.000}x", path, 0, 0, SKTextAlign.Center, NotePaints.GetStandardFont(40 * pixelScale), NotePaints.GetTextPaint(NotePaints.GetEventColor(@event).WithAlpha((byte)(255 * opacity))));
        }
        else if (@event is VisibilityChangeEvent visibilityChangeEvent)
        {
            SKPath path = new();
            path.AddCircle(canvasInfo.Center.X, canvasInfo.Center.Y, radius * 0.86f);

            canvas.RotateDegrees(130, canvasInfo.Center.X, canvasInfo.Center.Y);
            canvas.DrawTextOnPath(visibilityChangeEvent.Visible ? "VISIBLE" : "HIDDEN", path, 0, 0, SKTextAlign.Center, NotePaints.GetStandardFont(40 * pixelScale), NotePaints.GetTextPaint(NotePaints.GetEventColor(@event).WithAlpha((byte)(255 * opacity))));
        }
        
        canvas.Restore();
    }

    /// <summary>
    /// Draws an event area.
    /// </summary>
    private static void DrawEventArea(SKCanvas canvas, CanvasInfo canvasInfo, Event @event, float time, float viewDistance, float opacity)
    {
        if (opacity == 0) return;
        if (@event is StopEffectEvent stopEffectEvent && stopEffectEvent.SubEvents.Length == 2)
        {
            SKPoint[] vertices = new SKPoint[360];

            RenderUtils.GetProgress(stopEffectEvent.SubEvents[0], false, viewDistance, time, 0, out float r0);
            RenderUtils.GetProgress(stopEffectEvent.SubEvents[1], false, viewDistance, time, 0, out float r1);

            r0 = RenderUtils.Perspective(r0);
            r1 = RenderUtils.Perspective(r1);

            r0 = Math.Clamp(r0, 0, 1);
            r1 = Math.Clamp(r1, 0, 1);
            
            r0 *= canvasInfo.JudgementLineRadius;
            r1 *= canvasInfo.JudgementLineRadius;
            
            for (int i = 0; i < 60; i++)
            {
                float angle = i * -6;
                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, r0, angle);
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, r0, angle - 6);
                SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, r1, angle);
                SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, r1, angle - 6);

                vertices[i * 6]     = p0;
                vertices[i * 6 + 1] = p1;
                vertices[i * 6 + 2] = p2;
                vertices[i * 6 + 3] = p3;
                vertices[i * 6 + 4] = p2;
                vertices[i * 6 + 5] = p1;
            }
            
            canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, NotePaints.GetEventAreaPaint(@event, opacity));
            
            return;
        }
        
        if (@event is ReverseEffectEvent reverseEffectEvent && reverseEffectEvent.SubEvents.Length == 3)
        {
            SKPoint[] vertices0 = new SKPoint[360];
            SKPoint[] vertices1 = new SKPoint[360];

            RenderUtils.GetProgress(reverseEffectEvent.SubEvents[0], false, viewDistance, time, 0, out float r0);
            RenderUtils.GetProgress(reverseEffectEvent.SubEvents[1], false, viewDistance, time, 0, out float r1);
            RenderUtils.GetProgress(reverseEffectEvent.SubEvents[2], false, viewDistance, time, 0, out float r2);

            r0 = RenderUtils.Perspective(r0);
            r1 = RenderUtils.Perspective(r1);
            r2 = RenderUtils.Perspective(r2);

            r0 = Math.Clamp(r0, 0, 1);
            r1 = Math.Clamp(r1, 0, 1);
            r2 = Math.Clamp(r2, 0, 1);
            
            r0 *= canvasInfo.JudgementLineRadius;
            r1 *= canvasInfo.JudgementLineRadius;
            r2 *= canvasInfo.JudgementLineRadius;
            
            for (int i = 0; i < 60; i++)
            {
                float angle = i * -6;
                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, r0, angle);
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, r0, angle - 6);
                SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, r1, angle);
                SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, r1, angle - 6);
                SKPoint p4 = RenderUtils.PointOnArc(canvasInfo.Center, r2, angle);
                SKPoint p5 = RenderUtils.PointOnArc(canvasInfo.Center, r2, angle - 6);

                vertices0[i * 6]     = p0;
                vertices0[i * 6 + 1] = p1;
                vertices0[i * 6 + 2] = p2;
                vertices0[i * 6 + 3] = p3;
                vertices0[i * 6 + 4] = p2;
                vertices0[i * 6 + 5] = p1;
                
                vertices1[i * 6]     = p2;
                vertices1[i * 6 + 1] = p3;
                vertices1[i * 6 + 2] = p4;
                vertices1[i * 6 + 3] = p5;
                vertices1[i * 6 + 4] = p4;
                vertices1[i * 6 + 5] = p3;
            }

            canvas.DrawVertices(SKVertexMode.Triangles, vertices0, null, NotePaints.GetEventAreaPaint(@event, opacity));
            canvas.DrawVertices(SKVertexMode.Triangles, vertices1, null, NotePaints.GetEventAreaPaint(@event, opacity * 0.5f));
        }
    }
    
    /// <summary>
    /// Draws a lane toggle note.
    /// </summary>
    private static void DrawLaneToggle(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, ILaneToggle laneToggle, float time, float viewDistance, float perspectiveScale, float opacity, HashSet<ITimeable>? selectedObjects = null, HashSet<ITimeable>? pointerOverObjects = null)
    {
        if (opacity == 0) return;
        if (laneToggle is not ITimeable timeable) return;
        if (laneToggle is not IPositionable positionable) return;
        bool state = laneToggle is LaneShowNote;

        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        
        // Sweep Visualization
        if (settings.VisualizeLaneSweeps)
        {
            SKPoint[] vertices = new SKPoint[positionable.Size * 6];
            float startTime = timeable.Timestamp.Time;
            
            if (laneToggle.Direction is LaneSweepDirection.Center)
            {
                int center = (int)Math.Ceiling(positionable.Size / 2.0f);
                float duration = center * 8.3333333f;

                bool even = positionable.Size % 2 == 0;
                
                for (int i = 0; i < positionable.Size; i++)
                {
                    float angle = (positionable.Position + i) * -6;

                    float t = even
                        ? i < center ? startTime + duration - i * 8.3333333f : startTime - duration + (i + 1) * 8.3333333f
                        : i < center ? startTime + duration - i * 8.3333333f : startTime - duration + (i + 2) * 8.3333333f;
                    
                    float stepScale = RenderUtils.InverseLerp(time + viewDistance, time, t);
                    stepScale = RenderUtils.Perspective(stepScale);
                    
                    float stepRadius = canvasInfo.JudgementLineRadius * stepScale;
                    stepRadius = Math.Max(0, stepRadius);
                    
                    SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius,    angle    );
                    SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, radius,    angle - 6);
                    SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, stepRadius, angle    );
                    SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, stepRadius, angle - 6);

                    vertices[6 * i]     = p0;
                    vertices[6 * i + 1] = p1;
                    vertices[6 * i + 2] = p2;
                    vertices[6 * i + 3] = p3;
                    vertices[6 * i + 4] = p2;
                    vertices[6 * i + 5] = p1;
                }
            }
            else if (laneToggle.Direction is LaneSweepDirection.Clockwise)
            {
                float duration = positionable.Size * 8.3333333f;
                
                for (int i = 0; i < positionable.Size; i++)
                {
                    float angle = (positionable.Position + i) * -6;
                    float t = startTime + duration - i * 8.3333333f;

                    float stepScale = RenderUtils.InverseLerp(time + viewDistance, time, t);
                    stepScale = RenderUtils.Perspective(stepScale);
                    
                    float stepRadius = canvasInfo.JudgementLineRadius * stepScale;
                    stepRadius = Math.Max(0, stepRadius);
                    
                    SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius,    angle    );
                    SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, radius,    angle - 6);
                    SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, stepRadius, angle    );
                    SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, stepRadius, angle - 6);

                    vertices[6 * i]     = p0;
                    vertices[6 * i + 1] = p1;
                    vertices[6 * i + 2] = p2;
                    vertices[6 * i + 3] = p3;
                    vertices[6 * i + 4] = p2;
                    vertices[6 * i + 5] = p1;
                }
            }
            else if (laneToggle.Direction is LaneSweepDirection.Counterclockwise)
            {
                for (int i = 0; i < positionable.Size; i++)
                {
                    float angle = (positionable.Position + i) * -6;
                    float t = startTime + (i + 1) * 8.3333333f;
                    
                    float stepScale = RenderUtils.InverseLerp(time + viewDistance, time, t);
                    stepScale = RenderUtils.Perspective(stepScale);
                    
                    float stepRadius = canvasInfo.JudgementLineRadius * stepScale;
                    stepRadius = Math.Max(0, stepRadius);
                    
                    SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radius,    angle    );
                    SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, radius,    angle - 6);
                    SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, stepRadius, angle    );
                    SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, stepRadius, angle - 6);

                    vertices[6 * i]     = p0;
                    vertices[6 * i + 1] = p1;
                    vertices[6 * i + 2] = p2;
                    vertices[6 * i + 3] = p3;
                    vertices[6 * i + 4] = p2;
                    vertices[6 * i + 5] = p1;
                }
            }
            
            canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, NotePaints.GetLaneToggleFillPaint(state, opacity));
        }
        
        // Note body and selection outline.
        if (perspectiveScale is > 0 and <= 1.01f)
        {
            float pixelScale = canvasInfo.Scale * perspectiveScale;
            
            if (positionable.Size == 60)
            {
                canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetLaneTogglePaint(state, pixelScale, opacity));
            }
            else
            {
                SKRect rect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);
                
                float start = positionable.Position * -6; 
                float sweep = positionable.Size * -6;

                canvas.DrawArc(rect, start, sweep, false, NotePaints.GetLaneTogglePaint(state, pixelScale, opacity));
            }
            
            // Selection outline.
            bool selected = selectedObjects != null && selectedObjects.Contains(timeable);
            bool pointerOver = pointerOverObjects != null && pointerOverObjects.Contains(timeable);
            if (selected || pointerOver)
            {
                DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, positionable.Position, positionable.Size, selected, pointerOver);
            }
        }
    }

    /// <summary>
    /// Draws a selection outline.
    /// </summary>
    private static void DrawSelectionOutline(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, float radius, float pixelScale, int position, int size, bool selected, bool pointerOver)
    {
        float radius0 = radius * SyncOutlineRadius[(int)settings.NoteThickness][4] * 0.99f;
        float radius1 = radius * SyncOutlineRadius[(int)settings.NoteThickness][5] * 1.01f;
        SKPath path = new();
        
        if (size == 60)
        {
            path.AddCircle(canvasInfo.Center.X, canvasInfo.Center.Y, radius0);
            path.AddCircle(canvasInfo.Center.X, canvasInfo.Center.Y, radius1, SKPathDirection.CounterClockwise);
        }
        else
        {
            float startAngle = (position + 0.25f) * -6;
            float sweepAngle = (size - 0.5f) * -6;
            float endAngle = startAngle + sweepAngle;
            
            float capRadius = 12 * pixelScale;
        
            SKRect longArcRect0 = new(canvasInfo.Center.X - radius0, canvasInfo.Center.Y - radius0, canvasInfo.Center.X + radius0, canvasInfo.Center.Y + radius0);
            SKRect longArcRect1 = new(canvasInfo.Center.X - radius1, canvasInfo.Center.Y - radius1, canvasInfo.Center.X + radius1, canvasInfo.Center.Y + radius1);
        
            SKPoint capPoint0 = RenderUtils.PointOnArc(canvasInfo.Center, radius0 + capRadius, startAngle);
            SKPoint capPoint1 = RenderUtils.PointOnArc(canvasInfo.Center, radius1 - capRadius, startAngle);
            SKPoint capPoint2 = RenderUtils.PointOnArc(canvasInfo.Center, radius0 + capRadius, endAngle);
            SKPoint capPoint3 = RenderUtils.PointOnArc(canvasInfo.Center, radius1 - capRadius, endAngle);
            SKRect capRect0 = new(capPoint0.X - capRadius, capPoint0.Y - capRadius, capPoint0.X + capRadius, capPoint0.Y + capRadius);
            SKRect capRect1 = new(capPoint1.X - capRadius, capPoint1.Y - capRadius, capPoint1.X + capRadius, capPoint1.Y + capRadius);
            SKRect capRect2 = new(capPoint2.X - capRadius, capPoint2.Y - capRadius, capPoint2.X + capRadius, capPoint2.Y + capRadius);
            SKRect capRect3 = new(capPoint3.X - capRadius, capPoint3.Y - capRadius, capPoint3.X + capRadius, capPoint3.Y + capRadius);
            
            path.ArcTo(capRect0, startAngle + 90, 90, true);
            path.ArcTo(longArcRect0, startAngle, sweepAngle, false);
            path.ArcTo(capRect2, endAngle - 180, 90, false);
            path.ArcTo(capRect3, endAngle - 90, 90, false);
            path.ArcTo(longArcRect1, endAngle, -sweepAngle, false);
            path.ArcTo(capRect1, startAngle, 90, false);
            path.Close();
        }
        
        canvas.DrawPath(path, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
        canvas.DrawPath(path, NotePaints.GetObjectOutlineStrokePaint(selected, pointerOver));
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

    /// <summary>
    /// Draws a bonus note sweep.
    /// </summary>
    private static void DrawBonusSweepEffect(SKCanvas canvas, CanvasInfo canvasInfo, int startPosition, float startTime, float duration, bool isCounterclockwise, float time)
    {
        if (time < startTime) return;
        if (time > startTime + duration) return;
        
        SKRect rect = new(0, 0, canvasInfo.Width, canvasInfo.Height);

        float delta = time - startTime;
        float step = MathF.Ceiling(delta * 0.06f);

        float offset;
        float start;
        float sweep;
        float clamp;

        if (isCounterclockwise)
        {
            offset = -step - startPosition;
            
            start = 0;
            sweep = MathF.Min(15, step);
            clamp = MathF.Ceiling(MathF.Min(0, -step + (duration - 250) * 0.06f));
            
            start -= clamp;
            sweep += clamp;
        }
        else
        {
            offset = step - startPosition - 16;

            start = 15;
            sweep = -MathF.Min(15, step);
            clamp = MathF.Ceiling(MathF.Min(0, -step + (duration - 250) * 0.06f));

            start += clamp;
            sweep += clamp;
        }
        
        canvas.Save();
        canvas.RotateDegrees(offset * 6, canvasInfo.Center.X, canvasInfo.Center.Y);
        canvas.DrawArc(rect, start * 6, sweep * 6, true, NotePaints.GetBonusSweepEffectPaint(canvasInfo, isCounterclockwise));
        canvas.Restore();
    }

    /// <summary>
    /// Draws an r-note effect.
    /// </summary>
    private static void DrawRNoteEffect(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, float startTime, float time)
    {
        if (time < startTime) return;
        if (time > startTime + 550) return;
        
        const int squares = 21;
        float squareWidth = canvasInfo.Width / squares;
        float squareRadius = 10 * canvasInfo.Scale;

        float t = RenderUtils.InverseLerp(startTime, startTime + 550, time);
        t = 1 - MathF.Pow(1 - Math.Clamp(t, 0, 1), 3f);
        
        float scaleMultiplier = Math.Clamp(1 - MathF.Pow((2 * t - 1), 2), 0, 1);
        float innerWave = Math.Clamp(0.5f * t + 0.2f, 0.3f, 0.8f);
        float outerWave = 0.3f * t + 0.7f;

        float angle = 180 + t * 180;

        float opacity = settings.RNoteEffectOpacity * 0.1f;

        canvas.Save();
        canvas.RotateDegrees(-12, canvasInfo.Center.X, canvasInfo.Center.Y);

        canvas.DrawPaint(NotePaints.GetRNoteGlowPaint(canvasInfo, settings, angle, innerWave, outerWave, scaleMultiplier * opacity));
        
        for (int x = 0; x < squares; x++)
        for (int y = 0; y < squares; y++)
        {
            float tX = 2 * ((x + 0.5f) / squares) - 1;
            float tY = 2 * ((y + 0.5f) / squares) - 1;
            float tLength = MathF.Sqrt(tX * tX + tY * tY);

            float scale = tLength < innerWave || tLength > outerWave
                ? 0
                : MathF.Sin((MathF.PI * (tLength - innerWave)) / (outerWave - innerWave));

            scale *= scaleMultiplier;
            
            float width = squareWidth * scale;
            float radius = squareRadius * scale;

            float xOffset = x * squareWidth + (squareWidth - width) * 0.5f;
            float yOffset = y * squareWidth + (squareWidth - width) * 0.5f;
            
            canvas.DrawRoundRect(xOffset, yOffset, width, width, radius, radius, NotePaints.GetRNoteFillPaint(canvasInfo, settings, angle, opacity));
        }
        
        canvas.Restore();
    }

    /// <summary>
    /// Draws a background.
    /// </summary>
    private static void DrawBackground(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Chart chart, Entry entry, float time)
    {
        SKPoint screenA = new(0, 0);
        SKPoint screenB = new(canvasInfo.Width, 0);
        SKPoint screenC = new(0, canvasInfo.Height);
        SKPoint screenD = new(canvasInfo.Width, canvasInfo.Height);
        
        SKPoint textureA = new(0, 0);
        SKPoint textureB = new(1080, 0);
        SKPoint textureC = new(0, 1080);
        SKPoint textureD = new(1080, 1080);
        
        SKPoint[] vertexCoords = [screenA, screenB, screenC, screenD, screenC, screenB];
        SKPoint[] textureCoords = [textureA, textureB, textureC, textureD, textureC, textureB];

        // TODO: Re-do with proper clear logic eventually?
        // This is an approximation.
        bool clear = settings.ClearBackgroundVisibility == RenderSettings.ClearBackgroundVisibilityOption.ForceClear;
        if (settings.ClearBackgroundVisibility == RenderSettings.ClearBackgroundVisibilityOption.SimulateClear)
        {
            int noteCount = 0;
            int normalHitCount = 0;
            int bonusHitCount = 0;

            foreach (Layer layer in chart.Layers)
            foreach (Note note in layer.Notes)
            {
                if (note is not IPlayable playable) continue;
                if (playable.JudgementType == JudgementType.Fake) continue;

                noteCount++;

                if (note.Timestamp.Time > time) continue;
                
                normalHitCount++;
                
                if (playable.BonusType is BonusType.Bonus) bonusHitCount++;
            }

            float progress = noteCount == 0 ? 0 : (float)(normalHitCount + bonusHitCount + bonusHitCount) / noteCount;
            clear = progress > entry.ClearThreshold;    
        }
        
        canvas.DrawVertices(SKVertexMode.Triangles, vertexCoords, textureCoords, null, NotePaints.GetBackgroundPaint(entry, clear));

        if (settings.BackgroundDim != RenderSettings.BackgroundDimOption.NoDim)
        {
            SKColor dimColor = settings.BackgroundDim switch
            {
                RenderSettings.BackgroundDimOption.Plus1 => new(0x55000000),
                RenderSettings.BackgroundDimOption.Plus2 => new(0x9D000000),
                RenderSettings.BackgroundDimOption.Plus3 => new(0xB6000000),
                RenderSettings.BackgroundDimOption.Plus4 => new(0xDD000000),
                _ => new(0x00000000),
            };
            canvas.DrawColor(dimColor, SKBlendMode.SrcOver);
        }
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

file struct RenderBonusSweepEffect(int startPosition, float startTime, float duration, bool isCounterclockwise)
{
    public readonly int StartPosition = startPosition;
    public readonly float StartTime = startTime;
    public readonly float Duration = duration;
    public readonly bool IsCounterclockwise = isCounterclockwise;
}