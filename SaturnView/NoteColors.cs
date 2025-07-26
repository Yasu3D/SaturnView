using System.Drawing;

namespace SaturnView;

public static class NoteColors
{
    public const uint LightMagentaBase = 0xFFFF09DC;
    public const uint LightMagentaLight = 0xFFFF4AEE;
    public const uint LightMagentaDark = 0xFFD10AB7;

    public const uint LightYellowBase = 0xFFFFC60E;
    public const uint LightYellowLight = 0xFFFFE452;
    public const uint LightYellowDark = 0xFFD1A60F;
    
    public const uint OrangeBase = 0xFFFF5200;
    public const uint OrangeLight = 0xFFFF9A00;
    public const uint OrangeDark = 0xFFD14A00;
    
    public const uint LimeBase = 0xFF03A602;
    public const uint LimeLight = 0xFF39D234;
    public const uint LimeDark = 0xFF028D01;
    
    public const uint RedBase = 0xFFBA0101;
    public const uint RedLight = 0xFFDD2C2C;
    public const uint RedDark = 0xFF9D0000;
    
    public const uint SkyBlueBase = 0xFF026BFF;
    public const uint SkyBlueLight = 0xFF34ADFF;
    public const uint SkyBlueDark = 0xFF015ED1;
    
    public const uint DarkYellowBase = 0xFF544200;
    public const uint DarkYellowLight = 0xFF9C8D00;
    public const uint DarkYellowDark = 0xFF4B3C00;
    
    public const uint LightRedBase = 0xFFFF1200;
    public const uint LightRedLight = 0xFFFF5800;
    public const uint LightRedDark = 0xFFD11200;
    
    public const uint YellowBase = 0xFFFFE000;
    public const uint YellowLight = 0xFFFFF100;
    public const uint YellowDark = 0xFFD1BA00;
    
    public const uint PureGreenBase = 0xFF095B25;
    public const uint PureGreenLight = 0xFF4AA170;
    public const uint PureGreenDark = 0xFF0A5123;
    
    public const uint BrightBlueBase = 0xFF000DFF;
    public const uint BrightBlueLight = 0xFF0051FF;
    public const uint BrightBlueDark = 0xFF000ED1;
    
    public const uint LightBlueBase = 0xFF229AFF;
    public const uint LightBlueLight = 0xFF6DCBFF;
    public const uint LightBlueDark = 0xFF2184D1;
    
    public const uint LightGrayBase = 0xFF939398;
    public const uint LightGrayLight = 0xFFC7C7CA;
    public const uint LightGrayDark = 0xFF7E7E82;

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