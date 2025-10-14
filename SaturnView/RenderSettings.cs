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

    /// <summary>
    /// The list of available judgement line colors.
    /// </summary>
    public enum JudgementLineColorOption
    {
        Version0 = 0,
        Version1 = 1,
        Version2 = 2,
        Version3 = 3,
    }

    /// <summary>
    /// The list of available effect visibility options.
    /// </summary>
    public enum EffectVisibilityOption
    {
        AlwaysOn = 0,
        AlwaysOff = 1,
        OnlyWhenPlaying = 2,
        OnlyWhenPaused = 3,
    }

    /// <summary>
    /// The list of available clear background visibility options.
    /// </summary>
    public enum ClearBackgroundVisibilityOption
    {
        ForceNormal = 0,
        ForceClear = 1,
        SimulateClear = 2,
    }
    
#endregion Enum Definitions

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
    /// The color of the judgement line.
    /// </summary>
    public JudgementLineColorOption JudgementLineColor
    {
        get => judgementLineColor;
        set
        {
            if (judgementLineColor == value) return;
            
            judgementLineColor = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private JudgementLineColorOption judgementLineColor = JudgementLineColorOption.Version3;
    
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
    /// Should lane toggle animations be shown?
    /// </summary>
    public bool ShowLaneToggleAnimations
    {
        get => showLaneToggleAnimations;
        set 
        {
            showLaneToggleAnimations = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showLaneToggleAnimations = true;
    
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
    /// The opacity of "hidden" objects.
    /// </summary>
    public int HiddenOpacity
    {
        get => hiddenOpacity;
        set
        {
            if (hiddenOpacity == value) return;
            
            hiddenOpacity = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private int hiddenOpacity = 1;
    
    /// <summary>
    /// Should scroll speed changes be visible?
    /// </summary>
    public bool ShowSpeedChanges
    {
        get => showSpeedChanges;
        set
        {
            if (showSpeedChanges == value) return;
            
            showSpeedChanges = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showSpeedChanges = true;
    
    /// <summary>
    /// Should visibility changes be visible?
    /// </summary>
    public bool ShowVisibilityChanges
    {
        get => showVisibilityChanges;
        set
        {
            if (showVisibilityChanges == value) return;
            
            showVisibilityChanges = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showVisibilityChanges = true;
    
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
    public bool VisualizeLaneSweeps
    {
        get => visualizeLaneSweeps;
        set
        {
            if (visualizeLaneSweeps == value) return;
            
            visualizeLaneSweeps = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool visualizeLaneSweeps = true;
    
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
    /// Should Sync notes be shown?
    /// </summary>
    public bool ShowSyncNotes
    {
        get => showSyncNotes;
        set 
        {
            if (showSyncNotes == value) return;
            
            showSyncNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showSyncNotes = true;
    
    /// <summary>
    /// Should Measure Line notes be shown?
    /// </summary>
    public bool ShowMeasureLineNotes
    {
        get => showMeasureLineNotes;
        set 
        {
            if (showMeasureLineNotes == value) return;
            
            showMeasureLineNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showMeasureLineNotes = true;

    /// <summary>
    /// Should Beat Lines be shown?
    /// </summary>
    public bool ShowBeatLineNotes
    {
        get => showBeatLineNotes;
        set 
        {
            if (showBeatLineNotes == value) return;
            
            showBeatLineNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showBeatLineNotes = true;
    
    /// <summary>
    /// Should Lane Show notes be shown?
    /// </summary>
    public bool ShowLaneShowNotes
    {
        get => showLaneShowNotes;
        set 
        {
            if (showLaneShowNotes == value) return;
            
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
            if (showLaneHideNotes == value) return;
            
            showLaneHideNotes = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showLaneHideNotes = true;
    
    /// <summary>
    /// Should Tempo changes be shown?
    /// </summary>
    public bool ShowTempoChangeEvents
    {
        get => showTempoChangeEvents;
        set 
        {
            if (showTempoChangeEvents == value) return;
            
            showTempoChangeEvents = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showTempoChangeEvents = true;
    
    /// <summary>
    /// Should Metre changes be shown?
    /// </summary>
    public bool ShowMetreChangeEvents
    {
        get => showMetreChangeEvents;
        set 
        {
            if (showMetreChangeEvents == value) return;
            
            showMetreChangeEvents = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showMetreChangeEvents = true;
    
    /// <summary>
    /// Should Speed changes be shown?
    /// </summary>
    public bool ShowSpeedChangeEvents
    {
        get => showSpeedChangeEvents;
        set 
        {
            if (showSpeedChangeEvents == value) return;
            
            showSpeedChangeEvents = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showSpeedChangeEvents = true;
    
    /// <summary>
    /// Should Visibility changes be shown?
    /// </summary>
    public bool ShowVisibilityChangeEvents
    {
        get => showVisibilityChangeEvents;
        set 
        {
            if (showVisibilityChangeEvents == value) return;
            
            showVisibilityChangeEvents = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showVisibilityChangeEvents = true;
    
    /// <summary>
    /// Should Reverse effects be shown?
    /// </summary>
    public bool ShowReverseEffectEvents
    {
        get => showReverseEffectEvents;
        set 
        {
            if (showReverseEffectEvents == value) return;
            
            showReverseEffectEvents = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showReverseEffectEvents = true;
    
    /// <summary>
    /// Should Stop effects be shown?
    /// </summary>
    public bool ShowStopEffectEvents
    {
        get => showStopEffectEvents;
        set 
        {
            if (showStopEffectEvents == value) return;
            
            showStopEffectEvents = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showStopEffectEvents = true;
    
    /// <summary>
    /// Should Tutorial tags be shown?
    /// </summary>
    public bool ShowTutorialMarkerEvents
    {
        get => showTutorialMarkerEvents;
        set 
        {
            if (showTutorialMarkerEvents == value) return;
            
            showTutorialMarkerEvents = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showTutorialMarkerEvents = true;

    /// <summary>
    /// When should R-Note effects be visible?
    /// </summary>
    public EffectVisibilityOption RNoteEffectVisibility
    {
        get => rNoteEffectVisibility;
        set 
        {
            if (rNoteEffectVisibility == value) return;
            
            rNoteEffectVisibility = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private EffectVisibilityOption rNoteEffectVisibility = EffectVisibilityOption.OnlyWhenPlaying;
    
    /// <summary>
    /// When should R-Note effects be visible?
    /// </summary>
    public EffectVisibilityOption BonusEffectVisibility
    {
        get => bonusEffectVisibility;
        set 
        {
            if (bonusEffectVisibility == value) return;
            
            bonusEffectVisibility = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private EffectVisibilityOption bonusEffectVisibility = EffectVisibilityOption.OnlyWhenPlaying;

    /// <summary>
    /// How opaque/bright should the R-Note effect be?
    /// </summary>
    public int RNoteEffectOpacity
    {
        get => rNoteEffectOpacity;
        set
        {
            if (rNoteEffectOpacity == value) return;
            
            rNoteEffectOpacity = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private int rNoteEffectOpacity = 10;
    
    /// <summary>
    /// Should the background change when a clear is achieved?
    /// </summary>
    public ClearBackgroundVisibilityOption ClearBackgroundVisibility
    {
        get => clearBackgroundVisibility;
        set 
        {
            if (clearBackgroundVisibility == value) return;
            
            clearBackgroundVisibility = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private ClearBackgroundVisibilityOption clearBackgroundVisibility = ClearBackgroundVisibilityOption.SimulateClear;
}