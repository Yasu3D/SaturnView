namespace SaturnView;

[Serializable]
public class RenderSettings
{
    public event EventHandler? PropertyChanged;
    
#region Enum Definitions

    /// <summary>
    /// The list of available note colors.
    /// </summary>
    public enum NoteColorOption
    {
        LightMagenta = 0,
        LightYellow = 1,
        Orange = 2,
        Lime = 3,
        Red = 4,
        SkyBlue = 5,
        DarkYellow = 6,
        LightRed = 7,
        Yellow = 8,
        PureGreen = 9,
        BrightBlue = 10,
        LightBlue = 11,
        LightGray = 12,
    }

    /// <summary>
    /// The list of available guideline types.
    /// </summary>
    public enum GuideLineTypeOption
    {
        A = 0,
        B = 1,
        C = 2,
        D = 3,
        E = 4,
        F = 5,
        G = 6,
        None = 7,
    }

    /// <summary>
    /// The list of available background dim levels.
    /// </summary>
    public enum BackgroundDimOption
    {
        NoDim = 0,
        Plus1 = 1,
        Plus2 = 2,
        Plus3 = 3,
        Plus4 = 4,
    }

    /// <summary>
    /// The list of available note thicknesses.
    /// </summary>
    public enum NoteThicknessOption
    {
        Thickness1 = 0,
        Thickness2 = 1,
        Thickness3 = 2,
        Thickness4 = 3,
        Thickness5 = 4,
    }
    
#endregion Enum Definitions

