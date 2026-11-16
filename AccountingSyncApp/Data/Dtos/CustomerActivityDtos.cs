using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل الطلب: يحدد الفترة والعملة والفرع لجلب ملخص حركة العملاء
    /// </summary>
    public class CustomerActivityRequestDto
    {
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        // لتحديد الجدول الديناميكي
        [Required]
        public string Year { get; set; }

        [Required]
        public int BranchId { get; set; }

        public int DepartmentId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        // للفلترة حسب صلاحيات المستخدم (اختياري)
        public int? UserId { get; set; }
    }

    /// <summary>
    /// موديل الاستجابة: يحتوي على القائمة والمجاميع الكلية (اختياري)
    /// </summary>
    public class CustomerActivityResponseDto
    {
        public List<CustomerActivityItemDto> Data { get; set; }

        // مجاميع عامة للتقرير (مفيد للعرض في أسفل الشاشة)
        public decimal TotalSalesAll { get; set; }
        public decimal TotalReturnsAll { get; set; }
        public decimal TotalCollectionsAll { get; set; }

        public CustomerActivityResponseDto()
        {
            Data = new List<CustomerActivityItemDto>();
        }
    }

    /// <summary>
    /// موديل السطر الواحد: يمثل بيانات عميل واحد في الجدول
    /// </summary>
    public class CustomerActivityItemDto
    {
        public string CustomerName { get; set; }       // اسم العميل (TraPatName)
        public string CustomerNo { get; set; }         // رقم العميل (TraAccId)

        // الأعمدة المالية (يتم حسابها في SQL)
        public decimal TotalSales { get; set; }        // مجموع (601, 603, 633)
        public decimal TotalReturns { get; set; }      // مجموع (602, 604, 634)
        public decimal NetSales { get; set; }          // Sales - Returns

        public decimal TotalCollections { get; set; }  // (201) - (202) [قبض - صرف]

        // الحقول المحسوبة (Calculated Fields)
        public decimal OpeningBalance { get; set; }               // الرصيد الافتتاحي
        public decimal PeriodBalance { get; set; }        // NetSales - Collections (رصيد الحركة)
        public decimal BalancePercentage { get; set; }    // نسبة الرصيد المتبقي

        // التواريخ وعمر الدين
        public string LastInvoiceDate { get; set; }    // أحدث تاريخ بيع
        public string LastPaymentDate { get; set; }    // أحدث تاريخ قبض
        public int DebtAge { get; set; }               // الفرق بالأيام بين اليوم وآخر فاتورة
    }
}