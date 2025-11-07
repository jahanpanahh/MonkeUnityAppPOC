using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ConversationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button micButton;
    [SerializeField] private TextMeshProUGUI conversationText;
    [SerializeField] private TextMeshProUGUI micStatusText; // Shows "Listening..." or "Tap to speak"
    [SerializeField] private ScrollRect scrollRect;

    [Header("Components")]
    [SerializeField] private SpeechToText speechToText;
    [SerializeField] private TextToSpeech textToSpeech;

    [Header("LLM Server Settings")]
    [SerializeField] private string serverUrl = "http://192.168.0.4:8080/completion";

    // Get the correct server URL based on platform
    private string GetServerUrl()
    {
#if UNITY_EDITOR
        // In editor, use localhost
        return "http://localhost:8080/completion";
#else
        // On device, use Mac's IP
        return serverUrl;
#endif
    }

    private string conversationHistory = "";
    private bool shouldScrollToBottom = false;
    private bool isWaitingForResponse = false;

    // Called when Unity starts
    void Awake()
    {
        Debug.Log("=== CONVERSATIONMANAGER AWAKE ===");
    }

    void Start()
    {
        // Connect mic button
        if (micButton != null)
        {
            micButton.onClick.AddListener(OnMicButtonClicked);
        }
        else
        {
            Debug.LogError("CRITICAL: micButton is NULL!");
        }

        // Setup speech-to-text
        if (speechToText != null)
        {
            speechToText.OnTextRecognized += OnSpeechRecognized;
            speechToText.RequestPermission();
        }
        else
        {
            Debug.LogError("CRITICAL: SpeechToText component is NULL!");
        }

        // Setup text-to-speech
        if (textToSpeech != null)
        {
            textToSpeech.OnSpeechFinished += OnMonkeSpeechFinished;
        }
        else
        {
            Debug.LogError("CRITICAL: TextToSpeech component is NULL!");
        }

        // Set initial mic status
        UpdateMicStatus("Tap to speak");

        // Show welcome message
        AddMessage("Monke", "Hello friend! I'm Monke! What would you like to talk about?");
    }

    // Called every frame after UI updates
    void LateUpdate()
    {
        // If we flagged to scroll, do it now after UI has updated
        if (shouldScrollToBottom && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            shouldScrollToBottom = false;
        }
    }

    // Called when Mic button is clicked
    void OnMicButtonClicked()
    {
        // Don't allow interaction during LLM response
        if (isWaitingForResponse)
        {
            return;
        }

        // Only allow starting recording (auto-stop when user finishes speaking)
        if (!speechToText.IsRecording)
        {
            // Start recording
            bool success = speechToText.StartRecording();
            if (success)
            {
                UpdateMicStatus("Listening... (speak naturally)");
                // Change button color to indicate recording
                if (micButton != null)
                {
                    var colors = micButton.colors;
                    colors.normalColor = Color.red;
                    micButton.colors = colors;
                }
            }
        }
    }

    // Called when speech is recognized
    void OnSpeechRecognized(string recognizedText)
    {
        Debug.Log($"Speech recognized: {recognizedText}");

        // Check if message is not empty
        if (string.IsNullOrWhiteSpace(recognizedText))
        {
            UpdateMicStatus("Tap to speak");
            return;
        }

        // Reset button color
        if (micButton != null)
        {
            var colors = micButton.colors;
            colors.normalColor = Color.white;
            micButton.colors = colors;
        }

        // Add user's message to conversation
        AddMessage("You", recognizedText);

        // Show thinking indicator
        AddMessage("Monke", "...");

        UpdateMicStatus("Monke is thinking...");

        // Call LLM API asynchronously
        StartCoroutine(GetLLMResponse(recognizedText));
    }

    // Update mic status text
    void UpdateMicStatus(string status)
    {
        if (micStatusText != null)
        {
            micStatusText.text = status;
        }
    }

    // Called when Monke finishes speaking
    void OnMonkeSpeechFinished()
    {
        Debug.Log("Monke finished speaking");
        UpdateMicStatus("Tap to speak");
    }

    // Call the LLM server
    IEnumerator GetLLMResponse(string userMessage)
    {
        Debug.Log("=== STARTING LLM REQUEST ===");
        isWaitingForResponse = true;

        // Build the prompt with Monke's personality
        string prompt = $@"You are Monke, a curious and friendly monkey companion for children aged 7-12. You explore topics together, ask thoughtful questions, and encourage learning through curiosity. You never claim to know everything - you discover things together with the child. Keep responses to 2-3 sentences maximum.

User: {userMessage}
Monke:";

        // Create JSON request
        string jsonData = $@"{{
            ""prompt"": ""{EscapeJsonString(prompt)}"",
            ""n_predict"": 100,
            ""temperature"": 0.7,
            ""stop"": [""User:"", ""\n\n""]
        }}";

        string serverUrl = GetServerUrl();
        Debug.Log($"Sending request to: {serverUrl}");
        Debug.Log($"Request body length: {jsonData.Length} chars");

        // Send request to server - use POST helper method
        UnityWebRequest request = UnityWebRequest.Post(serverUrl, jsonData, "application/json");
        request.timeout = 30; // 30 second timeout

        Debug.Log("Sending web request...");
        yield return request.SendWebRequest();
        Debug.Log($"Request completed. Result: {request.result}");

        // Remove thinking indicator
        RemoveLastMessage();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse response
            string responseText = request.downloadHandler.text;
            LLMResponse response = JsonUtility.FromJson<LLMResponse>(responseText);

            if (response != null && !string.IsNullOrEmpty(response.content))
            {
                // Clean up the response
                string monkeResponse = response.content.Trim();

                // Add Monke's response to conversation (with asterisks)
                AddMessage("Monke", monkeResponse);

                // Remove asterisk actions for speech (e.g., *jumps excitedly*)
                string speechText = System.Text.RegularExpressions.Regex.Replace(monkeResponse, @"\*[^*]+\*", "").Trim();

                // Speak Monke's response (without asterisks)
                UpdateMicStatus("Monke is speaking...");
                if (textToSpeech != null && !string.IsNullOrEmpty(speechText))
                {
                    textToSpeech.Speak(speechText);
                }
                else
                {
                    // If no TTS, just reset status immediately
                    UpdateMicStatus("Tap to speak");
                }
            }
            else
            {
                // Fallback
                string fallbackMsg = "Hmm, I'm having trouble thinking right now. Can you try again?";
                AddMessage("Monke", fallbackMsg);
                if (textToSpeech != null)
                {
                    textToSpeech.Speak(fallbackMsg);
                }
            }
        }
        else
        {
            // Error handling with detailed info
            string errorMsg = $"Connection failed!\nURL: {GetServerUrl()}\nError: {request.error}\nResponse Code: {request.responseCode}";
            Debug.LogError($"LLM Error: {errorMsg}");
            AddMessage("Monke", $"Connection error! Check Unity console. Trying to reach: {GetServerUrl()}");
        }

        isWaitingForResponse = false;
        UpdateMicStatus("Tap to speak");
    }

    // Add a message to the conversation display
    void AddMessage(string sender, string message)
    {
        // Format: "Sender: message\n\n"
        conversationHistory += $"<b>{sender}:</b> {message}\n\n";

        // Update the text display
        conversationText.text = conversationHistory;

        // Flag to scroll on next frame (after UI updates)
        shouldScrollToBottom = true;
    }

    // Remove the last message (for removing "..." indicator)
    void RemoveLastMessage()
    {
        int lastIndex = conversationHistory.LastIndexOf("<b>Monke:</b>");
        if (lastIndex >= 0)
        {
            conversationHistory = conversationHistory.Substring(0, lastIndex);
            conversationText.text = conversationHistory;
        }
    }

    // Escape special characters for JSON
    string EscapeJsonString(string str)
    {
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

// Response structure from LLM server
[System.Serializable]
public class LLMResponse
{
    public string content;
    public bool stop;
}

// Certificate handler to accept all certificates (for local development)
public class AcceptAllCertificatesHandler : UnityEngine.Networking.CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // Accept all certificates for local development
    }
}
