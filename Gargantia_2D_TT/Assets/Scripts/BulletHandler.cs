using UnityEngine;
using System.Collections;

public class BulletHandler : MonoBehaviour {

	public GameObject associatedPlayer;

	private IEnumerator chargeRoutine;

	[System.NonSerialized]
	public float damageMultiplier = 2.0f;
	[System.NonSerialized]
	public float lifetime = 3.0f;	//survives for 10 seconds max

	private Rigidbody2D rb2d;


	public static Vector3 defaultScale = new Vector3(0.02f, 0.02f, 1.0f);
	
	// Use this for initialization
	void Start () {
		rb2d = GetComponent<Rigidbody2D> ();
		if (associatedPlayer != null) {
			//Debug.Log ("associated player: " + associatedPlayer.gameObject.name);

		}
	}


	public void AutoExpire() {
		StopCharging ();
		StartCoroutine (DisableSelf ());
	}

	public void StartCharging() {
		chargeRoutine = Charge ();
		StartCoroutine (chargeRoutine);
	}

	public void StopCharging() {
		if (chargeRoutine != null) {
			StopCoroutine (chargeRoutine);
		}
	}


	private IEnumerator Charge() {
		while (true) {
			yield return new WaitForSeconds(1.0f);
			GrowSize();
		}
	}

	private void GrowSize() {

		AudioSource.PlayClipAtPoint (associatedPlayer.GetComponent<PlayerHandler>().startChargeSound, transform.position);

		transform.SetParent (null);
		transform.localScale = new Vector3(transform.localScale.x + PlayerHandler.growthRate * 5, transform.localScale.y + PlayerHandler.growthRate * 5, 1);
		transform.SetParent (associatedPlayer.transform);
		foreach (Transform child in transform.GetChild(0)) {
			child.GetComponent<ParticleSystem>().startSize = child.GetComponent<ParticleSystem>().startSize + PlayerHandler.growthRate * 5;
		}
		if (!associatedPlayer.GetComponent<PlayerHandler> ().ShrinkPlayer ()) {
			StopCharging ();
		}
	}
	
	public void ResetSize() {
		transform.localScale = defaultScale;
		foreach (Transform child in transform.GetChild(0)) {
			child.GetComponent<ParticleSystem>().startSize = defaultScale.x;
		}
	}

	private IEnumerator DisableSelf() {
		yield return new WaitForSeconds (lifetime);
		gameObject.SetActive (false);
		transform.SetParent (null);
	}

	void OnTriggerEnter2D(Collider2D coll) {
		if (coll.gameObject.tag == "Player") {
			//shrink that player if it's not the shooter
			if (associatedPlayer == null || coll.gameObject.name != associatedPlayer.gameObject.name) {
				coll.gameObject.GetComponent<PlayerHandler> ().ShrinkPlayer (transform.localScale.x * damageMultiplier);
				gameObject.SetActive (false);
			}
		} else if (coll.gameObject.tag == "Bullet") {
			gameObject.SetActive (false);
			coll.gameObject.SetActive (false);
		} else if (coll.gameObject.tag == "Boundary") {
			if (rb2d != null)
				rb2d.velocity = -rb2d.velocity;
			else
				rb2d = GetComponent<Rigidbody2D>();
		}
	}

	public void ChaosBullet() {
		StartCoroutine (RandomMotions());
	}

	public IEnumerator RandomMotions() {
		for (int i = 0; i < 5; i++) {
			GetComponent<Rigidbody2D> ().velocity = (new Vector2(Random.Range (-1.0f, 1.0f), Random.Range (-1.0f, 1.0f)) * 1.0f);
			GetComponent<Rigidbody2D>().angularVelocity = (Random.Range (-1.0f, 1.0f) * 100.0f);
			yield return new WaitForSeconds(3);
		}
		gameObject.SetActive (false);
	}


}
