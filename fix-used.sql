UPDATE "Subscriptions" s
SET "UsedClasses" = (
  SELECT COUNT(*) FROM "Enrollments" e
  WHERE e."UserId" = s."UserId" AND e."Status" = 'Active'
)
WHERE s."Status" = 'Active';