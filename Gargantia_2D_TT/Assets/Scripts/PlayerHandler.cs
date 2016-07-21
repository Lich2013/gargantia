
//#define CONTROL_SCHEME_RELATIVE


#define CONTROL_SCHEME_ABSOLUTE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TouchScript;
using TouchScript.Gestures;

public class PlayerHandler : MonoBehaviour {

	public GameObject bulletPrefab;

	public AudioClip simpleShootSound;
	public AudioClip startChargeSound;
	public AudioClip endChargeSound;
	public AudioClip growSound;
	public AudioClip shrinkSound;
	public AudioClip cashPointsSound;

	public Stats scoreRecord;
	public Text localScore;

	private GameManager gameManager;

	private Color playersColor;
	private Rigidbody2D rb;

	[System.NonSerialized]
	public bool isBoundToSession = false;

	[System.NonSerialized]
	public string playerID;


	private static float bulletSpeed = 8.0f;
	private static float acceleration = 2.5f;
	private static float maxSpeed = 2.0f;
	private static float rotateSpeed = 2.0f;
	[System.NonSerialized]
	public static float growthRate = 0.01f;	//responsible for player's size AND spectrumstrength increase rate AND bullet growth rate

	private static float maxSize = 0.25f;
	private static float minSize = 0.05f;

	[System.NonSerialized]
	public Color thisColor;
	[System.NonSerialized]
	public bool hasPointPortal = false;
	
	private int shotsFired = 0;
	private int ammoBelt = 20;
	private float spectrumStrength = 1.4f;	//what the range of colors a player can consume is 
	private bool chargingShot = false;
	private List<GameObject> magazine;

	public float lastVelocityCommand = 0;
	public float lastTurnCommand = 0;

	public Vector3 lastAbsoluteDirection = new Vector3(0, 0, 0);

	void Start(){

		localScore = transform.FindChild ("LocalScoreBoard").FindChild ("LocalScore").GetComponent<Text>();
		localScore.color = Color.red;
		SetLocalScore ();
		rb = GetComponent<Rigidbody2D>();
		gameManager = GameObject.Find ("GameManager").GetComponent<GameManager> ();

		thisColor = GetComponent<Renderer> ().material.color;
		magazine = new List<GameObject> ();
		InitAmmoMagazine ();
	}

	void FixedUpdate() {
		//reset angularvelocity to stabilize
		rb.angularVelocity = 0f;

		/*
		if (Input.GetKey(KeyCode.UpArrow)) {
			MoveForward(1);
		} 
		if (Input.GetKey (KeyCode.DownArrow)) {
			MoveForward(-1);
		}
		if (Input.GetKey (KeyCode.LeftArrow)) {
			TurnRight(-1);
		}
		if (Input.GetKey (KeyCode.RightArrow)) {
			TurnRight(1);
		}

		if (Input.GetKeyDown (KeyCode.Space)) {
			SimpleShoot();
		}

		if (Input.GetKeyDown (KeyCode.G)) {
			GrowPlayer();
		}

		*/

#if CONTROL_SCHEME_RELATIVE
		MoveForward(lastVelocityCommand);
		TurnRight(lastTurnCommand);
#endif

#if CONTROL_SCHEME_ABSOLUTE
		GoInAbsoluteDirection(lastAbsoluteDirection, lastVelocityCommand);
#endif

	}

	private void SetLocalScore() {
		localScore.text = ((int)(GetPlayerSize () * 10000)).ToString();
	}

	public void GoInAbsoluteDirection(Vector3 direction, float forcePercentage) {
		//transform.Rotate(direction);
		transform.localEulerAngles = direction;

		rb.AddRelativeForce (Vector2.up * PlayerHandler.acceleration * forcePercentage);
		if (GetSpeed() >= PlayerHandler.maxSpeed) {
			rb.velocity = rb.velocity.normalized * PlayerHandler.maxSpeed;
		}
	}


	public void InitAmmoMagazine() {
		for (int i = 0; i < ammoBelt; i++) {
			GameObject bullet = Instantiate (bulletPrefab, transform.position, transform.rotation) as GameObject;
			bullet.GetComponent<Renderer> ().material.color = GetComponent<Renderer> ().material.color;
			bullet.GetComponent<BulletHandler> ().associatedPlayer = this.gameObject;
			bullet.SetActive(false);
			magazine.Add(bullet);
		}
	}

