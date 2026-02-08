Build started...
Build succeeded.
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Ingredients` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Unit` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `CarbonFootprint` decimal(10,3) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Ingredients` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Recipes` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Recipes` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Users` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Username` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Role` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `WastageData` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Date` datetime(6) NOT NULL,
    `IngredientId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Quantity` decimal(10,3) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_WastageData` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_WastageData_Ingredients_IngredientId` FOREIGN KEY (`IngredientId`) REFERENCES `Ingredients` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `RecipeIngredients` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `RecipeId` char(36) COLLATE ascii_general_ci NOT NULL,
    `IngredientId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Quantity` decimal(10,3) NOT NULL,
    CONSTRAINT `PK_RecipeIngredients` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RecipeIngredients_Ingredients_IngredientId` FOREIGN KEY (`IngredientId`) REFERENCES `Ingredients` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_RecipeIngredients_Recipes_RecipeId` FOREIGN KEY (`RecipeId`) REFERENCES `Recipes` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `SalesData` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Date` datetime(6) NOT NULL,
    `RecipeId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Quantity` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_SalesData` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_SalesData_Recipes_RecipeId` FOREIGN KEY (`RecipeId`) REFERENCES `Recipes` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

INSERT INTO `Ingredients` (`Id`, `CarbonFootprint`, `CreatedAt`, `Name`, `Unit`, `UpdatedAt`)
VALUES ('33333333-3333-3333-3333-333333333333', 1.1, TIMESTAMP '2026-01-22 07:13:49', 'Tomato', 'kg', TIMESTAMP '2026-01-22 07:13:49'),
('44444444-4444-4444-4444-444444444444', 13.5, TIMESTAMP '2026-01-22 07:13:49', 'Cheese', 'kg', TIMESTAMP '2026-01-22 07:13:49'),
('55555555-5555-5555-5555-555555555555', 0.9, TIMESTAMP '2026-01-22 07:13:49', 'Dough', 'kg', TIMESTAMP '2026-01-22 07:13:49'),
('66666666-6666-6666-6666-666666666666', 0.5, TIMESTAMP '2026-01-22 07:13:49', 'Lettuce', 'kg', TIMESTAMP '2026-01-22 07:13:49'),
('77777777-7777-7777-7777-777777777777', 27.0, TIMESTAMP '2026-01-22 07:13:49', 'Beef', 'kg', TIMESTAMP '2026-01-22 07:13:49');

INSERT INTO `Recipes` (`Id`, `CreatedAt`, `Name`, `UpdatedAt`)
VALUES ('88888888-8888-8888-8888-888888888888', TIMESTAMP '2026-01-22 07:13:49', 'Margherita Pizza', TIMESTAMP '2026-01-22 07:13:49'),
('99999999-9999-9999-9999-999999999999', TIMESTAMP '2026-01-22 07:13:49', 'Beef Burger', TIMESTAMP '2026-01-22 07:13:49');

INSERT INTO `Users` (`Id`, `CreatedAt`, `Name`, `PasswordHash`, `Role`, `UpdatedAt`, `Username`)
VALUES ('11111111-1111-1111-1111-111111111111', TIMESTAMP '2026-01-22 07:13:49', 'Administrator', '$2a$11$pbgI3k5CaqI.2cK90xdVA.xzIdHlV1rlQlhsAdUalddbcRQsoISTy', 'Manager', TIMESTAMP '2026-01-22 07:13:49', 'admin'),
('22222222-2222-2222-2222-222222222222', TIMESTAMP '2026-01-22 07:13:49', 'Employee User', '$2a$11$OxYACAQvLTSi63.SAYYIGuJIRxqvD28TMLTDzb71u2IpGjFHAYze2', 'Employee', TIMESTAMP '2026-01-22 07:13:49', 'employee');

