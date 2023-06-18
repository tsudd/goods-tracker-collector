namespace GoodsTracker.DataCollector.Common.Adapters.Helpers;

using System.Security.Cryptography;
using System.Text;

internal static class HashCodeGenerator
{
    private static readonly SHA256 hasher = SHA256.Create();

    public static uint GetHashCode(string s)
    {
        byte[] encoded = hasher.ComputeHash(Encoding.UTF8.GetBytes(s));

        return BitConverter.ToUInt32(encoded, 0) % 1000000;
    }
}
