using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using Newtonsoft.Json;
using Websocket.Client;

using CloudARApp.Utilities;

namespace CloudARApp.iOS.Controllers
{
    public class SignalingController : IDisposable
    {
        public Subject<Types.SDP> AnswerReceived = new Subject<Types.SDP>();
        public Subject<Types.Candidate> CandidateReceived = new Subject<Types.Candidate>();

        private Uri serverUrl;

        private WebsocketClient client;

        public SignalingController(string url)
        {
            serverUrl = new Uri(url);
            client = new WebsocketClient(serverUrl);
            client.MessageReceived.Subscribe(Receive);
            client.DisconnectionHappened.Subscribe((info) => Debug.WriteLine($"Disconnect happened\n{info.Exception}", "Signaling"));
            client.StartOrFail().Wait();
        }

        private void Send(string data)
        {
            client.Send(data);
        }

        public void Send(object data, string messageType)
        {
            Debug.WriteLine($"Sending {messageType}", "Signaling");
            string json = JsonConvert.SerializeObject(data, Formatting.None);
            Send(json);
        }

        public void Receive(ResponseMessage data)
        {
            Types.Message parsedType = JsonConvert.DeserializeObject<Types.Message>(data.Text);

            Debug.WriteLine($"Received {parsedType.type}", "Signaling");
            switch (parsedType.type)
            {
                case "answer":
                    Types.SDP answer = JsonConvert.DeserializeObject<Types.SDP>(data.Text);
                    AnswerReceived.OnNext(answer);
                    break;
                case "candidate":
                    Types.Candidate candidate = JsonConvert.DeserializeObject<Types.Candidate>(data.Text);
                    CandidateReceived.OnNext(candidate);
                    break;
                default:
                    Debug.WriteLine($"Received message with unknown type {parsedType.type}", "Signaling");
                    break;
            }
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
