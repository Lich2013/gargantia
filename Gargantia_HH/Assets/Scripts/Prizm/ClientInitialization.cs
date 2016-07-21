using UnityEngine;
using System.Collections;
using Extensions;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Net;
using UnityEngine.Assertions.Must;
using TouchScript;
using TouchScript.Gestures;

public class ClientInitialization : MonoBehaviour {

	//<----------> Reference to components for convenience <---------->//
	GameManager gameManager;
	PrizmRecordGroup recordGroup;
	[HideInInspector]
	public GameObject chooseSessionCanvas;
	public GameObject chooseSessionButton; 	//prefab gets dragged into here 
	public GameObject playerNameCanvas;
	public GameObject msgCanvas;
	public Text msgPrefab;

	string color;

	private const string typeName = "Gargantia";
	
	private HostData[] hostList;

	private bool readyToBindDDP = false;

	//<----------> Variables specific to this Handheld client (player) <---------->//
	public string playerName;	//sent to database to keep track of player in Tabletop Client
	
	//<----------> Collections are equivalent to tables in the database <---------->//
	Meteor.Collection<ChannelTemplate> channelCollection;	//how each client 'talks' to each other
	Meteor.Collection<ClientTemplate> clientCollection;		//list of clients
	Meteor.Collection<PlayerTemplate> playerCollection;		//list of all players
	Meteor.Collection<SessionTemplate> sessionCollection;	//list of all sessions

	//<----------> Allows for the immediate handling of record changes <---------->//
	Meteor.Subscription channelSubscription;
	Meteor.Subscription clientSubscription;
	Meteor.Subscription sessionSubscription;

	List<string> sessionNames = new List<string>();			//allows the player to choose which session to join

	//<----------> Variables associated with Meteor Databasery functions <---------->//
	private string meteorURL = "ws://127.0.0.1:6969/websocket";	

	string appID = "tapCube";							//name of game
	public string userID = "empty";							//set by player first thing
	public string playerID = "defaultPlayerID";					//given by the database after registering the player
	string deviceID = "defaultDeviceID";					//default string initialized for debugging (gets assigned from server)
	public string sessionID = "defaultSessionID";			//gets read by PrizmRecordGroup to add records to database
	string clientID = "defaultClientID";					//default string initialized for debugging (gets assigned from server)
	string DDPConnectionID = "defaultDDPConnectionID";		//default string initialized for debugging (gets assigned from server)
	string sessionName = "defaultSessionName";
	
	//<----------> variables to keep track of tabletop/handheld syncronization <---------->//
	private sessionStatuses sessionSyncPosition = (sessionStatuses)0;	//keeps track of position in client sync routine
	private clientStatuses clientSyncPosition = (clientStatuses)0;		//keeps track of position in client sync routine


	void Awake () {
		//obtain unique ID for device
		deviceID = SystemInfo.deviceUniqueIdentifier;
		recordGroup = GameObject.Find ("GameManager").GetComponent<PrizmRecordGroup> ();
		gameManager = GameObject.Find ("/GameManager").GetComponent<GameManager> ();
		chooseSessionCanvas = GameObject.Find ("ChooseSessionCanvas");
		playerNameCanvas = GameObject.Find ("PlayerNameCanvas");
		msgCanvas = GameObject.Find ("MsgCanvas");
		color = "red";
		playerNameCanvas.SetActive (false);	//disable "Enter Player Name" for now

	}

	void Update() {
		if (Meteor.LiveData.Instance.TimedOut)
			createMsgLog ("Meteor timed out!", 20f);
	}


	public void createMsgLog(string message, float timer) {
		if (msgCanvas.transform.FindChild ("connecting")) {
			msgCanvas.transform.FindChild ("connecting").GetComponent<SelfDestruct> ().KillMyself (message, timer);
		} else  {
			Text newText = Instantiate (msgPrefab) as Text;
			newText.transform.SetParent(msgCanvas.transform);
            newText.gameObject.name = "connecting";

            newText.GetComponent<SelfDestruct> ().KillMyself (message, timer);
		}
	}

