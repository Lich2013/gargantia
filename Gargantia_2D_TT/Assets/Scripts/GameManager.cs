using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TouchScript;
using TouchScript.Gestures;
using UnityEngine.UI;


public class GameManager : MonoBehaviour {
	public GameObject playerPrefab;
	public GameObject foodPrefab;
	public GameObject boundaryPrefab;
	public GameObject pointPortalPrefab;
	public GameObject bulletPrefab;
	public GameObject smartPiecePrefab;

	[System.NonSerialized]
	public GameObject introducePlayerSparkle;

	[System.NonSerialized]
	public AudioSource audioSource;
	public AudioClip antiGravitySound;
	public AudioClip changeColorSound;
	public AudioClip gravitySoundRepeater;
	public AudioClip pointPortalSoundRepeater;
	public AudioClip superSpawnRateSound;
	public AudioClip rfEventSound;
	public AudioClip humanScreamSound;
	public AudioClip easterEggSound;

	GameObject msgCanvas;
	public Text msgPrefab;

	Camera camera;

	private Vector3 cameraSetPosition;
	private float minPerlin = -10f, maxPerlin = 10f;
	private float shakeDuration = 0.02f;
	private int shakeIterations = 10;
	
	//power that new balls are exploded at
	public float Power = 10.0f;
	private float wallHeight = 2;

	//references to other scripts
	[System.NonSerialized]
	public PrizmRecordGroup recordGroup;
	[System.NonSerialized]
	public BootstrapTT bootstrap;


	private int totalFoodCapacity = 150;	//how many objects can be on the screen at one time
	private float spawnChance = 1.00f;	//increase this for faster gameplay (called every update loop, so keep under 1%
	private float spawnChanceMultiplier = 5.0f;
	private float bulletSpawnChance = 0.40f;	//% chance of spawning a dangerous bullet every createworld()
	private float rfEventChance = 0.10f;	//% chance of rf event
	private int numRFZones = 3;				//number of zones that show up in one rf swing

	private int foodSpawnIndex = 0;
	private int bulletSpawnIndex = 0;

	private bool expectingRF = false;
	private int randomChoice = 0;
	private int numRFEffectsOn = 0;
	private string findName;
	
	[System.NonSerialized]
	public List<GameObject> playerList = new List<GameObject> ();

	void Awake () {
		//uncomment when done
		introducePlayerSparkle = GameObject.Find ("IntroducePlayer");
		recordGroup = GetComponent<PrizmRecordGroup>();
		bootstrap = GetComponent<BootstrapTT> ();
		msgCanvas = GameObject.Find ("MsgCanvas");
		camera = GameObject.Find ("Main Camera").GetComponent<Camera> ();
		cameraSetPosition = camera.transform.position;
		audioSource = GetComponent<AudioSource> ();
	}

	void Start() {
		InitNewWorld ();
		HideRFZones ();

		SpawnNewWorld ();
		SpawnNewWorld ();
		SpawnNewWorld ();
		SpawnNewWorld ();
		SpawnNewWorld ();

		CreateBoundaries ();

		createMsgLog ("Creating World", 4f);
	}

	void InitNewWorld() {
		for (int i = 0; i < totalFoodCapacity; i++) {
			Color color = new Color(Random.value, Random.value, Random.value);
			float randomX = Random.Range (camera.ViewportToWorldPoint (Vector3.zero).x, camera.ViewportToWorldPoint(Vector3.one).x);
			float randomY = Random.Range (camera.ViewportToWorldPoint(Vector3.zero).y, camera.ViewportToWorldPoint(Vector3.one).y);
			
			Vector3 randomPositionInWorld = new Vector3(randomX, randomY, 0);
			var newFood = Instantiate(foodPrefab) as GameObject;
			var tran = newFood.transform;
			var rend = newFood.GetComponent<Renderer>();
			rend.material.SetColor("_Color", color);
			tran.name = "Food";
			tran.SetParent(transform);	//sets empty game mamager object as parent for all the foods
			tran.tag = "Food";
			tran.position = randomPositionInWorld;
			newFood.SetActive(false);
		}

		//init 1/4 as many bullets
		for (int i = 0; i < totalFoodCapacity / 4; i++) {
			float randomX = Random.Range (camera.ViewportToWorldPoint (Vector3.zero).x, camera.ViewportToWorldPoint(Vector3.one).x);
			float randomY = Random.Range (camera.ViewportToWorldPoint(Vector3.zero).y, camera.ViewportToWorldPoint(Vector3.one).y);
			
			Vector3 randomPositionInWorld = new Vector3(randomX, randomY, 0);
			var newBullet = Instantiate(bulletPrefab) as GameObject;
			newBullet.GetComponent<BulletHandler> ().associatedPlayer = null;
			var tran = newBullet.transform;
			var rend = newBullet.GetComponent<Renderer>();
			rend.material.SetColor("_Color", Color.red);
			tran.name = "Bullet";
			tran.tag = "Bullet";
			tran.localScale = new Vector3 (tran.localScale.x * 3, tran.localScale.y * 3, 1);
			tran.SetParent(transform);
			
			var particleTran = tran.GetChild (0);
			particleTran.GetComponent<ParticleSystem> ().startSize = particleTran.GetComponent<ParticleSystem> ().startSize * 3;
			foreach (Transform child in particleTran) {
				child.GetComponent<ParticleSystem>().startSize = child.GetComponent<ParticleSystem>().startSize * 3;
			}
			
			tran.position = randomPositionInWorld;
			newBullet.SetActive(false);
		}

	}


