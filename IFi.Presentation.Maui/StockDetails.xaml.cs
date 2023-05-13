using IFi.Presentation.VM.Maui.ViewModels;

namespace IFi.Presentation.Maui;

public partial class StockDetails : ContentPage
{
	public StockDetails(MainVM mainVm, StockPosition stockPosition)
	{
		BindingContext = new StockDetailsVM(mainVm, stockPosition, Navigation.PopAsync);
		InitializeComponent();
	}
}