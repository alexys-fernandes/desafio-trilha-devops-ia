using HabitApp.Infrastructure.Data.Utils;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace HabitApp.Infrastructure.Data.Context;

public static class SqliteSchemaMigrator
{
    public static async Task MigrateAsync(SqliteContext context)
    {
        await context.Database.EnsureCreatedAsync();

        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            if (!await TableExistsAsync(connection, "Habits"))
            {
                return;
            }

            await AddColumnIfMissingAsync(connection, "Habits", "Color", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(connection, "Habits", "Category", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(connection, "Habits", "RecurrenceType", "TEXT NOT NULL DEFAULT 'Daily'");
            await AddColumnIfMissingAsync(connection, "Habits", "RecurrenceConfig", "TEXT NULL");
            await AddColumnIfMissingAsync(connection, "Habits", "ReminderEnabled", "INTEGER NOT NULL DEFAULT 0");
            await AddColumnIfMissingAsync(connection, "Habits", "ReminderTime", "TEXT NULL");
            await AddColumnIfMissingAsync(connection, "Habits", "ReminderTimezone", "TEXT NOT NULL DEFAULT 'America/Sao_Paulo'");
            await AddColumnIfMissingAsync(connection, "Habits", "ReminderMessage", "TEXT NULL");
            await AddColumnIfMissingAsync(connection, "Habits", "ReminderType", "TEXT NOT NULL DEFAULT 'Standard'");
            await AddColumnIfMissingAsync(connection, "Habits", "IsArchived", "INTEGER NOT NULL DEFAULT 0");

            await CreateHabitCompletionsTableAsync(connection);
            await CreateUserNotificationPreferencesTableAsync(connection);
            await ImportLegacyCompletedDaysAsync(connection);
            await LocalizeLegacyTextDataAsync(connection);
            await DropLegacyHabitColumnsAsync(connection);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table' AND name = $tableName;
            """;
        AddParameter(command, "$tableName", tableName);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(
        DbConnection connection,
        string tableName,
        string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task AddColumnIfMissingAsync(
        DbConnection connection,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        if (await ColumnExistsAsync(connection, tableName, columnName))
        {
            return;
        }

        await ExecuteNonQueryAsync(
            connection,
            $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition};");
    }

    private static async Task CreateHabitCompletionsTableAsync(DbConnection connection)
    {
        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE TABLE IF NOT EXISTS "HabitCompletions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_HabitCompletions" PRIMARY KEY AUTOINCREMENT,
                "HabitId" INTEGER NOT NULL,
                "UserId" INTEGER NOT NULL,
                "CompletedDate" TEXT NOT NULL,
                "CompletedAt" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "ModifiedAt" TEXT NULL,
                "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT "FK_HabitCompletions_Habits_HabitId"
                    FOREIGN KEY ("HabitId") REFERENCES "Habits" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_HabitCompletions_Users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE INDEX IF NOT EXISTS "IX_HabitCompletions_HabitId"
            ON "HabitCompletions" ("HabitId");
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE INDEX IF NOT EXISTS "IX_HabitCompletions_UserId"
            ON "HabitCompletions" ("UserId");
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_HabitCompletions_HabitId_UserId_CompletedDate_Active"
            ON "HabitCompletions" ("HabitId", "UserId", "CompletedDate")
            WHERE "IsDeleted" = 0;
            """);
    }

    private static async Task CreateUserNotificationPreferencesTableAsync(DbConnection connection)
    {
        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE TABLE IF NOT EXISTS "UserNotificationPreferences" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_UserNotificationPreferences" PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "NotificationsEnabled" INTEGER NOT NULL DEFAULT 1,
                "QuietHoursStart" TEXT NULL DEFAULT '22:00:00',
                "QuietHoursEnd" TEXT NULL DEFAULT '07:00:00',
                "ReminderSoundEnabled" INTEGER NOT NULL DEFAULT 1,
                "MotivationalNotificationsEnabled" INTEGER NOT NULL DEFAULT 1,
                "AchievementNotificationsEnabled" INTEGER NOT NULL DEFAULT 1,
                "StreakRiskNotificationsEnabled" INTEGER NOT NULL DEFAULT 1,
                "DefaultReminderTime" TEXT NULL DEFAULT '09:00:00',
                "DefaultReminderType" TEXT NOT NULL DEFAULT 'Standard',
                "CreatedAt" TEXT NOT NULL,
                "ModifiedAt" TEXT NULL,
                "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT "FK_UserNotificationPreferences_Users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserNotificationPreferences_UserId"
            ON "UserNotificationPreferences" ("UserId")
            WHERE "IsDeleted" = 0;
            """);
    }

    private static async Task ImportLegacyCompletedDaysAsync(DbConnection connection)
    {
        if (!await ColumnExistsAsync(connection, "Habits", "CompletedDaysRaw"))
        {
            return;
        }

        var legacyHabits = await ReadLegacyHabitsAsync(connection);
        if (legacyHabits.Count == 0)
        {
            return;
        }

        var weekStart = GetWeekStart(DateOnly.FromDateTime(DateTimeUtils.GetHorarioBrasilia()));

        foreach (var habit in legacyHabits)
        {
            var completedDays = habit.CompletedDaysRaw
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(value => bool.TryParse(value, out var parsed) && parsed)
                .ToArray();

            for (var index = 0; index < Math.Min(completedDays.Length, 7); index++)
            {
                if (!completedDays[index])
                {
                    continue;
                }

                await InsertLegacyCompletionAsync(
                    connection,
                    habit.Id,
                    habit.UserId,
                    weekStart.AddDays(index));
            }
        }
    }

    private static async Task<List<LegacyHabit>> ReadLegacyHabitsAsync(DbConnection connection)
    {
        var legacyHabits = new List<LegacyHabit>();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Id", "UserId", "CompletedDaysRaw"
            FROM "Habits"
            WHERE "IsDeleted" = 0 AND "CompletedDaysRaw" LIKE '%true%';
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            legacyHabits.Add(new LegacyHabit(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2)));
        }

        return legacyHabits;
    }

    private static async Task InsertLegacyCompletionAsync(
        DbConnection connection,
        int habitId,
        int userId,
        DateOnly completedDate)
    {
        var now = DateTimeUtils.GetHorarioBrasilia();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO "HabitCompletions"
                ("HabitId", "UserId", "CompletedDate", "CompletedAt", "CreatedAt", "ModifiedAt", "IsDeleted")
            VALUES
                ($habitId, $userId, $completedDate, $completedAt, $createdAt, $modifiedAt, 0);
            """;

        AddParameter(command, "$habitId", habitId);
        AddParameter(command, "$userId", userId);
        AddParameter(command, "$completedDate", completedDate.ToString("yyyy-MM-dd"));
        AddParameter(command, "$completedAt", now);
        AddParameter(command, "$createdAt", now);
        AddParameter(command, "$modifiedAt", DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task LocalizeLegacyTextDataAsync(DbConnection connection)
    {
        await LocalizeLegacyHabitTitlesAsync(connection);
        await LocalizeLegacyHabitCategoriesAsync(connection);
        await LocalizeLegacyReminderMessagesAsync(connection);
    }

    private static async Task LocalizeLegacyHabitTitlesAsync(DbConnection connection)
    {
        if (!await ColumnExistsAsync(connection, "Habits", "Title"))
        {
            return;
        }

        await ExecuteNonQueryAsync(
            connection,
            """
            UPDATE "Habits"
            SET "Title" = CASE TRIM("Title")
                WHEN 'Beber Agua' THEN 'Beber Água'
                WHEN 'Drink Water' THEN 'Beber Água'
                WHEN 'Water' THEN 'Água'
                WHEN 'Reading' THEN 'Leitura'
                WHEN 'Meditation' THEN 'Meditação'
                WHEN 'Workout' THEN 'Treino'
                WHEN 'Exercise' THEN 'Exercício'
                WHEN 'Journal' THEN 'Diário'
                WHEN 'Run' THEN 'Corrida'
                ELSE "Title"
            END
            WHERE "IsDeleted" = 0
              AND TRIM("Title") IN (
                'Beber Agua',
                'Drink Water',
                'Water',
                'Reading',
                'Meditation',
                'Workout',
                'Exercise',
                'Journal',
                'Run'
              );
            """);
    }

    private static async Task LocalizeLegacyHabitCategoriesAsync(DbConnection connection)
    {
        if (!await ColumnExistsAsync(connection, "Habits", "Category"))
        {
            return;
        }

        await ExecuteNonQueryAsync(
            connection,
            """
            UPDATE "Habits"
            SET "Category" = CASE TRIM("Category")
                WHEN 'Personal' THEN 'Pessoal'
                WHEN 'Health' THEN 'Saúde'
                WHEN 'Fitness' THEN 'Atividade física'
                WHEN 'Wellness' THEN 'Bem-estar'
                WHEN 'Learning' THEN 'Aprendizado'
                WHEN 'Work' THEN 'Trabalho'
                ELSE "Category"
            END
            WHERE "IsDeleted" = 0
              AND TRIM("Category") IN (
                'Personal',
                'Health',
                'Fitness',
                'Wellness',
                'Learning',
                'Work'
              );
            """);
    }

    private static async Task LocalizeLegacyReminderMessagesAsync(DbConnection connection)
    {
        if (!await ColumnExistsAsync(connection, "Habits", "ReminderMessage"))
        {
            return;
        }

        await ExecuteNonQueryAsync(
            connection,
            """
            UPDATE "Habits"
            SET "ReminderMessage" = 'Hora de ' || SUBSTR("ReminderMessage", LENGTH('Time for ') + 1)
            WHERE "IsDeleted" = 0
              AND "ReminderMessage" LIKE 'Time for %.';
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            UPDATE "Habits"
            SET "ReminderMessage" = 'Conclua ' ||
                SUBSTR(
                    "ReminderMessage",
                    LENGTH('Complete ') + 1,
                    INSTR("ReminderMessage", ' today to protect') - LENGTH('Complete ') - 1
                ) ||
                ' hoje para proteger sua sequência.'
            WHERE "IsDeleted" = 0
              AND "ReminderMessage" LIKE 'Complete % today to protect your %-day streak.'
              AND INSTR("ReminderMessage", ' today to protect') > 0;
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            UPDATE "Habits"
            SET "ReminderMessage" = REPLACE("ReminderMessage", ' is still open today.', ' ainda está aberto hoje.')
            WHERE "IsDeleted" = 0
              AND "ReminderMessage" LIKE '% is still open today.';
            """);
    }

    private static async Task ExecuteNonQueryAsync(DbConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropLegacyHabitColumnsAsync(DbConnection connection)
    {
        if (await ColumnExistsAsync(connection, "Habits", "Streak"))
        {
            await ExecuteNonQueryAsync(connection, "ALTER TABLE \"Habits\" DROP COLUMN \"Streak\";");
        }

        if (await ColumnExistsAsync(connection, "Habits", "CompletedDaysRaw"))
        {
            await ExecuteNonQueryAsync(connection, "ALTER TABLE \"Habits\" DROP COLUMN \"CompletedDaysRaw\";");
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Sunday + 7) % 7;
        return date.AddDays(-offset);
    }

    private record LegacyHabit(int Id, int UserId, string CompletedDaysRaw);
}
