using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//parent class of all game objects that need syncing
public class PrizmRecord : MonoBehaviour{
	public DatabaseEntry dbEntry;
	public bool needsUpdate = false;

	virtual public void Awake() {
		dbEntry = new DatabaseEntry ();
	}

	virtual public void Start() {
		//AddToRecordGroup ();		//needs to happen after PrizmRecordGroup finishes Start(); (don't know how to do that yet)
	}
	
	virtual public void OnDestroy() {
		StartCoroutine (GameObject.Find ("/GameManager").GetComponent<PrizmRecordGroup> ().RemoveRecord (this));
	}
	
	public void dbUpdated() {
		needsUpdate = false;
	}

	public void setName(string n) {
		//Debug.LogError ("setting UID to : " + n);
//		dbEntry.UID = n;
		//Debug.LogError ("did it succeed? " + dbEntry.UID);
	}

	//add self to PrizmRecordGroup
	public void AddToRecordGroup() 
	{
		StartCoroutine(GameObject.Find ("/GameManager").GetComponent<PrizmRecordGroup>().AddRecord (this));	
	}
}
