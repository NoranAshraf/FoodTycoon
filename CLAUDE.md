# FoodTycoon

Mobile idle game in Unity 6 (6000.6, URP). Game scripts live in `Assets/_Game/Scripts/`, prefabs in `Assets/_Game/Prefabs/`.

## UI

- **Always implement UI with Unity UI Toolkit: UXML for layout, USS for styling, `UIDocument` for runtime screens.**
- Do not use uGUI (`Canvas`, `Image`, `Button`, `TextMeshProUGUI`) or IMGUI (`OnGUI`) for game UI, even for quick prototypes or single elements.
- Keep UXML/USS assets under `Assets/_Game/UI/`, one UXML per screen/panel, shared styles in a common USS.
- Drive UI from C# by querying elements (`rootVisualElement.Q<...>`) and binding to game state; keep game logic out of UI scripts.
