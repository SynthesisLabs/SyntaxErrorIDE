-- 
--  copy and paste this into your sql host if you don't yet have a database
--  we will definitely need more tables in the future

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
BEGIN;
SET time_zone = "+00:00";
    
--
--  make user table
--
    
DROP DATABASE IF EXISTS `syntaxerroride`;
CREATE DATABASE `syntaxerroride` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci; 
USE `syntaxerroride`;
     
--
-- make user table
--
     
DROP TABLE IF EXISTS `users`;
CREATE TABLE IF NOT EXISTS `users`
(
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(50) NOT NULL,
    `email` VARCHAR(255) NOT NULL UNIQUE,
    `password` VARCHAR(255) NOT NULL,
    `creation_date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `last_edited` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

COMMIT;