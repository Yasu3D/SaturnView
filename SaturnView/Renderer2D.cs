using SaturnData.Notation.Core;
using SaturnData.Notation.Events;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SaturnData.Utilities;
using SkiaSharp;

namespace SaturnView;

// TODO: make passed notes visible at lower opacity
public static class Renderer2D
{
    private const float MarginLeft = 70;
    private const float MarginRight = 140;
    private const float JudgementLineOffset = 100;
    private const float NoteThicknessMultiplier = 0.2f;
    
    private static readonly float[][] SyncOutlineOffset = 
    [
        [-15.37f * 2,  -9.01f * 2,  7.42f * 2, 13.78f * 2, -12.0f * 2.5f, 12.0f * 2.5f],
        [-19.61f * 2, -13.25f * 2, 13.25f * 2, 19.61f * 2, -16.0f * 2.5f, 16.0f * 2.5f],
        [-28.62f * 2, -22.26f * 2, 22.79f * 2, 29.15f * 2, -25.0f * 2.5f, 25.0f * 2.5f],
        [-39.22f * 2, -32.86f * 2, 33.39f * 2, 39.75f * 2, -36.0f * 2.5f, 36.0f * 2.5f],
        [-47.70f * 2, -41.34f * 2, 42.40f * 2, 48.76f * 2, -45.0f * 2.5f, 45.0f * 2.5f],
    ];
    
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
        float viewDistance = GetViewDistance(zoomLevel, canvasInfo);
        float laneStep = (canvasInfo.Width - MarginLeft - MarginRight) / 60;
        
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
        DrawLanes(canvas, canvasInfo, laneStep);

        renderEventAreas();
        renderHoldEnds();
        renderHoldSurfaces();
        renderJudgeAreas();
        renderObjects();
        
        if (boxSelect != null)
        {
            DrawBoxSelect(canvas, canvasInfo, time, viewDistance, boxSelect.Value, laneStep);
        }
        
        return;

