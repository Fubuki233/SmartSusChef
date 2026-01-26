using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartSusChef.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("37d4cf70-a55c-4ac9-92d4-138d8c3f9810"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("5e01e35c-571d-486f-8804-31028ea9a5db"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("83ce1726-1dfa-4822-b3f4-0989b1c8419c"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("9e81d73e-1c73-4337-8020-dd71dc7966d6"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("c74430e7-b008-46e9-b9ed-8bea4390e06e"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("dce5c65b-400b-4627-bcdb-8b6f41dba4cd"));

            migrationBuilder.CreateTable(
                name: "Store",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StoreName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpeningDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    Address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Store", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7582), new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7598) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7601), new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7601) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7604), new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7604) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7606), new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7607) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7698), new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7698) });

            migrationBuilder.InsertData(
                table: "RecipeIngredients",
                columns: new[] { "Id", "IngredientId", "Quantity", "RecipeId" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new Guid("55555555-5555-5555-5555-555555555555"), 0.3m, new Guid("88888888-8888-8888-8888-888888888888") },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new Guid("33333333-3333-3333-3333-333333333333"), 0.2m, new Guid("88888888-8888-8888-8888-888888888888") },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new Guid("44444444-4444-4444-4444-444444444444"), 0.15m, new Guid("88888888-8888-8888-8888-888888888888") },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new Guid("77777777-7777-7777-7777-777777777777"), 0.2m, new Guid("99999999-9999-9999-9999-999999999999") },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), new Guid("66666666-6666-6666-6666-666666666666"), 0.05m, new Guid("99999999-9999-9999-9999-999999999999") },
                    { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), new Guid("33333333-3333-3333-3333-333333333333"), 0.05m, new Guid("99999999-9999-9999-9999-999999999999") }
                });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7754), new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7756) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7758), new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(7758) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 83, DateTimeKind.Utc).AddTicks(9455), "$2a$11$MCPwavHuml54EKfVTg.1/.v9X2PxHjazKeXUxJNCkvG9MHn7ruY3O", new DateTime(2026, 1, 26, 7, 47, 26, 83, DateTimeKind.Utc).AddTicks(9464) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(6852), "$2a$11$O1DV9ldt8eHRHngnI8tD4OrRUo1S7o6KZzyjltx4ICNKB84HZwr6y", new DateTime(2026, 1, 26, 7, 47, 26, 230, DateTimeKind.Utc).AddTicks(6859) });

            migrationBuilder.CreateIndex(
                name: "IX_Store_IsActive",
                table: "Store",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Store_OpeningDate",
                table: "Store",
                column: "OpeningDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Store");

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(2996), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3015) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3018), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3019) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3022), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3022) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3025), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3025) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3028), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3028) });

            migrationBuilder.InsertData(
                table: "RecipeIngredients",
                columns: new[] { "Id", "IngredientId", "Quantity", "RecipeId" },
                values: new object[,]
                {
                    { new Guid("37d4cf70-a55c-4ac9-92d4-138d8c3f9810"), new Guid("66666666-6666-6666-6666-666666666666"), 0.05m, new Guid("99999999-9999-9999-9999-999999999999") },
                    { new Guid("5e01e35c-571d-486f-8804-31028ea9a5db"), new Guid("44444444-4444-4444-4444-444444444444"), 0.15m, new Guid("88888888-8888-8888-8888-888888888888") },
                    { new Guid("83ce1726-1dfa-4822-b3f4-0989b1c8419c"), new Guid("55555555-5555-5555-5555-555555555555"), 0.3m, new Guid("88888888-8888-8888-8888-888888888888") },
                    { new Guid("9e81d73e-1c73-4337-8020-dd71dc7966d6"), new Guid("33333333-3333-3333-3333-333333333333"), 0.05m, new Guid("99999999-9999-9999-9999-999999999999") },
                    { new Guid("c74430e7-b008-46e9-b9ed-8bea4390e06e"), new Guid("33333333-3333-3333-3333-333333333333"), 0.2m, new Guid("88888888-8888-8888-8888-888888888888") },
                    { new Guid("dce5c65b-400b-4627-bcdb-8b6f41dba4cd"), new Guid("77777777-7777-7777-7777-777777777777"), 0.2m, new Guid("99999999-9999-9999-9999-999999999999") }
                });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3086), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3088) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3090), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3091) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 208, DateTimeKind.Utc).AddTicks(7825), "$2a$11$pbgI3k5CaqI.2cK90xdVA.xzIdHlV1rlQlhsAdUalddbcRQsoISTy", new DateTime(2026, 1, 22, 7, 13, 49, 208, DateTimeKind.Utc).AddTicks(7833) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(2167), "$2a$11$OxYACAQvLTSi63.SAYYIGuJIRxqvD28TMLTDzb71u2IpGjFHAYze2", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(2173) });
        }
    }
}
