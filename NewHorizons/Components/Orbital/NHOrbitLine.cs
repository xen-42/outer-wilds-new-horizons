using System;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;
namespace NewHorizons.Components.Orbital
{
    public class NHOrbitLine : OrbitLine
    {
        private Vector3 _semiMajorAxis;
        private Vector3 _semiMinorAxis;

        private Vector3 _upAxis;
        private float _fociDistance;
        private Vector3[] _verts;

        public override void InitializeLineRenderer()
        {
            base.GetComponent<LineRenderer>().positionCount = this._numVerts;
        }

        public override void OnValidate()
        {
            if (_numVerts < 0 || _numVerts > 4096)
            {
                _numVerts = Mathf.Clamp(_numVerts, 0, 4096);
            }
            if (base.GetComponent<LineRenderer>().positionCount != this._numVerts)
            {
                InitializeLineRenderer();
            }
        }

        public override void Start()
        {
            base.Start();

            var a = _semiMajorAxis.magnitude;
            var b = _semiMinorAxis.magnitude;

            _upAxis = Vector3.Cross(_semiMajorAxis.normalized, _semiMinorAxis.normalized);

            _fociDistance = Mathf.Sqrt(a * a - b * b);
            if (float.IsNaN(_fociDistance)) _fociDistance = 0f;

            _verts = new Vector3[this._numVerts];

            transform.localRotation = Quaternion.Euler(270, 90, 0);

            base.enabled = false;
        }

        public override void Update()
        {
            try
            {
                AstroObject primary = _astroObject?.GetPrimaryBody();

                // If it has nothing to orbit then why is this here
                if (primary == null)
                {
                    base.enabled = false;
                    return;
                }

                Vector3 origin = primary.transform.position + _semiMajorAxis.normalized * _fociDistance;

                var rot = Quaternion.identity;

                if (_astroObject?._primaryBody?._primaryBody != null)
                {
                    var lhs = _astroObject._primaryBody.transform.position - _astroObject._primaryBody._primaryBody.transform.position;
                    var rhs = _astroObject._primaryBody.gameObject.GetComponent<InitialMotion>().GetInitVelocity();
                    var up = Vector3.Cross(lhs, rhs);
                    rot = Quaternion.FromToRotation(Vector3.up, up);
                }

                float num = CalcProjectedAngleToCenter(origin, rot * _semiMajorAxis, rot * _semiMinorAxis, _astroObject.transform.position);

                for (int i = 0; i < _numVerts; i++)
                {
                    var stepSize = 2f * Mathf.PI / (float)(_numVerts - 1);
                    float f = num + stepSize * i;
                    _verts[i] = _semiMajorAxis * Mathf.Cos(f) + _semiMinorAxis * Mathf.Sin(f);
                }
                _lineRenderer.SetPositions(_verts);

                transform.position = origin;

                transform.rotation = rot;

                float num2 = DistanceToEllipticalOrbitLine(origin, _semiMajorAxis, _semiMinorAxis, _upAxis, Locator.GetActiveCamera().transform.position);
                float widthMultiplier = Mathf.Min(num2 * (_lineWidth / 1000f), _maxLineWidth);
                float num3 = _fade ? (1f - Mathf.Clamp01((num2 - _fadeStartDist) / (_fadeEndDist - _fadeStartDist))) : 1f;

                _lineRenderer.widthMultiplier = widthMultiplier;
                _lineRenderer.startColor = new Color(_color.r, _color.g, _color.b, num3 * num3);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception in OrbitLine for [{_astroObject?.name}] : {ex.Message}, {ex.StackTrace}");
                enabled = false;
            }
        }

        public void SetFromParameters(IOrbitalParameters parameters)
        {
            var a = parameters.semiMajorAxis;
            var e = parameters.eccentricity;
            var b = a * Mathf.Sqrt(1f - (e * e));
            var l = parameters.longitudeOfAscendingNode;
            var p = parameters.argumentOfPeriapsis;
            var i = parameters.inclination;

            _semiMajorAxis = a * OrbitalParameters.Rotate(Vector3.left, l, i, p);
            _semiMinorAxis = b * OrbitalParameters.Rotate(Vector3.forward, l, i, p);
        }

        private float CalcProjectedAngleToCenter(Vector3 foci, Vector3 semiMajorAxis, Vector3 semiMinorAxis, Vector3 point)
        {
            Vector3 lhs = point - foci;
            Vector3 vector = new Vector3(Vector3.Dot(lhs, semiMajorAxis.normalized), 0f, Vector3.Dot(lhs, semiMinorAxis.normalized));
            vector.x *= semiMinorAxis.magnitude / semiMajorAxis.magnitude;
            return Mathf.Atan2(vector.z, vector.x);
        }

        private float DistanceToEllipticalOrbitLine(Vector3 foci, Vector3 semiMajorAxis, Vector3 semiMinorAxis, Vector3 upAxis, Vector3 point)
        {
            float f = CalcProjectedAngleToCenter(foci, semiMajorAxis, semiMinorAxis, point);
            Vector3 b = foci + _semiMajorAxis * Mathf.Cos(f) + _semiMinorAxis * Mathf.Sin(f);
            return Vector3.Distance(point, b);
        }
    }
}
