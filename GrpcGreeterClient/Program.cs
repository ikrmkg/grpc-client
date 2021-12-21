using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using static GrpcGreeterClient.Greeter;

namespace GrpcGreeterClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Greeter.GreeterClient(channel);

            //Unary
            var reply = await client.UnaryCallAsync(
                new ExampleRequest { Message = "GreeterClient" });
            Console.WriteLine("Unary: " + reply.Message);
            Console.WriteLine(new String('-', 25));
            
            //Server Stream
            Console.WriteLine("ServerStreamEx starts: ");
            await ServerStreamEx(client);
            Console.WriteLine(new String('-', 25));

            //Client Stream
            Console.WriteLine("ClientStreamEx starts: ");
            await ClientStreamEx(client);
            Console.WriteLine(new String('-', 25));

            //Bi-directional Stream
            Console.WriteLine("BiDirectionalStreamEx starts: ");
            await BiDirectionalStreamEx(client);
            Console.WriteLine(new String('-', 25));

            //Bi-directional Simultaneous Stream
            Console.WriteLine("BiDirectionalStreamEx starts: ");
            await BiDirectionalStreamSimultaneouslyEx(client);
            Console.WriteLine(new String('-', 25));

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static async Task ServerStreamEx(GreeterClient client)
        {
            using var call = client.StreamingFromServer(new ExampleRequest { Message = "ServerStream " });

            while (await call.ResponseStream.MoveNext())
            {
                Console.WriteLine("Greeting: " + call.ResponseStream.Current.Message);
                // "Greeting: Hello World" is written multiple times
            }
        }

        public static async Task ClientStreamEx(GreeterClient client)
        {
            using var call = client.StreamingFromClient();

            for (var i = 0; i < 3; i++)
            {
                await call.RequestStream.WriteAsync(new ExampleRequest { Message = "Client " + i.ToString() });
            }
            await call.RequestStream.CompleteAsync();

            var response = await call;
            Console.WriteLine(response.Message);
        }

        public static async Task BiDirectionalStreamEx(GreeterClient client)
        {
            using var call = client.StreamingBothWays();

            Console.WriteLine("Starting background task to receive messages");
            var readTask = Task.Run(async () =>
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    Console.WriteLine(response.Message);
                    // Echo messages sent to the service
                }
            });

            Console.WriteLine("Starting to send messages");
            Console.WriteLine("Type a message to echo then press enter.");
            while (true)
            {
                var result = Console.ReadLine();
                if (string.IsNullOrEmpty(result))
                {
                    break;
                }

                await call.RequestStream.WriteAsync(new ExampleRequest { Message = result });
            }

            Console.WriteLine("Disconnecting");
            await call.RequestStream.CompleteAsync();
            await readTask;
        }

        public static async Task BiDirectionalStreamSimultaneouslyEx(GreeterClient client)
        {
            using var call = client.StreamingBothWaysSimultaneously();

            Console.WriteLine("Starting background task to receive messages");
            var readTask = Task.Run(async () =>
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    Console.WriteLine(response.Message);
                    // Echo messages sent to the service
                }
            });

            Console.WriteLine("Starting to send messages");
            Console.WriteLine("Type a message to echo then press enter.");
            while (true)
            {
                var result = Console.ReadLine();
                if (string.IsNullOrEmpty(result))
                {
                    break;
                }

                await call.RequestStream.WriteAsync(new ExampleRequest { Message = result });
            }

            Console.WriteLine("Disconnecting");
            await call.RequestStream.CompleteAsync();
            await readTask;
        }
    }
}
