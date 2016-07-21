using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TouchScript;
using TouchScript.Gestures;
using UnityEngine.UI;


public class GameManager : MonoBehaviour {
	public GameObject playerPrefab;
	public GameObject foodPrefab;

	GameObject msgCanvas;
	public Text msgPrefab;

	Camera camera;
	
	//power that new balls are exploded at
	public float Power = 10.0f;
	private float wallHeight = 2;

	//references to other scripts
	[HideInInspector]
	public PrizmRecordGroup recordGroup; 
	[HideInInspector]
	public BootstrapTT bootstrap;

	//directions that balls explode in when world is created
	private Vector3[] directions =
	{
		new Vector3(1, 2, 1),
		new Vector3(-1, 2, 1),
		new Vector3(-1, 2, -1),
		new Vector3(1, 2, -1),
		new Vector3(1, 2, 1),
		new Vector3(-1, 2, 1),
		new Vector3(-1, 2, -1),
		new Vector3(1, 2, -1)
	};


	void Awake () {
		recordGroup = GetComponent<PrizmRecordGroup>();
		bootstrap = GetComponent<BootstrapTT> ();
		msgCanvas = GameObject.Find ("MsgCanvas");
		camera = GameObject.Find ("MainCamera").GetComponent<Camera> ();
	}

	void Start() {

		CreateNewWorld ();
		CreateBoundaries ();

		createMsgLog ("Creating World", 4f);
	}

	int seconds = 0;
	void Update(){

		seconds++;
		if (seconds % 100 == 0)
			CreateNewWorld ();

		//backup way to spawn players
		if (Input.GetKeyDown ("q")) {
			CreateNewPlayer ("DannyBoi!", "cyan", "Hxyso4l1nx903ad");
		}
		if (Input.GetKeyDown ("w")) {
			CreateNewPlayer ("JimmySon!", "green", "xc63oic0o9lhaz");
		}
		if (Input.GetKeyDown ("e")) {
			CreateNewPlayer ("Michael-San!", "magenter", "77xwbGDUbwui127");
		}

		if (Input.GetKeyDown ("c")) {
			Debug.Log ("creating world");
			CreateNewWorld();
		}
	}

	public void CreateNewPlayer(string playerName, string playerColor = "red", string player_id = "oink") {
		Debug.Log ("new player created");
		GameObject newPlayer = Instantiate (playerPrefab) as GameObject;

		//Color randomColor = new Color(Random.value, Random.value, Random.value);

		newPlayer.GetComponent<playerHandler>().initializePlayer(playerName, playerColor, player_id);
		//newPlayer.GetComponent<playerHandler>().initializePlayer(playerName, randomColor, player_id);
	}

	public void createMsgLog(string message, float timer = 2f){
		if(msgCanvas.transform.FindChild("mesg")){
			msgCanvas.transform.FindChild("mesg").GetComponent<selfDestructMessage>().killMyself(message, timer);
		}
		else{
			Text newText = Instantiate (msgPrefab) as Text;
			//newText.transform.position.Set (0, 0, 0);
			newText.transform.SetParent(msgCanvas.transform);
			newText.gameObject.name = "mesg";
			newText.GetComponent<selfDestructMessage> ().killMyself (message, timer);
		}
	}

	//generates lots of balls to fill the world
	public void CreateNewWorld() {

			Color color = new Color(Random.value, Random.value, Random.value);
			// break this cube into 8 parts
			for (int i = 0; i < 8; i++)
			{
				var obj = Instantiate(foodPrefab) as GameObject;
				var trnsfrm = obj.transform;
				trnsfrm.name = "Food";
				trnsfrm.localScale = 0.5f * transform.localScale;
				trnsfrm.position = transform.TransformPoint(directions[i] / 4);
				trnsfrm.GetComponent<Rigidbody2D>().AddForce(Power * Random.insideUnitSphere, ForceMode2D.Impulse);
				//trnsfrm.GetComponent<Rigidbody2D>().AddForce(ForceMo
				trnsfrm.GetComponent<Renderer>().material.color = color;
			}
	}

	//creates walls so balls can't escape world
	public void CreateBoundaries() {

		Vector3 lowerLeft = camera.ViewportToWorldPoint (new Vector3 (0, 0, 10));
		Vector3 lowerRight = camera.ViewportToWorldPoint (new Vector3 (1, 0, 10));
		Vector3 upperLeft = camera.ViewportToWorldPoint (new Vector3 (0, 1, 10));
		Vector3 upperRight = camera.ViewportToWorldPoint (new Vector3 (1, 1, 10));

		float width = lowerRight.x - lowerLeft.x;
		float height = upperRight.z - lowerRight.z; 


		Vector3 bottom = (lowerLeft + lowerRight ) / 2;
		Vector3 top = (upperLeft + upperRight ) / 2;
		Vector3 left = (upperLeft + lowerLeft ) / 2;
		Vector3 right = (lowerRight + upperRight ) / 2;


		GameObject bottomBound = GameObject.CreatePrimitive(PrimitiveType.Cube);
		bottomBound.transform.position = bottom;
		bottomBound.transform.localScale = new Vector3 (width, wallHeight, 0.1f);

		GameObject topBound = GameObject.CreatePrimitive(PrimitiveType.Cube);
		topBound.transform.position = top;
		topBound.transform.localScale = new Vector3 (width, wallHeight, 0.1f);

		GameObject leftBound = GameObject.CreatePrimitive(PrimitiveType.Cube);
		leftBound.transform.position = left;
		leftBound.transform.localScale = new Vector3 (0.1f, wallHeight, height);

		GameObject rightBound = GameObject.CreatePrimitive(PrimitiveType.Cube);
		rightBound.transform.position = right;
		rightBound.transform.localScale = new Vector3 (0.1f, wallHeight, height);


	}


	//when someone folds
	public void HandleDidChangeRecord (string arg1, DatabaseEntry arg2, IDictionary arg3, string[] arg4)
	{
		Debug.Log ("Record Changed: " + arg2 + "location: " + arg2.location + " uid: " + arg2._id + " _id: " + arg2._id + "number: " + arg2.number);
	}

	//when someone quits the game
	public void HandleDidLosePlayer(string id) {
		foreach(GameObject obj in Object.FindObjectsOfType(typeof(GameObject))){
			if(obj.tag == "Player"){
				if(obj.GetComponent<playerHandler>().playerID == id){
					Destroy(obj);
				}
			}
		}
		Debug.Log ("player lost connection, ID is: " + id);
	}

	void OnApplicationQuit(){
		reset ();
	}
	
	public void reset(){
		StartCoroutine (resetGame ());
	}

	IEnumerator resetGame() {
		var methodCall = Meteor.Method<ChannelResponse>.Call ("endTabletopSession", GameObject.Find ("GameManager").GetComponent<TabletopInitialization>().sessionID);
		yield return (Coroutine)methodCall;
	}
}