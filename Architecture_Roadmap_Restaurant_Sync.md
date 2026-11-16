# End-to-End Architectural Roadmap

### Part 1: Comprehension Map (The Data Flow)

**1. Mobile Local Storage (`untitled4`)**
The invoice is constructed in `RestaurantPosController` and saved to the isolated SQLite tables (`restaurant_invoices` and `restaurant_invoice_details`). Structurally, this matches the legacy system but carries our newer columns (`boxNo`, `boxParentName`, `notes`, `created_at`).

**2. The Sync Trigger & Payload Formulation (Flutter)**
A sync worker (e.g., inside `sync_service.dart` or a new `RestaurantSyncService`) identifies unsynced records (`is_synced = 0`) from the SQLite tables. Crucially, it reconstructs a JSON payload whose structure and keys (the Data Contract) are a 100% clone of the payload sent by the legacy `untitled8` app.

**3. The Network Transition**
The Flutter app issues an `HTTP POST` request to the new endpoint in `AccountingSyncApp` (e.g., `api/RestaurantSync/sync-invoice`), sending the cloned API payload.

**4. The Target Wrapper (`RestaurantSyncController.cs`)**
The payload lands in the new C# backend. Following the modern `TransactionController.cs` pattern, this controller uses Dependency Injection to acquire the `AccountingDbContext` to fetch the DB string and adheres to the new tokenized authentication (`[Authorize]`). It deserializes the JSON using the modern cloned DTOs (`RestaurantInvoiceDto`).

**5. The Core Injection (Soft-Swap Logic)**
Once the payload is validated, the controller extracts the raw DB connection from the DbContext, opens an ADO.NET `SqlConnection`, and begins a `SqlTransaction`. It then executes the **exact same raw string queries** as the legacy `PosController.cs`—dynamically targeting `SUNTRANSHEAD`, `SUNTRANSHEADLINE`, `SUNTRANSDETAILITEM`, and `SUNTRANSJE`. `BoxNo` is strictly mapped to `TRAACCID`, and `Notes` is passed into `TRADREMARK`, perfectly executing the insert while guaranteeing zero impact on the production SQL Server schema.

---

### Part 2: Execution Roadmap (The Step-by-Step Plan)

**📱 Flutter Execution (`untitled4`)**
1. **Service Blueprinting:** Create the background sync mechanism (or add it to `sync_service.dart`/`restaurant_repository.dart`) that selects `SELECT * FROM restaurant_invoices WHERE is_synced = 0`.
2. **Payload Formatting:** Write a strict mapping method that bundles the header and details back into the legacy JSON payload structure without dropping `boxNo` or `notes`.
3. **API Routing:** Route the `http.post` call to the new `AccountingSyncApp` endpoints.
4. **Lifecycle State Handlers:** On receiving `200 OK`, successfully update `is_synced = 1` in the isolated SQLite DB to prevent duplicate processing.

**⚙️ Backend Execution (`AccountingSyncApp`)**
5. **File Creation:** Create `Controllers/RestaurantSyncController.cs`.
6. **DTO Implementation:** Define `RestaurantInvoiceDto` and `RestaurantInvoiceDetailDto` DTO records matching the exact keys required to catch the legacy payload.
7. **Architectural Wrapping:** Implement Dependency Injection for standard logging, config binding, DbContext access, and apply `[ApiController]` annotations and proper HTTP responses.
8. **Pristine Logic Porting:** Transplant the legacy SQL Connection extraction and all raw `INSERT INTO` commands exactly as they lived in the legacy `Tearsoft_restrent` `PosController`, adapting only the parameters binding layer so no schema modifications (`ALTER TABLE`) are ever necessary.
