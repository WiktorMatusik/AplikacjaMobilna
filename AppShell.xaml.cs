using AplikacjaMobilna.Pages;

namespace AplikacjaMobilna
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(CompassPage), typeof(CompassPage));
            Routing.RegisterRoute(nameof(WalkPage), typeof(WalkPage));

        }
    }
}
