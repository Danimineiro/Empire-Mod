using UnityEngine;

namespace FactionColonies.util
{
    static class Modifiers
    {
        public static int GetModifier => 1 * (Event.current.shift ? 5 : 1) * (Event.current.control ? 10 : 1);
    }
}
