using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace noir {
    public class tool {
        public static void smbios() {
            string amiexe = hex.Convert(hex.AMIEXE(), Path.GetTempPath() + $"\\{helper.GetRandomString(8, "0123456789")}.exe");
            string amidrv = hex.Convert(hex.AMIDriver(), Path.GetTempPath() + $"\\amifldrv64.sys");
            string charSetAZ = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string moboBrand = helper.GetRandomMoboBrand();
            string moboModel = helper.GetRandomMoboModel(moboBrand);
            noir.WLine("spoofing smbios serials...");
            foreach (string oem in new string[] { "SM", "SP", "SV", "SK", "SF", "CM", "CV", "CA", "CSK", "PSN" })
                helper.RunHidden(amiexe, $"/{oem} To Be Filled By O.E.M.");
            helper.RunHidden(amiexe, $"/IVN American Megatrends Inc.");
            helper.RunHidden(amiexe, $"/IV P{helper.random.Next(1, 21)}.0{helper.random.Next(0, 10)}");
            helper.RunHidden(amiexe, $"/ID {new DateTime(2010, 1, 1).AddDays(helper.random.Next((DateTime.Today - new DateTime(2010, 1, 1)).Days)).ToString("MM/dd/yyyy")}");
            helper.RunHidden(amiexe, $"/SS {helper.GetRandomString(12, charSetAZ)}");
            helper.RunHidden(amiexe, $"/SU AUTO");
            helper.RunHidden(amiexe, $"/BM {moboBrand}");
            helper.RunHidden(amiexe, $"/BP {moboModel}");
            helper.RunHidden(amiexe, $"/BV \"\"");
            helper.RunHidden(amiexe, $"/BS {helper.GetRandomString(12, charSetAZ)}");
            helper.RunHidden(amiexe, $"/BT \"\"");
            helper.RunHidden(amiexe, $"/BLC \"\"");
            helper.RunHidden(amiexe, $"/CT {helper.GetRandomByte()}");
            helper.RunHidden(amiexe, $"/CS {helper.GetRandomString(12, charSetAZ)}");
            helper.RunHidden(amiexe, $"/CO {helper.GetRandomString(8, "0123456789")}");
            helper.RunHidden(amiexe, $"/CPC {helper.GetRandomByte()}");
            helper.RunHidden(amiexe, $"/PAT Unknown");
            helper.RunHidden(amiexe, $"/PPN Unknown");
            helper.RunHidden(amiexe, $"/OS Default string");
            try {
                File.Delete(amiexe);
                File.Delete(amidrv);
            } catch { }
        }

        public static void drives() {
            string volumeid = hex.Convert(hex.VolumeID(), Path.GetTempPath() + $"\\{helper.GetRandomString(8, "0123456789")}.exe");
            string charSetAF = "ABCDEF0123456789";
            foreach (string drive in DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.Name).ToArray()) {
                helper.RunHidden(volumeid, $"{drive} {helper.GetRandomString(4, charSetAF)}-{helper.GetRandomString(4, charSetAF)}");
                noir.WLine($"spoofing disk drive {drive}...");
            }
            try {
                File.Delete(volumeid);
            } catch { }
        }

        public static void mac() {
            noir.WLine("spoofing mac addresses...");
            var adapters = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = TRUE").Get();
            foreach (ManagementObject adapter in adapters) {
                string deviceId = adapter["DeviceID"].ToString();
                string chars = "ABCDEF0123456789";
                string firstSegmentChars = "26AE";
                char[] mac = new char[12];
                mac[0] = '0';
                mac[1] = '2';
                for (int i = 2; i < 12; i++) {
                    if (i == 3) mac[i] = firstSegmentChars[helper.random.Next(firstSegmentChars.Length)];
                    else mac[i] = chars[helper.random.Next(chars.Length)];
                }
                string newMac = string.Join(":", Enumerable.Range(0, 6).Select(i => new string(mac, i * 2, 2)));
                string baseKey = "SYSTEM\\CurrentControlSet\\Control\\Class\\{4D36E972-E325-11CE-BFC1-08002bE10318}";
                string[] subKeys = { "0", "00", "000" };
                foreach (string subKey in subKeys) {
                    try {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"{baseKey}\\{subKey}{deviceId}", writable: true))
                            if (key != null)
                                key.SetValue("NetworkAddress", newMac, RegistryValueKind.String);
                    } catch {
                        noir.WLine(null, $"couldn't change mac for {deviceId}.");
                    }
                }
                foreach (string subKey in subKeys) {
                    try {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"{baseKey}\\{subKey}{deviceId}", writable: true))
                            if (key != null)
                                key.SetValue("PnPCapabilities", 24, RegistryValueKind.DWord);
                    } catch {
                        noir.WLine($"couldn't disable power saving for {deviceId}.");
                    }
                }
            }
            var adapters2 = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID IS NOT NULL").Get();
            foreach (ManagementObject adapter in adapters2) {
                try {
                    string name = adapter["NetConnectionID"].ToString();
                    helper.RunHidden("netsh", $"interface set interface name=\"{name}\" admin=disable");
                    helper.RunHidden("netsh", $"interface set interface name=\"{name}\" admin=enable");
                } catch {
                    noir.WLine($"couldn't reset adapter {adapter["DeviceID"].ToString()}.");
                }
            }
        }

        public static void usb() {
            noir.WLine("updating usb perms...");
            string usbRegistryPath = @"SYSTEM\CurrentControlSet\Enum\USB";
            try {
                using (RegistryKey usbKey = Registry.LocalMachine.OpenSubKey(usbRegistryPath, true)) {
                    if (usbKey == null) {
                        noir.WLine("[-] couldn't find usb registry path.");
                        return;
                    }
                    foreach (string deviceKey in usbKey.GetSubKeyNames()) {
                        using (RegistryKey deviceRegistryKey = usbKey.OpenSubKey(deviceKey, true)) {
                            if (deviceRegistryKey == null)
                                continue;
                            foreach (string instanceKey in deviceRegistryKey.GetSubKeyNames()) {
                                using (RegistryKey instanceRegistryKey = deviceRegistryKey.OpenSubKey(instanceKey, true)) {
                                    if (instanceRegistryKey == null) continue;
                                    try {
                                        RegistrySecurity security = instanceRegistryKey.GetAccessControl();
                                        RegistryAccessRule denyRule = new RegistryAccessRule(
                                            new SecurityIdentifier("S-1-15-2-1"),
                                            RegistryRights.FullControl,
                                            InheritanceFlags.None,
                                            PropagationFlags.None,
                                            AccessControlType.Deny);
                                        security.AddAccessRule(denyRule);
                                        instanceRegistryKey.SetAccessControl(security);
                                    } catch {
                                        noir.WLine($"[-] failed to update usb perms for {deviceKey}.");
                                    }
                                }
                            }
                        }
                    }
                }
            } catch {
                Console.WriteLine($"failed to update usb perms.");
            }
        }

        public static void asus() {
            string charSetAZ = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string batch = "C:\\Windows\\Fonts\\noir-asus.bat";
            string amiexe = hex.Convert(hex.AMIEXE(), "C:\\Windows\\Fonts\\" + helper.GetRandomString(8, charSetAZ));
            string amidrv = hex.Convert(hex.AMIDriver(), "C:\\Windows\\Fonts\\amifldrv64.sys");
            string moboBrand = helper.GetRandomMoboBrand();
            string moboModel = helper.GetRandomMoboModel(moboBrand);
            noir.WLine("spoofing smbios serials...");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("title noir");
            foreach (string oem in new string[] { "SM", "SP", "SV", "SK", "SF", "CM", "CV", "CA", "CSK", "PSN" })
                sb.AppendLine($"{amiexe} /{oem} To Be Filled By O.E.M.");
            sb.AppendLine($"{amiexe} /IVN American Megatrends Inc.");
            sb.AppendLine($"{amiexe} /IV P{helper.random.Next(1, 21)}.0{helper.random.Next(0, 10)}");
            sb.AppendLine($"{amiexe} /ID {new DateTime(2010, 1, 1).AddDays(helper.random.Next((DateTime.Today - new DateTime(2010, 1, 1)).Days)).ToString("MM/dd/yyyy")}");
            sb.AppendLine($"{amiexe} /SS {helper.GetRandomString(12, charSetAZ)}");
            sb.AppendLine($"{amiexe} /SU AUTO");
            sb.AppendLine($"{amiexe} /BM {moboBrand}");
            sb.AppendLine($"{amiexe} /BP {moboModel}");
            sb.AppendLine($"{amiexe} /BV \"\"");
            sb.AppendLine($"{amiexe} /BS {helper.GetRandomString(12, charSetAZ)}");
            sb.AppendLine($"{amiexe} /BT \"\"");
            sb.AppendLine($"{amiexe} /BLC \"\"");
            sb.AppendLine($"{amiexe} /CT {helper.GetRandomByte()}");
            sb.AppendLine($"{amiexe} /CS {helper.GetRandomString(12, charSetAZ)}");
            sb.AppendLine($"{amiexe} /CO {helper.GetRandomString(8, "0123456789")}");
            sb.AppendLine($"{amiexe} /CPC {helper.GetRandomByte()}");
            sb.AppendLine($"{amiexe} /PAT Unknown");
            sb.AppendLine($"{amiexe} /PPN Unknown");
            sb.AppendLine($"{amiexe} /OS Default string");
            File.WriteAllText(batch, sb.ToString());
            helper.RunHidden("cmd.exe", $"/c {batch}");
            helper.RunHidden("schtasks /f /delete /tn \"noir-ASUS\"");
            helper.RunHidden("schtasks /create /tn \"noir-ASUS\" /tr \"C:\\Windows\\Fonts\\noir-asus.bat\" /sc onlogon /ru SYSTEM /rl HIGHEST /F");
        }

        public static void efi() {
            noir.WLine("you need a fat32 formatted usb drive.");
            string answer = noir.RLine("enter usb drive letter").Trim().Replace(":", "");
            if (answer.Length == 1 && DriveInfo.GetDrives().Any(d => d.Name.StartsWith(answer))) {
                noir.WLine($"pushing files to {answer}:...");
                string charSetAZ = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                string moboBrand = helper.GetRandomMoboBrand();
                string moboModel = helper.GetRandomMoboModel(moboBrand);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("echo -off");
                foreach (string oem in new string[] { "SM", "SP", "SV", "SK", "SF", "CM", "CV", "CA", "CSK", "PSN" })
                    sb.AppendLine($"noir.efi /{oem} To Be Filled By O.E.M.");
                sb.AppendLine($"noir.efi /IVN American Megatrends Inc.");
                sb.AppendLine($"noir.efi /IV P{helper.random.Next(1, 21)}.0{helper.random.Next(0, 10)}");
                sb.AppendLine($"noir.efi /ID {new DateTime(2010, 1, 1).AddDays(helper.random.Next((DateTime.Today - new DateTime(2010, 1, 1)).Days)).ToString("MM/dd/yyyy")}");
                sb.AppendLine($"noir.efi /SS {helper.GetRandomString(12, charSetAZ)}");
                sb.AppendLine($"noir.efi /SU AUTO");
                sb.AppendLine($"noir.efi /BM {moboBrand}");
                sb.AppendLine($"noir.efi /BP {moboModel}");
                sb.AppendLine($"noir.efi /BV \"\"");
                sb.AppendLine($"noir.efi /BS {helper.GetRandomString(12, charSetAZ)}");
                sb.AppendLine($"noir.efi /BT \"\"");
                sb.AppendLine($"noir.efi /BLC \"\"");
                sb.AppendLine($"noir.efi /CT {helper.GetRandomByte()}");
                sb.AppendLine($"noir.efi /CS {helper.GetRandomString(12, charSetAZ)}");
                sb.AppendLine($"noir.efi /CO {helper.GetRandomString(8, "0123456789")}");
                sb.AppendLine($"noir.efi /CPC {helper.GetRandomByte()}");
                sb.AppendLine($"noir.efi /PAT Unknown");
                sb.AppendLine($"noir.efi /PPN Unknown");
                sb.AppendLine($"noir.efi /OS Default string");
                sb.AppendLine("exit");
                Directory.CreateDirectory($"{answer}:\\efi");
                Directory.CreateDirectory($"{answer}:\\efi\\boot");
                string bootx64efi = hex.Convert(hex.BootEFI(), $"{answer}:\\efi\\boot\\bootx64.efi");
                string noirefi = hex.Convert(hex.NoirEFI(), $"{answer}:\\noir.efi");
                File.WriteAllText($"{answer}:\\efi\\boot\\startup.nsh", sb.ToString());
                noir.WLine($"done! now reboot to the usb drive.");
            } else {
                noir.WLine("invalid drive letter!", "error");
            }
        }

        public static void refresh() {
            noir.WLine("refreshing...");
            helper.RunHidden("cmd.exe", "net stop winmgmt /y");
            helper.RunHidden("cmd.exe", "net start winmgmt /y");
            helper.RunHidden("cmd.exe", "sc stop winmgmt");
            helper.RunHidden("cmd.exe", "sc start winmgmt");
            helper.RunHidden("cmd.exe", "ipconfig /flushdns");
        }

        public static void tpm() {
            noir.WLine("hiding tpm...");
            string tmpmap = hex.Convert(hex.Mapper(), Path.GetTempPath() + $"\\{helper.GetRandomString(8, "0123456789")}.exe");
            string tpmd = hex.Convert(hex.TPMDriver(), Path.GetTempPath() + "\\tpmhook.sys");
            helper.RunHidden(tmpmap, tpmd);
            noir.WLine("clearing tpm traces...");
            foreach (var key in new string[] { "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\TaskManufacturerId",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\TaskInformationFlags",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\TaskStates",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\PlatformQuoteKeys",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\Endorsement\\EKCertStoreECC\\Certificates",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\Endorsement\\EkRetryLast",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\Endorsement\\EKPub",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\Endorsement\\EkNoFetch",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\Endorsement\\EkTries",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\Parameters\\Wdf\\TimeOfLastTelemetry",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\Admin\\SRKPub",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\WMI\\User",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\KeyAttestationKeys",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\Enum",
                "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\TPM\\ODUID" })
                helper.RunHidden("cmd.exe", $"reg delete \"{key}\" /f");
            helper.RunHidden("powershell.exe", "Clear-Tpm");
            helper.RunHidden("powershell.exe", "Disable-TpmAutoProvisioning");
            try {
                File.Delete(tmpmap);
                File.Delete(tpmd);
            } catch { }
        }

        public static void temp() {
            noir.WLine("mapping driver...");
            string tmpmap = hex.Convert(hex.Mapper(), Path.GetTempPath() + $"\\{helper.GetRandomString(8, "0123456789")}.exe");
            string tmpdrv = hex.Convert(hex.TempDriver(), Path.GetTempPath() + $"\\{helper.GetRandomString(8, "0123456789")}.sys");
            helper.RunHidden(tmpmap, tmpdrv);
            try {
                File.Delete(tmpmap);
                File.Delete(tmpdrv);
            } catch { }
        }

        public static void clean() {
            noir.WLine("cleaning...");
            string cleaner = hex.Convert(hex.Cleaner(), Path.GetTempPath() + $"\\{helper.GetRandomString(8, "0123456789")}.exe");
            helper.RunHidden(cleaner);
            try {
                File.Delete(cleaner);
            } catch { }
        }

        public static string check() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[--- Baseboard ------------------]");
            foreach (string line in helper.GetWMIInfo("Win32_BaseBoard", new string[] { "Manufacturer", "Product", "SerialNumber" }))
                sb.AppendLine(line);
            sb.AppendLine("\r\n[--- Computer -------------------]");
            foreach (string line in helper.GetWMIInfo("Win32_ComputerSystemProduct", new string[] { "Name", "Vendor", "Version", "IdentifyingNumber", "UUID" }))
                sb.AppendLine(line);
            sb.AppendLine("\r\n[--- Volume IDs -----------------]");
            foreach (string line in helper.GetVolumeIDs())
                sb.AppendLine(line);
            sb.AppendLine("\r\n[--- Disk Drives ----------------]");
            foreach (string line in helper.GetWMIInfo("Win32_DiskDrive", new string[] { "SerialNumber" }))
                sb.AppendLine(line.Replace(" ", "").Replace("|", " | "));
            sb.AppendLine("\r\n[--- MAC Addresses --------------]");
            foreach (string line in helper.GetMACAddresses())
                sb.AppendLine(line);
            sb.AppendLine("\r\n[--------------------------------]");
            sb.AppendLine("generated by noir");
            return sb.ToString();
        }
    }
}