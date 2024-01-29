public static class TilemapExtensions {
	/// <summary> Turns a 2D array of type 'TileType' into a one dimensional array 'T[]', with index formula `i = x + lengthX * y`. </summary>
	/// <remarks> The output array is directly compatible with RT manipulation functions like <see cref="CSUtils.TextureUtils.SetPixels"/>. </remarks>
	public static float[] Flatten(this TileType[,] array2D, int excludedBorderWidth = 0) {
		var (lengthX, lengthY) = (array2D.GetLength(0), array2D.GetLength(1));
		var flatArray = new float[lengthX * lengthY];
		for (int x = 0; x < lengthX; x++) {
			for (int y = 0; y < lengthY; y++) {
				if (ShouldCrop(excludedBorderWidth, x, y, lengthX, lengthY)) { flatArray[x + y * lengthX] = -1; }
				else { flatArray[x + y * lengthX] = (int) array2D[x, y] - 1; }
			}
		}
		return flatArray;
	}
	/// <summary> Turns a 2D array of type 'CliffType' into a one dimensional array 'T[]', with index formula `i = x + lengthX * y`. </summary>
	/// <remarks> The output array is directly compatible with RT manipulation functions like <see cref="CSUtils.TextureUtils.SetPixels"/>. </remarks>
	public static float[] Flatten(this CliffType[,] array2D, int excludedBorderWidth = 0) {
		var (lengthX, lengthY) = (array2D.GetLength(0), array2D.GetLength(1));
		var flatArray = new float[lengthX * lengthY];
		for (int x = 0; x < lengthX; x++) {
			for (int y = 0; y < lengthY; y++) {
				if (ShouldCrop(excludedBorderWidth, x, y, lengthX, lengthY)) { flatArray[x + y * lengthX] = -1; }
				else { flatArray[x + y * lengthX] = (int) array2D[x, y] - 1; }
			}
		}
		return flatArray;
	}

	/// <summary> Determines whether a position (x, y) falls inside the excluded border area or not. `borderWidth == 0` means that there's no border. </summary>
	/// <remarks> Excluded border area is typically applied to remove unwanted values from an array, like semantic pixels that shouldn't be used. </remarks>
	static bool ShouldCrop(int excludedBorderWidth, int x, int y, int lengthX, int lengthY) {
		if (excludedBorderWidth == 0) { return false; }		// Nothing to crop if border width is 0.
		excludedBorderWidth = excludedBorderWidth / 2 + 1;	// Convert to half resolution, + 1 offset for the corner edges.
		return x < excludedBorderWidth || y < excludedBorderWidth || x >= lengthX - excludedBorderWidth || y >= lengthY - excludedBorderWidth;
	}
}
