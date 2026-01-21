#nullable enable

using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

    static public string CalcSignature(this VisFilterDefinitionDTO dto, ICryptoService cryptoService)
    {
        if (dto.Arguments != null && dto.Arguments.Length > 0)
        {
            List<string> args = new List<string>();

            foreach (var arg in dto.Arguments)
            {
                args.Add($"{arg.Name}={arg.Value}");
            }

            return Convert.ToBase64String(
                    SHA256.HashData(System.Text.UTF8Encoding.UTF8.GetBytes(
                        cryptoService.StaticDefaultEncrypt(
                            string.Join("&", args)
                        )
                    )
                )
            );
        }

        return "empty";
    }

    static public void CheckSignature(this VisFilterDefinitionDTO dto, ICryptoService cryptoService)
    {
        if (dto.Signature != null)
        {
            string calcSig = dto.CalcSignature(cryptoService);
            if (calcSig != dto.Signature)
            {
                throw new UnauthorizedAccessException("Invalid filter signature");
            }
        }
        else
        {
            throw new UnauthorizedAccessException("Missing filter signature");
        }
    }

    static public bool IsTocVisFilter(this VisFilterDefinitionDTO? dto, string serviceId = "")
    {
        if(dto?.Arguments?.Length != 1 
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
        if(!dto.IsTocVisFilter())
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
