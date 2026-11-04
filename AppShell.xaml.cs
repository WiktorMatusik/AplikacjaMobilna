using AplikacjaMobilna.Pages;

namespace AplikacjaMobilna
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(CompassPage), typeof(CompassPage));
            Routing.RegisterRoute(nameof(WalkPage), typeof(WalkPage));
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        }
    }
}
