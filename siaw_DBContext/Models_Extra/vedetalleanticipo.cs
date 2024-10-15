namespace siaw_DBContext.Models_Extra
{
    public class vedetalleanticipo
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public int codvendedor { get; set; }
        public DateTime fecha { get; set; }
        public string codcliente { get; set; }
        public double monto { get; set; }
        public string codmoneda { get; set; }
        public double montorest { get; set; }
        public bool deposito_cliente { get; set; }
        public string iddeposito { get; set; }
        public int numeroiddeposito { get; set; }
    }
}
