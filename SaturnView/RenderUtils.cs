using SaturnData.Notation.Core;
using SaturnData.Notation.Events;
using SaturnData.Notation.Interfaces;
using SaturnData.Notation.Notes;
using SkiaSharp;

namespace SaturnView;

public static class RenderUtils
{
    /// <summary>
    /// Returns if an object should be visible depending on the render settings.
    /// </summary>
    public static bool IsVisible(ITimeable obj, RenderSettings settings, ITimeable? activeObjectGroup)
    {
        if (obj is EffectSubEvent subEvent)
        {
            if (subEvent.Parent == activeObjectGroup) return true;
            
            return subEvent.Parent switch
            {
                StopEffectEvent => settings.ShowStopEffectEvents,
                ReverseEffectEvent => settings.ShowReverseEffectEvents,
                _ => true,
            };
        }

        if (obj is HoldPointNote holdPointNote)
        {
            if (holdPointNote.Parent == activeObjectGroup) return true;

            return settings.ShowHoldNotes;
        }
        
        if (activeObjectGroup != null)
        {
            return obj == activeObjectGroup;
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
            SlideClockwiseNote => settings.ShowSlideClockwiseNotes,
            SlideCounterclockwiseNote => settings.ShowSlideCounterclockwiseNotes,
            SnapForwardNote => settings.ShowSnapForwardNotes,
            SnapBackwardNote => settings.ShowSnapBackwardNotes,

            SyncNote => settings.ShowSyncNotes,
            MeasureLineNote measureLineNote => measureLineNote.IsBeatLine ? settings.ShowBeatLineNotes : settings.ShowMeasureLineNotes,

            LaneShowNote => settings.ShowLaneShowNotes,
            LaneHideNote => settings.ShowLaneHideNotes,
            
            Bookmark => settings.ShowBookmarks,
            _ => true,
        };
    }
    
    /// <summary>
    /// Returns if an object is within the currently visible area, and returns its linear scroll position as an <c>out</c> value.
    /// </summary>
    internal static bool GetProgress(float objTime, float objScaledTime, bool showSpeedChanges, float viewDistance, float time, float scaledTime, out float progress)
    {
        progress = showSpeedChanges
            ? 1 - (objScaledTime - scaledTime) / viewDistance
            : 1 - (objTime - time) / viewDistance;

        if (objTime < time) return false;
        if (progress is < 0 or > 1.25f) return false;
        
        return true;
    }
    
    /// <summary>
    /// Applies perspective distortion to a linear scale value.
    /// </summary>
    public static float Perspective(float x)
    {
        // Huge thanks to CG505 for figuring out the perspective math:
        // https://www.desmos.com/calculator/9a0srmgktj
        x = Math.Min(1.3f, x);
        return 3.325f * x / (13.825f - 10.5f * x);
    }
    
    /// <summary>
    /// Reverts a perspective distorted value back to to a linear scale value.
    /// </summary>
    public static float InversePerspective(float x)
    {
        return 13.825f * x / (10.5f * x + 3.325f);
    }
    
    /// <summary>
    /// Returns a point on an imaginary arc/circle.
    /// </summary>
    /// <param name="center">The center of the arc.</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <param name="angle">The angle of the point.</param>
    /// <returns></returns>
    public static SKPoint PointOnArc(SKPoint center, float radius, float angle) => PointOnArc(center.X, center.Y, radius, angle);

    /// <summary>
    /// Returns a point on an imaginary arc/circle.
    /// </summary>
    /// <param name="x">The x-coordinate of the center of the arc.</param>
    /// <param name="y">The y-coordinate of the center of the arc.</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <param name="angle">The angle of the point.</param>
    /// <returns></returns>
    public static SKPoint PointOnArc(float x, float y, float radius, float angle)
    {
        return new
        (
            (float)(radius * Math.Cos(angle * Math.PI / 180.0)) + x,
            (float)(radius * Math.Sin(angle * Math.PI / 180.0)) + y
        );
    }
}