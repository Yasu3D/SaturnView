namespace SaturnView;

public struct RenderBoxSelectData(float? startTime, float? endTime, int? position, int? size)
{
    public readonly float? StartTime = startTime;
    public readonly float? EndTime = endTime;
    public readonly int? Position = position;
    public readonly int? Size = size;
}