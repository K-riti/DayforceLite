-- DayforceLite Database Schema
-- Run this script to create the database schema

-- Create Departments table
CREATE TABLE Departments (
	DepartmentId    INT IDENTITY(1,1) PRIMARY KEY,
	Name            NVARCHAR(100) NOT NULL,
	CostCentre      NVARCHAR(20)  NOT NULL,
	Description     NVARCHAR(500) NULL,
	ManagerId       INT           NULL,
	IsActive        BIT           DEFAULT 1,
	CreatedAt       DATETIME2     DEFAULT GETUTCDATE(),
	UpdatedAt       DATETIME2     NULL
);

-- Create Employees table
CREATE TABLE Employees (
	EmployeeId      INT IDENTITY(1,1) PRIMARY KEY,
	FirstName       NVARCHAR(100) NOT NULL,
	LastName        NVARCHAR(100) NOT NULL,
	Email           NVARCHAR(255) NOT NULL UNIQUE,
	DepartmentId    INT           NOT NULL REFERENCES Departments(DepartmentId),
	HourlyRate      DECIMAL(10,2) NOT NULL,
	StartDate       DATE          NOT NULL,
	IsActive        BIT           DEFAULT 1,
	CreatedAt       DATETIME2     DEFAULT GETUTCDATE(),
	RowVersion      ROWVERSION    -- optimistic concurrency
);

-- Add manager foreign key after Employees table exists
ALTER TABLE Departments ADD CONSTRAINT FK_Departments_Manager 
	FOREIGN KEY (ManagerId) REFERENCES Employees(EmployeeId);

-- Create Timesheets table
CREATE TABLE Timesheets (
	TimesheetId     INT IDENTITY(1,1) PRIMARY KEY,
	EmployeeId      INT           NOT NULL REFERENCES Employees(EmployeeId),
	WeekStartDate   DATE          NOT NULL,
	RegularHours    DECIMAL(5,2)  NOT NULL,
	OvertimeHours   DECIMAL(5,2)  DEFAULT 0,
	Status          NVARCHAR(20)  DEFAULT 'Draft', -- Draft/Submitted/Approved
	SubmittedAt     DATETIME2,
	ApprovedAt      DATETIME2,
	ApprovedBy      INT           REFERENCES Employees(EmployeeId)
);

-- Create PayrollRecords table
CREATE TABLE PayrollRecords (
	PayrollId       INT IDENTITY(1,1) PRIMARY KEY,
	EmployeeId      INT           NOT NULL REFERENCES Employees(EmployeeId),
	PeriodStart     DATE          NOT NULL,
	PeriodEnd       DATE          NOT NULL,
	GrossPay        DECIMAL(12,2) NOT NULL,
	TaxDeduction    DECIMAL(12,2) NOT NULL,
	NetPay          DECIMAL(12,2) NOT NULL,
	ProcessedAt     DATETIME2     DEFAULT GETUTCDATE()
);

-- Create LeaveRequests table
CREATE TABLE LeaveRequests (
	LeaveRequestId  INT IDENTITY(1,1) PRIMARY KEY,
	EmployeeId      INT           NOT NULL REFERENCES Employees(EmployeeId),
	LeaveType       NVARCHAR(20)  NOT NULL, -- Vacation/Sick/Personal/Bereavement/Unpaid
	StartDate       DATE          NOT NULL,
	EndDate         DATE          NOT NULL,
	TotalDays       DECIMAL(5,2)  NOT NULL,
	Reason          NVARCHAR(500) NULL,
	Status          NVARCHAR(20)  DEFAULT 'Pending', -- Pending/Approved/Rejected/Cancelled
	ApprovedBy      INT           NULL REFERENCES Employees(EmployeeId),
	ApprovedAt      DATETIME2     NULL,
	ApproverComments NVARCHAR(500) NULL,
	CreatedAt       DATETIME2     DEFAULT GETUTCDATE(),
	UpdatedAt       DATETIME2     NULL
);

-- Create LeaveBalances table
CREATE TABLE LeaveBalances (
	LeaveBalanceId  INT IDENTITY(1,1) PRIMARY KEY,
	EmployeeId      INT           NOT NULL REFERENCES Employees(EmployeeId),
	Year            INT           NOT NULL,
	VacationDays    DECIMAL(5,2)  NOT NULL DEFAULT 15,
	SickDays        DECIMAL(5,2)  NOT NULL DEFAULT 10,
	PersonalDays    DECIMAL(5,2)  NOT NULL DEFAULT 3,
	VacationUsed    DECIMAL(5,2)  NOT NULL DEFAULT 0,
	SickUsed        DECIMAL(5,2)  NOT NULL DEFAULT 0,
	PersonalUsed    DECIMAL(5,2)  NOT NULL DEFAULT 0,
	UpdatedAt       DATETIME2     DEFAULT GETUTCDATE(),
	CONSTRAINT UQ_LeaveBalance_Employee_Year UNIQUE (EmployeeId, Year)
);

