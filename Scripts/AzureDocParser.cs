using System.Text;
using UnityEngine;

public static class AzureDocParser
{
    public static string ParseDocument(string json)
    {
        var sb = new StringBuilder();

        try
        {
            var root = JsonUtility.FromJson<AnalyzeRootWrapper>(WrapForUnityJson(json));

            if (root.analyzeResult != null)
            {
                // Key-Value Pairs
                if (root.analyzeResult.documents != null && root.analyzeResult.documents.Length > 0)
                {
                    sb.AppendLine("--- Key-Value Pairs ---");
                    foreach (var field in root.analyzeResult.documents[0].fields)
                    {
                        sb.AppendLine($"{field.key}: {field.content}");
                    }
                }

                // Tables
                if (root.analyzeResult.tables != null && root.analyzeResult.tables.Length > 0)
                {
                    sb.AppendLine("\n--- Tables ---");
                    foreach (var table in root.analyzeResult.tables)
                    {
                        foreach (var cell in table.cells)
                        {
                            sb.AppendLine($"Cell[{cell.rowIndex},{cell.columnIndex}]: {cell.content}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Form parsing failed: " + e.Message);
        }

        return sb.ToString();
    }

    private static string WrapForUnityJson(string json)
    {
        int index = json.IndexOf("\"analyzeResult\":");
        if (index == -1) return "{}";

        int braceCount = 0;
        int start = json.IndexOf('{', index);
        int end = start;

        for (; end < json.Length; end++)
        {
            if (json[end] == '{') braceCount++;
            if (json[end] == '}') braceCount--;
            if (braceCount == 0) break;
        }

        string body = json.Substring(start, end - start + 1);
        return "{\"analyzeResult\":" + body + "}";
    }

    [System.Serializable]
    private class AnalyzeRootWrapper
    {
        public AnalyzeResult analyzeResult;
    }

    [System.Serializable]
    private class AnalyzeResult
    {
        public Document[] documents;
        public Table[] tables;
    }

    [System.Serializable]
    private class Document
    {
        public Field[] fields;
    }

    [System.Serializable]
    private class Field
    {
        public string key;
        public string content;
    }

    [System.Serializable]
    private class Table
    {
        public TableCell[] cells;
    }

    [System.Serializable]
    private class TableCell
    {
        public string content;
        public int rowIndex;
        public int columnIndex;
    }
}