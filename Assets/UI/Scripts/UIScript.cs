using UnityEngine;
using System.Collections;

public class UIScript : MonoBehaviour
{
    public HealthText _HealthText;
    public ThirstText _ThirstText;
    public AmmoText _AmmoText;
    public PlayerWeapons _PlayerWeapons;

    void Start()
    {
        _HealthText = FindObjectOfType<HealthText>();
        _ThirstText = FindObjectOfType<ThirstText>();
        _AmmoText = FindObjectOfType<AmmoText>();
        _PlayerWeapons = FindObjectOfType<PlayerWeapons>();
        DesableBackgroundTexts();
    }
    private void DesableBackgroundTexts()
    {
        _HealthText.GetComponentInChildren<GUIText>().enabled = false;
        _ThirstText.GetComponentInChildren<GUIText>().enabled = false;
    }
}
