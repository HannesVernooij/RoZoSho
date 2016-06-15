//VisibleBody.cs by Azuline Studios© All Rights Reserved
//Positions and rotates visible player model and plays animations
//TODO replace with Mecanim animation setup
using UnityEngine;
using System.Collections;

public class VisibleBody : MonoBehaviour {

	[HideInInspector]
	public GameObject playerObj;
	private Transform playerTransform;
	[HideInInspector]
	public GameObject weaponObj;
	[TooltipAttribute("Object with animation component to animate.")]
	public GameObject objectWithAnims;
	private Animation AnimationComponent;
	private InputControl InputComponent;
	private FPSRigidBodyWalker walkerComponent;
	private FPSPlayer FPSPlayerComponent;
	private GunSway GunSwayComponent;
	[TooltipAttribute("Scale of model while standing.")]
	public float modelStandScale = 1.4f;
	[TooltipAttribute("Scale of model while crouching.")]
	public float modelCrouchScale = 1.1f;
	private float modelScaleAmt = 1.1f;
	public float modelPitch = -7.0f;
	public float modelForward = -0.35f;
	public float modelForwardCrouch = -0.3f;
	private float modelForwardAmt;
	private float modelRightAmt;
	public float modelUp = 1.8f;
	public float modelUpCrouch = 1.1f;
	private float modelUpAmt;
	public float walkAnimSpeed = 1.27f;
	public float crouchWalkAnimSpeed = 0.84f;
	public float sprintAnimSpeed = 2.0f;
	private float speedMax;
	[TooltipAttribute("Anglele to rotate model when strafing.")]
	public float strafeAngle = 40.0f;
	private float rotAngleAmt = 40.0f;
	[TooltipAttribute("Anglele to rotate model when jumping.")]
	public float jumpAngle = 30.0f;
	[TooltipAttribute("Anglele to rotate model when crouching.")]
	public float crouchAngle = 5.0f;
	[TooltipAttribute("Anglele to rotate model when idle.")]
	public float idleAngle = -35.0f;
	public float idleRight = -0.15f;
	private float modelRightMod;
	public float crouchRight = -0.12f;

	private Transform myTransform;
	private Vector3 myScale;

	void Start () {
		playerObj = Camera.main.transform.GetComponent<CameraKick>().playerObj;
		playerTransform = playerObj.transform;
		InputComponent = playerObj.GetComponent<InputControl>();
		FPSPlayerComponent = playerObj.GetComponent<FPSPlayer>();
		walkerComponent = playerObj.GetComponent<FPSRigidBodyWalker>();
		GunSwayComponent = weaponObj.GetComponent<GunSway>();
		AnimationComponent = objectWithAnims.GetComponent<Animation>();
		AnimationComponent.wrapMode = WrapMode.Loop;
		myTransform = transform;
	}

	void LateUpdate (){
		//align weapon parent origin with player camera origin
		Vector3 tempBodyPosition = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y - modelUpAmt,Camera.main.transform.position.z)
			+ (playerTransform.forward * modelForwardAmt)
			- (playerTransform.right * modelRightAmt)
			+ (playerTransform.right * modelRightMod);
		myTransform.position = tempBodyPosition;

		Vector3 tempBodyAngles = new Vector3(modelPitch, playerTransform.eulerAngles.y + rotAngleAmt, modelRightAmt);
		myTransform.eulerAngles = tempBodyAngles;

		//adjust model position for leaning left or right
		if(Mathf.Abs(walkerComponent.leanAmt) > 0.1f){
			modelRightAmt = Mathf.Lerp(modelRightAmt, walkerComponent.leanAmt, Time.deltaTime * 9.0f);
		}else{
			modelRightAmt = Mathf.Lerp(modelRightAmt, 0.0f, Time.deltaTime * 9.0f);
		}

		//adjust model scale for better visual tweaking for standing and crouching
		if(walkerComponent.crouched){
			modelScaleAmt = Mathf.MoveTowards(modelScaleAmt, modelCrouchScale, 0.06f);
		}else{
			modelScaleAmt = Mathf.MoveTowards(modelScaleAmt, modelStandScale, 0.06f);
		}

		myTransform.localScale = new Vector3(modelScaleAmt, modelScaleAmt, modelScaleAmt);

