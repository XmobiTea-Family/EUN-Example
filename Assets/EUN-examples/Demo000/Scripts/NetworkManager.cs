namespace XmobiTea.EUN.Demo000
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using XmobiTea.EUN;
    using XmobiTea.EUN.Common;
    using XmobiTea.EUN.Entity;
    using XmobiTea.EUN.Helper;

    public class NetworkManager : EUNManagerBehaviour
    {
        private const string PLAYER_PREFAB_PATH = "Demo000/Player";

        private static NetworkManager instance;
        public static NetworkManager Instance => instance;

        protected override void Awake()
        {
            base.Awake();

            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        protected override void Start()
        {
            base.Start();

            // step 1: we need to connect the server before create new player, if connected success, OnEUNConnected() will be call;

            Debug.Log("Connecting to server with customId " + UniqueIdHelper.GetId());

            EUNNetwork.Connect(UniqueIdHelper.GetId(), new XmobiTea.EUN.Common.EUNArray());
        }

        public override void OnEUNConnected()
        {
            base.OnEUNConnected();

            Debug.Log("On EUN Connected");

            // step 2: after connected server, we need join a lobby (it like the channel of room (or the city), where is contains other room
            // if join lobby success the call back OnEUNJoinLobby() will be call

            var subscriberChat = true;
            EUNNetwork.JoinDefaultLobby(subscriberChat);
        }

        public override void OnEUNJoinLobby()
        {
            base.OnEUNJoinLobby();

            Debug.Log("OnEUNJoinLobby success");

            // we need join a room to play this game
            var roomOption = new RoomOption.Builder()
                .SetOpen(true)
                .SetMaxPlayer(10)
                .SetVisible(true)
                .SetTtl(0)
                .SetCustomRoomProperties(new XmobiTea.EUN.Common.EUNHashtable())
                .SetCustomRoomPropertiesForLobby(new List<int>())
                .Build();

            EUNNetwork.JoinOrCreateRoom(0, null, roomOption);
        }

        public override void OnEUNJoinRoom()
        {
            base.OnEUNJoinRoom();

            Debug.Log("OnEUNJoinRoom");

            // step 3: we can create a agent player in here

            GetOrCreateNewPlayer();
        }

        void GetOrCreateNewPlayer()
        {
            var userId = EUNNetwork.UserId;

            var gameObjectLst = EUNNetwork.Room.GameObjectDic.Values;

            var characterCreated = gameObjectLst.FirstOrDefault(x => x.PrefabPath.Equals(PLAYER_PREFAB_PATH) && (x.InitializeData as EUNArray).GetString(0).Equals(userId));

            if (characterCreated == null)
            {
                // create new game object room

                EUNNetwork.CreateGameObjectRoom(PLAYER_PREFAB_PATH,
                    new object[] { userId },
                    new object[] { 0, 0, 0, 0 },
                    new EUNHashtable()
                );
            }
            else
            {
                // transfer this game object room to me

                EUNNetwork.TransferGameObjectRoom(characterCreated.ObjectId, EUNNetwork.PlayerId);
            }
        }

        public override EUNView OnEUNViewNeedCreate(RoomGameObject roomGameObject)
        {
            var prefab = Resources.Load<GameObject>(roomGameObject.PrefabPath);

            if (roomGameObject.PrefabPath.Equals(PLAYER_PREFAB_PATH))
            {
                var networkCharacterController = Instantiate(prefab).GetComponent<NetworkCharacterController>();

                return networkCharacterController.GetComponent<EUNView>();
            }

            return null;
        }
    }
}
