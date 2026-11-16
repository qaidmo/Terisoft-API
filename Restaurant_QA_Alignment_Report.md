# Restaurant QA Alignment Report

## Comprehensive Alignment Audit: Sales vs. Restaurant Sync

This report details the architectural discrepancies found between the newly developed Restaurant module and the proven Sales module, outlining the root causes and precise structural fixes needed to mirror standard behavior.

---

### Task 1: Credit Customer Mapping (Data Defect)

**Root Cause:**
In `lib/controllers/restaurant_pos_controller.dart`, when mapping the `customerName` attribute to the `isolatedHeader`, a logical flaw exists in the fallback mechanism. The `customerNameController` is globally initialized with the default string text `'. . .'`, meaning `customerNameController.text.isNotEmpty` always evaluates to `true`. This entirely bypasses the ternary condition meant to extract the `creditAccName` for credit sales (زبون آجل). 

Furthermore, the SQLite database table structure tracks `customerNo` for internal local use, but the financial system backend requires the exact `account_id` when passing Credit transaction bindings for customers and banks. Right now, `restaurant_pos_controller.dart` incorrectly binds `selectedCustomer.value!['id']` instead of the SQL-required `['account_id']`. The Sales controller natively utilizes the correct identity.

**Proposed Sales Alignment Fix:**
We must respect the literal `...` text strictly for Cash/Network sales while exclusively overriding it only when a Credit Sale is executed. Concurrently, we must correct the Credit Account ID binding targeting `account_id` instead of the internal ID.

```dart
// 1. Fix the Account Parameter in `_saveInvoice()`
if (isCreditSale.value && selectedCustomer.value != null) {
  // MUST USE account_id NOT id
  creditAccId = selectedCustomer.value!['account_id'].toString(); 
  creditAccName = selectedCustomer.value!['custname'].toString();
}

// 2. Fix the Fallback Mapping in `isolatedHeader`
String finalCustomerName = isCreditSale.value ? creditAccName : customerNameController.text;

'customerName': finalCustomerName,
```

**Print Alignment Verification:**
This structure dynamically guarantees that `isolatedHeader['customerName']` encapsulates either the exact Credit Account string or the unaltered text field (e.g., `...`). Consequently, when `_directPrintInvoice(isolatedHeader)` builds the PDF, `customerName: invoiceData['customerName']` consistently relays this identical String straight into the `generateCompositeInvoice` pipeline for print alignment.

---

### Task 2: Box Parent Name (Data Defect)

**Root Cause:**
Currently, `restaurant_pos_controller.dart` executes a SQL `LEFT JOIN` on `sun_account` targeting a financial control account/grandparent. This skips over the actual immediate Box name context. The REST payload expects the string name of the Box, not necessarily the financial hierarchy grandparent value. 

**Proposed Exact Mapping Fix:**
Revise the SQLite query to bypass the `sun_account` join completely and fetch the `box_name` straight from `sun_boxes`.

```dart
// Modify the SQLite Query to selectively target the exact Box Name
final List<Map<String, dynamic>> boxResult = await db.rawQuery('''
  SELECT account_id, box_name AS control_name
  FROM sun_boxes
  WHERE box_no = ?
''', [queryBoxNo]);
```

---

### Task 3: The Unprinted Queue & Prompts (UX Defect)

**Root Cause:**
While `restaurant_pos_controller.dart` queries the `pending_prints` table in `checkAndPrintPendingInvoices()`, its processing loop was commented out to hide lint warnings, effectively abandoning all queued invoices. The `pending_prints` table is uniquely dedicated to the Restaurant module, meaning no SQL `WHERE` isolation or filtering is necessary.

**Proposed Queue & Validation Fix:**
Reconstruct `checkAndPrintPendingInvoices` to query the entire table without filters and reactivate the dialog prompt.

```dart
Future<void> checkAndPrintPendingInvoices() async {
  try {
    final db = await DatabaseHelper.instance.database;
    // ALIGNMENT: Fetch all pending prints; the table is dedicated to Restaurant.
    final pending = await db.query('pending_prints');
    
    for (var row in pending) {
      final int id = row['id'] as int;
      final String traNo = row['tra_no'] as String;
      final Map<String, dynamic> data = jsonDecode(row['invoice_data'] as String);
      
      await Get.defaultDialog(
        title: 'فاتورة معلقة',
        middleText: 'هل تريد طباعة الفاتورة المعلقة رقم \$traNo؟',
        barrierDismissible: true,
        textConfirm: 'طباعة فورية',
        textCancel: 'إلغاء',
        confirmTextColor: Colors.white,
        onConfirm: () async {
          Get.back(); // close dialog
          await _directPrintInvoice(data, traNo, pendingId: id);
        },
      );
    }
  } catch (e) {
    debugPrint("Error checking print queue: \$e");
  }
}
```

---

### Task 4: PDF Header & Logo Rendering (UX Defect)

**Root Cause:**
The Restaurant printing pipeline currently generates an unbranded PDF because `companyLogoBytes: null` and empty static strings are hardcoded into `_directPrintInvoice()`. We must bypass the complex Sales `InvoiceTemplatesController` and strictly enforce a "Lite" header representing only 3 legacy elements. 

**Proposed Legacy Restoration Fix:**
Tap directly into `_userState` values to perfectly restore the 3 strict elements: the Logo, the Company/Restaurant Name (`branchHeaderL`), and Contact Information (`branchAddress`).

```dart
// Update the PDF Builder properties inside _directPrintInvoice to use the 3 "Lite" elements
final pdfBytes = await PdfInventoryData.generateCompositeInvoice(
  headerL: _userState.branchHeaderL ?? _userState.safeUserDepartmentName, // 1. Company/Restaurant Name
  headerR: '',
  branchAddress: _userState.branchAddress ?? '',                           // 2. Contact Information
  companyLogoBytes: _userState.companyLogo,                                // 3. Company/Restaurant Logo
  transactionNumber: traNo,
  // ... remaining existing parameters
);
```
