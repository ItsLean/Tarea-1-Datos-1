using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Server
{
    private const int Port = 8080;
    private static ConcurrentDictionary<TcpClient, string> clients = new ConcurrentDictionary<TcpClient, string>();
    private static TcpListener listener;

    static async Task Main()
    {
        listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine("Servidor iniciado. Esperando conexiones...");

        // Hilo para leer mensajes desde la consola
        _ = Task.Run(() => HandleConsoleInput());

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Cliente conectado.");

            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        string clientName = "Cliente" + client.GetHashCode(); // Nombre temporal para identificar al cliente

        try
        {
            // Saludar al nuevo cliente
            string welcomeMessage = "Bienvenido al chat!";
            byte[] welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
            await stream.WriteAsync(welcomeBytes, 0, welcomeBytes.Length);

            // Añadir el cliente a la lista de clientes
            clients.TryAdd(client, clientName);

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // El cliente se ha desconectado

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Mensaje recibido de {clientName}: {message}");

                // Enviar el mensaje a todos los clientes conectados
                foreach (var otherClient in clients.Keys)
                {
                    if (otherClient != client)
                    {
                        NetworkStream otherStream = otherClient.GetStream();
                        byte[] messageBytes = Encoding.UTF8.GetBytes($"{clientName}: {message}");
                        await otherStream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Eliminar el cliente de la lista y cerrar la conexión
            clients.TryRemove(client, out _);
            client.Close();
            Console.WriteLine($"Cliente {clientName} desconectado.");
        }
    }

    private static void HandleConsoleInput()
    {
        while (true)
        {
            string input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input))
            {
                // Enviar mensaje a todos los clientes conectados
                foreach (var client in clients.Keys)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] messageBytes = Encoding.UTF8.GetBytes($"Servidor: {input}");
                        stream.Write(messageBytes, 0, messageBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al enviar mensaje al cliente: {ex.Message}");
                    }
                }
            }
        }
    }
}
