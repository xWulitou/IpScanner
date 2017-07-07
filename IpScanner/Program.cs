using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

namespace IpScanner
{
    class Program
    {
        static void Main(string[] args)
        {

            ThreadPool.SetMinThreads(100, 100);

            var adapters = NetworkInterface.GetAllNetworkInterfaces().Where(
                p => p.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                p.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
            ).Where(p => p.OperationalStatus == OperationalStatus.Up);


            foreach (var adapter in adapters)
            {
                var pros = adapter.GetIPProperties().UnicastAddresses.Where(p=>p.SuffixOrigin!=SuffixOrigin.LinkLayerAddress);
                
                
                foreach(var p in pros)
                {
                   var ips= GetReachableIps(p.Address, p.IPv4Mask);
                    foreach(var ip in ips)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback((z) =>
                        {
                            var ping = new Ping();
                            if (ping.Send(ip).Status == IPStatus.Success)
                            {
                                Console.WriteLine("OK :::" + ip);
                            }
                        }));
                        
                    }
                }
            }
            Console.ReadKey();

        }


        private static string[] GetReachableIps(IPAddress ip, IPAddress mask)
        {
            var ipbytes = ip.GetAddressBytes();
            var mskbytes = mask.GetAddressBytes();
            var p = BytesAnd(ipbytes, mskbytes);
            var max = BytesNot(mskbytes);
            var min = p;
            var maxip = Hex2Int(BytesOr(p, max));
            var minip = Hex2Int(BytesOr(p, min));

            var strs = new List<string>();
            
            for(int i = minip; i <= maxip; i++)
            {
                strs.Add(Int2Ip(i));
            }


            return strs.ToArray();
        }


        private static byte[] BytesAnd(byte[] p1,byte[] p2)
        {
            if (p1.Length != p2.Length) return null;
            var len = p1.Length;
            var results = new byte[len];

            for(int i = 0; i < len; i++)
            {
                results[i] = (byte)(p1[i] & p2[i]);
            }
            return results;
        }

        private static byte[] BytesOr(byte[] p1, byte[] p2)
        {
            if (p1.Length != p2.Length) return null;
            var len = p1.Length;
            var results = new byte[len];

            for (int i = 0; i < len; i++)
            {
                results[i] = (byte)(p1[i] | p2[i]);
            }
            return results;
        }

        private static byte[] BytesNot(byte[] p1)
        {
            var len = p1.Length;
            var results = new byte[len];

            for (int i = 0; i < len; i++)
            {
                results[i] = (byte)(~p1[i]);
            }
            return results;
        }

        private static int Hex2Int(byte[] bytes)
        {
            int sum = 0;
            for(int i = 0; i < bytes.Length; i++)
            {
                sum = sum <<8 | bytes[i];
            }
            return sum;
        }

        private static string Int2Ip(int data)
        {
            var p1 = (data & 0xFF000000 )>> 24;

            var p2 =(data & 0x00FF0000) >> 16;
            var p3 =(data & 0x0000FF00) >> 8;
            var p4 =(data & 0x000000FF);
            return string.Format("{0}.{1}.{2}.{3}", p1, p2, p3, p4);

        }
    }
}
