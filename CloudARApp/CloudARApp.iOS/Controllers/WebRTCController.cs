using Foundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ARKit;

using CloudARApp.Interfaces;
using CloudARApp.Utilities;
using WebRTC.iOS.Bindings;
using CloudARApp.iOS.Views;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace CloudARApp.iOS.Controllers
{
    public class WebRTCController : NSObject, IRTCPeerConnectionDelegate, IRTCDataChannelDelegate
    {
        public SignalingController signalingController;

        private RTCPeerConnectionFactory peerConnectionFactory;

        private RTCConfiguration configuration;
        private RTCMediaConstraints mediaConstraints;
        private RTCDataChannelConfiguration dataChannelConfiguration;

        private RTCPeerConnection peerConnection;
        private RTCDataChannel dataChannel;

        private bool remotePeerConnected = false;
        private Queue<Types.Candidate> candidateQueue = new Queue<Types.Candidate>();

        public ARVideoView remoteRenderView;
        public RTCVideoTrack remoteTrack;

        private ARSCNView ARSceneView;

        public Action OnPeerConnected;

        public WebRTCController()
        {
            signalingController = new SignalingController(Constants.SIGNALING_SERVER_URL);
            signalingController.AnswerReceived.Subscribe(ReceiveAnswer);
            signalingController.CandidateReceived.Subscribe(ReceiveCandidate);

            // create configurations
            configuration = new RTCConfiguration();
            configuration.SdpSemantics = RTCSdpSemantics.UnifiedPlan;
            configuration.IceServers = new RTCIceServer[]
            {
                new RTCIceServer(new [] { Constants.STUN_SERVER_URL })
            };

            dataChannelConfiguration = new RTCDataChannelConfiguration();
            dataChannelConfiguration.ChannelId = 1;

            var constraints = new NSDictionary<NSString, NSString>(new NSString("OfferToReceiveVideo"), new NSString("true"));
            mediaConstraints = new RTCMediaConstraints(constraints, null);

            peerConnectionFactory = new RTCPeerConnectionFactory();

            SetupConnection();
            MakeOffer();
        }

        public void SetupConnection()
        {
            Debug.WriteLine($"Setting up peer connection", "WebRTC");
            peerConnection = peerConnectionFactory.PeerConnectionWithConfiguration(configuration, mediaConstraints, this);

            Debug.WriteLine($"Setting up data channel", "WebRTC");
            dataChannel = peerConnection.DataChannelForLabel("pose", dataChannelConfiguration);
            dataChannel.Delegate = this;
        }

        public void MakeOffer()
        {
            peerConnection.OfferForConstraints(mediaConstraints, (sdp, err) =>
            {
                if (err == null)
                {
                    Dispatch(() =>
                    {
                        Debug.WriteLine($"Created Offer", "WebRTC");
                        peerConnection.SetLocalDescription(sdp, (err1) =>
                        {
                            if (err1 != null) Debug.WriteLine($"Error in setting local description\n{err1}", "WebRTC");
                        });
                        Types.SDP message = new Types.SDP
                        {
                            type = "offer",
                            sdp = sdp.Sdp
                        };
                        signalingController.Send(message, "offer");
                    });
                }
                else
                {
                    Debug.WriteLine($"Error in MakeOffer\n{err}", "WebRTC");
                }
            });
        }

        public void ReceiveAnswer(Types.SDP message)
        {
            RTCSessionDescription answerSdp = new RTCSessionDescription(RTCSdpType.Answer, message.sdp);
            peerConnection.SetRemoteDescription(answerSdp, (err1) =>
            {
                if (err1 != null) Debug.WriteLine($"Error in setting local description\n{err1}", "WebRTC");
            });
            remotePeerConnected = true;
            while (candidateQueue.Count > 0)
                AddIceCandidate(candidateQueue.Dequeue());
        }

        public void ReceiveCandidate(Types.Candidate message)
        {
            if (remotePeerConnected)
                AddIceCandidate(message);
            else
                candidateQueue.Enqueue(message);
        }

        private void AddIceCandidate(Types.Candidate message)
        {
            RTCIceCandidate iceCandidate = new RTCIceCandidate(message.candidate.candidate, message.candidate.sdpMLineIndex, message.candidate.sdpMid);
            peerConnection.AddIceCandidate(iceCandidate);
        }

        private void SendDataChannel(string data)
        {
            dataChannel.SendData(data);
        }

        private void Dispatch(Action action)
        {
            DispatchQueue.MainQueue.DispatchAsync(action);
        }

        public void SetARSceneView(ARSCNView sceneView)
        {
            ARSceneView = sceneView;
            ThreadPool.QueueUserWorkItem(o => SendARPose());
        }

        public void SendARPose()
        {
            while (true)
            {
                var transform = ARSceneView.PointOfView.Transform;
                var raw_rotation = ARSceneView.PointOfView.WorldOrientation;
                var rotation = new Types.Rotation
                {
                    x = raw_rotation.X,
                    y = raw_rotation.Y,
                    z = raw_rotation.Z,
                    w = raw_rotation.W
                };
                var raw_position = ARSceneView.PointOfView.WorldPosition;
                var position = new Types.Position
                {
                    x = raw_position.X,
                    y = raw_position.Y,
                    z = raw_position.Z
                };
                var pose = new Types.Pose
                {
                    type = "pose",
                    position = position,
                    rotation = rotation
                };
                string json = JsonConvert.SerializeObject(pose, Formatting.None);
                SendDataChannel(json);
            }
        }

        public void SetRemoteViewRenderer(ARVideoView remoteVideoView)
        {
            remoteRenderView = remoteVideoView;
            remoteTrack.AddRenderer(remoteRenderView);
        }

        #region PeerConnectionDelegate
        public void DidAddStream(RTCPeerConnection peerConnection, RTCMediaStream stream)
        {
            if (stream.VideoTracks.FirstOrDefault() is RTCVideoTrack track)
            {
                Debug.WriteLine($"Received remote stream with {stream.VideoTracks.Length} tracks", "WebRTC");
                remoteTrack = track;
            }
        }

        public void DidChangeIceConnectionState(RTCPeerConnection peerConnection, RTCIceConnectionState newState)
        {
            Debug.WriteLine($"ICE Connection State Changed to: {newState}", "WebRTC");
            if (newState == RTCIceConnectionState.Connected)
                OnPeerConnected();
        }

        public void DidChangeIceGatheringState(RTCPeerConnection peerConnection, RTCIceGatheringState newState)
        {
        }

        public void DidChangeSignalingState(RTCPeerConnection peerConnection, RTCSignalingState stateChanged)
        {
            Debug.WriteLine($"Signaling State Changed to: {stateChanged}", "WebRTC");
        }

        public void DidGenerateIceCandidate(RTCPeerConnection peerConnection, RTCIceCandidate candidate)
        {
            Debug.WriteLine($"Generated ICE Candidate", "WebRTC");
            Types.Candidate c = new Types.Candidate
            {
                type = "candidate",
                candidate = new Types.CandidateString
                {
                    candidate = candidate.Sdp,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex
                }
            };
            signalingController.Send(c, "candidate");
        }

        public void DidOpenDataChannel(RTCPeerConnection peerConnection, RTCDataChannel dataChannel)
        {
            Debug.WriteLine($"Data channel opened:\n{dataChannel.Label}", "WebRTC");
        }

        public void DidRemoveIceCandidates(RTCPeerConnection peerConnection, RTCIceCandidate[] candidates)
        {
        }

        public void DidRemoveStream(RTCPeerConnection peerConnection, RTCMediaStream stream)
        {
        }
        public void PeerConnectionShouldNegotiate(RTCPeerConnection peerConnection)
        {
        }
        #endregion


        #region DataChannelDelegate
        public void DataChannelDidChangeState(RTCDataChannel dataChannel)
        {
            Debug.WriteLine($"DataChannel Connection State Changed to: {dataChannel.ReadyState}", "WebRTC");
        }

        public void DidReceiveMessageWithBuffer(RTCDataChannel dataChannel, RTCDataBuffer buffer)
        {
            string data = new NSString(buffer.Data, NSStringEncoding.UTF8);
            Types.Message parsedType = JsonConvert.DeserializeObject<Types.Message>(data);
            switch (parsedType.type)
            {
                case "ping":
                    dataChannel.SendData(data);
                    break;
                default:
                    Debug.WriteLine($"DataChannel received unknown message type {parsedType.type}", "WebRTC");
                    break;
            }
        }

        #endregion
    }
}