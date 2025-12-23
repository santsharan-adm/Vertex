using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Net.Sockets;
using System.Text;

namespace IPCSoftware.App.Services.UI 
{
    public class UiTcpClientFake : UiTcpClient
    {
        public UiTcpClientFake(IDialogService dialog, IAppLogger logger)
            : base(dialog, logger)
        {
            _dialog = dialog;
        }

        public new bool IsConnected { get; set; } = false;

       

        private TcpClient _client;
        private NetworkStream _stream;
        private readonly IDialogService _dialog;

        private bool _hasShownError = false;

        private readonly StringBuilder _messageAccumulator = new StringBuilder();


        public event Action<string> DataReceived;
        public event Action<bool> UiConnected;

        // This event is defined here but only raised in CoreClient.cs
        public event Action<AlarmMessage> AlarmMessageReceived;

    }
}
