using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XoKeyHostApp
{
    public delegate void Log_Msg_Handler(LogMsg msg);

    public class LogMsg
    {
        public enum Priority
        {            
            Critical = 0,
            Error = 1,
            Warning = 2,
            Info = 3,
            Debug = 4,
            //LAST_INVALID
        }

        public DateTime Time;
        public int Code;
        public String Message;
        public Priority Level;

        public LogMsg()
        {
            Time = DateTime.Now;
        }

        public LogMsg(String message, Priority level = Priority.Info, int code = 0)
        {
            this.Time = DateTime.Now;
            this.Code = code;
            this.Level = level;
            this.Message = message;
        }
    }
}
