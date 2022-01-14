using System.Text;

namespace arduino_ota_updater;

public static class ByteExtensions
{
    //from https://stackoverflow.com/a/2435734/779192
    public static string ToHex(this byte[] bytes, bool upperCase = false)
    {
        StringBuilder result = new StringBuilder(bytes.Length*2);

        for (int i = 0; i < bytes.Length; i++)
            result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

        return result.ToString();
    }
}
