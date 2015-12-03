namespace Sitecore.Configuration.Roles
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Xml;
  using Sitecore.Collections;
  using Sitecore.Diagnostics;
  using Sitecore.Xml;

  /// <summary>
  /// Originally a part of Sitecore.Configuration.Factory which is related to GetConfiguration() method.
  /// </summary>
  internal static class RoleConfigurationFactory
  {
    [NotNull]
    internal static XmlDocument GetConfiguration()
    {
      XmlNode root = ConfigReader.GetConfigNode();

      Assert.IsNotNull(root, "Could not read Sitecore configuration.");

      var config = new XmlDocument();

      config.AppendChild(config.ImportNode(root, true));

      RoleConfigurationHelper.LoadAppSetting();

      ExpandIncludeFiles(config.DocumentElement, new Hashtable());

      LoadAutoIncludeFiles(config.DocumentElement);

      ReplaceGlobalVariables(config.DocumentElement);

      RoleConfigurationHelper.Validate();

      return config;
    }

    /// <summary>
    /// Expands the include files embedded in the configuration document.
    /// </summary>
    /// <param name="rootNode">
    /// The root node.
    /// </param>
    /// <param name="cycleDetector">
    /// The cycle detector.
    /// </param>
    private static void ExpandIncludeFiles([NotNull] XmlNode rootNode, [NotNull] Hashtable cycleDetector)
    {
      Assert.ArgumentNotNull(rootNode, "rootNode");
      Assert.ArgumentNotNull(cycleDetector, "cycleDetector");

      if (rootNode.LocalName == "sc.include")
      {
        ExpandIncludeFile(rootNode, cycleDetector);
        return;
      }

      XmlNodeList nodes = rootNode.SelectNodes(".//sc.include");

      for (int n = 0; n < nodes.Count; n++)
      {
        ExpandIncludeFile(nodes[n], cycleDetector);
      }
    }

    /// <summary>
    /// Loads the auto include files.
    /// </summary>
    /// <param name="element">
    /// The element.
    /// </param>
    private static void LoadAutoIncludeFiles([NotNull] XmlNode element)
    {
      Assert.ArgumentNotNull(element, "element");

      var patcher = new ConfigPatcher(element);
      LoadAutoIncludeFiles(patcher, MainUtil.MapPath("/App_Config/Sitecore/Components"));
      LoadAutoIncludeFiles(patcher, MainUtil.MapPath("/App_Config/Include"));
    }

    /// <summary>
    /// Loads the auto include files.
    /// </summary>
    /// <param name="patcher">
    /// The patcher.
    /// </param>
    /// <param name="folder">
    /// The folder.
    /// </param>
    private static void LoadAutoIncludeFiles([NotNull] ConfigPatcher patcher, [NotNull] string folder)
    {
      Assert.ArgumentNotNull(patcher, "patcher");
      Assert.ArgumentNotNull(folder, "folder");

      try
      {
        if (Directory.Exists(folder))
        {
          foreach (string filename in Directory.GetFiles(folder, "*.config"))
          {
            try
            {
              if ((File.GetAttributes(filename) & FileAttributes.Hidden) == 0)
              {
                patcher.ApplyPatch(filename);
              }
            }
            catch (Exception e)
            {
              Log.Error("Could not load configuration file: " + filename + ": " + e, typeof(Factory));
            }
          }

          foreach (string subfolder in Directory.GetDirectories(folder))
          {
            try
            {
              if ((File.GetAttributes(subfolder) & FileAttributes.Hidden) == 0)
              {
                LoadAutoIncludeFiles(patcher, subfolder);
              }
            }
            catch (Exception e)
            {
              Log.Error("Could not scan configuration folder " + subfolder + " for files: " + e, typeof(Factory));
            }
          }
        }
      }
      catch (Exception e)
      {
        Log.Error("Could not scan configuration folder " + folder + " for files: " + e, typeof(Factory));
      }
    }

    /// <summary>
    /// Replaces global variables.
    /// </summary>
    /// <param name="rootNode">
    /// The root node.
    /// </param>
    private static void ReplaceGlobalVariables([NotNull] XmlNode rootNode)
    {
      Assert.ArgumentNotNull(rootNode, "rootNode");

      XmlNodeList nodes = rootNode.SelectNodes(".//sc.variable");

      var variables = new StringDictionary();

      foreach (XmlAttribute attribute in rootNode.Attributes)
      {
        string name = attribute.Name;
        string value = StringUtil.GetString(attribute.Value);

        if (name.Length > 0)
        {
          string variable = "$(" + name + ")";

          variables[variable] = value;
        }
      }

      for (int n = 0; n < nodes.Count; n++)
      {
        string name = XmlUtil.GetAttribute("name", nodes[n]);
        string value = XmlUtil.GetAttribute("value", nodes[n]);

        if (name.Length > 0)
        {
          string variable = "$(" + name + ")";

          variables[variable] = value;
        }
      }

      if (variables.Count == 0)
      {
        return;
      }

      ReplaceGlobalVariables(rootNode, variables);
    }

    /// <summary>
    /// Replaces global variables.
    /// </summary>
    /// <param name="node">
    /// The root node.
    /// </param>
    /// <param name="variables">
    /// The variables.
    /// </param>
    private static void ReplaceGlobalVariables([NotNull] XmlNode node, [NotNull] StringDictionary variables)
    {
      Assert.ArgumentNotNull(node, "node");
      Assert.ArgumentNotNull(variables, "variables");

      foreach (XmlAttribute attribute in node.Attributes)
      {
        string value = attribute.Value;

        if (value.IndexOf('$') < 0)
        {
          continue;
        }

        foreach (string variable in variables.Keys)
        {
          value = value.Replace(variable, variables[variable]);
        }

        attribute.Value = value;
      }

      foreach (XmlNode childNode in node.ChildNodes)
      {
        if (childNode.NodeType != XmlNodeType.Element)
        {
          continue;
        }

        ReplaceGlobalVariables(childNode, variables);
      }
    }

    /// <summary>
    /// Expands the include file represented by a single configuration element (node).
    /// </summary>
    /// <param name="xmlNode">
    /// The XML node.
    /// </param>
    /// <param name="cycleDetector">
    /// The cycle detector.
    /// </param>
    private static void ExpandIncludeFile([NotNull] XmlNode xmlNode, [NotNull] Hashtable cycleDetector)
    {
      Assert.ArgumentNotNull(xmlNode, "xmlNode");
      Assert.ArgumentNotNull(cycleDetector, "cycleDetector");

      string path = Factory.GetAttribute("file", xmlNode, null).ToLowerInvariant();

      if (path.Length == 0)
      {
        return;
      }

      Assert.IsTrue(
        !cycleDetector.ContainsKey(path),
        "Cycle detected in configuration include files. The file '{0}' is being included directly or indirectly in a way that causes a cycle to form.",
        path);

      XmlDocument document = XmlUtil.LoadXmlFile(path);

      if (document.DocumentElement == null)
      {
        return;
      }

      XmlNode destinationNode = xmlNode.ParentNode;

      XmlNode fileNode = xmlNode.OwnerDocument.ImportNode(document.DocumentElement, true);

      destinationNode.ReplaceChild(fileNode, xmlNode);

      cycleDetector.Add(path, string.Empty);

      ExpandIncludeFiles(fileNode, cycleDetector);

      cycleDetector.Remove(path);

      // move imported nodes up
      while (fileNode.FirstChild != null)
      {
        destinationNode.AppendChild(fileNode.FirstChild);
      }

      foreach (XmlNode childNode in fileNode.ChildNodes)
      {
        destinationNode.AppendChild(childNode);
      }

      // copy attributes
      XmlUtil.TransferAttributes(fileNode, destinationNode);

      // clean up
      destinationNode.RemoveChild(fileNode);
    }
  }
}