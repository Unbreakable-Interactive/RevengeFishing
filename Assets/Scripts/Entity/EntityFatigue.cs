using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class EntityFatigue
{
    [SerializeField] public int fatigue { get; set; }
    [SerializeField] public int maxFatigue { get; set; }

    public EntityFatigue(int maxFatigue, int initFatigue = 0)
    {
        this.maxFatigue = maxFatigue;
        fatigue = initFatigue;
    }
}