-- Create AuditLogs table
CREATE TABLE AuditLogs (
	AuditLogId      BIGINT IDENTITY(1,1) PRIMARY KEY,
	EntityType      NVARCHAR(50)  NOT NULL,
	EntityId        NVARCHAR(50)  NOT NULL,
	Action          NVARCHAR(20)  NOT NULL,
	OldValues       NVARCHAR(MAX) NULL,
	NewValues       NVARCHAR(MAX) NULL,
	UserId          INT           NULL,
	UserName        NVARCHAR(100) NULL,
	Timestamp       DATETIME2     DEFAULT GETUTCDATE(),
	IpAddress       NVARCHAR(45)  NULL
);

-- Create indexes for better performance
CREATE INDEX IX_Employees_DepartmentId ON Employees(DepartmentId);
CREATE INDEX IX_Employees_Email ON Employees(Email);
CREATE INDEX IX_Timesheets_EmployeeId ON Timesheets(EmployeeId);
CREATE INDEX IX_Timesheets_WeekStartDate ON Timesheets(WeekStartDate);
CREATE INDEX IX_PayrollRecords_EmployeeId ON PayrollRecords(EmployeeId);
CREATE INDEX IX_PayrollRecords_PeriodStart ON PayrollRecords(PeriodStart);
CREATE INDEX IX_LeaveRequests_EmployeeId ON LeaveRequests(EmployeeId);
CREATE INDEX IX_LeaveRequests_Status ON LeaveRequests(Status);
CREATE INDEX IX_LeaveBalances_EmployeeId ON LeaveBalances(EmployeeId);
CREATE INDEX IX_AuditLogs_EntityType ON AuditLogs(EntityType);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);

-- Stored procedure for payroll summary
GO
CREATE PROCEDURE usp_GetEmployeePayrollSummary
	@EmployeeId INT,
	@FromDate   DATE,
	@ToDate     DATE
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		e.EmployeeId,
		e.FirstName + ' ' + e.LastName AS FullName,
		ISNULL(SUM(p.GrossPay), 0)  AS TotalGross,
		ISNULL(SUM(p.NetPay), 0)    AS TotalNet,
		COUNT(p.PayrollId)          AS PayslipCount
	FROM Employees e
	LEFT JOIN PayrollRecords p ON e.EmployeeId = p.EmployeeId
		AND p.PeriodStart >= @FromDate
		AND p.PeriodEnd   <= @ToDate
	WHERE e.EmployeeId = @EmployeeId
	GROUP BY e.EmployeeId, e.FirstName, e.LastName;
END
GO

-- Insert sample data
INSERT INTO Departments (Name, CostCentre, Description) VALUES 
('Engineering', 'ENG-001', 'Software development and engineering'),
('Human Resources', 'HR-001', 'People operations and HR services'),
('Finance', 'FIN-001', 'Financial planning and accounting'),
('Marketing', 'MKT-001', 'Marketing and communications');

INSERT INTO Employees (FirstName, LastName, Email, DepartmentId, HourlyRate, StartDate) VALUES
('John', 'Doe', 'john.doe@dayforcelite.com', 1, 75.00, '2023-01-15'),
('Jane', 'Smith', 'jane.smith@dayforcelite.com', 1, 80.00, '2022-06-01'),
('Bob', 'Johnson', 'bob.johnson@dayforcelite.com', 2, 65.00, '2023-03-20'),
('Alice', 'Williams', 'alice.williams@dayforcelite.com', 3, 70.00, '2022-09-10');

-- Set department managers
UPDATE Departments SET ManagerId = 2 WHERE DepartmentId = 1; -- Jane manages Engineering
UPDATE Departments SET ManagerId = 3 WHERE DepartmentId = 2; -- Bob manages HR
UPDATE Departments SET ManagerId = 4 WHERE DepartmentId = 3; -- Alice manages Finance

INSERT INTO Timesheets (EmployeeId, WeekStartDate, RegularHours, OvertimeHours, Status) VALUES
(1, '2024-01-01', 40.00, 5.00, 'Approved'),
(1, '2024-01-08', 40.00, 0.00, 'Approved'),
(2, '2024-01-01', 40.00, 8.00, 'Approved'),
(2, '2024-01-08', 40.00, 2.00, 'Submitted');

-- Initialize leave balances for current year
INSERT INTO LeaveBalances (EmployeeId, Year, VacationDays, SickDays, PersonalDays) VALUES
(1, 2024, 15, 10, 3),
(2, 2024, 18, 10, 3), -- Senior employee gets more vacation
(3, 2024, 15, 10, 3),
(4, 2024, 15, 10, 3);

PRINT 'DayforceLite schema created successfully!';