	public void MoveForward(float percentage) {
		rb.AddRelativeForce (Vector2.up * PlayerHandler.acceleration * percentage);
		if (GetSpeed() >= PlayerHandler.maxSpeed) {
			rb.velocity = rb.velocity.normalized * PlayerHandler.maxSpeed;
		}
	}

	public void MoveBackward(float percentage) {
		rb.AddRelativeForce (Vector2.down * PlayerHandler.acceleration * percentage);
		if (GetSpeed() > PlayerHandler.maxSpeed) {
			rb.velocity = rb.velocity.normalized * PlayerHandler.maxSpeed;
		}
	}

	public void TurnLeft(float percentage) {
		transform.Rotate (new Vector3 (0, 0, 1) * PlayerHandler.rotateSpeed * percentage);
	}

	public void TurnRight(float percentage) {
		transform.Rotate (new Vector3 (0, 0, -1) * PlayerHandler.rotateSpeed * percentage);
	}

	private float GetVelocity() {
			return Mathf.Sqrt (rb.velocity.x * rb.velocity.x + rb.velocity.y * rb.velocity.y);
	}

	private float GetSpeed() {
			return Mathf.Abs(Mathf.Sqrt (rb.velocity.x * rb.velocity.x + rb.velocity.y * rb.velocity.y));
	}


	public void initializePlayer(string name, Color clr, string id){
		changeColorTo (clr);
		changeName (name);
		changeID (id);
	}

	private void changeID(string i){
		playerID = i;
	}
	

	private void changeName(string n){
		gameObject.name = n;
	}

	public void GrowPlayer() {
		spectrumStrength += PlayerHandler.growthRate;
		if (transform.localScale.x < PlayerHandler.maxSize && transform.localScale.y < PlayerHandler.maxSize) {
			AudioSource.PlayClipAtPoint (growSound, transform.position);
			transform.localScale = new Vector3 (transform.localScale.x + PlayerHandler.growthRate, transform.localScale.y + PlayerHandler.growthRate, 1);
			SetLocalScore();
		} else {
			localScore.transform.localScale = new Vector3(0.13f, 0.13f, 1);
			gameManager.SpawnPlayerPointPortal (this.gameObject);
		}
	}



	public void CashInPoints(float amtPoints) {
		StartCoroutine(gameManager.ShakeCamera (0.02f, 5));

		int total = (int) (amtPoints * 10000);
		gameManager.createMsgLog (name + " scored " + total.ToString () + " points!");
		AudioSource.PlayClipAtPoint (cashPointsSound, transform.position, 1f);
		localScore.transform.localScale = new Vector3(0.05f, 0.05f, 1);
		scoreRecord.dbEntry.totalScore += total;
		scoreRecord.needsUpdate = true;
		StartCoroutine(GameObject.Find ("GameManager").GetComponent<PrizmRecordGroup> ().Sync (scoreRecord));
	}

	public bool ShrinkPlayer() {
		if (transform.localScale.x > PlayerHandler.minSize && transform.localScale.y > PlayerHandler.minSize) {
			AudioSource.PlayClipAtPoint(shrinkSound, transform.position);
			transform.localScale = new Vector3 (transform.localScale.x - growthRate, transform.localScale.y - growthRate, 1);
			spectrumStrength -= growthRate;
			SetLocalScore();
			return true;
		} else {
			return false;
		}
	}

	public void ChargePowerShot() {
		if (!chargingShot) {
			chargingShot = true;

			AudioSource.PlayClipAtPoint(startChargeSound, transform.position);

			shotsFired++;
			if (shotsFired + 1 >= ammoBelt) {
				shotsFired = 0;
			}
			GameObject bullet = magazine [shotsFired];
			if (!bullet.activeSelf) {

				bullet.GetComponent<BulletHandler> ().ResetSize ();
				bullet.transform.position = transform.position + transform.up * transform.localScale.x * 5.0f;
				bullet.transform.SetParent (transform);
				bullet.SetActive (true);
				bullet.GetComponent<BulletHandler> ().StartCharging ();
			}
		}
	}

