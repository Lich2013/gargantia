
#define CONTROL_SCHEME_ABSOLUTE

//#define CONTROL_SCHEME_RELATIVE

using UnityEngine;
using System.Collections;
using SocketIO;
using SimpleJSON;
using Prizm;

[HideInInspector]
public class WebSocketManager : MonoBehaviour {

	private SocketIOComponent socket;
	private RFManager RFManagerReference;
	PlayerHandler player;
	GameManager gameManager;
	
	public void Start() {
		socket = GetComponent<SocketIOComponent>();
		socket.On ("toTabletop", HHMessage);
		//player = GameObject.Find ("Player").GetComponent<PlayerHandler>();
		
		gameManager = GetComponent<GameManager> ();

	}

	public void EjectPlayer(string playerToEjectID) {

		JSONObject js = new JSONObject ();
		js.AddField ("action", "ejectPlayer");
		js.AddField ("playerToEject", playerToEjectID);
		socket.Emit ("toHandheld", js);

	}

	public void HHMessage(SocketIOEvent e) {
		//Debug.Log ("socket.on to tabletop fired");
		
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
			//float velocity = e.data.GetField ("speed").n / 100;
			//float turn = e.data.GetField ("direction").n / 100;
			float velocity = e.data.GetField ("speed").n / 100;
			float angle = e.data.GetField ("angle").n;

			angle += (Mathf.PI / 2.0f);

			player.lastTurnCommand = -Mathf.Cos(angle);
			player.lastVelocityCommand = Mathf.Sin (angle);

			//Debug.Log ("angle: " + -Mathf.Cos(angle).ToString());
			//Debug.Log ("velocity: " + Mathf.Sin (angle).ToString());
			//player.lastVelocityCommand = velocity;
			//player.lastTurnCommand = turn;


			
			//player.lastVelocityCommand = velocity;
			//player.lastAbsoluteDirection.x = Mathf.Cos(angle);
			//player.lastAbsoluteDirection.y = Mathf.Sin (angle);
			//player.lastAbsoluteDirection.z = (-angle * 180 / Mathf.PI);
			
			//Debug.Log ("player's direction vector (x, y, z): " + player.lastAbsoluteDirection.ToString());

			#endif
			
			#if CONTROL_SCHEME_ABSOLUTE
			float velocity = e.data.GetField ("speed").n / 100;
			float angle = e.data.GetField ("angle").n;
			
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
}
