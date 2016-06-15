using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class WeaponBar : MonoBehaviour
{
    [SerializeField]
    private Text m_AmmoText;
    [SerializeField]
    private Image m_AmmoImage;
    [SerializeField]
    private Sprite[] m_WeaponSprites;

    private UIScript m_UIScript;
    private AmmoText _AmmoText;
    private PlayerWeapons _PlayerWeapons;

    private int m_iOldCurrentWeapon = -1, m_IOldAmmoInClip, m_IOldAmmoTotal;
    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        m_UIScript = GetComponentInParent<UIScript>();
        _AmmoText = m_UIScript._AmmoText;
        _PlayerWeapons = m_UIScript._PlayerWeapons;
    }

    void Update()
    {
        try
        {
            if (_AmmoText != null && _PlayerWeapons != null && (m_iOldCurrentWeapon != _PlayerWeapons.currentWeapon) || (m_IOldAmmoInClip != _AmmoText.ammoGui || m_IOldAmmoTotal != _AmmoText.ammoGui2))
            {
                string text = _AmmoText.ammoGui.ToString() + " / " + _AmmoText.ammoGui2.ToString();
                switch (_PlayerWeapons.currentWeapon)
                {
                    case 0:
                    case 1:
                    case 8:
                        text = "";
                        break;
                }
                m_AmmoText.text = text;

                m_AmmoImage.sprite = m_WeaponSprites[_PlayerWeapons.currentWeapon];
                m_AmmoImage.SetNativeSize();

                m_iOldCurrentWeapon = _PlayerWeapons.currentWeapon;
            }
        }
        catch
        {

        }
    }

}
