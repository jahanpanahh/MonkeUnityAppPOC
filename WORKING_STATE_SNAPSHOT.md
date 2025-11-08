# Working State Snapshot - Unity 2022.3 LTS
## Before Unity 6 Migration
**Date**: 2025-11-07
**Project**: MonkePOC (Monke AI Companion POC)

---

## Environment
- **Unity Version**: 2022.3 LTS
- **Target Platform**: iOS
- **Test Device**: iPhone 12
- **macOS Version**: Darwin 25.1.0
- **Xcode Version**: Latest compatible with Unity 2022.3
- **LLM Server**: llama.cpp server running on Mac
- **Model**: Llama-3.2-1B-Instruct-Q4_K_M.gguf (excluded from Git)

---

## Working Features

### 1. Voice Input (Speech-to-Text)
**Status**: ✅ Fully Working
**Implementation**: Native iOS plugin using SFSpeechRecognizer
**Behavior**:
- Tap mic button once to start recording
- Button turns red, status shows "Listening... (speak naturally)"
- Automatically detects end of speech (2-second silence)
- Auto-stops recording and sends transcription to LLM
- No need to tap again to stop

**Files**:
- `Assets/_POC/Plugins/iOS/SpeechRecognizer.mm` (Native Objective-C)
- `Assets/_POC/Scripts/SpeechToText.cs` (Unity bridge)

**Key technical details**:
- Uses silence timer (2 seconds) to detect speech end
- Stores partial results internally, only sends final transcription
- Properly handles audio session (PlayAndRecord mode)
- Callbacks marshaled correctly from native to Unity

### 2. Voice Output (Text-to-Speech)
**Status**: ✅ Fully Working
**Implementation**: Native iOS plugin using AVSpeechSynthesizer
**Behavior**:
- Automatically speaks LLM responses
- Uses kid-friendly voice (Samantha)
- Filters out asterisk actions (e.g., *jumps excitedly*) before speaking
- Displays full text with asterisks in UI
- Status shows "Monke is speaking..." during playback
- Returns to "Tap to speak" when done

**Files**:
- `Assets/_POC/Plugins/iOS/TextToSpeech.mm` (Native Objective-C)
- `Assets/_POC/Scripts/TextToSpeech.cs` (Unity bridge)

**Key technical details**:
- Voice parameters: rate=0.48, pitch=1.15, volume=1.0
- Completion callback properly triggers Unity event
- Regex filter: `@"\*[^*]+\*"` removes stage directions

### 3. LLM Integration
**Status**: ✅ Fully Working
**Implementation**: HTTP POST to llama.cpp server
**Performance**: ~1 second response time

**Server details**:
- URL in Unity Editor: `http://localhost:8080/completion`
- URL on iOS device: `http://192.168.0.4:8080/completion`
- Server command: `./llama-server -m Llama-3.2-1B-Instruct-Q4_K_M.gguf -ngl 99`
- Uses Metal GPU acceleration

**Request parameters**:
```json
{
  "prompt": "You are Monke, a curious and friendly monkey companion for children aged 7-12...",
  "n_predict": 100,
  "temperature": 0.7,
  "stop": ["User:", "\n\n"]
}
```

**Files**:
- `Assets/_POC/Scripts/ConversationManager.cs` (lines 172-256)

### 4. Conversation UI
**Status**: ✅ Fully Working
**Layout**:
- ScrollRect with vertical scrollbar
- TextMeshProUGUI for conversation history
- Auto-scrolls to bottom on new messages
- Button turns red during recording, white otherwise
- Status text shows current state

**UI Flow**:
1. Welcome message on start
2. "Tap to speak" → Tap → "Listening..."
3. User speaks → 2 sec silence → "Monke is thinking..."
4. LLM response → "Monke is speaking..."
5. Speech done → "Tap to speak"

**Files**:
- `Assets/_POC/Scenes/ConversationScene.unity`
- `Assets/_POC/Scripts/ConversationManager.cs`

### 5. iOS Build Automation
**Status**: ✅ Fully Working
**Implementation**: PostProcessBuild script
**Actions**:
- Adds frameworks to both UnityFramework and Unity-iPhone targets
- Adds required permissions to Info.plist
- Enables HTTP connections (NSAllowsArbitraryLoads)
- Adds Bonjour services for local network

**Files**:
- `Assets/_POC/Editor/iOSBuildPostProcessor.cs`

**Frameworks added**:
- Speech.framework
- AVFoundation.framework

**Permissions added**:
- NSSpeechRecognitionUsageDescription
- NSMicrophoneUsageDescription
- NSLocalNetworkUsageDescription

---

## File Checksums (for verification after migration)

Critical files that should NOT change during Unity 6 migration:

### Native Plugins (Objective-C - should be identical)
```
Assets/_POC/Plugins/iOS/SpeechRecognizer.mm
Assets/_POC/Plugins/iOS/TextToSpeech.mm
```

### Unity Scripts (C# - may get minor meta file changes)
```
Assets/_POC/Scripts/ConversationManager.cs
Assets/_POC/Scripts/SpeechToText.cs
Assets/_POC/Scripts/TextToSpeech.cs
Assets/_POC/Editor/iOSBuildPostProcessor.cs
```

### Scene File
```
Assets/_POC/Scenes/ConversationScene.unity
```

---

## Exact Build Settings (Unity 2022.3)

### Player Settings → iOS
- **Company Name**: DefaultCompany
- **Product Name**: MonkePOC
- **Bundle Identifier**: com.monke.poc
- **Version**: 0.1
- **Build Number**: 1
- **Target minimum iOS Version**: 15.0
- **Architecture**: ARM64
- **Scripting Backend**: IL2CPP
- **Target SDK**: Device SDK
- **Allow downloads over HTTP**: Always allowed

