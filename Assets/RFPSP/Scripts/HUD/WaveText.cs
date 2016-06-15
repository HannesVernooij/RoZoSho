//WaveText.cs by Azuline Studios© All Rights Reserved
using UnityEngine;
using System.Collections;

public class WaveText : MonoBehaviour {
	//draw ammo amount on screen
	public int waveGui;//bullets remaining in clip
	public int waveGui2;//total ammo in inventory
	private int oldWave = -512;
	private int oldWave2 = -512;
	public Color textColor;
	public float horizontalOffset = 0.95f;
	public float verticalOffset = 0.075f;
	[HideInInspector]
	public float horizontalOffsetAmt = 0.78f;
	[HideInInspector]
	public float verticalOffsetAmt = 0.1f;
	public float fontScale = 0.032f;
	
	void OnEnable(){
		horizontalOffsetAmt = horizontalOffset;
		verticalOffsetAmt = verticalOffset;	
		GetComponent<GUIText>().fontSize = Mathf.RoundToInt(Screen.height * fontScale);
		oldWave = -512;
		oldWave2 = -512;
	}
	
	void Update(){
		//only update GUIText if value to be displayed has changed
		if((waveGui != oldWave) || (waveGui2 != oldWave2)) {
			GetComponent<GUIText>().pixelOffset = new Vector2 (Screen.width * horizontalOffsetAmt, Screen.height * verticalOffsetAmt);
			
			GetComponent<GUIText>().text = "Wave "+ waveGui.ToString()+" - Remaining : "+ waveGui2.ToString();
			
			GetComponent<GUIText>().material.color = textColor;
			oldWave = waveGui;
			oldWave2 = waveGui2;
	    }
	
	}
}