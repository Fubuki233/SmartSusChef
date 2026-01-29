using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSusChef.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidayAndWeatherCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HolidayCalendars",
                columns: table => new
                {
                    CountryCode = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    HolidaysJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayCalendars", x => new { x.CountryCode, x.Year });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WeatherDaily",
                columns: table => new
                {
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Condition = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Humidity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherDaily", x => new { x.StoreId, x.Date });
                    table.ForeignKey(
                        name: "FK_WeatherDaily_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HolidayCalendars");

            migrationBuilder.DropTable(
                name: "WeatherDaily");

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4390), new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4390) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4390), new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4390) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4410), new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4410) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4410), new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4410) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4410), new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4410) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4440), new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4440) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4440), new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4440) });

            migrationBuilder.UpdateData(
                table: "Store",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 58, 985, DateTimeKind.Utc).AddTicks(8100), new DateTime(2026, 1, 27, 11, 42, 58, 985, DateTimeKind.Utc).AddTicks(8100) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 96, DateTimeKind.Utc).AddTicks(7380), "$2a$11$b/6y0xvJkmDXxuTNJu0PlutfA.UDmSQSK48hGOQnw2NxCLEcNJThG", new DateTime(2026, 1, 27, 11, 42, 59, 96, DateTimeKind.Utc).AddTicks(7380) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4180), "$2a$11$1L1XFjTvpHjqy67yiTn4TOIkcj6NJtebzIKQxyw1GygATf7LFG/8O", new DateTime(2026, 1, 27, 11, 42, 59, 206, DateTimeKind.Utc).AddTicks(4180) });
        }
    }
}
