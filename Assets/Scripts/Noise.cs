using System;
using System.Numerics;
using static System.Math;

public class Noise
{
    public int Octaves
    {
        get
        {
            return octaves;
        }

        set
        {
            if (value <= 0)
                throw new ArgumentException("Number of octaves must be greater than 0");
            octaves = value;
        }
    }
    public float Amplitude
    {
        get { return amplitude; }
        set
        {
            amplitude = value;
            RecalculateAmplitudes();
        }
    }
    public float Persistence
    {
        get { return persistence; }
        set
        {
            persistence = value;
            RecalculateAmplitudes();
        }
    }
    public float Frequency
    {
        get { return frequency; }
        set
        {
            frequency = value;
            RecalculateFrequencies();
        }
    }
    public float Lacunarity
    {
        get { return lacunarity; }
        set
        {
            lacunarity = value;
            RecalculateFrequencies();
        }
    }
    public int Precision
    {
        get
        {
            return precision;
        }

        set
        {
            if(value < 0)
                throw new ArgumentException("Precision must be greater than or equal to 0");
            precision = value;
            floatMultiplier = (int)Pow(10, precision);
        }
    }
    private int octaves = 1;
    private float amplitude = 1f;
    private float persistence = 1f;
    private float[] amplitudes;
    private float frequency = 1f;
    private float lacunarity = 1f;
    private float[] frequencies;
    private int precision = 1;
    private int floatMultiplier = 10;
    
    


    public Noise()
    {
        RecalculateAmplitudes();
        RecalculateFrequencies();
    }

    public Noise(int seed) : this()
    {
        NoiseGenerator.Seed = seed;
    }

    public void SetParameters(int octaves, float amplitude, float persistence, float frequency, float lacunarity)
    {
        Octaves = octaves;
        Amplitude = amplitude;
        Persistence = persistence;
        Frequency = frequency;
        Lacunarity = lacunarity;
    }

    /*public float Calc1D(float x)
    {
        int X = (int)(x * floatMultiplier);
        float result = 0;
        for (int i = 0; i < Octaves; i++)
        {
            result += amplitudes[i] * Simplex.Noise.CalcPixel1D(X, 1f / frequencies[i]) / 255f;
        }
        return result;
    }

    public float Calc2D(float x, float y)
    {
        int X = (int)(x * floatMultiplier);
        int Y = (int)(y * floatMultiplier);
        float result = 0;
        for (int i = 0; i < Octaves; i++)
        {
            result += amplitudes[i] * Simplex.Noise.CalcPixel2D(X, Y, 1f / frequencies[i]) / 255f;
        }
        return result;
    }*/

    

    public float Evaluate(Vector3 vector)
    {
        float result = 0;
        for (int i = 0; i < Octaves; i++)
        {
            result += amplitudes[i] * NoiseGenerator.Evaluate(vector * frequencies[i]);
        }
        return result;
    }

    private void RecalculateAmplitudes()
    {
        amplitudes = new float[Octaves];
        amplitudes[0] = Amplitude;
        for (int i = 1; i < Octaves; i++)
        {
            amplitudes[i] = amplitudes[i - 1] * Persistence;
        }
    }

    private void RecalculateFrequencies()
    {
        frequencies = new float[Octaves];
        frequencies[0] = Frequency;
        for (int i = 1; i < Octaves; i++)
        {
            frequencies[i] = frequencies[i - 1] * Lacunarity;
        }
    }
}
