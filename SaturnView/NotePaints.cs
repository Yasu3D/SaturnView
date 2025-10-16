using SaturnData.Notation.Core;
using SaturnData.Notation.Events;
using SaturnData.Notation.Notes;
using SkiaSharp;

namespace SaturnView;

internal static class NotePaints
{
    #region Color Definitions
    private static readonly SKColor NoteColorCapLight = new(NoteColors.CapLight);
    private static readonly SKColor NoteColorCapBase  = new(NoteColors.CapBase);
    private static readonly SKColor NoteColorCapDark  = new(NoteColors.CapDark);

    private static readonly SKColor NoteColorChainStripe = new(NoteColors.ChainStripe);
    private static readonly SKColor NoteColorRNoteGlow = new(NoteColors.RNoteGlow);
    private static readonly SKColor NoteColorSyncOutline = new(NoteColors.SyncOutline);
    
    private static readonly SKColor NoteColorMeasureLine = new(NoteColors.MeasureLine);
    private static readonly SKColor NoteColorBeatLine = new(NoteColors.BeatLine);
    
    private static readonly SKColor NoteColorLaneBaseA = new(NoteColors.LaneBaseA);
    private static readonly SKColor NoteColorLaneBaseB = new(NoteColors.LaneBaseB);
    
    private static readonly SKColor NoteColorSyncConnectorLight = new(NoteColors.SyncConnectorLight);
    private static readonly SKColor NoteColorSyncConnectorBase = new(NoteColors.SyncConnectorBase);
    private static readonly SKColor NoteColorSyncConnectorDark = new(NoteColors.SyncConnectorDark);
    
    private static readonly SKColor NoteColorLightMagentaAverage = new(NoteColors.LightMagentaAverage);
    private static readonly SKColor NoteColorLightMagentaBase    = new(NoteColors.LightMagentaBase);
    private static readonly SKColor NoteColorLightMagentaLight   = new(NoteColors.LightMagentaLight);
    private static readonly SKColor NoteColorLightMagentaDark    = new(NoteColors.LightMagentaDark);
    private static readonly SKColor NoteColorLightMagentaHoldEndLight = new(NoteColors.LightMagentaHoldEndLight);
    private static readonly SKColor NoteColorLightMagentaHoldEndDark  = new(NoteColors.LightMagentaHoldEndDark);
    
    private static readonly SKColor NoteColorLightYellowAverage = new(NoteColors.LightYellowAverage);
    private static readonly SKColor NoteColorLightYellowBase    = new(NoteColors.LightYellowBase);
    private static readonly SKColor NoteColorLightYellowLight   = new(NoteColors.LightYellowLight);
    private static readonly SKColor NoteColorLightYellowDark    = new(NoteColors.LightYellowDark);
    private static readonly SKColor NoteColorLightYellowHoldEndLight = new(NoteColors.LightYellowHoldEndLight);
    private static readonly SKColor NoteColorLightYellowHoldEndDark  = new(NoteColors.LightYellowHoldEndDark);
    
    private static readonly SKColor NoteColorOrangeAverage = new(NoteColors.OrangeAverage);
    private static readonly SKColor NoteColorOrangeBase    = new(NoteColors.OrangeBase);
    private static readonly SKColor NoteColorOrangeLight   = new(NoteColors.OrangeLight);
    private static readonly SKColor NoteColorOrangeDark    = new(NoteColors.OrangeDark);
    private static readonly SKColor NoteColorOrangeHoldEndLight = new(NoteColors.OrangeHoldEndLight);
    private static readonly SKColor NoteColorOrangeHoldEndDark  = new(NoteColors.OrangeHoldEndDark);
    
    private static readonly SKColor NoteColorLimeAverage = new(NoteColors.LimeAverage);
    private static readonly SKColor NoteColorLimeBase    = new(NoteColors.LimeBase);
    private static readonly SKColor NoteColorLimeLight   = new(NoteColors.LimeLight);
    private static readonly SKColor NoteColorLimeDark    = new(NoteColors.LimeDark);
    private static readonly SKColor NoteColorLimeHoldEndLight = new(NoteColors.LimeHoldEndLight);
    private static readonly SKColor NoteColorLimeHoldEndDark  = new(NoteColors.LimeHoldEndDark);
    
    private static readonly SKColor NoteColorRedAverage = new(NoteColors.RedAverage);
    private static readonly SKColor NoteColorRedBase    = new(NoteColors.RedBase);
    private static readonly SKColor NoteColorRedLight   = new(NoteColors.RedLight);
    private static readonly SKColor NoteColorRedDark    = new(NoteColors.RedDark);
    private static readonly SKColor NoteColorRedHoldEndLight = new(NoteColors.RedHoldEndLight);
    private static readonly SKColor NoteColorRedHoldEndDark  = new(NoteColors.RedHoldEndDark);
    
    private static readonly SKColor NoteColorSkyBlueAverage = new(NoteColors.SkyBlueAverage);
    private static readonly SKColor NoteColorSkyBlueBase    = new(NoteColors.SkyBlueBase);
    private static readonly SKColor NoteColorSkyBlueLight   = new(NoteColors.SkyBlueLight);
    private static readonly SKColor NoteColorSkyBlueDark    = new(NoteColors.SkyBlueDark);
    private static readonly SKColor NoteColorSkyBlueHoldEndLight = new(NoteColors.SkyBlueHoldEndLight);
    private static readonly SKColor NoteColorSkyBlueHoldEndDark  = new(NoteColors.SkyBlueHoldEndDark);
    
    private static readonly SKColor NoteColorDarkYellowAverage = new(NoteColors.DarkYellowAverage);
    private static readonly SKColor NoteColorDarkYellowBase    = new(NoteColors.DarkYellowBase);
    private static readonly SKColor NoteColorDarkYellowLight   = new(NoteColors.DarkYellowLight);
    private static readonly SKColor NoteColorDarkYellowDark    = new(NoteColors.DarkYellowDark);
    private static readonly SKColor NoteColorDarkYellowHoldEndLight = new(NoteColors.DarkYellowHoldEndLight);
    private static readonly SKColor NoteColorDarkYellowHoldEndDark  = new(NoteColors.DarkYellowHoldEndDark);
    
    private static readonly SKColor NoteColorLightRedAverage = new(NoteColors.LightRedAverage);
    private static readonly SKColor NoteColorLightRedBase    = new(NoteColors.LightRedBase);
    private static readonly SKColor NoteColorLightRedLight   = new(NoteColors.LightRedLight);
    private static readonly SKColor NoteColorLightRedDark    = new(NoteColors.LightRedDark);
    private static readonly SKColor NoteColorLightRedHoldEndLight = new(NoteColors.LightRedHoldEndLight);
    private static readonly SKColor NoteColorLightRedHoldEndDark  = new(NoteColors.LightRedHoldEndDark);
    
    private static readonly SKColor NoteColorYellowAverage = new(NoteColors.YellowAverage);
    private static readonly SKColor NoteColorYellowBase    = new(NoteColors.YellowBase);
    private static readonly SKColor NoteColorYellowLight   = new(NoteColors.YellowLight);
    private static readonly SKColor NoteColorYellowDark    = new(NoteColors.YellowDark);
    private static readonly SKColor NoteColorYellowHoldEndLight = new(NoteColors.YellowHoldEndLight);
    private static readonly SKColor NoteColorYellowHoldEndDark  = new(NoteColors.YellowHoldEndDark);
    
