using System.Collections.Generic;
using System.Numerics;
using System;

public class WaterDroplet
{
    public static WaterDropletSettings Settings { get; set; }

    private readonly Random random;

    private ErosionRegion position;
    private Vector3 direction;
    private float velocity = 1;
    private float volume = 1;
    private float sediment = 0;

    static WaterDroplet()
    {
        Settings = new WaterDropletSettings
        {
            lifetime = 50,
            inertia = 0.5f,
            gravity = 4.0f,
            evaporation = 0.05f,
            capacity = 2.0f,
            erosion = 0.3f,
            deposition = 0.01f,
            minErosion = 0.01f,
        };
    }

    public WaterDroplet(ErosionRegion region)
    {
        position = region;
        random = new Random((int)(position.Center.X * position.Center.Y));
        direction = Vector3.Normalize(new Vector3((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 2 - 1), 0));
    }

    public void Simulate()
    {
        for (int i = 0; i < Settings.lifetime; i++)
        {
            SimulationStep();
        }
    }

    private void SimulationStep()
    {
        Vector3 gradient = GetGradient(position);
        Vector3 newDirection = Vector3.Normalize(direction * Settings.inertia - gradient * (1f - Settings.inertia));

        ErosionRegion newPosition = FindNextLocation(newDirection);

        float heightDiff = newPosition.Elevation - position.Elevation;
        SimulateHydraulicAction(heightDiff);

        float newVelocity = (float)Math.Sqrt(Math.Max(0, Math.Pow(velocity, 2) - heightDiff * Settings.gravity));
        float newVolume = volume * (1 - Settings.evaporation);

        UpdateValues(newPosition, newDirection, newVelocity, newVolume);
    }

    private Vector3 GetGradient(ErosionRegion region)
    {
        Dictionary<Vector3, float> heightDiffs = new Dictionary<Vector3, float>();
        float totalHeightDiff = 0;

        foreach (ErosionRegion adjacentNode in region.AdjacentRegions)
        {
            float heightDiff = adjacentNode.Elevation - position.Elevation;
            heightDiffs.Add(Vector3.Normalize(adjacentNode.Center - position.Center), heightDiff);
            totalHeightDiff += Math.Abs(heightDiff);
        }

        Vector3 gradient = new Vector3(0, 0, 0);
        foreach (Vector3 direction in heightDiffs.Keys)
        {
            gradient += direction * heightDiffs[direction] / totalHeightDiff;
        }

        if (gradient == Vector3.Zero || totalHeightDiff == 0)
        {
            gradient.X = (float)(random.NextDouble() * 2 - 1);
            gradient.Y = (float)(random.NextDouble() * 2 - 1);
            gradient = Vector3.Normalize(gradient) * Settings.inertia / (1 - Settings.inertia);
            return gradient;
        }

        return Vector3.Normalize(gradient);
    }

    private ErosionRegion FindNextLocation(Vector3 newDirection)
    {
        ErosionRegion newPosition = position;
        float maxCosTheta = float.MinValue;
        foreach (ErosionRegion adjacentRegion in position.AdjacentRegions)
        {
            Vector3 direction = Vector3.Normalize(adjacentRegion.Center - position.Center);
            float cosTheta = Vector3.Dot(direction, newDirection);
            if (cosTheta > maxCosTheta)
            {
                maxCosTheta = cosTheta;
                newPosition = adjacentRegion;
            }
        }
        return newPosition;
    }

    private void SimulateHydraulicAction(float heightDiff)
    {
        float dropSedimentCapacity = Math.Max(-heightDiff * velocity * volume * Settings.capacity, Settings.minErosion);
        if (sediment > dropSedimentCapacity || heightDiff >= 0)
        {
            Deposit(dropSedimentCapacity, heightDiff);
        }
        else
        {
            Erode(dropSedimentCapacity, heightDiff);
        }
    }

    private void Deposit(float dropSedimentCapacity, float heightDiff)
    {
        float deposit = (heightDiff > 0) ? Math.Min(heightDiff, sediment) : (sediment - dropSedimentCapacity) * Settings.deposition;
        sediment -= deposit;
        position.Elevation += deposit;
    }

    private void Erode(float dropSedimentCapacity, float heightDiff)
    {
        float erosit = Math.Min((dropSedimentCapacity - sediment) * Settings.erosion, -heightDiff);
        foreach (KeyValuePair<ErosionRegion, float> kvp in position.ErosionWeights)
        {
            ErosionRegion location = kvp.Key;
            float weight = kvp.Value;
            location.Elevation -= erosit * weight;
        }
        sediment += erosit;
    }

    private void UpdateValues(ErosionRegion newPosition, Vector3 newDirection, float newVelocity, float newVolume)
    {
        position = newPosition;
        direction = newDirection;
        velocity = newVelocity;
        volume = newVolume;
    }
}

public struct WaterDropletSettings
{
    public int lifetime;
    public float inertia;
    public float gravity;
    public float evaporation;
    public float capacity;
    public float erosion;
    public float deposition;
    public float minErosion;
}
