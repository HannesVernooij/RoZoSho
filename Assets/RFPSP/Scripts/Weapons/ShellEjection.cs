//ShellEjection.cs by Azuline Studios© All Rights Reserved
//Rotates and moves instantiated rigidbody shell object and lerps mesh shell object.
using UnityEngine;
using System.Collections;

public class ShellEjection : MonoBehaviour{
	//set up external script references
	[HideInInspector]
	public FPSPlayer FPSPlayerComponent;
	private FPSRigidBodyWalker FPSWalkerComponent;
	private PlayerWeapons PlayerWeaponsComponent;
	private WeaponBehavior WeaponBehaviorComponent;
	[HideInInspector]
	public Rigidbody RigidbodyComponent;
	[HideInInspector]
	public Rigidbody PlayerRigidbodyComponent;
	//objects accessed by this script
	[HideInInspector]
	public GameObject playerObj;
	[HideInInspector]
	public GameObject gunObj;
	private Transform gunObjTransform;
	[HideInInspector]
	public Transform lerpShell;//the mesh shell object that lerps the rigidbody shell's position and rotation
	private Vector3 tempPos;
	private Vector3 tempRot;
	private bool rotated;
	private Transform myTransform;
	private Transform FPSMainTransform;
	public AudioClip[] shellSounds;//shell bounce sounds
	//shell states and settings
	private bool parentState = true;
	private bool soundState = true;
	//shell rotation
	private float rotateAmt = 0.0f;//amount that the shell rotates, scaled up after ejection
	[HideInInspector]
	public float shellRotateUp = 0.0f;//amount of vertical shell rotation
	[HideInInspector]
	public float shellRotateSide = 0.0f;//amount of horizontal shell rotation	
	//timers and shell lifetime duration
	private float shellRemovalTime = 0.0f;//time that this shell will be removed from the level
	[HideInInspector]
	public int shellDuration = 0;//time in seconds that shells persist in the world before being removed	
	private float startTime = 0.0f;//time that the shell instance was created in the world
	//private float landTime;

	void Start(){
		//set up external script references
		FPSWalkerComponent = FPSPlayerComponent.FPSWalkerComponent;
		PlayerWeaponsComponent = FPSPlayerComponent.PlayerWeaponsComponent;
		WeaponBehaviorComponent = PlayerWeaponsComponent.CurrentWeaponBehaviorComponent;
		myTransform = transform;//manually set transform for efficiency

		Physics.IgnoreCollision(GetComponent<Collider>(), playerObj.GetComponent<Collider>());

		//initialize shell rotation amounts
		shellRotateUp = WeaponBehaviorComponent.shellRotateUp / (Time.fixedDeltaTime * 100.0f);
		shellRotateSide = WeaponBehaviorComponent.shellRotateSide / (Time.fixedDeltaTime * 100.0f);
		shellDuration = WeaponBehaviorComponent.shellDuration;
		//track the time that the shell was ejected
		startTime = Time.time;
		shellRemovalTime = Time.time + shellDuration;//time that shell will be removed
		RigidbodyComponent.maxAngularVelocity = 100;//allow shells to spin faster than default
		//determine if shell rotates clockwise or counter-clockwise at random
		if(Random.value < 0.5f){shellRotateUp *= -1;} 
		tempPos = myTransform.position;
		lerpShell.position = tempPos;
		//rotate shell
		rotateAmt = 0.1f;
		//apply torque to rigidbody
		RigidbodyComponent.AddRelativeTorque(Vector3.up * (Random.Range (0.1f * 1.75f,rotateAmt) * shellRotateSide));
		RigidbodyComponent.AddRelativeTorque(Vector3.right * (Random.Range (0.1f * 4,rotateAmt * 6) * shellRotateUp));

		StartCoroutine(CalcShellPos());
	}
	
	void Update (){
		//if(soundState || (!soundState /*&& RigidbodyComponent.velocity.y > 0.01f/*landTime + 3.0f > Time.time*/)){
			//smooth/lerp mesh shell object's position and rotation from rigidbody shell's position and rotation
			tempPos = Vector3.Lerp(tempPos, myTransform.position, Time.deltaTime * 64.0f);
			lerpShell.position = tempPos;
			Quaternion shellRot = Quaternion.Euler(myTransform.eulerAngles);
			lerpShell.rotation = Quaternion.Lerp(lerpShell.rotation, shellRot, Time.deltaTime * 64.0f);
		//}
	}
	
	IEnumerator CalcShellPos(){

		while(true){
		
			if(Time.time > shellRemovalTime){
				Object.Destroy(lerpShell.gameObject);
				Object.Destroy(gameObject);
			}
			
			//Check if the player is on a moving platform to determine how to handle shell parenting and velocity
			//if(playerObjTransform.parent == FPSWalkerComponent.mainObj.transform){//if player is not on a moving platform
			if(FPSWalkerComponent.playerParented){
				//Make the shell's parent the weapon object for a short time after ejection
				//to the link shell ejection position with weapon object for more consistent movement,
				if(((startTime + 0.35f < Time.time && parentState) 
				//don't parent shell if switching weapon
				|| (PlayerWeaponsComponent.switching && parentState)
				//don't parent shell if moving weapon to sprinting position or moving while prone
				|| (FPSWalkerComponent.sprintActive && !FPSWalkerComponent.cancelSprint && parentState))
				|| (FPSWalkerComponent.prone && FPSWalkerComponent.moving && parentState)
				&& FPSWalkerComponent.grounded){
					Vector3 tempVelocity = PlayerRigidbodyComponent.velocity;
					tempVelocity.y = 0.0f;
					myTransform.parent = null;
					lerpShell.parent = null;
					//add player velocity to shell when unparenting from player object to prevent shell from suddenly changing direction
					if(!FPSWalkerComponent.sprintActive && !FPSWalkerComponent.canRun){//don't inherit parent velocity if sprinting to prevent visual glitches
						RigidbodyComponent.AddForce(tempVelocity, ForceMode.VelocityChange);
					}
					parentState = false;
				}
			}else{//if player is on elevator, keep gun object as parent for a longer time to prevent strange shell movements
				if(startTime + 0.5f < Time.time && parentState){
					myTransform.parent = null;
					lerpShell.parent = null;
					//add player velocity to shell when unparenting from player object to prevent shell from suddenly changing direction
					RigidbodyComponent.AddForce(PlayerRigidbodyComponent.velocity, ForceMode.VelocityChange);	
					parentState = false;
				}		
			}
			yield return new WaitForSeconds(0.5f);
		}

	}

	void OnCollisionEnter(Collision collision){
		//play a bounce sound when shell object collides with a surface
		if(soundState){
			if (shellSounds.Length > 0){
				PlayAudioAtPos.instance.PlayClipAt(shellSounds[(int)Random.Range(0, (shellSounds.Length))], myTransform.position, 0.75f, 1.0f, 1.0f);
			}
			soundState = false;
			//landTime = Time.time;
		}
		//remove shells if they collide with a moving object like an elevator
		if(collision.gameObject.layer == 15 /*|| collision.rigidbody*/){
			Object.Destroy(lerpShell.gameObject);
			Object.Destroy(gameObject);
		}
	}

}


	