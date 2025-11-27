using SaturnData.Notation.Notes;

namespace SaturnView;

internal struct RenderHoldPoint
{
    internal RenderHoldPoint(HoldNote hold, HoldPointNote point, int maxSize)
    {
        GlobalTime = point.Timestamp.Time;
        GlobalScaledTime = point.Timestamp.ScaledTime;

        LocalTime = hold.Points.Count > 1 && hold.Points[0].Timestamp.Time != hold.Points[^1].Timestamp.Time
            ? (point.Timestamp.Time - hold.Points[0].Timestamp.Time) / (hold.Points[^1].Timestamp.Time - hold.Points[0].Timestamp.Time)
            : 0;
        
        StartAngle = point.Size == 60 
            ? point.Position * -6f 
            : point.Position * -6f - 4.2f;
        
        IntervalAngle = point.Size == 60 
            ? point.Size * -6f / maxSize 
            : (point.Size * -6f + 8.4f) / maxSize;

        Position = point.Position;
        Size = point.Size;
    }

    public float GlobalTime;
    public float GlobalScaledTime;
    public float LocalTime;
    public float StartAngle;
    public float IntervalAngle;
    public int Position;
    public int Size;
}