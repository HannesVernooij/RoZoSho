using UnityEngine;
using System.Collections;

public class CameraRotation : MonoBehaviour
{
    [SerializeField]
    //[Range(0, 360)]
    private Vector3[] _sides;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float _rotationSpeed;
    [SerializeField]
    private float _targetDelay;
    private float _currentDelay;
    private int _side;
    private float _lerpValue;



    void Start()
    {
        SetTargetRotation();
    }

    void Update()
    {
        if (_currentDelay > 0)
        {
            _currentDelay -= Time.deltaTime;
            if (_currentDelay <= 0)
            {
                SetTargetRotation();
            }
        }
    }

    private void SetTargetRotation()
    {
        _side++;
        _side = _side % _sides.Length;
        Rotate(_sides[_side]);
    }

    private void Rotate(Vector3 targetRotation)
    {
        Quaternion from = transform.rotation;
        Quaternion to = Quaternion.Euler(targetRotation);
        _lerpValue += Time.deltaTime * _rotationSpeed;
        if (_lerpValue > 0.7) _lerpValue = 1;
        transform.rotation = Quaternion.Lerp(from, to, _lerpValue);
        if (_lerpValue < 1) StartCoroutine(PauseRotate(targetRotation));
        else
        {
            _lerpValue = 0;
            _currentDelay = _targetDelay;
        }
    }

    private IEnumerator PauseRotate(Vector3 targetYRotation)
    {
        yield return new WaitForEndOfFrame();
        Rotate(targetYRotation);
    }
}

   
