using UnityEngine;
using System.Collections;

public class AntiGravityEffect : MonoBehaviour {

	[System.NonSerialized]
	public Vector3 antiGravityWellLocation;

	private Rigidbody2D rb2d;
	
	void Start() {
		rb2d = GetComponent<Rigidbody2D> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		Vector3 directionV = antiGravityWellLocation - transform.position;
		
		rb2d.AddForce (-directionV.normalized);
	}
}
