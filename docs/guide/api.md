# API Reference

The plugin is currently focused as an Editor window tool, meaning most functionality relies on Unity's UI and editor-side scripts. 

Runtime APIs to generate audio directly from game scripts are **Planned**. 

## Editor Windows

- `VoiceoverEditorWindow`: The primary Hub Window.
- Use the central window to manage your voice clips and project state. The GUI ensures all generations, steps, and history are kept up-to-date and serialized.

## Audio Architecture

1. **Multi-Provider Bridge:** The system relies on a common interface to manage different APIs asynchronously.
2. **Audio Playback:** Uses `UnityEditor.AudioUtil` internally to preview audio directly without requiring a GameObject.

*(Full script reference will be added in upcoming builds)*
