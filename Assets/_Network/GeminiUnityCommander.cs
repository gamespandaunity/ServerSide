using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System;
using TMPro; // Requires TextMeshPro package (Window -> TextMeshPro -> Import TMP Essential Resources)

public class GeminiUnityCommander : MonoBehaviour
{
    [Header("Gemini API Settings")]
    [Tooltip("Your Google Cloud Gemini API Key. Get it from Google Cloud Console.")]
    [SerializeField] private string geminiApiKey;
    [Tooltip("The base URL for the Gemini API. Do not change unless you know what you're doing.")]
    [SerializeField] private string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=";

    [Header("UI References")]
    [Tooltip("The InputField where the user types commands.")]
    [SerializeField] private TMP_InputField inputField;
    [Tooltip("The Text element to display responses and errors.")]
    [SerializeField] private TMP_Text responseText;

    // --- Internal Data Structures for Gemini API communication ---
    // These match the expected JSON structure for Gemini requests and responses.

    [Serializable]
    public class GeminiRequest
    {
        public Content[] contents;
    }

    [Serializable]
    public class Content
    {
        public Part[] parts;
    }

    [Serializable]
    public class Part
    {
        public string text;
    }

    [Serializable]
    public class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [Serializable]
    public class Candidate
    {
        public Content content;
    }

    // --- Internal Data Structure for Unity Command Interpretation ---
    // This defines the expected JSON format that Gemini should return for Unity actions.
    [Serializable]
    public class UnityCommand
    {
        public string action; // e.g., "move", "color", "create", "log"
        public string target; // e.g., "Cube", "Sphere", "new"
        public string value;  // e.g., "0,5,0" (for move), "Cube" (for create), "Hello World" (for log)
        public string color;  // e.g., "red", "blue" (for color)
        public float scale;   // e.g., 2.0 (for scale)
    }

