using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSusChef.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropOldWeatherData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(560), new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570), new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570), new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570), new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570), new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(570) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(590), new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(590) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(590), new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(590) });

            migrationBuilder.UpdateData(
                table: "Store",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 190, DateTimeKind.Utc).AddTicks(8310), new DateTime(2026, 1, 27, 11, 29, 42, 190, DateTimeKind.Utc).AddTicks(8310) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 299, DateTimeKind.Utc).AddTicks(4770), "$2a$11$jydgygNLVXDy35g2YVEmCODgYXsbxDrX818TQ7QRJH7yRK2t9JwWC", new DateTime(2026, 1, 27, 11, 29, 42, 299, DateTimeKind.Utc).AddTicks(4770) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(470), "$2a$11$h5f5fpf6mnCRAYNieqtUfeap6WmM8TKUGgZkimWDceHUv2IYCMtrq", new DateTime(2026, 1, 27, 11, 29, 42, 408, DateTimeKind.Utc).AddTicks(470) });
        }
    }
}
