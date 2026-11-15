UPDATE "Subscriptions" SET "Status" = 'Cancelled' WHERE "Id" = 1;
UPDATE "Subscriptions" SET "Plan" = 4, "TotalClasses" = 4, "UsedClasses" = 0 WHERE "Id" = 2;
SELECT "Id","UserId","Status","Plan","TotalClasses","UsedClasses" FROM "Subscriptions" ORDER BY "Id";