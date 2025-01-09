using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ISDP2025_Parfonov_Zerrou
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("skdjfbjakjkadghjFJKFjjbdfJHIfdhifdshfasajhidsfhjdsfjaklvknoaetboerg6519816");
        }

        private void TogglePasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowPasswordResetForm(object sender, MouseButtonEventArgs e)
        {
            LoginForm.Visibility = Visibility.Collapsed;
            PasswordResetForm.Visibility = Visibility.Visible;
        }

        private void ShowLoginForm(object sender, MouseButtonEventArgs e)
        {
            PasswordResetForm.Visibility = Visibility.Collapsed;
            LoginForm.Visibility = Visibility.Visible;
        }

        private void ToggleNewPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (NewPasswordBox.Visibility == Visibility.Visible)
            {
                NewPasswordTextBox.Text = NewPasswordBox.Password;
                NewPasswordBox.Visibility = Visibility.Collapsed;
                NewPasswordTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                NewPasswordBox.Password = NewPasswordTextBox.Text;
                NewPasswordBox.Visibility = Visibility.Visible;
                NewPasswordTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleConfirmPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (ConfirmPasswordBox.Visibility == Visibility.Visible)
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void GeneratePassword(object sender, MouseButtonEventArgs e)
        {
            const string CapitalLetters = "QWERTYUIOPASDFGHJKLZXCVBNM";
            const string SmallLetters = "qwertyuiopasdfghjklzxcvbnm";
            const string Digits = "0123456789";
            const string SpecialCharacters = "!@#^&_=+<,>.";
            const string AllChars = CapitalLetters + SmallLetters + Digits + SpecialCharacters;

            int PasswordLength = 8;

            Random rnd = new Random();

            // Initialize the password with at least one character from each required category
            char[] password = new char[PasswordLength];
            password[0] = CapitalLetters[rnd.Next(CapitalLetters.Length)]; // Ensure at least one uppercase letter
            password[1] = Digits[rnd.Next(Digits.Length)];                 // Ensure at least one digit
            password[2] = SpecialCharacters[rnd.Next(SpecialCharacters.Length)]; // Ensure at least one special character

            // Fill the rest of the password randomly
            for (int i = 3; i < PasswordLength; i++)
            {
                password[i] = AllChars[rnd.Next(AllChars.Length)];
            }

            // Shuffle the password to randomize the order of characters
            password = password.OrderBy(x => rnd.Next()).ToArray();

            // Convert the password array to a string and display it
            string finalPassword = new string(password);
            MessageBox.Show(new string(finalPassword)); 
        }

        //HashPasswordWithMD5 will hash and salt the password
        //it takes two parameters the password and salt
        static string HashPasswordWithMD5(string password, string salt)
        {
            // Combine password and salt
            string saltedPassword = password + salt;

            // Create MD5 hash
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));

                // Convert hash to hexadecimal string
                StringBuilder hashBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hashBuilder.Append(b.ToString("x2")); // Convert byte to hex
                }
                return hashBuilder.ToString(); // This will be a 32-character string
            }
        }
    }
}