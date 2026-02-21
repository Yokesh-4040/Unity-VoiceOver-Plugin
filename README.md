# Unity AI Voice Over Plugin

![Unity Support](https://img.shields.io/badge/Unity-2021.3%2B-green.svg)
![License](https://img.shields.io/badge/License-MIT-blue.svg)

> [!TIP]
> **Multi-Provider Support: Voiceover & Sarvam AI**
> This plugin integrates industry-leading APIs for lifelike speech synthesis.
>
> | **English & Global** | **Specialized Indic (Hindi, etc.)** |
> | :---: | :---: |
> | ![Voiceover](.github/images/voiceover_unity.png) | ![Sarvam AI](.github/images/sarvam_unity.png) |
> | [**Voiceover**](https://voiceover.io/?from=partner) | [**Sarvam AI**](https://www.sarvam.ai/) |

A powerful Unity Editor plugin that provides a seamless **Voice Over** workflow directly within your project. Integrate high-quality AI speech from providers like **Voiceover** and **Sarvam AI**, manage voice lines in modular steps, and streamline your audio pipeline without leaving the Unity Editor.

## 🗺️ Roadmap & Features

We aim to provide a comprehensive voice-over solution for Unity.

| Feature | Description | Status | Provider |
| :--- | :--- | :--- | :--- |
| **Multi-Provider Support** | Toggle between Voiceover and Sarvam AI seamlessly. | ✅ **Implemented** | All |
| **Text-to-Speech (TTS)** | Generate lifelike speech from text using standard models. | ✅ **Implemented** | All |
| **Voice Selection** | Browse and select voices from your provider library. | ✅ **Implemented** | All |
| **Batch Generation** | Generate audio for multiple lines/steps at once. | ✅ **Implemented** | All |
| **Voice History** | View and retrieve past generations. | ✅ **Implemented** | Voiceover |
| **ZIP Export** | Export generated audio as a ZIP archive. | ✅ **Implemented** | All |
| **Speech-to-Speech** | Transform input audio into a different voice. | 🚧 **Planned** | Voiceover |
| **Sound Effects (SFX)** | Generate sound effects from text descriptions. | 🚧 **Planned** | Voiceover |
| **Runtime API** | Generate voiceovers dynamically in a built game. | 🚧 **Planned** | All |
| **Timeline Integration** | Native integration with Unity's Timeline for cutscenes. | ⏳ **Backlog** | All |

> **Legend**: ✅ Implemented | 🚧 Planned (Next Up) | ⏳ Backlog (Later)

## 📦 Installation

1.  **Clone the Repository**:
    ```bash
    git clone https://github.com/Yokesh-4040/Voiceover-Unity-Plugin.git
    ```
2.  **Copy to Project**:
    *   Copy the `Assets/Voiceover` folder into your Unity project's `Assets` directory.
    *   *(Note: The folder name will be updated to 'VoiceOver' in future versions, for now it remains 'Voiceover' for compatibility).*

## 🚀 Usage

1.  **Open the Window**:
    *   Go to `Window > Voice Over` in the Unity Editor menu (or press `Cmd+Opt+V` / `Ctrl+Alt+V`).
2.  **Provider Setup**:
    *   Navigate to the **Settings** tab.
    *   Choose your **Active Provider** (Voiceover or Sarvam AI).
    *   Enter your respective API Key.
3.  **Create a Module**:
    *   Click `+ New Module` in the sidebar.
    *   Give it a name (e.g., "IntroDialogue").
    *   Select a Default Voice for the module.
4.  **Add Steps**:
    *   Click the `+` button next to the module name to add voice steps.
    *   Select a step, enter your text, and click **Generate Audio**.
5.  **Save & Use**:
    *   Once satisfied, the audio clip is automatically saved to `Assets/Voiceover/Generated/[ModuleName]`.
    *   You can now use these `AudioClip` references in your game scripts!

## 🎵 Audio Playback Architecture

The plugin features a robust audio playback system with real-time UI updates and consistent state management across all audio sources.

### Audio Flow

![Audio Playback Flow](.github/images/final_audio_flow_1769518922058.png)

**Key Components:**

1. **Audio Sources** - Four entry points for audio playback:
   - History Tab (Voiceover) - Load previously generated audio
   - Voice Modules (Select) - Auto-play when selecting steps
   - Voice Modules (Generate) - Auto-play after generation
   - Bottom Player UI - Manual playback controls

2. **Centralized Playback** - All sources route through a unified playback bridge:
   - Stops current audio
   - Updates player state
   - Creates waveform editor
   - Triggers UI update

3. **Real-time Updates** - UI reflects playback state automatically:
   - Continuously checks if audio is playing
   - Updates play/pause button icon in real-time

### Technical Details

- **Multi-Provider Bridge**: Abstracted API calls to support both Voiceover and Sarvam AI.
- **Unity AudioUtil Integration**: Uses reflection to access Unity's internal audio preview system.
- **Error Handling**: Comprehensive try-catch blocks prevent crashes.
- **Performance**: Minimal overhead with smart repaint logic.

For detailed technical documentation, see:
- [Final Audio Audit](FINAL_AUDIO_AUDIT.md) - Complete flow analysis
- [Audio Editor Review](AUDIO_EDITOR_REVIEW.md) - Editor utility usage

## 🤝 Contribution

We welcome contributions from the community! Whether it's a bug fix, new provider integration, or documentation improvement, your help is appreciated.

### Guidelines
*   Please follow the existing code style (C# standards).
*   Ensure your code compiles without errors in the latest Unity LTS.
*   Namespace all new scripts under `FF.Voiceover` (Legacy) or appropriate sub-namespaces.

## ⭐ Support the Project

If you find this plugin useful for your projects, please consider **giving it a Star** ⭐️ on GitHub! It helps more developers find the tool and motivates us to add more features.

---
*This project is an independent tool and is not officially affiliated with Voiceover or Sarvam AI.*