    private static readonly SKColor NoteColorPureGreenAverage = new(NoteColors.PureGreenAverage);
    private static readonly SKColor NoteColorPureGreenBase    = new(NoteColors.PureGreenBase);
    private static readonly SKColor NoteColorPureGreenLight   = new(NoteColors.PureGreenLight);
    private static readonly SKColor NoteColorPureGreenDark    = new(NoteColors.PureGreenDark);
    private static readonly SKColor NoteColorPureGreenHoldEndLight = new(NoteColors.PureGreenHoldEndLight);
    private static readonly SKColor NoteColorPureGreenHoldEndDark  = new(NoteColors.PureGreenHoldEndDark);
    
    private static readonly SKColor NoteColorBrightBlueAverage = new(NoteColors.BrightBlueAverage);
    private static readonly SKColor NoteColorBrightBlueBase    = new(NoteColors.BrightBlueBase);
    private static readonly SKColor NoteColorBrightBlueLight   = new(NoteColors.BrightBlueLight);
    private static readonly SKColor NoteColorBrightBlueDark    = new(NoteColors.BrightBlueDark);
    private static readonly SKColor NoteColorBrightBlueHoldEndLight = new(NoteColors.BrightBlueHoldEndLight);
    private static readonly SKColor NoteColorBrightBlueHoldEndDark  = new(NoteColors.BrightBlueHoldEndDark);
    
    private static readonly SKColor NoteColorLightBlueAverage = new(NoteColors.LightBlueAverage);
    private static readonly SKColor NoteColorLightBlueBase    = new(NoteColors.LightBlueBase);
    private static readonly SKColor NoteColorLightBlueLight   = new(NoteColors.LightBlueLight);
    private static readonly SKColor NoteColorLightBlueDark    = new(NoteColors.LightBlueDark);
    private static readonly SKColor NoteColorLightBlueHoldEndLight = new(NoteColors.LightBlueHoldEndLight);
    private static readonly SKColor NoteColorLightBlueHoldEndDark  = new(NoteColors.LightBlueHoldEndDark);
    
    private static readonly SKColor NoteColorLightGrayAverage = new(NoteColors.LightGrayAverage);
    private static readonly SKColor NoteColorLightGrayBase    = new(NoteColors.LightGrayBase);
    private static readonly SKColor NoteColorLightGrayLight   = new(NoteColors.LightGrayLight);
    private static readonly SKColor NoteColorLightGrayDark    = new(NoteColors.LightGrayDark);
    private static readonly SKColor NoteColorLightGrayHoldEndLight = new(NoteColors.LightGrayHoldEndLight);
    private static readonly SKColor NoteColorLightGrayHoldEndDark  = new(NoteColors.LightGrayHoldEndDark);

    private static readonly SKColor NoteColorLaneShow = new(NoteColors.LaneShow);
    private static readonly SKColor NoteColorLaneHide = new(NoteColors.LaneHide);
    
    private static readonly SKColor JudgementLineShadeColorA = new(0x00FFFFFF);
    private static readonly SKColor JudgementLineShadeColorB = new(0x50FFFFFF);
    private static readonly SKColor JudgementLineShadeColorC = new(0xFFFFFFFF);
    private static readonly SKColor JudgementLineShadeColorD = new(0xFFAAAAAA);
    
    private static readonly SKColor EventColorTempoChangeEvent = new(NoteColors.TempoChangeEvent);
    private static readonly SKColor EventColorMetreChangeEvent = new(NoteColors.MetreChangeEvent);
    private static readonly SKColor EventColorTutorialMarkerEvent = new(NoteColors.TutorialMarkerEvent);
    private static readonly SKColor EventColorSpeedChangeEvent = new(NoteColors.SpeedChangeEvent);
    private static readonly SKColor EventColorVisibilityChangeEvent = new(NoteColors.VisibilityChangeEvent);
    private static readonly SKColor EventColorReverseEffectEvent = new(NoteColors.ReverseEffectEvent);
    private static readonly SKColor EventColorStopEffectEvent = new(NoteColors.StopEffectEvent);

    private static readonly SKColor PointerOverColorStroke = new(0x80FFFFFF);
    private static readonly SKColor PointerOverColorFill   = new(0x40FFFFFF);
    private static readonly SKColor SelectedColorStroke = new(0xFF3F80CC);
    private static readonly SKColor SelectedColorFill   = new(0x803F80CC);
    private static readonly SKColor PointerOverSelectedColorStroke = new(0xDD3F80CC);
    private static readonly SKColor PointerOverSelectedColorFill   = new(0x403F80CC);

    private static readonly SKColor[] BonusSweepEffectColorsClockwise = 
    [
        new(NoteColors.BonusSweepEffect - 0xEE000000), 
        new(NoteColors.BonusSweepEffect - 0xEE000000), 
        new(NoteColors.BonusSweepEffect - 0xDD000000), 
        new(NoteColors.BonusSweepEffect - 0xDD000000),
        new(NoteColors.BonusSweepEffect - 0xCC000000), 
        new(NoteColors.BonusSweepEffect - 0xCC000000),
        new(NoteColors.BonusSweepEffect - 0xBB000000), 
        new(NoteColors.BonusSweepEffect - 0xBB000000),
        new(NoteColors.BonusSweepEffect - 0xAA000000), 
        new(NoteColors.BonusSweepEffect - 0xAA000000),
        new(NoteColors.BonusSweepEffect - 0x99000000), 
        new(NoteColors.BonusSweepEffect - 0x99000000),
        new(NoteColors.BonusSweepEffect - 0x88000000), 
        new(NoteColors.BonusSweepEffect - 0x88000000),
        new(NoteColors.BonusSweepEffect - 0x77000000), 
        new(NoteColors.BonusSweepEffect - 0x77000000),
        new(NoteColors.BonusSweepEffect - 0x66000000), 
        new(NoteColors.BonusSweepEffect - 0x66000000),
        new(NoteColors.BonusSweepEffect - 0x55000000), 
        new(NoteColors.BonusSweepEffect - 0x55000000),
        new(NoteColors.BonusSweepEffect - 0x44000000), 
        new(NoteColors.BonusSweepEffect - 0x44000000),
        new(NoteColors.BonusSweepEffect - 0x33000000), 
        new(NoteColors.BonusSweepEffect - 0x33000000),
        new(NoteColors.BonusSweepEffect - 0x22000000), 
        new(NoteColors.BonusSweepEffect - 0x22000000),
        new(NoteColors.BonusSweepEffect - 0x11000000), 
        new(NoteColors.BonusSweepEffect - 0x11000000),
        new(NoteColors.BonusSweepEffect), 
        new(NoteColors.BonusSweepEffect),
    ];
    
