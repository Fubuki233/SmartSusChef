using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSusChef.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaToFinalERD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_Ingredients_IngredientId",
                table: "RecipeIngredients");

            migrationBuilder.DropForeignKey(
                name: "FK_WastageData_Ingredients_IngredientId",
                table: "WastageData");

            migrationBuilder.DropIndex(
                name: "IX_WastageData_Date",
                table: "WastageData");

            migrationBuilder.DropIndex(
                name: "IX_Store_IsActive",
                table: "Store");

            migrationBuilder.DropIndex(
                name: "IX_Store_OpeningDate",
                table: "Store");

            migrationBuilder.DropIndex(
                name: "IX_SalesData_Date",
                table: "SalesData");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "WastageData",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)",
                oldPrecision: 10,
                oldScale: 3);

            migrationBuilder.AlterColumn<Guid>(
                name: "IngredientId",
                table: "WastageData",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "RecipeId",
                table: "WastageData",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "WastageData",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserStatus",
                table: "Users",
                type: "longtext",
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Store",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Store",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Store",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "Store",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OutletLocation",
                table: "Store",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UEN",
                table: "Store",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "SalesData",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Recipes",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsSellable",
                table: "Recipes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubRecipe",
                table: "Recipes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Recipes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "RecipeIngredients",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)",
                oldPrecision: 10,
                oldScale: 3);

            migrationBuilder.AlterColumn<Guid>(
                name: "IngredientId",
                table: "RecipeIngredients",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "ChildRecipeId",
                table: "RecipeIngredients",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Ingredients",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Ingredients",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "CarbonFootprint",
                table: "Ingredients",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)",
                oldPrecision: 10,
                oldScale: 3);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Ingredients",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ForecastData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    RecipeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ForecastDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PredictedQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastData_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForecastData_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GlobalCalendarSignals",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsHoliday = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HolidayName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSchoolHoliday = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RainMm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    WeatherDesc = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalCalendarSignals", x => x.Date);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "StoreId", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(560), 1, new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "StoreId", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570), 1, new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "StoreId", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570), 1, new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "StoreId", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570), 1, new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "StoreId", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570), 1, new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "ChildRecipeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "ChildRecipeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "ChildRecipeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                column: "ChildRecipeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                column: "ChildRecipeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "RecipeIngredients",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                column: "ChildRecipeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "IsSellable", "IsSubRecipe", "StoreId", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(590), false, false, 1, new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(590) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "IsSellable", "IsSubRecipe", "StoreId", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(590), false, false, 1, new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(590) });

            migrationBuilder.InsertData(
                table: "Store",
                columns: new[] { "Id", "Address", "CompanyName", "ContactNumber", "CreatedAt", "IsActive", "Latitude", "Longitude", "OpeningDate", "OutletLocation", "StoreName", "UEN", "UpdatedAt" },
                values: new object[] { 1, null, "Smart Sus Chef Corp", "+65 6000 0000", new DateTime(2026, 1, 27, 11, 29, 42, 190, DateTimeKind.Utc).AddTicks(8310), true, 0m, 0m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "123 Orchard Road", "Downtown Outlet", "202400001A", new DateTime(2026, 1, 27, 11, 29, 42, 190, DateTimeKind.Utc).AddTicks(8310) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Email", "PasswordHash", "StoreId", "UpdatedAt", "UserStatus" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 299, DateTimeKind.Utc).AddTicks(4770), "", "$2a$11$jydgygNLVXDy35g2YVEmCODgYXsbxDrX818TQ7QRJH7yRK2t9JwWC", 1, new DateTime(2026, 1, 27, 11, 29, 42, 299, DateTimeKind.Utc).AddTicks(4770), "Active" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "Email", "PasswordHash", "StoreId", "UpdatedAt", "UserStatus" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(470), "", "$2a$11$h5f5fpf6mnCRAYNieqtUfeap6WmM8TKUGgZkimWDceHUv2IYCMtrq", 1, new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(470), "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_WastageData_RecipeId",
                table: "WastageData",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_WastageData_StoreId",
                table: "WastageData",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StoreId",
                table: "Users",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_StoreId",
                table: "SalesData",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_StoreId",
                table: "Recipes",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_ChildRecipeId",
                table: "RecipeIngredients",
                column: "ChildRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_StoreId",
                table: "Ingredients",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastData_RecipeId",
                table: "ForecastData",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastData_StoreId",
                table: "ForecastData",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_Store_StoreId",
                table: "Ingredients",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_Ingredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_Recipes_ChildRecipeId",
                table: "RecipeIngredients",
                column: "ChildRecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Store_StoreId",
                table: "Recipes",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesData_Store_StoreId",
                table: "SalesData",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Store_StoreId",
                table: "Users",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WastageData_Ingredients_IngredientId",
                table: "WastageData",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WastageData_Recipes_RecipeId",
                table: "WastageData",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WastageData_Store_StoreId",
                table: "WastageData",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ingredients_Store_StoreId",
                table: "Ingredients");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_Ingredients_IngredientId",
                table: "RecipeIngredients");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_Recipes_ChildRecipeId",
                table: "RecipeIngredients");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Store_StoreId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesData_Store_StoreId",
                table: "SalesData");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Store_StoreId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_WastageData_Ingredients_IngredientId",
                table: "WastageData");

            migrationBuilder.DropForeignKey(
                name: "FK_WastageData_Recipes_RecipeId",
                table: "WastageData");

            migrationBuilder.DropForeignKey(
                name: "FK_WastageData_Store_StoreId",
                table: "WastageData");

            migrationBuilder.DropTable(
                name: "ForecastData");

            migrationBuilder.DropTable(
                name: "GlobalCalendarSignals");

            migrationBuilder.DropIndex(
                name: "IX_WastageData_RecipeId",
                table: "WastageData");

            migrationBuilder.DropIndex(
                name: "IX_WastageData_StoreId",
                table: "WastageData");

            migrationBuilder.DropIndex(
                name: "IX_Users_StoreId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SalesData_StoreId",
                table: "SalesData");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_StoreId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_ChildRecipeId",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_StoreId",
                table: "Ingredients");

            migrationBuilder.DeleteData(
                table: "Store",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "WastageData");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "WastageData");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Store");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "Store");

            migrationBuilder.DropColumn(
                name: "OutletLocation",
                table: "Store");

            migrationBuilder.DropColumn(
                name: "UEN",
                table: "Store");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "SalesData");

            migrationBuilder.DropColumn(
                name: "IsSellable",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "IsSubRecipe",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ChildRecipeId",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Ingredients");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "WastageData",
                type: "decimal(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<Guid>(
                name: "IngredientId",
                table: "WastageData",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Store",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Store",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Recipes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "RecipeIngredients",
                type: "decimal(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<Guid>(
                name: "IngredientId",
                table: "RecipeIngredients",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Ingredients",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Ingredients",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "CarbonFootprint",
                table: "Ingredients",
                type: "decimal(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

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
                name: "IX_WastageData_Date",
                table: "WastageData",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Store_IsActive",
                table: "Store",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Store_OpeningDate",
                table: "Store",
                column: "OpeningDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_Date",
                table: "SalesData",
                column: "Date");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_Ingredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WastageData_Ingredients_IngredientId",
                table: "WastageData",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
