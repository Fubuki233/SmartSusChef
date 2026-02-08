-- SmartSusChef: Create GlobalIngredients Table
-- Created: 2026-02-08
-- Description: Creates immutable global ingredients table with 20 default items

CREATE TABLE IF NOT EXISTS `GlobalIngredients` (
  `Id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Unit` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CarbonFootprint` decimal(18,3) NOT NULL,
  `IsDefault` tinyint(1) NOT NULL DEFAULT 0,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_GlobalIngredients_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert default 20 ingredients
INSERT INTO `GlobalIngredients` (`Id`, `Name`, `Unit`, `CarbonFootprint`, `IsDefault`, `CreatedAt`, `UpdatedAt`) VALUES
('a0000000-0000-0000-0000-000000000001', 'Tomato', 'kg', 1.100, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000002', 'Cheese', 'kg', 13.500, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000003', 'Flour', 'kg', 0.950, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000004', 'Rice', 'kg', 2.700, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000005', 'Beef', 'kg', 27.000, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000006', 'Pork', 'kg', 12.100, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000007', 'Chicken', 'kg', 6.900, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000008', 'Lettuce', 'kg', 0.500, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000009', 'Potato', 'kg', 0.300, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-00000000000A', 'Onion', 'kg', 0.700, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-00000000000B', 'Garlic', 'kg', 1.500, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-00000000000C', 'Sugar', 'kg', 1.800, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-00000000000D', 'Butter', 'kg', 11.900, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-00000000000E', 'Milk', 'L', 1.900, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-00000000000F', 'Egg', 'pcs', 0.300, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000010', 'Olive Oil', 'L', 6.000, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000011', 'Soy Sauce', 'L', 2.200, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000012', 'Salt', 'kg', 0.050, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000013', 'Pepper', 'kg', 8.000, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000014', 'Dough', 'kg', 0.900, 1, NOW(), NOW());
