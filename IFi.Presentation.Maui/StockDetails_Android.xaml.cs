using IFi.Presentation.VM.Maui.ViewModels;
using Microsoft.Maui.Platform;

namespace IFi.Presentation.Maui;

public partial class StockDetails_Android : ContentPage
{
	public StockDetails_Android(MainVM mainVm, StockPosition stockPosition)
	{
		BindingContext = new StockDetailsVM(mainVm, stockPosition, Navigation.PopAsync);
		InitializeComponent();
	}

    private void Entry_Completed(object sender, EventArgs e)
    {
#if ANDROID
      if(Platform.CurrentActivity.CurrentFocus != null)
            Platform.CurrentActivity.HideKeyboard(Platform.CurrentActivity.CurrentFocus);
#endif
    }
}