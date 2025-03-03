using System.Windows;
using ISDP2025_Parfonov_Zerrou.Functionality;

namespace ISDP2025_Parfonov_Zerrou
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize the Wednesday 4 PM order submitter
            WednesdayOrderSubmitter.Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop the submitter when exiting
            WednesdayOrderSubmitter.Shutdown();

            base.OnExit(e);
        }
    }

}
