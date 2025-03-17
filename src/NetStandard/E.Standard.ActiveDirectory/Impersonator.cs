using System;

namespace E.Standard.ActiveDirectory;

public class Impersonator : IImpersonator
{
    private string _domain = String.Empty, _user = String.Empty, _pwd = String.Empty;
    private bool _isValid = false;

    public Impersonator(string impersonateString)
    {
        try
        {
            string[]? s = impersonateString?.Split('|');

            if (s != null && s.Length == 3)
            {
                _domain = s[0];
                _user = s[1];
                _pwd = s[2];

                _isValid = !String.IsNullOrEmpty(_user) && !String.IsNullOrEmpty(_pwd);
            }
        }
        catch (Exception/* ex*/)
        {
            _isValid = false;
        }
    }

    public IDisposable ImpersonateContext(bool impersonate)
    {
        ImpersoneContextClass iuser = new ImpersoneContextClass();
        if (impersonate && _isValid)
        {
            try
            {
                iuser.Impersonate(_domain, _user, _pwd);
            }
            catch { }
        }

        return iuser;
    }

    private class ImpersoneContextClass : IImpersonateUser, IDisposable
    {
        private IImpersonateUser? _iuser = null;

        #region IDisposable Member

        public void Dispose()
        {
            this.Undo();
        }

        #endregion

        public void Impersonate(string domainName, string userName, string password)
        {
            if (_iuser == null)
            {
                _iuser = ActiveDirectoryFactory.InterfaceImplementation<IImpersonateUser>();
            }

            _iuser.Impersonate(domainName, userName, password);
        }

        public void Undo()
        {
            if (_iuser != null)
            {
                _iuser.Undo();
                _iuser = null;
            }
        }


    }
}