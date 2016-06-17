using UnityEngine;
using System.Collections;

public class DoorScript : MonoBehaviour
{
    [SerializeField]
    private AudioSource _source;

    public void OpenDoor()
    {
        GetComponent<Animator>().SetBool("open", true);
        _source.Play();
    }

    public void CloseDoor()
    {
        GetComponent<Animator>().SetBool("open", false);
        _source.Play();
    }
}
