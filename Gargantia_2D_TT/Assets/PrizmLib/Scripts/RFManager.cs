using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using Prizm;
using TouchScript;
using UnityEngine.UI;

//would-be-developer's script
public class RFManager: MonoBehaviour {
	public PrizmObject prizmFactory = new PrizmObject();
	private GameManager gameManager;

	void Awake(){
		StartCoroutine(prizmFactory.readJson());
		gameManager = GameObject.Find ("GameManager").GetComponent<GameManager> ();
	}
	void OnEnable(){
		prizmFactory.smartTouchStart += smartTouchStartHandler;
		prizmFactory.smartTouchEnd += smartTouchEndHandler;
	}

	void OnDisable(){
		prizmFactory.smartTouchStart -= smartTouchEndHandler;
		prizmFactory.smartTouchEnd -= smartTouchEndHandler;
	}

	//location is in pixel coordinate
	private void smartTouchStartHandler(bindedObject rfAttributes){
		Debug.Log ("Screen position found!: " + rfAttributes.Location.x + ", " + rfAttributes.Location.y);

		//convert 1920 x 1080 screen coordinates to float between 0 and 1:
		float xScreenCoord = rfAttributes.Location.x / 1920.0f;
		float yScreenCoord = rfAttributes.Location.y / 1080.0f;

		if (rfAttributes.Properties ["type"] == "trigger") {
			switch (rfAttributes.Properties["action"]) {

			case "gravity":
				gameManager.ActivateGravityWell(xScreenCoord, yScreenCoord);
				break;
			case "antiGravity":
				gameManager.ActivateAntiGravityWell(xScreenCoord, yScreenCoord);
				break;
			case "colorChange":
				gameManager.ActivateChangeAllColors(xScreenCoord, yScreenCoord);
				break;
			case "superSpawn":
				gameManager.ActivateSuperSpawnRate(xScreenCoord, yScreenCoord);
				break;
			case "random":
				gameManager.ActivateRandom(xScreenCoord, yScreenCoord);
				break;
			default:
				gameManager.ActivateRandom(xScreenCoord, yScreenCoord);
				break;
			}

		} else if (rfAttributes.Properties ["type"] == "mainmenu") {
			Application.Quit();
		} else if (rfAttributes.Properties ["type"] == "HUMAN") {	//easter egg
			gameManager.audioSource.PlayOneShot(gameManager.humanScreamSound);
			gameManager.ActivateRandom(xScreenCoord, yScreenCoord);
		} 
	}


	private void smartTouchEndHandler(bindedObject rfAttributes){
		if (rfAttributes.Properties ["type"] == "trigger") {
			switch (rfAttributes.Properties["action"]) {
				
			case "gravity":
				gameManager.DisableGravityWell();
				break;
			case "antiGravity":
				gameManager.DisableAntiGravityWell();
				break;
			case "colorChange":
				gameManager.DisableChangeAllColors();
				break;
			case "superSpawn":
				gameManager.DisableSuperSpawnRate();
				break;
			case "random":
				gameManager.DisableRandom();
				break;
			default:
				gameManager.DisableRandom();
				break;
				
			}
		} else if (rfAttributes.Properties ["type"] == "HUMAN") {	//easter egg
			gameManager.DisableRandom();
		} 
	}





}