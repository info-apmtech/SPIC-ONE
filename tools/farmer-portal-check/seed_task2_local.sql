-- Farmer portal Task 2: clearly fake demo rows for the LOCAL spicone_dev database ONLY
-- (smoke tests and screenshots of /farmer/dealers, /farmer/team, /farmer-requests).
-- Never run against any other database. Undo with cleanup_task2_local.sql.
--
--   psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/seed_task2_local.sql
--
-- Dealers are named "Demo ...", phones 90000000xx, PAN / bank fields "DEMO..." so the smoke test can
-- prove they never leave the API. Three rows must NEVER be listed (terminated, to-be-terminated,
-- new dealer awaiting final approval).
DO $$
BEGIN
  IF current_database() <> 'spicone_dev' THEN
    RAISE EXCEPTION 'seed_task2_local.sql runs only on spicone_dev (this is %)', current_database();
  END IF;
END $$;

INSERT INTO "DealerRegistrations" (
  "IsDealer","DealerType","InSpic","InGreenStar","DealerCode","StateId","DealerStateId","Region","HQ","Status","InactiveProposal",
  "ParentDealer","FirmName","DateOfAppointment","DebitorBalance","ShopNoORRoomNoOrBlockNo","Street","SubVillage","Village","PinCode",
  "Taluk","DistrictId","OfficialContactNumber","WhatsAppNumber","GoogleMapURL","Latitude","Longitude",
  "AccountHolderName","AccountNumber","Branch","IFSC","PANNumber","GSTNumber","AadhaarNumber",
  "WholesaleLicenseExpiryDate","RetailLicenseExpiryDate","IsOfficeAutomation","IsSDWA","CreditLimitExperiance",
  "CreatedBy","CreatedAt","UpdatedBy","UpdatedAt","YearsofExperiance","AssetValue","CapitalInvestment","CashCreditLimit",
  "OwnGodownCapacity","RentGodownCapacity","SettlementAmount","TradeDepositAmount","IsNewDealerRegistration","IsCreditLimitConsentGiven",
  "AVPApproved")
VALUES
 (true,0,true,false,'D900001',1,1,1,1,0,NULL,0,'Demo Agro Centre',now(),0,'12','Main Bazaar Road',NULL,'Kovilpatti','628501','Kovilpatti',1,'9000000001','9000000001',NULL,9.1755,77.8710,'DEMO HOLDER','DEMOACC0001','Demo Branch','DEMO0000001','DEMOPAN001','DEMOGST001','DEMOAADHAAR1',now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,false,false,NULL),
 (true,0,true,false,'D900002',1,1,1,1,0,NULL,0,'Demo Farm Inputs',now(),0,'4/7','Bus Stand Street','North Colony','Ettayapuram','628902','Ettayapuram',1,'0461 2000001','9000000002',NULL,9.1490,78.0010,'DEMO HOLDER','DEMOACC0002','Demo Branch','DEMO0000002','DEMOPAN002',NULL,NULL,now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,false,false,NULL),
 (true,NULL,true,false,'D900003',1,1,1,1,0,NULL,0,'Demo Kisan Store',now(),0,'88','Temple Car Street',NULL,'Vilathikulam','628907','Vilathikulam',1,'9000000003','9000000003','https://maps.google.com/?q=9.131,78.169',9.1310,78.1690,'DEMO HOLDER','DEMOACC0003','Demo Branch','DEMO0000003','DEMOPAN003',NULL,NULL,now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,false,false,NULL),
 (true,0,true,false,'D900004',1,1,1,6,0,NULL,0,'Demo Green Traders',now(),0,'21','South Car Street',NULL,'Palayamkottai','627002','Palayamkottai',2,'9000000004','9000000004',NULL,8.7139,77.7567,'DEMO HOLDER','DEMOACC0004','Demo Branch','DEMO0000004','DEMOPAN004',NULL,NULL,now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,false,false,NULL),
 (true,0,true,true,'D900005',1,1,1,1,0,NULL,0,'Demo Uzhavan Agencies',now(),0,'3','Beach Road',NULL,'Thoothukudi','628001','Thoothukudi',1,'9000000005','',NULL,8.7642,78.1348,'DEMO HOLDER','DEMOACC0005','Demo Branch','DEMO0000005','DEMOPAN005',NULL,NULL,now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,false,false,NULL),
 (true,0,true,false,'D900006',1,1,0,1,0,NULL,0,'Demo Sakthi Fertilisers',now(),0,'','',NULL,'Kayathar','628952','Kayathar',1,'9000000006','9000000006',NULL,NULL,NULL,'DEMO HOLDER','DEMOACC0006','Demo Branch','DEMO0000006','DEMOPAN006',NULL,NULL,now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,false,false,NULL),
 -- never listed: terminated, to-be-terminated (InActive + proposal Terminated), new dealer without final approval
 (true,0,true,false,'D900007',1,1,1,1,2,NULL,0,'Demo Terminated Agro',now(),0,'1','',NULL,'Kovilpatti','628501','Kovilpatti',1,'9000000007','9000000007',NULL,9.1760,77.8700,'DEMO HOLDER','DEMOACC0007','Demo Branch','DEMO0000007','DEMOPAN007',NULL,NULL,now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,false,false,NULL),
 (true,0,true,false,'D900008',1,1,1,1,1,2,0,'Demo To Be Terminated',now(),0,'2','',NULL,'Kovilpatti','628501','Kovilpatti',1,'9000000008','9000000008',NULL,9.1770,77.8720,'DEMO HOLDER','DEMOACC0008','Demo Branch','DEMO0000008','DEMOPAN008',NULL,NULL,now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,false,false,NULL),
 (true,0,true,false,NULL,1,1,1,1,0,NULL,0,'Demo New Dealer Pending',now(),0,'3','',NULL,'Kovilpatti','628501','Kovilpatti',1,'9000000009','9000000009',NULL,9.1780,77.8730,'DEMO HOLDER','DEMOACC0009','Demo Branch','DEMO0000009','DEMOPAN009',NULL,NULL,now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,true,false,NULL);

