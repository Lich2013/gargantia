using UnityEngine;
using System.Collections;
using Extensions;
using System.Collections.Generic;

public class PrizmRecordGroup : MonoBehaviour  {
	public Meteor.Collection<DatabaseEntry> gameObjectCollection;
	public List<PrizmRecord> associates;							//list of all PrizmRecords that belong to this recordGroup
	
	public string defaultRecordGroup = "cards";
	
	TabletopInitialization tabletopInitObject;
	
	public void Awake() {
		associates = new List<PrizmRecord> ();
		tabletopInitObject = GetComponent<TabletopInitialization> ();
	}
	
	//called from PrizmRecord class to add itself to the database
	public IEnumerator AddRecord (PrizmRecord record) {
		//Debug.LogError ("Adding Record: " + record.name);
		associates.Add(record);	//adds record to list of all associated prizm records
		
		//forms a dictionary to pass into meteor's 'addGameObject' from the record's databaseEntry parameters
		//simplify this for the developer in the future (maybe use an enum?)
		Dictionary<string, string> dict = new Dictionary<string, string> () {
			{"location", record.dbEntry.location},
			{"back", record.dbEntry.back},
			{"suit", record.dbEntry.suit},
			{"number", record.dbEntry.number}
		};
		
		var methodCall = Meteor.Method<ChannelResponse>.Call ("addGameObject", tabletopInitObject.sessionID, defaultRecordGroup, dict);	
		yield return (Coroutine)methodCall;
		if (methodCall.Response.success) {
			Debug.LogError ("call to 'addGameObject' succeeded, response: " + methodCall.Response.message);
			string UniqueID = methodCall.Response.message;
			record.dbEntry._id = UniqueID;	
		} else {
			Debug.LogError ("uh oh! call to 'addGameObject' failed! Response: " + methodCall.Response.message);
		}
	}
	
	//sync all objects with 'needsUpdate' flag to database
	//developer calls at own discretion
	public IEnumerator SyncAll() {
		Debug.LogError ("Begin syncing database");
		for (int i = 0; i < associates.Count; i++) {			//go through list of associates and look for ones that need to be synced
			if (associates[i].needsUpdate == true) {
				Debug.LogError ("Updating: " + associates[i].name + ":" + associates[i].dbEntry._id + " needs update: " + associates[i].needsUpdate);
				
				//forms a dictionary to pass into meteor's 'updateGameObject' from the record's databaseEntry parameters
				//simplify this for the developer in the future (maybe use an enum?)
				Dictionary<string, string> dict = new Dictionary<string, string> () {
					{"location", associates[i].dbEntry.location},
					{"back", associates[i].dbEntry.back},
					{"suit", associates[i].dbEntry.suit},
					{"number", associates[i].dbEntry.number}
				};
				
				var methodCall = Meteor.Method<ChannelResponse>.Call ("updateGameObject", associates[i].dbEntry._id, dict);
				yield return (Coroutine)methodCall;
				if (methodCall.Response.success) {
					//Debug.LogError (associates[i].dbEntry._id + " should = " + methodCall.Response.message);
					associates[i].dbUpdated();		//tells the record that it was updated and it can rest now
				} else {
					Debug.LogError ("Uh oh! database sync failed on record: " + associates[i].name + ", with _id: " + associates[i].dbEntry._id);
				}

			}
		}
		Debug.LogError ("Finished with SyncAll()");
		yield return null;
	}

	//sync all objects with 'needsUpdate' flag to database
	//developer calls at own discretion
	public IEnumerator Sync(PrizmRecord itemToSync) {
		Debug.LogError ("now syncing: " + itemToSync.name);

		if (itemToSync.needsUpdate) {
			//forms a dictionary to pass into meteor's 'updateGameObject' from the record's databaseEntry parameters
			//simplify this for the developer in the future (maybe use an enum?)
			Dictionary<string, string> dict = new Dictionary<string, string> () {
				{"location", itemToSync.dbEntry.location},
				{"back", itemToSync.dbEntry.back},
				{"suit", itemToSync.dbEntry.suit},
				{"number", itemToSync.dbEntry.number}
			};


			var methodCall = Meteor.Method<ChannelResponse>.Call ("updateGameObject", itemToSync.dbEntry._id, dict);
			yield return (Coroutine)methodCall;
			if (methodCall.Response.success) {
				//Debug.LogError (associates[i].dbEntry._id + " should = " + methodCall.Response.message);
				itemToSync.dbUpdated();		//tells the record that it was updated and it can rest now
			} else {
				Debug.LogError ("Uh oh! database sync failed on record: " + itemToSync.name + ", with _id: " + itemToSync.dbEntry._id);
			}

		} else {
			Debug.LogError(itemToSync.name + "did not need to be updated, but Sync() was called on it");
		}
		Debug.LogError ("Finished with Sync()");
		yield return null;
	}
	
	//removes record from GameObjects
	public IEnumerator RemoveRecord (PrizmRecord record) {
		Debug.LogError ("Removing from database: " + record.name + ", _id: " + record.dbEntry._id);		
		
		var methodCall = Meteor.Method<ChannelResponse>.Call ("removeGameObject", record.dbEntry._id);		
		yield return (Coroutine)methodCall;
		if (methodCall.Response.success) {
			//Destroy(record);			//optional to remove it from the scene too
			Debug.LogError ("Successfully removed");
		} else {
			Debug.LogError ("Uh oh! call to 'removeGameObject' failed on record: " + record.name + ", with _id: " + record.dbEntry._id);
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
	}
}

