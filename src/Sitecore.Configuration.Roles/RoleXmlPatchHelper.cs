namespace Sitecore.Configuration.Roles
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Xml;
  using Sitecore.Diagnostics;
  using Sitecore.Xml.Patch;

  public class RoleXmlPatchHelper : XmlPatchHelper
  {
    private const string RoleNamespace = "http://www.sitecore.net/xmlconfig/role/";

    private readonly RoleConfigurationHelper RoleConfigurationHelper;

    public RoleXmlPatchHelper(RoleConfigurationHelper roleConfigurationHelper)
    {
      RoleConfigurationHelper = roleConfigurationHelper;
    }

    public override void CopyAttributes([NotNull] XmlNode target, [NotNull] IXmlElement patch, [NotNull] XmlPatchNamespaces ns)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(patch, "patch");
      Assert.ArgumentNotNull(ns, "ns");

      var attributes = patch.GetAttributes().Where(a => a.NamespaceURI != ns.PatchNamespace && (a.NamespaceURI != RoleNamespace) && a.NamespaceURI != "http://www.w3.org/2000/xmlns/");
      var values = attributes.Select(a => ParseXmlNodeInfo(ns, a)).ToArray();

      if (!values.Any())
      {
        return;
      }

      this.AssignAttributes(target, values);
      this.AssignSource(target, patch, ns);
    }

    public override void MergeNodes([NotNull] XmlNode target, [NotNull] IXmlElement patch, [NotNull] XmlPatchNamespaces ns)
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

        if (attribute.NamespaceURI == RoleNamespace && !RoleConfigurationHelper.ProcessRolesNamespace(attribute))
        {
          // we need to finish enumerating attributes to avoid reader problem
          exit = true;
        }
      }

      if (exit)
      {
        foreach (var node in patch.GetChildren())
        {
          // we need to get children to avoid reader problem
        }

        return;
      }

      base.MergeNodes(target, patch, ns);
    }

    protected override void MergeChildren(XmlNode target, IXmlElement patch, XmlPatchNamespaces ns, bool targetWasInserted)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(patch, "patch");
      Assert.ArgumentNotNull(ns, "ns");

      string savedComment = null;
      var pendingOperations = new Stack<InsertOperation>();

      // copy child nodes
      foreach (IXmlElement node in patch.GetChildren())
      {
        var exit = false;

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

          if (attribute.NamespaceURI == RoleNamespace)
          {
            if (!RoleConfigurationHelper.ProcessRolesNamespace(attribute))
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

    [NotNull]
    private static IXmlNode ParseXmlNodeInfo([NotNull] XmlPatchNamespaces ns, [NotNull] IXmlNode a)
    {
      Assert.ArgumentNotNull(ns, "ns");
      Assert.ArgumentNotNull(a, "a");

      var targetNamespace = a.NamespaceURI == ns.SetNamespace ? string.Empty : a.NamespaceURI;

      return new XmlNodeInfo
      {
        NodeType = a.NodeType,
        NamespaceURI = targetNamespace,
        LocalName = a.LocalName,
        Value = a.Value,
        Prefix = a.Prefix
      };
    }
  }
}