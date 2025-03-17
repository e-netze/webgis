using E.Standard.Api.App.DTOs;
using System;

namespace E.Standard.Api.App.Extensions;

internal static class ModelExtensions
{
    static public bool IsValid(this EditThemeDTO.MaskValidation maskValidation)
        => !String.IsNullOrEmpty(maskValidation?.FieldName);
}
