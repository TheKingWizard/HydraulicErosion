using System.Collections.Generic;
using System.Numerics;
using System;

public class WaterDroplet
{
    private static WaterDropletSettings settings;

    private readonly Random random;

    private ErosionRegion position;
    private Vector3 direction;
    private float velocity = 1;
    private float volume = 1;
    private float sediment = 0;

    static WaterDroplet()
    {
        settings.lifetime = 50;
        settings.inertia = 0.5f;
        settings.gravity = 4.0f;
        settings.evaporation = 0.05f;
        settings.capacity = 2.0f;
        settings.erosion = 0.3f;
        settings.deposition = 0.01f;
        settings.minErosion = 0.01f;
    }

    public static void SetWaterDropletSettings(WaterDropletSettings settings)
    {
        WaterDroplet.settings = settings;
    }

    public WaterDroplet(ErosionRegion region)
    {
        position = region;
        random = new Random((int)(position.Center.X * position.Center.Y));
        direction = Vector3.Normalize(new Vector3((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 2 - 1), 0));
    }

    public void Simulate()
    {
        for (int i = 0; i < settings.lifetime; i++)
        {
            SimulationStep();
        }
    }

    private void SimulationStep()
    {
        Vector3 gradient = GetGradient(position);
        Vector3 newDirection = Vector3.Normalize(direction * settings.inertia - gradient * (1f - settings.inertia));

        ErosionRegion newPosition = FindNextLocation(newDirection);

        float heightDiff = newPosition.Elevation - position.Elevation;
        SimulateHydraulicAction(heightDiff);

        float newVelocity = (float)Math.Sqrt(Math.Max(0, Math.Pow(velocity, 2) - heightDiff * settings.gravity));
        float newVolume = volume * (1 - settings.evaporation);

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
            gradient = Vector3.Normalize(gradient) * settings.inertia / (1 - settings.inertia);
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
        float dropSedimentCapacity = Math.Max(-heightDiff * velocity * volume * settings.capacity, settings.minErosion);
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
        float deposit = (heightDiff > 0) ? Math.Min(heightDiff, sediment) : (sediment - dropSedimentCapacity) * settings.deposition;
        sediment -= deposit;
        position.Elevation += deposit;
    }

    private void Erode(float dropSedimentCapacity, float heightDiff)
    {
        float erosit = Math.Min((dropSedimentCapacity - sediment) * settings.erosion, -heightDiff);
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
