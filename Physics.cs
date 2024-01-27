using System.Collections.Generic;
using Godot.Collections;
using Godot;

namespace Cutulu
{
    public static class Physics
    {
        #region Values
        private static float _gravity2D, _gravity3D;
        private static Vector2 _gravity2DV;
        private static Vector3 _gravity3DV;

        public static Vector2 Gravity2DVector { get { if (_gravity2DV.Equals(default)) _gravity2DV = ProjectSettings.GetSetting("physics/2d/default_gravity_vector", Vector2.Down).AsVector2() * Gravity2D; return _gravity2DV; } }
        public static float Gravity2D { get { if (_gravity2D.Equals(default)) _gravity2D = ProjectSettings.GetSetting("physics/2d/default_gravity", 980).AsSingle(); return _gravity2D; } }

        public static Vector3 Gravity3DVector { get { if (_gravity3DV.Equals(default)) _gravity3DV = ProjectSettings.GetSetting("physics/3d/default_gravity_vector", Vector3.Down).AsVector3() * Gravity3D; return _gravity3DV; } }
        public static float Gravity3D { get { if (_gravity3D.Equals(default)) _gravity3D = ProjectSettings.GetSetting("physics/3d/default_gravity", 9.8f).AsSingle(); return _gravity3D; } }
        #endregion

        #region Raycast Functions
        public static bool Linecast(this Node3D node, Vector3 fromGlobal, Vector3 toGlobal, out RaycastHit hit, uint mask = 4294967295)
            => Raycast(node, fromGlobal, toGlobal - fromGlobal, out hit, fromGlobal.DistanceTo(toGlobal), mask);

        public static bool Raycast(this Camera3D camera, out RaycastHit hit, float maxDistance, uint mask = 4294967295)
        {
            Vector3 origin = camera.ProjectRayOrigin(camera.MousePosition());
            Vector3 dir = camera.ProjectRayNormal(camera.MousePosition());

            return Raycast(camera, origin, dir, out hit, maxDistance, mask);
        }

        public static bool Raycast(this Node3D node, Vector3 globalOrigin, Vector3 direction, out RaycastHit hit, float maxDistance, uint mask = 4294967295)
        {
            direction = direction.Normalized();

            var query = PhysicsRayQueryParameters3D.Create(globalOrigin, globalOrigin + direction * maxDistance, mask);
            var state = node.GetWorld3D().DirectSpaceState;

            var result = state.IntersectRay(query);

            if (result.Count < 1)
            {
                hit = default;
                return false;
            }

            hit = new RaycastHit(globalOrigin, (Vector3)result["position"], (GodotObject)result["collider"], result);
            return true;
        }

        public static bool Raycast2D(this Node2D node, Vector2 globalOrigin, Vector2 direction, out RaycastHit2D hit, float maxDistance)
        {
            direction = direction.Normalized();

            var query = PhysicsRayQueryParameters2D.Create(globalOrigin, globalOrigin + direction * maxDistance);
            var state = node.GetWorld2D().DirectSpaceState;

            var result = state.IntersectRay(query);

            if (result.Count < 1)
            {
                hit = default;
                return false;
            }

            hit = new RaycastHit2D(globalOrigin, (Vector2)result["position"], (GodotObject)result["collider"], result);
            return true;
        }
        #endregion

        #region Curved Raycast Functions
        public static bool RaycastCurve(Vector3 origin, Vector3 direction, out RaycastHit hit, Color color, float gravity, float resolution, float length)
            => Core.Main3D.RaycastCurve(origin, direction, out hit, color, gravity, resolution, length);

        public static bool RaycastCurve(this Node3D node, out RaycastHit hit, Color color, float gravity, float resolution, float length)
            => RaycastCurve(node, node.GlobalPosition, node.Forward(), out hit, color, gravity, resolution, length);

        public static bool RaycastCurve(this Node3D node, Vector3 origin, Vector3 direction, out RaycastHit hit, Color color, float gravity, float resolution, float length)
        {
            hit = default;

            if (length <= 0) return false;

            int count = Mathf.FloorToInt(length / resolution);
            float rest = length - count * resolution;
            if (count < 1 && rest <= 0) return false;

            Vector3 right = direction.toRight();
            Vector3 last = origin;
            float distance = 0;

            gravity *= resolution;

            // Draw main part
            RaycastHit _hit = default;
            for (int i = 0; i < count; i++)
            {
                Vector3 _v = last;

                if (check())
                {
                    hit = new RaycastHit(origin, _hit.point, _hit.collider, _hit.result, distance + _v.DistanceTo(_hit.point));
                    return true;
                }

                distance += _v.DistanceTo(last);
            }

            // Draw rest
            if (rest > 0)
            {
                Vector3 _v = last;

                if (check(rest / resolution))
                {
                    hit = new RaycastHit(origin, _hit.point, _hit.collider, _hit.result, distance + _v.DistanceTo(_hit.point));
                    return true;
                }
            }

            return false;

            void rotate() => direction = direction.Rotated(right, gravity);
            bool check(float value = 1f)
            {
                Vector3 from = last;

                rotate();
                last += direction * value * resolution;

                return Linecast(node, from, last, out _hit);
            }
        }
        #endregion
    }

    #region RaycastHit
    public struct RaycastHit
    {
        public GodotObject collider;
        public float distance;
        public Vector3 point;

        public Dictionary result;

        public RaycastHit(Vector3 origin, Vector3 point, GodotObject collider, Dictionary result)
        {
            this.collider = collider;
            this.point = point;

            distance = origin.DistanceTo(point);
            this.result = result;
        }

        public RaycastHit(Vector3 origin, Vector3 point, GodotObject collider, Dictionary result, float distance)
        {
            this.collider = collider;
            this.distance = distance;
            this.point = point;

            this.result = result;
        }
    }

    public struct RaycastHit2D
    {
        public GodotObject collider;
        public float distance;
        public Vector2 point;

        public Dictionary result;

        public RaycastHit2D(Vector2 origin, Vector2 point, GodotObject collider, Dictionary result)
        {
            this.collider = collider;
            this.point = point;

            distance = origin.DistanceTo(point);
            this.result = result;
        }

        public RaycastHit2D(Vector2 origin, Vector2 point, GodotObject collider, Dictionary result, float distance)
        {
            this.collider = collider;
            this.distance = distance;
            this.point = point;

            this.result = result;
        }
    }
    #endregion
}