    /// <summary>
    /// Call this method from a UI Button to send the command to Gemini.
    /// </summary>
    public async void SendCommandToGemini()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            responseText.text = "Please enter a command.";
            return;
        }
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            responseText.text = "Error: Gemini API Key is not set in the Inspector.";
            Debug.LogError("Gemini API Key is not set.");
            return;
        }

        string userPrompt = inputField.text;
        responseText.text = "Sending command to Gemini...";
        inputField.interactable = false; // Disable input while processing

        // The system prompt is crucial for guiding Gemini to return a structured JSON.
        // It defines the schema for the UnityCommand class.
        string systemInstruction = "You are an AI assistant for Unity. Interpret the following natural language command and respond ONLY with a JSON object that strictly adheres to this schema: " +
                                   "{\"action\": \"string\", \"target\": \"string\", \"value\": \"string\", \"color\": \"string\", \"scale\": \"float\"}. " +
                                   "Possible 'action' values: 'move', 'color', 'create', 'scale', 'log'. " +
                                   "For 'move', 'target' is GameObject name, 'value' is 'x,y,z' translation. " +
                                   "For 'color', 'target' is GameObject name, 'color' is a color name (e.g., 'red', 'blue'). " +
                                   "For 'create', 'target' is 'new', 'value' is primitive type ('Cube', 'Sphere', 'Plane'). " +
                                   "For 'scale', 'target' is GameObject name, 'scale' is a float value. " +
                                   "For 'log', 'target' is 'none', 'value' is the message to display. " +
                                   "If the command is not understood or cannot be performed, use action 'log' with a helpful 'value' message. " +
                                   "Example for 'move the Cube forward by 5': {\"action\": \"move\", \"target\": \"Cube\", \"value\": \"0,0,5\"}. " +
                                   "Example for 'change Sphere color to green': {\"action\": \"color\", \"target\": \"Sphere\", \"color\": \"green\"}. " +
                                   "Example for 'create a new Capsule': {\"action\": \"create\", \"target\": \"new\", \"value\": \"Capsule\"}. " +
                                   "Example for 'make the Plane twice as big': {\"action\": \"scale\", \"target\": \"Plane\", \"scale\": 2.0}. " +
                                   "Example for 'say hello': {\"action\": \"log\", \"target\": \"none\", \"value\": \"Hello!\"}. " +
                                   "Now, interpret this command: ";

        // Construct Gemini API request body
        GeminiRequest requestData = new GeminiRequest
        {
            contents = new Content[]
            {
                new Content
                {
                    parts = new Part[]
                    {
                        new Part { text = systemInstruction + userPrompt }
                    }
                }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(geminiApiUrl + geminiApiKey, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOperation = request.SendWebRequest();

            // Use Task.Yield() to await the UnityWebRequest without blocking the main thread.
            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }

            inputField.interactable = true; // Re-enable input

            if (request.result != UnityWebRequest.Result.Success)
            {
                responseText.text = $"Network Error: {request.error}";
                Debug.LogError($"Gemini API Network Error: {request.error}\nResponse: {request.downloadHandler.text}");
            }
            else
            {
                string responseJson = request.downloadHandler.text;
                Debug.Log($"Gemini Raw Response: {responseJson}");
                try
                {
                    GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(responseJson);
                    if (geminiResponse != null && geminiResponse.candidates != null && geminiResponse.candidates.Length > 0)
                    {
                        string commandJson = geminiResponse.candidates[0].content.parts[0].text;
                        Debug.Log($"Parsed Command JSON from Gemini: {commandJson}");
                        ExecuteUnityCommand(commandJson);
                    }
                    else
                    {
                        responseText.text = "Gemini response was empty or malformed.";
                        Debug.LogError("Gemini response was empty or malformed: " + responseJson);
                    }
                }
                catch (Exception e)
                {
                    responseText.text = $"Failed to parse Gemini response: {e.Message}";
                    Debug.LogError($"Failed to parse Gemini response: {e.Message}\nRaw Response: {responseJson}");
                }
            }
        }
    }

    /// <summary>
    /// Executes a Unity command based on the JSON string received from Gemini.
    /// </summary>
    /// <param name="commandJson">The JSON string representing the command.</param>
    private void ExecuteUnityCommand(string commandJson)
    {
        try
        {
            UnityCommand command = JsonUtility.FromJson<UnityCommand>(commandJson);

            if (command == null || string.IsNullOrEmpty(command.action))
            {
                responseText.text = "Invalid command format from Gemini.";
                Debug.LogError("Invalid command format from Gemini: " + commandJson);
                return;
            }

            GameObject targetObject = null;
            if (!string.IsNullOrEmpty(command.target) && command.target.ToLower() != "new" && command.target.ToLower() != "none")
            {
                targetObject = GameObject.Find(command.target);
                if (targetObject == null)
                {
                    responseText.text = $"Error: GameObject '{command.target}' not found.";
                    Debug.LogError($"GameObject '{command.target}' not found.");
                    // For 'log' action, we don't need a target object, so don't return early.
                    if (command.action.ToLower() != "log") return;
                }
            }

            switch (command.action.ToLower())
            {
                case "move":
                    if (targetObject != null && !string.IsNullOrEmpty(command.value))
                    {
                        string[] coords = command.value.Split(',');
                        if (coords.Length == 3 &&
                            float.TryParse(coords[0].Trim(), out float x) &&
                            float.TryParse(coords[1].Trim(), out float y) &&
                            float.TryParse(coords[2].Trim(), out float z))
                        {
                            targetObject.transform.Translate(new Vector3(x, y, z), Space.Self); // Use Space.Self for relative movement
                            responseText.text = $"Moved {targetObject.name} by ({x},{y},{z}).";
                        }
                        else
                        {
                            responseText.text = $"Invalid move value: {command.value}. Expected 'x,y,z'.";
                        }
                    }
                    else
                    {
                        responseText.text = "Missing target or value for move command.";
                    }
                    break;

                case "color":
                    if (targetObject != null && !string.IsNullOrEmpty(command.color))
                    {
                        Renderer renderer = targetObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            Color newColor;
                            switch (command.color.ToLower())
                            {
                                case "red": newColor = Color.red; break;
                                case "green": newColor = Color.green; break;
                                case "blue": newColor = Color.blue; break;
                                case "yellow": newColor = Color.yellow; break;
                                case "white": newColor = Color.white; break;
                                case "black": newColor = Color.black; break;
                                case "cyan": newColor = Color.cyan; break;
                                case "magenta": newColor = Color.magenta; break;
                                case "gray": newColor = Color.gray; break;
                                default:
                                    responseText.text = $"Unknown color: {command.color}.";
                                    return;
                            }
                            renderer.material.color = newColor;
                            responseText.text = $"Changed {targetObject.name} color to {command.color}.";
                        }
                        else
                        {
                            responseText.text = $"{targetObject.name} has no Renderer component to change color.";
                        }
                    }
                    else
                    {
                        responseText.text = "Missing target or color for color command.";
                    }
                    break;

                case "create":
                    if (command.target.ToLower() == "new" && !string.IsNullOrEmpty(command.value))
                    {
                        GameObject newObj = null;
                        PrimitiveType primitiveType;
                        if (Enum.TryParse(command.value, true, out primitiveType))
                        {
                            newObj = GameObject.CreatePrimitive(primitiveType);
                        }
                        
                        if (newObj != null)
                        {
                            newObj.name = command.value; // Name it after its type
                            newObj.transform.position = Vector3.zero; // Place at origin
                            responseText.text = $"Created a new {command.value}.";
                        }
                        else
                        {
                            responseText.text = $"Cannot create primitive type: {command.value}. Supported: Cube, Sphere, Capsule, Cylinder, Plane, Quad.";
                        }
                    }
                    else
                    {
                        responseText.text = "Missing type for create command. Example: 'create a new Cube'.";
                    }
                    break;

                case "scale":
                    if (targetObject != null && command.scale > 0)
                    {
                        targetObject.transform.localScale = Vector3.one * command.scale;
                        responseText.text = $"Scaled {targetObject.name} to {command.scale} times its original size.";
                    }
                    else
                    {
                        responseText.text = "Missing target or invalid scale value for scale command (must be > 0).";
                    }
                    break;

                case "log":
                    if (!string.IsNullOrEmpty(command.value))
                    {
                        responseText.text = $"Gemini: {command.value}";
                    }
                    else
                    {
                        responseText.text = "Gemini sent an empty log message.";
                    }
                    break;

                default:
                    responseText.text = $"Unknown action from Gemini: {command.action}.";
                    break;
            }
        }
        catch (Exception e)
        {
            responseText.text = $"Error executing command: {e.Message}";
            Debug.LogError($"Error executing command '{commandJson}': {e.Message}\nStack: {e.StackTrace}");
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            SendCommandToGemini(); // Call the method when Enter is pressed
        }
    }
}