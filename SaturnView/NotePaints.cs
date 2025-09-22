using SkiaSharp;

namespace SaturnView;

internal static class NotePaints
{
    #region Color Definitions
    private static readonly SKColor NoteColorCapLight = new(NoteColors.CapLight);
    private static readonly SKColor NoteColorCapBase  = new(NoteColors.CapBase);
    private static readonly SKColor NoteColorCapDark  = new(NoteColors.CapDark);

    private static readonly SKColor NoteColorRNoteGlow = new(NoteColors.RNoteGlow);
    
    private static readonly SKColor NoteColorLightMagentaAverage = new(NoteColors.LightMagentaAverage);
    private static readonly SKColor NoteColorLightMagentaBase    = new(NoteColors.LightMagentaBase);
    private static readonly SKColor NoteColorLightMagentaLight   = new(NoteColors.LightMagentaLight);
    private static readonly SKColor NoteColorLightMagentaDark    = new(NoteColors.LightMagentaDark);
    
    private static readonly SKColor NoteColorLightYellowAverage = new(NoteColors.LightYellowAverage);
    private static readonly SKColor NoteColorLightYellowBase    = new(NoteColors.LightYellowBase);
    private static readonly SKColor NoteColorLightYellowLight   = new(NoteColors.LightYellowLight);
    private static readonly SKColor NoteColorLightYellowDark    = new(NoteColors.LightYellowDark);
    
    private static readonly SKColor NoteColorOrangeAverage = new(NoteColors.OrangeAverage);
    private static readonly SKColor NoteColorOrangeBase    = new(NoteColors.OrangeBase);
    private static readonly SKColor NoteColorOrangeLight   = new(NoteColors.OrangeLight);
    private static readonly SKColor NoteColorOrangeDark    = new(NoteColors.OrangeDark);
    
    private static readonly SKColor NoteColorLimeAverage = new(NoteColors.LimeAverage);
    private static readonly SKColor NoteColorLimeBase    = new(NoteColors.LimeBase);
    private static readonly SKColor NoteColorLimeLight   = new(NoteColors.LimeLight);
    private static readonly SKColor NoteColorLimeDark    = new(NoteColors.LimeDark);
    
    private static readonly SKColor NoteColorRedAverage = new(NoteColors.RedAverage);
    private static readonly SKColor NoteColorRedBase    = new(NoteColors.RedBase);
    private static readonly SKColor NoteColorRedLight   = new(NoteColors.RedLight);
    private static readonly SKColor NoteColorRedDark    = new(NoteColors.RedDark);
    
    private static readonly SKColor NoteColorSkyBlueAverage = new(NoteColors.SkyBlueAverage);
    private static readonly SKColor NoteColorSkyBlueBase    = new(NoteColors.SkyBlueBase);
    private static readonly SKColor NoteColorSkyBlueLight   = new(NoteColors.SkyBlueLight);
    private static readonly SKColor NoteColorSkyBlueDark    = new(NoteColors.SkyBlueDark);
    
    private static readonly SKColor NoteColorDarkYellowAverage = new(NoteColors.DarkYellowAverage);
    private static readonly SKColor NoteColorDarkYellowBase    = new(NoteColors.DarkYellowBase);
    private static readonly SKColor NoteColorDarkYellowLight   = new(NoteColors.DarkYellowLight);
    private static readonly SKColor NoteColorDarkYellowDark    = new(NoteColors.DarkYellowDark);
    
    private static readonly SKColor NoteColorLightRedAverage = new(NoteColors.LightRedAverage);
    private static readonly SKColor NoteColorLightRedBase    = new(NoteColors.LightRedBase);
    private static readonly SKColor NoteColorLightRedLight   = new(NoteColors.LightRedLight);
    private static readonly SKColor NoteColorLightRedDark    = new(NoteColors.LightRedDark);
    
    private static readonly SKColor NoteColorYellowAverage = new(NoteColors.YellowAverage);
    private static readonly SKColor NoteColorYellowBase    = new(NoteColors.YellowBase);
    private static readonly SKColor NoteColorYellowLight   = new(NoteColors.YellowLight);
    private static readonly SKColor NoteColorYellowDark    = new(NoteColors.YellowDark);
    
    private static readonly SKColor NoteColorPureGreenAverage = new(NoteColors.PureGreenAverage);
    private static readonly SKColor NoteColorPureGreenBase    = new(NoteColors.PureGreenBase);
    private static readonly SKColor NoteColorPureGreenLight   = new(NoteColors.PureGreenLight);
    private static readonly SKColor NoteColorPureGreenDark    = new(NoteColors.PureGreenDark);
    
