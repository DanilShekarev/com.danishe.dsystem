using System;

namespace DSystem.Utils
{
    public static class ArrayExtension
    {
        public static void Expand(this Array array, int newLength)
        {
            var temp = (Array)array.Clone();
            array = Array.CreateInstance(array.GetType(), newLength);
            temp.CopyTo(array, 0);
        }
    }
}