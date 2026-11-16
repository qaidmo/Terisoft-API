using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل استجابة تقرير حركة الأصناف - يحتوي على 34 عمود لكل صف
    /// </summary>
    public class ItemMovementResponseDto
    {
        public int RowCount { get; set; }
        public decimal GrandTotalAmount { get; set; }
        public decimal GrandNetAmount { get; set; }
        public decimal GrandTotalCost { get; set; }
        public decimal GrandMargin { get; set; }
        public List<ItemMovementRowDto> Rows { get; set; } = new List<ItemMovementRowDto>();
    }

    /// <summary>
    /// بيانات صف واحد في تقرير حركة الأصناف (34 عمود)
    /// جميع الأسماء هنا تطابق أسماء الأعمدة في SELECT SQL
    /// </summary>
    public class ItemMovementRowDto
    {
        // --- بيانات الصنف ---
        public string ItemNo { get; set; }              // رقم الصنف
        public string ItemName { get; set; }            // اسم الصنف
        public string ItemUnit { get; set; }            // الوحدة
        public string PackFactor { get; set; }          // العبوة

        // --- الكميات ---
        public decimal Quantity { get; set; }           // الكمية (دائماً موجبة)
        public decimal FreeQuantity { get; set; }       // كمية مجاني

        // --- الأسعار والقيم ---
        public decimal UnitPrice { get; set; }          // القيمة (سعر الوحدة)
        public decimal TotalAmount { get; set; }        // الإجمالي (الكمية * السعر)
        public decimal NetUnitPrice { get; set; }       // القيمة بعد الخصم (سعر الوحدة بعد الخصم)
        public decimal FreeValue { get; set; }          // قيمة المجاني

        // --- بيانات الحركة ---
        public string Date { get; set; }                // التاريخ
        public string Description { get; set; }         // الوصف (اسم الحركة)
        public string TransactionNumber { get; set; }   // رقم الحركة
        public string ReferenceNumber { get; set; }     // المرجع
        public string CustomerName { get; set; }        // الاسم (عميل/مورد)
        public string ExpiryDate { get; set; }          // تاريخ الانتهاء
        public string BatchNumber { get; set; }         // رقم الدفعة

        // --- القيم المالية ---
        public decimal NetAmount { get; set; }          // الصافي (الكمية * سعر الوحدة بعد الخصم)
        public string Currency { get; set; }            // العملة
        public decimal CostPrice { get; set; }          // التكلفة
        public decimal TotalCost { get; set; }          // إجمالي التكلفة
        public decimal Margin { get; set; }             // الهامش
        public decimal ExchangeRate { get; set; }       // سعر الصرف
        public decimal LocalNetAmount { get; set; }     // الصافي محلي

        // --- بيانات الحساب ---
        public string AccountNumber { get; set; }       // رقم الحساب
        public string AccountName { get; set; }         // اسم الحساب
        public string CostCenterName { get; set; }      // مركز التكلفة

        // --- بيانات المندوب والمحصل ---
        public string SalespersonName { get; set; }     // المندوب
        public string CollectorName { get; set; }       // المحصل

        // --- بيانات الفرع والقسم ---
        public string BranchId { get; set; }            // الفرع
        public string DepartmentId { get; set; }        // القسم

        // --- بيانات المخزن والمستخدم ---
        public string StoreNo { get; set; }             // رقم المخزن
        public string StoreName { get; set; }           // اسم المخزن
        public string UserName { get; set; }            // المستخدم

        // --- نوع الحركة (للمعلومات فقط) ---
        public string TransactionTypeId { get; set; }   // كود نوع الحركة
        public string TransactionTypeName { get; set; } // اسم نوع الحركة
    }
}
