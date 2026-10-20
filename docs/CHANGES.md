# PowerFit: ما تغيّر ولماذا

## قبل التشغيل
1. أنشئ قاعدة البيانات بسكربت الجداول الأصلي عندك (لم يصلني، فهو غير موجود هنا)، ثم شغّل `Database/001_safety_and_integrity.sql`. الكود يقرأ العمودين `Food.Allergens` و`FitnessPlan.RowVersion` ولن يعمل بدونهما. السكربت آمن للتكرار، ويؤرشف أولاً أي خطط Active مكررة لنفس المستخدم.
2. ضع الأسرار في user-secrets (انظر `README.md`)، ولا تكتبها في `appsettings.json`.
3. إعدادات اختيارية في `appsettings.json` (موجودة بقيم افتراضية): `Cors:AllowedOrigins`، `ForwardedHeaders:TrustedProxies`، `Swagger:Enabled`، `Ai:MaxGenerationsPerUserPerDay`، `Ai:MaxGenerationsPerDay`.

## 1) البنود 1-3: Program.cs والـ Seed
- `Program.cs` أصبح كاملاً: `AddBusiness` (ويستدعي الآن `AddDataAccess` والـ repositories)، JWT، Rate limiting، Request timeouts، CORS، Swagger، و`ExceptionHandlingMiddleware` أول الـ pipeline، بالترتيب الصحيح.
- `Main` صار `async Task`، ويستدعي `InitializeAsync()` الذي يفحص الاتصال بقاعدة البيانات وينشئ أول أدمن.
- مهلة `AiGeneration` = `Ai:TimeoutSeconds` + 60 ثانية، حتى يعمل تحويل 504.
- حُذف `MapOpenApi` (كانت ستُحجب بالـ Fallback Policy).
- أخطاء الـ model binding (JSON خاطئ، `IsActive` ناقص) تُرجع الآن نفس `ErrorResponse` بدل `ValidationProblemDetails`.

## 2) البنود 4 و5 و9
- **4:** `GET /api/plans` يخفي فقط الـ Draft الفارغة (فشل التوليد)، فلا تختفي الخطة الصالحة عند `ActivateImmediately=false`.
- **5:** فهرس فريد مفلتر `UX_FitnessPlan_OneActivePerUser` في قاعدة البيانات. و`ActivateAsync` يؤرشف الخطط الأخرى فوراً بـ `ExecuteUpdate` قبل تفعيل الخطة، حتى لا يرى الفهرس صفّين Active لحظة واحدة. لذلك يجب استدعاؤها داخل `ExecuteInTransactionAsync` (الخدمات تفعل ذلك).
- **9:** `DbUpdateException` تُترجم إلى 409 (قيمة مكررة، سجل مستخدم، سجل عُدّل من شخص آخر) بدل 500. تفاصيل SQL داخل `DbErrors` في طبقة البيانات، وطبقة الـ API لا تعرفها.
- إضافة `RowVersion` على `FitnessPlan`: كل تعديل أدمن يلمس صف الخطة، فتعديلان متزامنان ينتجان 409 بدل "آخر كاتب يفوز".

## 3) بنود السلامة 10-13
- **10:** `BodyAreas` يوحّد مناطق الجسم بالعربية والإنجليزية ("الركبة" = "knee"، "أسفل الظهر" = "lower back"). "ظهر" وحدها تعني الأسفل والأعلى معاً (حذر مقصود). ما لا يعرفه القاموس يُقارَن كنص كما كان.
- **11:** `DietaryFlags.FromText` يفهم النفي ("non-vegetarian"، "غير نباتي"). وأُضيف عمود `Food.Allergens` (Egg, Soy, Fish, Shellfish, Sesame) مع Create/Update/DTO/Validator، وتُستبعد الأطعمة المطابقة عند بناء الخطة. لا تنسَ وسم أطعمتك الحالية (أمثلة في آخر ملف SQL).
- **12:** تعديلات الأدمن (إضافة/تغيير تمرين أو طعام) تفحص الآن إصابات المستخدم وقيوده الغذائية وترفض بـ 400. للتجاوز: `overrideSafetyCheck: true` في الطلب.
- **13:** توليد الخطة يتطلب تاريخ ميلاد (حُذف العمر الافتراضي 30)، وعمراً 16+، وهدف Fat Loss من 18+ وبـ BMI ≥ 18.5. وكل خطة تحمل الآن الحقل `disclaimer` (عربي/إنجليزي) يجب أن تعرضه الواجهة. والحد الأدنى للسعرات 1200 صار موحداً بين الـ AI وتعديلات الأدمن.

