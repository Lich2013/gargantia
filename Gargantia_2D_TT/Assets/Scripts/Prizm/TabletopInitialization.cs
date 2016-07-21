using UnityEngine;
using System.Collections;
using Extensions;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Net;

public class TabletopInitialization : MonoBehaviour {
	private const string typeName = "Gargantia";
	public string gameName = "Gargantia";

	public Text msgPrefab;
	GameObject msgCanvas;
	GameManager gameManager;

	private bool readyToBindDDP = false;
	
	//Tables in database
	Meteor.Collection<ChannelTemplate> channelCollection;	//how each client 'talks' to each other
	Meteor.Collection<ClientTemplate> clientCollection;		//list of clients connected in session
	Meteor.Collection<SessionTemplate> sessionCollection;	//keep track of session(s)
	Meteor.Collection<PlayerTemplate> playerCollection;		//list of players
	
	//subscriptions 
	Meteor.Subscription channelSubscription;
	Meteor.Subscription sessionSubscription;
	Meteor.Subscription clientSubscription;
	Meteor.Subscription playerSubscription;

	[HideInInspector]
	public Meteor.Subscription recordGroupSubscription;		//used by PrizmRecordGroup
	
	//URL of Meteor Server
	string meteorURL = "ws://localhost:6969/websocket";	//thats not michaels' laptop for debugging

	[HideInInspector]
	public string appID = "Gargantia";							//name of game

	//public string defaultRecordGroup = "cards";					//name of recordGroup that GameObjects can occupy
	[HideInInspector]
	public string sessionID = "defaultSessionID";				//default string initialized for debugging  (gets assigned from server)
	[HideInInspector]
	public string clientID = "defaultClientID";					//default string initialized for debugging  (gets assigned from server)
	[HideInInspector]
	public string DDPConnectionID = "defaultDDPConnectionID";	//default string initialized for debugging  (gets assigned from server)
	[HideInInspector]
	public string deviceID = "defaultDeviceID";					//default string initialized for debugging  (gets assigned from unique identifier)
	[HideInInspector]
	public string sessionName = "defaultSessionName";
	
	List<string> userIDs= new List<string> ();
	List<string> players= new List<string> ();

	[HideInInspector]
	public PrizmRecordGroup recordGroup;
	
	//Sync routine variables	
	bool gameSynced = false;									//developer can choose to use (or not)
	clientStatuses clientSyncPosition = (clientStatuses)0;		//keeps track of position in client sync routine
	sessionStatuses sessionSyncPosition = (sessionStatuses)0;	//keeps track of position in client sync routine
	int playersSynced = 0;										//tabletop needs to keep track of how many players are synced
	
	int numberOfPlayers;								//is determined by userIDs that are in list
	
	//meteor connection code needs to be the first to run and connect to database (all further actions depend on finishing this initialization)
	void Awake() {
		recordGroup = GetComponent<PrizmRecordGroup> ();
		deviceID = SystemInfo.deviceUniqueIdentifier;	//assign device UID for server
		gameManager = GetComponent<GameManager> ();
		msgCanvas = GameObject.Find ("MsgCanvas");
	}
	public void createMsgLog(string message, float timer = 2f){
		if(msgCanvas.transform.FindChild("connecting")){
			msgCanvas.transform.FindChild("connecting").GetComponent<selfDestructMessage>().killMyself(message, timer);
		}
		else{
			Text newText = Instantiate (msgPrefab) as Text;
			//newText.transform.position.Set (0, 0, 0);
			newText.transform.SetParent(msgCanvas.transform);
			newText.gameObject.name = "connecting";
			newText.GetComponent<selfDestructMessage> ().killMyself (message, timer);
		}
	}
	

