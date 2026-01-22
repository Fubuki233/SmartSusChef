using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartSusChef.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CarbonFootprint = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WastageData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IngredientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Quantity = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WastageData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WastageData_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RecipeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IngredientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Quantity = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalesData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RecipeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesData_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "CarbonFootprint", "CreatedAt", "Name", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333333"), 1.1m, new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(2996), "Tomato", "kg", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3015) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 13.5m, new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3018), "Cheese", "kg", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3019) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 0.9m, new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3022), "Dough", "kg", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3022) },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 0.5m, new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3025), "Lettuce", "kg", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3025) },
                    { new Guid("77777777-7777-7777-7777-777777777777"), 27.0m, new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3028), "Beef", "kg", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3028) }
                });

            migrationBuilder.InsertData(
                table: "Recipes",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3086), "Margherita Pizza", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3088) },
                    { new Guid("99999999-9999-9999-9999-999999999999"), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3090), "Beef Burger", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(3091) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Name", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 22, 7, 13, 49, 208, DateTimeKind.Utc).AddTicks(7825), "Administrator", "$2a$11$pbgI3k5CaqI.2cK90xdVA.xzIdHlV1rlQlhsAdUalddbcRQsoISTy", "Manager", new DateTime(2026, 1, 22, 7, 13, 49, 208, DateTimeKind.Utc).AddTicks(7833), "admin" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(2167), "Employee User", "$2a$11$OxYACAQvLTSi63.SAYYIGuJIRxqvD28TMLTDzb71u2IpGjFHAYze2", "Employee", new DateTime(2026, 1, 22, 7, 13, 49, 366, DateTimeKind.Utc).AddTicks(2173), "employee" }
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_Date",
                table: "SalesData",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_RecipeId",
                table: "SalesData",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WastageData_Date",
                table: "WastageData",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_WastageData_IngredientId",
                table: "WastageData",
                column: "IngredientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "SalesData");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WastageData");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "Ingredients");
        }
    }
}
