namespace AplikacjaMobilna.Services;

public static class ThemeService
{
    private const string DarkModeKey = "IsDarkMode";
    private const string FontScaleKey = "FontScale"; 

    public static bool IsDarkMode
    {
        get => Preferences.Default.Get(DarkModeKey, false);
        set
        {
            Preferences.Default.Set(DarkModeKey, value);
            ApplyTheme();
        }
    }

    public static double FontScale
    {
        get => Preferences.Default.Get(FontScaleKey, 1.0);
        set
        {
            Preferences.Default.Set(FontScaleKey, value);
            ApplyTheme();
        }
    }

    public static void ApplyTheme()
    {
        var resources = Application.Current?.Resources;
        if (resources == null) return;

        bool dark = IsDarkMode;
        double scale = FontScale;

        // Kolory globalne
        resources["AppBackgroundColor"] = dark ? Color.FromArgb("#000000") : Color.FromArgb("#F1F5F9");
        resources["CardBackgroundColor"] = dark ? Color.FromArgb("#121212") : Color.FromArgb("#FFFFFF");
        resources["SecondaryCardColor"] = dark ? Color.FromArgb("#1E1E1E") : Color.FromArgb("#F8FAFC");
        resources["CardBorderColor"] = dark ? Color.FromArgb("#333333") : Color.FromArgb("#E2E8F0");
        resources["PrimaryTextColor"] = dark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#0F172A");
        resources["SecondaryTextColor"] = dark ? Color.FromArgb("#CCCCCC") : Color.FromArgb("#64748B");
        resources["AccentColor"] = dark ? Color.FromArgb("#14B8A6") : Color.FromArgb("#0F766E");
        resources["CaregiverCardBg"] = dark ? Color.FromArgb("#172554") : Color.FromArgb("#EFF6FF");
        resources["CaregiverCardBorder"] = dark ? Color.FromArgb("#1D4ED8") : Color.FromArgb("#BFDBFE");
        resources["CaregiverTitleColor"] = dark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#1E3A8A");
        resources["CaregiverSubColor"] = dark ? Color.FromArgb("#93C5FD") : Color.FromArgb("#2563EB");
        // Kolory paska nawigacji
        resources["NavBarBgColor"] = dark ? Color.FromArgb("#1E1E1E") : Color.FromArgb("#0F766E");
        resources["NavBarTextColor"] = Color.FromArgb("#FFFFFF");

        // Rozmiary czcionek skalowane dynamicznie
        resources["FontSmall"] = 12.0 * scale;
        resources["FontBody"] = 15.0 * scale;
        resources["FontMedium"] = 18.0 * scale;
        resources["FontLarge"] = 22.0 * scale;
        resources["FontTitle"] = 26.0 * scale;
        resources["FontHero"] = 38.0 * scale;
    }
}