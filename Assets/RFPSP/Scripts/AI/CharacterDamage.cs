//CharacterDamage.cs by Azuline Studios© All Rights Reserved
//Applies damage to NPCs 
using UnityEngine;
using System.Collections;

public class CharacterDamage : MonoBehaviour {
	private AI AIComponent;
	private RemoveBody RemoveBodyComponent;
	public float hitPoints = 100.0f;
	private float initialHitPoints;
	public Transform deadReplacement;
	public AudioClip dieSound;
	[Tooltip("Determine if this object or parent should be removed on death. This is to allow for different hit detection collider types as children of NPC parent.")]
	public bool notParent;
	[Tooltip("Should this NPC's body be removed after Body Stay Time?")]
	public bool  removeBody;
	public float bodyStayTime = 15.0f;
	private Vector3 attackerPos2;
	private Vector3 attackDir2;
	private Transform myTransform;
	private Transform dead;
	//set ray mask to layer 9, the ragdoll layer
	//for applying force to specific ragdoll limbs
	private LayerMask raymask = 1 << 9;
	
	void OnEnable (){
		myTransform = transform;
		AIComponent = myTransform.GetComponent<AI>();
		initialHitPoints = hitPoints;
	}
	//damage NPC
	public void ApplyDamage ( float damage, Vector3 attackDir, Vector3 attackerPos, Transform attacker, bool isPlayer ){

		if (hitPoints <= 0.0f){
			return;
		}

		if(!AIComponent.damaged 
		&& (hitPoints / initialHitPoints) < 0.65f//has NPC been damaged significantly?
		&& attacker
		){
			if(!isPlayer){
				AIComponent.target = attacker;
				AIComponent.TargetAIComponent = attacker.GetComponent<AI>();
				AIComponent.targetEyeHeight = AIComponent.TargetAIComponent.eyeHeight;
			}else{
				if(!AIComponent.ignoreFriendlyFire){//go hostile on a friendly if they repeatedly attacked us
					AIComponent.target = AIComponent.playerObj.transform;
					AIComponent.targetEyeHeight = AIComponent.playerObj.GetComponent<FPSRigidBodyWalker>().capsule.height * 0.25f;
					AIComponent.playerAttacked = true;
					AIComponent.TargetAIComponent = null;
				}
			}
			AIComponent.damaged = true;
		}
		
		//prevent hitpoints from going into negative values
		if(hitPoints - damage > 0.0f){
			hitPoints -= damage;
		}else{
			hitPoints = 0.0f;	
		}
		
		attackDir2 = attackDir;
		attackerPos2 = attackerPos;
		
		//to expand enemy search radius if attacked to defend against sniping
		AIComponent.attackedTime = Time.time;
		
		if (hitPoints <= 0.0f){
			SendMessage("Die");//use SendMessage() to allow other script components on this object to detect NPC death
		}
	}
	
	void Die (){
		
		RaycastHit rayHit;
		// Play a dying audio clip
		if (dieSound){
			PlayAudioAtPos.instance.PlayClipAt(dieSound, transform.position, 1.0f, 1.0f, 1.0f);
		}

		AIComponent.NPCRegistryComponent.UnregisterNPC(AIComponent);//unregister NPC from main NPC registry
		if(AIComponent.spawned && AIComponent.NPCSpawnerComponent){
			AIComponent.NPCSpawnerComponent.UnregisterSpawnedNPC(AIComponent);//unregister NPC from spawner registry
		}

		AIComponent.agent.Stop();
		AIComponent.StopAllCoroutines();
	
		// Replace NPC object with the dead body
		if (deadReplacement) {
			dead = Instantiate(deadReplacement, transform.position, transform.rotation) as Transform;
			RemoveBodyComponent = dead.GetComponent<RemoveBody>();
	
			// Copy position & rotation from the old hierarchy into the dead replacement
			CopyTransformsRecurse(transform, dead);
			
			//apply damage force to NPC ragdoll
			if(Physics.SphereCast(attackerPos2, 0.2f, attackDir2, out rayHit, 750.0f, raymask)
			&& rayHit.rigidbody 
			&& attackDir2.x !=0){
				//apply damage force to the ragdoll rigidbody hit by the sphere cast (can be any body part)
				rayHit.rigidbody.AddForce(attackDir2 * 10.0f, ForceMode.Impulse);
			
			}else{//apply damage force to NPC ragdoll if being damaged by an explosive object or other damage source without a specified attack direction
			
				Component[] bodies;
				bodies = dead.GetComponentsInChildren<Rigidbody>();
				foreach(Rigidbody body in bodies) {
					if(body.transform.name == "Chest"){//only apply damage force to the chest of the ragdoll if damage is from non-player source 
						//calculate direction to apply damage force to ragdoll
						body.AddForce((myTransform.position - attackerPos2).normalized * 10.0f, ForceMode.Impulse);
					}
				}
			}
			
			//initialize the RemoveBody.cs script attached to the NPC ragdoll
			if(RemoveBodyComponent){
				if(removeBody){
					RemoveBodyComponent.enabled = true;
					RemoveBodyComponent.bodyStayTime = bodyStayTime;//pass bodyStayTime to RemoveBody.cs script
				}else{
					RemoveBodyComponent.enabled = false;
				}
			}
			
			//Determine if this object or parent should be removed.
			//This is to allow for different hit detection collider types as children of NPC parent.
			if(notParent){
				Destroy(transform.parent.gameObject);
			}else{
				Destroy(transform.gameObject);
			}
			
		}
	
	}
	
	static void CopyTransformsRecurse ( Transform src , Transform dst ){
		dst.position = src.position;
		dst.rotation = src.rotation;
		
		foreach(Transform child in dst) {
			// Match the transform with the same name
			Transform curSrc = src.Find(child.name);
			if (curSrc)
				CopyTransformsRecurse(curSrc, child);
		}
	}
}