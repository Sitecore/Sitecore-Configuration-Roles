namespace Sitecore.Configuration.Roles
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Xml;
  using Sitecore.Diagnostics;
  using Sitecore.Xml.Patch;

  /// <summary>
  /// A set of utility functions to apply XML patches.
  /// </summary>
  public static class XmlPatchUtils
  {
    #region Public Methods

    /// <summary>
    /// Assigns the attributes.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="attributes">The attributes.</param>
    public static void AssignAttributes([NotNull] XmlNode target, [NotNull] IEnumerable<IXmlNode> attributes)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(attributes, "attributes");

      foreach (IXmlNode attribute in attributes)
      {
        Assert.IsNotNull(target.Attributes, "attributes");
        XmlAttribute targetAttribute = target.Attributes[attribute.LocalName, attribute.NamespaceURI];
        if (targetAttribute == null)
        {
          Assert.IsNotNull(target.OwnerDocument, "document");
          targetAttribute = target.OwnerDocument.CreateAttribute(MakeName(attribute.Prefix, attribute.LocalName), attribute.NamespaceURI);
          target.Attributes.Append(targetAttribute);
        }

        targetAttribute.Value = attribute.Value;
      }
    }

    /// <summary>
    /// Assigns modification source to the target element.
    /// </summary>
    /// <param name="target">An element in the target document.</param>
    /// <param name="source">Source to take information from. The object must implement <see cref="IXmlSource"/>.</param>
    /// <param name="ns">Namespace context.</param>
    private static void AssignSource([NotNull] XmlNode target, [NotNull] object source, [NotNull] XmlPatchNamespaces ns)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(source, "source");
      Assert.ArgumentNotNull(ns, "ns");

      var xmlSource = source as IXmlSource;
      if (xmlSource == null)
      {
        return;
      }

      string sourceName = xmlSource.SourceName;
      if (string.IsNullOrEmpty(sourceName))
      {
        return;
      }

      string patchPrefix = target.OwnerDocument.GetPrefixOfNamespace(ns.PatchNamespace);
      if (string.IsNullOrEmpty(patchPrefix))
      {
        patchPrefix = "patch";
        XmlNode rootElement = target.OwnerDocument.DocumentElement;
        XmlAttribute xmlnsDefinition = target.OwnerDocument.CreateAttribute("xmlns:" + patchPrefix);
        xmlnsDefinition.Value = ns.PatchNamespace;
        rootElement.Attributes.Append(xmlnsDefinition);
      }

      XmlAttribute attribute = target.Attributes["source", ns.PatchNamespace];
      if (attribute == null)
      {
        attribute = target.OwnerDocument.CreateAttribute(patchPrefix, "source", ns.PatchNamespace);
        target.Attributes.Append(attribute);
      }

      attribute.Value = sourceName;
    }

    /// <summary>
    /// Copies the attributes.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="patch">The patch.</param>
    /// <param name="ns">The namespace.</param>
    public static void CopyAttributes([NotNull] XmlNode target, [NotNull] IXmlElement patch, [NotNull] XmlPatchNamespaces ns)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(patch, "patch");
      Assert.ArgumentNotNull(ns, "ns");

      IEnumerable<IXmlNode> attributes = patch.GetAttributes().Where(a => a.NamespaceURI != ns.PatchNamespace && (a.NamespaceURI != ns.RoleNamespace || a.LocalName == "define") && a.NamespaceURI != "http://www.w3.org/2000/xmlns/");
      IEnumerable<IXmlNode> values = attributes.Select(a =>
      {
        string targetNamespace = a.NamespaceURI == ns.SetNamespace ? string.Empty : a.NamespaceURI;
        return new XmlNodeInfo
        {
          NodeType = a.NodeType, 
          NamespaceURI = targetNamespace, 
          LocalName = a.LocalName, 
          Value = a.Value, 
          Prefix = a.Prefix
        } as IXmlNode;
      });

      if (values.Any())
      {
        AssignAttributes(target, values);
        AssignSource(target, patch, ns);
      }
    }

    /// <summary>
    /// Determines whether the specified value is an XML patch.
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified value is an XML patch; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsXmlPatch([NotNull] string value)
    {
      Assert.ArgumentNotNull(value, "value");

      return value.IndexOf("p:p=\"1\"", StringComparison.InvariantCulture) >= 0;
    }

    /// <summary>
    /// Merges the nodes.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="patch">The patch.</param>
    /// <param name="ns">The namespace.</param>
    public static void MergeNodes([NotNull] XmlNode target, [NotNull] IXmlElement patch, [NotNull] XmlPatchNamespaces ns)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(patch, "patch");
      Assert.ArgumentNotNull(ns, "ns");

      if (target.NamespaceURI != patch.NamespaceURI || target.LocalName != patch.LocalName)
      {
        return;
      }
 
      var exit = false;
      foreach (var attribute in patch.GetAttributes())
      {
        if (exit)
        {
          continue;
        }

        if (attribute.NamespaceURI == ns.RoleNamespace && !RoleConfigurationHelper.ProcessRolesNamespace(attribute))
        {
          // we need to finish enumerating attributes to avoid reader problem
          exit = true;
        }
      }

      if (exit)
      {
        foreach(var node in patch.GetChildren())
        {
          // we need to get children to avoid reader problem
        }

        return;
      }

      CopyAttributes(target, patch, ns);

      MergeChildren(target, patch, ns, false);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Inserts the child.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <param name="child">The child.</param>
    /// <param name="operation">The operation.</param>
    /// <returns>The insert child.</returns>
    /// <exception cref="Exception">Insert operation is not implemented</exception>
    private static bool InsertChild([NotNull] XmlNode parent, [NotNull] XmlNode child, [CanBeNull] InsertOperation operation)
    {
      Assert.ArgumentNotNull(parent, "parent");
      Assert.ArgumentNotNull(child, "child");

      if (operation == null)
      {
        parent.AppendChild(child);
        return true;
      }

      XmlNode reference = parent.SelectSingleNode(operation.Reference);
      if (reference == null)
      {
        parent.AppendChild(child);
        return false;
      }

      switch (operation.Disposition)
      {
        case 'b':
          parent.InsertBefore(child, reference);
          return true;
        case 'a':
          parent.InsertAfter(child, reference);
          return true;
        case 'i':
          parent.InsertBefore(child, reference);
          parent.RemoveChild(reference);
          return true;
        default:
          throw new Exception("Insert operation is not implemented");
      }
    }

    /// <summary>
    /// Makes the name.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <param name="localName">Name of the local.</param>
    /// <returns>The full name.</returns>
    [NotNull]
    private static string MakeName([CanBeNull] string prefix, [NotNull] string localName)
    {
      Assert.ArgumentNotNull(localName, "localName");

      return string.IsNullOrEmpty(prefix) ? localName : prefix + ":" + localName;
    }

    /// <summary>
    /// Appends the children.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="patch">The patch.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="targetWasInserted">if set to <c>true</c> means that <paramref name="target"/> has been inserted in the target document earlier by this patch.</param>
    private static void MergeChildren([NotNull] XmlNode target, [NotNull] IXmlElement patch, [NotNull] XmlPatchNamespaces ns, bool targetWasInserted)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(patch, "patch");
      Assert.ArgumentNotNull(ns, "ns");

      var exit = false;
      string savedComment = null;
      var pendingOperations = new Stack<InsertOperation>();

      // copy child nodes
      foreach (IXmlElement node in patch.GetChildren())
      {
        if(exit)
        {
          continue;
        }

        if (node.NodeType == XmlNodeType.Text)
        {
          target.InnerText = node.Value;
          continue;
        }

        if (node.NodeType == XmlNodeType.Comment)
        {
          savedComment = node.Value;
          continue;
        }

        if (node.NodeType != XmlNodeType.Element)
        {
          continue;
        }

        if (node.NamespaceURI == ns.PatchNamespace)
        {
          ProcessConfigNode(target, node);
          continue;
        }

        var queryAttributes = new List<IXmlNode>();
        var setAttributes = new List<IXmlNode>();

        InsertOperation operation = null;

        foreach (IXmlNode attribute in node.GetAttributes())
        {
          if (exit)
          {
            continue;
          }

          if (attribute.NamespaceURI == ns.RoleNamespace)
          {
            if(!RoleConfigurationHelper.ProcessRolesNamespace(node))
            {
              // we need to finish enumerating attributes to avoid reader problem
              exit = true;
              pendingOperations.Clear();
            }

            continue;
          }

          if (attribute.NamespaceURI == ns.PatchNamespace)
          {
            switch (attribute.LocalName)
            {
              case "b":
              case "before":
              case "a":
              case "after":
              case "i":
              case "instead":
                operation = new InsertOperation
                {
                  Reference = attribute.Value, 
                  Disposition = attribute.LocalName[0]
                };
                break;
            }

            continue;
          }

          if (attribute.NamespaceURI == ns.SetNamespace)
          {
            setAttributes.Add(new XmlNodeInfo
            {
              NodeType = attribute.NodeType, 
              NamespaceURI = string.Empty, // "set" NS translates into an empty NS
              LocalName = attribute.LocalName, 
              Prefix = string.Empty, 
              Value = attribute.Value
            });
            continue;
          }

          if (attribute.Prefix != "xmlns")
          {
            queryAttributes.Add(new XmlNodeInfo
            {
              NodeType = attribute.NodeType, 
              NamespaceURI = attribute.NamespaceURI, 
              LocalName = attribute.LocalName, 
              Prefix = attribute.Prefix, 
              Value = attribute.Value
            });
          }
        }

        if (exit)
        {
          continue;
        }

        var nsManager = new XmlNamespaceManager(new NameTable());

        var predicateBuilder = new StringBuilder();
        var added = false;
        foreach (var a in queryAttributes)
        {
          if (added)
          {
            predicateBuilder.Append(" and ");
          }

          if (a.Prefix != null && string.IsNullOrEmpty(nsManager.LookupPrefix(a.Prefix)))
          {
            nsManager.AddNamespace(a.Prefix, a.NamespaceURI);
          }

          predicateBuilder.Append("@" + MakeName(a.Prefix, a.LocalName) + "=\"" + a.Value + "\"");
          added = true;
        }

        if (node.Prefix != null && string.IsNullOrEmpty(nsManager.LookupPrefix(node.Prefix)))
        {
          nsManager.AddNamespace(node.Prefix, node.NamespaceURI);
        }

        XmlNode targetChild = null;
        bool created = false;

        if (!targetWasInserted)
        {
          string predicate = MakeName(node.Prefix, node.LocalName);

          var expression = predicateBuilder.ToString();
          if (expression.Length > 0)
          {
            predicate = predicate + "[" + expression + "]";
          }

          targetChild = target.SelectSingleNode(predicate, nsManager);
        }

        if (targetChild == null)
        {
          Assert.IsNotNull(target.OwnerDocument, "document");
          targetChild = target.OwnerDocument.CreateElement(MakeName(node.Prefix, node.LocalName), node.NamespaceURI);
          created = true;
          if (!InsertChild(target, targetChild, operation) && operation != null)
          {
            operation.Node = targetChild;
            pendingOperations.Push(operation);
          }

          AssignAttributes(targetChild, queryAttributes);
        }
        else if (operation != null)
        {
          if (!InsertChild(target, targetChild, operation))
          {
            operation.Node = targetChild;
            pendingOperations.Push(operation);
          }
        }

        if (savedComment != null)
        {
          Assert.IsNotNull(targetChild.OwnerDocument, "document");
          XmlComment comment = targetChild.OwnerDocument.CreateComment(savedComment);
          Assert.IsNotNull(targetChild.ParentNode, "parent");
          targetChild.ParentNode.InsertBefore(comment, targetChild);
          savedComment = null;
        }

        AssignAttributes(targetChild, setAttributes);
        MergeChildren(targetChild, node, ns, /*targetWasInserted = */ created);
        if ((created || setAttributes.Any()) && !targetWasInserted)
        {
          AssignSource(targetChild, node, ns);
        }
      }

      while (pendingOperations.Count > 0)
      {
        InsertOperation operation = pendingOperations.Pop();
        Assert.IsNotNull(operation.Node.ParentNode, "parent");
        InsertChild(operation.Node.ParentNode, operation.Node, operation);
      }
    }

    /// <summary>
    /// Processes the config node.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="command">The command.</param>
    private static void ProcessConfigNode([NotNull] XmlNode target, [NotNull] IXmlElement command)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(command, "command");

      Dictionary<string, string> parameters = command.GetAttributes().ToDictionary(a => a.LocalName, a => a.Value);
      switch (command.LocalName)
      {
        case "a":
        case "attribute":
          string ns;
          if (!parameters.TryGetValue("ns", out ns))
          {
            ns = null;
          }

          Assert.IsNotNull(target.Attributes, "attributes");
          XmlAttribute attribute = target.Attributes[ns, parameters["name"]];
          if (attribute == null)
          {
            Assert.IsNotNull(target.OwnerDocument, "document");
            attribute = target.OwnerDocument.CreateAttribute(parameters["name"], ns);
            target.Attributes.Append(attribute);
          }

          string value;
          if (!parameters.TryGetValue("value", out value))
          {
            value = string.Empty;
          }

          foreach (IXmlElement node in command.GetChildren())
          {
            value = node.Value ?? value;
          }

          attribute.Value = value;
          break;
        case "d":
        case "delete":
          Assert.IsNotNull(target.ParentNode, "parent");
          target.ParentNode.RemoveChild(target);
          break;
      }
    }

    #endregion

    /// <summary>
    /// The insert operation.
    /// </summary>
    private class InsertOperation
    {
      #region Properties

      /// <summary>
      /// Gets or sets Disposition.
      /// </summary>
      public char Disposition { get; set; }

      /// <summary>
      /// Gets or sets Node.
      /// </summary>
      public XmlNode Node { get; set; }

      /// <summary>
      /// Gets or sets Reference.
      /// </summary>
      public string Reference { get; set; }

      /// <summary>
      /// Gets or sets a value indicating whether Succeeded.
      /// </summary>
      public bool Succeeded { get; set; }

      #endregion
    }
  }
}