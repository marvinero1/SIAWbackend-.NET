namespace siaw_DBContext.Models_Extra
{
    public class itemDataMatriz
    {
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public string udm { get; set; }
        public double porceniva { get; set; }
        public int ? empaque { get; set; }
        public double cantidad_pedida { get; set; }
        public double cantidad { get; set; }
        public double porcen_mercaderia { get; set; }
        public int codtarifa { get; set; }
        public int coddescuento { get; set; }
        public double preciolista { get; set; }
        public string niveldesc { get; set; }
        public double porcendesc { get; set; }
        public double preciodesc { get; set; }
        public double precioneto { get; set; }
        public double total { get; set; }
        public bool cumple { get; set;} = true;
        // PARA PINTAR
        public bool cumpleMin { get; set; } = true;
        public bool cumpleEmp { get; set; } = true;

        public int nroitem { get; set; } = 0;

        // no mostrar 
        public double porcentaje { get; set; } = 0;
        public double monto_descto { get; set; } = 0;
        public double subtotal_descto_extra { get; set; } = 0;

    }
    public class itemDataMatrizMaxVta
    {
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public string udm { get; set; }
        public double porceniva { get; set; }
        public string niveldesc { get; set; }
        public double porcendesc { get; set; }
        public double porcen_mercaderia { get; set; }
        public double cantidad_pedida { get; set; }
        public double cantidad { get; set; }
        public int codtarifa { get; set; }
        public int coddescuento { get; set; }
        public double precioneto { get; set; }
        public double preciodesc { get; set; }
        public double preciolista { get; set; }
        public double total { get; set; }
        public bool cumple { get; set; } = true;
        public int nroitem { get; set; } = 0;

        // no mostrar 
        public double porcentaje { get; set; } = 0;
        public double monto_descto { get; set; } = 0;
        public double subtotal_descto_extra { get; set; } = 0;
        public double cantidad_pf_anterior { get; set; } = 0;
        public double cantidad_pf_total { get; set; } = 0;

    }
    public class tabladescuentos
    {
        public int codproforma { get; set; }
        public int coddesextra { get; set; }
        public decimal porcen { get; set; }
        public decimal montodoc { get; set; }
        public int? codcobranza { get; set; } = 0;
        public int? codcobranza_contado { get; set; } = 0;
        public int? codanticipo { get; set; } = 0;
        public int id { get; set; }

        public string? aplicacion { get; set; }
        public string? codmoneda { get; set; }
        public string? descrip { get; set; }

        // no mostrar
        public double total_dist { get; set; } = 0;
        public double total_desc { get; set; } = 0;
        public double montorest { get; set; } = 0;
    }

    public class tablarecargos
    {
        public int codproforma { get; set; }
        public int codrecargo { get; set; }
        public decimal porcen { get; set; }
        public decimal monto { get; set; }
        public string moneda { get; set; }
        public decimal montodoc { get; set; }
        public int? codcobranza { get; set; } = 0;
        public string? descripcion { get; set; } = "";

    }

    public class tblcbza_deposito
    {
        public int codproforma { get; set; }
        public int codcobranza { get; set; }
        public int codcobranza_contado { get; set; }
        public int codanticipo { get; set; }
        public double montodist { get; set; }
        public double montodescto { get; set; }
        public double montorest { get; set; }
    }

    public class itemDataSugerencia
    {
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public double cantidad { get; set; }
        public string cantidad_sugerida { get; set; }
        public double cantidad_sugerida_aplicable { get; set; }
        public double empaque_caja_cerrada { get; set; }
        public double porcentaje { get; set; }
        public double cantidad_porcentaje { get; set; }
        public double diferencia { get; set; }
        public string obs { get; set; }
    }

    public partial class veproforma1_2
    {
        public int codproforma { get; set; }
        public string coditem { get; set; }
        public int ? empaque { get; set; }
        public decimal cantidad { get; set; }
        public string udm { get; set; }
        public decimal precioneto { get; set; }
        public decimal? preciodesc { get; set; }
        public string niveldesc { get; set; }
        public decimal preciolista { get; set; }
        public int codtarifa { get; set; }
        public short coddescuento { get; set; }
        public decimal total { get; set; }
        public decimal? cantaut { get; set; }
        public decimal? totalaut { get; set; }
        public string? obs { get; set; }
        public decimal? porceniva { get; set; }
        public decimal? cantidad_pedida { get; set; }
        public decimal? peso { get; set; }
        public int? nroitem { get; set; }
        public int id { get; set; }
        public decimal porcen_mercaderia { get; set; }
    }
}
