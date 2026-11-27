-- Farmer portal Task 5: removes the SPIC AI chats of the LOCAL test farmers (and so resets their daily
-- allowance). LOCAL spicone_dev only.
--     psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/cleanup_task5_local.sql
DO $$
BEGIN
    IF current_database() <> 'spicone_dev' THEN
        RAISE EXCEPTION 'cleanup_task5_local.sql runs on the local spicone_dev database only (this is %)', current_database();
    END IF;
END $$;

DELETE FROM "FarmerChatConversations" c
USING "FarmerProfiles" p
WHERE p."UserId" = c."FarmerUserId"
  AND p."Phone" IN ('6777777703', '6163665844', '6999174165', '6404528677', '6846575346', '6271619756', '6614843176', '6224431458');
