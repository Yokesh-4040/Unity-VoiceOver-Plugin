# ElevenLabs Unity Plugin

![ElevenLabs Unity Plugin](https://img.shields.io/badge/Unity-2021.3%2B-green.svg)
![License](https://img.shields.io/badge/License-MIT-blue.svg)

> [!TIP]
> **Powered by ElevenLabs**
> This plugin uses the industry-leading **ElevenLabs API** for lifelike speech synthesis.
> [**Create your free account here**](https://elevenlabs.io/?from=partner) to get your API key and start generating voices!

A powerful Unity Editor plugin that integrates the **ElevenLabs Text-to-Speech API** directly into your workflow. Generate high-quality AI voiceovers, manage voice lines in modular steps, and streamline your comprehensive audio pipeline without leaving Unity.

## 🗺️ Roadmap & Features

## 🗺️ Roadmap & Features

We plan to bring the full power of ElevenLabs to Unity. Below is the roadmap of features we are implementing.

| Feature | Description | Status | Priority |
| :--- | :--- | :--- | :--- |
| **Text-to-Speech (TTS)** | Generate lifelike speech from text using standard models. | ✅ **Implemented** | - |
| **Voice Selection** | Browse and select voices from your ElevenLabs library. | ✅ **Implemented** | - |
| **Batch Generation** | Generate audio for multiple lines/steps at once. | ✅ **Implemented** | - |
| **Voice History** | View and retrieve past generations. | ✅ **Implemented** | - |
| **ZIP Export** | Export generated audio as a ZIP archive. | ✅ **Implemented** | - |
| **Speech-to-Speech** | Transform input audio into a different voice (e.g., creature voices). | 🚧 **Planned** | High |
| **Sound Effects (SFX)** | Generate sound effects from text descriptions. | 🚧 **Planned** | High |
| **Runtime API** | Generate voiceovers dynamically in a built game (at runtime). | 🚧 **Planned** | High |
| **Voice Design** | Create new custom voices directly within the Editor. | ⏳ **Backlog** | Medium |
| **Dubbing / Localization** | Auto-translate and dub voice lines into multiple languages. | ⏳ **Backlog** | Medium |
| **Pronunciation Dictionaries** | Custom rules for character names and lore terms. | ⏳ **Backlog** | Low |
| **Timeline Integration** | Native integration with Unity's Timeline for cutscenes. | ⏳ **Backlog** | Low |

> **Legend**: ✅ Implemented | 🚧 Planned (Next Up) | ⏳ Backlog (Later)

## 📦 Installation

1.  **Clone the Repository**:
    ```bash
    git clone https://github.com/Yokesh-4040/ElevenLabs-Unity-Plugin.git
    ```
2.  **Copy to Project**:
    *   Copy the `Assets/ElevenLabs` folder into your Unity project's `Assets` directory.
    *   *Alternatively, you can just drag and drop the folder if you downloaded the ZIP.*

## 🚀 Usage

1.  **Open the Window**:
    *   Go to `Window > Voice Over` in the Unity Editor menu (or press `Cmd+Opt+V` / `Ctrl+Alt+V`).
2.  **Authentication**:
    *   Enter your ElevenLabs API Key (found in your [ElevenLabs Profile](https://elevenlabs.io/)).
    *   The key is stored securely in your local EditorPrefs.
3.  **Create a Module**:
    *   Click `+ New Module` in the sidebar.
    *   Give it a name (e.g., "IntroDialogue").
    *   Select a Default Voice for the module.
4.  **Add Steps**:
    *   Click the `+` button next to the module name to add voice steps.
    *   Select a step, enter your text, and click **Generate Audio**.
5.  **Save & Use**:
    *   Once satisfied, the audio clip is automatically saved to `Assets/ElevenLabs/Generated/[ModuleName]`.
    *   You can now use these `AudioClip` references in your game scripts!

## 🤝 Contribution

We welcome contributions from the community! Whether it's a bug fix, new feature, or documentation improvement, your help is appreciated.

### How to Contribute
1.  **Fork the Repository**: Click the "Fork" button at the top right of this page.
2.  **Clone your Fork**: `git clone https://github.com/YOUR_USERNAME/ElevenLabs-Unity-Plugin.git`
3.  **Create a Branch**: `git checkout -b feature/amazing-feature`
4.  **Make Changes**: Implement your feature or fix.
5.  **Commit**: `git commit -m "Add amazing feature"`
6.  **Push**: `git push origin feature/amazing-feature`
7.  **Open a Pull Request**: Submit your PR to the `main` branch of this repository.

### Guidelines
*   Please follow the existing code style (C# standards).
*   Ensure your code compiles without errors in the latest Unity LTS.
*   Namespace all new scripts under `FF.ElevenLabs` or `FF.ElevenLabs.Editor`.

## ⭐ Support the Project

If you find this plugin useful for your projects, please consider **giving it a Star** ⭐️ on GitHub! It helps more developers find the tool and motivates us to add more features.

---
*This project is not officially affiliated with ElevenLabs.*
