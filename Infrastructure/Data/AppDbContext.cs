using Frank.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frank.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Tables ────────────────────────────────────────────
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<TapOption> TapOptions { get; set; }
    public DbSet<QuestionSelectionRule> QuestionSelectionRules { get; set; }
    public DbSet<QuestionUsage> QuestionUsages { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ConversationHistory> ConversationHistories { get; set; }
    public DbSet<MorningCheckin> MorningCheckins { get; set; }
    public DbSet<UrgeEvent> UrgeEvents { get; set; }
    public DbSet<EveningCheckin> EveningCheckins { get; set; }
    public DbSet<WeeklySummary> WeeklySummaries { get; set; }
    public DbSet<UserSummary> UserSummaries { get; set; }
    public DbSet<PreUrgeVersion> PreUrgeVersions { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<MiniMirrorEvent> MiniMirrorEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── UserProfile ───────────────────────────────────
        mb.Entity<UserProfile>(e =>
        {
            e.ToTable("user_profiles");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
            e.Property(x => x.AddictionTypes).HasColumnName("addiction_types")
                .HasColumnType("text[]");
            e.Property(x => x.RiskWindows).HasColumnName("risk_windows")
                .HasColumnType("text[]");
            e.Property(x => x.PreUrgeDescription).HasColumnName("pre_urge_description").IsRequired();
            e.Property(x => x.CoreMotivation).HasColumnName("core_motivation").IsRequired();
            e.Property(x => x.EnvironmentRisks).HasColumnName("environment_risks")
                .HasColumnType("text[]");
            e.Property(x => x.ReplacementStack).HasColumnName("replacement_stack")
                .HasColumnType("text[]");
            e.Property(x => x.FutureSelfText).HasColumnName("future_self_text");
            e.Property(x => x.HabitDuration).HasColumnName("habit_duration").IsRequired();
            e.Property(x => x.IdentityFraming).HasColumnName("identity_framing");
            e.Property(x => x.ChangeStage).HasColumnName("change_stage");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.AddictionDuration).HasColumnName("addiction_duration");
            e.Property(x => x.SocialSupportLevel).HasColumnName("social_support_level");
            e.Property(x => x.PostSlipEmotion).HasColumnName("post_slip_emotion");
            e.Property(x => x.SpecificTriggers).HasColumnName("specific_triggers");
            e.Property(x => x.PushToken).HasColumnName("push_token");
            e.Property(x => x.TimezoneOffset).HasColumnName("timezone_offset").HasDefaultValue(0);
        });

        // ── Question ──────────────────────────────────────
        mb.Entity<Question>(e =>
        {
            e.ToTable("questions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Text).HasColumnName("text").IsRequired();
            e.Property(x => x.SessionType).HasColumnName("session_type").IsRequired();
            e.Property(x => x.InputType).HasColumnName("input_type").IsRequired();
            e.Property(x => x.Phase).HasColumnName("phase").HasDefaultValue("mvp");
            e.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true);
            e.Property(x => x.Weight).HasColumnName("weight").HasDefaultValue(1);
            e.Property(x => x.Conditions).HasColumnName("conditions").HasColumnType("jsonb");
            e.Property(x => x.Frame).HasColumnName("frame");
            e.Property(x => x.PatternTags).HasColumnName("pattern_tags")
                .HasColumnType("text[]");
            e.Property(q => q.Subtype).HasMaxLength(50);

        });

        // ── TapOption ─────────────────────────────────────
        mb.Entity<TapOption>(e =>
        {
            e.ToTable("tap_options");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.QuestionId).HasColumnName("question_id").IsRequired();
            e.Property(x => x.Text).HasColumnName("text").IsRequired();
            e.Property(x => x.Value).HasColumnName("value").IsRequired();
            e.Property(x => x.Order).HasColumnName("order");
            e.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true);
            e.Property(x => x.PatternTag).HasColumnName("pattern_tag");
            e.Property(x => x.ColorHint).HasColumnName("color_hint");
            e.HasOne(x => x.Question)
                .WithMany(q => q.TapOptions)
                .HasForeignKey(x => x.QuestionId);
        });

        // ── QuestionSelectionRule ─────────────────────────
        mb.Entity<QuestionSelectionRule>(e =>
        {
            e.ToTable("question_selection_rules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SessionType).HasColumnName("session_type").IsRequired();
            e.Property(x => x.RuleName).HasColumnName("rule_name").IsRequired();
            e.Property(x => x.Frame).HasColumnName("frame").IsRequired();
            e.Property(x => x.Slot).HasColumnName("slot");
            e.Property(x => x.Required).HasColumnName("required").HasDefaultValue(true);
            e.Property(x => x.Conditions).HasColumnName("conditions").HasColumnType("jsonb");
            e.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true);
        });

        // ── QuestionUsage ─────────────────────────────────
        mb.Entity<QuestionUsage>(e =>
        {
            e.ToTable("question_usage");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.QuestionId).HasColumnName("question_id");
            e.Property(x => x.ShownAt).HasColumnName("shown_at");
            e.Property(x => x.SessionType).HasColumnName("session_type");
            e.HasOne(x => x.Question)
                .WithMany(q => q.QuestionUsages)
                .HasForeignKey(x => x.QuestionId);
            e.HasIndex(x => new { x.UserId, x.QuestionId, x.ShownAt });
        });

        // ── AppSetting ────────────────────────────────────
        mb.Entity<AppSetting>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasColumnName("key");
            e.Property(x => x.Value).HasColumnName("value").IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // ── Answer ────────────────────────────────────────
        mb.Entity<Answer>(e =>
        {
            e.ToTable("answers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.SessionType).HasColumnName("session_type");
            e.Property(x => x.QuestionId).HasColumnName("question_id");
            e.Property(x => x.QuestionText).HasColumnName("question_text").IsRequired();
            e.Property(x => x.TapValue).HasColumnName("tap_value");
            e.Property(x => x.AnswerText).HasColumnName("answer_text");
            e.Property(x => x.Timestamp).HasColumnName("timestamp");
            e.HasOne(x => x.Session)
                .WithMany(s => s.Answers)
                .HasForeignKey(x => x.SessionId);
            e.HasIndex(x => new { x.UserId, x.SessionId });
            e.HasIndex(x => new { x.UserId, x.QuestionId });
        });

        // ── ChatSession ───────────────────────────────────
        mb.Entity<ChatSession>(e =>
        {
            e.ToTable("chat_sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.SessionType).HasColumnName("session_type");
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.StartedAt).HasColumnName("started_at");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at");
            e.Property(x => x.Completed).HasColumnName("completed").HasDefaultValue(false);
            e.HasIndex(x => new { x.UserId, x.Date, x.SessionType });
        });

        // ── ConversationHistory ───────────────────────────
        mb.Entity<ConversationHistory>(e =>
        {
            e.ToTable("conversation_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.SessionType).HasColumnName("session_type");
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.Timestamp).HasColumnName("timestamp");
            e.Property(x => x.Role).HasColumnName("role");
            e.Property(x => x.Content).HasColumnName("content");
            e.Property(x => x.MessageType).HasColumnName("message_type");
            e.Property(x => x.TapValue).HasColumnName("tap_value");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.Property(x => x.CrisisFlag).HasColumnName("crisis_flag").HasDefaultValue(false);
            e.HasOne(x => x.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(x => x.SessionId);
            e.HasIndex(x => new { x.UserId, x.Date });
            // Never return soft-deleted rows by default
            e.HasQueryFilter(x => x.DeletedAt == null);
        });

        // ── MorningCheckin ────────────────────────────────
        mb.Entity<MorningCheckin>(e =>
        {
            e.ToTable("morning_checkins");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.HighRiskFlag).HasColumnName("high_risk_flag").HasDefaultValue(false);
            e.Property(x => x.ForecastText).HasColumnName("forecast_text");
            e.Property(x => x.YesterdayScore).HasColumnName("yesterday_score");
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.HasOne(x => x.User)
                .WithMany(u => u.MorningCheckins)
                .HasForeignKey(x => x.UserId);
            // One per user per day
            e.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
        });

        // ── UrgeEvent ─────────────────────────────────────
        mb.Entity<UrgeEvent>(e =>
        {
            e.ToTable("urge_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Timestamp).HasColumnName("timestamp");
            e.Property(x => x.AddictionType).HasColumnName("addiction_type");
            e.Property(x => x.UrgeIntensity).HasColumnName("urge_intensity");
            e.Property(x => x.HaltRoot).HasColumnName("halt_root");
            e.Property(x => x.InterventionId).HasColumnName("intervention_id");
            e.Property(x => x.Resisted).HasColumnName("resisted");
            e.Property(x => x.MoodValence).HasColumnName("mood_valence");
            e.Property(x => x.Environment).HasColumnName("environment");
            e.Property(x => x.DayOfWeek).HasColumnName("day_of_week");
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.PostUrgeMood).HasColumnName("post_urge_mood");
            e.Property(x => x.PreUrgeContext).HasColumnName("pre_urge_context");
            e.HasOne(x => x.User)
                .WithMany(u => u.UrgeEvents)
                .HasForeignKey(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.Timestamp });
            e.HasIndex(x => new { x.UserId, x.AddictionType });
        });

        // ── EveningCheckin ────────────────────────────────
        mb.Entity<EveningCheckin>(e =>
        {
            e.ToTable("evening_checkins");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.DayResult).HasColumnName("day_result").IsRequired();
            e.Property(x => x.SlipType).HasColumnName("slip_type").HasColumnType("text[]");
            e.Property(x => x.UsedUrgeButton).HasColumnName("used_urge_button");
            e.Property(x => x.TomorrowRisk).HasColumnName("tomorrow_risk");
            e.Property(x => x.HardEnvironment).HasColumnName("hard_environment");
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.HasOne(x => x.User)
                .WithMany(u => u.EveningCheckins)
                .HasForeignKey(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
        });

        // ── WeeklySummary ─────────────────────────────────
        mb.Entity<WeeklySummary>(e =>
        {
            e.ToTable("weekly_summaries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.WeekStart).HasColumnName("week_start");
            e.Property(x => x.TotalUrges).HasColumnName("total_urges");
            e.Property(x => x.TotalResisted).HasColumnName("total_resisted");
            e.Property(x => x.ResistRate).HasColumnName("resist_rate");
            e.Property(x => x.ResistRateDelta).HasColumnName("resist_rate_delta");
            e.Property(x => x.WorstDay).HasColumnName("worst_day");
            e.Property(x => x.WorstHour).HasColumnName("worst_hour");
            e.Property(x => x.TopHaltRoot).HasColumnName("top_halt_root");
            e.Property(x => x.Observation1).HasColumnName("observation_1");
            e.Property(x => x.Observation2).HasColumnName("observation_2");
            e.Property(x => x.Observation3).HasColumnName("observation_3");
            e.Property(x => x.QuotesJson).HasColumnName("quotes_json").HasColumnType("jsonb");
            e.Property(x => x.ReflectionQId).HasColumnName("reflection_q_id");
            e.Property(x => x.ReflectionText).HasColumnName("reflection_text");
            e.HasOne(x => x.User)
                .WithMany(u => u.WeeklySummaries)
                .HasForeignKey(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.WeekStart }).IsUnique();
        });

        // ── UserSummary ───────────────────────────────────
        mb.Entity<UserSummary>(e =>
        {
            e.ToTable("user_summaries");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Last7Days).HasColumnName("last_7_days").HasColumnType("jsonb");
            e.Property(x => x.Last30Days).HasColumnName("last_30_days").HasColumnType("jsonb");
            e.Property(x => x.AllTime).HasColumnName("all_time").HasColumnType("jsonb");
            e.Property(x => x.DailyUrgeCount).HasColumnName("daily_urge_count").HasDefaultValue(0);
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.User)
                .WithOne(u => u.UserSummary)
                .HasForeignKey<UserSummary>(x => x.UserId);
        });

        // ── PreUrgeVersion ────────────────────────────────
        mb.Entity<PreUrgeVersion>(e =>
        {
            e.ToTable("pre_urge_versions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.VersionText).HasColumnName("version_text").IsRequired();
            e.Property(x => x.VersionNum).HasColumnName("version_num");
            e.Property(x => x.Source).HasColumnName("source").HasDefaultValue("onboarding");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.User)
                .WithMany(u => u.PreUrgeVersions)
                .HasForeignKey(x => x.UserId);
        });

        // ── NotificationLog ───────────────────────────────
        mb.Entity<NotificationLog>(e =>
        {
            e.ToTable("notification_log");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Type).HasColumnName("type");
            e.Property(x => x.SentAt).HasColumnName("sent_at");
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.Opened).HasColumnName("opened").HasDefaultValue(false);
            e.Property(x => x.LinkedId).HasColumnName("linked_id");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.Date });
        });

        // ── MiniMirrorEvent ───────────────────────────────
        mb.Entity<MiniMirrorEvent>(e =>
        {
            e.ToTable("mini_mirror_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TriggeredAt).HasColumnName("triggered_at");
            e.Property(x => x.UrgeCount).HasColumnName("urge_count");
            e.Property(x => x.QuotesShown).HasColumnName("quotes_shown").HasColumnType("jsonb");
            e.Property(x => x.Observation).HasColumnName("observation");
            e.Property(x => x.Opened).HasColumnName("opened").HasDefaultValue(false);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });
    }


}