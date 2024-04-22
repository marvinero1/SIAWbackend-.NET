namespace siaw_DBContext.Models_Extra
{
    public class itemDataMatriz
    {
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public string udm { get; set; }
        public float porceniva { get; set; }
        public float cantidad_pedida { get; set; }
        public float cantidad { get; set; }
        public float porcen_mercaderia { get; set; }
        public int codtarifa { get; set; }
        public int coddescuento { get; set; }
        public float preciolista { get; set; }
        public string niveldesc { get; set; }
        public float porcendesc { get; set; }
        public float preciodesc { get; set; }
        public float precioneto { get; set; }
        public float total { get; set; }
        public bool cumple { get; set;} = true;
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
        public float porceniva { get; set; }
        public string niveldesc { get; set; }
        public float porcendesc { get; set; }
        public float porcen_mercaderia { get; set; }
        public float cantidad_pedida { get; set; }
        public float cantidad { get; set; }
        public int codtarifa { get; set; }
        public int coddescuento { get; set; }
        public float precioneto { get; set; }
        public float preciodesc { get; set; }
        public float preciolista { get; set; }
        public float total { get; set; }
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
        public int? codcobranza { get; set; }
        public int? codcobranza_contado { get; set; }
        public int? codanticipo { get; set; }
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
        public int? codcobranza { get; set; }
        public string? descripcion { get; set; }

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
}
