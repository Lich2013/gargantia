using UnityEngine;
using System.Collections;
using TouchScript;
using System;
using TouchScript.Gestures;

public class PlayerHandler : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnEnable() {
		GetComponent<TapGesture>().Tapped += HandleTapped;

	}

	void HandleTapped (object sender, EventArgs e)
	{
		Debug.Log ("Tapped, sending UDP packet");
	}
}