    private static readonly SKColor[] BonusSweepEffectColorsCounterclockwise = 
    [
        new(NoteColors.BonusSweepEffect), 
        new(NoteColors.BonusSweepEffect), 
        new(NoteColors.BonusSweepEffect - 0x11000000), 
        new(NoteColors.BonusSweepEffect - 0x11000000),
        new(NoteColors.BonusSweepEffect - 0x22000000), 
        new(NoteColors.BonusSweepEffect - 0x22000000),
        new(NoteColors.BonusSweepEffect - 0x33000000), 
        new(NoteColors.BonusSweepEffect - 0x33000000),
        new(NoteColors.BonusSweepEffect - 0x44000000), 
        new(NoteColors.BonusSweepEffect - 0x44000000),
        new(NoteColors.BonusSweepEffect - 0x55000000), 
        new(NoteColors.BonusSweepEffect - 0x55000000),
        new(NoteColors.BonusSweepEffect - 0x66000000), 
        new(NoteColors.BonusSweepEffect - 0x66000000),
        new(NoteColors.BonusSweepEffect - 0x77000000), 
        new(NoteColors.BonusSweepEffect - 0x77000000),
        new(NoteColors.BonusSweepEffect - 0x88000000), 
        new(NoteColors.BonusSweepEffect - 0x88000000),
        new(NoteColors.BonusSweepEffect - 0x99000000), 
        new(NoteColors.BonusSweepEffect - 0x99000000),
        new(NoteColors.BonusSweepEffect - 0xAA000000), 
        new(NoteColors.BonusSweepEffect - 0xAA000000),
        new(NoteColors.BonusSweepEffect - 0xBB000000), 
        new(NoteColors.BonusSweepEffect - 0xBB000000),
        new(NoteColors.BonusSweepEffect - 0xCC000000), 
        new(NoteColors.BonusSweepEffect - 0xCC000000),
        new(NoteColors.BonusSweepEffect - 0xDD000000), 
        new(NoteColors.BonusSweepEffect - 0xDD000000),
        new(NoteColors.BonusSweepEffect - 0xEE000000), 
        new(NoteColors.BonusSweepEffect - 0xEE000000),
    ];
    
    private static readonly float[] BonusSweepEffectPositions = 
    [
        0,
        1.0f  / 15.0f - 0.0001f,
        1.0f  / 15.0f,
        2.0f  / 15.0f - 0.0001f,
        2.0f  / 15.0f,
        3.0f  / 15.0f - 0.0001f,
        3.0f  / 15.0f,
        4.0f  / 15.0f - 0.0001f,
        4.0f  / 15.0f,
        5.0f  / 15.0f - 0.0001f,
        5.0f  / 15.0f,
        6.0f  / 15.0f - 0.0001f,
        6.0f  / 15.0f,
        7.0f  / 15.0f - 0.0001f,
        7.0f  / 15.0f,
        8.0f  / 15.0f - 0.0001f,
        8.0f  / 15.0f,
        9.0f  / 15.0f - 0.0001f,
        9.0f  / 15.0f,
        10.0f / 15.0f - 0.0001f,
        10.0f / 15.0f,
        11.0f / 15.0f - 0.0001f,
        11.0f / 15.0f,
        12.0f / 15.0f - 0.0001f,
        12.0f / 15.0f,
        13.0f / 15.0f - 0.0001f,
        13.0f / 15.0f,
        14.0f / 15.0f - 0.0001f,
        14.0f / 15.0f,
        1,
    ];
    
#endregion Color Definitions
    
    private static SKColor GetNoteColorAverage(int id) => id switch
    {
        0 => NoteColorLightMagentaAverage,
        1 => NoteColorLightYellowAverage,
        2 => NoteColorOrangeAverage,
        3 => NoteColorLimeAverage,
        4 => NoteColorRedAverage,
        5 => NoteColorSkyBlueAverage,
        6 => NoteColorDarkYellowAverage,
        7 => NoteColorLightRedAverage,
        8 => NoteColorYellowAverage,
        9 => NoteColorPureGreenAverage,
        10 => NoteColorBrightBlueAverage,
        11 => NoteColorLightBlueAverage,
        12 => NoteColorLightGrayAverage,
        _ => NoteColorLightMagentaAverage,
    };

    private static SKColor GetNoteColorBase(int id) => id switch
    {
        0 => NoteColorLightMagentaBase,
        1 => NoteColorLightYellowBase,
        2 => NoteColorOrangeBase,
        3 => NoteColorLimeBase,
        4 => NoteColorRedBase,
        5 => NoteColorSkyBlueBase,
        6 => NoteColorDarkYellowBase,
        7 => NoteColorLightRedBase,
        8 => NoteColorYellowBase,
        9 => NoteColorPureGreenBase,
        10 => NoteColorBrightBlueBase,
        11 => NoteColorLightBlueBase,
        12 => NoteColorLightGrayBase,
        _ => NoteColorLightMagentaBase,
    };

    private static SKColor GetNoteColorDark(int id) => id switch
    {
        0 => NoteColorLightMagentaDark,
        1 => NoteColorLightYellowDark,
        2 => NoteColorOrangeDark,
        3 => NoteColorLimeDark,
        4 => NoteColorRedDark,
        5 => NoteColorSkyBlueDark,
        6 => NoteColorDarkYellowDark,
        7 => NoteColorLightRedDark,
        8 => NoteColorYellowDark,
        9 => NoteColorPureGreenDark,
        10 => NoteColorBrightBlueDark,
        11 => NoteColorLightBlueDark,
        12 => NoteColorLightGrayDark,
        _ => NoteColorLightMagentaDark,
    };

    private static SKColor GetNoteColorLight(int id) => id switch
    {
        0 => NoteColorLightMagentaLight,
        1 => NoteColorLightYellowLight,
        2 => NoteColorOrangeLight,
        3 => NoteColorLimeLight,
        4 => NoteColorRedLight,
        5 => NoteColorSkyBlueLight,
        6 => NoteColorDarkYellowLight,
        7 => NoteColorLightRedLight,
        8 => NoteColorYellowLight,
        9 => NoteColorPureGreenLight,
        10 => NoteColorBrightBlueLight,
        11 => NoteColorLightBlueLight,
        12 => NoteColorLightGrayLight,
        _ => NoteColorLightMagentaLight,
    };
    
    private static SKColor GetNoteColorHoldEndLight(int id) => id switch
    {
        0 => NoteColorLightMagentaHoldEndLight,
        1 => NoteColorLightYellowHoldEndLight,
        2 => NoteColorOrangeHoldEndLight,
        3 => NoteColorLimeHoldEndLight,
        4 => NoteColorRedHoldEndLight,
        5 => NoteColorSkyBlueHoldEndLight,
        6 => NoteColorDarkYellowHoldEndLight,
        7 => NoteColorLightRedHoldEndLight,
        8 => NoteColorYellowHoldEndLight,
        9 => NoteColorPureGreenHoldEndLight,
        10 => NoteColorBrightBlueHoldEndLight,
        11 => NoteColorLightBlueHoldEndLight,
        12 => NoteColorLightGrayHoldEndLight,
        _ => NoteColorLightMagentaHoldEndLight,
    };
    
