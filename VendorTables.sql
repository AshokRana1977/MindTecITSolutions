CREATE TABLE VendorMaster (
    VendorId INT PRIMARY KEY IDENTITY(1,1),
    VendorName VARCHAR(200) NOT NULL,
    Phone VARCHAR(50),
    Email VARCHAR(100),
    Address VARCHAR(MAX)
);

CREATE TABLE VendorContact (
    ContactId INT PRIMARY KEY IDENTITY(1,1),
    VendorId INT FOREIGN KEY REFERENCES VendorMaster(VendorId) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL,
    Title VARCHAR(100),
    Email VARCHAR(100),
    Phone VARCHAR(50),
    PrimaryContact BIT
);

CREATE TABLE VendorWebsite (
    WebsiteId INT PRIMARY KEY IDENTITY(1,1),
    VendorId INT FOREIGN KEY REFERENCES VendorMaster(VendorId) ON DELETE CASCADE,
    WebsiteUrl VARCHAR(200) NOT NULL,
    UserName VARCHAR(100),
    Password VARCHAR(200)
);
