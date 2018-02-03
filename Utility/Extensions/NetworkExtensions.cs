using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Bee.Eee.Utility.Extensions.NetworkExtensions
{
    public static class NetworkExtensions
    {
        public static bool IsLocalHost(this Uri uri)
        {
            if (null == uri)
                throw new ArgumentNullException("uri");

            if (uri.IsLoopback)
                return true;

            string hostName = Dns.GetHostName();
            //var entry = Dns.GetHostEntry(uri.DnsSafeHost);
            if (uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
            {
                IPAddress address = IPAddress.Parse(uri.Host);                
                var localhosts = Dns.GetHostAddresses(hostName);
                return localhosts.Contains(address);
            }
            else
            {                
                return hostName.Equals(uri.Host, StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }

	public static class IPAddressExtensions
	{
		// http://blogs.msdn.com/b/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx

		public static readonly IPAddress ClassA = IPAddress.Parse("255.0.0.0");
		public static readonly IPAddress ClassB = IPAddress.Parse("255.255.0.0");
		public static readonly IPAddress ClassC = IPAddress.Parse("255.255.255.0");

		public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
		{
			byte[] ipAdressBytes = address.GetAddressBytes();
			byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

			byte[] broadcastAddress = new byte[ipAdressBytes.Length];
			for (int i = 0; i < broadcastAddress.Length; i++)
			{
				broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
			}
			return new IPAddress(broadcastAddress);
		}

		public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
		{
			byte[] ipAdressBytes = address.GetAddressBytes();
			byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

			byte[] broadcastAddress = new byte[ipAdressBytes.Length];
			for (int i = 0; i < broadcastAddress.Length; i++)
			{
				broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
			}
			return new IPAddress(broadcastAddress);
		}

		public static bool IsInSameSubnet(this IPAddress address, IPAddress address2, IPAddress subnetMask)
		{
			IPAddress network1 = address.GetNetworkAddress(subnetMask);
			IPAddress network2 = address2.GetNetworkAddress(subnetMask);

			return network1.Equals(network2);
		}

        /// <summary>
        /// Determine if two addresses are the "same", where sameness is literally equal, or matching
        /// as far as the supplied mask is concerned (e.g., 192.168.100.15 is same as 192.168.255.255)
        /// </summary>
        /// <param name="address">Primary address</param>
        /// <param name="addressOrMask">Comparison address, or address mask</param>
        public static bool IsSameAddress(this IPAddress address, IPAddress addressOrMask)
        {
            try
            {
                if (address.AddressFamily != addressOrMask.AddressFamily)
                    return false;
                // we can use GetNetworkAddress because it does the byte ANDing we are looking for...
                return address.Equals(address.GetNetworkAddress(addressOrMask));
            }

            catch(Exception)
            {
                return false;
            }
        }
	}
}
