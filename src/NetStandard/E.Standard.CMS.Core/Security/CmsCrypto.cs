using System;
using System.IO;
using System.Security.Cryptography;

namespace E.Standard.CMS.Core.Security;

public class CmsCryptoHelper
{
    public static byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV)
    {

        MemoryStream ms = new MemoryStream();

        using var alg = Aes.Create();

        alg.Key = Key;
        alg.IV = IV;

        CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(clearData, 0, clearData.Length);
        cs.Close();

        byte[] encryptedData = ms.ToArray();

        return encryptedData;
    }

    public static string Encrypt(string clearText, string Password)
    {
        if (string.IsNullOrEmpty(clearText))
        {
            return string.Empty;
        }

        // First we need to turn the input string into a byte array. 

        byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(clearText);



        // Then, we need to turn the password into Key and IV 

        // We are using salt to make it harder to guess our key using a dictionary attack - 

        // trying to guess a password by enumerating all possible words. 

        PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,

                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4e, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });



        // Now get the key/IV and do the encryption using the function that accepts byte arrays. 

        // Using PasswordDeriveBytes object we are first getting 32 bytes for the Key 

        // (the default Rijndael key length is 256bit = 32bytes) and then 16 bytes for the IV. 

        // IV should always be the block size, which is by default 16 bytes (128 bit) for Rijndael. 

        // If you are using DES/TripleDES/RC2 the block size is 8 bytes and so should be the IV size. 

        // You can also read KeySize/BlockSize properties off the algorithm to find out the sizes. 

        byte[] encryptedData = Encrypt(clearBytes, pdb.GetBytes(32), pdb.GetBytes(16));



        // Now we need to turn the resulting byte array into a string. 

        // A common mistake would be to use an Encoding class for that. It does not work 

        // because not all byte values can be represented by characters. 

        // We are going to be using Base64 encoding that is designed exactly for what we are 

        // trying to do. 

        return Convert.ToBase64String(encryptedData);



    }

    public static byte[] Encrypt(byte[] clearData, string Password)
    {

        // We need to turn the password into Key and IV. 

        // We are using salt to make it harder to guess our key using a dictionary attack - 

        // trying to guess a password by enumerating all possible words. 

        PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,

                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4e, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });



        // Now get the key/IV and do the encryption using the function that accepts byte arrays. 

        // Using PasswordDeriveBytes object we are first getting 32 bytes for the Key 

        // (the default Rijndael key length is 256bit = 32bytes) and then 16 bytes for the IV. 

        // IV should always be the block size, which is by default 16 bytes (128 bit) for Rijndael. 

        // If you are using DES/TripleDES/RC2 the block size is 8 bytes and so should be the IV size. 

        // You can also read KeySize/BlockSize properties off the algorithm to find out the sizes. 

        return Encrypt(clearData, pdb.GetBytes(32), pdb.GetBytes(16));



    }

    public static void Encrypt(string fileIn, string fileOut, string Password)
    {

        // First we are going to open the file streams 

        FileStream fsIn = new FileStream(fileIn, FileMode.Open, FileAccess.Read);

        FileStream fsOut = new FileStream(fileOut, FileMode.OpenOrCreate, FileAccess.Write);



        // Then we are going to derive a Key and an IV from the Password and create an algorithm 

        PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,

                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4e, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });



        using var alg = Aes.Create();

        alg.Key = pdb.GetBytes(32);
        alg.IV = pdb.GetBytes(16);

        // Now create a crypto stream through which we are going to be pumping data. 

        // Our fileOut is going to be receiving the encrypted bytes. 

        CryptoStream cs = new CryptoStream(fsOut, alg.CreateEncryptor(), CryptoStreamMode.Write);



        // Now will will initialize a buffer and will be processing the input file in chunks. 

        // This is done to avoid reading the whole file (which can be huge) into memory. 

        int bufferLen = 4096;

        byte[] buffer = new byte[bufferLen];

        int bytesRead;



        do
        {

            // read a chunk of data from the input file 

            bytesRead = fsIn.Read(buffer, 0, bufferLen);



            // encrypt it 

            cs.Write(buffer, 0, bytesRead);



        } while (bytesRead != 0);



        // close everything 

        cs.Close(); // this will also close the unrelying fsOut stream 

        fsIn.Close();

    }

    public static byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV)
    {
        MemoryStream ms = new MemoryStream();

        using var alg = Aes.Create();

        alg.Key = Key;
        alg.IV = IV;

        CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(cipherData, 0, cipherData.Length);
        cs.Close();

        byte[] decryptedData = ms.ToArray();

        return decryptedData;
    }

    public static string Decrypt(string cipherText, string Password)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        byte[] cipherBytes = Convert.FromBase64String(cipherText);

        PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4e, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });


        byte[] decryptedData = Decrypt(cipherBytes, pdb.GetBytes(32), pdb.GetBytes(16));

        return System.Text.Encoding.Unicode.GetString(decryptedData);
    }

    public static byte[] Decrypt(byte[] cipherData, string Password)
    {
        PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4e, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

        return Decrypt(cipherData, pdb.GetBytes(32), pdb.GetBytes(16));
    }

    public static void Decrypt(string fileIn, string fileOut, string Password)
    {
        FileStream fsIn = new FileStream(fileIn, FileMode.Open, FileAccess.Read);
        FileStream fsOut = new FileStream(fileOut, FileMode.OpenOrCreate, FileAccess.Write);

        PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                                new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4e, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

        using var alg = Aes.Create();
        alg.Key = pdb.GetBytes(32);
        alg.IV = pdb.GetBytes(16);

        CryptoStream cs = new CryptoStream(fsOut, alg.CreateDecryptor(), CryptoStreamMode.Write);

        int bufferLen = 4096;
        byte[] buffer = new byte[bufferLen];
        int bytesRead;

        do
        {
            bytesRead = fsIn.Read(buffer, 0, bufferLen);
            cs.Write(buffer, 0, bytesRead);
        } while (bytesRead != 0);

        cs.Close();
        fsIn.Close();

    }
}
