using UnityEngine;
using System.Collections;

public class endGame : MonoBehaviour {

	void OnTriggerEnter(Collider coll)
	{
		Debug.Log ("test");
		Application.Quit ();
	}
}
