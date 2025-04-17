using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Security;
using System.Collections.Generic;

namespace E.Standard.Api.App.Services.Cache;

public class AuthObject<T>
{
    private T _object;
    private CmsDocument.AuthNode _authNode;

    public AuthObject(T o, CmsDocument.AuthNode authNode)
    {
        _object = o;
        _authNode = authNode;
    }

    public T QueryObject(CmsDocument.UserIdentification cmsui)
    {
        //if (cmsui == null && _authNode != null)
        //    return default(T);

        if (!CmsDocument.CheckAuthorization(cmsui, _authNode))
        {
            return default(T);
        }

        return _object;
    }

    public static T[] QueryObjectArray(IEnumerable<AuthObject<T>> array, CmsDocument.UserIdentification cmsui)
    {
        if (array == null)
        {
            return new T[0];
        }

        List<T> ret = new List<T>();
        foreach (var a in array)
        {
            T t = a.QueryObject(cmsui);
            if (t is IAuthClone<T>)
            {
                t = ((IAuthClone<T>)t).AuthClone(cmsui);
            }

            if (t != null)
            {
                ret.Add(t);
            }
        }

        return ret.ToArray();
    }
}

public class AuthProperty<T>
{
    private CmsDocument.AuthNode _authNode;
    private T _propertyValue, _unauthorizedValue;

    public AuthProperty(string property, T propertyValue, T unauthorizedValue, CmsDocument.AuthNode authNode, bool appendEveryoneAllowedIfEmpty = true)
    {
        this.Property = property;
        _authNode = authNode;
        _propertyValue = propertyValue;
        _unauthorizedValue = unauthorizedValue;

        if (appendEveryoneAllowedIfEmpty == true 
            && authNode.Users.Count == 0 
            && authNode.Roles.Count == 0)
        {
            authNode.Append(new CmsDocument.AuthNode(CmsUserList.Everyone, CmsUserList.Empty));
        }
    }

    public string Property { get; private set; }

    public T AuthorizedPropertyValue(CmsDocument.UserIdentification cmsui)
    {
        if (!CmsDocument.CheckAuthorization(cmsui, _authNode))
        {
            return _unauthorizedValue;
        }

        return _propertyValue;
    }

    public CmsDocument.AuthNode AuthNode => _authNode;
}

public interface IAuthClone<T>
{
    T AuthClone(CmsDocument.UserIdentification cmsui);
}