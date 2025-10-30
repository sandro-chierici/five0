-- SQL script for Postgres DB to seed 10 resources for testing, all related to tenant with id 1

INSERT into public."TenantsTypes" ("Id", "Code") VALUES (1, 'tenant-type-default');  
INSERT into public."Tenants" ("Id", "Code", "TenantTypeId") VALUES (1, 'tenant-default', 1);  

INSERT into public."ResourceTypes" ("Id", "TenantId", "Code") VALUES (1, 1, 'Test Resource Type 1');    

INSERT INTO public."Resources" ("TenantId", "Name", "Description", "ResourceTypeId")
VALUES
  (1, 'Resource 1', 'Test resource 1', 1),
  (1, 'Resource 2', 'Test resource 2', 1),
  (1, 'Resource 3', 'Test resource 3', 1),
  (1, 'Resource 4', 'Test resource 4', 1),
  (1, 'Resource 5', 'Test resource 5', 1),
  (1, 'Resource 6', 'Test resource 6', 1),
  (1, 'Resource 7', 'Test resource 7', 1),
  (1, 'Resource 8', 'Test resource 8', 1),
  (1, 'Resource 9', 'Test resource 9', 1),
  (1, 'Resource 10', 'Test resource 10', 1);


