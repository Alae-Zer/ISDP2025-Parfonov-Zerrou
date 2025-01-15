using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq.Expressions;
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
using System.Xml.Linq;
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

        //Global VAriables
        string TheSalt = "TheSalt";
        int maxPasswordAttempts = 1;
        string defaultPassword = "P@ssw0rd-";
        List<string> usersAttempts = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            BestContext context = new BestContext();
            ResetInputs();
            ShowLoginForm();
        }

        private void TogglePasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            TogglePasswords(pwdPassword, txtPassword);
        }

        private void ToggleNewPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            TogglePasswords(pwdNewPassword, txtNewPassword);
        }

        private void ToggleConfirmPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            TogglePasswords(pwdConfirmPassword, txtConfirmPassword);
        }

        //Switch Visibility for Password, Collapse Unnecessary info
        //Sends Two Controls (PasswordBox and textBox)
        //Returns Nothing
        private void TogglePasswords (PasswordBox pwdPassword, TextBox txtPassword)
        {
            //Swap Visibility
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
            Employee user = new Employee();
            user = GetUser();
            
            if (user.Username == txtUserName.Text && user.Active == 1)
            {
                ShowPasswordResetForm("Forgot Your Password? No Problem!");
            }
            else
            {
                MessageBox.Show("Something is wrong", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void ShowLoginForm(object sender, MouseButtonEventArgs e)
        {
            ShowLoginForm();
        }

        //Changes Style
        //Sends Nothing
        //Returns Nothing
        private void ShowLoginForm()
        {
            PasswordResetForm.Visibility = Visibility.Collapsed;
            LoginForm.Visibility = Visibility.Visible;
            ResetInputs();
            BlankResetForm();
            txtUserName.Text = "User Name";
            txtUserName.Focus();
            txtUserName.Select(0, txtUserName.Text.Length);
        }

        //Blank Inputs
        //Sends Nothing
        //Returns Nothing
        private void BlankResetForm()
        {
            //Blank Inputs
            txtNewPassword.Clear();
            txtConfirmPassword.Clear();
            txtUserNameReset.Clear();
        }
        
        //Verifies that input is not empty
        //Sends TextBox with Displayed Name
        //Return Boolean
        private bool IsEmptyInput(string textInput, string name)
        {
            //If String is empty - return false
            if (textInput == "")
            {
                MessageBox.Show($"{name} Can't Be Empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        //Generates Strong Password
        //Sends Nothing
        //Returns Nothing
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
            string hashedFinal = HashPasswordWithMD5(finalPassword, TheSalt);

            txtNewPassword.Visibility = Visibility.Visible;
            txtConfirmPassword.Visibility = Visibility.Visible;
            txtNewPassword.Text = finalPassword;
            txtConfirmPassword.Text = finalPassword;

        }

        //Finds Employee In the database
        //Sends Nothing
        //Returns Employee
        private Employee GetUser()
        {
            //Initialize and try to retrieve
            Employee userOutput = new Employee();
            if(IsEmptyInput(txtUserName.Text, "User name"))
            {
                string userName = txtUserName.Text;

                try
                {
                    var user = context.Employees.Where(e => e.Username == userName).FirstOrDefault();

                    //Assign Value if User Exists and Status Is Active
                    if (user != null && user.Active == 1)
                    {
                        userOutput = user;
                    }
                    return userOutput;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while retrieving users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return userOutput;
                }
            }
            return userOutput;
        }

        //Check If Access Is Granted or Refused
        //Sends Nothing
        //Returns Nothing
        private void ValidateLoginAndHandleAccess()
        {
            //Initialize and Assign
            string inputPassword;
            Employee employee = GetUser();

            //Assign Password based On Active State
            if (pwdPassword.Visibility == Visibility.Visible)
            {
                inputPassword = pwdPassword.Password;
            }
            else
            {
                inputPassword = txtNewPassword.Text;
            }

            //Verify that Employee Exists
            if (employee != null && employee.Active == 1)
            {
                //Check That Employee Has A Default Password And Prompt to Change
                if (inputPassword == employee.Password && employee.Password == defaultPassword)
                {
                    MessageBox.Show("You need to reset your password.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowPasswordResetForm("Reset Your Password");
                    ResetInputs();
                }
                //Verify If Password Is The Same As Input and Redirect
                else if (employee.Password == inputPassword)
                {
                    ResetInputs();
                }
                //Employee Exists But Password Doesn't Match Any Patterns
                else
                {
                    //Display Message And Sends Employee For Verification of Number Of Attempts
                    MessageBox.Show("Your login credentials are incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (FindUserInTheList(employee.Username))
                    {
                        //If Number Is Exceeded And It's Verified, Employee Is Locked
                        LockUser(employee.Username);
                    }
                    ResetInputs();
                }
            }
            else
            {
                //If User Doesn't Exist
                MessageBox.Show("Your login credentials are incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetInputs();
            }
        }

        //Browse the User in The list and Checks Number of session Attempts
        //Accepts Username String
        //Return Boolean Value (Exceeded limit or Not)
        private bool FindUserInTheList(string userName)
        {
            //Initialize Loop
            for (int i = 0; i < usersAttempts.Count; i++)
            {
                //Split Row
                string[] userParts = usersAttempts[i].Split(',');

                //Check the Number Of Arguments and Name Is the Same as In the Records
                if (userParts.Length == 2 && userName == userParts[0])
                {
                    //Parse String To Integer
                    if (int.TryParse(userParts[1], out int attempt))
                    {
                        //Increment and Record
                        attempt++;
                        usersAttempts[i] = $"{userName},{attempt}";

                        //Check The Limit
                        if (attempt >= maxPasswordAttempts)
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }

            //Add New Record If Not Found
            usersAttempts.Add($"{userName},1");

            return false;
        }

        //Checks The List Of Users Who Attempted to Login, If Attempts Exceed limits - LOCK
        private void LockUser(string userName)
        {
            try
            {
                var employee = context.Employees.FirstOrDefault(e => e.Username == userName);

                //DOUBLE CHECK IF EMPLOYEE EXISTS
                if (employee != null)
                {
                    //Remove Permissions
                    employee.Active = 0;
                    context.SaveChanges();
                    MessageBox.Show("Your account has been locked because of too many incorrect login attempts. " +
                        "Please contact your Administrator at admin@bullseye.ca for assistance", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
              
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while locking the user: {ex.Message}");
            }
        }

        //Resets Some Inputs
        //Sends Nothing
        //Returns Nothing
        private void ResetInputs()
        {
            txtPassword.Clear();
            pwdPassword.Clear();
        }
        

        //Display Password Rewset Form
        //Sends Nothing
        //Returns Nothing
        private void ShowPasswordResetForm(string message)
        {
            LoginForm.Visibility = Visibility.Collapsed;
            PasswordResetForm.Visibility = Visibility.Visible;
            txtUserNameReset.IsEnabled = false;
            txtUserNameReset.Text = txtUserName.Text;
            txtResetTitle.Text = message;
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
        
        //Changes Input State Based On Visibility
        //Sends Two Inputs
        //Returns Nothing
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

            string userName = txtUserName.Text;
            string password = txtPassword.Text;

            //Verify That Input is Not Empty
            if (IsEmptyInput(userName, "User Name") && IsEmptyInput(password, "Password"))
            {
                ValidateLoginAndHandleAccess();
            }
            
        }

        private void btnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string inputPassword = pwdNewPassword.Visibility == 0 ? pwdNewPassword.Password : txtNewPassword.Text;
                string confirmPassword = pwdConfirmPassword.Visibility == 0 ? pwdConfirmPassword.Password : txtConfirmPassword.Text;

                // Verify That Inputs Are Not Empty
                if (IsEmptyInput(inputPassword, "New Password") && IsEmptyInput(confirmPassword, "Confirm Password"))
                {
                    // DOUBLE VALIDATION
                    var user = context.Employees.FirstOrDefault(u => u.Username == txtUserNameReset.Text);
                    if (user != null)
                    {
                        if (IsValidPassword(txtNewPassword.Text) && IsValidPassword(txtConfirmPassword.Text))
                        {
                            user.Password = inputPassword;
                            context.SaveChanges();
                            MessageBox.Show("Password updated Successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show($"An error occurred while resetting the password: {ex.Message}", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
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

        //Compares two Password Inputs For Matching Passwords
        //Sends Nothing
        //Returns Nothing
        private void MatchPassword()
        {
            string newPassword = pwdNewPassword.Visibility == 0 ? pwdNewPassword.Password : txtNewPassword.Text;
            string confirmPassword = pwdConfirmPassword.Visibility == 0 ? pwdConfirmPassword.Password : txtConfirmPassword.Text;
            
            //Display Message If Don't Match
            if (newPassword != confirmPassword)
            {
                txtMatchPassword.Text = "Passwords Don't Match";
                txtMatchPassword.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
               txtMatchPassword.Text = "";
            }
        }

        //Checks that input meets requirements for password
        //sends String
        //returns Bool
        public bool IsValidPassword(string inputString)
        {
            //Flags and Specials
            bool hasUpper = false;
            bool hasDigit = false;
            bool hasSpecialChar = false;
            string specialCharacters = "!@#^&_=+<,>.";

            //Assembly Message
            string message = "Your Password must contain:\n";

            //LOOP Through Input, if single condition is met - Flag Changes 
            foreach (char character in inputString)
            {
                if (char.IsUpper(character))
                {
                    hasUpper = true;
                }
             
                else if (char.IsDigit(character))
                {
                    hasDigit = true;
                }
                
                else if (specialCharacters.Contains(character))
                {
                    hasSpecialChar = true;
                }
                
                //Brake The loop if Conditions are Met
                if (hasDigit && hasUpper && hasSpecialChar)
                {
                    break;
                }

            }

            //Assembly message based on What neeeded to Add
            if (!hasUpper)
            {
                message += "- At least one uppercase letter\n";
            }
            if (!hasDigit)
            {
                message += "- At least one number\n";
            }
            if (!hasSpecialChar)
            {
                message += "- At least one special character\n";
            }

            if (!hasUpper || !hasDigit || !hasSpecialChar)
            {
                MessageBox.Show(message, "Password Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            //LOL, found if one of them is false - it will return false
            return hasUpper && hasDigit && hasSpecialChar;
        }
    }
}