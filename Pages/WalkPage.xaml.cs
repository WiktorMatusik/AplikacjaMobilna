using AplikacjaMobilna.PageModels;

namespace AplikacjaMobilna.Pages;

public partial class WalkPage : ContentPage
{
    public WalkPage()
    {
        InitializeComponent();
        BindingContext = new WalkPageViewModel();
    }
}