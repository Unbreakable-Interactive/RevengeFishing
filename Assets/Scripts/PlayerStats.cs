using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private float powerLevel = 100; //default starting power level is 100
    private float starterLevel;
    /*[SerializeField]*/ private float hunger; //player dies if reaches 100%
    /*[SerializeField]*/ private float fatigue; //player dies if reaches 100%
    private enum Phase { infant, juvenile, adult, beast, monster }
    [SerializeField] private Phase phase = Phase.infant;

    // Start is called before the first frame update
    void Start()
    {
        starterLevel = powerLevel;
        SetPhase(powerLevel);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetPhase(float powLevel)
    {
        if (powLevel >= 10000000000) phase = Phase.monster; //modify these to be dynamic instead of hardcoded
        else if (powLevel >= 100000000) phase = Phase.beast;
        else if (powLevel >= 1000000) phase = Phase.adult;
        else if (powLevel >= 10000) phase = Phase.juvenile;
        else phase = Phase.infant;
    }
}
