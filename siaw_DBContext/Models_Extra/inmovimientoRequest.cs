using siaw_DBContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_DBContext.Models_Extra
{

    public class requestGabrar
    {
        public inmovimiento cabecera { get; set; }
        public List<tablaDetalleNM> tablaDetalle { get; set; }
    }

    public class tablaDetalleNM
    {
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public string udm { get; set; }
        public string codaduana { get; set; }
        public decimal cantidad { get; set; }
        public double costo { get; set; }

        public decimal cantidad_revisada { get; set; } = 0;
        public string nuevo { get; set; } = "si";
        public decimal diferencia { get; set; } = 0;
    }

    public class respValidaDecimales
    {
        public string cabecera { get; set; }
        public List<string> detalleObs { get; set; }
        public string alerta { get; set; }
        public bool cumple { get; set; }
    }

    public class addNMDespachos 
    {
        public int codmovimiento { get; set; }
        public int codigo { get; set; }
        public string preparacion { get; set; }
        public string tipoentrega { get; set; }
        public int codalmacen {  get; set; }
        public int codvendedor { get; set; }
        public string doc {  get; set; }
        public DateTime fecha { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public int Codcliente { get; set; }
        public string nomcliente { get; set; }
        public string odc { get; set; }
        public DateTime frecibido { get; set; }
        public string hrecibido { get; set; }
        public double hojas { get; set; }
        public string estado { get; set; }
        public int preparapor { get; set; }
        public string nombpersona { get; set; }
        public decimal peso { get; set; }
        public string total { get; set; }
        public string codmoneda { get; set; }
        public int nroitems { get; set; }
        public double bolsas { get; set; }
        public double cajas { get; set; }
        public double amarres { get; set; }
        public double bultos { get; set; }
        public int resdespacho { get; set; }
        public DateTime fterminado { get; set; }
        public string hterminado { get; set; }
        public string guia { get; set; }
        public string nombtrans { get; set; }
        public string tipotrans { get; set; }
        public DateTime fdespacho { get; set; }
        public string nombchofer { get; set; }
        public string celchofer { get; set; }
        public string nroplaca { get; set; }
        public string monto_flete { get; set; }
        public string hdespacho { get; set; }
        public string tarribo { get; set; }
        public string obs {  get; set; }
    }

    public class dataPorConcepto
    {
        public bool codalmdestinoReadOnly { get; set; }
        public bool codalmorigenReadOnly { get; set; }
        public bool traspaso { get; set; }
        public bool fidEnable { get; set; } = true;
        public bool fnumeroidEnable { get; set; } = true;
        public bool codpersonadesdeReadOnly { get; set; } = true;
        public int codalmdestinoText { get; set; }
        public int codalmorigenText { get; set; }
        public int factor { get; set; }

        public bool codclienteReadOnly { get; set; } = true;
        public bool cargar_proformaEnabled { get; set; } = true;
        public bool cvenumeracion1Enabled { get; set; } = true;
        public bool id_proforma_solReadOnly { get; set; } = true;
        public bool numeroidproforma_solReadOnly { get; set; } = true;
    }

}
