using Microsoft.AspNetCore.Mvc;
using CibApi.Models;
using Microsoft.Data.SqlClient;

namespace CibApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncAllController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<SyncAllController> _logger;

        public SyncAllController(IConfiguration configuration, ILogger<SyncAllController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }

        public class SyncAllRequest
        {
            public List<Sala>? salas { get; set; }
            public List<Artigo>? artigos { get; set; }
            public List<Inventario>? inventarios { get; set; }
            public List<InventarioArtigo>? inventarioArtigos { get; set; }
        }

        [HttpPost("sync")]
        public IActionResult SyncAll([FromBody] SyncAllRequest data)
        {
            if (data == null)
                return BadRequest(new { message = "Nenhum dado recebido para sincronização." });

            int totalSalas = 0, totalArtigos = 0, totalInventarios = 0, totalItens = 0;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        // --- SALAS ---
                        if (data.salas != null)
                        {
                            foreach (var s in data.salas)
                            {
                                var cmd = new SqlCommand(@"
                                    IF EXISTS (SELECT 1 FROM Sala WHERE id_sala = @id_sala)
                                        UPDATE Sala SET codigo_barra=@codigo_barra, num_sala=@num_sala, IsSynced=1, LastUpdated=@LastUpdated
                                        WHERE id_sala=@id_sala
                                    ELSE
                                        INSERT INTO Sala (id_sala, codigo_barra, num_sala, IsSynced, LastUpdated)
                                        VALUES (@id_sala, @codigo_barra, @num_sala, 1, @LastUpdated)", connection, transaction);

                                cmd.Parameters.AddWithValue("@id_sala", s.id_sala);
                                cmd.Parameters.AddWithValue("@codigo_barra", s.codigo_barra);
                                cmd.Parameters.AddWithValue("@num_sala", s.num_sala);
                                cmd.Parameters.AddWithValue("@LastUpdated", DateTime.Now);

                                cmd.ExecuteNonQuery();
                                totalSalas++;
                            }
                        }

                        // --- ARTIGOS ---
                        if (data.artigos != null)
                        {
                            foreach (var a in data.artigos)
                            {
                                var cmd = new SqlCommand(@"
                                    IF EXISTS (SELECT 1 FROM Artigo WHERE id_artigo = @id_artigo)
                                        UPDATE Artigo SET id_sala=@id_sala, codigo_barra=@codigo_barra, num_artigo=@num_artigo,
                                            nome_artigo=@nome_artigo, data_update=@data_update, IsSynced=1, LastUpdated=@LastUpdated
                                        WHERE id_artigo=@id_artigo
                                    ELSE
                                        INSERT INTO Artigo (id_artigo, id_sala, codigo_barra, num_artigo, nome_artigo, data_registo, data_update, IsSynced, LastUpdated)
                                        VALUES (@id_artigo, @id_sala, @codigo_barra, @num_artigo, @nome_artigo, @data_registo, @data_update, 1, @LastUpdated)",
                                    connection, transaction);

                                cmd.Parameters.AddWithValue("@id_artigo", a.id_artigo);
                                cmd.Parameters.AddWithValue("@id_sala", a.id_sala);
                                cmd.Parameters.AddWithValue("@codigo_barra", a.codigo_barra);
                                cmd.Parameters.AddWithValue("@num_artigo", a.num_artigo);
                                cmd.Parameters.AddWithValue("@nome_artigo", a.nome_artigo);
                                cmd.Parameters.AddWithValue("@data_registo", a.data_registo);
                                cmd.Parameters.AddWithValue("@data_update", a.data_update);
                                cmd.Parameters.AddWithValue("@LastUpdated", DateTime.Now);

                                cmd.ExecuteNonQuery();
                                totalArtigos++;
                            }
                        }

                        // --- INVENTARIOS ---
                        if (data.inventarios != null)
                        {
                            foreach (var inv in data.inventarios)
                            {
                                var cmd = new SqlCommand(@"
                                    IF EXISTS (SELECT 1 FROM Inventario WHERE id_inventario = @id_inventario)
                                        UPDATE Inventario SET unique_id_sala=@unique_id_sala, nome_inventario=@nome_inventario,
                                            data_inicio=@data_inicio, lastUpdated=@lastUpdated, isSynced=1
                                        WHERE id_inventario=@id_inventario
                                    ELSE
                                        INSERT INTO Inventario (id_inventario, unique_id_sala, nome_inventario, data_inicio, lastUpdated, isSynced)
                                        VALUES (@id_inventario, @unique_id_sala, @nome_inventario, @data_inicio, @lastUpdated, 1)",
                                    connection, transaction);

                                cmd.Parameters.AddWithValue("@id_inventario", inv.id_inventario ?? Guid.NewGuid());
                                cmd.Parameters.AddWithValue("@unique_id_sala", inv.unique_id_sala);
                                cmd.Parameters.AddWithValue("@nome_inventario", inv.nome_inventario);
                                cmd.Parameters.AddWithValue("@data_inicio", inv.data_inicio);
                                cmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);

                                cmd.ExecuteNonQuery();
                                totalInventarios++;
                            }
                        }

                        // --- INVENTARIO ARTIGO ---
                        if (data.inventarioArtigos != null)
                        {
                            foreach (var ia in data.inventarioArtigos)
                            {
                                var cmd = new SqlCommand(@"
                                    IF EXISTS (SELECT 1 FROM InventarioArtigo WHERE id_inventario_artigo = @id_inventario_artigo)
                                        UPDATE InventarioArtigo SET id_inventario=@id_inventario, id_artigo=@id_artigo,
                                            unique_id_sala=@unique_id_sala, lastUpdated=@lastUpdated, isSynced=1
                                        WHERE id_inventario_artigo=@id_inventario_artigo
                                    ELSE
                                        INSERT INTO InventarioArtigo (id_inventario_artigo, id_inventario, id_artigo, unique_id_sala, lastUpdated, isSynced)
                                        VALUES (@id_inventario_artigo, @id_inventario, @id_artigo, @unique_id_sala, @lastUpdated, 1)",
                                    connection, transaction);

                                cmd.Parameters.AddWithValue("@id_inventario_artigo", ia.id_inventario_artigo);
                                cmd.Parameters.AddWithValue("@id_inventario", ia.id_inventario);
                                cmd.Parameters.AddWithValue("@id_artigo", ia.id_artigo);
                                cmd.Parameters.AddWithValue("@unique_id_sala", ia.unique_id_sala);
                                cmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);

                                cmd.ExecuteNonQuery();
                                totalItens++;
                            }
                        }

                        transaction.Commit();
                    }
                }

                return Ok(new
                {
                    message = "Sincronização completa",
                    totalSalas,
                    totalArtigos,
                    totalInventarios,
                    totalItens
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na sincronização geral");
                return StatusCode(500, new { message = "Erro ao sincronizar dados", erro = ex.Message });
            }
        }
    }
}
