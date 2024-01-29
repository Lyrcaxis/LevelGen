using UnityEngine;

public static partial class CSUtils {
	public static class TextureUtils {
		static readonly ComputeShader cs1 = Resources.Load<ComputeShader>("TextureUtils_SingleChannel");
		static readonly ComputeShader cs4 = Resources.Load<ComputeShader>("TextureUtils_FourChannel");

		/// <summary> Creates a full copy of the input texture, including the unclamped floating-point values. </summary>
		/// <remarks> We need it instead of `Graphics.Blit` when we care for more than the color data (e.g.: values outside the [0-1] range). </remarks>
		public static RenderTexture Copy(RenderTexture input, RenderTexture target = null) {
			if (target == null) { target = CreateRT(input.width, input.height, input.format); }
			if (input.format != target.format) { Debug.LogWarning("Texture format mismatch. Only X component will be copied."); }
			var cs = target.format == RenderTextureFormat.RHalf ? cs1 : cs4;
			cs.SetTexture(0, "input", input);
			cs.SetTexture(0, "output", target);
			return cs.Dispatch(0, target);
		}
		/// <summary> Turns the values of a texture to either 0 or 1, losing the numerical info (`val=0` if pixel is 0, otherwise `val=1`). </summary>
		/// <remarks> This is useful when we have a texture whose values represent more info than just semantics, and want to turn it into a mask. </remarks>
		public static RenderTexture MakeBinaryMask(RenderTexture target) {
			var cs = target.format == RenderTextureFormat.RHalf ? cs1 : cs4;
			var tempRT = Copy(target);
			cs.SetTexture(1, "input", tempRT);
			cs.SetTexture(1, "output", target);
			return cs.Dispatch(1, target, tempRT);
		}
		/// <summary> Creates a noise binary mask. Internally creates random [0-1] floats, and values above the threshold pass as 1, others as 0. </summary>
		/// <remarks> NOTE: Bigger threshold means MORE 0s and LESS 1s, so adjust accordingly. </remarks>
		public static RenderTexture GetNoiseMap(int sizeX, int sizeY, float threshold = 0.5f, bool singleChannel = true, RenderTexture target = null) {
			if (target == null) { target = CreateRT(sizeX, sizeY, singleChannel ? RenderTextureFormat.RHalf : RenderTextureFormat.ARGBHalf); }
			var cs = singleChannel ? cs1 : cs4;
			cs.SetFloat("noiseThreshold", threshold);
			cs.SetTexture(2, "output", target);
			return cs.Dispatch(2, target);
		}
		/// <summary> Retrieves the pixels of a RenderTexture from the GPU. Currently supports <see cref="float"/> and <see cref="Vector4"/> retrievals. </summary>
		/// <remarks> NOTE: Retrieving data from the GPU is relatively slow, so use this only when it's absolutely necessary!! </remarks>
		public static T[] GetPixels<T>(RenderTexture input) {
			bool singleChannel = typeof(T) == typeof(float);
			if (!singleChannel && typeof(T) != typeof(Vector4)) { throw new("Only float and Vector4 retrievals are currently supported."); }
			var size = input.width * input.height;
			var cb = new ComputeBuffer(size, (singleChannel ? 1 : 4) * sizeof(float));
			var cs = singleChannel ? cs1 : cs4;
			System.Array data = (singleChannel ? new float[size] : new Vector4[size]);
			cs.SetTexture(3, "input", input);
			cs.SetBuffer(3, "buffer", cb);
			cb.SetData(data);
			cs.Dispatch(3, input);
			cb.GetData(data);
			cb.Release();

			return data as T[];
		}
		/// <summary> Sets the values of a RenderTexture's Pixels from the CPU. Currently supports <see cref="float"/> and <see cref="Vector4"/> values. </summary>
		/// <remarks> NOTE: This is not slow, but pixels must be laid out in flat 2D whose index formula is `i = x + lengthX * y`!! </remarks>
		public static RenderTexture SetPixels<T>(T[] input, RenderTexture target) {
			bool singleChannel = typeof(T) == typeof(float);
			if (!singleChannel && typeof(T) != typeof(Vector4)) { throw new("Only float and Vector4 values are currently supported."); }
			var cb = new ComputeBuffer(input.Length, (singleChannel ? 1 : 4) * sizeof(float));
			var cs = singleChannel ? cs1 : cs4;
			cs.SetTexture(4, "output", target);
			cs.SetBuffer(4, "buffer", cb);
			cb.SetData(input);
			cs.Dispatch(4, target);
			cb.Release();
			return target;
		}

