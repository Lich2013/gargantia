using UnityEngine;
using System.Collections;

public class PlatformManagerScript : MonoBehaviour {
	public GameObject SocketIOObject;
	public GameObject AndroidObject;

	// Use this for initialization
	void Start () {
		if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
			Instantiate(SocketIOObject);
		}
		
		//android 
		else if (Application.platform == RuntimePlatform.Android) {
			Instantiate(AndroidObject);
		}

	}

}
