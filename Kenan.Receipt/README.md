# Kenan.Receipt

[![License](https://img.shields.io/github/license/KenanExe/Kenan.Receipt)](https://github.com/KenanExe/Kenan.Receipt/blob/master/LICENSE) [![Language](https://img.shields.io/badge/Language-C%23-blue)](https://github.com/KenanExe/Kenan.Receipt) [![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)](https://github.com/KenanExe/Kenan.Receipt) [![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4)](https://dotnet.microsoft.com/) [![Release](https://img.shields.io/github/v/release/KenanExe/Kenan.Receipt)](https://github.com/KenanExe/Kenan.Receipt/releases/latest)

Create receipt PDFs from C# in a few lines. Returns the PDF as `byte[]`.

> Receipts made with this library are for demo and learning purposes. They are not legal documents.

## Install

```
dotnet add package Kenan.Receipt
```

## How to use

```csharp
using Kenan.Receipt;

// 1. Fill in the receipt data (all optional)
ReceiptMaker.BrandName = "Northwind Studio";   // default: empty
ReceiptMaker.InvoiceNumber = "RCPT-88213";     // default: empty (row hidden)
ReceiptMaker.Date = "2026-11-27";              // default: DateTime.Now
ReceiptMaker.CustomerName = "Jamie Rivera";    // default: empty (row hidden)
ReceiptMaker.Notes = "See you again!";         // default: "Thank you for your purchase."

// 2. Add items (at least one)
ReceiptMaker.Items.Add(new ReceiptItem { Name = "Pro plan", Amount = 1, Price = 19.00m });
ReceiptMaker.Items.Add(new ReceiptItem { Name = "Extra seat", Amount = 2, Price = 5.00m });

// 3. Create the PDF
byte[] pdf = ReceiptMaker.GetReceipt();

// 4. Save it (or return it from your API)
File.WriteAllBytes("receipt.pdf", pdf);
```

## Fields

| Field | Required | Default | Description |
|---|---|---|---|
| `BrandName` | no | empty | Name shown at the top |
| `InvoiceNumber` | no | empty | Receipt number |
| `Date` | no | `DateTime.Now` | Date text, shown as you write it |
| `CustomerName` | no | empty | Customer name |
| `Items` | yes | empty list | List of `ReceiptItem` (`Name`, `Amount`, `Price`) |
| `Notes` | no | "Thank you for your purchase." | Footer note |

An empty receipt number or customer name is not shown and leaves no blank space.
The total is calculated for you from `Amount * Price`.

## Good to know

- Values are `static`. To make another receipt, clear the old items first: `ReceiptMaker.Items.Clear();`
- Because of that, don't create two receipts at the same time from different threads (for now).

## Built with

[PDFsharp and MigraDoc](https://docs.pdfsharp.net) 6.2.4 (MIT).

## License

MIT