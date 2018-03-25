using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;


using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;

sealed class PreMergeToMergedDeserializationBinder : SerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        Type typeToDeserialize = null;

        // For each assemblyName/typeName that you want to deserialize to
        // a different type, set typeToDeserialize to the desired type.
        String exeAssembly = Assembly.GetExecutingAssembly().FullName;


        // The following line of code returns the type.
        typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
            typeName, exeAssembly));

        return typeToDeserialize;
    }
}

[Serializable]
public class UserDB
{
    public static int IDCount = 0;

    public int ID;
    public string Name { get; set; }
    public string Password { get; set; }

}

[Serializable]
public class ProductDB
{
    public static int IDCount = 0;

    public int ID;
    public string Name { get; set; }
    public string Location { get; set; }
    public bool ActiveAdd { get; set; }
    public string Price { get; set; }
    public int IDUser { get; set; }

    public string Description { get; set; }
}

// State object for receiving data from remote device.  
public class StateObject
{
    // Client socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 256;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

public class AsynchronousClient
{
    // The port number for the remote device.  
    private const int port = 11000;

    // ManualResetEvent instances signal completion.  
    private static ManualResetEvent connectDone =
        new ManualResetEvent(false);
    private static ManualResetEvent sendDone =
        new ManualResetEvent(false);
    private static ManualResetEvent receiveDone =
        new ManualResetEvent(false);

    // The response from the remote device.  
    private static String response = String.Empty;

    public static List<ProductDB> Products = new List<ProductDB>();

    public static int num;

    public static void SendRequest()
    {
        try
        {
            IPAddress ipAddress = IPAddress.Parse("192.168.0.132");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket client = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            Send(client, "RequestProducts<EOF>");
            sendDone.WaitOne();
            // Receive the response from the remote device.  
            Receive(client);
            receiveDone.WaitOne();

            //first = false;
            //Products = new List<ProductDB>();
            //for (int i = 0; i < num; i++)
            //{
            //    Receive(client);
            //    receiveDone.WaitOne();
            //}
            // Write the response to the console.  
            //Console.WriteLine("Response received : {0}", response);
            Console.Read();



            // Release the socket.  
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static void SendProduct(Socket handler, ProductDB product)
    {
        //var memStream = new MemoryStream();
        //BinaryFormatter formatter = new BinaryFormatter();
        //formatter.Serialize(memStream, product);
        //Console.WriteLine(memStream.ToString());
        //memStream.ToArray();

        XmlSerializer x = new XmlSerializer(typeof(ProductDB));
        var sww = new StringWriter();
        XmlWriter writer = XmlWriter.Create(sww);
        x.Serialize(writer, product);
        string xml = sww.ToString() + "<EOF>";
        Console.WriteLine(xml);


        byte[] byteData = Encoding.ASCII.GetBytes(xml);
        Console.WriteLine(byteData.ToString());

        //Console.Read();
        //Thread.Sleep(1000);

        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    public static void SendAction(ProductDB product)
    {
        try
        {
            IPAddress ipAddress = IPAddress.Parse("192.168.0.132");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket client = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            SendProduct(client, product);
            sendDone.WaitOne();


            // Release the socket.  
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }


    private static void StartClient()
    {
        // Connect to a remote device.  
        try
        {
            IPAddress ipAddress = IPAddress.Parse("192.168.0.132");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket client = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            // Send test data to the remote device.  
            Send(client, "This is a test<EOF>");
            sendDone.WaitOne();

            // Receive the response from the remote device.  
            Receive(client);
            receiveDone.WaitOne();

            // Write the response to the console.  
            Console.WriteLine("Response received : {0}", response);
            Console.Read();
            // Release the socket.  
            client.Shutdown(SocketShutdown.Both);
            client.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static bool asking = false;
    public static void AskServer(string nameID)
    {
        // Connect to a remote device.  
        try
        {
            IPAddress ipAddress = IPAddress.Parse("192.168.0.132");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket client = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            // Send test data to the remote device.  
            asking = true;
            Send(client, "<FACE>" + nameID + "<EOF>");
            sendDone.WaitOne();

            // Receive the response from the remote device.  
            Receive(client);
            receiveDone.WaitOne();

            // Write the response to the console.  
            Console.WriteLine("Response received : {0}", response);
            Console.Read();
            // Release the socket.  
            client.Shutdown(SocketShutdown.Both);
            client.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }


    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            Console.WriteLine("Socket connected to {0}",
                client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static void Receive(Socket client)
    {
        try
        {
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = client;

            //state.buffer
            // Begin receiving the data from the remote device.  
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static Boolean first = true;
    public static string AskResponse = "";

    public static object XmlDeserializeFromString(string objectData, Type type)
    {
        var serializer = new XmlSerializer(type);
        object result;

        using (TextReader reader = new StringReader(objectData))
        {
            result = serializer.Deserialize(reader);
        }

        return result;
    }

    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket   
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.  
            int bytesRead = client.EndReceive(ar);
            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Get the rest of the data.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                // All the data has arrived; put it in response.  
                if (state.sb.Length > 1)
                {
                    {
                        if (asking)
                        {
                            AskResponse = state.sb.ToString();
                        }
                        else
                        {
                            var settings = XmlDeserializeFromString(state.sb.ToString(), typeof(List<ProductDB>));

                            Products = settings as List<ProductDB>;
                            //Console.WriteLine(prr[0].Name);
                            //Console.WriteLine(prr[1].Name);
                            //Console.Read();
                        }
                    }

                }
                // Signal that all bytes have been received.  
                receiveDone.Set();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine(response);
        Console.Read();
    }

    private static void Send(Socket client, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            // Signal that all bytes have been sent.  
            sendDone.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static int Main(String[] args)
    {
        //StartClient();
        //ProductDB prod = new ProductDB();
        //prod.ID = 10;
        //prod.Name = "A10";
        //prod.ActiveAdd = true;

        //SendAction(prod);


        //prod.ID = 20;
        //prod.Name = "A20";
        //prod.ActiveAdd = true;

        //SendAction(prod);


        //prod.ID = 30;
        //prod.Name = "3BAS0";
        //prod.ActiveAdd = true;

        //SendAction(prod);


        //prod.ID = 10;
        //prod.Name = "A10";
        //prod.ActiveAdd = false;
        //SendAction(prod);


        //SendRequest();
        //foreach (var product in Products)
        //{
        //    Console.WriteLine(product.Name);
        //}
        //Console.WriteLine();

        //AskServer("alex");
        //Console.WriteLine(AskResponse);
        //Console.Read();

        //AskServer("alex");
        //Console.WriteLine(AskResponse);
        //Console.Read();

        //AskServer("Dan");
        //Console.WriteLine(AskResponse);
        //Console.Read();

        //AskServer("alex");
        //Console.WriteLine(AskResponse);
        //Console.Read();

        return 0;
    }
}