using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;

public class TextToSpeech : MonoBehaviour
{
    // Callback delegate
    public delegate void OnSpeechComplete();
    public event OnSpeechComplete OnSpeechFinished;

    private bool isSpeaking = false;

#if UNITY_IOS && !UNITY_EDITOR
    // Native iOS function imports
    [DllImport("__Internal")]
    private static extern void _InitTextToSpeech();

    [DllImport("__Internal")]
    private static extern void _Speak(string text, TTSCompletionCallback callback);

    [DllImport("__Internal")]
    private static extern void _StopSpeaking();

    [DllImport("__Internal")]
    private static extern bool _IsSpeaking();

    // Callback delegate that matches native side
    private delegate void TTSCompletionCallback();

    // Static callback function (must be static for native calls)
    [MonoPInvokeCallback(typeof(TTSCompletionCallback))]
    private static void OnTTSComplete()
    {
        // Forward to instance method on main thread
        if (Instance != null)
        {
            Instance.HandleSpeechComplete();
        }
    }

    private static TextToSpeech Instance;
#endif

    void Awake()
    {
#if UNITY_IOS && !UNITY_EDITOR
        Instance = this;
        _InitTextToSpeech();
#endif
    }

    public void Speak(string text)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!string.IsNullOrEmpty(text))
        {
            Debug.Log($"TTS: Starting to speak: {text}");
            isSpeaking = true;
            _Speak(text, OnTTSComplete);
        }
#else
        Debug.Log($"TTS: Would speak (simulated): {text}");
        // Simulate completion after a delay in editor
        isSpeaking = true;
        Invoke(nameof(SimulateComplete), 2f);
#endif
    }

    public void StopSpeaking()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (isSpeaking)
        {
            Debug.Log("TTS: Stopping speech");
            _StopSpeaking();
            isSpeaking = false;
        }
#else
        Debug.Log("TTS: Stopped speaking (simulated)");
        CancelInvoke(nameof(SimulateComplete));
        isSpeaking = false;
#endif
    }

    private void HandleSpeechComplete()
    {
        Debug.Log("TTS: Speech completed");
        isSpeaking = false;
        OnSpeechFinished?.Invoke();
    }

    private void SimulateComplete()
    {
        HandleSpeechComplete();
    }

    public bool IsSpeaking
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _IsSpeaking();
#else
            return isSpeaking;
#endif
        }
    }

    void OnDestroy()
    {
        if (isSpeaking)
        {
            StopSpeaking();
        }
    }
}
