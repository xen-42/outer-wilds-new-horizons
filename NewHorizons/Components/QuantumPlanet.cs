using NewHorizons.Builder.General;
using NewHorizons.Builder.Orbital;
using NewHorizons.Components.Orbital;
using NewHorizons.External.Modules;
using NewHorizons.Handlers;
using NewHorizons.Utility;
using System.Collections.Generic;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;
using Random = UnityEngine.Random;
namespace NewHorizons.Components
{
    public class QuantumPlanet : QuantumObject
    {
        public List<State> states = new List<State>();
        public State groundState;

        private int _currentIndex;
        private NHAstroObject _astroObject;
        private ConstantForceDetector _detector;
        private AlignWithTargetBody _alignment;
        private OWRigidbody _rb;
        private OrbitLine _orbitLine;

        public int CurrentIndex { get { return _currentIndex; } }

        public override void Awake()
        {
            base.Awake();

            _astroObject = GetComponent<NHAstroObject>();
            _detector = GetComponentInChildren<ConstantForceDetector>();
            _alignment = GetComponent<AlignWithTargetBody>();
            _rb = GetComponent<OWRigidbody>();
            _orbitLine = GetComponent<OrbitLine>();

            GlobalMessenger.AddListener("PlayerBlink", new Callback(OnPlayerBlink));

            _maxSnapshotLockRange = 300000f;
        }

        public override void Start()
        {
            base.Start();

            foreach (var state in states)
            {
                state.sector?.gameObject?.SetActive(false);
            }

            ChangeQuantumState(true);
        }

        public override bool ChangeQuantumState(bool skipInstantVisibilityCheck)
        {
            Logger.Log($"Trying to change quantum state");

            if (states.Count <= 1) return false;

            // Don't move if we have a picture or if we're on it
            if (IsLockedByProbeSnapshot() || IsPlayerEntangled()) return false;

            var canChange = false;

            var oldState = states[_currentIndex];

            // This will all get set in the for loop
            State newState = oldState;
            int newIndex = _currentIndex;
            AstroObject primaryBody = null;
            OrbitalParameters orbitalParams = null;

            // The QM tries to switch 10 times so we'll do that too
            for (int i = 0; i < 10; i++)
            {
                newIndex = _currentIndex;
                while (newIndex == _currentIndex)
                {
                    newIndex = Random.Range(0, states.Count);
                }

                newState = states[newIndex];

                // Figure out what the new orbit will be if we switch
                var newOrbit = newState.orbit ?? groundState.orbit;
                newOrbit.trueAnomaly = Random.Range(0f, 360f);

                primaryBody = AstroObjectLocator.GetAstroObject(newOrbit.primaryBody);
                var primaryGravity = new Gravity(primaryBody.GetGravityVolume());
                var secondaryGravity = new Gravity(_astroObject.GetGravityVolume());
                orbitalParams = newOrbit.GetOrbitalParameters(primaryGravity, secondaryGravity, primaryBody, _astroObject);

                var pos = primaryBody.transform.position + orbitalParams.InitialPosition;

                // See if we can switch, so we move the visibility tracker to the new position
                _visibilityTrackers[0].transform.position = pos;

                // Only if not seen
                if (!CheckVisibilityInstantly())
                {
                    canChange = true;

                    // Since we were able to change states we break
                    break;
                }
            }

            if (canChange)
            {
                if (newState.sector != null && newState.sector != oldState.sector) SetNewSector(oldState, newState);
                if (newState.orbit != null && newState.orbit != oldState.orbit) SetNewOrbit(primaryBody, orbitalParams);

                _currentIndex = newIndex;

                GlobalMessenger<OWRigidbody>.FireEvent("QuantumMoonChangeState", _rb);
            }

            // Be completely sure we move the visibility tracker back to our planet
            _visibilityTrackers[0].transform.localPosition = Vector3.zero;

            return true;
        }

        private void SetNewSector(State oldState, State newState)
        {
            oldState.sector.gameObject.SetActive(false);
            newState.sector.gameObject.SetActive(true);
        }

        private void SetNewOrbit(AstroObject primaryBody, OrbitalParameters orbitalParameters)
        {
            _astroObject._primaryBody = primaryBody;
            DetectorBuilder.SetDetector(primaryBody, _astroObject, _detector);
            _detector._activeInheritedDetector = primaryBody.GetComponentInChildren<ForceDetector>();
            _detector._activeVolumes = new List<EffectVolume>() { primaryBody.GetGravityVolume() };
            if (_alignment != null) _alignment.SetTargetBody(primaryBody.GetComponent<OWRigidbody>());

            _astroObject.SetOrbitalParametersFromTrueAnomaly(orbitalParameters.eccentricity, orbitalParameters.semiMajorAxis, orbitalParameters.inclination, orbitalParameters.argumentOfPeriapsis, orbitalParameters.longitudeOfAscendingNode, orbitalParameters.trueAnomaly);

            PlanetCreationHandler.UpdatePosition(gameObject, orbitalParameters, primaryBody, _astroObject);

            if (!Physics.autoSyncTransforms)
            {
                Physics.SyncTransforms();
            }

            _rb.SetVelocity(orbitalParameters.InitialVelocity + primaryBody.GetAttachedOWRigidbody().GetVelocity());

            if (_orbitLine is NHOrbitLine nhOrbitLine)
            {
                nhOrbitLine.SetFromParameters(orbitalParameters);
            }

            if (_orbitLine is TrackingOrbitLine trackingOrbitLine)
            {
                trackingOrbitLine.Reset();
            }
        }

        private void OnPlayerBlink()
        {
            if (base.IsVisible())
            {
                base.Collapse(true);
            }
        }

        public override bool IsPlayerEntangled()
        {
            return states[_currentIndex].sector.ContainsAnyOccupants(DynamicOccupant.Player);
        }

        public class State
        {
            public Sector sector { get; set; }
            public OrbitModule orbit { get; set; }

            public State(Sector sector, OrbitModule orbit)
            {
                this.sector = sector;
                this.orbit = orbit;
            }
        }
    }
}
