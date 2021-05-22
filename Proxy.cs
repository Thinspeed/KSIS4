using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KSIS4
{
	class Proxy
	{
		BlackList blackList;
		IPAddress ipAdress;
		IPEndPoint ipEndPoint;
		Socket socket;
		FileInfo log;

		public Proxy()
		{
			blackList = new BlackList("config.cfg");
			log = new FileInfo("log.txt");
			ipAdress = IPAddress.Parse("127.0.0.1");
			ipEndPoint = new IPEndPoint(ipAdress, 5879);
			socket = new Socket(ipAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(ipEndPoint);
		}

		public void Start(int maxThreads)
		{
			while (true)
			{
				socket.Listen();
				var handler = socket.Accept();
				//var func = new Action<Socket>(GetData);
				//func.BeginInvoke(handler, null, null);

				var socketThread = new Thread(() => GetData(handler));
				socketThread.Start();
			}
		}

		void GetData(Socket handler)
		{
			byte[] buffer = new byte[8192];
			int nuberOfBytes = handler.Receive(buffer);
			if (nuberOfBytes != 0)
			{
				SendByHttp(handler, buffer, nuberOfBytes);
			}
		}

		void SendByHttp(Socket handler, byte[] buffer, int dataLength)
		{
			try
			{
				var myString = Encoding.ASCII.GetString(buffer);
				string[] data = Encoding.ASCII.GetString(buffer).Split(new char[] { '\r', '\n' });
				string host = data.First(x => x.Contains("Host"));
				host = host.Substring(host.IndexOf(":") + 2);
				string[] port = host.Trim().Split(new char[] { ':' });

				int startIndex = data[0].IndexOf("http://");
				int ind = data[0].IndexOf('/', 12);
				if (data[0].Remove(startIndex, ind - startIndex) != "GET / HTTP/1.1")
                {
					data[0] = data[0].Remove(startIndex, ind - startIndex);
				}

				StringBuilder updatedData = new StringBuilder();
				foreach (var s in data)
				{
					updatedData.Append(s + "\r\n");
				}

				buffer = Encoding.ASCII.GetBytes(updatedData.ToString());
				if (blackList.Contains(port[0]))
				{
					Console.WriteLine("Этот сайт находится в чёрном списке");
					return;
				}

				var siteIpAdress = Dns.GetHostEntry(port[0]).AddressList[0];
				IPEndPoint ipEndPoint = new IPEndPoint(siteIpAdress, port.Length == 2 ? int.Parse(port[1]) : 80);

				Socket sender = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				sender.Connect(ipEndPoint);
				sender.Send(buffer[0..dataLength]);

				var responseHeader = new byte[32];
				sender.Receive(responseHeader, responseHeader.Length, SocketFlags.None);
				handler.Send(responseHeader, responseHeader.Length, SocketFlags.None);

				string[] head = Encoding.UTF8.GetString(responseHeader).Split(new char[] { '\r', '\n' });
				string ResponseCode = head[0].Substring(head[0].IndexOf(" ") + 1);
				Console.WriteLine($"\n{host} {ResponseCode}");
				WriteLog($"\n{host} {ResponseCode}");
				while (true)
				{
					byte[] otherData = new byte[8192];
					EndPoint endPoint = (EndPoint)ipEndPoint;
					int numberOfBytes = sender.Receive(otherData);
					if (numberOfBytes == 0)
					{
						return;
					}

					handler.Send(otherData, numberOfBytes, SocketFlags.None);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				WriteLog(e.Message);
				return;
			}
		}

		void WriteLog(string s)
		{
			lock (log)
			{
				using (var stream = log.OpenWrite())
				{
					byte[] buffer = Encoding.UTF8.GetBytes(s);
					stream.Write(buffer);
				}
			}
		}
	}
}
