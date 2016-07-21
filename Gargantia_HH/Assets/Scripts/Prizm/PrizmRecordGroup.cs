using UnityEngine;
using System.Collections;
using Extensions;
using System.Collections.Generic;

//keeps track of all PrizmRecords, syncs & adds gameObjects to database
public class PrizmRecordGroup : MonoBehaviour  {
	public Meteor.Collection<DatabaseEntry> gameObjectCollection;
	public List<PrizmRecord> associates;							//list of all PrizmRecords that belong to this recordGroup
	
	public string defaultRecordGroup = "defaultRecordGroup";

	public bool gameObjectCollectionCreated = false;
	ClientInitialization handheldInitObject;

	public void Awake() {
		associates = new List<PrizmRecord> ();
		handheldInitObject = GetComponent<ClientInitialization> ();
	}


	//called from PrizmRecord class to add to the list of objects that need to be synced
	//this does NOT create a new entry in the database, it is so that the handheld client can
	//sync PrizmRecords
	public void AddRecord (PrizmRecord record) {
		Debug.LogError ("Adding Record: " + record.name + record.dbEntry._id);
		associates.Add(record);	//adds record to list of all associated prizm records
		Debug.Log ("associates ID: " + associates [0].dbEntry._id);
	}

	//called from PrizmRecord class to add itself to the database
	//use this when the tabletop does not add the record to the database itself
	public IEnumerator AddRecordDB (PrizmRecord record) {
		Debug.LogError ("Adding Record: " + record.name);
		associates.Add(record);	//adds record to list of all associated prizm records
		
		//forms a dictionary to pass into meteor's 'addGameObject' from the record's databaseEntry parameters
		Dictionary<string, string> dict = new Dictionary<string, string> () {
			{"location", record.dbEntry.location},
			{"color", record.dbEntry.color},
		};
		
		var methodCall = Meteor.Method<ChannelResponse>.Call ("addGameObject", handheldInitObject.sessionID, defaultRecordGroup, dict);	
		yield return (Coroutine)methodCall;
		if (methodCall.Response.success) {
			Debug.LogError ("call to 'addGameObject' succeeded, response: " + methodCall.Response.message);
			string UniqueID = methodCall.Response.message;
		} else {
			Debug.LogError ("uh oh! call to 'addGameObject' failed! Response: " + methodCall.Response.message);
		}
	}

	//sync all objects with 'needsUpdate' flag to database
	//developer calls at own discretion
	public IEnumerator SyncAll() {
		Debug.LogError ("Begin syncing database, size of associates list: " + associates.Count);
		for (int i = 0; i < associates.Count; i++) {			//go through list of associates and look for ones that need to be synced
			Debug.Log ("associates[" + i + "] is: " + associates[i].name + ", dbEntry(location): " + associates[i].dbEntry.location + "ID: " + associates[i].dbEntry._id);
			if (associates[i].needsUpdate == true) {
				Debug.LogError ("Updating: " + associates[i].name + ":" + associates[i].dbEntry._id);

				//forms a dictionary to pass into meteor's 'updateGameObject' from the record's databaseEntry parameters
				//simplify this for the developer in the future (maybe use an enum?)
				Dictionary<string, string> dict = new Dictionary<string, string> () {
					{"location", associates[i].dbEntry.location},
					{"color", associates[i].dbEntry.color},
				};

				var methodCall = Meteor.Method<ChannelResponse>.Call ("updateGameObject", associates[i].dbEntry._id, dict);
				yield return (Coroutine)methodCall;
				if (methodCall.Response.success) {
					//Debug.LogError (associates[i].dbEntry.UID + " should = " + methodCall.Response.message);
					associates[i].dbUpdated();		//tells the record that it was updated and it can rest now
				} else {
					Debug.LogError ("Uh oh! database sync failed on record: " + associates[i].name + ", with UID: " + associates[i].dbEntry._id);
				}
			}
		}
		Debug.LogError ("Finished with SyncAll()");
	}

	//removes record from associates list to not keep track of it
	public void RemoveRecord (PrizmRecord record) {
		associates.Remove (record);
	}

	//removes record from GameObjects in database
	public IEnumerator RemoveRecordDB (PrizmRecord record) {
		Debug.LogError ("Removing from database: " + record.name + ", UID: " + record.dbEntry._id);		

		var methodCall = Meteor.Method<ChannelResponse>.Call ("removeGameObject", record.dbEntry._id);		
		yield return (Coroutine)methodCall;
		if (methodCall.Response.success) {
			//Destroy(record);			//optional to remove it from the scene too
			Debug.LogError ("Successfully removed");
		} else {
			Debug.LogError ("Uh oh! call to 'removeGameObject' failed on record: " + record.name + ", with UID: " + record.dbEntry._id);
		}
	}
	
	//collection of gameObjects
	public IEnumerator CreateGameObjectCollection() {
		gameObjectCollection = Meteor.Collection <DatabaseEntry>.Create ("gameObjects");
		yield return gameObjectCollection;
		/* Handler for debugging the addition of gameObjects: */
		/*
		gameObjectCollection.DidAddRecord += (string id, DatabaseEntry document) => {				
			Debug.LogError(string.Format("gameObject document added:\n{0}", document.Serialize()));
		};
		*/
		Debug.LogError ("adding record change handler(s)");
		gameObjectCollection.DidChangeRecord += GetComponent<GameManager>().HandleDidChangeRecord;
		gameObjectCollection.DidAddRecord += GetComponent<GameManager>().HandleDidAddRecord;
		gameObjectCollectionCreated = true;
	}


}

