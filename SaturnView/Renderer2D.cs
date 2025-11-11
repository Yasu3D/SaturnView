using SaturnData.Notation.Events;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SaturnData.Utilities;
using SkiaSharp;

namespace SaturnView;

public static class Renderer2D
{
    public static void Render(SKCanvas canvas, CanvasInfo canvasInfo)
    {
        canvas.Clear(canvasInfo.BackgroundColor);
        canvas.DrawPoint(canvasInfo.Center, new SKPaint { Color = SKColors.Red, StrokeWidth = 10 } );
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
        float depth = GetHitTestPointerDepth(canvasInfo, x, y);
        int lane = GetHitTestPointerLane(canvasInfo, x, y);
        
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
        return ((NotePaints.NoteStrokeWidths[(int)noteThickness] * canvasInfo.Scale) / canvasInfo.Radius) * 0.5f;
    }

    /// <summary>
    /// Returns the 0-1 depth of a pixel coordinate.
    /// </summary>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="x">The x-coordinate in pixels</param>
    /// <param name="y">The y-coordinate in pixels</param>
    public static float GetHitTestPointerDepth(CanvasInfo canvasInfo, float x, float y)
    {
        // TODO: Implement
        return 0;
        /*x =  (x - canvasInfo.Radius) / canvasInfo.JudgementLineRadius;
        y =  (y - canvasInfo.Radius) / canvasInfo.JudgementLineRadius;
        
        return MathF.Sqrt(x * x + y * y);*/
    }

    /// <summary>
    /// Returns the 0-59 lane position of a pixel coordinate.
    /// </summary>
    /// <param name="canvasInfo">The CanvasInfo of the canvas to hit test on.</param>
    /// <param name="x">The x-coordinate in pixels</param>
    /// <param name="y">The y-coordinate in pixels</param>
    public static int GetHitTestPointerLane(CanvasInfo canvasInfo, float x, float y)
    {
        // TODO: Implement
        return 0;
        /*x =  (x - canvasInfo.Radius) / canvasInfo.JudgementLineRadius;
        y =  (y - canvasInfo.Radius) / canvasInfo.JudgementLineRadius;
        
        float angle = MathF.Atan2(y, x) / MathF.PI * 180 + 180;
        return (int)((90 - angle / 6) % 60);*/
    }
}