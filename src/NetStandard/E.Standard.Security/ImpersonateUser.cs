//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Security.Permissions;
//using System.Security.Principal;
//using System.Text;

//namespace E.Standard.Security
//{
//    public class ImpersonateUser : IDisposable
//    {
//        [DllImport("advapi32.dll", SetLastError = true)]
//        public static extern bool LogonUser(
//            String lpszUsername,
//            String lpszDomain,
//            String lpszPassword,
//            int dwLogonType,
//            int dwLogonProvider,
//            ref IntPtr phToken);

//        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
//        public extern static bool CloseHandle(IntPtr handle);

//        private IntPtr tokenHandle = new IntPtr(0);
//        private /*WindowsImpersonationContext*/ object impersonatedUser;


//        public ImpersonateUser()
//        {

//        }

//        public ImpersonateUser(string domainName, string userName, string password)
//        {
//            this.Impersonate(domainName, userName, password);
//        }

//        public ImpersonateUser(string[] user)
//        {
//            if (user != null && user.Length == 3)
//            {
//                this.Impersonate(user[0], user[1], user[2]);
//            }
//        }

//        // If you incorporate this code into a DLL, be sure to demand that it
//        // runs with FullTrust.
//        //[PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
//        public void Impersonate(string domainName, string userName, string password)
//        {
//            //try
//            //{

//            //    // Use the unmanaged LogonUser function to get the user token for
//            //    // the specified user, domain, and password.
//            //    const int LOGON32_PROVIDER_DEFAULT = 0;
//            //    // Passing this parameter causes LogonUser to create a primary token.
//            //    const int LOGON32_LOGON_INTERACTIVE = 2;
//            //    tokenHandle = IntPtr.Zero;

//            //    // ---- Step - 1 
//            //    // Call LogonUser to obtain a handle to an access token.
//            //    bool returnValue = LogonUser(
//            //        userName,
//            //        domainName,
//            //        password,
//            //        LOGON32_LOGON_INTERACTIVE,
//            //        LOGON32_PROVIDER_DEFAULT,
//            //        ref tokenHandle);         // tokenHandle - new security token

//            //    if (false == returnValue)
//            //    {
//            //        int ret = Marshal.GetLastWin32Error();
//            //        Console.WriteLine("LogonUser call failed with error code : " +
//            //            ret);
//            //        throw new System.ComponentModel.Win32Exception(ret);
//            //    }

//            //    // ---- Step - 2 
//            //    WindowsIdentity newId = new WindowsIdentity(tokenHandle);

//            //    // ---- Step - 3 
//            //    impersonatedUser = newId.Impersonate();

//            //}
//            //catch (Exception ex)
//            //{
//            //    Console.WriteLine("Exception occurred. " + ex.Message);
//            //}
//        }

//        // Stops impersonation
//        public void Undo()
//        {
//            //if (impersonatedUser != null)
//            //{
//            //    impersonatedUser.Undo();
//            //    impersonatedUser = null;
//            //}
//            //// Free the tokens.
//            //if (tokenHandle != IntPtr.Zero)
//            //{
//            //    CloseHandle(tokenHandle);
//            //    tokenHandle = IntPtr.Zero;
//            //}
//        }

//        #region IDisposable Member

//        public void Dispose()
//        {
//            this.Undo();
//        }

//        #endregion
//    }
//}
