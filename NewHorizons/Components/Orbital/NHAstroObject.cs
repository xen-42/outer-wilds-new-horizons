using NewHorizons.External.Modules;
namespace NewHorizons.Components.Orbital
{
    public class NHAstroObject : AstroObject, IOrbitalParameters
    {
        public float inclination { get; set; }
        public float semiMajorAxis { get; set; }
        public float longitudeOfAscendingNode { get; set; }
        public float eccentricity { get; set; }
        public float argumentOfPeriapsis { get; set; }
        public float trueAnomaly { get; set; }
        public bool HideDisplayName { get; set; }

        public void SetOrbitalParametersFromConfig(OrbitModule orbit)
        {
            SetOrbitalParametersFromTrueAnomaly(orbit.eccentricity, orbit.semiMajorAxis, orbit.inclination, orbit.argumentOfPeriapsis, orbit.longitudeOfAscendingNode, orbit.trueAnomaly);
        }

        public void SetOrbitalParametersFromTrueAnomaly(float ecc, float a, float i, float p, float l, float trueAnomaly)
        {
            inclination = ecc;
            semiMajorAxis = a;
            longitudeOfAscendingNode = l;
            inclination = i;
            eccentricity = ecc;
            argumentOfPeriapsis = p;
            this.trueAnomaly = trueAnomaly;
        }

        public override string ToString()
        {
            return $"ParameterizedAstroObject: Eccentricity {eccentricity}, SemiMajorAxis {semiMajorAxis}, Inclination {inclination}, ArgumentOfPeriapsis {argumentOfPeriapsis}, LongitudeOfAscendingNode {longitudeOfAscendingNode}, TrueAnomaly {trueAnomaly}";
        }

        public OrbitalParameters GetOrbitalParameters(Gravity primaryGravity, Gravity secondaryGravity, AstroObject primary, AstroObject secondary)
        {
            return OrbitalParameters.FromTrueAnomaly(primaryGravity, secondaryGravity, primary, secondary, eccentricity, semiMajorAxis, inclination, argumentOfPeriapsis, longitudeOfAscendingNode, trueAnomaly);
        }
    }
}