	//connects and creates collections and subscriptions
	public IEnumerator MeteorInit() {

		Meteor.LiveData.Instance.DidConnect += (string connectionID) => {	//recieved DDPConnectionID from Meteor
			DDPConnectionID = connectionID;
			readyToBindDDP = true;
		};

		yield return Meteor.Connection.Connect (meteorURL);	//establish initial connection to database


		//<----------> Create all collections and subscribe to recieve database updates <---------->//
		yield return StartCoroutine (CreateChannelDoc ("channels"));	//Create channels collection
		yield return StartCoroutine (CreatePlayerDoc ("players"));	//Create channels collection
		yield return StartCoroutine (CreateSessionDoc ("sessions"));	//create sessions collection

		sessionCollection.DidAddRecord += HandleDidAddSessionRecord;

		yield return StartCoroutine (recordGroup.CreateGameObjectCollection ());		//starts up prizmrecordgroup's initialization as well
		yield return StartCoroutine (Subscribe ());		//subscribes to everything for now	//need to fix
	}


	
	IEnumerator callCreatePlayer() {
		Debug.Log ("Calling 'createPlayer' with name: " + playerName + ", and color: " + color);
		
		var methodCall = Meteor.Method<ChannelResponse>.Call ("createPlayer", playerName, color, deviceID, appID, sessionName);
		yield return (Coroutine)methodCall;
		Debug.Log ("createPlayer called");

		if (methodCall.Response.success) {
			Debug.Log ("call to createPlayer succeeded! Message: " + methodCall.Response.message);
			playerID = methodCall.Response.message;
		} else {
			Debug.LogError ("uh oh! createPlayer call failed. PlayerID not set");
		}
	}
	
	//opens a handheld client session, sets clientID and sessionID
	IEnumerator OpenSession(string sessionName) {
		Debug.LogError ("Calling 'openHandheldSession' :");

		var methodCall = Meteor.Method<OpenSessionResponse>.Call ("openHandheldSession", appID, deviceID, userID);
		yield return (Coroutine)methodCall;
		Debug.LogError ("openHandheldSession called");

		if (methodCall.Response.success) {
			Debug.LogError ("call to openHandheldSession succeeded! clientID: " + methodCall.Response.clientID + ", sessionID: " + methodCall.Response.sessionID);
			clientID = methodCall.Response.clientID;
			sessionID = methodCall.Response.sessionID;
		} else {
			Debug.LogError("uh oh! openHandheldSession call failed. sessionID and clientID not set (this is serious)");
		}

		if (readyToBindDDP)
			yield return StartCoroutine (BindDDPConnection ());
		else
			Debug.LogError ("Did not receive DDP Connection ID, but we are past the call to BindDDPConnection()");

	}	

	IEnumerator GameSyncRoutine() {
		Debug.Log ("Adding game sync record handlers");
		channelCollection.DidChangeRecord += HandleDidChangeRecordSync;			//add handler

		//report to tabletop that we are 'paired'
		yield return StartCoroutine(callReportToTabletopClient (clientSyncPosition.ToString()));	//should change record in channels, and trigger handledidchangerecord
		clientSyncPosition++;		//now we are syncing groups
		sessionSyncPosition++;		//the next session is 'allPaired'
	}

