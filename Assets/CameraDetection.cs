using UnityEngine;
using UnityEngine.Events;

public class CameraDetection : MonoBehaviour
{
    [SerializeField]
    private Transform _origin;
    [SerializeField]
    private UnityEvent _onDetected;

    void Update()
    {
        Ray ray = new Ray(_origin.position, _origin.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red);
            if(hit.collider.name == "FPS Player")
            {
                TriggerEvent();
                Debug.Log("SPAWN");
            }
        }
    }

    private void TriggerEvent()
    {
        _onDetected.Invoke();
    }
}
