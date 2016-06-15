//NPCRegistry.cs by Azuline Studios© All Rights Reserved
//Manages registry of all NPCs in scene and assigns targets,
//taking NPC faction alignments into account.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCRegistry : MonoBehaviour {
	private FPSRigidBodyWalker FPSWalker;
	[HideInInspector]
	public List<AI> Npcs = new List<AI>();//list containing references to all NPCs' AI.cs components
	private GameObject playerObj;
	private Transform playerTransform;
	private float nearestNpcDist;
	private float NpcDist = 0.0f;
	private float playerDist = 0.0f;
	private float playerDistMod;//reduce player search distance when player is crouching or prone
	private RaycastHit hit;

	void Start () {
		playerObj = Camera.main.transform.GetComponent<CameraKick>().playerObj;
		FPSWalker = playerObj.GetComponent<FPSRigidBodyWalker>();
	}

	//Remove an NPC from the NPC registry
	public void UnregisterNPC(AI NpcAI){
		for(int i = 0; i < Npcs.Count; i++){
			if(Npcs[i] == NpcAI){
				Npcs.RemoveAt(i);
				break;
			}
		}
	}

	//Find the closest hostile target for this NPC, taking into account faction alignments and player stance
	public void FindClosestTarget(GameObject NPC, float distance, int myFaction){

		nearestNpcDist = distance;

		AI NpcAIcomponent = NPC.GetComponent<AI>();
		AI nearestNpcAIcomponent = null;
		playerTransform = playerObj.transform;

		playerDist = Vector3.Distance(NpcAIcomponent.myTransform.position, playerTransform.position);

		//calculate range based on player stance/sneaking
		if(!NpcAIcomponent.heardPlayer){
			if(FPSWalker.crouched){
				playerDistMod = distance * NpcAIcomponent.sneakRangeMod;//reduce NPC's attack range by sneakRangeMod amount when player is crouched
			}else if(FPSWalker.prone){
				playerDistMod = distance * (NpcAIcomponent.sneakRangeMod * 0.75f);//reduce NPC's attack range further when player is prone
			}else{
				playerDistMod = distance;
			}
		}else{
			playerDistMod = distance;
		}

		for(int i = 0; i < Npcs.Count; i++){

			if(Npcs[i] && Npcs[i].myTransform.gameObject.activeInHierarchy){

				NpcDist = Vector3.Distance(NpcAIcomponent.myTransform.position, Npcs[i].myTransform.position);

				if(NpcDist < distance && NpcDist < nearestNpcDist && NpcAIcomponent != Npcs[i]){
					//check if potential target's faction is one that this NPC is hostile to
					if(
						   (myFaction == 1 && Npcs[i].factionNum == 2)
					    || (myFaction == 1 && Npcs[i].factionNum == 3)
					    || (myFaction == 2 && Npcs[i].factionNum == 1)
					    || (myFaction == 2 && Npcs[i].factionNum == 3)
					    || (myFaction == 3 && Npcs[i].factionNum != 3)
					){
						nearestNpcDist = NpcDist;
						nearestNpcAIcomponent = Npcs[i];
					}
				}
			}
		}
		//determine if player is closer than the nearest hostile NPC
		if(myFaction != 1
		&& playerDist < playerDistMod 
		&& playerDist < nearestNpcDist){
			//set NPC target to player
			NpcAIcomponent.target = playerTransform;
			NpcAIcomponent.targetEyeHeight = FPSWalker.capsule.height * 0.25f;
			NpcAIcomponent.TargetAIComponent = null;

		}else{
			//set NPC target to nearest Hostile NPC
			if(nearestNpcAIcomponent){
				NpcAIcomponent.TargetAIComponent = nearestNpcAIcomponent;
				NpcAIcomponent.targetEyeHeight = nearestNpcAIcomponent.eyeHeight;
				NpcAIcomponent.target = nearestNpcAIcomponent.myTransform;
				NpcAIcomponent.lastVisibleTargetPosition = nearestNpcAIcomponent.myTransform.position + (nearestNpcAIcomponent.myTransform.up * nearestNpcAIcomponent.eyeHeight); 
			}

		}

	}

}