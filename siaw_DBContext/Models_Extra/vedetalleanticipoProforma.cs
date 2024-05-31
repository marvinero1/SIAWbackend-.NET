namespace siaw_DBContext.Models_Extra
{
    public class vedetalleanticipoProforma
    {
        public int codproforma { get; set; }
        public int codanticipo { get; set; }
        public string docanticipo { get; set; }
        public string id_anticipo { get; set; }
        public int nroid_anticipo { get; set; }
        public double monto { get; set; }
        public double tdc { get; set; }
        public string codmoneda { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string horareg { get; set; }
        public string codvendedor { get; set; }
    }
}
