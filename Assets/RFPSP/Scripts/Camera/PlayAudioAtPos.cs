//PlayAudioAtPos.cs 
//Plays audio at a point with several parameters for customization of playback
using UnityEngine;
using System.Collections;

public class PlayAudioAtPos : MonoBehaviour {

	public static PlayAudioAtPos instance;

	void Awake(){
		instance = this;
	}

	public AudioSource PlayClipAt(AudioClip clip, Vector3 pos, float vol, float blend, float pitch){
		GameObject tempGO = new GameObject("TempAudio"); // create the temp object
		tempGO.transform.position = pos; // set its position
		AudioSource aSource = tempGO.AddComponent<AudioSource>(); // add an audio source
		aSource.clip = clip; // define the clip
		// set other aSource properties here, if desired
		aSource.spatialBlend = blend;
		aSource.volume = vol;
		aSource.pitch = pitch;
		aSource.Play(); // start the sound
		Destroy(tempGO, clip.length); // destroy object after clip duration
		return aSource; // return the AudioSource reference
	}
}
