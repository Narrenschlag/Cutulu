using System.Collections.Generic;
using Godot;

namespace Cutulu.Core
{
	public class PathHeap
	{
		private List<Vector3> list;
		private List<float> keys;

		public float PathLength => keys[keys.Count - 1];

		public PathHeap(List<Vector3> list)
		{
			keys = new List<float>() { 0 };
			this.list = list;

			float sum = 0;
			if (list.NotEmpty())
				for (int i = 0; i < list.Count - 1; i++)
				{
					float distance = list[i].DistanceTo(list[i + 1]);
					sum += distance;

					keys.Add(sum);
				}
		}

		public PathHeap(Vector3 v3) : this(new List<Vector3>() { v3 }) { }

		public void Add(Vector3 element)
		{
			keys.Add(keys[keys.Count - 1] + list[list.Count - 1].DistanceTo(element));
			list.Add(element);
		}

		public float PercentageAt(int index) => keys[Mathf.Clamp(index, 0, keys.Count - 1)] / PathLength;
	}
}