	//this recordchange handler only is run during inital game sync phase
	void HandleDidChangeRecordSync (string arg1, ChannelTemplate arg2, IDictionary arg3, string[] arg4)
	{
		Debug.Log ("HH is syncing");
		if (arg2.receiver_id == clientID) {	//if this message applies to us (the receiver_id is us) 
			if ((int)sessionSyncPosition > (int)sessionStatuses.running) {	//when game is running, we can stop setup
				Debug.Log ("done with sync");
				channelCollection.DidChangeRecord -= HandleDidChangeRecordSync;	//remove this handler
			} else if (arg2.payload == sessionSyncPosition.ToString ()) {		//if the channel broadcasts what we are expecting
				StartCoroutine (callReportToTabletopClient (clientSyncPosition.ToString ()));
				clientSyncPosition++;
				sessionSyncPosition++;
			} else {
				Debug.LogError ("Uh oh! Sync routine error, expecting: " + sessionSyncPosition.ToString () + ", recieved: " + arg2.payload + " on client step: " + clientSyncPosition.ToString ());
			}
		} else {	//message not directed to us
			Debug.LogError ("this message is not directed at us, senderID: " + arg2.sender_id + ", receiverID: '" + arg2.receiver_id + "', our clientID: '" + clientID + "'");
		}
	}

	//establishes DDP connection
	IEnumerator BindDDPConnection() {
		Debug.LogError ("in BindDDPConnection()" + DDPConnectionID);
		var methodCall = Meteor.Method<ChannelResponse>.Call ("bindClientToDDPConnection", clientID, DDPConnectionID);	//make clientID the handheld ID later, last parameter is dictionary for intiial statuses

		yield return (Coroutine)methodCall;

		if (methodCall.Response.success) {
			Debug.LogError ("call to bindClientToDDPConnection SUCCEEDED!, response: " + methodCall.Response.message);
		} else {
			Debug.LogError("call to 'bindClientToDDPConnection' did not succeed.");
		}

		Debug.LogError ("out of BindDDPConnection(), beginning game sync routine");
		StartCoroutine(GameSyncRoutine());
	}

	//broadcasts to tabletop over channel
	IEnumerator callReportToTabletopClient(string msg) {
		Debug.LogError ("calling reportToTabletopClient with message: " + msg);

		var methodCall = Meteor.Method<ChannelResponse>.Call ("reportToTabletopClient", sessionID, clientID, msg);
		yield return (Coroutine)methodCall;

		if (methodCall.Response.success) {
			Debug.LogError ("reportToTabletopClient succeeded! Recieved: " + methodCall.Response.message);
		} else {
			Debug.LogError ("Uh oh! reportToTabletopClient failed.  Received: " + methodCall.Response.message);
		}
	}

	//if you call this, you need to remove the record from PrizmRecordGroup.associates list as well
	public IEnumerator RemoveRecord (ExampleObject record) {
		Debug.LogError ("Removing from database: " + record.name + "with UID: " + record.dbEntry._id);

		var methodCall = Meteor.Method<ChannelResponse>.Call ("removeGameObject", record.dbEntry._id);		
		yield return (Coroutine)methodCall;

		if (methodCall.Response.success) {
			Debug.LogError("Successfully removed from database, message: " + methodCall.Response.message);
		} else {	
			Debug.LogError("Error removing " + record.dbEntry._id + " from database");
		}
	}

	//associates clientCollection to meteor's client document
	IEnumerator CreateClientDoc(string recordGroupName) {	
		clientCollection = Meteor.Collection <ClientTemplate>.Create (recordGroupName);	//creates meteor collection
		yield return clientCollection;
		/* Handler for knowing our client's color */

		clientCollection.DidAddRecord += (string id, ClientTemplate document) => {				
			Debug.LogError(string.Format("Client document added:\n{0}", document.Serialize()));
			};

	}

