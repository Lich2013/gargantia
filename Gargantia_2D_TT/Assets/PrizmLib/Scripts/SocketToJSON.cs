


//#define CONTROL_SCHEME_ABSOLUTE

#define CONTROL_SCHEME_RELATIVE

using System.Collections;
using UnityEngine;
using SocketIO;
using SimpleJSON;
using Prizm;

[HideInInspector]
public class SocketToJSON : MonoBehaviour{
	private SocketIOComponent socket;
	private RFManager RFManagerReference;
	PlayerHandler player;
	GameManager gameManager;

	public void Start() {
		RFManagerReference = GameObject.Find ("PrizmGameManager").GetComponent<RFManager> ();
		socket = GetComponent<SocketIOComponent>();
		socket.On("smarttouch-start", SmartTouch);
		socket.On("smarttouch-end", SmartTouch);

		//socket.On ("toTabletop", HHMessage);
		//player = GameObject.Find ("Player").GetComponent<PlayerHandler>();

		gameManager = GameObject.Find ("GameManager").GetComponent<GameManager> ();
	}
	//when receiving smart touch data, call this function:
	public void SmartTouch(SocketIOEvent e){
		string RFID = e.data.GetField("tagId").str;
		string typeOfTouch = e.name;
		Vector3 smartTouchPoint = new Vector3 ();
		touchType ST;
		ST = enumerateString (typeOfTouch);
		RFID = filterRFID (RFID);
		smartTouchPoint.x = e.data.GetField("x").n;
		smartTouchPoint.y = Camera.main.pixelHeight - e.data.GetField("y").n;
		Debug.Log ("SmartTouchFound!");
		Debug.Log ("Tag: " + RFID);
		Debug.Log ("Coordinates: " + smartTouchPoint);
		RFManagerReference.prizmFactory.RFIDEventManager (RFID, ST, smartTouchPoint);
	}
	public void HHMessage(SocketIOEvent e) {

		string recvID = e.data.GetField("player").str;

		//get player from game manager's list (untested)
		player = gameManager.playerList.Find (x => x.GetComponent<PlayerHandler>().playerID.Equals(recvID)).GetComponent<PlayerHandler>();

		//Debug.Log ("analyzing message for: " + recvID);

		switch (e.data.GetField("type").str) {

		case "fireQuick":
			player.SimpleShoot ();
			break;

		case "navigation":
			//sample data fields

#if CONTROL_SCHEME_RELATIVE
			float velocity = e.data.GetField ("speed").n / 100;
			float turn = e.data.GetField ("direction").n / 100;

			player.lastVelocityCommand = velocity;
			player.lastTurnCommand = turn;
#endif

#if CONTROL_SCHEME_ABSOLUTE
			float velocity = e.data.GetField ("speed").n / 100;
			float angle = e.data.GetField ("angle").n;
			//Debug.Log ("velocitY: " + 
			
			player.lastVelocityCommand = velocity;
			//player.lastAbsoluteDirection.x = Mathf.Cos(angle);
			//player.lastAbsoluteDirection.y = Mathf.Sin (angle);
			player.lastAbsoluteDirection.z = (-angle * 180 / Mathf.PI);

			//Debug.Log ("player's direction vector (x, y, z): " + player.lastAbsoluteDirection.ToString());
#endif

			/*
			if (velocity >= 0) {
				player.MoveForward (velocity);
			} else {
				player.MoveBackward (-velocity);
			}
			
			if (turn >= 0) {
				player.TurnRight (turn);
			} else { 
				player.TurnLeft (-turn);
			}
			break;
			*/
			break;

		case "startCharge":
			player.ChargePowerShot();
			break;

		case "endCharge":
			player.FirePowerShot();
			break;

		default:
			Debug.LogError("Unknown message type: " + e.data.GetField("type").str);
			break;
		}
	}

	private touchType enumerateString(string str){
		if (str == "smarttouch-start") {
			return touchType.smartTouchStart;
		} else
			return touchType.smartTouchEnd;
	}

	private string filterRFID(string ID){
		if (ID.Length == 12) {
			return ID.Substring (0, ID.Length - 1);
		} else
			return ID;

	}
}