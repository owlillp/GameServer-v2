using System.Security.Cryptography;
using System.Text;

namespace RealtimeService.Core.Messaging;

internal static class NotificationId
{
    public static Guid Create(string key)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }
}
