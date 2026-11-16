using System.Diagnostics;

namespace AccountingSyncAPP.Configuration
{
    public class ConnectionSettings
    {
        // =========================================================
        // إعدادات SQL Server (مطلوبة وضرورية جداً)
        // =========================================================
        public string? BaseServerName { get; set; } // مثال: DESKTOP-DHV2UG0
        public string? InstanceName { get; set; }   // مثال: TS
        public string? Database { get; set; }
        public string? User { get; set; }
        public string? Password { get; set; }

        // 🛑 تم حذف SupabaseUrl و SupabaseKey لأننا لم نعد نستخدمهما 🛑

        public string ToSqlConnectionString()
        {
            // 1. بناء اسم الخادم والمثبت بناءً على البيانات المُدخلة
            string serverFullName;

            if (string.IsNullOrWhiteSpace(InstanceName))
            {
                // إذا لم يتم إدخال اسم المثبت، نستخدم اسم الخادم الأساسي فقط
                serverFullName = BaseServerName ?? "";
            }
            else
            {
                // إذا تم إدخال اسم المثبت، نستخدم اسم الخادم الأساسي متبوعًا بـ \ واسم المثبت
                serverFullName = $"{BaseServerName}\\{InstanceName}";
            }

            // 2. استخدام serverFullName مباشرة لـ safeServer
            var safeServer = serverFullName;

            // 3. بناء سلسلة الاتصال (إعدادات التشفير مهمة جداً للاتصال الحديث)
            var connectionString = $"Server={safeServer};Database={Database};User Id={User};Password={Password};Encrypt=True;TrustServerCertificate=True;";

            // 4. طباعة السلسلة لتصحيح الأخطاء (مفيد جداً في وضع الـ Debug)
            Debug.WriteLine($"--- SQL CONNECTION STRING: {connectionString} ---");

            return connectionString;
        }
    }
}