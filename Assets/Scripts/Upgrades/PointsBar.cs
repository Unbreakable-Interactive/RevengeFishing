using UnityEngine;
using UnityEngine.UI;

public class PointsBar : MonoBehaviour
{
    [SerializeField] private Image[] pips;
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color spentColor = Color.gray;
    [SerializeField] private Color inactiveColor = new Color(1,1,1,0.15f);

    private int _max;

    public void Init(int max)
    {
        _max = Mathf.Clamp(max, 0, pips != null ? pips.Length : 0);
        for (int i = 0; i < pips.Length; i++)
        {
            pips[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < pips.Length; i++)
        {
            if (!pips[i]) continue;
            pips[i].gameObject.SetActive(i < _max);
            if (i < _max) pips[i].color = inactiveColor;
        }
    }

    public void SetState(int bank, int spentThisPanel)
    {
        bank = Mathf.Clamp(bank, 0, _max);
        spentThisPanel = Mathf.Clamp(spentThisPanel, 0, bank);

        for (int i = 0; i < _max; i++)
        {
            if (!pips[i]) continue;
            if (i < bank) pips[i].color = availableColor;
            else pips[i].color = inactiveColor;
        }

        int startSpent = Mathf.Max(0, bank - spentThisPanel);
        for (int i = startSpent; i < bank; i++)
        {
            if (!pips[i]) continue;
            pips[i].color = spentColor;
        }
    }
}