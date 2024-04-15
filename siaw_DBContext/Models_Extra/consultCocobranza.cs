using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace siaw_DBContext.Models_Extra
{
    public class consultCocobranza
    {
        public string nro { get; set; }
        public int tipo { get; set; }
        public bool aplicado { get; set; }
        public int codalmacen { get; set;}
        public int codcobranza { get; set; }

        public string idcbza { get; set; }
        public int nroidcbza { get; set; }
        public DateTime fecha_cbza { get; set; }
        public DateTime fdeposito { get; set; }
        public string cliente { get; set; }

        public string nit { get; set; }
        public string nomcliente_nit { get; set; }
        public decimal monto_cbza { get; set; }
        public decimal monto_dis { get; set; }
        public string moncbza { get; set; }

        public bool reciboanulado { get; set; }
        public string iddeposito { get; set; }
        public int numeroiddeposito { get; set; }
        public bool deposito_cliente { get; set; }
        public bool contra_entrega { get; set; }

        public int codremision { get; set; }
        public string idrem { get; set; }
        public int nroidrem { get; set; }
        public DateTime fecha_remi { get; set; }
        public int nrocuota { get; set; }

        public DateTime vencimiento { get; set; }
        public DateTime vencimiento_cliente { get; set; }
        public decimal monto { get; set; }
        public decimal montopagado { get; set; }
        public bool deposito_en_mora_habilita_descto { get; set; }

        public string codcliente { get; set; }
        public int codigo { get; set; }

        public string valido { get; set; }
        public string obs { get; set; }
        public string obs1 { get; set; }
        public string tipo_pago { get; set; }
        public string monpago { get; set; }
    }



    public class dtdepositos_pendientes
    {
        public string cliente { get; set; }
        public int ? codcobranza { get; set; }
        public string idcbza { get; set; }
        public int nroidcbza { get; set; }
        public string iddeposito { get; set; }

        public int numeroiddeposito { get; set; }
        public DateTime fecha_cbza { get; set; }
        public string tipo_pago { get; set; }
        public int tipo { get; set; }
        public decimal monto_dis { get; set; }

        public decimal monto_cbza { get; set; }
        public string moncbza { get; set; }
        
    }
}
