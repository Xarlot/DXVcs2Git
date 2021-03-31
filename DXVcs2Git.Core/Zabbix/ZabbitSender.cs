using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace DXVcs2Git.Core.Zabbix {
    public class ZsData {
        public string Host { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public ZsData(string zbxhost, string zbxkey, string zbxval) {
            Host = zbxhost;
            Key = zbxkey;
            Value = zbxval;
        }
    }

    public class ZsResponse {
        public string Response { get; set; }
        public string Info { get; set; }
    }

    public class ZsRequest {
        public string Request { get; set; }
        public ZsData[] Data { get; set; }
        public ZsRequest(string zbxHost, string zbxKey, string zbxVal) {
            Request = "sender data";
            Data = new ZsData[] {new ZsData(zbxHost, zbxKey, zbxVal)};
        }

        public ZsResponse Send(string zbxServer, int zbxPort = 10051, int zbxTimeOut = 5000) {
            var jr = JsonConvert.SerializeObject(new ZsRequest(Data[0].Host, Data[0].Key, Data[0].Value));
            using var lTcPc = new TcpClient(zbxServer, zbxPort);
            using var lStream = lTcPc.GetStream();
            var header = Encoding.ASCII.GetBytes("ZBXD\x01");
            var dataLen = BitConverter.GetBytes((long)jr.Length);
            var content = Encoding.ASCII.GetBytes(jr);
            var message = new byte[header.Length + dataLen.Length + content.Length];
            Buffer.BlockCopy(header, 0, message, 0, header.Length);
            Buffer.BlockCopy(dataLen, 0, message, header.Length, dataLen.Length);
            Buffer.BlockCopy(content, 0, message, header.Length + dataLen.Length, content.Length);
            lStream.Write(message, 0, message.Length);
            lStream.Flush();
            var counter = 0;
            while (!lStream.DataAvailable)
                if (counter < zbxTimeOut / 50) {
                    counter++;
                    Thread.Sleep(50);
                }
                else {
                    throw new TimeoutException();
                }

            var resbytes = new byte[1024];
            lStream.Read(resbytes, 0, resbytes.Length);
            var s = Encoding.UTF8.GetString(resbytes);
            var jsonRes = s.Substring(s.IndexOf('{'));
            return JsonConvert.DeserializeObject<ZsResponse>(jsonRes);
        }
    }
}
