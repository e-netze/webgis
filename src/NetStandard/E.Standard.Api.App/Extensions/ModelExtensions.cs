using System;

using E.Standard.Api.App.DTOs;

namespace E.Standard.Api.App.Extensions;

internal static class ModelExtensions
{
    static public bool IsValid(this EditThemeDTO.MaskValidation maskValidation)
        => !String.IsNullOrEmpty(maskValidation?.FieldName);
}
