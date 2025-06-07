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
    [SerializeField] private Phase phase;

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
        switch (powerLevel)
        {
            case (100):
                phase = Phase.infant;
                break;

            case (10000):
                phase = Phase.juvenile;
                break;

            case (1000000):
                phase = Phase.adult;
                break;

            case (100000000):
                phase = Phase.beast;
                break;

            case (10000000000):
                phase = Phase.monster;
                break;

            default:
                phase = Phase.infant;
                break;
        }
    }
}
