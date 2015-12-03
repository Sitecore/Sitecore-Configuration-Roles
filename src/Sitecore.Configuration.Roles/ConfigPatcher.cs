namespace Sitecore.Configuration.Roles
{
  using System.IO;
  using System.Text;
  using System.Xml;
  using Sitecore.Xml.Patch;

  /// <summary>
  ///   A helper class to merge XML configuration files.
  /// </summary>
  internal class ConfigPatcher
  {
    /// <summary>
    ///   XML namespace for configuration management nodes and attributes
    /// </summary>
    public const string ConfigurationNamespace = "http://www.sitecore.net/xmlconfig/";

    public const string SetNamespace = "http://www.sitecore.net/xmlconfig/set/";

    XmlNode _root;
    XmlPatcher _patcher = new XmlPatcher(SetNamespace, ConfigurationNamespace);

    /// <summary>
    /// Initializes a new instance of the <see cref="Sitecore.Configuration.Roles.ConfigPatcher"/> class.
    /// </summary>
    /// <param name="node">The node.</param>
    public ConfigPatcher(XmlNode node)
    {
      this._root = node;
    }

    /// <summary>
    /// Gets the document.
    /// </summary>
    /// <value>The document.</value>
    public XmlNode Document
    {
      get
      {
        return this._root;
      }
    }

    /// <summary>
    /// Applies the patch.
    /// </summary>
    /// <param name="filename">The filename.</param>
    public void ApplyPatch(string filename)
    {
      using (StreamReader reader = new StreamReader(filename, Encoding.UTF8))
      {
        this.ApplyPatch(reader, Path.GetFileName(filename));
      }
    }

    /// <summary>
    /// Applies the patch.
    /// </summary>
    /// <param name="patch">The patch.</param>
    public void ApplyPatch(TextReader patch)
    {
      this.ApplyPatch(patch, string.Empty);
    }

    /// <summary>
    /// Applies the patch and marks changes with a source.
    /// </summary>
    /// <param name="patch">A stream with the XML patch.</param>
    /// <param name="sourceName">Name of the source of the stream (e.g. a file name)</param>
    public void ApplyPatch(TextReader patch, string sourceName)
    {
      XmlTextReader reader = new XmlTextReader(patch);
      reader.WhitespaceHandling = WhitespaceHandling.None;
      reader.MoveToContent();
      reader.ReadStartElement("configuration");
      this._patcher.Merge(this._root, new XmlReaderSource(reader, sourceName));
      reader.ReadEndElement();
    }
  }
}