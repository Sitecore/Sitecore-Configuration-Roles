namespace Sitecore.Configuration.Roles.UnitTests
{
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using FluentAssertions;

  [TestClass]
  public class RoleConfigurationHelper_ValidateRoles
  {
    [TestMethod]
    public void Authoring()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring" })
        .Should().BeNull();
    }

    [TestMethod]
    public void Processing()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "processing" })
        .Should().BeNull();
    }

    [TestMethod]
    public void Reporting()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "reporting" })
        .Should().BeNull();
    }

    [TestMethod]
    public void Delivery()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "delivery" })
        .Should().BeNull();
    }

    [TestMethod]
    public void DeliveryOne()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "delivery-1" })
        .Should().BeNull();
    }

    [TestMethod]
    public void DeliveryOneTwo()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "delivery-1", "delivery-2" })
        .Should().BeNull();
    }

    [TestMethod]
    public void AuthoringDelivery()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "delivery" })
        .Should().Be("The delivery role is specified alongside with authoring which is not supported.");
    }

    [TestMethod]
    public void AuthoringDeliveryOne()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "delivery-1" })
        .Should().BeNull();
    }

    [TestMethod]
    public void AllInOneDeliveryOne()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "processing", "reporting", "delivery-1" })
        .Should().BeNull();
    }

    [TestMethod]
    public void AllInOneDeliveryTwo()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "processing", "reporting", "delivery-2" })
        .Should().BeNull();
    }

    [TestMethod]
    public void AuthoringProcessingReporting()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "processing", "reporting" })
        .Should().BeNull();
    }
  }
}
