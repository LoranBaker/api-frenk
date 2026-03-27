using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrankApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_type = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "question_selection_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_type = table.Column<string>(type: "text", nullable: false),
                    rule_name = table.Column<string>(type: "text", nullable: false),
                    frame = table.Column<string>(type: "text", nullable: false),
                    slot = table.Column<int>(type: "integer", nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    conditions = table.Column<string>(type: "jsonb", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_selection_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    session_type = table.Column<string>(type: "text", nullable: false),
                    input_type = table.Column<string>(type: "text", nullable: false),
                    phase = table.Column<string>(type: "text", nullable: false, defaultValue: "mvp"),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    conditions = table.Column<string>(type: "jsonb", nullable: true),
                    frame = table.Column<string>(type: "text", nullable: true),
                    pattern_tags = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    addiction_types = table.Column<string[]>(type: "text[]", nullable: false),
                    risk_windows = table.Column<string[]>(type: "text[]", nullable: false),
                    pre_urge_description = table.Column<string>(type: "text", nullable: false),
                    core_motivation = table.Column<string>(type: "text", nullable: false),
                    environment_risks = table.Column<string[]>(type: "text[]", nullable: false),
                    replacement_stack = table.Column<string[]>(type: "text[]", nullable: false),
                    future_self_text = table.Column<string>(type: "text", nullable: true),
                    habit_duration = table.Column<string>(type: "text", nullable: false),
                    identity_framing = table.Column<string>(type: "text", nullable: true),
                    change_stage = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    addiction_duration = table.Column<string>(type: "text", nullable: true),
                    social_support_level = table.Column<string>(type: "text", nullable: true),
                    post_slip_emotion = table.Column<string>(type: "text", nullable: true),
                    specific_triggers = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "conversation_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_type = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    message_type = table.Column<string>(type: "text", nullable: false),
                    tap_value = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    crisis_flag = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_conversation_history_chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_type = table.Column<string>(type: "text", nullable: false),
                    question_id = table.Column<string>(type: "text", nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    tap_value = table.Column<string>(type: "text", nullable: true),
                    answer_text = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_answers_chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_answers_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_usage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<string>(type: "text", nullable: false),
                    shown_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    session_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_usage", x => x.id);
                    table.ForeignKey(
                        name: "FK_question_usage_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tap_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    pattern_tag = table.Column<string>(type: "text", nullable: true),
                    color_hint = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tap_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_tap_options_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evening_checkins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    day_result = table.Column<string>(type: "text", nullable: false),
                    slip_type = table.Column<string[]>(type: "text[]", nullable: true),
                    used_urge_button = table.Column<bool>(type: "boolean", nullable: false),
                    tomorrow_risk = table.Column<string>(type: "text", nullable: true),
                    hard_environment = table.Column<string>(type: "text", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evening_checkins", x => x.id);
                    table.ForeignKey(
                        name: "FK_evening_checkins_chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evening_checkins_user_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "user_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mini_mirror_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    urge_count = table.Column<int>(type: "integer", nullable: false),
                    quotes_shown = table.Column<string>(type: "jsonb", nullable: false),
                    observation = table.Column<string>(type: "text", nullable: false),
                    opened = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mini_mirror_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_mini_mirror_events_user_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "user_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "morning_checkins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    high_risk_flag = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    forecast_text = table.Column<string>(type: "text", nullable: true),
                    yesterday_score = table.Column<int>(type: "integer", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_morning_checkins", x => x.id);
                    table.ForeignKey(
                        name: "FK_morning_checkins_chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_morning_checkins_user_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "user_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    opened = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    linked_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_log_user_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "user_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pre_urge_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_text = table.Column<string>(type: "text", nullable: false),
                    version_num = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false, defaultValue: "onboarding"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pre_urge_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_pre_urge_versions_user_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "user_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "urge_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    addiction_type = table.Column<string>(type: "text", nullable: false),
                    urge_intensity = table.Column<int>(type: "integer", nullable: false),
                    halt_root = table.Column<string>(type: "text", nullable: true),
                    intervention_id = table.Column<string>(type: "text", nullable: false),
                    resisted = table.Column<bool>(type: "boolean", nullable: false),
                    mood_valence = table.Column<string>(type: "text", nullable: false),
                    environment = table.Column<string>(type: "text", nullable: true),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_urge_mood = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_urge_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_urge_events_chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_urge_events_user_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "user_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_summaries",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_7_days = table.Column<string>(type: "jsonb", nullable: true),
                    last_30_days = table.Column<string>(type: "jsonb", nullable: true),
                    all_time = table.Column<string>(type: "jsonb", nullable: true),
                    daily_urge_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_summaries", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_summaries_user_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "user_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start = table.Column<DateOnly>(type: "date", nullable: false),
                    total_urges = table.Column<int>(type: "integer", nullable: false),
                    total_resisted = table.Column<int>(type: "integer", nullable: false),
                    resist_rate = table.Column<double>(type: "double precision", nullable: false),
                    resist_rate_delta = table.Column<double>(type: "double precision", nullable: true),
                    worst_day = table.Column<string>(type: "text", nullable: true),
                    worst_hour = table.Column<int>(type: "integer", nullable: true),
                    top_halt_root = table.Column<string>(type: "text", nullable: true),
                    observation_1 = table.Column<string>(type: "text", nullable: true),
                    observation_2 = table.Column<string>(type: "text", nullable: true),
                    observation_3 = table.Column<string>(type: "text", nullable: true),
                    quotes_json = table.Column<string>(type: "jsonb", nullable: true),
                    reflection_q_id = table.Column<string>(type: "text", nullable: true),
                    reflection_text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_summaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_weekly_summaries_user_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "user_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_answers_question_id",
                table: "answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_answers_session_id",
                table: "answers",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_answers_user_id_question_id",
                table: "answers",
                columns: new[] { "user_id", "question_id" });

            migrationBuilder.CreateIndex(
                name: "IX_answers_user_id_session_id",
                table: "answers",
                columns: new[] { "user_id", "session_id" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_user_id_date_session_type",
                table: "chat_sessions",
                columns: new[] { "user_id", "date", "session_type" });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_history_session_id",
                table: "conversation_history",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_history_user_id_date",
                table: "conversation_history",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_evening_checkins_session_id",
                table: "evening_checkins",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_evening_checkins_user_id_date",
                table: "evening_checkins",
                columns: new[] { "user_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mini_mirror_events_user_id",
                table: "mini_mirror_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_morning_checkins_session_id",
                table: "morning_checkins",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_morning_checkins_user_id_date",
                table: "morning_checkins",
                columns: new[] { "user_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_log_user_id_date",
                table: "notification_log",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_pre_urge_versions_user_id",
                table: "pre_urge_versions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_question_usage_question_id",
                table: "question_usage",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_question_usage_user_id_question_id_shown_at",
                table: "question_usage",
                columns: new[] { "user_id", "question_id", "shown_at" });

            migrationBuilder.CreateIndex(
                name: "IX_tap_options_question_id",
                table: "tap_options",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_urge_events_session_id",
                table: "urge_events",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_urge_events_user_id_addiction_type",
                table: "urge_events",
                columns: new[] { "user_id", "addiction_type" });

            migrationBuilder.CreateIndex(
                name: "IX_urge_events_user_id_timestamp",
                table: "urge_events",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_summaries_user_id_week_start",
                table: "weekly_summaries",
                columns: new[] { "user_id", "week_start" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answers");

            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "conversation_history");

            migrationBuilder.DropTable(
                name: "evening_checkins");

            migrationBuilder.DropTable(
                name: "mini_mirror_events");

            migrationBuilder.DropTable(
                name: "morning_checkins");

            migrationBuilder.DropTable(
                name: "notification_log");

            migrationBuilder.DropTable(
                name: "pre_urge_versions");

            migrationBuilder.DropTable(
                name: "question_selection_rules");

            migrationBuilder.DropTable(
                name: "question_usage");

            migrationBuilder.DropTable(
                name: "tap_options");

            migrationBuilder.DropTable(
                name: "urge_events");

            migrationBuilder.DropTable(
                name: "user_summaries");

            migrationBuilder.DropTable(
                name: "weekly_summaries");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "chat_sessions");

            migrationBuilder.DropTable(
                name: "user_profiles");
        }
    }
}