	public void FirePowerShot() {
		chargingShot = false;

		AudioSource.PlayClipAtPoint (endChargeSound, transform.position);

		GameObject bullet = magazine[shotsFired];
		bullet.transform.SetParent (null);
		//bullet.transform.rotation = transform.rotation;
		bullet.GetComponent<Rigidbody2D> ().velocity = transform.up * PlayerHandler.bulletSpeed;
		bullet.GetComponent<BulletHandler> ().AutoExpire ();
	}

	public void ShrinkPlayer(float customAmount) {
		if (transform.localScale.x > minSize && transform.localScale.y > minSize) {
			AudioSource.PlayClipAtPoint(shrinkSound, transform.position);

			transform.localScale = new Vector3 (transform.localScale.x - customAmount, transform.localScale.y - customAmount, 1);
			spectrumStrength -= growthRate;
			SetLocalScore();
		}

			//if we shrank too much, rectify the situation
		if (transform.localScale.x < minSize && transform.localScale.y < minSize) {
			transform.localScale = new Vector3 (minSize, minSize, 1);
		}
	}

	public void SimpleShoot() {
		if (!chargingShot) {
			AudioSource.PlayClipAtPoint(simpleShootSound, transform.position);

			shotsFired++;
			if (shotsFired + 1 >= ammoBelt) {
				shotsFired = 0;
			}
			if (shotsFired % (ammoBelt / 4) == 0) {
				ShrinkPlayer ();
			}

			GameObject bullet = magazine [shotsFired];
			if (!bullet.activeSelf) {
				bullet.SetActive (true);
				bullet.transform.rotation = transform.rotation;
				bullet.GetComponent<BulletHandler>().ResetSize();
				bullet.transform.position = transform.position + transform.up * transform.localScale.x * 5.0f;
				bullet.GetComponent<Rigidbody2D> ().velocity = transform.up * PlayerHandler.bulletSpeed;
				//Debug.Log ("bullet speed: " + PlayerHandler.bulletSpeed.ToString());
				bullet.GetComponent<BulletHandler> ().AutoExpire ();
			}
		}
	}

	public float GetPlayerSize() {
		Vector3 size = transform.localScale;
		return Mathf.Abs (Mathf.Sqrt (size.x * size.x + size.y * size.y));
	}

	private void changeColorTo(Color clr){
		playersColor = clr;
		transform.GetComponent<Renderer> ().material.color = clr;
	}

	void OnEnable() {
		GetComponent<TapGesture>().Tapped += HandleTapped;
	}

	void OnDisable() {
		Debug.LogError ("player disabled");

		Debug.Log ("score: " + scoreRecord.dbEntry.totalScore.ToString ());
		scoreRecord.needsUpdate = true;
		GetComponent<TapGesture> ().Tapped -= HandleTapped;
	}
	
	void HandleTapped (object sender, System.EventArgs e)
	{
		Debug.Log ("player is tapped");
	}


	public bool ColorsCloseEnough(Color col) {
		float rDiff = Mathf.Abs (thisColor.r - col.r);
		float gDiff = Mathf.Abs (thisColor.g - col.g);
		float bDiff = Mathf.Abs (thisColor.b - col.b);
		
		float totalDiff = rDiff + gDiff + bDiff;

		if (totalDiff < spectrumStrength) {
			return true;
		} else {
			return false;
		}
	}

	//collides with point portal (trigger)
	private void OnTriggerEnter2D(Collider2D coll) {
		if ((coll.gameObject.tag == "PointPortal") && (coll.gameObject.name == (gameObject.name + "PointPortal"))) {
			//cash in points
			CashInPoints(GetPlayerSize());

			//shrinkplayer to almost min size
			transform.localScale = new Vector3(minSize + growthRate, minSize + growthRate, 1);

			hasPointPortal = false;
			Destroy (coll.gameObject);
		}
	}

	//collides with food
	private void OnCollisionEnter2D(Collision2D coll) {
		if (coll.gameObject.tag == "Food") {
			if (ColorsCloseEnough(coll.gameObject.GetComponent<Renderer>().material.color)) {
				GrowPlayer();
				coll.gameObject.SetActive(false);
			}
		}
	}
	

	void OnDestroy() {
		Debug.LogError ("player destroyed");
	}
}
