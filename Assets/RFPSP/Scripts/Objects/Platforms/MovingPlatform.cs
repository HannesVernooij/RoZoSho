//MovingPlatform.cs by Azuline Studios© All Rights Reserved
//script for moving a platform horizontally back and forth
using UnityEngine;
using System.Collections;

public class MovingPlatform : MonoBehaviour {
	public Transform pointB;
	public Transform pointA;
	public float speed = 1.0f;
	private float direction = 1.0f;
	private Transform myTransform;

	IEnumerator Start (){
		myTransform = transform;
	    while (true) {
	    
			if (myTransform.position.z < pointA.position.z){
				direction = 1;
			}else{
				if(myTransform.position.z > pointB.position.z){direction = -1;}
			}
			float delta = Time.deltaTime * 60;
			myTransform.Translate(direction  * -speed * delta, 0, direction  * speed * delta, Space.World);
				
			yield return 0;
	    }
	}
	
}