	public IEnumerator MeteorInit() {

		Meteor.LiveData.Instance.DidConnect += (string connectionID) => {	//recieved DDPConnectionID from Meteor
			DDPConnectionID = connectionID;
			Debug.Log ("received ddpconnectionID: " + DDPConnectionID);		//is this the same ddpconnectionID that is returned by bindClientToDDPConnection?

			readyToBindDDP = true;
		};
		
		Debug.Log ("didconnect handler is added");


		Debug.Log ("connecting to meteor");
		createMsgLog ("Connecting to Meteor...");
		yield return Meteor.Connection.Connect (meteorURL);		//establish initial connection to database
		Debug.Log ("called Meteor.connect");
		createMsgLog ("...called Meteor.connect");

		yield return StartCoroutine (SetGameName ());
												//broadcasts session name so HH can connect to IP
		yield return StartCoroutine (OpenSession ());			//creates session document(s) on meteor side
		StartServer ();	
		
		//creates all collections on Unity side
		yield return StartCoroutine (CreateClientDoc ("clients"));		//Create clients document
		yield return StartCoroutine (CreateChannelDoc ("channels"));	//Create document
		yield return StartCoroutine (CreateSessionDoc ("sessions"));	//Create document
		yield return StartCoroutine (CreatePlayerDoc ("players"));		//Create document
		yield return StartCoroutine (recordGroup.CreateGameObjectCollection ());
		
		yield return StartCoroutine(Subscribe ());						//subscribes to tabletopBootstrap, which is all channels
		
		recordGroup.gameObjectCollection.DidChangeRecord += GetComponent<GameManager>().HandleDidChangeRecord;		//add if a handheld folds recordChangeHandler

		
		playerCollection.DidAddRecord += (string arg1, PlayerTemplate arg2) => {
			Debug.Log ("player collection got record added, name: " + arg2.name + ", color: " + arg2.colorRGB.ToString() + "playerID: " + arg2.playerID + ", _id: " + arg2._id + "sessionID" + arg2.session_id);
			Debug.Log ("r: " + arg2.colorRGB[0]);
			//This gives the player a unique color and broadcasts it to them
			Debug.Log ("our sessionID: " + sessionID + ", player's sessionID: " + arg2.session_id);
			Color tempColor = new Color(arg2.colorRGB[0], arg2.colorRGB[1], arg2.colorRGB[2]);
			if (arg2.session_id == sessionID) {
				//tempColor = gameManager.ColorOfPlayer();

				//makes UI player circle object
				GetComponent<GameManager>().CreateNewPlayer(arg2.name, tempColor, arg2._id);
				//StartCoroutine(callUpdatePlayer(arg2.name, tempColor));

				//make UI player circle object
				//'callBindPlayerToSession' is called in edgeSnap.cs after the player releases their portrait
				//this gives the HH client a sessionID so that they can openHandheldSession
			}
		};

		playerCollection.DidRemoveRecord += gameManager.HandleDidLosePlayer;
		playerCollection.DidChangeRecord += gameManager.HandleDidChangePlayerRecord;
		
		channelCollection.DidChangeRecord += HandleDidChangeRecordSync;		//allows HH to gamesync routine with TT
		Debug.Log ("Done with MeteorInit");
		createMsgLog ("Table is now ready!", 5.0f);

		OnServerInitialized ();
	}



	//Record change handler for initial game sync
	void HandleDidChangeRecordSync (string arg1, ChannelTemplate arg2, IDictionary arg3, string[] arg4)
	{
		if (arg2.receiver_id == clientID) {	//if this message applies to us (the receiverID is us)
			clientSyncPosition = (clientStatuses) System.Enum.Parse (typeof(clientStatuses), arg2.payload);
			Debug.Log ("client Sync position is: " + clientSyncPosition.ToString());
			sessionSyncPosition = (sessionStatuses)(int)(clientSyncPosition + 1);
			Debug.Log ("session Sync position is: " + sessionSyncPosition.ToString());
			if ((int)sessionSyncPosition > (int)sessionStatuses.running) {		//last stage of sync is 'running'
				//don't broadcast anything to the client, they are running
				playersSynced++;
				Debug.Log ("One more client is fully synced!" + sessionSyncPosition + ", total: " + playersSynced);
				if (numberOfPlayers != playersSynced) {
					Debug.Log ("number of players != playersSynced! numplayers: " + numberOfPlayers + ", playersSynced: " + playersSynced);
				}
			} else {
				Debug.Log ("recieved message for sync: " + arg2.payload + "; on state: " + sessionSyncPosition);
				StartCoroutine (callUpdateSessionStatus (sessionSyncPosition.ToString ()));
			}
		} else {
			Debug.Log ("this message is not directed at us, senderID: " + arg2.sender_id + ", receiverID: '" + arg2.receiver_id + "', our clientID: '" + clientID + "'");
		}
	}

