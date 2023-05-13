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
                .HasMap<Stock>((stock, point) =>
                {
                    point.PrimaryValue = (double)stock.High;
                    //point.SecondaryValue = point.Context.Entity.EntityIndex;
                    point.SecondaryValue = stock.Date.Ticks;

                    point.TertiaryValue = (double)stock.Open;
                    point.QuaternaryValue = (double)stock.Close;
                    point.QuinaryValue = (double)stock.Low;
                })
            );
        }
    }
}