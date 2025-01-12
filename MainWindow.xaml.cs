using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
namespace ISDP2025_Parfonov_Zerrou
{
    
    //Main
    public partial class MainWindow : Window
    {
        BestContext context = new BestContext();
        string TheSalt = "TheSalt";
        Employee employee = new Employee();

        //List<Employee> employees = new List<Employee>();
        int passwordAttempts = 0;
        int maxPasswordAttempts = 1;
        
        public MainWindow()
        {
            InitializeComponent();
            BestContext context = new BestContext();
        }

        private void TogglePasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            //Switch Visibility for Password, Collapse Unnecessary info
            if (pwdPassword.Visibility == Visibility.Visible)
            {
                txtPassword.Text = pwdPassword.Password;
                pwdPassword.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
            }
            else
            {
                pwdPassword.Password = txtPassword.Text;
                pwdPassword.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowPasswordResetForm(object sender, MouseButtonEventArgs e)
        {
            LoginForm.Visibility = Visibility.Collapsed;
            PasswordResetForm.Visibility = Visibility.Visible;
            txtUserNameReset.IsEnabled = false;
            txtUserNameReset.Text = txtUserName.Text;
        }

        private void ShowLoginForm(object sender, MouseButtonEventArgs e)
        {
            PasswordResetForm.Visibility = Visibility.Collapsed;
            LoginForm.Visibility = Visibility.Visible;
            txtUserName.Clear();

            BlankResetForm();
        }
        
        //Resets Inputs
        private void BlankResetForm()
        {
            txtNewPassword.Clear();
            txtConfirmPassword.Clear();
            txtUserNameReset.Clear();
        }

        private void ToggleNewPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (pwdNewPassword.Visibility == Visibility.Visible)
            {
                txtNewPassword.Text = pwdNewPassword.Password;
                pwdNewPassword.Visibility = Visibility.Collapsed;
                txtNewPassword.Visibility = Visibility.Visible;
            }
            else
            {
                pwdNewPassword.Password = txtNewPassword.Text;
                pwdNewPassword.Visibility = Visibility.Visible;
                txtNewPassword.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleConfirmPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (pwdConfirmPassword.Visibility == Visibility.Visible)
            {
                txtConfirmPassword.Text = pwdConfirmPassword.Password;
                pwdConfirmPassword.Visibility = Visibility.Collapsed;
                txtConfirmPassword.Visibility = Visibility.Visible;
            }
            else
            {
                pwdConfirmPassword.Password = txtConfirmPassword.Text;
                pwdConfirmPassword.Visibility = Visibility.Visible;
                txtConfirmPassword.Visibility = Visibility.Collapsed;
            }
        }

        private void GeneratePassword(object sender, MouseButtonEventArgs e)
        {
            //Available Character Storage
            const string capitalLetters = "QWERTYUIOPASDFGHJKLZXCVBNM";
            const string smallLetters = "qwertyuiopasdfghjklzxcvbnm";
            const string digits = "0123456789";
            const string specialCharacters = "!@#^&_=+<,>.";
            const string allChars = capitalLetters + smallLetters + digits + specialCharacters;

            //Assign Lenght
            const int passwordLength = 15;

            //Initialize Random Object
            Random randNum = new Random();

            //Initialize the password with at least one character from each required category
            char[] password = new char[passwordLength];
            password[0] = capitalLetters[randNum.Next(capitalLetters.Length)];
            password[1] = digits[randNum.Next(digits.Length)];
            password[2] = specialCharacters[randNum.Next(specialCharacters.Length)];

            //Fill the rest of the password randomly
            for (int i = 3; i < passwordLength; i++)
            {
                password[i] = allChars[randNum.Next(allChars.Length)];
            }

            //Shuffle the password to randomize the order of characters
            for (int i = 0; i < password.Length; i++)
            {

                int randIndex = randNum.Next(i, password.Length);

                char temp = password[i];
                password[i] = password[randIndex];
                password[randIndex] = temp;
            }

            //Cast Char Array With Password as String and Display
            string finalPassword = new string(password);
            string hashedFinal = HashPasswordWithMD5(finalPassword, "Mohammed");

            txtNewPassword.Visibility = Visibility.Visible;
            txtConfirmPassword.Visibility = Visibility.Visible;
            txtNewPassword.Text = finalPassword;
            txtConfirmPassword.Text = finalPassword;

        }

        private void GetUsers()
        {
            employee = context.Employees.FirstOrDefault(employee => employee.Username==txtUserName.Text);
            if (employee == null) {
                MessageBox.Show("No employee found");
            try
            {
                // Retrieve UserName from the input
                string userName = txtUserName.Text;
                // Retrieve password from the textbox for comparison
                string hashedPassword = pwdPassword.Password;

                // Query to find the user by username
                var user = context.Employees.Where(e => e.Username == userName).FirstOrDefault();

                if (user != null)
                {
                    // Find the User By Password If User Exists
                    var userPassword = context.Employees.Where(e => e.Password == hashedPassword && e.Username == userName).FirstOrDefault();

                    if (userPassword != null)
                    {
                        // Login successful
                        MessageBox.Show($"Login Successful, Welcome {user.FirstName.ToUpper()}");
                        
                    }
                    else
                    {
                        MessageBox.Show("Your Credentials don't match our records!");
                        passwordAttempts++;

                        if (passwordAttempts >= maxPasswordAttempts)
                        {
                            this.IsEnabled = false;
                            MessageBox.Show("You have exceeded the maximum login attempts. Please reset your password.");
                            txtResetTitle.Text = "Password Reset Is Required";

                            //NEEDS REVIEW
                            LoginForm.Visibility = Visibility.Collapsed;
                            PasswordResetForm.Visibility = Visibility.Visible;
                            txtUserNameReset.IsEnabled = false;
                            txtUserNameReset.Text = txtUserName.Text;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Your Credentials don't match our records!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while retrieving users: {ex.Message}");
            }

        }


        //HashPasswordWithMD5 will hash and salt the password
        //It takes two parameters the password and the salt
        static string HashPasswordWithMD5(string password, string salt)
        {
            //Combine password and salt
            string saltedPassword = password + salt;

            //Create MD5 hash
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));

                //Convert hash to hexadecimal string
                StringBuilder hashBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    //Convert byte to hex
                    hashBuilder.Append(b.ToString("x2")); 
                }

                //This will be a 32-character string
                return hashBuilder.ToString(); 
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            context.Employees.Load();
            

            GetUsers();      
        }

        private void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            context = new BestContext();
            context.Employees.Load();
            GetUsers();
            MessageBox.Show(employee.Username + "  " + employee.Password);
            if (HashPasswordWithMD5(txtPassword.Text, TheSalt) == employee.Password)
            {
                MessageBox.Show("You can login");
            }
        }
    }
}