	//used for updating the player's color and telling HH client what color it is
	IEnumerator callUpdatePlayer(string playerName, Color clr) {
		//string theColor = clr.ToString ();
		yield return new WaitForSeconds (0.1f);
		Debug.Log ("in callUpdatePlayer, name: " + playerName +", color: " + new float[]{clr.r, clr.g, clr.b}.ToString());
		var methodCall = Meteor.Method<ChannelResponse>.Call ("updatePlayer", playerName, new float[]{clr.r, clr.g, clr.b});
		yield return (Coroutine)methodCall;
		
		if (methodCall.Response.success) {
			Debug.Log ("call to updatePlayer SUCCEEDED!, response: " + methodCall.Response.message);
			createMsgLog(playerName + " has joined the table!", 3.0f);
			DDPConnectionID = methodCall.Response.message;
		} else {
			Debug.Log("call to 'updatePlayer' did not succeed.");
		}
	}

	//establishes DDP connection
	IEnumerator BindDDPConnection() {
		Debug.Log ("in BindDDPConnection(), connectionID is: " + DDPConnectionID);
		var methodCall = Meteor.Method<ChannelResponse>.Call ("bindClientToDDPConnection", clientID, DDPConnectionID);
		yield return (Coroutine)methodCall;
		
		if (methodCall.Response.success) {
			Debug.Log ("call to bindClientToDDPConnection SUCCEEDED!, response: " + methodCall.Response.message);
			DDPConnectionID = methodCall.Response.message;
		} else {
			Debug.Log("call to 'bindClientToDDPConnection' did not succeed.");
		}
		Debug.Log ("out of BindDDPConnection()");
	}
	
	//update session status to 'msg' (i.e. 'allPaired')
	IEnumerator callUpdateSessionStatus(string msg) {
		Debug.Log ("Calling updateSession Status with: " + msg);
		var methodCall = Meteor.Method<ChannelResponse>.Call ("updateSessionStatus", sessionID, clientID, msg);	//make clientID the handheld ID later, last parameter is dictionary for intiial statuses
		yield return (Coroutine)methodCall;
		if (methodCall.Response.success) {
			Debug.Log("session status updated successfully, received: " + methodCall.Response.message);
		} else {
			Debug.LogError ("updateSessionStatus returned false: " + methodCall.Response.message);
		}
		
		//broadcast updated status to all handhelds using channels:
		yield return StartCoroutine (callBroadcastToHandheldClients (msg));
		//Debug.Log ("Done with updatesessionstatus");
	}
	
	//broadcast a message to all handhelds with this
	IEnumerator callBroadcastToHandheldClients(string msg) {
		//Debug.Log ("Broadcasting to all channels: " + msg);
		var methodCall = Meteor.Method<ChannelResponse>.Call ("broadcastToHandheldClients", sessionID, clientID, msg);	//make clientID the handheld ID later, last parameter is dictionary for intiial statuses
		yield return (Coroutine)methodCall;
		if (methodCall.Response.success) {
			//Debug.Log("broadcast to all channels successfully, received: " + methodCall.Response.message);
		} else {
			Debug.LogError ("broadcastToAllChannels returned false");
		}
	}
	
