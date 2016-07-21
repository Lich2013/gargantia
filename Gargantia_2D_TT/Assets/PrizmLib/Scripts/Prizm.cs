using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using SocketIO;

namespace Prizm {
	public enum touchType{smartTouchStart, smartTouchEnd};
	public delegate void rfidDetected(bindedObject rfBinded);

	public class bindedObject{
		public string ID{get;set;}
		public List<string> OtherID{ get; set;}
		public Vector3 Location{ get; set;}
		public List<string> Components{ get; set;}
		public Dictionary<string, string> Properties{ get; set;}
		
		public bindedObject(string newID, List<string> newOtherID, Vector3 newLocation, List<string> newComponents, Dictionary<string, string> newProperties){
			ID = newID;
			OtherID = newOtherID;
			Location = newLocation;
			Components = newComponents;
			Properties = newProperties;
		}
	}

	public class PrizmObject{
		public rfidDetected smartTouchStart;
		public rfidDetected smartTouchEnd;
		private JSONClass j;

		public void RFIDEventManager(string ID, touchType ST, Vector3 location){
			List<string> otherID = new List<string> ();
			List<string> components = new List<string> ();
			Dictionary<string, string> properties = new Dictionary<string,string> ();
			checkJSON.findTagInfo (j, ID, out otherID, out components, out properties);
			bindedObject rfReadyObject = new bindedObject(ID, otherID, location, components, properties);
			if (components != null & properties != null) { //entry MUST contain some components and properties
				if (ST == touchType.smartTouchStart) {
					Debug.Log ("Starting smarttouch start event");
					smartTouchStart (rfReadyObject);
				} else if (ST == touchType.smartTouchEnd) {
					smartTouchEnd (rfReadyObject);
				}
			}
		}
		public IEnumerator readJson(){
			Debug.Log ("Reading JSON");
			j = null;
			string fileName = "rfidpieces.json";
			string jsonPath = Application.streamingAssetsPath + "/" + fileName;
			StreamReader sr;
			//windows
			if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
				Debug.Log ("Found windows");
				sr = new StreamReader (jsonPath);
				yield return sr;
				j = JSON.Parse (sr.ReadToEnd ()) as JSONClass;
			}
			
			//android 
			else if (Application.platform == RuntimePlatform.Android) {
				WWW www = new WWW (jsonPath);
				yield return www;
				j = JSON.Parse (www.text.ToString ()) as JSONClass;
				Debug.Log ("JSON found! Number of IDs located: " + j["rfBindings"].Count);
			} else 
				Debug.LogError ("Android and Windows only");
			yield return null;
		}
	}

	/*
	public class RfidBinding{ //smart-touch event delegates
		public static rfidDetected smartTouchStart;
		public static rfidDetected smartTouchEnd;
	}
    */

/*
	public static class RFIDEventManager{
		public static void rfidDetected(string ID, touchType ST, Vector3 location){
			List<string> otherID = new List<string> ();
			List<string> components = new List<string> ();
			Dictionary<string, string> properties = new Dictionary<string,string> ();

			checkJSON.findTagInfo (ID, out otherID, out components, out properties);

			bindedObject rfReadyObject = new bindedObject(ID, otherID, location, components, properties);
			if (components != null & properties != null) { //entry MUST contain some components and properties
				if (ST == touchType.smartTouchStart) {
					RfidBinding.smartTouchStart (rfReadyObject);
				} else if (ST == touchType.smartTouchEnd) {
					RfidBinding.smartTouchEnd (rfReadyObject);
				}
			}
		}
	}*/

	public static class checkJSON{
		/*private static TextAsset textFile;
		private static string jsonPath;
		private static StreamReader sr;
		private static JSONClass j;
*/
		public static void findTagInfo(JSONClass jstring, string id, out List<string> OtherID, out List<string> Components, out Dictionary<string, string> Properties){
			Debug.Log ("Searching JSON file for tag...");

			//used to give a message about a tag that's not in the JSON file
			//GameObject.Find ("GameManager").GetComponent<GameManager>().createMsgLog("id: '" + id+"'");

			JSONNode entry = findEntry (jstring, id);
			OtherID = null;
			Components = null;
			Properties = null;
			if (entry != null) {
				Debug.Log ("Found tag in JSON file!");
				OtherID = findOtherID (id, entry);
				Components = findComponents (entry);
				Properties = findProperties (entry);
			} else {
				Debug.LogError ("Tag does not exist in JSON file...");
				GameObject.Find ("GameManager").GetComponent<GameManager> ().createMsgLog ("id: '" + id + "'");
			}
		}
		
		public static JSONNode findEntry(JSONClass jString, string id){
			JSONNode entry = null;
			if (jString != null) {
				for (int i = 0; i < jString["rfBindings"].Count; i++) {
					for (int k = 0; k < jString["rfBindings"][i]["id"].Count; k++) {
						if (id == jString ["rfBindings"] [i] ["id"] [k].Value.ToString ()) {
							entry = jString["rfBindings"] [i];
						}
					}
				}
			}
			return entry;
		}//end of findEntry
		
		public static List<string> findOtherID(string id, JSONNode entry){
			Debug.Log ("Searching for other id in same lookup");
			List<string> otherid = new List<string>();
			for(int i =0; i<entry["id"].Count; i++){
				if(id != entry["id"][i].Value){
					otherid.Add(entry["id"][i].Value);
				}
			}
			return otherid;
		}//end of findOtherID
		
		public static List<string> findComponents(JSONNode entry){
			Debug.Log ("Searching for components");
			List<string> comp = new List<string> ();
			if (entry["components"] != null) {
				for(int i =0; i<entry["components"].Count; i++){
					comp.Add(entry["components"][i].Value);
				}
			}
			return comp;
		} //end of findComponents
		
		public static Dictionary<string,string> findProperties(JSONNode entry){
			Debug.Log ("Searching for properties");
			Dictionary<string, string> prop = new Dictionary<string,string>();
			if (entry["properties"] != null) {
				foreach(var key in entry["properties"].Keys){
					prop.Add(key, entry["properties"][key]);
				}
			}
			return prop;
		}//end of findProperties

		/*
		public static IEnumerator readJson(){
			j = null;
			string fileName = "rfidpieces.json";
			string jsonPath = Application.streamingAssetsPath + "/" + fileName;

			//windows
			if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
				Debug.LogError ("Reading JSON for windows");
				sr = new StreamReader (jsonPath);
				yield return sr;
				j = JSON.Parse (sr.ReadToEnd ()) as JSONClass;
			}

		//android 
		else if (Application.platform == RuntimePlatform.Android) {
				Debug.LogError ("Reading JSON for android");
				WWW www = new WWW (jsonPath);
				yield return www;
				j = JSON.Parse (www.text.ToString ()) as JSONClass;
			} else 
				Debug.LogError ("Android and Windows only");
			yield return null;

		}*/

	}
}