    private static SKColor GetNoteColorHoldEndDark(int id) => id switch
    {
        0 => NoteColorLightMagentaHoldEndDark,
        1 => NoteColorLightYellowHoldEndDark,
        2 => NoteColorOrangeHoldEndDark,
        3 => NoteColorLimeHoldEndDark,
        4 => NoteColorRedHoldEndDark,
        5 => NoteColorSkyBlueHoldEndDark,
        6 => NoteColorDarkYellowHoldEndDark,
        7 => NoteColorLightRedHoldEndDark,
        8 => NoteColorYellowHoldEndDark,
        9 => NoteColorPureGreenHoldEndDark,
        10 => NoteColorBrightBlueHoldEndDark,
        11 => NoteColorLightBlueHoldEndDark,
        12 => NoteColorLightGrayHoldEndDark,
        _ => NoteColorLightMagentaHoldEndDark,
    };

    internal static SKColor GetEventColor(Event @event)
    {
        return @event switch
        {
            TempoChangeEvent => EventColorTempoChangeEvent,
            MetreChangeEvent => EventColorMetreChangeEvent,
            TutorialMarkerEvent => EventColorTutorialMarkerEvent,
            
            SpeedChangeEvent => EventColorSpeedChangeEvent,
            VisibilityChangeEvent => EventColorVisibilityChangeEvent,
            
            StopEffectEvent => EventColorStopEffectEvent,
            ReverseEffectEvent => EventColorReverseEffectEvent,
            
            EffectSubEvent effectSubEvent => effectSubEvent.Parent switch
            {
                StopEffectEvent => EventColorStopEffectEvent,
                ReverseEffectEvent => EventColorReverseEffectEvent,
                _ => new(0xFFFFFFFF),
            },
            _ => new(0xFFFFFFFF),
        };
    }

    internal static readonly float[] NoteStrokeWidths     = [ 16.0f, 22.0f, 34.0f, 46.0f, 58.0f ];
    
    internal static readonly float[] NoteGradientPos1     = [ 0.200f, 0.250f, 0.194f, 0.145f, 0.100f ];
    internal static readonly float[] NoteGradientPos2     = [ 0.500f, 0.333f, 0.333f, 0.250f, 0.183f ];
    internal static readonly float[] NoteGradientPos3     = [ 0.500f, 0.458f, 0.444f, 0.604f, 0.666f ];
    internal static readonly float[] NoteGradientPos4     = [ 0.800f, 0.580f, 0.638f, 0.708f, 0.783f ];
    internal static readonly float[] NoteGradientPos5     = [ 0.800f, 0.791f, 0.861f, 0.875f, 0.916f ];
    
    private static readonly SKPaint FlatStrokePaint = new()
    {
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Butt, 
        Style = SKPaintStyle.Stroke,
        StrokeJoin = SKStrokeJoin.Miter,
        StrokeMiter = 10,
    };

    private static readonly SKPaint ShaderStrokePaint = new()
    {
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Butt,
        Style = SKPaintStyle.Stroke,
        StrokeJoin = SKStrokeJoin.Miter,
        StrokeMiter = 10,
    };

