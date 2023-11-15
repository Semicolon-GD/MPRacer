using UnityEngine;

public class TerrainDetector : MonoBehaviour
{
    public string mudLayerName = "Mud";
    public float mudSlowdownFactor = 0.5f;

    [SerializeField] private GameObject _mudVisual;

    public bool IsOnMud { get; private set; }
    
    private void Update()
    {
        TerrainLayer currentLayer = GetCurrentTerrainLayer();
        if (currentLayer != null && currentLayer.name == mudLayerName)
        {
            IsOnMud = true;
            _mudVisual.SetActive(true);
            // Slow the player down.
            // Apply the mudSlowdownFactor to your player movement logic.
        }
        else
        {
            IsOnMud = false;
            _mudVisual.SetActive(false);

        }
    }

    private TerrainLayer GetCurrentTerrainLayer()
    {
        if (Terrain.activeTerrain == null)
            return null;
        Terrain terrain = Terrain.activeTerrain;
        TerrainData terrainData = terrain.terrainData;
        Vector3 position = terrain.transform.InverseTransformPoint(transform.position);
        Vector2Int terrainCoord = GetTerrainCoord(terrainData, position);

        float[,,] alphaMaps = terrainData.GetAlphamaps(terrainCoord.x, terrainCoord.y, 1, 1);

        int mostPrevalentLayerIndex = 0;
        float highestAlpha = 0;
        for (int i = 0; i < terrainData.alphamapLayers; i++)
        {
            float alpha = alphaMaps[0, 0, i];
            if (alpha > highestAlpha)
            {
                highestAlpha = alpha;
                mostPrevalentLayerIndex = i;
            }
        }

        return terrainData.terrainLayers[mostPrevalentLayerIndex];
    }

    private Vector2Int GetTerrainCoord(TerrainData terrainData, Vector3 position)
    {
        float xNormalized = (position.x - terrainData.bounds.min.x) / terrainData.bounds.size.x;
        float zNormalized = (position.z - terrainData.bounds.min.z) / terrainData.bounds.size.z;
        int x = Mathf.FloorToInt(xNormalized * terrainData.alphamapWidth);
        int z = Mathf.FloorToInt(zNormalized * terrainData.alphamapHeight);
        return new Vector2Int(x, z);
    }
}