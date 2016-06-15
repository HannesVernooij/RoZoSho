//WarmupText.cs by Azuline Studios© All Rights Reserved
using UnityEngine;
using System.Collections;

public class WarmupText : MonoBehaviour {
	//draw ammo amount on screen
	public float warmupGui;//bullets remaining in clip
	private float oldWarmup = -512;
	public Color textColor;
	public float horizontalOffset = 0.65f;
	public float verticalOffset = 0.96f;
	[HideInInspector]
	public float horizontalOffsetAmt = 0.78f;
	[HideInInspector]
	public float verticalOffsetAmt = 0.1f;
	public float fontScale = 0.032f;
	[HideInInspector]
	public bool waveBegins;
	[HideInInspector]
	public bool waveComplete;
	
	void OnEnable(){
		horizontalOffsetAmt = horizontalOffset;
		verticalOffsetAmt = verticalOffset;	
		GetComponent<GUIText>().fontSize = Mathf.RoundToInt(Screen.height * fontScale);
		oldWarmup = -512;
	}
	
	void Update(){
		//only update GUIText if value to be displayed has changed
		if(warmupGui != oldWarmup) {
			GetComponent<GUIText>().pixelOffset = new Vector2 (Screen.width * horizontalOffsetAmt, Screen.height * verticalOffsetAmt);

			if(!waveComplete){
				if(!waveBegins){
					GetComponent<GUIText>().fontSize = Mathf.RoundToInt(Screen.height * fontScale);
					GetComponent<GUIText>().text = "Warmup Time : "+  Mathf.Round(warmupGui).ToString();
				}else{
					GetComponent<GUIText>().fontSize = Mathf.RoundToInt(Screen.height * (fontScale * 1.5f));
					GetComponent<GUIText>().text = "INCOMING WAVE";
				}
			}else{
				GetComponent<GUIText>().fontSize = Mathf.RoundToInt(Screen.height * (fontScale * 1.5f));
				GetComponent<GUIText>().text = "WAVE COMPLETE";
			}
			
			GetComponent<GUIText>().material.color = textColor;
			oldWarmup = warmupGui;
	    }
	
	}
}