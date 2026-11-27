-- Undo seed_task2_local.sql (and the smoke test's requests) on the LOCAL spicone_dev database only.
--   psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/cleanup_task2_local.sql
DO $$
BEGIN
  IF current_database() <> 'spicone_dev' THEN
    RAISE EXCEPTION 'cleanup_task2_local.sql runs only on spicone_dev (this is %)', current_database();
  END IF;
END $$;

DELETE FROM "FarmerContactRequests" WHERE "Message" LIKE '[smoke]%' OR "Message" LIKE '[demo]%';
DELETE FROM "DealerRegistrations" WHERE "CreatedBy" = 'demo-seed' AND "FirmName" LIKE 'Demo %';
DELETE FROM "Employeelogins" l USING "EmployeeInformation" i
  WHERE l."EmployeeInformationID" = i."Id" AND i."EmployeeCode" = 'DEMO-JMDO-1';
UPDATE "Employeelogins" l SET "EmployeeInformationID" = 0 FROM "EmployeeInformation" i
  WHERE l."EmployeeInformationID" = i."Id" AND i."EmployeeCode" LIKE 'DEMO-%';
DELETE FROM "EmployeeInformation" WHERE "EmployeeCode" LIKE 'DEMO-%';
UPDATE "AppUsers" SET "IsActive" = false WHERE "UserName" IN ('qa.jmdo','qa.mdo','qa.rm','qa.rmd','qa.mo','qa.admin');
