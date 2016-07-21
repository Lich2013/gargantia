using UnityEngine;
using System.Collections;
using TouchScript;
using TouchScript.Gestures;

public class FoodHandler : MonoBehaviour {

	Rigidbody2D rb2d;
	float leftBorder;
	float rightBorder;
	float bottomBorder;
	float topBorder;

	private GameManager gameManager;
	
	public float Power = 10.0f;
	public int TimeMoveInterval = 50;
	
	// Use this for initialization
	void Start () {
		rb2d = GetComponent<Rigidbody2D>();
		gameManager = GameObject.Find ("GameManager").GetComponent<GameManager> ();

		//GetComponent<GravityEffect> ().enabled = false;
	}
	private int seconds = 0;
	void FixedUpdate() {
		if (seconds++ % TimeMoveInterval == 0) {
			rb2d.AddForce (new Vector2 (Random.Range (-1f, 1f), Random.Range (-1f, 1f)) * Power);
		}
	}
	
	void OnEnable() {
		GetComponent<TapGesture>().Tapped += HandleTapped;
	}

	public void Consumed() {
		gameObject.SetActive (false);
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
				var newFood = obj.transform;
				newFood.parent = transform.parent;
				newFood.name = "Food";
				newFood.tag = "Food";
				newFood.localScale = 0.5f * transform.localScale;
				newFood.GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range (0, 1), Random.Range (0, 1)));
				newFood.GetComponent<Renderer>().material.color = color;
			}
			Destroy(gameObject);
		}
	}
	
}
