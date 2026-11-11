using AplikacjaMobilna.PageModels;

namespace AplikacjaMobilna.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainPageViewModel();
        }
    }
}