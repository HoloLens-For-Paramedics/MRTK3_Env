using UnityEngine;

public static class AzureOCRParser
{
    public static string ExtractTextBlock(string json)
    {
        string output = "";

        try
        {
            var root = JsonUtility.FromJson<AnalyzeRootWrapper>("{\"analyzeResult\":" + ExtractJsonSection(json, "\"analyzeResult\":") + "}");

            foreach (var page in root.analyzeResult.readResults)
            {
                foreach (var line in page.lines)
                {
                    output += line.text + "\n";
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse OCR JSON: " + ex.Message);
        }

        return output.Trim();
    }

    private static string ExtractJsonSection(string json, string startKey)
    {
        int startIndex = json.IndexOf(startKey);
        if (startIndex == -1) return "{}";

        int braceCount = 0;
        int i = json.IndexOf('{', startIndex);
        for (; i < json.Length; i++)
        {
            if (json[i] == '{') braceCount++;
            else if (json[i] == '}') braceCount--;

            if (braceCount == 0)
                return json.Substring(json.IndexOf('{', startIndex), i - json.IndexOf('{', startIndex) + 1);
        }

        return "{}";
    }

    [System.Serializable]
    private class AnalyzeRootWrapper
    {
        public AnalyzeResult analyzeResult;
    }

    [System.Serializable]
    private class AnalyzeResult
    {
        public ReadResult[] readResults;
    }

    [System.Serializable]
    private class ReadResult
    {
        public Line[] lines;
    }

    [System.Serializable]
    private class Line
    {
        public string text;
    }
}