	//calls openTabletopSession on meteor, stores the sessionID and clientID	
	IEnumerator OpenSession() {
		Debug.Log ("Calling 'openTableTopSession' :" + appID + " " + deviceID);
		var methodCall = Meteor.Method<OpenSessionResponse>.Call ("openTabletopSession", appID, deviceID);
		yield return (Coroutine)methodCall;
		Debug.Log ("Called 'openTabletopsession' ");
		
		// Get the value returned by the method.
		if (methodCall.Response.success) {
			Debug.Log ("Open Session succeeded!" + "clientID: " + methodCall.Response.clientID + ", sessionID: " + methodCall.Response.sessionID + ", name: " + methodCall.Response.sessionName);
			clientID = methodCall.Response.clientID;
			sessionID = methodCall.Response.sessionID;
			sessionName = methodCall.Response.sessionName;
			gameName = methodCall.Response.sessionName;
			Debug.Log ("gameName set: " + gameName);
			GameObject.Find ("NameCanvas").transform.FindChild ("SessionName").gameObject.GetComponent<Text> ().text = sessionName;

			if (readyToBindDDP) {
				Debug.Log ("calling bindddp connection with: " + DDPConnectionID);
			StartCoroutine (BindDDPConnection ());		//binds low-level DDP connection
			}

		} else {
			Debug.LogError ("Open Session failed :(\nclientID and sessionID not set!");
		}


	}

	/*
	public IEnumerator AddRecord (Stats record) {	//adds a PrizmRecord to the GameObject doc
		Debug.Log ("Adding to database");	//call method "addObjectToGroup"
		
		//come up with clever way of using enums for this
		Dictionary<string, string> dict = new Dictionary<string, string> () {
			{"location", record.dbEntry.location},
			{"back", record.dbEntry.back},
			{"suit", record.dbEntry.suit},
			{"number", record.dbEntry.number}
		};
		
		var methodCall = Meteor.Method<ChannelResponse>.Call ("addGameObject", sessionID, defaultRecordGroup, dict);	
		yield return (Coroutine)methodCall;
		if (methodCall.Response.success) {
			Debug.Log("call to 'addGameObject' succeeded! Response: " + methodCall.Response.message);
			string UniqueID = methodCall.Response.message;
			record.setName(UniqueID);	//automatically sets isInDatase to be true (should only be called once)
		}
		else {
			Debug.LogError("call to 'addGameObject' failed! Response: " + methodCall.Response.message);
		}
	}
	*/
	
	
	public IEnumerator callBindPlayerToSession(string playerID) {
		Debug.Log ("Binding player to session: player: " + playerID);
		var methodCall = Meteor.Method<ChannelResponse>.Call ("bindPlayerToSession", sessionID, playerID);
		yield return (Coroutine)methodCall;
		Debug.Log ("bindPlayerToSession called");
		
		if (methodCall.Response.success) {
			Debug.Log ("call to bindPlayerToSession succeeded! Message: " + methodCall.Response.message);
			players.Add (methodCall.Response.message);
			if (playerID != methodCall.Response.message) {
				Debug.Log ("the playerID returned by 'bindPlayerToSession' was not the same as the stored PlayerID");
			}
		} else {
			Debug.LogError("uh oh! bindPlayerToSession call failed.");
		}
	}
	
	//removes record from GameObjects Collection by calling 'removeGameObject'
	//if you call this, you need to remove the record from PrizmRecordGroup.associates list as well
	/*
	public IEnumerator RemoveRecord (Stats record) {
		Debug.Log ("Removing from database: " + record.dbEntry._id);		
		var methodCall = Meteor.Method<ChannelResponse>.Call ("removeGameObject", record.dbEntry._id);		
		yield return (Coroutine)methodCall;
		if (methodCall.Response.success) {
			Debug.Log("Successfully removed from database, message: " + methodCall.Response.message);
		} else {	
			Debug.LogError("Error removing " + record.dbEntry._id + " from database");
		}
	}
	*/
	
	//associates clientCollection with meteor's client document
	IEnumerator CreateClientDoc(string recordGroupName) {	//
		clientCollection = Meteor.Collection <ClientTemplate>.Create (recordGroupName);
		yield return clientCollection;	//waits until collection is finished being created
		/* Add handler for debugging client adds: */
		/*
		clientCollection.DidAddRecord += (string id, ClientTemplate document) => {				
			Debug.Log(string.Format("Client document added:\n{0}", document.Serialize()));
			};
		*/
	}
	
