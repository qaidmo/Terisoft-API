using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    // هذا الموديل يمثل جدول وحدات الأصناف في SQL Server
    [Table("SUNITEMSIZE")]
    public class SunItemSize
    {
        [Key]
        [Column("SIZID")]
        public string? SizId { get; set; } // المفتاح الأساسي (مثل: 39-003.0)

        [Column("SIZITMID")]
        public string? SizItmId { get; set; } // رقم الصنف للأب

        [Column("SIZNAME")]
        public string? SizName { get; set; } // اسم الوحدة

        [Column("SIZVALUE")]
        public string? SizValue { get; set; } // معامل التحويل (نصي من المصدر)

        [Column("SIZINDEX")]
        public string? SizIndex { get; set; } // الترتيب (نصي من المصدر)

        [Column("SIZCODE")]
        public string? SizCode { get; set; } // الباركود

        [Column("SIZPRICE")]
        public string? SizPrice { get; set; } // السعر (نصي من المصدر)

        [Column("SIZSTATE")]
        public string? SizState { get; set; } // الحالة (0 = نشط)

        [Column("EDTDATE")]
        public string? EdtDate { get; set; } // تاريخ التعديل

        [Column("SIZMAXNO")]
        public string? SizMaxNo { get; set; }

        [Column("SIZCODEW")]
        public string? SizCodeW { get; set; }

        [Column("SIZPRICE2")]
        public string? SizPrice2 { get; set; }

        [Column("SIZPRICE3")]
        public string? SizPrice3 { get; set; }

        [Column("SIZCLASS")]
        public string? SizClass { get; set; }

        [Column("SIZREMARKS")]
        public string? SizRemarks { get; set; }

        [Column("SIZINVSTATE")]
        public string? SizInvState { get; set; }

        [Column("SIZSTOP")]
        public string? SizStop { get; set; }

        [Column("SIZSTOPDATE")]
        public string? SizStopDate { get; set; }

        [Column("SIZSTOPDATEID")]
        public string? SizStopDateId { get; set; }

        [Column("SIZMAXSTATE")]
        public string? SizMaxState { get; set; }

        [Column("SIZMINSTATE")]
        public string? SizMinState { get; set; }

        [Column("SYNCFLAG")]
        public string? SyncFlag { get; set; }
    }
}