using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;

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

// State object for reading client data asynchronously  
public class StateObject
{
    // Client  socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

public class AsynchronousSocketListener
{
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);

    public static List<UserDB> Users = new List<UserDB>();
    public static List<ProductDB> Products = new List<ProductDB>();

    public AsynchronousSocketListener()
    {
    }

    public static void StartListening()
    {
        // Data buffer for incoming data.  
        byte[] bytes = new Byte[1024];

        IPAddress ipAddress = IPAddress.Parse("192.168.0.132");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);

            while (true)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\nPress ENTER to continue...");
        Console.Read();
    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        allDone.Set();

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    public static Dictionary<string, DateTime> dic = new Dictionary<string, DateTime>();


    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.   
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There  might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read   
            // more data.  
            content = state.sb.ToString();
            if (content.IndexOf("<EOF>") > -1)
            {
                // All the data has been read from the   
                // client. Display it on the console.  
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content);
                // Echo the data back to the client.  

                if (content.Contains("<FACE>"))
                {
                    if (content.Length > 7 && content[6] != '<')
                    {
                        DateTime current = DateTime.Now;
                        bool inited = false;
                        DateTime last = DateTime.Now;
                        if (dic.ContainsKey(content))
                        {
                            last = dic[content];
                            inited = true;
                        }
                        //dic[content] = current;
                        if (inited)
                        {
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine(current.Subtract(last).TotalMinutes);
                            Console.WriteLine((current - last).TotalMinutes);
                            Console.WriteLine();
                            Console.WriteLine();
                            if ((current - last).Seconds > 10)
                            {
                                SendValidity(handler, "Valid");
                                Console.WriteLine("Valid");
                                dic[content] = current;
                            }
                            else
                            {
                                SendValidity(handler, "Invalid");
                                Console.WriteLine("Invalid");
                            }
                        }
                        else
                        {
                            Console.WriteLine("NOT INITED");
                            Console.WriteLine("NOT INITED");
                            Console.WriteLine("NOT INITED");
                            Console.WriteLine("NOT INITED");
                            SendValidity(handler, "Valid");
                            Console.WriteLine("Valid");
                            dic[content] = current;
                        }
                    }
                    else
                    {
                        SendValidity(handler, "-");
                    }

                }
                else if (content.Contains("RequestProducts"))
                {
                    SendProducts(handler);
                    content = String.Empty;
                }
                else
                {

                    int index = content.IndexOf("<EOF>");
                    content = (index < 0)
                        ? content
                        : content.Remove(index, "<EOF>".Length);

                    ActionProducts(content);

                    Console.WriteLine();
                    Console.WriteLine("--------------------PRODUCTS HERE-------------------");
                    foreach (var product in Products)
                    {
                        Console.WriteLine(product.Name);
                    }
                    Console.WriteLine("-------------------END OF     -PRODUCTS HERE-------------------");

                    Console.WriteLine();
                    Console.WriteLine();
                }

            }
            else
            {
                // Not all data received. Get more.  
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            }
        }


    }

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

    private static void ActionProducts(string data)
    {

        Console.WriteLine(data);
        Console.Read();
        var settings = XmlDeserializeFromString(data, typeof(ProductDB));

        ProductDB prr = settings as ProductDB;

        Console.WriteLine("PRODUCT RECEIVED");
        Console.WriteLine("PRODUCT RECEIVED");
        Console.WriteLine("PRODUCT RECEIVED");
        Console.WriteLine("PRODUCT RECEIVED");
        Console.WriteLine("PRODUCT RECEIVED");
        Console.WriteLine("PRODUCT RECEIVED");
        Console.WriteLine("PRODUCT RECEIVED");
        Console.WriteLine("PRODUCT RECEIVED");

        Console.WriteLine();

        if (prr.ActiveAdd)
        {
            Products.Add(prr);
            Console.WriteLine("ADD product");
            Console.WriteLine(prr.ID);
            Console.WriteLine(prr.Name);
            Console.WriteLine();
        }
        else
        {
            for (int i = 0; i < Products.Count; i++)
            {
                if (prr.ID == Products[i].ID)
                {
                    Products.RemoveAt(i);
                    Console.WriteLine("Removing item:");
                    Console.WriteLine(prr.ID);
                    Console.WriteLine(prr.Name);
                    Console.WriteLine();

                    break;
                }
            }
        }
        Console.Read();
    }

    public static void SendValidity(Socket handler, string data)
    {
        byte[] byteData = Encoding.ASCII.GetBytes(data);
        handler.BeginSend(byteData, 0, byteData.Length, 0,
        new AsyncCallback(SendCallback), handler);
    }


    public static void SendProducts(Socket handler)
    {

        XmlSerializer x = new XmlSerializer(typeof(List<ProductDB>));
        var sww = new StringWriter();
        XmlWriter writer = XmlWriter.Create(sww);
        x.Serialize(writer, Products);
        string xml = sww.ToString();
        Console.WriteLine(xml);

        byte[] byteData = Encoding.ASCII.GetBytes(xml);
        handler.BeginSend(byteData, 0, byteData.Length, 0,
        new AsyncCallback(SendCallback), handler);
    }


    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static int Main(String[] args)
    {

        StartListening();
        return 0;
    }
}