INSERT INTO `RecipeIngredients` (`Id`, `IngredientId`, `Quantity`, `RecipeId`)
VALUES ('37d4cf70-a55c-4ac9-92d4-138d8c3f9810', '66666666-6666-6666-6666-666666666666', 0.05, '99999999-9999-9999-9999-999999999999'),
('5e01e35c-571d-486f-8804-31028ea9a5db', '44444444-4444-4444-4444-444444444444', 0.15, '88888888-8888-8888-8888-888888888888'),
('83ce1726-1dfa-4822-b3f4-0989b1c8419c', '55555555-5555-5555-5555-555555555555', 0.3, '88888888-8888-8888-8888-888888888888'),
('9e81d73e-1c73-4337-8020-dd71dc7966d6', '33333333-3333-3333-3333-333333333333', 0.05, '99999999-9999-9999-9999-999999999999'),
('c74430e7-b008-46e9-b9ed-8bea4390e06e', '33333333-3333-3333-3333-333333333333', 0.2, '88888888-8888-8888-8888-888888888888'),
('dce5c65b-400b-4627-bcdb-8b6f41dba4cd', '77777777-7777-7777-7777-777777777777', 0.2, '99999999-9999-9999-9999-999999999999');

CREATE INDEX `IX_RecipeIngredients_IngredientId` ON `RecipeIngredients` (`IngredientId`);

CREATE INDEX `IX_RecipeIngredients_RecipeId` ON `RecipeIngredients` (`RecipeId`);

CREATE INDEX `IX_SalesData_Date` ON `SalesData` (`Date`);

CREATE INDEX `IX_SalesData_RecipeId` ON `SalesData` (`RecipeId`);

CREATE UNIQUE INDEX `IX_Users_Username` ON `Users` (`Username`);

CREATE INDEX `IX_WastageData_Date` ON `WastageData` (`Date`);

