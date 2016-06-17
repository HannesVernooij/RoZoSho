using UnityEngine;
using System.Collections;

public class C4Pickup : MonoBehaviour {
    [SerializeField]
    private GameObject _coreInteractionTrigger;

    public void PickUpItem()
    {
        _coreInteractionTrigger.SetActive(true);
        Destroy(gameObject);
    }
}
