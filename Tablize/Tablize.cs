using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tablize;

public record Table
{
	public string Name { get; set; } = string.Empty;
	public bool ShowTableName { get; set; }
	public bool ShowColumnNames { get; set; }
}

public record Column
{
	public string Name { get; set; } = string.Empty;
	public int Width { get; set; }
	public int PaddingLeft { get; set; }
	public int PaddingRight { get; set; }
	public int Padding => PaddingLeft + PaddingRight;
	public Align Align { get; set; }
}

public enum Align
{
	Left,
	Right,
	Center
}

public record TableMetadata
{
	public bool IncludeTitle;
	public bool IncludeColumnHeaders;
	public int TitleWidth;
	public int TotalColumnWidth;
	public int TableWidth;
	public int InnerTableWidth; // not including borders
	public int ExtraPaddingForTitle; // title is longest component, extra padding needed on last column
	public int RowCount;
	public int ColumnCount;

	/// <summary>
	/// <column index, length> 
	/// </summary>
	public Dictionary<int, int> ColumnIndexLengthMap = [];

	public List<TableRowValues> TableRowValues = [];
}

public record TableRowValues(string RowId, List<TableValue> Values)
{
	public int Length => Values.Sum(x => x.Length);
}

public record TableValue(TableValueType Type, int Length)
{
	public TableValueType Type = Type;
	public int Length = Length;
	// set when Type = Literal
	public char? Literal;
	// set when Type = Content
	public int? RowIndex;
	// set when Type = ColumnHeader | Content
	public int? ColumnIndex;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TableValueType
{
	Literal,
	Title,
	ColumnHeader,
	Content
}

public static class Tablize
{
}
