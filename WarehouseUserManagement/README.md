<h1 align="center">
🏬 Warehouse Management System
</h1>

<p align="center">
An enterprise warehouse management system built to organize inventory operations, purchasing, stock movements, and reporting.
</p>

<p align="center">

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)
![Entity Framework](https://img.shields.io/badge/ORM-Entity%20Framework%20Core-purple)
![Bootstrap](https://img.shields.io/badge/UI-Bootstrap-blueviolet)

</p>

---

# 📌 Overview

**Warehouse Management System** is an internal business application designed to manage warehouse operations efficiently.

The system helps companies control inventory, manage purchasing processes, track stock movements, and generate reports to support better decision-making.

The system provides a secure environment using authentication and role-based permissions to control access to different modules.

---

# 🎯 Project Objectives

- Manage warehouse products efficiently.
- Track incoming and outgoing materials.
- Maintain accurate inventory quantities.
- Calculate inventory value.
- Reduce manual warehouse operations.
- Provide clear reports and dashboard analytics.

---

# 🚀 Main Features

## 🔐 Authentication & Authorization

- Secure user login.
- Role-based access control.
- Permission management.
- Control access to system features.

---

## 📦 Product Management

- Create and manage products.
- Organize products by categories.
- Manage product information.
- Track:
  - Current quantity.
  - Average cost.
  - Minimum stock level.
- Prevent duplicate products.

---

## 🛒 Purchase Invoice Management

- Create purchase invoices.
- Add products directly from invoices.
- Calculate:

  - Subtotal.
  - Sales tax.
  - Grand total.

- Approve invoices before updating inventory.
- Maintain purchase history.

---

## 📤 Stock Issue Management

- Issue materials to company departments.
- Track receiver information.
- Validate available stock quantity.
- Update inventory automatically after approval.

---

## 🔄 Inventory Management

- Track all stock movements.

Supported transactions:

- Purchase.
- Issue.
- Stock Take.
- Adjustment.

---

## 📋 Stock Taking

- Compare system quantity with actual quantity.
- Calculate differences.
- Create inventory adjustments.

---

## 📊 Dashboard & Reports

The system provides:

### Dashboard

- Total products.
- Inventory value.
- Low stock items.
- Purchase statistics.
- Stock movement overview.

### Reports

- Current inventory report.
- Inventory valuation report.
- Purchase reports.
- Issue reports.
- Stock movement report.
- Stock taking report.

Export formats:

- PDF
- Excel
- Word

---

# 🏗 System Modules

| Module | Description |
|---|---|
| Users | Manage users and access |
| Roles & Permissions | Control system permissions |
| Departments | Manage material receiving departments |
| Categories | Organize products |
| Products | Manage warehouse items |
| Purchase Invoices | Manage incoming stock |
| Issues | Manage outgoing stock |
| Stock Transactions | Track inventory history |
| Stock Take | Inventory counting |
| Reports | Business reports |
| Dashboard | Data visualization |

---

# 🔄 System Workflow

```
Supplier
    |
    ▼
Purchase Invoice
    |
    ▼
Invoice Approval
    |
    ▼
Increase Inventory
    |
    ▼
Stock Transactions
    |
    ▼
Issue Materials To Departments
    |
    ▼
Inventory Reports & Dashboard
```

---

# 🛠 Technologies

### Backend

- ASP.NET Core MVC
- C#
- Entity Framework Core
- LINQ

### Database

- SQL Server

### Frontend

- HTML
- CSS
- Bootstrap
- JavaScript

### Reporting

- SSRS / Report Generation
- PDF Export
- Excel Export

---

# 🗄 Database Design

The system uses a relational database design with entities such as:

- Users
- Roles
- Permissions
- Products
- Categories
- Departments
- PurchaseInvoices
- PurchaseInvoiceDetails
- Issues
- IssueDetails
- StockTransactions
- StockTake
- StockTakeDetails

---

# 🔒 Security

The system includes:

- Password protection.
- Role-based authorization.
- Permission validation.
- Audit information for important operations.

---

# 📈 Future Enhancements

Future improvements:

- Barcode scanner integration.
- Multi warehouse support.
- Multi branch support.
- Mobile application.
- ERP integration.
- Advanced analytics.

---

# 👩‍💻 Developer

**Rania Taweel**

Full Stack Developer
