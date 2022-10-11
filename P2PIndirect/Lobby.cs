using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Soxbear.Networking;
using Soxbear.Networking.P2PIndirect.Messages;

namespace Soxbear.Networking.P2PIndirect {
    public class Lobby : MonoBehaviour
    {
        public bool Connected;

        public bool Host;

        public int LobbyNumber;

        public List<string> PlayerNames;


        public Disconnect OnDisconnect;


        public void ConnectToLobbyServer(ConnectionType ServerType, string IP) {
            Connection LobbyServer = Core.OpenConnection(ServerType, IP, "Server");
            LobbyServer.OnOpen += () => {
                Connected = true;
            };
            LobbyServer.OnClose += () => {
                Connected = false;
                OnDisconnect(true);
            };
        }

        public void JoinLobby(int LobbyId, string Name) {
            Core.Connection.Message("Server", new Join(LobbyId, Name));
        }

        Subscription<LobbyNameInfo> LobbyUpdateSub;

        public void UpdateLobby(LobbyNameInfo Info) {
            PlayerNames = Info.Names;
            OnLobbyUpdate();
        }

        public System.Action OnLobbyUpdate;

        public void MessageHost(object Data) {
            Core.Connection.Message("Host", Data);
        }

        public void Disconnect(bool server = false) {
            if (server)
                Core.Connection.Close();
            else {
                PlayerNames = new List<string>();
                Core.Connection.Message("Server", new Messages.Disconnect(), "Self");
            }
        }

        public void PlayerDisconnect(Messages.Disconnect Dsc, string Target) {
            int DscTarget = int.Parse(Target);
            PlayerNames[DscTarget] = null;
        }



        public void HostGame() {
            Core.Connection.Message("Server", new Host());
            LobbyIdSub = new Subscription<Join>(RecieveLobbyId);
        }
        Subscription<Join> LobbyIdSub;
        public void RecieveLobbyId(Join Info) {
            LobbyNumber = Info.GameId;
            LobbyIdSub.Remove();
            LobbyIdSub = null;
        }

        public void SendLobbyUpdate() {
            MessageAll(new LobbyNameInfo(PlayerNames));
        }

        SubscriptionTarget<Join> LobbyJoinSub;
        public void JoinLobby(Join Info, string Target) {
            PlayerNames.Add(Info.Name);
            SendLobbyUpdate();
            LobbyUpdateSub = new Subscription<LobbyNameInfo>(UpdateLobby);
        }

        public void MessageAll(object Data, string Target = "") {
            Core.Connection.Message("All", Data, Target);
        }

        public void MessagePlayer(int PlayerId, object Data) {
            Core.Connection.Message(PlayerId.ToString(), Data);
        }

        public void DisconnectPlayer(int PlayerId) {
            Core.Connection.Message("Server", new Messages.Disconnect(), PlayerId.ToString());
            PlayerDisconnect(new P2PIndirect.Messages.Disconnect(), PlayerId.ToString());
            MessageAll(new Messages.Disconnect(), PlayerId.ToString());
        }

    
        void Awake()
        {
            DontDestroyOnLoad(this);
        }
    }

    public delegate void Disconnect(bool Server);

    namespace Messages {
        public struct Join {
            public int GameId;

            public string Name;

            public Join(int Id, string _Name) {
                GameId = Id;
                Name = _Name;
            }
        }

        [System.Serializable]
        public struct Disconnect {
        }

        public struct Host {            
        }

        public struct LobbyNameInfo {
            public List<string> Names;

            public LobbyNameInfo(List<string> _Names) {
                Names = _Names;
            }
        }

        public class LobbyList {
            public List<int> LobbyIds;
            public List<int> PlayerCounts;
        }
    }
}

