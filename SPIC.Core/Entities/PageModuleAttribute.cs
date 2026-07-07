using System;

namespace SPIC.Core.Entities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PageModuleAttribute : Attribute
    {
        public string Module { get; }
        public PageModuleAttribute(string module) => Module = module;
    }
}
