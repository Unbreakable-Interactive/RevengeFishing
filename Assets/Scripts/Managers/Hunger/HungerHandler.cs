using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RevengeFishing.Hunger
{
    /// <summary>
    /// Used to monitor, and control the <seealso cref="Entity"/> hunger, and their fatigue over time.
    /// 
    /// As the player is pulled further in by the fisherman, The player will start to struggle, and build up their fatigue,
    /// which will then challenge the player. This should hold the Player's values of Fatigue.
    /// </summary>
    [System.Serializable]
    public class HungerHandler
    {
        /// <summary>
        /// Controls the player's Hunger.
        /// 
        /// Hunger increases by 1 each second; player starves if hunger reaches 40
        /// </summary>
        [SerializeField] protected int _hunger;
        [SerializeField] protected int _maxHunger;
        [SerializeField] protected int _maxHungerBonus = 0;

        protected EntityFatigue _entityFatigue;

        public HungerHandler(int maxHunger, EntityFatigue entityFatigue, int initialHunger = 0)
        {
            _maxHunger = maxHunger + _maxHungerBonus;
            _hunger = initialHunger;
            _entityFatigue = entityFatigue;
        }

        /// <summary>
        /// How much Hunger has the player accumulated?
        /// </summary>
        public int GetHunger() => _hunger;

        /// <summary>
        /// How much Hunger can the player handle?
        /// </summary>
        public int GetMaxHunger() => _maxHunger;

        /// <summary>
        /// Used Whenever an NPC dies to the player, and the player 'heals' from hunger.
        /// Also helps restore the players fatigue.
        /// </summary>
        /// <param name="enemyPowerLevel">The enemies given power level.</param>
        /// <param name="newPowerLevel">This is the 'new' power level after it has been adjusted.</param>
        public void GainedPowerFromEating(int enemyPowerLevel, int newPowerLevel)
        {
            int prevFatigue = _entityFatigue.maxFatigue;
            int prevHunger = GetMaxHunger();
            _hunger -= Mathf.RoundToInt((float)enemyPowerLevel * 0.5f);

            if (_hunger < 0)
            {
                _entityFatigue.fatigue += _hunger; //heals as much fatigue as hunger overflowed
                _hunger = 0;
            }

            // Ensure fatigue does not drop below 0
            if (_entityFatigue.fatigue < 0) _entityFatigue.fatigue = 0;

            // Update new max values to match new power level
            _entityFatigue.maxFatigue = newPowerLevel;
            _maxHunger = newPowerLevel;

            // keep values proportional to new power level
            _hunger = Mathf.RoundToInt((float)_hunger / (float)prevHunger * (float)_maxHunger);
            _entityFatigue.fatigue = Mathf.RoundToInt((float)_entityFatigue.fatigue / (float)prevFatigue * (float)_entityFatigue.maxFatigue);

            //Debug.Log($"Player gained {Mathf.RoundToInt((float)enemyPowerLevel * 0.2f)} power from eating enemy! New power level: {_powerLevel}");
        }


        public float GetHungerPercentage() => _maxHunger > 0 ? (float)_hunger / (float)_maxHunger : 0f;

        /// <summary>
        /// What is the players current hunger?
        /// </summary>
        public void SetHunger(int value)
        {
            _hunger = Mathf.Clamp(value, 0, _maxHunger);
        }


        public void ModifyHunger(int amount)
        {
            SetHunger(_hunger + amount);
        }

        public void SetFatigue(int value)
        {
            _entityFatigue.fatigue = Mathf.Clamp(value, 0, _entityFatigue.maxFatigue);
        }

        public void ModifyFatigue(int amount)
        {
            SetFatigue(_entityFatigue.fatigue + amount);
        }
    }
}