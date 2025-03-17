using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run <path-to-directory>");
            return;
        }

        string directoryPath = args[0];

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Error: Directory '{directoryPath}' does not exist.");
            return;
        }

        Console.WriteLine($"Scanning directory: {directoryPath}\n");

        // Get all .cs files recursively
        var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (file.EndsWith("program.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!file.EndsWith("editenvironment.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ProcessFile(file);
        }

        Console.WriteLine("Processing complete.");
    }

    static void ProcessFile(string filePath)
    {
        string originalContent = File.ReadAllText(filePath);
        string updatedContent = originalContent;

        // Apply Regex replacements


        updatedContent = Regex.Replace(updatedContent,
            @"\[JsonIgnore\]",
            "[JsonIgnore]\n[System.Text.Json.Serialization.JsonIgnore]");

        updatedContent = Regex.Replace(updatedContent,
            @"\[JsonProperty\(\""(.*?)\"", NullValueHandling = NullValueHandling.Ignore\)\]",
            "[JsonProperty(\"$1\", NullValueHandling = NullValueHandling.Ignore)]\r\n[System.Text.Json.Serialization.JsonPropertyName(\"$1\")]\r\n[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]");

        updatedContent = Regex.Replace(updatedContent,
            @"\[JsonProperty\(PropertyName = \""(.*?)\"", NullValueHandling = NullValueHandling.Ignore\)\]",
            "[JsonProperty(PropertyName = \"$1\", NullValueHandling = NullValueHandling.Ignore)]\r\n[System.Text.Json.Serialization.JsonPropertyName(\"$1\")]\r\n[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]");


        updatedContent = Regex.Replace(updatedContent,
            @"\[JsonProperty\(NullValueHandling = NullValueHandling.Ignore\)\]",
            "[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]\r\n[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]");

        updatedContent = Regex.Replace(updatedContent,
            @"\[JsonProperty\(\""(.*?)\""\)\]",
            "[JsonProperty(\"$1\")]\r\n[System.Text.Json.Serialization.JsonPropertyName(\"$1\")]");

        updatedContent = Regex.Replace(updatedContent,
            @"\[JsonProperty\(PropertyName = \""(.*?)\""\)\]",
            "[JsonProperty(PropertyName = \"$1\")]\r\n[System.Text.Json.Serialization.JsonPropertyName(\"$1\")]");


        // Write the file only if changes were made
        if (originalContent != updatedContent)
        {
            File.WriteAllText(filePath, updatedContent);
            Console.WriteLine($"Updated: {filePath}");
        }
        else
        {
            //Console.WriteLine($"No changes: {filePath}");
        }
    }
}
