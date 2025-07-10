using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityFatigue
{
    public int _fatigue { get; set; }
    public int _maxFatigue { get; set; }

    public EntityFatigue(int maxFatigue, int initFatigue = 0)
    {
        _maxFatigue = maxFatigue;
        _fatigue = initFatigue;
    }
}
