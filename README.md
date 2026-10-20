# PowerFit

واجهة API لخطط تمارين وتغذية مخصصة، تُولَّد بالذكاء الاصطناعي (Anthropic Messages API) وتُراجَع من أدمن.
مبنية على ثلاث طبقات (3-Tier) في ثلاثة مشاريع منفصلة:

```
Powerfit.API  (العرض)  ──►  Powerfit.Business  (المنطق)  ──►  DataAccess  (البيانات)
Controllers, Middleware      Services, DTOs, Validators        EF Core, Repositories,
JWT, Rate limit, CORS        AI, Security, Mapping             Unit of Work, Entities
```

الـ API يعتمد على Business فقط، ولا يستخدم أي نوع من DataAccess.

## المتطلبات
- .NET 10 SDK، وللفتح في Visual Studio يلزم **Visual Studio 2026** (VS 2022 لا يدعم .NET 10). يمكن تغيير الإصدار من `Directory.Build.props` وحده
- SQL Server

## الإعداد
1. أنشئ قاعدة البيانات `PowerFit` بسكربت الجداول الأصلي عندك، ثم شغّل `Database/001_safety_and_integrity.sql`.
2. عدّل `ConnectionStrings:DefaultConnection` في `src/Powerfit.API/appsettings.json` إن لزم.
3. الأسرار في user-secrets وليس في الملفات:
```bash
cd src/Powerfit.API
dotnet user-secrets set "Jwt:Key" "<32 حرفاً عشوائياً أو أكثر>"
dotnet user-secrets set "Ai:ApiKey" "<مفتاح مزوّد الذكاء الاصطناعي>"
dotnet user-secrets set "AdminSeed:Email" "admin@example.com"
dotnet user-secrets set "AdminSeed:Password" "<كلمة مرور قوية>"   # احذفها بعد أول تشغيل
```
4. التشغيل:
```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Powerfit.API      # Swagger: https://localhost:7080/swagger (في Development)
```

أول تشغيل ينشئ أول أدمن من `AdminSeed` إن لم يوجد أدمن. ومن الإنتاج: فعّل `Swagger:Enabled` فقط إن احتجته، واضبط `Cors:AllowedOrigins` و`ForwardedHeaders:TrustedProxies`.

## إعادة توليد الكيانات من قاعدة البيانات (Scaffold)
```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer \
  --project src/DataAccess --startup-project src/Powerfit.API \
  --context PowerFitDbContext --context-dir Context --output-dir Entities --force
```
الملفات المولَّدة هي `Context/PowerFitDbContext.cs` والكيانات مباشرة تحت `Entities/`.
أما `Context/PowerFitDbContext.Partial.cs` و`Entities/Partials/*` فهي يدوية ولا يلمسها الأمر.

