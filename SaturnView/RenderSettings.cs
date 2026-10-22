using SaturnData.Content.Items;
using SaturnData.Content.Items.Attributes;

namespace SaturnView;

public class RenderSettings : ContentItem
{
    public new event EventHandler? PropertyChanged;
    
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
        Version1 = 0,
        Version2 = 1,
        Version3 = 2,
        Greyscale = 3,
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

    /// <summary>
    /// The list of available interface visibility options.
    /// </summary>
    public enum InterfaceVisibilityOption
    {
        Hidden = 0,
        Obscured = 1,
        Visible = 2,
    }
    
#endregion Enum Definitions

    /// <summary>
    /// Simplifies chart visuals to lower the performance cost.
    /// </summary>
    [SctLineBreak]
    [SctKey("LOW_PERFORMANCE", 27)]
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
    [SctLineBreak]
    [SctKey("NOTE_SPEED", 27)]
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
    /// The amount to dim the background by.
    /// </summary>
    [SctKey("BACKGROUND_DIM", 27)]
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
    /// The thickness of notes.
    /// </summary>
    [SctKey("NOTE_THICKNESS", 27)]
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
    /// The color of the judgement line.
    /// </summary>
    [SctKey("JUDGEMENT_LINE_COLOR", 27)]
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
    /// The density of guidelines.
    /// </summary>
    [SctKey("GUIDE_LINE_TYPE", 27)]
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
    /// The color of Touch notes.
    /// </summary>
    [SctLineBreak]
    [SctKey("COLOR_TOUCH", 27)]
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
    [SctKey("COLOR_CHAIN", 27)]
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
    [SctKey("COLOR_HOLD", 27)]
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
    [SctKey("COLOR_SLIDE_CLW", 27)]
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
    [SctKey("COLOR_SLIDE_CCW", 27)]
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
    [SctKey("COLOR_SNAP_FWD", 27)]
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
    [SctKey("COLOR_SNAP_BWD", 27)]
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
    /// The opacity of "hidden" objects.
    /// </summary>
    [SctLineBreak]
    [SctKey("HIDDEN_OPACITY", 27)]
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
    /// Should Touch notes be shown?
    /// </summary>
    [SctKey("SHOW_NOTE_TOUCH", 27)]
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
    [SctKey("SHOW_NOTE_CHAIN", 27)]
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
    [SctKey("SHOW_NOTE_HOLD", 27)]
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
    [SctKey("SHOW_NOTE_SLIDE_CLW", 27)]
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
    [SctKey("SHOW_NOTE_SLIDE_CCW", 27)]
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
    [SctKey("SHOW_NOTE_SNAP_FWD", 27)]
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
    [SctKey("SHOW_NOTE_SNAP_BWD", 27)]
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
    [SctKey("SHOW_NOTE_SYNC", 27)]
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
    [SctKey("SHOW_NOTE_MEASURELINE", 27)]
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
    [SctKey("SHOW_NOTE_BEATLINE", 27)]
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
    [SctKey("SHOW_NOTE_LANESHOW", 27)]
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
    [SctKey("SHOW_NOTE_LANEHIDE", 27)]
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
    [SctKey("SHOW_EVENT_TEMPO", 27)]
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
    [SctKey("SHOW_EVENT_METRE", 27)]
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
    [SctKey("SHOW_EVENT_SPEED", 27)]
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
    [SctKey("SHOW_EVENT_VISIBILITY", 27)]
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
    [SctKey("SHOW_EVENT_REVERSE", 27)]
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
    [SctKey("SHOW_EVENT_STOP", 27)]
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
    [SctKey("SHOW_EVENT_TUTORIAL", 27)]
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
    /// Should Bookmarks be shown?
    /// </summary>
    [SctKey("SHOW_BOOKMARKS", 27)]
    public bool ShowBookmarks
    {
        get => showBookmarks;
        set 
        {
            if (showBookmarks == value) return;
            
            showBookmarks = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showBookmarks = true;
    
    /// <summary>
    /// Should event markers be hidden during playback?
    /// </summary>
    [SctLineBreak]
    [SctKey("HIDE_PLAYBACK_EVENTMARKERS", 27)]
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
    [SctKey("HIDE_PLAYBACK_LANETOGGLES", 27)]
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
    [SctKey("HIDE_PLAYBACK_HOLDPOINTS", 27)]
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
    /// Should bookmarks be hidden during playback?
    /// </summary>
    [SctKey("HIDE_PLAYBACK_BOOKMARKS", 27)]
    public bool HideBookmarksDuringPlayback
    {
        get => hideBookmarksDuringPlayback;
        set 
        {
            hideBookmarksDuringPlayback = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool hideBookmarksDuringPlayback = true;
    
    /// <summary>
    /// Should scroll speed changes be visible?
    /// </summary>
    [SctLineBreak]
    [SctKey("EFFECTS_SPEED_SHOW", 27)]
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
    [SctKey("EFFECTS_VISIBILITY_SHOW", 27)]
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
    /// Should lane toggle animations be shown?
    /// </summary>
    [SctKey("EFFECTS_LANES_SHOW", 27)]
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
    /// Should judge areas be shown?
    /// </summary>
    [SctLineBreak]
    [SctKey("JUDGE_AREA_SHOW", 27)]
    public bool ShowJudgeAreas
    {
        get => showJudgeAreas;
        set
        {
            if (showJudgeAreas == value) return;
            
            showJudgeAreas = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showJudgeAreas = false;

    /// <summary>
    /// Should MARVELOUS judge areas be shown?
    /// </summary>
    [SctKey("JUDGE_AREA_MARVELOUS", 27)]
    public bool ShowMarvelousArea
    {
        get => showMarvelousArea;
        set
        {
            if (showMarvelousArea == value) return;
            
            showMarvelousArea = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showMarvelousArea = true;

    /// <summary>
    /// Should GREAT judge areas be shown?
    /// </summary>
    [SctKey("JUDGE_AREA_GREAT", 27)]
    public bool ShowGreatArea
    {
        get => showGreatArea;
        set
        {
            if (showGreatArea == value) return;
            
            showGreatArea = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showGreatArea = true;
    
    /// <summary>
    /// Should GOOD judge areas be shown?
    /// </summary>
    [SctKey("JUDGE_AREA_GOOD", 27)]
    public bool ShowGoodArea
    {
        get => showGoodArea;
        set
        {
            if (showGoodArea == value) return;
            
            showGoodArea = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool showGoodArea = true;
    
    /// <summary>
    /// Should judge areas follow the Saturn specifications?<br/>
    /// (Slight engine changes to prevent "splashing")
    /// </summary>
    [SctKey("JUDGE_AREA_SATURN", 27)]
    public bool SaturnJudgeAreas
    {
        get => saturnJudgeAreas;
        set
        {
            if (saturnJudgeAreas == value) return;
            
            saturnJudgeAreas = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool saturnJudgeAreas = true;
    
    /// <summary>
    /// Should the sweep animations of lane toggles be visualized?
    /// </summary>
    [SctLineBreak]
    [SctKey("VISUALIZE_LANE_SWEEPS", 27)]
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
    /// How opaque/bright should the R-Note effect be?
    /// </summary>
    [SctLineBreak]
    [SctKey("R_EFFECT_OPACITY", 27)]
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
    /// When should R-Note effects be visible?
    /// </summary>
    [SctKey("EFFECT_VISIBILITY_R", 27)]
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
    [SctKey("EFFECT_VISIBILITY_BONUS", 27)]
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
    /// Should unit ticks be shown when paused?
    /// </summary>
    [SctKey("EFFECT_VISIBILITY_UNIT_TICK", 27)]
    public EffectVisibilityOption UnitTickVisibility
    {
        get => unitTickVisibility;
        set 
        {
            if (unitTickVisibility == value) return;
            
            unitTickVisibility = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private EffectVisibilityOption unitTickVisibility = EffectVisibilityOption.OnlyWhenPaused;
    
    /// <summary>
    /// Should the background change when a clear is achieved?
    /// </summary>
    [SctLineBreak]
    [SctKey("CLEAR_BACKGROUND_VISIBILITY", 27)]
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
    
    /// <summary>
    /// Should the chart level be displayed?
    /// </summary>
    [SctLineBreak]
    [SctKey("INTERFACE_VISIBILITY_LEVEL", 31)]
    public InterfaceVisibilityOption LevelDisplayVisibility
    {
        get => levelDisplayVisibility;
        set 
        {
            if (levelDisplayVisibility == value) return;
            
            levelDisplayVisibility = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private InterfaceVisibilityOption levelDisplayVisibility = InterfaceVisibilityOption.Visible;
    
    /// <summary>
    /// Should the chart title be displayed?
    /// </summary>
    [SctKey("INTERFACE_VISIBILITY_TITLE", 31)]
    public InterfaceVisibilityOption TitleDisplayVisibility
    {
        get => titleDisplayVisibility;
        set 
        {
            if (titleDisplayVisibility == value) return;
            
            titleDisplayVisibility = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private InterfaceVisibilityOption titleDisplayVisibility = InterfaceVisibilityOption.Visible;
    
    /// <summary>
    /// Should the chart difficulty be displayed?
    /// </summary>
    [SctKey("INTERFACE_VISIBILITY_DIFFICULTY", 31)]
    public InterfaceVisibilityOption DifficultyDisplayVisibility
    {
        get => difficultyDisplayVisibility;
        set 
        {
            if (difficultyDisplayVisibility == value) return;
            
            difficultyDisplayVisibility = value;
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private InterfaceVisibilityOption difficultyDisplayVisibility = InterfaceVisibilityOption.Visible;
}