//------------------------------------------------------------------------------
// <auto-generated>
//    這個程式碼是由範本產生。
//
//    對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//    如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Finder
{
    using System;
    using System.Collections.Generic;
    
    public partial class Task
    {
        public Task()
        {
            this.ElectricityBill = new HashSet<ElectricityBill>();
            this.UserTask = new HashSet<UserTask>();
        }
    
        public int TID { get; set; }
        public int TMID { get; set; }
        public string Name { get; set; }
    
        public virtual ICollection<ElectricityBill> ElectricityBill { get; set; }
        public virtual TaskModel TaskModel { get; set; }
        public virtual ICollection<UserTask> UserTask { get; set; }
    }
}