using System.Windows;
using System.Windows.Threading;

namespace ISDP2025_Parfonov_Zerrou.Forms
{
    public partial class ItemToCartMovement : Window
    {
        private readonly DispatcherTimer timer;

        public ItemToCartMovement(string message)
        {
            InitializeComponent();
            txtMessage.Text = message;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();

            // Center on screen
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            Close();
        }
    }
}