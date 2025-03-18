using System.Collections.Concurrent;

namespace E.Standard.Localization.Models;
internal class Translations : ConcurrentDictionary<string, (string Header, string Body)>
{
}