## 4) الأمان والتكلفة
- **Timing في Login:** يُنفَّذ تحقق كلمة المرور دائماً (على hash وهمي إن لم يوجد الحساب).
- **قفل الحساب:** 5 محاولات فاشلة خلال 15 دقيقة تعني قفل 15 دقيقة (429 مع `Retry-After`). مخزَّن في ذاكرة النسخة الواحدة؛ مع عدة نسخ ضعه في Redis/SQL.
- **CORS:** من `Cors:AllowedOrigins`. في Development بلا إعداد يسمح للجميع، وخارجه لا يسمح لأحد.
- **سقف التوليد:** `MaxGenerationsPerUserPerDay` و`MaxGenerationsPerDay` (24 ساعة متحركة، المحاولات الفاشلة تُحسب)، والتجاوز يعطي 429.
- **Forwarded headers:** لا يُوثق بأي proxy افتراضياً. اذكر عناوينها في `ForwardedHeaders:TrustedProxies` ليعمل الـ rate limit بحسب IP الحقيقي.

## 5) التنظيف والاختبارات
- تقسيم `Partials.cs` إلى ملف لكل كلاس (تلقائياً، دون تغيير المنطق).
- إزالة تسرب الطبقات: الـ API لم يعد يستخدم `DataAccess` إطلاقاً (`AuthRoles` و`IAppInitializer` في Business).
- مشروع `Powerfit.Tests` (xUnit): 104 اختباراً لمناطق الجسم، التمارين مقابل الإصابات، القيود الغذائية والحساسيات، BMI/TDEE، قواعد السلامة، `AiPlanValidator`، الـ validators، تجزئة كلمة المرور، وقفل الدخول.

## تغييرات سلوكية تؤثر على الواجهة
- توليد خطة بلا تاريخ ميلاد أو بعمر أقل من 16 يعطي 400.
- ظهور الحقل `disclaimer` في الخطة، و`allergens` في الأطعمة.
- أخطاء جديدة بكود 429 (قفل الدخول، سقف التوليد) و409 (تعارض قاعدة البيانات).
- تعديل أدمن يخالف إصابات المستخدم أو قيوده الغذائية يعطي 400 ما لم يُرسَل `overrideSafetyCheck`.

## ما تحققتُ منه وما لم أستطع
- **تحقق فعلي:** ترجمتُ كل الكود (الكيانات، Business، وAPI بما فيه `Program.cs` والـ controllers والـ middleware) على .NET 8 وASP.NET Core، واستبدلتُ فقط ما يحتاج حزماً غير متاحة هنا (EF Core، JwtBearer، Swashbuckle، IdentityModel) بواجهات بديلة، وشغّلتُ الاختبارات بمشغّل بسيط (وليس xunit نفسه): 104 ناجحة.
- **لم يُختبر:** `Repositories.cs` (استعلامات EF مثل `ExecuteUpdate` و`p.ExercisePlan != null`)، وربط `DbContext`، و`DbErrors` مع `SqlException`، و`ExceptionTranslatorTests` (تحتاج EF)، وتشغيل ملف SQL. شغّل `dotnet build` و`dotnet test` عندك أولاً، ثم جرّب سيناريو التفعيل المتزامن على نسخة تجريبية من قاعدة البيانات.
- **قاموس مناطق الجسم والقيود الغذائية تقريبي** ويحتاج مراجعته على بياناتك. الحل الدائم هو قائمة ثابتة (جدول أو enum) تُختار من الواجهة بدل النص الحر.

## لم يكن ضمن قائمتك وبقي كما هو
تنظيف الخطط الفارغة القديمة (6)، نقل تمرين بين الأيام (7)، توحيد باقي نسب الـ prompt مع الـ validator (14)، التحقق من `SuitableMeals` (16)، شفافية قوائم الأصناف غير النشطة (17)، انحياز `Spread` (18)، إعادة المحاولة وتسجيل `usage` في الـ AI (19-20)، تفعيل البريد، إبطال الـ JWT، موافقة الخصوصية، اختبارات تكامل على SQL Server، وإعادة تسمية `Aigeneration`. وبعض دوال الـ repositories غير المستخدمة تُركت.
