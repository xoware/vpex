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
            Emergency = 0,
            Alert = 1,
            Critical = 2,
            Error = 3,
            Warning = 4,
            Notice = 5,
            Info = 6,
            Debug = 7,
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
