using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Soxbear.Networking.Messages;

namespace Soxbear.Networking {
    public static class Controller
    {
        public static ConnectionMode ConnectionMode;
    }

    public enum ConnectionMode {
        Server,
        DirectHost,
        Host,
        Client
    }

    namespace Messages {
        public struct NetworkMessage {
            public string Target;

            public string Type;

            public string Data;

            public string Json {
                get {
                    return JsonUtility.ToJson(this);
                }
            }

            public NetworkMessage(string Destination, object _Data) {
                Target = Destination;
                Data = JsonUtility.ToJson(_Data);
                Type = _Data.GetType().ToString();
            }

            public NetworkMessage(string Json) {
                NetworkMessage Message = JsonUtility.FromJson<NetworkMessage>(Json);
                Target = Message.Target;
                Type = Message.Type;
                Data = Message.Data;
            }
        }

        public struct ServerMessage {
            public string Target;

            public string Data;

            public ServerMessage(string _Target, string SecondaryTarget, object _Data) {
                Target = _Target;
                Data = JsonUtility.ToJson(new NetworkMessage(SecondaryTarget, _Data));
            }

            public string Json {
                get { return JsonUtility.ToJson(this); }
            }
        }
    }
}