	//associates playerCollection to meteor's channel document
	IEnumerator CreatePlayerDoc(string recordGroupName) {
		playerCollection = Meteor.Collection <PlayerTemplate>.Create (recordGroupName);	//creates meteor collection
		yield return playerCollection;	//waits until collection is finished being created
		/* Add handler for debugging channel adds: */
		/*
		playerCollection.DidAddRecord += (string id, ChannelTemplate document) => {				
			Debug.LogError(string.Format("Player document added:\n{0}", document.Serialize()));
			};
		*/

		//triggered when player receives their color from the tabletop
		//this changes the UI of the chip
		Debug.Log ("recordchangehandler added for player");
		playerCollection.DidChangeRecord += (string arg1, PlayerTemplate arg2, IDictionary arg3, string[] arg4) => {
			Debug.Log ("record changed in playerCollection, the change is: " + arg2._id + ", but our ID is : " + playerID);
			if (arg2._id == playerID) {
				/*
				Debug.Log ("showing the player's token now");
				
				//set color to the same one as on tabletop
				color = (PlayerColor.color)System.Enum.Parse(typeof(PlayerColor.color), arg2.color);
				string colorName = color.ToString();
				Transform foundColor = GameObject.Find("PlayerCircle").transform.FindChild(colorName).transform;
				if (foundColor)
				{
					foundColor.gameObject.SetActive(true);
				}
				*/
				
				//creates client document and opens channels
				if (arg2.sessionID != null) {
					sessionID = arg2.sessionID;
					Debug.Log ("sessionID set: " + sessionID);
					StartCoroutine (OpenSession (sessionName));
				}
			}
		};
	}
	
	//associates channelCollection to meteor's channel document
	IEnumerator CreateChannelDoc(string recordGroupName) {
		channelCollection = Meteor.Collection <ChannelTemplate>.Create (recordGroupName);	//creates meteor collection
		yield return channelCollection;	//waits until collection is finished being created
		/* Add handler for debugging channel adds: */
		/*
		channelCollection.DidAddRecord += (string id, ChannelTemplate document) => {				
			Debug.LogError(string.Format("Channel document added:\n{0}", document.Serialize()));
			};
		channelCollection.DidChangeRecord += (string arg1, ChannelTemplate arg2, IDictionary arg3, string[] arg4) => {
			Debug.Log ("Channel document CHANGED: " + arg2.Serialize());
			};
		*/
	}

	//associates sessionCollection to meteor's sessions document
	IEnumerator CreateSessionDoc(string recordGroupName) {
		sessionCollection = Meteor.Collection <SessionTemplate>.Create (recordGroupName);	//creates meteor collection
		yield return sessionCollection;	//waits until collection is finished being created
		/* Add handler for debugging session adds: */
		/*
		sessionCollection.DidAddRecord += (string id, SessionTemplate document) => {				
			Debug.LogError(string.Format("Session document added:\n{0}", document.Serialize()));
			};
		sessionCollection.DidChangeRecord += (string arg1, ChannelTemplate arg2, IDictionary arg3, string[] arg4) => {
			Debug.Log ("Session document CHANGED: " + arg2.Serialize());
			};
		*/
	}

	//subscribes client to all necessary collections
	//in the future, handheld client will get its own publication
	IEnumerator Subscribe() {
		Debug.Log ("calling Subscribe() with no params"); //with sessionID:" + sessionID + ", clientID: " + clientID);
		var subscription = Meteor.Subscription.Subscribe ("handheldBootstrap");	//channels, clients, sessions tables, create collections
		yield return (Coroutine)subscription;
		Debug.Log ("subscription finished");
	}
	
	//calls a generic method on the meteor server of 'methodName' with parameters 'args'
	//method must return a success bool and a string
	IEnumerator MethodCall(string methodName, string[] args) {	
		var methodCall = Meteor.Method<ChannelResponse>.Call (methodName, args);	
		yield return (Coroutine)methodCall;
		
		if (methodCall.Response.success) {
			Debug.LogError (methodName + " executed successfully! Response: " + methodCall.Response.message);
		} else {
			Debug.LogError (methodName + " did NOT execute successfully.");
		}
	}			

	public void setMeteorURL(string url) {
		meteorURL = "ws://" + url + ":6969/websocket";
		Debug.Log ("meteor url set: " + meteorURL + ":"+url);

	}


	
	private void RefreshHostList()
	{
		MasterServer.RequestHostList(typeName);

	}
	
