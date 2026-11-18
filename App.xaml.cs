using AplikacjaMobilna.Services;

namespace AplikacjaMobilna
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ThemeService.ApplyTheme();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}