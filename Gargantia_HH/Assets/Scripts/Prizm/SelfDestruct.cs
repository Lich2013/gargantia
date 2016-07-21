using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SelfDestruct : MonoBehaviour {

	public void KillMyself(string message, float countDown) {
		GetComponent<Text> ().text = message;
		Debug.Log ("In Killmyself, message: " + message);
		StartCoroutine (destroyMessage (countDown));
	}
	private IEnumerator destroyMessage (float time) {
		yield return new WaitForSeconds (time);
		if (time != 0) {
			Destroy (gameObject);
		}

	}
}
