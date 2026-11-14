# StudioSport — מערכת ניהול שיעורי סטודיו כושר

מערכת Web API לניהול שיעורי סטודיו כושר: משתמשת נרשמת לשיעור, רואה כמה מקומות נותרו, מבטלת הרשמה, ומנהל/ת מנהלת שיעורים ומאשרת מנויים. הלב של המערכת הוא המשאב המוגבל — מספר המקומות בכל שיעור — והטיפול בתחרות שנוצרת כשכמה משתמשות מנסות לתפוס את אותו מקום באותו רגע.

---

## המשאב המוגבל והתחרות עליו

**המשאב המוגבל** הוא מספר המקומות הפנויים בכל שיעור (`ClassSession.Capacity` מול `ClassSession.EnrolledCount`).

**איפה נוצרת התחרות:** שתי משתשמות לוחצות "הרשמה" לשיעור שנותר בו מקום אחד, בהפרש של מילישניות. הקוד הנאיבי — קריאה, בדיקה שהמקום פנוי, שמירה — ייתן לשתיהן לעבור, כי שתיהן קראו את השורה כשהיא עדיין פנויה. התוצאה: שתי הרשמות על מקום אחד, ובבסיס הנתונים אין סימן לתקלה.

**איך המערכת מגיבה:** בשתי שכבות הגנה.

1. **תפיסה אטומית בצד בסיס הנתונים.** `IClassRepository.TryReserveSpotAsync` מבצע UPDATE עם התנאי בתוך ה-WHERE:

```sql
UPDATE "ClassSessions"
SET "EnrolledCount" = "EnrolledCount" + 1
WHERE "Id" = @id
  AND "IsCancelled" = false
  AND "EnrolledCount" < "Capacity"
```

אם הבקשה המפסידה מגיעה, מספר השורות המושפעות הוא 0, והשירות מחזיר `EnrollmentOutcome.ClassFull` — כלומר 409. אף פעם לא נוצרת הרשמה על מקום שאינו פנוי.

2. **Concurrency token על הישות.** ל-`ClassSession` שדה `Version` המוגדר כ-concurrency token ב-Fluent API (`IsConcurrencyToken()`). ב-UPDATE EF Core מוסיף את הערך הישן לתנאי ה-WHERE:

```sql
UPDATE "ClassSessions" SET "EnrolledCount" = @new, "Version" = @newVersion
WHERE "Id" = @id AND "Version" = @oldVersion
```

אם השורה השתנתה בינתיים — אף שורה לא מתעדכנת, ו-EF Core זורק `DbUpdateConcurrencyException`. ה-middleware מתרגם אותה ל-409 Conflict עם הודעת JSON אחידה (`status`, `message`, `correlationId`), וכותב Warning ל-NLog.

**ההוכחה:** בדיקת האינטגרציה `StudioSport.Tests/Integration/EnrollmentConcurrencyTests` משתמשת בשני `DbContext` נפרדים: הראשון שומר בהצלחה, השני מקבל `DbUpdateConcurrencyException`. כמו כן אומתה התנהגות מקצה לקצה מול השרת: שתי בקשות מקבילות לשיעור בקיבולת 1 החזירו `201 Created` ו-`409 Conflict`, והמונה `EnrolledCount` לא עבר את הקיבולת.

---

## סטק טכנולוגי

- ASP.NET Core Web API (.NET 10)
- EF Core — Code First, עם PostgreSQL (Npgsql)
- JWT — אימות והרשאות לפי roles
- AutoMapper, NLog, Swagger
- xUnit + Moq

## ארכיטקטורה

```
StudioSport.sln
  StudioSport.Core      // Entities, Enums, DTOs, Interfaces, Common
  StudioSport.Data      // DbContext, Repositories, Migrations, Seed
  StudioSport.Service   // Business logic
  StudioSport.API       // Controllers, Middleware, Program.cs
  StudioSport.Tests     // Unit + Integration tests
```

כיוון התלויות: `Core` אינו מפנה לאף פרויקט ואינו תלוי ב-EF Core. `Data` ו-`Service` מפנים ל-`Core`. `API` מפנה ל-`Service` ול-`Core`, ומכיר את `Data` רק לצורך רישום השירותים ב-`Program.cs`. ה-Controllers אינם מזריקים `DbContext` — כל גישה לנתונים עוברת דרך שכבת ה-Service והממשקים ב-Core.

## הרצה מקומית

**דרישות מוקדמות:** .NET 10 SDK, PostgreSQL 17 פועל על `localhost:5432`.

```powershell
dotnet restore
dotnet build

# סודות — אינם בקוד ואינם בריפו
dotnet user-secrets init --project StudioSport.API
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=StudioSportDb;Username=YOUR_USER;Password=YOUR_PASSWORD" --project StudioSport.API
dotnet user-secrets set "Jwt:Key" "YOUR_SECRET_KEY_AT_LEAST_32_CHARS" --project StudioSport.API

# יצירת בסיס הנתונים
dotnet ef database update -p StudioSport.Data -s StudioSport.API

# הרצת השרת
dotnet run --project StudioSport.API --urls http://localhost:5170
```

**Swagger:** `http://localhost:5170/swagger`

ה-Seed (`DbSeeder.SeedAsync`) רץ אוטומטית בעליית השרת: הוא מריץ מיגרציות, ואם הטבלאות ריקות — יוצר משתמשים, מנוי פעיל, מדריכות ושיעורים. אין צורך בהתערבות ידנית בבסיס הנתונים אחרי `database update`.

### הרצת הלקוח

```powershell
cd client
python -m http.server 5500
```

