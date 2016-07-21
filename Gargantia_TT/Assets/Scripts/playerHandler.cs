using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TouchScript;
using TouchScript.Gestures;


public class playerHandler : MonoBehaviour {

	public AudioClip shootSound;

	string playersColor;
	public string playerID;
	public bool isBoundToSession = false;
	Rigidbody2D rb;

	//power of shooting bullets
	public float Power = 10.0f;

	public float moveSpeed = 10.0f;
	public float rotateSpeed = 10.0f;

	void Start(){
		rb = GetComponent<Rigidbody2D>();
	}

	void Update() {



		if (Input.GetKey (KeyCode.UpArrow)) {
			MoveForward();
		}

		if (Input.GetKeyDown (KeyCode.DownArrow)) {
			MoveBackward();
		}

		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			TurnLeft();
		}

		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			TurnRight();
		}
	}

	public void MoveForward() {
		rb.AddRelativeForce (Vector3.forward * moveSpeed);
	}

	public void MoveBackward() {
		rb.AddRelativeForce (Vector3.back * moveSpeed);
	}

	public void TurnLeft() {
		//rb.AddTorque(Vector3.up * rotateSpeed);
	}

	public void TurnRight() {
		//rb.AddTorque (Vector3.down * rotateSpeed);
	}


	public void initializePlayer(string name, string clr, string id){
		changeID (id);
		changeName (name);
		changeColorTo (clr);
	}

	private void changeID (string id){
		playerID = id;
	}

	private void changeName(string n){
		gameObject.name = n;
	}

	private void changeColorTo(string clr){
		playersColor = clr;
		transform.GetComponent<Renderer> ().material.color = new Color ();
		//transform.GetComponent<Renderer>().material.color = clr;
	}

	void OnEnable() {
		GetComponent<TapGesture>().Tapped += HandleTapped;
	}

	void OnDisable() {
		GetComponent<TapGesture> ().Tapped -= HandleTapped;
	}
	
	void HandleTapped (object sender, System.EventArgs e)
	{
		Debug.Log ("player is tapped");
	}



}
