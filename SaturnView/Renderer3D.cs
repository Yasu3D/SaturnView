using SaturnData.Notation.Core;
using SaturnData.Notation.Events;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SaturnData.Utilities;
using SkiaSharp;

namespace SaturnView;

public static class Renderer3D
{
    private static readonly float[][] SyncOutlineMultiplier = 
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
    public static void Render(SKCanvas canvas,
        CanvasInfo canvasInfo,
        RenderSettings settings,
        Chart chart,
        Entry entry,
        float time,
        bool playing,
        HashSet<ITimeable>? selectedObjects = null,
        ITimeable? pointerOverObject = null,
        ITimeable? activeObjectGroup = null,
        RenderBoxSelectData? boxSelect = null,
        Note? cursorNote = null,
        SKPaint? jacketBackgroundPaint = null,
        int jacketBackgroundWidth = 0,
        int jacketBackgroundHeight = 0)
    {
        float viewDistance = GetViewDistance(settings.NoteSpeed);

        bool[] lanesToDraw = new bool[60];
        List<RenderObject> objectsToDraw = [];
        List<RenderObject> holdEndsToDraw = [];
        List<RenderObject> holdsToDraw = [];
        List<RenderObject> eventAreasToDraw = [];
        List<RenderBonusSweepEffect> bonusSweepEffectsToDraw = [];
        List<RenderJudgeArea> timingWindowsToDraw = [];
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
        
        DrawBackground(canvas, canvasInfo, settings, chart, entry, time, jacketBackgroundPaint, jacketBackgroundWidth, jacketBackgroundHeight);
        
        renderRNoteEffects();
        renderLanes();
        renderEventAreas();
        renderHoldEnds();
        renderHoldSurfaces();
        renderJudgeAreas();
        renderObjects();

        if (boxSelect != null)
        {
            DrawBoxSelect(canvas, canvasInfo, time, viewDistance, boxSelect.Value);
        }
        
        renderBonusEffects();

        DrawInterface(canvas, canvasInfo, settings, entry, time, playing);
        
        return;
        
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
                float duration = ILaneToggle.SweepDuration(laneToggle.Direction, positionable.Size);
                
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

            bool checkForJudgeAreas = settings.ShowJudgeAreas &&
                                         (
                                                settings.ShowGoodArea
                                             || settings.ShowGreatArea
                                             || settings.ShowMarvelousArea
                                         );
            
            // Find all visible global events.
            foreach (Event @event in chart.Events)
            {
                if (settings.HideEventMarkersDuringPlayback && playing) break;
                
                if (!RenderUtils.GetProgress(@event.Timestamp.Time, @event.Timestamp.ScaledTime, false, viewDistance, time, scaledTime, out float progress)) continue;
                objectsToDraw.Add(new(@event, chart.Layers[0], 0, progress, false, RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
            }

            // Find all visible bookmarks.
            foreach (Bookmark bookmark in chart.Bookmarks)
            {
                if (settings.HideBookmarksDuringPlayback && playing) break;
                
                if (!RenderUtils.GetProgress(bookmark.Timestamp.Time, bookmark.Timestamp.ScaledTime, false, viewDistance, time, scaledTime, out float progress)) continue;
                objectsToDraw.Add(new(bookmark, chart.Layers[0], 0, progress, false, RenderUtils.IsVisible(bookmark, settings, activeObjectGroup)));
            }
            
            // Find all visible lane toggles.
            foreach (Note note in chart.LaneToggles)
            {
                if (settings.HideLaneToggleNotesDuringPlayback && playing) break;
                if (note is not ILaneToggle laneToggle) continue;
                if (note is not IPositionable positionable) continue;
                
                float tStart = 1 - (note.Timestamp.Time - time) / viewDistance;
                float tEnd = 1 - (note.Timestamp.Time + ILaneToggle.SweepDuration(laneToggle.Direction, positionable.Size) - time) / viewDistance;

                if (tStart <= 0 && tEnd <= 0) continue;
                if (tStart > 1.01f && tEnd > 1.01f) continue;
                
                objectsToDraw.Add(new(note, null, 0, tStart, false, RenderUtils.IsVisible(note, settings, activeObjectGroup)));
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
                        Timestamp start = stopEffectEvent.SubEvents[0].Timestamp;
                        Timestamp end = stopEffectEvent.SubEvents[1].Timestamp;
                        
                        // Start Marker
                        if (RenderUtils.GetProgress(start.Time, start.ScaledTime, false, viewDistance, time, scaledTime, out float t0))
                        {
                            objectsToDraw.Add(new(stopEffectEvent.SubEvents[0], layer, l, t0, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                        
                        // End Marker
                        if (RenderUtils.GetProgress(end.Time, end.ScaledTime, false, viewDistance, time, scaledTime, out float t1))
                        {
                            objectsToDraw.Add(new(stopEffectEvent.SubEvents[1], layer, l, t1, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }

                        // Area Fill
                        if (stopEffectEvent.SubEvents[0].Timestamp.Time <= time + viewDistance && stopEffectEvent.SubEvents[1].Timestamp.Time >= time)
                        {
                            eventAreasToDraw.Add(new(stopEffectEvent, layer, l, 0, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                    }
                    else if (@event is ReverseEffectEvent reverseEffectEvent && reverseEffectEvent.SubEvents.Length == 3)
                    {
                        Timestamp start = reverseEffectEvent.SubEvents[0].Timestamp;
                        Timestamp middle = reverseEffectEvent.SubEvents[1].Timestamp;
                        Timestamp end = reverseEffectEvent.SubEvents[2].Timestamp;
                        
                        // Start Marker
                        if (RenderUtils.GetProgress(start.Time, start.ScaledTime, false, viewDistance, time, scaledTime, out float t0))
                        {
                            objectsToDraw.Add(new(reverseEffectEvent.SubEvents[0], layer, l, t0, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                        
                        // Middle Marker
                        if (RenderUtils.GetProgress(middle.Time, middle.ScaledTime, false, viewDistance, time, scaledTime, out float t1))
                        {
                            objectsToDraw.Add(new(reverseEffectEvent.SubEvents[1], layer, l, t1, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                        
                        // End Marker
                        if (RenderUtils.GetProgress(end.Time, end.ScaledTime, false, viewDistance, time, scaledTime, out float t2))
                        {
                            objectsToDraw.Add(new(reverseEffectEvent.SubEvents[2], layer, l, t2, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                        
                        // Area Fill
                        if (start.Time <= time + viewDistance && end.Time >= time)
                        {
                            eventAreasToDraw.Add(new(reverseEffectEvent, layer, l, 0, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                    }
                    else
                    {
                        if (!RenderUtils.GetProgress(@event.Timestamp.Time, @event.Timestamp.ScaledTime, false, viewDistance, time, scaledTime, out float t)) continue;
                        objectsToDraw.Add(new(@event, layer, l, t, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                    }
                }
                
                VisibilityChangeEvent? lastVisibilityChange = layer.LastVisibilityChange(time);
                if (settings.ShowVisibilityChanges && lastVisibilityChange != null && !lastVisibilityChange.Visibility) continue;
                
                ReverseEffectEvent? lastReverseEffect = layer.LastReverseEffect(time);
                bool reverseActive = settings.ShowSpeedChanges && lastReverseEffect != null && lastReverseEffect.IsActive(time);
                
                // Find all visible notes.
                for (int n = 0; n < layer.Notes.Count; n++)
                {
                    Note note = layer.Notes[n];
                    
                    // Non-reversed notes are hidden during a reverse.
                    if (reverseActive && !lastReverseEffect!.ContainedNotes.Contains(note)) continue;

                    if (note is IPlayable playable)
                    {
                        // Bonus Spin FX
                        if (checkForBonusNotes && note is SlideClockwiseNote or SlideCounterclockwiseNote && playable.BonusType == BonusType.Bonus)
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
                        if (checkForRNotes && playable.BonusType == BonusType.R && playable.JudgementType != JudgementType.Fake)
                        {
                            if (note.Timestamp.Time <= time && note.Timestamp.Time + 550f > time)
                            {
                                activeRNote = note;
                            }
                        }
                        
                        // Timing windows
                        if (checkForJudgeAreas && playable.JudgementType != JudgementType.Fake && note is IPositionable positionable2)
                        {
                            if (timingWindowVisible())
                            {
                                // Long names for everything........
                                RenderUtils.GetProgress(playable.JudgeArea.MarvelousEarly,        playable.JudgeArea.ScaledMarvelousEarly,        settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float marvEarly);
                                RenderUtils.GetProgress(playable.JudgeArea.MarvelousLate,         playable.JudgeArea.ScaledMarvelousLate,         settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float marvLate);
                                RenderUtils.GetProgress(playable.JudgeArea.GreatEarly,            playable.JudgeArea.ScaledGreatEarly,            settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float greatEarly);
                                RenderUtils.GetProgress(playable.JudgeArea.GreatLate,             playable.JudgeArea.ScaledGreatLate,             settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float greatLate);
                                RenderUtils.GetProgress(playable.JudgeArea.GoodEarly,             playable.JudgeArea.ScaledGoodEarly,             settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float goodEarly);
                                RenderUtils.GetProgress(playable.JudgeArea.GoodLate,              playable.JudgeArea.ScaledGoodLate,              settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float goodLate);
                                RenderUtils.GetProgress(note.Timestamp.Time,                         note.Timestamp.ScaledTime,                         settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float noteScale);
                                
                                marvEarly     = Math.Max(0, marvEarly);
                                marvLate      = Math.Max(0, marvLate);
                                greatEarly    = Math.Max(0, greatEarly);
                                greatLate     = Math.Max(0, greatLate);
                                goodEarly     = Math.Max(0, goodEarly);
                                goodLate      = Math.Max(0, goodLate);
                                noteScale     = Math.Max(0, noteScale);

                                timingWindowsToDraw.Add(new(positionable2.Position, positionable2.Size, noteScale, marvEarly, marvLate, greatEarly, greatLate, goodEarly, goodLate));
                            }

                            bool timingWindowVisible()
                            {
                                if (settings.ShowSpeedChanges)
                                {
                                    if (playable.JudgeArea.MaxLate < time) return false;
                                    if (playable.JudgeArea.ScaledMaxLate < scaledTime) return false;
                                    if (playable.JudgeArea.ScaledMaxEarly > scaledTime + viewDistance) return false;
                                }
                                else
                                {
                                    if (playable.JudgeArea.MaxLate < time) return false;
                                    if (playable.JudgeArea.MaxEarly > time + viewDistance) return false;
                                }

                                return true;
                            }
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
                        
                        bool isVisible = layer.Visible && RenderUtils.IsVisible(holdNote, settings, activeObjectGroup);
                        
                        holdsToDraw.Add(new(holdNote, layer, l, 0, false, isVisible));
                        
                        // Hold Start
                        Timestamp start = holdNote.Points[0].Timestamp;
                        if (RenderUtils.GetProgress(start.Time, start.ScaledTime, settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float tStart))
                        {
                            Note? prev = n > 0                     ? layer.Notes[n - 1] : null;
                            Note? next = n < layer.Notes.Count - 1 ? layer.Notes[n + 1] : null;
                            bool sync = note.IsSync(prev) || note.IsSync(next);

                            objectsToDraw.Add(new(note, layer, l, tStart, sync, isVisible));
                        }

                        // Hold End
                        Timestamp end = holdNote.Points[^1].Timestamp;
                        if (holdNote.Points.Count > 1 && RenderUtils.GetProgress(end.Time, end.ScaledTime, settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float tEnd))
                        {
                            holdEndsToDraw.Add(new(holdNote.Points[^1], layer, l, tEnd, false, isVisible));
                        }

                        // Hold Points
                        if (activeObjectGroup == holdNote && holdNote.Points.Count > 2)
                        {
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
                    }
                    else
                    {
                        // Normal Notes
                        if (!RenderUtils.GetProgress(note.Timestamp.Time, note.Timestamp.ScaledTime, settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float t)) continue;

                        Note? prev = n > 0                     ? layer.Notes[n - 1] : null;
                        Note? next = n < layer.Notes.Count - 1 ? layer.Notes[n + 1] : null;
                        bool sync = note.IsSync(prev) || note.IsSync(next);

                        objectsToDraw.Add(new(note, layer, l, t, sync, layer.Visible && RenderUtils.IsVisible(note, settings, activeObjectGroup)));
                    }
                }

                // Find all visible generated notes.
                foreach (Note note in layer.GeneratedNotes)
                {
                    if (reverseActive && !lastReverseEffect!.ContainedNotes.Contains(note)) continue;
                    if (!RenderUtils.GetProgress(note.Timestamp.Time, note.Timestamp.ScaledTime, settings.ShowSpeedChanges, viewDistance, time, scaledTime, out float t)) continue;

                    objectsToDraw.Add(new(note, layer, l, t, false, layer.Visible && RenderUtils.IsVisible(note, settings, activeObjectGroup)));
                }
            }
            
            objectsToDraw = objectsToDraw
                .OrderBy(x => x.IsVisible)
                .ThenBy(x => x.LayerIndex)
                .ThenByDescending(x => x.Object is ILaneToggle)
                .ThenBy(x => x.Scale)
                .ThenByDescending(x => x.Object is SyncNote or MeasureLineNote)
                .ThenByDescending(x => x.Object is Event or Bookmark)
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
                bool selected = selectedObjects != null && selectedObjects.Contains(renderObject.Object);
                bool pointerOver = pointerOverObject != null && pointerOverObject == renderObject.Object;
                
                DrawEventArea(canvas, canvasInfo, @event, time, viewDistance, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f, selected, pointerOver);
            }
        }

        void renderHoldEnds()
        {
            foreach (RenderObject renderObject in holdEndsToDraw)
            {
                if (renderObject.Object is not HoldPointNote holdPointNote) continue;
                
                bool selected = selectedObjects != null && selectedObjects.Contains(holdPointNote);
                bool pointerOver = pointerOverObject != null && pointerOverObject == holdPointNote;
                
                DrawHoldEndNote(canvas, canvasInfo, settings, holdPointNote, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f, selected, pointerOver);
            }
        }
        
        void renderHoldSurfaces()
        {
            foreach (RenderObject renderObject in holdsToDraw)
            {
                if (renderObject.Object is not HoldNote holdNote) continue;
                if (renderObject.Layer == null) continue;
                
                bool selected = selectedObjects != null && selectedObjects.Contains(holdNote);
                bool pointerOver = pointerOverObject != null && pointerOverObject == holdNote;
                
                DrawHoldSurface(canvas, canvasInfo, settings, holdNote, renderObject.Layer, time, viewDistance, playing, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f, selected, pointerOver);
            }
        }
        
        void renderJudgeAreas()
        {
            foreach (RenderJudgeArea renderJudgeArea in timingWindowsToDraw)
            {
                DrawJudgeArea(canvas, canvasInfo, settings, renderJudgeArea, 1);
            }
        }
        
        void renderObjects()
        {
            foreach (RenderObject renderObject in objectsToDraw)
            {
                bool selected = selectedObjects != null && selectedObjects.Contains(renderObject.Object);
                bool pointerOver = pointerOverObject != null && pointerOverObject == renderObject.Object;
                float opacity = renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f;

                if (renderObject.Object is EffectSubEvent subEvent)
                {
                    selected = selected || (selectedObjects != null && selectedObjects.Contains(subEvent.Parent));
                    pointerOver = pointerOver || pointerOverObject == subEvent.Parent;
                }
                else if (renderObject.Object is HoldNote holdNote && holdNote.Points.Count != 0)
                {
                    selected = selected || (selectedObjects != null && selectedObjects.Contains(holdNote.Points[0]));
                    pointerOver = pointerOver || pointerOverObject == holdNote.Points[0];
                }
                
                render(renderObject, selected, pointerOver, opacity, false);
            }

            if (cursorNote != null)
            {
                render(new(cursorNote, null, null, 1, false, true), false, false, 0.5f, true);
            }

            return;

            void render(RenderObject renderObject, bool selected, bool pointerOver, float opacity, bool overwriteLaneToggleStartTime)
            {
                if (renderObject.Object is HoldPointNote holdPointNote)
                {
                    DrawHoldPointNote
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        note: holdPointNote,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );
                }
                else if (renderObject.Object is SyncNote syncNote)
                {
                    DrawSyncNote
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        note: syncNote,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );
                }
                else if (renderObject.Object is MeasureLineNote measureLineNote)
                {
                    if (!settings.ShowBeatLineNotes && measureLineNote.IsBeatLine) return;
                    
                    DrawMeasureLineNote
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        linearScale: renderObject.Scale,
                        isBeatLine: measureLineNote.IsBeatLine,
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );
                }
                else if (renderObject.Object is ILaneToggle laneToggle)
                {
                    float startTime = overwriteLaneToggleStartTime 
                        ? time 
                        : ((ITimeable)laneToggle).Timestamp.Time;
                    
                    DrawLaneToggle(
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        startTime: startTime,
                        time: time,
                        viewDistance: viewDistance,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        laneToggle: laneToggle,
                        selected: selected,
                        pointerOver: pointerOver);
                }
                else if (renderObject.Object is Note note)
                {
                    DrawNote
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        linearScale: renderObject.Scale,
                        sync: renderObject.Sync,
                        opacity: opacity,
                        note: note,
                        selected: selected,
                        pointerOver: pointerOver
                    );
                }
                else if (renderObject.Object is Event @event)
                {
                    DrawEvent
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        @event: @event,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );
                }
                else if (renderObject.Object is Bookmark bookmark)
                {
                    DrawBookmark
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        bookmark: bookmark,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );
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
    /// Returns <c>true</c> if the specified pixel coordinate lies on an object.
    /// </summary>
    /// <param name="obj">The object to hit test. (May also implement IPositionable.)</param>
    /// <param name="x">The x-coordinate of the pixel.</param>
    /// <param name="y">The y-coordinate of the pixel.</param>
    /// <param name="time">The current time.</param>
    /// <param name="scaledTime">The current scaled time.</param>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="settings">The current render settings.</param>
    public static IPositionable.OverlapResult HitTest(ITimeable obj, Layer? layer, float x, float y, float time, float scaledTime, CanvasInfo canvasInfo, bool showSpeedChanges, RenderSettings settings, ITimeable? activeObjectGroup)
    {
        float viewDistance = GetViewDistance(settings.NoteSpeed);
        float threshold = GetHitTestThreshold(canvasInfo, settings.NoteThickness);
        float radius = GetHitTestPointerRadius(canvasInfo, x, y);
        int lane = GetHitTestPointerLane(canvasInfo, x, y);
        
        return HitTest(obj, layer, radius, lane, time, scaledTime, viewDistance, threshold, showSpeedChanges, settings, activeObjectGroup);
    }
    
    /// <summary>
    /// Returns <c>true</c> if the specified radial coordinate lies on an object.
    /// </summary>
    /// <param name="obj">The object to hit test. (May also implement IPositionable.)</param>
    /// <param name="radius">The 0-1 radius of the radial coordinate.</param>
    /// <param name="lane">The 0-59 lane of the radial coordinate.</param>
    /// <param name="time">The current time.</param>
    /// <param name="scaledTime">The current scaled time.</param>
    /// <param name="viewDistance">The current view distance.</param>
    /// <param name="showSpeedChanges">Should speed changes be taken into account?</param>
    /// <param name="threshold">The radius threshold for hit testing.</param>
    /// <returns></returns>
    public static IPositionable.OverlapResult HitTest(ITimeable obj, Layer? layer, float radius, int lane, float time, float scaledTime, float viewDistance, float threshold, bool showSpeedChanges, RenderSettings settings, ITimeable? activeObjectGroup)
    {
        if (lane is > 59 or < 0) return IPositionable.OverlapResult.None;

        if (layer != null && !layer.Visible) return IPositionable.OverlapResult.None;
        if (!RenderUtils.IsVisible(obj, settings, activeObjectGroup)) return IPositionable.OverlapResult.None;

        // Hit test stop effect event
        if (obj is StopEffectEvent stopEffectEvent)
        {
            foreach (EffectSubEvent subEvent in stopEffectEvent.SubEvents)
            {
                IPositionable.OverlapResult result = hitTestObject(subEvent);
                if (result == IPositionable.OverlapResult.None) continue;

                return result;
            }
        }
        // Hit test reverse effect event
        else if (obj is ReverseEffectEvent reverseEffectEvent)
        {
            foreach (EffectSubEvent subEvent in reverseEffectEvent.SubEvents)
            {
                IPositionable.OverlapResult result = hitTestObject(subEvent);
                if (result == IPositionable.OverlapResult.None) continue;

                return result;
            }
        }
        // Hit test normal objects
        else
        {
            IPositionable.OverlapResult result = hitTestObject(obj);
            if (result != IPositionable.OverlapResult.None)
            {
                return result;
            }
        }
        
        // Hit test hold note surface separately if no hit test has been successful so far.
        if (obj is HoldNote holdNote && holdNote.Points.Count > 1)
        {
            if (holdNote.Points[^1].Timestamp.Time < time) return IPositionable.OverlapResult.None;

            if (showSpeedChanges)
            {
                if (holdNote.Points[^1].Timestamp.ScaledTime < scaledTime) return IPositionable.OverlapResult.None;
                if (holdNote.Points[0].Timestamp.ScaledTime > scaledTime + viewDistance) return IPositionable.OverlapResult.None;
            }
            else
            {
                if (holdNote.Points[0].Timestamp.Time > time + viewDistance) return IPositionable.OverlapResult.None;
            }

            for (int i = 1; i < holdNote.Points.Count; i++)
            {
                HoldPointNote startPoint = holdNote.Points[i - 1];
                HoldPointNote endPoint = holdNote.Points[i];
                
                RenderUtils.GetProgress(startPoint.Timestamp.Time, startPoint.Timestamp.ScaledTime, showSpeedChanges, viewDistance, time, scaledTime, out float startRadius);
                RenderUtils.GetProgress(endPoint.Timestamp.Time, endPoint.Timestamp.ScaledTime, showSpeedChanges, viewDistance, time, scaledTime, out float endRadius);
                
                float r = RenderUtils.InversePerspective(radius);

                if (startRadius < r) continue;
                if (endRadius > r) continue;
                
                float t = SaturnMath.InverseLerp(startRadius, endRadius, r);
                
                bool flip = SaturnMath.FlipHoldInterpolation(startPoint, endPoint);
                int position = (int)MathF.Round(SaturnMath.LerpCyclicManual(startPoint.Position, endPoint.Position, t, 60, flip));
                int size = (int)MathF.Round(SaturnMath.Lerp(startPoint.Size, endPoint.Size, t));
                
                IPositionable.OverlapResult result = IPositionable.HitTestResult(position, size, lane);
                if (result != IPositionable.OverlapResult.None) return result;
            }
        }
        
        return IPositionable.OverlapResult.None;

        IPositionable.OverlapResult hitTestObject(ITimeable t)
        {
            if (!RenderUtils.GetProgress(t.Timestamp.Time, t.Timestamp.ScaledTime, showSpeedChanges, viewDistance, time, scaledTime, out float progress)) return IPositionable.OverlapResult.None;
        
            progress = RenderUtils.Perspective(progress);
            if (Math.Abs(radius - progress) > threshold) return IPositionable.OverlapResult.None;
            
            if (t is IPositionable positionable)
            {
                return IPositionable.HitTestResult(positionable.Position, positionable.Size, lane);
            }
            
            return IPositionable.OverlapResult.Body;
        }
    }

    /// <summary>
    /// Returns the view distance in milliseconds.
    /// </summary>
    /// <param name="noteSpeed">The current note speed.</param>
    public static float GetViewDistance(int noteSpeed) => 3333.333f / (noteSpeed * 0.1f);

    /// <summary>
    /// Returns the hit test threshold, based on the current note thickness.
    /// </summary>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="noteThickness">The current note thickness.</param>
    /// <returns></returns>
    public static float GetHitTestThreshold(CanvasInfo canvasInfo, RenderSettings.NoteThicknessOption noteThickness)
    {
        return ((NotePaints.NoteStrokeWidths[(int)noteThickness] * canvasInfo.Scale3D) / canvasInfo.Radius) * 0.5f;
    }

    /// <summary>
    /// Returns the 0-1 radius of a pixel coordinate.
    /// </summary>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="x">The x-coordinate in pixels</param>
    /// <param name="y">The y-coordinate in pixels</param>
    public static float GetHitTestPointerRadius(CanvasInfo canvasInfo, float x, float y)
    {
        x =  (x - canvasInfo.Radius) / canvasInfo.JudgementLineRadius;
        y =  (y - canvasInfo.Radius) / canvasInfo.JudgementLineRadius;
        
        return MathF.Sqrt(x * x + y * y);
    }

    /// <summary>
    /// Returns the 0-59 lane position of a pixel coordinate.
    /// </summary>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="x">The x-coordinate in pixels</param>
    /// <param name="y">The y-coordinate in pixels</param>
    public static int GetHitTestPointerLane(CanvasInfo canvasInfo, float x, float y)
    {
        x =  (x - canvasInfo.Radius) / canvasInfo.JudgementLineRadius;
        y =  (y - canvasInfo.Radius) / canvasInfo.JudgementLineRadius;
        
        float angle = MathF.Atan2(y, x) / MathF.PI * 180 + 180;
        return (int)((90 - angle / 6) % 60);
    }
    
    /// <summary>
    /// Draws a standard note body, sync outline, r-effect, and arrows.
    /// </summary>
    private static void DrawNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Note note, float perspectiveScale, float linearScale, bool sync, float opacity, bool selected, bool pointerOver)
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
        float pixelScale = canvasInfo.Scale3D * perspectiveScale;
        
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
                float radius0 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][4];
                float radius1 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][5];

                SKPaint paint = NotePaints.GetSyncOutlineStrokePaint(pixelScale, opacity);
                canvas.DrawCircle(canvasInfo.Center, radius0, paint);
                canvas.DrawCircle(canvasInfo.Center, radius1, paint);
            }
            else
            {
                float radius0 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][0];
                float radius1 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][1];
                float radius2 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][2];
                float radius3 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][3];
                
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
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, positionable.Position, positionable.Size, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws a hold end note.
    /// </summary>
    private static void DrawHoldEndNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldPointNote note, float perspectiveScale, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (perspectiveScale is <= 0 or > 1.25f) return;

        int colorId = (int)settings.HoldNoteColor;
        
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = canvasInfo.Scale3D * perspectiveScale;

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
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, note.Position, note.Size, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws a hold control point.
    /// </summary>
    private static void DrawHoldPointNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldPointNote note, float perspectiveScale, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (perspectiveScale is <= 0 or > 1.25f) return;

        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = canvasInfo.Scale3D * perspectiveScale;
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
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, note.Position, note.Size, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws a hold note surface.
    /// </summary>
    private static void DrawHoldSurface(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldNote hold, Layer layer, float time, float viewDistance, bool playing, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;

        List<HoldPointNote> points = hold.Points.Where(x => x.RenderType is HoldPointRenderType.Visible).ToList();

        if (hold.Points[0].RenderType == HoldPointRenderType.Hidden)
        {
            points.Insert(0, hold.Points[0]);
        }
        
        if (hold.Points[^1].RenderType == HoldPointRenderType.Hidden)
        {
            points.Add(hold.Points[^1]);
        }
            
        if (points.Count < 2) return;

        List<SKPoint> vertexScreenCoords = [];
        List<SKPoint> vertexTextureCoords = [];
        
        float scaledTime = settings.ShowSpeedChanges ? Timestamp.ScaledTimeFromTime(layer, time) : time;
        int maxSize = hold.MaxSize;
        int arcs = 0;
        
        bool active = hold.Timestamp.Time < time && playing;
        
        // Generate parts (groups of arcs) for every hold point, except the last.
        for (int y = 0; y < points.Count - 1; y++)
        {
            RenderHoldPoint start = new(hold, points[y], maxSize);
            RenderHoldPoint end = new(hold, points[y + 1], maxSize);

            bool skipEnd = y < points.Count - 2;
            
            if (start.GlobalTime < time && end.GlobalTime > time)
            {
                // Judgement line is between start and end. Insert a third point on the judgement line.
                // Then generate from start to center, and center to end.
                float t = SaturnMath.InverseLerp(start.GlobalTime, end.GlobalTime, time);

                float startCenter = start.StartAngle + start.IntervalAngle * maxSize * 0.5f;
                float endCenter = end.StartAngle + end.IntervalAngle * maxSize * 0.5f;
                
                RenderHoldPoint center = new()
                {
                    GlobalTime = time, 
                    GlobalScaledTime = scaledTime, 
                    LocalTime = SaturnMath.Lerp(start.LocalTime, end.LocalTime, t),
                    IntervalAngle  = SaturnMath.Lerp(start.IntervalAngle,  end.IntervalAngle,  t),
                    StartAngle = SaturnMath.LerpCyclicManual(start.StartAngle, end.StartAngle, t, 360, SaturnMath.FlipHoldInterpolation(startCenter, endCenter)),
                };

                generatePart(start, center, false);
                generatePart(center, end, skipEnd);
            }
            else
            {
                // This segment does not cross the judgement line. No special handling is necessary.
                generatePart(start, end, skipEnd);
            }
        }
        
        // Build mesh
        SKPoint[] triangles = new SKPoint[maxSize * (arcs - 1) * 6];
        SKPoint[] textureCoords = new SKPoint[maxSize * (arcs -1) * 6];

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
        if (selected || pointerOver)
        {
            canvas.DrawVertices(SKVertexMode.Triangles, triangles, null, null, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
        }
        
        return;

        void generatePart(RenderHoldPoint start, RenderHoldPoint end, bool skipLast)
        {
            float startScale = getScale(start.GlobalTime, start.GlobalScaledTime);
            float endScale = getScale(end.GlobalTime, end.GlobalScaledTime);
            
            if (startScale > 1.25f && endScale > 1.25f) return;
            
            bool sameTime = start.GlobalTime == end.GlobalTime;
            bool sameShape = start.StartAngle == end.StartAngle && start.IntervalAngle == end.IntervalAngle;

            float interval;
            
            // No in-between steps on the same tick.
            if (sameTime)
            {
                interval = 1;
            }
            // 4 sub-points for basic perspective correction on straight holds.
            else if (sameShape)
            {
                interval = 0.25f;
            }
            // Smoothly interpolate every 20ms.
            else
            {
                interval = 20.0f / (end.GlobalTime - start.GlobalTime);
            }
            
            float startCenter = start.StartAngle + start.IntervalAngle * maxSize * 0.5f;
            float endCenter = end.StartAngle + end.IntervalAngle * maxSize * 0.5f;
            bool flip = SaturnMath.FlipHoldInterpolation(startCenter, endCenter);

            // For every imaginary "sub point" between start and end.
            for (float t = 0; t < 1; t += interval)
            {
                // Very brute-force optimization: Look ahead and behind by one step to see if the arc and its neighbors are entirely off-screen.
                if (skip(t, out float scale)) continue;
                
                float localTime     = SaturnMath.Lerp(start.LocalTime,     end.LocalTime,     t);
                float intervalAngle = SaturnMath.Lerp(start.IntervalAngle, end.IntervalAngle, t);
                float startAngle    = SaturnMath.LerpCyclicManual(start.StartAngle, end.StartAngle, t, 360, flip);

                generateArc(scale, localTime, startAngle, intervalAngle);
            }

            if (!skipLast && !skip(1, out float scale2))
            {
                generateArc(scale2, end.LocalTime, end.StartAngle, end.IntervalAngle);
            }

            return;

            bool skip(float t, out float scale)
            {
                float previousT = t - interval;
                float previousGlobalTime       = SaturnMath.Lerp(start.GlobalTime,       end.GlobalTime,       previousT);
                float previousGlobalScaledTime = SaturnMath.Lerp(start.GlobalScaledTime, end.GlobalScaledTime, previousT);
                float previousScale = getScale(previousGlobalTime, previousGlobalScaledTime);
                
                float nextT = t + interval;
                float nextGlobalTime       = SaturnMath.Lerp(start.GlobalTime,       end.GlobalTime,       nextT);
                float nextGlobalScaledTime = SaturnMath.Lerp(start.GlobalScaledTime, end.GlobalScaledTime, nextT);
                float nextScale = getScale(nextGlobalTime, nextGlobalScaledTime);
                
                float globalTime       = SaturnMath.Lerp(start.GlobalTime,       end.GlobalTime,       t);
                float globalScaledTime = SaturnMath.Lerp(start.GlobalScaledTime, end.GlobalScaledTime, t);
                scale = getScale(globalTime, globalScaledTime);

                if (scale < 0 && nextScale < 0 && nextT < 1 && previousScale < 0 && previousT >= 0)
                {
                    return true;
                }

                if (scale > 1.25f && nextScale > 1.25f && nextT < 1 && previousScale > 1.25f && previousT >= 0)
                {
                    return true;
                }

                return false;
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

                float texX = (512 * ((int)settings.HoldNoteColor + 0.5f) / 13.0f) + x * 0.01f;
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
                ? SaturnMath.InverseLerp(time + viewDistance, time, globalTime)
                : SaturnMath.InverseLerp(scaledTime + viewDistance, scaledTime, globalScaledTime);
        }
    }
    
    /// <summary>
    /// Draws a sync connector.
    /// </summary>
    private static void DrawSyncNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, SyncNote note, float perspectiveScale, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (perspectiveScale is <= 0 or > 1.25f) return;
        
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = perspectiveScale * canvasInfo.Scale3D;

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
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, note.Position, note.Size, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws a measure or beat line.
    /// </summary>
    private static void DrawMeasureLineNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, float perspectiveScale, float linearScale, bool isBeatLine, float opacity, bool selected, bool pointerOver)
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
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, canvasInfo.Scale3D * perspectiveScale, 0, 60, selected, pointerOver);
        }
    }

    /// <summary>
    /// Draws an event.
    /// </summary>
    private static void DrawEvent(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Event @event, float perspectiveScale, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = canvasInfo.Scale3D * perspectiveScale;

        if (opacity == 1 && @event is not EffectSubEvent)
        {
            canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetEventMarkerFillPaint(canvasInfo, @event, perspectiveScale));
        }
        canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetEventMarkerPaint(@event, pixelScale, opacity));

        // Selection outline.
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
            canvas.DrawTextOnPath(visibilityChangeEvent.Visibility ? "VISIBLE" : "HIDDEN", path, 0, 0, SKTextAlign.Center, NotePaints.GetStandardFont(40 * pixelScale), NotePaints.GetTextPaint(NotePaints.GetEventColor(@event).WithAlpha((byte)(255 * opacity))));
        }
        
        canvas.Restore();
    }

    /// <summary>
    /// Draws an event area.
    /// </summary>
    private static void DrawEventArea(SKCanvas canvas, CanvasInfo canvasInfo, Event @event, float time, float viewDistance, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (@event is StopEffectEvent stopEffectEvent && stopEffectEvent.SubEvents.Length == 2)
        {
            SKPoint[] vertices = new SKPoint[360];

            Timestamp start = stopEffectEvent.SubEvents[0].Timestamp;
            Timestamp end = stopEffectEvent.SubEvents[1].Timestamp;
            RenderUtils.GetProgress(start.Time, start.ScaledTime, false, viewDistance, time, 0, out float r0);
            RenderUtils.GetProgress(end.Time, end.ScaledTime, false, viewDistance, time, 0, out float r1);

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
            
            if (selected || pointerOver)
            {
                canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, null, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
            }
            
            return;
        }
        
        if (@event is ReverseEffectEvent reverseEffectEvent && reverseEffectEvent.SubEvents.Length == 3)
        {
            SKPoint[] vertices0 = new SKPoint[360];
            SKPoint[] vertices1 = new SKPoint[360];

            Timestamp start = reverseEffectEvent.SubEvents[0].Timestamp;
            Timestamp middle = reverseEffectEvent.SubEvents[1].Timestamp;
            Timestamp end = reverseEffectEvent.SubEvents[2].Timestamp;
            RenderUtils.GetProgress(start.Time, start.ScaledTime, false, viewDistance, time, 0, out float r0);
            RenderUtils.GetProgress(middle.Time, middle.ScaledTime, false, viewDistance, time, 0, out float r1);
            RenderUtils.GetProgress(end.Time, end.ScaledTime, false, viewDistance, time, 0, out float r2);

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
            
            if (selected || pointerOver)
            {
                canvas.DrawVertices(SKVertexMode.Triangles, vertices0, null, null, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
                canvas.DrawVertices(SKVertexMode.Triangles, vertices1, null, null, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
            }
        }
    }
    
    /// <summary>
    /// Draws a lane toggle note.
    /// </summary>
    private static void DrawLaneToggle(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, ILaneToggle laneToggle, float startTime, float time, float viewDistance, float perspectiveScale, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (laneToggle is not IPositionable positionable) return;
        bool state = laneToggle is LaneShowNote;

        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        
        // Sweep Visualization
        if (settings.VisualizeLaneSweeps)
        {
            lock (laneToggle)
            {
                SKPoint[] vertices = new SKPoint[positionable.Size * 6];
            
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
                    
                        float stepScale = SaturnMath.InverseLerp(time + viewDistance, time, t);
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

                        float stepScale = SaturnMath.InverseLerp(time + viewDistance, time, t);
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
                    
                        float stepScale = SaturnMath.InverseLerp(time + viewDistance, time, t);
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
        }
        
        // Note body and selection outline.
        if (perspectiveScale is > 0 and <= 1.01f)
        {
            float pixelScale = canvasInfo.Scale3D * perspectiveScale;
            
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
            if (selected || pointerOver)
            {
                DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, positionable.Position, positionable.Size, selected, pointerOver);
            }
        }
    }

    /// <summary>
    /// Draws a bookmark.
    /// </summary>
    private static void DrawBookmark(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Bookmark bookmark, float perspectiveScale, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        float radius = canvasInfo.JudgementLineRadius * perspectiveScale;
        float pixelScale = canvasInfo.Scale3D * perspectiveScale;
        
        canvas.DrawCircle(canvasInfo.Center, radius, NotePaints.GetBookmarkPaint(bookmark.Color, pixelScale, opacity));

        // Selection outline.
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, radius, pixelScale, 0, 60, selected, pointerOver);
        }
    }
    
    /// <summary>
    /// Draws a selection outline.
    /// </summary>
    private static void DrawSelectionOutline(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, float radius, float pixelScale, int position, int size, bool selected, bool pointerOver)
    {
        float radius0 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][4] * 0.99f;
        float radius1 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][5] * 1.01f;
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
    private static void DrawInterface(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Entry entry, float time, bool playing)
    {
        if (entry.ChartEnd.Time > 0)
        {
            float radius = 0.98113f * canvasInfo.Radius;
            SKRect rect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);

            float t = time / entry.ChartEnd.Time;
            t = Math.Clamp(t, 0, 1);
            canvas.DrawArc(rect, 270, 360 - t * 360, false, NotePaints.GetSongTimerPaint(canvasInfo.Scale3D));
        }

        float textRadius = canvasInfo.JudgementLineRadius * 0.987f;
        SKRect textRect = new(canvasInfo.Center.X - textRadius, canvasInfo.Center.Y - textRadius, canvasInfo.Center.X + textRadius, canvasInfo.Center.Y + textRadius);
        
        SKPath path = new();
        path.ArcTo(textRect, 0, 359, true);
        path.ArcTo(textRect, 359, 359, false);

        bool obscureDifficulty = settings.DifficultyDisplayVisibility == RenderSettings.InterfaceVisibilityOption.Obscured;
        
        string difficultyString = obscureDifficulty
            ? "? ? ? ? / Lv."
            : entry.Difficulty switch
            {
                Difficulty.None => "N O N E / Lv.",
                Difficulty.Normal => "N O R M A L / Lv.",
                Difficulty.Hard => "H A R D / Lv.",
                Difficulty.Expert => "E X P E R T / Lv.",
                Difficulty.Inferno => "I N F E R N O / Lv.",
                Difficulty.WorldsEnd => "W O R L D ' S  E N D / Lv.",
                _ => "",
            };
        
        uint difficultyTextColor = obscureDifficulty
            ? 0xFF000000
            : entry.Difficulty switch
            {
                Difficulty.None => 0xFFFFFFFF,
                Difficulty.Normal => 0xFF1B7CFF,
                Difficulty.Hard => 0xFFFFC300,
                Difficulty.Expert => 0xFFFF0084,
                Difficulty.Inferno => 0xFF400084,
                Difficulty.WorldsEnd => 0xFF000000,
                _ => 0xFFBFBFBF,
            };

        uint titleTextColor = settings.JudgementLineColor switch
        {
            RenderSettings.JudgementLineColorOption.Greyscale => 0xFFFFFFFF,
            RenderSettings.JudgementLineColorOption.Version1 => 0xFF821C5F,
            RenderSettings.JudgementLineColorOption.Version2 => 0xFF601C95,
            RenderSettings.JudgementLineColorOption.Version3 => 0xFFFB67B7,
            _ => 0xFF000000,
        };
        
        float circumference = textRadius * float.Pi;
        float titleAngle = circumference * 0.865f;
        float levelAngle = circumference * 0.836f;
        float difficultyAngle = circumference * -0.32f;

        switch (settings.LevelDisplayVisibility)
        {
            case RenderSettings.InterfaceVisibilityOption.Hidden: break;
            
            case RenderSettings.InterfaceVisibilityOption.Obscured:
            {
                canvas.DrawTextOnPath(difficultyString, path, new(difficultyAngle, 0), SKTextAlign.Right, NotePaints.GetBoldFont(20 * canvasInfo.Scale3D), NotePaints.GetTextPaint(difficultyTextColor));
                canvas.DrawTextOnPath("??", path, new(levelAngle, 0), SKTextAlign.Left, NotePaints.GetBoldFont(25 * canvasInfo.Scale3D), NotePaints.GetTextPaint(difficultyTextColor));
                break;
            }
            
            case RenderSettings.InterfaceVisibilityOption.Visible:
            {
                canvas.DrawTextOnPath(difficultyString, path, new(difficultyAngle, 0), SKTextAlign.Right, NotePaints.GetBoldFont(20 * canvasInfo.Scale3D), NotePaints.GetTextPaint(difficultyTextColor));
                canvas.DrawTextOnPath(entry.RawLevelString, path, new(levelAngle, 0), SKTextAlign.Left, NotePaints.GetBoldFont(25 * canvasInfo.Scale3D), NotePaints.GetTextPaint(difficultyTextColor));
                break;
            }
        }

        switch (settings.TitleDisplayVisibility)
        {
            case RenderSettings.InterfaceVisibilityOption.Hidden: break;
            
            case RenderSettings.InterfaceVisibilityOption.Obscured:
            {
                canvas.DrawTextOnPath("???", path, new(titleAngle, 0), SKTextAlign.Left, NotePaints.GetBoldFont(20 * canvasInfo.Scale3D), NotePaints.GetTextPaint(titleTextColor));
                break;
            }
            
            case RenderSettings.InterfaceVisibilityOption.Visible:
            {
                canvas.DrawTextOnPath(entry.Title, path, new(titleAngle, 0), SKTextAlign.Left, NotePaints.GetBoldFont(20 * canvasInfo.Scale3D), NotePaints.GetTextPaint(titleTextColor));
                break;
            }
        }

        if (settings.UnitTickVisibility == RenderSettings.EffectVisibilityOption.AlwaysOn 
        || (settings.UnitTickVisibility == RenderSettings.EffectVisibilityOption.OnlyWhenPaused && !playing)
        || (settings.UnitTickVisibility == RenderSettings.EffectVisibilityOption.OnlyWhenPlaying && playing))
        {
            for (int i = 0; i < 60; i++)
            {
                float judgementLineThickness = (NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] + 2) * canvasInfo.Scale3D;
                float outerRadius = canvasInfo.JudgementLineRadius + judgementLineThickness * 0.5f;
                float innerRadius = canvasInfo.JudgementLineRadius - judgementLineThickness;

                if (i % 5 == 0)
                {
                    innerRadius -= judgementLineThickness;
                }

                float angle = i * -6;
                
                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, outerRadius, angle);
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, innerRadius, angle);

                NotePaints.UnitTickPaint.StrokeWidth = 0.5f;
                canvas.DrawLine(p0, p1, NotePaints.UnitTickPaint);
            }
        }
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
        float squareRadius = 10 * canvasInfo.Scale3D;

        float t = SaturnMath.InverseLerp(startTime, startTime + 550, time);
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
    private static void DrawBackground(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Chart chart, Entry entry, float time, SKPaint? jacketBackgroundPaint, int width, int height)
    {
        SKPoint screenA = new(0, 0);
        SKPoint screenB = new(canvasInfo.Width, 0);
        SKPoint screenC = new(0, canvasInfo.Height);
        SKPoint screenD = new(canvasInfo.Width, canvasInfo.Height);

        width = entry.Background != BackgroundOption.Jacket || width == 0 ? 1080 : width;
        height = entry.Background != BackgroundOption.Jacket || height == 0 ? 1080 : height;
        SKPoint textureA = new(0, 0);
        SKPoint textureB = new(width, 0);
        SKPoint textureC = new(0, height);
        SKPoint textureD = new(width, height);
        
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

        SKRect rect = new(1, 1, canvasInfo.Width - 1, canvasInfo.Height - 1);
        SKRoundRect roundRect = new(rect, canvasInfo.Radius - 1);
        
        canvas.Save();
        canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);

        if (entry.Background == BackgroundOption.Jacket && jacketBackgroundPaint != null)
        {
            canvas.DrawVertices(SKVertexMode.Triangles, vertexCoords, textureCoords, null, jacketBackgroundPaint);
        }
        else
        {
            canvas.DrawVertices(SKVertexMode.Triangles, vertexCoords, textureCoords, null, NotePaints.GetBackgroundPaint(entry, clear));    
        }
        
        
        canvas.Restore();
        
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

    /// <summary>
    /// Draws a judge area.
    /// </summary>
    private static void DrawJudgeArea(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, RenderJudgeArea timingWindow, float opacity)
    {
        if (opacity == 0) return;
        bool drawEarlyGood      = settings.ShowGoodArea      && timingWindow.GreatEarlyScale <= 1     && timingWindow.GoodEarlyScale      > timingWindow.GreatEarlyScale;
        bool drawEarlyGreat     = settings.ShowGreatArea     && timingWindow.MarvelousEarlyScale <= 1 && timingWindow.GreatEarlyScale     > timingWindow.MarvelousEarlyScale;
        bool drawEarlyMarvelous = settings.ShowMarvelousArea && timingWindow.NoteScale <= 1           && timingWindow.MarvelousEarlyScale > timingWindow.NoteScale;
        bool drawLateMarvelous  = settings.ShowMarvelousArea && timingWindow.MarvelousLateScale <= 1  && timingWindow.MarvelousLateScale  < timingWindow.NoteScale;
        bool drawLateGreat      = settings.ShowGreatArea     && timingWindow.GreatLateScale <= 1      && timingWindow.GreatLateScale      < timingWindow.MarvelousLateScale;
        bool drawLateGood       = settings.ShowGoodArea      && timingWindow.GoodLateScale <= 1       && timingWindow.GoodLateScale       < timingWindow.GreatLateScale;

        if (!drawEarlyGood && !drawEarlyGreat && !drawEarlyMarvelous && !drawLateMarvelous && !drawLateGreat && !drawLateGood) return;

        SKRect rect = new(canvasInfo.Center.X - canvasInfo.JudgementLineRadius, canvasInfo.Center.Y - canvasInfo.JudgementLineRadius, canvasInfo.Center.X + canvasInfo.JudgementLineRadius, canvasInfo.Center.Y + canvasInfo.JudgementLineRadius);
        SKRoundRect roundRect = new(rect, canvasInfo.JudgementLineRadius);
        
        canvas.Save();
        canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);
        
        if (drawEarlyGood)
        {
            float radiusStart = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.GoodEarlyScale);
            float radiusEnd = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.GreatEarlyScale);
            drawJudgeAreaPart(radiusStart, radiusEnd, NotePaints.GetGoodJudgeAreaPaint(false, opacity));
        }
        
        if (drawEarlyGreat)
        {
            float radiusStart = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.GreatEarlyScale);
            float radiusEnd = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.MarvelousEarlyScale);
            drawJudgeAreaPart(radiusStart, radiusEnd, NotePaints.GetGreatJudgeAreaPaint(false, opacity));
        }
        
