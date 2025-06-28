# Unity Project Naming Conventions & Best Practices

> Consistency is key. These conventions help ensure clean, readable, and maintainable Unity code.

---

## 🧱 Project Structure

- **Folders**: `PascalCase` or `kebab-case` (project-wide consistency is more important)
  - Examples:
    - `Scripts/Player/`
    - `Prefabs/Enemies/`
    - `Art/UI/`
- **Scenes**: `PascalCase` or `snake_case` based on style
  - `MainMenu.unity`, `Level_01.unity`

---

## 🔠 Class Names

- Use `PascalCase`.
- Singular nouns for single entities (`Player`, `EnemyAI`), or compound nouns for managers/systems (`AudioManager`, `GameStateHandler`).

```csharp
public class PlayerMovement : MonoBehaviour
{
}
```

---

## 🔤 Variable Names

### 🟡 Private Fields
- Use **`camelCase`** + **optional `_` prefix** (choose one convention for the whole team).
  - `private int currentHealth;`
  - or `private int _currentHealth;`

### 🔵 Public Fields / Properties
- Use **`PascalCase`**.
- Prefer **properties** for public access.

```csharp
public int CurrentHealth { get; private set; }
```

### 🟣 Constants
- Use `UPPER_SNAKE_CASE`.

```csharp
private const float MAX_SPEED = 10f;
```

---

## ⚙️ Method Names

- Use **`PascalCase`**.
- Use action words (verbs) that describe behavior clearly.

```csharp
void MovePlayer() {}
bool IsGrounded() {}
void PlayDeathAnimation() {}
```

---

## ⏳ Coroutines

- Prefix with `IEnumerator` return type.
- Use `PascalCase`, ideally with `Routine` or `Coroutine` suffix for clarity.

```csharp
private IEnumerator FadeOutRoutine() {}
```

---

## 🧩 Interfaces

- Prefix with `I`, use `PascalCase`.

```csharp
public interface IDamageable
{
    void TakeDamage(int amount);
}
```

---

## 🧨 Events and Delegates

- Use `PascalCase`.
- Use past tense for events (`OnDeath`, `OnScoreChanged`).
- Prefer `Action` or custom delegates.

```csharp
public event Action OnGameOver;
public event Action<int> OnScoreChanged;
```

---

## 🧰 Enums

- Use `PascalCase` for the enum name and enum values.

```csharp
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}
```

---

## 🎯 ScriptableObjects

- Name ends with `Data`, `Config`, or `Asset` to clarify purpose.

```csharp
public class EnemyStatsData : ScriptableObject
{
    public float Health;
}
```

---

## 🧪 Unit Tests

- Follow the format: `MethodName_Condition_ExpectedResult`
- Use `PascalCase`

```csharp
[Test]
public void TakeDamage_WhenCalled_HealthIsReduced()
{
}
```

---

## 🧼 Miscellaneous Tips

- Avoid abbreviations unless universally known (`UI`, `HP`, `XP`).
- Keep names descriptive but concise.
- Use regions for organizing large scripts:

```csharp
#region Movement
void MovePlayer() {}
#endregion
```

- Group serialized fields at the top of the script for clarity.
- Group Unity lifecycle methods (`Awake`, `Start`, `Update`, etc.) in order.

---

## 🧭 Sample Structure

```csharp
public class EnemyAI : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int _maxHealth = 100;

    private int _currentHealth;

    public event Action OnDeath;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    private void Update()
    {
        Patrol();
    }

    private void Patrol()
    {
        // Patrol logic
    }

    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
```

---

## ✅ Summary Table

| Type             | Convention      | Example                    |
|------------------|------------------|-----------------------------|
| Class            | PascalCase       | `PlayerManager`            |
| Method           | PascalCase       | `StartGame()`              |
| Public Variable  | PascalCase       | `Speed`                    |
| Private Variable | camelCase/_camelCase | `_playerSpeed`        |
| Constant         | UPPER_SNAKE_CASE | `MAX_SPEED`                |
| Coroutine        | PascalCase       | `FadeOutRoutine()`         |
| Interface        | I + PascalCase   | `IDamageable`              |
| Event            | PascalCase       | `OnGameOver`               |
| Enum             | PascalCase       | `GameState.Paused`         |

---

Keep your code readable, expressive, and clean. This is especially important in larger teams or reusable system development.
