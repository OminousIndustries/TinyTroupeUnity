using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Text.RegularExpressions;
using System.Text;

public class TinyTroupeConversationManager : MonoBehaviour
{
    public string serverUrl = "http://192.168.50.125:8000/stream_conversation";
    public string initialPrompt = "Test prompt";
    public int conversationSteps = 4;
    public TextMeshProUGUI dialogueText;

    [Header("TTS Settings")]
    public bool enableTTS = true;
    public SpeechManager speechManager;

    [Header("Message Type Display Settings")]
    public bool showConversationMessages = true;
    public bool showTalkMessages = true;
    public bool showReachOutMessages = true;
    public bool showThoughtMessages = false;

    private HashSet<string> processedMessages = new HashSet<string>();
    private StringBuilder conversationBuilder = new StringBuilder();
    private string previousChunk = "";
    private HashSet<string> knownSpeakers = new HashSet<string>();

    void Start()
    {
        if (enableTTS && speechManager == null)
        {
            Debug.LogWarning("Speech Manager not assigned. TTS will be disabled.");
            enableTTS = false;
        }
        StartCoroutine(StreamConversation());
    }

    private IEnumerator StreamConversation()
    {
        processedMessages.Clear();
        conversationBuilder.Clear();
        previousChunk = "";
        knownSpeakers.Clear();

        string jsonPayload = JsonUtility.ToJson(new ConversationRequest
        {
            prompt = initialPrompt,
            steps = conversationSteps
        });

        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        request.SendWebRequest();

        while (!request.isDone)
        {
            if (request.downloadHandler.data != null && request.downloadHandler.data.Length > 0)
            {
                string newData = request.downloadHandler.text;
                ProcessStreamData(newData);
            }
            yield return new WaitForSeconds(0.1f);
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Request failed: {request.error}");
        }
    }

    private void ProcessStreamData(string data)
    {
        string fullData = previousChunk + data;
        string[] messages = fullData.Split(new[] { "data: " }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < messages.Length - 1; i++)
        {
            ProcessSingleMessage(messages[i]);
        }

        previousChunk = messages.Length > 0 ? messages[messages.Length - 1] : "";

        if (previousChunk.EndsWith("\n"))
        {
            ProcessSingleMessage(previousChunk);
            previousChunk = "";
        }
    }

    private bool ShouldDisplayMessageType(string message)
    {
        if (message.Contains("[CONVERSATION]")) return showConversationMessages;
        if (message.Contains("[TALK]")) return showTalkMessages;
        if (message.Contains("[REACH_OUT]")) return showReachOutMessages;
        if (message.Contains("[THOUGHT]")) return showThoughtMessages;
        return false;
    }

    private void ProcessSingleMessage(string jsonMessage)
    {
        try
        {
            jsonMessage = jsonMessage.Trim();
            if (string.IsNullOrEmpty(jsonMessage)) return;

            StreamMessage message = JsonUtility.FromJson<StreamMessage>(jsonMessage);
            if (message == null || string.IsNullOrEmpty(message.message)) return;

            if (!ShouldDisplayMessageType(message.message)) return;

            string messageKey = message.message.Trim();
            if (processedMessages.Contains(messageKey)) return;

            processedMessages.Add(messageKey);
            var (speaker, dialogue, messageType) = ParseDialogue(message.message);

            if (!string.IsNullOrEmpty(dialogue) && !string.IsNullOrEmpty(speaker))
            {
                knownSpeakers.Add(speaker);
                string formattedMessage = $"{speaker}: [{messageType}] {dialogue}";
                conversationBuilder.AppendLine(formattedMessage);
                UpdateDialogueUI();

                // Handle TTS through SpeechManager
                if (enableTTS && messageType == "CONVERSATION" && speechManager != null)
                {
                    speechManager.HandleSpeech(dialogue, speaker);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Error processing message chunk: {ex.Message}");
        }
    }

    public void ToggleTTS(bool enable)
    {
        enableTTS = enable;
        if (!enable && speechManager != null)
        {
            speechManager.StopAllSpeech();
        }
    }

    private string CleanupMessage(string message)
    {
        message = Regex.Replace(message, @"\[(?:bold|italic|dim|underline|\/)?(?:\s+)?(?:cyan1|green3)?\]", "");
        message = Regex.Replace(message, @"\[(?!CONVERSATION|TALK|REACH_OUT|THOUGHT)[^\]]*\]", "");
        message = Regex.Replace(message, @"\s*>\s*", " ");
        message = Regex.Replace(message, @"\s+", " ");
        return message.Trim();
    }

    private (string speaker, string content, string messageType) ParseDialogue(string message)
    {
        try
        {
            message = CleanupMessage(message);

            string messageType = "";
            Match typeMatch = Regex.Match(message, @"\[(CONVERSATION|TALK|REACH_OUT|THOUGHT)\]");
            if (typeMatch.Success)
            {
                messageType = typeMatch.Groups[1].Value;
            }

            Match arrowMatch = Regex.Match(message, @"([^-]+)(?:-->|--)\s*([^:]+):");
            if (arrowMatch.Success)
            {
                string speaker = arrowMatch.Groups[1].Value.Trim();
                string content = message.Substring(message.IndexOf(':') + 1).Trim();
                return (speaker, content, messageType);
            }

            Match speakerMatch = Regex.Match(message, @"(\w+):");
            if (speakerMatch.Success)
            {
                string speaker = speakerMatch.Groups[1].Value.Trim();
                string content = message.Substring(message.IndexOf(':') + 1).Trim();
                return (speaker, content, messageType);
            }

            foreach (string knownSpeaker in knownSpeakers)
            {
                if (message.Contains(knownSpeaker))
                {
                    return (knownSpeaker, message, messageType);
                }
            }

            Match nameMatch = Regex.Match(message, @"\b[A-Z][a-z]+\b");
            if (nameMatch.Success)
            {
                string potentialSpeaker = nameMatch.Value;
                return (potentialSpeaker, message, messageType);
            }

            return ("Unknown", message, messageType);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Error parsing dialogue: {ex.Message}");
            return ("Unknown", message, "");
        }
    }

    private void UpdateDialogueUI()
    {
        if (dialogueText != null)
        {
            string formattedText = conversationBuilder.ToString()
                .Replace("\n", "\n\n");
            dialogueText.text = formattedText;
        }
    }

    public void ToggleMessageType(string messageType, bool show)
    {
        switch (messageType.ToUpper())
        {
            case "CONVERSATION":
                showConversationMessages = show;
                break;
            case "TALK":
                showTalkMessages = show;
                break;
            case "REACH_OUT":
                showReachOutMessages = show;
                break;
            case "THOUGHT":
                showThoughtMessages = show;
                break;
        }
    }

    [System.Serializable]
    private class ConversationRequest
    {
        public string prompt;
        public int steps;
    }

    [System.Serializable]
    private class StreamMessage
    {
        public string message;
    }
}