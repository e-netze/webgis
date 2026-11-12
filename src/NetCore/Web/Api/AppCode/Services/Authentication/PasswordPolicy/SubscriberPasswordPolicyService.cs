#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using E.Standard.Api.App.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Core.AppCode.Services.Authentication.PasswordPolicy;

public sealed record PasswordPolicyResult(bool IsValid, string? ErrorKey = null, string? ErrorArgument = null)
{
    public const string ErrorInvalidLength = "invalid-length";
    public const string ErrorPersonalInfo = "personal-info";
    public const string ErrorTooCommon = "too-common";
    public const string ErrorTooSimple = "too-simple";
    public const string ErrorCharacterClasses = "character-classes";

    public static readonly PasswordPolicyResult Valid = new(true);

    /// <summary>For the (english only) admin/register pages.</summary>
    public string ToEnglishMessage() => ErrorKey switch
    {
        null => "",
        ErrorInvalidLength => $"Invalid password: min. {ErrorArgument} characters, max. {SubscriberSecurityOptions.PasswordMaxLength} characters",
        ErrorPersonalInfo => "Invalid password: must not contain the username, name or email address",
        ErrorTooCommon => "Invalid password: this password is too common",
        ErrorTooSimple => "Invalid password: too simple (repetitions or sequences)",
        ErrorCharacterClasses => $"Invalid password: must contain at least {ErrorArgument} of the following: lowercase letter, uppercase letter, digit, special character",
        _ => "Invalid password"
    };
}

/// <summary>
/// Policy for new or changed subscriber passwords (existing passwords stay valid):
/// length, no personal info, no trivial patterns, blocklist of common passwords and (optional) character classes.
/// </summary>
public class SubscriberPasswordPolicyService
{
    private readonly SubscriberSecurityOptions _options;
    private readonly Lazy<HashSet<string>> _fileBlocklist;

    public SubscriberPasswordPolicyService(IOptions<SubscriberSecurityOptions> options,
                                           IWebHostEnvironment environment,
                                           ILogger<SubscriberPasswordPolicyService> logger)
    {
        _options = options.Value;
        _fileBlocklist = new Lazy<HashSet<string>>(() => LoadBlocklist(_options.PasswordBlocklistFile, environment.ContentRootPath, logger));
    }

    public PasswordPolicyResult Validate(string? password, string? username, string? firstName = null, string? lastName = null, string? email = null)
    {
        if (String.IsNullOrWhiteSpace(password)
            || password.Length < _options.PasswordMinLength
            || password.Length > SubscriberSecurityOptions.PasswordMaxLength)
        {
            return new(false, PasswordPolicyResult.ErrorInvalidLength, _options.PasswordMinLength.ToString());
        }

        string lower = password.ToLowerInvariant();

        if (ContainsPersonalInfo(NormalizedVariants(lower), username, firstName, lastName, email))
        {
            return new(false, PasswordPolicyResult.ErrorPersonalInfo);
        }

        if (IsTrivial(lower))
        {
            return new(false, PasswordPolicyResult.ErrorTooSimple);
        }

        if (IsBlocked(lower))
        {
            return new(false, PasswordPolicyResult.ErrorTooCommon);
        }

        if (_options.PasswordRequiredCharacterClasses > 0 && CountCharacterClasses(password) < _options.PasswordRequiredCharacterClasses)
        {
            return new(false, PasswordPolicyResult.ErrorCharacterClasses, _options.PasswordRequiredCharacterClasses.ToString());
        }

        return PasswordPolicyResult.Valid;
    }

    /// <summary>
    /// The password is a publicly known default password (e.g. of the initial installation)
    /// => it must be changed at the next login.
    /// </summary>
    public bool IsKnownDefaultPassword(string? password)
        => !String.IsNullOrEmpty(password) && KnownDefaultPasswords.Contains(password.Trim().ToLowerInvariant());

    #region Rules

