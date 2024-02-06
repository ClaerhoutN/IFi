using IFi.Domain.ApiResponse;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace IFi.Presentation.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            LiveCharts.Configure(config =>
            config
                .AddSkiaSharp()
                .AddDefaultMappers()
                .AddLightTheme()
                .HasMap<Stock>((stock, index) =>
                {
                    return new LiveChartsCore.Kernel.Coordinate(
                        stock.Date.Ticks,
                        (double)stock.High, 
                        (double)stock.Open, 
                        (double)stock.Close, 
                        (double)stock.Low);
                })
            );
        }
    }
}