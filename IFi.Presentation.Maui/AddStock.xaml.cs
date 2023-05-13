using IFi.Presentation.VM.Maui.ViewModels;
using Microsoft.Maui.Platform;

namespace IFi.Presentation.Maui;

public partial class AddStock : ContentPage
{
	public AddStock(MainVM mainVm)
	{
		BindingContext = new AddStockVM(mainVm, Navigation.PopAsync);
		InitializeComponent();
	}

    private void SearchBar_SearchButtonPressed(object sender, EventArgs e)
    {
        ((SearchBar)sender).Unfocus();
#if ANDROID
      if(Platform.CurrentActivity.CurrentFocus != null)
            Platform.CurrentActivity.HideKeyboard(Platform.CurrentActivity.CurrentFocus);
#endif
    }
}