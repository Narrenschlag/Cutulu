namespace Cutulu
{
    using Godot;

    public static class Hexe
    {
        /// <summary>
        /// Returns the total cell count of a range including all rings from 0 to the specified ringCount.
        /// </summary>
        public static int GetCellCountInRange(int ringCount)
        {
            return ringCount < 1 ? 1 : 1 + 3 * ringCount * (ringCount + 1);
        }

        /// <summary>
        /// Returns the cell count of a single ring.
        /// </summary>
        public static int GetCellCountInRing(int ringCount)
        {
            return ringCount < 1 ? 1 : 6 * ringCount;
        }
    }
}