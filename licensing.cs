using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;

namespace noir {
    public class licensing {
        private string baseUrl, keyFile;

        private class LicenseResponse {
            public bool Valid { get; set; }
            public string Message { get; set; }
            public int UsesLeft { get; set; }
            public string Expiration { get; set; }
        }

        public licensing(string baseUrl, string keyFile) {
            this.baseUrl = baseUrl;
            this.keyFile = keyFile;
        }

        public async Task<(bool isValid, string message, string expiration, int usesLeft)> Verify(string licenseKey) {
            try {
                var payload = new { key = licenseKey };
                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");
                var response = await new HttpClient().PostAsync($"{baseUrl}/verify", content);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<LicenseResponse>(jsonResponse);
                return (
                    result.Valid,
                    result.Message ?? "Valid",
                    result.Expiration,
                    result.UsesLeft
                );
            } catch (Exception ex) {
                return (false, $"Error: {ex.Message}", null, 0);
            }
        }
        public void SetKey(string licenseKey) {
            try {
                if (!File.Exists(keyFile))
                    File.Create(keyFile).Close();
                using (Aes aes = Aes.Create()) {
                    byte[] key = GetEncryptionKey();
                    aes.Key = key;
                    aes.GenerateIV();
                    byte[] iv = aes.IV;
                    using (MemoryStream ms = new MemoryStream()) {
                        ms.Write(iv, 0, iv.Length);
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(licenseKey);
                        File.WriteAllBytes(keyFile, ms.ToArray());
                    }
                }
            } catch { }
        }

        public string GetKey() {
            try {
                if (File.Exists(keyFile)) {
                    byte[] encryptedData = File.ReadAllBytes(keyFile);
                    using (Aes aes = Aes.Create()) {
                        byte[] key = GetEncryptionKey();
                        byte[] iv = new byte[aes.BlockSize / 8];
                        Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                        aes.Key = key;
                        aes.IV = iv;
                        using (MemoryStream ms = new MemoryStream(encryptedData, iv.Length, encryptedData.Length - iv.Length))
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        using (StreamReader sr = new StreamReader(cs))
                            return sr.ReadToEnd();
                    }
                }
            } catch { }
            return "";
        }

        private byte[] GetEncryptionKey() {
            string userSid = WindowsIdentity.GetCurrent().User.Value;
            using (SHA256 sha256 = SHA256.Create())
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(userSid));
        }

        public void DeleteKey() {
            try {
                if (File.Exists(keyFile))
                    File.Delete(keyFile);
            } catch { }
        }
    }
}