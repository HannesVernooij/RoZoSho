//NPCSpawner.cs by Azuline Studios© All Rights Reserved
//Spawns NPCs, using several parameters to control spawn timing and amounts.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour {

	[Tooltip("Set to the wave manager object if this spawner should be controled by the wave manager")]
	public WaveManager WaveManager;
	[Tooltip("If not linked to a wave manager to spawn NPC waves, this is the NPC prefab that will be spawned")]
	public GameObject NPCPrefab;
	public float spawnDelay = 30.0f;
	private float spawnTime;
	private GameObject NPCInstance = null;
	private List<AI> Npcs = new List<AI>(); 
	private float timeLeft;
	[Tooltip("The waypoint group that this NPC should patrol after spawning.")]
	public WaypointGroup waypointGroup;
	public int firstWaypoint = 1;
	private AI AIcomponent;
	public bool unlimitedSpawning = true;
	[Tooltip("True if this NPC should hunt the player across the map")]
	public bool huntPlayer;
	public int maxActiveNpcs = 3;
	[Tooltip("The number of NPCs to spawn if not spawning unlimited NPCs.")]
	public int NpcsToSpawn = 5;
	[HideInInspector]
	public int spawnedNpcAmt;
	[HideInInspector]
	public bool pauseSpawning;

	void Start (){
		spawnTime = -1024.0f;
	}

	void Update (){

		if(!pauseSpawning){

			if(Npcs.Count < maxActiveNpcs
			&& (unlimitedSpawning || (!unlimitedSpawning && spawnedNpcAmt < NpcsToSpawn))){
				if(spawnTime + spawnDelay < Time.time){
					Spawn(NPCPrefab);
				}
			}

		}

	}

	void Spawn (GameObject NpcPrefab){
		// Make an instance of the NPC
		if(NPCPrefab){
			NPCInstance = Instantiate(NpcPrefab,transform.position,transform.rotation) as GameObject;
		
			Npcs.Add(NPCInstance.GetComponent<AI>());

			NPCInstance.GetComponent<AI>().NPCSpawnerComponent = transform.GetComponent<NPCSpawner>();
	
			AIcomponent = NPCInstance.GetComponent<AI>();
			AIcomponent.spawned = true;
			AIcomponent.waypointGroup = waypointGroup;
			AIcomponent.firstWaypoint = firstWaypoint;
			AIcomponent.walkOnPatrol = false;
			if(huntPlayer){
				AIcomponent.huntPlayer = true;
			}
			StartCoroutine(AIcomponent.SpawnNPC());
			spawnTime = Time.time;
			spawnedNpcAmt ++;
		}
	}

	public void UnregisterSpawnedNPC(AI NpcAI){
		for(int i = 0; i < Npcs.Count; i++){
			if(Npcs[i] == NpcAI){
				Npcs.RemoveAt(i);
				if(WaveManager){
					WaveManager.killedNpcs ++;
					if(WaveManager.killedNpcs >= WaveManager.NpcsToSpawn){
						StartCoroutine(WaveManager.StartWave());
					}
				}
				break;
			}
		}
	}
}