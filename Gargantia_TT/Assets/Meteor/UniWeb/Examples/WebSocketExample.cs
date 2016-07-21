using UnityEngine;
using System.Collections;

public class WebSocketExample : MonoBehaviour {

	IEnumerator Start () {
		yield return null;
		
		var ws = new HTTP.WebSocket();
		StartCoroutine(ws.Dispatcher());
		
		Debug.LogError(ws);
		
		ws.Connect("http://echo.websocket.org");
		
		ws.OnTextMessageRecv += (e) => {
			Debug.LogError("Reply came from server -> " + e);
		};
		ws.Send("Hello");
		
		ws.Send("Hello again!");
		
		ws.Send("Goodbye");
	}
	
	
}