	void ShowRFEvent() {
		expectingRF = true;
		createMsgLog ("Smart Touch Opportunity!");
		StartCoroutine(ShakeCamera (shakeDuration, shakeIterations));

		audioSource.PlayOneShot (rfEventSound);
	
		int totalAntennas = GameObject.Find ("RFID_Zones_24in").transform.childCount;
		for (int i = 0; i < numRFZones; i++) {
			GameObject.Find ("RFID_Zones_24in").transform.GetChild ((int)Random.Range (0, totalAntennas - 1)).gameObject.SetActive (true);
		}

		StartCoroutine(HideRFZonesAfterSeconds (5));
	}

	IEnumerator HideRFZonesAfterSeconds(float time) {
		yield return new WaitForSeconds (time);
		HideRFZones ();
	}

	void HideRFZones() {
		audioSource.Stop ();
		expectingRF = false;
		Transform zoneParent = GameObject.Find ("RFID_Zones_24in").transform;
		for (int i = 0; i < zoneParent.childCount; i++) {
			if (zoneParent.GetChild(i).gameObject.activeSelf) {
				zoneParent.GetChild(i).gameObject.SetActive(false);
			}
		}
	}



	void FixedUpdate(){


		if ((Random.Range (0.0f, 100.0f) < spawnChance))	//gives 1% chance ever update loop to spawn another world
			SpawnNewWorld ();

		if ((Random.Range (0.0f, 100.0f) < rfEventChance))
			ShowRFEvent ();


		//backup way to spawn players
		if (Input.GetKeyDown ("q")) {
			CreateNewPlayer ("DannyBoi!", Color.cyan, "Hxyso4l1nx903ad");
		}
		if (Input.GetKeyDown ("w")) {
			CreateNewPlayer ("JimmySon!", Color.green, "xc63oic0o9lhaz");
		}
		if (Input.GetKeyDown ("e")) {
			CreateNewPlayer ("Michael-San!", Color.magenta, "77xwbGDUbwui127");
		}

		if (Input.GetKeyDown ("c")) {
			Debug.Log ("testing new player spawn");
			introducePlayerSparkle.GetComponent<ParticleSystem>().Play();
		}

		if (Input.GetKeyDown ("r")) {
			Debug.Log ("demo rfid event");
			ShowRFEvent();
		}


		if (Input.GetKeyDown ("h")) {
			HideRFZones();
		}

		if (Input.GetKeyDown ("g")) {
			ActivateGravityWell(3,  2);
		}

		if (Input.GetKeyDown ("h")) {
			DisableGravityWell();
		}

		if (Input.GetKeyDown ("t")) {
			ActivateAntiGravityWell(4, 3);
		}

		if (Input.GetKeyDown ("y")) {
			DisableAntiGravityWell();
		}

		if (Input.GetKeyDown ("u")) {
			ActivateChangeAllColors(4, 3);
		}
		
		if (Input.GetKeyDown ("i")) {
			DisableChangeAllColors();
		}

		if (Input.GetKeyDown ("j")) {
			ActivateSuperSpawnRate(4, 3);
		}
		
		if (Input.GetKeyDown ("k")) {
			DisableSuperSpawnRate();
		}

	}
	

