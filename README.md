# TinyUnityScripts

**TinyUnityScripts** is a set of modular scripts designed to integrate the features of the **TinyTroupe** repository into Unity projects. These scripts allow developers to link NPCs in their Unity games to the **TinyTroupe** repository, enabling deep, social interactions between characters. The system includes tools for handling prompts, server communication, and speech synthesis, offering a robust foundation for adding advanced social dynamics to Unity NPCs.

### Features
- **TinyTroupe Integration**: Connect NPCs to the **TinyTroupe** repository for in-depth, socially interactive characters.
- **Server Communication**: A Python Flask server, implemented in `Unity3.py`, facilitates communication between Unity and the TinyTroupe API, running on the same machine as **ollama/tinytroupe**.
- **Speech Synthesis**: Leverages Microsoft Azure TTS for character dialogue, with an option to display text responses on UI elements instead.
- **Multi-Character Speech Flow**: Manages dialogue flow between multiple characters with unique voices.
- **Animator Component Integration**: Supports animating characters while they "speak" (can be omitted if animations are not required).

### Script Details
1. **Unity3.py**: A Python Flask server that handles the connection between Unity and the TinyTroupe repository. This script enables sending prompts and receiving responses from **ollama/tinytroupe** running locally.
2. **TinyTroupeConversationManager.cs**: Handles the communication between Unity and the TinyTroupe server using UnityWebRequest. It sends prompts and retrieves responses to integrate with your game.
3. **CharacterSpeech.cs**: Manages speech synthesis using Microsoft Azure TTS. This script can be replaced with another TTS solution or omitted for text-based dialogue display.
4. **SpeechManager.cs**: Oversees the dialogue flow between multiple characters, ensuring smooth transitions and unique voices for each NPC.

### Example Usage
- The example implementation uses Microsoft Azure TTS for spoken dialogue. If TTS is not required, responses can be displayed on a canvas or other UI elements instead.
- Designed to work with Unity's Animator component, enabling characters to animate while speaking. References to animations can be removed if they are not part of your setup.

### Learn More
For more information about using local AI agents as NPCs in Unity, watch the video:  
[**Using Local AI Agents As NPCs In A Unity Game (Qwen2.5 & Ollama)**](https://youtu.be/pP2-TS-z_nY)

### Purpose
These scripts provide a skeleton implementation, making it easier to integrate **TinyTroupe's** social agent capabilities into Unity NPCs. Developers can build upon this framework to add unique and dynamic social interactions to their games. The design prioritizes simplicity, allowing for easy customization and adaptation to different project needs.
