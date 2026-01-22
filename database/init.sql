-- SmartSusChef Database Initialization Script
-- This script is executed automatically when MySQL container starts for the first time

USE smartsuschef;

-- Create tables will be created by EF Core migrations
-- This file is kept for future manual SQL operations if needed

-- Set timezone
SET time_zone = '+08:00';

-- Enable event scheduler for future scheduled tasks
SET GLOBAL event_scheduler = ON;