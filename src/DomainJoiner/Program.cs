﻿using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;

namespace DomainJoiner {
    class Program {

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        private static string domain, username, password;

        static void Main(string[] args) {
            /* Grab handle and hide (mostly for mass deployment to users who are using their systems) */
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            /* Create support directory */
            System.IO.Directory.CreateDirectory(@"C:\Support");

            if (args.Length == 0) {
                /* Ask user to input params */
                string path = @"C:\Support\failed.txt";
                File.Create(path).Dispose();
                File.AppendAllLines(path, new[] { "No Params." });
            } else if (args.Length > 3) {
                /* Too many params */
                string path = @"C:\Support\failed.txt";
                File.Create(path).Dispose();
                File.AppendAllLines(path, new[] { "Arguments Overloaded." });
            } else {
                /* loop args array */
                for (int i = 0; i < args.Length; i++) {
                    if (i == 0)
                        domain = args[i];
                    if (i == 1)
                        username = args[i];
                    if (i == 2)
                        password = args[i];
                }

                Connect(domain, username, password);

                Console.WriteLine("Finished.");
            }
        }

        public static bool Connect(string domain, string username, string password) {
            using (ManagementObject wmiObject = new ManagementObject(new ManagementPath("Win32_ComputerSystem.Name='" + Environment.MachineName + "'"))) {
                try {
                    // Obtain in-parameters for the method
                    ManagementBaseObject inParams = wmiObject.GetMethodParameters("JoinDomainOrWorkgroup");
                    inParams["Name"] = domain;
                    inParams["Password"] = password;
                    inParams["UserName"] = username;
                    inParams["FJoinOptions"] = 3; // Magic number: 3 = join to domain and create computer account
                    // Execute the method and obtain the return values.
                    ManagementBaseObject joinParams = wmiObject.InvokeMethod("JoinDomainOrWorkgroup", inParams, null);

                    // Did it work?
                    if ((uint)(joinParams.Properties["ReturnValue"].Value) != 0) {
                        /* Domain error codes: https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/joindomainorworkgroup-method-in-class-win32-computersystem */
                        string path = @"C:\Support\failed.txt";
                        File.Create(path).Dispose();
                        File.AppendAllLines(path, new[] { string.Format("JoinDomainOrWorkgroup failed with return code: '{0}', see https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/joindomainorworkgroup-method-in-class-win32-computersystem for return codes.", joinParams["ReturnValue"]) });
                        return false;
                    }
                    return true;
                } catch {
                    return false;
                }
            }
        }
    }
}