using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeographicalProperties
{
    public float Elevation { get; set; } // In meters

    public GeographicalProperties(float elevation)
    {
        Elevation = elevation;
    }
}
