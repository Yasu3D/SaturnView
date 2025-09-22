using System.Drawing;

namespace SaturnView;

public static class NoteColors
{
    public const uint CapLight  = 0xFF79E5FF;
    public const uint CapBase = 0xFF4EACF7;
    public const uint CapDark  = 0xFF0093E7;

    public const uint ChainStripe = 0x80000000;
    public const uint RNoteGlow = 0xFFFFFFC0;
    
    public const uint LightMagentaAverage  = 0xFFFF09DC;
    public const uint LightMagentaBase = 0xFFFF4AEE;
    public const uint LightMagentaDark  = 0xFFD10AB7;
    public const uint LightMagentaLight  = 0xFFFFC4FF;

    public const uint LightYellowAverage  = 0xFFFFC60E;
    public const uint LightYellowBase = 0xFFFFE452;
    public const uint LightYellowDark  = 0xFFD1A60F;
    public const uint LightYellowLight  = 0xFFFFFFCC;
    
    public const uint OrangeAverage  = 0xFFFF5200;
    public const uint OrangeBase = 0xFFFF9A00;
    public const uint OrangeDark  = 0xFFD14A00;
    public const uint OrangeLight  = 0xFFFFFFA0;
    
    public const uint LimeAverage  = 0xFF03A602;
    public const uint LimeBase = 0xFF39D234;
    public const uint LimeDark  = 0xFF028D01;
    public const uint LimeLight  = 0xFFB7FFB4;
    
    public const uint RedAverage  = 0xFFBA0101;
    public const uint RedBase = 0xFFDD2C2C;
    public const uint RedDark  = 0xFF9D0000;
    public const uint RedLight  = 0xFFFFAEAE;
    
    public const uint SkyBlueAverage  = 0xFF026BFF;
    public const uint SkyBlueBase = 0xFF34ADFF;
    public const uint SkyBlueDark  = 0xFF015ED1;
    public const uint SkyBlueLight  = 0xFFB4FFFF;
    
    public const uint DarkYellowAverage  = 0xFF544200;
    public const uint DarkYellowBase = 0xFF9C8D00;
    public const uint DarkYellowDark  = 0xFF4B3C00;
    public const uint DarkYellowLight  = 0xFFFFFFA0;
    
    public const uint LightRedAverage  = 0xFFFF1200;
    public const uint LightRedBase = 0xFFFF5800;
    public const uint LightRedDark  = 0xFFD11200;
    public const uint LightRedLight  = 0xFFFFD2A0;
    
    public const uint YellowAverage  = 0xFFFFE000;
    public const uint YellowBase = 0xFFFFF100;
    public const uint YellowDark  = 0xFFD1BA00;
    public const uint YellowLight  = 0xFFFFFFA0;
    
    public const uint PureGreenAverage  = 0xFF095B25;
    public const uint PureGreenBase = 0xFF4AA170;
    public const uint PureGreenDark  = 0xFF0A5123;
    public const uint PureGreenLight  = 0xFFC4FFEC;
    
    public const uint BrightBlueAverage  = 0xFF000DFF;
    public const uint BrightBlueBase = 0xFF0051FF;
    public const uint BrightBlueDark  = 0xFF000ED1;
    public const uint BrightBlueLight  = 0xFFA0CBFF;
    
    public const uint LightBlueAverage  = 0xFF229AFF;
    public const uint LightBlueBase = 0xFF6DCBFF;
    public const uint LightBlueDark  = 0xFF2184D1;
    public const uint LightBlueLight  = 0xFFE8FFFF;
    
    public const uint LightGrayAverage  = 0xFF939398;
    public const uint LightGrayBase = 0xFFC7C7CA;
    public const uint LightGrayDark  = 0xFF7E7E82;
    public const uint LightGrayLight  = 0xFFFFFFFF;

    public static uint AverageNoteColorFromId(int id)
    {
        return id switch
        {
            0  => LightMagentaAverage,
            1  => LightYellowAverage,
            2  => OrangeAverage,
            3  => LimeAverage,
            4  => RedAverage,
            5  => SkyBlueAverage,
            6  => DarkYellowAverage,
            7  => LightRedAverage,
            8  => YellowAverage,
            9  => PureGreenAverage,
            10 => BrightBlueAverage,
            11 => LightBlueAverage,
            12 => LightGrayAverage,
            _  => LightMagentaAverage,
        };
    }
    
    public static uint BaseNoteColorFromId(int id)
    {
        return id switch
        {
            0  => LightMagentaBase,
            1  => LightYellowBase,
            2  => OrangeBase,
            3  => LimeBase,
            4  => RedBase,
            5  => SkyBlueBase,
            6  => DarkYellowBase,
            7  => LightRedBase,
            8  => YellowBase,
            9  => PureGreenBase,
            10 => BrightBlueBase,
            11 => LightBlueBase,
            12 => LightGrayBase,
            _  => LightMagentaBase,
        };
    }
    
    public static uint LightNoteColorFromId(int id)
    {
        return id switch
        {
            0  => LightMagentaLight,
            1  => LightYellowLight,
            2  => OrangeLight,
            3  => LimeLight,
            4  => RedLight,
            5  => SkyBlueLight,
            6  => DarkYellowLight,
            7  => LightRedLight,
            8  => YellowLight,
            9  => PureGreenLight,
            10 => BrightBlueLight,
            11 => LightBlueLight,
            12 => LightGrayLight,
            _  => LightMagentaLight,
        };
    }
    
    public static uint DarkNoteColorFromId(int id)
    {
        return id switch
        {
            0  => LightMagentaDark,
            1  => LightYellowDark,
            2  => OrangeDark,
            3  => LimeDark,
            4  => RedDark,
            5  => SkyBlueDark,
            6  => DarkYellowDark,
            7  => LightRedDark,
            8  => YellowDark,
            9  => PureGreenDark,
            10 => BrightBlueDark,
            11 => LightBlueDark,
            12 => LightGrayDark,
            _  => LightMagentaDark,
        };
    }
}