### Build Settings
- **Platform**: iOS
- **Scenes in Build**:
  - [0] Assets/_POC/Scenes/ConversationScene.unity ✓
- **Development Build**: Unchecked
- **Autoconnect Profiler**: Unchecked
- **Script Debugging**: Unchecked

### Xcode Project Settings (after Unity build)
- **Automatically manage signing**: YES
- **Team**: Personal Team (Hiren Sakaria)
- **Signing Certificate**: Apple Development
- **Provisioning Profile**: Automatic

---

## Known Working Flow (End-to-End)

### Successful Test Sequence
1. Launch app on iPhone 12
2. See "Hello friend! I'm Monke! What would you like to talk about?"
3. Tap mic button (turns red)
4. Say: "Hello Monke, how are you?"
5. Wait 2 seconds (auto-stops)
6. See: "You: Hello Monke, how are you?"
7. See: "Monke: ..." (thinking indicator)
8. Wait ~1 second
9. See: "Monke: Hi there! I'm doing great, thanks for asking! *bounces up and down* What would you like to explore together?"
10. Hear Samantha voice say: "Hi there! I'm doing great, thanks for asking! What would you like to explore together?" (no "bounces up and down")
11. Status returns to "Tap to speak"
12. Tap mic again - cycle repeats successfully

**This exact flow has been tested and verified working.**

---

## Git Repository State

**Repository**: https://github.com/jahanpanahh/MonkeUnityAppPOC.git
**Branch**: main
**Last commit**: Initial commit with Unity 2022.3 project

**Excluded files** (in .gitignore):
- All Unity standard excludes (Library/, Temp/, Builds/, etc.)
- `Assets/StreamingAssets/Llama-3.2-1B-Instruct-Q4_K_M.gguf` (770MB model)

**Included files**:
- All C# scripts
- All .mm native plugins
- Scene files
- Editor scripts
- Build post-processor

---

## Dependencies

### Unity Packages
- **TextMeshPro**: Installed (for UI text)
- **iOS Build Support**: Installed
- **All other packages**: Default Unity 2022.3 packages

### External Dependencies
- **llama.cpp**: Compiled with Metal support
- **Model file**: Must be in `Assets/StreamingAssets/` (not in Git)

### iOS Frameworks (auto-added by post-processor)
- Speech.framework
- AVFoundation.framework

---

## Known Issues (Already Fixed)

These issues were encountered and FIXED during development:
1. ✅ Partial speech results triggering multiple LLM calls → Fixed with silence timer
2. ✅ Audio session configuration error → Fixed with PlayAndRecord mode
3. ✅ HTTP blocked on iOS → Fixed with NSAllowsArbitraryLoads
4. ✅ Speech.framework not in Unity-iPhone target → Fixed with dual target adding
5. ✅ Recording only working once → Fixed with proper cleanup
6. ✅ Asterisks being spoken → Fixed with regex filter
7. ✅ Model file too large for GitHub → Fixed with .gitignore

**All features are currently working with no known bugs.**

---

## Performance Metrics

- **LLM Response Time**: ~1 second (with Metal GPU)
- **TTS Latency**: Immediate (iOS native)
- **STT Latency**: Real-time transcription
- **App Launch Time**: Normal after first launch
- **Memory Usage**: Acceptable on iPhone 12

---

## Critical Configuration Details

### ConversationManager.cs Settings
```csharp
[SerializeField] private string serverUrl = "http://192.168.0.4:8080/completion";
```

### SpeechRecognizer.mm Settings
```objective-c
// Silence detection timeout
self.silenceTimer = [NSTimer scheduledTimerWithTimeInterval:2.0 ...];
```

### TextToSpeech.mm Settings
```objective-c
utterance.rate = 0.48f;
utterance.pitchMultiplier = 1.15f;
utterance.volume = 1.0f;
```

### LLM Prompt Template
```
You are Monke, a curious and friendly monkey companion for children aged 7-12.
You explore topics together, ask thoughtful questions, and encourage learning through curiosity.
You never claim to know everything - you discover things together with the child.
Keep responses to 2-3 sentences maximum.

User: {userMessage}
Monke:
```

---

## Verification Commands

To verify this snapshot after migration:

```bash
# Check file existence
ls -la Assets/_POC/Plugins/iOS/SpeechRecognizer.mm
ls -la Assets/_POC/Plugins/iOS/TextToSpeech.mm
ls -la Assets/_POC/Scripts/ConversationManager.cs
ls -la Assets/_POC/Editor/iOSBuildPostProcessor.cs

# Check Git status
cd /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC
git status

# Check .gitignore for model exclusion
grep "Llama-3.2-1B-Instruct-Q4_K_M.gguf" .gitignore
```

---

## Rollback Information

If Unity 6 migration fails, this snapshot documents the exact working state to return to.

**Backup command**:
```bash
cp -r /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC \
     /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC_Unity2022_Backup
```

**Restore command**:
```bash
rm -rf /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC
cp -r /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC_Unity2022_Backup \
     /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC
```

---

## Sign-off

This snapshot represents a fully working Unity 2022.3 project with:
- ✅ Voice input with auto-detection
- ✅ Voice output with kid-friendly voice
- ✅ LLM integration with ~1s response
- ✅ Automated iOS build configuration
- ✅ Full conversation loop tested on iPhone 12
- ✅ No known bugs or issues

**Ready for Unity 6 migration.**
