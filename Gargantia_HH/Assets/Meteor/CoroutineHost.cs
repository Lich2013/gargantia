using System;
using UnityEngine;

public class CoroutineHost : MonoSingleton<CoroutineHost>
{
	public CoroutineHost ()
	{
	}

	void OnApplicationQuit() {
		base.OnApplicationQuit ();
		try {
			Meteor.LiveData.Instance.Close();
		} catch (Exception e) {
			Debug.LogError(e);
		}
	}
}

