using UnityEngine;
using System.Collections;

public class DoorScript : MonoBehaviour
{
    public void OpenDoor()
    {
        GetComponent<Animator>().SetBool("IsOpen", true);
    }

    public void CloseDoor()
    {
        GetComponent<Animator>().SetBool("IsOpen", false);
    }
}
