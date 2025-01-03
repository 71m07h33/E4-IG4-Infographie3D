using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static UnityEngine.Mesh;
using UnityEditor.AssetImporters;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color colour;
    }

    public struct MapData
    {
        public readonly float[,] heightMap;
        public readonly Color[] colourMap;

        public MapData(float[,] heightMap, Color[] colourMap)
        {
            this.heightMap = heightMap;
            this.colourMap = colourMap;
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }


    public enum DrawMode { NoiseMap, ColourMap, Mesh/*, FalloffMap */};



    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;

    public const int mapChunckSize = 241;
    [Range(0, 6)]
    public int editorPreviewLOD;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool autoUpdate;

    public TerrainType[] regions;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();



    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunckSize, mapChunckSize, seed, noiseScale, octaves, persistance, lacunarity, center+offset, normalizeMode);
        Color[] colourMap = new Color[mapChunckSize * mapChunckSize];

        for (int y = 0; y < mapChunckSize; y++)
        {
            for (int x = 0; x < mapChunckSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                Color finalColor = Color.black;

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight < regions[i].height)
                    {
                        if (i == 0)
                        {
                            finalColor = regions[i].colour;
                        }
                        else
                        {
                            // Interpolation lin�aire entre la couleur actuelle et la pr�c�dente
                            float t = Mathf.InverseLerp(regions[i - 1].height, regions[i].height, currentHeight);
                            finalColor = Color.Lerp(regions[i - 1].colour, regions[i].colour, t);
                        }
                        break;
                    }
                }

                colourMap[y * mapChunckSize + x] = finalColor;
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;

        if (octaves < 0)
            octaves = 0;

    }

    public void DrawMapInEditor()
    {
        MapData mapData= GenerateMapData(Vector2.zero); 
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap ));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunckSize, mapChunckSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunckSize, mapChunckSize));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod ,callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
}