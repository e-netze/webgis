using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Reflection;
using E.Standard.CMS.Core.Security;
using E.Standard.Extensions.Text;
using System;
using System.Reflection;

namespace E.Standard.CMS.Core.Schema;

public class Persistable : Creatable, IPersistable
{
    virtual public void Load(IStreamDocument stream)
    {
        foreach (var propertyInfo in GetType().GetProperties())
        {
            var cmsPersitableAttribute = propertyInfo.GetCustomAttribute<CmsPersistableAttribute>();

            if (cmsPersitableAttribute != null)
            {
                if (propertyInfo.PropertyType.IsEnum)
                {
                    propertyInfo.SetValue(
                        this,
                        (int)stream.Load(cmsPersitableAttribute.Name, 0));
                }
                else
                {
                    var value = stream.Load(cmsPersitableAttribute.Name, propertyInfo.PropertyType.CmsPropertyDefaultValue());

                    if (value != null && propertyInfo.UsePersistableCmsPropertyEncryption())
                    {
                        if (value.ToString().StartsWith("enc0:"))
                        {
                            value = CmsCryptoHelper.Decrypt(value.ToString().Substring(5), "{09897E63-3D87-4E68-8E07-434B46CC72EC}")
                                .Replace(stream.StringReplace);
                        }
                    }

                    propertyInfo.SetValue(
                        this,
                        Convert.ChangeType(
                            value,
                            propertyInfo.PropertyType));
                }
            }
        }
    }

    virtual public void Save(IStreamDocument stream)
    {
        foreach (var propertyInfo in GetType().GetProperties())
        {
            var cmsPersitableAttribute = propertyInfo.GetCustomAttribute<CmsPersistableAttribute>();

            if (cmsPersitableAttribute != null)
            {
                if (propertyInfo.PropertyType.IsEnum)
                {
                    stream.Save(
                        cmsPersitableAttribute.Name,
                        (int)propertyInfo.GetValue(this));
                }
                else if (propertyInfo.GetValue(this) != null)
                {
                    var value = propertyInfo.GetValue(this);

                    if (propertyInfo.UsePersistableCmsPropertyEncryption())
                    {
                        value = $"enc0:{CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(value.ToString()), "{09897E63-3D87-4E68-8E07-434B46CC72EC}")}";
                    }

                    stream.Save(cmsPersitableAttribute.Name, value);
                }
            }
        }
    }
}
