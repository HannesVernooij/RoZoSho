//HealthPickup.cs by Azuline Studios© All Rights Reserved
//script for health pickup items
using UnityEngine;
using System.Collections;

public class HealthPickup : MonoBehaviour {
	private Transform myTransform;
	
	public float healthToAdd = 25.0f;
	public bool removeOnUse = true;//Does this pickup disappear when used/activated by player?
	
	public AudioClip pickupSound;//sound to playe when picking up this item
	public AudioClip fullSound;//sound to play when health is full
	
	public Texture2D healthPickupReticle;//the texture used for the pick up crosshair
	
	void Start () {
		myTransform = transform;//manually set transform for efficiency
	}
	
	void PickUpItem (GameObject user){
		FPSPlayer FPSPlayerComponent = user.GetComponent<FPSPlayer>();
	
		if (FPSPlayerComponent.hitPoints < FPSPlayerComponent.maximumHitPoints){
			//heal player
			FPSPlayerComponent.HealPlayer(healthToAdd);
			
			if(pickupSound){PlayAudioAtPos.instance.PlayClipAt(pickupSound, myTransform.position, 0.75f, 1.0f, 1.0f);}
			
			if(removeOnUse){
				//remove this pickup
				Object.Destroy(gameObject);
			}
			
		}else{
			//player is already at max health, just play beep sound effect
			if(fullSound){PlayAudioAtPos.instance.PlayClipAt(fullSound, myTransform.position, 0.75f, 1.0f, 1.0f);}		
		}
	}
}