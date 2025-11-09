-- seed_db.sql
-- Seed data for Postgres databases used by the ResourcesManager service
--
-- Two logical sections follow. Run the Tenants section against the Tenants DB,
-- and the Resources section against the Resource DB. Example using psql:
--   psql -d Tenants -f scripts/seed_db.sql --section=tenants
--   psql -d Resource -f scripts/seed_db.sql --section=resources
-- (If your psql doesn't support named sections, run the parts manually.)

-- ==============================================================
-- SECTION: Tenants database (seed tenant #1, organization & tenant enums)
-- ==============================================================

BEGIN;

-- Tenant types
INSERT INTO "TenantsTypes" ("Id", "Code", "Description", "UtcCreated") VALUES
(1, 'DEFAULT', 'Default tenant type', now()),
(2, 'EXTERNAL', 'External tenant type', now())
ON CONFLICT ("Id") DO NOTHING;

-- Tenant (tenant #1)
INSERT INTO "Tenants" ("Id", "TenantTypeId", "Code", "Alias", "Description", "UtcCreated") VALUES
(1, 1, 'TENANT_1', 'tenant_one', 'Seeded tenant number 1', now())
ON CONFLICT ("Id") DO NOTHING;

-- Organization types
INSERT INTO "OrganizationTypes" ("Id", "TenantId", "Code", "Alias", "Description", "UtcCreated") VALUES
(1, 1, 'HQ', 'Headquarters', 'Headquarters organization', now()),
(2, 1, 'DEPT', 'Department', 'Department organization', now()),
(3, 1, 'SITE', 'Site', 'Physical site', now())
ON CONFLICT ("Id") DO NOTHING;

-- Organizations for tenant 1
INSERT INTO "Organizations" ("Id", "TenantId", "OrganizationTypeId", "Code", "Alias", "Description", "HasTable", "UtcCreated") VALUES
(1, 1, 1, 'ORG_HQ', 'Org_HQ', 'Main headquarters for tenant 1', NULL, now()),
(2, 1, 2, 'ORG_DEPT_A', 'DeptA', 'Department A', NULL, now()),
(3, 1, 2, 'ORG_DEPT_B', 'DeptB', 'Department B', NULL, now()),
(4, 1, 3, 'ORG_SITE_1', 'Site1', 'Site 1', NULL, now())
ON CONFLICT ("Id") DO NOTHING;

-- Organization hierarchy (HQ -> departments, HQ -> site)
INSERT INTO "OrganizationHierarchy" ("Id", "TenantId", "OrganizationParentId", "OrganizationChildId", "UtcCreated") VALUES
(1, 1, 1, 2, now()),
(2, 1, 1, 3, now()),
(3, 1, 1, 4, now())
ON CONFLICT ("Id") DO NOTHING;

-- User types
INSERT INTO "UserTypes" ("Id", "TenantId", "OrganizationId", "Code", "UtcCreated") VALUES
(1, 1, NULL, 'ADMIN', now()),
(2, 1, NULL, 'REGULAR', now())
ON CONFLICT ("Id") DO NOTHING;

-- Users for tenant 1
INSERT INTO "Users" ("Id", "TenantId", "OrganizationId", "UserTypeId", "Username", "Code", "Description", "UtcCreated") VALUES
(1, 1, 1, 1, 'alice', 'alice', 'Tenant 1 - Admin user', now()),
(2, 1, 2, 2, 'bob', 'bob', 'Tenant 1 - Dept A user', now()),
(3, 1, 3, 2, 'carol', 'carol', 'Tenant 1 - Dept B user', now())
ON CONFLICT ("Id") DO NOTHING;

-- User groups
INSERT INTO "UserGroups" ("Id", "TenantId", "OrganizationId", "Code", "Description", "UtcCreated") VALUES
(1, 1, NULL, 'ADMINS', 'Administrators group', now()),
(2, 1, 2, 'DEPT_A_TEAM', 'Dept A team', now())
ON CONFLICT ("Id") DO NOTHING;

-- User <-> group memberships
INSERT INTO "UserUserGroups" ("Id", "TenantId", "OrganizationId", "UserGroupId", "UserId", "UtcCreated") VALUES
(1, 1, NULL, 1, 1, now()),
(2, 1, 2, 2, 2, now())
ON CONFLICT ("Id") DO NOTHING;

-- Role types
INSERT INTO "RoleTypes" ("Id", "TenantId", "OrganizationId", "Code", "Description", "UtcCreated") VALUES
(1, 1, NULL, 'SYS', 'System role', now()),
(2, 1, NULL, 'BUSINESS', 'Business role', now())
ON CONFLICT ("Id") DO NOTHING;

-- Roles
INSERT INTO "Roles" ("Id", "TenantId", "OrganizationId", "RoleTypeId", "RoleName", "Code", "Description", "UtcCreated") VALUES
(1, 1, NULL, 1, 'SysAdmin', 'SYS_ADMIN', 'System administrator role', now()),
(2, 1, 2, 2, 'DeptAManager', 'DEPT_A_MANAGER', 'Dept A manager role', now())
ON CONFLICT ("Id") DO NOTHING;

-- Role assignments to users
INSERT INTO "RoleUsers" ("Id", "TenantId", "OrganizationId", "RoleId", "UserId", "UtcCreated") VALUES
(1, 1, NULL, 1, 1, now()),
(2, 1, 2, 2, 2, now())
ON CONFLICT ("Id") DO NOTHING;

-- Role <-> user-group linkage
INSERT INTO "RoleUserGroups" ("Id", "TenantId", "OrganizationId", "RoleId", "UserGroupId", "UtcCreated") VALUES
(1, 1, NULL, 1, 1, now())
ON CONFLICT ("Id") DO NOTHING;

-- User statuses & event store
INSERT INTO "UserStatuses" ("Id", "TenantId", "Code", "Description", "UtcCreated") VALUES
(1, 1, 'ACTIVE', 'Active user', now()),
(2, 1, 'DISABLED', 'Disabled user', now())
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "UserEventStores" ("Id", "TenantId", "UserId", "UserStatusId", "UtcCreated", "Notes") VALUES
(1, 1, 1, 1, now(), 'Initial active status for alice'),
(2, 1, 2, 1, now(), 'Initial active status for bob')
ON CONFLICT ("Id") DO NOTHING;

COMMIT;

-- ==============================================================
-- SECTION: Resource database (seed resources related to tenant #1)
-- ==============================================================

BEGIN;

-- Resource types
INSERT INTO "ResourceTypes" ("Id", "TenantId", "OrganizationId", "Code", "Description", "HasTable", "ResourceTypeParentId", "UtcCreated") VALUES
(1, 1, NULL, 'VM', 'Virtual Machine', NULL, NULL, now()),
(2, 1, NULL, 'STORAGE', 'Storage Bucket', NULL, NULL, now()),
(3, 1, NULL, 'NETWORK', 'Virtual Network', NULL, NULL, now())
ON CONFLICT ("Id") DO NOTHING;

-- Resource statuses
INSERT INTO "ResourceStatuses" ("Id", "TenantId", "Code", "Description", "UtcCreated") VALUES
(1, 1, 'PROVISIONING', 'Resource is provisioning', now()),
(2, 1, 'ACTIVE', 'Resource is active', now()),
(3, 1, 'DECOMMISSIONED', 'Resource is decommissioned', now())
ON CONFLICT ("Id") DO NOTHING;

-- Resource groups
INSERT INTO "ResourceGroups" ("Id", "TenantId", "OrganizationId", "Code", "Description", "UtcCreated") VALUES
(1, 1, 1, 'RG_GLOBAL', 'Global resources group', now()),
(2, 1, 2, 'RG_DEPT_A', 'Dept A resources', now())
ON CONFLICT ("Id") DO NOTHING;

-- Resources for tenant 1 (basic fields based on Resource.cs)
INSERT INTO "Resources" ("Id", "TenantId", "OrganizationId", "Code", "Description", "ResourceTypeId", "UtcCreated") VALUES
(1, 1, 1, 'RES_VM_1', 'Tenant1 - VM in HQ', 1, now()),
(2, 1, 2, 'RES_STORAGE_A1', 'Tenant1 - Storage in Dept A', 2, now()),
(3, 1, 4, 'RES_NET_SITE1', 'Tenant1 - Network for Site 1', 3, now())
ON CONFLICT ("Id") DO NOTHING;

-- Link resources to groups
INSERT INTO "ResourceResourceGroups" ("Id", "TenantId", "ResourceId", "ResourceGroupId", "UtcCreated") VALUES
(1, 1, 1, 1, now()),
(2, 1, 2, 2, now())
ON CONFLICT ("Id") DO NOTHING;

-- Resource hierarchy (parent/child relationships)
INSERT INTO "ResourcesHierarchy" ("Id", "TenantId", "ParentResourceId", "ChildResourceId", "UtcCreated") VALUES
(1, 1, 1, 2, now()) -- e.g. VM depends on storage
ON CONFLICT ("Id") DO NOTHING;

-- Resource event store (status history)
INSERT INTO "ResourceEventStores" ("Id", "TenantId", "ResourceId", "ResourceStatusId", "UtcCreated", "Notes") VALUES
(1, 1, 1, 1, now(), 'Provisioning started for RES_VM_1'),
(2, 1, 1, 2, now(), 'RES_VM_1 became active'),
(3, 1, 2, 2, now(), 'Storage for Dept A active')
ON CONFLICT ("Id") DO NOTHING;

COMMIT;

-- End of seed_db.sql