    /// <summary>
    /// How many times per second the viewport is updated.
    /// </summary>
    public int RefreshRate
    {
        get => refreshRate;
        set
        {
            if (refreshRate == value) return;
            
            refreshRate = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private int refreshRate = 60;

    /// <summary>
    /// Simplifies chart visuals to lower the performance cost.
    /// </summary>
    public bool LowPerformanceMode
    {
        get => lowPerformanceMode;
        set
        {
            if (lowPerformanceMode == value) return;
            
            lowPerformanceMode = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool lowPerformanceMode = false;
    
    /// <summary>
    /// The note speed to render the chart with. (Game-Accurate)
    /// </summary>
    public int NoteSpeed
    {
        get => noteSpeed;
        set
        {
            if (noteSpeed == value) return;
            
            noteSpeed = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private int noteSpeed = 30;
    
    /// <summary>
    /// The thickness of notes.
    /// </summary>
    public NoteThicknessOption NoteThickness
    {
        get => noteThickness;
        set
        {
            if (noteThickness == value) return;
            
            noteThickness = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private NoteThicknessOption noteThickness = NoteThicknessOption.Thickness3;
    
    /// <summary>
    /// Should lane toggle animations be ignored?
    /// </summary>
    public bool IgnoreLaneToggleAnimations
    {
        get => ignoreLaneToggleAnimations;
        set 
        {
            ignoreLaneToggleAnimations = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool ignoreLaneToggleAnimations = false;
    
    /// <summary>
    /// Should event markers be hidden during playback?
    /// </summary>
    public bool HideEventMarkersDuringPlayback
    {
        get => hideEventMarkersDuringPlayback;
        set 
        {
            hideEventMarkersDuringPlayback = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool hideEventMarkersDuringPlayback = true;
    
    /// <summary>
    /// Should lane toggle notes be hidden during playback?
    /// </summary>
    public bool HideLaneToggleNotesDuringPlayback
    {
        get => hideLaneToggleNotesDuringPlayback;
        set 
        {
            hideLaneToggleNotesDuringPlayback = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool hideLaneToggleNotesDuringPlayback = true;
    
    /// <summary>
    /// Should hold note control points be hidden during playback?
    /// </summary>
    public bool HideHoldControlPointsDuringPlayback
    {
        get => hideHoldControlPointsDuringPlayback;
        set 
        {
            hideHoldControlPointsDuringPlayback = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool hideHoldControlPointsDuringPlayback = true;
    
    /// <summary>
    /// The color of Touch notes.
    /// </summary>
    public NoteColorOption TouchNoteColor
    {
        get => touchNoteColor;
        set
        {
            if (touchNoteColor == value) return;
            
            touchNoteColor = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private NoteColorOption touchNoteColor = NoteColorOption.LightMagenta;
    
    /// <summary>
    /// The color of Chain notes.
    /// </summary>
    public NoteColorOption ChainNoteColor
    {
        get => chainNoteColor;
        set
        {
            if (chainNoteColor == value) return;
            
            chainNoteColor = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private NoteColorOption chainNoteColor = NoteColorOption.LightYellow;
    
    /// <summary>
    /// The color of Hold notes.
    /// </summary>
    public NoteColorOption HoldNoteColor
    {
        get => holdNoteColor;
        set
        {
            if (holdNoteColor == value) return;
            
            holdNoteColor = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private NoteColorOption holdNoteColor = NoteColorOption.DarkYellow;
    
    /// <summary>
    /// The color of Slide Clockwise notes.
    /// </summary>
    public NoteColorOption SlideClockwiseNoteColor
    {
        get => slideClockwiseNoteColor;
        set
        {
            if (slideClockwiseNoteColor == value) return;
            
            slideClockwiseNoteColor = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private NoteColorOption slideClockwiseNoteColor = NoteColorOption.Orange;
    
    /// <summary>
    /// The color of Slide Counterclockwise notes.
    /// </summary>
    public NoteColorOption SlideCounterclockwiseNoteColor
    {
        get => slideCounterclockwiseNoteColor;
        set
        {
            if (slideCounterclockwiseNoteColor == value) return;
            
            slideCounterclockwiseNoteColor = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private NoteColorOption slideCounterclockwiseNoteColor = NoteColorOption.Lime;

    /// <summary>
    /// The color of Snap Forward notes.
    /// </summary>
    public NoteColorOption SnapForwardNoteColor
    {
        get => snapForwardNoteColor;
        set
        {
            if (snapForwardNoteColor == value) return;
            
            snapForwardNoteColor = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private NoteColorOption snapForwardNoteColor = NoteColorOption.Red;
    
    /// <summary>
    /// The color of Snap Backward notes.
    /// </summary>
    public NoteColorOption SnapBackwardNoteColor
    {
        get => snapBackwardNoteColor;
        set
        {
            if (snapBackwardNoteColor == value) return;
            
            snapBackwardNoteColor = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private NoteColorOption snapBackwardNoteColor = NoteColorOption.SkyBlue;
    
    /// <summary>
    /// The density of guidelines.
    /// </summary>
    public GuideLineTypeOption GuideLineType
    {
        get => guideLineType;
        set
        {
            if (guideLineType == value) return;
            
            guideLineType = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private GuideLineTypeOption guideLineType;
    
    /// <summary>
    /// The amount to dim the background by.
    /// </summary>
    public BackgroundDimOption BackgroundDim
    {
        get => backgroundDim;
        set
        {
            if (backgroundDim == value) return;
            
            backgroundDim = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private BackgroundDimOption backgroundDim = BackgroundDimOption.NoDim;
    
    /// <summary>
    /// Should judgement windows be shown?
    /// </summary>
    public bool ShowJudgementWindows
    {
        get => showJudgementWindows;
        set
        {
            if (showJudgementWindows == value) return;
            
            showJudgementWindows = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showJudgementWindows = true;


    /// <summary>
    /// Should MARVELOUS judgement windows be shown?
    /// </summary>
    public bool ShowMarvelousWindows
    {
        get => showMarvelousWindows;
        set
        {
            if (showMarvelousWindows == value) return;
            
            showMarvelousWindows = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showMarvelousWindows = true;

    /// <summary>
    /// Should GREAT judgement windows be shown?
    /// </summary>
    public bool ShowGreatWindows
    {
        get => showGreatWindows;
        set
        {
            if (showGreatWindows == value) return;
            
            showGreatWindows = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showGreatWindows = true;
    
    /// <summary>
    /// Should GOOD judgement windows be shown?
    /// </summary>
    public bool ShowGoodWindows
    {
        get => showGoodWindows;
        set
        {
            if (showGoodWindows == value) return;
            
            showGoodWindows = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showGoodWindows = true;
    
    /// <summary>
    /// Should judgement windows follow the Saturn specifications?<br/>
    /// (Slight engine changes to prevent "splashing")
    /// </summary>
    public bool SaturnJudgementWindows
    {
        get => saturnJudgementWindows;
        set
        {
            if (saturnJudgementWindows == value) return;
            
            saturnJudgementWindows = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool saturnJudgementWindows = true;
    
    /// <summary>
    /// Should hold note detection windows be visualized?
    /// </summary>
    public bool VisualizeHoldNoteWindows
    {
        get => visualizeHoldNoteWindows;
        set
        {
            if (visualizeHoldNoteWindows == value) return;
            
            visualizeHoldNoteWindows = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool visualizeHoldNoteWindows = true;
    
    /// <summary>
    /// Should the sweep animations of lane toggles be visualized?
    /// </summary>
    public bool VisualizeSweepAnimations
    {
        get => visualizeSweepAnimations;
        set
        {
            if (visualizeSweepAnimations == value) return;
            
            visualizeSweepAnimations = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool visualizeSweepAnimations = true;
    
    /// <summary>
    /// Should Touch notes be shown?
    /// </summary>
    public bool ShowTouchNotes
    {
        get => showTouchNotes;
        set
        {
            if (showTouchNotes == value) return;
            
            showTouchNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showTouchNotes = true;
    
    /// <summary>
    /// Should Chain notes be shown?
    /// </summary>
    public bool ShowChainNotes
    {
        get => showChainNotes;
        set
        {
            if (showChainNotes == value) return;
            
            showChainNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showChainNotes = true;
    
    /// <summary>
    /// Should Hold notes be shown?
    /// </summary>
    public bool ShowHoldNotes
    {
        get => showHoldNotes;
        set
        {
            if (showHoldNotes == value) return;
            
            showHoldNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showHoldNotes = true;

    /// <summary>
    /// Should Slide Clockwise notes be shown?
    /// </summary>
    public bool ShowSlideClockwiseNotes
    {
        get => showSlideClockwiseNotes;
        set
        {
            if (showSlideClockwiseNotes == value) return;
            
            showSlideClockwiseNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showSlideClockwiseNotes = true;
    
    /// <summary>
    /// Should Slide Counterclockwise notes be shown?
    /// </summary>
    public bool ShowSlideCounterclockwiseNotes
    {
        get => showSlideCounterclockwiseNotes;
        set
        {
            if (showSlideCounterclockwiseNotes == value) return;
            
            showSlideCounterclockwiseNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showSlideCounterclockwiseNotes = true;
    
    /// <summary>
    /// Should Snap Forward notes be shown?
    /// </summary>
    public bool ShowSnapForwardNotes
    {
        get => showSnapForwardNotes;
        set
        {
            if (showSnapForwardNotes == value) return;
            
            showSnapForwardNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showSnapForwardNotes = true;
    
    /// <summary>
    /// Should Snap Backward notes be shown?
    /// </summary>
    public bool ShowSnapBackwardNotes
    {
        get => showSnapBackwardNotes;
        set
        {
            if (showSnapBackwardNotes == value) return;
            
            showSnapBackwardNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showSnapBackwardNotes = true;
    
    /// <summary>
    /// Should Lane Show notes be shown?
    /// </summary>
    public bool ShowLaneShowNotes
    {
        get => showLaneShowNotes;
        set 
        {
            showLaneShowNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showLaneShowNotes = true;
    
    /// <summary>
    /// Should Lane Hide notes be shown?
    /// </summary>
    public bool ShowLaneHideNotes
    {
        get => showLaneHideNotes;
        set 
        {
            showLaneHideNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showLaneHideNotes = true;
    
    /// <summary>
    /// Should Tempo changes be shown?
    /// </summary>
    public bool ShowTempoChanges
    {
        get => showTempoChanges;
        set 
        {
            showTempoChanges = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showTempoChanges = true;
    
    /// <summary>
    /// Should Metre changes be shown?
    /// </summary>
    public bool ShowMetreChanges
    {
        get => showMetreChanges;
        set 
        {
            showMetreChanges = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showMetreChanges = true;
    
    /// <summary>
    /// Should Speed changes be shown?
    /// </summary>
    public bool ShowSpeedChanges
    {
        get => showSpeedChanges;
        set 
        {
            showSpeedChanges = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showSpeedChanges = true;
    
    /// <summary>
    /// Should Visibility changes be shown?
    /// </summary>
    public bool ShowVisibilityChanges
    {
        get => showVisibilityChanges;
        set 
        {
            showVisibilityChanges = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showVisibilityChanges = true;
    
    /// <summary>
    /// Should Reverse effects be shown?
    /// </summary>
    public bool ShowReverseEffects
    {
        get => showReverseEffects;
        set 
        {
            showReverseEffects = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showReverseEffects = true;
    
    /// <summary>
    /// Should Stop effects be shown?
    /// </summary>
    public bool ShowStopEffects
    {
        get => showStopEffects;
        set 
        {
            showStopEffects = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showStopEffects = true;
    
    /// <summary>
    /// Should Tutorial tags be shown?
    /// </summary>
    public bool ShowTutorialMarkers
    {
        get => showTutorialMarkers;
        set 
        {
            showTutorialMarkers = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showTutorialMarkers = true;
}