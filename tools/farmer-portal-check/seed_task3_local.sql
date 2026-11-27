-- Farmer portal Task 3: clearly fake Knowledge Community rows for the LOCAL spicone_dev database ONLY
-- (smoke test and screenshots of /farmer/community and the "Talked about near you" card).
-- Never run against any other database. Undo with cleanup_task3_local.sql.
--
--   psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/seed_task3_local.sql
--
-- Authors are the local test farmers (profiles in Thoothukudi, district 1) and the local qa.mdo /
-- qa.jmdo staff logins (Madurai HQ 1, which serves the Task 2 demo dealers of district 1). Every row is
-- tagged "demo-task3" and its body ends with "(Local demo post.)". Needs seed_task2_local.sql first
-- (the demo dealers give HQ 1 to district 1 and qa.jmdo its login).
DO $$
BEGIN
  IF current_database() <> 'spicone_dev' THEN
    RAISE EXCEPTION 'seed_task3_local.sql runs only on spicone_dev (this is %)', current_database();
  END IF;
  IF EXISTS (SELECT 1 FROM "CommunityPosts" WHERE "Tags" = 'demo-task3') THEN
    RAISE EXCEPTION 'Task 3 demo posts already exist; run cleanup_task3_local.sql first';
  END IF;
END $$;

WITH authors AS (
  SELECT
    (SELECT "UserId" FROM "FarmerProfiles" WHERE "Phone" = '6777777703') AS murugan,
    (SELECT "UserId" FROM "FarmerProfiles" WHERE "Phone" = '6404528677') AS selvi,
    (SELECT "Id" FROM "AppUsers" WHERE "UserName" = 'qa.mdo') AS mdo,
    (SELECT "Id" FROM "AppUsers" WHERE "UserName" = 'qa.jmdo') AS jmdo
)
INSERT INTO "CommunityPosts" ("Title","Body","Category","Product","Crop","Tags","Status","AuthorUserId","AuthorName","AuthorRole",
  "AuthorLocation","Views","LikeCount","ReplyCount","LastActivityAt","CreatedAt","UpdatedAt","IsDeleted")
SELECT v.title, v.body || ' (Local demo post.)', v.category, NULL, v.crop, 'demo-task3', v.status,
       CASE v.who WHEN 'murugan' THEN a.murugan WHEN 'selvi' THEN a.selvi WHEN 'mdo' THEN a.mdo ELSE a.jmdo END,
       v.author, v.role, v.place, v.views, v.likes, v.replies,
       now() - (v.age || ' hours')::interval, now() - (v.age + 48 || ' hours')::interval, now() - (v.age || ' hours')::interval, false
FROM authors a, (VALUES
  ('murugan', 'Test Farmer Murugan', 'Farmer', 'Kovilpatti, Thoothukudi',
   'Paddy leaves turning yellow after the rain', 'My samba paddy is 40 days old. After last week''s rain the lower leaves are turning yellow. Should I apply urea now or wait?',
   'Soil Health', 'Paddy', 2, 64, 5, 3, 6),
  ('selvi', 'Test Farmer Selvi', 'Farmer', 'Kovilpatti, Thoothukudi',
   'Best time to apply potash for banana', 'Banana plants are 5 months old. When should I give the second dose of potash, and how much per plant?',
   'Crop Fertilizer Knowledge', 'Banana', 1, 38, 3, 2, 20),
  ('mdo', 'QA MDO', 'SPIC Expert', 'Madurai, Tamil Nadu',
   'Zinc shortage is common in Thoothukudi soils', 'Many soil tests from our area are low in zinc. Apply zinc sulphate at 25 kg per hectare once in two seasons, mixed with sand, before transplanting.',
   'Soil Health', 'Paddy', 2, 120, 9, 2, 30),
  ('jmdo', 'QA JMDO', 'SPIC Expert', 'Madurai, Tamil Nadu',
   'Heavy rain this week: hold the top-dressing', 'The forecast shows heavy rain for the next three days. Please wait to top-dress urea until the rain stops, so it is not washed away.',
   'Crop Fertilizer Knowledge', NULL, 0, 52, 4, 0, 4),
  ('murugan', 'Test Farmer Murugan', 'Farmer', 'Kovilpatti, Thoothukudi',
   'Where can I get a soil test near Kovilpatti?', 'I want to test my field before the next crop. Who collects soil samples in our village?',
   'Soil Health', NULL, 3, 21, 1, 1, 70)
) AS v(who, author, role, place, title, body, category, crop, status, views, likes, replies, age);

-- A few replies (the "recent replies" that rank "Talked about near you").
INSERT INTO "CommunityPostReplies" ("PostId","ParentReplyId","AuthorUserId","AuthorName","AuthorRole","AuthorLocation","MentionName","Body","LikeCount","CreatedAt","IsDeleted")
SELECT p."Id", NULL, u.uid, r.author, r.role, r.place, NULL, r.body || ' (Local demo reply.)', 0, now() - (r.age || ' hours')::interval, false
FROM "CommunityPosts" p
JOIN (VALUES
  ('Paddy leaves turning yellow after the rain', 'qa.mdo', 'QA MDO', 'SPIC Expert', 'Madurai, Tamil Nadu', 'Wait two days for the field to drain, then top-dress 25 kg urea per acre.', 8),
  ('Paddy leaves turning yellow after the rain', 'qa.jmdo', 'QA JMDO', 'SPIC Expert', 'Madurai, Tamil Nadu', 'Also check for zinc shortage: yellow lower leaves with rusty spots point to it.', 7),
  ('Paddy leaves turning yellow after the rain', 'farmer:6404528677', 'Test Farmer Selvi', 'Farmer', 'Kovilpatti, Thoothukudi', 'Same in my field. Urea after the rain helped.', 6),
  ('Best time to apply potash for banana', 'qa.mdo', 'QA MDO', 'SPIC Expert', 'Madurai, Tamil Nadu', 'Give the second potash dose at the 5th month, about 100 g per plant.', 22),
  ('Best time to apply potash for banana', 'farmer:6777777703', 'Test Farmer Murugan', 'Farmer', 'Kovilpatti, Thoothukudi', 'Thank you, will try.', 21),
  ('Zinc shortage is common in Thoothukudi soils', 'farmer:6777777703', 'Test Farmer Murugan', 'Farmer', 'Kovilpatti, Thoothukudi', 'Is zinc sulphate available at the SPIC dealer in Kovilpatti?', 31),
  ('Zinc shortage is common in Thoothukudi soils', 'qa.jmdo', 'QA JMDO', 'SPIC Expert', 'Madurai, Tamil Nadu', 'Yes, Demo Agro Centre keeps it.', 30),
  ('Where can I get a soil test near Kovilpatti?', 'qa.jmdo', 'QA JMDO', 'SPIC Expert', 'Madurai, Tamil Nadu', 'Send a call-back request from My SPIC team and I will collect it.', 70)
) AS r(title, who, author, role, place, body, age) ON r.title = p."Title"
CROSS JOIN LATERAL (
  SELECT CASE WHEN r.who LIKE 'farmer:%'
              THEN (SELECT "UserId" FROM "FarmerProfiles" WHERE "Phone" = substr(r.who, 8))
              ELSE (SELECT "Id" FROM "AppUsers" WHERE "UserName" = r.who) END AS uid
) u
WHERE p."Tags" = 'demo-task3';
