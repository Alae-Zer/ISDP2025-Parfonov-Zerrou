using ISDP2025_Parfonov_Zerrou.Models;
using System.Windows;
using System.Windows.Threading;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Mohammed with some changes from serhii
//Last Modified by Mohammed on Feb 1,2025
namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public class LogoutManager
    {
        //Set of variables
        Window currentWindow;
        BestContext context;
        DispatcherTimer logoutTimer;
        int logoutTimeInMinutes;

        //Constructor initializes logout management
        //Requires active window and database context
        //Sets up timer and event handlers for user activity tracking
        public LogoutManager(Window window, BestContext context)
        {
            this.currentWindow = window;
            this.context = context;

            try
            {
                //Get logout time from settings
                var settings = context.Settings.FirstOrDefault();
                if (settings != null)
                {
                    logoutTimeInMinutes = settings.LogoutTimeMinutes;

                    // Create and configure the timer
                    logoutTimer = new DispatcherTimer();
                    logoutTimer.Interval = TimeSpan.FromMinutes(logoutTimeInMinutes);
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

        //Start Timer
        public void StartTimer()
        {
            logoutTimer.Start();
        }

        //Stops Timer
        public void StopTimer()
        {
            logoutTimer.Stop();
        }

        //Resets Logout Timer
        private void ResetLogoutTimer()
        {
            if (logoutTimer != null)
            {
                logoutTimer.Stop();
                logoutTimer.Start();
            }
        }

        //Handles timer completion by triggering logout
        private void LogoutTimer_Tick(object sender, EventArgs e)
        {
            SafeLogout();
        }

        //Performs secure logout: stops timer, notifies user, closes window
        private void SafeLogout()
        {
            try
            {
                //End timer
                StopTimer();

                //Show message to user
                MessageBox.Show("Your session has expired due to inactivity.", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Information);

                //Close current form and open MainWindow
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

        //Reset timer on mouse movement
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ResetLogoutTimer();
        }

        //Reset timer on keyboard activity
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ResetLogoutTimer();
        }

        //Remove event handlers and stop timer when closing
        public void Cleanup()
        {
            StopTimer();
            currentWindow.MouseMove -= Window_MouseMove;
            currentWindow.KeyDown -= Window_KeyDown;
        }
    }
}
