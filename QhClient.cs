using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace qubic_miner_helper
{
    class QhClient
    {
        private TcpClient socketConnection;
        private Thread clientReceiveThread;

        public void Start()
        {
            ConnectToTcpServer();
        }

        private void ConnectToTcpServer()
        {
            try
            {
                clientReceiveThread = new Thread(new ThreadStart(ListenForData));
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Client connect exception: " + e);
            }
        }

        private void ListenForData()
        {
            try
            {
                
                socketConnection = new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6363));
                Byte[] bytes = new Byte[1024];
                while (true)
                {
                    // Get a stream object for reading 				
                    using (NetworkStream stream = socketConnection.GetStream())
                    {
                        int length;
                        // Read incomming stream into byte arrary. 					
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            // Convert byte array to string message. 						
                            string serverMessage = Encoding.ASCII.GetString(incommingData);
                            Console.WriteLine("server message received as: " + serverMessage);
                        }
                    }
                }
            }
            catch (SocketException socketException)
            {
                Console.WriteLine("Socket exception: " + socketException);
            }
        }


        public void SendData(MachineState machineState)
        {
            if (socketConnection == null)
            {
                Console.WriteLine("Got no socket connection");
                return;
            }
            try
            {
                // Get a stream object for writing. 			
                NetworkStream stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    var sendData = JsonConvert.SerializeObject(machineState);
                    var sendDataBytes = Encoding.UTF8.GetBytes(sendData);
                    var dataLength = Encoding.UTF8.GetByteCount(sendData);
                    
                    stream.Write(sendDataBytes, 0, dataLength);
                    Console.WriteLine("Data sent");
                }
            }
            catch (SocketException socketException)
            {
                Console.WriteLine("Socket exception: " + socketException);
            }
        }

        private void SendMessage()
        {
            if (socketConnection == null)
            {
                return;
            }
            try
            {
                // Get a stream object for writing. 			
                NetworkStream stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    string clientMessage = "This is a message from one of your clients.";
                    // Convert string message to byte array.                 
                    byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
                    // Write byte array to socketConnection stream.                 
                    stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                    Console.WriteLine("Client sent his message - should be received by server");
                }
            }
            catch (SocketException socketException)
            {
                Console.WriteLine("Socket exception: " + socketException);
            }
        }

    }
}
