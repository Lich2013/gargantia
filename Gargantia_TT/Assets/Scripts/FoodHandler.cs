using UnityEngine;
using System.Collections;
using TouchScript;
using TouchScript.Gestures;

public class FoodHandler : MonoBehaviour {
	Rigidbody rigidbody;
	float leftBorder;
	float rightBorder;
	float bottomBorder;
	float topBorder;
	
	private Vector3[] directions =
	{
		new Vector3(1, -1, 1),
		new Vector3(-1, -1, 1),
		new Vector3(-1, -1, -1),
		new Vector3(1, -1, -1),
		new Vector3(1, 1, 1),
		new Vector3(-1, 1, 1),
		new Vector3(-1, 1, -1),
		new Vector3(1, 1, -1)
	};
	
	public float Power = 10.0f;
	
	// Use this for initialization
	void Start () {
		rigidbody = GetComponent<Rigidbody>();

	}
	
	void Update() {
		
	}
	
	void OnEnable() {
		GetComponent<TapGesture>().Tapped += HandleTapped;
	}
	
	void HandleTapped (object sender, System.EventArgs e)
	{
		
		Debug.Log ("Food is tapped");
		
		// if we are not too small
		if (transform.localScale.x > 0.05f)
		{
			Color color = new Color(Random.value, Random.value, Random.value);
			// break this cube into 8 parts
			for (int i = 0; i < 8; i++)
			{
				var obj = Instantiate(gameObject) as GameObject;
				var cube = obj.transform;
				cube.parent = transform.parent;
				cube.name = "Cube";
				cube.localScale = 0.5f * transform.localScale;
				cube.position = transform.TransformPoint(directions[i] / 4);
				cube.GetComponent<Rigidbody>().AddForce(Power * Random.insideUnitSphere, ForceMode.VelocityChange);
				cube.GetComponent<Renderer>().material.color = color;
			}
			Destroy(gameObject);
		}
	}
	
}
