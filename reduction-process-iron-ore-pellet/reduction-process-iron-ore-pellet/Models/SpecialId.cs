using System;

namespace grain_growth.Models
{
    public class SpecialId
    {
        public static bool IsSpecialId(int id)
        {
            return Enum.IsDefined(typeof(Id), id);
        }

        public enum Id
        {
            Empty = 0,
            Border = -1,
            Transparent = -9
        }
    }
}
