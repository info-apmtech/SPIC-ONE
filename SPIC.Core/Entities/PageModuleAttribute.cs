using System;

namespace SPIC.Core.Entities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PageModuleAttribute : Attribute
    {
        public string Module { get; }
        public PageModuleAttribute(string module) => Module = module;
    }

    /// <summary>
    /// Marks a PagePermission member as a page the product deliberately OPENS to every
    /// signed-in user, independent of that user's designation - e.g. the Knowledge Community
    /// and the read-only Digital Library.
    /// <para>
    /// This is data, declared next to the permission it describes, so declaring a new
    /// open-to-all page is a single attribute on its enum member and needs no change in
    /// NavMenu, PageGuard, ShellNavigation or LoginState. The flag is read by
    /// PageAuthorization, which is the single place page access is decided.
    /// </para>
    /// <para>
    /// Roles whose authorization model is <see cref="PageAccessModel.DesignationOnly"/>
    /// (i.e. CommonRole) deliberately ignore this flag: for those roles the designation is the
    /// authoritative and only source of page access, so such a page is reachable only when the
    /// designation grants it.
    /// </para>
    /// <para>
    /// Routes that have no PagePermission member at all (so cannot carry this attribute) are
    /// declared in <see cref="PageAuthorization"/> as OpenAccessRoutes.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class OpenToAllAttribute : Attribute
    {
    }
}
