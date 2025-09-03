using UnityEngine;
using UnityEngine.UI;

// public class HungerDisplay : BaseDisplay
// {
//     [Header("Hunger Settings")]
//     [SerializeField] private Player player;
//     [SerializeField] private Image image;
//
//     protected override void UpdateDisplay()
//     {
//         if (player == null || player.HungerHandler == null) return;
//
//         float currentHunger = player.HungerHandler.GetHunger();
//         float maxHunger = player.HungerHandler.GetMaxHunger();
//         
//         image.fillAmount = 1 - ((float)currentHunger / (float)maxHunger);
//     }
// }