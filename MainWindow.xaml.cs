using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Security.Principal;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        string defaultPassword = "P@ssw0rd-";

        public MainWindow()
        {
            InitializeComponent();
            BestContext context = new BestContext();
            ResetInputs();
        }

        private void TogglePasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            PasswordVisibility();
        }

        private void PasswordVisibility()
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
            ShowPasswordResetForm();
            txtUserNameReset.IsEnabled = true;
        }

        private void ShowLoginForm(object sender, MouseButtonEventArgs e)
        {
            PasswordResetForm.Visibility = Visibility.Collapsed;
            LoginForm.Visibility = Visibility.Visible;
            ResetInputs();
            BlankResetForm();
        }

        private void BlankResetForm()
        {
            txtNewPassword.Clear();
            txtConfirmPassword.Clear();
            txtUserNameReset.Clear();
        }

        private bool IsEmptyInput(TextBox textInput, string name)
        {
            if (textInput.Text == "")
            {
                MessageBox.Show($"{name} Can't Be Empty");
                return false;
            }
            return true;
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

        private string GetUser()
        {
            string output = "error";
            try
            {
                string userName = txtUserName.Text;
                var user = context.Employees.Where(e => e.Username == userName).FirstOrDefault();
                
                if (user != null && user.Active == 1)
                {
                    output = user.Username;
                }
                return output;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while retrieving users: {ex.Message}");
                return "DBerror";
            }  
        }

        private void ValidateLoginAndHandleAccess()
        {

            try
            {
                string inputPassword;
                string userName = GetUser();

                if (pwdPassword.Visibility == Visibility.Visible)
                {
                    inputPassword = pwdPassword.Password;
                }
                else
                {
                    inputPassword = txtNewPassword.Text;
                }

                var employee = context.Employees.FirstOrDefault(e => e.Username == userName);
                var password = context.Employees.FirstOrDefault(e => e.Username == userName && e.Password == inputPassword);

                if (employee != null)
                {
                    if (password != null && password.Password == defaultPassword)
                    {
                        MessageBox.Show("You need to reset your password.");
                        ShowPasswordResetForm();
                        ResetInputs();
                    }
                    else if (employee.Password == inputPassword)
                    {
                        MessageBox.Show("Login Successful!");
                        ResetInputs();
                        // Navigate to the next page or main dashboard
                    }
                    else
                    {
                        MessageBox.Show("Your login credentials are incorrect.");
                        passwordAttempts++;
                        if (passwordAttempts > maxPasswordAttempts)
                        {
                            //LOCK USER HERE
                            LockUser(userName);
                            passwordAttempts = 0;
                        }
                        ResetInputs();
                    }
                }
                else
                {
                    MessageBox.Show("Your login credentials are incorrect.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while processing your login: {ex.Message}");
                // Optionally log the error or handle it further
            }
        }

        private void LockUser(string userName)
        {
            try
            {
                var employee = context.Employees.FirstOrDefault(e => e.Username == userName);

                if (employee != null)
                {
                    employee.Active = 0;
                    context.SaveChanges();

                    MessageBox.Show($"User '{userName}' has been locked due to too many failed login attempts.");
                }
                //this else means there is no employee with the correct name
                else
                {
                    MessageBox.Show("User not found. Unable to lock the account.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while locking the user: {ex.Message}");
            }
        }

        private void ResetInputs()
        {
            txtUserName.Clear();
            txtPassword.Clear();
            pwdPassword.Clear();
        }

        private void ShowPasswordResetForm()
        {
            LoginForm.Visibility = Visibility.Collapsed;
            PasswordResetForm.Visibility = Visibility.Visible;
            txtUserNameReset.IsEnabled = false;
            txtUserNameReset.Text = txtUserName.Text;
        }


        //HashPasswordWithMD5 will hash and salt the password
        //It takes two parameters the password and the salt
        static string HashPasswordWithMD5(string password, string salt){
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
        }
        
        private void TogglePassword(PasswordBox pwbInput, TextBox txtInput)
        {
            if (pwbInput.Visibility == Visibility.Visible)
            {
                txtInput.Text = pwbInput.Password;
            }
            else
            {
                pwbInput.Password = txtInput.Text;
            }
        }

        private void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            TogglePassword(pwdPassword,txtPassword);

            if (IsEmptyInput(txtUserName, "User Name") && IsEmptyInput(txtPassword, "Password"))
            {
                ValidateLoginAndHandleAccess();
            }
            
        }

        private void btnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            string inputPassword = pwdNewPassword.Visibility == 0 ? pwdNewPassword.Password : txtNewPassword.Text;
            
            if (IsEmptyInput(txtUserNameReset, "User Name")&& IsEmptyInput(txtNewPassword, "New Password") && IsEmptyInput(txtConfirmPassword, "Confirm Password")){
                var user = context.Employees.FirstOrDefault(u => u.Username == txtUserNameReset.Text);
                if (user != null) 
                {
                    user.Password = inputPassword;
                    context.SaveChanges();
                    MessageBox.Show("Password updated successfully!", "Success");
                }
            }
            
        }

        private void pwdConfirmPassword_KeyUp(object sender, KeyEventArgs e)
        {
            MatchPassword();
        }

        private void txtConfirmPassword_KeyUp(object sender, KeyEventArgs e)
        {
            MatchPassword();

        }

        private void MatchPassword()
        {
            string newPassword = pwdNewPassword.Visibility == 0 ? pwdNewPassword.Password : txtNewPassword.Text;
            string confirmPassword = pwdConfirmPassword.Visibility == 0 ? pwdConfirmPassword.Password : txtConfirmPassword.Text;
            if (newPassword != confirmPassword)
            {
                txtMatchPassword.Text = "the passwords doesnt match";
                txtMatchPassword.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
               txtMatchPassword.Text = "";
            }

        }
    }
}