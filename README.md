# Service Tracking System

## Project Description

This project is a web application for tracking services.  It allows for the registration and management of Drivers and Employees.  The system uses ASP.NET Core, C#, and SQL Server. Key features include user authentication, role-based access control, and data management for drivers and employees.

## Project Structure

The project is organized as follows:

- **Areas:** Contains the different areas of the application:
    - **Areas/Driver:** Contains pages and controllers specific to the Driver functionality.  Includes a dashboard, registration page and layout.
    - **Areas/Employee:** Contains pages and controllers specific to the Employee functionality.  Includes address management, registration, and a home page.  
- **Controllers:** Contains controllers for handling requests.
- **Migrations:** Contains database migration scripts.
- **Models:** Contains the data models for the application.
- **Services:** Contains custom services, including email sending and user management.
- **Views:** Contains the views for the application.
- **wwwroot:** Contains static assets such as CSS, JavaScript, and libraries (Bootstrap, jQuery, and jQuery Validation).


## Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/OmerAksaray/ServiceTrackingSystem.git
   ```

2. **Prerequisites:**
   - .NET 8.0 SDK
   - SQL Server (with a database named `ServiceTrackingDB`)

3. **Build the application:**
   Open the `ServiceTrackingSystem.sln` solution file in Visual Studio and build the project.  Ensure that the connection string in `appsettings.json` points to your SQL Server instance and database.

## Configuration

- **appsettings.json:** Contains the main application settings, including the connection string to the database, base URL, and email settings.
- **appsettings.Development.json:** Contains development-specific settings.  In this case, only logging levels are specified.

**Configuration Options:**

- **ConnectionStrings:** Contains the connection string to the SQL Server database.  **Make sure this is updated to reflect your environment.**
- **ApplicationSettings:** Contains the base URL for the application.
- **EmailSettings:** Contains the email server settings for sending emails, including SMTP server, port, sender email, and password.  **Treat the password as highly sensitive and DO NOT commit it to source control.**  Consider using environment variables or a secrets manager for production.


## Running the Application

1. Make sure SQL Server is running and the `ServiceTrackingDB` database exists.
2. Navigate to the project directory in your terminal.
3. Run the application using the command:
   ```bash
   dotnet run
   ```
The application will start and be accessible via the `BaseUrl` specified in `appsettings.json` (default is `http://localhost:5086`).

## Deployment

Deployment instructions are not provided.  For deployment, you'll likely need to publish the application to a web server, configure the database connection string appropriately, and set up any necessary environment variables for email settings.

## Handling Sensitive Data

This project contains sensitive information such as API keys, connection strings, and passwords that should not be pushed to public repositories. Follow these steps to secure your sensitive data:

1. **Never commit real credentials to GitHub**. The repository includes `appsettings.json.template` with placeholder values.

2. For local development:
   - Copy `appsettings.json.template` to `appsettings.json`
   - Fill in your actual credentials in `appsettings.json`
   - The `.gitignore` file is configured to exclude these files from Git

3. For production deployment:
   - Use environment variables or a secure secret management service
   - For Azure, use Azure Key Vault or App Configuration
   - For AWS, use AWS Secrets Manager
   - For local servers, consider using environment variables

4. If you accidentally committed sensitive data:
   - Remove the sensitive data from your code
   - Change any exposed credentials immediately
   - Use tools like `git filter-branch` or the BFG Repo-Cleaner to remove sensitive data from Git history

Remember that once data is pushed to a public repository, it should be considered compromised, even if you later remove it.
