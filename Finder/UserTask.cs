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
    
    public partial class UserTask
    {
        public string UID { get; set; }
        public int TID { get; set; }
        public System.DateTime CreateTime { get; set; }
        public System.DateTime UpdateTime { get; set; }
        public string Schedule { get; set; }
    
        public virtual Task Task { get; set; }
        public virtual User User { get; set; }
    }
}
