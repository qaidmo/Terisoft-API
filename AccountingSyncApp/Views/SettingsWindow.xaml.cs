using AccountingSyncAPP.Configuration;
using AccountingSyncAPP.Services;
using System.Windows;

namespace AccountingSyncApp.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        public ConnectionSettings? SavedSettings { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            // استدعاء دالة تحميل الإعدادات عند فتح النافذة
            LoadExistingSettings();
        }

        /// <summary>
        /// دالة لتحميل الإعدادات الحالية وعرضها في الحقول
        /// </summary>
        private void LoadExistingSettings()
        {
            var existingSettings = _settingsService.LoadSettings();
            if (existingSettings != null)
            {
                // تحميل بيانات SQL Server فقط
                BaseServerNameTextBox.Text = existingSettings.BaseServerName;
                InstanceNameTextBox.Text = existingSettings.InstanceName;
                DatabaseTextBox.Text = existingSettings.Database;
                UserTextBox.Text = existingSettings.User;
                PasswordBox.Password = existingSettings.Password;
                
                // 🗑️ تم حذف تحميل إعدادات Supabase لأننا لم نعد نستخدمها
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // --- التحقق من المدخلات ---

            // التحقق من حقول SQL Server فقط
            if (string.IsNullOrWhiteSpace(BaseServerNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(DatabaseTextBox.Text) ||
                string.IsNullOrWhiteSpace(UserTextBox.Text))
            {
                // رسالة معدلة لتناسب الوضع الجديد
                System.Windows.MessageBox.Show("الرجاء تعبئة بيانات الاتصال بقاعدة البيانات (اسم السيرفر، قاعدة البيانات، المستخدم).", 
                                "بيانات غير مكتملة", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // تعطيل الزر لمنع الضغط المتكرر
            var saveButton = sender as System.Windows.Controls.Button;
            if (saveButton != null) saveButton.IsEnabled = false;

            try
            {
                string serverName = string.IsNullOrWhiteSpace(InstanceNameTextBox.Text) 
                    ? BaseServerNameTextBox.Text 
                    : $"{BaseServerNameTextBox.Text}\\{InstanceNameTextBox.Text}";

                string connectionString = $"Server={serverName};Database={DatabaseTextBox.Text};User Id={UserTextBox.Text};Password={PasswordBox.Password};TrustServerCertificate=True;MultipleActiveResultSets=true;";

                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                }

                System.Windows.MessageBox.Show("تم الاتصال بقاعدة البيانات بنجاح.", "اتصال ناجح", MessageBoxButton.OK, MessageBoxImage.Information);

                // إنشاء كائن الإعدادات النظيف
                var settings = new ConnectionSettings
                {
                    BaseServerName = BaseServerNameTextBox.Text,
                    InstanceName = InstanceNameTextBox.Text,
                    Database = DatabaseTextBox.Text,
                    User = UserTextBox.Text,
                    Password = PasswordBox.Password
                };

                // حفظ الإعدادات الجديدة
                _settingsService.SaveSettings(settings);
                SavedSettings = settings;
                
                System.Windows.MessageBox.Show("تم حفظ الإعدادات بنجاح. التطبيق جاهز الآن لاستقبال البيانات.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true; // إعلام الكود الرئيسي بنجاح العملية
                Close();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"فشل الاتصال بقاعدة البيانات. الرجاء التأكد من صحة البيانات.\n\nتفاصيل الخطأ:\n{ex.Message}", 
                                               "خطأ في الاتصال", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (saveButton != null) saveButton.IsEnabled = true;
            }
        }
    }
}