        void calculateRenderObjects()
        {
            if (chart.Layers.Count == 0) return;
            
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
                
                if (!RenderUtils.GetProgress(@event.Timestamp.Time, 0, false, viewDistance, time, 0, out float progress)) continue;
                objectsToDraw.Add(new(@event, chart.Layers[0], 0, progress, false, RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
            }

            // Find all visible bookmarks.
            foreach (Bookmark bookmark in chart.Bookmarks)
            {
                if (settings.HideBookmarksDuringPlayback && playing) break;
                
                if (!RenderUtils.GetProgress(bookmark.Timestamp.Time, 0, false, viewDistance, time, 0, out float progress)) continue;
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

                // Find all visible events.
                foreach (Event @event in layer.Events)
                {
                    if (settings.HideEventMarkersDuringPlayback && playing) break;

                    if (@event is StopEffectEvent stopEffectEvent && stopEffectEvent.SubEvents.Length == 2)
                    {
                        Timestamp start = stopEffectEvent.SubEvents[0].Timestamp;
                        Timestamp end = stopEffectEvent.SubEvents[1].Timestamp;
                        
                        // Start Marker
                        if (RenderUtils.GetProgress(start.Time, 0, false, viewDistance, time, 0, out float t0))
                        {
                            objectsToDraw.Add(new(stopEffectEvent.SubEvents[0], layer, l, t0, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                        
                        // End Marker
                        if (RenderUtils.GetProgress(end.Time, 0, false, viewDistance, time, 0, out float t1))
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
                        if (RenderUtils.GetProgress(start.Time, 0, false, viewDistance, time, 0, out float t0))
                        {
                            objectsToDraw.Add(new(reverseEffectEvent.SubEvents[0], layer, l, t0, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                        
                        // Middle Marker
                        if (RenderUtils.GetProgress(middle.Time, 0, false, viewDistance, time, 0, out float t1))
                        {
                            objectsToDraw.Add(new(reverseEffectEvent.SubEvents[1], layer, l, t1, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                        }
                        
                        // End Marker
                        if (RenderUtils.GetProgress(end.Time, 0, false, viewDistance, time, 0, out float t2))
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
                        if (!RenderUtils.GetProgress(@event.Timestamp.Time, 0, false, viewDistance, time, 0, out float t)) continue;
                        objectsToDraw.Add(new(@event, layer, l, t, false, layer.Visible && RenderUtils.IsVisible(@event, settings, activeObjectGroup)));
                    }
                }
                
                // Find all visible notes.
                for (int n = 0; n < layer.Notes.Count; n++)
                {
                    Note note = layer.Notes[n];
                    
                    if (note is IPlayable playable)
                    {
                        // Timing windows
                        if (checkForJudgeAreas && playable.JudgementType != JudgementType.Fake && note is IPositionable positionable2)
                        {
                            if (timingWindowVisible())
                            {
                                // Long names for everything........
                                RenderUtils.GetProgress(playable.JudgeArea.MarvelousEarly, 0, false, viewDistance, time, 0, out float marvEarly);
                                RenderUtils.GetProgress(playable.JudgeArea.MarvelousLate,  0, false, viewDistance, time, 0, out float marvLate);
                                RenderUtils.GetProgress(playable.JudgeArea.GreatEarly,     0, false, viewDistance, time, 0, out float greatEarly);
                                RenderUtils.GetProgress(playable.JudgeArea.GreatLate,      0, false, viewDistance, time, 0, out float greatLate);
                                RenderUtils.GetProgress(playable.JudgeArea.GoodEarly,      0, false, viewDistance, time, 0, out float goodEarly);
                                RenderUtils.GetProgress(playable.JudgeArea.GoodLate,       0, false, viewDistance, time, 0, out float goodLate);
                                RenderUtils.GetProgress(note.Timestamp.Time,               0, false, viewDistance, time, 0, out float noteScale);
                                
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
                                if (playable.JudgeArea.MaxLate < time) return false;
                                if (playable.JudgeArea.MaxEarly > time + viewDistance) return false;

                                return true;
                            }
                        }
                    }
                    
                    if (note is HoldNote holdNote && holdNote.Points.Count != 0)
                    {
                        // Hold Notes
                        if (holdNote.Points[^1].Timestamp.Time < time) continue;
                        if (holdNote.Points[ 0].Timestamp.Time > time + viewDistance) continue;
                        
                        bool isVisible = layer.Visible && RenderUtils.IsVisible(holdNote, settings, activeObjectGroup);
                        
                        holdsToDraw.Add(new(holdNote, layer, l, 0, false, isVisible));
                        
                        // Hold Start
                        Timestamp start = holdNote.Points[0].Timestamp;
                        if (RenderUtils.GetProgress(start.Time, 0, false, viewDistance, time, 0, out float tStart))
                        {
                            Note? prev = n > 0                     ? layer.Notes[n - 1] : null;
                            Note? next = n < layer.Notes.Count - 1 ? layer.Notes[n + 1] : null;
                            bool sync = note.IsSync(prev) || note.IsSync(next);

                            objectsToDraw.Add(new(note, layer, l, tStart, sync, isVisible));
                        }

                        // Hold End
                        Timestamp end = holdNote.Points[^1].Timestamp;
                        if (holdNote.Points.Count > 1 && RenderUtils.GetProgress(end.Time, 0, false, viewDistance, time, 0, out float tEnd))
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
                                float t = 1 - (point.Timestamp.Time - time) / viewDistance;

                                objectsToDraw.Add(new(point, layer, l, t, false, isVisible));
                            }
                        }
                    }
                    else
                    {
                        // Normal Notes
                        if (!RenderUtils.GetProgress(note.Timestamp.Time, 0, false, viewDistance, time, 0, out float t)) continue;

                        Note? prev = n > 0                     ? layer.Notes[n - 1] : null;
                        Note? next = n < layer.Notes.Count - 1 ? layer.Notes[n + 1] : null;
                        bool sync = note.IsSync(prev) || note.IsSync(next);

                        objectsToDraw.Add(new(note, layer, l, t, sync, layer.Visible && RenderUtils.IsVisible(note, settings, activeObjectGroup)));
                    }
                }

                // Find all visible generated notes.
                foreach (Note note in layer.GeneratedNotes)
                {
                    if (!RenderUtils.GetProgress(note.Timestamp.Time, 0, false, viewDistance, time, 0, out float t)) continue;

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
                
                DrawHoldEndNote(canvas, canvasInfo, settings, holdPointNote, renderObject.Scale, laneStep, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f, selected, pointerOver);
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
                
                DrawHoldSurface(canvas, canvasInfo, settings, holdNote, time, viewDistance, laneStep, playing, renderObject.IsVisible ? 1 : settings.HiddenOpacity * 0.1f, selected, pointerOver);
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
                    DrawHoldPointNote
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        note: holdPointNote,
                        depth: renderObject.Scale,
                        laneStep: laneStep,
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
                        depth: renderObject.Scale,
                        laneStep: laneStep,
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
                        depth: renderObject.Scale,
                        laneStep: laneStep,
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
                    DrawNote
                    (
                        canvas: canvas,
                        canvasInfo: canvasInfo,
                        settings: settings,
                        depth: renderObject.Scale,
                        laneStep: laneStep,
                        sync: renderObject.Sync,
                        opacity: opacity,
                        note: note,
                        selected: selected,
                        pointerOver: pointerOver
                    );
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
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="settings">The current render settings.</param>
    public static IPositionable.OverlapResult HitTest(ITimeable obj, float x, float y, float time, CanvasInfo canvasInfo, RenderSettings settings, ITimeable? activeObjectGroup)
    {
        float viewDistance = GetViewDistance(settings.NoteSpeed, canvasInfo);
        float threshold = GetHitTestThreshold(canvasInfo, settings.NoteThickness);
        float depth = GetHitTestPointerDepth(canvasInfo, y);
        int lane = GetHitTestPointerLane(canvasInfo, x);
        
        return HitTest(obj, depth, lane, time, viewDistance, threshold, settings, activeObjectGroup);
    }
    
    /// <summary>
    /// Returns <c>true</c> if the specified radial coordinate lies on an object.
    /// </summary>
    /// <param name="obj">The object to hit test. (May also implement IPositionable.)</param>
    /// <param name="depth">The 0-1 radius of the radial coordinate.</param>
    /// <param name="lane">The 0-59 lane of the radial coordinate.</param>
    /// <param name="time">The current time.</param>
    /// <param name="viewDistance">The current view distance.</param>
    /// <param name="threshold">The radius threshold for hit testing.</param>
    /// <returns></returns>
    public static IPositionable.OverlapResult HitTest(ITimeable obj, float depth, int lane, float time, float viewDistance, float threshold, RenderSettings settings, ITimeable? activeObjectGroup)
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

            if (holdNote.Points[0].Timestamp.Time > time + viewDistance) return IPositionable.OverlapResult.None;

            for (int i = 1; i < holdNote.Points.Count; i++)
            {
                HoldPointNote start = holdNote.Points[i - 1];
                HoldPointNote end = holdNote.Points[i];

                RenderUtils.GetProgress(start.Timestamp.Time, 0, false, viewDistance, time, 0, out float startProgress);
                RenderUtils.GetProgress(end.Timestamp.Time, 0, false, viewDistance, time, 0, out float endProgress);
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
            if (!RenderUtils.GetProgress(t.Timestamp.Time, 0, false, viewDistance, time, 0, out float progress)) return IPositionable.OverlapResult.None;
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
    public static float GetViewDistance(int zoomLevel, CanvasInfo canvasInfo) => 3333.333f * zoomLevel * 0.1f * canvasInfo.ScaleY;

    /// <summary>
    /// Returns the hit test threshold, based on the current note thickness.
    /// </summary>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="noteThickness">The current note thickness.</param>
    /// <returns></returns>
    public static float GetHitTestThreshold(CanvasInfo canvasInfo, RenderSettings.NoteThicknessOption noteThickness)
    {
        return (NotePaints.NoteStrokeWidths[(int)noteThickness] / canvasInfo.Height) * 1.2f * NoteThicknessMultiplier;
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

    private static void DrawLanes(SKCanvas canvas, CanvasInfo canvasInfo, float laneStep)
    {
        for (int i = 0; i <= 60; i++)
        {
            float x = i * laneStep + 1 + MarginLeft;
            
            bool major = i % 5 == 0;
            canvas.DrawLine(x, 0, x, canvasInfo.Height, major ? NotePaints.LanePaintMajor_2D : NotePaints.LanePaintMinor_2D);
        }
    }

    private static void DrawJudgementLine(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings)
    {
        float y = canvasInfo.Height - JudgementLineOffset;
        float right = canvasInfo.Width - MarginRight + 1;
        
        canvas.DrawLine(MarginLeft, y, right, y, NotePaints.GetJudgementLinePaint_2D(settings, MarginLeft, right, y, NoteThicknessMultiplier));
    }

    private static void DrawMeasureLineNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, float depth, float laneStep, bool isBeatLine, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        
        float y = depth * (canvasInfo.Height - JudgementLineOffset);
        
        if (isBeatLine)
        {
            canvas.DrawLine(MarginLeft, y, canvasInfo.Width - MarginRight, y, NotePaints.GetBeatLinePaint_2D(opacity));
        }
        else
        {
            canvas.DrawLine(MarginLeft, y, canvasInfo.Width - MarginRight, y, NotePaints.GetMeasureLinePaint_2D(opacity));
        }

        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, depth, laneStep, 0, 60, selected, pointerOver);
        }
    }

    private static void DrawNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, Note note, float depth, float laneStep, bool sync, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (depth is <= -0.1f or > 1.25f) return;
        if (note is not IPositionable positionable) return;

        IPlayable? playable = note as IPlayable;

        int colorId = note switch
        {
            TouchNote => (int)settings.TouchNoteColor,
            ChainNote => (int)settings.ChainNoteColor,
            HoldNote => (int)settings.HoldNoteColor,
            SlideClockwiseNote => (int)settings.SlideClockwiseNoteColor,
            SlideCounterclockwiseNote => (int)settings.SlideCounterclockwiseNoteColor,
            SnapForwardNote => (int)settings.SnapForwardNoteColor,
            SnapBackwardNote => (int)settings.SnapBackwardNoteColor,
            _ => -1,
        };

        if (colorId == -1) return;

        float center = depth * (canvasInfo.Height - JudgementLineOffset);
        float top = center + NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] * NoteThicknessMultiplier;
        float bottom = center - NotePaints.NoteStrokeWidths[(int)settings.NoteThickness] * NoteThicknessMultiplier;

        // Note Body
        if (positionable.Size == 60)
        {
            // R-Effect Glow
            if (playable != null && playable.BonusType == BonusType.R)
            {
                canvas.DrawLine(MarginLeft, center, canvasInfo.Width - MarginRight, center, NotePaints.GetRNotePaint(settings, NoteThicknessMultiplier * 1.75f, opacity));
            }

            // Body
            SKRect rect = new(MarginLeft, top, canvasInfo.Width - MarginRight, bottom);
            canvas.DrawRect(rect, NotePaints.GetNoteBasePaint_2D(settings, colorId, top, bottom, opacity));
        }
        else
        {
            int offsetPosition = positionable.Position < 15
                ? positionable.Position + 45
                : positionable.Position - 15;

            if (positionable.Size != 1)
            {
                int insetPosition = (offsetPosition + 1) % 60;
                int insetSize = positionable.Size - 2;

                bool wrapGlow = offsetPosition + positionable.Size > 60;
                bool wrapNote = insetPosition + insetSize > 60;

                // R-Effect Glow
                if (playable != null && playable.BonusType == BonusType.R)
                {
                    if (wrapGlow)
                    {
                        float left1 = MarginLeft + offsetPosition % 60 * laneStep;
                        float right1 = canvasInfo.Width - MarginRight;

                        float left2 = MarginLeft;
                        float right2 = MarginLeft + (offsetPosition + positionable.Size) % 60 * laneStep;

                        canvas.DrawLine(left1 + laneStep * 0.5f, center, right1, center, NotePaints.GetRNotePaint(settings, NoteThicknessMultiplier * 1.75f, opacity));
                        canvas.DrawLine(left2, center, right2 - laneStep * 0.5f, center, NotePaints.GetRNotePaint(settings, NoteThicknessMultiplier * 1.75f, opacity));
                    }
                    else
                    {
                        float left = MarginLeft + insetPosition * laneStep;
                        float right = left + insetSize * laneStep;

                        canvas.DrawLine(left - laneStep * 0.5f, center, right + laneStep * 0.5f, center, NotePaints.GetRNotePaint(settings, NoteThicknessMultiplier * 1.75f, opacity));
                    }
                }

                if (wrapNote)
                {
                    float left1 = MarginLeft + insetPosition * laneStep;
                    float right1 = canvasInfo.Width - MarginRight;

                    float left2 = MarginLeft;
                    float right2 = MarginLeft + (insetPosition + insetSize) % 60 * laneStep;

                    SKRect rect1 = new(left1, top, right1, bottom);
                    SKRect rect2 = new(left2, top, right2, bottom);
                    canvas.DrawRect(rect1, NotePaints.GetNoteBasePaint_2D(settings, colorId, top, bottom, opacity));
                    canvas.DrawRect(rect2, NotePaints.GetNoteBasePaint_2D(settings, colorId, top, bottom, opacity));
                }
                else
                {
                    float left = MarginLeft + insetPosition * laneStep;
                    float right = left + insetSize * laneStep;

                    SKRect rect = new(left, top, right, bottom);
                    canvas.DrawRect(rect, NotePaints.GetNoteBasePaint_2D(settings, colorId, top, bottom, opacity));
                }
            }

            // Caps
            float capRight = MarginLeft + (offsetPosition + 1 % 60) * laneStep;
            float capLeft = capRight - laneStep * 0.25f;

            SKRect capRect = new(capLeft, top, capRight, bottom);
            canvas.DrawRect(capRect, NotePaints.GetNoteCapPaint_2D(settings, top, bottom, opacity));

            if (positionable.Size > 1)
            {
                capLeft = MarginLeft + (offsetPosition + positionable.Size - 1) % 60 * laneStep;
                capRight = capLeft + laneStep * 0.25f;

                capRect = new(capLeft, top, capRight, bottom);
                canvas.DrawRect(capRect, NotePaints.GetNoteCapPaint_2D(settings, top, bottom, opacity));
            }

            /*SKRect rect = new(canvasInfo.Center.X - radius, canvasInfo.Center.Y - radius, canvasInfo.Center.X + radius, canvasInfo.Center.Y + radius);

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
            }*/
        }

        // Sync Outline
        if (sync)
        {
            float top1 = center + SyncOutlineOffset[(int)settings.NoteThickness][0] * NoteThicknessMultiplier;
            float bottom1 = center + SyncOutlineOffset[(int)settings.NoteThickness][3] * NoteThicknessMultiplier;

            float top2 = center + SyncOutlineOffset[(int)settings.NoteThickness][1] * NoteThicknessMultiplier;
            float bottom2 = center + SyncOutlineOffset[(int)settings.NoteThickness][2] * NoteThicknessMultiplier;

            if (positionable.Size == 60)
            {
                SKRect rect = new(MarginLeft, top1, canvasInfo.Width - MarginRight, top2);
                canvas.DrawRect(rect, NotePaints.GetSyncOutlinePaint(opacity));

                rect = new(MarginLeft, bottom1, canvasInfo.Width - MarginRight, bottom2);
                canvas.DrawRect(rect, NotePaints.GetSyncOutlinePaint(opacity));
            }
            else
            {
                int offsetPosition = positionable.Position < 15
                    ? positionable.Position + 45
                    : positionable.Position - 15;

                bool wrap = offsetPosition + positionable.Size > 60;

                SKPath path = new();

                if (wrap)
                {
                    float left = MarginLeft + offsetPosition * laneStep + laneStep * 0.45f;
                    float right = canvasInfo.Width - MarginRight;

                    SKPoint p0 = new(left, top1);
                    SKPoint p1 = new(right, top1);
                    SKPoint p2 = new(right, bottom1);
                    SKPoint p3 = new(left, bottom1);

                    SKPoint c0 = new(left - laneStep * 0.2f, center);

                    path.MoveTo(p1);
                    path.LineTo(p0);
                    path.QuadTo(c0, p3);
                    path.LineTo(p2);

                    left += laneStep * 0.07f;
                    p0 = new(left, top2);
                    p1 = new(right, top2);
                    p2 = new(right, bottom2);
                    p3 = new(left, bottom2);

                    c0 = new(left - laneStep * 0.2f, center);

                    path.LineTo(p2);
                    path.LineTo(p3);
                    path.QuadTo(c0, p0);
                    path.LineTo(p1);
                    path.Close();

                    left = MarginLeft;
                    right = MarginLeft + (offsetPosition + positionable.Size) % 60 * laneStep - laneStep * 0.45f;

                    p0 = new(left, top1);
                    p1 = new(right, top1);
                    p2 = new(right, bottom1);
                    p3 = new(left, bottom1);

                    c0 = new(right + laneStep * 0.2f, center);

                    path.MoveTo(p0);
                    path.LineTo(p1);
                    path.QuadTo(c0, p2);
                    path.LineTo(p3);

                    right -= laneStep * 0.07f;

                    p0 = new(left, top2);
                    p1 = new(right, top2);
                    p2 = new(right, bottom2);
                    p3 = new(left, bottom2);

                    c0 = new(right + laneStep * 0.2f, center);

                    path.LineTo(p3);
                    path.LineTo(p2);
                    path.QuadTo(c0, p1);
                    path.LineTo(p0);
                    path.Close();
                }
                else
                {
                    float left = MarginLeft + offsetPosition * laneStep + laneStep * 0.45f;
                    float right = MarginLeft + (offsetPosition + positionable.Size) * laneStep - laneStep * 0.45f;

                    SKPoint p0 = new(left, top1);
                    SKPoint p1 = new(right, top1);
                    SKPoint p2 = new(right, bottom1);
                    SKPoint p3 = new(left, bottom1);

                    SKPoint c0 = new(left - laneStep * 0.2f, center);
                    SKPoint c1 = new(right + laneStep * 0.2f, center);

                    path.MoveTo(p0);
                    path.LineTo(p1);
                    path.QuadTo(c1, p2);
                    path.LineTo(p3);
                    path.QuadTo(c0, p0);
                    path.Close();

                    left += laneStep * 0.07f;
                    right -= laneStep * 0.07f;
                    p0 = new(left, top2);
                    p1 = new(right, top2);
                    p2 = new(right, bottom2);
                    p3 = new(left, bottom2);

                    c0 = new(left - laneStep * 0.2f, center);
                    c1 = new(right + laneStep * 0.2f, center);

                    path.MoveTo(p1);
                    path.LineTo(p0);
                    path.QuadTo(c0, p3);
                    path.LineTo(p2);
                    path.QuadTo(c1, p1);
                    path.Close();
                }

                canvas.DrawPath(path, NotePaints.GetSyncOutlinePaint(opacity));

                /*float radius0 = radius * SyncOutlineMultiplier[(int)settings.NoteThickness][0];
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

                canvas.DrawPath(path, NotePaints.GetSyncOutlinePaint(opacity));*/
            }
        }

        // Chain Stripes
        if (note is ChainNote && positionable.Size > 2)
        {
            int offsetPosition = positionable.Position < 15
                ? positionable.Position + 45
                : positionable.Position - 15;

            float left = positionable.Size == 60 
                ? MarginLeft 
                : MarginLeft + offsetPosition * laneStep;

            int stripes = positionable.Size * 2;
            SKPath path = new();

            bool wrap = offsetPosition + positionable.Size > 60;

            if (wrap && positionable.Size != 60)
            {
                int startRight = 3;
                int endRight = (60 - offsetPosition) * 2;

                if (startRight < endRight)
                {
                    SKPoint start0 = new(left + (startRight - 1) * laneStep * 0.5f, top);
                    SKPoint start1 = new(left + (startRight - 1) * laneStep * 0.5f, bottom);
                    SKPoint start2 = new(left + (startRight - 1) * laneStep * 0.5f + laneStep * 0.25f, bottom);
                    path.MoveTo(start0);
                    path.LineTo(start1);
                    path.LineTo(start2);
                    
                    for (int i = startRight; i < endRight; i++)
                    {
                        SKPoint p0 = new(left + i * laneStep * 0.5f, top);
                        SKPoint p1 = new(left + i * laneStep * 0.5f - laneStep * 0.25f, top);
                        SKPoint p2 = new(left + i * laneStep * 0.5f, bottom);
                        SKPoint p3 = new(left + i * laneStep * 0.5f + laneStep * 0.25f, bottom);

                        path.MoveTo(p0);
                        path.LineTo(p1);
                        path.LineTo(p2);
                        path.LineTo(p3);
                    }
                    
                    SKPoint end0 = new(left + (endRight) * laneStep * 0.5f, top);
                    SKPoint end1 = new(left + (endRight) * laneStep * 0.5f - laneStep * 0.25f, top);
                    SKPoint end2 = new(left + (endRight) * laneStep * 0.5f, bottom);
                    path.MoveTo(end0);
                    path.LineTo(end1);
                    path.LineTo(end2);
                }

                int startLeft = 1;
                int endLeft = (positionable.Size - (61 - offsetPosition)) * 2;
                
                if (startLeft < endLeft)
                {
                    SKPoint start0 = new(MarginLeft + (startLeft - 1) * laneStep * 0.5f, top);
                    SKPoint start1 = new(MarginLeft + (startLeft - 1) * laneStep * 0.5f, bottom);
                    SKPoint start2 = new(MarginLeft + (startLeft - 1) * laneStep * 0.5f + laneStep * 0.25f, bottom);
                    path.MoveTo(start0);
                    path.LineTo(start1);
                    path.LineTo(start2);
                    
                    for (int i = startLeft; i < endLeft; i++)
                    {
                        SKPoint p0 = new(MarginLeft + i * laneStep * 0.5f, top);
                        SKPoint p1 = new(MarginLeft + i * laneStep * 0.5f - laneStep * 0.25f, top);
                        SKPoint p2 = new(MarginLeft + i * laneStep * 0.5f, bottom);
                        SKPoint p3 = new(MarginLeft + i * laneStep * 0.5f + laneStep * 0.25f, bottom);

                        path.MoveTo(p0);
                        path.LineTo(p1);
                        path.LineTo(p2);
                        path.LineTo(p3);
                    }
                    
                    SKPoint end0 = new(MarginLeft + (endLeft) * laneStep * 0.5f, top);
                    SKPoint end1 = new(MarginLeft + (endLeft) * laneStep * 0.5f - laneStep * 0.25f, top);
                    SKPoint end2 = new(MarginLeft + (endLeft) * laneStep * 0.5f, bottom);
                    path.MoveTo(end0);
                    path.LineTo(end1);
                    path.LineTo(end2);
                }
            }
            else
            {
                int start = positionable.Size == 60 ? 1 : 3;
                int end = positionable.Size == 60 ? stripes : stripes - 2;
                
                SKPoint start0 = new(left + (start - 1) * laneStep * 0.5f, top);
                SKPoint start1 = new(left + (start - 1) * laneStep * 0.5f, bottom);
                SKPoint start2 = new(left + (start - 1) * laneStep * 0.5f + laneStep * 0.25f, bottom);
                path.MoveTo(start0);
                path.LineTo(start1);
                path.LineTo(start2);
                
                for (int i = start; i < end; i++)
                {
                    SKPoint p0 = new(left + i * laneStep * 0.5f, top);
                    SKPoint p1 = new(left + i * laneStep * 0.5f - laneStep * 0.25f, top);
                    SKPoint p2 = new(left + i * laneStep * 0.5f, bottom);
                    SKPoint p3 = new(left + i * laneStep * 0.5f + laneStep * 0.25f, bottom);

                    path.MoveTo(p0);
                    path.LineTo(p1);
                    path.LineTo(p2);
                    path.LineTo(p3);
                }
                
                SKPoint end0 = new(left + (end) * laneStep * 0.5f, top);
                SKPoint end1 = new(left + (end) * laneStep * 0.5f - laneStep * 0.25f, top);
                SKPoint end2 = new(left + (end) * laneStep * 0.5f, bottom);
                path.MoveTo(end0);
                path.LineTo(end1);
                path.LineTo(end2);
            }
            
            canvas.DrawPath(path, NotePaints.GetChainStripePaint(opacity));
        }

        // Bonus Triangles
        if (playable != null && playable.BonusType == BonusType.Bonus)
        {
            SKPath path = new();
            
            int offsetPosition = positionable.Position < 15
                ? positionable.Position + 45
                : positionable.Position - 15;
            
            int count = positionable.Size == 60
                ? positionable.Size
                : positionable.Size - 2;
            int startPosition = positionable.Size == 60
                ? offsetPosition
                : offsetPosition + 1;
            
            for (int i = 0; i < count; i++)
            {
                int x = (startPosition + i) % 60;
                float left = MarginLeft + x * laneStep;
                float right = left + laneStep;

                bool even = i % 2 == 0;

                SKPoint p0 = new(left, bottom);
                SKPoint p1 = new(right, bottom);
                SKPoint p2 = new(even ? left : right, top);

                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
                path.Close();
            }

            canvas.DrawPath(path, NotePaints.GetNoteBonusPaint_2D(settings, colorId, top, bottom, opacity));
        }
        
        // Snap Arrows
        if (note is SnapForwardNote or SnapBackwardNote && positionable.Size > 2)
        {
            bool flip = note is SnapBackwardNote;
            
            int offsetPosition = positionable.Position < 15
                ? positionable.Position + 45
                : positionable.Position - 15;
            
            float depth0 = center - NoteThicknessMultiplier * (flip ?  42.40f : 291.50f);
            float depth1 = center - NoteThicknessMultiplier * (flip ? 107.06f : 238.50f);
            float depth2 = center - NoteThicknessMultiplier * (flip ? 165.36f : 180.20f);
            float depth3 = center - NoteThicknessMultiplier * (flip ? 218.36f : 115.54f);
            float depth4 = center - NoteThicknessMultiplier * (flip ? 129.32f : 209.88f);
            float depth5 = center - NoteThicknessMultiplier * 169.60f;
            float depth6 = center - NoteThicknessMultiplier * (flip ? 248.04f : 84.80f);
            float depth7 = center - NoteThicknessMultiplier * (flip ? 291.50f : 42.40f);
            
            int count = positionable.Size / 3;
            float startPosition = MarginLeft + offsetPosition * laneStep;
            
            int m = positionable.Size % 3;
            
            if (m == 0)
            {
                startPosition += laneStep * 1.5f;
            }
            else if (m == 1)
            {
                startPosition += laneStep * 2f;
            }
            else
            {
                startPosition += laneStep * 2.5f;
            }
            
            SKPath path = new();
            
            generatePoints();

            bool wrap = offsetPosition + positionable.Size > 60;
            if (wrap)
            {
                startPosition -= (canvasInfo.Width - MarginLeft - MarginRight);
                generatePoints();
            }

            SKRect clip = new(MarginLeft, 0, canvasInfo.Width - MarginRight, canvasInfo.Height);
            canvas.Save();
            canvas.ClipRect(clip);
            
            canvas.DrawPath(path, NotePaints.GetSnapFillPaint_2D(settings, colorId, depth0, depth7, opacity, flip));
            
            if (!settings.LowPerformanceMode)
            {
                canvas.DrawPath(path, NotePaints.GetSnapStrokePaint(colorId, 0.25f, opacity));
            }
            
            canvas.Restore();

            void generatePoints()
            {
                for (int i = 0; i < count; i++)
                {
                    float arrowWidth = laneStep * 0.6666666666f;
                    float arrowSpacing = laneStep * 3;
                    float arrowOffset = arrowSpacing * i;

                    float arrowCenter = startPosition + arrowOffset;
                    float left = startPosition + arrowOffset + arrowWidth;
                    float right = startPosition + arrowOffset - arrowWidth;

                    SKPoint p0 = new(arrowCenter, depth0);
                    SKPoint p1 = new(right, depth2);
                    SKPoint p2 = new(right, depth3);
                    SKPoint p3 = new(arrowCenter, depth1);
                    SKPoint p4 = new(left, depth3);
                    SKPoint p5 = new(left, depth2);

                    path.MoveTo(p0);
                    path.LineTo(p1);
                    path.LineTo(p2);
                    path.LineTo(p3);
                    path.LineTo(p4);
                    path.LineTo(p5);
                    path.Close();

                    p0 = new(arrowCenter, depth4);
                    p1 = new(right, depth6);
                    p2 = new(right, depth7);
                    p3 = new(arrowCenter, depth5);
                    p4 = new(left, depth7);
                    p5 = new(left, depth6);

                    path.MoveTo(p0);
                    path.LineTo(p1);
                    path.LineTo(p2);
                    path.LineTo(p3);
                    path.LineTo(p4);
                    path.LineTo(p5);
                    path.Close();
                }
            }
        }
        
        // Slide Arrows
        if (note is SlideClockwiseNote or SlideCounterclockwiseNote)
        {
            bool flip = note is SlideCounterclockwiseNote;
            
            int offsetPosition = positionable.Position < 15
                ? positionable.Position + 45
                : positionable.Position - 15;
            
            float depth0 = center - 50;
            float depth1 = center - 35;
            float depth2 = center - 20;

            float arrowCount = positionable.Size * 0.5f + 1;
            
            float startPosition = MarginLeft + offsetPosition * laneStep;
            SKPath path = new();
            SKPath maskPath = new();
            
            generateMaskPoints();
            generateArrowPoints();
            
            bool wrap = offsetPosition + positionable.Size > 60;
            if (wrap)
            {
                startPosition -= (canvasInfo.Width - MarginLeft - MarginRight);
                generateMaskPoints();
                generateArrowPoints();
            }
            
            SKRect clip = new(MarginLeft, 0, canvasInfo.Width - MarginRight, canvasInfo.Height);
            
            canvas.Save();
            canvas.ClipRect(clip);
            
            canvas.ClipPath(maskPath, SKClipOperation.Intersect, true);
            canvas.DrawPath(path, NotePaints.DebugPaint3);
            canvas.DrawPath(path, NotePaints.GetSlideFillPaint_2D(settings, colorId, opacity));
            
            if (!settings.LowPerformanceMode)
            {
                canvas.DrawPath(path, NotePaints.GetSlideStrokePaint_2D(colorId, opacity));
            }
            
            canvas.Restore();

            void generateMaskPoints()
            {
                // inner side
                float maskPosition;
                float maskDepth;
                SKPoint maskPoint;

                for (int i = 0; i <= positionable.Size; i++)
                {
                    float x = flip
                        ? (float)i / positionable.Size
                        : 1 - (float)i / positionable.Size;

                    maskPosition = startPosition + i * laneStep;
                    maskDepth = depth1 + (depth2 - depth1) * slideArrowMask(x);

                    maskPoint = new(maskPosition, maskDepth);
                    if (i == 0) maskPath.MoveTo(maskPoint);
                    else maskPath.LineTo(maskPoint);
                }

                // center point
                maskPosition = startPosition + positionable.Size * laneStep;
                maskPoint = new(maskPosition, depth1);
                maskPath.LineTo(maskPoint);

                // outer side
                for (int i = positionable.Size; i >= 0; i--)
                {
                    float x = flip
                        ? (float)i / positionable.Size
                        : 1 - (float)i / positionable.Size;

                    maskPosition = startPosition + i * laneStep;
                    maskDepth = depth1 + (depth0 - depth1) * slideArrowMask(x);

                    maskPoint = new(maskPosition, maskDepth);
                    maskPath.LineTo(maskPoint);
                }

                maskPath.Close();
            }

            void generateArrowPoints()
            {
                for (int i = 0; i < arrowCount; i++)
                {
                    //    p0____p1
                    //   /     / 
                    // p5    p2
                    //   \     \
                    //   p4_____p3

                    float position = startPosition + (i * laneStep * 2);
                    float offset = flip ? laneStep : -laneStep;

                    SKPoint p0 = new(position,          depth0);
                    SKPoint p1 = new(position - offset, depth0);
                    SKPoint p2 = new(position,          depth1);
                    SKPoint p3 = new(position - offset, depth2);
                    SKPoint p4 = new(position,          depth2);
                    SKPoint p5 = new(position + offset, depth1);

                    path.MoveTo(p0);
                    path.LineTo(p1);
                    path.LineTo(p2);
                    path.LineTo(p3);
                    path.LineTo(p4);
                    path.LineTo(p5);
                    path.Close();
                }
            }
            
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
            DrawSelectionOutline(canvas, canvasInfo, settings, depth, laneStep, positionable.Position, positionable.Size, selected, pointerOver);
        }
    }
    
    private static void DrawHoldEndNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldPointNote note, float depth, float laneStep, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (depth is <= -0.1f or > 1.25f) return;

        int colorId = (int)settings.HoldNoteColor;
        
        float center = depth * (canvasInfo.Height - JudgementLineOffset);

        int offsetPosition = note.Position < 15
                    ? note.Position + 45
                    : note.Position - 15;

        float startPosition = MarginLeft + offsetPosition * laneStep + laneStep * 0.3f;

        SKRect clip = new(MarginLeft, 0, canvasInfo.Width - MarginRight, canvasInfo.Height);

        canvas.Save();
        canvas.ClipRect(clip);
        
        
        draw();
        
        bool wrap = offsetPosition + note.Size > 60;
        if (wrap)
        {
            startPosition -= (canvasInfo.Width - MarginLeft - MarginRight);
            draw();
        }
        
        canvas.Restore();
        
        // Selection outline.
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, depth, laneStep, note.Position, note.Size, selected, pointerOver);
        }

        return;

        void draw()
        {
            if (note.Size == 60)
            {
                canvas.DrawLine(MarginLeft, center, canvasInfo.Width - MarginRight, center, NotePaints.GetHoldEndBaseStrokePaint_2D(colorId, 0.3f, opacity));

                if (!settings.LowPerformanceMode)
                {
                    float depth0 = center + 3;
                    float depth1 = center - 3;

                    canvas.DrawLine(MarginLeft, depth0, canvasInfo.Width - MarginRight, depth0, NotePaints.GetHoldEndOutlinePaint(colorId, 0.5f, opacity));
                    canvas.DrawLine(MarginLeft, depth1, canvasInfo.Width - MarginRight, depth1, NotePaints.GetHoldEndOutlinePaint(colorId, 0.5f, opacity));
                }
            }
            else
            {
                float right = startPosition + note.Size * laneStep - laneStep * 0.6f;

                if (settings.LowPerformanceMode)
                {
                    canvas.DrawLine(startPosition, center, right, center, NotePaints.GetHoldEndBaseStrokePaint_2D(colorId, 0.5f, opacity));
                }
                else
                {
                    float depth0 = center + 3;
                    float depth1 = center - 3;
                    
                    SKPath path = new();

                    SKPoint p0 = new(startPosition, center);
                    SKPoint p1 = new(startPosition + laneStep * 0.3f, depth0);
                    SKPoint p2 = new(startPosition + laneStep * 0.3f, depth1);
                    
                    if (note.Size == 1)
                    {
                        SKPoint p3 = new(right + laneStep * 0.3f, depth0);
                        SKPoint p4 = new(right + laneStep * 0.3f, depth1);
                        
                        path.MoveTo(p3);
                        path.LineTo(p1);
                        path.LineTo(p0);
                        path.LineTo(p2);
                        path.LineTo(p4);
                    }
                    else
                    {
                        SKPoint p3 = new(right, center);
                        SKPoint p4 = new(right - laneStep * 0.3f, depth0);
                        SKPoint p5 = new(right - laneStep * 0.3f, depth1);

                        path.MoveTo(p0);
                        path.LineTo(p1);
                        path.LineTo(p4);
                        path.LineTo(p3);
                        path.LineTo(p5);
                        path.LineTo(p2);
                        path.Close();
                    }
                    
                    canvas.DrawPath(path, NotePaints.GetHoldEndBaseFillPaint(colorId, opacity));
                    canvas.DrawPath(path, NotePaints.GetHoldEndOutlinePaint(colorId, 0.5f, opacity));
                }
            }
        }
    }
    
    private static void DrawHoldPointNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldPointNote note, float depth, float laneStep, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (depth is <= -0.1f or > 1.25f) return;
        
        float center = depth * (canvasInfo.Height - JudgementLineOffset);

        int offsetPosition = note.Position < 15
                    ? note.Position + 45
                    : note.Position - 15;

        float startPosition = MarginLeft + offsetPosition * laneStep;

        SKRect clip = new(MarginLeft, 0, canvasInfo.Width - MarginRight, canvasInfo.Height);

        canvas.Save();
        canvas.ClipRect(clip);
        
        draw();
        
        bool wrap = offsetPosition + note.Size > 60;
        if (wrap)
        {
            startPosition -= (canvasInfo.Width - MarginLeft - MarginRight);
            draw();
        }
        
        canvas.Restore();
        
        // Selection outline.
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, depth, laneStep, note.Position, note.Size, selected, pointerOver);
        }

        return;

        void draw()
        {
            float left = startPosition + 0.7f * laneStep;
            float right = startPosition + (note.Size * laneStep) - 0.7f * laneStep;
            
            if (settings.LowPerformanceMode)
            {
                canvas.DrawLine(left, center, right, center, NotePaints.GetHoldPointPaint(settings, 0.5f, note.RenderType, opacity));
                
                return;
            }
            
            const float capRadius = 3;
            float depth0 = center - capRadius;
            float depth1 = center + capRadius;
            
            SKPoint capPoint0 = new(left, center);
            SKPoint capPoint1 = new(right, center);
            SKRect capRect0 = new(capPoint0.X - capRadius, depth0, capPoint0.X + capRadius, depth1);
            SKRect capRect1 = new(capPoint1.X - capRadius, depth0, capPoint1.X + capRadius, depth1);
        
            SKPath path = new();

            path.ArcTo(capRect0, 90, 180, true);
            path.ArcTo(capRect1, 270, 180, false);
            path.Close();
        
            canvas.DrawPath(path, NotePaints.GetHoldPointPaint(settings, 0.5f, note.RenderType, opacity));
        }
    }
    
    private static void DrawHoldSurface(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, HoldNote hold, float time, float viewDistance, float laneStep, bool playing, float opacity, bool selected, bool pointerOver)
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
        
        int arcs = 0;
        
        // Generate points for every hold point.
        for (int y = 0; y < points.Count; y++)
        {
            RenderHoldPoint point = new(hold, points[y], 1);
            float scale = SaturnMath.InverseLerp(time + viewDistance, time, point.GlobalTime);

            int position = point.Position - 15;

            if (y == 0 && points.Count > 1)
            {
                int pos0 = points[0].Position;
                int pos1 = points[1].Position;

                int delta = pos0 - pos1;

                if (delta > 30)
                {
                    
                }
                else if (delta < -30)
                {
                    position += 60;
                }
            }
            
            if (y != 0)
            {
                int pos0 = points[y].Position - 15;
                int pos1 = points[y - 1].Position - 15;
                
                int delta = pos0 - pos1;

                if (delta > 30)
                {
                    position += 60;
                }
                else if (delta < -30)
                {
                    position += 60;
                }

                position %= 60;
            }
            
            generatePoint(scale, point.LocalTime, position, point.Size);
        }
        
        // Build mesh
        SKPoint[] triangles = new SKPoint[1 * (arcs - 1) * 6];
        SKPoint[] textureCoords = new SKPoint[1 * (arcs -1) * 6];
        
        int vert = 0;
        int tris = 0;
        for (int y = 0; y < arcs - 1; y++)
        {
            triangles[tris + 0] = vertexScreenCoords[vert];
            triangles[tris + 1] = vertexScreenCoords[vert + 1];
            triangles[tris + 2] = vertexScreenCoords[vert + 2];

            triangles[tris + 5] = vertexScreenCoords[vert + 1];
            triangles[tris + 4] = vertexScreenCoords[vert + 2];
            triangles[tris + 3] = vertexScreenCoords[vert + 3];
            
            textureCoords[tris + 0] = vertexTextureCoords[vert];
            textureCoords[tris + 1] = vertexTextureCoords[vert + 1];
            textureCoords[tris + 2] = vertexTextureCoords[vert + 2];

            textureCoords[tris + 5] = vertexTextureCoords[vert + 1];
            textureCoords[tris + 4] = vertexTextureCoords[vert + 2];
            textureCoords[tris + 3] = vertexTextureCoords[vert + 3];

            tris += 6;
            vert += 2;
        }
        
        bool active = hold.Timestamp.Time < time && playing;
        
        // Draw mesh
        SKRect clip = new(MarginLeft, 0, canvasInfo.Width - MarginRight, canvasInfo.Height);

        canvas.Save();
        canvas.ClipRect(clip);
        
        canvas.DrawVertices(SKVertexMode.Triangles, triangles, textureCoords, null, NotePaints.GetHoldSurfacePaint(active, opacity));
        if (selected || pointerOver)
        {
            canvas.DrawVertices(SKVertexMode.Triangles, triangles, null, null, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
        }
        
        float offset = canvasInfo.Width - MarginLeft - MarginRight;
        canvas.Translate(offset, 0);
        canvas.DrawVertices(SKVertexMode.Triangles, triangles, textureCoords, null, NotePaints.GetHoldSurfacePaint(active, opacity));
        if (selected || pointerOver)
        {
            canvas.DrawVertices(SKVertexMode.Triangles, triangles, null, null, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
        }

        canvas.Translate(offset * -2, 0);
        canvas.DrawVertices(SKVertexMode.Triangles, triangles, textureCoords, null, NotePaints.GetHoldSurfacePaint(active, opacity));
        if (selected || pointerOver)
        {
            canvas.DrawVertices(SKVertexMode.Triangles, triangles, null, null, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
        }
        
        canvas.Restore();
        
        return;
        
        void generatePoint(float scale, float localTime, int position, int size)
        {
            scale = Math.Max(0, scale);
            
            float center = scale * (canvasInfo.Height - JudgementLineOffset);

            float startPosition = MarginLeft + position * laneStep;

            float left = startPosition;
            float right = startPosition + size * laneStep;

            SKPoint screen0 = new(left, center);
            SKPoint screen1 = new(right, center);

            float texX = 512 * ((int)settings.HoldNoteColor + 0.5f) / 13.0f;
            float texY = 512 * (1 - localTime);
            SKPoint tex0 = new(texX, texY);
            SKPoint tex1 = new(texX + 0.01f, texY);

            vertexScreenCoords.Add(screen0);
            vertexScreenCoords.Add(screen1);
            vertexTextureCoords.Add(tex0);
            vertexTextureCoords.Add(tex1);
            
            arcs++;
        }
    }
    
    private static void DrawSyncNote(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, SyncNote note, float depth, float laneStep, float opacity, bool selected, bool pointerOver)
    {
        if (opacity == 0) return;
        if (depth is <= 0 or > 1.25f) return;

        float center = depth * (canvasInfo.Height - JudgementLineOffset);
        float top = center + 10 * NoteThicknessMultiplier;
        float bottom = center - 10 * NoteThicknessMultiplier;
        
        if (note.Size == 60)
        {
            SKRect rect = new(MarginLeft, top, canvasInfo.Width - MarginRight, bottom);
            canvas.DrawRect(rect, NotePaints.GetSyncConnectorPaint_2D(settings, top, bottom, opacity));
        }
        else
        {
            int offsetPosition = note.Position < 15 
            ? note.Position + 45 
            : note.Position - 15;
            bool wrap = offsetPosition + note.Size > 60;

            if (wrap)
            {
                float left1 = MarginLeft + offsetPosition % 60 * laneStep;
                float right1 = canvasInfo.Width - MarginRight;
                
                float left2 = MarginLeft;
                float right2 = MarginLeft + (offsetPosition + note.Size) % 60 * laneStep;
                
                SKRect rect1 = new(left1, top, right1, bottom);
                SKRect rect2 = new(left2, top, right2, bottom);
                canvas.DrawRect(rect1, NotePaints.GetSyncConnectorPaint_2D(settings, top, bottom, opacity));
                canvas.DrawRect(rect2, NotePaints.GetSyncConnectorPaint_2D(settings, top, bottom, opacity));
            }
            else
            {
                float left = MarginLeft + offsetPosition * laneStep;
                float right = MarginLeft + (offsetPosition + note.Size) * laneStep;
                
                SKRect rect = new(left, top, right, bottom);
                canvas.DrawRect(rect, NotePaints.GetSyncConnectorPaint_2D(settings, top, bottom, opacity));
            }
        }
        
        // Selection outline.
        if (selected || pointerOver)
        {
            DrawSelectionOutline(canvas, canvasInfo, settings, depth, laneStep, note.Position, note.Size, selected, pointerOver);
        }
    }
    
    /// <summary>
    /// Draws a selection outline.
    /// </summary>
    private static void DrawSelectionOutline(SKCanvas canvas, CanvasInfo canvasInfo, RenderSettings settings, float depth, float laneStep, int position, int size, bool selected, bool pointerOver)
    {
        float top = depth * (canvasInfo.Height - JudgementLineOffset) + SyncOutlineOffset[(int)settings.NoteThickness][4] * NoteThicknessMultiplier;
        float bottom = depth * (canvasInfo.Height - JudgementLineOffset) + SyncOutlineOffset[(int)settings.NoteThickness][5] * NoteThicknessMultiplier;
        
        SKPath path = new();

        if (size == 60)
        {
            SKRect rect = new(MarginLeft, bottom, canvasInfo.Width - MarginRight, top);
            path.AddRect(rect);
        }
        else
        {
            int offsetPosition = position < 15 
            ? position + 45 
            : position - 15;
            bool wrap = offsetPosition + size > 60;
            
            const float capDiameter = 9;

            if (wrap)
            {
                float left1 = MarginLeft + offsetPosition % 60 * laneStep;
                float right1 = canvasInfo.Width - MarginRight;
                
                float left2 = MarginLeft;
                float right2 = MarginLeft + (offsetPosition + size) % 60 * laneStep;
                
                SKRect rectTopLeft = new(left1, top, left1 + capDiameter, top + capDiameter);
                SKRect rectBottomLeft = new(left1, bottom - capDiameter, left1 + capDiameter, bottom);
                
                SKRect rectTopRight = new(right2 - capDiameter, top, right2, top + capDiameter);
                SKRect rectBottomRight = new(right2 - capDiameter, bottom - capDiameter, right2, bottom);
                
                path.ArcTo(rectTopLeft, 180, 90, true);
                path.LineTo(right1, top);
                path.LineTo(right1, bottom);
                path.ArcTo(rectBottomLeft, 90, 90, false);
                path.Close();
                
                path.ArcTo(rectTopRight, 270, 90, true);
                path.ArcTo(rectBottomRight, 0, 90, false);
                path.LineTo(left2, bottom);
                path.LineTo(left2, top);
                path.Close();
            }
            else
            {
                float left = MarginLeft + offsetPosition * laneStep;
                float right = left + size * laneStep;
                
                SKRect rectTopLeft = new(left, top, left + capDiameter, top + capDiameter);
                SKRect rectTopRight = new(right - capDiameter, top, right, top + capDiameter);
                SKRect rectBottomLeft = new(left, bottom - capDiameter, left + capDiameter, bottom);
                SKRect rectBottomRight = new(right - capDiameter, bottom - capDiameter, right, bottom);
                
                path.ArcTo(rectTopLeft, 180, 90, true);
                path.ArcTo(rectTopRight, 270, 90, false);
                path.ArcTo(rectBottomRight, 0, 90, false);
                path.ArcTo(rectBottomLeft, 90, 90, false);
                path.Close();
            }
        }

        canvas.DrawPath(path, NotePaints.GetObjectOutlineFillPaint(selected, pointerOver));
        canvas.DrawPath(path, NotePaints.GetObjectOutlineStrokePaint(selected, pointerOver));
    }
    
    private static void DrawBoxSelect(SKCanvas canvas, CanvasInfo canvasInfo, float time, float viewDistance, RenderBoxSelectData renderBoxSelect, float laneStep)
    {
        if (renderBoxSelect.StartTime == null) return;
        if (renderBoxSelect.EndTime == null) return;
        if (renderBoxSelect.Position == null) return;
        if (renderBoxSelect.Size == null) return;
        
        RenderUtils.GetProgress(renderBoxSelect.StartTime.Value, 0, false, viewDistance, time, 0, out float d0);
        RenderUtils.GetProgress(renderBoxSelect.EndTime.Value, 0, false, viewDistance, time, 0, out float d1);

        int offsetPosition = renderBoxSelect.Position.Value < 15 
            ? renderBoxSelect.Position.Value + 45 
            : renderBoxSelect.Position.Value - 15;
        bool wrap = offsetPosition + renderBoxSelect.Size.Value > 60;
        
        float bottom = d0 * (canvasInfo.Height - JudgementLineOffset);
        float top = d1 * (canvasInfo.Height - JudgementLineOffset);
        

        if (renderBoxSelect.Size == 60)
        {
            SKRect rect = new(MarginLeft, top, canvasInfo.Width - MarginRight, bottom);
            canvas.DrawRect(rect, NotePaints.GetObjectOutlineFillPaint(true, false));
            canvas.DrawRect(rect, NotePaints.GetObjectOutlineStrokePaint(true, false));
        }
        else if (wrap)
        {
            float left1 = MarginLeft + offsetPosition % 60 * laneStep;
            float right1 = canvasInfo.Width - MarginRight;
            float left2 = MarginLeft;
            float right2 = MarginLeft + (offsetPosition + renderBoxSelect.Size.Value) % 60 * laneStep;

            SKRect rect1 = new(left1, top, right1, bottom);
            SKRect rect2 = new(left2, top, right2, bottom);
            
            canvas.DrawRect(rect1, NotePaints.GetObjectOutlineFillPaint(true, false));
            canvas.DrawRect(rect2, NotePaints.GetObjectOutlineFillPaint(true, false));
            canvas.DrawRect(rect1, NotePaints.GetObjectOutlineStrokePaint(true, false));
            canvas.DrawRect(rect2, NotePaints.GetObjectOutlineStrokePaint(true, false));
        }
        else
        {
            float left = MarginLeft + offsetPosition * laneStep;
            float right = left + renderBoxSelect.Size.Value * laneStep;
            
            SKRect rect = new(left, top, right, bottom);
            canvas.DrawRect(rect, NotePaints.GetObjectOutlineFillPaint(true, false));
            canvas.DrawRect(rect, NotePaints.GetObjectOutlineStrokePaint(true, false));
        }
    }
}