using System.Text;
using UnityEngine;

public static class AzureDocParser
{
    public static string ParseDocument(string json)
    {
        var sb = new StringBuilder();

        try
        {
            var root = JsonUtility.FromJson<LayoutWrapper>(WrapForUnityJson(json));

            if (root.analyzeResult?.pages != null)
            {
                for (int i = 0; i < root.analyzeResult.pages.Length; i++)
                {
                    var page = root.analyzeResult.pages[i];
                    sb.AppendLine($"--- Page {i + 1} ---");

                    // Lines
                    if (page.lines != null)
                    {
                        sb.AppendLine("Lines:");
                        foreach (var line in page.lines)
                        {
                            sb.AppendLine($"• {line.content}");
                        }
                    }

                    // Tables
                    if (page.tables != null)
                    {
                        sb.AppendLine("\nTables:");
                        foreach (var table in page.tables)
                        {
                            foreach (var cell in table.cells)
                            {
                                sb.AppendLine($"Cell[{cell.rowIndex},{cell.columnIndex}]: {cell.content}");
                            }
                        }
                    }

                    sb.AppendLine();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Layout parsing failed: " + e.Message);
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
    private class LayoutWrapper
    {
        public LayoutResult analyzeResult;
    }

    [System.Serializable]
    private class LayoutResult
    {
        public Page[] pages;
    }

    [System.Serializable]
    private class Page
    {
        public Line[] lines;
        public Table[] tables;
    }

    [System.Serializable]
    private class Line
    {
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
