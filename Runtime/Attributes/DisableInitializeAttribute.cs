using System;
using JetBrains.Annotations;

namespace DSystem
{
    [AttributeUsage(AttributeTargets.Class), BaseTypeRequired(typeof(DBehaviour))]
    public class DisableInitializeAttribute : Attribute
    {
        
    }
}