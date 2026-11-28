-- Farmer portal Task 6: removes the leaf checks of the LOCAL test farmers (and so resets their daily allowance) and
-- their LeafCheck AI budget rows. LOCAL spicone_dev only. The photo files stay in SpicAPI/Uploads/FarmerLeaf/:
-- delete that folder by hand.
--     psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/cleanup_task6_local.sql
DO $$
BEGIN
    IF current_database() <> 'spicone_dev' THEN
        RAISE EXCEPTION 'cleanup_task6_local.sql runs on the local spicone_dev database only (this is %)', current_database();
    END IF;
END $$;

DELETE FROM "LeafDiagnoses" d
USING "FarmerProfiles" p
WHERE p."UserId" = d."FarmerUserId"
  AND p."Phone" IN ('6777777703', '6163665844', '6999174165', '6404528677', '6846575346', '6271619756', '6614843176', '6224431458');

DELETE FROM "FarmerAiUsages" u
USING "FarmerProfiles" p
WHERE p."UserId" = u."FarmerUserId"
  AND u."Kind" = 'LeafCheck'
  AND p."Phone" IN ('6777777703', '6163665844', '6999174165', '6404528677', '6846575346', '6271619756', '6614843176', '6224431458');