	void OnMasterServerEvent(MasterServerEvent msEvent)
	{
		if (msEvent == MasterServerEvent.HostListReceived)
			hostList = MasterServer.PollHostList();

		if (!Network.isClient && !Network.isServer)
		{
			if (hostList != null)
			{
				foreach(Transform child in chooseSessionCanvas.transform){
					Destroy (child.gameObject);
				}
				for (int i = 0; i < hostList.Length; i++)
				{

					GameObject SessionButton = Instantiate (chooseSessionButton) as GameObject;
					SessionButton.transform.parent = chooseSessionCanvas.transform;

					Debug.Log ("hostList[i].gameName: " + hostList[i].gameName);
					SessionButton.transform.FindChild("Text").gameObject.GetComponent<Text> ().text = hostList[i].gameName;			//the random name of the session is the new button
					Debug.Log ("hostList[i].gameName: " + hostList[i].gameName);
					HostData tempHost = hostList[i];
					SessionButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => JoinServer (tempHost) );			//fetch server's IP info on click
				}
			}
		}
	}
	
	//is called by a Button UI element so that the player can enter their name
	//sets the UserID and begins the Meteor bootstrapping process
	public void pickUserID() {
		string id = GameObject.Find ("PlayerNameCanvas/InputPlayerName").GetComponent<InputField>().text;
		if (id.Length > 0) {	//id must be at least 1 characters long

			userID = id;
			playerName = id;

			Debug.Log ("setting playerName: " + playerName + "from id: " + id);
			playerNameCanvas.SetActive (false);						//turn off the button ui after picking
			StartCoroutine (GetComponent<BootstrapHH> ().Bootstrap ());							//establish connection to database, create collections and subscriptions
			Debug.Log ("done with pickUserID()");

			StartCoroutine (callCreatePlayer ());
			//create UI player's colored token
			Debug.Log ("Done with Meteor Initialization, waiting for player to be seated on tabletop");
		}
	}
	
	private string GetIP(){
		string strHostName = "";
		strHostName = System.Net.Dns.GetHostName ();
		
		IPHostEntry ipEntry = System.Net.Dns.GetHostEntry (strHostName);
		
		IPAddress[] addr = ipEntry.AddressList;
		
		return addr [addr.Length - 1].ToString ();
	}

	void HandleDidAddSessionRecord(string arg1, SessionTemplate arg2) {
		Debug.Log ("session add detected!!1");
		//chooseSessionButton = new GameObject ();
		if (arg2.name != null)
			Debug.Log ("name of session: " + arg2.name);
		else
			Debug.Log ("session arg2.name as no name :( ");
		//chooseSessionCanvas.AddComponent (chooseSessionButton);
	}


	void OnGUI()
	{
		if (!Network.isClient && !Network.isServer && !readyToBindDDP)
		{
			GUIStyle guiStyle = new GUIStyle("button");
			guiStyle.fontSize = 20;
			if (GUI.Button(new Rect(Screen.width - 200, Screen.height - 100, 200, 100), "Refresh Hosts", guiStyle))
				RefreshHostList();
		}
	}


	//fetches url info, saves it and launches to prompt player's name
	public void JoinServer(HostData hostData)
	{
		Debug.Log ("joining server: " + hostData.gameName);
		sessionName = hostData.gameName;
		Network.Connect(hostData);
		foreach (var host in hostData.ip) {
			Debug.Log (host + ":" + hostData.port + " " );
		}
		
		//saves the meteor DB url
		setMeteorURL (hostData.ip [0]);

		Debug.Log ("showing playername prompt");
		
		//sessionName = name;
		chooseSessionCanvas.SetActive (false);

		playerNameCanvas.SetActive (true);			//let player pick their name
	}
	
	void OnConnectedToServer()
	{
		Debug.Log("Server Joined" + ", our local IP: " + GetIP());
		createMsgLog ("Connected to Server!", 3f);
	}

	void OnDisconnectedFromServer() {
		Debug.Log ("server died, quitting");
		createMsgLog ("Disconnected from Server!\n:(\n(Unity MasterServer)", 7f);
		//Application.Quit ();
	}
}


	
