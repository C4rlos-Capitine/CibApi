using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CibApi.Models;

namespace CibApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventarioController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<InventarioController> _logger;

        public InventarioController(IConfiguration configuration, ILogger<InventarioController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }

        // ✅ GET: api/inventario
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var lista = new List<Inventario>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"SELECT id_inventario, unique_id_sala, nome_inventario, data_inicio, lastUpdated, isSynced 
                                  FROM Inventario";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Inventario
                            {
                                id_inventario = reader.GetGuid(reader.GetOrdinal("id_inventario")),
                                unique_id_sala = reader.GetGuid(reader.GetOrdinal("unique_id_sala")),
                                nome_inventario = reader.GetString(reader.GetOrdinal("nome_inventario")),
                                data_inicio = reader.GetDateTime(reader.GetOrdinal("data_inicio")),
                                lastUpdated = reader.GetDateTime(reader.GetOrdinal("lastUpdated")),
                                isSynced = reader.GetBoolean(reader.GetOrdinal("isSynced"))
                            });
                        }
                    }
                }

                return Ok(lista);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar inventários");
                return StatusCode(500, new { message = "Erro ao buscar inventários", erro = ex.Message });
            }
        }

        // ✅ POST: api/inventario
        [HttpPost]
        public IActionResult Create([FromBody] Inventario inventario)
        {
            try
            {
                if (inventario == null)
                    return BadRequest(new { message = "Dados do inventário inválidos" });

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"INSERT INTO Inventario 
                                (id_inventario, unique_id_sala, nome_inventario, data_inicio, lastUpdated, isSynced)
                              VALUES 
                                (@id_inventario, @unique_id_sala, @nome_inventario, @data_inicio, @lastUpdated, @isSynced)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id_inventario", inventario.id_inventario ?? Guid.NewGuid());
                        command.Parameters.AddWithValue("@unique_id_sala", inventario.unique_id_sala);
                        command.Parameters.AddWithValue("@nome_inventario", inventario.nome_inventario);
                        command.Parameters.AddWithValue("@data_inicio", inventario.data_inicio);
                        command.Parameters.AddWithValue("@lastUpdated", inventario.lastUpdated);
                        command.Parameters.AddWithValue("@isSynced", inventario.isSynced);

                        command.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "Inventário inserido com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar inventário");
                return StatusCode(500, new { message = "Erro ao criar inventário", erro = ex.Message });
            }
        }

        // ✅ POST: api/inventario/lote
        [HttpPost("lote")]
        public IActionResult CreateBatch([FromBody] List<Inventario> inventarios)
        {
            if (inventarios == null || inventarios.Count == 0)
                return BadRequest(new { message = "Nenhum inventário fornecido" });

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    foreach (var inv in inventarios)
                    {
                        var query = @"INSERT INTO Inventario 
                                    (id_inventario, unique_id_sala, nome_inventario, data_inicio, lastUpdated, isSynced)
                                  VALUES 
                                    (@id_inventario, @unique_id_sala, @nome_inventario, @data_inicio, @lastUpdated, @isSynced)";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id_inventario", inv.id_inventario ?? Guid.NewGuid());
                            command.Parameters.AddWithValue("@unique_id_sala", inv.unique_id_sala);
                            command.Parameters.AddWithValue("@nome_inventario", inv.nome_inventario);
                            command.Parameters.AddWithValue("@data_inicio", inv.data_inicio);
                            command.Parameters.AddWithValue("@lastUpdated", inv.lastUpdated);
                            command.Parameters.AddWithValue("@isSynced", inv.isSynced);

                            command.ExecuteNonQuery();
                        }
                    }
                }

                return Ok(new { message = $"{inventarios.Count} inventário(s) inserido(s) com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inserir lote de inventários");
                return StatusCode(500, new { message = "Erro ao inserir lote", erro = ex.Message });
            }
        }

        // ✅ POST: api/inventario/sync
        // ✅ POST: api/inventario/sync
        [HttpPost("sync")]
        public IActionResult Sync([FromBody] InventarioSyncRequest request)
        {
            try
            {
                var inventarios = request.inventarios;

                if (inventarios == null || inventarios.Count == 0)
                    return BadRequest(new { message = "Nenhum inventário recebido para sincronização" });

                int inseridos = 0, atualizados = 0;

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    foreach (var inv in inventarios)
                    {
                        var checkQuery = "SELECT COUNT(*) FROM Inventario WHERE id_inventario = @id_inventario";
                        using (var checkCmd = new SqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id_inventario", inv.id_inventario ?? Guid.NewGuid());
                            int existe = (int)checkCmd.ExecuteScalar();

                            if (existe > 0)
                            {
                                // Atualiza
                                var updateQuery = @"UPDATE Inventario
                                            SET unique_id_sala = @unique_id_sala,
                                                nome_inventario = @nome_inventario,
                                                data_inicio = @data_inicio,
                                                lastUpdated = @lastUpdated,
                                                isSynced = 1
                                            WHERE id_inventario = @id_inventario";

                                using (var updateCmd = new SqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@unique_id_sala", inv.unique_id_sala);
                                    updateCmd.Parameters.AddWithValue("@nome_inventario", inv.nome_inventario);
                                    updateCmd.Parameters.AddWithValue("@data_inicio", inv.data_inicio);
                                    updateCmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);
                                    updateCmd.Parameters.AddWithValue("@id_inventario", inv.id_inventario);

                                    updateCmd.ExecuteNonQuery();
                                    atualizados++;
                                }
                            }
                            else
                            {
                                // Insere
                                var insertQuery = @"INSERT INTO Inventario
                                            (id_inventario, unique_id_sala, nome_inventario, data_inicio, lastUpdated, isSynced)
                                            VALUES (@id_inventario, @unique_id_sala, @nome_inventario, @data_inicio, @lastUpdated, 1)";

                                using (var insertCmd = new SqlCommand(insertQuery, connection))
                                {
                                    insertCmd.Parameters.AddWithValue("@id_inventario", inv.id_inventario ?? Guid.NewGuid());
                                    insertCmd.Parameters.AddWithValue("@unique_id_sala", inv.unique_id_sala);
                                    insertCmd.Parameters.AddWithValue("@nome_inventario", inv.nome_inventario);
                                    insertCmd.Parameters.AddWithValue("@data_inicio", inv.data_inicio);
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
                    message = "Sincronização concluída",
                    inseridos,
                    atualizados,
                    total = inventarios.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao sincronizar inventário: " + ex.Message);
                return StatusCode(500, new { message = "Erro interno no servidor", erro = ex.Message });
            }
        }

    }
}