		/// <summary> Doubles the resolution of a given texture, by copying each pixel 4 times in the [(0,0)-(1,1)] square. </summary>
		/// <remarks> This allows us to work with RTs of different sizes for different purposes (e.g.: level gen vs tile 'dressing'). </remarks>
		public static RenderTexture DoubleResolution(RenderTexture input, RenderTexture target = null) {
			var cs = input.format == RenderTextureFormat.RHalf ? cs1 : cs4;
			if (target == null) { target = CreateRT(2 * input.width, 2 * input.height, input.format); }
			if (input.format != target.format) { Debug.LogWarning("Texture format mismatch. Only X component will be copied."); }
			cs.SetTexture(5, "input", input);
			cs.SetTexture(5, "output", target);
			cs.Dispatch(5, input);
			return target;
		}

		/// <summary> Returns the amount of pixels in the 'input' that have a non-0 value. </summary>
		/// <remarks> This can be useful when we want to compare coverage percentages and various conditions. </remarks>
		public static int GetActivePixelsCount(RenderTexture input) {
			var cs = input.format == RenderTextureFormat.RHalf ? cs1 : cs4;
			var cbCounter = new ComputeBuffer(1, sizeof(int));
			var validPositionCount = new int[1];
			cbCounter.SetData(validPositionCount);
			cs.SetBuffer(6, "countBuffer", cbCounter);
			cs.SetTexture(6, "input", input);
			cs.Dispatch(6, input);
			cbCounter.GetData(validPositionCount);
			cbCounter.Release();
			return validPositionCount[0];
		}

		/// <summary> Returns an array containing the **positions** of all pixels in the 'input' that have a non-0 value. </summary>
		/// <remarks> This is an efficient way of retrieving only the positions we care about, instead of all of them, which would be very slow when dealing with large maps. </remarks>
		public static Vector2[] GetActivePositions(RenderTexture input) {
			var cs = input.format == RenderTextureFormat.RHalf ? cs1 : cs4;
			var cbCounter = new ComputeBuffer(1, sizeof(int));
			var validPositionCount = new int[1];
			cbCounter.SetData(validPositionCount);
			cs.SetBuffer(6, "countBuffer", cbCounter);
			cs.SetTexture(6, "input", input);
			cs.Dispatch(6, input);

			// Retrieve the count of the valid positions and return an empty array if not any were found.
			cbCounter.GetData(validPositionCount);
			if (validPositionCount[0] == 0) { cbCounter.Release(); return System.Array.Empty<Vector2>(); }

			// Retrieve the position on that 'valid index'.
			var cbPos = new ComputeBuffer(validPositionCount[0], 2 * sizeof(float));
			var positions = new Vector2[validPositionCount[0]];
			cbPos.SetData(positions);
			cs.SetBuffer(7, "countBuffer", cbCounter);
			cs.SetBuffer(7, "posBuffer", cbPos);
			cs.SetTexture(7, "input", input);
			cs.Dispatch(7, input);
			cbPos.GetData(positions);
			cbPos.Release();
			cbCounter.Release();

			return positions;
		}

		/// <summary> Returns a random position of a pixel that's not 0 from the 'input' texture. </summary>
		public static Vector2Int GetRandomPosition(RenderTexture input) {
			var cs = input.format == RenderTextureFormat.RHalf ? cs1 : cs4;
			var cbCounter = new ComputeBuffer(1, sizeof(int));
			var validPositionCount = new int[1];
			cbCounter.SetData(validPositionCount);
			cs.SetBuffer(6, "countBuffer", cbCounter);
			cs.SetTexture(6, "input", input);
			cs.Dispatch(6, input);

			// Retrieve the count of the valid positions and return invalid index if not any were found.
			cbCounter.GetData(validPositionCount);
			if (validPositionCount[0] == 0) { cbCounter.Release(); return Vector2Int.one * -1; }

			// Get a random index whose position we want to retrieve and update the buffer.
			validPositionCount[0] = Random.Range(0, validPositionCount[0]);
			cbCounter.SetData(validPositionCount);

			// Retrieve the position on that 'valid index'.
			var cb = new ComputeBuffer(1, 2 * sizeof(float));
			var data = new Vector2[1];
			cb.SetData(data);
			cs.SetTexture(8, "input", input);
			cs.SetBuffer(8, "countBuffer", cbCounter);
			cs.SetBuffer(8, "posBuffer", cb);
			cs.Dispatch(8, input);
			cb.GetData(data);
			cb.Release();
			cbCounter.Release();

			return Vector2Int.CeilToInt(new(data[0].x, data[0].y));
		}
	}
}