using System;

namespace AccountingSyncAPP.Services
{
    /// <summary>
    /// خدمة بسيطة تعمل كوسيط لإرسال إشارات بين مكونات التطبيق.
    /// </summary>
    public class SyncStateService
    {
        // هذا هو "الحدث" الذي سيتم إطلاقه عند بدء أول مزامنة
        public event Action? OnFirstSyncStarted;

        // سيقوم الـ Worker باستدعاء هذه الدالة لإطلاق الحدث
        public void ReportFirstSyncStarted()
        {
            OnFirstSyncStarted?.Invoke();
        }
    }
}