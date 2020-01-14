using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System;

public class WaterDroplet
{
    private static readonly int lifetime = 50;
    private static readonly float inertia = 0.1f;
    private static readonly float gravity = 4.0f;
    private static readonly float evaporation = 0.05f;
    private static readonly float capacity = 2.0f;
    private static readonly float erosion = 0.3f;
    private static readonly float deposition = 0.01f;
    private static readonly float minErosion = 0.01f;

    private readonly Random random;

    private ErosionRegion position;
    private Vector3 direction;
    private float velocity = 1;
    private float volume = 1;
    private float sediment = 0;

    

    private bool IsAlive
    {
        get 
        {
            if (float.IsNaN(velocity))
                return false;
            else
                return true;
        }
    }

    public WaterDroplet(ErosionRegion region)
    {
        position = region;
        random = new Random((int)(position.Center.X * position.Center.Y));
        direction = Vector3.Normalize(new Vector3((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 2 - 1), 0));
    }

    public async void Simulate()
    {
        for (int i = 0; i < lifetime; i++)
        {
            if(IsAlive)
                await Task.Run(new Action(SimulationStep));
            else
                break;
        }
    }

    private void SimulationStep()
    {
        Vector3 gradient = GetGradient(position);
        Vector3 newDirection = Vector3.Normalize(direction * inertia - gradient * (1f - inertia));

        ErosionRegion newPosition = FindNextLocation(newDirection);

        float heightDiff = newPosition.elevation - position.elevation;
        SimulateHydraulicAction(heightDiff);

        float newVelocity = (float)Math.Sqrt(Math.Pow(velocity, 2) - heightDiff * gravity);
        float newVolume = volume * (1 - evaporation);

        UpdateValues(newPosition, newDirection, newVelocity, newVolume);
    }

    private Vector3 GetGradient(ErosionRegion region)
    {
        Dictionary<Vector3, float> heightDiffs = new Dictionary<Vector3, float>();
        float totalHeightDiff = 0;
        bool inPit = true;
        foreach (ErosionRegion adjacentNode in region.AdjacentRegions)
        {
            float heightDiff = adjacentNode.elevation - position.elevation;
            heightDiffs.Add(Vector3.Normalize(adjacentNode.Center - position.Center), heightDiff);
            totalHeightDiff += Math.Abs(heightDiff);
            if (heightDiff < 0)
                inPit = false;
        }
        Vector3 gradient = new Vector3(0, 0, 0);
        if (totalHeightDiff == 0)
        {
            gradient.X = (float)(random.NextDouble() * 2 - 1);
            gradient.Y = (float)(random.NextDouble() * 2 - 1);
            gradient = Vector3.Normalize(gradient) * 0.5f * inertia / (1 - inertia);
            return gradient;
        }
        else if (inPit)
        {
            return gradient;
        }
        else
        {
            foreach (Vector3 direction in heightDiffs.Keys)
            {
                gradient += direction * heightDiffs[direction] / totalHeightDiff;
            }
            if (gradient == Vector3.Zero)
            {
                gradient.X = (float)(random.NextDouble() * 2 - 1);
                gradient.Y = (float)(random.NextDouble() * 2 - 1);
                gradient = Vector3.Normalize(gradient) * inertia / (1 - inertia);
                return gradient;
            }
            gradient = Vector3.Normalize(gradient);
            if (random.NextDouble() > 0.5)
            {
                Vector3 jitter = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), 0);
                jitter = Vector3.Normalize(jitter);
                gradient = Vector3.Normalize(gradient + 0.5f * jitter);
            }

        }

        return gradient;
    }

    private ErosionRegion FindNextLocation(Vector3 newDirection)
    {
        ErosionRegion newPosition = new ErosionRegion(new Region(new Vector3(), null));
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
        float dropSedimentCapacity = System.Math.Max(-heightDiff * velocity * volume * capacity, minErosion);
        if (sediment > dropSedimentCapacity || heightDiff > 0)
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
        float deposit = (heightDiff > 0) ? System.Math.Min(heightDiff, sediment) : (sediment - dropSedimentCapacity) * deposition;
        sediment -= deposit;
        Interlocked.Exchange(ref position.elevation, position.elevation + deposit);
    }

    private void Erode(float dropSedimentCapacity, float heightDiff)
    {
        float erosit = System.Math.Min((dropSedimentCapacity - sediment) * erosion, -heightDiff);
        foreach (KeyValuePair<ErosionRegion, float> kvp in position.ErosionWeights)
        {
            ErosionRegion location = kvp.Key;
            float weight = kvp.Value;

            Interlocked.Exchange(ref location.elevation, location.elevation - erosit * weight);
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
