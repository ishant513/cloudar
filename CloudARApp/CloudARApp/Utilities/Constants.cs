using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Essentials;

namespace CloudARApp.Utilities
{
    public static class Constants
    {
        public static string STUN_SERVER_URL = "stun:stun.l.google.com:19302";
        public static string SIGNALING_SERVER_URL = "wss://55b9d1d5ae6c.ngrok.io/signaling";
    }

    public static class Types
    {
        public class Message
        {
            public string type { get; set; }

            [JsonExtensionData]
            private IDictionary<string, JToken> additionalData;
        }

        public class SDP
        {
            public string type;
            public string sdp;
        }

        public class CandidateString
        {
            public string candidate;
            public string sdpMid;
            public int sdpMLineIndex;
        }

        public class Candidate
        {
            public string type;
            public CandidateString candidate;
        }

        public class Pose
        {
            public string type;
            public Position position;
            public Rotation rotation;
        }

        public class Position
        {
            public float x;
            public float y;
            public float z;
        }

        public class Rotation
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }


    }
}
