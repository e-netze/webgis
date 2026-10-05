namespace Cms.Models;

public sealed class EditFormPlaygroundViewModel
{
    public EditFormPlaygroundCmsItem[] CmsItems { get; set; } = [];
}

public sealed record EditFormPlaygroundCmsItem(string Id, string Name, string[] Deployments);
