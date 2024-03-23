using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace ChatServer
{
    class Server
    {
        private const int MAX_CONNECTIONS = 100;
        private static int con_Counter = 0;

        private static List<TcpClient> connections_List = new List<TcpClient>();
        private static Dictionary<int, string> indexConName_Dict = new Dictionary<int, string>();
        private static List<int> indexToChange_List = new List<int>();


        enum clientConnection_Type
        {
            cConnected = 1,
            cDisconnected = 2
        }

        private static void Client_ConAnnounce(int index, clientConnection_Type cCType)
        {
            string toWrite_str = string.Empty;
            switch (cCType)
            {
                case clientConnection_Type.cConnected:
                    toWrite_str = $"Client {indexConName_Dict[index]} connected to the chat.";
                    break;
                case clientConnection_Type.cDisconnected:
                    toWrite_str = $"Client {indexConName_Dict[index]} has left the chat.";
                    break;
            }

            for (int i = 0; i < con_Counter; i++)
            {
                if (i == index)
                {
                    continue;
                }
                Client_StreamWrite(connections_List[i], toWrite_str);
            }
        }

        private static string? Client_StreamRead(TcpClient connection)
        {
            NetworkStream stream = connection.GetStream();

            byte[] bytesRead = new byte[256];
            int length = stream.Read(bytesRead, 0, bytesRead.Length);
            string recMes_str = Encoding.UTF8.GetString(bytesRead, 0, length);

            if (recMes_str == string.Empty
                || recMes_str == ""
                || recMes_str == " ")
            {
                recMes_str = string.Empty;
            }
            return recMes_str;
        }

        private static void Client_StreamWrite(TcpClient connection, string toWrite_str)
        {
            NetworkStream stream = connection.GetStream();

            byte[] bytesWrite = Encoding.UTF8.GetBytes(toWrite_str);
            stream.Write(bytesWrite, 0, bytesWrite.Length);
            stream.Flush();
        }

        private static void Disconnect(object index)
        {
            Client_ConAnnounce(index: (int)index, cCType: clientConnection_Type.cDisconnected);

            indexConName_Dict.Remove((int)index);
            connections_List[(int)index].Close();
            connections_List.RemoveAt((int)index);

            con_Counter = con_Counter - 1;

            Dictionary<int, string> tempDict = new Dictionary<int, string>();
            for (int i = 0; i < indexConName_Dict.Count; i++)
            {
                if (i >= (int)index)
                {
                    tempDict.Add(i, indexConName_Dict[i + 1]);
                }
                else
                {
                    tempDict.Add(i, indexConName_Dict[i]);
                }
            }
            indexConName_Dict = tempDict;

            if ((int)index != con_Counter)
            {
                indexToChange_List.Clear();

                for (int i = (int)index + 1; i <= con_Counter; i++)
                {
                    indexToChange_List.Add(i);
                }
            }


            Console.WriteLine("Current connections: ");
            foreach (var item in indexConName_Dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }

        }

        private static bool ConNameExists(string name)
        {
            if (!indexConName_Dict.ContainsValue(name))
            {
                return false;
            }
            return true;
        }

        private static void ClientHandler(object? index)
        {
            if (index == null || (int)index >= con_Counter)
            {
                return;
            }

            bool indexChanged = false;
            try
            {
                while (true)
                {
                    if (indexToChange_List.Contains((int)index) && !indexChanged)
                    {
                        indexToChange_List.Remove((int)index);
                        index = (int)index - 1;

                        indexChanged = true;
                        Console.WriteLine((int)index + 1 +
                            " changed to "
                            + (int)index);
                    }

                    string recMes_str = (DateTime.Now.ToString() + " "
                        + indexConName_Dict[(int)index] + ": "
                        + (Client_StreamRead(connections_List[(int)index]) ?? "empty message"));

                    Console.WriteLine(recMes_str);

                    for (int i = 0; i < con_Counter; i++)
                    {
                        Client_StreamWrite(connections_List[i], recMes_str);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + $"\n\tClient {index} disconnected.");
                indexChanged = false;
                Disconnect(index);
            }
        }

        private static void RequestHandler(object? index)
        {
            try
            {
                if (index == null || (int)index >= con_Counter)
                {
                    return;
                }

                Console.WriteLine($"Client №{index} sent connection request.");

                /*получение потока для отправки/получения
                * информации с клиента*/

                Console.WriteLine($"\tWaiting for client's №{index} nickname.");

                string? recName_Mes = Client_StreamRead(connections_List[(int)index]);

                if (recName_Mes == null)
                {
                    return;
                }

                Console.WriteLine($"\tClient №{index} wants to take {recName_Mes} name.");

                switch (ConNameExists(recName_Mes))
                {
                    case true:
                        Client_StreamWrite(connections_List[(int)index], "Nickname is already taken, please choose another.");

                        RequestHandler(index);
                        break;

                    case false:
                        indexConName_Dict.Add((int)index, recName_Mes);

                        Console.WriteLine($"\tClient №{index} with {recName_Mes} connected successfully.");
                        Client_ConAnnounce(index: (int)index, cCType: clientConnection_Type.cConnected);

                        Thread chatThread = new(start: ClientHandler);
                        chatThread.Start(index);

                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                TcpListener serverSocket = new TcpListener(IPAddress.Any, 1111);
                Console.WriteLine("Server started.");
                serverSocket.Start();

                while (con_Counter < MAX_CONNECTIONS)
                {
                    connections_List.Add(serverSocket.AcceptTcpClient());
                    if (connections_List[con_Counter].Connected == false)
                    {
                        Console.WriteLine($"Client {con_Counter} couldn't connect to the server.");
                        return;
                    }
                    else
                    {
                        Thread conThread = new(start: RequestHandler);
                        conThread.Start(con_Counter);
                        con_Counter++;
                    }
                }

                serverSocket.Stop();
                Console.WriteLine("Server stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}