-- Fix round 1: an approved New Dealer in a credit-limit maintenance cycle (AVPApproved reset to NULL,
-- FinalApprovalDate kept): must stay listed.
INSERT INTO "DealerRegistrations" (
  "IsDealer","DealerType","InSpic","InGreenStar","DealerCode","StateId","DealerStateId","Region","HQ","Status",
  "ParentDealer","FirmName","DateOfAppointment","DebitorBalance","ShopNoORRoomNoOrBlockNo","Village","PinCode","Taluk","DistrictId",
  "OfficialContactNumber","WhatsAppNumber","AccountHolderName","AccountNumber","Branch","IFSC","PANNumber",
  "WholesaleLicenseExpiryDate","RetailLicenseExpiryDate","IsOfficeAutomation","IsSDWA","CreditLimitExperiance",
  "CreatedBy","CreatedAt","UpdatedBy","UpdatedAt","YearsofExperiance","AssetValue","CapitalInvestment","CashCreditLimit",
  "OwnGodownCapacity","RentGodownCapacity","SettlementAmount","TradeDepositAmount","IsNewDealerRegistration","IsCreditLimitConsentGiven",
  "AVPApproved","RMApproved","SMApproved","FinalApprovalDate","IsSubmittedForReview")
SELECT true,0,true,false,'D900010',1,1,1,1,0,0,'Demo Maintenance Agro',now(),0,'9','Kadambur','628703','Kayathar',1,
  '9000000010','9000000010','DEMO HOLDER','DEMOACC0010','Demo Branch','DEMO0000010','DEMOPAN010',
  now(),now(),false,false,now(),'demo-seed',now(),'demo-seed',now(),0,0,0,0,0,0,0,0,true,false,
  NULL,NULL,NULL,now() - interval '120 days',true
WHERE NOT EXISTS (SELECT 1 FROM "DealerRegistrations" WHERE "FirmName" = 'Demo Maintenance Agro');

-- Demo staff names / official numbers for the local QA staff logins (qa.mdo, qa.rm, qa.rmd) and a new
-- JMDO login for qa.jmdo, all at Madurai HQ (1) / South Region (1), Tamil Nadu (1).
INSERT INTO "EmployeeInformation" ("EmployeeCode","Name","PersonalPhoneNumber","OfficialPhoneNumber","Email","CreatedBy","UpdatedBy","CreatedAt","UpdatedAt")
VALUES
 ('DEMO-JMDO-1','Demo JMDO Priya S','9000000021','9000000011','demo.jmdo@example.invalid','demo-seed','demo-seed',now(),now()),
 ('DEMO-MDO-1','Demo MDO Karthik R','9000000022','9000000012','demo.mdo@example.invalid','demo-seed','demo-seed',now(),now()),
 ('DEMO-RM-1','Demo RM Ravi Kumar','9000000023','9000000013','demo.rm@example.invalid','demo-seed','demo-seed',now(),now()),
 ('DEMO-RMD-1','Demo RMD Lakshmi N','9000000024','9000000014','demo.rmd@example.invalid','demo-seed','demo-seed',now(),now());

UPDATE "Employeelogins" l SET "EmployeeInformationID" = i."Id"
FROM "EmployeeInformation" i, "AppUsers" u
WHERE u."Id" = l."UserId" AND l."EmployeeInformationID" = 0
  AND ((u."UserName" = 'qa.mdo' AND i."EmployeeCode" = 'DEMO-MDO-1')
    OR (u."UserName" = 'qa.rm' AND i."EmployeeCode" = 'DEMO-RM-1')
    OR (u."UserName" = 'qa.rmd' AND i."EmployeeCode" = 'DEMO-RMD-1'));

INSERT INTO "Employeelogins" ("UserId","Role","ZoneId","StateId","RegionId","HeadquartersId","RegionIds","HeadquartersIds","IsActive","EmployeeInformationID")
SELECT u."Id", 10, 1, 1, 1, 1, ARRAY[1], ARRAY[1], true, i."Id"
FROM "AppUsers" u, "EmployeeInformation" i
WHERE u."UserName" = 'qa.jmdo' AND i."EmployeeCode" = 'DEMO-JMDO-1'
  AND NOT EXISTS (SELECT 1 FROM "Employeelogins" x WHERE x."UserId" = u."Id");

-- The QA staff accounts are normally inactive locally; the team lists active accounts only.
UPDATE "AppUsers" SET "IsActive" = true WHERE "UserName" IN ('qa.jmdo','qa.mdo','qa.rm','qa.rmd','qa.mo','qa.admin');
