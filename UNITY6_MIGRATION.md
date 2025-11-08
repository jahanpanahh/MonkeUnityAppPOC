# Unity 6 LTS Migration Guide
## MonkePOC - Migration from Unity 2022.3 LTS to Unity 6 (6000.0.62f1)

---

## Pre-Migration Checklist

### 1. Backup Current Project
- [✓] Code pushed to GitHub: https://github.com/jahanpanahh/MonkeUnityAppPOC.git
- [ ] Create local backup: `cp -r MonkePOC MonkePOC_Unity2022_Backup`
- [ ] Verify backup can be opened in Unity 2022.3 LTS
- [ ] Note current Unity version: 2022.3 LTS

### 2. Document Current Working State
**Last successful build**: Unity 2022.3 LTS
**Target device**: iPhone 12 (iOS)
**Working features**:
- Voice input (Speech-to-Text with auto-detection)
- Voice output (Text-to-Speech with Samantha voice)
- LLM integration (llama.cpp server at http://192.168.0.4:8080)
- Conversation UI with auto-scroll
- iOS build automation (frameworks + permissions)

**Critical files**:
- `Assets/_POC/Plugins/iOS/SpeechRecognizer.mm`
- `Assets/_POC/Plugins/iOS/TextToSpeech.mm`
- `Assets/_POC/Scripts/ConversationManager.cs`
- `Assets/_POC/Scripts/SpeechToText.cs`
- `Assets/_POC/Scripts/TextToSpeech.cs`
- `Assets/_POC/Editor/iOSBuildPostProcessor.cs`

**Build settings**:
- Platform: iOS
- Architecture: ARM64
- Target SDK: Device SDK
- Minimum iOS Version: 15.0+
- Bundle Identifier: com.monke.poc

---

## Migration Steps

### Step 1: Open Project in Unity 6
1. Close Unity 2022.3 completely
2. Open Unity Hub
3. Select Unity 6 LTS (6000.0.62f1) from installed editors
4. Click "Add" → "Add project from disk"
5. Select: `/Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC`
6. Unity 6 will detect older version and prompt to upgrade
7. Click "Upgrade" to convert project

**Expected behavior**: Unity will upgrade project files automatically

### Step 2: Initial Checks After Opening
1. **Check for console errors immediately after opening**
   - Look for any API deprecation warnings
   - Note any script compilation errors

2. **Verify project structure**
   - Confirm all scenes are present
   - Check that `_POC/Plugins/iOS/` folder exists
   - Verify `_POC/Scripts/` folder intact
   - Check `_POC/Editor/` folder intact

3. **Check Build Settings**
   - File → Build Settings
   - Confirm platform is still iOS
   - Verify scene "Assets/_POC/Scenes/ConversationScene.unity" is in build list

---

## Post-Migration Testing Checklist

### Phase 1: Unity Editor Tests
- [ ] Open ConversationScene in Unity 6
- [ ] Press Play - check for console errors
- [ ] Verify UI displays correctly
- [ ] Check that all SerializeField references are connected:
  - ConversationManager: micButton, conversationText, micStatusText, scrollRect
  - ConversationManager: speechToText, textToSpeech components
- [ ] Verify simulated TTS works in editor (2-second delay)

### Phase 2: Build System Tests
- [ ] File → Build Settings → iOS
- [ ] Click "Build" (or Build and Run)
- [ ] **Critical**: Watch for post-processor logs in console:
  ```
  iOS Post-Process: Starting...
  iOS Post-Process: Info.plist updated successfully
  iOS Post-Process: Added frameworks to UnityFramework target
  iOS Post-Process: Added frameworks to Unity-iPhone target
  iOS Post-Process: Complete!
  ```
- [ ] Check for any build errors or warnings

### Phase 3: Xcode Verification
After Unity build completes, open Xcode project and verify:

1. **Info.plist entries** (in Unity-iPhone target):
   - [ ] `NSSpeechRecognitionUsageDescription`
   - [ ] `NSMicrophoneUsageDescription`
   - [ ] `NSLocalNetworkUsageDescription`
   - [ ] `NSBonjourServices` array with `_http._tcp`
   - [ ] `NSAppTransportSecurity` → `NSAllowsArbitraryLoads` = YES

2. **Frameworks** (check BOTH targets):
   - [ ] **UnityFramework** target has:
     - Speech.framework
     - AVFoundation.framework
   - [ ] **Unity-iPhone** target has:
     - Speech.framework
     - AVFoundation.framework

3. **Native plugin files**:
   - [ ] `SpeechRecognizer.mm` present in project
   - [ ] `TextToSpeech.mm` present in project

### Phase 4: Device Testing
Deploy to iPhone 12 and test full workflow:

1. **Launch test**:
   - [ ] App launches successfully
   - [ ] No crash on first launch
   - [ ] Welcome message appears: "Hello friend! I'm Monke!..."
   - [ ] Mic button visible and tap-able

2. **Voice input test**:
   - [ ] Tap mic button
   - [ ] Permission prompt appears (first time only)
   - [ ] Status shows "Listening... (speak naturally)"
   - [ ] Button turns red while recording
   - [ ] Speak: "Hello Monke"
   - [ ] Wait 2 seconds after speaking
   - [ ] Recording auto-stops
   - [ ] "You: Hello Monke" appears in conversation
   - [ ] "Monke: ..." thinking indicator appears
   - [ ] Status shows "Monke is thinking..."

3. **LLM integration test**:
   - [ ] Response arrives within ~1-2 seconds
   - [ ] "Monke: [response]" appears in conversation
   - [ ] Check that response is relevant to "Hello Monke"
   - [ ] Verify asterisk actions (like *jumps*) appear in text

4. **Voice output test**:
   - [ ] Status changes to "Monke is speaking..."
   - [ ] TTS begins speaking automatically
   - [ ] Voice is Samantha (kid-friendly)
   - [ ] Asterisk actions are NOT spoken (filtered out)
   - [ ] After speech completes, status returns to "Tap to speak"

5. **Second interaction test**:
   - [ ] Tap mic button again
   - [ ] Recording works correctly
   - [ ] Full cycle works again (this was previously broken)
   - [ ] Repeat 2-3 times to ensure stability

6. **Scroll test**:
   - [ ] Have 5-6 exchanges to fill screen
   - [ ] Verify conversation auto-scrolls to bottom
   - [ ] Latest message always visible

---

## Known Compatibility Issues & Solutions

### Issue 1: PBXProject API Changes
**Risk**: Low - API stable since Unity 2019.3
**Symptoms**: Post-processor fails to add frameworks
**Solution**: If `GetUnityMainTargetGuid()` fails, use fallback:
```csharp
string mainTarget = project.GetUnityMainTargetGuid();
if (string.IsNullOrEmpty(mainTarget)) {
    mainTarget = project.TargetGuidByName("Unity-iPhone");
}
```
**Status**: Already implemented in current code (line 74 of iOSBuildPostProcessor.cs)

### Issue 2: TextMeshPro Package
**Risk**: Low - should auto-upgrade
**Symptoms**: Missing TMP components, UI text not rendering
**Solution**:
1. Window → Package Manager
2. Search "TextMesh Pro"
3. Ensure it's installed
4. If needed: Import "TMP Essential Resources"

### Issue 3: iOS Plugin Compatibility
**Risk**: Very Low - Native Objective-C code
**Symptoms**: Build errors in .mm files
**Solution**: Native iOS APIs (SFSpeechRecognizer, AVSpeechSynthesizer) are unchanged
- No code changes needed
- Verify `#import` statements work

### Issue 4: UnityWebRequest Changes
**Risk**: Low - API mature
**Symptoms**: HTTP requests fail
**Solution**: Current implementation uses standard APIs:
```csharp
UnityWebRequest.Post(serverUrl, jsonData, "application/json")
```
This should work identically in Unity 6.

---

## Rollback Plan

If migration fails or critical issues found:

### Option A: Quick Rollback (Keep Unity 6)
1. Close Unity 6
2. Open Unity Hub
3. Right-click project → Remove from list
4. Re-add project using Unity 2022.3 LTS
5. Project files should revert automatically

### Option B: Full Rollback (Restore Backup)
1. Close Unity completely
2. Delete: `/Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC`
3. Restore from backup: `cp -r MonkePOC_Unity2022_Backup MonkePOC`
4. Open with Unity 2022.3 LTS

### Option C: Git Rollback
```bash
cd /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC
git status  # Check what changed during migration
git checkout .  # Revert Unity meta file changes
git clean -fd  # Remove Unity 6 generated files
```

---

## Unity 6 Specific Considerations

### New Features to Consider (Post-Migration)
1. **Improved build times**: Unity 6 has faster iOS builds
2. **Better Xcode integration**: More reliable post-processing
3. **Updated UI Toolkit**: (We're using uGUI, so no impact)
4. **Enhanced profiler**: Useful for optimizing LLM response times

### Settings to Verify
1. **Player Settings → iOS**:
   - [ ] "Allow downloads over HTTP" = "Always allowed"
   - [ ] Target minimum iOS version = 15.0+
   - [ ] Architecture = ARM64
   - [ ] Automatically Sign = ON
   - [ ] Signing Team ID = your personal team

2. **Editor Settings**:
   - [ ] Asset Serialization Mode = "Force Text" (for better Git diffs)
   - [ ] Version Control = "Visible Meta Files"

---

## Success Criteria

Migration is successful when:
1. ✅ Project opens in Unity 6 without errors
2. ✅ All scripts compile successfully
3. ✅ Build completes without errors
4. ✅ Post-processor adds all frameworks to both targets
5. ✅ App runs on iPhone 12
6. ✅ Voice input works with auto-detection
7. ✅ LLM responses arrive correctly
8. ✅ Voice output works with filtered asterisks
9. ✅ Multiple conversation cycles work
10. ✅ No regressions from Unity 2022.3 functionality

---

## Important Notes

### DO NOT PANIC IF:
- Unity shows "Upgrading project..." on first open (expected)
- Some warnings appear in console (check if they're critical)
- Unity reimports all assets (normal during upgrade)

### STOP AND INVESTIGATE IF:
- Red console errors mentioning `PBXProject` or `UnityEditor.iOS.Xcode`
- Build fails with framework-related errors
- Post-processor logs don't appear during build
- App crashes immediately on launch

### CONTACT POINTS:
- **Unity 6 Documentation**: https://docs.unity3d.com/6000.0/Documentation/Manual/
- **PBXProject API Docs**: https://docs.unity3d.com/ScriptReference/iOS.Xcode.PBXProject.html
- **Migration Guide**: https://docs.unity3d.com/6000.0/Documentation/Manual/UpgradeGuides.html

---

## Partner Collaboration Setup (After Migration)

Once Unity 6 migration is verified:
1. Push Unity 6 changes to GitHub
2. Partner clones repository
3. Partner needs:
   - Unity 6 LTS (6000.0.62f1) installed
   - Xcode 15+ installed
   - Git LFS for large files (if we add assets later)
4. Partner should follow "Phase 4: Device Testing" checklist

---

## Timeline

- **Migration**: 5-10 minutes (Unity upgrade)
- **Initial checks**: 5 minutes
- **Build test**: 10-15 minutes
- **Device testing**: 15-20 minutes
- **Total**: ~45-60 minutes

---

## Current Status
- [ ] Backup created
- [ ] Unity 6 opened project
- [ ] Initial checks passed
- [ ] Build successful
- [ ] Device testing passed
- [ ] Migration complete

**Start time**: _________________
**Completion time**: _________________
**Issues encountered**: _________________
