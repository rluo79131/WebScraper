using CsvHelper.Configuration.Attributes;

namespace Web.Models
{
    /// <summary>
    /// 球員資訊資料
    /// </summary>
    public class PlayerInfo
    {
        public string Player { get; set; }

        public string G { get; set; }

        public string PTS { get; set; }

        public string TRB { get; set; }

        public string AST { get; set; }

        [Name("FG(%)")]
        public string FG { get; set; }

        [Name("FG3(%)")]
        public string FG3 { get; set; }

        [Name("FT(%)")]
        public string FT { get; set; }

        [Name("eFG(%)")]
        public string EFG { get; set; }

        public string PER { get; set; }

        public string WS { get; set; }
    }
}
