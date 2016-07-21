using UnityEngine;
using System.Collections;

public class BootstrapHH : MonoBehaviour {
	PrizmRecordGroup recordGroup;
	ClientInitialization clientInit;
	GameManager gameManager;
	
	void Awake() {
		recordGroup = GameObject.Find ("GameManager").GetComponent<PrizmRecordGroup> ();
		clientInit = GameObject.Find ("GameManager").GetComponent<ClientInitialization> ();
		gameManager = GameObject.Find ("GameManager").GetComponent<GameManager> ();

	}
	
	public IEnumerator Bootstrap() {
		yield return StartCoroutine (clientInit.MeteorInit ());


	}
}
