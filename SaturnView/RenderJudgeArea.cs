namespace SaturnView;

internal struct RenderJudgeArea(int position, int size, float noteScale, float marvelousEarlyScale, float marvelousLateScale, float greatEarlyScale, float greatLateScale, float goodEarlyScale, float goodLateScale)
{
    public readonly int Position = position;
    public readonly int Size = size;
    public readonly float NoteScale = noteScale;
    public readonly float MarvelousEarlyScale = marvelousEarlyScale;
    public readonly float MarvelousLateScale = marvelousLateScale;
    public readonly float GreatEarlyScale = greatEarlyScale;
    public readonly float GreatLateScale = greatLateScale;
    public readonly float GoodEarlyScale = goodEarlyScale;
    public readonly float GoodLateScale = goodLateScale;
}