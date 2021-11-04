using System.Collections.Generic;

namespace MemberSite.Service.Model
{
    public interface ISelLang
    {
        string GetErrMsg(int code);
    }

    public class ApiResult
    {
        public int ErrorCode { get; set; }
        private string _Message;
        protected ISelLang SelLang;
        public string Message
        {
            get
            {
                if (!string.IsNullOrEmpty(_Message))
                {
                    return _Message;
                }
                else
                {
                    if (SelLang != null)
                        _Message = SelLang.GetErrMsg(ErrorCode);
                }

                return _Message;
            }
            set
            {
                _Message = value;
            }
        }

        public void SetSelLang(ISelLang selLang)
        {
            this.SelLang = selLang;
        }
        public string RawErrorCode { get; set; }
        public string RawMessage { get; set; }
        public ApiResult() : this(9000, "", "", "") { }
        public ApiResult(ISelLang selLang) : this(9000, "", "", "") { this.SelLang = selLang; }
        public ApiResult(int ErrorCode, string Message) : this(ErrorCode, Message, "", "") { }
        public ApiResult(int _ErrorCode, string Message, string RawErrorCode, string RawMessage)
        {
            this.ErrorCode = _ErrorCode;
            this.Message = Message;
            this.RawErrorCode = RawErrorCode;
            this.RawMessage = RawMessage;
        }
    }
    public class ApiResult<TData> : ApiResult
    {
        public ApiResult() : this(9000, "",  default) { }
        public ApiResult(ISelLang selLang) : this(9000, "", default) { this.SelLang = selLang; }
        public ApiResult(int ErrorCode, string Message, TData data) : base(ErrorCode, Message) { this.Data = data; }
        public TData Data { get; set; }
    }

    public class ApiListResult<TData> : ApiResult
    {
        public ApiListResult() : this(9000, "") { }
        public ApiListResult(ISelLang selLang) : this(9000, "") { SelLang = selLang; }
        public ApiListResult(int ErrorCode, string Message) : base(ErrorCode, Message) { }
        public IEnumerable<TData> Data { get; set; }
        public int TotalPages { get; set; }
        public int PageNumber { get; set; }
        public int RowCount { get; set; }
    }
}
