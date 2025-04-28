namespace Cutulu.Audio.Spatial
{
    using System.Threading.Tasks;
    using Cutulu.Core;
    using Godot;

    public partial class SpatialCast : Node3D
    {
        public static float GetSoundTravelDuration(float _distance_in_m, float _temperature_in_c = 20.0f)
        {
            var _speed_mtps = 343.0f + 0.6f * (_temperature_in_c - 20.0f);
            return _distance_in_m / _speed_mtps;
        }

        [Export] private Node3D DirectionalTarget { get; set; }
        [Export] private Color Color { get; set; } = Colors.Purple;

        public override async void _Ready()
        {
            base._Ready();

            var _reflection_value = 0.75f;

            var _db = 20.0f;
            var _origin = GlobalPosition;
            var _direction = DirectionalTarget.GlobalPosition - GlobalPosition;

            while (Soundcast(_origin, _direction, _db, out var _spatial))
            {
                var _duration = GetSoundTravelDuration(_spatial.Distance);

                await Task.Delay(Mathf.RoundToInt(_duration * 1000.0f));

                Debug.Log($"Play @{_spatial.Db:n2} db");
                Cutulu.Mesh.Render.DrawLine(this, Color, _origin, _spatial.Position);

                _db = _spatial.Db * _reflection_value;
                _origin = _spatial.Position;

                _direction = CalculateReflection(_direction, _spatial.Normal);
            }
        }

        public bool Soundcast(Vector3 _origin, Vector3 _direction, float _db, out SpatialResult _result)
        {
            if (Physics.Raycast(this, _origin, _direction, out RaycastHit _hit, 100))
            {
                var _distance = _hit.Distance;
                var _hitDb = CalculateDbAfterDistance(_db, _distance, 1.0f, 440f);

                if (_hitDb > 0.1f)
                {
                    _result = new SpatialResult()
                    {
                        Db = _hitDb,
                        Distance = _hit.Distance,
                        Position = _hit.Point,
                        Normal = _hit.Normal,
                    };
                    return true;
                }
            }

            _result = default;
            return false;
        }

        public struct SpatialResult
        {
            public float Distance { get; set; }
            public float Db { get; set; }

            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }
        }

        /// <summary>
        /// Calculates the reflection vector when a ray hits a surface.
        /// </summary>
        /// <param name="incidentVector">The incoming ray direction (should be normalized).</param>
        /// <param name="normal">The surface normal at the hit point (should be normalized).</param>
        /// <returns>The reflection vector.</returns>
        public static Vector3 CalculateReflection(Vector3 incidentVector, Vector3 normal)
        {
            // Formula: reflection = incident - 2 * (incident • normal) * normal
            // Where • represents dot product

            float dotProduct = incidentVector.Dot(normal);
            return incidentVector - 2 * dotProduct * normal;
        }

        /// <summary>
        /// Calculates the perceived sound level in dB after traveling a given distance through air.
        /// Stops if the sound level falls below a minimum dB threshold.
        /// </summary>
        /// <param name="initialDb">Starting sound level in dB.</param>
        /// <param name="startDistance">Starting distance in meters (usually 1.0).</param>
        /// <param name="targetDistance">Target distance in meters.</param>
        /// <param name="frequencyHz">Frequency of the sound in Hz.</param>
        /// <param name="minDbThreshold">Minimum dB threshold below which sound is ignored.</param>
        /// <returns>The resulting dB level at the target distance, or min threshold if lower.</returns>
        public static float CalculateDbAfterDistance(float initialDb, float startDistance, float targetDistance, float frequencyHz, float minDbThreshold = 0.0f)
        {
            if (targetDistance <= startDistance)
                return Mathf.Max(initialDb, minDbThreshold);

            // 1. Spherical spreading loss
            // Spreading loss in dB: 20 * log10(target/start)
            var spreadingLossDb = 20f * (float)System.Math.Log10(targetDistance / startDistance);

            // 2. Air absorption loss
            var absorptionLossPerMeterDb = EstimateAirAbsorptionDbPerMeter(frequencyHz);
            var absorptionLossDb = (targetDistance - startDistance) * absorptionLossPerMeterDb;

            // Total loss
            var totalLossDb = spreadingLossDb + absorptionLossDb;

            var resultingDb = initialDb - totalLossDb;

            if (resultingDb < minDbThreshold)
                return minDbThreshold;

            return resultingDb;
        }

        /// <summary>
        /// Very rough estimate of air absorption loss per meter depending on frequency.
        /// </summary>
        private static float EstimateAirAbsorptionDbPerMeter(float frequencyHz)
        {
            if (frequencyHz <= 250f)
                return 0.01f;
            else if (frequencyHz <= 1000f)
                return 0.1f;
            else if (frequencyHz <= 4000f)
                return 0.5f;
            else if (frequencyHz <= 8000f)
                return 1.5f;
            else
                return 5f;
        }
    }
}