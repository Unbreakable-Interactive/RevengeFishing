using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelDisplay : MonoBehaviour
{
    [SerializeField] private Player player;
    private TextMeshProUGUI levelDisplay;
    private int initPowerLevel;

    // Start is called before the first frame update
    void Start()
    {
        levelDisplay = GetComponent<TextMeshProUGUI>();
        initPowerLevel = player.PowerLevel;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLevelDisplay();
    }

    void UpdateLevelDisplay()
    {
        levelDisplay.text = (player.PowerLevel - initPowerLevel).ToString();
    }
}
