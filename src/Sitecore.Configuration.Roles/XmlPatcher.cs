namespace Sitecore.Configuration.Roles
{
  using System.Xml;
  using Sitecore.Xml.Patch;

  public class XmlPatcher
  {
    /// <summary>
    /// 
    /// </summary>
    XmlPatchNamespaces ns;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlPatcher"/> class.
    /// </summary>
    /// <param name="roleNamespace">The role namespace.</param>
    /// <param name="setNamespace">The set namespace.</param>
    /// <param name="patchNamespace">The config namespace.</param>
    public XmlPatcher(string roleNamespace, string setNamespace, string patchNamespace)
    {
      this.ns = new XmlPatchNamespaces
      {
        RoleNamespace = roleNamespace,
        SetNamespace = setNamespace,
        PatchNamespace = patchNamespace
      };
    }

    /// <summary>
    /// Merges the specified target.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="patch">The patch.</param>
    public void Merge(XmlNode target, IXmlElement patch)
    {
      XmlPatchUtils.MergeNodes(target, patch, this.ns);
    }

    /// <summary>
    /// Merges the specified target.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="reader">The reader.</param>
    public void Merge(XmlNode target, XmlReader reader)
    {
      XmlPatchUtils.MergeNodes(target, new XmlReaderSource(reader), this.ns);
    }

    /// <summary>
    /// Merges the specified target.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="patch">The patch.</param>
    public void Merge(XmlNode target, XmlNode patch)
    {
      XmlPatchUtils.MergeNodes(target, new XmlDomSource(patch), this.ns);
    }
  }
}