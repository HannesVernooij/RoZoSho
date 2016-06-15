//MovingElevator.cs by Azuline Studios© All Rights Reserved
//script for moving an elevator vertically up and down
using UnityEngine;
using System.Collections;

public class MovingElevator : MonoBehaviour {
	public Transform pointB;
	private Vector3 pointA;
	public float speed = 1.0f;
	private float direction = 1.0f;
	private Transform myTransform;
	
	IEnumerator Start (){
		myTransform = transform;
		pointA = transform.position;
		while (true) {
		
			if (myTransform.position.y > pointA.y){
				direction = -1;
			}else{
				if(myTransform.position.y < pointB.position.y){direction = 1;}
			}
			float delta = Time.deltaTime * 60;
			myTransform.Translate(0, direction  * speed * delta, 0, Space.World);
			
			yield return 0;
		}
	}
	
}