//
//  webgis.keygen.exe [-o,--organisation my-organisation-name]
//

using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Services;

string organisation = String.Empty;

if (args != null)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        switch (args[i])
        {
            case "-o":
            case "--organisation":
                organisation = args[i + 1];
                break;
        }
    }
}

if (String.IsNullOrEmpty(organisation))
{
    Console.Write("Organisation: ");
    organisation = Console.ReadLine();
}

if (String.IsNullOrEmpty(organisation))
{
    Console.WriteLine("No organisation entered. Goodby...");
    return;
}

var targetFileInfo = new FileInfo($"{Environment.CurrentDirectory}/{organisation}/keys.config");

if (targetFileInfo.Exists)
{
    Console.WriteLine($"keys.config already exists in folder {organisation}");

    var fileConent = File.ReadAllText(targetFileInfo.FullName);

    if (fileConent.StartsWith("enc::"))
    {
        Console.WriteLine("keys.config is already encrypted");
    }
    else
    {
        Console.WriteLine("encrypting keys.config");
        File.WriteAllText(targetFileInfo.FullName, CryptoImpl.PseudoEncryptString(fileConent));
    }

}

var cryptoOptions = new CryptoServiceOptions();
cryptoOptions.LoadOrCreate(new string[] { targetFileInfo.Directory.FullName }, typeof(CustomPasswords));


Console.WriteLine($"{targetFileInfo.FullName}:");
Console.WriteLine(CryptoImpl.PseudoDecryptString(File.ReadAllText(targetFileInfo.FullName)));
Console.WriteLine("--------------------------------------------------------------------------------------");
Console.WriteLine(File.ReadAllText(targetFileInfo.FullName));
