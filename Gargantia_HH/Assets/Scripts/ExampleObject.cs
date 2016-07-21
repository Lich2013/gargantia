using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TouchScript.Gestures;
using TouchScript;


//example class showing how to use a game object inheriting from PrizmRecord
public class ExampleObject : PrizmRecord{
	
	protected override void Awake()
	{
		base.Awake ();		//call base.Awake() to initialize PrizmRecord::Awake()
	}

	protected override void Start ()
	{
		base.Start ();		//call base.Start() to initialize PrizmRecord::Start()
	}
	
	//<---------> Functions for Touchscript <---------->//
	private void OnEnable()
	{
		GetComponent<TapGesture>().Tapped += HandleTapped;
	}
	
	private void OnDisable()
	{
		GetComponent<TapGesture>().Tapped -= HandleTapped;
	}
	
	public void HandleTapped(object sender, EventArgs e) {
		Debug.Log ("object tapped, stats: " + dbEntry._id + dbEntry.location);
		UpdateServer ();
	}
	
	//<----------> Functions manage database operations <---------->//
	public void UpdateServer() {
		dbEntry.location = "home";
		needsUpdate = true;
		StartCoroutine (GameObject.Find ("GameManager").GetComponent<PrizmRecordGroup>().SyncAll());	//syncs every single PrizmRecord that needs updating (could use Sync(this) instead)
		gameObject.SetActive (false);
	}


}
