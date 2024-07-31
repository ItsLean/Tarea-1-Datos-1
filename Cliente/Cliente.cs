using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Client
{
    private const string ServerAddress = "127.0.0.1";
    private const int Port = 8080;

    static async Task Main()
    {
        try
        {
            TcpClient client = new TcpClient(ServerAddress, Port);
            NetworkStream stream = client.GetStream();

            // Hilo para leer mensajes del servidor
            _ = Task.Run(async () =>
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // El servidor se ha desconectado
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Mensaje del servidor: {message}");
                }
            });

            // Enviar mensajes al servidor
            while (true)
            {
                string message = Console.ReadLine();
                if (string.IsNullOrEmpty(message)) continue;

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
