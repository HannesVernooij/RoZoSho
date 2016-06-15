//ElevatorCrushCollider.cs by Azuline Studios© All Rights Reserved
//script for instant death collider which kills player on contact
using UnityEngine;
using System.Collections;

public class ElevatorCrushCollider : MonoBehaviour {
	public AudioClip squishSnd;
	private bool fxPlayed;
	
	void OnTriggerEnter ( Collider col  ){
		if(col.gameObject.tag == "Player"){
			FPSPlayer player = col.GetComponent<FPSPlayer>();
			if (player && !fxPlayed) {
				player.ApplyDamage(player.maximumHitPoints + 1.0f);
				PlayAudioAtPos.instance.PlayClipAt(squishSnd, player.transform.position, 0.75f, 1.0f, 1.0f);
				fxPlayed = true;
			}
		}
	}
	
	void Reset (){
		if (GetComponent<Collider>() == null){
			gameObject.AddComponent<BoxCollider>();
			GetComponent<Collider>().isTrigger = true;
		}
	}
}