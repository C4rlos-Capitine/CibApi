using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CibApi.Models;

namespace CibApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventarioArtigoController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<InventarioArtigoController> _logger;

        public InventarioArtigoController(IConfiguration configuration, ILogger<InventarioArtigoController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }

        // ✅ GET: api/inventarioartigo/byInventario/{id}
        [HttpGet("byInventario/{id}")]
        public IActionResult GetByInventario(Guid id)
        {
            try
            {
                var lista = new List<InventarioArtigo>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var query = @"SELECT id_inventario_artigo, id_inventario, id_artigo, unique_id_sala, lastUpdated, isSynced
                                  FROM InventarioArtigo
                                  WHERE id_inventario = @id";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new InventarioArtigo
                                {
                                    id_inventario_artigo = reader.GetGuid(reader.GetOrdinal("id_inventario_artigo")),
                                    id_inventario = reader.GetGuid(reader.GetOrdinal("id_inventario")),
                                    id_artigo = reader.GetGuid(reader.GetOrdinal("id_artigo")),
                                    unique_id_sala = reader.GetGuid(reader.GetOrdinal("unique_id_sala")),
                                    lastUpdated = reader.GetDateTime(reader.GetOrdinal("lastUpdated")),
                                    isSynced = reader.GetBoolean(reader.GetOrdinal("isSynced"))
                                });
                            }
                        }
                    }
                }

                return Ok(lista);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar InventarioArtigo por inventário");
                return StatusCode(500, new { message = "Erro ao buscar dados", erro = ex.Message });
            }
        }

        // ✅ POST: api/inventarioartigo
        [HttpPost]
        public IActionResult Create([FromBody] InventarioArtigo item)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var query = @"INSERT INTO InventarioArtigo 
                                (id_inventario_artigo, id_inventario, id_artigo, unique_id_sala, lastUpdated, isSynced)
                              VALUES 
                                (@id_inventario_artigo, @id_inventario, @id_artigo, @unique_id_sala, @lastUpdated, @isSynced)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id_inventario_artigo", item.id_inventario_artigo);
                        command.Parameters.AddWithValue("@id_inventario", item.id_inventario);
                        command.Parameters.AddWithValue("@id_artigo", item.id_artigo);
                        command.Parameters.AddWithValue("@unique_id_sala", item.unique_id_sala);
                        command.Parameters.AddWithValue("@lastUpdated", item.lastUpdated);
                        command.Parameters.AddWithValue("@isSynced", item.isSynced);

                        command.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "InventarioArtigo criado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar InventarioArtigo");
                return StatusCode(500, new { message = "Erro ao criar registro", erro = ex.Message });
            }
        }

        // ✅ POST: api/inventarioartigo/lote
        [HttpPost("lote")]
        public IActionResult CreateBatch([FromBody] List<InventarioArtigo> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new { message = "Nenhum item fornecido para inserção" });

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    foreach (var item in items)
                    {
                        var query = @"INSERT INTO InventarioArtigo 
                                    (id_inventario_artigo, id_inventario, id_artigo, unique_id_sala, lastUpdated, isSynced)
                                  VALUES 
                                    (@id_inventario_artigo, @id_inventario, @id_artigo, @unique_id_sala, @lastUpdated, @isSynced)";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id_inventario_artigo", item.id_inventario_artigo);
                            command.Parameters.AddWithValue("@id_inventario", item.id_inventario);
                            command.Parameters.AddWithValue("@id_artigo", item.id_artigo);
                            command.Parameters.AddWithValue("@unique_id_sala", item.unique_id_sala);
                            command.Parameters.AddWithValue("@lastUpdated", item.lastUpdated);
                            command.Parameters.AddWithValue("@isSynced", item.isSynced);

                            command.ExecuteNonQuery();
                        }
                    }
                }

                return Ok(new { message = $"{items.Count} registro(s) inserido(s) com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inserir lote de InventarioArtigo");
                return StatusCode(500, new { message = "Erro ao inserir lote", erro = ex.Message });
            }
        }

        // ✅ POST: api/inventarioartigo/sync
        [HttpPost("sync")]
        public IActionResult Sync([FromBody] List<InventarioArtigo> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new { message = "Nenhum item recebido para sincronização" });

            int inseridos = 0, atualizados = 0;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    foreach (var item in items)
                    {
                        // Verifica se o registro já existe
                        var checkQuery = "SELECT COUNT(*) FROM InventarioArtigo WHERE id_inventario_artigo = @id";
                        using (var checkCmd = new SqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.id_inventario_artigo);
                            int existe = (int)checkCmd.ExecuteScalar();

                            if (existe > 0)
                            {
                                // Atualiza registro existente
                                var updateQuery = @"UPDATE InventarioArtigo
                                                    SET id_inventario = @id_inventario,
                                                        id_artigo = @id_artigo,
                                                        unique_id_sala = @unique_id_sala,
                                                        lastUpdated = @lastUpdated,
                                                        isSynced = 1
                                                    WHERE id_inventario_artigo = @id";
                                using (var updateCmd = new SqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@id_inventario", item.id_inventario);
                                    updateCmd.Parameters.AddWithValue("@id_artigo", item.id_artigo);
                                    updateCmd.Parameters.AddWithValue("@unique_id_sala", item.unique_id_sala);
                                    updateCmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);
                                    updateCmd.Parameters.AddWithValue("@id", item.id_inventario_artigo);

                                    updateCmd.ExecuteNonQuery();
                                    atualizados++;
                                }
                            }
                            else
                            {
                                // Insere novo registro
                                var insertQuery = @"INSERT INTO InventarioArtigo
                                                    (id_inventario_artigo, id_inventario, id_artigo, unique_id_sala, lastUpdated, isSynced)
                                                    VALUES (@id_inventario_artigo, @id_inventario, @id_artigo, @unique_id_sala, @lastUpdated, 1)";
                                using (var insertCmd = new SqlCommand(insertQuery, connection))
                                {
                                    insertCmd.Parameters.AddWithValue("@id_inventario_artigo", item.id_inventario_artigo);
                                    insertCmd.Parameters.AddWithValue("@id_inventario", item.id_inventario);
                                    insertCmd.Parameters.AddWithValue("@id_artigo", item.id_artigo);
                                    insertCmd.Parameters.AddWithValue("@unique_id_sala", item.unique_id_sala);
                                    insertCmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);

                                    insertCmd.ExecuteNonQuery();
                                    inseridos++;
                                }
                            }
                        }
                    }
                }

                return Ok(new
                {
                    message = "Sincronização concluída com sucesso",
                    inseridos,
                    atualizados,
                    total = items.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar InventarioArtigo");
                return StatusCode(500, new { message = "Erro interno no servidor", erro = ex.Message });
            }
        }
    }
}
