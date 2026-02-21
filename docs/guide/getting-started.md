# Getting Started

The **Unity AI Voice Over Plugin** is a powerful Unity Editor plugin that provides a seamless workflow directly within your project.

## Installation

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/Yokesh-4040/Unity-VoiceOver-Plugin.git
   ```
2. **Copy to Project**:
   - Copy the `Assets/Voiceover` folder into your Unity project's `Assets` directory.

## Usage Guide

1. **Open the Window**:
   - Go to `Window > Voice Over` in the Unity Editor menu (or press `Cmd+Opt+V` / `Ctrl+Alt+V`).
2. **Provider Setup**:
   - Navigate to the **Settings** tab.
   - Choose your **Active Provider** (Voiceover or Sarvam AI).
   - Enter your respective API Key.
3. **Create a Module**:
   - Click `+ New Module` in the sidebar.
   - Give it a name (e.g., "IntroDialogue").
   - Select a Default Voice for the module.
4. **Add Steps**:
   - Click the `+` button next to the module name to add voice steps.
   - Select a step, enter your text, and click **Generate Audio**.
5. **Save & Use**:
   - Once satisfied, the audio clip is automatically saved to `Assets/Voiceover/Generated/[ModuleName]`.
   - You can now use these `AudioClip` references in your game scripts!
