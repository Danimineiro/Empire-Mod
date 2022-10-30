using System;
using Verse;

namespace FactionColonies
{
    public enum ResourceType
    {
        Food,
        Weapons,
        Apparel,
        Animals,
        Logging,
        Mining,
        Research,
        Power,
        Medicine
    }

    public static class ResourceUtils
    {
        public static ResourceType[] resourceTypes = (ResourceType[]) Enum.GetValues(typeof(ResourceType));
        
        public static ResourceType getTypeFromName(String name)
        {
            int index = Array.FindIndex(Enum.GetNames(typeof(ResourceType)), 
                foundName => foundName.EqualsIgnoreCase(name));
            
            if (index == -1)
            {
                Log.Warning("Unknown resource type " + name);
            }

            return resourceTypes[index];
        }
    }
}