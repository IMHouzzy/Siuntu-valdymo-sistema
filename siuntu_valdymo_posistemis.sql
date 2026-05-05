-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: May 05, 2026 at 06:06 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `siuntu_valdymo_posistemis`
--

-- --------------------------------------------------------

--
-- Table structure for table `category`
--

CREATE TABLE `category` (
  `id_Category` int(11) NOT NULL,
  `name` varchar(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `category`
--

INSERT INTO `category` (`id_Category`, `name`) VALUES
(1, 'Paslaugos'),
(2, 'Prekės'),
(3, 'Ilgalaikis turtas');

-- --------------------------------------------------------

--
-- Table structure for table `client_company`
--

CREATE TABLE `client_company` (
  `fk_Clientid_Users` int(11) NOT NULL,
  `fk_Companyid_Company` int(11) NOT NULL,
  `externalClientId` int(11) DEFAULT NULL,
  `deliveryAddress` varchar(255) DEFAULT NULL,
  `city` varchar(255) DEFAULT NULL,
  `country` varchar(255) DEFAULT NULL,
  `vat` varchar(255) DEFAULT NULL,
  `bankCode` int(5) DEFAULT NULL,
  `createdAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `client_company`
--

INSERT INTO `client_company` (`fk_Clientid_Users`, `fk_Companyid_Company`, `externalClientId`, `deliveryAddress`, `city`, `country`, `vat`, `bankCode`, `createdAt`) VALUES
(22, 1, 1, 'Laisvės al. 15', 'Kaunas', 'Lietuvos Respublika', 'LT488846005245', 73000, '2026-05-05 12:18:22'),
(23, 1, 2, 'Gedimino g. 8', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:23'),
(24, 1, 3, 'Savanorių pr. 22', 'Kaunas', 'Lietuvos Respublika', '', 73000, '2026-05-05 12:18:23'),
(25, 1, 4, 'K.Donelaičio g. 7', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:23'),
(26, 1, 5, 'Pilies g. 5', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:23'),
(27, 1, 7, 'Antakalnio g. 19', 'Vilnius', 'Lietuvos Respublika', 'LT343461241', NULL, '2026-05-05 12:18:23'),
(28, 1, 8, 'Žirmūnų g. 43', 'Vilnius', 'Lietuvos Respublika', 'LT385127646', NULL, '2026-05-05 12:18:23'),
(29, 1, 9, 'Nemuno g. 4', 'Kaunas', 'Lietuvos Respublika', 'LT212956347', NULL, '2026-05-05 12:18:23'),
(30, 1, 10, 'Tvirtovės al. 39', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:23'),
(31, 1, 11, 'Vytauto pr. 25', 'Kaunas', 'Lietuvos Respublika', 'LT488846670', NULL, '2026-05-05 12:18:24'),
(32, 1, 12, 'Gedimino g. 5', 'Marijampolė', 'Lietuvos Respublika', 'LT644285044', NULL, '2026-05-05 12:18:24'),
(33, 1, 13, 'Jonavos g. 5', 'Kaunas', 'Lietuvos Respublika', 'LT488888057443', NULL, '2026-05-05 12:18:24'),
(34, 1, 14, 'Perkūno al. 17', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:24'),
(35, 1, 15, 'Žalgirio g. 44', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:24'),
(36, 1, 16, 'Kalvarijų g. 8', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:24'),
(37, 1, 17, 'Ukmergės g. 55', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:24'),
(38, 1, 18, 'Vilijampolės g. 18', 'Kaunas', 'Lietuvos Respublika', 'LT488846618943', NULL, '2026-05-05 12:18:25'),
(39, 1, 19, 'Demokratų g. 28', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:25'),
(40, 1, 20, 'Tilžės g. 144', 'Šiauliai', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:25'),
(41, 1, 21, 'Aušros al. 53', 'Šiauliai', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:25'),
(42, 1, 22, 'Kęstučio g. 22', 'Kaunas', 'Lietuvos Respublika', 'LT488841584042', NULL, '2026-05-05 12:18:25'),
(43, 1, 23, 'Panerių g. 51', 'Kaunas', 'Lietuvos Respublika', 'LT488848005140', NULL, '2026-05-05 12:18:25'),
(44, 1, 24, 'Šilutės g. 12', 'Kaunas', 'Lietuvos Respublika', 'LT488880428445', NULL, '2026-05-05 12:18:25'),
(45, 1, 25, 'Maironio g. 4', 'Raseiniai', 'Lietuvos Respublika', 'LT488844332640', NULL, '2026-05-05 12:18:25'),
(46, 1, 26, 'Triq San Ġorġ 5', 'Valletta', 'Maltos Respublika', 'LT488884617842', NULL, '2026-05-05 12:18:26'),
(47, 1, 27, 'Mickevičiaus g. 11', 'Kaunas', 'Lietuvos Respublika', 'LT488841103845', NULL, '2026-05-05 12:18:26'),
(48, 1, 28, 'Konstitucijos pr. 16', 'Vilnius', 'Lietuvos Respublika', 'LT488886452944', NULL, '2026-05-05 12:18:26'),
(49, 1, 29, 'Vasaros g. 9', 'Kaunas', 'Lietuvos Respublika', 'LT488886356745', NULL, '2026-05-05 12:18:26'),
(50, 1, 30, 'Respublikos g. 12', 'Biržai', 'Lietuvos Respublika', 'LT488886025843', NULL, '2026-05-05 12:18:26'),
(51, 1, 31, 'Ąžuolų g. 7', 'Kaunas', 'Lietuvos Respublika', 'LT488844447545', NULL, '2026-05-05 12:18:26'),
(52, 1, 32, 'Žvėryno g. 6', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:26'),
(53, 1, 33, 'Naugarduko g. 48', 'Vilnius', 'Lietuvos Respublika', 'LT488884078445', NULL, '2026-05-05 12:18:26'),
(54, 1, 34, 'Ozo g. 25', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:26'),
(55, 1, 35, 'Savanorių pr. 3', 'Vilnius', 'Lietuvos Respublika', 'LT314483147', NULL, '2026-05-05 12:18:27'),
(56, 1, 36, 'Lazdynų g. 13', 'Kaunas', 'Lietuvos Respublika', 'LT488844937247', NULL, '2026-05-05 12:18:27'),
(57, 1, 37, 'Saltoniškių g. 8', 'Vilnius', 'Lietuvos Respublika', 'LT488887031848', NULL, '2026-05-05 12:18:27'),
(58, 1, 38, 'Rudninkų g. 4', 'Vilnius', 'Lietuvos Respublika', 'LT488887457741', NULL, '2026-05-05 12:18:27'),
(59, 1, 39, 'Mokyklos g. 17', 'Kaunas', 'Lietuvos Respublika', 'LT488844352541', NULL, '2026-05-05 12:18:27'),
(60, 1, 40, 'Bažnyčios g. 2', 'Kaišiadorys', 'Lietuvos Respublika', 'LT600217442', NULL, '2026-05-05 12:18:27'),
(61, 1, 41, 'Pergalės g. 9', 'Kaunas', 'Lietuvos Respublika', 'LT229576241', NULL, '2026-05-05 12:18:27'),
(62, 1, 42, 'Liepkalnio g. 72', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:28'),
(63, 1, 43, 'Pramonės pr. 6', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:28'),
(64, 1, 44, 'Viršuliškių g. 40', 'Kaunas', 'Lietuvos Respublika', 'LT488843642049', NULL, '2026-05-05 12:18:28'),
(65, 1, 45, 'Kęstučio g. 3', 'Raseiniai', 'Lietuvos Respublika', 'LT488846850', NULL, '2026-05-05 12:18:28'),
(66, 1, 46, 'Triq il-Vittorja 8', 'Valletta', 'Maltos Respublika', '', NULL, '2026-05-05 12:18:28'),
(67, 1, 47, 'Šiaurės g. 22', 'Kaunas', 'Lietuvos Respublika', 'LT488846708242', NULL, '2026-05-05 12:18:28'),
(68, 1, 48, 'Didžioji g. 11', 'Vilnius', 'Lietuvos Respublika', 'LT488848003842', NULL, '2026-05-05 12:18:28'),
(69, 1, 49, 'Trakų g. 15', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:28'),
(70, 1, 50, 'Triq il-Merkanti 3', 'Valletta', 'Maltos Respublika', '', NULL, '2026-05-05 12:18:28'),
(71, 1, 51, 'Hauptstraße 42', 'Berlin', 'Vokietijos Federacinė Respublika', '', NULL, '2026-05-05 12:18:29'),
(72, 1, 52, 'Rue de la Liberté 15', 'Paris', 'Prancūzijos Respublika', '', NULL, '2026-05-05 12:18:29'),
(73, 1, 53, 'Collins Street 8', 'Melbourne', 'Australija', '', NULL, '2026-05-05 12:18:29'),
(74, 1, 54, 'High Street 21', 'London', 'Jungt.Didž.Brit. ir Š.Airijos Karalystė', '', NULL, '2026-05-05 12:18:29'),
(75, 1, 55, 'Bajcsy-Zsilinszky út 10', 'Budapest', 'Vengrijos Respublika', '', NULL, '2026-05-05 12:18:29'),
(76, 1, 56, 'O\'Connell Street 3', 'Dublin', 'Airija', '', NULL, '2026-05-05 12:18:29'),
(77, 1, 57, 'Avenue des Champs 7', 'Lyon', 'Prancūzijos Respublika', '', NULL, '2026-05-05 12:18:29'),
(78, 1, 58, 'Grafton Street 12', 'Dublin', 'Airija', '', NULL, '2026-05-05 12:18:29'),
(79, 1, 59, 'Oxford Street 45', 'London', 'Jungt.Didž.Brit. ir Š.Airijos Karalystė', '', NULL, '2026-05-05 12:18:30'),
(80, 1, 60, 'Avenida da Liberdade 50', 'Lisbon', 'Portugalijos Respublika', '', NULL, '2026-05-05 12:18:30'),
(81, 1, 61, 'Mannerheimintie 10', 'Helsinki', 'Suomijos Respublika', '', NULL, '2026-05-05 12:18:30'),
(82, 1, 62, 'Gran Vía 20', 'Madrid', 'Ispanijos Karalystė', '', NULL, '2026-05-05 12:18:30'),
(83, 1, 63, 'Friedrichstraße 30', 'Berlin', 'Vokietijos Federacinė Respublika', '', NULL, '2026-05-05 12:18:30'),
(84, 1, 64, 'Baker Street 10', 'London', 'Jungt.Didž.Brit. ir Š.Airijos Karalystė', '', NULL, '2026-05-05 12:18:30'),
(85, 1, 65, 'Vasario 16-osios g. 6', 'Kaunas', 'Lietuvos Respublika', 'LT488883788047', NULL, '2026-05-05 12:18:30'),
(86, 1, 66, 'Šventaragio g. 4', 'Vilnius', 'Lietuvos Respublika', 'LT353335241', NULL, '2026-05-05 12:18:30'),
(87, 1, 67, 'Kauno g. 3', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:31'),
(88, 1, 68, 'Dvaro g. 12', 'Kaunas', 'Lietuvos Respublika', 'LT360429347', NULL, '2026-05-05 12:18:31'),
(89, 1, 69, 'Partizanų g. 66', 'Kaunas', 'Lietuvos Respublika', 'LT213717646', NULL, '2026-05-05 12:18:31'),
(90, 1, 70, 'Architektų g. 33', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:31'),
(91, 1, 71, 'Gėlių g. 5', 'Vilnius', 'Lietuvos Respublika', 'LT488848994645', NULL, '2026-05-05 12:18:31'),
(92, 1, 72, 'Naujoji g. 77', 'Alytus', 'Lietuvos Respublika', 'LT179303945', NULL, '2026-05-05 12:18:31'),
(93, 1, 73, 'Ateities pl. 28', 'Kaunas', 'Lietuvos Respublika', 'LT347048749', NULL, '2026-05-05 12:18:31'),
(94, 1, 74, 'Šilo g. 9', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:31'),
(95, 1, 75, 'Sporto g. 4', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:32'),
(96, 1, 76, 'Taikos pr. 55', 'Šiauliai', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:32'),
(97, 1, 77, 'Žaliosios g. 18', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:32'),
(98, 1, 78, 'Dariaus ir Girėno g. 7', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:32'),
(99, 1, 79, 'Verkių g. 29', 'Vilnius', 'Lietuvos Respublika', 'LT445273749', NULL, '2026-05-05 12:18:32'),
(100, 1, 80, 'Ramybės g. 11', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:32'),
(101, 1, 81, 'Uosių g. 3', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:32'),
(102, 1, 82, 'Kaštonų g. 14', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:32'),
(103, 1, 83, 'Eglių g. 8', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:32'),
(104, 1, 84, 'Vyturių g. 22', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:33'),
(105, 1, 85, 'Fabijoniškių g. 17', 'Vilnius', 'Lietuvos Respublika', 'LT488884178448', NULL, '2026-05-05 12:18:33'),
(106, 1, 86, 'Santaros g. 5', 'Vilnius', 'Lietuvos Respublika', 'LT488844611543', NULL, '2026-05-05 12:18:33'),
(107, 1, 87, 'Tauro g. 2', 'Kaunas', 'Lietuvos Respublika', 'LT488882711045', NULL, '2026-05-05 12:18:33'),
(108, 1, 88, 'Brīvības bulv. 22', 'Rīga', 'Latvijos Respublika', '', NULL, '2026-05-05 12:18:33'),
(109, 1, 89, 'Karaliaus Mindaugo pr. 6', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:33'),
(110, 1, 90, 'Saulėtekio al. 15', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:33'),
(111, 1, 91, 'Pušų g. 7', 'Kaunas', 'Lietuvos Respublika', 'LT488846443548', NULL, '2026-05-05 12:18:33'),
(112, 1, 92, 'Paupio g. 25', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:34'),
(113, 1, 93, 'Ąžuolų al. 4', 'Kaunas', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:34'),
(114, 1, 94, 'Mindaugo g. 10', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:34'),
(115, 1, 95, 'Birželio 23-iosios g. 3', 'Vilnius', 'Lietuvos Respublika', 'LT488842709249', NULL, '2026-05-05 12:18:34'),
(116, 1, 96, 'Via Roma 8', 'Roma', 'Italijos Respublika', 'IT80005088752', NULL, '2026-05-05 12:18:34'),
(117, 1, 97, 'Sapiegos g. 14', 'Kaunas', 'Lietuvos Respublika', 'LT488843784743', NULL, '2026-05-05 12:18:34'),
(118, 1, 98, 'Rinktinės g. 20', 'Vilnius', 'Lietuvos Respublika', 'LT985229646', NULL, '2026-05-05 12:18:34'),
(119, 1, 99, 'Boulevard Haussmann 30', 'Paris', 'Prancūzijos Respublika', '', NULL, '2026-05-05 12:18:34'),
(120, 1, 100, 'Calea Victoriei 15', 'Bucharest', 'Rumunija', '', NULL, '2026-05-05 12:18:35'),
(121, 1, 101, 'Kairėnų g. 9', 'Kaunas', 'Lietuvos Respublika', 'LT488883596841', NULL, '2026-05-05 12:18:35'),
(122, 1, 102, 'Žemaitijos g. 6', 'Vilnius', 'Lietuvos Respublika', '', NULL, '2026-05-05 12:18:35'),
(123, 1, 103, 'Elizabetes iela 10', 'Rīga', 'Latvijos Respublika', 'LV18482940224', NULL, '2026-05-05 12:18:35'),
(124, 1, 104, 'Marszałkowska 10', 'Warszawa', 'Lenkijos Respublika', '', NULL, '2026-05-05 12:18:35'),
(125, 1, 105, 'Pelesos g. 5', 'Kaunas', 'Lietuvos Respublika', 'LT488882203346', NULL, '2026-05-05 12:18:35'),
(126, 1, 106, 'Rue de Rivoli 22', 'Paris', 'Prancūzijos Respublika', '', NULL, '2026-05-05 12:18:35'),
(127, 1, 107, 'Unter den Linden 5', 'Berlin', 'Vokietija', 'Germany', NULL, '2026-05-05 12:18:35'),
(128, 1, NULL, 'Vilniaus g. 2', 'Kaunas', 'Lietuva', 'LT354215421', 7300, '2026-05-05 18:53:03');

-- --------------------------------------------------------

--
-- Table structure for table `company`
--

CREATE TABLE `company` (
  `id_Company` int(11) NOT NULL,
  `name` varchar(255) NOT NULL,
  `companyCode` varchar(100) NOT NULL,
  `active` tinyint(1) NOT NULL DEFAULT 1,
  `creationDate` datetime NOT NULL DEFAULT current_timestamp(),
  `shippingAddress` varchar(255) DEFAULT NULL,
  `shippingStreet` varchar(100) DEFAULT NULL,
  `shippingCity` varchar(100) DEFAULT NULL,
  `shippingPostalCode` varchar(20) DEFAULT NULL,
  `shippingCountry` varchar(10) NOT NULL DEFAULT 'LT',
  `returnAddress` varchar(255) DEFAULT NULL,
  `returnStreet` varchar(100) DEFAULT NULL,
  `returnCity` varchar(100) DEFAULT NULL,
  `returnPostalCode` varchar(20) DEFAULT NULL,
  `returnCountry` varchar(10) NOT NULL DEFAULT 'LT',
  `documentCode` varchar(100) NOT NULL,
  `phoneNumber` varchar(255) NOT NULL,
  `address` varchar(255) NOT NULL,
  `email` varchar(255) NOT NULL,
  `image` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `company`
--

INSERT INTO `company` (`id_Company`, `name`, `companyCode`, `active`, `creationDate`, `shippingAddress`, `shippingStreet`, `shippingCity`, `shippingPostalCode`, `shippingCountry`, `returnAddress`, `returnStreet`, `returnCity`, `returnPostalCode`, `returnCountry`, `documentCode`, `phoneNumber`, `address`, `email`, `image`) VALUES
(1, 'Baltic Goods', '4658944', 1, '2026-05-05 12:06:11', NULL, 'Kauno g. 50', 'Kaunas', '51368', 'LT', NULL, NULL, NULL, NULL, 'LT', 'BG', '+37060000000', 'N/A', 'admin@balticgoods.lt', '/uploads/companies/1/logo.png');

-- --------------------------------------------------------

--
-- Table structure for table `company_integration`
--

CREATE TABLE `company_integration` (
  `id_CompanyIntegration` int(11) NOT NULL,
  `fk_Companyid_Company` int(11) NOT NULL,
  `type` varchar(50) NOT NULL,
  `baseUrl` varchar(500) DEFAULT NULL,
  `encryptedSecrets` text NOT NULL,
  `enabled` tinyint(1) NOT NULL DEFAULT 1,
  `updatedAt` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dpdToken` varchar(1000) DEFAULT NULL,
  `dpdTokenExpires` datetime DEFAULT NULL,
  `dpdTokenSecretId` varchar(36) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `company_integration`
--

INSERT INTO `company_integration` (`id_CompanyIntegration`, `fk_Companyid_Company`, `type`, `baseUrl`, `encryptedSecrets`, `enabled`, `updatedAt`, `dpdToken`, `dpdTokenExpires`, `dpdTokenSecretId`) VALUES
(5, 1, 'DPD', 'https://sandbox-esiunta.dpd.lt/api/v1/', 'fVLkQlBVzYJ5N9Nq7HMzOJtCrnXcCs10miB9ht2kk+d6Su5r7Ps8jI5dJhLee7P34j2EY52gSm8YlPKKhjO6wifRHuVZhZqsgSRqDJGpKQKWVn8tihVm+R+3yg5ZsfQfFyO6lhY48QA9lOmyi6N/OtXc1JHUQXvikEjzJvXlXL6ylJy+SijTVQ51aD7s', 1, '2026-05-05 12:09:47', NULL, NULL, NULL),
(6, 1, 'BUTENT', 'http://94.176.235.151:3069/api/v1', '3zYQt+o6zEZgEr/WepOK90Hs5yPjil+rWgIpX8v3Ezmh03vY2btFvTohdowfCZiAMo6dppFixKOFGu2AWDszVZSf1N9vV/tiGOoXHB81sIgS6orEyN6PBNdvUTSyCD6lD1fq0ItFw3huPOhgrihOvadI', 1, '2026-05-05 12:10:30', NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- Table structure for table `company_users`
--

CREATE TABLE `company_users` (
  `fk_Companyid_Company` int(11) NOT NULL,
  `fk_Usersid_Users` int(11) NOT NULL,
  `role` varchar(50) NOT NULL DEFAULT 'CLIENT',
  `position` varchar(255) DEFAULT NULL,
  `startDate` datetime DEFAULT NULL,
  `active` tinyint(1) NOT NULL DEFAULT 1,
  `createdAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `company_users`
--

INSERT INTO `company_users` (`fk_Companyid_Company`, `fk_Usersid_Users`, `role`, `position`, `startDate`, `active`, `createdAt`) VALUES
(1, 1, 'OWNER', 'ADMIN', '2026-05-05 12:06:11', 1, '2026-05-05 12:06:11'),
(1, 22, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:22'),
(1, 23, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:23'),
(1, 24, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:23'),
(1, 25, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:23'),
(1, 26, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:23'),
(1, 27, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:23'),
(1, 28, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:23'),
(1, 29, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:23'),
(1, 30, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:23'),
(1, 31, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:24'),
(1, 32, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:24'),
(1, 33, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:24'),
(1, 34, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:24'),
(1, 35, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:24'),
(1, 36, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:24'),
(1, 37, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:24'),
(1, 38, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:25'),
(1, 39, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:25'),
(1, 40, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:25'),
(1, 41, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:25'),
(1, 42, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:25'),
(1, 43, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:25'),
(1, 44, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:25'),
(1, 45, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:25'),
(1, 46, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 47, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 48, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 49, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 50, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 51, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 52, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 53, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 54, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:26'),
(1, 55, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:27'),
(1, 56, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:27'),
(1, 57, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:27'),
(1, 58, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:27'),
(1, 59, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:27'),
(1, 60, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:27'),
(1, 61, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:27'),
(1, 62, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 63, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 64, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 65, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 66, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 67, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 68, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 69, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 70, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:28'),
(1, 71, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:29'),
(1, 72, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:29'),
(1, 73, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:29'),
(1, 74, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:29'),
(1, 75, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:29'),
(1, 76, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:29'),
(1, 77, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:29'),
(1, 78, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:29'),
(1, 79, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:30'),
(1, 80, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:30'),
(1, 81, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:30'),
(1, 82, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:30'),
(1, 83, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:30'),
(1, 84, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:30'),
(1, 85, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:30'),
(1, 86, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:30'),
(1, 87, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:31'),
(1, 88, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:31'),
(1, 89, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:31'),
(1, 90, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:31'),
(1, 91, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:31'),
(1, 92, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:31'),
(1, 93, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:31'),
(1, 94, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:31'),
(1, 95, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 96, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 97, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 98, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 99, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 100, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 101, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 102, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 103, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:32'),
(1, 104, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:33'),
(1, 105, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:33'),
(1, 106, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:33'),
(1, 107, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:33'),
(1, 108, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:33'),
(1, 109, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:33'),
(1, 110, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:33'),
(1, 111, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:33'),
(1, 112, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:34'),
(1, 113, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:34'),
(1, 114, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:34'),
(1, 115, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:34'),
(1, 116, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:34'),
(1, 117, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:34'),
(1, 118, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:34'),
(1, 119, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:34'),
(1, 120, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:35'),
(1, 121, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:35'),
(1, 122, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:35'),
(1, 123, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:35'),
(1, 124, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:35'),
(1, 125, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:35'),
(1, 126, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:35'),
(1, 127, 'CLIENT', NULL, NULL, 1, '2026-05-05 12:18:35'),
(1, 128, 'COURIER', NULL, NULL, 1, '2026-05-05 18:56:07');

-- --------------------------------------------------------

--
-- Table structure for table `courier`
--

CREATE TABLE `courier` (
  `id_Courier` int(11) NOT NULL,
  `name` varchar(255) NOT NULL,
  `contactPhone` varchar(50) DEFAULT NULL,
  `deliveryTermDays` int(11) DEFAULT NULL,
  `deliveryPrice` double DEFAULT NULL,
  `type` varchar(30) NOT NULL DEFAULT 'CUSTOM',
  `fk_Companyid_Company` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `courier`
--

INSERT INTO `courier` (`id_Courier`, `name`, `contactPhone`, `deliveryTermDays`, `deliveryPrice`, `type`, `fk_Companyid_Company`) VALUES
(1, 'DPD Paštomatas', NULL, 2, 3.5, 'DPD_PARCEL', NULL),
(2, 'DPD Kurjeris', NULL, 1, 5, 'DPD_HOME', NULL),
(3, 'Įmonės Kurjeris', '+37060000000', 3, 5, 'CUSTOM', 1);

-- --------------------------------------------------------

--
-- Table structure for table `invoice`
--

CREATE TABLE `invoice` (
  `id_Invoice` int(11) NOT NULL,
  `invoiceNumber` varchar(100) NOT NULL,
  `date` datetime NOT NULL DEFAULT current_timestamp(),
  `dueDate` datetime DEFAULT NULL,
  `total` double NOT NULL DEFAULT 0,
  `vatTotal` double NOT NULL DEFAULT 0,
  `isPaid` tinyint(1) NOT NULL DEFAULT 0,
  `paidAt` datetime DEFAULT NULL,
  `notes` varchar(1000) DEFAULT NULL,
  `fileUrl` varchar(500) DEFAULT NULL COMMENT 'Path to generated PDF invoice',
  `emailSent` tinyint(1) NOT NULL DEFAULT 0,
  `emailSentAt` datetime DEFAULT NULL,
  `fk_Ordersid_Orders` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `notification`
--

CREATE TABLE `notification` (
  `id_Notification` int(11) NOT NULL,
  `theme` varchar(255) NOT NULL,
  `content` varchar(1000) NOT NULL,
  `date` datetime NOT NULL DEFAULT current_timestamp(),
  `isRead` tinyint(1) NOT NULL DEFAULT 0,
  `type` varchar(50) NOT NULL DEFAULT 'INFO' COMMENT 'INFO | ORDER | SHIPMENT | RETURN | INVOICE',
  `referenceId` int(11) DEFAULT NULL COMMENT 'orderId / shipmentId / returnId',
  `referenceType` varchar(50) DEFAULT NULL COMMENT 'ORDER | SHIPMENT | RETURN',
  `emailSent` tinyint(1) NOT NULL DEFAULT 0,
  `fk_Companyid_Company` int(11) DEFAULT NULL COMMENT 'Company whose staff can see this notification in the bell',
  `fk_Usersid_Users` int(11) DEFAULT NULL,
  `visibleToClient` tinyint(1) NOT NULL DEFAULT 1,
  `visibleToCompany` tinyint(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `orders`
--

CREATE TABLE `orders` (
  `id_Orders` int(11) NOT NULL,
  `OrdersDate` datetime NOT NULL DEFAULT current_timestamp(),
  `totalAmount` double NOT NULL DEFAULT 0,
  `paymentMethod` varchar(255) DEFAULT NULL,
  `deliveryPrice` double DEFAULT NULL,
  `status` int(11) NOT NULL,
  `fk_Clientid_Users` int(11) NOT NULL,
  `externalDocumentId` int(11) DEFAULT NULL,
  `fk_Companyid_Company` int(11) NOT NULL,
  `snapshotDeliveryAddress` varchar(255) DEFAULT NULL,
  `snapshotCity` varchar(100) DEFAULT NULL,
  `snapshotCountry` varchar(100) DEFAULT NULL,
  `snapshotPhone` varchar(50) DEFAULT NULL,
  `snapshotCourierId` int(11) DEFAULT NULL,
  `snapshotDeliveryMethod` varchar(20) DEFAULT NULL COMMENT 'HOME or LOCKER',
  `snapshotLockerId` varchar(100) DEFAULT NULL,
  `snapshotLockerName` varchar(255) DEFAULT NULL,
  `snapshotLockerAddress` varchar(255) DEFAULT NULL,
  `snapshotLat` double DEFAULT NULL,
  `snapshotLng` double DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `orders`
--

INSERT INTO `orders` (`id_Orders`, `OrdersDate`, `totalAmount`, `paymentMethod`, `deliveryPrice`, `status`, `fk_Clientid_Users`, `externalDocumentId`, `fk_Companyid_Company`, `snapshotDeliveryAddress`, `snapshotCity`, `snapshotCountry`, `snapshotPhone`, `snapshotCourierId`, `snapshotDeliveryMethod`, `snapshotLockerId`, `snapshotLockerName`, `snapshotLockerAddress`, `snapshotLat`, `snapshotLng`) VALUES
(1, '2026-04-05 15:13:00', 49, 'butent', 0, 1, 30, 5, 1, 'Tvirtovės al. 39', 'Kaunas', 'Lietuvos Respublika', '+37061000010', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(2, '2026-04-05 13:26:00', 10, 'butent', 0, 1, 31, 6, 1, 'Vytauto pr. 25', 'Kaunas', 'Lietuvos Respublika', '+37062000011', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(3, '2026-04-06 11:39:00', 700, 'butent', 0, 1, 32, 7, 1, 'Gedimino g. 5', 'Marijampolė', 'Lietuvos Respublika', '+37061000012', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(4, '2026-04-07 09:52:00', 5710.06, 'butent', 0, 1, 33, 8, 1, 'Jonavos g. 5', 'Kaunas', 'Lietuvos Respublika', '+37062000013', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(5, '2026-04-07 16:05:00', 1368.5, 'butent', 0, 1, 34, 9, 1, 'Perkūno al. 17', 'Kaunas', 'Lietuvos Respublika', '+37061000014', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(6, '2026-04-07 14:18:00', 2212.92, 'butent', 0, 1, 33, 10, 1, 'Jonavos g. 5', 'Kaunas', 'Lietuvos Respublika', '+37062000013', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(7, '2026-04-09 12:31:00', 30, 'butent', 0, 1, 35, 11, 1, 'Žalgirio g. 44', 'Kaunas', 'Lietuvos Respublika', '+37062000015', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(8, '2026-04-09 10:44:00', 19, 'butent', 0, 1, 36, 12, 1, 'Kalvarijų g. 8', 'Vilnius', 'Lietuvos Respublika', '+37061000016', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(9, '2026-04-10 08:57:00', 189.97, 'butent', 0, 1, 37, 13, 1, 'Ukmergės g. 55', 'Vilnius', 'Lietuvos Respublika', '+37062000017', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(10, '2026-04-11 15:10:00', 40, 'butent', 0, 1, 38, 14, 1, 'Vilijampolės g. 18', 'Kaunas', 'Lietuvos Respublika', '+37061000018', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(11, '2026-04-11 13:23:00', 20, 'butent', 0, 1, 39, 15, 1, 'Demokratų g. 28', 'Kaunas', 'Lietuvos Respublika', '+37062000019', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(12, '2026-04-11 11:36:00', 53, 'butent', 0, 1, 40, 16, 1, 'Tilžės g. 144', 'Šiauliai', 'Lietuvos Respublika', '+37061000020', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(13, '2026-04-13 09:49:00', 60, 'butent', 0, 1, 42, 17, 1, 'Kęstučio g. 22', 'Kaunas', 'Lietuvos Respublika', '+37061000022', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(14, '2026-04-13 16:02:00', 20, 'butent', 0, 1, 52, 29, 1, 'Žvėryno g. 6', 'Kaunas', 'Lietuvos Respublika', '+37061000032', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(15, '2026-04-14 14:15:00', 45, 'butent', 0, 1, 53, 30, 1, 'Naugarduko g. 48', 'Vilnius', 'Lietuvos Respublika', '+37062000033', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(16, '2026-04-15 12:28:00', 70, 'butent', 0, 1, 54, 31, 1, 'Ozo g. 25', 'Vilnius', 'Lietuvos Respublika', '+37061000034', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(17, '2026-04-15 10:41:00', 65, 'butent', 0, 1, 62, 45, 1, 'Liepkalnio g. 72', 'Vilnius', 'Lietuvos Respublika', '+37061000042', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(18, '2026-04-15 08:54:00', 10, 'butent', 0, 1, 63, 46, 1, 'Pramonės pr. 6', 'Kaunas', 'Lietuvos Respublika', '+37062000043', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(19, '2026-04-17 15:07:00', 701.8, 'butent', 0, 1, 64, 47, 1, 'Viršuliškių g. 40', 'Kaunas', 'Lietuvos Respublika', '+37061000044', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(20, '2026-04-17 13:20:00', 70, 'butent', 0, 1, 65, 48, 1, 'Kęstučio g. 3', 'Raseiniai', 'Lietuvos Respublika', '+37062000045', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(21, '2026-04-18 11:33:00', 17, 'butent', 0, 1, 66, 49, 1, 'Triq il-Vittorja 8', 'Valletta', 'Maltos Respublika', '+37061000046', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(22, '2026-04-19 09:46:00', 20, 'butent', 0, 1, 67, 50, 1, 'Šiaurės g. 22', 'Kaunas', 'Lietuvos Respublika', '+37062000047', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(23, '2026-04-19 16:59:00', 1300, 'butent', 0, 1, 69, 52, 1, 'Trakų g. 15', 'Kaunas', 'Lietuvos Respublika', '+37062000049', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(24, '2026-04-19 14:12:00', 19.99, 'butent', 0, 1, 70, 53, 1, 'Triq il-Merkanti 3', 'Valletta', 'Maltos Respublika', '+37061000050', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(25, '2026-04-21 12:25:00', 30, 'butent', 0, 1, 71, 54, 1, 'Hauptstraße 42', 'Berlin', 'Vokietijos Federacinė Respublika', '+37062000051', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(26, '2026-04-21 10:38:00', 50, 'butent', 0, 1, 72, 55, 1, 'Rue de la Liberté 15', 'Paris', 'Prancūzijos Respublika', '+37061000052', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(27, '2026-04-22 08:51:00', 16.5, 'butent', 0, 1, 73, 56, 1, 'Collins Street 8', 'Melbourne', 'Australija', '+37062000053', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(28, '2026-04-23 15:04:00', 84.3, 'butent', 0, 1, 74, 57, 1, 'High Street 21', 'London', 'Jungt.Didž.Brit. ir Š.Airijos Karalystė', '+37061000054', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(29, '2026-04-23 13:17:00', 59, 'butent', 0, 1, 75, 58, 1, 'Bajcsy-Zsilinszky út 10', 'Budapest', 'Vengrijos Respublika', '+37062000055', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(30, '2026-04-23 11:30:00', 21, 'butent', 0, 1, 76, 59, 1, 'O\'Connell Street 3', 'Dublin', 'Airija', '+37061000056', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(31, '2026-04-25 09:43:00', 13, 'butent', 0, 1, 77, 60, 1, 'Avenue des Champs 7', 'Lyon', 'Prancūzijos Respublika', '+37062000057', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(32, '2026-04-25 16:56:00', 14, 'butent', 0, 1, 78, 61, 1, 'Grafton Street 12', 'Dublin', 'Airija', '+37061000058', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(33, '2026-04-26 14:09:00', 20.5, 'butent', 0, 1, 79, 62, 1, 'Oxford Street 45', 'London', 'Jungt.Didž.Brit. ir Š.Airijos Karalystė', '+37062000059', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(34, '2026-04-27 12:22:00', 15, 'butent', 0, 1, 80, 63, 1, 'Avenida da Liberdade 50', 'Lisbon', 'Portugalijos Respublika', '+37061000060', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(35, '2026-04-27 10:35:00', 49, 'butent', 0, 1, 81, 64, 1, 'Mannerheimintie 10', 'Helsinki', 'Suomijos Respublika', '+37062000061', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(36, '2026-04-27 08:48:00', 22, 'butent', 0, 1, 82, 65, 1, 'Gran Vía 20', 'Madrid', 'Ispanijos Karalystė', '+37061000062', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(37, '2026-04-29 15:01:00', 20, 'butent', 0, 1, 83, 66, 1, 'Friedrichstraße 30', 'Berlin', 'Vokietijos Federacinė Respublika', '+37062000063', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(38, '2026-04-29 13:14:00', 12.89, 'butent', 0, 1, 84, 67, 1, 'Baker Street 10', 'London', 'Jungt.Didž.Brit. ir Š.Airijos Karalystė', '+37061000064', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(39, '2026-04-30 11:27:00', 65, 'butent', 0, 1, 94, 98, 1, 'Šilo g. 9', 'Vilnius', 'Lietuvos Respublika', '+37061000074', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(40, '2026-04-30 09:40:00', 50, 'butent', 0, 1, 95, 101, 1, 'Sporto g. 4', 'Kaunas', 'Lietuvos Respublika', '+37062000075', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(41, '2026-05-01 16:53:00', 400, 'butent', 0, 1, 96, 103, 1, 'Taikos pr. 55', 'Šiauliai', 'Lietuvos Respublika', '+37061000076', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(42, '2026-05-01 14:06:00', 170, 'butent', 0, 1, 97, 104, 1, 'Žaliosios g. 18', 'Vilnius', 'Lietuvos Respublika', '+37062000077', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(43, '2026-05-01 12:19:00', 20, 'butent', 0, 1, 98, 105, 1, 'Dariaus ir Girėno g. 7', 'Kaunas', 'Lietuvos Respublika', '+37061000078', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(44, '2026-05-02 10:32:00', 13, 'butent', 0, 1, 100, 111, 1, 'Ramybės g. 11', 'Vilnius', 'Lietuvos Respublika', '+37061000080', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(45, '2026-05-03 08:45:00', 650, 'butent', 0, 1, 101, 112, 1, 'Uosių g. 3', 'Kaunas', 'Lietuvos Respublika', '+37062000081', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(46, '2026-05-03 15:58:00', 17, 'butent', 0, 1, 102, 113, 1, 'Kaštonų g. 14', 'Kaunas', 'Lietuvos Respublika', '+37061000082', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(47, '2026-05-03 13:11:00', 17, 'butent', 0, 1, 109, 133, 1, 'Karaliaus Mindaugo pr. 6', 'Kaunas', 'Lietuvos Respublika', '+37062000089', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(48, '2026-05-04 11:24:00', 58, 'butent', 0, 1, 113, 137, 1, 'Ąžuolų al. 4', 'Kaunas', 'Lietuvos Respublika', '+37062000093', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(49, '2026-05-04 09:37:00', 123.99, 'butent', 0, 1, 120, 146, 1, 'Calea Victoriei 15', 'Bucharest', 'Rumunija', '+37061000100', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(50, '2026-05-04 16:50:00', 507.18, 'butent', 0, 1, 121, 147, 1, 'Kairėnų g. 9', 'Kaunas', 'Lietuvos Respublika', '+37062000101', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(51, '2026-05-05 14:03:00', 23.99, 'butent', 0, 1, 122, 148, 1, 'Žemaitijos g. 6', 'Vilnius', 'Lietuvos Respublika', '+37061000102', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(52, '2026-05-05 12:16:00', 105.36, 'butent', 0, 1, 123, 149, 1, 'Elizabetes iela 10', 'Rīga', 'Latvijos Respublika', '+37062000103', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(53, '2026-05-05 10:29:00', 108.1, 'butent', 0, 1, 124, 150, 1, 'Marszałkowska 10', 'Warszawa', 'Lenkijos Respublika', '+37061000104', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(54, '2026-05-05 08:42:00', 23.18, 'butent', 0, 1, 125, 151, 1, 'Pelesos g. 5', 'Kaunas', 'Lietuvos Respublika', '+37062000105', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(55, '2026-05-05 15:55:00', 51.51, 'butent', 0, 1, 126, 152, 1, 'Rue de Rivoli 22', 'Paris', 'Prancūzijos Respublika', '+37061000106', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(56, '2026-05-05 13:08:00', 38.35, 'butent', 0, 1, 127, 153, 1, 'Unter den Linden 5', 'Berlin', 'Vokietija', '+37062000107', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL),
(57, '2026-05-05 11:21:00', 121, 'butent', 0, 1, 120, 154, 1, 'Calea Victoriei 15', 'Bucharest', 'Rumunija', '+37061000100', NULL, 'HOME', NULL, NULL, NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- Table structure for table `ordersproduct`
--

CREATE TABLE `ordersproduct` (
  `id_OrdersProduct` int(11) NOT NULL,
  `quantity` double NOT NULL,
  `unitPrice` double NOT NULL,
  `vatValue` double NOT NULL,
  `fk_Ordersid_Orders` int(11) NOT NULL,
  `fk_Productid_Product` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `ordersproduct`
--

INSERT INTO `ordersproduct` (`id_OrdersProduct`, `quantity`, `unitPrice`, `vatValue`, `fk_Ordersid_Orders`, `fk_Productid_Product`) VALUES
(1, 1, 40.49, 8.5, 1, 18),
(2, 1, 8.264, 1.74, 2, 18),
(3, 1, 578.51, 121.49, 3, 18),
(4, 1.08, 180, 37.8, 4, 19),
(5, 8.455, 270, 56.7, 4, 20),
(6, 8.303, 270, 56.7, 4, 21),
(7, 43, 20.93, 4.4, 5, 7),
(8, 33, 7, 1.47, 5, 3),
(9, 0.263, 1450, 304.5, 6, 22),
(10, 0.073, 1170, 245.7, 6, 22),
(11, 0.704, 1100, 231, 6, 23),
(12, 0.653, 900, 189, 6, 24),
(13, 1, 24.79, 5.21, 7, 11),
(14, 1, 15.7, 3.3, 8, 5),
(15, 10, 15.7, 3.3, 9, 8),
(16, 1, 33.06, 6.94, 10, 5),
(17, 1, 16.53, 3.47, 11, 10),
(18, 1, 43.8, 9.2, 12, 5),
(19, 1, 49.59, 10.41, 13, 3),
(20, 1, 16.53, 3.47, 14, 3),
(21, 1, 37.19, 7.81, 15, 11),
(22, 1, 57.85, 12.15, 16, 10),
(23, 1, 53.72, 11.28, 17, 11),
(24, 1, 8.265, 1.74, 18, 3),
(25, 4, 145, 30.45, 19, 35),
(26, 1, 57.85, 12.15, 20, 8),
(27, 1, 14.05, 2.95, 21, 10),
(28, 1, 16.53, 3.47, 22, 5),
(29, 1, 1074.38, 225.62, 23, 10),
(30, 1, 16.52, 3.47, 24, 5),
(31, 1, 24.79, 5.21, 25, 3),
(32, 1, 41.32, 8.68, 26, 3),
(33, 1, 13.64, 2.86, 27, 10),
(34, 1, 69.67, 14.63, 28, 5),
(35, 1, 48.76, 10.24, 29, 3),
(36, 1, 17.355, 3.64, 30, 8),
(37, 1, 10.74, 2.26, 31, 9),
(38, 1, 11.57, 2.43, 32, 3),
(39, 1, 16.94, 3.56, 33, 8),
(40, 1, 12.4, 2.6, 34, 3),
(41, 1, 40.496, 8.5, 35, 8),
(42, 1, 18.18, 3.82, 36, 3),
(43, 1, 16.53, 3.47, 37, 10),
(44, 1, 10.65, 2.24, 38, 3),
(45, 1, 53.72, 11.28, 39, 10),
(46, 1, 41.32, 8.68, 40, 10),
(47, 3, 110.193, 23.14, 41, 8),
(48, 5, 11.57, 2.43, 42, 3),
(49, 3, 27.548, 5.79, 42, 40),
(50, 2, 8.264, 1.74, 43, 40),
(51, 1, 10.74, 2.26, 44, 10),
(52, 1, 537.19, 112.81, 45, 10),
(53, 1, 14.05, 2.95, 46, 40),
(54, 1, 14.05, 2.95, 47, 40),
(55, 1, 47.93, 10.07, 48, 40),
(56, 1, 81.82, 17.18, 49, 41),
(57, 1, 20.65, 4.34, 49, 42),
(58, 1, 413.21, 86.77, 50, 41),
(59, 1, 5.94, 1.25, 50, 42),
(60, 1, 16.12, 3.39, 51, 41),
(61, 1, 3.71, 0.78, 51, 42),
(62, 1, 82.64, 17.35, 52, 41),
(63, 1, 22.72, 4.77, 52, 42),
(64, 2, 12.12, 2.55, 53, 42),
(65, 1, 49.59, 10.41, 53, 41),
(66, 1, 16.52, 3.47, 53, 42),
(67, 1, 16.52, 3.47, 54, 41),
(68, 1, 2.64, 0.55, 54, 42),
(69, 1, 28.88, 6.06, 55, 41),
(70, 1, 14.04, 2.95, 55, 42),
(71, 1, 20.66, 4.34, 56, 41),
(72, 1, 11.56, 2.43, 56, 42),
(73, 5, 20, 4.2, 57, 26);

-- --------------------------------------------------------

--
-- Table structure for table `orderstatus`
--

CREATE TABLE `orderstatus` (
  `id_OrderStatus` int(11) NOT NULL,
  `name` char(21) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `orderstatus`
--

INSERT INTO `orderstatus` (`id_OrderStatus`, `name`) VALUES
(1, 'Awaiting confirmation'),
(2, 'Cancelled'),
(3, 'Completed'),
(4, 'In progress'),
(5, 'Sent');

-- --------------------------------------------------------

--
-- Table structure for table `package`
--

CREATE TABLE `package` (
  `id_Package` int(11) NOT NULL,
  `creationDate` datetime NOT NULL DEFAULT current_timestamp(),
  `labelFile` varchar(500) DEFAULT NULL,
  `weight` double DEFAULT NULL,
  `fk_Shipmentid_Shipment` int(11) NOT NULL,
  `trackingNumber` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `product`
--

CREATE TABLE `product` (
  `id_Product` int(11) NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `price` double DEFAULT NULL,
  `currency` varchar(15) DEFAULT NULL,
  `canTheProductBeProductReturned` tinyint(1) NOT NULL DEFAULT 0,
  `countableItem` tinyint(1) NOT NULL DEFAULT 0,
  `unit` varchar(6) NOT NULL DEFAULT 'vnt',
  `shipping_mode` varchar(255) DEFAULT NULL,
  `vat` tinyint(1) NOT NULL DEFAULT 1,
  `creationDate` datetime DEFAULT current_timestamp(),
  `externalCode` int(11) DEFAULT NULL,
  `fk_Companyid_Company` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `product`
--

INSERT INTO `product` (`id_Product`, `name`, `description`, `price`, `currency`, `canTheProductBeProductReturned`, `countableItem`, `unit`, `shipping_mode`, `vat`, `creationDate`, `externalCode`, `fk_Companyid_Company`) VALUES
(1, 'Transporto paslauga', 'Krovinių pervežimo ir pristatymo paslauga', 11.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2019-10-22 16:07:00', 1, 1),
(2, 'Biuro reikmenų rinkinys', 'Rašikliai, sąsiuviniai, lipdukai ir kiti biuro reikmenys', 12.99, 'EUR', 1, 1, 'vnt', NULL, 1, '2019-10-22 16:10:00', 2, 1),
(3, 'Popierius A4 (500 lapų)', 'Universalus spausdinimo popierius, 80 g/m², 500 lapų', 13.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:08:00', 3, 1),
(4, 'Spausdinimo kasetė', 'Universali rašalinė kasetė, suderinama su daugeliu modelių', 14.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:09:00', 4, 1),
(5, 'Universali valymo priemonė', 'Koncentruota valymo priemonė paviršiams, 1 L', 15.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:10:00', 5, 1),
(6, 'Dezinfekcinė priemonė', 'Greito poveikio dezinfekcinė priemonė, 500 ml', 16.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:10:00', 6, 1),
(7, 'Darbo pirštinės', 'Apsauginės darbo pirštinės, nitrilinės, 100 vnt.', 17.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:10:00', 7, 1),
(8, 'Apsauginė kaukė FFP2', 'Filtrais apsauganti pusiau veido kaukė, CE sertifikuota', 18.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:14:00', 8, 1),
(9, 'Darbo batai S3', 'Apsauginiai darbo batai su plienine nosele, S3 klasė', 19.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:15:00', 9, 1),
(10, 'Apsauginis kombinezonas', 'Vienkarinis apsauginis kombinezonas, baltas, L dydis', 20.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:15:00', 10, 1),
(11, 'Apsauginis šalmas', 'Statybinis apsauginis šalmas, ABS plastiko, EN 397', 21.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:15:00', 11, 1),
(12, 'Atšvaitinė liemenė', 'Didelio matomumo atšvaitinė liemenė, EN ISO 20471', 22.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 19:16:00', 12, 1),
(13, 'Konsultavimo paslauga', 'Verslo procesų konsultavimo paslauga', 23.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-06-10 20:23:00', 13, 1),
(14, 'Dyzelinas (1 L)', 'Europinės kokybės dyzelinis kuras, EN 590', 24.99, 'EUR', 1, 1, 'vnt', NULL, 1, '2023-06-10 20:32:00', 14, 1),
(15, 'Elektrinis grąžtas 18V', 'Belaidis elektrinis grąžtas su akumuliatoriumi 18V, 2Ah', 25.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:23:00', 15, 1),
(16, 'Rankinis pjūklas', 'Universalus rankinis medžio pjūklas, 500 mm ašmenys', 26.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:24:00', 16, 1),
(17, 'Santechninių įrankių rinkinys', 'Santechnikui skirtų įrankių rinkinys 25 dalių', 27.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:24:00', 17, 1),
(18, 'Pakavimo juosta', 'Rudos spalvos pakavimo scotch juosta, 48mm x 66m', 28.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:33:00', 18, 1),
(19, 'Kartoninė dėžė (vid.)', 'Tvirta kartoninė pakavimo dėžė 40x30x25 cm', 29.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:41:00', 19, 1),
(20, 'Burbulinė plėvelė (5 m)', 'Apsauginė oro burbulų plėvelė, plotis 50 cm, ilgis 5 m', 30.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:45:00', 20, 1),
(21, 'Pakavimo popierius', 'Natūralus pakavimo popierius ritinyje, 70g/m², 5 kg', 31.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:46:00', 21, 1),
(22, 'Metalinis lankstelis', 'Cinku dengtas metalinis kampinis lankstelis 50x50x2 mm', 32.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:54:00', 22, 1),
(23, 'Plastikinis konteineris', 'Sandarus maisto produktų laikymo konteineris su dangteliu', 33.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:55:00', 23, 1),
(24, 'Stiklinis butelis (1 L)', 'Neutralus stiklinis butelis su kamšteliu, 1000 ml', 34.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-10 21:55:00', 24, 1),
(25, 'Logistikos paslauga', 'Sandėliavimo ir paskirstymo logistikos paslauga', 35.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-06-21 07:29:00', 25, 1),
(26, 'Saugojimo paslauga', 'Trumpalaikio ir ilgalaikio saugojimo paslauga', 36.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-06-21 07:45:00', 26, 1),
(27, 'IT priežiūros paslauga', 'Informacinių sistemų priežiūros paslauga', 37.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-06-21 19:32:00', 27, 1),
(28, 'Buhalterinė paslauga', 'Apskaitos ir finansų tvarkymo paslauga', 38.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-06-21 19:42:00', 28, 1),
(29, 'Suvirinimo aparatas MIG', 'Pusiau automatinis suvirinimo aparatas MIG/MAG, 200A', 39.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-06-21 19:53:00', 29, 1),
(30, 'Reklamos paslauga', 'Skaitmeninės rinkodaros ir reklamos paslauga', 40.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-06-21 19:58:00', 30, 1),
(31, 'Valymo paslauga', 'Patalpų ir teritorijos valymo paslauga', 41.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-07-02 21:24:00', 31, 1),
(32, 'Apsaugos paslauga', 'Objekto ir turto apsaugos paslauga', 42.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-07-10 21:23:00', 32, 1),
(33, 'Skaitmeninis matuoklis', 'Tikslus skaitmeninis matavimo rouletas 8 m, ±1 mm', 43.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-07-10 21:28:00', 33, 1),
(34, 'Juridinė paslauga', 'Teisinio konsultavimo ir dokumentų rengimo paslauga', 44.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-07-10 21:46:00', 34, 1),
(35, 'Valymo šepetys', 'Ilgakotis pramoninis valymo šepetys, natūraliai šeriai', 45.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-07-13 22:33:00', 35, 1),
(36, 'Oro kompresorius 50 L', 'Stacionarus oro kompresorius 50 L talpos, 2 kW, 8 bar', 46.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-07-17 20:02:00', 36, 1),
(37, 'Hidraulinis keltuvas 3T', 'Hidraulinis grindinis keltuvas 3 t keliamoji galia', 47.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-07-19 21:27:00', 37, 1),
(38, 'Rinkodaros paslauga', 'Prekės ženklo kūrimo ir plėtros paslauga', 48.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-07-19 21:37:00', 38, 1),
(39, 'Mokymo paslauga', 'Darbuotojų mokymo ir kvalifikacijos kėlimo paslauga', 49.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-07-19 21:38:00', 39, 1),
(40, 'Laboratorinė kolba (500 ml)', 'Borosilikatinio stiklo kolba su kakleliu, 500 ml', 50.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2023-07-21 18:03:00', 40, 1),
(41, 'Projektavimo paslauga', 'Techninio projektavimo ir dizaino paslauga', 51.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-08-17 12:07:00', 41, 1),
(42, 'Draudimo paslauga', 'Turto ir atsakomybės draudimo paslauga', 52.99, 'EUR', 0, 0, 'vnt', NULL, 1, '2023-08-17 12:07:00', 42, 1),
(43, 'Automobilis VW', 'Naudotas lengvasis automobilis, automatinė pavarų dėžė, dyzelis', 53.99, 'EUR', 1, 0, 'vnt', NULL, 1, '2026-03-13 12:43:41', 43, 1);

-- --------------------------------------------------------

--
-- Table structure for table `productcategory`
--

CREATE TABLE `productcategory` (
  `fk_Productid_Product` int(11) NOT NULL,
  `fk_Categoryid_Category` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `productcategory`
--

INSERT INTO `productcategory` (`fk_Productid_Product`, `fk_Categoryid_Category`) VALUES
(1, 1),
(13, 1),
(25, 1),
(26, 1),
(27, 1),
(28, 1),
(30, 1),
(31, 1),
(32, 1),
(34, 1),
(38, 1),
(39, 1),
(41, 1),
(42, 1),
(2, 2),
(3, 2),
(4, 2),
(5, 2),
(6, 2),
(7, 2),
(8, 2),
(9, 2),
(10, 2),
(11, 2),
(12, 2),
(14, 2),
(15, 2),
(16, 2),
(17, 2),
(18, 2),
(19, 2),
(20, 2),
(21, 2),
(22, 2),
(23, 2),
(24, 2),
(29, 2),
(33, 2),
(35, 2),
(36, 2),
(37, 2),
(40, 2),
(43, 3);

-- --------------------------------------------------------

--
-- Table structure for table `productgroup`
--

CREATE TABLE `productgroup` (
  `id_ProductGroup` int(11) NOT NULL,
  `name` varchar(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `productgroup`
--

INSERT INTO `productgroup` (`id_ProductGroup`, `name`) VALUES
(1, 'Gaunamos paslaugos'),
(2, 'Prekės skirtos perparduoti'),
(3, 'Prekės be skaičiuojamo likučio'),
(4, 'Kuras'),
(5, 'Inventorius'),
(6, 'Transporto priemonės');

-- --------------------------------------------------------

--
-- Table structure for table `product_images`
--

CREATE TABLE `product_images` (
  `id_ProductImage` int(11) NOT NULL,
  `fk_Productid_Product` int(11) NOT NULL,
  `url` varchar(500) NOT NULL,
  `isPrimary` tinyint(1) NOT NULL DEFAULT 0,
  `createdAt` datetime NOT NULL DEFAULT current_timestamp(),
  `sortOrder` int(11) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `product_images`
--

INSERT INTO `product_images` (`id_ProductImage`, `fk_Productid_Product`, `url`, `isPrimary`, `createdAt`, `sortOrder`) VALUES
(1, 10, '/uploads/products/10/7cdfa03673cc45bfaa5c31acd1ebb895.jpg', 1, '2026-05-05 18:33:59', 0),
(2, 11, '/uploads/products/11/166d5b37d73a43b7bd1174d436b1b2cd.jpg', 1, '2026-05-05 18:34:08', 0),
(3, 8, '/uploads/products/8/1d64216453474c91ae7ede9d9bc03369.png', 1, '2026-05-05 18:34:23', 0),
(4, 12, '/uploads/products/12/de1fdb9384d94970b5ed6645853e49d6.webp', 1, '2026-05-05 18:34:34', 0),
(5, 43, '/uploads/products/43/108ef428598c453c9c57f2d4132d6fea.jpg', 1, '2026-05-05 18:34:44', 0),
(7, 2, '/uploads/products/2/680a40298ca74d87be0d7412cd44494e.webp', 1, '2026-05-05 18:36:08', 0),
(8, 20, '/uploads/products/20/bba5d2a0c3ea4de7a8c67344cfb67294.jpg', 1, '2026-05-05 18:36:18', 0),
(9, 9, '/uploads/products/9/c6b41b4c024342c5be20e82a092d9574.jpg', 1, '2026-05-05 18:36:26', 0),
(10, 7, '/uploads/products/7/08770973046043ad8218b1e95951b56d.webp', 1, '2026-05-05 18:36:36', 0),
(11, 6, '/uploads/products/6/6988ab2320b44e028c5ba8823a57c18b.jpg', 1, '2026-05-05 18:36:47', 0),
(12, 15, '/uploads/products/15/08408690344845e692f683485b78580e.jpg', 1, '2026-05-05 18:37:00', 0),
(13, 37, '/uploads/products/37/5da439a89f344fe1b69191449056e7b7.jpg', 1, '2026-05-05 18:37:14', 0),
(14, 19, '/uploads/products/19/6086eca31fc145b4b58d6b2409cccd15.webp', 1, '2026-05-05 18:37:30', 0),
(15, 40, '/uploads/products/40/93a5ea2f527542a79fad6fb9e5dc502a.jpg', 1, '2026-05-05 18:37:40', 0),
(16, 22, '/uploads/products/22/fe000989975f423fac774ea0566a548e.jpg', 1, '2026-05-05 18:37:51', 0),
(17, 36, '/uploads/products/36/f29aa875783643d9bf03a9c09347b891.jpg', 1, '2026-05-05 18:38:06', 0),
(18, 18, '/uploads/products/18/216147ed3df84d7495ea2e3625f6c714.jpg', 1, '2026-05-05 18:38:17', 0),
(19, 21, '/uploads/products/21/aa93e87fb211476581fda8bfe4260c6c.png', 1, '2026-05-05 18:38:29', 0),
(20, 23, '/uploads/products/23/fbca1613f3e34f36b4b655c30c287d06.jpg', 1, '2026-05-05 18:38:41', 0),
(21, 3, '/uploads/products/3/971a81eed45c49c79136e8930ded88c7.jpg', 1, '2026-05-05 18:38:53', 0),
(22, 16, '/uploads/products/16/9ebd5cd36577454cba489a6267fc4172.jpg', 1, '2026-05-05 18:39:06', 0),
(23, 17, '/uploads/products/17/d06a4128ce43455f909d24d88ec312da.jpg', 1, '2026-05-05 18:39:22', 0),
(24, 33, '/uploads/products/33/bba78370ec5f4044b54633c73833a2ee.jpg', 1, '2026-05-05 18:39:34', 0),
(25, 4, '/uploads/products/4/1b1df970d3614a75aa0e21c73986e7be.jpg', 1, '2026-05-05 18:39:44', 0),
(26, 24, '/uploads/products/24/473b26fa65164d9e9a59901c82411cdf.webp', 1, '2026-05-05 18:39:59', 0),
(27, 29, '/uploads/products/29/13ac4af214a648af8cbebbfa2f4d8496.jpg', 1, '2026-05-05 18:40:12', 0),
(28, 5, '/uploads/products/5/369bf75186924544ae747c84e49109bb.webp', 1, '2026-05-05 18:40:28', 0),
(29, 35, '/uploads/products/35/11593b51336a4d5d9872f1684d5211f2.webp', 1, '2026-05-05 18:40:42', 0);

-- --------------------------------------------------------

--
-- Table structure for table `product_productgroup`
--

CREATE TABLE `product_productgroup` (
  `fk_Productid_Product` int(11) NOT NULL,
  `fk_ProductGroupId_ProductGroup` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `product_productgroup`
--

INSERT INTO `product_productgroup` (`fk_Productid_Product`, `fk_ProductGroupId_ProductGroup`) VALUES
(1, 1),
(2, 2),
(3, 3),
(4, 3),
(5, 3),
(6, 3),
(7, 3),
(8, 3),
(9, 3),
(10, 3),
(11, 3),
(12, 3),
(13, 1),
(14, 4),
(15, 5),
(16, 5),
(17, 5),
(18, 3),
(19, 3),
(20, 3),
(21, 3),
(22, 3),
(23, 3),
(24, 3),
(25, 1),
(26, 1),
(27, 1),
(28, 1),
(29, 5),
(30, 1),
(31, 1),
(32, 1),
(33, 5),
(34, 1),
(35, 3),
(36, 5),
(37, 5),
(38, 1),
(39, 1),
(40, 3),
(41, 1),
(42, 1),
(43, 6);

-- --------------------------------------------------------

--
-- Table structure for table `product_returns`
--

CREATE TABLE `product_returns` (
  `id_Returns` int(11) NOT NULL,
  `date` datetime NOT NULL DEFAULT current_timestamp(),
  `fk_ReturnStatusTypeid_ReturnStatusType` int(11) NOT NULL,
  `fk_Clientid_Users` int(11) NOT NULL,
  `fk_Adminid_Users` int(11) DEFAULT NULL,
  `fk_Companyid_Company` int(11) NOT NULL,
  `fk_ordersid_orders` int(11) DEFAULT NULL,
  `returnMethod` varchar(30) NOT NULL DEFAULT 'CUSTOM',
  `returnCourierId` int(11) DEFAULT NULL,
  `employeeNote` varchar(1000) DEFAULT NULL,
  `clientNote` varchar(1000) DEFAULT NULL,
  `returnStreet` varchar(255) DEFAULT NULL,
  `returnCity` varchar(100) DEFAULT NULL,
  `returnPostalCode` varchar(20) DEFAULT NULL,
  `returnCountry` varchar(100) DEFAULT NULL,
  `fk_Courierid_Courier` int(11) DEFAULT NULL,
  `returnLockerId` varchar(100) DEFAULT NULL,
  `returnLockerName` varchar(200) DEFAULT NULL,
  `returnLockerAddress` varchar(300) DEFAULT NULL,
  `returnLat` double DEFAULT NULL,
  `returnLng` double DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `return_item`
--

CREATE TABLE `return_item` (
  `id_ReturnItem` int(11) NOT NULL,
  `quantity` int(11) NOT NULL,
  `reason` varchar(255) DEFAULT NULL,
  `reasonId` int(11) DEFAULT NULL,
  `evaluationComment` varchar(1000) DEFAULT NULL,
  `evaluation` tinyint(1) DEFAULT NULL,
  `evaluationDate` date DEFAULT NULL,
  `returnSubTotal` double NOT NULL DEFAULT 0,
  `imageUrls` text DEFAULT NULL,
  `fk_Returnsid_Returns` int(11) NOT NULL,
  `fk_OrdersProductid_OrdersProduct` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `return_reason`
--

CREATE TABLE `return_reason` (
  `id_ReturnReason` int(11) NOT NULL,
  `name` varchar(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `return_reason`
--

INSERT INTO `return_reason` (`id_ReturnReason`, `name`) VALUES
(1, 'Pažeistas'),
(2, 'Neatitinka aprašymo'),
(3, 'Netinkamas dydis'),
(4, 'Apsigalvojau'),
(5, 'Bloga kokybė'),
(6, 'Neveikia');

-- --------------------------------------------------------

--
-- Table structure for table `return_status_type`
--

CREATE TABLE `return_status_type` (
  `id_ReturnStatusType` int(11) NOT NULL,
  `name` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `return_status_type`
--

INSERT INTO `return_status_type` (`id_ReturnStatusType`, `name`) VALUES
(6, 'Atmestas'),
(7, 'Etiketės paruoštos'),
(3, 'Gauta'),
(5, 'Patvirtintas'),
(1, 'Sukurtas'),
(4, 'Užbaigta'),
(2, 'Vertinamas');

-- --------------------------------------------------------

--
-- Table structure for table `shipment`
--

CREATE TABLE `shipment` (
  `id_Shipment` int(11) NOT NULL,
  `trackingNumber` varchar(100) DEFAULT NULL,
  `shippingDate` datetime DEFAULT NULL,
  `estimatedDeliveryDate` datetime DEFAULT NULL,
  `DeliveryLat` double DEFAULT NULL,
  `DeliveryLng` double DEFAULT NULL,
  `fk_Courierid_Courier` int(11) DEFAULT NULL,
  `fk_Ordersid_Orders` int(11) NOT NULL,
  `fk_Companyid_Company` int(11) NOT NULL,
  `providerShipmentId` varchar(36) DEFAULT NULL,
  `providerLockerId` varchar(20) DEFAULT NULL,
  `providerParcelNumber` varchar(500) DEFAULT NULL,
  `fk_Returnsid_Returns` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `shipment_status`
--

CREATE TABLE `shipment_status` (
  `id_ShipmentStatus` int(11) NOT NULL,
  `fk_Shipmentid_Shipment` int(11) NOT NULL,
  `fk_ShipmentStatusTypeid_ShipmentStatusType` int(11) NOT NULL,
  `date` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `shipment_status_type`
--

CREATE TABLE `shipment_status_type` (
  `id_ShipmentStatusType` int(11) NOT NULL,
  `name` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `shipment_status_type`
--

INSERT INTO `shipment_status_type` (`id_ShipmentStatusType`, `name`) VALUES
(7, 'Grąžinimas pristatytas'),
(5, 'Grąžinimas sukurtas'),
(8, 'Grąžinimas vėluoja'),
(6, 'Grąžinimas vežamas'),
(3, 'Pristatyta'),
(1, 'Sukurta'),
(4, 'Vėluoja'),
(2, 'Vežama');

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `id_Users` int(11) NOT NULL,
  `name` varchar(255) NOT NULL,
  `surname` varchar(255) NOT NULL,
  `email` varchar(255) NOT NULL,
  `password` varchar(255) DEFAULT NULL,
  `phoneNumber` varchar(255) DEFAULT NULL,
  `image` varchar(255) DEFAULT NULL,
  `creationDate` datetime NOT NULL DEFAULT current_timestamp(),
  `googleId` varchar(255) DEFAULT NULL,
  `authProvider` varchar(50) NOT NULL DEFAULT 'LOCAL',
  `fk_Companyid_Company` int(11) DEFAULT NULL,
  `isMasterAdmin` tinyint(1) NOT NULL DEFAULT 0,
  `resetToken` varchar(128) DEFAULT NULL,
  `resetTokenExpiry` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`id_Users`, `name`, `surname`, `email`, `password`, `phoneNumber`, `image`, `creationDate`, `googleId`, `authProvider`, `fk_Companyid_Company`, `isMasterAdmin`, `resetToken`, `resetTokenExpiry`) VALUES
(1, 'Master', 'Admin', 'admin@test.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', NULL, '', '2026-05-05 12:06:11', NULL, 'LOCAL', 1, 1, NULL, NULL),
(22, 'Tomas', 'Kazlauskas', 'tomas.kazlauskas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000001', NULL, '2026-05-05 12:18:22', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(23, 'Laura', 'Petrauskaitė', 'laura.petrauskaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000002', NULL, '2026-05-05 12:18:23', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(24, 'Marius', 'Jankauskas', 'marius.jankauskas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000003', NULL, '2026-05-05 12:18:23', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(25, 'Rūta', 'Stankevičiūtė', 'ruta.stankeviciute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000004', NULL, '2026-05-05 12:18:23', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(26, 'Andrius', 'Butkus', 'andrius.butkus@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000005', NULL, '2026-05-05 12:18:23', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(27, 'Eglė', 'Vasiliauskaite', 'egle.vasiliauskaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000007', NULL, '2026-05-05 12:18:23', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(28, 'Jonas', 'Žukauskas', 'jonas.zukauskas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000008', NULL, '2026-05-05 12:18:23', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(29, 'Indrė', 'Mackevičiūtė', 'indre.mackeviciute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000009', NULL, '2026-05-05 12:18:23', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(30, 'Lukas', 'Paulauskas', 'lukas.paulauskas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000010', NULL, '2026-05-05 12:18:23', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(31, 'Viktorija', 'Kavaliauskaitė', 'viktorija.kavaliausk@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000011', NULL, '2026-05-05 12:18:24', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(32, 'Mantas', 'Keršys', 'mantas.kersys@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000012', NULL, '2026-05-05 12:18:24', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(33, 'Giedrė', 'Balčiūtė', 'giedre.balciute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000013', NULL, '2026-05-05 12:18:24', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(34, 'Darius', 'Tarvydas', 'darius.tarvydas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000014', NULL, '2026-05-05 12:18:24', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(35, 'Aistė', 'Grigaitė', 'aiste.grigaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000015', NULL, '2026-05-05 12:18:24', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(36, 'Mindaugas', 'Butkevičius', 'mindaugas.butkev@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000016', NULL, '2026-05-05 12:18:24', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(37, 'Monika', 'Andriulytė', 'monika.andriulyte@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000017', NULL, '2026-05-05 12:18:24', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(38, 'Gediminas', 'Rimkus', 'gediminas.rimkus@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000018', NULL, '2026-05-05 12:18:24', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(39, 'Daiva', 'Martinaitytė', 'daiva.martinaityte@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000019', NULL, '2026-05-05 12:18:25', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(40, 'Vilius', 'Šileika', 'vilius.sileika@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000020', NULL, '2026-05-05 12:18:25', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(41, 'Justė', 'Vaitkutė', 'juste.vaitkute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000021', NULL, '2026-05-05 12:18:25', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(42, 'Aurimas', 'Norkus', 'aurimas.norkus@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000022', NULL, '2026-05-05 12:18:25', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(43, 'Rasa', 'Tamošiūnaitė', 'rasa.tamosiunaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000023', NULL, '2026-05-05 12:18:25', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(44, 'Saulius', 'Jurevičius', 'saulius.jurecius@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000024', NULL, '2026-05-05 12:18:25', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(45, 'Jolanta', 'Sabonytė', 'jolanta.sabonyte@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000025', NULL, '2026-05-05 12:18:25', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(46, 'Arnas', 'Grigaitis', 'arnas.grigaitis@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000026', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(47, 'Vaida', 'Žemaitytė', 'vaida.zemaityte@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000027', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(48, 'Kastytis', 'Daugirdas', 'kastytis.daugirdas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000028', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(49, 'Dovilė', 'Ramonaitė', 'dovile.ramonaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000029', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(50, 'Paulius', 'Šimkus', 'paulius.simkus@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000030', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(51, 'Jurgita', 'Sakalauskaitė', 'jurgita.sakalausk@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000031', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(52, 'Erikas', 'Bartkus', 'erikas.bartkus@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000032', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(53, 'Renata', 'Juodytė', 'renata.juodyte@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000033', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(54, 'Nerijus', 'Urbas', 'nerijus.urbas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000034', NULL, '2026-05-05 12:18:26', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(55, 'Loreta', 'Stonkutė', 'loreta.stonkute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000035', NULL, '2026-05-05 12:18:27', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(56, 'Algirdas', 'Bieliauskas', 'algirdas.bieliausk@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000036', NULL, '2026-05-05 12:18:27', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(57, 'Inga', 'Gudaitė', 'inga.gudaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000037', NULL, '2026-05-05 12:18:27', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(58, 'Renatas', 'Jokubaitis', 'renatas.jokubaitis@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000038', NULL, '2026-05-05 12:18:27', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(59, 'Zita', 'Žilinskaitė', 'zita.zilinskaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000039', NULL, '2026-05-05 12:18:27', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(60, 'Valdas', 'Girnius', 'valdas.girnius@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000040', NULL, '2026-05-05 12:18:27', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(61, 'Asta', 'Ažubalytė', 'asta.azubalyte@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000041', NULL, '2026-05-05 12:18:27', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(62, 'Egidijus', 'Mikalauskas', 'egidijus.mikalausk@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000042', NULL, '2026-05-05 12:18:27', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(63, 'Birutė', 'Urbonaitė', 'birute.urbonaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000043', NULL, '2026-05-05 12:18:28', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(64, 'Gintaras', 'Repšys', 'gintaras.repsys@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000044', NULL, '2026-05-05 12:18:28', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(65, 'Danguolė', 'Baranauskaitė', 'danguole.baranausk@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000045', NULL, '2026-05-05 12:18:28', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(66, 'Vytautas', 'Juška', 'vytautas.juska@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000046', NULL, '2026-05-05 12:18:28', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(67, 'Aldona', 'Bakšytė', 'aldona.baksyte@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000047', NULL, '2026-05-05 12:18:28', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(68, 'Rolandas', 'Kairys', 'rolandas.kairys@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000048', NULL, '2026-05-05 12:18:28', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(69, 'Valerija', 'Venslovaitė', 'valerija.venslova@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000049', NULL, '2026-05-05 12:18:28', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(70, 'Vaidotas', 'Pabedinksas', 'vaidotas.pabed@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000050', NULL, '2026-05-05 12:18:28', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(71, 'Roma', 'Antanavičiūtė', 'roma.antanaviciu@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000051', NULL, '2026-05-05 12:18:29', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(72, 'Kęstutis', 'Reklaitis', 'kestutis.reklaitis@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000052', NULL, '2026-05-05 12:18:29', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(73, 'Vilma', 'Zubovaitė', 'vilma.zubovaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000053', NULL, '2026-05-05 12:18:29', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(74, 'Deividas', 'Lesauskas', 'deividas.lesauskas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000054', NULL, '2026-05-05 12:18:29', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(75, 'Sandra', 'Martinaitytė', 'sandra.martinaity@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000055', NULL, '2026-05-05 12:18:29', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(76, 'Adomas', 'Noreika', 'adomas.noreika@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000056', NULL, '2026-05-05 12:18:29', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(77, 'Sonata', 'Šimkutė', 'sonata.simkute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000057', NULL, '2026-05-05 12:18:29', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(78, 'Laurynas', 'Vaitkevičius', 'laurynas.vaitkev@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000058', NULL, '2026-05-05 12:18:29', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(79, 'Neringa', 'Kačinskaitė', 'neringa.kacinsk@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000059', NULL, '2026-05-05 12:18:30', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(80, 'Ignas', 'Ramanauskas', 'ignas.ramanauskas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000060', NULL, '2026-05-05 12:18:30', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(81, 'Ramunė', 'Bučiūtė', 'ramune.buciute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000061', NULL, '2026-05-05 12:18:30', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(82, 'Karolis', 'Černiauskas', 'karolis.cerniauska@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000062', NULL, '2026-05-05 12:18:30', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(83, 'Lina', 'Mockutė', 'lina.mockute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000063', NULL, '2026-05-05 12:18:30', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(84, 'Martynas', 'Lukoševičius', 'martynas.lukosev@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000064', NULL, '2026-05-05 12:18:30', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(85, 'Irena', 'Gintautaitė', 'irena.gintautaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000065', NULL, '2026-05-05 12:18:30', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(86, 'Tadas', 'Bernatavičius', 'tadas.bernatav@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000066', NULL, '2026-05-05 12:18:30', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(87, 'Brigita', 'Jonikaitė', 'brigita.jonikait@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000067', NULL, '2026-05-05 12:18:31', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(88, 'Dominykas', 'Gudelis', 'dominykas.gudelis@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000068', NULL, '2026-05-05 12:18:31', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(89, 'Kristina', 'Povilaitytė', 'kristina.povilait@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000069', NULL, '2026-05-05 12:18:31', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(90, 'Žygimantas', 'Urbonavičius', 'zygimantas.urbon@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000070', NULL, '2026-05-05 12:18:31', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(91, 'Dalia', 'Grybaitė', 'dalia.grybaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000071', NULL, '2026-05-05 12:18:31', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(92, 'Rimantas', 'Kalvaitis', 'rimantas.kalvaitis@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000072', NULL, '2026-05-05 12:18:31', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(93, 'Audra', 'Daukšaitė', 'audra.dauksa@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000073', NULL, '2026-05-05 12:18:31', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(94, 'Vidmantas', 'Česnavičius', 'vidmantas.cesna@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000074', '', '2026-05-05 12:18:31', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(95, 'Vida', 'Šapalaitė', 'vida.sapalait@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000075', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(96, 'Alvydas', 'Stankus', 'alvydas.stankus@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000076', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(97, 'Nijolė', 'Jonavičiūtė', 'nijole.jonavic@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000077', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(98, 'Edvinas', 'Petryla', 'edvinas.petryla@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000078', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(99, 'Aušra', 'Rudokaitė', 'ausra.rudoka@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000079', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(100, 'Donatas', 'Mikelėnas', 'donatas.mikelenas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000080', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(101, 'Laima', 'Bieliūnaitė', 'laima.bieliunaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000081', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(102, 'Aidas', 'Vaičiulis', 'aidas.vaiciulis@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000082', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(103, 'Rita', 'Kuzminskaitė', 'rita.kuzminskaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000083', NULL, '2026-05-05 12:18:32', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(104, 'Šarūnas', 'Skardžius', 'sarunas.skard@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000084', NULL, '2026-05-05 12:18:33', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(105, 'Edita', 'Žalienė', 'edita.zaliene@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000085', NULL, '2026-05-05 12:18:33', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(106, 'Ąžuolas', 'Pocius', 'azuolas.pocius@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000086', NULL, '2026-05-05 12:18:33', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(107, 'Ineta', 'Orentaitė', 'ineta.orenta@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000087', NULL, '2026-05-05 12:18:33', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(108, 'Povilas', 'Aleksa', 'povilas.aleksa@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000088', NULL, '2026-05-05 12:18:33', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(109, 'Simona', 'Gumuliauskaitė', 'simona.gumul@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000089', NULL, '2026-05-05 12:18:33', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(110, 'Simonas', 'Kavaliūnas', 'simonas.kavaliunas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000090', NULL, '2026-05-05 12:18:33', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(111, 'Virginija', 'Kanapickaitė', 'virginija.kanap@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000091', NULL, '2026-05-05 12:18:33', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(112, 'Antanas', 'Rakauskas', 'antanas.rakauskas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000092', NULL, '2026-05-05 12:18:34', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(113, 'Violeta', 'Valiulytė', 'violeta.valiulyt@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000093', NULL, '2026-05-05 12:18:34', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(114, 'Teodoras', 'Sasnauskas', 'teodoras.sasnauska@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000094', NULL, '2026-05-05 12:18:34', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(115, 'Živilė', 'Radavičiūtė', 'zivile.radavic@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000095', NULL, '2026-05-05 12:18:34', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(116, 'Henrikas', 'Kubilius', 'henrikas.kubilius@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000096', NULL, '2026-05-05 12:18:34', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(117, 'Ugnė', 'Mačiulaitytė', 'ugne.maciulait@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000097', NULL, '2026-05-05 12:18:34', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(118, 'Kazimieras', 'Šimonis', 'kazimieras.simonis@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000098', NULL, '2026-05-05 12:18:34', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(119, 'Rugilė', 'Naujokaitė', 'rugile.naujokaite@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000099', NULL, '2026-05-05 12:18:34', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(120, 'Arvydas', 'Stonys', 'arvydas.stonys@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000100', NULL, '2026-05-05 12:18:35', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(121, 'Gabija', 'Tamulaitytė', 'gabija.tamulait@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000101', NULL, '2026-05-05 12:18:35', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(122, 'Linas', 'Vainauskis', 'linas.vainauskis@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000102', NULL, '2026-05-05 12:18:35', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(123, 'Miglė', 'Griciūtė', 'migle.griciute@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000103', NULL, '2026-05-05 12:18:35', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(124, 'Povilas', 'Čiupys', 'povilas.ciupys@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000104', NULL, '2026-05-05 12:18:35', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(125, 'Emilija', 'Savickaitė', 'emilija.savickaite@demo.lt', '$2a$11$chicaqerf.7TVXeisbFHLOaTeLXSxrR/eY5TfCnfvM3KJFt8YvFem$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000105', NULL, '2026-05-05 12:18:35', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(126, 'Darius', 'Dičiūnas', 'darius.diciunas@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37061000106', NULL, '2026-05-05 12:18:35', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(127, 'Aistė', 'Vilkelytė', 'aiste.vilkelyte@demo.lt', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37062000107', NULL, '2026-05-05 12:18:35', NULL, 'LOCAL', NULL, 0, NULL, NULL),
(128, 'Ignas', 'Makackas', 'ignasmakackas@gmail.com', '$2a$11$d6OpHmk/PLgBt7WMVX8.euh.HfILtOhsEDibBD760ecb7xznkVHQK', '+37060000000', NULL, '2026-05-05 15:52:09', NULL, 'LOCAL', NULL, 0, NULL, NULL);

--
-- Indexes for dumped tables
--

--
-- Indexes for table `category`
--
ALTER TABLE `category`
  ADD PRIMARY KEY (`id_Category`);

--
-- Indexes for table `client_company`
--
ALTER TABLE `client_company`
  ADD PRIMARY KEY (`fk_Clientid_Users`,`fk_Companyid_Company`),
  ADD UNIQUE KEY `UX_client_company_externalClientId` (`fk_Companyid_Company`,`externalClientId`),
  ADD KEY `IX_client_company_company` (`fk_Companyid_Company`);

--
-- Indexes for table `company`
--
ALTER TABLE `company`
  ADD PRIMARY KEY (`id_Company`),
  ADD UNIQUE KEY `UQ_company_code` (`companyCode`);

--
-- Indexes for table `company_integration`
--
ALTER TABLE `company_integration`
  ADD PRIMARY KEY (`id_CompanyIntegration`),
  ADD UNIQUE KEY `UX_company_integration` (`fk_Companyid_Company`,`type`);

--
-- Indexes for table `company_users`
--
ALTER TABLE `company_users`
  ADD PRIMARY KEY (`fk_Companyid_Company`,`fk_Usersid_Users`),
  ADD KEY `IX_company_users_user` (`fk_Usersid_Users`);

--
-- Indexes for table `courier`
--
ALTER TABLE `courier`
  ADD PRIMARY KEY (`id_Courier`),
  ADD KEY `IX_courier_name` (`name`),
  ADD KEY `FK_courier_company` (`fk_Companyid_Company`);

--
-- Indexes for table `invoice`
--
ALTER TABLE `invoice`
  ADD PRIMARY KEY (`id_Invoice`),
  ADD UNIQUE KEY `UQ_invoice_order` (`fk_Ordersid_Orders`),
  ADD KEY `IX_invoice_date` (`date`);

--
-- Indexes for table `notification`
--
ALTER TABLE `notification`
  ADD PRIMARY KEY (`id_Notification`),
  ADD KEY `IX_notification_user` (`fk_Usersid_Users`),
  ADD KEY `IX_notification_date` (`date`),
  ADD KEY `IX_notification_company` (`fk_Companyid_Company`);

--
-- Indexes for table `orders`
--
ALTER TABLE `orders`
  ADD PRIMARY KEY (`id_Orders`),
  ADD UNIQUE KEY `UX_orders_company_externalDocumentId` (`fk_Companyid_Company`,`externalDocumentId`),
  ADD KEY `IX_orders_status` (`status`),
  ADD KEY `IX_orders_client` (`fk_Clientid_Users`),
  ADD KEY `IX_orders_company` (`fk_Companyid_Company`),
  ADD KEY `fk_orders_courier` (`snapshotCourierId`);

--
-- Indexes for table `ordersproduct`
--
ALTER TABLE `ordersproduct`
  ADD PRIMARY KEY (`id_OrdersProduct`),
  ADD KEY `IX_op_order` (`fk_Ordersid_Orders`),
  ADD KEY `IX_op_product` (`fk_Productid_Product`);

--
-- Indexes for table `orderstatus`
--
ALTER TABLE `orderstatus`
  ADD PRIMARY KEY (`id_OrderStatus`);

--
-- Indexes for table `package`
--
ALTER TABLE `package`
  ADD PRIMARY KEY (`id_Package`),
  ADD UNIQUE KEY `UQ_package_trackingNumber` (`trackingNumber`),
  ADD KEY `IX_package_shipment` (`fk_Shipmentid_Shipment`);

--
-- Indexes for table `product`
--
ALTER TABLE `product`
  ADD PRIMARY KEY (`id_Product`),
  ADD UNIQUE KEY `UX_product_company_externalCode` (`fk_Companyid_Company`,`externalCode`),
  ADD KEY `IX_product_company` (`fk_Companyid_Company`);

--
-- Indexes for table `productcategory`
--
ALTER TABLE `productcategory`
  ADD PRIMARY KEY (`fk_Categoryid_Category`,`fk_Productid_Product`),
  ADD KEY `IX_productcategory_product` (`fk_Productid_Product`);

--
-- Indexes for table `productgroup`
--
ALTER TABLE `productgroup`
  ADD PRIMARY KEY (`id_ProductGroup`);

--
-- Indexes for table `product_images`
--
ALTER TABLE `product_images`
  ADD PRIMARY KEY (`id_ProductImage`),
  ADD KEY `IX_product_images_product` (`fk_Productid_Product`),
  ADD KEY `IX_product_images_isPrimary` (`isPrimary`),
  ADD KEY `IX_product_images_sortOrder` (`fk_Productid_Product`,`sortOrder`);

--
-- Indexes for table `product_productgroup`
--
ALTER TABLE `product_productgroup`
  ADD PRIMARY KEY (`fk_Productid_Product`,`fk_ProductGroupId_ProductGroup`),
  ADD KEY `idx_ppg_productGroupId` (`fk_ProductGroupId_ProductGroup`);

--
-- Indexes for table `product_returns`
--
ALTER TABLE `product_returns`
  ADD PRIMARY KEY (`id_Returns`),
  ADD KEY `IX_returns_company` (`fk_Companyid_Company`),
  ADD KEY `IX_returns_client` (`fk_Clientid_Users`),
  ADD KEY `IX_returns_admin` (`fk_Adminid_Users`),
  ADD KEY `IX_returns_status` (`fk_ReturnStatusTypeid_ReturnStatusType`),
  ADD KEY `FK_returns_orders` (`fk_ordersid_orders`),
  ADD KEY `FK_returns_courier` (`returnCourierId`),
  ADD KEY `fk_return_courier` (`fk_Courierid_Courier`);

--
-- Indexes for table `return_item`
--
ALTER TABLE `return_item`
  ADD PRIMARY KEY (`id_ReturnItem`),
  ADD KEY `IX_ri_return` (`fk_Returnsid_Returns`),
  ADD KEY `IX_ri_ordersproduct` (`fk_OrdersProductid_OrdersProduct`),
  ADD KEY `IX_ri_reason` (`reasonId`);

--
-- Indexes for table `return_reason`
--
ALTER TABLE `return_reason`
  ADD PRIMARY KEY (`id_ReturnReason`);

--
-- Indexes for table `return_status_type`
--
ALTER TABLE `return_status_type`
  ADD PRIMARY KEY (`id_ReturnStatusType`),
  ADD UNIQUE KEY `UQ_return_status_type_name` (`name`);

--
-- Indexes for table `shipment`
--
ALTER TABLE `shipment`
  ADD PRIMARY KEY (`id_Shipment`),
  ADD KEY `IX_shipment_courier` (`fk_Courierid_Courier`),
  ADD KEY `IX_shipment_order` (`fk_Ordersid_Orders`),
  ADD KEY `IX_shipment_company` (`fk_Companyid_Company`),
  ADD KEY `fk_shipment_return` (`fk_Returnsid_Returns`);

--
-- Indexes for table `shipment_status`
--
ALTER TABLE `shipment_status`
  ADD PRIMARY KEY (`id_ShipmentStatus`),
  ADD KEY `IX_ss_shipment` (`fk_Shipmentid_Shipment`),
  ADD KEY `IX_ss_type` (`fk_ShipmentStatusTypeid_ShipmentStatusType`),
  ADD KEY `IX_ss_date` (`date`);

--
-- Indexes for table `shipment_status_type`
--
ALTER TABLE `shipment_status_type`
  ADD PRIMARY KEY (`id_ShipmentStatusType`),
  ADD UNIQUE KEY `UQ_shipment_status_type_name` (`name`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id_Users`),
  ADD KEY `IX_users_company` (`fk_Companyid_Company`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `category`
--
ALTER TABLE `category`
  MODIFY `id_Category` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `company`
--
ALTER TABLE `company`
  MODIFY `id_Company` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `company_integration`
--
ALTER TABLE `company_integration`
  MODIFY `id_CompanyIntegration` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- AUTO_INCREMENT for table `courier`
--
ALTER TABLE `courier`
  MODIFY `id_Courier` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `invoice`
--
ALTER TABLE `invoice`
  MODIFY `id_Invoice` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `notification`
--
ALTER TABLE `notification`
  MODIFY `id_Notification` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `orders`
--
ALTER TABLE `orders`
  MODIFY `id_Orders` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=58;

--
-- AUTO_INCREMENT for table `ordersproduct`
--
ALTER TABLE `ordersproduct`
  MODIFY `id_OrdersProduct` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=74;

--
-- AUTO_INCREMENT for table `orderstatus`
--
ALTER TABLE `orderstatus`
  MODIFY `id_OrderStatus` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `package`
--
ALTER TABLE `package`
  MODIFY `id_Package` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `product`
--
ALTER TABLE `product`
  MODIFY `id_Product` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=44;

--
-- AUTO_INCREMENT for table `productgroup`
--
ALTER TABLE `productgroup`
  MODIFY `id_ProductGroup` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- AUTO_INCREMENT for table `product_images`
--
ALTER TABLE `product_images`
  MODIFY `id_ProductImage` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=30;

--
-- AUTO_INCREMENT for table `product_returns`
--
ALTER TABLE `product_returns`
  MODIFY `id_Returns` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `return_item`
--
ALTER TABLE `return_item`
  MODIFY `id_ReturnItem` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `return_reason`
--
ALTER TABLE `return_reason`
  MODIFY `id_ReturnReason` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- AUTO_INCREMENT for table `return_status_type`
--
ALTER TABLE `return_status_type`
  MODIFY `id_ReturnStatusType` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT for table `shipment`
--
ALTER TABLE `shipment`
  MODIFY `id_Shipment` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `shipment_status`
--
ALTER TABLE `shipment_status`
  MODIFY `id_ShipmentStatus` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `shipment_status_type`
--
ALTER TABLE `shipment_status_type`
  MODIFY `id_ShipmentStatusType` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=9;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `id_Users` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=129;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `client_company`
--
ALTER TABLE `client_company`
  ADD CONSTRAINT `FK_client_company_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_client_company_user` FOREIGN KEY (`fk_Clientid_Users`) REFERENCES `users` (`id_Users`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `company_integration`
--
ALTER TABLE `company_integration`
  ADD CONSTRAINT `FK_company_integration_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `company_users`
--
ALTER TABLE `company_users`
  ADD CONSTRAINT `FK_company_users_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_company_users_users` FOREIGN KEY (`fk_Usersid_Users`) REFERENCES `users` (`id_Users`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `courier`
--
ALTER TABLE `courier`
  ADD CONSTRAINT `FK_courier_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `invoice`
--
ALTER TABLE `invoice`
  ADD CONSTRAINT `FK_invoice_order` FOREIGN KEY (`fk_Ordersid_Orders`) REFERENCES `orders` (`id_Orders`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `notification`
--
ALTER TABLE `notification`
  ADD CONSTRAINT `FK_notification_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_notification_user` FOREIGN KEY (`fk_Usersid_Users`) REFERENCES `users` (`id_Users`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `orders`
--
ALTER TABLE `orders`
  ADD CONSTRAINT `FK_orders_client` FOREIGN KEY (`fk_Clientid_Users`) REFERENCES `users` (`id_Users`),
  ADD CONSTRAINT `FK_orders_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_orders_status` FOREIGN KEY (`status`) REFERENCES `orderstatus` (`id_OrderStatus`),
  ADD CONSTRAINT `fk_orders_courier` FOREIGN KEY (`snapshotCourierId`) REFERENCES `courier` (`id_Courier`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- Constraints for table `ordersproduct`
--
ALTER TABLE `ordersproduct`
  ADD CONSTRAINT `FK_op_orders` FOREIGN KEY (`fk_Ordersid_Orders`) REFERENCES `orders` (`id_Orders`),
  ADD CONSTRAINT `FK_op_product` FOREIGN KEY (`fk_Productid_Product`) REFERENCES `product` (`id_Product`);

--
-- Constraints for table `package`
--
ALTER TABLE `package`
  ADD CONSTRAINT `FK_package_shipment` FOREIGN KEY (`fk_Shipmentid_Shipment`) REFERENCES `shipment` (`id_Shipment`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `product`
--
ALTER TABLE `product`
  ADD CONSTRAINT `FK_product_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON UPDATE CASCADE;

--
-- Constraints for table `productcategory`
--
ALTER TABLE `productcategory`
  ADD CONSTRAINT `FK_productcategory_category` FOREIGN KEY (`fk_Categoryid_Category`) REFERENCES `category` (`id_Category`),
  ADD CONSTRAINT `FK_productcategory_product` FOREIGN KEY (`fk_Productid_Product`) REFERENCES `product` (`id_Product`);

--
-- Constraints for table `product_images`
--
ALTER TABLE `product_images`
  ADD CONSTRAINT `FK_product_images_product` FOREIGN KEY (`fk_Productid_Product`) REFERENCES `product` (`id_Product`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `product_productgroup`
--
ALTER TABLE `product_productgroup`
  ADD CONSTRAINT `fk_ppg_product` FOREIGN KEY (`fk_Productid_Product`) REFERENCES `product` (`id_Product`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_ppg_productgroup` FOREIGN KEY (`fk_ProductGroupId_ProductGroup`) REFERENCES `productgroup` (`id_ProductGroup`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `product_returns`
--
ALTER TABLE `product_returns`
  ADD CONSTRAINT `FK_returns_admin` FOREIGN KEY (`fk_Adminid_Users`) REFERENCES `users` (`id_Users`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_returns_client` FOREIGN KEY (`fk_Clientid_Users`) REFERENCES `users` (`id_Users`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_returns_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_returns_courier` FOREIGN KEY (`returnCourierId`) REFERENCES `courier` (`id_Courier`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_returns_orders` FOREIGN KEY (`fk_ordersid_orders`) REFERENCES `orders` (`id_Orders`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_returns_status_type` FOREIGN KEY (`fk_ReturnStatusTypeid_ReturnStatusType`) REFERENCES `return_status_type` (`id_ReturnStatusType`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_product_returns_courier` FOREIGN KEY (`returnCourierId`) REFERENCES `courier` (`id_Courier`),
  ADD CONSTRAINT `fk_product_returns_order` FOREIGN KEY (`fk_ordersid_orders`) REFERENCES `orders` (`id_Orders`),
  ADD CONSTRAINT `fk_return_courier` FOREIGN KEY (`fk_Courierid_Courier`) REFERENCES `courier` (`id_Courier`) ON DELETE SET NULL;

--
-- Constraints for table `return_item`
--
ALTER TABLE `return_item`
  ADD CONSTRAINT `FK_ri_ordersproduct` FOREIGN KEY (`fk_OrdersProductid_OrdersProduct`) REFERENCES `ordersproduct` (`id_OrdersProduct`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_ri_reason` FOREIGN KEY (`reasonId`) REFERENCES `return_reason` (`id_ReturnReason`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_ri_returns` FOREIGN KEY (`fk_Returnsid_Returns`) REFERENCES `product_returns` (`id_Returns`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `shipment`
--
ALTER TABLE `shipment`
  ADD CONSTRAINT `FK_shipment_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_shipment_courier` FOREIGN KEY (`fk_Courierid_Courier`) REFERENCES `courier` (`id_Courier`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_shipment_orders` FOREIGN KEY (`fk_Ordersid_Orders`) REFERENCES `orders` (`id_Orders`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_shipment_return` FOREIGN KEY (`fk_Returnsid_Returns`) REFERENCES `product_returns` (`id_Returns`) ON DELETE SET NULL;

--
-- Constraints for table `shipment_status`
--
ALTER TABLE `shipment_status`
  ADD CONSTRAINT `FK_ss_shipment` FOREIGN KEY (`fk_Shipmentid_Shipment`) REFERENCES `shipment` (`id_Shipment`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_ss_type` FOREIGN KEY (`fk_ShipmentStatusTypeid_ShipmentStatusType`) REFERENCES `shipment_status_type` (`id_ShipmentStatusType`) ON UPDATE CASCADE;

--
-- Constraints for table `users`
--
ALTER TABLE `users`
  ADD CONSTRAINT `FK_users_company` FOREIGN KEY (`fk_Companyid_Company`) REFERENCES `company` (`id_Company`) ON DELETE SET NULL ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
