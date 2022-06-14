namespace NewHorizons.Components.Orbital
{
    public interface IOrbitalParameters
    {
        // lowercase cuz schema
        float inclination { get; set; }
        float semiMajorAxis { get; set; }
        float longitudeOfAscendingNode { get; set; }
        float eccentricity { get; set; }
        float argumentOfPeriapsis { get; set; }
        float trueAnomaly { get; set; }

        OrbitalParameters GetOrbitalParameters(Gravity primaryGravity, Gravity secondaryGravity, AstroObject primary, AstroObject secondary);
    }
}
