using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using System.Collections.Generic;
using System.IO;

namespace E.Standard.Esri.Shapefile;

class DBFFileHader
{
    public DBFFileHader(BinaryReader br)
    {
        br.BaseStream.Position = 0;

        version = br.ReadByte();
        YY = br.ReadByte();
        MM = br.ReadByte();
        DD = br.ReadByte();
        recordCount = (uint)br.ReadInt32();
        headerLength = br.ReadInt16();
        LegthOfEachRecord = br.ReadInt16();
        Reserved1 = br.ReadInt16();
        IncompleteTransac = br.ReadByte();
        EncryptionFlag = br.ReadByte();
        FreeRecordThread = (uint)br.ReadInt32();
        Reserved2 = br.ReadInt32();
        Reserved3 = br.ReadInt32();
        MDX = br.ReadByte();
        LanguageDriver = br.ReadByte();
        Reserved4 = br.ReadInt16();
    }

    public static bool Write(BinaryWriter bw, List<IField> fields)
    {
        if (bw == null || fields == null)
        {
            return false;
        }

        int c = 0, rl = 1;  // deleted Flag
        foreach (IField field in fields)
        {
            switch (field.Type)
            {
                case FieldType.BigInteger:
                    c++; rl += 18;
                    break;
                case FieldType.Boolean:
                    c++; rl += 1;
                    break;
                case FieldType.Char:
                    c++; rl += 1;
                    break;
                case FieldType.Date:
                    c++; rl += 8;
                    break;
                case FieldType.Double:
                    c++; rl += 31;
                    break;
                case FieldType.Float:
                    c++; rl += 11;
                    break;
                case FieldType.ID:
                    c++; rl += 9;
                    break;
                case FieldType.Interger:
                    c++; rl += 9;
                    break;
                case FieldType.Shape:
                    break;
                case FieldType.SmallInteger:
                    c++; rl += 6;
                    break;
                case FieldType.String:
                    //c++; rl += (field. > 255) ? 255 : field.size;
                    c++; rl += 255;
                    break;
                default:
                    //c++; rl += (field.size <= 0) ? 255 : field.size;
                    c++; rl += 255;
                    break;
            }
        }

        short hLength = (short)(32 * c + 33);

        bw.Write((byte)3);   // Version
        bw.Write((byte)106); // YY
        bw.Write((byte)6);   // MM
        bw.Write((byte)12);  // DD
        bw.Write(0);    // recordCount
        bw.Write(hLength);    // headerLength
        bw.Write((short)rl); // Length of each record
        bw.Write((short)0);  // Reserved1
        bw.Write((byte)0);   // IncompleteTransac
        bw.Write((byte)0);   // EncryptionFlag
        bw.Write(0);    // FreeRecordThread
        bw.Write(0);    // Reserved2
        bw.Write(0);    // Reserved3
        bw.Write((byte)0);   // MDX
        bw.Write((byte)CodePage.UTF_7);
        bw.Write((short)0);  // Reserved4

        foreach (IField field in fields)
        {
            FieldDescriptor.Write(bw, field);
        }

        bw.Write((byte)13); // Terminator 0x0D
        return true;
    }

    public int FieldsCount
    {
        get
        {
            return (headerLength - 1) / 32 - 1;
        }
    }

    public byte version;
    public byte YY, MM, DD;
    public uint recordCount;
    public short headerLength;
    public short LegthOfEachRecord;
    public short Reserved1;
    public byte IncompleteTransac;
    public byte EncryptionFlag;
    public uint FreeRecordThread;
    public int Reserved2;
    public int Reserved3;
    public byte MDX;
    public byte LanguageDriver;
    public short Reserved4;
}