		if(walkerComponent.prone 
		|| walkerComponent.holdingBreath 
		|| walkerComponent.swimming 
		|| FPSPlayerComponent.hitPoints <= 0.0f){
			//move model back and out of sight if the player's legs shouldn't be visible in this state
			modelForwardAmt = Mathf.Lerp(modelForwardAmt, -1.75f, Time.deltaTime * 7.0f);

		}else if(!walkerComponent.grounded){//play jumping animation and rotate to jumpAngle amount
			rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, jumpAngle, 220.0f * Time.deltaTime);
			modelRightMod = Mathf.Lerp(modelRightMod, 0.0f, Time.deltaTime * 7.0f);
			modelForwardAmt = Mathf.Lerp(modelForwardAmt, modelForward, Time.deltaTime * 7.0f);
			modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUp, 0.06f);
			AnimationComponent["jump"].speed = -0.35f;
			AnimationComponent.CrossFade("jump", 0.3f);
		}else{
			//position model forward
			if(walkerComponent.proneRisen || walkerComponent.crouchRisen){
				if(!walkerComponent.crouched){
					modelForwardAmt = Mathf.Lerp(modelForwardAmt, modelForward, Time.deltaTime * 7.0f);
				}else{
					modelForwardAmt = Mathf.Lerp(modelForwardAmt, modelForwardCrouch, Time.deltaTime * 7.0f);
				}
			}
			//calculate movement speed to scale animation speed when using a joystick
			speedMax = Mathf.Max(Mathf.Abs(walkerComponent.inputY), Mathf.Abs(walkerComponent.inputX));
		
			if(Mathf.Abs(walkerComponent.inputY) > 0.1f){
				modelRightMod = Mathf.Lerp(modelRightMod, 0.0f, Time.deltaTime * 7.0f);
				if(walkerComponent.crouched){//play walking crouch animation
					modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUpCrouch, 0.06f);
					if(walkerComponent.inputY > 0.1f){
						AnimationComponent["crouchwalk"].speed = crouchWalkAnimSpeed;
					}else if(walkerComponent.inputY < 0.1f){
						AnimationComponent["crouchwalk"].speed = -crouchWalkAnimSpeed;
					}
					AnimationComponent.CrossFade("crouchwalk", 0.3f);
				}else{//play walking animation
					modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUp, 0.06f);
					AnimationComponent.CrossFade("walk", 0.25f);
					if(walkerComponent.inputY > 0.1f){
						if(!walkerComponent.sprintActive){
							AnimationComponent["walk"].speed = walkAnimSpeed * speedMax;
						}else{
							AnimationComponent["walk"].speed = sprintAnimSpeed * speedMax;
						}
					}else if(walkerComponent.inputY < -0.1f){
						if(!walkerComponent.sprintActive){
							AnimationComponent["walk"].speed = -walkAnimSpeed * speedMax;
						}else{
							AnimationComponent["walk"].speed = -sprintAnimSpeed * speedMax;
						}
					}
				}
				if(InputComponent.moveX > 0.1f){//play strafing animations and rotate to strafing angle
					if(walkerComponent.inputY > 0.1f){
						rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, strafeAngle, 200.0f * Time.deltaTime);
					}else{
						rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, -strafeAngle, 200.0f * Time.deltaTime);
					}
				}else if(InputComponent.moveX < -0.1f){
					if(walkerComponent.inputY > 0.1f){
						rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, -strafeAngle, 200.0f * Time.deltaTime);
					}else{
						rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, strafeAngle, 200.0f * Time.deltaTime);
					}
				}else{
					rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, 0.0f, 200.0f * Time.deltaTime);
				}
			}else{
				if(InputComponent.moveX > 0.1f){
					modelRightMod = Mathf.Lerp(modelRightMod, 0.0f, Time.deltaTime * 7.0f);
					rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, 0.0f, 220.0f * Time.deltaTime);
					if(walkerComponent.crouched){
						AnimationComponent.CrossFade("crouchstraferight", 0.3f);
						AnimationComponent["crouchstraferight"].speed = crouchWalkAnimSpeed * speedMax;
						modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUpCrouch, 0.06f);
					}else{
						AnimationComponent.CrossFade("straferight", 0.3f);
						AnimationComponent["straferight"].speed = 1.27f * speedMax;
						modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUp, 0.06f);
					}
				}else if(InputComponent.moveX < -0.1f){
					modelRightMod = Mathf.Lerp(modelRightMod, 0.0f, Time.deltaTime * 7.0f);
					rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, 0.0f, 220.0f * Time.deltaTime);
					if(walkerComponent.crouched){
						AnimationComponent.CrossFade("crouchstrafeleft", 0.3f);
						AnimationComponent["crouchstrafeleft"].speed = crouchWalkAnimSpeed * speedMax;
						modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUpCrouch, 0.06f);
					}else{
						AnimationComponent.CrossFade("strafeleft", 0.3f);
						AnimationComponent["strafeleft"].speed = 1.27f * speedMax;
						modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUp, 0.06f);
					}
				}else{
					if(GunSwayComponent.localSide > 0.0075f){
						modelRightMod = Mathf.Lerp(modelRightMod, idleRight, Time.deltaTime * 7.0f);
						rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, 0.0f, 2.0f);
						if(walkerComponent.crouched){
							AnimationComponent["crouchstraferight"].speed = 0.7f;
							AnimationComponent.CrossFade("crouchstraferight", 0.4f);
							modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUpCrouch, 0.06f);
						}else{
							AnimationComponent["straferight"].speed = 1.0f;
							AnimationComponent.CrossFade("straferight", 0.4f);
							modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUp, 0.06f);
						}
					}else if(GunSwayComponent.localSide < -0.0075f){
						modelRightMod = Mathf.Lerp(modelRightMod, idleRight, Time.deltaTime * 7.0f);
						rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, 0.0f, 1.0f);
						if(walkerComponent.crouched){
							AnimationComponent["crouchstrafeleft"].speed = 0.7f;
							AnimationComponent.CrossFade("crouchstrafeleft", 0.4f);
							modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUpCrouch, 0.06f);
						}else{
							AnimationComponent["strafeleft"].speed = 1.0f;
							AnimationComponent.CrossFade("strafeleft", 0.4f);
							modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUp, 0.06f);
						}
					}else{//play idle animation and rotate to idle angles
						if(walkerComponent.crouched){
							modelRightMod = Mathf.Lerp(modelRightMod, crouchRight, Time.deltaTime * 7.0f);
							rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, crouchAngle, 2.0f);
							modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUpCrouch, 0.06f);
							AnimationComponent.CrossFade("crouchidle", 0.4f);
						}else{
							modelRightMod = Mathf.Lerp(modelRightMod, idleRight, Time.deltaTime * 7.0f);
							rotAngleAmt = Mathf.MoveTowards(rotAngleAmt, idleAngle, 2.0f);
							modelUpAmt = Mathf.MoveTowards(modelUpAmt, modelUp, 0.06f);
							AnimationComponent.CrossFade("idle2", 0.4f);
						}
					}
				}
			}
		}
	}
}
