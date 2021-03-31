using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Zabbix_Sender {
    public class ZS_Data {
        public string host { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public ZS_Data(string Zbxhost, string Zbxkey, string Zbxval) {
            host = Zbxhost;
            key = Zbxkey;
            value = Zbxval;
        }
    }

    public class ZS_Response {
        public string response { get; set; }
        public string info { get; set; }

    }

    public class ZS_Request {
        public string request { get; set; }
        public ZS_Data[] data { get; set; }
        public ZS_Request(string ZbxHost, string ZbxKey, string ZbxVal) {
            request = "sender data";
            data = new ZS_Data[] { new ZS_Data(ZbxHost, ZbxKey, ZbxVal) };
        }


        public ZS_Response Send(string ZbxServer, int ZbxPort = 10051, int ZbxTimeOut = 500) {
            string jr = JsonConvert.SerializeObject(new ZS_Request(data[0].host, data[0].key, data[0].value));
            using (TcpClient lTCPc = new TcpClient(ZbxServer, ZbxPort))
            using (NetworkStream lStream = lTCPc.GetStream()) {
                byte[] Header = Encoding.ASCII.GetBytes("ZBXD\x01");
                byte[] DataLen = BitConverter.GetBytes((long)jr.Length);
                byte[] Content = Encoding.ASCII.GetBytes(jr);
                byte[] Message = new byte[Header.Length + DataLen.Length + Content.Length];
                Buffer.BlockCopy(Header, 0, Message, 0, Header.Length);
                Buffer.BlockCopy(DataLen, 0, Message, Header.Length, DataLen.Length);
                Buffer.BlockCopy(Content, 0, Message, Header.Length + DataLen.Length, Content.Length);

                lStream.Write(Message, 0, Message.Length);
                lStream.Flush();
                int counter = 0;
                while (!lStream.DataAvailable) {
                    if (counter < ZbxTimeOut / 50) {
                        counter++;
                        Thread.Sleep(50);
                    } else {
                        throw new TimeoutException();
                    }
                }

                byte[] resbytes = new Byte[1024];
                lStream.Read(resbytes, 0, resbytes.Length);
                string s = Encoding.UTF8.GetString(resbytes);
                string jsonRes = s.Substring(s.IndexOf('{'));
                return JsonConvert.DeserializeObject<ZS_Response>(jsonRes);
            }
        }
    }
}