        if (drawEarlyMarvelous)
        {
            float radiusStart = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.MarvelousEarlyScale);
            float radiusEnd = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.NoteScale);
            drawJudgeAreaPart(radiusStart, radiusEnd, NotePaints.GetMarvelousJudgeAreaPaint(false, opacity));
        }
        
        if (drawLateMarvelous)
        {
            float radiusStart = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.NoteScale);
            float radiusEnd = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.MarvelousLateScale);
            drawJudgeAreaPart(radiusStart, radiusEnd, NotePaints.GetMarvelousJudgeAreaPaint(true, opacity));
        }
        
        if (drawLateGreat)
        {
            float radiusStart = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.MarvelousLateScale);
            float radiusEnd = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.GreatLateScale);
            drawJudgeAreaPart(radiusStart, radiusEnd, NotePaints.GetGreatJudgeAreaPaint(true, opacity));
        }
        
        if (drawLateGood)
        {
            float radiusStart = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.GreatLateScale);
            float radiusEnd = canvasInfo.JudgementLineRadius * RenderUtils.Perspective(timingWindow.GoodLateScale);
            drawJudgeAreaPart(radiusStart, radiusEnd, NotePaints.GetGoodJudgeAreaPaint(true, opacity));
        }
        
        canvas.Restore();

        return;

        void drawJudgeAreaPart(float radiusStart, float radiusEnd, SKPaint paint)
        {
            SKPoint[] vertices = new SKPoint[timingWindow.Size * 6];
    
            for (int i = 0; i < timingWindow.Size; i++)
            {
                float angle = (timingWindow.Position + i) * -6;
                SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, radiusStart, angle    );
                SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, radiusStart, angle - 6);
                SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, radiusEnd,   angle    );
                SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, radiusEnd,   angle - 6);

                vertices[6 * i]     = p0;
                vertices[6 * i + 1] = p1;
                vertices[6 * i + 2] = p2;
                vertices[6 * i + 3] = p3;
                vertices[6 * i + 4] = p2;
                vertices[6 * i + 5] = p1;
            }
            
            canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, paint);
        }
    }

    /// <summary>
    /// Draws a box select area.
    /// </summary>
    private static void DrawBoxSelect(SKCanvas canvas, CanvasInfo canvasInfo, float time, float viewDistance, RenderBoxSelectData renderBoxSelect)
    {
        if (renderBoxSelect.StartTime == null) return;
        if (renderBoxSelect.EndTime == null) return;
        if (renderBoxSelect.Position == null) return;
        if (renderBoxSelect.Size == null) return;
        
        RenderUtils.GetProgress(renderBoxSelect.StartTime.Value, 0, false, viewDistance, time, 0, out float r0);
        RenderUtils.GetProgress(renderBoxSelect.EndTime.Value, 0, false, viewDistance, time, 0, out float r1);

        r0 = Math.Clamp(r0, 0, 1.25f);
        r1 = Math.Clamp(r1, 0, 1.25f);
        
        r0 = RenderUtils.Perspective(r0) * canvasInfo.JudgementLineRadius;
        r1 = RenderUtils.Perspective(r1) * canvasInfo.JudgementLineRadius;
        
        SKPoint[] vertices = new SKPoint[renderBoxSelect.Size.Value * 6];
        for (int i = 0; i < renderBoxSelect.Size; i++)
        {
            float angle = (renderBoxSelect.Position.Value + i) * -6;
            SKPoint p0 = RenderUtils.PointOnArc(canvasInfo.Center, r0, angle    );
            SKPoint p1 = RenderUtils.PointOnArc(canvasInfo.Center, r0, angle - 6);
            SKPoint p2 = RenderUtils.PointOnArc(canvasInfo.Center, r1, angle    );
            SKPoint p3 = RenderUtils.PointOnArc(canvasInfo.Center, r1, angle - 6);

            vertices[6 * i]     = p0;
            vertices[6 * i + 1] = p1;
            vertices[6 * i + 2] = p2;
            vertices[6 * i + 3] = p3;
            vertices[6 * i + 4] = p2;
            vertices[6 * i + 5] = p1;
        }
        
        SKRect rect = new(canvasInfo.Center.X - canvasInfo.JudgementLineRadius, canvasInfo.Center.Y - canvasInfo.JudgementLineRadius, canvasInfo.Center.X + canvasInfo.JudgementLineRadius, canvasInfo.Center.Y + canvasInfo.JudgementLineRadius);
        SKRoundRect roundRect = new(rect, canvasInfo.JudgementLineRadius);
        
        canvas.Save();
        canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);
        
        canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, NotePaints.GetObjectOutlineFillPaint(true, false));

        if (renderBoxSelect.Size == 60)
        {
            SKPaint paint = NotePaints.GetObjectOutlineStrokePaint(true, false);
            canvas.DrawCircle(canvasInfo.Center, r0, paint);
            canvas.DrawCircle(canvasInfo.Center, r1, paint);
        }
        else
        {
            SKPath path = new();
            SKRect rect0 = new(canvasInfo.Center.X - r0, canvasInfo.Center.Y - r0, canvasInfo.Center.X + r0, canvasInfo.Center.Y + r0);
            SKRect rect1 = new(canvasInfo.Center.X - r1, canvasInfo.Center.Y - r1, canvasInfo.Center.X + r1, canvasInfo.Center.Y + r1);

            path.ArcTo(rect0, renderBoxSelect.Position.Value * -6, renderBoxSelect.Size.Value * -6, true);
            path.ArcTo(rect1, (renderBoxSelect.Position.Value + renderBoxSelect.Size.Value) * -6, renderBoxSelect.Size.Value * 6, false);
            path.Close();
            
            canvas.DrawPath(path, NotePaints.GetObjectOutlineStrokePaint(true, false));
        }
        
        canvas.Restore();
    }
    
    private struct RenderBonusSweepEffect(int startPosition, float startTime, float duration, bool isCounterclockwise)
    {
        public readonly int StartPosition = startPosition;
        public readonly float StartTime = startTime;
        public readonly float Duration = duration;
        public readonly bool IsCounterclockwise = isCounterclockwise;
    }
}