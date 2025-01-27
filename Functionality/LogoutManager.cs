using System.Windows;
using System.Windows.Threading;
using ISDP2025_Parfonov_Zerrou.Models;

namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public class LogoutManager
    {
        private readonly Window currentWindow;
        private readonly BestContext context;
        private readonly DispatcherTimer logoutTimer;
        private readonly int logoutTimeInMinutes;

        public LogoutManager(Window window, BestContext context)
        {
            this.currentWindow = window;
            this.context = context;

            try
            {
                // Get logout time from settings
                var settings = context.Settings.FirstOrDefault();
                if (settings != null)
                {
                    logoutTimeInMinutes = settings.LogoutTimeMinutes;

                    // Create and configure the timer
                    logoutTimer = new DispatcherTimer();
                    logoutTimer.Interval = TimeSpan.FromMinutes(0.1);
                    logoutTimer.Tick += LogoutTimer_Tick;

                    // Add event handlers for user activity
                    currentWindow.MouseMove += Window_MouseMove;
                    currentWindow.KeyDown += Window_KeyDown;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing logout timer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StartTimer()
        {
            logoutTimer?.Start();
        }

        public void StopTimer()
        {
            logoutTimer?.Stop();
        }

        private void ResetLogoutTimer()
        {
            if (logoutTimer != null)
            {
                logoutTimer.Stop();
                logoutTimer.Start();
            }
        }

        private void LogoutTimer_Tick(object sender, EventArgs e)
        {
            SafeLogout();
        }

        private void SafeLogout()
        {
            try
            {
                // Stop the timer
                StopTimer();

                // Show message to user
                MessageBox.Show("Your session has expired due to inactivity.", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Information);

                // Close current form and open MainWindow
                Application.Current.Dispatcher.Invoke(() =>
                {
                    context.Dispose();
                    currentWindow.Close();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during logout: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ResetLogoutTimer();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ResetLogoutTimer();
        }

        public void Cleanup()
        {
            StopTimer();
            currentWindow.MouseMove -= Window_MouseMove;
            currentWindow.KeyDown -= Window_KeyDown;
        }
    }
}
