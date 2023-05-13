using IFi.Presentation.VM.Maui.ViewModels;

namespace IFi.Presentation.Maui
{
    public partial class MainPage_Android : ContentPage
    {
        private readonly MainVM _vm;
        public MainPage_Android()
        {
            _vm = new MainVM(Navigation.PushAsync);
            BindingContext = _vm;
            InitializeComponent();
            Task.Run(_vm.InitializeAsync);
        }

        private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new StockDetails_Android(_vm, (StockPosition)((BindableObject)sender).BindingContext));
        }
    }
}