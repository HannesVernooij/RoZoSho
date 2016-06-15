using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthScript : MonoBehaviour
{
    [SerializeField]
    private Text m_TextHealth;
    [SerializeField]
    private Image m_ImageHealth;

    public UIScript m_UIScript;
    private HealthText m_HealthText;

    private float OldHP;
    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        m_UIScript = GetComponentInParent<UIScript>();
        m_HealthText = m_UIScript._HealthText;
    }
    private void Update()
    {
        if (m_HealthText != null && OldHP != m_HealthText.healthGui)
        {
            m_TextHealth.text = Mathf.Clamp(m_HealthText.healthGui, 0, 999).ToString();
            ChangeHealthBar();
            OldHP = m_HealthText.healthGui;
        }
    }
    private float imageFirstSize = -1;
    private void ChangeHealthBar()
    {
        if (imageFirstSize == -1)
            imageFirstSize = m_ImageHealth.rectTransform.sizeDelta.x;
        float perc = imageFirstSize / 100;
        perc *= m_HealthText.healthGui;
        perc = Mathf.Clamp(perc, 0, imageFirstSize);
        m_ImageHealth.rectTransform.sizeDelta = new Vector2(perc, m_ImageHealth.rectTransform.sizeDelta.y);
    }
}
