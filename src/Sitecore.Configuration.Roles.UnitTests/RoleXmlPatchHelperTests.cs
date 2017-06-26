namespace Sitecore.Configuration.Roles.UnitTests
{
  using System.Xml;
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using Sitecore.Data.Items;
  using Sitecore.Xml.Patch;

  [TestClass]
  public class RoleXmlPatchHelperTests
  {
    [TestMethod]
    public void MergeChildrenTest_Root()
    {
      var sut = new RoleXmlPatchHelperEx();
      var target = ParseXml("<root />");
      var patch = ParseIXml("<root />");
      
      sut.MergeChildren(target, patch, false);
      
      Assert.AreEqual(0, target.ChildNodes.Count);
    }

    [TestMethod]
    public void MergeChildrenTest_AddChild()
    {
      var sut = new RoleXmlPatchHelperEx();
      var target = ParseXml("<root />");
      var patch = ParseIXml("<root><child /></root>");

      sut.MergeChildren(target, patch, false);

      Assert.AreEqual(1, target.ChildNodes.Count);
      Assert.AreEqual("child", target.ChildNodes[0].Name);
    }

    [TestMethod]
    public void MergeChildrenTest_AddChildren()
    {
      var sut = new RoleXmlPatchHelperEx();
      var target = ParseXml("<root />");
      var patch = ParseIXml("<root><child1 /><child2 /></root>");

      sut.MergeChildren(target, patch, false);

      Assert.AreEqual(2, target.ChildNodes.Count);
      Assert.AreEqual("child1", target.ChildNodes[0].Name);
      Assert.AreEqual("child2", target.ChildNodes[1].Name);
    }

    [TestMethod]
    public void MergeChildrenTest_RemoveChild()
    {
      var sut = new RoleXmlPatchHelperEx();
      var target = ParseXml(
      "<configuration>" +
      " <root>" +
      "   <child1 />" +
      " </root>" +
      "</configuration>");
      var patch = ParseIXml(
      "<configuration xmlns:patch=\"http://www.sitecore.net/xmlconfig/\">" +
      " <root>" +
      "   <child1>" +
      "     <patch:delete />" +
      "   </child1>" +
      " </root>" +
      "</configuration>");

      sut.MergeChildren(target, patch, false);

      Assert.AreEqual(0, target.ChildNodes[0].ChildNodes.Count);
    }

    [TestMethod]
    public void MergeChildrenTest_RemoveChild_Role1()
    {
      var sut = new RoleXmlPatchHelperEx("role1");
      var target = ParseXml(
        "<configuration>" +
        " <root>" +
        "   <child1 />" +
        "   <child2 />" +
        "   <child3 />" +
        " </root>" +
        "</configuration>");

      var patch = ParseIXml(
        "<configuration xmlns:patch=\"http://www.sitecore.net/xmlconfig/\" xmlns:role=\"http://www.sitecore.net/xmlconfig/role/\">" +
        "  <root>" +
        "   <child1 role:require=\"role1\">" +
        "     <patch:delete />" +
        "   </child1>" +
        "   <child2 role:require=\"role2\">" +
        "     <patch:delete />" +
        "   </child2>" +
        "   <child3 role:require=\"role3\">" +
        "     <patch:delete />" +
        "   </child3>" +
        " </root>" +
        "</configuration>");

      sut.MergeChildren(target, patch, false);

      Assert.AreEqual(2, target.ChildNodes[0].ChildNodes.Count);
      Assert.AreEqual("child2", target.ChildNodes[0].ChildNodes[0].Name);
      Assert.AreEqual("child3", target.ChildNodes[0].ChildNodes[1].Name);
    }

    [TestMethod]
    public void MergeChildrenTest_RemoveChild_Role2()
    {
      var sut = new RoleXmlPatchHelperEx("role2");
      var target = ParseXml(
        "<configuration>" +
        " <root>" +
        "   <child1 />" +
        "   <child2 />" +
        "   <child3 />" +
        " </root>" +
        "</configuration>");

      var patch = ParseIXml(
        "<configuration xmlns:patch=\"http://www.sitecore.net/xmlconfig/\" xmlns:role=\"http://www.sitecore.net/xmlconfig/role/\">" +
        "  <root>" +
        "   <child1 role:require=\"role1\">" +
        "     <patch:delete />" +
        "   </child1>" +
        "   <child2 role:require=\"role2\">" +
        "     <patch:delete />" +
        "   </child2>" +
        "   <child3 role:require=\"role3\">" +
        "     <patch:delete />" +
        "   </child3>" +
        " </root>" +
        "</configuration>");

      sut.MergeChildren(target, patch, false);

      Assert.AreEqual(2, target.ChildNodes[0].ChildNodes.Count);
      Assert.AreEqual("child1", target.ChildNodes[0].ChildNodes[0].Name);
      Assert.AreEqual("child3", target.ChildNodes[0].ChildNodes[1].Name);
    }

    [TestMethod]
    public void MergeChildrenTest_RemoveChild_Role3()
    {
      var sut = new RoleXmlPatchHelperEx("role3");
      var target = ParseXml(
        "<configuration>" +
        " <root>" +
        "   <child1 />" +
        "   <child2 />" +
        "   <child3 />" +
        " </root>" +
        "</configuration>");

      var patch = ParseIXml(
        "<configuration xmlns:patch=\"http://www.sitecore.net/xmlconfig/\" xmlns:role=\"http://www.sitecore.net/xmlconfig/role/\">" +
        "  <root>" +
        "   <child1 role:require=\"role1\">" +
        "     <patch:delete />" +
        "   </child1>" +
        "   <child2 role:require=\"role2\">" +
        "     <patch:delete />" +
        "   </child2>" +
        "   <child3 role:require=\"role3\">" +
        "     <patch:delete />" +
        "   </child3>" +
        " </root>" +
        "</configuration>");

      sut.MergeChildren(target, patch, false);

      Assert.AreEqual(2, target.ChildNodes[0].ChildNodes.Count);
      Assert.AreEqual("child1", target.ChildNodes[0].ChildNodes[0].Name);
      Assert.AreEqual("child2", target.ChildNodes[0].ChildNodes[1].Name);
    }

    private IXmlElement ParseIXml(string xml)
    {
      return new XmlDomSource(ParseXml(xml));
    }

    private XmlElement ParseXml(string xml)
    {
      var doc = new XmlDocument();
      doc.LoadXml(xml);

      return doc.DocumentElement;
    }

    private class RoleXmlPatchHelperEx : RoleXmlPatchHelper
    {
      private XmlPatchNamespaces Namespace { get; } 
        = new XmlPatchNamespaces
          {
            PatchNamespace = "http://www.sitecore.net/xmlconfig/",
            SetNamespace = "http://www.sitecore.net/xmlconfig/set/"
          };

      internal RoleXmlPatchHelperEx(params string[] roles) : base(new RoleConfigurationHelper(roles))
      {
      }

      internal new void MergeChildren(XmlNode target, IXmlElement patch, bool targetWasInserted)
      {
        base.MergeChildren(target, patch, Namespace, targetWasInserted);
      }
    }
  }
}
