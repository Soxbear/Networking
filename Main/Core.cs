using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using WebSocketSharp;
using Soxbear.Networking.Messages;

namespace Soxbear.Networking {

    public static class Core
    {
        public static List<Connection> Connections = new List<Connection>();

        public static Connection Connection {
            get { return Connections[0]; }
        }


        public static Connection OpenConnection(ConnectionType ConnectionType, string Address, params string[] tags) {
            if (ConnectionType == ConnectionType.Mask) {
                Debug.LogError("Connection Masks must be opened with CreateMask(Connection, Mask)");
                return null;
            }
            Connections.Add(new Connection(ConnectionType, Address));
            Connections[Connections.Count - 1].Tags.AddRange(tags);            
            return Connections[Connections.Count - 1];
        }

        public static Connection ReplaceConnection(ConnectionType ConnectionType, string Address, int ConnectionToReplace, params string[] tags) {
            if (ConnectionType == ConnectionType.Mask){                
                Debug.LogError("Connection Masks must be opened with CreateMask(Connection, Mask)");
                return null;
            }
            Connections[ConnectionToReplace].Close();
            Connections[ConnectionToReplace] = new Connection(ConnectionType, Address);
            Connections[ConnectionToReplace].Tags.AddRange(tags);
            return Connections[ConnectionToReplace];
        }

        public static Connection CreateMask(int Connection, string Mask, params string[] tags) {
            Connections.Add(new Connection(ConnectionType.Mask, Connection.ToString()));
            Connections[Connections.Count - 1].Masks.Add(Mask);
            Connections[Connections.Count - 1].Tags.AddRange(tags);
            return Connections[Connections.Count - 1];
        }

        public static Connection CreateMask(Connection Connection, string Mask, params string[] tags) {
            return CreateMask(Connections.IndexOf(Connection), Mask, tags);
        }

        public static void CloseConnection(int ConnectionToClose, bool Remove = true) {
            Connections[ConnectionToClose].Close();
            if (Remove)
                Connections.RemoveAt(ConnectionToClose);
        }

        public static void CloseConnection(Connection ConnectionToClose, bool Remove = true) {
            CloseConnection(Connections.IndexOf(ConnectionToClose), Remove);
        }

        public static void Disconnect() {
            foreach (Connection Connect in Connections) {
                Connect.Close();
            }
            Connections = new List<Connection>();
        }

        public static MessageEvent AllMessageEvents;
    }

    public enum ConnectionType {
        Websocket,
        Mask
    }

    public enum ConnectionState {
        None,
        Connecting,
        Connected
    }

    public class Connection {
        public ConnectionType Type {
            get { return ConnectionType; }
        }
        private ConnectionType ConnectionType;

        public ConnectionState ConnectionState {
            get { return ConnectState; }
        }
        private ConnectionState ConnectState;

        public string Address {
            get { return InternetAddress; }
        }
        private string InternetAddress;


        public List<string> Tags;

        public List<string> Masks;

        public int DefaultSecondaryTarget;

        
        public ServerEvent OnOpen;

        public MessageEvent OnMessage;

        public MaskMessageEvent OnMaskMessage;

        public ServerEvent Close;

        public ServerEvent OnClose;

        private SendMessage SendMessage;
        

        public void Message(string Target, object Data, string SecondaryTarget = "") {
            if (ConnectionType == ConnectionType.Mask && SecondaryTarget == "") {
                SecondaryTarget = Target;
                Target = Masks[0];
            }

            if (Tags.Contains("Server"))
                SendMessage(new ServerMessage(Target, SecondaryTarget, Data).Json);
            else
                SendMessage(new NetworkMessage(Target, Data).Json);
        }