    private static readonly SKPaint FlatFillPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint ShaderFillPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint BlurStrokePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        BlendMode = SKBlendMode.Plus,
    };

    private static readonly SKPaint HoldSurfacePaint = new()
    {
        IsAntialias = false,
        Shader = SKShader.CreateImage
        (
            SKImage.FromEncodedData
            (
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/hold_gradient.png")
            )
        ),
    };
    
    private static readonly SKPaint HoldSurfacePaintActive = new()
    {
        IsAntialias = false,
        Shader = SKShader.CreateImage
        (
            SKImage.FromEncodedData
            (
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/hold_gradient_active.png")
            )
        ),
    };

    private static readonly SKPaint BackgroundVersion3Paint = new()
    {
        IsAntialias = false,
        Shader = SKShader.CreateImage
        (
            SKImage.FromEncodedData
            (
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/background_version3.png")
            )
        ),
    };
    
    private static readonly SKPaint BackgroundVersion3ClearPaint = new()
    {
        IsAntialias = false,
        Shader = SKShader.CreateImage
        (
            SKImage.FromEncodedData
            (
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/background_version3_clear.png")
            )
        ),
    };
    
    private static readonly SKPaint BackgroundBossPaint = new()
    {
        IsAntialias = false,
        Shader = SKShader.CreateImage
        (
            SKImage.FromEncodedData
            (
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/background_boss.png")
            )
        ),
    };
    
    private static readonly SKPaint BackgroundBossClearPaint = new()
    {
        IsAntialias = false,
        Shader = SKShader.CreateImage
        (
            SKImage.FromEncodedData
            (
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/background_boss_clear.png")
            )
        ),
    };
    
    public static readonly SKPaint DebugPaint = new()
    {
        IsAntialias = false,
        Color = new(0xFF00FF00),
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Butt,
        StrokeWidth = 1,
    };
    
    public static readonly SKPaint DebugPaint2 = new()
    {
        IsAntialias = false,
        Color = new(0xFFFF0000),
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Butt,
        StrokeWidth = 5,
    };
    
    public static readonly SKPaint DebugPaint3 = new()
    {
        IsAntialias = false,
        Color = new(0xFF00FFFF),
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKFont InterfaceFont = new(SKTypeface.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/noto_plus_1.ttf")));
    
    internal static SKPaint GetNoteCapPaint(CanvasInfo canvasInfo, RenderSettings settings, float scaleScaledByScreen, float rawScale, float opacity)
    {
        if (settings.LowPerformanceMode)
        {
            FlatStrokePaint.StrokeWidth = NoteStrokeWidths[(int)settings.NoteThickness] * scaleScaledByScreen;
            FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
            FlatStrokePaint.Color = NoteColorCapBase;

            if (opacity != 1)
            {
                FlatStrokePaint.Color = FlatStrokePaint.Color.WithAlpha((byte)(opacity * 255));
            }
            
            return FlatStrokePaint;
        }
        
        byte alpha = (byte)(opacity * 255);
        ShaderStrokePaint.Color = new(0xFF, 0xFF, 0xFF, alpha);
        
        float strokeWidth = NoteStrokeWidths[(int)settings.NoteThickness] * scaleScaledByScreen;
        ShaderStrokePaint.StrokeWidth = strokeWidth;
        
        float noteRadius = canvasInfo.JudgementLineRadius * rawScale;
        float offset = strokeWidth * 0.5f;

        float minRadius = (noteRadius - offset) / canvasInfo.Radius;
        float maxRadius = (noteRadius + offset) / canvasInfo.Radius;
        float delta = maxRadius - minRadius;

        float pos0 = minRadius + delta * 0.05f;
        float pos1 = minRadius + delta * 0.25f;
        float pos2 = minRadius + delta * 0.5f;
        float pos3 = minRadius + delta * 0.75f;
        float pos4 = minRadius + delta * 0.95f;
        
        SKColor[] colors = [NoteColorCapLight, NoteColorCapBase, NoteColorCapDark, NoteColorCapBase, NoteColorCapLight];
        float[] positions = [pos0, pos1, pos2, pos3, pos4];
        
        SKShader shader = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, colors, positions, SKShaderTileMode.Clamp);
        ShaderStrokePaint.Shader = shader;

        return ShaderStrokePaint;
    }
    
    internal static SKPaint GetNoteBasePaint(CanvasInfo canvasInfo, RenderSettings settings, int colorId, float pixelScale, float rawScale, float opacity)
    {
        if (settings.LowPerformanceMode)
        {
            FlatStrokePaint.StrokeWidth = NoteStrokeWidths[(int)settings.NoteThickness] * pixelScale;
            FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
            FlatStrokePaint.Color = GetNoteColorAverage(colorId).WithAlpha((byte)(opacity * 255));
            
            return FlatStrokePaint;
        }
        
        byte alpha = (byte)(opacity * 255);
        ShaderStrokePaint.Color = new(0xFF, 0xFF, 0xFF, alpha);
        
        float strokeWidth = NoteStrokeWidths[(int)settings.NoteThickness] * pixelScale;
        ShaderStrokePaint.StrokeWidth = strokeWidth;
    
        float noteRadius = canvasInfo.JudgementLineRadius * rawScale;
        float offset = strokeWidth * 0.5f;

        float minRadius = (noteRadius - offset) / canvasInfo.Radius;
        float maxRadius = (noteRadius + offset) / canvasInfo.Radius;
        float delta = maxRadius - minRadius;

        float pos0 = minRadius - delta * 0.100f;
        float pos1 = minRadius + delta * NoteGradientPos1[(int)settings.NoteThickness];
        float pos2 = minRadius + delta * NoteGradientPos2[(int)settings.NoteThickness];
        float pos3 = minRadius + delta * NoteGradientPos3[(int)settings.NoteThickness];
        float pos4 = minRadius + delta * NoteGradientPos4[(int)settings.NoteThickness];
        float pos5 = minRadius + delta * NoteGradientPos5[(int)settings.NoteThickness];
        float pos6 = minRadius + delta * 1.100f;

        SKColor colorBase = GetNoteColorBase(colorId);
        SKColor colorLight = GetNoteColorLight(colorId);
        SKColor colorDark = GetNoteColorDark(colorId);
        
        SKColor[] colors = [colorLight, colorBase, colorDark, colorDark, colorBase, colorBase, colorLight];
        float[] positions = [pos0, pos1, pos2, pos3, pos4, pos5, pos6];
        
        SKShader shader = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, colors, positions, SKShaderTileMode.Clamp);
        ShaderStrokePaint.Shader = shader;

        return ShaderStrokePaint;
    }

    internal static SKPaint GetNoteBonusPaint(CanvasInfo canvasInfo, RenderSettings settings, int colorId, float pixelScale, float rawScale, float opacity)
    {
        if (settings.LowPerformanceMode)
        {
            FlatFillPaint.Color = GetNoteColorBase(colorId).WithAlpha((byte)(opacity * 255));
            
            return FlatFillPaint;
        }
        
        byte alpha = (byte)(opacity * 255);
        ShaderFillPaint.Color = new(0xFF, 0xFF, 0xFF, alpha);
        
        float strokeWidth = NoteStrokeWidths[(int)settings.NoteThickness] * pixelScale;
        
        float noteRadius = canvasInfo.JudgementLineRadius * rawScale;
        float offset = strokeWidth * 0.5f;

        float minRadius = (noteRadius - offset) / canvasInfo.Radius;
        float maxRadius = (noteRadius + offset) / canvasInfo.Radius;
        float delta = maxRadius - minRadius;

        float pos0 = minRadius - delta * 0.10f;
        float pos1 = minRadius + delta * 0.4f;
        float pos2 = minRadius + delta * 0.6f;
        float pos3 = minRadius + delta * 1.10f;

        SKColor colorBase = GetNoteColorBase(colorId);
        SKColor colorLight = GetNoteColorLight(colorId);
        
        SKColor[] colors = [colorLight, colorBase, colorBase, colorLight];
        float[] positions = [pos0, pos1, pos2, pos3];
        
        SKShader shader = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, colors, positions, SKShaderTileMode.Clamp);
        ShaderFillPaint.Shader = shader;
        ShaderFillPaint.BlendMode = SKBlendMode.SrcOver;

        return ShaderFillPaint;
    }
    
    internal static SKPaint GetChainStripePaint(float opacity)
    {
        FlatFillPaint.Color = NoteColorChainStripe.WithAlpha((byte)(opacity * 128));
        return FlatFillPaint;
    }

    internal static SKPaint GetRNotePaint(RenderSettings settings, float scaleScaledByScreen, float opacity)
    {
        if (settings.LowPerformanceMode)
        {
            FlatStrokePaint.Color = NoteColorRNoteGlow.WithAlpha((byte)(255 * opacity * 0.86f));
            FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
            FlatStrokePaint.StrokeWidth = 70 * scaleScaledByScreen;

            return FlatStrokePaint;
        }

        BlurStrokePaint.Color = NoteColorRNoteGlow.WithAlpha((byte)(255 * opacity));
        BlurStrokePaint.StrokeWidth = 70 * scaleScaledByScreen;
        
        float blur = 10 * scaleScaledByScreen;
        BlurStrokePaint.ImageFilter = SKImageFilter.CreateBlur(blur, blur);
        
        return BlurStrokePaint;
    }

    internal static SKPaint GetSnapStrokePaint(int colorId, float pixelScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 2.5f * pixelScale;
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        FlatStrokePaint.Color = GetNoteColorDark(colorId).WithAlpha((byte)(opacity * 255));
        
        return FlatStrokePaint;
    }
    
    internal static SKPaint GetSnapFillPaint(CanvasInfo canvasInfo, RenderSettings settings, int colorId, float rawScale, float opacity, bool flip)
    {
        if (settings.LowPerformanceMode)
        {
            FlatFillPaint.Color = GetNoteColorAverage(colorId).WithAlpha((byte)(opacity * 255));
            return FlatFillPaint;
        }

        byte alpha = (byte)(opacity * 255);
        ShaderFillPaint.Color = new(0xFF, 0xFF, 0xFF, alpha);
        
        float radius = canvasInfo.JudgementLineRadius * rawScale / canvasInfo.Radius;
        
        float radius0 = radius * 0.73f;
        float radius1 = flip ? radius * 0.86f : radius * 0.77f;
        float radius2 = radius * 0.90f;

        SKColor colorBase = GetNoteColorBase(colorId);
        SKColor colorLight = GetNoteColorLight(colorId);
        
        SKColor[] colors = flip ? [colorBase, colorLight, SKColors.White] : [SKColors.White, colorLight, colorBase];
        float[] positions = [radius0, radius1, radius2];
        
        SKShader shader = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, colors, positions, SKShaderTileMode.Clamp);
        ShaderFillPaint.Shader = shader;
        ShaderFillPaint.BlendMode = SKBlendMode.SrcOver;

        return ShaderFillPaint;
    }
    
    internal static SKPaint GetSlideStrokePaint(int colorId, float pixelScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 5f * pixelScale;
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        FlatStrokePaint.Color = GetNoteColorDark(colorId).WithAlpha((byte)(opacity * 255));
        
        return FlatStrokePaint;
    }
    
    internal static SKPaint GetSlideFillPaint(CanvasInfo canvasInfo, RenderSettings settings, int size, int colorId, float opacity, bool flip)
    {
        if (settings.LowPerformanceMode)
        {
            FlatFillPaint.Color = GetNoteColorAverage(colorId).WithAlpha((byte)(opacity * 255));
            return FlatFillPaint;
        }
        
        byte alpha = (byte)(opacity * 255);
        ShaderFillPaint.Color = new(0xFF, 0xFF, 0xFF, alpha);
        
        SKColor colorBase = GetNoteColorBase(colorId);
        SKColor colorLight = GetNoteColorLight(colorId);
        
        SKColor[] colors = flip ? [colorLight, colorBase] : [colorBase, colorLight];
        float[] positions = flip ? [0.0f, 0.4f] : [0.6f, 1.0f];
        
        float sweepAngle = size * 6;
        
        ShaderFillPaint.Shader = SKShader.CreateSweepGradient(canvasInfo.Center, colors, positions, SKShaderTileMode.Decal, 0, sweepAngle);
        ShaderFillPaint.BlendMode = SKBlendMode.SrcOver;

        return ShaderFillPaint;
    }

    internal static SKPaint GetSyncOutlinePaint(float opacity)
    {
        FlatFillPaint.Color = NoteColorSyncOutline.WithAlpha((byte)(opacity * 255));
        return FlatFillPaint;
    }

    internal static SKPaint GetSyncOutlineStrokePaint(float pixelScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 6.36f * pixelScale;
        FlatStrokePaint.Color = NoteColorSyncOutline.WithAlpha((byte)(opacity * 255));

        return FlatStrokePaint;
    }

    internal static SKPaint GetSyncConnectorPaint(CanvasInfo canvasInfo, RenderSettings settings, float pixelScale, float rawScale, float opacity)
    {
        if (settings.LowPerformanceMode)
        {
            FlatStrokePaint.StrokeWidth = 10 * pixelScale;
            FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
            FlatStrokePaint.Color = NoteColorSyncOutline.WithAlpha((byte)(opacity * 255));
            return FlatStrokePaint;
        }
        
        byte alpha = (byte)(opacity * 255);
        ShaderStrokePaint.Color = new(0xFF, 0xFF, 0xFF, alpha);
        
        float strokeWidth = 10 * pixelScale;
        ShaderStrokePaint.StrokeWidth = strokeWidth;
    
        float noteRadius = canvasInfo.JudgementLineRadius * rawScale;
        float offset = strokeWidth * 0.5f;

        float minRadius = (noteRadius - offset) / canvasInfo.Radius;
        float maxRadius = (noteRadius + offset) / canvasInfo.Radius;
        float delta = maxRadius - minRadius;

        float pos0 = minRadius + delta * 0.05f;
        float pos1 = minRadius + delta * 0.15f;
        float pos2 = minRadius + delta * 0.45f;
        float pos3 = minRadius + delta * 0.55f;
        float pos4 = minRadius + delta * 0.85f;
        float pos5 = minRadius + delta * 0.95f;
        
        SKColor[] colors = [NoteColorSyncConnectorLight, NoteColorSyncConnectorBase, NoteColorSyncConnectorDark, NoteColorSyncConnectorDark, NoteColorSyncConnectorBase, NoteColorSyncConnectorLight];
        float[] positions = [pos0, pos1, pos2, pos3, pos4, pos5];
        
        SKShader shader = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, colors, positions, SKShaderTileMode.Clamp);
        ShaderStrokePaint.Shader = shader;

        return ShaderStrokePaint;
    }

    internal static SKPaint GetMeasureLinePaint(CanvasInfo canvasInfo, float linearScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 3 * canvasInfo.Scale * Math.Min(1, linearScale * 1.5f);
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        FlatStrokePaint.Color = NoteColorMeasureLine.WithAlpha((byte)(opacity * 255));

        return FlatStrokePaint;
    }

    internal static SKPaint GetBeatLinePaint(CanvasInfo canvasInfo, float linearScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 1.5f * canvasInfo.Scale * Math.Min(1, linearScale * 1.5f);
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        FlatStrokePaint.Color = NoteColorBeatLine.WithAlpha((byte)(opacity * 90));

        return FlatStrokePaint;
    }

    internal static SKPaint GetJudgementLinePaint(CanvasInfo canvasInfo, RenderSettings settings)
    {
        float strokeWidth = (NoteStrokeWidths[(int)settings.NoteThickness] + 2) * canvasInfo.Scale;
        float centerOffset = strokeWidth * 0.08f;
        float lineOffset = strokeWidth * 0.5f;
        float glowOffset = strokeWidth * 0.875f;
        
        float radius0 = canvasInfo.JudgementLineRadius - glowOffset;
        float radius1 = canvasInfo.JudgementLineRadius - lineOffset;
        float radius2 = canvasInfo.JudgementLineRadius - lineOffset + 1f;
        float radius3 = canvasInfo.JudgementLineRadius - centerOffset;
        float radius4 = canvasInfo.JudgementLineRadius + centerOffset;
        float radius5 = canvasInfo.JudgementLineRadius + lineOffset - 1f;
        float radius6 = canvasInfo.JudgementLineRadius + lineOffset;
        float radius7 = canvasInfo.JudgementLineRadius + glowOffset;
        
        radius0 /= canvasInfo.Radius;
        radius1 /= canvasInfo.Radius;
        radius2 /= canvasInfo.Radius;
        radius3 /= canvasInfo.Radius;
        radius4 /= canvasInfo.Radius;
        radius5 /= canvasInfo.Radius;
        radius6 /= canvasInfo.Radius;
        radius7 /= canvasInfo.Radius;
        
        ShaderStrokePaint.StrokeWidth = strokeWidth * 1.75f;
        ShaderStrokePaint.Color = new(0xFFFFFFFF);
        
        int id = (int)settings.JudgementLineColor;
        SKColor sweepColorA = new(NoteColors.JudgeLineStartFromId(id));
        SKColor sweepColorB = new(NoteColors.JudgeLineEndFromId(id));
        float angle = NoteColors.JudgeLineTiltFromId(id);

        SKColor[] sweepColors = [sweepColorA, sweepColorB, sweepColorA, sweepColorB, sweepColorA];
        SKColor[] shadeColors = [JudgementLineShadeColorA, JudgementLineShadeColorB, JudgementLineShadeColorC, JudgementLineShadeColorD, JudgementLineShadeColorD, JudgementLineShadeColorC, JudgementLineShadeColorB, JudgementLineShadeColorA];
        float[] shadePositions = [radius0, radius1, radius2, radius3, radius4, radius5, radius6, radius7];
        
        SKShader sweepGradient = SKShader.CreateSweepGradient(canvasInfo.Center, sweepColors, SKShaderTileMode.Repeat, 0 + angle, 360 + angle);
        SKShader shadeGradient = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, shadeColors, shadePositions, SKShaderTileMode.Clamp);
        ShaderStrokePaint.Shader = SKShader.CreateCompose(shadeGradient, sweepGradient, SKBlendMode.Modulate);
        
        return ShaderStrokePaint;
    }

    internal static SKPaint GetGuideLinePaint(CanvasInfo canvasInfo)
    {
        ShaderStrokePaint.Color = new(0xFFFFFFFF);
        ShaderStrokePaint.StrokeWidth = 1.5f * canvasInfo.Scale;
        
        SKColor[] colors = [new(NoteColors.LaneGuideLine), new(0x10000000 + NoteColors.LaneGuideLine), new(0x50000000 + NoteColors.LaneGuideLine)];
        float[] positions = [0.15f, 0.2f, 0.7f];
        
        ShaderStrokePaint.Shader = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.JudgementLineRadius, colors, positions, SKShaderTileMode.Clamp);
        return ShaderStrokePaint;
    }

    internal static SKPaint GetLanePaint(CanvasInfo canvasInfo, RenderSettings settings, float time)
    {
        if (settings.LowPerformanceMode)
        {
            FlatFillPaint.Color = NoteColorLaneBaseA;
            return FlatFillPaint;
        }
        
        ShaderFillPaint.Color = new(0xFFFFFFFF);
        
        SKColor[] alphaColors = [new(0x00FFFFFF), new(0x60FFFFFF), new(0xEEFFFFFF)];
        float[] alphaPositions = [0.1f, 0.25f, 0.75f];

        const int stripeCount = 9;
        const float interval = 1.0f / stripeCount;

        const float speed = 0.0004f;
        float t = (time * speed) % interval * 2;
        
        SKColor[] scrollColors = new SKColor[stripeCount * 2 + 2];
        float[] scrollPositions = new float[stripeCount * 2 + 2];
        
        for (int i = 0; i <= stripeCount; i++)
        {
            bool even = i % 2 == 0;

            scrollColors[i * 2] = even ? NoteColorLaneBaseA : NoteColorLaneBaseB;
            scrollColors[i * 2 + 1] = even ? NoteColorLaneBaseB : NoteColorLaneBaseA;

            scrollPositions[i * 2] = RenderUtils.Perspective((i - 1) * interval + t);
            scrollPositions[i * 2 + 1] = scrollPositions[i * 2] + 0.001f;
        }
        
        SKShader alphaGradient = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, alphaColors, alphaPositions, SKShaderTileMode.Clamp);
        SKShader colorGradient = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, scrollColors, scrollPositions, SKShaderTileMode.Clamp);
        ShaderFillPaint.Shader = SKShader.CreateCompose(colorGradient, alphaGradient, SKBlendMode.Modulate);
        ShaderFillPaint.BlendMode = SKBlendMode.SrcOver;
        return ShaderFillPaint;
    }

    internal static SKPaint GetHoldEndBaseStrokePaint(int colorId, float pixelScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 20 * pixelScale;
        FlatStrokePaint.StrokeCap = SKStrokeCap.Round;
        FlatStrokePaint.Color = GetNoteColorHoldEndLight(colorId).WithAlpha((byte)(opacity * 255));
        
        return FlatStrokePaint;
    }

    internal static SKPaint GetHoldEndBaseFillPaint(int colorId, float opacity)
    {
        FlatFillPaint.Color = GetNoteColorHoldEndLight(colorId).WithAlpha((byte)(opacity * 255));
        return FlatFillPaint;
    }

    internal static SKPaint GetHoldEndOutlinePaint(int colorId, float pixelScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 3.5f * pixelScale;
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        FlatStrokePaint.Color = GetNoteColorHoldEndDark(colorId).WithAlpha((byte)(opacity * 255));
        
        return FlatStrokePaint;
    }

    internal static SKPaint GetHoldPointPaint(RenderSettings settings, float pixelScale, HoldPointRenderType renderType, float opacity)
    {
        FlatStrokePaint.StrokeWidth = settings.LowPerformanceMode
        ? 14f * pixelScale
        : 4f * pixelScale;
        
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        
        FlatStrokePaint.Color = renderType == HoldPointRenderType.Visible
            ? new(0xEE, 0xEE, 0xEE, (byte)(opacity * 255))
            : new(0x55, 0x55, 0x55, (byte)(opacity * 255));

        return FlatStrokePaint;
    }

    internal static SKPaint GetHoldSurfacePaint(bool active, float opacity)
    {
        if (active)
        {
            HoldSurfacePaintActive.Color = new(0xFF, 0xFF, 0xFF, (byte)(opacity * 207));
            return HoldSurfacePaintActive;
        }

        HoldSurfacePaint.Color = new(0xFF, 0xFF, 0xFF, (byte)(opacity * 207));
        return HoldSurfacePaint;
    }

    internal static SKPaint GetEventMarkerPaint(Event @event, float pixelScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 10 * pixelScale;
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        FlatStrokePaint.Color = GetEventColor(@event).WithAlpha((byte)(opacity * 255));

        return FlatStrokePaint;
    }

    internal static SKPaint GetEventMarkerFillPaint(CanvasInfo canvasInfo, Event @event, float rawScale)
    {
        ShaderFillPaint.Color = new(0xFFFFFFFF);
        
        SKColor color = GetEventColor(@event);
        
        SKColor[] colors = [color.WithAlpha(0x00), color.WithAlpha(0x32)];
        float[] positions = [rawScale * 0.75f, rawScale];
        
        ShaderFillPaint.Shader = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.JudgementLineRadius, colors, positions, SKShaderTileMode.Clamp);
        ShaderFillPaint.BlendMode = SKBlendMode.SrcOver;
        return ShaderFillPaint;
    }

    internal static SKPaint GetEventAreaPaint(Event @event, float opacity)
    {
        FlatFillPaint.Color = GetEventColor(@event).WithAlpha((byte)(opacity * 70));

        return FlatFillPaint;
    }
    
    internal static SKPaint GetLaneTogglePaint(bool state, float pixelScale, float opacity)
    {
        FlatStrokePaint.StrokeWidth = 34 * pixelScale;
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        FlatStrokePaint.Color = state ? NoteColorLaneShow.WithAlpha((byte)(opacity * 255)) : NoteColorLaneHide.WithAlpha((byte)(opacity * 255));

        return FlatStrokePaint;
    }

    internal static SKPaint GetLaneToggleFillPaint(bool state, float opacity)
    {
        FlatFillPaint.Color = state ? NoteColorLaneShow.WithAlpha((byte)(opacity * 100)) : NoteColorLaneHide.WithAlpha((byte)(opacity * 100));

        return FlatFillPaint;
    }
    
    internal static SKPaint GetSongTimerPaint(float pixelScale)
    {
        FlatStrokePaint.StrokeWidth = 10.5f * pixelScale;
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;
        FlatStrokePaint.Color = new(0x80000000);
        
        return FlatStrokePaint;
    }
    
    internal static SKPaint GetTextPaint(uint color)
    {
        FlatFillPaint.Color = new(color);
        
        return FlatFillPaint;
    }

    internal static SKPaint GetTextPaint(SKColor color)
    {
        FlatFillPaint.Color = color;
        
        return FlatFillPaint;
    }

    internal static SKPaint GetBonusSweepEffectPaint(CanvasInfo canvasInfo, bool isCounterClockwise)
    {
        SKShader sweepGradient = SKShader.CreateSweepGradient(canvasInfo.Center, isCounterClockwise ? BonusSweepEffectColorsCounterclockwise : BonusSweepEffectColorsClockwise, BonusSweepEffectPositions, SKShaderTileMode.Decal, 0, 90);
        SKShader radialGradient = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, [new(0x00FFFFFF), new(0xFFFFFFFF)], [0.1f, 1.0f], SKShaderTileMode.Clamp);

        ShaderFillPaint.Color = new(0xFFFFFFFF);
        ShaderFillPaint.Shader = SKShader.CreateCompose(sweepGradient, radialGradient, SKBlendMode.Modulate);
        ShaderFillPaint.BlendMode = SKBlendMode.SrcOver;
        
        return ShaderFillPaint;
    }

    internal static SKPaint GetObjectOutlineStrokePaint(bool selected, bool pointerOver)
    {
        FlatStrokePaint.StrokeWidth = 1;
        FlatStrokePaint.StrokeCap = SKStrokeCap.Butt;

        if (selected && pointerOver)
        {
            FlatStrokePaint.Color = PointerOverSelectedColorStroke;
        }
        else if (selected)
        {
            FlatStrokePaint.Color = SelectedColorStroke;
        }
        else
        {
            FlatStrokePaint.Color = PointerOverColorStroke;
        }
        
        return FlatStrokePaint;
    }

    internal static SKPaint GetObjectOutlineFillPaint(bool selected, bool pointerOver)
    {
        if (selected && pointerOver)
        {
            FlatFillPaint.Color = PointerOverSelectedColorFill;
        }
        else if (selected)
        {
            FlatFillPaint.Color = SelectedColorFill;
        }
        else
        {
            FlatFillPaint.Color = PointerOverColorFill;
        }
       
        return FlatFillPaint;
    }

    internal static SKPaint GetRNoteFillPaint(CanvasInfo canvasInfo, RenderSettings settings, float angle, float opacity)
    {
        byte hexOpacity = (byte)(opacity * 255);
        SKColor[] colors = [new(0x18, 0xBB, 0xFF, hexOpacity), new(0xFF, 0x1D, 0x8C, hexOpacity), new(0xFF, 0xCB, 0x00, hexOpacity), new(0xFF, 0x1D, 0x8C, hexOpacity), new(0x18, 0xBB, 0xFF, hexOpacity)];
        float[] positions = [0, 0.25f, 0.5f, 0.75f, 1];

        ShaderFillPaint.Shader = SKShader.CreateSweepGradient(canvasInfo.Center, colors, positions, SKShaderTileMode.Repeat, angle, angle + 360);
        ShaderFillPaint.BlendMode = SKBlendMode.Plus;
        
        return ShaderFillPaint;
    }

    internal static SKPaint GetRNoteGlowPaint(CanvasInfo canvasInfo, RenderSettings settings, float angle, float inner, float outer, float opacity)
    {
        SKColor[] colors0 = [new(0xFF18BBFF), new(0xFFFF1D8C), new(0xFFFFCB00), new(0xFFFF1D8C), new(0xFF18BBFF)];
        float[] positions0 = [0, 0.25f, 0.5f, 0.75f, 1];

        SKColor[] colors1 = [new(0x00FFFFFF), new(0xFF, 0xFF, 0xFF, (byte)(255 * opacity)), new(0x00FFFFFF)];
        float[] positions1 = [inner, (inner + outer) * 0.5f, outer];
        
        SKShader shader0 = SKShader.CreateSweepGradient(canvasInfo.Center, colors0, positions0, SKShaderTileMode.Repeat, angle, angle + 360);
        SKShader shader1 = SKShader.CreateRadialGradient(canvasInfo.Center, canvasInfo.Radius, colors1, positions1, SKShaderTileMode.Clamp);

        ShaderFillPaint.Shader = SKShader.CreateCompose(shader0, shader1, SKBlendMode.Modulate);
        ShaderFillPaint.BlendMode = SKBlendMode.Plus;

        return ShaderFillPaint;
    }

    internal static SKPaint GetBackgroundPaint(BackgroundOption backgroundOption, Difficulty difficulty, bool clear)
    {
        return clear
            ? backgroundOption switch
            {
                BackgroundOption.Auto => difficulty == Difficulty.Inferno ? BackgroundBossClearPaint : BackgroundVersion3ClearPaint,
                BackgroundOption.Saturn => BackgroundVersion3ClearPaint,
                BackgroundOption.Version3 => BackgroundVersion3ClearPaint,
                BackgroundOption.Version2 => BackgroundVersion3ClearPaint,
                BackgroundOption.Version1 => BackgroundVersion3ClearPaint,
                BackgroundOption.Boss => BackgroundBossClearPaint,
                BackgroundOption.StageUp => BackgroundVersion3ClearPaint,
                BackgroundOption.WorldsEnd => BackgroundVersion3ClearPaint,
                BackgroundOption.Jacket => BackgroundVersion3ClearPaint,
                _ => BackgroundVersion3ClearPaint,
            }
            : backgroundOption switch
            {
                BackgroundOption.Auto => difficulty == Difficulty.Inferno ? BackgroundBossPaint : BackgroundVersion3Paint,
                BackgroundOption.Saturn => BackgroundVersion3Paint,
                BackgroundOption.Version3 => BackgroundVersion3Paint,
                BackgroundOption.Version2 => BackgroundVersion3Paint,
                BackgroundOption.Version1 => BackgroundVersion3Paint,
                BackgroundOption.Boss => BackgroundBossPaint,
                BackgroundOption.StageUp => BackgroundVersion3Paint,
                BackgroundOption.WorldsEnd => BackgroundVersion3Paint,
                BackgroundOption.Jacket => BackgroundVersion3Paint,
                _ => BackgroundVersion3Paint,
            };
    }

    internal static SKPaint GetMarvelousTimingWindowPaint(bool late, float opacity)
    {
        int baseOpacity = late ? 100 : 150;
        FlatFillPaint.Color = new(0xFF, 0x00, 0x40, (byte)(baseOpacity * opacity));
        return FlatFillPaint;
    }
    
    internal static SKPaint GetGreatTimingWindowPaint(bool late, float opacity)
    {
        int baseOpacity = late ? 100 : 150;
        FlatFillPaint.Color = new(0x00, 0xFF, 0x00, (byte)(baseOpacity * opacity));
        return FlatFillPaint;
    }
    
    internal static SKPaint GetGoodTimingWindowPaint(bool late, float opacity)
    {
        int baseOpacity = late ? 100 : 150;
        FlatFillPaint.Color = new(0x00, 0xB3, 0xFF, (byte)(baseOpacity * opacity));
        return FlatFillPaint;
    }
    
    internal static SKFont GetBoldFont(float scale)
    {
        InterfaceFont.Size = scale;
        InterfaceFont.Embolden = true;
        
        return InterfaceFont;
    }
    
    internal static SKFont GetStandardFont(float scale)
    {
        InterfaceFont.Size = scale;
        InterfaceFont.Embolden = false;
        
        return InterfaceFont;
    }
}