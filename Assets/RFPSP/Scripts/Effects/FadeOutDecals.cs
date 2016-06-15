//FadeOutDecals.cs by Azuline Studios© All Rights Reserved
using UnityEngine;
using System.Collections;
//script to fade out decals like bullet marks smoothly
public class FadeOutDecals : MonoBehaviour {

	float startTime;
	public int markDuration = 10;
	[HideInInspector]
	public MeshRenderer hitMesh;
	private Renderer RendererComponent;
	private Color tempColor;
		
	void Start (){
		startTime = Time.time;
		RendererComponent = GetComponent<Renderer>();
		tempColor =  RendererComponent.material.color;
	}
	
	void Update (){
		
		if(startTime + markDuration < Time.time){
			tempColor.a -= Time.deltaTime;//fade out the color's alpha amount
			RendererComponent.material.color = tempColor;//set the guiTexture's color to the value(s) of our temporary color vector
		}
		
		//destroy this decal if the hit object's mesh component gets disabled 
		//such as when the player damages and destroys a breakable object
		if(hitMesh){
			if(!hitMesh.enabled){
				Destroy(transform.parent.transform.gameObject);
			}
		}
		
	}
}