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
    class Login
    {
        Database1Entities db = new Database1Entities();
        private byte[] salt = new byte[] { 0x0A, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0xF1 };
        RecognizeForm form;
        public Login(RecognizeForm form)
        {
            this.form = form;
        }

        public bool CreateUser(string account, string pwd, string name, string email, string title)
        {
            form.UpdateText("Check if user already exist");
            User user = (from u in db.User where u.UID.Equals(account) select u).FirstOrDefault();
            if (user != null) return false;
            form.UpdateLog("Check if user already exist");

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
            
            return true;
        }

        public User GetUser(string account, string pwd)
        {
            User user = (from u in db.User where u.UID.Equals(account) select u).FirstOrDefault();
            if (user == null) return null;

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

            return (isPasswordCorrect) ? user : null;
        }
    }
}
