#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;

using static E.Standard.WebMapping.Core.Api.EventResponse.Models.VisFilterDefinitionDTO;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class VisFilterDefinitionDTOExtensions
{
    static public void AddArgument(this VisFilterDefinitionDTO dto, string name, string value)
    {
        List<VisFilterDefinitionArgument> args = dto.Arguments != null ?
            new List<VisFilterDefinitionArgument>(dto.Arguments) :
            new List<VisFilterDefinitionArgument>();

        args.Add(new VisFilterDefinitionArgument()
        {
            Name = name,
            Value = value
        });
        dto.Arguments = args.ToArray();
    }

    static public void CalcServiceId(this VisFilterDefinitionDTO dto)
    {
        if (dto.IsTocVisFilter() == false
            && dto.Id?.Contains("~") == true)
        {
            dto.ServiceId = dto.Id.Split('~')[0];
            dto.Id = dto.Id.Split('~')[1];
        }
    }

    /// <summary>
    /// HMAC (purpose <see cref="CryptoPurposes.VisFilterSignature"/>) over the filter id (service + layer) and the arguments,
    /// so a signed where clause can't be applied to another layer.
    /// </summary>
    static public string CalcSignature(this VisFilterDefinitionDTO dto, ICryptoService cryptoService)
    {
        string? args = dto.SignatureArguments();
        if (args == null)
        {
            return "empty";
        }

        string signed = cryptoService.SignText($"visfilter|{dto.Id}|{args}", CryptoPurposes.VisFilterSignature);
        return signed.Substring(signed.LastIndexOf('.') + 1);
    }

    // legacy: hash of the default key encryption, args only (still persisted in saved/shared maps)
    static private string CalcLegacySignature(this VisFilterDefinitionDTO dto, ICryptoService cryptoService)
    {
        string? args = dto.SignatureArguments();
        if (args == null)
        {
            return "empty";
        }

        return Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(
                    cryptoService.StaticDefaultEncrypt_Aes(args))));
    }

    static private string? SignatureArguments(this VisFilterDefinitionDTO dto)
        => dto.Arguments != null && dto.Arguments.Length > 0
            ? string.Join("&", dto.Arguments.Select(arg => $"{arg.Name}={arg.Value}"))
            : null;

    static public void CheckSignature(this VisFilterDefinitionDTO dto, ICryptoService cryptoService)
    {
        if (dto.Signature == null)
        {
            throw new UnauthorizedAccessException("Missing filter signature");
        }

        var signature = Encoding.UTF8.GetBytes(dto.Signature);
        bool valid = CryptographicOperations.FixedTimeEquals(signature, Encoding.UTF8.GetBytes(dto.CalcSignature(cryptoService)));
        valid |= CryptographicOperations.FixedTimeEquals(signature, Encoding.UTF8.GetBytes(dto.CalcLegacySignature(cryptoService)));

        if (!valid)
        {
            throw new UnauthorizedAccessException("Invalid filter signature");
        }
    }

    static public bool IsTocVisFilter(this VisFilterDefinitionDTO? dto, string serviceId = "")
    {
        if (dto?.Arguments?.Length != 1
            || dto.Arguments[0].Name != "sql"
            || String.IsNullOrEmpty(dto.Arguments[0].Value))
        {
            return false;
        }

        return String.IsNullOrEmpty(serviceId)
            ? dto.Id.StartsWith(VisFilterDefinitionDTO.TocFilterPrefix, StringComparison.Ordinal)
            : dto.Id.StartsWith($"{VisFilterDefinitionDTO.TocFilterPrefix}{serviceId}{VisFilterDefinitionDTO.TocFilterSeparator}", StringComparison.Ordinal);
    }

    static public string? TocVisFilterServiceId(this VisFilterDefinitionDTO? dto)
    {
        if (!dto.IsTocVisFilter())
        {
            throw new InvalidOperationException("Not a TOC vis filter");
        }

        return dto?.Id.Split(VisFilterDefinitionDTO.TocFilterSeparator).Skip(1).First();
    }

    static public string? TocVisFilterLayerId(this VisFilterDefinitionDTO? dto)
    {
        if (!dto.IsTocVisFilter())
        {
            throw new InvalidOperationException("Not a TOC vis filter");
        }

        return dto?.Id.Substring(dto.Id.LastIndexOf(VisFilterDefinitionDTO.TocFilterSeparator) + 1);
    }

    static public string TocVisFilterWhereClause(this VisFilterDefinitionDTO? dto)
    {
        if (!dto.IsTocVisFilter())
        {
            throw new InvalidOperationException("Not a TOC vis filter");
        }
        return dto?.Arguments?[0].Value ?? "";
    }
}
