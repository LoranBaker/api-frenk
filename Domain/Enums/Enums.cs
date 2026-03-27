namespace Frank.Domain.Enums;

public enum SessionType
{
    Morning,
    Urge,
    Evening,
    Weekly,
    Onboarding,
    Halt
}

public enum InputType
{
    TapOnly,
    TapText,
    TextOnly
}

public enum DayResult
{
    StayedInControl,
    Slipped,
    HardDay
}

public enum MoodValence
{
    Positive,
    Neutral,
    Negative
}

public enum HabitDuration
{
    LessThan3Months,    // lt_3mo
    ThreeTo12Months,    // 3_12mo
    OneToThreeYears,    // 1_3yr
    MoreThan3Years      // gt_3yr
}

public enum IdentityFraming
{
    Doer,           // "I am a gamer / this is just who I am"
    TryingToStop,   // "I want to stop but keep slipping"
    Unsure
}

public enum ChangeStage
{
    Precontemplation,
    Contemplation,
    Preparation,
    Action,
    Maintenance
}

public enum AddictionType
{
    Gaming,
    Weed,
    Cigarettes,
    Gambling,
    SocialMedia
}

public enum NotificationType
{
    MorningNudge,
    EveningGentle,
    PostUrgeMood,
    Milestone,
    MiniMirror
}

public enum MessageType
{
    Text,
    TapOption,
    Forecast,
    Interrupt,
    ResultBadge,
    System
}

public enum MessageRole
{
    Frank,
    User
}

public enum QuestionFrame
{
    Frame1,         // Emotional / reflective — morning slot 1, urge at intensity 1-3
    Circumstance,   // Situational — morning slot 2, evening primary
    Frame2          // Activation interrupt — urge at intensity 4-10 only
}

public enum InterventionType
{
    Question,
    Physical,
    Mirror,
    Replacement,
    Surfing
}

public enum HaltRoot
{
    Hungry,
    Angry,
    Lonely,
    Tired,
    Stressed,
    Sick,
    Sensory
}