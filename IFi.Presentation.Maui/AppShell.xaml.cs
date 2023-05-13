namespace IFi.Presentation.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
#if (ANDROID || IOS)
            content.ContentTemplate = new DataTemplate(typeof(MainPage_Android));
#else
            content.ContentTemplate = new DataTemplate(typeof(MainPage));
#endif
        }
    }
}