        private void Open() {
            switch (ConnectionType) {
                case ConnectionType.Websocket:
                    WebSocket Socket = new WebSocket(Address);
                    Socket.Connect();
                    Socket.OnOpen += (sender, e) => {
                        ConnectState = ConnectionState.Connected; 
                        OnOpen(); 
                    };
                    Socket.OnMessage += (sender, e) => {
                        NetworkMessage Msg = JsonUtility.FromJson<NetworkMessage>(e.Data);
                        OnMaskMessage(Msg.Target, System.Type.GetType(Msg.Type), JsonUtility.FromJson<object>(Msg.Data));
                        if (!Masks.Contains(Msg.Target))
                            OnMessage(Msg.Target, System.Type.GetType(Msg.Type), JsonUtility.FromJson<object>(Msg.Data));
                    };
                    Socket.OnClose += (sender, e) => { OnClose(); };
                    SendMessage += (Data) => {
                        Socket.Send(Data);
                    };
                    Close += () => {
                        Socket.Close();
                    };
                break;

                case ConnectionType.Mask:
                    Core.Connections[int.Parse(Address)].OnMaskMessage += (Mask, Type, Data) => {
                        if (!Masks.Contains(Mask))
                            return;
                        
                        OnMessage(Masks[0], Type, Data);
                    };
                    Core.Connections[int.Parse(Address)].OnClose += OnClose;
                    SendMessage += (Data) => {
                        Core.Connections[int.Parse(Address)].SendMessage(Data);
                    };
                break;
            }

            

            Close += () => {
                ConnectState = ConnectionState.None;
            };
        }


        public Connection(ConnectionType _ConnectionType, string _Address, params string[] _Tags) {
            ConnectionType = _ConnectionType;
            InternetAddress = _Address;
            ConnectState = ConnectionState.Connecting;
            Tags = new List<string>(_Tags);
            Masks = new List<string>();
            Open();
        }
    }

    public class Subscription<T> {
        public List<string> SubscriptionTags;
        Action<T> Sub;
        int Connection;

        public void OnRecieve(string _, Type Type, object Data) {
            if (Type == typeof(T)) {
                    Sub((T) Data);
                }
        }

        public Subscription(Action<T> Subscriber, int ConnectionId = 0, params string[] Tags) {
            Sub = Subscriber;
            SubscriptionTags = new List<string>();
            SubscriptionTags.Add(typeof(T).ToString());
            SubscriptionTags.AddRange(Tags);
            Connection = ConnectionId;

            Core.Connections[ConnectionId].OnMessage += this.OnRecieve;
        }

        public Subscription(Action<T> Subscriber, Connection ConnectionToSub, params string[] Tags) {
            int ConnectionId = Core.Connections.IndexOf(ConnectionToSub);
            Sub = Subscriber;
            SubscriptionTags = new List<string>();
            SubscriptionTags.Add(typeof(T).ToString());
            SubscriptionTags.AddRange(Tags);
            Connection = ConnectionId;

            Core.Connections[ConnectionId].OnMessage += this.OnRecieve;
        }

        public void Remove() {
            Core.Connections[Connection].OnMessage -= this.OnRecieve;
        }
    }

    public class SubscriptionTarget<T> {
        public List<string> SubscriptionTags;
        Action<T, string> Sub;
        int Connection;

        public void OnRecieve(string Target, Type Type, object Data) {
            if (Type == typeof(T)) {
                    Sub((T) Data, Target);
                }
        }

        public SubscriptionTarget(Action<T, string> Subscriber, int ConnectionId = 0, params string[] Tags) {
            Sub = Subscriber;
            SubscriptionTags = new List<string>();
            SubscriptionTags.Add(typeof(T).ToString());
            SubscriptionTags.AddRange(Tags);
            Connection = ConnectionId;

            Core.Connections[ConnectionId].OnMessage += this.OnRecieve;
        }

        public SubscriptionTarget(Action<T, string> Subscriber, Connection ConnectionToSub, params string[] Tags) {
            int ConnectionId = Core.Connections.IndexOf(ConnectionToSub);
            Sub = Subscriber;
            SubscriptionTags = new List<string>();
            SubscriptionTags.Add(typeof(T).ToString());
            SubscriptionTags.AddRange(Tags);
            Connection = ConnectionId;

            Core.Connections[ConnectionId].OnMessage += this.OnRecieve;
        }

        public void Remove() {
            Core.Connections[Connection].OnMessage -= this.OnRecieve;
        }
    }

    public delegate void MessageEvent(string Target, Type Type, object Data);

    public delegate void MaskMessageEvent(string Mask, Type Type, object Data);

    public delegate void SendMessage(string Data);

    public delegate void ServerEvent();
}


