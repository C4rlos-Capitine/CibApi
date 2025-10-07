using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CibApi.Models
{
    public class Inventario
    {
        [Key]
        public Guid? id_inventario { get; set; } = Guid.NewGuid();
        public Guid unique_id_sala { get; set; }
        public string nome_inventario { get; set; }
        public DateTime data_inicio { get; set; }
        public DateTime lastUpdated { get; set; } = DateTime.Now;
        public bool isSynced { get; set; } = false;

    }
}
