using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KSIS4
{
	class Program
	{
		static void Main(string[] args)
		{
			var proxy = new Proxy();
			proxy.Start(10);
		}
	}
}
