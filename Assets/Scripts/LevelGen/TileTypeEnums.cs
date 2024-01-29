/// <summary> The Type/ID of each tile, in the order they appear in the tileset. </summary>
/// <remarks> (0 for None, 1-16 for Edge tiles, then 17-40 for Middle tiles). </remarks>
public enum TileType {
	None = 0,

	// First row
	TopLeft_Outer,
	Top1,
	Top2,
	TopRight_Outer,

	// Second row
	Left1,
	BotRight_Inner, // Opening facing bot right
	BotLeft_Inner,	// Opening facing bot left
	Right1,

	// Third row
	Left2,
	TopRight_Inner,	// Opening facing top right
	TopLeft_Inner,	// Opening facing top left
	Right2,

	// Fourth row
	BotLeft_Outer,
	Bot1,
	Bot2,
	BotRight_Outer,

	// Rows 5 to 10
	Middle1_TopL, Middle1_TopR, Middle2_TopL, Middle2_TopR,
	Middle1_BotL, Middle1_BotR, Middle2_BotL, Middle2_BotR,
	Middle3_TopL, Middle3_TopR, Middle4_TopL, Middle4_TopR,
	Middle3_BotL, Middle3_BotR, Middle4_BotL, Middle4_BotR,
	Middle5_TopL, Middle5_TopR, Middle6_TopL, Middle6_TopR,
	Middle5_BotL, Middle5_BotR, Middle6_BotL, Middle6_BotR,
}


/// <summary> The Type/ID of each cliff tile, in the order they appear in the tileset. </summary>
/// <remarks> (0 for None, then 4 for each row). The 2nd and 3rd rows are skipped for 1-step cliffs. </remarks>
public enum CliffType {
	None = 0,

	TopLeft,	Top1,	Top2,	TopRight,   // First row
	Left1,		Mid1,	Mid2,	Right1,     // Second row
	Left2,		Mid3,	Mid4,	Right2,     // Third row
	BotLeft,	Bot1,	Bot2,	BotRight	// Fourth row
}