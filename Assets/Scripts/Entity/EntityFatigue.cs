using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class EntityFatigue
{
    [SerializeField] public int _fatigue { get; set; }
    [SerializeField] public int _maxFatigue { get; set; }

    public EntityFatigue(int maxFatigue, int initFatigue = 0)
    {
        _maxFatigue = maxFatigue;
        _fatigue = initFatigue;
    }
}
