using SaturnData.Notation.Core;
using SaturnData.Notation.Events;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SaturnData.Utilities;
using SkiaSharp;

namespace SaturnView;

public static class Renderer2D
{
    private const float MarginLeft = 100;
    private const float MarginRight = 140;
    private const float JudgementLineOffset = 30;
    
    public static void Render(SKCanvas canvas,
        CanvasInfo canvasInfo,
        RenderSettings settings,
        Chart chart,
        float time,
        bool playing,
        int zoomLevel,
        HashSet<ITimeable>? selectedObjects,
        ITimeable? pointerOverObject,
        ITimeable? activeObjectGroup,
        RenderBoxSelectData? boxSelect,
        Note? cursorNote)
    {
        float viewDistance = GetViewDistance(zoomLevel);
        List<RenderObject> objectsToDraw = [];
        List<RenderObject> holdEndsToDraw = [];
        List<RenderObject> holdsToDraw = [];
        List<RenderObject> eventAreasToDraw = [];
        List<RenderJudgeArea> timingWindowsToDraw = [];

        lock (chart)
        {
            calculateRenderObjects();
        }
        
        canvas.Clear(canvasInfo.BackgroundColor);
        
        //canvas.DrawRect(0, 0, MarginLeft, canvasInfo.Height, NotePaints.DebugPaint3);
        //canvas.DrawRect(canvasInfo.Width - MarginRight, 0, MarginRight, canvasInfo.Height, NotePaints.DebugPaint3);
        
        DrawJudgementLine(canvas, canvasInfo, settings);
        DrawLanes(canvas, canvasInfo);

        if (boxSelect != null)
        {
            DrawBoxSelect(canvas, canvasInfo, time, viewDistance, boxSelect.Value);
        }
        
        return;

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
        
        void renderEventAreas()
        {
            foreach (RenderObject renderObject in eventAreasToDraw)
            {
                if (renderObject.Object is not Event @event) continue;
                bool selected = selectedObjects != null && selectedObjects.Contains(renderObject.Object);
                bool pointerOver = pointerOverObject != null && pointerOverObject == renderObject.Object;
                
                // TODO
                //DrawEventArea(canvas, canvasInfo, @event, time, viewDistance, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f, selected, pointerOver);
            }
        }

        void renderHoldEnds()
        {
            foreach (RenderObject renderObject in holdEndsToDraw)
            {
                if (renderObject.Object is not HoldPointNote holdPointNote) continue;
                
                bool selected = selectedObjects != null && selectedObjects.Contains(holdPointNote);
                bool pointerOver = pointerOverObject != null && pointerOverObject == holdPointNote;
                
                // TODO
                //DrawHoldEndNote(canvas, canvasInfo, settings, holdPointNote, RenderUtils.Perspective(renderObject.Scale), renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f, selected, pointerOver);
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
                
                // TODO
                // DrawHoldSurface(canvas, canvasInfo, settings, holdNote, renderObject.Layer, time, viewDistance, playing, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f, selected, pointerOver);
            }
        }
        
        void renderJudgeAreas()
        {
            foreach (RenderJudgeArea renderJudgeArea in timingWindowsToDraw)
            {
                // TODO
                //DrawJudgeArea(canvas, canvasInfo, settings, renderJudgeArea, 1);
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
                    // TODO
                    /*DrawHoldPointNote
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        note: holdPointNote,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );*/
                }
                else if (renderObject.Object is SyncNote syncNote)
                {
                    // TODO
                    /*DrawSyncNote
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        note: syncNote,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );*/
                }
                else if (renderObject.Object is MeasureLineNote measureLineNote)
                {
                    if (!settings.ShowBeatLineNotes && measureLineNote.IsBeatLine) return;
                    
                    // TODO
                    /*DrawMeasureLineNote
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
                    );*/
                }
                else if (renderObject.Object is ILaneToggle laneToggle)
                {
                    float startTime = overwriteLaneToggleStartTime 
                        ? time 
                        : ((ITimeable)laneToggle).Timestamp.Time;
                    
                    // TODO
                    /*DrawLaneToggle
                     (
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
                        pointerOver: pointerOver
                    );*/
                }
                else if (renderObject.Object is Note note)
                {
                    // TODO
                    /*DrawNote
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
                    );*/
                }
                else if (renderObject.Object is Event @event)
                {
                    // TODO
                    /*DrawEvent
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        @event: @event,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );*/
                }
                else if (renderObject.Object is Bookmark bookmark)
                {
                    // TODO
                    /*DrawBookmark
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        bookmark: bookmark,
                        perspectiveScale: RenderUtils.Perspective(renderObject.Scale),
                        opacity: opacity,
                        selected: selected,
                        pointerOver: pointerOver
                    );*/
                }
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
    public static void RenderToPng(string filepath, int resolution, RenderSettings settings, Chart chart, float time, bool playing, int zoomLevel)
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

        Render(canvas, canvasInfo, settings, chart, time, playing, zoomLevel, null, null, null, null, null);
        
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
    public static IPositionable.OverlapResult HitTest(ITimeable obj, float x, float y, float time, float scaledTime, CanvasInfo canvasInfo, bool showSpeedChanges, RenderSettings settings, ITimeable? activeObjectGroup)
    {
        float viewDistance = GetViewDistance(settings.NoteSpeed);
        float threshold = GetHitTestThreshold(canvasInfo, settings.NoteThickness);
        float depth = GetHitTestPointerDepth(canvasInfo, y);
        int lane = GetHitTestPointerLane(canvasInfo, x);
        
        return HitTest(obj, depth, lane, time, scaledTime, viewDistance, threshold, showSpeedChanges, settings, activeObjectGroup);
    }
    
    /// <summary>
    /// Returns <c>true</c> if the specified radial coordinate lies on an object.
    /// </summary>
    /// <param name="obj">The object to hit test. (May also implement IPositionable.)</param>
    /// <param name="depth">The 0-1 radius of the radial coordinate.</param>
    /// <param name="lane">The 0-59 lane of the radial coordinate.</param>
    /// <param name="time">The current time.</param>
    /// <param name="scaledTime">The current scaled time.</param>
    /// <param name="viewDistance">The current view distance.</param>
    /// <param name="showSpeedChanges">Should speed changes be taken into account?</param>
    /// <param name="threshold">The radius threshold for hit testing.</param>
    /// <returns></returns>
    public static IPositionable.OverlapResult HitTest(ITimeable obj, float depth, int lane, float time, float scaledTime, float viewDistance, float threshold, bool showSpeedChanges, RenderSettings settings, ITimeable? activeObjectGroup)
    {
        if (lane is > 59 or < 0) return IPositionable.OverlapResult.None;

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
                HoldPointNote start = holdNote.Points[i - 1];
                HoldPointNote end = holdNote.Points[i];

                RenderUtils.GetProgress(start.Timestamp.Time, start.Timestamp.ScaledTime, showSpeedChanges, viewDistance, time, scaledTime, out float startProgress);
                RenderUtils.GetProgress(end.Timestamp.Time, end.Timestamp.ScaledTime, showSpeedChanges, viewDistance, time, scaledTime, out float endProgress);
                startProgress = RenderUtils.Perspective(startProgress);
                endProgress = RenderUtils.Perspective(endProgress);

                if (startProgress < depth) continue;
                if (endProgress > depth) continue;

                float t = SaturnMath.InverseLerp(startProgress, endProgress, depth);
                t = RenderUtils.Perspective(t);

                int position = (int)MathF.Round(SaturnMath.LerpCyclic(start.Position, end.Position, t, 60));
                int size = (int)MathF.Round(SaturnMath.Lerp(start.Size, end.Size, t));

                IPositionable.OverlapResult result = IPositionable.HitTestResult(position, size, lane);
                if (result != IPositionable.OverlapResult.None) return result;
            }
        }
        
        return IPositionable.OverlapResult.None;

        IPositionable.OverlapResult hitTestObject(ITimeable t)
        {
            if (!RenderUtils.GetProgress(t.Timestamp.Time, t.Timestamp.ScaledTime, showSpeedChanges, viewDistance, time, scaledTime, out float progress)) return IPositionable.OverlapResult.None;
            if (Math.Abs(depth - progress) > threshold) return IPositionable.OverlapResult.None;
            
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
    /// <param name="zoomLevel">The current note speed.</param>
    public static float GetViewDistance(int zoomLevel) => 3333.333f * zoomLevel * 0.1f;

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
    /// Returns the 0-1 depth of a pixel coordinate.
    /// </summary>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="x">The x-coordinate in pixels</param>
    /// <param name="y">The y-coordinate in pixels</param>
    public static float GetHitTestPointerDepth(CanvasInfo canvasInfo, float y)
    {
        return 1 - SaturnMath.InverseLerp(JudgementLineOffset, canvasInfo.Height, canvasInfo.Height - y);
    }

    /// <summary>
    /// Returns the 0-59 lane position of a pixel coordinate.
    /// </summary>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="x">The x-coordinate in pixels</param>
    /// <param name="y">The y-coordinate in pixels</param>
    public static int GetHitTestPointerLane(CanvasInfo canvasInfo, float x)
    {
        float t = SaturnMath.InverseLerp(MarginLeft, canvasInfo.Width - MarginRight, x);
        
        int lane = (int)(t * 60);
        lane = (lane + 15) % 60;

        return (lane + 60) % 60;
    }

    private static void DrawLanes(SKCanvas canvas, CanvasInfo canvasInfo)
    {
        float gridWidth = canvasInfo.Width - MarginLeft - MarginRight;
        float step = gridWidth / 60;
        
        for (int i = 0; i <= 60; i++)
        {
            float x = i * step + 1 + MarginLeft;
            
            bool major = i % 5 == 0;
            canvas.DrawLine(x, 0, x, canvasInfo.Height, major ? NotePaints.LanePaintMajor_2D : NotePaints.LanePaintMinor_2D);
        }
    }

    private static void DrawJudgementLine(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings)
    {
        float y = canvasInfo.Height - JudgementLineOffset;
        float right = canvasInfo.Width - MarginRight + 1;
        
        canvas.DrawLine(MarginLeft, y, right, y, NotePaints.GetJudgementLinePaint_2D(settings, MarginLeft, right, y));
    }

    private static void DrawBoxSelect(SKCanvas canvas, CanvasInfo canvasInfo, float time, float viewDistance, RenderBoxSelectData renderBoxSelect)
    {
        if (renderBoxSelect.StartTime == null) return;
        if (renderBoxSelect.EndTime == null) return;
        if (renderBoxSelect.Position == null) return;
        if (renderBoxSelect.Size == null) return;
        
        RenderUtils.GetProgress(renderBoxSelect.StartTime.Value, 0, false, viewDistance, time, 0, out float d0);
        RenderUtils.GetProgress(renderBoxSelect.EndTime.Value, 0, false, viewDistance, time, 0, out float d1);

        int offsetPosition = renderBoxSelect.Position.Value - 15;

        float left = MarginLeft;
        float right = canvasInfo.Width - MarginRight;
        float bottom = d0 * (canvasInfo.Height - JudgementLineOffset);
        float top = d1 * (canvasInfo.Height - JudgementLineOffset);
        SKRect rect = new(left, top, right, bottom);
        canvas.DrawRect(rect, NotePaints.DebugPaint3);
        
        Console.WriteLine($"{d0} | {d1}");
        
        
        //canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, NotePaints.GetObjectOutlineFillPaint(true, false));
        //
        //if (renderBoxSelect.Size == 60)
        //{
        //    SKPaint paint = NotePaints.GetObjectOutlineStrokePaint(true, false);
        //    canvas.DrawCircle(canvasInfo.Center, d0, paint);
        //    canvas.DrawCircle(canvasInfo.Center, d1, paint);
        //}
        //else
        //{
        //    SKPath path = new();
        //    SKRect rect0 = new(canvasInfo.Center.X - d0, canvasInfo.Center.Y - d0, canvasInfo.Center.X + d0, canvasInfo.Center.Y + d0);
        //    SKRect rect1 = new(canvasInfo.Center.X - d1, canvasInfo.Center.Y - d1, canvasInfo.Center.X + d1, canvasInfo.Center.Y + d1);
        //
        //    path.ArcTo(rect0, renderBoxSelect.Position.Value * -6, renderBoxSelect.Size.Value * -6, true);
        //    path.ArcTo(rect1, (renderBoxSelect.Position.Value + renderBoxSelect.Size.Value) * -6, renderBoxSelect.Size.Value * 6, false);
        //    path.Close();
        //    
        //    canvas.DrawPath(path, NotePaints.GetObjectOutlineStrokePaint(true, false));
        //}
    }
}