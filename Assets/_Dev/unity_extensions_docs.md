# Unity Extensions Documentation

A comprehensive collection of Unity C# extension methods for faster development.

## Table of Contents
1. [Setup](#setup)
2. [Transform Extensions](#transform-extensions)
3. [GameObject Extensions](#gameobject-extensions)
4. [Component Extensions](#component-extensions)
5. [Vector Extensions](#vector-extensions)
6. [Collection Extensions](#collection-extensions)
7. [UI Extensions](#ui-extensions)
8. [Color Extensions](#color-extensions)
9. [Rigidbody Extensions](#rigidbody-extensions)
10. [Animation Extensions](#animation-extensions)
11. [Delay Extensions](#delay-extensions)
12. [Easing Extensions](#easing-extensions)
13. [LayerMask Extensions](#layermask-extensions)
14. [Utility Extensions](#utility-extensions)

## Setup

1. Add the script to your Unity project
2. Add `using UnityExtensions;` to any script where you want to use the extensions
3. Extensions will be available on relevant Unity objects

---

## Transform Extensions

### Reset()
Resets transform to default values (position: 0,0,0, rotation: identity, scale: 1,1,1)

```csharp
transform.Reset();
```

### SetPosition(x, y, z)
Sets position with optional axis masking. Pass `null` to leave axis unchanged.

```csharp
transform.SetPosition(x: 5f);           // Only change X
transform.SetPosition(y: 10f, z: 2f);  // Change Y and Z, leave X
transform.SetPosition(1f, 2f, 3f);     // Change all axes
```

### SetLocalPosition(x, y, z)
Same as SetPosition but for local position.

```csharp
transform.SetLocalPosition(x: 0f);
transform.SetLocalPosition(y: 5f, z: -2f);
```

### SetScale(x, y, z) / SetScale(uniformScale)
Sets scale with optional axis masking or uniform scaling.

```csharp
transform.SetScale(x: 2f);              // Scale X only
transform.SetScale(2f);                 // Uniform scale
transform.SetScale(2f, 1f, 0.5f);      // Scale all axes
```

### DestroyChildren()
Destroys all child GameObjects (works in editor and runtime).

```csharp
transform.DestroyChildren();
```

### GetChildren()
Returns array of all child transforms.

```csharp
Transform[] children = transform.GetChildren();
foreach (Transform child in children)
{
    Debug.Log(child.name);
}
```

### FindDeep(name)
Recursively searches for child by name (searches grandchildren too).

```csharp
Transform deepChild = transform.FindDeep("DeepNestedObject");
```

**Method Chaining Example:**
```csharp
transform.Reset()
         .SetPosition(x: 5f)
         .SetScale(2f);
```

---

## GameObject Extensions

### GetOrAddComponent<T>()
Gets component if exists, adds it if it doesn't.

```csharp
Rigidbody rb = gameObject.GetOrAddComponent<Rigidbody>();
AudioSource audio = gameObject.GetOrAddComponent<AudioSource>();
```

### HasComponent<T>()
Checks if GameObject has a specific component.

```csharp
if (gameObject.HasComponent<Collider>())
{
    Debug.Log("Has collider");
}
```

### SetLayerRecursively(layer)
Sets layer for GameObject and all children recursively.

```csharp
gameObject.SetLayerRecursively(LayerMask.NameToLayer("UI"));
```

### SetActive(active)
Sets active state and returns GameObject for chaining.

```csharp
gameObject.SetActive(false).GetComponent<Renderer>().enabled = false;
```

### ToggleActive()
Toggles active state and returns GameObject.

```csharp
button.onClick.AddListener(() => menuObject.ToggleActive());
```

---

## Component Extensions

### GetComponentInFamily<T>()
Searches for component in self, parent, or children (in that order).

```csharp
AudioSource audio = transform.GetComponentInFamily<AudioSource>();
```

### DestroySafe()
Safely destroys component (handles null checks and editor/runtime differences).

```csharp
GetComponent<OldScript>().DestroySafe();
```

---

## Vector Extensions

### With(x, y, z) / With(x, y)
Creates new vector with specified components changed.

```csharp
Vector3 newPos = transform.position.With(y: 10f);
Vector2 newVel = rigidbody2D.velocity.With(x: 0f);
```

### ToVector2() / ToVector3(z)
Converts between Vector2 and Vector3.

```csharp
Vector2 screenPos = worldPos.ToVector2();
Vector3 worldPos = screenPos.ToVector3(z: 5f);
```

### RandomPoint()
Gets random point within bounds.

```csharp
Bounds spawnArea = new Bounds(Vector3.zero, Vector3.one * 10f);
Vector3 randomSpawn = spawnArea.RandomPoint();
```

**Usage Examples:**
```csharp
// Move only on Y axis
transform.position = transform.position.With(y: playerHeight);

// Zero out velocity but keep Y
rb.velocity = rb.velocity.With(x: 0f, z: 0f);
```

---

## Collection Extensions

### Random<T>()
Gets random element from array or list.

```csharp
string[] greetings = {"Hello", "Hi", "Hey"};
string randomGreeting = greetings.Random();

List<Enemy> enemies = GetEnemies();
Enemy randomEnemy = enemies.Random();
```

### Shuffle<T>()
Shuffles list in place using Fisher-Yates algorithm.

```csharp
List<Card> deck = GetDeck();
deck.Shuffle();
```

### IsNullOrEmpty<T>()
Checks if list is null or empty.

```csharp
if (inventory.IsNullOrEmpty())
{
    Debug.Log("Inventory is empty");
}
```

### AddRange(params)
Adds multiple items to list using params.

```csharp
itemList.AddRange(sword, shield, potion);
```

**Usage Examples:**
```csharp
// Random spawn
GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
Vector3 spawnPos = spawnPoints.Random().transform.position;

// Shuffle playlist
List<AudioClip> playlist = GetMusicTracks();
playlist.Shuffle();
```

---

## UI Extensions

### CanvasGroup Extensions

#### SetAlpha(alpha)
Sets alpha and returns CanvasGroup for chaining.

```csharp
canvasGroup.SetAlpha(0.5f);
```

#### SetInteractable(interactable)
Sets interactable and blocksRaycasts states.

```csharp
canvasGroup.SetInteractable(false); // Disables interaction
canvasGroup.SetInteractable(true);  // Enables interaction
```

### Image Extensions

#### SetColor(color) / SetAlpha(alpha)
Sets color or alpha of Image.

```csharp
image.SetColor(Color.red);
image.SetAlpha(0.5f);
```

### Text Extensions

#### SetText(content)
Sets text content for Text and TextMeshPro components.

```csharp
scoreText.SetText($"Score: {currentScore}");
titleText.SetText("Game Over");
```

### Button Extensions

#### SetInteractable(interactable)
Sets button interactable state.

```csharp
submitButton.SetInteractable(formValid);
```

**Chaining Example:**
```csharp
// Fade menu
menuCanvasGroup.SetAlpha(0f)
              .SetInteractable(false);

// Setup button
button.SetInteractable(true)
      .GetComponent<Image>()
      .SetColor(Color.green);
```

---

## Color Extensions

### WithAlpha(alpha)
Creates new color with different alpha.

```csharp
Color fadedRed = Color.red.WithAlpha(0.5f);
renderer.material.color = originalColor.WithAlpha(transparency);
```

### FromHex(hex)
Creates color from hex string.

```csharp
Color purple = ColorExtensions.FromHex("#9B59B6");
Color white = ColorExtensions.FromHex("#FFFFFF");
Color invalid = ColorExtensions.FromHex("invalid"); // Returns white
```

### ToHex()
Converts color to hex string.

```csharp
string hexColor = Color.red.ToHex(); // Returns "FF0000"
```

**Usage Examples:**
```csharp
// Health bar color based on health percentage
float healthPercent = currentHealth / maxHealth;
Color healthColor = Color.red.WithAlpha(healthPercent);

// Load colors from config
Color themeColor = ColorExtensions.FromHex(gameSettings.themeColorHex);
```

---

## Rigidbody Extensions

### SetVelocity(x, y, z)
Sets velocity with optional axis masking.

```csharp
rb.SetVelocity(x: 5f);              // Set X velocity only
rb.SetVelocity(y: 10f, z: 0f);     // Set Y and Z, leave X
rb.SetVelocity(0f, 0f, 0f);        // Stop all movement
```

### AddForceInDirection(direction, force, mode)
Adds force in normalized direction.

```csharp
rb.AddForceInDirection(Vector3.up, 500f);
rb.AddForceInDirection(transform.forward, jumpForce, ForceMode.Impulse);
```

### Stop()
Stops all movement (velocity and angular velocity).

```csharp
rb.Stop(); // Immediately stops the rigidbody
```

**Usage Examples:**
```csharp
// Jump only if not already moving up
if (rb.velocity.y <= 0f)
    rb.SetVelocity(y: jumpForce);

// Launch projectile
projectileRb.AddForceInDirection(targetDirection, launchForce, ForceMode.Impulse);
```

---

## Animation Extensions

### PlayAndGetLength(stateName, layer)
Plays animation and returns its length.

```csharp
float attackDuration = animator.PlayAndGetLength("AttackAnimation");
StartCoroutine(WaitForAttack(attackDuration));
```

### IsPlaying(stateName, layer)
Checks if specific animation is currently playing.

```csharp
if (!animator.IsPlaying("IdleAnimation"))
{
    animator.Play("IdleAnimation");
}
```

**Usage Examples:**
```csharp
// Wait for animation to finish
float animTime = animator.PlayAndGetLength("DeathAnimation");
yield return new WaitForSeconds(animTime);
DestroyCharacter();

// Prevent animation overlap
if (!animator.IsPlaying("AttackAnimation"))
{
    animator.Play("AttackAnimation");
}
```

---

## Delay Extensions

### Delay(seconds, action)
Executes action after specified delay.

```csharp
this.Delay(2f, () => Debug.Log("2 seconds later"));
this.Delay(1.5f, () => SpawnEnemy());
```

### DelayIf(seconds, condition, action)
Executes action after delay only if condition is true.

```csharp
this.DelayIf(3f, () => player.isAlive, () => ShowVictoryScreen());
```

### DelayFrame(action, frames)
Executes action after specified number of frames.

```csharp
this.DelayFrame(() => UpdateUI(), 1);    // Next frame
this.DelayFrame(() => TakeScreenshot(), 3); // After 3 frames
```

### DelayEndOfFrame(action)
Executes action at end of current frame.

```csharp
this.DelayEndOfFrame(() => CaptureCamera());
```

### DelayUntil(condition, action)
Waits until condition becomes true, then executes action.

```csharp
this.DelayUntil(() => player.health <= 0, () => GameOver());
this.DelayUntil(() => loadingComplete, () => StartGame());
```

### DelayWhile(condition, whileAction, finalAction)
Executes whileAction every frame while condition is true, then executes finalAction.

```csharp
this.DelayWhile(
    () => isLoading,
    () => UpdateLoadingBar(),
    () => HideLoadingScreen()
);
```

### DelayRepeat(interval, action, times)
Repeats action at intervals. Use -1 for infinite repetition.

```csharp
this.DelayRepeat(0.5f, () => Blink(), 5);     // Blink 5 times
this.DelayRepeat(1f, () => SpawnEnemy(), -1); // Spawn forever
```

### DelayRandom(minSeconds, maxSeconds, action)
Executes action after random delay between min and max.

```csharp
this.DelayRandom(1f, 3f, () => PlayRandomSound());
```

**Usage Examples:**
```csharp
// Death sequence
void Die()
{
    // Flash red
    renderer.color = Color.red;
    this.Delay(0.2f, () => renderer.color = Color.white);
    
    // Destroy after 1 second
    this.Delay(1f, () => Destroy(gameObject));
}

// Loading sequence
void StartLoading()
{
    this.DelayWhile(
        () => !dataLoaded,
        () => rotatingIcon.Rotate(0, 0, 90 * Time.deltaTime),
        () => ShowMainMenu()
    );
}
```

---

## Easing Extensions

### Available Easing Types
```csharp
EaseType.Linear           // Constant speed
EaseType.EaseInQuad       // Slow start, fast end
EaseType.EaseOutQuad      // Fast start, slow end
EaseType.EaseInOutQuad    // Slow start and end
EaseType.EaseInCubic      // More pronounced quad
EaseType.EaseOutCubic
EaseType.EaseInOutCubic
EaseType.EaseInSine       // Smooth trigonometric curves
EaseType.EaseOutSine
EaseType.EaseInOutSine
EaseType.EaseInBack       // Overshoots then settles
EaseType.EaseOutBack
EaseType.EaseInOutBack
EaseType.EaseInElastic    // Spring-like bouncing
EaseType.EaseOutElastic
EaseType.EaseInOutElastic
EaseType.EaseInBounce     // Ball bouncing effect
EaseType.EaseOutBounce
EaseType.EaseInOutBounce
```

### AnimateFloat(from, to, duration, updateAction, easeType, onComplete)
Animates any float value with easing.

```csharp
// Animate health bar
this.AnimateFloat(0f, 100f, 2f, 
    value => healthBar.fillAmount = value / 100f,
    EaseType.EaseOutBounce,
    () => Debug.Log("Health animation complete"));

// Animate audio volume
this.AnimateFloat(1f, 0f, 3f,
    value => audioSource.volume = value,
    EaseType.EaseInQuad);
```

### AnimateVector3(from, to, duration, updateAction, easeType, onComplete)
Animates Vector3 values.

```csharp
// Custom position animation
Vector3 startPos = transform.position;
Vector3 endPos = target.position;

this.AnimateVector3(startPos, endPos, 2f,
    pos => transform.position = pos,
    EaseType.EaseInOutCubic,
    () => Debug.Log("Movement complete"));
```

### AnimateColor(from, to, duration, updateAction, easeType, onComplete)
Animates Color values.

```csharp
// Animate material color
Color startColor = renderer.material.color;
Color endColor = Color.red;

this.AnimateColor(startColor, endColor, 1f,
    color => renderer.material.color = color,
    EaseType.EaseOutQuad);
```

### Transform Easing Methods

#### EasePosition(targetPosition, duration, easeType, onComplete)
```csharp
transform.EasePosition(targetPos, 1f, EaseType.EaseOutBack);
transform.EasePosition(Vector3.zero, 2f, EaseType.EaseInOutCubic, () => {
    Debug.Log("Reached destination");
});
```

#### EaseScale(targetScale, duration, easeType, onComplete)
```csharp
transform.EaseScale(Vector3.one * 2f, 0.5f, EaseType.EaseOutElastic);
transform.EaseScale(Vector3.zero, 1f, EaseType.EaseInBack, () => {
    Destroy(gameObject);
});
```

### UI Easing Methods

#### EaseAlpha(targetAlpha, duration, easeType, onComplete)
```csharp
canvasGroup.EaseAlpha(0f, 1f, EaseType.EaseOutQuad);
canvasGroup.EaseAlpha(1f, 0.5f, EaseType.EaseInQuad, () => {
    gameObject.SetActive(false);
});
```

#### EaseColor(targetColor, duration, easeType, onComplete)
```csharp
image.EaseColor(Color.red, 0.3f, EaseType.EaseOutQuad);
image.EaseColor(Color.white, 1f, EaseType.EaseInOutSine);
```

**Real-world Examples:**

```csharp
// Button press animation
void OnButtonPressed()
{
    button.transform.EaseScale(Vector3.one * 0.9f, 0.1f, EaseType.EaseOutQuad, () => {
        button.transform.EaseScale(Vector3.one, 0.1f, EaseType.EaseOutBack);
    });
}

// Item pickup sequence
void CollectItem()
{
    // Scale up briefly
    transform.EaseScale(Vector3.one * 1.2f, 0.2f, EaseType.EaseOutBack, () => {
        // Move to UI and fade out
        transform.EasePosition(inventoryUIPos, 0.8f, EaseType.EaseInBack);
        GetComponent<SpriteRenderer>().EaseAlpha(0f, 0.8f, EaseType.EaseInQuad, () => {
            AddToInventory();
            Destroy(gameObject);
        });
    });
}

// Screen transition
void TransitionToNextLevel()
{
    fadeImage.EaseAlpha(1f, 1f, EaseType.EaseInOutQuad, () => {
        SceneManager.LoadScene("NextLevel");
    });
}
```

---

## LayerMask Extensions

### Contains(layer)
Checks if layer is included in LayerMask.

```csharp
LayerMask groundLayers = LayerMask.GetMask("Ground", "Platform");
if (groundLayers.Contains(hit.collider.gameObject.layer))
{
    Debug.Log("Hit ground!");
}
```

### Add(layer) / Remove(layer)
Adds or removes layer from LayerMask.

```csharp
LayerMask layers = LayerMask.GetMask("Ground");
layers = layers.Add(LayerMask.NameToLayer("Platform"));
layers = layers.Remove(LayerMask.NameToLayer("Ground"));
```

**Usage Examples:**
```csharp
// Dynamic layer filtering
LayerMask raycastLayers = LayerMask.GetMask("Default");

if (includeEnemies)
    raycastLayers = raycastLayers.Add(LayerMask.NameToLayer("Enemy"));

if (Physics.Raycast(ray, out hit, maxDistance, raycastLayers))
{
    // Handle hit
}
```

---

## Utility Extensions

### Remap(fromLow, fromHigh, toLow, toHigh)
Remaps value from one range to another.

```csharp
float health = 75f;
float healthPercent = health.Remap(0f, 100f, 0f, 1f); // 0.75
float redIntensity = distance.Remap(0f, 100f, 1f, 0f); // Closer = more red
```

### IsApproximatelyZero(threshold)
Checks if float is approximately zero within threshold.

```csharp
if (velocity.magnitude.IsApproximatelyZero())
{
    Debug.Log("Object is stationary");
}

if (input.x.IsApproximatelyZero(0.1f))
{
    // Stop horizontal movement
}
```

### ClampAngle()
Clamps angle to -180 to 180 range.

```csharp
float mouseX = Input.GetAxis("Mouse X");
currentRotation += mouseX;
currentRotation = currentRotation.ClampAngle();
```

**Usage Examples:**
```csharp
// Health bar color based on health percentage
float healthPercent = currentHealth.Remap(0f, maxHealth, 0f, 1f);
Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);

// Camera follow with deadzone
float distanceToPlayer = Vector3.Distance(transform.position, player.position);
if (!distanceToPlayer.IsApproximatelyZero(2f)) // 2 unit deadzone
{
    // Move camera
}
```

---

## Best Practices

### Method Chaining
Many extensions return the object they operate on for chaining:

```csharp
// Good: Chain related operations
transform.Reset()
         .SetPosition(x: 5f)
         .SetScale(2f);

canvasGroup.SetAlpha(1f)
           .SetInteractable(true);

gameObject.SetActive(true)
          .GetOrAddComponent<AudioSource>()
          .volume = 0.5f;
```

### Performance Considerations

```csharp
// Cache results when possible
Transform[] children = transform.GetChildren(); // Don't call repeatedly

// Use appropriate delay methods
this.DelayFrame(() => UpdateUI()); // Better than Delay(0f, ...)

// Prefer specific easing methods over generic ones
transform.EasePosition(target, 1f); // Better than AnimateVector3
```

### Error Handling

```csharp
// Extensions handle null checks internally
component.DestroySafe(); // Won't throw if component is null

// But still check when logical
if (enemies.IsNullOrEmpty())
    return;

Enemy target = enemies.Random(); // Safe even with empty list
```

### Common Patterns

```csharp
// UI fade sequence
void ShowMenu()
{
    menuCanvas.SetActive(true);
    menuCanvasGroup.SetAlpha(0f)
                   .SetInteractable(false)
                   .EaseAlpha(1f, 0.5f, EaseType.EaseOutQuad, () => {
                       menuCanvasGroup.SetInteractable(true);
                   });
}

// Object spawn with animation
void SpawnPickup(Vector3 position)
{
    GameObject pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
    pickup.transform.SetScale(0f)
                    .EaseScale(Vector3.one, 0.3f, EaseType.EaseOutBack);
}

// Delayed destruction with warning
void DestroyAfterWarning(GameObject target, float delay)
{
    // Flash warning
    this.DelayRepeat(0.2f, () => target.ToggleActive(), 5);
    
    // Destroy after delay
    this.Delay(delay, () => Destroy(target));
}
```

## Installation and Setup

1. **Add the Script**: Copy the extension methods script to your Unity project
2. **Add Using Statement**: Add `using UnityExtensions;` to scripts where you want to use extensions
3. **IntelliSense**: Extensions will appear in autocomplete for relevant objects
4. **Optional**: Create a `Plugins/Extensions` folder to organize extension scripts

## Version History

- **v1.0**: Initial release with basic extensions
- **v1.1**: Added comprehensive delay and easing systems
- **v1.2**: Added LayerMask extensions and improved documentation

## Support

For issues or suggestions, refer to the script comments or create custom extensions following the same patterns.

Remember: These extensions are designed to make Unity development faster and more readable. Use them to reduce boilerplate code and create more expressive, chainable APIs in your projects!