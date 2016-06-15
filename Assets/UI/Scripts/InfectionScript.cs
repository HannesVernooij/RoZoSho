using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InfectionScript : MonoBehaviour
{
    [SerializeField]
    private Text m_TextInfection;
    [SerializeField]
    private Image m_ImageInfection;

    private UIScript m_UIScript;
    private ThirstText m_ThirstText;

    private float OldInfection = -2;
    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        m_UIScript = GetComponentInParent<UIScript>();
        m_ThirstText = m_UIScript._ThirstText;
    }
    private void Update()
    {
        if (m_ThirstText != null && OldInfection != m_ThirstText.thirstGui)
        {
            m_TextInfection.text = Mathf.Clamp(m_ThirstText.thirstGui, 0, 100).ToString() + "%";
            ChangeHealthBar();
            OldInfection = m_ThirstText.thirstGui;
        }
    }
    private float imageFirstSize = -1;
    private void ChangeHealthBar()
    {
        if (imageFirstSize == -1)
            imageFirstSize = m_ImageInfection.rectTransform.sizeDelta.x;
        float perc = imageFirstSize / 100;
        perc *= m_ThirstText.thirstGui;
        m_ImageInfection.rectTransform.sizeDelta = new Vector2(perc, m_ImageInfection.rectTransform.sizeDelta.y);
    }
}
