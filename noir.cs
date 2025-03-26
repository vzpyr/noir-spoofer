using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace noir {
    public class noir {
        [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")] static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int Width, int Height, uint uFlags);
        [DllImport("user32.dll")] static extern bool BlockInput(bool fBlockIt);

        public static licensing licensing = new licensing("", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\.keyfile");
        public static bool isValid;
        public static string message, expiration;
        public static int usesLeft;

        public static async Task Main(string[] args) {
            IntPtr hwnd = GetConsoleWindow();
            SetWindowPos(hwnd, IntPtr.Zero,
                (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - 385) / 2,
                (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - 527) / 2,
            0, 0, 0x0004 | 0x0001);
            Console.SetWindowSize(46, 30);
            Console.SetBufferSize(46, 30);
            SetWindowLong(hwnd, -20, GetWindowLong(hwnd, -20) | 0x80000);
            SetLayeredWindowAttributes(hwnd, 0, 200, 0x2);
            Console.Title = "noir / 0.1";
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) {
                WLogo();
                WLine("administrative permissions are required!", "error");
                Thread.Sleep(2000);
                try {
                    Process.Start(new ProcessStartInfo(Assembly.GetEntryAssembly().Location) { Verb = "runas" });
                    Environment.Exit(0);
                } catch {
                    Environment.Exit(1);
                }
            }
            Console.Clear();
            WLogo();
            if (licensing.GetKey() != null) {
                WLine("trying saved key...");
                var (isValid, message, expiration, usesLeft) = await licensing.Verify(licensing.GetKey());
                noir.isValid = isValid;
                noir.message = message;
                noir.expiration = expiration;
                noir.usesLeft = usesLeft;
                if (isValid) {
                    WLine("valid key!", "success");
                    Thread.Sleep(500);
                    Menu();
                } else {
                    string licenseKey = RLine("enter key", licensing.GetKey(), true);
                    (isValid, message, expiration, usesLeft) = await licensing.Verify(licenseKey);
                    noir.isValid = isValid;
                    noir.message = message;
                    noir.expiration = expiration;
                    noir.usesLeft = usesLeft;
                    if (isValid) {
                        licensing.SetKey(licenseKey);
                        WLine("valid key!", "success");
                        Thread.Sleep(500);
                        Menu();
                    } else {
                        WLine("invalid key!", "error");
                        Thread.Sleep(2000);
                        Environment.Exit(2);
                    }
                }
            }
        }

        public static void Menu() {
            string defenderStatus = helper.GetDefenderStatus();
            Console.Clear();
            WLogo();
            WLine($"expires on | %{DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiration)).DateTime.ToString("dd. MMM yyyy")}%", "success");
            WLine($"uses left  | %{usesLeft}%", "success");
            WLine($"defender   | %{defenderStatus}%", defenderStatus.Equals("Enabled") ? "error" : (defenderStatus.Equals("Disabled") ? "success" : "warn"));
            if (defenderStatus != "Disabled")
                WLine("you need to disable defender!");
            WDiv();
            WLine("[1] perm");
            WLine("[2] temp");
            WLine("[3] tpm");
            WLine("[4] check");
            WDiv();
            switch (RLine("enter a number")) {
                case "1":
                    try {
                        var methodMap = new Dictionary<string, int> {
                            { "Acer", 3 }, { "ASRock", 1 }, { "ASUS", 2 }, { "Biostar", 3 },
                            { "Dell", 3 }, { "Gigabyte", 1 }, { "HP", 3 }, { "Lenovo", 3 }, { "MSI", 3 }
                        };
                        string brand = RLine("what motherboard do you have?").Trim().ToLower();
                        BlockInput(true);
                        switch (methodMap.TryGetValue(brand.ToLower(), out int value) ? value : 0) {
                            case 0:
                                WLine("unknown brand. possible answers:", "error");
                                WLine("acer, asrock, asus, biostar, dell, gigabyte, hp, lenovo, msi", "error");
                                break;
                            case 1:
                                tool.smbios();
                                tool.drives();
                                tool.mac();
                                tool.usb();
                                tool.refresh();
                                break;
                            case 2:
                                tool.asus();
                                tool.drives();
                                tool.mac();
                                tool.usb();
                                tool.refresh();
                                break;
                            case 3:
                                tool.smbios();
                                tool.drives();
                                tool.mac();
                                tool.usb();
                                tool.refresh();
                                tool.efi();
                                break;
                        }
                        tool.tpm();
                    } catch (Exception e) {
                        WLine("perm spoofing failed!", "error");
                        WLine("show this error to our support:", "error");
                        Console.WriteLine(e.StackTrace);
                    }
                    BlockInput(false);
                    break;
                case "2":
                    tool.clean();
                    tool.temp();
                    break;
                case "3":
                    tool.tpm();
                    break;
                case "4":
                    try {
                        string path = Path.GetTempPath() + $"\\serials-{helper.GetRandomString(4, "0123456789")}.txt";
                        File.Create(path).Close();
                        File.WriteAllText(path, tool.check());
                        Process.Start("notepad.exe", path);
                    } catch {
                        WLine("couldn't save serials!", "error");
                    }
                    break;
            }
            WLine("returning...");
            Thread.Sleep(2000);
            Menu();
        }

        public static string RLine(string text, string preInput = "", bool censor = false) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("?");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.White;
            if (censor) {
                string input = preInput;
                Console.Write(new string('*', preInput.Length));
                ConsoleKeyInfo keyInfo;
                do {
                    keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Backspace) {
                        if (input.Length > 0) {
                            input = input.Substring(0, input.Length - 1);
                            Console.Write("\b \b");
                        }
                    } else if (keyInfo.Key != ConsoleKey.Enter) {
                        input += keyInfo.KeyChar;
                        Console.Write("*");
                    }
                } while (keyInfo.Key != ConsoleKey.Enter);
                Console.WriteLine();
                return input;
            } else {
                if (!string.IsNullOrEmpty(preInput)) {
                    string input = preInput;
                    Console.Write(preInput);
                    Console.CursorLeft = Console.CursorLeft;
                    string enteredText = Console.ReadLine();
                    return input + enteredText;
                } else {
                    return Console.ReadLine();
                }
            }
        }

        public static void WLine(string text, string status = "info") {
            string code = "#";
            switch (status) {
                case "success":
                    code = "+";
                    break;
                case "warn":
                    code = "!";
                    break;
                case "error":
                    code = "-";
                    break;
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(code);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] ");
            string[] parts = text.Split('%');
            bool insideApostrophes = false;
            foreach (var part in parts) {
                if (insideApostrophes) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(part);
                } else {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(part);
                }
                insideApostrophes = !insideApostrophes;
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        public static void WDiv(int length = 32) {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(new string('-', length));
            Console.WriteLine();
            Thread.Sleep(50);
        }

        public static void WLogo() {
            Console.WriteLine("▄▄▄▄   ▄▄▄  ▄  ▄▄▄\r\n█   █ █   █ ▄ █\r\n█   █ ▀▄▄▄▀ █ █");
            Thread.Sleep(1);
            WDiv(18);
        }
    }
}