    private static bool ContainsPersonalInfo(List<string> variants, params string?[] values)
    {
        var parts = new List<string>();
        foreach (var value in values.Where(v => !String.IsNullOrWhiteSpace(v)))
        {
            string lowerValue = value!.Trim().ToLowerInvariant();
            parts.Add(lowerValue.Split('@')[0]);
            parts.AddRange(lowerValue.Split(new[] { '.', '_', '-', '@', ' ', '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));
        }

        return parts
            .Where(p => p.Length >= 4)
            .Any(p => variants.Any(v => v.Contains(p)));
    }

    private static bool IsTrivial(string lower)
    {
        // few different characters: aaaaaaaaaaaa, abababababab, 111122221111
        if (lower.Distinct().Count() < 4)
        {
            return true;
        }

        // repeated block: abcdabcdabcd, hans1hans1hans1
        for (int period = 1; period <= lower.Length / 2; period++)
        {
            bool periodic = true;
            for (int i = period; i < lower.Length && periodic; i++)
            {
                periodic = lower[i] == lower[i - period];
            }
            if (periodic)
            {
                return true;
            }
        }

        // sequences: 123456789012, abcdefghijkl, qwertzuiopas (also reversed)
        foreach (var sequence in Sequences)
        {
            if (sequence.Contains(lower) || new string(sequence.Reverse().ToArray()).Contains(lower))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBlocked(string lower)
    {
        var fileBlocklist = _fileBlocklist.Value;

        if (fileBlocklist.Contains(lower) || KnownDefaultPasswords.Contains(lower))
        {
            return true;
        }

        // "Sommer2026!", "P@ssw0rd123" => base word without leading/trailing digits and special characters, leetspeak decoded
        string core = lower.Trim(TrimCharacters);
        if (core.Length == 0)
        {
            return false;
        }

        return NormalizedVariants(core).Any(variant => BuiltInBlocklist.Contains(variant) || KnownDefaultPasswords.Contains(variant) || fileBlocklist.Contains(variant));
    }

    private static int CountCharacterClasses(string password)
    {
        int classes = 0;
        if (password.Any(Char.IsLower)) classes++;
        if (password.Any(Char.IsUpper)) classes++;
        if (password.Any(Char.IsDigit)) classes++;
        if (password.Any(c => !Char.IsLetterOrDigit(c))) classes++;

        return classes;
    }

    /// <summary>The lowercase value and "leetspeak" decoded variants (p@ssw0rd => password).</summary>
    private static List<string> NormalizedVariants(string lower)
    {
        string Decode(char one)
        {
            var sb = new StringBuilder(lower.Length);
            foreach (char c in lower)
            {
                sb.Append(c switch
                {
                    '@' or '4' => 'a',
                    '0' => 'o',
                    '3' => 'e',
                    '$' or '5' => 's',
                    '7' or '+' => 't',
                    '1' or '!' => one,
                    _ => c
                });
            }
            return sb.ToString();
        }

        return new[] { lower, Decode('i'), Decode('l') }.Distinct().ToList();
    }

    #endregion

    #region Lists

    private static readonly char[] TrimCharacters = "0123456789!\"§$%&/()=?`´*+~'#,;.:-_<>|^°@€{}[]\\ ".ToCharArray();

    /// <summary>Default passwords shipped with WebGIS (e.g. subscriber "author" of the initial installation, see _setup/proto).</summary>
    /// <remarks>Not checked in DEBUG builds, so the default subscriber can still be used in development environments.</remarks>
    private static readonly HashSet<string> KnownDefaultPasswords = new(StringComparer.Ordinal)
    {
#if !DEBUG
        "webgisauthor"
#endif
    };

    private static readonly string[] Sequences = new[]
    {
        "0123456789",
        "abcdefghijklmnopqrstuvwxyz",
        "qwertzuiopüasdfghjklöäyxcvbnm",
        "qwertzuiopasdfghjklyxcvbnm",
        "qwertyuiopasdfghjklzxcvbnm",
        "1qay2wsx3edc4rfv5tgb6zhn7ujm8ik9ol0p",
        "1qaz2wsx3edc4rfv5tgb6yhn7ujm8ik9ol0p"
    }
    .Select(sequence => String.Concat(Enumerable.Repeat(sequence, SubscriberSecurityOptions.PasswordMaxLength / sequence.Length + 2)))
    .ToArray();

    /// <summary>
    /// Small built-in list of very common (german/english) passwords and base words.
    /// Compared with the password without leading/trailing digits/special characters, leetspeak decoded.
    /// </summary>
    private static readonly HashSet<string> BuiltInBlocklist = new(StringComparer.Ordinal)
    {
        "password", "passwort", "kennwort", "passwd", "pass", "geheim", "secret", "geheimnis",
        "admin", "administrator", "root", "user", "benutzer", "gast", "guest", "login", "anmelden", "test", "tester", "demo",
        "welcome", "willkommen", "hallo", "hello", "letmein", "changeme", "default", "start", "startpasswort", "initial",
        "qwerty", "qwertz", "asdf", "asdfgh", "asdfghjkl", "yxcvbnm", "zxcvbnm", "abc", "abcd", "abcdef",
        "iloveyou", "ichliebedich", "schatz", "schatzi", "liebe", "love", "baby", "engel", "sonne", "sunshine",
        "dragon", "monkey", "master", "shadow", "superman", "batman", "princess", "prinzessin", "starwars", "pokemon",
        "football", "fussball", "fußball", "baseball", "soccer", "bayern", "fcbayern", "schalke", "borussia", "dortmund", "rapid", "sturm",
        "sommer", "winter", "fruehling", "frühling", "herbst", "summer", "spring", "autumn",
        "januar", "februar", "maerz", "märz", "april", "mai", "juni", "juli", "august", "september", "oktober", "november", "dezember",
        "january", "february", "march", "june", "july", "october", "december",
        "montag", "dienstag", "mittwoch", "donnerstag", "freitag", "samstag", "sonntag", "monday", "friday",
        "computer", "internet", "server", "service", "system", "office", "firma", "company",
        "webgis", "gis", "karte", "map", "maps", "geodaten", "portal", "api", "subscriber", "intranet",
        "austria", "oesterreich", "österreich", "deutschland", "germany", "graz", "wien", "vienna", "berlin", "muenchen", "münchen",
        "trustno", "whatever", "freedom"
    };

    private static HashSet<string> LoadBlocklist(string file, string contentRoot, ILogger logger)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (String.IsNullOrWhiteSpace(file))
        {
            return result;
        }

        try
        {
            string path = Path.IsPathRooted(file) ? file : Path.Combine(contentRoot, file);
            foreach (var line in File.ReadLines(path))
            {
                string entry = line.Trim();
                if (entry.Length > 0 && !entry.StartsWith('#'))
                {
                    result.Add(entry.ToLowerInvariant());
                }
            }

            logger.LogInformation("Password blocklist loaded: {count} entries from {file}", result.Count, path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Password blocklist file can't be loaded: {file}. Only the built-in list is used.", file);
        }

        return result;
    }

    #endregion
}
