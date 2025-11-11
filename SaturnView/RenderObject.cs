using SaturnData.Notation.Core;
using SaturnData.Notation.Interfaces;

namespace SaturnView;

internal struct RenderObject(ITimeable @object, Layer? layer, int? layerIndex, float scale, bool sync, bool isVisible)
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