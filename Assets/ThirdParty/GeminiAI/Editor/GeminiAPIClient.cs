#if UNITY_EDITOR
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeminiScriptGenerator;
using UnityEngine;

public static class GeminiAPIClient
{
    public static async Task<string> SendPromptToGeminiAPI(string prompt, GeminiAPISettings apiSettings)
    {
        if (string.IsNullOrEmpty(apiSettings.ApiKey))
        {
            Debug.LogError("Gemini API key is not set in the settings.");
            UnityEditor.EditorUtility.DisplayDialog("API Key Missing", 
                "Please set your Gemini API key in the settings asset.", "OK");
            return null;
        }
        
        string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";
        string fullUrl = $"{apiUrl}{apiSettings.ApiKey}";
        
        // Create the request object
        GeminiRequest requestData = new GeminiRequest(prompt);
        
        // Convert to JSON
        string jsonRequest = JsonUtility.ToJson(requestData);
        
        using (HttpClient client = new HttpClient())
        {
            HttpContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await client.PostAsync(fullUrl, content);
            string jsonResponse = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // Parse the response to extract the generated text
                    GeminiResponse responseObj = JsonUtility.FromJson<GeminiResponse>(jsonResponse);
                    
                    if (responseObj != null && responseObj.candidates != null && 
                        responseObj.candidates.Count > 0 && 
                        responseObj.candidates[0].content != null &&
                        responseObj.candidates[0].content.parts != null &&
                        responseObj.candidates[0].content.parts.Count > 0)
                    {
                        string generatedText = responseObj.candidates[0].content.parts[0].text;
                        Debug.Log(generatedText);
                        
                        // Extract just the code if it's wrapped in code blocks
                        if (generatedText.Contains("```csharp") || generatedText.Contains("```cs"))
                        {
                            Regex codeBlockRegex = new Regex(@"```(?:csharp|cs)\s*([\s\S]*?)\s*```");
                            Match match = codeBlockRegex.Match(generatedText);
                            
                            if (match.Success)
                            {
                                return match.Groups[1].Value.Trim();
                            }
                        }
                        
                        return generatedText;
                    }
                    else
                    {
                        // If JsonUtility parsing failed or the structure is different than expected
                        Debug.LogError("Failed to parse Gemini API response. Response structure may have changed.");
                        Debug.LogError("Raw response: " + jsonResponse);
                        
                        // Fallback to simple string extraction if the response format is different
                        return ExtractTextFromRawResponse(jsonResponse);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error parsing Gemini API response: {ex.Message}\nResponse: {jsonResponse}");
                    
                    // Try the fallback extraction method
                    return ExtractTextFromRawResponse(jsonResponse);
                }
            }
            else
            {
                Debug.LogError($"Gemini API request failed with status code {response.StatusCode}: {jsonResponse}");
                return null;
            }
        }
    }

    // Fallback method for cases where JsonUtility might struggle with the response format
    private static string ExtractTextFromRawResponse(string jsonResponse)
    {
        try
        {
            // Simple string-based extraction for the most common response patterns
            const string textMarker = "\"text\": \"";
            int textStart = jsonResponse.IndexOf(textMarker);
            
            if (textStart >= 0)
            {
                textStart += textMarker.Length;
                int textEnd = jsonResponse.IndexOf("\"", textStart);
                
                if (textEnd > textStart)
                {
                    string extractedText = jsonResponse.Substring(textStart, textEnd - textStart)
                        .Replace("\\n", "\n")
                        .Replace("\\\"", "\"")
                        .Replace("\\\\", "\\");
                
                    // Check for code blocks and extract them
                    if (extractedText.Contains("```csharp") || extractedText.Contains("```cs"))
                    {
                        Regex codeBlockRegex = new Regex(@"```(?:csharp|cs)\s*([\s\S]*?)\s*```");
                        Match match = codeBlockRegex.Match(extractedText);
                        
                        if (match.Success)
                        {
                            return match.Groups[1].Value.Trim();
                        }
                    }
                
                    return extractedText;
                }
            }
        
            // If we couldn't extract text properly, return a meaningful error message
            return "// Error: Unable to extract code from API response.\n// Please check Unity Console for details.";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in fallback text extraction: {ex.Message}");
            return null;
        }
    }
}
#endif