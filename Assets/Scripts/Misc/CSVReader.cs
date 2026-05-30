using System.Collections.Generic;

public class CSVReader
{
    private Dictionary<int, List<string>> table;
    public CSVReader(string data, char separator)
    {
        ReadData(data, separator);
    }
    public CSVReader(string data)
    {
        ReadData(data, '\t');
    }
    public int TotalRow => table.Count;
    public string[] GetRowData(int row)
    {
        return table != null && table.ContainsKey(row) ? table[row].ToArray() : default;
    }
    public string[] GetColumnData(int column)
    {
        if (table != null && table[0].Count > column)
        {
            List<string> columnData = new List<string>();
            foreach (KeyValuePair<int, List<string>> rowData in table)
                columnData.Add(rowData.Value[column]);
            return columnData.ToArray();
        }
        return default;
    }
    private void ReadData(string data, char separator)
    {
        string[] rowDatas = data.Split('\n');
        if (rowDatas.Length < 2) return;
        table = new Dictionary<int, List<string>>();
        for (int row = 1; row < rowDatas.Length; row++)
        {
            string[] rowData = rowDatas[row].Trim().Split(separator);
            table.Add(row - 1, new List<string>(rowData));
        }
    }
}