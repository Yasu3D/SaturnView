using SaturnData.Notation.Events;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SkiaSharp;

namespace SaturnView;

internal static class RenderUtils
{
    /// <summary>
    /// Determines if an object should be visible depending on the render settings.
    /// </summary>
    internal static bool IsVisible(ITimeable obj, RenderSettings settings)
    {
        if (obj is EffectSubEvent subEvent)
        {
            return subEvent.Parent switch
            {
                StopEffectEvent => settings.ShowStopEffectEvents,
                ReverseEffectEvent => settings.ShowReverseEffectEvents,
                _ => true,
            };
        }

        return obj switch
        {
            TempoChangeEvent => settings.ShowTempoChangeEvents,
            MetreChangeEvent => settings.ShowMetreChangeEvents,
            TutorialMarkerEvent => settings.ShowTutorialMarkerEvents,
            
            SpeedChangeEvent => settings.ShowSpeedChangeEvents,
            VisibilityChangeEvent => settings.ShowVisibilityChangeEvents,
            StopEffectEvent => settings.ShowStopEffectEvents,
            ReverseEffectEvent => settings.ShowReverseEffectEvents,

            TouchNote => settings.ShowTouchNotes,
            ChainNote => settings.ShowChainNotes,
            HoldNote => settings.ShowHoldNotes,
            HoldPointNote => settings.ShowHoldNotes,
            SlideClockwiseNote => settings.ShowSlideClockwiseNotes,
            SlideCounterclockwiseNote => settings.ShowSlideCounterclockwiseNotes,
            SnapForwardNote => settings.ShowSnapForwardNotes,
            SnapBackwardNote => settings.ShowSnapBackwardNotes,

            SyncNote => settings.ShowSyncNotes,
            MeasureLineNote => settings.ShowMeasureLineNotes,

            LaneShowNote => settings.ShowLaneShowNotes,
            LaneHideNote => settings.ShowLaneHideNotes,
            _ => true,
        };
    }

    internal static bool GetProgress(ITimeable obj, bool showSpeedChanges, float viewDistance, float time, float scaledTime, out float progress)
    {
        progress = -1;

        if (obj.Timestamp.Time < time) return false;
        
        if (showSpeedChanges)
        {
            if (obj.Timestamp.ScaledTime < scaledTime) return false;
            if (obj.Timestamp.ScaledTime > scaledTime + viewDistance) return false;
        }
        else
        {
            if (obj.Timestamp.Time > time + viewDistance) return false;
        }

        progress = showSpeedChanges
            ? 1 - (obj.Timestamp.ScaledTime - scaledTime) / viewDistance
            : 1 - (obj.Timestamp.Time - time) / viewDistance;

        if (progress is < 0 or > 1) return false;
        
        return true;
    }

    internal static float GetSweepDuration(LaneSweepDirection direction, int size)
    {
        return direction switch
        {
            LaneSweepDirection.Counterclockwise => size * 8.3333333f,
            LaneSweepDirection.Clockwise => size * 8.3333333f,
            LaneSweepDirection.Center => size * 4.1666666f,
            LaneSweepDirection.Instant => 0,
            _ => 0,
        };
    }
    
    /// <summary>
    /// Applies perspective distortion to a linear scale value.
    /// </summary>
    internal static float Perspective(float x)
    {
        // Huge thanks to CG505 for figuring out the perspective math:
        // https://www.desmos.com/calculator/9a0srmgktj
        x = Math.Min(1.3f, x);
        return 3.325f * x / (13.825f - 10.5f * x);
    }

    /// <summary>
    /// Linearly interpolates between <c>a</c> and <c>b</c>, controlled by <c>t</c>
    /// </summary>
    internal static float Lerp(float a, float b, float t) => a + t * (b - a);
    
    /// <summary>
    /// Linearly interpolates between <c>a</c> and <c>b</c>, controlled by <c>t</c>, cycled by <c>m</c>
    /// </summary>
    internal static float LerpCyclic(float a, float b, float t, float m)
    {
        if (Math.Abs(a - b) <= m * 0.5f) return a + t * (b - a);
        
        if (a > b) b += m;
        else a += m;

        return a + t * (b - a);
    }
    
    /// <summary>
    /// Returns where <c>x</c> lies between <c>a</c> and <c>b</c>.
    /// </summary>
    internal static float InverseLerp(float a, float b, float x) => a == b ? 0 : (x - a) / (b - a);
    
    /// <summary>
    /// Returns a point on an imaginary arc/circle.
    /// </summary>
    /// <param name="center">The center of the arc.</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <param name="angle">The angle of the point.</param>
    /// <returns></returns>
    internal static SKPoint PointOnArc(SKPoint center, float radius, float angle)
    {
        return new
        (
            (float)(radius * Math.Cos(angle * Math.PI / 180.0)) + center.X,
            (float)(radius * Math.Sin(angle * Math.PI / 180.0)) + center.Y
        );
    }
}