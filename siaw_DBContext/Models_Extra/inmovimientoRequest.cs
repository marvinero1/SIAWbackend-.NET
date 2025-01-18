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
}
