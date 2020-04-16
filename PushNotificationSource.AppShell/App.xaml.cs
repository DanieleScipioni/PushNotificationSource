using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace PushNotificationSource.AppShell
{
    sealed partial class App
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var mainPage = (MainPage) Window.Current.Content;

            if (mainPage == null)
            {
                mainPage = new MainPage();

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                }

                Window.Current.Content = mainPage;
            }

            if (e.PrelaunchActivated) return;

            Window.Current.Activate();
        }
    }
}
