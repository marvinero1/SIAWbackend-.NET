namespace siaw_DBContext.Models_Extra
{
    public class DatosDocVta
    {
        public string estado_doc_vta { get; set; }
        public int coddocumento { get; set; }
        public string id { get; set; }
        public string numeroid { get; set; }
        public DateTime fechadoc { get; set; }
        public string codcliente { get; set; }
        public string nombcliente { get; set; }
        public string nitfactura { get; set; }
        public string tipo_doc_id { get; set; }
        public string codcliente_real { get; set; }
        public string nomcliente_real { get; set; }
        public int codtarifadefecto { get; set; }
        public string codmoneda { get; set; }
        public double subtotaldoc { get; set; }
        public double totaldoc { get; set; }
        public string tipo_vta { get; set; }
        public string codalmacen { get; set; }
        public string codvendedor { get; set; }
        public string preciovta { get; set; }
        public string desctoespecial { get; set; }
        public string preparacion { get; set; }
        public string tipo_cliente { get; set; }
        public string cliente_habilitado { get; set; }
        public string contra_entrega { get; set; }
        public bool vta_cliente_en_oficina { get; set; }
        public string estado_contra_entrega { get; set; }
        public bool desclinea_segun_solicitud { get; set; }
        public string idsol_nivel { get; set; }
        public string nroidsol_nivel { get; set; }
        public bool pago_con_anticipo { get; set; }
        public string niveles_descuento { get; set; }


        // datos al pie de la proforma
        public string transporte { get; set; }
        public string nombre_transporte { get; set; }
        public string fletepor { get; set; }
        public string tipoentrega { get; set; }
        public string direccion { get; set; }
        public string ubicacion { get; set; }
        public string latitud { get; set; }
        public string longitud { get; set; }
        public int nroitems { get; set; }
        public double totdesctos_extras { get; set; }
        public double totrecargos { get; set; }

        // complemento marorista-dimediado / o complemento para descto por importe
        public string tipo_complemento { get; set; } = "";
        public string idpf_complemento { get; set; } = "";
        public string nroidpf_complemento { get; set; } = "";

        // para facturacion mostrador
        public string idFC_complementaria { get; set; } = "";
        public string nroidFC_complementaria { get; set; } = "";
        public string nrocaja { get; set; } = "";
        public string nroautorizacion { get; set; } = "";
        public DateTime fechalimite_dosificacion { get; set; }
        public string tipo_caja { get; set; } = "";
        public string version_codcontrol { get; set; } = "";
        public string nrofactura { get; set; } = "";
        public string nroticket { get; set; } = "";
        public string idanticipo { get; set; } = "";
        public string noridanticipo { get; set; } = "";
        public Double monto_anticipo { get; set; }
        public string idpf_solurgente { get; set; } = "";
        public string noridpf_solurgente { get; set; } = "";


    }
}
