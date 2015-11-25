=================================================================================
Sitecore CMS 8.1 rev. 151003 POC 447737-2 (CONFIGURATION ROLES SUPPORT)
=================================================================================

In this POC Sitecore configuration engine was extended with two simple commands
and modified configuration files that use them:

1.  Define Role Command

    This command can be used only once to avoid accidential misconfiguration. 
    It defines pipe-separated "white list" of roles this Sitecore instance have. 
    
    Example 1:
    
    /App_Config/Include/! Role-Define.config
    <configuration xmlns:role="http://www.sitecore.net/xmlconfig/role/">
      <sitecore role:define="role1|role2|..." />
    </configuration>
    
    Example 2:
    
    /web.config
    <configuration>
      <appSettings>
        <add key="role:define" value="role1|role2|..." />
      </appSettings>
    </configuration>
    
    Roles can be any string, but out of the box this POC offers these roles:
    * authoring
    * publishing
    * dedicated-publishing
    * reporting
    * processing
    * delivery
    
    These roles are described below.       
    
2.  Require Role Command

    When this command is applied to a node within Sitecore include config file
    the node will be ignored if the required role is not specified by the Define
    command. 
    
    Example:
    
    <configuration xmlns:role="http://www.sitecore.net/xmlconfig/role/">
      <sitecore>
        <contentSearch>
          ...
              <index id="sitecore_web_index" role:require="authoring|delivery">
                ...

3.  Modified configuration files

    The POC is shipped with modified stock configuration files to make Sitecore
    pre-configured to serve each of these configuration roles. 
    
    There are a number of out-of-box roles that attached configuration files 
    are aware of.                     
    
    - authoring 
      
      Defines Content Management (CM) role that allows editors to use editing
      applications like Content Editor, Page Editor etc.
      
    - publishing
      
      Defines Publishing role that allows current Sitecore instance to
      process publishing requests. It can be enabled on the same instance
      with other roles or on a dedicated Sitecore instance.
      
    - dedicated-publishing
    
      Defines Dedicated Publishing (Pub) role that allows current Sitecore 
      instance to behave as dedicated publishing instance. It can be enabled
      only on single Sitecore instance in solution.
      
    - reporting
      
      Defines xDB Reporting (Rep) role that fetches reporting data from various 
      data sources to use in Sitecore reporting applications. It can be enabled
      on the same instance with other roles or on a dedicated Sitecore instance.
      
    - processing
      
      Defines xDB Processing (Proc) role. It can be enabled on the same instance 
      with other roles or on a dedicated Sitecore instance.
      
    - delivery
      
      Defines Content Delivery (CD) role that assumes current Sitecore instance
      is accessed only by end-users and Sitecore administrators. It cannot be 
      enabled on the same instance with other roles.
        
=================================================================================
GETTING STARTED
=================================================================================

The only step is required to configure the given Sitecore instance is to modify
the ! Role-Define.config file and define roles there - refer to the help in that
file for more details and conventions. 