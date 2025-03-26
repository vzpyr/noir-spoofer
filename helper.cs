using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;

namespace noir {
    public class helper {
        public static readonly Random random = new Random();
        public static string[] GetWMIInfo(string className, string[] properties) {
            List<string> result = new List<string>();
            int maxLength = 0;
            try {
                foreach (string property in properties) {
                    string displayProperty = property == "IdentifyingNumber" ? "ID" : property;
                    if (displayProperty.Length > maxLength)
                        maxLength = displayProperty.Length;
                }
                foreach (ManagementObject queryObj in new ManagementObjectSearcher($"SELECT * FROM {className}").Get()) {
                    foreach (var property in properties) {
                        string displayProperty = property == "IdentifyingNumber" ? "ID" : property;
                        string value = queryObj[property]?.ToString() ?? "N/A";
                        result.Add($"{displayProperty.PadRight(maxLength)} | {value}");
                    }
                }
            } catch (Exception e) {
                result.Add($"Error | {e.Message}");
            }
            return result.ToArray();
        }

        public static string[] GetVolumeIDs() {
            List<string> result = new List<string>();
            int maxLength = "Drive".Length;
            try {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
                foreach (ManagementObject queryObj in searcher.Get()) {
                    string driveLetter = queryObj["DeviceID"]?.ToString() ?? "N/A";
                    string volumeSerialNumber = queryObj["VolumeSerialNumber"]?.ToString() ?? "N/A";
                    volumeSerialNumber = volumeSerialNumber.Insert(4, "-");
                    string entry = $"Drive {driveLetter}";
                    if (entry.Length > maxLength)
                        maxLength = entry.Length;
                    result.Add($"{entry.PadRight(maxLength)} | {volumeSerialNumber}");
                }
            } catch (Exception e) {
                result.Add($"Error | {e.Message}");
            }
            return result.ToArray();
        }

