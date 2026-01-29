using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSusChef.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreCountryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Store",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6434), new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6434) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6438), new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6439) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6459), new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6460) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6463), new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6463) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6466), new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6467) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6562), new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6564) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6566), new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(6567) });

            migrationBuilder.UpdateData(
                table: "Store",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CountryCode", "CreatedAt", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 29, 10, 32, 34, 7, DateTimeKind.Utc).AddTicks(7704), new DateTime(2026, 1, 29, 10, 32, 34, 7, DateTimeKind.Utc).AddTicks(7707) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 125, DateTimeKind.Utc).AddTicks(4058), "$2a$11$UgKlLTqGhRjy8vBUxfq4D.rAslh1BvCkBfTNIFeLmL5pmyk1p.1he", new DateTime(2026, 1, 29, 10, 32, 34, 125, DateTimeKind.Utc).AddTicks(4067) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(5605), "$2a$11$9cbaY78NhA4ZnhWUdW9hs.Hh.OiEoL1RZTaBzL3Ng5fPoqUwWgJz2", new DateTime(2026, 1, 29, 10, 32, 34, 246, DateTimeKind.Utc).AddTicks(5611) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Store");

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9440), new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9441) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9462), new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9462) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9465), new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9466) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9468), new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9468) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9471), new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9472) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9571), new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9572) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9575), new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(9576) });

            migrationBuilder.UpdateData(
                table: "Store",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 228, DateTimeKind.Utc).AddTicks(4501), new DateTime(2026, 1, 29, 10, 4, 17, 228, DateTimeKind.Utc).AddTicks(4504) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 346, DateTimeKind.Utc).AddTicks(8748), "$2a$11$ZmwH8Hdc0zP7eMdtzPC.DuooLxBTlCBWj.Lxyd4JMvUrbkImQ8Bfe", new DateTime(2026, 1, 29, 10, 4, 17, 346, DateTimeKind.Utc).AddTicks(8759) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(8711), "$2a$11$lMrDmj8m8bVVY6CNdjntMOXPUKtSq9Js76kaOsCMr8DHuVcD.Ogk2", new DateTime(2026, 1, 29, 10, 4, 17, 458, DateTimeKind.Utc).AddTicks(8719) });
        }
    }
}
