using UnityEngine;
using System.Collections;

public class CoreDestructionButton : MonoBehaviour {
    [SerializeField]
    private GameObject[] _c4;
    [SerializeField]
    private AudioSource _explosion;
    [SerializeField]
    private GameObject[] _explosionps;
    [SerializeField]
    private AudioPlayer _audioManager;
    [Header("ALARM PHASE")]
    [SerializeField]
    private Light[] _lights;
    [SerializeField]
    private GameObject _lockdownDoor;
    [SerializeField]
    private Material _lockDownMat;
    [SerializeField]
    private GameObject _doorTriggerToDisable, doorTriggerToEnable;

    public void PickUpItem()
    {
        foreach (GameObject item in _c4)
        {
            item.SetActive(true);
        }
        StartCoroutine(WaitForExpl());
        StartCoroutine(WaitForVoice());
        _lockdownDoor.GetComponent<Animator>().SetBool("open", false);
        _doorTriggerToDisable.SetActive(false);
    }

    private IEnumerator WaitForExpl()
    {
        yield return new WaitForSeconds(3);
        foreach (GameObject ps in _explosionps)
        {
            ps.SetActive(true);
        }
    }

    private IEnumerator WaitForVoice()
    {
        yield return new WaitForSeconds(5);
        _audioManager.PlayClip(3);
        DisableDoor();
        
    }

    private void DisableDoor()
    {
        _lockdownDoor.GetComponent<Renderer>().material = _lockDownMat;
        foreach (Light l in _lights)
        {
            l.color = Color.red;
        }
    }
}
