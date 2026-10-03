using System.Net;
using System.Net.Sockets;
using EzDinner.Application.Commands.RecipeSnapshots;

namespace EzDinner.Infrastructure.RecipeSnapshots;

public interface IRecipeHostResolver
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken);
}

public sealed class PublicRecipeHostResolver : IRecipeHostResolver
{
    public async Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        EnsurePublic(addresses);
        return addresses;
    }

    public static void EnsurePublic(IPAddress[] addresses)
    {
        if (addresses.Length == 0 || addresses.Any(address => !IsPublic(address)))
            throw new RecipeImportException("RECIPE_UNSAFE_URL");
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) return IsPublic(address.MapToIPv4());
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if ((bytes[0] & 0xe0) != 0x20) return false;
            if (bytes[0] == 0x3f && bytes[1] == 0xff && (bytes[2] & 0xf0) == 0) return false;
            if (bytes[0] == 0x20 && bytes[1] == 0x02) return false;
            if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] < 0x02) return false;
            return !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8);
        }
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;
        if (bytes[0] is 0 or 10 or 127 || bytes[0] >= 224) return false;
        if (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) return false;
        if (bytes[0] == 169 && bytes[1] == 254) return false;
        if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) return false;
        if (bytes[0] == 192 && bytes[1] == 168) return false;
        if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] is 0 or 2) return false;
        if (bytes[0] == 192 && bytes[1] == 88 && bytes[2] == 99) return false;
        if (bytes[0] == 198 && bytes[1] is 18 or 19) return false;
        if (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) return false;
        return !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
    }

    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var resolver = new PublicRecipeHostResolver();
        var addresses = await resolver.ResolveAsync(context.DnsEndPoint.Host, cancellationToken);
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
            if (socket.RemoteEndPoint is not IPEndPoint remote || !IsPublic(remote.Address))
                throw new RecipeImportException("RECIPE_UNSAFE_URL");
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
