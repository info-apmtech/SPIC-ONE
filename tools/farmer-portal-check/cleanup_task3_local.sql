-- Undo seed_task3_local.sql (and the smoke test's posts) on the LOCAL spicone_dev database only.
--   psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/cleanup_task3_local.sql
DO $$
BEGIN
  IF current_database() <> 'spicone_dev' THEN
    RAISE EXCEPTION 'cleanup_task3_local.sql runs only on spicone_dev (this is %)', current_database();
  END IF;
END $$;

DELETE FROM "CommunityPostReplies" r USING "CommunityPosts" p
  WHERE r."PostId" = p."Id" AND (p."Tags" = 'demo-task3' OR p."Title" LIKE '[smoke]%');
DELETE FROM "CommunityReactions" x USING "CommunityPosts" p
  WHERE x."TargetType" = 0 AND x."TargetId" = p."Id" AND (p."Tags" = 'demo-task3' OR p."Title" LIKE '[smoke]%');
DELETE FROM "CommunityPosts" WHERE "Tags" = 'demo-task3' OR "Title" LIKE '[smoke]%';