CREATE INDEX `IX_WastageData_IngredientId` ON `WastageData` (`IngredientId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260122071351_InitialCreate', '8.0.0');

COMMIT;

START TRANSACTION;

DELETE FROM `RecipeIngredients`
WHERE `Id` = '37d4cf70-a55c-4ac9-92d4-138d8c3f9810';
SELECT ROW_COUNT();


DELETE FROM `RecipeIngredients`
WHERE `Id` = '5e01e35c-571d-486f-8804-31028ea9a5db';
SELECT ROW_COUNT();


DELETE FROM `RecipeIngredients`
WHERE `Id` = '83ce1726-1dfa-4822-b3f4-0989b1c8419c';
SELECT ROW_COUNT();


DELETE FROM `RecipeIngredients`
WHERE `Id` = '9e81d73e-1c73-4337-8020-dd71dc7966d6';
SELECT ROW_COUNT();


DELETE FROM `RecipeIngredients`
WHERE `Id` = 'c74430e7-b008-46e9-b9ed-8bea4390e06e';
SELECT ROW_COUNT();


DELETE FROM `RecipeIngredients`
WHERE `Id` = 'dce5c65b-400b-4627-bcdb-8b6f41dba4cd';
SELECT ROW_COUNT();


CREATE TABLE `Store` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `StoreName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `OpeningDate` datetime(6) NOT NULL,
    `Latitude` decimal(10,7) NOT NULL,
    `Longitude` decimal(10,7) NOT NULL,
    `Address` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Store` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '33333333-3333-3333-3333-333333333333';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '44444444-4444-4444-4444-444444444444';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '55555555-5555-5555-5555-555555555555';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '66666666-6666-6666-6666-666666666666';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '77777777-7777-7777-7777-777777777777';
SELECT ROW_COUNT();


INSERT INTO `RecipeIngredients` (`Id`, `IngredientId`, `Quantity`, `RecipeId`)
VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '55555555-5555-5555-5555-555555555555', 0.3, '88888888-8888-8888-8888-888888888888'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '33333333-3333-3333-3333-333333333333', 0.2, '88888888-8888-8888-8888-888888888888'),
('cccccccc-cccc-cccc-cccc-cccccccccccc', '44444444-4444-4444-4444-444444444444', 0.15, '88888888-8888-8888-8888-888888888888'),
('dddddddd-dddd-dddd-dddd-dddddddddddd', '77777777-7777-7777-7777-777777777777', 0.2, '99999999-9999-9999-9999-999999999999'),
('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', '66666666-6666-6666-6666-666666666666', 0.05, '99999999-9999-9999-9999-999999999999'),
('ffffffff-ffff-ffff-ffff-ffffffffffff', '33333333-3333-3333-3333-333333333333', 0.05, '99999999-9999-9999-9999-999999999999');

UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '88888888-8888-8888-8888-888888888888';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '99999999-9999-9999-9999-999999999999';
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `PasswordHash` = '$2a$11$MCPwavHuml54EKfVTg.1/.v9X2PxHjazKeXUxJNCkvG9MHn7ruY3O', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '11111111-1111-1111-1111-111111111111';
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-26 07:47:26', `PasswordHash` = '$2a$11$O1DV9ldt8eHRHngnI8tD4OrRUo1S7o6KZzyjltx4ICNKB84HZwr6y', `UpdatedAt` = TIMESTAMP '2026-01-26 07:47:26'
WHERE `Id` = '22222222-2222-2222-2222-222222222222';
SELECT ROW_COUNT();


CREATE INDEX `IX_Store_IsActive` ON `Store` (`IsActive`);

CREATE INDEX `IX_Store_OpeningDate` ON `Store` (`OpeningDate`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260126074726_AddStoreTable', '8.0.0');

COMMIT;

START TRANSACTION;

ALTER TABLE `RecipeIngredients` DROP FOREIGN KEY `FK_RecipeIngredients_Ingredients_IngredientId`;

ALTER TABLE `WastageData` DROP FOREIGN KEY `FK_WastageData_Ingredients_IngredientId`;

ALTER TABLE `WastageData` DROP INDEX `IX_WastageData_Date`;

ALTER TABLE `Store` DROP INDEX `IX_Store_IsActive`;

ALTER TABLE `Store` DROP INDEX `IX_Store_OpeningDate`;

ALTER TABLE `SalesData` DROP INDEX `IX_SalesData_Date`;

ALTER TABLE `WastageData` MODIFY COLUMN `Quantity` decimal(18,3) NOT NULL;

ALTER TABLE `WastageData` MODIFY COLUMN `IngredientId` char(36) COLLATE ascii_general_ci NULL;

ALTER TABLE `WastageData` ADD `RecipeId` char(36) COLLATE ascii_general_ci NULL;

ALTER TABLE `WastageData` ADD `StoreId` int NOT NULL DEFAULT 0;

ALTER TABLE `Users` MODIFY COLUMN `Name` longtext CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Users` ADD `Email` varchar(255) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `Users` ADD `StoreId` int NOT NULL DEFAULT 0;

ALTER TABLE `Users` ADD `UserStatus` longtext CHARACTER SET utf8mb4 NOT NULL DEFAULT ('Active');

ALTER TABLE `Store` MODIFY COLUMN `Address` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Store` MODIFY COLUMN `Id` int NOT NULL;

ALTER TABLE `Store` ADD `CompanyName` varchar(255) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `Store` ADD `ContactNumber` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `Store` ADD `OutletLocation` varchar(200) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `Store` ADD `UEN` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `SalesData` ADD `StoreId` int NOT NULL DEFAULT 0;

ALTER TABLE `Recipes` MODIFY COLUMN `Name` longtext CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Recipes` ADD `IsSellable` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Recipes` ADD `IsSubRecipe` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Recipes` ADD `StoreId` int NOT NULL DEFAULT 0;

ALTER TABLE `RecipeIngredients` MODIFY COLUMN `Quantity` decimal(18,3) NOT NULL;

ALTER TABLE `RecipeIngredients` MODIFY COLUMN `IngredientId` char(36) COLLATE ascii_general_ci NULL;

ALTER TABLE `RecipeIngredients` ADD `ChildRecipeId` char(36) COLLATE ascii_general_ci NULL;

ALTER TABLE `Ingredients` MODIFY COLUMN `Unit` longtext CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Ingredients` MODIFY COLUMN `Name` longtext CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Ingredients` MODIFY COLUMN `CarbonFootprint` decimal(18,3) NOT NULL;

ALTER TABLE `Ingredients` ADD `StoreId` int NOT NULL DEFAULT 0;

CREATE TABLE `ForecastData` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `StoreId` int NOT NULL,
    `RecipeId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ForecastDate` datetime(6) NOT NULL,
    `PredictedQuantity` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_ForecastData` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ForecastData_Recipes_RecipeId` FOREIGN KEY (`RecipeId`) REFERENCES `Recipes` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ForecastData_Store_StoreId` FOREIGN KEY (`StoreId`) REFERENCES `Store` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `GlobalCalendarSignals` (
    `Date` datetime(6) NOT NULL,
    `IsHoliday` tinyint(1) NOT NULL,
    `HolidayName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `IsSchoolHoliday` tinyint(1) NOT NULL,
    `RainMm` decimal(10,2) NOT NULL,
    `WeatherDesc` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_GlobalCalendarSignals` PRIMARY KEY (`Date`)
) CHARACTER SET=utf8mb4;

UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42'
WHERE `Id` = '33333333-3333-3333-3333-333333333333';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42'
WHERE `Id` = '44444444-4444-4444-4444-444444444444';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42'
WHERE `Id` = '55555555-5555-5555-5555-555555555555';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42'
WHERE `Id` = '66666666-6666-6666-6666-666666666666';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42'
WHERE `Id` = '77777777-7777-7777-7777-777777777777';
SELECT ROW_COUNT();


UPDATE `RecipeIngredients` SET `ChildRecipeId` = NULL
WHERE `Id` = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
SELECT ROW_COUNT();


UPDATE `RecipeIngredients` SET `ChildRecipeId` = NULL
WHERE `Id` = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
SELECT ROW_COUNT();


UPDATE `RecipeIngredients` SET `ChildRecipeId` = NULL
WHERE `Id` = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
SELECT ROW_COUNT();


UPDATE `RecipeIngredients` SET `ChildRecipeId` = NULL
WHERE `Id` = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
SELECT ROW_COUNT();


UPDATE `RecipeIngredients` SET `ChildRecipeId` = NULL
WHERE `Id` = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
SELECT ROW_COUNT();


UPDATE `RecipeIngredients` SET `ChildRecipeId` = NULL
WHERE `Id` = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `IsSellable` = FALSE, `IsSubRecipe` = FALSE, `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42'
WHERE `Id` = '88888888-8888-8888-8888-888888888888';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `IsSellable` = FALSE, `IsSubRecipe` = FALSE, `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42'
WHERE `Id` = '99999999-9999-9999-9999-999999999999';
SELECT ROW_COUNT();


INSERT INTO `Store` (`Id`, `Address`, `CompanyName`, `ContactNumber`, `CreatedAt`, `IsActive`, `Latitude`, `Longitude`, `OpeningDate`, `OutletLocation`, `StoreName`, `UEN`, `UpdatedAt`)
VALUES (1, NULL, 'Smart Sus Chef Corp', '+65 6000 0000', TIMESTAMP '2026-01-27 11:29:42', TRUE, 0.0, 0.0, TIMESTAMP '2024-01-01 00:00:00', '123 Orchard Road', 'Downtown Outlet', '202400001A', TIMESTAMP '2026-01-27 11:29:42');

UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `Email` = '', `PasswordHash` = '$2a$11$jydgygNLVXDy35g2YVEmCODgYXsbxDrX818TQ7QRJH7yRK2t9JwWC', `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42', `UserStatus` = 'Active'
WHERE `Id` = '11111111-1111-1111-1111-111111111111';
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:29:42', `Email` = '', `PasswordHash` = '$2a$11$h5f5fpf6mnCRAYNieqtUfeap6WmM8TKUGgZkimWDceHUv2IYCMtrq', `StoreId` = 1, `UpdatedAt` = TIMESTAMP '2026-01-27 11:29:42', `UserStatus` = 'Active'
WHERE `Id` = '22222222-2222-2222-2222-222222222222';
SELECT ROW_COUNT();


CREATE INDEX `IX_WastageData_RecipeId` ON `WastageData` (`RecipeId`);

CREATE INDEX `IX_WastageData_StoreId` ON `WastageData` (`StoreId`);

CREATE INDEX `IX_Users_StoreId` ON `Users` (`StoreId`);

CREATE INDEX `IX_SalesData_StoreId` ON `SalesData` (`StoreId`);

CREATE INDEX `IX_Recipes_StoreId` ON `Recipes` (`StoreId`);

CREATE INDEX `IX_RecipeIngredients_ChildRecipeId` ON `RecipeIngredients` (`ChildRecipeId`);

CREATE INDEX `IX_Ingredients_StoreId` ON `Ingredients` (`StoreId`);

CREATE INDEX `IX_ForecastData_RecipeId` ON `ForecastData` (`RecipeId`);

CREATE INDEX `IX_ForecastData_StoreId` ON `ForecastData` (`StoreId`);

ALTER TABLE `Ingredients` ADD CONSTRAINT `FK_Ingredients_Store_StoreId` FOREIGN KEY (`StoreId`) REFERENCES `Store` (`Id`) ON DELETE CASCADE;

ALTER TABLE `RecipeIngredients` ADD CONSTRAINT `FK_RecipeIngredients_Ingredients_IngredientId` FOREIGN KEY (`IngredientId`) REFERENCES `Ingredients` (`Id`) ON DELETE SET NULL;

ALTER TABLE `RecipeIngredients` ADD CONSTRAINT `FK_RecipeIngredients_Recipes_ChildRecipeId` FOREIGN KEY (`ChildRecipeId`) REFERENCES `Recipes` (`Id`) ON DELETE RESTRICT;

ALTER TABLE `Recipes` ADD CONSTRAINT `FK_Recipes_Store_StoreId` FOREIGN KEY (`StoreId`) REFERENCES `Store` (`Id`) ON DELETE CASCADE;

ALTER TABLE `SalesData` ADD CONSTRAINT `FK_SalesData_Store_StoreId` FOREIGN KEY (`StoreId`) REFERENCES `Store` (`Id`) ON DELETE CASCADE;

ALTER TABLE `Users` ADD CONSTRAINT `FK_Users_Store_StoreId` FOREIGN KEY (`StoreId`) REFERENCES `Store` (`Id`) ON DELETE CASCADE;

ALTER TABLE `WastageData` ADD CONSTRAINT `FK_WastageData_Ingredients_IngredientId` FOREIGN KEY (`IngredientId`) REFERENCES `Ingredients` (`Id`);

ALTER TABLE `WastageData` ADD CONSTRAINT `FK_WastageData_Recipes_RecipeId` FOREIGN KEY (`RecipeId`) REFERENCES `Recipes` (`Id`);

ALTER TABLE `WastageData` ADD CONSTRAINT `FK_WastageData_Store_StoreId` FOREIGN KEY (`StoreId`) REFERENCES `Store` (`Id`) ON DELETE CASCADE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260127112942_UpdateSchemaToFinalERD', '8.0.0');

COMMIT;

START TRANSACTION;

UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '33333333-3333-3333-3333-333333333333';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '44444444-4444-4444-4444-444444444444';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '55555555-5555-5555-5555-555555555555';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '66666666-6666-6666-6666-666666666666';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '77777777-7777-7777-7777-777777777777';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '88888888-8888-8888-8888-888888888888';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '99999999-9999-9999-9999-999999999999';
SELECT ROW_COUNT();


UPDATE `Store` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:58', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:58'
WHERE `Id` = 1;
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `PasswordHash` = '$2a$11$b/6y0xvJkmDXxuTNJu0PlutfA.UDmSQSK48hGOQnw2NxCLEcNJThG', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '11111111-1111-1111-1111-111111111111';
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-27 11:42:59', `PasswordHash` = '$2a$11$1L1XFjTvpHjqy67yiTn4TOIkcj6NJtebzIKQxyw1GygATf7LFG/8O', `UpdatedAt` = TIMESTAMP '2026-01-27 11:42:59'
WHERE `Id` = '22222222-2222-2222-2222-222222222222';
SELECT ROW_COUNT();


INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260127114259_DropOldWeatherData', '8.0.0');

COMMIT;

START TRANSACTION;

CREATE TABLE `HolidayCalendars` (
    `CountryCode` varchar(2) CHARACTER SET utf8mb4 NOT NULL,
    `Year` int NOT NULL,
    `HolidaysJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_HolidayCalendars` PRIMARY KEY (`CountryCode`, `Year`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `WeatherDaily` (
    `StoreId` int NOT NULL,
    `Date` datetime(6) NOT NULL,
    `Temperature` decimal(10,2) NOT NULL,
    `Condition` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Humidity` int NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_WeatherDaily` PRIMARY KEY (`StoreId`, `Date`),
    CONSTRAINT `FK_WeatherDaily_Store_StoreId` FOREIGN KEY (`StoreId`) REFERENCES `Store` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '33333333-3333-3333-3333-333333333333';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '44444444-4444-4444-4444-444444444444';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '55555555-5555-5555-5555-555555555555';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '66666666-6666-6666-6666-666666666666';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '77777777-7777-7777-7777-777777777777';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '88888888-8888-8888-8888-888888888888';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '99999999-9999-9999-9999-999999999999';
SELECT ROW_COUNT();


UPDATE `Store` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = 1;
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `PasswordHash` = '$2a$11$ZmwH8Hdc0zP7eMdtzPC.DuooLxBTlCBWj.Lxyd4JMvUrbkImQ8Bfe', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '11111111-1111-1111-1111-111111111111';
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:04:17', `PasswordHash` = '$2a$11$lMrDmj8m8bVVY6CNdjntMOXPUKtSq9Js76kaOsCMr8DHuVcD.Ogk2', `UpdatedAt` = TIMESTAMP '2026-01-29 10:04:17'
WHERE `Id` = '22222222-2222-2222-2222-222222222222';
SELECT ROW_COUNT();


INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260129100419_AddHolidayAndWeatherCache', '8.0.0');

COMMIT;

START TRANSACTION;

ALTER TABLE `Store` ADD `CountryCode` varchar(2) CHARACTER SET utf8mb4 NULL;

UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '33333333-3333-3333-3333-333333333333';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '44444444-4444-4444-4444-444444444444';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '55555555-5555-5555-5555-555555555555';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '66666666-6666-6666-6666-666666666666';
SELECT ROW_COUNT();


UPDATE `Ingredients` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '77777777-7777-7777-7777-777777777777';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '88888888-8888-8888-8888-888888888888';
SELECT ROW_COUNT();


UPDATE `Recipes` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '99999999-9999-9999-9999-999999999999';
SELECT ROW_COUNT();


UPDATE `Store` SET `CountryCode` = NULL, `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = 1;
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `PasswordHash` = '$2a$11$UgKlLTqGhRjy8vBUxfq4D.rAslh1BvCkBfTNIFeLmL5pmyk1p.1he', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '11111111-1111-1111-1111-111111111111';
SELECT ROW_COUNT();


UPDATE `Users` SET `CreatedAt` = TIMESTAMP '2026-01-29 10:32:34', `PasswordHash` = '$2a$11$9cbaY78NhA4ZnhWUdW9hs.Hh.OiEoL1RZTaBzL3Ng5fPoqUwWgJz2', `UpdatedAt` = TIMESTAMP '2026-01-29 10:32:34'
WHERE `Id` = '22222222-2222-2222-2222-222222222222';
SELECT ROW_COUNT();


INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260129103234_AddStoreCountryCode', '8.0.0');

COMMIT;


