=================================================================================
Sitecore CMS 8.1 rev. 151003 POC 447737-1 (CONFIGURATION ROLES SUPPORT)
=================================================================================

In this POC Sitecore configuration engine was extended with two simple commands
and modified configuration files that use them:

1.  Define Role Command

    This command can be used only once to avoid accidential misconfiguration. 
    It defines pipe-separated "white list" of roles this Sitecore instance have. 
    
    Example:
    
    <configuration xmlns:role="http://www.sitecore.net/xmlconfig/role/">
      <sitecore role:define="role1|role2|..." />
    </configuration>
    
    Roles can be any string, but out of the box this POC offers these roles:
    * authoring
    * publishing
    * reporting
    * processing
    * delivery
    
    These roles are described below. 
    
    Note: 
    
    it is recommended to use only in pre-defined ! Role-Define-*.config files.
    
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
      
      Defines Publishing (Pub) role that allows current Sitecore instance to
      process publishing requests. It can be enabled on the same instance
      with other roles or on a dedicated Sitecore instance.
      
    - reporting
      
      Defines xDB Reporting (Rep) role that fetches reporting data from various 
      data sources to use in Sitecore reporting applications. It can be enabled
      on the same instance with other roles or on a dedicated Sitecore instance.
      
    - processing
      
      Defines xDB Processing (Proc) role. It can be enabled on the same instance 
      with other roles or on a dedicated Sitecore instance.
      
    - delivery
      
      Defines Content Delivery (CD) role that assumes current Sitecore instance
      can be accessed by end-users. It can be enabled on the same instance 
      with other roles or on a dedicated Sitecore instance.
        
=================================================================================
GETTING STARTED
=================================================================================

In the given version of POC (447737-1) only 2 modes are supported:

1. Single (1) all-in-one instance
2. Four (4) or more instances each with single dedicated role (2xCM, 1xPub, 1xRep, 1xProc, 4xCD)

So the single step you need to perform is to choose one of "! Role-Define-*.config" files and
delete all the rest.