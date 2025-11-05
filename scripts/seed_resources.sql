
-- ================================================================
-- PostgreSQL Seed Data Script for ResourcesManager
-- ================================================================
-- This script seeds all entities in Business.DataModel folders
-- All tenant-scoped entities use TenantId = 1
-- Execution order respects foreign key dependencies
-- ================================================================

BEGIN;

-- ================================================================
-- TENANT ENTITIES (Business.DataModel.Tenants)
-- ================================================================

-- 1. TenantTypes (no dependencies)
INSERT INTO "TenantsTypes" ("Id", "Code", "Alias", "Description", "UtcCreated")
VALUES 
    (1, 'ENTERPRISE', 'Enterprise', 'Enterprise-level tenant', NOW()),
    (2, 'DEPARTMENT', 'Department', 'Department-level tenant', NOW()),
    (3, 'TEAM', 'Team', 'Team-level tenant', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 2. Tenants (depends on TenantTypes)
INSERT INTO "Tenants" ("Id", "TenantTypeId", "Code", "Alias", "Description", "UtcCreated")
VALUES 
    (1, 1, 'MAIN', 'Main Tenant', 'Primary tenant for the organization', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 3. TenantHierarchy (depends on Tenants)
-- Example: If you need hierarchical tenants, add them here
-- INSERT INTO "TenantsHierarchy" ("Id", "TenantParentId", "TenantChildId", "UtcCreated")
-- VALUES 
--     (1, 1, 2, NOW());

-- 4. OrganizationTypes (tenant-scoped)
INSERT INTO "OrganizationTypes" ("Id", "TenantId", "Code", "Alias", "Description", "UtcCreated")
VALUES 
    (1, 1, 'CORP', 'Corporation', 'Corporate organization', NOW()),
    (2, 1, 'DEPT', 'Department', 'Department organization', NOW()),
    (3, 1, 'DIV', 'Division', 'Division organization', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 5. Organizations (depends on OrganizationTypes)
INSERT INTO "Organizations" ("Id", "TenantId", "OrganizationTypeId", "Code", "Alias", "Description", "UtcCreated")
VALUES 
    (1, 1, 1, 'ORG001', 'Head Office', 'Main organizational unit', NOW()),
    (2, 1, 2, 'ORG002', 'IT Department', 'Information Technology department', NOW()),
    (3, 1, 2, 'ORG003', 'HR Department', 'Human Resources department', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 6. OrganizationHierarchy (depends on Organizations)
INSERT INTO "OrganizationHierarchies" ("Id", "TenantId", "OrganizationParentId", "OrganizationChildId", "UtcCreated")
VALUES 
    (1, 1, 1, 2, NOW()),
    (2, 1, 1, 3, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 7. UserTypes (tenant-scoped)
INSERT INTO "UserTypes" ("Id", "TenantId", "OrganizationId", "Name", "Code", "UtcCreated")
VALUES 
    (1, 1, NULL, 'Administrator', 'ADMIN', NOW()),
    (2, 1, NULL, 'Standard User', 'STANDARD', NOW()),
    (3, 1, NULL, 'Guest', 'GUEST', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 8. UserStatuses (tenant-scoped enumerative)
INSERT INTO "UserStatuses" ("Id", "TenantId", "Code", "Description", "UtcCreated")
VALUES 
    (1, 1, 'ACTIVE', 'User is active and can access the system', NOW()),
    (2, 1, 'INACTIVE', 'User is inactive and cannot access the system', NOW()),
    (3, 1, 'SUSPENDED', 'User account is temporarily suspended', NOW()),
    (4, 1, 'PENDING', 'User registration is pending approval', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 9. Users (depends on Organizations, UserTypes)
INSERT INTO "Users" ("Id", "TenantId", "OrganizationId", "UserTypeId", "Username", "Code", "Description", "UtcCreated")
VALUES 
    (1, 1, 1, 1, 'admin', 'USR001', 'System Administrator', NOW()),
    (2, 1, 2, 2, 'john.doe', 'USR002', 'IT Department User', NOW()),
    (3, 1, 3, 2, 'jane.smith', 'USR003', 'HR Department User', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 10. UserStatusHistory (depends on Users, UserStatuses)
INSERT INTO "UserStatusHistories" ("Id", "TenantId", "UserId", "UserStatusId", "UtcCreated", "Notes")
VALUES 
    (1, 1, 1, 1, NOW(), 'Initial status - Active'),
    (2, 1, 2, 1, NOW(), 'Initial status - Active'),
    (3, 1, 3, 1, NOW(), 'Initial status - Active')
ON CONFLICT ("Id") DO NOTHING;

-- 11. UserGroups (tenant-scoped)
INSERT INTO "UserGroups" ("Id", "TenantId", "OrganizationId", "Name", "Alias", "FactoryCode", "Description", "UtcCreated")
VALUES 
    (1, 1, NULL, 'Administrators', 'Admins', 'GRP_ADMIN', 'System administrators group', NOW()),
    (2, 1, 2, 'IT Team', 'IT', 'GRP_IT', 'IT department team', NOW()),
    (3, 1, 3, 'HR Team', 'HR', 'GRP_HR', 'HR department team', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 12. UserUserGroups (depends on Users, UserGroups)
INSERT INTO "UserUserGroups" ("Id", "TenantId", "OrganizationId", "UserGroupId", "UserId", "UtcCreated")
VALUES 
    (1, 1, 1, 1, 1, NOW()),
    (2, 1, 2, 2, 2, NOW()),
    (3, 1, 3, 3, 3, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 13. RoleTypes (tenant-scoped)
INSERT INTO "RoleTypes" ("Id", "TenantId", "OrganizationId", "Name", "Alias", "FactoryCode", "Description", "UtcCreated")
VALUES 
    (1, 1, NULL, 'System Role', 'System', 'ROLE_SYS', 'System-level role', NOW()),
    (2, 1, NULL, 'Organizational Role', 'Org', 'ROLE_ORG', 'Organization-level role', NOW()),
    (3, 1, NULL, 'Functional Role', 'Func', 'ROLE_FUNC', 'Functional role', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 14. Roles (depends on Organizations, RoleTypes)
INSERT INTO "Roles" ("Id", "TenantId", "OrganizationId", "RoleTypeId", "RoleName", "Alias", "FactoryCode", "Description", "UtcCreated")
VALUES 
    (1, 1, NULL, 1, 'Super Admin', 'SuperAdmin', 'ROLE_SUPER', 'Full system access', NOW()),
    (2, 1, 2, 2, 'IT Manager', 'ITMgr', 'ROLE_IT_MGR', 'IT department manager', NOW()),
    (3, 1, 3, 2, 'HR Manager', 'HRMgr', 'ROLE_HR_MGR', 'HR department manager', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 15. RoleUsers (depends on Roles, Users)
INSERT INTO "RoleUsers" ("Id", "TenantId", "OrganizationId", "RoleId", "UserId", "UtcCreated")
VALUES 
    (1, 1, NULL, 1, 1, NOW()),
    (2, 1, 2, 2, 2, NOW()),
    (3, 1, 3, 3, 3, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 16. RoleGroups (depends on Roles, UserGroups)
INSERT INTO "RoleGroups" ("Id", "TenantId", "OrganizationId", "RoleId", "UserGroupId", "UtcCreated")
VALUES 
    (1, 1, NULL, 1, 1, NOW()),
    (2, 1, 2, 2, 2, NOW()),
    (3, 1, 3, 3, 3, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- ================================================================
-- RESOURCE ENTITIES (Business.DataModel.Resources)
-- ================================================================

-- 17. ResourceTypes (tenant-scoped)
INSERT INTO "ResourceTypes" ("Id", "TenantId", "OrganizationId", "Code", "Description", "HasTable", "UtcCreated")
VALUES 
    (1, 1, NULL, 'HARDWARE', 'Hardware resources', 'true', NOW()),
    (2, 1, NULL, 'SOFTWARE', 'Software licenses', 'true', NOW()),
    (3, 1, NULL, 'FACILITY', 'Facilities and spaces', 'false', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 18. ResourceStatuses (tenant-scoped enumerative)
INSERT INTO "ResourceStatuses" ("Id", "TenantId", "Code", "Description", "UtcCreated")
VALUES 
    (1, 1, 'AVAILABLE', 'Resource is available for use', NOW()),
    (2, 1, 'IN_USE', 'Resource is currently in use', NOW()),
    (3, 1, 'MAINTENANCE', 'Resource is under maintenance', NOW()),
    (4, 1, 'RETIRED', 'Resource has been retired', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 19. Resources (depends on Organizations, ResourceTypes)
INSERT INTO "Resources" ("Id", "TenantId", "OrganizationId", "Code", "Description", "ResourceTypeId", "UtcCreated")
VALUES 
    (1, 1, 2, 'RES001', 'Dell Laptop - XPS 15', 1, NOW()),
    (2, 1, 2, 'RES002', 'Microsoft Office 365 License', 2, NOW()),
    (3, 1, 1, 'RES003', 'Conference Room A', 3, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 20. ResourceStatusHistory (depends on Resources, ResourceStatuses)
INSERT INTO "ResourceStatusHistories" ("Id", "TenantId", "ResourceId", "ResourceStatusId", "UtcCreated", "Notes")
VALUES 
    (1, 1, 1, 1, NOW(), 'Initial status - Available'),
    (2, 1, 2, 2, NOW(), 'Assigned to user'),
    (3, 1, 3, 1, NOW(), 'Initial status - Available')
ON CONFLICT ("Id") DO NOTHING;

-- 21. ResourceGroups (tenant-scoped)
INSERT INTO "ResourceGroups" ("Id", "TenantId", "OrganizationId", "Name", "Description", "UtcCreated")
VALUES 
    (1, 1, 2, 'IT Equipment', 'Information Technology equipment pool', NOW()),
    (2, 1, 2, 'Software Licenses', 'Software license pool', NOW()),
    (3, 1, 1, 'Meeting Spaces', 'Conference and meeting rooms', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 22. ResourceResourceGroups (depends on Resources, ResourceGroups)
INSERT INTO "ResourceResourceGroups" ("Id", "TenantId", "ResourceId", "ResourceGroupId", "UtcCreated")
VALUES 
    (1, 1, 1, 1, NOW()),
    (2, 1, 2, 2, NOW()),
    (3, 1, 3, 3, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- ================================================================
-- RESET SEQUENCES
-- ================================================================
-- Ensure sequences are set to the next available ID

SELECT setval(pg_get_serial_sequence('"TenantsTypes"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "TenantsTypes";
SELECT setval(pg_get_serial_sequence('"Tenants"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "Tenants";
SELECT setval(pg_get_serial_sequence('"TenantsHierarchy"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "TenantsHierarchy";
SELECT setval(pg_get_serial_sequence('"OrganizationTypes"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "OrganizationTypes";
SELECT setval(pg_get_serial_sequence('"Organizations"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "Organizations";
SELECT setval(pg_get_serial_sequence('"OrganizationHierarchies"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "OrganizationHierarchies";
SELECT setval(pg_get_serial_sequence('"UserTypes"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "UserTypes";
SELECT setval(pg_get_serial_sequence('"UserStatuses"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "UserStatuses";
SELECT setval(pg_get_serial_sequence('"Users"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "Users";
SELECT setval(pg_get_serial_sequence('"UserStatusHistories"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "UserStatusHistories";
SELECT setval(pg_get_serial_sequence('"UserGroups"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "UserGroups";
SELECT setval(pg_get_serial_sequence('"UserUserGroups"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "UserUserGroups";
SELECT setval(pg_get_serial_sequence('"RoleTypes"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "RoleTypes";
SELECT setval(pg_get_serial_sequence('"Roles"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "Roles";
SELECT setval(pg_get_serial_sequence('"RoleUsers"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "RoleUsers";
SELECT setval(pg_get_serial_sequence('"RoleGroups"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "RoleGroups";
SELECT setval(pg_get_serial_sequence('"ResourceTypes"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "ResourceTypes";
SELECT setval(pg_get_serial_sequence('"ResourceStatuses"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "ResourceStatuses";
SELECT setval(pg_get_serial_sequence('"Resources"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "Resources";
SELECT setval(pg_get_serial_sequence('"ResourceStatusHistories"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "ResourceStatusHistories";
SELECT setval(pg_get_serial_sequence('"ResourceGroups"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "ResourceGroups";
SELECT setval(pg_get_serial_sequence('"ResourceResourceGroups"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "ResourceResourceGroups";

COMMIT;

-- ================================================================
-- VERIFICATION QUERIES
-- ================================================================
-- Uncomment to verify the seed data

-- SELECT * FROM "Tenants";
-- SELECT * FROM "Organizations";
-- SELECT * FROM "Users";
-- SELECT * FROM "Resources";