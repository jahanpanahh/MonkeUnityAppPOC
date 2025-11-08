# Unity 6 Migration Test Plan
## Comprehensive Testing Checklist for MonkePOC

---

## Pre-Migration Backup

**BEFORE OPENING IN UNITY 6**, create backup:

```bash
cd /Users/hirensakaria/Documents/Projects/MonkeUnity
cp -r MonkePOC MonkePOC_Unity2022_Backup
```

**Verify backup**:
```bash
ls -la MonkePOC_Unity2022_Backup/Assets/_POC/
```

Expected output should show:
- Editor/
- Plugins/
- Scenes/
- Scripts/

---

## Phase 1: Initial Migration (5-10 minutes)

### Step 1: Open Project in Unity 6
- [ ] Close all Unity instances
- [ ] Open Unity Hub
- [ ] Select Unity 6 LTS (6000.0.62f1)
- [ ] Click "Add" → Add project from disk
- [ ] Select: `/Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC`
- [ ] Wait for upgrade dialog
- [ ] Click "Upgrade"
- [ ] Wait for project to open (may take 5-10 minutes)

**Expected behavior**:
- Progress bar showing "Upgrading project..."
- "Importing assets..." progress
- Unity Editor opens with project

**Stop if**:
- Error dialog appears during upgrade
- Unity crashes during import
- Red errors flood the console immediately

### Step 2: First Console Check
Once Unity 6 Editor opens:

- [ ] Open Console window (Window → General → Console)
- [ ] Check error count (top right)
- [ ] Review any red errors
- [ ] Note any yellow warnings

**Pass criteria**:
- 0-2 errors maximum (ignore deprecation warnings)
- No errors mentioning "PBXProject", "iOS", or "DllImport"

**Log errors here**:
```
_______________________________________
```

### Step 3: Verify Project Structure
In Project window, verify all folders exist:

