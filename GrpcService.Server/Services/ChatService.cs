﻿namespace GrpcService.Server
{
    using System;
    using System.Threading.Tasks;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using Microsoft.Extensions.Logging;

    public class ChatService : Chat.ChatBase
    {
        private readonly ILogger<ChatService> _logger;

        public ChatService(ILogger<ChatService> logger)
        {
            _logger = logger;
        }

        public override async Task SendMessage(IAsyncStreamReader<ClientToServerMessage> requestStream,
            IServerStreamWriter<ServerToClientMessage> responseStream,
            ServerCallContext context)
        {
            var clientToServerTask =  ClientToServerPingHandlingAsync(requestStream, context);

            var serverToClientTask =  ServerToClientPingAsync(responseStream, context);

            await Task.WhenAll(clientToServerTask, serverToClientTask);
        }

        private static async Task ServerToClientPingAsync(IServerStreamWriter<ServerToClientMessage> responseStream, ServerCallContext context)
        {
            var pingCount = 0;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await responseStream.WriteAsync(new ServerToClientMessage
                {
                    Text = $"Server said hi {++pingCount} times",
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                });
                await Task.Delay(1000);
            }
        }

        private async Task ClientToServerPingHandlingAsync(IAsyncStreamReader<ClientToServerMessage> requestStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                var message = requestStream.Current;
                this._logger.LogInformation($"The Client said: {message.Text}");
            }
        }
    }
}
