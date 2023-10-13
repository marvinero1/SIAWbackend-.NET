using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_DBContext.Models
{
    public partial class vetipo_control_vtas
    {
        public int codigo { get; set; }
        public int orden { get; set; }
        public string codcontrol { get; set; } = "";
        public bool? grabar { get; set; }
        public string grabar_aprobar { get; set; } = "";
        public bool? habilitado_pf { get; set; }
        public bool? habilitado_nr { get; set; }
        public bool? habilitado_fc { get; set; }
        public string descripcion { get; set; } = "";
        public string codservicio { get; set; } = "";
    }
}
