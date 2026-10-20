using DataAccess.Entities;
using Powerfit.Business.Exceptions;

namespace Powerfit.Business.Validators;

/// <summary>Health-related limits applied before a plan is generated.</summary>
public static class SafetyRules
{
    /// <summary>Youngest age that gets a generated plan.</summary>
    public const int MinPlanAge = 16;

    /// <summary>A calorie deficit (Fat Loss goal) is only offered to adults.</summary>
    public const int MinAgeForFatLoss = 18;

    /// <summary>Below this BMI (WHO "Underweight") a Fat Loss goal is refused.</summary>
    public const decimal UnderweightBmi = 18.5m;

    /// <summary>Returned with every plan (FitnessPlanDto.Disclaimer): the UI must show it.</summary>
    public const string MedicalDisclaimer =
        "This plan is general fitness and nutrition guidance created by an AI system. It is not medical advice. " +
        "Talk to a doctor before you start, especially if you have a medical condition, an injury, are pregnant or take medication. " +
        "Stop and get medical help if you feel pain, dizziness or unwell. | " +
        "هذه الخطة إرشادات عامة للياقة والتغذية أنشأها نظام ذكاء اصطناعي وليست نصيحة طبية. " +
        "استشر طبيبك قبل البدء، خاصة إذا كان لديك مرض أو إصابة أو حمل أو تتناول أدوية. " +
        "توقف واطلب مساعدة طبية عند الشعور بألم أو دوخة أو تعب غير معتاد.";

    /// <summary>
    /// Throws ValidationException when the user must not get a generated plan. Returns the age
    /// (a birth date is required: no silent default age is used for calorie estimates anymore).
    /// </summary>
    public static int EnsureEligibleForPlan(Profile profile, Person person)
    {
        if (person.Age is not { } age)
            throw new ValidationException("birthDate",
                "Add your birth date to your profile before generating a plan (it is needed for safe calorie estimates).");

        if (age < MinPlanAge)
            throw new ValidationException("birthDate", $"Plans are available from age {MinPlanAge}.");

        if (profile.Goal == Goals.FatLoss)
        {
            if (age < MinAgeForFatLoss)
                throw new ValidationException("goal",
                    $"A Fat Loss plan is available from age {MinAgeForFatLoss}. Choose another goal.");

            if (profile.Bmi is { } bmi && bmi < UnderweightBmi)
                throw new ValidationException("goal",
                    "Your BMI is in the underweight range, so a Fat Loss plan is not available. Choose another goal or talk to a doctor.");
        }

        return age;
    }
}
