namespace E.Standard.Cms.Abstraction;
public interface ICmsLogger
{
    public void Log(string username,
                    string method,
                    string command,
                    params string[] values);
}