    private static readonly SKColor NoteColorBrightBlueAverage = new(NoteColors.BrightBlueAverage);
    private static readonly SKColor NoteColorBrightBlueBase    = new(NoteColors.BrightBlueBase);
    private static readonly SKColor NoteColorBrightBlueLight   = new(NoteColors.BrightBlueLight);
    private static readonly SKColor NoteColorBrightBlueDark    = new(NoteColors.BrightBlueDark);
    
    private static readonly SKColor NoteColorLightBlueAverage = new(NoteColors.LightBlueAverage);
    private static readonly SKColor NoteColorLightBlueBase    = new(NoteColors.LightBlueBase);
    private static readonly SKColor NoteColorLightBlueLight   = new(NoteColors.LightBlueLight);
    private static readonly SKColor NoteColorLightBlueDark    = new(NoteColors.LightBlueDark);
    
    private static readonly SKColor NoteColorLightGrayAverage = new(NoteColors.LightGrayAverage);
    private static readonly SKColor NoteColorLightGrayBase    = new(NoteColors.LightGrayBase);
    private static readonly SKColor NoteColorLightGrayLight   = new(NoteColors.LightGrayLight);
    private static readonly SKColor NoteColorLightGrayDark    = new(NoteColors.LightGrayDark);

    private static readonly SKColor NoteColorChainStripe = new(NoteColors.ChainStripe);
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

    internal static readonly float[] NoteStrokeWidths     = [ 18.0f, 24.0f, 36.0f, 48.0f, 60.0f ];
    internal static readonly float RNoteStrokeWidth = 36.0f;
    
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
    };

    private static readonly SKPaint ShaderStrokePaint = new()
    {
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Butt,
        Style = SKPaintStyle.Stroke,
    };

    private static readonly SKPaint FillPaint = new()
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
    
    internal static SKPaint GetNoteCapPaint(CanvasInfo canvasInfo, RenderSettings settings, float scaleScaledByScreen, float rawScale, float opacity)
    {
        if (settings.LowPerformanceMode)
        {
            FlatStrokePaint.StrokeWidth = NoteStrokeWidths[(int)settings.NoteThickness] * scaleScaledByScreen;
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
        
        float noteRadius = canvasInfo.ScaledRadius * rawScale;
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
    
    internal static SKPaint GetNoteBasePaint(CanvasInfo canvasInfo, RenderSettings settings, int colorId, float scaleScaledByScreen, float rawScale, float opacity)
    {
        if (settings.LowPerformanceMode)
        {
            FlatStrokePaint.StrokeWidth = NoteStrokeWidths[(int)settings.NoteThickness] * scaleScaledByScreen;
            FlatStrokePaint.Color = GetNoteColorAverage(colorId).WithAlpha((byte)(opacity * 255));
            
            return FlatStrokePaint;
        }
        
        byte alpha = (byte)(opacity * 255);
        ShaderStrokePaint.Color = new(0xFF, 0xFF, 0xFF, alpha);
        
        float strokeWidth = NoteStrokeWidths[(int)settings.NoteThickness] * scaleScaledByScreen;
        ShaderStrokePaint.StrokeWidth = strokeWidth;
    
        float noteRadius = canvasInfo.ScaledRadius * rawScale;
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

    internal static SKPaint GetChainStripePaint()
    {
        FillPaint.Color = NoteColorChainStripe;
        return FillPaint;
    }

    internal static SKPaint GetRNotePaint(RenderSettings settings, float scaleScaledByScreen, float opacity)
    {
        if (settings.LowPerformanceMode)
        {
            FlatStrokePaint.Color = NoteColorRNoteGlow.WithAlpha((byte)(255 * opacity * 0.86f));
            FlatStrokePaint.StrokeWidth = 70 * scaleScaledByScreen;

            return FlatStrokePaint;
        }

        BlurStrokePaint.Color = NoteColorRNoteGlow.WithAlpha((byte)(255 * opacity));
        BlurStrokePaint.StrokeWidth = 70 * scaleScaledByScreen;
        
        float blur = 10 * scaleScaledByScreen;
        BlurStrokePaint.ImageFilter = SKImageFilter.CreateBlur(blur, blur);
        
        return BlurStrokePaint;
    }
}