using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApi.Models;

namespace TaskApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly TaskContext _context;

        public TaskController(TaskContext context)
        {
            _context = context;
        }

        // GET: Task
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTaskItems()
        {
            if (_context.TaskItems == null)
            {
                return NotFound();
            }
            return await _context.TaskItems.ToListAsync();
        }

        // GET: Task/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTaskItem(long id)
        {
            if (_context.TaskItems == null)
            {
                return NotFound();
            }
            var taskItem = await _context.TaskItems.FindAsync(id);

            if (taskItem == null)
            {
                return NotFound();
            }

            return taskItem;
        }

        // PUT: api/Task/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTaskItem(long id, TaskItem taskItem)
        {
            if (id != taskItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(taskItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: Task
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TaskItem>> PostTaskItem(TaskItem taskItem)
        {
            if (_context.TaskItems == null)
            {
                return Problem("Entity set 'TaskContext.TaskItems'  is null.");
            }
            _context.TaskItems.Add(taskItem);
            await _context.SaveChangesAsync();


            Console.WriteLine(System.Environment.GetEnvironmentVariable("RABBITMQ_HOST"));
            Console.WriteLine(System.Environment.GetEnvironmentVariable("RABBITMQ_PORT"));
            var factory = new ConnectionFactory()
            {
                HostName = System.Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(System.Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };
            var contentToSend = new StringContent(JsonSerializer.Serialize(taskItem), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: "tasks",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null

                );

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(taskItem));

                channel.BasicPublish(
                    exchange: "",
                    routingKey: "tasks",
                    basicProperties: null,
                    body: body
                );
            }

            return CreatedAtAction(nameof(GetTaskItem), new { id = taskItem.Id }, taskItem);
        }

        // DELETE: api/Task/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskItem(long id)
        {
            if (_context.TaskItems == null)
            {
                return NotFound();
            }
            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null)
            {
                return NotFound();
            }

            _context.TaskItems.Remove(taskItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskItemExists(long id)
        {
            return (_context.TaskItems?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
