using System;

namespace IPCSoftware.Shared.Attribute
{
    [AttributeUsage(AttributeTargets.Field)]
    public class GroupAttribute : System.Attribute
    {
        public string Name { get; }
        public GroupAttribute(string name) => Name = name;
    }
}