- [ ] Assets/_POC/Editor/ (contains iOSBuildPostProcessor.cs)
- [ ] Assets/_POC/Plugins/ (contains iOS folder)
- [ ] Assets/_POC/Plugins/iOS/ (contains .mm files)
- [ ] Assets/_POC/Scenes/ (contains ConversationScene.unity)
- [ ] Assets/_POC/Scripts/ (contains C# scripts)

**Pass criteria**: All folders present with files

### Step 4: Verify Scene
- [ ] Double-click `Assets/_POC/Scenes/ConversationScene.unity`
- [ ] Scene loads without errors
- [ ] Hierarchy shows:
  - Canvas
    - ConversationScrollArea
    - InputPanel
    - MicButton
    - MicStatusText
  - ConversationManager
  - SpeechToTextManager
  - TextToSpeechManager
  - EventSystem

**Pass criteria**: Scene loads, all GameObjects visible in Hierarchy

---

## Phase 2: Component Verification (5 minutes)

### Test 1: ConversationManager Component
- [ ] Select "ConversationManager" in Hierarchy
- [ ] Look at Inspector

**Verify all references assigned**:
- [ ] Mic Button → [MicButton]
- [ ] Conversation Text → [ConversationText]
- [ ] Mic Status Text → [MicStatusText]
- [ ] Scroll Rect → [ConversationScrollArea]
- [ ] Speech To Text → [SpeechToTextManager]
- [ ] Text To Speech → [TextToSpeechManager]
- [ ] Server Url = "http://192.168.0.4:8080/completion"

**Pass criteria**: All fields show blue references (not "None")

**If references missing**:
1. Check if GameObjects exist in Hierarchy
2. Manually drag-and-drop to reassign
3. Or restore scene from Git: `git checkout Assets/_POC/Scenes/ConversationScene.unity`

### Test 2: SpeechToText Component
- [ ] Select "SpeechToTextManager" in Hierarchy
- [ ] Verify component attached
- [ ] No errors in Inspector

### Test 3: TextToSpeech Component
- [ ] Select "TextToSpeechManager" in Hierarchy
- [ ] Verify component attached
- [ ] No errors in Inspector

### Test 4: Script Compilation
- [ ] Open `Assets/_POC/Scripts/ConversationManager.cs` in your code editor
- [ ] Make a trivial change (add a space)
- [ ] Save file
- [ ] Return to Unity
- [ ] Wait for recompilation
- [ ] Check Console for errors

**Pass criteria**: No compilation errors

---

## Phase 3: Editor Play Mode Test (2 minutes)

### Test 1: Basic Play Mode
- [ ] Click Play button
- [ ] Wait for scene to start
- [ ] Check Console for errors
- [ ] Check Game view shows UI

**Expected behavior**:
- UI visible in Game view
- Welcome message appears in conversation area
- Mic button visible
- Status text shows "Tap to speak"
- No red errors in Console

**Pass criteria**: Game runs without errors

### Test 2: Simulated TTS in Editor
- [ ] While in Play mode
- [ ] Click "Tap to speak" button in Game view
- [ ] Check Console logs

**Expected logs**:
```
STT: Would start recording (simulated)
```

**Pass criteria**: Button responds, logs appear

- [ ] Click Stop button
- [ ] Wait 2 seconds

**Expected**:
- Console shows: "TTS: Would speak (simulated): [text]"
- After 2 seconds: "TTS: Speech completed"

---

## Phase 4: Build Test (10-15 minutes)

### Step 1: Build Settings Verification
- [ ] File → Build Settings
- [ ] Platform shows "iOS" (with Unity icon next to it)
- [ ] Scene list shows:
  - [✓] Assets/_POC/Scenes/ConversationScene.unity

**If iOS not selected**:
- [ ] Select iOS
- [ ] Click "Switch Platform"
- [ ] Wait for platform switch

### Step 2: Player Settings Check
- [ ] In Build Settings, click "Player Settings"
- [ ] Select "iOS" tab

**Verify settings**:
- [ ] Company Name: DefaultCompany
- [ ] Product Name: MonkePOC
- [ ] Bundle Identifier: com.monke.poc
- [ ] Target minimum iOS Version: 15.0 or higher
- [ ] Architecture: ARM64
- [ ] "Allow downloads over HTTP": Always allowed (or equivalent in Unity 6)

**Pass criteria**: All settings match Unity 2022.3 configuration

### Step 3: Build to Xcode
- [ ] In Build Settings, click "Build"
- [ ] Choose location: `/Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC/Builds/iOS`
- [ ] Click "Save"
- [ ] Wait for build to complete (5-10 minutes)

**CRITICAL: Watch Console During Build**

Look for post-processor logs:
```
iOS Post-Process: Starting...
iOS Post-Process: Info.plist updated successfully
iOS Post-Process: Added frameworks to UnityFramework target
iOS Post-Process: Added frameworks to Unity-iPhone target
iOS Post-Process: Complete!
```

**Pass criteria**:
- Build completes successfully
- Post-processor logs appear
- No errors in Console

**If post-processor logs DON'T appear**:
1. Build still succeeded? Continue to next phase
2. Note: Will need to manually add frameworks in Xcode

**Log build result**:
```
Build succeeded: YES / NO
Post-processor ran: YES / NO
Errors: _______________________
```

---

## Phase 5: Xcode Verification (5 minutes)

### Step 1: Open Xcode Project
```bash
cd /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC/Builds/iOS
open Unity-iPhone.xcodeproj
```

### Step 2: Verify Info.plist
- [ ] In Xcode, select Unity-iPhone target
- [ ] Click "Info" tab
- [ ] Verify custom iOS target properties exist:

**Required entries**:
- [ ] Privacy - Speech Recognition Usage Description
- [ ] Privacy - Microphone Usage Description
- [ ] Privacy - Local Network Usage Description
- [ ] Bonjour services → _http._tcp

**Pass criteria**: All 4 entries present

**If missing**: Post-processor failed, see "Manual Fix" section below

### Step 3: Verify Frameworks (UnityFramework target)
- [ ] In Xcode, select "UnityFramework" target (not Unity-iPhone)
- [ ] Click "Build Phases" tab
- [ ] Expand "Link Binary With Libraries"
- [ ] Scroll through frameworks list

**Required frameworks**:
- [ ] Speech.framework (Status: Required)
- [ ] AVFoundation.framework (Status: Required)

### Step 4: Verify Frameworks (Unity-iPhone target)
- [ ] In Xcode, select "Unity-iPhone" target
- [ ] Click "Build Phases" tab
- [ ] Expand "Link Binary With Libraries"
- [ ] Check for frameworks

**Required frameworks**:
- [ ] Speech.framework (Status: Required)
- [ ] AVFoundation.framework (Status: Required)

**Pass criteria**: Both frameworks in BOTH targets

**If missing**: See "Manual Fix" section below

### Step 5: Verify Native Plugin Files
- [ ] In Xcode Project Navigator (left sidebar)
- [ ] Expand Libraries folder
- [ ] Look for .mm files

**Required files**:
- [ ] SpeechRecognizer.mm
- [ ] TextToSpeech.mm

**Pass criteria**: Both files visible

---

## Phase 6: Device Deployment (5 minutes)

### Step 1: Configure Signing
- [ ] Connect iPhone 12 via USB
- [ ] In Xcode, select Unity-iPhone target
- [ ] Click "Signing & Capabilities" tab
- [ ] Check "Automatically manage signing"
- [ ] Select your Team from dropdown

**Pass criteria**: "Provisioning profile: Xcode Managed Profile" appears

### Step 2: Build and Deploy
- [ ] Select iPhone 12 from device dropdown (top of Xcode)
- [ ] Click Play button (or Product → Run)
- [ ] Wait for build and install
- [ ] App launches on iPhone 12

**Build success criteria**:
- Xcode shows "Build Succeeded"
- App installs on device
- App icon appears on home screen

**If build fails**: Check Xcode error logs, note exact error

**Log deployment**:
```
Build succeeded: YES / NO
App installed: YES / NO
Errors: _______________________
```

---

## Phase 7: Device Functional Testing (15-20 minutes)

### Test 1: App Launch
- [ ] App launches without crash
- [ ] Main scene loads
- [ ] UI is visible and properly formatted

**Expected**:
- White background
- Conversation area at top with scrollbar
- Mic button at bottom (white)
- Status text shows "Tap to speak"
- Welcome message visible: "Hello friend! I'm Monke!..."

**Pass criteria**: App launches successfully, UI looks correct

### Test 2: Permissions Request
On FIRST launch only:

- [ ] Tap mic button
- [ ] Permission dialog appears: "MonkePOC Would Like to Access the Microphone"
- [ ] Tap "OK"
- [ ] Another dialog: "MonkePOC Would Like to Access Speech Recognition"
- [ ] Tap "OK"

**Pass criteria**: Both permissions requested

**If permissions don't appear**: Info.plist entries missing, see Manual Fix

### Test 3: Speech Recognition (First Test)
- [ ] Tap mic button
- [ ] Button turns red
- [ ] Status shows: "Listening... (speak naturally)"
- [ ] Speak clearly: "Hello Monke"
- [ ] Wait 2 seconds (don't tap anything)
- [ ] Recording should auto-stop

**Expected behavior**:
- Button returns to white after 2 seconds
- Text appears: "You: Hello Monke"
- Thinking indicator: "Monke: ..."
- Status: "Monke is thinking..."

**Pass criteria**: Voice recorded, transcribed, and displayed

**If fails**:
- Check Xcode Console logs (View → Debug Area → Activate Console)
- Look for "SpeechRecognizer" logs
- Note specific error

### Test 4: LLM Response
Continuing from Test 3:

- [ ] Wait for LLM response (~1-2 seconds)
- [ ] "Monke: ..." disappears
- [ ] Actual response appears (e.g., "Monke: Hi there! *waves*...")
- [ ] Status changes to: "Monke is speaking..."

**Expected response characteristics**:
- 2-3 sentences
- Kid-friendly language
- May include asterisk actions like *waves*
- Response is relevant to "Hello Monke"

**Pass criteria**: LLM response received and displayed

**If fails**:
- Check server is running: `curl http://192.168.0.4:8080/health`
- Check iPhone on same network as Mac
- Check Xcode Console for "ConnectionError"

### Test 5: Text-to-Speech
Continuing from Test 4:

- [ ] Immediately after response displays, listen for voice
- [ ] Samantha voice speaks the response
- [ ] Asterisk actions (like *waves*) are NOT spoken
- [ ] Voice has kid-friendly tone (slightly higher pitch)
- [ ] After speech finishes, status returns to: "Tap to speak"

**Expected voice behavior**:
- Clear, natural speech
- Moderate pace (not too fast)
- Slightly higher pitch than default
- Pauses between sentences

**Pass criteria**: Response is spoken correctly without asterisk text

**If fails**:
- No audio: Check iPhone volume
- Wrong voice: Note which voice is used
- Asterisks spoken: Regex filter not working
- Check Xcode Console for "TTS" logs

### Test 6: Second Interaction (Regression Test)
**This test caught bugs before - critical to verify**

- [ ] After first interaction completes
- [ ] Tap mic button again
- [ ] Button turns red
- [ ] Status: "Listening..."
- [ ] Speak: "Tell me about space"
- [ ] Wait 2 seconds
- [ ] Recording auto-stops

**Expected**:
- Full cycle works again
- "You: Tell me about space" appears
- Monke responds about space
- TTS speaks the response
- Returns to "Tap to speak"

**Pass criteria**: Second interaction works identically to first

**This is critical**: Previously, second interaction failed completely

### Test 7: Multiple Rapid Interactions
- [ ] Have 3-4 exchanges in quick succession
- [ ] Tap mic → speak → wait → response → repeat

**Test questions**:
1. "What is your favorite color?"
2. "Why do birds fly?"
3. "Tell me a fun fact"
4. "What should we explore?"

**Expected behavior**:
- Each interaction completes successfully
- Conversation history grows (scrolls down)
- No crashes or freezes
- Mic always responds to taps

**Pass criteria**: All 4 interactions complete without issues

### Test 8: Conversation Scrolling
After Test 7 (with 5-6 exchanges total):

- [ ] Check conversation area
- [ ] Latest message visible at bottom
- [ ] Older messages scrolled up
- [ ] Can manually scroll up to see older messages
- [ ] New messages auto-scroll to bottom

**Pass criteria**: Scrolling works automatically and manually

### Test 9: App Backgrounding
- [ ] During Monke speech, press Home button
- [ ] App goes to background
- [ ] Wait 5 seconds
- [ ] Tap MonkePOC icon to return
- [ ] App resumes

**Expected**:
- App doesn't crash
- Conversation history preserved
- Can continue interaction

**Pass criteria**: App handles backgrounding gracefully

### Test 10: Network Recovery
- [ ] Stop llama-server on Mac
- [ ] In app, tap mic and say something
- [ ] Wait for error message

**Expected**:
- "Connection error!" message appears
- Error details in text
- App doesn't crash

**Pass criteria**: Graceful error handling

- [ ] Restart llama-server on Mac
- [ ] Tap mic and try again
- [ ] Should work normally

**Pass criteria**: Recovers after server restart

---

## Phase 8: Performance Verification (5 minutes)

### Metrics to Measure
Use stopwatch or Xcode's Time Profiler:

- [ ] **LLM Response Time**:
  - Speak → Wait for "Monke: ..." to change to actual response
  - Expected: ~1-2 seconds
  - Measured: _____ seconds

- [ ] **TTS Latency**:
  - Response appears → Audio starts
  - Expected: < 0.5 seconds
  - Measured: _____ seconds

- [ ] **Recording Auto-Stop**:
  - Finish speaking → Recording stops
  - Expected: ~2 seconds
  - Measured: _____ seconds

- [ ] **App Launch Time**:
  - Tap icon → Welcome message visible
  - Expected: < 3 seconds (after first launch)
  - Measured: _____ seconds

**Pass criteria**: All timings similar to Unity 2022.3 version

---

## Phase 9: Xcode Console Log Review (5 minutes)

### Check Xcode Console
View → Debug Area → Activate Console

**Look for these log sequences**:

### Successful STT sequence:
```
SpeechRecognizer: Starting recording...
SpeechRecognizer: Transcription: [partial results]
Silence timeout - auto-stopping and sending transcription
SpeechRecognizer: Stopped recording
```

### Successful TTS sequence:
```
TTS: Speaking text: [response text without asterisks]
TTS: Using voice: Samantha
TTS: Finished speaking
```

### Successful LLM sequence:
```
=== STARTING LLM REQUEST ===
Sending request to: http://192.168.0.4:8080/completion
Sending web request...
Request completed. Result: Success
```

**Pass criteria**: Logs show expected sequences without errors

**Note any unexpected errors**:
```
_______________________________________
```

---

## Phase 10: Comparison with Unity 2022.3 (Final Check)

### Side-by-Side Comparison
If you still have Unity 2022.3 backup build:

**Feature** | **Unity 2022.3** | **Unity 6** | **Match?**
--- | --- | --- | ---
App launches | ✓ | [ ] | [ ]
Voice input works | ✓ | [ ] | [ ]
Auto-stop (2 sec) | ✓ | [ ] | [ ]
LLM response time | ~1s | ___s | [ ]
Voice output works | ✓ | [ ] | [ ]
Asterisks filtered | ✓ | [ ] | [ ]
Second interaction | ✓ | [ ] | [ ]
Multiple exchanges | ✓ | [ ] | [ ]
Auto-scrolling | ✓ | [ ] | [ ]
No crashes | ✓ | [ ] | [ ]

**Pass criteria**: All features match Unity 2022.3 version

---

## Manual Fix Procedures (If Needed)

### Manual Fix 1: Add Frameworks in Xcode
If post-processor failed to add frameworks:

1. Open Xcode project
2. Select "UnityFramework" target
3. Build Phases → Link Binary With Libraries
4. Click "+" button
5. Search "Speech", add "Speech.framework"
6. Click "+" button
7. Search "AVFoundation", add "AVFoundation.framework"
8. Repeat steps 2-7 for "Unity-iPhone" target
9. Build and run again

### Manual Fix 2: Add Info.plist Entries
If permissions missing:

1. Open Xcode project
2. Select Unity-iPhone target
3. Info tab → Custom iOS Target Properties
4. Hover over any row, click "+" button
5. Add key: "Privacy - Speech Recognition Usage Description"
   Value: "Monke uses speech recognition to understand what you say"
6. Add key: "Privacy - Microphone Usage Description"
   Value: "Monke listens to your voice"
7. Add key: "Privacy - Local Network Usage Description"
   Value: "Monke needs to connect to local server for AI responses"
8. Build and run again

### Manual Fix 3: Enable HTTP
If LLM connection fails:

1. Open Xcode project
2. Select Unity-iPhone target
3. Info tab → Custom iOS Target Properties
4. Add key: "App Transport Security Settings" (Dictionary type)
5. Expand it, add sub-key: "Allow Arbitrary Loads" (Boolean type)
6. Set value: YES
7. Build and run again

---

## Test Results Summary

### Phase 1: Migration
- [ ] PASS - Project upgraded successfully
- [ ] FAIL - Issues: _____________________

### Phase 2: Components
- [ ] PASS - All components verified
- [ ] FAIL - Issues: _____________________

### Phase 3: Editor Testing
- [ ] PASS - Plays in editor
- [ ] FAIL - Issues: _____________________

### Phase 4: Build
- [ ] PASS - Build completed
- [ ] FAIL - Issues: _____________________

### Phase 5: Xcode
- [ ] PASS - All frameworks present
- [ ] FAIL - Issues: _____________________

### Phase 6: Deployment
- [ ] PASS - Deployed to device
- [ ] FAIL - Issues: _____________________

### Phase 7: Functional Testing
- [ ] PASS - All 10 tests passed
- [ ] FAIL - Failed tests: _____________________

### Phase 8: Performance
- [ ] PASS - Performance acceptable
- [ ] FAIL - Issues: _____________________

### Phase 9: Logs
- [ ] PASS - Clean logs
- [ ] FAIL - Issues: _____________________

### Phase 10: Comparison
- [ ] PASS - Matches Unity 2022.3
- [ ] FAIL - Differences: _____________________

---

## Final Decision

### Migration Successful ✅
If all phases PASS:
- [ ] Commit to Git
- [ ] Push to GitHub
- [ ] Update README with Unity 6 requirement
- [ ] Notify partner that project is Unity 6 ready

**Git commands**:
```bash
cd /Users/hirensakaria/Documents/Projects/MonkeUnity/MonkePOC
git add .
git commit -m "Migrate to Unity 6 LTS (6000.0.62f1) - All tests passing"
git push origin main
```

### Migration Failed ❌
If critical issues found:
- [ ] Document specific failures
- [ ] Attempt Manual Fixes (see above)
- [ ] Re-run failed tests
- [ ] If still failing, consider rollback

**Rollback command**:
```bash
cd /Users/hirensakaria/Documents/Projects/MonkeUnity
rm -rf MonkePOC
cp -r MonkePOC_Unity2022_Backup MonkePOC
```

---

## Sign-off

**Tested by**: ___________________
**Date**: ___________________
**Unity 6 Version**: 6000.0.62f1
**Test Device**: iPhone 12
**Result**: PASS / FAIL
**Notes**:
```
_______________________________________
_______________________________________
_______________________________________
```

**Ready for partner collaboration**: YES / NO