	//associates channelCollection with meteor's channel document
	IEnumerator CreateChannelDoc(string recordGroupName) {
		channelCollection = Meteor.Collection <ChannelTemplate>.Create (recordGroupName);
		yield return channelCollection;	//waits until collection is finished being created
		/* Add handler for debugging channel adds: */
		/*
		channelCollection.DidAddRecord += (string id, ChannelTemplate document) => {				
			Debug.Log(string.Format("Channel document added:\n{0}", document.Serialize()));
		};
		*/
	}
	
	//associates sessionCollection with meteor's session document
	IEnumerator CreateSessionDoc(string recordGroupName) {	//
		sessionCollection = Meteor.Collection <SessionTemplate>.Create (recordGroupName);
		yield return sessionCollection;	//waits until collection is finished being created
		/* Add handler for debugging session adds: */
		/*
		sessionCollection.DidAddRecord += (string id, SessionTemplate document) => {
			Debug.Log(string.Format("Session document added:\n{0}", document.Serialize()));
		};
		*/
	}
	
	//associates playerCollection with meteor's session document
	IEnumerator CreatePlayerDoc(string recordGroupName) {	//
		playerCollection = Meteor.Collection <PlayerTemplate>.Create (recordGroupName);
		yield return playerCollection;	//waits until collection is finished being created
		/* Add handler for debugging session adds: */
		/*
		playerCollection.DidAddRecord += (string id, PlayerTemplate document) => {
			Debug.Log(string.Format("Player document added:\n{0}", document.Serialize()));
		};
		*/
	}

	
	
	//subscribes to all channels relevant to Tabletop device
	IEnumerator Subscribe() {
		var subscription = Meteor.Subscription.Subscribe ("tabletopBootstrap", sessionID, clientID);
		yield return (Coroutine)subscription;	//wait until subscription successful
		Debug.Log ("Subscribe() in TabletopInitilization finished");
		createMsgLog ("Subscribe() in TabletopInitilization finished");
	}
	
	//calls a generic method on the meteor server of 'methodName' with parameters 'args'
	//method must return a success bool and a string
	IEnumerator MethodCall(string methodName, string[] args) {	
		var methodCall = Meteor.Method<ChannelResponse>.Call (methodName, args);	
		yield return (Coroutine)methodCall;
		
		if (methodCall.Response.success) {
			Debug.Log (methodName + " executed successfully! Response: " + methodCall.Response.message);
		} else {
			Debug.LogError (methodName + " did NOT execute successfully.");
		}
	}	

	//calls meteor's 'getRandomMoniker' and sets that as our gameName
	IEnumerator SetGameName() {	
		var methodCall = Meteor.Method<ChannelResponse>.Call ("getRandomMoniker");	
		yield return (Coroutine)methodCall;
		
		if (methodCall.Response.success) {
			Debug.Log ("getRandomMoniker successfully! Response: " + methodCall.Response.message);

		} else {
			Debug.LogError ("getRandomMoniker did NOT execute successfully.");
		}
	}	

	private string GetIP() {
		string strHostName = "";
		strHostName = System.Net.Dns.GetHostName ();
		IPHostEntry ipEntry = System.Net.Dns.GetHostEntry (strHostName);
		IPAddress[] addr = ipEntry.AddressList;
		createMsgLog (addr.ToString(), 10);
		Debug.Log ("addr: " + addr.ToString());
		return addr [addr.Length - 1].ToString ();
	}

	private void StartServer() {
		Network.InitializeServer (32, 25000, !Network.HavePublicAddress ());
		Debug.Log ("starting server: " + typeName + ": " + gameName);
		MasterServer.RegisterHost (typeName, gameName);
	}

	void OnServerInitialized() {
		Debug.Log ("Server initialized");
		Debug.Log ("Server master ip address: " + MasterServer.ipAddress + " , our local IP: " + GetIP ());
		GameObject.Find ("NameCanvas").transform.FindChild ("IPAddress").gameObject.GetComponent<Text> ().text = "IP Address:'" + GetIP() + ":8100'";
		createMsgLog ("Connected to master server");
	}
	
}

