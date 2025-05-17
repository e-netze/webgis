using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core;

public class FeatureAttachements : IFeatureAttachments
{
    private readonly List<IFeatureAttachment> _attachements = new List<IFeatureAttachment>();

    public void Add(IFeatureAttachment attachment)
    {
        if (attachment == null) throw new ArgumentNullException(nameof(attachment));
        _attachements.Add(attachment);
    }

    public IEnumerable<IFeatureAttachment> Attachements => _attachements.AsReadOnly();
}

public class FeatureAttachment : IFeatureAttachment
{
    public string Name { get; set; }
    public string Type { get; set; }
    public byte[] Data { get; set; }
}
