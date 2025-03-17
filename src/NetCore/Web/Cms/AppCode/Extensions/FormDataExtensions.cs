using Cms.Models.Json;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Extensions.Text;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Cms.AppCode.Extensions;

static public class FormDataExtensions
{
    static public void ApplyTo(this IEnumerable<NameValue> formData,
                               IUIControl control,
                               NameValueCollection secrets = null)
    {
        foreach (var nv in formData)
        {
            var input = control.GetControl(nv.Name) as IInputUIControl;
            if (input != null)
            {
                input.Value = nv.Value.Replace(secrets);
            }
        }
    }


    static public void ApplyAndSumit(this IEnumerable<NameValue> formData,
                               ICreatable creatable,
                               NameValueCollection secrets = null,
                               string path = null,
                               bool applySecrets = true)
    {
        if (creatable is IUI)
        {
            string originalRelativePath = null;
            if (!String.IsNullOrEmpty(path) && creatable is SchemaNode)
            {
                originalRelativePath = ((SchemaNode)creatable).RelativePath;
                ((SchemaNode)creatable).RelativePath = path;
            }

            var control = ((IUI)creatable).GetUIControl(false);
            if (control != null)
            {
                formData.ApplyTo(control, applySecrets ? secrets : null);

                if (control is ISubmit controlSubmit)
                {
                    controlSubmit.Submit(secrets ?? new NameValueCollection());
                }
            }

            if (!String.IsNullOrEmpty(path) && creatable is SchemaNode)
            {
                ((SchemaNode)creatable).RelativePath = originalRelativePath;
            }
        }
    }
}
