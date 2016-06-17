using UnityEngine;
using System.Collections;

public class SpawnEnemies : MonoBehaviour
{
    [SerializeField]
    private GameObject _targetObject;
    [SerializeField]
    private int _amount;
    private float _spawnDelay = 1;
    private int _enemiesSpawned;

    public void StartSpawning()
    {
        if (_enemiesSpawned < _amount)
        {
            if (_spawnDelay > 0)
            {
                _spawnDelay -= Time.deltaTime;
            }
            else
            {
                SpawnEnemy();
                _spawnDelay = 1;
                _enemiesSpawned++;
            }

            StartCoroutine(Delay());
        }
    }

    private void SpawnEnemy()
    {
        GameObject a = Instantiate(_targetObject);
        a.transform.position = transform.position;
        AI script = a.GetComponent<AI>();
        script.huntPlayer = true;
    }

    private IEnumerator Delay()
    {
        yield return new WaitForEndOfFrame();
        StartSpawning();
    }
}
