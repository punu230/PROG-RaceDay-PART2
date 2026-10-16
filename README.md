# RaceDay System

A full-stack web-based event management system designed for the South African road running, walking, and cycling community.

![RaceDay](https://img.shields.io/badge/RaceDay-Event%20Management-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-Academic-green)

---

## Table of Contents

1. [Overview](#overview)
2. [Part 2: RESTful API](#part-2-restful-api)
3. [User Roles](#user-roles)
4. [Setup Instructions](#setup-instructions)
5. [API Endpoints](#api-endpoints)
6. [Authentication](#authentication)
7. [Testing](#testing)
8. [CI/CD Pipeline](#cicd-pipeline)
9. [Screenshots](#screenshots)
10. [Video Presentation](#video-presentation)
11. [Technologies Used](#technologies-used)
12. [Part 1: System Planning](#part-1-system-planning)

---

## Overview

The RaceDay platform is a full-stack web application for managing road running, walking, and cycling events in South Africa. It allows **Event Organisers** to create and manage events, and **Participants** to browse events, enrol, and track their results.

This repository contains:
- **Part 1** — System planning, ERD, API plan, SQL database script
- **Part 2** — RESTful API implementation with authentication, testing, and CI/CD

---

## Part 2: RESTful API

The RaceDay RESTful API is built with **ASP.NET Core 8** using **Entity Framework Core (Code-First)** with **SQL Server**. It provides JWT-authenticated endpoints with role-based access control.

### Features Implemented
- ✅ User registration and login (Organiser and Participant)
- ✅ JWT Bearer authentication with BCrypt password hashing
- ✅ Role-based access control
- ✅ Full CRUD for Events and Categories
- ✅ Event enrolments
- ✅ Race results management
- ✅ 27 unit tests with 100% pass rate
- ✅ Swagger UI documentation with accurate auth indicators
- ✅ GitHub Actions CI/CD pipeline

---

## User Roles

| Role | Description | Permissions |
|------|-------------|-------------|
| **Organiser** | Event creator and manager | Create, update, delete events; add categories; view enrolments; record results |
| **Participant** | Event participant | Browse events; enrol in categories; view own enrolments and results |

---

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or Express)
- Visual Studio 2022 (or VS Code with C# extension)

### Steps to Run

1. **Clone the repository:**
   ```bash
   git clone https://github.com/punu230/PROG-RaceDay-PART2.git
   cd PROG-RaceDay-PART2

   <img width="1895" height="612" alt="ci-green-build" src="https://github.com/user-attachments/assets/cb6e2a14-53a3-417c-8b7a-71dc3341cbe7" />
   <img width="1919" height="601" alt="tests-passing" src="https://github.com/user-attachments/assets/9698452d-5716-4412-ac53-950fd9491952" />

