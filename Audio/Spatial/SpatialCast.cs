namespace Cutulu.Audio.Spatial
{
    using Cutulu.Core;
    using Godot;

    public partial class SpatialCast : Node3D
    {
        [Export] private Color Color = Colors.Purple;

        public override void _Ready()
        {
            base._Ready();


        }

        public bool Soundcast(Vector3 _origin, Vector3 _direction, out SpatialResult _result)
        {
            if (Physics.Raycast(this, _origin, _direction, out RaycastHit _hit, 100))
            {
                var _distance = _hit.Distance;
                var _hitDb = CalculateDbAfterDistance(0.0f, _distance, 1.0f, 440f);

                if (_hitDb > 10.0f)
                {
                    _result = new SpatialResult()
                    {
                        Db = _hitDb,
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
            public float Db { get; set; }
            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }
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