	public void ActivateGravityWell(float xCoord, float yCoord) {
		if (expectingRF) {
			numRFEffectsOn++;
			HideRFZones ();
			createMsgLog ("Gravity Well Activated!");

			Vector3 positionInWorld = camera.ViewportToWorldPoint(new Vector3(xCoord, yCoord, 0));
			var sPiece = Instantiate (smartPiecePrefab) as GameObject;
			sPiece.tag = "GravityWell";
			sPiece.transform.position = positionInWorld;

			audioSource.loop = true;
			audioSource.clip = gravitySoundRepeater;
			audioSource.Play ();

			for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild (i).tag == "Food") {
					transform.GetChild (i).GetComponent<GravityEffect> ().gravityWellLocation = sPiece.transform.position;
					transform.GetChild (i).GetComponent<GravityEffect> ().enabled = true;
				}
			}
		}
	}

	public void DisableGravityWell() {
		if (numRFEffectsOn > 0) {
			numRFEffectsOn--;
			createMsgLog ("Gravity Well Deactivated");
			StartCoroutine (ShakeCamera (shakeDuration, shakeIterations));

			for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild (i).tag == "Food") {
					transform.GetChild (i).GetComponent<GravityEffect> ().enabled = false;
					transform.GetChild (i).GetComponent<Rigidbody2D>().velocity = (new Vector2(Random.value, Random.value) * 2);
				}
			}
			foreach (GameObject gameObj in GameObject.FindGameObjectsWithTag("GravityWell")) {
				Destroy (gameObj);
			}
		}
	}

	public void ActivateAntiGravityWell(float xCoord, float yCoord) {
		if (expectingRF) {
			numRFEffectsOn++;
			HideRFZones ();
			createMsgLog ("Anti-Gravity Well Activated!");
			
			
			Vector3 positionInWorld = camera.ViewportToWorldPoint(new Vector3(xCoord, yCoord, 0));
			var sPiece = Instantiate (smartPiecePrefab) as GameObject;
			sPiece.tag = "AntiGravity";
			sPiece.transform.position = positionInWorld;
			
			AudioSource.PlayClipAtPoint(antiGravitySound, positionInWorld);
			
			for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild (i).tag == "Food") {
					transform.GetChild (i).GetComponent<AntiGravityEffect> ().antiGravityWellLocation = sPiece.transform.position;
					transform.GetChild (i).GetComponent<AntiGravityEffect> ().enabled = true;
				}
			}
		}
	}
	
	public void DisableAntiGravityWell() {
		if (numRFEffectsOn > 0) {
			numRFEffectsOn--;
			createMsgLog ("Anti-Gravity Well Deactivated");
			StartCoroutine (ShakeCamera (shakeDuration, shakeIterations));
		
			for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild (i).tag == "Food") {
					transform.GetChild (i).GetComponent<AntiGravityEffect> ().enabled = false;
				}
			}
			foreach (GameObject gameObj in GameObject.FindGameObjectsWithTag("AntiGravity")) {
				Destroy (gameObj);
			}
		}
	}
	
	public void ActivateSuperSpawnRate(float xCoord, float yCoord) {
		if (expectingRF) {
			numRFEffectsOn++;
			HideRFZones ();
			createMsgLog ("Super Spawn Rate!");

			audioSource.loop = false;
			audioSource.clip = superSpawnRateSound;
			audioSource.Play();
			
			Vector3 positionInWorld = camera.ViewportToWorldPoint(new Vector3(xCoord, yCoord, 0));
			var sPiece = Instantiate (smartPiecePrefab) as GameObject;
			sPiece.tag = "SuperSpawn";
			sPiece.transform.position = positionInWorld;
			
			spawnChance = spawnChance * spawnChanceMultiplier;
		}
	}
	
	public void DisableSuperSpawnRate() {
		if (numRFEffectsOn > 0) {
			numRFEffectsOn--;
			createMsgLog ("Spawn Rate Normalized");
			StartCoroutine (ShakeCamera (shakeDuration, shakeIterations));
		
			spawnChance = spawnChance / spawnChanceMultiplier;
		
			foreach (GameObject gameObj in GameObject.FindGameObjectsWithTag("SuperSpawn")) {
				Destroy (gameObj);
			}
		}
	}

	public void ActivateChangeAllColors(float xCoord, float yCoord) {
		if (expectingRF) {
			numRFEffectsOn++;
			HideRFZones ();
			createMsgLog ("Colors Changed!");

			Vector3 positionInWorld = camera.ViewportToWorldPoint(new Vector3(xCoord, yCoord, 0));
			var sPiece = Instantiate (smartPiecePrefab) as GameObject;
			sPiece.tag = "ChangeColors";
			sPiece.transform.position = positionInWorld;


			audioSource.PlayOneShot(changeColorSound);

			Color col = new Color(Random.value, Random.value, Random.value);
			for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild (i).tag == "Food") {
					transform.GetChild (i).GetComponent<Renderer>().material.color = col;
				}
			}
		}
	}
	
	public void DisableChangeAllColors() {
		if (numRFEffectsOn > 0) {
			numRFEffectsOn--;
			createMsgLog ("Colors Normalized");
			StartCoroutine (ShakeCamera (shakeDuration, shakeIterations));

			for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild (i).tag == "Food") {
					transform.GetChild (i).GetComponent<Renderer> ().material.color = new Color (Random.value, Random.value, Random.value);
				}
			}

			foreach (GameObject gameObj in GameObject.FindGameObjectsWithTag("SmartPiece")) {
				Destroy (gameObj);
			}
		}
	}

	public void ActivateRandom(float xCoord, float yCoord) {
		if (expectingRF) {
			int randomNum = (int)(Random.value * 100);
			randomChoice = randomNum % 4;

			switch (randomChoice) {
			case 0:
				ActivateGravityWell (xCoord, yCoord);
				break;
			case 1:
				ActivateSuperSpawnRate (xCoord, yCoord);
				break;
			case 2:
				ActivateAntiGravityWell (xCoord, yCoord);
				break;
			case 3:
				ActivateChangeAllColors (xCoord, yCoord);
				break;
			default:
				Debug.LogError ("ActivateRandom() did not choose something!!");
				break;
			}
		}
	}

	public void DisableRandom() {
		switch (randomChoice) {
		case 0:
			DisableGravityWell ();
			break;
		case 1:
			DisableSuperSpawnRate ();
			break;
		case 2:
			DisableAntiGravityWell ();
			break;
		case 3:
			DisableChangeAllColors ();
			break;
		default:
			Debug.LogError("DisableRandom() did not disable anything!");
			break;
		}
		
	}
	
	private bool scoreChartExists(PrizmRecord scoreRecord) {
		return (scoreRecord.dbEntry.playersName == findName);
	}


	public void CreateNewPlayer(string playerName, Color playerColor = new Color(), string player_id = "oink", int timeToLive = 5) {
		findName = playerName;

		Debug.Log ("new player created");

		GameObject newPlayer = Instantiate (playerPrefab) as GameObject;
		introducePlayerSparkle.transform.position = newPlayer.transform.position;
		introducePlayerSparkle.GetComponent<ParticleSystem>().Play();
		StartCoroutine(KillPlayerAfterTime(newPlayer, timeToLive));	//testing out 20 min. gameplay

		//if record exists in database for players score card, use that
		//otherwise, create a new one
		if (recordGroup.associates.Find (scoreChartExists) != null) {
			Debug.Log ("record exists!" + recordGroup.associates.Find (scoreChartExists).ToString ());
			newPlayer.GetComponent<PlayerHandler> ().scoreRecord = (Stats) recordGroup.associates.Find (scoreChartExists);
		} else {

			Stats record = new Stats ();
			Debug.Log ("name: " + record.ToString ());
			record.Awake ();
			record.dbEntry.playersID = player_id;
			record.dbEntry.playersName = playerName;
			record.dbEntry.timePlayed = 0;
			record.dbEntry.totalScore = 0;

			StartCoroutine(recordGroup.AddRecord (record));
			StartCoroutine(recordGroup.Sync (record));

			newPlayer.GetComponent<PlayerHandler> ().scoreRecord = record;
		}


		newPlayer.GetComponent<PlayerHandler>().initializePlayer(playerName, playerColor, player_id);


		//add player to list of players
		playerList.Add (newPlayer);
	}

	private IEnumerator KillPlayerAfterTime(GameObject player, int minutesToPlay) {
		yield return new WaitForSeconds (minutesToPlay * 60);
		Destroy(GameObject.Find (player.name + "PointPortal"));
		player.GetComponent<PlayerHandler> ().scoreRecord.dbEntry.timePlayed += minutesToPlay;
		player.GetComponent<PlayerHandler> ().scoreRecord.needsUpdate = true;
		Debug.Log ("time played: " + player.GetComponent<PlayerHandler> ().scoreRecord.dbEntry.timePlayed.ToString());
		StartCoroutine(recordGroup.SyncAll ());
		playerList.Remove (player);
		Destroy (player);
		introducePlayerSparkle.transform.position = player.transform.position;
		introducePlayerSparkle.GetComponent<ParticleSystem> ().Play ();
	}

	//spawns a portal for a specific player for them to cash in their points
	public void SpawnPlayerPointPortal(GameObject player) {
		if (!player.GetComponent<PlayerHandler> ().hasPointPortal) {
			float randomX = Random.Range (camera.ViewportToWorldPoint (Vector3.zero).x, camera.ViewportToWorldPoint (Vector3.one).x);
			float randomY = Random.Range (camera.ViewportToWorldPoint (Vector3.zero).y, camera.ViewportToWorldPoint (Vector3.one).y);
		
			Vector3 randomPositionInWorld = new Vector3 (randomX, randomY, -1);
			GameObject pointPortal = Instantiate (pointPortalPrefab) as GameObject;
			pointPortal.GetComponent<ParticleSystem> ().startColor = player.GetComponent<Renderer> ().material.color;
			var tran = pointPortal.transform;

			audioSource.PlayOneShot(pointPortalSoundRepeater);

			tran.GetChild (0).GetComponent<ParticleSystem> ().startColor = player.GetComponent<Renderer> ().material.color;
			tran.GetChild (1).GetComponent<ParticleSystem> ().startColor = player.GetComponent<Renderer> ().material.color;
			tran.GetChild (2).GetComponent<ParticleSystem> ().startColor = player.GetComponent<Renderer> ().material.color;

			var rend = pointPortal.GetComponent<Renderer> ();
			rend.material.SetColor ("_SpecColor", player.GetComponent<Renderer> ().material.color);
			rend.sortingLayerName = "foreground";
			rend.sortingOrder = 2;
			tran.name = player.name + "PointPortal";
			tran.tag = "PointPortal";
			tran.position = randomPositionInWorld;
			player.GetComponent<PlayerHandler> ().hasPointPortal = true;
		}

	}


	public IEnumerator ShakeCamera(float innerDuration = 0.02f, int iterations = 10) {
		Vector3 newPos;
		newPos = cameraSetPosition;
		Debug.Log (newPos.ToString ());

		for (int i = 0; i < iterations; i++) {

			newPos.x = Mathf.PerlinNoise (newPos.x * minPerlin, newPos.x * maxPerlin) - 0.5f;
			newPos.y = Mathf.PerlinNoise (newPos.y * minPerlin, newPos.y * maxPerlin) - 0.5f;
			camera.transform.position = newPos;

			yield return new WaitForSeconds (innerDuration);
		}

		camera.transform.position = cameraSetPosition;

		yield return null;
	}
	
	
	public void createMsgLog(string message, float timer = 2f){
		if(msgCanvas.transform.FindChild("mesg")){
			msgCanvas.transform.FindChild("mesg").GetComponent<selfDestructMessage>().killMyself(message, timer);
		}
		else{
			Text newText = Instantiate (msgPrefab) as Text;
			newText.transform.position.Set (0, 0, 0);
			newText.transform.SetParent(msgCanvas.transform);
			newText.gameObject.name = "mesg";
			newText.GetComponent<selfDestructMessage> ().killMyself (message, timer);
		}
	}

	//generates lots of balls to fill the world
	public void SpawnNewWorld() {

		if (Random.value < bulletSpawnChance) {
			SpawnBullet();
		}
	
		//Debug.Log ("color of food: " + color.linear.);
	
		//create 7 new foods
		for (int i = 0; i < 7; i++)
		{
			if ((foodSpawnIndex + 1) < totalFoodCapacity) {
				foodSpawnIndex++;
			}
			else {
				foodSpawnIndex = 0;
			}
			var newFood = transform.GetChild(foodSpawnIndex).gameObject;
			if (!newFood.gameObject.activeSelf) {
				newFood.SetActive(true);
				Color color = new Color(Random.value, Random.value, Random.value);
				float randomX = Random.Range (camera.ViewportToWorldPoint (Vector3.zero).x, camera.ViewportToWorldPoint(Vector3.one).x);
				float randomY = Random.Range (camera.ViewportToWorldPoint(Vector3.zero).y, camera.ViewportToWorldPoint(Vector3.one).y);

				Vector3 randomPositionInWorld = new Vector3(randomX, randomY, 0);
				var tran = newFood.transform;
				var rend = newFood.GetComponent<Renderer>();
				rend.material.SetColor("_Color", color);
				tran.position = randomPositionInWorld;
			}
		}
	}

	private void SpawnBullet() {

		if ((bulletSpawnIndex + 1) < (totalFoodCapacity / 4)) {
			bulletSpawnIndex++;
		}
		else {
			bulletSpawnIndex = 0;
		}

		var newBullet = transform.GetChild (bulletSpawnIndex + totalFoodCapacity).gameObject;
		newBullet.SetActive (true);

		float randomX = Random.Range (camera.ViewportToWorldPoint (Vector3.zero).x, camera.ViewportToWorldPoint(Vector3.one).x);
		float randomY = Random.Range (camera.ViewportToWorldPoint(Vector3.zero).y, camera.ViewportToWorldPoint(Vector3.one).y);
		
		Vector3 randomPositionInWorld = new Vector3(randomX, randomY, 0);

		newBullet.GetComponent<BulletHandler> ().associatedPlayer = null;
		var tran = newBullet.transform;

		tran.position = randomPositionInWorld;
		newBullet.GetComponent<BulletHandler> ().ChaosBullet ();
	}
	
	//creates walls so balls can't escape world
	public void CreateBoundaries() {

		Vector3 lowerLeft = camera.ViewportToWorldPoint (new Vector3 (0, 0, 10));
		Vector3 lowerRight = camera.ViewportToWorldPoint (new Vector3 (1, 0, 10));
		Vector3 upperLeft = camera.ViewportToWorldPoint (new Vector3 (0, 1, 10));
		Vector3 upperRight = camera.ViewportToWorldPoint (new Vector3 (1, 1, 10));

		float width = lowerRight.x - lowerLeft.x;
		float height = upperRight.y - lowerRight.y; 


		Vector3 bottom = (lowerLeft + lowerRight ) / 2;
		Vector3 top = (upperLeft + upperRight ) / 2;
		Vector3 left = (upperLeft + lowerLeft ) / 2;
		Vector3 right = (lowerRight + upperRight ) / 2;


		GameObject bottomBound = Instantiate( boundaryPrefab) as GameObject;
		bottomBound.transform.position = bottom;
		bottomBound.transform.localScale = new Vector3 (width, 0.1f, 0);

		GameObject topBound = Instantiate( boundaryPrefab) as GameObject;
		topBound.transform.position = top;
		topBound.transform.localScale = new Vector3 (width, 0.1f, 0);

		GameObject leftBound = Instantiate( boundaryPrefab) as GameObject;
		leftBound.transform.position = left;
		leftBound.transform.localScale = new Vector3 (0.1f, height, 0);

		GameObject rightBound = Instantiate( boundaryPrefab) as GameObject;
		rightBound.transform.position = right;
		rightBound.transform.localScale = new Vector3 (0.1f, height, 0);


	}


	//when someone folds
	//uncomment this when done

	public void HandleDidChangeRecord (string arg1, DatabaseEntry arg2, IDictionary arg3, string[] arg4)
	{
		Debug.Log ("Record Changed: " + arg2 + "score: " + arg2.totalScore + " playerName: " + arg2.playersName + " _id: " + arg2._id + "time: " + arg2.timePlayed);
	}


	//when someone quits the game
	public void HandleDidLosePlayer(string id) {
		foreach(GameObject obj in Object.FindObjectsOfType(typeof(GameObject))){
			if(obj.tag == "Player"){
				if(obj.gameObject.name == id){
					Destroy(obj);
				}
			}
		}
		Debug.Log ("player lost connection, ID is: " + id);
	}

	void OnApplicationQuit(){
		StopAllCoroutines ();
		reset ();
		//uncomment this when done prototyping
	}

	public void HandleDidChangePlayerRecord (string arg1, PlayerTemplate arg2, IDictionary arg3, string[] arg4)
	{
		if (arg2.active == false) {
			playerList.Find (x => x.GetComponent<PlayerHandler>().playerID.Equals(arg2.playerID)).SetActive(false);
		} else if (arg2.active == true) {
			playerList.Find (x => x.GetComponent<PlayerHandler>().playerID.Equals(arg2.playerID)).SetActive(true);
		}
	}

	public void reset(){
		StartCoroutine (resetGame ());
	}

	IEnumerator resetGame() {
		var methodCall = Meteor.Method<ChannelResponse>.Call ("endTabletopSession", GameObject.Find ("GameManager").GetComponent<TabletopInitialization>().sessionID);
		yield return (Coroutine)methodCall;
	}

}