using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

//enum to match colored ring on handheld device to colored ring on tabletop device
public class PlayerColor {
	public enum color {white=0, purple, brown, orange, teal, green, yellow, violet, black, blue}
}

public class GameManager : MonoBehaviour {

	//<----------> Variables that give us access to GameManager components <---------->//
	PrizmRecordGroup recordGroup;
	ClientInitialization clientInit;
	BootstrapHH bootstrap;
	

	//<---------->  <---------->//
	public GameObject newThing;
	PrizmRecord exampleObject;
	GameObject something;

	
	//set all variables
	void Awake() {
		recordGroup = GameObject.Find ("GameManager").GetComponent<PrizmRecordGroup> ();
		clientInit = GameObject.Find ("GameManager").GetComponent<ClientInitialization> ();
		bootstrap = GameObject.Find ("GameManager").GetComponent<BootstrapHH> ();
	}

	void Update() {
		//exits the app if back button is pressed on android
		if (Input.GetKeyDown (KeyCode.Escape))
			StartCoroutine (ExitApplication ());
	}

	//closes player's record on the database, exits android app
	IEnumerator ExitApplication() {
		//Debug.LogError ("in ExitApplication()");
		var methodCall = Meteor.Method<ChannelResponse>.Call ("removePlayerRecord", clientInit.playerID);
		
		yield return (Coroutine)methodCall;
		
		if (methodCall.Response.success) {
			Debug.LogError ("call to removePlayerRecord SUCCEEDED!, response: " + methodCall.Response.message);
		} else {
			Debug.LogError("call to 'removePlayerRecord' did not succeed.");
		}

		Application.Quit ();
	}



	//manages if a card is dealt to the player OR if the tabletop device recalls cards
	public void HandleDidChangeRecord(string arg1, DatabaseEntry arg2, IDictionary arg3, string[] arg4) {
		Debug.Log ("GameObject Record Change Detected");
		//if the card is dealt to us
		if (arg2.location == "Player") {
			Debug.Log ("record change's location is 'Player'");
			something.SetActive(true);
			something.GetComponent<ExampleObject>().dbEntry._id = arg2._id;
			Debug.Log ("setting Id to " + arg2._id + "and it worked? " + something.GetComponent<ExampleObject>().dbEntry._id );

		} else {
			Debug.LogError ("this does not belong to us, it belongs to: " + arg2.location);
		}
	}

	public void HandleDidAddRecord (string arg1, DatabaseEntry arg2)
	{
		Debug.Log ("Record added: " + arg2.location + arg2.color + arg2._id + ".");
		if (arg2.location == "home") {
			something = Instantiate (newThing, new Vector3 (0f, 10f, 0f), newThing.transform.rotation) as GameObject;
			something.name = "thing";
			
			exampleObject = something.GetComponent<ExampleObject> ();
			
			//sets the variables to a local copy of the card
			exampleObject.dbEntry.location = arg2.location;
			exampleObject.dbEntry.color = arg2.color;
			exampleObject.dbEntry._id = arg2._id;
			Debug.Log ("Make sure ID is set: " + exampleObject.dbEntry._id);

			
			//keep track of this card and enable us to manipulate it
			exampleObject.AddToRecordGroup ();
			something.SetActive(false);

			
			//if the tabletop device is recalling the cards (to reshuffle)
		} else {
			Debug.LogError ("this: " + arg2.location);
		}
	}
	

}
