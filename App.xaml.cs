using System.Globalization;
using AplikacjaMobilna.Services;

namespace AplikacjaMobilna;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var polishCulture = new CultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentCulture = polishCulture;
        CultureInfo.DefaultThreadCurrentUICulture = polishCulture;

        ThemeService.ApplyTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}