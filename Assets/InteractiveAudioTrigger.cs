using UnityEngine;
using System.Collections;

public class InteractiveAudioTrigger : MonoBehaviour
{
    [SerializeField]
    private AudioPlayer _audioManager;

    public void PickUpItem()
    {
        _audioManager.PlayClip(2);
    }
}
