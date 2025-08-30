using UnityEngine;

namespace RevengeFishing.Hunger
{
    [System.Serializable]
    public class HungerHandler
    {
        [SerializeField] protected int _hunger;
        [SerializeField] protected int _maxHunger;
        [SerializeField] protected int _maxHungerBonus = 0;
        protected EntityFatigue _entityFatigue;

        [SerializeField] private float _eatSatiationMultiplier = 1f;
        [SerializeField] private float _hungerDecayMultiplier = 1f;

        public HungerHandler(int maxHunger, EntityFatigue entityFatigue, int initialHunger = 0)
        {
            _maxHunger = maxHunger + _maxHungerBonus;
            _hunger = initialHunger;
            _entityFatigue = entityFatigue;
        }

        public int  GetHunger() => _hunger;
        public int  GetMaxHunger() => _maxHunger;
        public float GetHungerPercentage() => _maxHunger > 0 ? (float)_hunger / (float)_maxHunger : 0f;
        public float GetEatSatiationMultiplier() => _eatSatiationMultiplier;
        public float GetHungerDecayMultiplier()  => _hungerDecayMultiplier;

        public void SetHunger(int value)  => _hunger = Mathf.Clamp(value, 0, _maxHunger);
        public void ModifyHunger(int amt) => SetHunger(_hunger + amt);

        public void SetFatigue(int value)           => _entityFatigue.fatigue = Mathf.Clamp(value, 0, _entityFatigue.maxFatigue);
        public void ModifyFatigue(int amount)       => SetFatigue(_entityFatigue.fatigue + amount);

        public void MultiplyMaxHunger(float factor)
        {
            _maxHunger = Mathf.Max(1, Mathf.RoundToInt(_maxHunger * factor));
            _hunger = Mathf.Clamp(_hunger, 0, _maxHunger);
        }

        public void AddMaxHunger(int amount)
        {
            _maxHunger = Mathf.Max(1, _maxHunger + amount);
            _hunger = Mathf.Clamp(_hunger, 0, _maxHunger);
        }

        public void MultiplyEatSatiation(float factor) => _eatSatiationMultiplier = Mathf.Max(0f, _eatSatiationMultiplier * factor);
        public void MultiplyHungerDecay(float factor)  => _hungerDecayMultiplier  = Mathf.Max(0f, _hungerDecayMultiplier  * factor);

        public void GainedPowerFromEating(int enemyPowerLevel, int newPowerLevel)
        {
            int prevFatigue = _entityFatigue.maxFatigue;
            int prevHunger  = GetHunger();

            // antes: 0.5f; ahora escalado por _eatSatiationMultiplier
            _hunger -= Mathf.RoundToInt(enemyPowerLevel * 0.5f * _eatSatiationMultiplier);

            if (_hunger < 0)
            {
                ModifyFatigue(_hunger);
                SetHunger(0);
            }
            if (_entityFatigue.fatigue < 0) SetFatigue(0);

            _entityFatigue.maxFatigue = newPowerLevel;
            _maxHunger = newPowerLevel;

            SetHunger(prevHunger > 0 ? Mathf.RoundToInt(_hunger / (float)prevHunger * _maxHunger) : _hunger);
            SetFatigue(prevFatigue > 0 ? Mathf.RoundToInt(_entityFatigue.fatigue / (float)prevFatigue * _entityFatigue.maxFatigue) : _entityFatigue.fatigue);
        }
    }
}