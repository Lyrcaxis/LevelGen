using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ObjectPopulator {
	/// <summary> Creates a dictionary containing valid spawn positions for the given object types, accounting their biome preferences and coverage percentages. </summary>
	/// <remarks> The positions are raw indices in the map and should be translated to world coordinates before actually spawning the objects in the world. </remarks>
	public static Dictionary<SpawnableObjectType, List<Vector2Int>> SpawnObjects(List<SpawnableObjectType> spawnableObjects, Vector2Int size, Dictionary<BiomeType, List<RenderTexture>> biomeDict, Dictionary<RenderTexture, int> orderDict) {
		// First, we'll need to get a list of the biomes the map has, to handle object spawning preferences.
		var biomeTypes = System.Enum.GetValues(typeof(BiomeType)).Cast<BiomeType>();
		var isolatedBiomeMap = biomeTypes.ToDictionary(x => x, x => CSUtils.CreateRT(size.x, size.y));
		foreach (var biomeType in biomeTypes) {
			var isolatedRT = isolatedBiomeMap[biomeType];	// Get ONLY the space belonging exclusively to this biome.
			var orderedRTs = biomeDict[biomeType].OrderBy(x => orderDict[x]); // .. ordered by sorting order, since we want top-most to take precedence.
			foreach (var biomeRT in orderedRTs) {			// This should include the total space where the biome type is rendered on top of everything else.
				CSUtils.MaskUtils.IntersectAdd(biomeRT, isolatedRT); // .. so we go through each added biome, compare its sorting order to other biomes,
				foreach (var nonBiomeRT in biomeTypes.Except(new[] { biomeType }).SelectMany(x => biomeDict[x])) { // .. and only keep the top-most parts.
					if (orderDict[nonBiomeRT] > orderDict[biomeRT]) { CSUtils.MaskUtils.IntersectRemove(nonBiomeRT, isolatedRT); }
				}
			}
		}

		// Now we can go through all objects, check their preferences, and only keep valid spawn points, from which we'll randomly select a few.
		var spawnedObjectRTs = new Dictionary<SpawnableObjectType, RenderTexture>(); // .. and make sure we don't spawn on top of other objects.
		var spawnedObjects = spawnableObjects.ToDictionary(x => x, x => new List<Vector2Int>());
		foreach (var objType in spawnableObjects) {
			var availableSpawnPoints = CSUtils.CreateRT(size.x, size.y); // Valid points include the biomes the object can spawn on.
			foreach (var type in biomeTypes.Where(x => objType.viableBiomes.HasFlag(x))) { CSUtils.MaskUtils.IntersectAdd(isolatedBiomeMap[type], availableSpawnPoints); }
			CSUtils.MaskUtils.IntersectRemove(isolatedBiomeMap[BiomeType.None], availableSpawnPoints); // Remove cliffs and unspawnable area.
			foreach (var spawnMapRT in spawnedObjectRTs.Values) { CSUtils.MaskUtils.IntersectRemove(spawnMapRT, availableSpawnPoints); }
			if (objType.size > 1) { CSUtils.MaskUtils.IntersectRemove(CSUtils.EdgeUtils.GetOutline(availableSpawnPoints), availableSpawnPoints); }

			// All ready to start spawning! What we'll do is determine the amount of objects we want to spawn, then spawn them on random valid positions.
			var objectsRT = spawnedObjectRTs[objType] = CSUtils.CreateRT(size.x, size.y); // ..all while making sure objects don't spawn on top of each other.
			var availableSpacesCount = CSUtils.TextureUtils.GetActivePixelsCount(availableSpawnPoints);
			var maxObjectsToSpawn = (objType.coverage / 100f) * availableSpacesCount / (objType.size * objType.size);
			var objectsToSpawn = Mathf.RoundToInt(Random.Range(maxObjectsToSpawn / (4f / objType.size), maxObjectsToSpawn));
			for (int i = 0; i < objectsToSpawn; i++) { // .. then go ahead and spawn the amount of objects for this object type:
				var randomSpawnPos = CSUtils.TextureUtils.GetRandomPosition(availableSpawnPoints);	// Get a random position from all the available points.
				CSUtils.RenderUtils.RenderRectangle(objectsRT, randomSpawnPos, Vector2.one, false);	// Add that to the spawned points to prevent double spawning.
				CSUtils.MaskUtils.IntersectRemove(objectsRT, availableSpawnPoints);					// Update the available spawn points to exclude that position.
				spawnedObjects[objType].Add(randomSpawnPos);										// .. and finally add it to the list of positions for that object.
			}
		}
		CSUtils.ReleaseAll(isolatedBiomeMap.Values.Concat(spawnedObjectRTs.Values).ToArray()); // Clean up the temp RTs that were created previously.
		return spawnedObjects; // Once all objects have been spawned, return the dictionary that contains their positions, ready to be used by a spawner.
	}
}