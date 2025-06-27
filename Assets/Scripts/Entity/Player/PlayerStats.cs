using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum Phase { infant, juvenile, adult, beast, monster }

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private long powerLevel = 100; //default starting power level is 100
    private long starterLevel;
    /*[SerializeField]*/ private long hunger; //player dies if reaches 100%
    /*[SerializeField]*/ private long fatigue; //player dies if reaches 100%
    [SerializeField] private Phase phase = Phase.infant;

    // Start is called before the first frame update
    public void Initialize()
    {
        starterLevel = powerLevel;
        SetPhase();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetPhase()
    {
        if (powerLevel >= 10000000000) phase = Phase.monster; //modify these to be dynamic instead of hardcoded
        else if (powerLevel >= 100000000) phase = Phase.beast;
        else if (powerLevel >= 1000000) phase = Phase.adult;
        else if (powerLevel >= 10000) phase = Phase.juvenile;
        else phase = Phase.infant;
    }
}