הלקוח נפתח ב-`http://localhost:5500`, ופונה ל-API ב-`http://localhost:5170`. יש להריץ את השרת קודם.

> **הערה:** אם הדפדפן מציג גרסה ישנה של הקבצים, יש לרענן עם `Ctrl+F5` או לפתוח חלון גלישה בסתר.

## משתמשי דמו

| Username | Password | Role | מה ניתן להדגים |
|---|---|---|---|
| admin | Admin123! | Admin | יצירת שיעור, עריכה, מחיקה, אישור בקשות מנוי |
| ruthi | Member123! | Member | התחברות, הרשמה לשיעור, ביטול, צפייה בהרשמות שלי |

המשתמשים נוצרים אוטומטית ב-Seed על בסיס נתונים ריק.

## Endpoints עיקריים

| Method | Route | הרשאה |
|---|---|---|
| POST | /api/auth/register | אנונימי |
| POST | /api/auth/login | אנונימי |
| GET | /api/auth/me | מחובר |
| GET | /api/classes | אנונימי |
| GET | /api/classes/stats | מחובר |
| GET | /api/classes/{id} | אנונימי |
| POST | /api/classes | Admin |
| PUT | /api/classes/{id} | Admin |
| DELETE | /api/classes/{id} | Admin |
| POST | /api/classes/{classId}/enrollments | מחובר — כאן נוצרת התחרות |
| DELETE | /api/enrollments/{id} | בעל ההרשמה |
| GET | /api/enrollments/my | מחובר |
| POST | /api/enrollments/{id}/feedback | בעל ההרשמה |
| GET | /api/subscriptions/plans | אנונימי |
| POST | /api/subscriptions/request | מחובר |
| GET | /api/subscriptions/my | מחובר |
| GET | /api/subscriptions/my-pending | מחובר |
| GET | /api/subscriptions/pending | Admin |
| POST | /api/subscriptions/{id}/approve | Admin |

## בדיקת התחרות בפועל

דרושים שני משתמשים שונים בשני חלונות גלישה נפרדים (או חלון פרטי), ושיעור עם מקום פנוי אחד בלבד.

1. התחברי כ-`ruthi` בחלון אחד, וכ-`admin` בחלון שני.
2. ודאי שהשיעור פנוי — `availableSpots: 1`. ניתן ליצור שיעור עם `Capacity = 1` כמנהל.
3. שלחי בקשה משני החלונות בו-זמנית:

```powershell
curl.exe -X POST http://localhost:5170/api/classes/{id}/enrollments -H "Authorization: Bearer TOKEN" -H "Content-Type: application/json" -d "{}"
```

בקשה אחת תחזור `201 Created`, השנייה `409 Conflict` עם הודעת JSON הכוללת `correlationId`. ביומן NLog תופיע שורת Warning עם אותו `correlationId`, למשל:

```
Booking conflict: class 1 is full. User 5 was rejected.
```

המקור יכול להיות גם ממנשק הלקוח: שני חלונות על אותו שיעור, לחיצה על "הרשמה". הלקוח מציג הודעה ברורה כשמתקבל 409.

## לוגים — NLog

ההגדרות ב-`StudioSport.API/nlog.config`. הלוגים נכתבים לקובץ יומי:

```
StudioSport.API/logs/YYYY-MM-DD.log
```

פורמט השורה:

```
{longdate}|{LEVEL}|{logger}|[{CorrelationId}]|{message}
```

- כל בקשה נכנסת נרשמת עם ה-`CorrelationId` שנוצר ב-`CorrelationIdMiddleware`.
- כל חריגה שנתפסת — ברמת Error.
- כל התנגשות על משאב — ברמת Warning.
- סיסמאות, טוקנים ותוכן בקשות הרשמה אינם נכתבים ללוג.

## בדיקות

```powershell
dotnet test
```

| סוג | קובץ | מה נבדק |
|---|---|---|
| יחידה (xUnit + Moq) | `Services/EnrollmentServiceTests.cs` | הרשמה תקינה, שיעור מלא (409), שיעור לא קיים, שיעור מבוטל, שיעור שהחל, אין מנוי, אין יתרת שיעורים, הרשמה כפולה, מכסה יומית, ביטול הרשמה, ביטול בידי מי שאינו הבעלים |
| אינטגרציה | `Integration/EnrollmentConcurrencyTests.cs` | שני `DbContext` נפרדים — הראשון שומר, השני מקבל `DbUpdateConcurrencyException` |

בדיקות התחרות דורשות משתנה סביבה `STUDIO_TEST_DB` עם מחרוזת חיבור לבסיס נתונים אמיתי. ללא המשתנה הן מדולגות.

בדיקות היחידה אינן נוגעות בבסיס נתונים — כולן עם repositories מדומים.

## מבנה בסיס הנתונים

ישויות עיקריות: `AppUser`, `Trainer`, `ClassSession`, `Enrollment`, `Subscription`, `Feedback`, `ClassTemplate`.

- `ClassSession` — one-to-many מול `Enrollment` ו-`Trainer`.
- `Enrollment` — many-to-many בפועל בין משתמש לשיעור, עם סטטוס (Active/Cancelled) וזמן ביטול.
- `Subscription` — מנוי עם `TotalClasses` ו-`UsedClasses`, וסטטוס Pending/Active/Cancelled.

המיגרציות ב-`StudioSport.Data/Migrations`. ה-DbContext מוגדר ב-Fluent API (`OnModelCreating`).

## מחוץ לתחום

תשלומים, רשימת המתנה, ניהול חדרים, פריסה בענן, Docker, אחסון קבצים, caching ו-SignalR.
