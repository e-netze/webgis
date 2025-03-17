using E.Standard.WebMapping.Core.Abstraction;
using System.IO;

namespace E.Standard.Esri.Shapefile;

class FieldDescriptor
{
    public FieldDescriptor(BinaryReader br)
    {
        br.Read(fieldName, 0, 11);
        FieldType = br.ReadChar();
        FieldDataAddress = (uint)br.ReadInt32();
        FieldLength = br.ReadByte();
        DecimalCount = br.ReadByte();
        Reserved1 = br.ReadInt16();
        WorkAreaID = br.ReadByte();
        Reserved2 = br.ReadInt16();
        FlagForSET_FIELDS = br.ReadByte();
        Reserved3 = br.ReadByte();
        Reserved4 = br.ReadByte();
        Reserved5 = br.ReadByte();
        Reserved6 = br.ReadByte();
        Reserved7 = br.ReadByte();
        Reserved8 = br.ReadByte();
        Reserved9 = br.ReadByte();
        IndexFieldFlag = br.ReadByte();
    }

    public static bool Write(BinaryWriter bw, IField field)
    {
        if (bw == null || field == null)
        {
            return false;
        }

        byte decimalCount = 0, fieldLength = 0;
        char fieldType = 'C';
        switch (field.Type)
        {
            case WebMapping.Core.FieldType.BigInteger:
                fieldLength = 18;
                fieldType = 'N';
                break;
            case WebMapping.Core.FieldType.Boolean:
                fieldLength = 1;
                fieldType = 'L';
                break;
            case WebMapping.Core.FieldType.Char:
                fieldLength = 1;
                fieldType = 'C';
                break;
            case WebMapping.Core.FieldType.Date:
                fieldLength = 8;
                fieldType = 'D';
                break;
            case WebMapping.Core.FieldType.Double:
                fieldLength = 31;
                decimalCount = 31;
                fieldType = 'F';
                break;
            case WebMapping.Core.FieldType.Float:
                fieldLength = 11;
                fieldType = 'F';
                break;
            case WebMapping.Core.FieldType.ID:
                fieldLength = 9;
                fieldType = 'N';
                break;
            case WebMapping.Core.FieldType.Interger:
                fieldLength = 9;
                fieldType = 'N';
                break;
            case WebMapping.Core.FieldType.Shape:
                return false;

            case WebMapping.Core.FieldType.SmallInteger:
                fieldLength = 6;
                fieldType = 'N';
                break;
            case WebMapping.Core.FieldType.String:
                //fieldLength = (byte)(field.size > 255 ? 255 : field.size);
                fieldLength = 255;
                fieldType = 'C';
                break;
            default:
                //fieldLength = (byte)(field.size > 0 ? field.size : 255);
                fieldLength = 255;
                fieldType = 'C';
                break;
        }

        // fieldName
        for (int i = 0; i < 10; i++)
        {
            if (i < field.Name.Length)
            {
                bw.Write((byte)field.Name[i]);
            }
            else
            {
                bw.Write((byte)0);
            }
        }
        bw.Write((byte)0);

        bw.Write((byte)fieldType);     // FieldType
        bw.Write(0);              // FieldDataAddress
        bw.Write(fieldLength);   // FieldLength
        bw.Write(decimalCount);  // DecimalCount
        bw.Write((short)0);            // Reserved1
        bw.Write((byte)0);             // WorkAreaID
        bw.Write((short)0);            // Reserved2
        bw.Write((byte)0);             // FlagForSET_FIELDS
        bw.Write((byte)0);             // Reserved3
        bw.Write((byte)0);             // Reserved4
        bw.Write((byte)0);             // Reserved5
        bw.Write((byte)0);             // Reserved6
        bw.Write((byte)0);             // Reserved7
        bw.Write((byte)0);             // Reserved8
        bw.Write((byte)0);             // Reserved9
        bw.Write((byte)0);             // IndexFieldFlag

        return true;
    }

    public string FieldName
    {
        get
        {
            char[] trims = { '\0' };
            System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
            return encoder.GetString(fieldName).TrimEnd(trims);
        }
    }
    private byte[] fieldName = new byte[11];
    public char FieldType;
    public uint FieldDataAddress;
    public byte FieldLength;
    public byte DecimalCount;
    public short Reserved1;
    public byte WorkAreaID;
    public short Reserved2;
    public byte FlagForSET_FIELDS;
    public byte Reserved3;
    public byte Reserved4;
    public byte Reserved5;
    public byte Reserved6;
    public byte Reserved7;
    public byte Reserved8;
    public byte Reserved9;
    public byte IndexFieldFlag;
}