        public static string[] GetMACAddresses() {
            List<string> result = new List<string>();
            int maxNicNameLength = 0;
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()) {
                string nicName = nic.Name;
                if (nicName.Length > maxNicNameLength)
                    maxNicNameLength = nicName.Length;
            }
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()) {
                string macAddress = nic.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(macAddress)) {
                    string nicName = nic.Name;
                    for (int i = 2; i < macAddress.Length; i += 3)
                        macAddress = macAddress.Insert(i, ":");
                    int padding = maxNicNameLength - nicName.Length;
                    string formattedNicName = nicName + new string(' ', padding);
                    result.Add($"{formattedNicName} | {macAddress}");
                }
            }
            return result.ToArray();
        }

        public static string GetDefenderStatus() {
            string command = @"
            $status = Get-MpComputerStatus
            $preference = Get-MpPreference
            [PSCustomObject]@{
                RealTimeProtectionEnabled = $status.RealTimeProtectionEnabled
                TamperProtection = $status.IsTamperProtected
                CloudProtection = $preference.MAPSReporting
                SampleSubmission = $preference.SubmitSamplesConsent
            }";
            var result = "";
            using (var process = new Process()) {
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = command;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                result = output;
            }
            bool rtp = result.Contains("True");
            bool tmp = result.Contains("True");
            bool cloud = result.Contains("2");
            bool sample = result.Contains("1");
            if (rtp && tmp && cloud && sample) return "Enabled";
            else if (!rtp && !tmp && !cloud && !sample) return "Disabled";
            else return "Semi-Enabled";
        }

        public static string GetRandomString(int length, string charSet) {
            if (length < 1 || string.IsNullOrEmpty(charSet))
                return null;
            StringBuilder result = new StringBuilder(length);
            for (int i = 0; i < length; i++) {
                int index = random.Next(charSet.Length);
                result.Append(charSet[index]);
            }
            return result.ToString();
        }

        public static string GetRandomByte() {
            return random.Next(0, 256).ToString("X2");
        }

        public static string GetRandomMoboBrand() {
            string[] brands = { "Gigabyte", "MSI", "ASUS", "ASRock", "Lenovo", "Dell", "HP", "Acer", "Biostar" };
            return brands[random.Next(brands.Length)];
        }

        public static string GetRandomMoboModel(string brand) {
            Dictionary<string, string[]> brandModels = new Dictionary<string, string[]> {
                { "Gigabyte", new[] {
                    "Z790 AORUS Elite AX", "B550 AORUS Master", "X670 AORUS PRO", "H510M S2H",
                    "Z590 Gaming X", "Z690 AORUS Master", "B450M DS3H", "Z790 Gaming X AX",
                    "B550M S2H", "A320M-H", "Z490 UD", "Z390 AORUS Ultra" } },
                { "MSI", new[] {
                    "MAG B550 TOMAHAWK", "MEG X570 UNIFY", "PRO Z790-P WIFI", "MPG B650 EDGE WIFI",
                    "B450 TOMAHAWK MAX", "H510M PRO-E", "Z690 FORCE WIFI", "MAG Z790 TOMAHAWK WIFI",
                    "X470 GAMING PLUS MAX", "Z590 PRO WIFI", "B460M-A PRO", "X570 GAMING EDGE WIFI" } },
                { "ASUS", new[] {
                    "ROG STRIX Z790-E GAMING", "TUF GAMING B550-PLUS", "PRIME B760M-A D4", "ROG MAXIMUS Z690 HERO",
                    "ROG STRIX B650E-E GAMING WIFI", "TUF GAMING Z490-PLUS", "PRIME X570-P", "TUF GAMING B450-PLUS II",
                    "H310M-K", "ROG STRIX X670E-E GAMING", "ROG ZENITH EXTREME ALPHA", "PRIME B560-PLUS" } },
                { "ASRock", new[] {
                    "B550M Steel Legend", "X570 Phantom Gaming 4", "Z690 Taichi", "H570M-ITX",
                    "B450 Pro4", "Z790 PG Lightning", "X470 Gaming K4", "A520M/ac",
                    "Z590 Extreme", "B660M Pro RS", "X370 Taichi", "Z270 Killer SLI" } },
                { "Lenovo", new[] {
                    "ThinkCentre M90t", "ThinkStation P520", "IdeaCentre 5 14IOB6", "Legion T530 Tower",
                    "IdeaCentre AIO 3", "ThinkStation P340 Tiny", "ThinkCentre M80q", "IdeaPad Gaming Tower 5i",
                    "Legion C530 Cube", "ThinkStation P620", "ThinkCentre M720q", "IdeaCentre Gaming 5 17IAB7" } },
                { "Dell", new[] {
                    "OptiPlex 5090", "Inspiron 3881", "Precision T5820", "XPS 8940 SE",
                    "Vostro 3681", "Alienware Aurora R13", "G5 Gaming Desktop", "Inspiron 3020",
                    "OptiPlex 7080 Micro", "Precision 3560", "Inspiron 3910", "XPS 8930" } },
                { "HP", new[] {
                    "OMEN 30L GT13", "Pavilion TP01", "EliteDesk 800 G8", "ProDesk 400 G7",
                    "ZBook Fury 15 G8", "Envy Desktop TE02", "ProOne 600 G6", "Z1 Entry Tower G9",
                    "OMEN 25L GT12", "Pavilion Desktop 590", "Z2 Mini G5 Workstation", "EliteDesk 705 G5" } },
                { "Acer", new[] {
                    "Aspire TC-1660", "Nitro N50-620", "Veriton X2660G", "ConceptD 500",
                    "Aspire GX-281", "Predator Orion 3000", "Swift Edge 16", "Veriton M4660G",
                    "Aspire XC-895", "Nitro 50 N50-610", "Veriton Z4660G", "Predator Orion 9000" } },
                { "Biostar", new[] {
                    "B560MX-E PRO", "TB360-BTC PRO 2.0", "X570GT8", "A320MH",
                    "Z690 VALKYRIE", "B450GT3", "X670E VALKYRIE", "H410MH",
                    "B250BTC+", "H61MGV3", "X470GT8", "B760MX2 PRO" } }
            };
            if (brandModels.ContainsKey(brand)) {
                string[] models = brandModels[brand];
                return models[random.Next(models.Length)];
            }
            return "Unknown";
        }

        public static void RunHidden(string file, string arguments = "") {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = file,
                Arguments = arguments,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
    }
}