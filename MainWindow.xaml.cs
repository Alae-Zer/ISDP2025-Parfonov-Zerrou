using System.Diagnostics;
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
            try
            {
                // Retrieve UserName from the input
                string userName = txtUserName.Text;
                // Retrieve password from the textbox or tha password box based on which one is visible (if it is visible == the password in the element is the latest)
                string inputPassword = pwdPassword.Visibility == 0 ? pwdPassword.Password : txtPassword.Text;
                //hashing the password
                string hashedPassword = HashPasswordWithMD5(inputPassword, TheSalt);

                //getting the employee from the DB
                employee = context.Employees.FirstOrDefault(employee => employee.Username == txtUserName.Text);

                // checking if there is an employee with the given name
                if (employee != null)
                {
                    //checking if the employee is already locked
                    if (employee.Locked != 1)
                    {
                        //checking if the hashed password is the password in the DB
                        if (hashedPassword == employee.Password)
                        {
                            // if it is the Login is successful
                            MessageBox.Show($"Login Successful, Welcome {employee.FirstName.ToUpper()}");
                        }
                        else
                        {
                            // checking if the employee still has the default password
                            if (employee.Password == "P@ssw0rd-")
                            {
                                // if the employee has the default password it asks the employee to change the password
                                MessageBox.Show("You need to change your default password");
                                txtResetTitle.Text = "Password Reset Is Required";

                                LoginForm.Visibility = Visibility.Collapsed;
                                PasswordResetForm.Visibility = Visibility.Visible;
                                txtUserNameReset.IsEnabled = false;
                                txtUserNameReset.Text = txtUserName.Text;
                            }
                            else
                            {
                                // if the code reach here that means this isnt the first login and password is wrong
                                MessageBox.Show("Your Credentials don't match our records!");
                                passwordAttempts++;

                                if (passwordAttempts >= maxPasswordAttempts)
                                {
                                    MessageBox.Show("You have exceeded the maximum login attempts.");
                                    MessageBox.Show("You account has been locked because of too many incorrect login attempts.Please contact your Administrator at admin@bullseye.ca for assistance");
                                    lockOutUser();
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("You account has been locked because of too many incorrect login attempts.Please contact your Administrator at admin@bullseye.ca for assistance");
                    }

                }
                //this else means there is no employee with the correct name
                else
                {
                    MessageBox.Show("Your Credentials don't match our records!");
                    passwordAttempts++;

                    if (passwordAttempts >= maxPasswordAttempts)
                    {
                        MessageBox.Show("You have exceeded the maximum login attempts.");
                        MessageBox.Show("You account has been locked because of too many incorrect login attempts.Please contact your Administrator at admin@bullseye.ca for assistance");
                        lockOutUser();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while retrieving users: {ex.Message}");
            }
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

        private void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            GetUsers();
            MessageBox.Show(employee.Username + "  " + employee.Password);
        }
        private void lockOutUser()
        {
            // this function will lock the user
        }
        private void updatePassword(string password)
        {
            // this function will hash the password and update it
        }

        private void btnResetPasswrd_Click(object sender, RoutedEventArgs e)
        {
            string inputPassword = pwdNewPassword.Visibility == 0 ? pwdNewPassword.Password : txtNewPassword.Text;
            updatePassword(inputPassword);
        }

        
        private void matchPassword()
        {
            string newPassword = pwdNewPassword.Visibility == 0 ? pwdNewPassword.Password : txtNewPassword.Text;
            string confirmPassword = pwdConfirmPassword.Visibility == 0 ? pwdConfirmPassword.Password : txtConfirmPassword.Text;
            if (newPassword != confirmPassword)
            {
                txtMatchPassword.Text = "the passwords doesnt match";
                txtMatchPassword.Foreground = new SolidColorBrush(Colors.Red);
            }
            else {
                txtMatchPassword.Text = "";
            }

        }

        private void pwdConfirmPassword_KeyUp(object sender, KeyEventArgs e)
        {
            matchPassword();
        }

        private void txtConfirmPassword_KeyUp(object sender, KeyEventArgs e)
        {
            matchPassword();

        }
    } 
}