using AplikacjaMobilna.PageModels;

namespace AplikacjaMobilna.Pages;

public partial class CompassPage : ContentPage
{
    public CompassPage()
    {
        InitializeComponent();
        BindingContext = new CompassPageViewModel();
    }
}