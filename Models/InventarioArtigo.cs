using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CibApi.Models
{
    public class InventarioArtigo
    {
        [Key]
        public Guid id_inventario_artigo { get; set; } = Guid.NewGuid();

        [Required]
        public Guid id_inventario { get; set; }
        public Guid unique_id_sala { get; set; }
        public Guid id_artigo { get; set; }
        public DateTime lastUpdated { get; set; } = DateTime.Now;
        public bool isSynced { get; set; } = false;
    }
}
