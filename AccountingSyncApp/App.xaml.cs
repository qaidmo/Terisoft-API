using AccountingSyncAPP.Services;
using AccountingSyncApp.Views;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms; // تأكد من وجود هذا الـ Namespace لأيقونة النظام
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace AccountingSyncApp
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;

        // ملاحظة: الـ Main Host يتم إدارته في Program.cs
        // هذا الكلاس مسؤول فقط عن الواجهة وأيقونة شريط المهام

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // التحقق من وجود الإعدادات عند البدء
            var settingsService = new SettingsService();
            var settings = settingsService.LoadSettings();

            if (settings == null)
            {
                // إذا لم توجد إعدادات، نفتح نافذة الإعدادات فوراً
                var settingsWindow = new SettingsWindow();
                if (settingsWindow.ShowDialog() != true || settingsWindow.SavedSettings == null)
                {
                    // إذا ألغى المستخدم العملية، نغلق التطبيق بالكامل
                    Shutdown();
                    return;
                }
            }

            // تشغيل أيقونة شريط المهام
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon();

            try
            {
                // محاولة تحميل الأيقونة الخاصة بالتطبيق
                // ملاحظة: تأكد أن ملف sync.ico موجود في مجلد Assets وأن خاصية Build Action هي "Embedded Resource"
                string resourceName = "AccountingSyncApp.Assets.sync.ico";
                var iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

                if (iconStream != null)
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                }
                else
                {
                    // أيقونة احتياطية في حال لم يجد الملف
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                // في حال حدوث أي خطأ، نستخدم أيقونة النظام الافتراضية
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon.Text = "خدمة المزامنة المحلية (Local API)";
            _notifyIcon.Visible = true;

            // قائمة الزر الأيمن
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("إعدادات الاتصال", null, OnShowSettingsClicked);
            contextMenu.Items.Add(new ToolStripSeparator()); // خط فاصل
            contextMenu.Items.Add("خروج وإيقاف الخدمة", null, OnExitClicked);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void OnShowSettingsClicked(object? sender, System.EventArgs e)
        {
            // فتح نافذة الإعدادات
            var settingsWindow = new Views.SettingsWindow();

            // إذا ضغط المستخدم على "حفظ"
            if (settingsWindow.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    "تم حفظ إعدادات قاعدة البيانات الجديدة.\nيجب إعادة تشغيل التطبيق لتطبيق التغييرات.\n\nهل تريد إعادة التشغيل الآن؟",
                    "إعادة التشغيل مطلوبة",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 🔄 كود إعادة التشغيل التلقائي 🔄
                    var currentProcess = Process.GetCurrentProcess();
                    var fileName = Environment.ProcessPath; // مسار ملف exe الحالي

                    if (fileName != null)
                    {
                        Process.Start(fileName); // تشغيل نسخة جديدة
                        Environment.Exit(0);     // إغلاق النسخة الحالية
                    }
                }
            }
        }

        private void OnExitClicked(object? sender, System.EventArgs e)
        {
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // تنظيف الأيقونة من الذاكرة عند الخروج
            _notifyIcon?.Dispose();
            base.OnExit(e);

            // ضمان إغلاق الـ Web API وكل العمليات الخلفية
            Environment.Exit(0);
        }
    }
}