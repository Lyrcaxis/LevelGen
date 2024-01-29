/// <summary> An enum flag containing all possible values for outer edge of rendered shapes. </summary>
/// <remarks> The values are an exact C# copy of the HLSL equivalent in 'EdgeEnumFlags.hlsl'. </remarks>
[System.Flags]
public enum IsleEdgeFlag {
	RIGHT  = (1 << 0), // 1
	LEFT   = (1 << 1), // 2
	TOP    = (1 << 2), // 4
	BOTTOM = (1 << 3), // 8
	RIGHT_LEFT            = (RIGHT | LEFT),                // 3
	TOP_RIGHT             = (TOP | RIGHT),                 // 5
	TOP_LEFT              = (TOP | LEFT),                  // 6
	TOP_RIGHT_LEFT        = (TOP | RIGHT | LEFT),          // 7
	BOTTOM_RIGHT          = (BOTTOM | RIGHT),              // 9
	BOTTOM_LEFT           = (BOTTOM | LEFT),               // 10
	BOTTOM_RIGHT_LEFT     = (BOTTOM | RIGHT | LEFT),       // 11
	TOP_BOTTOM            = (TOP | BOTTOM),                // 12
	TOP_RIGHT_BOTTOM      = (TOP | RIGHT | BOTTOM),        // 13
	TOP_LEFT_BOTTOM       = (TOP | LEFT | BOTTOM),         // 14
	TOP_RIGHT_LEFT_BOTTOM = (TOP | RIGHT | LEFT | BOTTOM)  // 15
}
