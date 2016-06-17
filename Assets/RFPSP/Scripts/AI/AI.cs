//AI.cs by Azuline Studios© All Rights Reserved
//Allows NPC to track and attack targets and patrol waypoints.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AI : MonoBehaviour {
	[HideInInspector]
	public bool spawned = false;
	[HideInInspector]
	public GameObject playerObj;
	private Transform playerTransform;
	[HideInInspector]
	public GameObject NPCMgrObj;
	[HideInInspector]
	public GameObject weaponObj;//currently equipped weapon object of player
	[HideInInspector]
	public FPSRigidBodyWalker FPSWalker;
	private NPCAttack NPCAttackComponent;
	private PlayerWeapons PlayerWeaponsComponent;
	private WeaponBehavior WeaponBehaviorComponent;
	[HideInInspector]
	public CharacterDamage TargetCharacterDamageComponent;
	[HideInInspector]
	public NPCSpawner NPCSpawnerComponent;
	[HideInInspector]
	public NPCRegistry NPCRegistryComponent;
	[HideInInspector]
	public AI TargetAIComponent;
	[HideInInspector]
	public NavMeshAgent agent;
	private CapsuleCollider capsule;

	private Animation AnimationComponent;
	private Animator AnimatorComponent;
	private float animSpeed;
	[Tooltip("If true, AI.cs will use Mecanim animations for this NPC.")]
	public bool useMecanim;
	[Tooltip("The object with the Animation/Animator component which will be accessed by AI.cs to play NPC animations. If none, this root object will be checked for the Animator/Animations component.")]
	public Transform objectWithAnims;
	[Tooltip("Chance between 0 and 1 that NPC will spawn. Used to randomize NPC locations and surprise the player.")]
	[Range(0.0f, 1.0f)]
	public float randomSpawnChance = 1.0f;
	
	//NPC movement speeds
	public float runSpeed = 6.0f;//movement speed of the NPC
	public float walkSpeed = 1.0f;
	public float walkAnimSpeed = 1.0f;
	public float runAnimSpeed = 1.0f;
	private float speedAmt = 1.0f;
	private float rotationSpeed = 5.0f;

	[Tooltip("Sets the alignment of this NPC. 1 = friendly to player and hostile to factions 2 and 3, 2 = hostile to player and factions 1 and 3, 3 = hostile to player and factions 1 and 2.")]
	public int factionNum = 1;//1 = human(friendly to player), 2 = alien(hostile to player), 3 = zombie(hostile to all other factions)
	[Tooltip("If false, NPC will attack any character that attacks it, regardless of faction.")]
	public bool ignoreFriendlyFire;
	[HideInInspector]
	public bool playerAttacked;//to make friendly NPCs go hostile if attacked by player
	[HideInInspector]
	public float attackedTime;

	//waypoints and patrolling
	[HideInInspector]
	public Transform myTransform;
	public bool huntPlayer;
	public bool patrolOnce;
	//[HideInInspector]
	public bool walkOnPatrol = true;
	private  Transform curWayPoint;
	[Tooltip("Drag the parent waypoint object with the WaypointGroup.cs script attached here. If none, NPC will stand watch instead of patrolling.")]
	public WaypointGroup waypointGroup;
	[Tooltip("The number of the waypoint in the waypoint group which should be followed first.")]
	public int firstWaypoint = 1;
	private bool  countBackwards = false;
	private float pickNextWaypointDistance = 2.0f;
	private Vector3 startPosition;

	private AudioSource footStepsFx;
	public AudioClip[] footSteps;
	public float walkStepTime = 1.0f;
	public float runStepTime = 1.0f;
	private float stepInterval;
	private float stepTime;

	//targeting and attacking
	public float shootRange = 15.0f;//minimum range to target for attack
	public float attackRange = 30.0f;//range that NPC will start chasing target until they are within shootRange
	[Tooltip("Range that NPC will hear player attacks.")]
	public float listenRange = 30.0f;
	[Tooltip("Time between shots (longer for burst weapons).")]
	public float shotDuration = 0.0f;
	public float shootAnimSpeed = 1.0f;
	[HideInInspector]
	public float attackRangeAmt = 30.0f;//increased by character damage script if NPC is damaged by player
	[Tooltip("Percentage to reduce enemy search range if player is crouching.")]
	public float sneakRangeMod = 0.4f;
	private float shootAngle = 3.0f;
	[Tooltip("Time before atack starts, to allow weapon to be raised before firing.")]
	public float delayShootTime = 0.35f;
	[Tooltip("Height of rayCast origin which detects targets (can be raised if NPC origin is at their feet).")]
	public float eyeHeight = 0.4f;
	[Tooltip("Draws spheres in editor for position and eye height.")]
	public bool drawDebugGizmos;
	[HideInInspector]
	public Transform target = null;
	[HideInInspector]
	public Collider targetCol;
	[HideInInspector]
	public float targetRadius;
	private float attackTime = -16.0f;//time last attacked
	private bool attackFinished = true;
	private bool turning;//true if turning to face target

	private float targetDistance;
	private Vector3 targetDirection;
	private RaycastHit[] hits;
	private bool sightBlocked;//true if sight to target is blocked
	[HideInInspector]
	public float targetEyeHeight;
	private bool pursueTarget;
	[HideInInspector]
	public Vector3 lastVisibleTargetPosition;
	[HideInInspector]
	public float timeout = 0.0f;//to allow NPC to resume initial behavior after searching for target
	[HideInInspector]
	public bool heardPlayer = false;
	[HideInInspector]
	public bool damaged;//true if attacked
	private bool damagedState;

	[HideInInspector]
	public LayerMask searchMask = 0;//only layers to include in target search (for efficiency)

	void Start(){

		NPCMgrObj = GameObject.Find("NPC Manager");
		NPCRegistryComponent = NPCMgrObj.GetComponent<NPCRegistry>();

		if(Random.value > randomSpawnChance){
			Destroy(myTransform.gameObject);
		}else{
			NPCRegistryComponent.Npcs.Add(myTransform.gameObject.GetComponent<AI>());//register this NPC with the NPCRegistry
		}
	}
	
	void OnEnable(){
		
		myTransform = transform;
		startPosition = myTransform.position;
		timeout = 0.0f;
		Mathf.Clamp01(randomSpawnChance);
		capsule = myTransform.GetComponent<CapsuleCollider>();

		//initialize audiosource for footsteps
		footStepsFx = myTransform.gameObject.AddComponent<AudioSource>();
		footStepsFx.spatialBlend = 1.0f;
		footStepsFx.volume = 1.0f;
		footStepsFx.pitch = 1.0f;
		footStepsFx.dopplerLevel = 0.0f;
		footStepsFx.bypassEffects = true;
		footStepsFx.bypassListenerEffects = true;
		footStepsFx.bypassReverbZones = true;
		footStepsFx.maxDistance = 10.0f;
		footStepsFx.rolloffMode = AudioRolloffMode.Linear;

		//set layermask to layers such as layer 10 (world collision) and 19 (interactive objects) for target detection 
		searchMask = ~(~(1 << 10) & ~(1 << 19) & ~(1 << 13) & ~(1 << 11) & ~(1 << 20));
		
		//if there is no objectWithAnims defined, use the Animation Component attached to this game object
		if(objectWithAnims == null){objectWithAnims = transform;}

		if(!useMecanim){//initialize legacy animations
			AnimationComponent = objectWithAnims.GetComponent<Animation>();
			// Set all animations to loop
			AnimationComponent.wrapMode = WrapMode.Loop;
			// Except our action animations, Dont loop those
			AnimationComponent["shoot"].wrapMode = WrapMode.Once;
			// Put idle and run in a lower layer. They will only animate if our action animations are not playing
			AnimationComponent["idle"].layer = -1;
			AnimationComponent["walk"].layer = -1;
			AnimationComponent["run"].layer = -1;
			
			AnimationComponent["walk"].speed = walkAnimSpeed;
			AnimationComponent["shoot"].speed = shootAnimSpeed;
			AnimationComponent["run"].speed = runAnimSpeed;
			
			AnimationComponent.Stop();
		}else{
			AnimatorComponent = objectWithAnims.GetComponent<Animator>();//set reference to Mecanim animator component
		}
		
		//initialize AI vars
		playerObj = Camera.main.transform.GetComponent<CameraKick>().playerObj;
		playerTransform = playerObj.transform;
		PlayerWeaponsComponent = Camera.main.transform.GetComponent<CameraKick>().weaponObj.GetComponentInChildren<PlayerWeapons>();
		FPSWalker = playerObj.GetComponent<FPSRigidBodyWalker>();
		NPCAttackComponent = GetComponent<NPCAttack>();

		//initialize navmesh agent
		agent = GetComponent<NavMeshAgent>();
		agent.speed = runSpeed;
		agent.acceleration = 60.0f;
		agent.radius = capsule.radius;

		pickNextWaypointDistance = capsule.height * 1.25f;//manually set this value to minimize unreachable (too high) waypoints
		attackRangeAmt = attackRange;

		if(!useMecanim){
			AnimationComponent.CrossFade("idle", 0.3f);
		}else{
			AnimatorComponent.SetBool("Idle", true);
			AnimatorComponent.SetBool("Walk", false);
			AnimatorComponent.SetBool("Run", false);
		}

		if(!spawned){//if spawned, SpawnNPC function will be called from NPCSpawner.cs. Otherwise, spawn now.
			StartCoroutine(SpawnNPC());
		}
		
	}

	//initialize NPC behavior
	public IEnumerator SpawnNPC(){

		yield return new WaitForSeconds(1.0f);//wait for a time to allow other scripts and gameobjects to initialize after level load

		if(agent.isOnNavMesh){
			StartCoroutine(PlayFootSteps());
			if(!huntPlayer){
				//determine if NPC should patrol or stand watch
				if(waypointGroup && waypointGroup.wayPoints[firstWaypoint - 1]){
					curWayPoint = waypointGroup.wayPoints[firstWaypoint - 1];
					speedAmt = runSpeed;
					startPosition = curWayPoint.position;
					MoveTowards (curWayPoint.position);
					StartCoroutine(Patrol());
				}else{
					MoveTowards (startPosition);
					StartCoroutine(StandWatch());
				}
			}else{
				//hunt the player accross the map
				factionNum = 2;
				target = playerTransform;
				targetEyeHeight = FPSWalker.capsule.height * 0.25f;
				lastVisibleTargetPosition = target.position + (target.up * targetEyeHeight);
				attackRange = 2048.0f;
				StartCoroutine(AttackTarget());

				speedAmt = runSpeed;
				MoveTowards(target.position);
			}
		}else{
			Debug.Log("<color=red>NPC can't find Navmesh:</color> Please bake Navmesh for this scene or reposition NPC closer to navmesh.");
		}
	}

	//draw debug spheres in editor
	void OnDrawGizmos() {
		if(drawDebugGizmos){
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(transform.position, 0.2f);
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(transform.position + new Vector3(0.0f, eyeHeight, 0.0f), 0.2f);
		
			Vector3 myPos = transform.position + (transform.up * eyeHeight);
			Vector3 targetPos = lastVisibleTargetPosition;
			Vector3 testdir1 = (targetPos - myPos).normalized;
			float distance = Vector3.Distance(myPos, targetPos);
			Vector3 testpos3 = myPos + (testdir1 * distance);
			
			if (Physics.Linecast(myPos, targetPos)) {
				Gizmos.color = Color.red;
			}else{
				Gizmos.color = Color.green;
			}

			Gizmos.DrawLine (myPos, testpos3);
			Gizmos.DrawSphere(testpos3, 0.2f);

			
			if(target){
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine (myPos, target.position + (transform.up * targetEyeHeight));
			}
		}
		
	}
	
	IEnumerator StandWatch(){
		while (true) {

			if(!huntPlayer){//expand search radius if attacked
				if(attackedTime + 6.0 > Time.time){
					attackRangeAmt = attackRange * 3.0f;//expand enemy search radius if attacked to defend against sniping
				}else{
					attackRangeAmt = attackRange;
				}
			}

			if ((target && CanSeeTarget()) || heardPlayer){
				yield return StartCoroutine(AttackTarget());
			}else{
				NPCRegistryComponent.FindClosestTarget(myTransform.gameObject, attackRangeAmt, factionNum);
			}
			if(attackTime < Time.time){
				if(Vector3.Distance(startPosition, myTransform.position) > pickNextWaypointDistance){
					speedAmt = walkSpeed;
					MoveTowards(startPosition);
				}else{
					//play idle animation
					speedAmt = 0.0f;
					SetSpeed(speedAmt);
					if(!useMecanim){
						AnimationComponent.CrossFade("idle", 0.3f);
					}else{
						AnimatorComponent.SetBool("Idle", true);
					}
				}
			}
			
			yield return new WaitForSeconds(1.0f);
		}
		
	}
	
	IEnumerator Patrol(){
		while (true) {
			if(curWayPoint && waypointGroup){//patrol if NPC has a current waypoint, otherwise stand watch
				Vector3 waypointPosition = curWayPoint.position;
				float waypointDist = Vector3.Distance(waypointPosition, myTransform.position);
				int waypointNumber = waypointGroup.wayPoints.IndexOf(curWayPoint);


				//if NPC is close to a waypoint, pick the next one
				if((patrolOnce && waypointNumber == waypointGroup.wayPoints.Count - 1)){
					if(waypointDist < pickNextWaypointDistance){
						speedAmt = 0.0f;
						startPosition = waypointPosition;
						StartCoroutine(StandWatch());
						yield break;//cancel patrol if patrolOnce var is true
					}
				}else{	
					if(waypointDist < pickNextWaypointDistance){
						if(waypointGroup.wayPoints.Count == 1){
							speedAmt = 0.0f;
							startPosition = waypointPosition;
							StartCoroutine(StandWatch());
							yield break;//cancel patrol if NPC has reached their only waypoint
						}
						curWayPoint = PickNextWaypoint (curWayPoint, waypointNumber);
						if(spawned && Vector3.Distance(waypointPosition, myTransform.position) < pickNextWaypointDistance){
							walkOnPatrol = true;//make spawned NPCs run to their first waypoint, but walk on the patrol
						}
					}
				}

				if(!huntPlayer){//expand search radius if attacked
					if(attackedTime + 6.0 > Time.time){
						attackRangeAmt = attackRange * 3.0f;//expand enemy search radius if attacked to defend against sniping
					}else{
						attackRangeAmt = attackRange;
					}
				}

				//determine if player is within sight of NPC
				if((target && CanSeeTarget()) || heardPlayer){
					yield return StartCoroutine(AttackTarget());
				}else{
					NPCRegistryComponent.FindClosestTarget(myTransform.gameObject, attackRangeAmt, factionNum);
					// Move towards our target
					if(attackTime < Time.time){
						//determine if NPC should walk or run on patrol
						if(walkOnPatrol){speedAmt = walkSpeed;}else{speedAmt = runSpeed;}
						MoveTowards(waypointPosition);
					}
				}
			}else{
				StartCoroutine(StandWatch());
                yield break;// return false;
			}
			yield return new WaitForSeconds(1.0f);
		}

	}


	bool CanSeeTarget(){

		//target player
		if(factionNum != 1 || playerAttacked){

			float playerDistance = Vector3.Distance(myTransform.position + (myTransform.up * eyeHeight), playerTransform.position + (playerTransform.up * FPSWalker.capsule.height * 0.25f));

			//listen for player attacks
			if(!heardPlayer && !huntPlayer){
				if(playerDistance < listenRange && (target == playerTransform || target == null)){
					if(PlayerWeaponsComponent.CurrentWeaponBehaviorComponent){
						WeaponBehaviorComponent = PlayerWeaponsComponent.CurrentWeaponBehaviorComponent;
						if(WeaponBehaviorComponent.shootStartTime + 2.0f > Time.time && !WeaponBehaviorComponent.silentShots){
							targetEyeHeight = FPSWalker.capsule.height * 0.25f;
							target = playerTransform;
							timeout = Time.time + 6.0f;
							heardPlayer = true;
							return true;
						}
					}
				}
			}

			if(huntPlayer){
				targetEyeHeight = FPSWalker.capsule.height * 0.25f;
				target = playerTransform;
			}

			if(playerDistance < attackRangeAmt){

				//target lean collider if player is leaning around a corner
				if(Mathf.Abs(FPSWalker.leanAmt) > 0.1f && playerDistance > 2.0f && target == playerTransform){
					targetEyeHeight = 0.0f;
					target = FPSWalker.leanObj.transform;
				}
				//target main player object if they are not leaning
				if((Mathf.Abs(FPSWalker.leanAmt) < 0.1f || playerDistance < 2.0f) && target == FPSWalker.leanObj.transform){
					targetEyeHeight = FPSWalker.capsule.height * 0.25f;
					target = playerTransform;
				}

			}

		}

		//calculate range and LOS to target
		if(target){
			Vector3 myPos = myTransform.position + (myTransform.up * eyeHeight);
			Vector3 targetPos = target.position + (target.up * targetEyeHeight);
			targetDistance = Vector3.Distance(myPos, targetPos);
			targetDirection = (targetPos - myPos).normalized;

			if(targetDistance > attackRangeAmt){
				sightBlocked = true;
				return false;//don't continue to check LOS if target is not within attack range
			}

			//check LOS with sphere casts and raycasts
			if(target == playerTransform){
				hits = Physics.SphereCastAll(myPos, capsule.radius * 0.5f, targetDirection, targetDistance, searchMask);
			}else if(target == FPSWalker.leanObj.transform){
				hits = Physics.SphereCastAll(myPos, capsule.radius * 0.01f, targetDirection, targetDistance, searchMask);
			}else{
				hits = Physics.RaycastAll (myPos, targetDirection, targetDistance, searchMask);
			}
			sightBlocked = false;

			targetCol = target.GetComponent<Collider>();

			//check if NPC can see their target
			for(int i = 0; i < hits.Length; i++){
				if((hits[i].collider != targetCol//hit is not target
				&& hits[i].collider != capsule)//hit is not this NPC's collider
			    || (!playerAttacked//attack player if they attacked us (friendly fire)
			    	&& (factionNum == 1 && target != playerObj && (hits[i].collider == FPSWalker.capsule//try not to shoot the player if friendly 
                   		|| hits[i].collider == FPSWalker.leanCol)))
				){
					sightBlocked = true;
					break;
				}
			}
			
			if(!sightBlocked){
				if(target != FPSWalker.leanObj.transform){
					pursueTarget = false;
					return true;
				}else{
					pursueTarget = true;//true when NPC has seen only the player leaning around a corner
					return true;
				}
			}else{
				return false;
			}
			
		}else{
			return false;
		}
	
	}
	
	IEnumerator Shoot(){

		attackFinished = false;

		// Start shoot animation
		if(!useMecanim){
			AnimationComponent.CrossFade("shoot", 0.3f);
		}else{
			AnimatorComponent.CrossFade("Attack", 0.1f, -1, 0.0f);
		}
		//don't move during attack
		speedAmt = 0.0f;
		SetSpeed(speedAmt);
		agent.Stop();

		// Wait until delayShootTime to allow part of the animation to play
		yield return new WaitForSeconds(delayShootTime);
		//attack
		NPCAttackComponent.Fire();
		attackTime = Time.time + 2.0f;
		// Wait for the rest of the animation to finish
		if(!useMecanim){
			yield return new WaitForSeconds(AnimationComponent["shoot"].length + delayShootTime + Random.Range(shotDuration, shotDuration + 0.75f));
		}else{
			yield return new WaitForSeconds(delayShootTime + Random.Range(shotDuration, shotDuration + 0.75f));
		}

		attackFinished = true;

	}
	
	IEnumerator AttackTarget(){
		while (true) {
			// no target - stop hunting
			if(!huntPlayer){
				if(target == null){
					timeout = 0.0f;
					heardPlayer = false;
                    yield break;// return false;
				}
			}else{//hunt player across map
				speedAmt = runSpeed;
				MoveTowards(target.position);
			}

			float distance = Vector3.Distance(myTransform.position, target.position);

			// Target is too far away - give up	
			if(distance > attackRangeAmt && !huntPlayer){
				speedAmt = walkSpeed;
				target = null;
                yield break;//return false;
			}

			if(pursueTarget){//should NPC attack player collider or leaning collider?
				lastVisibleTargetPosition = FPSWalker.leanObj.transform.position;
			}else{
				lastVisibleTargetPosition = target.position + (target.up * targetEyeHeight);
			}

			//search for player if their attacks have been heard
			if(!huntPlayer && heardPlayer && target == playerTransform){
				if(distance > shootRange){
					if(!huntPlayer){
						speedAmt = runSpeed;
						SearchTarget(lastVisibleTargetPosition);
					}
				}
			}
			
			if(CanSeeTarget()){
				timeout = Time.time + 6.0f;
				if(distance > shootRange){
					if(!huntPlayer){
						SearchTarget(lastVisibleTargetPosition);
					}
				}else{//close to target, rotate NPC to face it
					if(!turning){
						StartCoroutine(RotateTowards(lastVisibleTargetPosition));
					}
					speedAmt = 0.0f;
					SetSpeed(speedAmt);
					agent.speed = speedAmt;
				}
				
				speedAmt = runSpeed;

				Vector3 forward = myTransform.TransformDirection(Vector3.forward);
				Vector3 targetDirection = lastVisibleTargetPosition - (myTransform.position + (myTransform.up * eyeHeight));
				targetDirection.y = 0;
				
				float angle = Vector3.Angle(targetDirection, forward);
				
				// Start shooting if close and player is in sight
				if(distance < shootRange && angle < shootAngle){
					if(attackFinished){
						yield return StartCoroutine(Shoot());
					}else{
						speedAmt = 0.0f;
						SetSpeed(speedAmt);
						agent.Stop();
					}
				}
				
			}else{
				if(!huntPlayer){
					if(attackFinished){
						if(timeout > Time.time){
							speedAmt = runSpeed;
							SetSpeed(speedAmt);
							SearchTarget(lastVisibleTargetPosition);
						}else{//if timeout has elapsed and target is not visible, resume initial behavior
							heardPlayer = false;
							speedAmt = 0.0f;
							SetSpeed(speedAmt);
							agent.Stop();
							target = null;
							targetCol = null;
                            yield break;// return false;
						}
					}
				}
			}
			
			yield return new WaitForFixedUpdate();
		}
	}

	//look for target at a location
	void SearchTarget( Vector3 position  ){
		if(attackFinished){
			if(target){
				if(!huntPlayer){
					speedAmt = runSpeed;
					MoveTowards(target.position);
				}
			}else{
				timeout = 0.0f;
			}
		}
	}

	//rotate to face target
	IEnumerator RotateTowards( Vector3 position  ){
		float turnTime;
		turnTime = Time.time;

		SetSpeed(0.0f);
		agent.Stop();

		while(turnTime + 4.0 > Time.time){
			turning = true;

			if(pursueTarget){
				position = FPSWalker.leanObj.transform.position;
			}else{
				if(target){
					lastVisibleTargetPosition = target.position + (target.up * targetEyeHeight);
				}else{
					lastVisibleTargetPosition = position;
				}
			}
			
			Vector3 direction = lastVisibleTargetPosition - myTransform.position;
			direction.y = 0;

			// Rotate towards the target
			myTransform.rotation = Quaternion.Slerp (myTransform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime * 4);
			myTransform.eulerAngles = new Vector3(0, myTransform.eulerAngles.y, 0);
			yield return 0;
		}
		turning = false;
	}

	//set navmesh destination and set NPC speed
	void MoveTowards( Vector3 position  ){
		if(attackFinished){
			agent.SetDestination(position);
			agent.Resume();
			agent.speed = speedAmt;
			SetSpeed(speedAmt);
		}
	}
	
	//pick the next waypoint and determine if patrol should continue forward or backward through waypoint group
	Transform PickNextWaypoint( Transform currentWaypoint, int curWaypointNumber  ){
		
		Transform waypoint = currentWaypoint;

		if(!countBackwards){
			if(curWaypointNumber < waypointGroup.wayPoints.Count -1){
				waypoint = waypointGroup.wayPoints[curWaypointNumber + 1];
			}else{
				waypoint = waypointGroup.wayPoints[curWaypointNumber - 1];
				countBackwards = true;
			}
		}else{
			if(curWaypointNumber != 0){
				waypoint = waypointGroup.wayPoints[curWaypointNumber - 1];
			}else{
				waypoint = waypointGroup.wayPoints[curWaypointNumber + 1];
				countBackwards = false;
			}
		}
		return waypoint;
	}

	//play animations for NPC moving state/speed and set footstep sound intervals
	void SetSpeed( float speed  ){
		if(!useMecanim){
			if (speed > walkSpeed){
				AnimationComponent.CrossFade("run");
				stepInterval = runStepTime;
			}else{
				if(speed > 0.0f){
					AnimationComponent.CrossFade("walk");
					stepInterval = walkStepTime;
				}else{
					AnimationComponent.CrossFade("idle");
					stepInterval = -1;
				}
			}
		}else{
			if (speed > walkSpeed){
				AnimatorComponent.SetBool("Run", true);
				AnimatorComponent.SetBool("Walk", false);
				AnimatorComponent.SetBool("Idle", false);
				stepInterval = runStepTime;
			}else{
				if(speed > 0.0f){
					AnimatorComponent.SetBool("Walk", true);
					AnimatorComponent.SetBool("Idle", false);
					AnimatorComponent.SetBool("Run", false);
					stepInterval = walkStepTime;
				}else{
					if(attackFinished && attackTime < Time.time){
						AnimatorComponent.SetBool("Idle", true);
					}else{
						AnimatorComponent.SetBool("Idle", false);
					}
					AnimatorComponent.SetBool("Walk", false);
					AnimatorComponent.SetBool("Run", false);
					stepInterval = -1;
				}
			}
		}
	}

	IEnumerator PlayFootSteps(){
		while(true){
			if(stepInterval > 0.0f && stepTime + stepInterval < Time.time){
				if(footSteps.Length > 0){
					footStepsFx.clip = footSteps[Random.Range(0, footSteps.Length)];
					footStepsFx.PlayOneShot(footStepsFx.clip);
					stepTime = Time.time;
				}
			}
			yield return 0;
		}
	}

//	//used to change the faction of the NPC
//	public void ChangeFaction(int factionChange){
//		target = null;
//		factionNum = factionChange;
//	}

}