## الهيكل
```
PowerFit/
├── Database
│   └── 001_safety_and_integrity.sql
├── docs
│   └── CHANGES.md
├── src
│   ├── DataAccess
│   │   ├── Context
│   │   │   ├── PowerFitDbContext.cs
│   │   │   └── PowerFitDbContext.Partial.cs
│   │   ├── Entities
│   │   │   ├── Partials
│   │   │   │   ├── Admin.Partial.cs
│   │   │   │   ├── AigenerationLog.Partial.cs
│   │   │   │   ├── BodyAreas.cs
│   │   │   │   ├── Constants.cs
│   │   │   │   ├── Exercise.Partial.cs
│   │   │   │   ├── ExercisePlan.Partial.cs
│   │   │   │   ├── ExercisePlanItem.Partial.cs
│   │   │   │   ├── FitnessPlan.Partial.cs
│   │   │   │   ├── Food.Partial.cs
│   │   │   │   ├── NutritionPlan.Partial.cs
│   │   │   │   ├── NutritionPlanItem.Partial.cs
│   │   │   │   ├── Person.Partial.cs
│   │   │   │   ├── Profile.Partial.cs
│   │   │   │   ├── TextList.cs
│   │   │   │   ├── User.Partial.cs
│   │   │   │   └── ValueTypes.cs
│   │   │   ├── Admin.cs
│   │   │   ├── AigenerationLog.cs
│   │   │   ├── Exercise.cs
│   │   │   ├── ExercisePlan.cs
│   │   │   ├── ExercisePlanItem.cs
│   │   │   ├── FitnessPlan.cs
│   │   │   ├── Food.cs
│   │   │   ├── NutritionPlan.cs
│   │   │   ├── NutritionPlanItem.cs
│   │   │   ├── Person.cs
│   │   │   ├── Profile.cs
│   │   │   └── User.cs
│   │   ├── Repositories
│   │   │   └── Repositories.cs
│   │   ├── DataAccess.csproj
│   │   ├── DbErrors.cs
│   │   └── DependencyInjection.cs
│   ├── Powerfit.API
│   │   ├── Controllers
│   │   │   ├── Admin
│   │   │   │   ├── AdminAccountsController.cs
│   │   │   │   ├── AdminExercisesController.cs
│   │   │   │   ├── AdminFoodsController.cs
│   │   │   │   ├── AdminPlanEditingController.cs
│   │   │   │   └── AdminPlansController.cs
│   │   │   ├── AuthController.cs
│   │   │   ├── ExercisesController.cs
│   │   │   ├── FoodsController.cs
│   │   │   ├── PlansController.cs
│   │   │   └── ProfileController.cs
│   │   ├── Extensions
│   │   │   ├── AuthenticationExtensions.cs
│   │   │   ├── ClaimsPrincipalExtensions.cs
│   │   │   ├── HostingExtensions.cs
│   │   │   ├── RateLimitingExtensions.cs
│   │   │   ├── StartupExtensions.cs
│   │   │   └── SwaggerExtensions.cs
│   │   ├── Middlewares
│   │   │   └── ExceptionHandlingMiddleware.cs
│   │   ├── Properties
│   │   │   └── launchSettings.json
│   │   ├── Requests
│   │   │   └── SetActiveRequest.cs
│   │   ├── Responses
│   │   │   └── ErrorResponse.cs
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.json
│   │   ├── Powerfit.API.csproj
│   │   └── Program.cs
│   └── Powerfit.Business
│       ├── AI
│       │   ├── AiModels.cs
│       │   ├── AiPlanGenerator.cs
│       │   └── AiPlanValidator.cs
│       ├── DTOs
│       │   ├── Auth
│       │   │   ├── AuthResponse.cs
│       │   │   ├── ChangePasswordRequest.cs
│       │   │   ├── LoginRequest.cs
│       │   │   └── RegisterRequest.cs
│       │   ├── Common
│       │   │   └── PagedResponse.cs
│       │   ├── Exercises
│       │   │   ├── CreateExerciseRequest.cs
│       │   │   ├── ExerciseDto.cs
│       │   │   ├── ExerciseQuery.cs
│       │   │   └── UpdateExerciseRequest.cs
│       │   ├── Foods
│       │   │   ├── CreateFoodRequest.cs
│       │   │   ├── FoodDto.cs
│       │   │   ├── FoodQuery.cs
│       │   │   └── UpdateFoodRequest.cs
│       │   ├── Plans
│       │   │   ├── ExercisePlanDto.cs
│       │   │   ├── ExercisePlanItemDto.cs
│       │   │   ├── FitnessPlanDto.cs
│       │   │   ├── GeneratePlanRequest.cs
│       │   │   ├── NutritionPlanDto.cs
│       │   │   ├── NutritionPlanItemDto.cs
│       │   │   ├── PlanEditRequests.cs
│       │   │   └── PlanQuery.cs
│       │   └── Profiles
│       │       ├── ProfileDto.cs
│       │       └── UpdateProfileRequest.cs
│       ├── Exceptions
│       │   ├── AiGenerationException.cs
│       │   ├── ConflictException.cs
│       │   ├── ExceptionTranslator.cs
│       │   ├── NotFoundException.cs
│       │   ├── TooManyRequestsException.cs
│       │   ├── UnauthorizedException.cs
│       │   └── ValidationException.cs
│       ├── Interfaces
│       │   ├── IAiPlanGenerator.cs
│       │   ├── IAppInitializer.cs
│       │   ├── IAuthService.cs
│       │   ├── IExerciseService.cs
│       │   ├── IFoodService.cs
│       │   ├── ILoginAttemptTracker.cs
│       │   ├── IPasswordHasher.cs
│       │   ├── IPlanEditingService.cs
│       │   ├── IPlanService.cs
│       │   ├── IProfileService.cs
│       │   └── ITokenService.cs
│       ├── Mapping
│       │   ├── ExerciseMappings.cs
│       │   ├── FoodMappings.cs
│       │   ├── MappingHelpers.cs
│       │   ├── PersonMappings.cs
│       │   ├── PlanMappings.cs
│       │   └── ProfileMappings.cs
│       ├── Properties
│       │   └── AssemblyInfo.cs
│       ├── Security
│       │   ├── AuthRoles.cs
│       │   ├── LoginAttemptTracker.cs
│       │   ├── PasswordHasher.cs
│       │   └── TokenService.cs
│       ├── Services
│       │   ├── AppInitializer.cs
│       │   ├── AuthService.cs
│       │   ├── ExerciseService.cs
│       │   ├── FoodService.cs
│       │   ├── PlanEditingService.cs
│       │   ├── PlanService.cs
│       │   └── ProfileService.cs
│       ├── Settings
│       │   ├── AiSettings.cs
│       │   └── JwtSettings.cs
│       ├── Validators
│       │   ├── AllowedValues.cs
│       │   ├── AuthValidator.cs
│       │   ├── ExerciseValidator.cs
│       │   ├── FoodValidator.cs
│       │   ├── PlanValidator.cs
│       │   ├── ProfileValidator.cs
│       │   ├── SafetyRules.cs
│       │   └── ValidationBuilder.cs
│       ├── DependencyInjection.cs
│       └── Powerfit.Business.csproj
├── tests
│   └── Powerfit.Tests
│       ├── Business
│       │   ├── AiPlanValidatorTests.cs
│       │   ├── ExceptionTranslatorTests.cs
│       │   ├── LoginAttemptTrackerTests.cs
│       │   ├── PasswordHasherTests.cs
│       │   ├── SafetyRulesTests.cs
│       │   └── ValidatorTests.cs
│       ├── Entities
│       │   ├── BodyAreasTests.cs
│       │   ├── DietaryFlagsTests.cs
│       │   ├── ExerciseSafetyTests.cs
│       │   ├── FoodTests.cs
│       │   ├── PlanEntitiesTests.cs
│       │   └── ProfileTests.cs
│       └── Powerfit.Tests.csproj
├── .gitignore
├── Directory.Build.props
├── PowerFit.sln
└── README.md
```

## ملاحظات
- التغييرات الأخيرة وسببها: `docs/CHANGES.md`.
- مشروع `Powerfit.Tests` اختبارات وحدة (xUnit) للمنطق الصرف. اختبارات التكامل على SQL Server غير موجودة بعد.
- أرقام إصدارات الحزم في ملفات `.csproj` مضبوطة على .NET 10 (حزم Microsoft = 10.0.0)، حدّثها إلى أحدث إصدار مستقر عندك. أبقِ Swashbuckle على 6.9.0: الإصدارات الأحدث تغيّر واجهة `Microsoft.OpenApi` وتتطلب تعديل `SwaggerExtensions.cs`.
