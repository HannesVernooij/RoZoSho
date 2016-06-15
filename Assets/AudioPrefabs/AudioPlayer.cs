﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour {
    private AudioSource _audioSource;
    [SerializeField]
    private List<AudioClip> _clips;
    private int _lastPlayedID;

    private void Start()
    {
        _lastPlayedID = -1;
        _clips = new List<AudioClip>();
        _audioSource = GetComponent<AudioSource>();
        Object[] clips = Resources.LoadAll("AudioClips", typeof(AudioClip));

        Debug.Log(clips.Length + " Clips Loaded.");

        foreach(AudioClip clip in clips)
        {
            _clips.Add(clip);
        }
    }

    public void PlayClip(int id)
    {
        if (id == _lastPlayedID+1)
        {
            _audioSource.clip = _clips[id];
            _audioSource.Play();
            Debug.Log("PLAYING");
            _lastPlayedID = id;
        }
    }
}
