using System.Runtime.InteropServices;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customise this process see: https://aka.ms/assembly-info-properties


// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid("cf9527ff-62b7-4638-a7a9-4d28ff33d422")]
public static class PLCTagTypeExtensions
{
    public static double GetMaxValue(this int type)
    {
        switch (type)
        {
            case DataType_Int16:     // short
                return short.MaxValue;          // 32767

            case DataType_Int32:    // DWord / Int32 / Word
                                     // Choose ONE depending on your protocol meaning:
                return uint.MaxValue;           // 4294967295 (DWord)
                                                // return int.MaxValue;         // 2147483647 (Int32)

            case DataType_FP:        // float / real
                return float.MaxValue;          // 3.4028235E38

            case DataType_UInt16:    // ushort
                return ushort.MaxValue;         // 65535

            case DataType_UInt32:    // uint
                return uint.MaxValue;           // 4294967295

            default:
                return short.MaxValue;
        }
    }

    public static double GetMinValue(this int type)
    {
        switch (type)
        {
            case DataType_Int16:     // short
                return short.MinValue;          // 32767

            case DataType_Int32:    // DWord / Int32 / Word
                                     // Choose ONE depending on your protocol meaning:
                return uint.MinValue;           // 4294967295 (DWord)
                                                // return int.MaxValue;         // 2147483647 (Int32)

            case DataType_FP:        // float / real
                return float.MinValue;          // 3.4028235E38

            case DataType_UInt16:    // ushort
                return ushort.MinValue;         // 65535

            case DataType_UInt32:    // uint
                return uint.MinValue;           // 4294967295

            default:
                return short.MinValue;
        }
    }
    public static string GetDataTypeString(int typeId)
    {
        return typeId switch
        {
            1 => "Int16",
            2 => "Word",
            3 => "Bit",
            4 => "Float",
            5 => "String",
            6 => "UInt16",
            7 => "UInt32",
            _ => "Unknown"
        };
    }

    public const int DataType_Int16 = 1;
    public const int DataType_Int32 = 2; // DWord, Int32, Word
    public const int DataType_Bit = 3;
    public const int DataType_FP = 4; // Real, Float
    public const int DataType_String = 5;

    public const int DataType_UInt16 = 6;
    public const int DataType_UInt32 = 7;

    public const int AlgoNo_Raw = 0;
    public const int AlgoNo_LinearScale = 1;

    

}
