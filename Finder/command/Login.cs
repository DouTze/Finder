using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Data.SqlClient;

namespace Finder
{
    public class Login
    {
        private static Database1Entities db = new Database1Entities();
        private static byte[] salt = new byte[] { 0x0A, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0xF1 };

        public Login()
        {
        }

        public static void CreateUser(object obj)
        {
            string account, pwd, name, email, title;
            object[] args = (object[])obj;
            account = (string)args[0];
            pwd = (string)args[1];
            name = (string)args[2];
            email = (string)args[3];
            title = (string)args[4];
            RecognizeForm form = (RecognizeForm)args[5];

            form.UpdateText("Check if user already exist");
            User user = (from u in db.User where u.UID.Equals(account) select u).FirstOrDefault();
            form.UpdateLog("Check if user already exist");
            if (user != null)
            {
                form.UpdateLog("User already exist");
            }
            else
            {
                form.UpdateText("Create new account");
                AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
                form.UpdateLog("Create new account");

                form.UpdateText("Encrypt password");
                byte[] key = Encoding.UTF8.GetBytes(pwd);
                Rfc2898DeriveBytes rfcKey = new Rfc2898DeriveBytes(key, salt, 16);
                byte[] keyData = rfcKey.GetBytes(16);
                aes.Key = keyData;

                string iv = Convert.ToBase64String(aes.IV);
                byte[] dataByteArray = Encoding.UTF8.GetBytes(account);

                string encrypt = "";
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(dataByteArray, 0, dataByteArray.Length);
                    cs.FlushFinalBlock();
                    encrypt = Convert.ToBase64String(ms.ToArray());
                }
                form.UpdateLog("Encrypt password");
                user = new User();
                user.UID = account;
                user.Password = encrypt;
                user.IV = iv;
                user.Name = name;
                user.Email = email;
                user.Title = title;
                user.Authority = 0;

                form.UpdateText("Update database");
                db.User.Add(user);
                db.SaveChanges();
                form.UpdateLog("Update database");

                form.UpdateText("Create user [ " + account + " ] successful");
                form.UpdateLog("Create user [ " + account + " ] successful");
            }
        }

        public static void GetUser(object obj)
        {
            object[] args = (object[])obj;
            string account = (string)args[0];
            string pwd = (string)args[1];
            RecognizeForm form = (RecognizeForm)args[2];

            form.UpdateText("Now loading...");
            User user = (from u in db.User where u.UID.Equals(account) select u).FirstOrDefault();
            if (user == null)
            {
                form.UpdateLog("Login failed, please check your account/password");
                form.UpdateText("");
            }
            else
            {
                AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
                byte[] key = Encoding.UTF8.GetBytes(pwd);
                Rfc2898DeriveBytes rfcKey = new Rfc2898DeriveBytes(key, salt, 16);
                byte[] keyData = rfcKey.GetBytes(16);
                aes.Key = keyData;

                byte[] iv = Convert.FromBase64String(user.IV);
                aes.IV = iv;

                bool isPasswordCorrect = false;
                byte[] dataByteArray = Convert.FromBase64String(user.Password);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(dataByteArray, 0, dataByteArray.Length);
                        cs.FlushFinalBlock();
                        isPasswordCorrect = Encoding.UTF8.GetString(ms.ToArray()).Equals(user.UID);
                    }
                }

                form.UpdateText((isPasswordCorrect) ? "Login successful" : "Login failed");
                form.UpdateLog((isPasswordCorrect) ? "Login successful" : "Login failed");
                form.UpdateLog((isPasswordCorrect) ? "Welcome back " + user.Name + "!" : "Wrong account/password, please try again or type \"login -r\" to regist new account");
                form.UpdateText("");
                form.user = user;
            }
        }
    }
}
