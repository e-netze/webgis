using System.Collections.Generic;

namespace E.Standard.Api.App.DTOs;

public sealed class EditThemesResponseDTO : VersionDTO
{
    public IEnumerable<object> themes { get; set; }
}
