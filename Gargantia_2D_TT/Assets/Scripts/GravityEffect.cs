using UnityEngine;
using System.Collections;
using System;

public class GravityEffect : MonoBehaviour {

	[System.NonSerialized]
	public Vector3 gravityWellLocation;


	private Rigidbody2D rb2d;

	void Start() {
		rb2d = GetComponent<Rigidbody2D> ();
	}

	// Update is called once per frame
	void FixedUpdate () {
		Vector3 directionV = gravityWellLocation - transform.position;

		rb2d.AddForce (directionV.normalized);
	}
}
