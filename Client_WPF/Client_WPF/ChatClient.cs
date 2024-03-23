using System.Net.Sockets;
using System.Text;

namespace ChatClient
{
    class Client
    {
        private static TcpClient? client;

        public static void Disconnect()
        {
            client?.Close();
            client = null;
        }

        public static async Task<string> MessageReceiverHandler()
        {
            string recMes = string.Empty;
            await Task.Run(() =>
            {
                try
                {
                    if (client != null)
                    {

                        byte[] bytesRead = new byte[256];
                        int length = client.GetStream().Read(bytesRead, 0, bytesRead.Length);
                        recMes = Encoding.UTF8.GetString(bytesRead, 0, length);
                    }
                    else
                    {
                        recMes = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    Disconnect();
                }
            });
            return recMes;
        }

        public static void MessageSenderHandle(string message)
        {
            if (client != null)
            {
                NetworkStream stream = client.GetStream();
                byte[] bytesMsgWrite = Encoding.UTF8.GetBytes(message);
                stream.Write(bytesMsgWrite, 0, bytesMsgWrite.Length);
                stream.Flush();
            }
        }

        public static void Connect(string name)
        {
            try
            {

                client = new TcpClient("127.0.0.1", 1111);
                Console.WriteLine("Client connected");

                NetworkStream stream = client.GetStream();

                byte[] bytesWrite = Encoding.UTF8.GetBytes(name);
                stream.Write(bytesWrite, 0, bytesWrite.Length);
                stream.Flush();

            }
            catch (Exception ex)
            {
                Disconnect();
            }
        }
    }
}