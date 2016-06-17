using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class TriggerEvent : MonoBehaviour
{
    [SerializeField]
    private UnityEvent _triggerEnterEvent, _triggerExitEvent;

    void OnTriggerEnter()
    {
        _triggerEnterEvent.Invoke();
        Debug.Log("ENTER");
    }

    void OnTriggerExit()
    {
        _triggerExitEvent.Invoke();
        Debug.Log("EXIT");
    }
}
