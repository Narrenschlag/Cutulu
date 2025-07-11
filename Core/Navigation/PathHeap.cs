#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using System.Collections.Generic;
    using Godot;

    public class PathHeap
    {
        private readonly List<Vector3> list;
        private readonly List<float> keys;

        public float PathLength => keys[^1];

        public PathHeap(List<Vector3> list)
        {
            this.list = list ?? [];
            keys = [0];

            float sum = 0;
            if (list.NotEmpty())
                for (int i = 0; i < list.Count - 1; i++)
                {
                    float distance = list[i].DistanceTo(list[i + 1]);
                    sum += distance;

                    keys.Add(sum);
                }
        }

        public PathHeap(Vector3 v3) : this([v3]) { }

        public void Add(Vector3 element)
        {
            keys.Add(keys[^1] + list[^1].DistanceTo(element));
            list.Add(element);
        }

        public float PercentageAt(int index) => keys[Mathf.Clamp(index, 0, keys.Count - 1)] / PathLength;
    }
}
#endif