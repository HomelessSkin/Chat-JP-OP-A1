using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace MultiChat
{
    public class AdvancedWebSocketClient : MonoBehaviour
    {
        public event Action OnConnected;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action OnClose;

        bool isConnecting = false;
        ClientWebSocket ws;
        CancellationTokenSource cts;

        public async void Connect(string url)
        {
            if (isConnecting)
                return;

            isConnecting = true;

            try
            {
                ws = new ClientWebSocket();
                cts = new CancellationTokenSource();

                await ws.ConnectAsync(new Uri(url), cts.Token);

                isConnecting = false;
                OnConnected?.Invoke();

                _ = Task.Run(ReceiveLoop, cts.Token);
            }
            catch (Exception ex)
            {
                isConnecting = false;
                OnError?.Invoke($"Connect error: {ex.Message}");
            }
        }
        public async void Send(string message)
        {
            if (ws?.State != WebSocketState.Open)
            {
                Debug.LogWarning("WebSocket is not connected");

                return;
            }

            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(message);

                await ws.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token
                );
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Send error: {ex.Message}");
            }
        }
        public async void Disconnect()
        {
            try
            {
                cts?.Cancel();

                if (ws?.State == WebSocketState.Open)
                {
                    await ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Disconnect",
                        CancellationToken.None
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Disconnect error: {ex.Message}");
            }
            finally
            {
                ws?.Dispose();
            }
        }
        public WebSocketState GetState() => ws?.State ?? WebSocketState.None;

        async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            try
            {
                while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);

                        MainThreadDispatcher.Execute(() =>
                        {
                            OnMessage?.Invoke(message);
                        });
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                        break;
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                MainThreadDispatcher.Execute(() =>
                {
                    OnError?.Invoke($"Receive error: {ex.Message}");
                });
            }
            finally
            {
                MainThreadDispatcher.Execute(() =>
                {
                    OnClose?.Invoke();
                });
            }
        }
    }

    public static class MainThreadDispatcher
    {
        static readonly Queue<Action> executionQueue = new Queue<Action>();

        public static void Execute(Action action)
        {
            lock (executionQueue)
                executionQueue.Enqueue(action);
        }

        public static void Update()
        {
            lock (executionQueue)
                while (executionQueue.Count > 0)
                    executionQueue.Dequeue()?.Invoke();
        }
    }
}